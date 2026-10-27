// Hirdmandr
// Soul item (R3c redesign, Option B / pickable): a dead rescued NPC's persistent record.
// The soul is a real Valheim item (a refined-eitr-style green sphere) so Thomas can pick a
// fallen NPC up, carry it home, and store it in a chest while he gathers resurrection
// materials. The item is stack-size 1 (a soul is always a stack of 1 - two souls never
// merge) and is identified by the "hmnpc_soul" key in its custom data.
//
// Persistence: the NPC's full state is written into ItemDrop.ItemData.m_customData
// (a string->string map). That map is the STANDARD item custom-data field that round-trips
// through world-drop -> inventory -> container, so it survives pickup AND world reload
// regardless of where the soul currently sits (floor, pocket, or chest). The MonoBehaviour
// skin (tooltip / no-stack / resurrect interaction) is a thin runtime layer re-created from
// the prefab each load; only the m_customData payload is durable.

using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Hirdmandr
{
    internal static class SoulItem
    {
        /// <summary>Prefab + item name of the soul (unique, so it never stacks with refined eitr).</summary>
        public const string SoulPrefabName = "hirdmandr_soul";

        /// <summary>Vanilla source cloned for the green-sphere visual + ItemDrop behaviour.</summary>
        private const string SourcePrefabName = "Eitr";

        /// <summary>Custom-data key that marks an item as a soul (and carries the payload).</summary>
        public const string SoulTagKey = "hmnpc_soul";

        private static bool s_registered;

        /// <summary>
        /// Registers the soul item once, called from Hirdmandr.Awake on
        /// PrefabManager.OnVanillaPrefabsAvailable (same timing as the existing pieces).
        /// Clones the refined-eitr prefab and adds it to the game's item table via Jotunn.
        /// </summary>
        public static void RegisterSoulItem()
        {
            try
            {
                if (s_registered)
                {
                    return;
                }
                s_registered = true;

                var prefabManager = PrefabManager.Instance;
                if (prefabManager == null || !prefabManager.GetPrefab(SoulPrefabName))
                {
                    Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: '" + SoulPrefabName + "' not present; registering.");
                }
                else
                {
                    Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: '" + SoulPrefabName + "' already present; skipping registration.");
                    return;
                }

                GameObject source = prefabManager.GetPrefab(SourcePrefabName);
                if (source == null)
                {
                    Jotunn.Logger.LogError("[Hirdmandr] SoulItem: source prefab '" + SourcePrefabName + "' is not registered yet - cannot register the soul item. (Wrong source name or items not ready.)");
                    s_registered = false; // allow a later retry
                    return;
                }

                GameObject soul = prefabManager.CreateClonedPrefab(SoulPrefabName, SourcePrefabName);
                if (soul == null)
                {
                    Jotunn.Logger.LogError("[Hirdmandr] SoulItem: CreateClonedPrefab('" + SoulPrefabName + "', '" + SourcePrefabName + "') returned null.");
                    s_registered = false;
                    return;
                }

                // The Eitr's damaging green bolts are fired by a "Radiator" component on the
                // child node "attach/Radiator" (it spawns m_projectile bolts at a rate from
                // m_emitFrom). Remove every Radiator in the clone so the soul has no side
                // effects, while keeping the visible sphere (core mesh), the light, the
                // sparks, and pickup (Transform/ZNetView/ZSyncTransform/ItemDrop) intact.
                RemoveBoltEmitters(soul);

                // Mirror the source's icon + weight so the soul looks/feels like a refined eitr.
                Sprite icon = null;
                float weight = 0.1f;
                var sourceDrop = source.GetComponent<ItemDrop>();
                if (sourceDrop != null && sourceDrop.m_itemData != null && sourceDrop.m_itemData.m_shared != null)
                {
                    var srcIcons = sourceDrop.m_itemData.m_shared.m_icons;
                    if (srcIcons != null && srcIcons.Length > 0)
                    {
                        icon = srcIcons[0];
                    }
                    if (sourceDrop.m_itemData.m_shared.m_weight > 0f)
                    {
                        weight = sourceDrop.m_itemData.m_shared.m_weight;
                    }
                }

                var itemConfig = new Jotunn.Configs.ItemConfig
                {
                    Name = "Soul",
                    Description = "A hirdmandr soul. Stack: 1. Resurrect at your hearth.",
                    Weight = weight,
                    Icon = icon
                };
                itemConfig.StackSize = 1;

                var customItem = new CustomItem(soul, fixReference: true, itemConfig);
                bool ok = ItemManager.Instance.AddItem(customItem);
                Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: registered '" + SoulPrefabName + "' (AddItem=" + ok + ", stackSize=1, icon=" + (icon != null ? "set" : "null") + ").");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("[Hirdmandr] SoulItem.RegisterSoulItem threw:\n" + e);
                s_registered = false;
            }
        }

        /// <summary>
        /// Test aid (Increment A): spawn a soul drop a couple of metres in front of the local
        /// player, carrying a minimal custom-data payload (the soul tag + a marker string).
        /// Lets Thomas verify the whole foundation - the sphere appears, is pickable, is not
        /// stackable, and the custom data survives a reload - before the real death-transition
        /// payload is wired up.
        /// </summary>
        public static void SpawnTestSoul(Player player)
        {
            if (player == null || player.transform == null)
            {
                Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: SpawnTestSoul: no local player.");
                return;
            }

            var prefabManager = PrefabManager.Instance;
            GameObject prefab = (prefabManager != null) ? prefabManager.GetPrefab(SoulPrefabName) : null;
            if (prefab == null)
            {
                Jotunn.Logger.LogError("[Hirdmandr] SoulItem: SpawnTestSoul: '" + SoulPrefabName + "' prefab is not registered yet.");
                return;
            }

            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
            {
                Jotunn.Logger.LogError("[Hirdmandr] SoulItem: SpawnTestSoul: the soul prefab has no ItemData/SharedData (registration incomplete).");
                return;
            }

            // Clone so we never mutate the prefab's template, then stamp in our payload.
            ItemDrop.ItemData data = drop.m_itemData.Clone();
            data.m_stack = 1;
            data.m_customData = new Dictionary<string, string>
            {
                { SoulTagKey, "1" },
                { "hmnpc_test", "IncrementA" }
            };

            Vector3 pos = player.transform.position + player.transform.forward * 2f + Vector3.up * 0.5f;
            ItemDrop spawned = ItemDrop.DropItem(data, 1, pos, Quaternion.identity);
            Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: SpawnTestSoul: dropped soul (shared='" + data.m_shared.m_name + "') in front of the player. Pick it up, store it, reload, confirm it persists.");
                if (spawned == null)
                {
                    Jotunn.Logger.LogError("[Hirdmandr] SoulItem: SpawnTestSoul: ItemDrop.DropItem returned null.");
                }
        }

        // --- Increment B: real soul from a welcomed NPC's death ---

        /// <summary>
        /// Drops a soul at the death location of a welcomed NPC, carrying the NPC's full durable
        /// identity in m_customData so it can later be resurrected as the SAME person (look,
        /// role, jobs, fighting style, skills, owner, bed). The gear inventory is dropped
        /// separately as items (HirdmandrNPC.DropContainerItems); the soul is only the identity
        /// record. All values are read from the NPC's ZDO (the authoritative saved identity).
        /// </summary>
        public static void SpawnSoulFromNpc(HirdmandrNPC npc, Vector3 pos)
        {
            try
            {
                var prefabManager = PrefabManager.Instance;
                GameObject prefab = (prefabManager != null) ? prefabManager.GetPrefab(SoulPrefabName) : null;
                if (prefab == null)
                {
                    Jotunn.Logger.LogError("[Hirdmandr] SoulItem.SpawnSoulFromNpc: '" + SoulPrefabName + "' prefab is not registered yet.");
                    return;
                }
                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                {
                    Jotunn.Logger.LogError("[Hirdmandr] SoulItem.SpawnSoulFromNpc: the soul prefab has no ItemData/SharedData (registration incomplete).");
                    return;
                }

                ZDO zdo = (npc.m_znet != null) ? npc.m_znet.GetZDO() : null;
                var payload = new Dictionary<string, string> { { SoulTagKey, "1" } };
                if (zdo != null)
                {
                    // Appearance (name, sex, colors, hair, beard).
                    payload["hmnpc_name"] = zdo.GetString("hmnpc_name");
                    Jotunn.Logger.LogInfo("[soultrace] SpawnSoulFromNpc: zdo.name='" + payload["hmnpc_name"] + "'");
                    payload["hmnpc_female"] = B(zdo.GetBool("hmnpc_female"));
                    payload["hmnpc_haircolor"] = V3(zdo.GetVec3("hmnpc_haircolor", Vector3.one));
                    payload["hmnpc_skincolor"] = V3(zdo.GetVec3("hmnpc_skincolor", Vector3.one));
                    payload["hmnpc_beard"] = zdo.GetString("hmnpc_beard");
                    payload["hmnpc_hair"] = zdo.GetString("hmnpc_hair");
                    // Origin structure hash (which PoI this hirdmandr was rescued from). Carried so
                    // a future resurrection knows which structure's claim this person belongs to.
                    payload[RescueLedger.HashKey] = zdo.GetInt(RescueLedger.HashKey, 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    // Ownership + role/jobs/fighting style. (The bed is intentionally NOT
                    // carried - see the exclusion note at the end of this block.)
                    payload["hmnpc_jarlZOID"] = zdo.GetLong("hmnpc_jarlZOID", 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    payload["hmnpc_jarlName"] = zdo.GetString("hmnpc_jarlName");
                    payload["hmnpc_roleArtisan"] = B(zdo.GetBool("hmnpc_roleArtisan"));
                    payload["hmnpc_roleWarrior"] = B(zdo.GetBool("hmnpc_roleWarrior"));
                    payload["hmnpc_jobThegn"] = B(zdo.GetBool("hmnpc_jobThegn"));
                    payload["hmnpc_thegnDayshift"] = B(zdo.GetBool("hmnpc_thegnDayshift"));
                    payload["hmnpc_jobHimthiki"] = B(zdo.GetBool("hmnpc_jobHimthiki"));
                    payload["hmnpc_fightingStyleDefense"] = B(zdo.GetBool("hmnpc_fightingStyleDefense"));
                    payload["hmnpc_fightingStyleOffense"] = B(zdo.GetBool("hmnpc_fightingStyleOffense"));
                    payload["hmnpc_jobGatherer"] = B(zdo.GetBool("hmnpc_jobGatherer"));
                    payload["hmnpc_fightingRangeClose"] = B(zdo.GetBool("hmnpc_fightingRangeClose"));
                    payload["hmnpc_fightingRangeMid"] = B(zdo.GetBool("hmnpc_fightingRangeMid"));
                    payload["hmnpc_fightingRangeFar"] = B(zdo.GetBool("hmnpc_fightingRangeFar"));
                    // Bed intentionally excluded from the soul. On death the NPC's ZDO is
                    // destroyed, so HirdmandrBed's stale-claim sweep (owner ZDO gone) releases
                    // the bed for another NPC to claim, and HirdmandrChest's dead-owner sweep
                    // frees any artisan job claims. A resurrected NPC re-acquires its bed/jobs
                    // via normal state-machine operation. (Carrying the ZDOID here would also
                    // be meaningless: it points at a piece that may be destroyed/moved.)
                }
                // Skills (value + enable for each) from the live skill state.
                if (npc.m_skills != null)
                {
                    foreach (HMSkills.SkillData skill in npc.m_skills.m_hmSkills)
                    {
                        if (skill == null)
                        {
                            continue;
                        }
                        payload["hmnpc_skill" + skill.m_name] = skill.m_value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        payload["hmnpc_skill" + skill.m_name + "enable"] = B(skill.m_isEnabled);
                    }
                }

                ItemDrop.ItemData data = drop.m_itemData.Clone();
                data.m_stack = 1;
                data.m_customData = payload;
                ItemDrop.DropItem(data, 1, pos, Quaternion.identity);
                Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem.SpawnSoulFromNpc: dropped soul (" + payload.Count + " payload keys) at " + pos.ToString("F1") + ".");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("[Hirdmandr] SoulItem.SpawnSoulFromNpc threw:\n" + e);
            }
        }

        private static string B(bool b)
        {
            return b ? "1" : "0";
        }

        private static string V3(Vector3 v)
        {
            return v.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ","
                 + v.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ","
                 + v.z.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Destroys every "Radiator" component in the soul clone. The Eitr prefab's Radiator
        /// (on the "attach/Radiator" child) is what spawns the damaging green-bolt projectiles
        /// at nearby pieces/characters; a soul must be inert, so we remove it. The visible
        /// sphere (core mesh), the light, the sparks, and the pickup components are untouched.
        /// </summary>
        private static void RemoveBoltEmitters(GameObject soul)
        {
            if (soul == null)
            {
                return;
            }
            Radiator[] radiators = soul.GetComponentsInChildren<Radiator>(true);
            foreach (Radiator r in radiators)
            {
                if (r == null)
                {
                    continue;
                }
                Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: removing bolt emitter 'Radiator' on node '" + r.gameObject.name + "' from the soul clone (inert soul).");
                UnityEngine.Object.DestroyImmediate(r);
            }
        }

        /// <summary>
        /// Diagnostic: dump the full GameObject / component tree of the Eitr source prefab to
        /// the BepInEx log, so we can see exactly which node fires the damaging green bolts
        /// and which node renders the visible sphere. Triggered by "/hirdmandr soulinspect".
        /// </summary>
        public static void InspectSoulPrefab()
        {
            var prefabManager = PrefabManager.Instance;
            GameObject source = (prefabManager != null) ? prefabManager.GetPrefab(SourcePrefabName) : null;
            if (source == null)
            {
                Jotunn.Logger.LogError("[Hirdmandr] SoulItem: InspectSoulPrefab: source prefab '" + SourcePrefabName + "' is not registered yet.");
                return;
            }
            Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: ===== Eitr prefab tree (" + SourcePrefabName + ") - start =====");
            LogPrefabTree(source, source.name, 0);
            Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: ===== Eitr prefab tree - end =====");
        }

        private static void LogPrefabTree(GameObject go, string path, int depth)
        {
            if (go == null)
            {
                return;
            }
            string pad = new string(' ', depth * 2);
            var sb = new System.Text.StringBuilder();
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null)
                {
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(c.GetType().Name);
            }
            Jotunn.Logger.LogInfo("[Hirdmandr] SoulItem: tree: " + pad + path + "  [" + sb + "]");

            foreach (Transform t in go.transform)
            {
                LogPrefabTree(t.gameObject, path + "/" + t.name, depth + 1);
            }
        }
    }
}
