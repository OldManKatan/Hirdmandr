using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hirdmandr
{
    // R3c rescue refactor: the world-rescue scanner. Replaces the old ZoneSystem.PlaceLocations
    // Harmony hook (RescueSpawnHook) with a periodic, deterministic pass. Every Interval seconds -
    // on a coroutine hosted by the persistent plugin MonoBehaviour - and on the authoritative
    // client only, it scans the (2*GridRadius+1)^2 zones around the player and offers an
    // unrescued rescue at HitPercent of whitelisted, unclaimed structures.
    //
    // Deterministic: a structure's offer is a pure function of (structure name, zone) via a stable
    // hash, so the same structure offers (or doesn't) on every pass and on every client - no
    // re-rolling, no per-client divergence. Once a rescue at a structure is welcomed home, the
    // structure is marked (RescueLedger.SetWelcomed) and never re-offers.
    internal static class RescueScanner
    {
        public const float Interval = 5f;     // seconds between passes
        public const int GridRadius = 1;      // 3x3 zones around the player
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

            Player player = Player.m_localPlayer;
            Vector2s pzone = ZoneSystem.GetZone(player.transform.position);

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
                    // hmnpc_rescuepoiHash = h on the new ZDO; the live set is rebuilt next
                    // pass, so this structure is "live" from the following interval onward
                    // (checked at most once per pass, so no double-spawn this pass).
                    RescueSpawner.PlaceAtLocation(inst, "rescue scan", h);
                }
            }
        }

        // Only the client running the instance (the server/host) scans and spawns. In single
        // player the local player is always the server, so this is effectively always true today;
        // it keeps a future multiplayer session from double-spawning (otherwise every client would
        // see an unclaimed structure and each spawn its own rescue).
        private static bool Ready()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
				Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn Ready() returned false, reason: if (ZNet.instance == null || !ZNet.instance.IsServer())");
                Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn Ready() ZNet.instance == " + (ZNet.instance == null ? "null" : "true"));
                Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn Ready() !ZNet.instance.IsServer() == " + (ZNet.instance == null ? "n/a" : (!ZNet.instance.IsServer()).ToString()));
                return false;
            }
            if (ZNetScene.instance == null)
            {
				Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn Ready() returned false, reason: if (ZNetScene.instance == null)");
                return false;
            }
            if (Player.m_localPlayer == null || Player.m_localPlayer.transform == null)
            {
				Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn Ready() returned false, reason: if (Player.m_localPlayer == null || Player.m_localPlayer.transform == null)");
                return false;
            }
            if (ZoneSystem.instance == null || ZoneSystem.instance.m_locationInstances == null)
            {
				Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn Ready() returned false, reason: if (ZoneSystem.instance == null || ZoneSystem.instance.m_locationInstances == null)");
                return false;
            }
            if (ZDOMan.instance == null)
            {
				Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn Ready() returned false, reason: if (ZDOMan.instance == null)");
                return false;
            }
			Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn Ready() returned true");
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
