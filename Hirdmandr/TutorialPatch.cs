using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hirdmandr
{
    [HarmonyPatch(typeof(Tutorial))]
    [HarmonyPatch("Awake")]
    internal class TutorialPatch
    {
        static void Postfix(ref Tutorial __instance)
        {
            var tut_adds = new List<string>
            {
                "hirdmandr_find_rescue",
                "hirdmandr_first_rescue",
                "hirdmandr_welcome_home",
                "hirdmandr_thegn",
                "hirdmandr_himthiki",
                "hirdmandr_artisan"
            };
            for (var i = 0; i < __instance.m_texts.Count; i++)
            {
                if (tut_adds.Contains(__instance.m_texts[i].m_name))
                {
                    tut_adds.RemoveAt(tut_adds.IndexOf(__instance.m_texts[i].m_name));
                }
            }

            if (tut_adds.Contains("hirdmandr_find_rescue"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_find_rescue",
                    m_topic = "You are not the first...",
                    m_label = "This is the label",
                    m_text = "Odin's Valkyires have been bringing warriors to Valheim for centuries. Warriors who succeed in the tasks laid out by Odin are " +
                    "carried to Valhalla by the Valkyries. Those who fail lose Odin's blessing and are no longer to be reborn on Valheim when they die, instead " +
                    "they are claimed by Hel and the Valkyries bring them to Helheim upon their death in Valheim.\n\n" +
                    "If you find these cast out warriors and offer them protection: they will join your cause as one of your Hirdmandr and serve you faithfully!"
                };

                __instance.m_texts.Add(hm_temp);
            }

            if (tut_adds.Contains("hirdmandr_first_rescue"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_first_rescue",
                    m_topic = "Becoming a Jarl of Valheim",
                    m_label = "This is the label",
                    m_text = "You have rescued a lost warrior! \n\n" +
                    "You have taken your first step to becoming a mighty Jarl of Valheim! To be a Jarl is to be responsible for the health, happiness, and security of your Hirdmandr. " +
                    "Your first task as a Jarl is to lead this lost warrior to one of your strongholds or camps. You may welcome them to their new home only within range of a " +
                    "workbench and a fire \n\n" +
                    "Whether grand castle or humble shack, your new friend will make their home in that place as a Hirdmandr, loyal to their benevolent Jarl: You."
                };

                __instance.m_texts.Add(hm_temp);
            }

            if (tut_adds.Contains("hirdmandr_welcome_home"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_welcome_home",
                    m_topic = "A Jarl's Hirdmandr",
                    m_label = "This is the label",
                    m_text = "<color=yellow><b>Hugin</b></color> and <color=yellow><b>Munin</b></color> join you in welcoming this lost warrior home! \n\n" +
                    "Take some time to speak [<color=yellow><b>E</b></color>] with your new hirdmandr, and discuss their duties [<color=yellow><b>LShift+E</b></color>] " +
                    "at length. Learn about their personality, a good Jarl assigns duties by each person's stengths! You can also discover their skills, " +
                    "which may help you. Remember: Each of your hirdmandr may be assigned a role of Artisan or Warrior, but not both."
                };

                __instance.m_texts.Add(hm_temp);
            }

            if (tut_adds.Contains("hirdmandr_thegn"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_thegn",
                    m_topic = "Your Garrison and Militia",
                    m_label = "This is the label",
                    m_text = "Thegn are young warriors, full of drive but lacking in experience. Warriors assigned to the Thegn duty will remain " +
                    "in your stronghold and protect your hirdmandr from the threats of wandering creatures and attacks by the forsaken. Once the " +
                    "mod author figures out how to do patrols, there will be a note about it here!"
                };

                __instance.m_texts.Add(hm_temp);
            }

            if (tut_adds.Contains("hirdmandr_himthiki"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_himthiki",
                    m_topic = "A Jarl's Elite Warriors",
                    m_label = "This is the label",
                    m_text = "A Himthiki is an elite warrior! They have been the backbone of every Jarl's royal guard for centuries. At your command, " +
                    "your Himthiki will follow you out of your stronghold and into glorious battle. Take care with their trust! Valheim " +
                    "is dangerous, and a Himthiki who dies is not reborn. Himthiki with the Gatherer job enabled will pick up useful things on your " +
                    "journeys, everything from stone to cloudberries."
                };

                __instance.m_texts.Add(hm_temp);
            }

            if (tut_adds.Contains("hirdmandr_artisan"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_artisan",
                    m_topic = "Artisans and Laborers",
                    m_label = "This is the label",
                    m_text = "While any hirdmandr can learn any skill, many hirdmandr are best suited to the quiet, industrious life of an Artisan. " +
                    "You may give any number of artisan duties to each hirdmandr, but they always prefer the jobs they are best at. Each artisan " +
                    "requires a workspace before they can work. A Wood Burner, for example, requires an NPC Chest (any kind) close to a Kiln."
                };

                __instance.m_texts.Add(hm_temp);
            }
        }
    }
}
