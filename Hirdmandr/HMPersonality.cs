using System;


namespace Hirdmandr
{
    [Serializable]
    public class HMPersonality
    {

        [Serializable]
        public class ValueData
        {
            public string m_name;
            public string m_readable;
            public float m_value;
        }

        public static string[] m_hmValueNames = new string[6]
        {
            "craft",
            "valor",
            "relationship",
            "learning",
            "authority",
            "comfort"
        };

        public static string[] m_hmValueReadables = new string[6]
        {
            "Crafting",
            "Valor",
            "Relationships",
            "Learning",
            "Authority",
            "Comfort"
        };

        public ValueData[] m_hmValues = new ValueData[m_hmValueNames.Length];
        public ZNetView m_znetv;

        public void LoadValues()
        {
            var num_positive = 0;
            var num_total = 0f;
            bool new_generation = false;
            for (var i = 0; i < m_hmValueNames.Length; i++)
            {
                m_hmValues[i] = new ValueData();
                m_hmValues[i].m_name = m_hmValueNames[i];
                m_hmValues[i].m_readable = m_hmValueReadables[i];

                m_hmValues[i].m_value = m_znetv.GetZDO().GetFloat("hmnpc_value" + m_hmValueNames[i], -2f);

                if (m_hmValues[i].m_value == -2f)
                {
                    new_generation = true;
                }
            }

            if (new_generation)
            {
                for (var j = 0; j < 5; j++)
                {
                    for (var i = 0; i < m_hmValueNames.Length; i++)
                    {
                        m_hmValues[i].m_value = UnityEngine.Random.Range(-1f, 1f);
                    }

                    num_positive = 0;
                    foreach (ValueData this_value in m_hmValues)
                    {
                        if (this_value.m_value >= 0)
                        {
                            num_positive++;
                        }
                        num_total += this_value.m_value;
                    }
                    if ((num_positive >= (m_hmValueNames.Length / 2)) && (num_total > 0))
                    {
                        break;
                    }
                    else if (num_positive >= (m_hmValueNames.Length / 2))
                    {
                        Jotunn.Logger.LogInfo("Hirdmandr NPC creating new personality values was rejected for 'TOO MANY NEGATIVE', trying again.");
                    }
                    else if (num_positive >= (m_hmValueNames.Length / 2))
                    {
                        Jotunn.Logger.LogInfo("Hirdmandr NPC creating new personality values was rejected for 'TOTAL IS NEGATIVE', trying again.");
                    }
                    if (j == 4)
                    {
                        Jotunn.Logger.LogInfo("Hirdmandr NPC creating new personality values exceeded retry count. Using existing values.");
                    }
                }
            }
            if (new_generation)
            {
                LogPersonality();
            }
            SaveValues();
        }

        public void SaveValues()
        {
            for (var i = 0; i < m_hmValueNames.Length; i++)
            {
                m_znetv.GetZDO().Set("hmnpc_value" + m_hmValueNames[i], m_hmValues[i].m_value);
            }
        }

        public float GetValue(string value_name)
        {
            var value_index = Array.IndexOf(m_hmValueNames, value_name);
            if (value_index > -1)
            {
                return m_hmValues[value_index].m_value;
            }
            return 0f;
        }

        public void ModifyValue(string value_name, float change)
        {
            value_name = value_name.Replace("value", "");
            var value_index = Array.IndexOf(m_hmValueNames, value_name);
            if (value_index > -1)
            {
                var new_value = m_hmValues[value_index].m_value + change;
                if (new_value < -1f)
                {
                    new_value = -1f;
                }
                if (new_value > 1f)
                {
                    new_value = 1f;
                }
                m_hmValues[value_index].m_value = new_value;
                m_znetv.GetZDO().Set("hmnpc_value" + value_name, m_hmValues[value_index].m_value);
            }
            else
            {
                Jotunn.Logger.LogError("ModifyValue was called with unknown value name '" + value_name + "'!");
            }
        }

        public string GetHighestValue()
        {
            var highest_value_str = "";
            float highest_value_value = -1f;

            foreach (ValueData skl in m_hmValues)
            {
                if (skl.m_value > highest_value_value)
                {
                    highest_value_str = skl.m_name;
                    highest_value_value = skl.m_value;
                }
            }

            return highest_value_str;
        }

        public void LogPersonality()
        {
            Jotunn.Logger.LogInfo("Hirdmandr NPC personality values:");
            foreach (ValueData this_value in m_hmValues)
            {
                Jotunn.Logger.LogInfo("    " + this_value.m_name + " = " + this_value.m_value);
            }
        }

    }
}
