using System.Collections.Generic;

namespace Hirdmandr
{
    // R3c rescue refactor: the persistent "ledger" for world rescues. It lives in the world's
    // global key space (ZoneSystem global keys) - the same persistent, cross-client-synced store
    // that holds boss-defeat flags (defeated_eikthyr, killed_surtling, ...), written by
    // Character.m_defeatSetGlobalKey and synced via ZoneSystem.RPC_GlobalKeys. No dedicated ZDO,
    // no ownership, no bare prefab: everything rides on the world save and reloads with it.
    //
    // What actually PERSISTS: one boolean global key per structure - hmnpc_rescue_welcomed_<H> -
    // set the moment a rescue at that structure is welcomed home (monotonic, never cleared). That
    // is the only durable data.
    //
    // "Live" is NOT persisted - it is derived each scan from the world as it currently is: every
    // rescue NPC carries hmnpc_rescuepoiHash on its own ZDO, and CollectLive() reads that key off
    // every currently-loaded ZDO. When a rescue dies its ZDO is destroyed, so it drops out of the
    // live set on its own - that is the entire safety net (no sweep pass needed).
    internal static class RescueLedger
    {
        // The (structure name + zone) hash a rescue was placed at. Stamped on the rescue's own
        // ZDO at spawn (see RescueSpawner.PlaceAtLocation); it is the only link between a loaded
        // NPC and the structure it claims. 0 means "not tied to a structure" (manual spawns).
        public const string HashKey = "hmnpc_rescuepoiHash";

        private static string WelcomedKey(int h)
        {
            return "hmnpc_rescue_welcomed_" + h.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // Has a rescue at structure H been welcomed home? (Set once, never cleared.)
        public static bool IsWelcomed(int h)
        {
            if (ZoneSystem.instance == null)
            {
                return false;
            }
            return ZoneSystem.instance.GetGlobalKey(WelcomedKey(h));
        }

        // Mark structure H as welcomed home. Called from HirdmandrNPC.WelcomeHome. Idempotent.
        public static void SetWelcomed(int h)
        {
            if (ZoneSystem.instance == null)
            {
                return;
            }
            ZoneSystem.instance.SetGlobalKey(WelcomedKey(h));
        }

        // Fill `live` with the hmnpc_rescuepoiHash of every currently-loaded ZDO that carries a
        // non-zero hash (i.e. every rescue NPC that exists right now). Rebuilt fresh each scan
        // pass, so it always reflects the live world - a dead rescue is simply gone from it.
        public static void CollectLive(HashSet<int> live)
        {
            live.Clear();
            if (ZDOMan.instance == null || ZDOMan.instance.m_objectsByID == null)
            {
                return;
            }
            foreach (var pair in ZDOMan.instance.m_objectsByID)
            {
                ZDO zdo = pair.Value;
                if (zdo == null)
                {
                    continue;
                }
                int h = zdo.GetInt(HashKey, 0);
                if (h != 0)
                {
                    live.Add(h);
                }
            }
        }

        // Is structure H already occupied - either by a live rescue there right now, or by a
        // welcome that already happened? `live` must be pre-filled for this pass via CollectLive.
        public static bool Claimed(int h, HashSet<int> live)
        {
            if (live != null && live.Contains(h))
            {
                return true;
            }
            return IsWelcomed(h);
        }
    }
}
