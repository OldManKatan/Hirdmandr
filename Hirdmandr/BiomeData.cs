using System.Collections.Generic;
using UnityEngine;

// Per-biome starting equipment + skill values for rescue NPCs (R4).
// Source: PMW #29 (D3, decided 2026-10-06). Item names are ObjectDB prefab names
// (ObjectDB.instance.GetItemPrefab). Rules: "None" = empty slot; duplicate entries =
// weighted toward that item; all equipment spawns base level (quality 1), never upgraded.
//
// Names below are the exact ObjectDB prefab keys (GetItemPrefab). The 9 names that were
// wrong in the first pass were corrected 2026-10-06 from an in-game invdump round-trip
// (each confirmed ROUNDTRIP:OK). Casing is intentionally irregular (matches the game:
// e.g. AxeBlackMetal vs AtgeirBlackmetal, ArmorAshlandsMediumlegs). Deep North gets no
// NPCs in the current design, but its skills are recorded here for future use.
// Re-verify with `/hirdmandr rescue verifygear` (in a world) if names change.
public static class BiomeData
{
    public class Gear
    {
        public readonly string name;
        public readonly Heightmap.Biome biome;
        public readonly List<string> weapon1h;
        public readonly List<string> weapon2h;
        public readonly List<string> weaponRanged;
        public readonly List<string> ammo;
        public readonly List<string> shield;
        public readonly List<string> head;
        public readonly List<string> chest;
        public readonly List<string> legs;
        public readonly float[] skills;

        public Gear(string name, Heightmap.Biome biome, string[] weapon1h, string[] weapon2h,
            string[] weaponRanged, string[] ammo, string[] shield, string[] head,
            string[] chest, string[] legs, float[] skills)
        {
            this.name = name;
            this.biome = biome;
            this.weapon1h = new List<string>(weapon1h);
            this.weapon2h = new List<string>(weapon2h);
            this.weaponRanged = new List<string>(weaponRanged);
            this.ammo = new List<string>(ammo);
            this.shield = new List<string>(shield);
            this.head = new List<string>(head);
            this.chest = new List<string>(chest);
            this.legs = new List<string>(legs);
            this.skills = (float[])skills.Clone();
        }
    }

    private static readonly List<Gear> s_all = new List<Gear>();

    static BiomeData()
    {
        s_all.Add(new Gear("Meadows", Heightmap.Biome.Meadows,
            new[] { "AxeStone", "SpearFlint", "Club", "KnifeFlint" },
            new string[0],
            new[] { "Bow" },
            new[] { "ArrowWood" },
            new[] { "ShieldWood" },
            new[] { "None", "None", "HelmetLeather" },
            new[] { "ArmorRagsChest", "ArmorRagsChest", "ArmorLeatherChest" },
            new[] { "ArmorRagsLegs", "ArmorRagsLegs", "ArmorLeatherLegs" },
            new[] { 10f, 5f, 5f }));

        s_all.Add(new Gear("Black Forest", Heightmap.Biome.BlackForest,
            new[] { "AxeBronze", "SwordBronze", "MaceBronze", "KnifeCopper" },
            new[] { "AtgeirBronze" },
            new[] { "BowFineWood" },
            new[] { "ArrowFire" },
            new[] { "ShieldWood", "ShieldBronzeBuckler" },
            new[] { "HelmetBronze", "HelmetTrollLeather" },
            new[] { "ArmorBronzeChest", "ArmorTrollLeatherChest" },
            new[] { "ArmorBronzeLegs", "ArmorTrollLeatherLegs" },
            new[] { 20f, 10f, 5f }));

        s_all.Add(new Gear("Swamp", Heightmap.Biome.Swamp,
            new[] { "AxeIron", "SwordIron", "MaceIron", "SpearElderbark" },
            new[] { "AtgeirIron", "SledgeIron", "Battleaxe" },
            new[] { "BowHuntsman" },
            new[] { "ArrowFire", "ArrowIron" },
            new[] { "ShieldBanded", "ShieldIronBuckler", "ShieldIronTower" },
            new[] { "HelmetIron", "HelmetIron", "HelmetIron", "HelmetRoot" },
            new[] { "ArmorIronChest", "ArmorIronChest", "ArmorIronChest", "ArmorRootChest" },
            new[] { "ArmorIronLegs", "ArmorIronLegs", "ArmorIronLegs", "ArmorRootLegs" },
            new[] { 30f, 15f, 15f }));

        s_all.Add(new Gear("Mountain", Heightmap.Biome.Mountain,
            new[] { "KnifeSilver", "SwordSilver", "SpearWolfFang" },
            new[] { "BattleaxeCrystal" },
            new[] { "BowDraugrFang" },
            new[] { "ArrowObsidian", "ArrowFrost" },
            new[] { "ShieldSilver", "ShieldIronBuckler" },
            new[] { "HelmetDrake", "HelmetDrake", "HelmetDrake", "HelmetFenring" },
            new[] { "ArmorWolfChest", "ArmorWolfChest", "ArmorWolfChest", "ArmorFenringChest" },
            new[] { "ArmorWolfLegs", "ArmorWolfLegs", "ArmorWolfLegs", "ArmorFenringLegs" },
            new[] { 40f, 20f, 20f }));

        s_all.Add(new Gear("Plains", Heightmap.Biome.Plains,
            new[] { "AxeBlackMetal", "SwordBlackmetal", "KnifeBlackMetal" },
            new[] { "AtgeirBlackmetal", "BattleaxeBlackmetal" },
            new string[0],
            new[] { "ArrowNeedle" },
            new[] { "ShieldBlackmetal", "ShieldBlackmetal", "ShieldBlackmetalTower" },
            new[] { "HelmetPadded" },
            new[] { "ArmorPaddedCuirass" },
            new[] { "ArmorPaddedGreaves" },
            new[] { 50f, 25f, 25f, 10f, 10f }));

        s_all.Add(new Gear("Mistlands", Heightmap.Biome.Mistlands,
            new[] { "SpearCarapace", "AxeJotunBane" },
            new[] { "AtgeirHimminAfl", "SledgeDemolisher", "BattleaxeSkullSplittur", "StaffIceShards", "StaffShield" },
            new[] { "CrossbowArbalest" },
            new[] { "BoltBone", "BoltCarapace" },
            new[] { "ShieldCarapaceBuckler", "ShieldCarapace" },
            new[] { "HelmetCarapace", "HelmetCarapace", "HelmetMage" },
            new[] { "ArmorCarapaceChest", "ArmorCarapaceChest", "ArmorMageChest" },
            new[] { "ArmorCarapaceLegs", "ArmorCarapaceLegs", "ArmorMageLegs" },
            new[] { 60f, 30f, 30f, 10f, 10f }));

        s_all.Add(new Gear("Ashlands", Heightmap.Biome.AshLands,
            new[] { "SwordNiedhogg", "SpearSplitner", "MaceEldner" },
            new[] { "AxeBerzerkr", "THSwordSlayer", "StaffLightning" },
            new[] { "BowAshlands" },
            new[] { "ArrowCharred" },
            new[] { "ShieldFlametal", "ShieldFlametal", "ShieldFlametalTower" },
            new[] { "HelmetFlametal", "HelmetFlametal", "HelmetAshlandsMediumHood", "HelmetMage_Ashlands" },
            new[] { "ArmorFlametalChest", "ArmorFlametalChest", "ArmorAshlandsMediumChest", "ArmorMageChest_Ashlands" },
            new[] { "ArmorFlametalLegs", "ArmorFlametalLegs", "ArmorAshlandsMediumlegs", "ArmorMageLegs_Ashlands" },
            new[] { 70f, 35f, 35f, 20f, 20f }));

        // No NPCs spawn in the Deep North (current design); skills recorded for the future.
        s_all.Add(new Gear("Deep North", Heightmap.Biome.DeepNorth,
            new string[0],
            new string[0],
            new string[0],
            new string[0],
            new string[0],
            new string[0],
            new string[0],
            new string[0],
            new[] { 80f, 40f, 40f, 20f, 20f }));
    }

    public static IEnumerable<Gear> All
    {
        get { return s_all; }
    }

    public static Gear ByBiome(Heightmap.Biome biome)
    {
        foreach (Gear g in s_all)
        {
            if (g.biome == biome)
            {
                return g;
            }
        }
        return null;
    }

    // ByBiome with a safe fallback: a spawn position over a biome that has no table (e.g.
    // Ocean) still yields a valid kit, falling back to Meadows.
    public static Gear Resolve(Heightmap.Biome biome)
    {
        Gear g = ByBiome(biome);
        if (g == null)
        {
            g = ByBiome(Heightmap.Biome.Meadows);
        }
        return g;
    }

    // ---- PMW #52 rescue gate: a generated (unrescued) NPC can only be rescued once the
    // previous biome's boss is defeated AND its trophy sacrificed to Oden at the altar. Those
    // states are exactly the boss-defeat global keys Valheim sets (Character.m_defeatSetGlobalKey):
    // defeated_eikthyr / defeated_gdking / defeated_bonemass / defeated_dragon /
    // defeated_goblinking / defeated_queen / defeated_fader. Gated at HirdmandrNPC.Interact:
    // while the key is unset the RescueGUI is suppressed and the NPC uses Say() to tell the player
    // what must be done. A "" globalKey == no gate (Meadows).
    public class Gate
    {
        public readonly Heightmap.Biome biome;
        public readonly string globalKey; // ZoneSystem global key; "" = no gate
        public readonly string bossName;  // display name substituted into the Say() lines

        public Gate(Heightmap.Biome biome, string globalKey, string bossName)
        {
            this.biome = biome;
            this.globalKey = globalKey;
            this.bossName = bossName;
        }
    }

    // {0} is replaced with the biome's boss display name.
    private static readonly string[] s_gateLines = new[]
    {
        "{0} still has dominion over my soul.",
        "The magicks of {0} keep me rooted here, forever.",
        "Defeat {0}, and I will swear a blood oath to you!",
        "{0} set me here to rot, forever.",
        "Only the fall of {0} can free me from this place.",
        "Break the hold {0} has on this land, then speak of rescue."
    };

    private static readonly Gate[] s_gates = new[]
    {
        new Gate(Heightmap.Biome.Meadows,     "",                    "Eikthyrdeer"),
        new Gate(Heightmap.Biome.BlackForest, "defeated_eikthyr",   "Eikthyrdeer"),
        new Gate(Heightmap.Biome.Swamp,       "defeated_gdking",    "Elder"),
        new Gate(Heightmap.Biome.Mountain,    "defeated_bonemass",  "Bonemass"),
        new Gate(Heightmap.Biome.Plains,      "defeated_dragon",    "Moder"),
        new Gate(Heightmap.Biome.Mistlands,   "defeated_goblinking", "Yagluth"),
        new Gate(Heightmap.Biome.AshLands,    "defeated_queen",     "Seeker Queen"),
        new Gate(Heightmap.Biome.DeepNorth,   "defeated_fader",     "Fader")
    };

    private static Gate FindGate(Heightmap.Biome biome)
    {
        foreach (Gate g in s_gates)
        {
            if (g.biome == biome)
            {
                return g;
            }
        }
        return null;
    }

    // True if a generated NPC in this biome should be gated (its boss-defeat key is unset).
    public static bool GateBlocked(Heightmap.Biome biome)
    {
        Gate g = FindGate(biome);
        if (g == null || string.IsNullOrEmpty(g.globalKey))
        {
            return false;
        }
        if (ZoneSystem.instance == null)
        {
            return true; // can't verify -> don't allow sequence-breaking
        }
        return !ZoneSystem.instance.GetGlobalKey(g.globalKey);
    }

    // A random Say() line for a gated biome, with the boss name filled in.
    public static string GateLine(Heightmap.Biome biome)
    {
        Gate g = FindGate(biome);
        string boss = (g != null) ? g.bossName : "";
        string template = s_gateLines[UnityEngine.Random.Range(0, s_gateLines.Length)];
        return string.Format(template, boss);
    }
}
