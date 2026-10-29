using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace Hirdmandr
{
    // Single source of truth for every Hirdmandr tutorial: the name constants, the tutorial text
    // definitions, and the one-line Show() helper. Replaces the old TutorialPatch (which only
    // registered texts) and the scattered Player.ShowTutorial("...") call sites, so the exact
    // string used to register a tutorial and the string used to show it are the same const and can
    // never drift apart (a ShowTutorial() of an unregistered name is a silent no-op).
    //
    // Raven scheme (matches the game's two ravens), used to pick m_isMunin for each tutorial:
    //   Hugin  (m_isMunin = false) = action / war / discovery / forward  ("what's to come")
    //   Munin  (m_isMunin = true)  = memory / knowledge / management / lore / correction ("what was")
    public static class HMTutorial
    {
        // One const per tutorial. The same string is registered in Definitions() and passed to
        // Show(), so a call can never reference a name that was never registered.
        public const string FindRescue  = "hirdmandr_find_rescue";
        public const string FirstRescue = "hirdmandr_first_rescue";
        public const string WelcomeHome = "hirdmandr_welcome_home";
        public const string Thegn       = "hirdmandr_thegn";
        public const string Himthiki    = "hirdmandr_himthiki";
        public const string Artisan     = "hirdmandr_artisan";

        // Phase 2: first-time / failure tutorials, wired at their natural trigger points. Each shows
        // only once (the game's m_shownTutorials store), so callers need no per-tutorial "shown" tracking.
        public const string FirstRest      = "hirdmandr_first_rest";
        public const string FirstWorksite  = "hirdmandr_first_worksite";
        public const string FirstBattle    = "hirdmandr_first_battle";
        public const string FirstFollow    = "hirdmandr_first_follow";
        public const string FirstGuardHere = "hirdmandr_first_guard_here";
        public const string FirstSkill     = "hirdmandr_first_skill";
        public const string WelcomeFailed  = "hirdmandr_welcome_failed";
        public const string BedTooFar      = "hirdmandr_bed_too_far";
        public const string HimthikiDeath  = "hirdmandr_himthiki_death";

        // Show a tutorial to a player. Defaults to the local player; pass a specific Player for
        // owner-scoped cases (e.g. first_rescue, shown to the rescuing player, not necessarily the
        // local one). The game's own m_shownTutorials store guarantees each tutorial shows only once,
        // so callers need no per-tutorial "has been shown" tracking.
        public static void Show(string key, Player player = null)
        {
            (player ?? Player.m_localPlayer)?.ShowTutorial(key);
        }

        // Every Hirdmandr tutorial definition, text kept verbatim from the originals. m_label is left
        // as the game's placeholder exactly as the originals had it. m_topic is the heading line and
        // m_text is the body (Valheim rich text: \n\n for paragraphs, <color>/<b> for emphasis).
        internal static List<Tutorial.TutorialText> Definitions()
        {
            return new List<Tutorial.TutorialText>
            {
                new Tutorial.TutorialText()
                {
                    m_name = FindRescue,
                    m_topic = "You are not the first...",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "Odin's Valkyries have been bringing warriors to Valheim for centuries. Warriors who please the allfather are " +
                    "carried to Valhalla by his Valkyries. Those who fail lose Odin's blessing and are no longer reborn in the 10th world. Their souls are claimed " +
                    "by the Forsaken, to be their unholy soldiers should the 10th world ever rejoin the world tree.\n\n" +
                    "If you find these cast out warriors and offer them protection: they will join your cause as one of your Hirdmandr and serve you faithfully!"
                },
                new Tutorial.TutorialText()
                {
                    m_name = FirstRescue,
                    m_topic = "Becoming a Jarl of Valheim",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "You have rescued a lost warrior! \n\n" +
                    "You have taken your first step to becoming a mighty Jarl of Valheim! To be a Jarl is to be responsible for the health, happiness, and security of your Hirdmandr. " +
                    "Your first task as a Jarl is to lead this lost warrior to one of your strongholds or camps. You may welcome them to their new home only within range of a " +
                    "<color=yellow><b>workbench</b></color> and a <color=yellow><b>fire</b></color> \n\n" +
                    "Whether grand castle or humble shack, your new friend will make their home in that place as a Hirdmandr, loyal to their benevolent Jarl: You."
                },
                new Tutorial.TutorialText()
                {
                    m_name = WelcomeHome,
                    m_topic = "A Jarl's Hirdmandr",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "<color=yellow><b>Hugin</b></color> and <color=yellow><b>Munin</b></color> join you in welcoming this lost warrior home! \n\n" +
                    "Take some time to speak with your new hirdmandr, and discuss their duties [<color=yellow><b>LShift+E</b></color>] " +
                    "at length. A good Jarl assigns duties by each person's strengths! Hirdmandr have many skills, " +
                    "which may help you, in stronghold or afield. Remember: Each of your hirdmandr may be assigned a role of Artisan or Warrior, but not both."
                },
                new Tutorial.TutorialText()
                {
                    m_name = Thegn,
                    m_topic = "Your Garrison and Militia",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "Thegn are young warriors, full of drive but lacking in experience. Warriors assigned to the Thegn duty will remain " +
                    "in your stronghold and protect your hirdmandr from the threats of wandering creatures and attacks by the forsaken. Lead them " +
                    "to their their post and ask them to guard there. They will return to this spot each guard shift, to keep the people and holdings " + 
					"of their Jarl safe.\n\n" + 
					"Should any Hirdmandr find an enemy of their Jarl, Thegn will converge upon the threat to protect their kin!"
                },
                new Tutorial.TutorialText()
                {
                    m_name = Himthiki,
                    m_topic = "A Jarl's Elite Warriors",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "A Himthiki is an elite warrior! They have been the backbone of every Jarl's royal guard for centuries. At your command, " +
                    "your Himthiki will follow you out of your stronghold and into glorious battle. Take care with their trust! Valheim " +
                    "is dangerous, and a Jarl must barter with Hel to recover a Himthiki who has died.\n\n" + 
					"Himthiki with the Gatherer job enabled will " + 
					"pick up useful things on your journeys, everything from stone to cloudberries."
                },
                new Tutorial.TutorialText()
                {
                    m_name = Artisan,
                    m_topic = "Artisans and Laborers",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "While any hirdmandr can learn any skill, many hirdmandr are best suited to the quiet, industrious life of an Artisan. " +
                    "You may give any number of artisan duties to each hirdmandr, but they always prefer the jobs they are best at. Each artisan " +
                    "requires a workspace before they can work. A Wood Burner, for example, requires an NPC Chest (any kind) close to a Kiln. " + 
					"Stock this chest with wood, and your artisan will put the coal they produce back in this chest. Hirdmandr respect their Jarl's " + 
					"possessions, and only ever touch items in NPC containers."
                },
                new Tutorial.TutorialText()
                {
                    m_name = FirstRest,
                    m_topic = "A Rest for the Weary",
                    m_label = "This is the label",
                    m_isMunin = true,
                    m_text = "Your hirdmandr has found rest! \n\n" +
                    "A hirdmandr rests by lying on a bed, and can only rest in a bed it can reach. Keep a bed close to where your hirdmandr live " +
                    "and work, so they can find their way back to it. A hirdmandr with no bed in reach has no rest to find."
                },
                new Tutorial.TutorialText()
                {
                    m_name = FirstWorksite,
                    m_topic = "A Place to Work",
                    m_label = "This is the label",
                    m_isMunin = true,
                    m_text = "Your artisan has set to work. \n\n" +
                    "You can see an artisan at their worksite: they move to their chest, take what they need, and do the work. A Wood Burner feeds " +
                    "a Kiln, a Furnace Operator feeds a Smelter, a Farmer tends a plot, and a Lamplighter fills torches and fires. Give each artisan " + 
					"a chest stocked with what they need, and they will happily ply their trade."
                },
                new Tutorial.TutorialText()
                {
                    m_name = FirstBattle,
                    m_topic = "Your Hirdmandr Fight",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "Your hirdmandr has entered battle! \n\n" +
                    "A hirdmandr with a Warrior role will defend you and your halls. They fight to protect the hirdmandr around them, and if " +
                    "they are overmatched they will fall back to safety to recover. A Thegn guards your stronghold, and a Himthiki will follow " +
                    "you into battle. Your artisans will defend themselves when attacked, all Hirdmandr bleed viking blood! Lead them well, Jarl."
                },
                new Tutorial.TutorialText()
                {
                    m_name = FirstFollow,
                    m_topic = "A Hirdmandr at Your Side",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "A hirdmandr now follows you. \n\n" +
                    "Command a hirdmandr to follow you, and they will travel at your side, ready for whatever lies ahead. You can send them or recall " +
                    "them at will with [<color=yellow><b>E</b></color>] or from the hirdmandr panel. A Himthiki makes a fine companion on the road! " + 
					"Their Jarl is blessed by Oden, giving them the power to use Portals. Should a hirdmandr die, a Jarl may carry their soul to an " +
					"Altar to Hel, and treat with the gods for their rebirth in the 10th world."
                },
                new Tutorial.TutorialText()
                {
                    m_name = FirstGuardHere,
                    m_topic = "A Thegn on Watch",
                    m_label = "This is the label",
                    m_isMunin = false,
                    m_text = "You have set a guard post. \n\n" +
                    "A Thegn will hold the spot you choose and keep watch over it, guarding the hirdmandr around it. Set a guard post where you " +
                    "need a steady hand, and your Thegn will stand watch there during their shift. Thegn are young and eager. Place their post " +
                    "where their courage is needed most. Tell your Thegn whether to guard during the day, the night, or both, and ensure your stronghold " + 
					"is never undefended."
                },
                new Tutorial.TutorialText()
                {
                    m_name = FirstSkill,
                    m_topic = "A New Skill",
                    m_label = "This is the label",
                    m_isMunin = true,
                    m_text = "You have given your hirdmandr a skill. \n\n" +
                    "A skill is a trade your hirdmandr can learn: a Mender mends, a Lamplighter refuels, a Wood Burner feeds the kiln, and a " +
                    "Farmer tends the fields. Enable a skill to set that trade loose, and your hirdmandr will take it up. As their skill improves, " + 
					"they work faster and are more efficient with their materials. A hirdmandr can learn " +
                    "many skills at once, but do best the skills they were born to do."
                },
                new Tutorial.TutorialText()
                {
                    m_name = WelcomeFailed,
                    m_topic = "Not Yet a Home",
                    m_label = "This is the label",
                    m_isMunin = true,
                    m_text = "You cannot welcome your hirdmandr home here. \n\n" +
                    "A Jarl welcomes a hirdmandr to their home only within reach of both a <color=yellow><b>Workbench</b></color> and a " +
                    "<color=yellow><b>Fire</b></color>. Lead your hirdmandr to a proper stronghold or camp, and welcome them there. They cannot " +
                    "truly come home until their new home is ready to hold them."
                },
                new Tutorial.TutorialText()
                {
                    m_name = BedTooFar,
                    m_topic = "No Bed in Reach",
                    m_label = "This is the label",
                    m_isMunin = true,
                    m_text = "Your hirdmandr could not reach a bed to rest. \n\n" +
                    "A hirdmandr can only rest in a bed it can reach. If no bed is within reach of where they are, they will stay awake and " +
                    "grow weary. Keep a bed close to where your hirdmandr live and work, and they will find rest when they need it."
                },
                new Tutorial.TutorialText()
                {
                    m_name = HimthikiDeath,
                    m_topic = "A Himthiki Falls",
                    m_label = "This is the label",
                    m_isMunin = true,
                    m_text = "A Himthiki has fallen!\n\n" +
                    "Oden and Hel, Moder and Fader, the Forsaken. These all have some dominion over souls in the drifting 10th world. " + 
					"The trust and faith of your Hirdmandr has given you a sliver of this power. Your Himthiki has died but their memory yet remains " + 
					"in the soul that is left behind! Take it. Keep it safe until you can build an Altar to Hella. I've heard stories of a Jarl whose " + 
					"offerings returned the lost..."
                },
            };
        }

        // Register every HMTutorial definition with the game's Tutorial instance. Harmony Postfix on
        // Tutorial.Awake so the base Awake() (which builds the vanilla m_texts) runs first, then we
        // append ours. Dedup by m_name guards against a second Awake double-adding. Applied via the
        // mod's harmony.PatchAll() (attribute scan), which picks up this nested class.
        [HarmonyPatch(typeof(Tutorial))]
        [HarmonyPatch("Awake")]
        internal static class Register
        {
            static void Postfix(ref Tutorial __instance)
            {
                foreach (var t in HMTutorial.Definitions())
                {
                    if (!__instance.m_texts.Any(x => x.m_name == t.m_name))
                    {
                        __instance.m_texts.Add(t);
                    }
                }
            }
        }
    }
}
