using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hirdmandr
{
    public static class Utils
    {
        // How far around a Lamplighter's chest / NPC it looks for fuel fixtures to refuel.
        // "Wide radius" per the Lamplighter spec; covers a whole homestead.
        public const float LamplighterRange = 40f;

        // How far around a Mender it looks for damaged player-built pieces to repair. Wider
        // than the Lamplighter: a Mender tends the whole settlement, not one chest.
        public const float MenderRange = 50f;

        // Horizontal (XZ) distance at which the Mender is "close enough" to repair a piece.
        public const float MenderRepairReach = 4f;

        // How long the Mender tries to walk to a piece's anchor before giving up and mending from
        // wherever it ended up. Bounds the walk so an unreachable anchor (a ship's bow) can't make
        // it waddle here forever; a normal reachable stand point (3-6f) settles well before this.
        public const float MenderRepairMoveTimeout = 15f;

        // How far around a Farmer's chest it looks for mature crops to harvest + replant.
        // Matches the chest's existing farmer worksite radius so the field reads as one area.
        public const float FarmerRange = 25f;

        // Horizontal (XZ) distance at which the Farmer is "close enough" to harvest (Interact) a
        // crop. Same idea as MenderRepairReach: the walk aims a bit closer than this.
        public const float FarmerInteractReach = 4f;

        // How long the Farmer tries to walk to a crop before giving up (bounds the walk so an
        // unreachable crop can't make it waddle here forever; a normal stand point settles sooner).
        public const float FarmerMoveTimeout = 15f;

        // Gatherer safe zone: the radius around a player-placed storage piece (or a Hirdmandr
        // chest) within which the Gatherer never picks - "gathering away from home".
        public const float GatherSafeRadius = 40f;

        // Gather (S5): how far from the follow center (the player while following) the Gatherer
        // will look for a pickable to detour to. Same radius as the follow leash: a straight walk
        // to a <=20m pickable provably stays within the 20m follow leash (distance-to-a-point
        // along a segment is convex; the max is at an endpoint).
        public const float GatherTargetRadius = 20f;

        // Gather (S5): how long to skip a pickable whose walk settled out of reach (blocked), so
        // the next scan doesn't immediately re-select it (tug loop).
        public const float GatherTargetCooldown = 15f;

        // Gather (reselect pause): how long to wait after a pick / abandon before the Gatherer may
        // select the next target. A short beat so it doesn't rush to the next (possibly
        // opposite-side) pickable and shuttle back and forth. Longer than the ~1s ground sweep so
        // the sweep completes within the pause.
        public const float GatherReselectPause = 1.5f;

        // Gather (S6): ground-sweep radius around the pick point (where the yield lands). The
        // vanilla Player auto-pickup range is 2m; 10m clears a whole bush's scattered yield
        // drops in a single sweep.
        public const float GatherSweepRadius = 10f;

        public static string TrimCloneTag(this string s)
        {
            if (!s.EndsWith("(Clone)"))
            {
                return s;
            }
            return s.Substring(0, s.Length - "(Clone)".Length);
        }

        public static EffectList CloneEffectList(EffectList elPP)
        {
            EffectList effectList = new EffectList();
            effectList.m_effectPrefabs = new EffectList.EffectData[elPP.m_effectPrefabs.Length];
            for (int i = 0; i < elPP.m_effectPrefabs.Length; i++)
            {
                EffectList.EffectData effectData = elPP.m_effectPrefabs[i];
                effectList.m_effectPrefabs[i] = new EffectList.EffectData
                {
                    m_prefab = effectData.m_prefab,
                    m_enabled = effectData.m_enabled,
                    m_attach = effectData.m_attach,
                    m_inheritParentRotation = effectData.m_inheritParentRotation,
                    m_inheritParentScale = effectData.m_inheritParentScale,
                    m_randomRotation = effectData.m_randomRotation,
                    m_scale = effectData.m_scale
                };
            }
            return effectList;
        }

        public static GameObject DesignSfxDeepen(string originalName, string newName)
        {
            bool alreadyExisted = false;
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab(newName, originalName);
            if (alreadyExisted)
            {
                return gameObject;
            }
            ZSFX component = gameObject.GetComponent<ZSFX>();
            component.m_minPitch = 0.8f;
            component.m_maxPitch = 0.9f;
            return gameObject;
        }

        public static List<ZDO> GetAllZDOsInRange(Vector3 point, float range)
        {
            int MinX = (int)(point.x - range);
            int MinZ = (int)(point.z - range);
            int MaxX = (int)(point.x + range);
            int MaxZ = (int)(point.z + range);

            // Get sectors to check
            List<Vector2i> sectors;
            sectors = GetSectors(MinX, MinZ, MaxX, MaxZ);

            // Get zdo's
            var foundZDOs = new List<ZDO>();
            var visited = new HashSet<ZoneSystem.SectorIndex>();

            foreach (var sector in sectors)
            {
                ZDOMan.instance.FindObjects(new Vector2s(sector), foundZDOs, visited);
            }

            return foundZDOs;
        }

        public static List<ZDO> GetPrefabZDOsInRange(Vector3 point, float range, string prefabName)
        {
            int prefabStableHashCode = prefabName.GetStableHashCode();

            var foundZDOs = GetAllZDOsInRange(point, range);

            var matchedZDOs = new List<ZDO>();

            foreach (ZDO aZDO in foundZDOs)
            {
                if (aZDO.GetPrefab() == prefabStableHashCode)
                {
                    matchedZDOs.Add(aZDO);
                }
            }

            return matchedZDOs;
        }

        // The three NPC fire clones CreateNPCFires registers (Hirdmandr.cs) - the ONLY valid
        // socialize gathering points (Thomas: nothing else). Searching all three means a village
        // whose gathering fire is a hearth or bonfire, not just a campfire, still has one.
        public static readonly string[] NpcGatheringFirePrefabs =
        {
            "piece_npc_fire_pit",
            "piece_npc_hearth",
            "piece_npc_bonfire"
        };

        // All NPC gathering fires (any of the three clones) within range of point. Single source
        // for the socialize / patrol / workday gathering searches; the three old copies each
        // searched piece_npc_fire_pit only, so a placed hearth or bonfire was invisible to the
        // NPCs.
        public static List<ZDO> GetNpcGatheringFiresInRange(Vector3 point, float range)
        {
            var fires = new List<ZDO>();
            foreach (string prefab in NpcGatheringFirePrefabs)
            {
                foreach (ZDO fire in GetPrefabZDOsInRange(point, range, prefab))
                {
                    fires.Add(fire);
                }
            }
            return fires;
        }

        // Nearest NPC gathering fire within range of point, or Vector3.zero if none (the
        // "zero = none found" convention the callers already use).
        public static Vector3 FindNearestNpcGatheringFire(Vector3 point, float range)
        {
            ZDO closest = null;
            float bestDist = float.MaxValue;
            foreach (ZDO fire in GetNpcGatheringFiresInRange(point, range))
            {
                float d = Vector3.Distance(point, fire.m_position);
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = fire;
                }
            }
            return closest != null ? closest.m_position : Vector3.zero;
        }

        private static List<Vector2i> GetSectors(int minX, int minZ, int maxX, int maxZ)
        {
            List<Vector2i> sectors = new List<Vector2i>();

            int stepMinX = Zonify(minX);
            int stepMaxX = Zonify(maxX);

            int stepMinZ = Zonify(minZ);
            int stepMaxZ = Zonify(maxZ);

            for (int x = stepMinX; x <= stepMaxX; ++x)
            {
                for (int z = stepMinZ; z <= stepMaxZ; ++z)
                {
                    sectors.Add(new Vector2i(x, z));
                }
            }

            return sectors;

            int Zonify(int coordinate)
            {
                return Mathf.FloorToInt((coordinate + 32) / 64f);
            }
        }
    }

    // A single farmer crop: the mature Pickable's yield item (the internal item name a live
    // Pickable's m_itemPrefab reports; the detection key), how much it drops per harvest, the
    // item the farmer pulls from the chest (x1) to replant, and the sapling_* prefab to
    // instantiate so the SAME mature crop regrows in place.
    public struct FarmerCrop
    {
        public string yieldItem;
        public int yieldAmount;
        public string consumeItem;
        public string sapling;

        public FarmerCrop(string yieldItem, int yieldAmount, string consumeItem, string sapling)
        {
            this.yieldItem = yieldItem;
            this.yieldAmount = yieldAmount;
            this.consumeItem = consumeItem;
            this.sapling = sapling;
        }
    }

    // The farmer's crop table + lookup, keyed by the mature crop's YIELD item (internal name).
    //   - Two-tier crops cross-sustain: food crop replants with the seed item, seed crop with the
    //     food item (net = seed growth).
    //   - One-tier crops "drop themselves": replant with their own yield.
    // Sustainability is the player's job: a crop whose consumeItem isn't in the chest is left
    // alone (no harvest, no replant) -- enforced in Step 4, not here.
    public static class FarmerCrops
    {
        // The cultivator tool the farmer equips to work a field.
        public static readonly string CultivatorItem = "$item_cultivator";

        // Sapling names (ALL confirmed registered via /hirdmandr farmscan section 3): the two-tier
        // food/seed pairs, the one-tier drops, and the mushrooms. Note sapling_Kale has a capital K
        // (the others are lowercase); the mushrooms are sapling_magecap / sapling_jotunpuffs (no
        // "mushroom_" prefix -- that name is NOT a registered prefab).
        private static readonly FarmerCrop[] m_crops = new FarmerCrop[]
        {
            // --- two-tier (food + seed) ---
            new FarmerCrop("$item_carrot", 1, "$item_carrotseeds", "sapling_carrot"),
            new FarmerCrop("$item_carrotseeds", 3, "$item_carrot", "sapling_seedcarrot"),
            new FarmerCrop("$item_turnip", 1, "$item_turnipseeds", "sapling_turnip"),
            new FarmerCrop("$item_turnipseeds", 3, "$item_turnip", "sapling_seedturnip"),
            new FarmerCrop("$item_onion", 1, "$item_onionseeds", "sapling_onion"),
            new FarmerCrop("$item_onionseeds", 3, "$item_onion", "sapling_seedonion"),
            new FarmerCrop("$item_kale", 3, "$item_kaleseeds", "sapling_Kale"),
            new FarmerCrop("$item_kaleseeds", 3, "$item_kale", "sapling_seedkale"),
            // --- one-tier (drop themselves) ---
            new FarmerCrop("$item_oatseeds", 3, "$item_oatseeds", "sapling_oat"),
            new FarmerCrop("$item_poteitr", 3, "$item_poteitr", "sapling_poteitr"),
            new FarmerCrop("$item_barley", 2, "$item_barley", "sapling_barley"),
            new FarmerCrop("$item_flax", 2, "$item_flax", "sapling_flax"),
            new FarmerCrop("$item_magecap", 3, "$item_magecap", "sapling_magecap"),
            new FarmerCrop("$item_jotunpuffs", 3, "$item_jotunpuffs", "sapling_jotunpuffs"),
        };

        private static readonly Dictionary<string, FarmerCrop> m_byYield = new Dictionary<string, FarmerCrop>();

        static FarmerCrops()
        {
            foreach (var c in m_crops)
            {
                m_byYield[c.yieldItem] = c;
            }
        }

        // The crop entry for a mature crop's yield item; false if the yield item isn't a farmer crop.
        public static bool TryGetByYield(string yieldItem, out FarmerCrop crop)
        {
            if (yieldItem != null && m_byYield.TryGetValue(yieldItem, out crop))
            {
                return true;
            }
            crop = default(FarmerCrop);
            return false;
        }

        // True if this item name is the yield of a farmer crop (the Step 3 field-scan filter).
        public static bool IsKnownYield(string itemName)
        {
            return itemName != null && m_byYield.ContainsKey(itemName);
        }
    }
}