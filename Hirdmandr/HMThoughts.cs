using System;
using System.Collections.Generic;


namespace Hirdmandr
{
    [Serializable]
    public class HMThoughts
    {

        [Serializable]
        public class Thought
        {
            public tType m_type;
            public float m_calcStrength;
            public string m_subject;
            public float m_expires;
            public string m_misc;

            public Thought(tType thoughtType, float calculatedStrength, string subject, float expires, string misc)
            {
                m_type = thoughtType;
                m_calcStrength = calculatedStrength;
                m_subject = subject;
                m_expires = expires;
                m_misc = misc;
            }
        }

        public enum tType
        {
            noMod = 0,
            made10Things = 1,
            killedMob = 2,
            friendUp = 3,
            skillUp = 4,
            jobChange = 5,
            restedComfort = 6,
            craftSkillUp = 7,
            finishWorkday = 8,
            comfortWorkSite = 9,
            martialSkillUp = 10,
            himthikiFight = 11,
            thegnFight = 12,
            talkedWithAny = 13,
            talkedJarl = 14,
            talkedFriend = 15
        };

        public List<Thought> m_thoughts = new List<Thought>();
        public float[] m_thoughtModifiers;
        public Dictionary<tType, Dictionary<string, List<string>>> m_thoughtStrings = new Dictionary<tType, Dictionary<string, List<string>>>();
        public HirdmandrNPC m_hmnpc;
        public ZNetView m_znetv;
        public int made10ThingsCounter = 0;

        public HMThoughts(HirdmandrNPC npc, ZNetView znet)
        {
            m_hmnpc = npc;
            m_znetv = znet;
            m_thoughts = new List<Thought>();

            m_thoughtModifiers = new float[16]
            {
                1,
                m_hmnpc.m_personality.GetValue("craft"),  // made10Things
                m_hmnpc.m_personality.GetValue("valor"),  // killedMob
                m_hmnpc.m_personality.GetValue("relationship"),  // friendUp
                m_hmnpc.m_personality.GetValue("learning"),  // skillUp
                m_hmnpc.m_personality.GetValue("authority"),  // jobChange
                m_hmnpc.m_personality.GetValue("comfort"),  // restedComfort
                m_hmnpc.m_personality.GetValue("craft") + m_hmnpc.m_personality.GetValue("learning"),  // craftSkillUp
                m_hmnpc.m_personality.GetValue("craft") + m_hmnpc.m_personality.GetValue("authority"),  // finishWorkday
                m_hmnpc.m_personality.GetValue("craft") + m_hmnpc.m_personality.GetValue("comfort"),  // comfortWorkSite
                m_hmnpc.m_personality.GetValue("valor") + m_hmnpc.m_personality.GetValue("learning"),  // martialSkillUp
                m_hmnpc.m_personality.GetValue("valor") + m_hmnpc.m_personality.GetValue("authority"),  // himthikiFight
                m_hmnpc.m_personality.GetValue("valor") + m_hmnpc.m_personality.GetValue("comfort"),  // thegnFight
                m_hmnpc.m_personality.GetValue("relationship") + m_hmnpc.m_personality.GetValue("learning"),  // talkedWithAny
                m_hmnpc.m_personality.GetValue("relationship") + m_hmnpc.m_personality.GetValue("authority"),  // talkedJarl
                m_hmnpc.m_personality.GetValue("relationship") + m_hmnpc.m_personality.GetValue("comfort")  // talkedFriend
            };

            AddThoughtStrings();

            LoadThoughts();

            foreach (float tm in m_thoughtModifiers)
            {
                Jotunn.Logger.LogWarning("m_thoughtModifiers: " + tm);
            }
        }

        // public void GetThoughtModifiers()
        // {
        //     float[] m_thoughtModifiers = new float[15]
        //     {
        //         m_hmnpc.m_personality.GetValue("craft"),  // made10Things
        //         m_hmnpc.m_personality.GetValue("valor"),  // killedMob
        //         m_hmnpc.m_personality.GetValue("relationship"),  // friendUp
        //         m_hmnpc.m_personality.GetValue("learning"),  // skillUp
        //         m_hmnpc.m_personality.GetValue("authority"),  // jobChange
        //         m_hmnpc.m_personality.GetValue("comfort"),  // restedComfort
        //         m_hmnpc.m_personality.GetValue("craft") + m_hmnpc.m_personality.GetValue("learning"),  // craftSkillUp
        //         m_hmnpc.m_personality.GetValue("craft") + m_hmnpc.m_personality.GetValue("authority"),  // finishWorkday
        //         m_hmnpc.m_personality.GetValue("craft") + m_hmnpc.m_personality.GetValue("comfort"),  // comfortWorkSite
        //         m_hmnpc.m_personality.GetValue("valor") + m_hmnpc.m_personality.GetValue("learning"),  // martialSkillUp
        //         m_hmnpc.m_personality.GetValue("valor") + m_hmnpc.m_personality.GetValue("authority"),  // himthikiFight
        //         m_hmnpc.m_personality.GetValue("valor") + m_hmnpc.m_personality.GetValue("comfort"),  // thegnFight
        //         m_hmnpc.m_personality.GetValue("relationship") + m_hmnpc.m_personality.GetValue("learning"),  // talkedWithAny
        //         m_hmnpc.m_personality.GetValue("relationship") + m_hmnpc.m_personality.GetValue("authority"),  // talkedJarl
        //         m_hmnpc.m_personality.GetValue("relationship") + m_hmnpc.m_personality.GetValue("comfort")  // talkedFriend
        //     };
        // }

        public void LoadThoughts()
        {
            m_thoughts = new List<Thought>();
            string allThoughtStr = m_znetv.GetZDO().GetString("hmnpc_thoughts", "");

            if (allThoughtStr == "")
            {
                return;
            }

            string[] thoughtStr = allThoughtStr.Split('|');

            foreach (var aThought in thoughtStr)
            {
                if (aThought == "")
                {
                    continue;
                }
                string[] fieldStr = aThought.Split(';');
                AddThought(
                    (tType)int.Parse(fieldStr[0]),
                    float.Parse(fieldStr[1]),
                    fieldStr[2],
                    float.Parse(fieldStr[3]),
                    fieldStr[4]);
            }

            CalculateMood();
        }

        public void SaveThoughts()
        {
            List<string> eachThoughtStr = new List<string>();
            foreach (Thought aThought in m_thoughts)
            {
                eachThoughtStr.Add((int)aThought.m_type + ";" + aThought.m_calcStrength + ";" + aThought.m_subject + ";" + aThought.m_expires + ";" + aThought.m_misc);
            }

            if (eachThoughtStr.Count > 0)
            {
                string thoughtStr = string.Join("|", eachThoughtStr);
                m_znetv.GetZDO().Set("hmnpc_thoughts", thoughtStr);
            }
            else
            {
                m_znetv.GetZDO().Set("hmnpc_thoughts", "");
            }
        }

        public void AddThought(tType thoughtType, float baseStrength, string subject, float expires, string misc = "")
        {
            if (thoughtType == tType.made10Things)
            {
                made10ThingsCounter += 1;
                if (made10ThingsCounter < 10)
                {
                    return;
                }
                else
                {
                    made10ThingsCounter = 0;
                }
            }

            Jotunn.Logger.LogWarning("AddThought Base Strength = " + baseStrength);
            Jotunn.Logger.LogWarning("AddThought m_thoughtModifiers Strength = " + m_thoughtModifiers[(int)thoughtType]);

            float calcStrength = baseStrength * m_thoughtModifiers[(int)thoughtType];
            Jotunn.Logger.LogWarning("AddThought calcStrength Strength = " + calcStrength);

            m_thoughts.Add(new Thought(thoughtType, calcStrength, subject, expires, misc));

            CalculateMood();
            SaveThoughts();

            Jotunn.Logger.LogWarning("Thought of type " + Enum.GetName(typeof(tType), thoughtType) + " added with CalcStrength " + calcStrength);
        }

        public void CalculateMood()
        {
            float new_mood = 0f;
            List<Thought> removeThoughts = new List<Thought>();
            foreach (Thought aThought in m_thoughts)
            {
                if (UnityEngine.Time.time > aThought.m_expires)
                {
                    removeThoughts.Add(aThought);
                    Jotunn.Logger.LogWarning("GOING TO REMOVE Thought of type " + Enum.GetName(typeof(tType), aThought.m_type) + " with CalcStrength " + aThought.m_calcStrength);
                }
                else
                {
                    new_mood += aThought.m_calcStrength;
                }
            }

            foreach (Thought remThought in removeThoughts)
            {
                Jotunn.Logger.LogWarning("REMOVING Thought of type " + Enum.GetName(typeof(tType), remThought.m_type) + " with CalcStrength " + remThought.m_calcStrength);
                m_thoughts.RemoveAt(m_thoughts.IndexOf(remThought));
            }
            m_hmnpc.m_mood = new_mood;

            Jotunn.Logger.LogWarning("New MOOD calculated as " + m_hmnpc.m_mood);
        }

        public string StrengthToFeelStr(float strength)
        {
            string outStr = "";

            if (strength > 2000f) { outStr = "<color=green>Ecstatic</color>"; }  // 2000+
            else if (strength > 500) { outStr = "<color=green>Excellent</color>"; }  // 500 - 2000
            else if (strength > 100f) { outStr = "<color=blue>Great</color>"; }  // 100 - 500
            else if (strength > 50f) { outStr = "<color=blue>Good</color>"; }  // 50 - 100
            else if (strength > -50) { outStr = "Fine"; }                       // -50 - 50
            else if (strength > -100f) { outStr = "<color=orange>Bad</color>"; }  // -100 - -50
            else if (strength > -500f) { outStr = "<color=orange>Very Bad</color>"; }     // -500 - -100
            else if (strength > -2000f) { outStr = "<color=red>Terrible</color>"; }    // -2000 - -500
            else { outStr = "<color=red>The worst they have felt</color>"; }                      // -2000-

            return outStr;
        }

        public void AddThoughtStrings()
        {
            // made10Things
            m_thoughtStrings.Add(tType.made10Things, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.made10Things].Add("talkPositiveStrings", new List<string>()
            {
                "I've been really productive lately!",
                "I've done a lot of %subject% lately!"
            });
            m_thoughtStrings[tType.made10Things].Add("talkNeutralStrings", new List<string>()
            {
                "There's a lot to do around here."
            });
            m_thoughtStrings[tType.made10Things].Add("talkNegativeStrings", new List<string>()
            {
                "There is so much work around here.",
                "No matter how hard I work, there's more %subject% to do..."
            });
            m_thoughtStrings[tType.made10Things].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after being productive while %subject%. "
            });

            // killedMob
            m_thoughtStrings.Add(tType.killedMob, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.killedMob].Add("talkPositiveStrings", new List<string>()
            {
                "I really gave them a whopping on the battlefield!",
                "Let me tell you the story of my latest victory!"
            });
            m_thoughtStrings[tType.killedMob].Add("talkNeutralStrings", new List<string>()
            {
                "Fighting is something that has to be done.",
                "I don't like fighting but I like keeping you all safe."
            });
            m_thoughtStrings[tType.killedMob].Add("talkNegativeStrings", new List<string>()
            {
                "I fought for my life, it was terrifying.",
                "I was attacked, and barely made it out alive!"
            });
            m_thoughtStrings[tType.killedMob].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after killing a %subject%. "
            });

            // friendUp
            m_thoughtStrings.Add(tType.friendUp, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.friendUp].Add("talkPositiveStrings", new List<string>()
            {
                "I'm really getting along with %subject% these days!",
                "I'm closer than ever with %subject%, it's nice."
            });
            m_thoughtStrings[tType.friendUp].Add("talkNeutralStrings", new List<string>()
            {
                "I guess me and %subject% are friends or something?"
            });
            m_thoughtStrings[tType.friendUp].Add("talkNegativeStrings", new List<string>()
            {
                "%subject% kind of bothers me, but they really like me.",
                "Being friends wtih %subject% is hard work."
            });
            m_thoughtStrings[tType.friendUp].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after improving their relationship with %subject%. "
            });

            // skillUp
            m_thoughtStrings.Add(tType.skillUp, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.skillUp].Add("talkPositiveStrings", new List<string>()
            {
                "It felt good learning more about %subject%!",
                "I really love learning new things!",
                "When you really dive into something, there's so much to learn!"
            });
            m_thoughtStrings[tType.skillUp].Add("talkNeutralStrings", new List<string>()
            {
                "The learning goes on.",
                "It's tiresome, there's more to figure out."
            });
            m_thoughtStrings[tType.skillUp].Add("talkNegativeStrings", new List<string>()
            {
                "I had to solve some difficult problems today.",
                "Figuring things out is exhausting."
            });
            m_thoughtStrings[tType.skillUp].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after improving their skill at %subject%. "
            });

            // jobChange
            m_thoughtStrings.Add(tType.jobChange, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.jobChange].Add("talkPositiveStrings", new List<string>()
            {
                "Our Jarl has a new duty for me! I'm excited!",
                "I have some new work to do, I think our Jarl trusts me!"
            });
            m_thoughtStrings[tType.jobChange].Add("talkNeutralStrings", new List<string>()
            {
                "Another change to the old duties.",
                "The only thing that stays the same is changes."
            });
            m_thoughtStrings[tType.jobChange].Add("talkNegativeStrings", new List<string>()
            {
                "The Jarl keeps changing my duties around.",
                "I just want to do one thing, ya know?"
            });
            m_thoughtStrings[tType.jobChange].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after their duties were changed. "
            });

            // restedComfort
            m_thoughtStrings.Add(tType.restedComfort, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.restedComfort].Add("talkPositiveStrings", new List<string>()
            {
                "These bedrooms sure beat the inside of a Fuling's cage!",
                "I had a wonderful rest last night!"
            });
            m_thoughtStrings[tType.restedComfort].Add("talkNeutralStrings", new List<string>()
            {
                "The Jarl puts so much work into the bedrooms, but I'm not even awake there."
            });
            m_thoughtStrings[tType.restedComfort].Add("talkNegativeStrings", new List<string>()
            {
                "All these fancy beds are going to make us soft."
            });
            m_thoughtStrings[tType.restedComfort].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after sleeping in a comfortable room. "
            });

            // craftSkillUp
            m_thoughtStrings.Add(tType.craftSkillUp, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.craftSkillUp].Add("talkPositiveStrings", new List<string>()
            {
                "It felt good learning more about %subject%!",
                "At this rate, I'm going to be the best around here at %subject%."
            });
            m_thoughtStrings[tType.craftSkillUp].Add("talkNeutralStrings", new List<string>()
            {
                "I seem to be better at %subject% I guess."
            });
            m_thoughtStrings[tType.craftSkillUp].Add("talkNegativeStrings", new List<string>()
            {
                "[sigh] I'm struggling to learn %subject%.",
                "%subject% is boring, but I guess I gotta learn it."
            });
            m_thoughtStrings[tType.craftSkillUp].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after improving their skill at %subject%. "
            });

            // finishWorkday
            m_thoughtStrings.Add(tType.finishWorkday, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.finishWorkday].Add("talkPositiveStrings", new List<string>()
            {
                "Another fine day's work!",
                "Nothing feeds my soul quite like stout labor!"
            });
            m_thoughtStrings[tType.finishWorkday].Add("talkNeutralStrings", new List<string>()
            {
                "Another day done, tomorrow is another one.",
                "Finally got my work done, where's the mead?"
            });
            m_thoughtStrings[tType.finishWorkday].Add("talkNegativeStrings", new List<string>()
            {
                "It's just work, work, work, all the time.",
                "I work all day, just so I can have a few minutes to sleep then do it again."
            });
            m_thoughtStrings[tType.finishWorkday].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after finishing an artisan workday. "
            });

            // comfortWorkSite
            m_thoughtStrings.Add(tType.comfortWorkSite, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.comfortWorkSite].Add("talkPositiveStrings", new List<string>()
            {
                "Work is much nicer in a %subject% place!",
                "The Jarl makes such %subject% workshops!"
            });
            m_thoughtStrings[tType.comfortWorkSite].Add("talkNeutralStrings", new List<string>()
            {
                "Work is work, no matter how %subject% the workshop is."
            });
            m_thoughtStrings[tType.comfortWorkSite].Add("talkNegativeStrings", new List<string>()
            {
                "Having such lavish workshops seems wastegful."
            });
            m_thoughtStrings[tType.comfortWorkSite].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after working at a %subject% worksite. "
            });

            // martialSkillUp
            m_thoughtStrings.Add(tType.martialSkillUp, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.martialSkillUp].Add("talkPositiveStrings", new List<string>()
            {
                "It felt good learning more about %subject%!",
                "I am becoming a mighty warrior!"
            });
            m_thoughtStrings[tType.martialSkillUp].Add("talkNeutralStrings", new List<string>()
            {
                "I suppose learning about %subject% is important?"
            });
            m_thoughtStrings[tType.martialSkillUp].Add("talkNegativeStrings", new List<string>()
            {
                "I don't like violence, learning about %subject% is uncomfortable."
            });
            m_thoughtStrings[tType.martialSkillUp].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after improving their skill at %subject%. "
            });

            // himthikiFight
            m_thoughtStrings.Add(tType.himthikiFight, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.himthikiFight].Add("talkPositiveStrings", new List<string>()
            {
                "Ha ha ha! I was a legend among heroes on the battlefield!",
                "I waged glorious war in the name of our Jarl!",
                "Those %subject%s were no match for us!"
            });
            m_thoughtStrings[tType.himthikiFight].Add("talkNeutralStrings", new List<string>()
            {
                "Another day, another fight. It was %subject%s this time.",
                "The Jarl and I ventured out again, I'm glad I made it back."
            });
            m_thoughtStrings[tType.himthikiFight].Add("talkNegativeStrings", new List<string>()
            {
                "It's a dirty job, murdering %subject%s.",
                "So many dead, by my hand no less."
            });
            m_thoughtStrings[tType.himthikiFight].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after fighting %subject%s as a Himthiki. "
            });

            // thegnFight
            m_thoughtStrings.Add(tType.thegnFight, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.thegnFight].Add("talkPositiveStrings", new List<string>()
            {
                "Ha ha ha! I was a legend among heroes on the battlefield!",
                "Our settlement is safe, because of me!",
                "Some %subject%s tried to sack our stronghold, but I sent them to the Underworld!"
            });
            m_thoughtStrings[tType.thegnFight].Add("talkNeutralStrings", new List<string>()
            {
                "The attacks are everyday, but we turned them back again.",
                "I kept us safe again, for whatever it is worth."
            });
            m_thoughtStrings[tType.thegnFight].Add("talkNegativeStrings", new List<string>()
            {
                "The %subject%s attacked again, they'll be the death of me one of these days.",
                "When I fall in defense of this place, who will watch the walls?"
            });
            m_thoughtStrings[tType.thegnFight].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after fighting %subject%s as a Thegn. "
            });

            // talkedWithAny
            m_thoughtStrings.Add(tType.talkedWithAny, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.talkedWithAny].Add("talkPositiveStrings", new List<string>()
            {
                "I had a nice chat with %subject%!",
                "I always love talking with %subject%, they're such fun!",
                "I got to spend time with %subject%, it was nice!"
            });
            m_thoughtStrings[tType.talkedWithAny].Add("talkNeutralStrings", new List<string>()
            {
                "I had a chat with %subject%.",
                "%subject% sure had a lot to say earlier."
            });
            m_thoughtStrings[tType.talkedWithAny].Add("talkNegativeStrings", new List<string>()
            {
                "%subject% was talking my ear off.",
                "'You know who' was bothering me the other day."
            });
            m_thoughtStrings[tType.talkedWithAny].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after talking with %subject%. "
            });

            // talkedJarl
            m_thoughtStrings.Add(tType.talkedJarl, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.talkedJarl].Add("talkPositiveStrings", new List<string>()
            {
                "I got to speak with the Jarl, it is such an honor!",
                "Our Jarl is just, and takes time with their subjects!"
            });
            m_thoughtStrings[tType.talkedJarl].Add("talkNeutralStrings", new List<string>()
            {
                "The Jarl needed to talk to me.",
                "%subject% sure had a lot to say earlier."
            });
            m_thoughtStrings[tType.talkedJarl].Add("talkNegativeStrings", new List<string>()
            {
                "Our Jarl wanted to speak with me, I fear they don't like me.",
                "The Lord always has an opinion, on everything."
            });
            m_thoughtStrings[tType.talkedJarl].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after speaking with their Jarl. "
            });

            // talkedFriend
            m_thoughtStrings.Add(tType.talkedFriend, new Dictionary<string, List<string>>());
            m_thoughtStrings[tType.talkedFriend].Add("talkPositiveStrings", new List<string>()
            {
                "I love spending time with %subject%!",
                "Have you seen %subject% around today, I miss them!",
                "I had a nice long chat with %subject%, it was the highlight of my day!"
            });
            m_thoughtStrings[tType.talkedFriend].Add("talkNeutralStrings", new List<string>()
            {
                "I got to speak with a friend.",
                "I spoke to my friend %subject% about a few things."
            });
            m_thoughtStrings[tType.talkedFriend].Add("talkNegativeStrings", new List<string>()
            {
                "I like %subject%, but sometimes being their friend is a lot of work.",
                "%subject% and I had a bit of a fight recently."
            });
            m_thoughtStrings[tType.talkedFriend].Add("thoughtStrings", new List<string>()
            {
                "Felt %feltAbout% after speaking with their friend %subject%. "
            });

        }
    }
}
