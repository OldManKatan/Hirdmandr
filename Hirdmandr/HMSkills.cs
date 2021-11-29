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

        public static string[] m_hmSkillNames = new string[9]
        {
            "woodburner",
            "furnaceoperator",
            "farmer",
            "cook",
            "baker",
            "melee",
            "range",
            "fighter",
            "gatherer"
        };

        public static string[] m_hmSkillReadables = new string[9]
        {
            "Wood Burner",
            "Furnace Operator",
            "Farmer",
            "Cook",
            "Baker",
            "Melee User",
            "Ranged User",
            "Fighter",
            "Gatherer"
        };

        public SkillData[] m_hmSkills = new SkillData[m_hmSkillNames.Length];
        public ZNetView m_znetv;

        public void LoadSkills()
        {
            for (var i = 0; i < m_hmSkillNames.Length; i++)
            {
                if (m_hmSkills[i] == null)
                {
                    m_hmSkills[i] = new SkillData();
                    m_hmSkills[i].m_name = m_hmSkillNames[i];
                    m_hmSkills[i].m_readable = m_hmSkillReadables[i];
                }

                m_hmSkills[i].m_value = m_znetv.GetZDO().GetFloat("hmnpc_skill" + m_hmSkillNames[i], 0f);
                m_hmSkills[i].m_isEnabled = m_znetv.GetZDO().GetBool("hmnpc_skill" + m_hmSkillNames[i] + "enable", false);
            }
        }

        public void SaveSkills()
        {
            for (var i = 0; i < m_hmSkillNames.Length; i++)
            {
                m_znetv.GetZDO().Set("hmnpc_skill" + m_hmSkillNames[i], m_hmSkills[i].m_value);
                m_znetv.GetZDO().Set("hmnpc_skill" + m_hmSkillNames[i] + "enable", m_hmSkills[i].m_isEnabled);
            }
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

        public void ModifySkill(string skill_name, float change, bool notLeveled=false)
        {
            skill_name = skill_name.Replace("skill", "");
            var skill_index = Array.IndexOf(m_hmSkillNames, skill_name);
            if (skill_index > -1)
            {
                var old_value = m_hmSkills[skill_index].m_value;
                var new_value = 0f;
                if (notLeveled)
                {
                    new_value = old_value + change;
                }
                else
                {
                    new_value = old_value + (change / (int)old_value);
                }
                if (new_value < 0)
                {
                    new_value = 0;
                }
                if (new_value > 100)
                {
                    new_value = 100;
                }
                if (Math.Floor(new_value) > Math.Floor(old_value))
                {
                    Jotunn.Logger.LogWarning("SKILLUP OCCURRED for " + m_znetv.GetComponent<Humanoid>().m_name + " from " + old_value + " to " + new_value);
                }
                m_hmSkills[skill_index].m_value = new_value;
                m_znetv.GetZDO().Set("hmnpc_skill" + skill_name, m_hmSkills[skill_index].m_value);
            }
            else
            {
                Jotunn.Logger.LogError("ModifySkill was called with unknown skill '" + skill_name + "'!");
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
            string[] artisanSkills = new string[]
            {
                "woodburner",
                "furnaceoperator",
                "farmer",
                "cook",
                "baker"
            };

            List<string> enabledSkills = new List<string>();
            foreach (SkillData skl in m_hmSkills)
            {
                if (skl.m_isEnabled)
                {
                    enabledSkills.Add(skl.m_name);
                    Jotunn.Logger.LogWarning("GetEnabledSkillsHighestFirst is adding " + skl.m_name + " to enabledSkills");
                }
            }

            Jotunn.Logger.LogWarning("enabledSkills return list");
            foreach (string aSkill in enabledSkills)
            {
                Jotunn.Logger.LogWarning("    " + aSkill);
            }

            List<string> returnList = new List<string>();
            var numSkills = enabledSkills.Count;

            for (int i = 0; i < numSkills; i++) {
                Jotunn.Logger.LogWarning("GetEnabledSkillsHighestFirst is evaluating skill #" + i);

                float highest_skill_value = -1f;
                string highest_skill_str = "";

                foreach (string aSkill in enabledSkills)
                {
                    if (GetSkill(aSkill) > highest_skill_value)
                    {
                        highest_skill_value = GetSkill(aSkill);
                        highest_skill_str = aSkill;
                    }
                    Jotunn.Logger.LogWarning("GetEnabledSkillsHighestFirst is looking at " + aSkill);
                }
                if (highest_skill_str != "")
                {
                    Jotunn.Logger.LogWarning("GetEnabledSkillsHighestFirst is adding " + highest_skill_str + " to returnList");
                    returnList.Add(highest_skill_str);
                    enabledSkills.RemoveAt(enabledSkills.IndexOf(highest_skill_str));
                }
            }
            Jotunn.Logger.LogWarning("GetEnabledSkillsHighestFirst FINAL RETURN LIST");
            foreach (string aStr in returnList)
            {
                Jotunn.Logger.LogWarning("    " + aStr);
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
