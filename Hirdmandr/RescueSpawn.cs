using System;
using UnityEngine;

namespace Hirdmandr
{
    // Shared code path that places an unrescued world rescue inside a (placed) PoI location.
    // Used by the periodic world-rescue scanner (RescueScanner) and the test command
    // (/hirdmandr rescue spawn). Creating an owner-owned 'npc_entity' ZDO is all that is needed:
    // HirdmandrNPC.OwnerInit sees no saved name and runs its first-time branch, which puts the
    // NPC in the unrescued state (monster AI off, sitting, position-based biome gear, and - via
    // the R3a patches - invulnerable + not a mob target).
    internal static class RescueSpawner
    {
        // Spawn one unrescued rescue at a clear point inside the given (placed) location.
        // `rescuepoiHash` (0 = not tied to a structure) is stamped on the new ZDO (RescueLedger.
        // HashKey) so the scanner can later tell which structure this rescue claims. Returns the
        // spawned HirdmandrNPC, or null on any failure. Never throws.
        public static HirdmandrNPC PlaceAtLocation(ZoneSystem.LocationInstance inst, string reason, int rescuepoiHash = 0)
        {
            try
            {
                if (inst.m_location == null || !inst.m_placed)
                {
                    return null;
                }

                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn skipped (" + reason + "): no local player to own the ZDO.");
                    return null;
                }

                ZNetScene scene = ZNetScene.instance;
                if (scene == null)
                {
                    return null;
                }

                GameObject prefab = scene.GetPrefab("npc_entity");
                if (prefab == null)
                {
                    Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn skipped (" + reason + "): 'npc_entity' prefab not registered yet.");
                    return null;
                }

                Vector3 pos = PickClearSpot(inst);
                Quaternion rot = player.transform.rotation;
                int prefabHash = scene.GetPrefabHash(prefab);

                ZDO zdo = ZDOMan.instance.CreateNewZDO(pos, prefabHash);
                zdo.SetPrefab(prefabHash);
                zdo.SetRotation(rot);

                // Ownership: the local session owns it, so OwnerInit runs the owner (first-time)
                // branch. Copy the local player's owner, exactly like /hirdmandr spawn. Without
                // an owner the NPC would not initialize (OwnerInit owner-only path).
                var playerView = player.m_nview;
                var playerZdo = (playerView != null) ? playerView.GetZDO() : null;
                if (playerZdo == null)
                {
                    Jotunn.Logger.LogInfo("[Hirdmandr] rescue spawn skipped (" + reason + "): could not resolve local session owner.");
                    return null;
                }
                zdo.SetOwner(playerZdo.GetOwner());

                // Which structure this rescue belongs to (0 = none, e.g. a manual spawn). Persists
                // with the ZDO and is how RescueLedger.CollectLive derives the live structures.
                if (rescuepoiHash != 0)
                {
                    zdo.Set(RescueLedger.HashKey, rescuepoiHash);
                }

                GameObject instance = scene.CreateObject(zdo);
                if (instance == null)
                {
                    Jotunn.Logger.LogError("[Hirdmandr] rescue spawn failed (" + reason + "): CreateObject returned null.");
                    return null;
                }

                scene.AddInstance(zdo, instance.GetComponent<ZNetView>());

                var npc = instance.GetComponent<HirdmandrNPC>();
                Jotunn.Logger.LogInfo(string.Format("[Hirdmandr] rescue placed ({0}) at ({1:F1},{2:F1},{3:F1}) in '{4}' (ZDO {5})",
                    reason, pos.x, pos.y, pos.z, inst.m_location.m_name, zdo.m_uid));
                return npc;
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("[Hirdmandr] RescueSpawner.PlaceAtLocation threw (" + reason + "):\n" + e);
                return null;
            }
        }

        // A clear, ground-level point inside the structure's footprint: the location center + a
        // small random offset (so we are not dead-center), grounded by a downward raycast. Only
        // trust the raycast hit if it is close (a floor near the origin); otherwise keep the
        // center height. Heuristic - tuned in-game (see R3c).
        private static Vector3 PickClearSpot(ZoneSystem.LocationInstance inst)
        {
            Vector3 center = inst.m_position;
            float interior = inst.m_location.m_interiorRadius;
            Vector2 h = UnityEngine.Random.insideUnitCircle * Mathf.Max(1f, interior * 0.5f);
            Vector3 p = center + new Vector3(h.x, 0f, h.y);

            RaycastHit hit;
            Vector3 origin = p + Vector3.up * 6f;
            if (Physics.Raycast(origin, Vector3.down, out hit, 12f, Physics.DefaultRaycastLayers) && hit.distance <= 8f)
            {
                p = hit.point + Vector3.up * 0.5f;
            }
            return p;
        }
    }
}
