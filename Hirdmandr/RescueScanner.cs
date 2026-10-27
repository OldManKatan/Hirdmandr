using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hirdmandr
{
    // R3c rescue refactor: the world-rescue scanner. Replaces the old ZoneSystem.PlaceLocations
    // Harmony hook (RescueSpawnHook) with a periodic, deterministic pass. Every Interval seconds -
    // on a coroutine hosted by the persistent plugin MonoBehaviour - and on the server only, it
    // scans the (2*GridRadius+1)^2 zones around each player on the server and offers an
    // unrescued rescue at HitPercent of whitelisted, unclaimed structures. The centers are the
    // connected peers (ZNet.instance.GetPeers()) plus the local player when this process has
    // one - in SP the server IS the host process, and on a hosted MP world the host is a local
    // player, not a remote peer, so GetPeers() alone does not cover the host in every topology
    // (see GetScanCenters).
    //
    // Deterministic: a structure's offer is a pure function of (structure name, zone) via a stable
    // hash, so the same structure offers (or doesn't) on every pass and on every client - no
    // re-rolling, no per-client divergence. Once a rescue at a structure is welcomed home, the
    // structure is marked (RescueLedger.SetWelcomed) and never re-offers.
    internal static class RescueScanner
    {
        public const float Interval = 5f;     // seconds between passes
        public const int GridRadius = 1;      // 3x3 zones around each connected player
        public const int HitPercent = 20;     // deterministic offer chance per structure

        private static bool s_started;
        private static readonly HashSet<int> s_live = new HashSet<int>();

        // Start the scanner. `host` is the persistent plugin MonoBehaviour that carries the
        // coroutine. Safe to call from Awake; repeat calls are ignored.
        public static void Start(MonoBehaviour host)
        {
            if (s_started || host == null)
            {
                return;
            }
            s_started = true;
            host.StartCoroutine(Loop());
        }

        private static IEnumerator Loop()
        {
            while (true)
            {
                try
                {
                    RunPass();
                }
                catch (System.Exception e)
                {
                    // A failing pass must never take the game down; log and retry next interval.
                    Jotunn.Logger.LogError("[Hirdmandr] RescueScanner pass threw:\n" + e);
                }
                yield return new WaitForSeconds(Interval);
            }
        }

        private static void RunPass()
        {
            if (!Ready())
            {
                return;
            }

            // Rebuild the live set from the world as it is right now (dead rescues are gone from
            // it automatically), so Claimed() below reflects the current state.
            RescueLedger.CollectLive(s_live);

            foreach (Vector3 center in GetScanCenters())
            {
                ScanAround(center);
            }
        }

        // The scan centers: one per player on this server. The connected peers come from
        // ZNet.instance.GetPeers(); the local player (when this process has one) is added as
        // well, because GetPeers() does not reliably list the host: in single player the
        // server IS the host process, and on a hosted MP world the host is a local player,
        // not a remote peer. Dedup: if a peer already sits within 10m of the local player
        // (SP: the host listed both ways) the window is scanned only once this pass.
        // An empty list means nothing to scan (e.g. a dedicated MP server, no one connected).
        internal static List<Vector3> GetScanCenters()
        {
            var centers = new List<Vector3>();
            var peers = ZNet.instance.GetPeers();
            if (peers != null)
            {
                foreach (var peer in peers)
                {
                    centers.Add(peer.GetRefPos());
                }
            }
            var local = Player.m_localPlayer;
            if (local != null && local.transform != null)
            {
                Vector3 lp = local.transform.position;
                bool have = false;
                foreach (Vector3 c in centers)
                {
                    if ((c - lp).sqrMagnitude <= 100f)
                    {
                        have = true;
                        break;
                    }
                }
                if (!have)
                {
                    centers.Add(lp);
                }
            }
            return centers;
        }

        // Scan the (2*GridRadius+1)^2 zones around `center` and offer an unrescued rescue at
        // HitPercent of whitelisted, unclaimed structures (deterministic rules: class comment).
        private static void ScanAround(Vector3 center)
        {
            Vector2s pzone = ZoneSystem.GetZone(center);

            for (int dx = -GridRadius; dx <= GridRadius; dx++)
            {
                for (int dy = -GridRadius; dy <= GridRadius; dy++)
                {
                    Vector2s zone = new Vector2s((short)(pzone.x + dx), (short)(pzone.y + dy));
                    ZoneSystem.LocationInstance inst;
                    if (!ZoneSystem.instance.m_locationInstances.TryGetValue(zone, out inst))
                    {
                        continue;
                    }
                    if (inst.m_location == null || !inst.m_placed)
                    {
                        continue;
                    }
                    if (!RescueWhitelist.Contains(inst.m_location))
                    {
                        continue;
                    }

                    int h = HashFor(inst.m_location.m_name, zone);
                    if (!Hits(h))
                    {
                        // Deterministic hash is above the 20% gate - this structure does not
                        // offer a rescue (same result every pass, every client).
                        continue;
                    }
                    if (RescueLedger.Claimed(h, s_live))
                    {
                        // A rescue is already here (live) or was welcomed home (global key) -
                        // never re-offer.
                        continue;
                    }

                    // Eligible and unclaimed - place one. PlaceAtLocation stamps
                    // hmnpc_rescuepoiHash = h on the new ZDO.
                    if (RescueSpawner.PlaceAtLocation(inst, "rescue scan", h) != null)
                    {
                        // Two peers' windows can overlap within one pass; claim the structure in
                        // the live set now so the overlapping window does not place a second
                        // rescue at the same structure this pass (the live set is re-derived
                        // next pass anyway).
                        s_live.Add(h);
                    }
                }
            }
        }

        // Only the server scans and spawns: every client would otherwise see the same unclaimed
        // structure and each spawn its own rescue. In SP the server is the host; in MP it is
        // the (headless) dedicated server. There is deliberately NO local-player gate here -
        // the server has no local player in MP, and the pass is centered on the connected
        // peers instead (see RunPass).
        private static bool Ready()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return false;
            }
            if (ZNetScene.instance == null)
            {
                return false;
            }
            if (ZoneSystem.instance == null || ZoneSystem.instance.m_locationInstances == null)
            {
                return false;
            }
            if (ZDOMan.instance == null)
            {
                return false;
            }
            return true;
        }

        // Stable identity of a structure for offer purposes: its name + the zone it sits in. Two
        // structures of the same name in different zones count as different structures.
        private static int HashFor(string name, Vector2s zone)
        {
            return StringExtensionMethods.GetStableHashCode(name + "|" + zone.x + "," + zone.y);
        }

        // Deterministic offer roll: the low bits of the stable hash decide, so the same structure
        // always gives the same result on every pass and on every client.
        private static bool Hits(int h)
        {
            return (h & 0x7FFFFFFF) % 100 < HitPercent;
        }
    }
}
