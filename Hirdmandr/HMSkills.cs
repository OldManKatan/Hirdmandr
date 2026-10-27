using System;
using System.Collections.Generic;


namespace Hirdmandr
{
    [Serializable]
    public class HMSkills
    {

        [Serializable]
        public class SkillData
        {
            public string m_name;
            public string m_readable;
            public float m_value;
            public bool m_isEnabled = false;
        }

        public class SkillDef
        {
            public readonly string key;
            public readonly string readable;
            public SkillDef(string key, string readable)
            {
                this.key = key;
                this.readable = readable;
            }
        }

        // Single source of truth for the skill registry + display order. Each entry couples a
        // persistent key with its display name so the two can never drift apart (the old parallel
        // m_hmSkillNames / m_hmSkillReadables arrays could, which misaligned the GUI after cook/baker
        // were dropped). List order == top-to-bottom order in the GUI skill list.
        public static readonly SkillDef[] m_skillDefs = new SkillDef[]
        {
            new SkillDef("mender", "Mender"),
            new SkillDef("lamplighter", "Lamplighter"),
            new SkillDef("woodburner", "Wood Burner"),
            new SkillDef("furnaceoperator", "Furnace Operator"),
            new SkillDef("farmer", "Farmer"),
            new SkillDef("gatherer", "Gatherer"),
            new SkillDef("fighter", "Fighter"),
            new SkillDef("melee", "Melee User"),
            new SkillDef("range", "Ranged User")
        };

        // Flat key array in display order, derived from m_skillDefs so the keys live in exactly one
        // place. Kept so the existing Array.IndexOf(m_hmSkillNames, ...) / m_hmSkillNames[i] call sites
        // keep working unchanged.
        public static readonly string[] m_hmSkillNames = DeriveSkillNames();
        static string[] DeriveSkillNames()
        {
            var names = new string[m_skillDefs.Length];
            for (var i = 0; i < m_skillDefs.Length; i++)
            {
                names[i] = m_skillDefs[i].key;
            }
            return names;
        }

        // The artisan worksite jobs - the ONLY skills GetEnabledSkillsHighestFirst ranks.
        // Same set as HirdmandrCommand's "Valid jobs" list. Everything else in m_hmSkillNames
        // is NOT a job: fighter/melee/range are the warrior's combat-style flags (the
        // Thegn/Himthiki persona code enables/reads them under the hood - left untouched here),
        // and gatherer is a skill with no worksite.
        public static readonly string[] m_artisanJobSkills = new string[]
        {
            "mender",
            "lamplighter",
            "woodburner",
            "furnaceoperator",
            "farmer"
        };

        public SkillData[] m_hmSkills = new SkillData[m_hmSkillNames.Length];
        public ZNetView m_znetv;
        public HirdmandrNPC m_hmnpc;


        public HMSkills(HirdmandrNPC npc, ZNetView znet)
        {
            m_hmnpc = npc;
            m_znetv = znet;

            LoadSkills();
        }

        public void LoadSkills()
        {
            for (var i = 0; i < m_hmSkillNames.Length; i++)
            {
                if (m_hmSkills[i] == null)
                {
                    m_hmSkills[i] = new SkillData();
                    m_hmSkills[i].m_name = m_hmSkillNames[i];
                    m_hmSkills[i].m_readable = m_skillDefs[i].readable;
                }

                // The ZDO is wired onto this ZNetView only after Awake (CreateObject binds it
                // post-Instantiate), so it may be null here. Keep the defaults until it is set.
                var zdo = m_znetv.GetZDO();
                if (zdo == null)
                {
                    continue;
                }
                m_hmSkills[i].m_value = zdo.GetFloat("hmnpc_skill" + m_hmSkillNames[i], 0f);
                m_hmSkills[i].m_isEnabled = zdo.GetBool("hmnpc_skill" + m_hmSkillNames[i] + "enable", false);
            }
        }

        public void SaveSkills()
        {
            if (m_znetv.GetZDO() == null)
            {
                Jotunn.Logger.LogInfo("[HMSkills.SaveSkills] ZDO is null, NOT persisting");
                return;
            }
            var enabledNow = new List<string>();
            for (var i = 0; i < m_hmSkillNames.Length; i++)
            {
                m_znetv.GetZDO().Set("hmnpc_skill" + m_hmSkillNames[i], m_hmSkills[i].m_value);
                m_znetv.GetZDO().Set("hmnpc_skill" + m_hmSkillNames[i] + "enable", m_hmSkills[i].m_isEnabled);
                if (m_hmSkills[i].m_isEnabled)
                {
                    enabledNow.Add(m_hmSkillNames[i]);
                }
            }
            Jotunn.Logger.LogInfo("[HMSkills.SaveSkills] persisted enabled=[" + string.Join(",", enabledNow.ToArray()) + "] furnaceop_readback=" + m_znetv.GetZDO().GetBool("hmnpc_skillfurnaceoperatorenable"));
        }

        public float GetSkill(string skill_name)
        {
            var skill_index = Array.IndexOf(m_hmSkillNames, skill_name);
            if (skill_index > -1)
            {
                return m_hmSkills[skill_index].m_value;
            }
            return 0f;
        }

        public void ModifySkill(string skill_name, float change, bool leveled=true)
        {
            skill_name = skill_name.Replace("skill", "");
            var skill_index = Array.IndexOf(m_hmSkillNames, skill_name);
            if (skill_index > -1)
            {
                var old_value = m_hmSkills[skill_index].m_value;
                var new_value = 0f;
                if (!leveled)
                {
                    new_value = old_value + change;
                }
                else
                {
                    if (Math.Floor(old_value) > 0)
                    {
                        new_value = old_value + (float)(change / Math.Floor(old_value));
                    }
                    else
                    {
                        new_value = old_value + change;
                    }
                }

                if (new_value < 0)
                {
                    new_value = 0;
                }
                if (new_value > 100)
                {
                    new_value = 100;
                }

                // One trace line per modification (down from the old 5-line dump) so skill
                // progression is still visible without flooding the log.
                Jotunn.Logger.LogInfo("  MODIFY SKILL " + skill_name + ": " + old_value.ToString("0.##") + " -> " + new_value.ToString("0.##") + " (change " + change.ToString("0.##") + ")");

                if (leveled && Math.Floor(new_value) > Math.Floor(old_value))
                {
                    Jotunn.Logger.LogInfo("SKILLUP OCCURRED for " + m_znetv.GetComponent<NPCPlayerClone>().m_name + " from " + old_value + " to " + new_value);
                    // Play via the effect bus: FireMpEffect plays locally on the owner and
                    // broadcasts to every other client (the handler skips the owner). The skill
                    // level stays in the ZDO (state channel); only the trigger + a log-only
                    // "skillName:level" ride the RPC.
                    m_hmnpc.FireMpEffect(HMEffectKind.SkillLevelup, skill_name + ":" + (int)Math.Floor(new_value));
                    // Socialize: queue a "skillup" moment (the readable name + the new level).
                    // Warrior skills (fighter/melee/range) are suppressed: their readable names
                    // ("Fighter", "Melee User", "Ranged User") read awkwardly in the lines, and
                    // this Valheim build has no clean weapon-in-hand to name them after (equipment
                    // is stored by item hash, no Item/GetFullName; fighter has no weapon at all).
                    // Only artisan skillups create moments, so the trade names carry the value.
                    if (skill_name != "fighter" && skill_name != "melee" && skill_name != "range")
                    {
                        m_hmnpc.m_socialize.EnqueueSkillup(skill_name, m_skillDefs[skill_index].readable, (int)Math.Floor(new_value));
                    }
                }
                m_hmSkills[skill_index].m_value = new_value;
                m_znetv.GetZDO().Set("hmnpc_skill" + skill_name, m_hmSkills[skill_index].m_value);

                // Combat skills scale live stats: fighter -> maxHP/heal, melee -> melee damage,
                // range -> bow arrow power. Re-deriving here keeps level-ups visible immediately.
                if (m_hmnpc != null
                    && (skill_name == "fighter" || skill_name == "melee" || skill_name == "range"))
                {
                    m_hmnpc.PopulateCombatProps();
                }
            }
            else
            {
                Jotunn.Logger.LogError("ModifySkill was called with unknown skill '" + skill_name + "'!");
            }
        }

        // Direct test setter: sets an exact skill value (bypassing the leveled ModifySkill math)
        // and re-applies any live combat stat that depends on it.
        public void SetSkillValue(string skill_name, float value)
        {
            skill_name = skill_name.Replace("skill", "");
            var skill_index = Array.IndexOf(m_hmSkillNames, skill_name);
            if (skill_index > -1)
            {
                if (value < 0f)
                {
                    value = 0f;
                }
                if (value > 100f)
                {
                    value = 100f;
                }

                m_hmSkills[skill_index].m_value = value;
                if (m_znetv != null)
                {
                    var zdo = m_znetv.GetZDO();
                    if (zdo != null)
                    {
                        zdo.Set("hmnpc_skill" + skill_name, value);
                    }
                }

                if (m_hmnpc != null
                    && (skill_name == "fighter" || skill_name == "melee" || skill_name == "range"))
                {
                    m_hmnpc.PopulateCombatProps();
                }
            }
            else
            {
                Jotunn.Logger.LogError("SetSkillValue was called with unknown skill '" + skill_name + "'!");
            }
        }

        public string GetHighestSkill()
        {
            var highest_skill_str = "";
            float highest_skill_value = -1f;

            foreach (SkillData skl in m_hmSkills)
            {
                if (skl.m_value > highest_skill_value)
                {
                    highest_skill_str = skl.m_name;
                    highest_skill_value = skl.m_value;
                }
            }

            return highest_skill_str;
        }

        public List<string> GetEnabledSkillsHighestFirst()
        {
            List<string> enabledSkills = new List<string>();
            foreach (SkillData skl in m_hmSkills)
            {
                // Only the 5 worksite job skills are jobs (m_artisanJobSkills). Without this
                // filter an enabled warrior combat skill would be ranked as a job: the
                // Thegn/Himthiki persona code enables melee/range under the hood, and that flag
                // survives a warrior -> artisan role switch (RoleChanged never clears it), so a
                // switched-back artisan would rank "melee" and crash in
                // HirdmandrChest.IsValidWorksite (no artisanJobPieces entry for a combat key).
                if (Array.IndexOf(m_artisanJobSkills, skl.m_name) < 0)
                {
                    continue;
                }
                if (skl.m_isEnabled)
                {
                    enabledSkills.Add(skl.m_name);
                }
            }

            List<string> returnList = new List<string>();
            var numSkills = enabledSkills.Count;

            for (int i = 0; i < numSkills; i++)
            {
                float highest_skill_value = -1f;
                string highest_skill_str = "";

                foreach (string aSkill in enabledSkills)
                {
                    if (GetSkill(aSkill) > highest_skill_value)
                    {
                        highest_skill_value = GetSkill(aSkill);
                        highest_skill_str = aSkill;
                    }
                }
                if (highest_skill_str != "")
                {
                    returnList.Add(highest_skill_str);
                    enabledSkills.RemoveAt(enabledSkills.IndexOf(highest_skill_str));
                }
            }
            return returnList;
        }

        public void LogSkills()
        {
            Jotunn.Logger.LogInfo("Hirdmandr NPC skill values:");
            foreach (SkillData this_skill in m_hmSkills)
            {
                Jotunn.Logger.LogInfo("    " + this_skill.m_name + " = " + this_skill.m_value);
            }
        }

        public void SetSkillEnable(string skill_name, bool enable_value)
        {
            var skill_index = Array.IndexOf(m_hmSkillNames, skill_name);
            if (skill_index > -1)
            {
                m_hmSkills[skill_index].m_isEnabled = enable_value;
                m_znetv.GetZDO().Set("hmnpc_skill" + m_hmSkillNames[skill_index] + "enable", m_hmSkills[skill_index].m_isEnabled);
                if (enable_value)
                {
                    Jotunn.Logger.LogInfo("Skill " + skill_name + " was Enabled.");
                }
                else
                {
                    Jotunn.Logger.LogInfo("Skill " + skill_name + " was Disabled.");
                }
            }
        }

        // Local-only enable toggle: updates this client's cache without touching the ZDO.
        // The GUI routes the toggle through HirdmandrNPC.InvokeSetting, which persists on
        // the owner; this keeps a non-owner toggle from doing an owner-only ZDO Set (review F2).
        public void SetSkillEnableLocal(string skill_name, bool enable_value)
        {
            var skill_index = Array.IndexOf(m_hmSkillNames, skill_name);
            if (skill_index > -1)
            {
                m_hmSkills[skill_index].m_isEnabled = enable_value;
            }
        }

        public bool isEnabled(string skill_name)
        {
            var skill_index = Array.IndexOf(m_hmSkillNames, skill_name);
            if (skill_index > -1)
            {
                return m_hmSkills[skill_index].m_isEnabled;
            }
            return false;
        }
    }
}
