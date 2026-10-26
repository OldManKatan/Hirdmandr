namespace Hirdmandr
{
    // The rescue whitelist (R3b): the PoI location names a world-spawned rescue may appear
    // inside. Hardcoded from D:\OpenCode\hirdmandr_project\valid_hirdmandr_spawn_prefabs.md
    // (Thomas, 2026-10-06) - 67 structures across 7 biomes (no Frozen North). Matched against
    // ZoneSystem.ZoneLocation.m_name / m_prefabName. To change the pool, edit the array below
    // and rebuild.
    internal static class RescueWhitelist
    {
        private static readonly string[] s_names = new string[]
        {
            // Meadows (12)
            "WoodHouse1", "WoodHouse2", "WoodHouse3", "WoodHouse5", "WoodHouse7", "WoodHouse9",
            "WoodHouse11", "WoodHouse13", "WoodFarm1", "WoodVillage1", "CombatRuin01", "WoodVillage2",
            // Black Forest (9)
            "Ruin1", "Ruin2", "StoneHouse3", "StoneHouse4",
            "StoneTowerRuins03", "StoneTowerRuins07", "StoneTowerRuins08", "StoneTowerRuins09", "StoneTowerRuins10",
            // Swamp (11)
            "SwampRuin1", "SwampRuin2", "SwampHut1", "SwampHut2", "SwampHut3", "SwampHut4", "SwampHut5",
            "SwampWell1", "SwampHut1_1", "SwampHut2_1", "SwampHut3_1",
            // Mountains (8)
            "StoneTowerRuins04", "StoneTowerRuins05", "AbandonedLogCabin02", "AbandonedLogCabin03",
            "AbandonedLogCabin04", "MountainWell1", "Runestone_Mountains", "StoneTowerRuins05_leet",
            // Plains (8)
            "Ruin3", "GoblinCamp2", "StoneTower1", "StoneTower3",
            "GoblinCamp2_1", "GoblinHut01", "GoblinHut02", "GoblinHut03",
            // Mistlands (8)
            "Mistlands_Giant1", "Mistlands_Giant2", "Mistlands_StatueGroup1", "Mistlands_Statue1",
            "Mistlands_Statue2", "Mistlands_Swords1", "Mistlands_Swords2", "Mistlands_Swords3",
            // Ashlands (11)
            "PlaceofMystery1", "PlaceofMystery2", "PlaceofMystery3", "CharredFortress", "CharredRuins1",
            "CharredTowerRuins1", "CharredTowerRuins2", "CharredTowerRuins3",
            "CharredRuins2", "CharredRuins3", "CharredRuins4"
        };

        public static int Count => s_names.Length;

        public static bool Contains(ZoneSystem.ZoneLocation loc)
        {
            if (loc == null)
            {
                return false;
            }
            return IndexOf(loc.m_name) >= 0 || IndexOf(loc.m_prefabName) >= 0;
        }

        private static int IndexOf(string name)
        {
            if (name == null)
            {
                return -1;
            }
            for (int i = 0; i < s_names.Length; i++)
            {
                if (s_names[i] == name)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
