using Hirdmandr;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Hirdmandr
{
    public class HirdmandrGUI : MonoBehaviour
    {
        GameObject GUIHirdmandr;
        NPCPlayerClone m_humanoid;
        HirdmandrNPC m_hirdmandrnpc;
        GameObject g_name;
        GameObject g_jarl;
        GameObject g_talk;
        GameObject g_skillnames;
        GameObject g_skillvalues;
        GameObject g_role_t_header;
        GameObject g_art_ch_woodburner;
        GameObject g_art_t_woodburner;
        GameObject g_art_ch_furnaceoper;
        GameObject g_art_t_furnaceoper;
        GameObject g_art_ch_farmer;
        GameObject g_art_t_farmer;
        GameObject g_art_ch_lamplighter;
        GameObject g_art_t_lamplighter;
        GameObject g_art_ch_mender;
        GameObject g_art_t_mender;
        GameObject g_role_ch_artisan;
        GameObject g_role_ch_warrior;
        ToggleGroup g_role_tg;
        ToggleGroup g_job_tg;
        GameObject g_war_ch_thegn;
        GameObject g_war_t_thegn;
        // Thegn "guard here" controls (warrior-page only): the button anchors the guard post to the
        // NPC's current position; Day/Night are the two independent shift flags.
        GameObject g_b_guard;
        GameObject g_war_ch_thegnDay;
        GameObject g_war_t_thegnDay;
        GameObject g_war_ch_thegnNight;
        GameObject g_war_t_thegnNight;
        GameObject g_war_t_guardHeader;
        GameObject g_war_ch_himthiki;
        GameObject g_war_t_himthiki;
        GameObject g_b_follow;
        GameObject g_war_ch_gatherer;
        GameObject g_war_t_gatherer;
        GameObject g_war_t_styleHeader;
        ToggleGroup g_style_tg;
        GameObject g_war_ch_styleDefense;
        GameObject g_war_t_styleDefense;
        GameObject g_war_ch_styleOffense;
        GameObject g_war_t_styleOffense;
        GameObject g_war_t_rangeHeader;
        GameObject g_war_ch_rangeClose;
        GameObject g_war_t_rangeClose;
        GameObject g_war_ch_rangeMid;
        GameObject g_war_t_rangeMid;
        GameObject g_war_ch_rangeFar;
        GameObject g_war_t_rangeFar;
        ToggleGroup g_range_tg;

        GameObject[] all_arts = new GameObject[0];
        GameObject[] all_wars = new GameObject[0];

        void Update()
        {
            if (GUIHirdmandr)
            {
                if (
                    GUIHirdmandr.activeSelf &&
                        (
                            ZInput.GetButtonDown("Inventory") ||
                            ZInput.GetButtonDown("JoyButtonB") ||
                            ZInput.GetButtonDown("JoyButtonY") ||
                            // Input.GetKeyDown(KeyCode.Escape) ||
                            ZInput.GetButtonDown("Use")
                        )
                    )
                {
                    ZInput.ResetButtonStatus("Inventory");
                    ZInput.ResetButtonStatus("JoyButtonB");
                    ZInput.ResetButtonStatus("JoyButtonY");
                    ZInput.ResetButtonStatus("Use");
                    TogglePanel();
                }
            }
        }

        // The panel is a child of GUIManager.CustomGUIFront, not the NPC, so it would outlive the
        // NPC's death/despawn and leak under the UI layer. Tear it down with the NPC (gui_research.md §4.5).
        void OnDestroy()
        {
            if (GUIHirdmandr)
            {
                if (GUIHirdmandr.activeSelf)
                {
                    // Panel was open when the NPC died: close the player-facing inventory + unblock input.
                    // Don't touch the dying NPC's own m_container/m_visequip here.
                    InventoryGui.instance.Hide();
                    GUIManager.BlockInput(false);
                }
                Destroy(GUIHirdmandr);
            }
        }

        public void TogglePanel()
        {
            Jotunn.Logger.LogInfo("TogglePanel happened in Hirdmandr GUI");
            // Create the panel if it does not exist
            if (!GUIHirdmandr)
            {
                if (GUIManager.Instance == null)
                {
                    Jotunn.Logger.LogError("GUIManager instance is null");
                    return;
                }

                if (!GUIManager.CustomGUIFront)
                {
                    Jotunn.Logger.LogError("GUIManager CustomGUI is null");
                    return;
                }

                m_humanoid = GetComponent<NPCPlayerClone>();
                m_hirdmandrnpc = GetComponent<HirdmandrNPC>();

                var skillnames = "";
                var skillvalues = "";
                var first_line = true;
                foreach (HMSkills.SkillData each_skill in m_hirdmandrnpc.m_skills.m_hmSkills)
                {
                    if (!first_line)
                    {
                        skillnames += "\n";
                        skillvalues += "\n";
                    }
                    skillnames += each_skill.m_readable;
                    skillvalues += (int)each_skill.m_value;

                    first_line = false;
                }


                // Create the panel object
                GUIHirdmandr = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0),
                    width: 1000,
                    height: 640,
                    draggable: true);
                GUIHirdmandr.SetActive(false);

                // Add the Jötunn draggable Component to the panel
                // Note: This is normally automatically added when using CreateWoodpanel()
                // GUIHirdmandr.AddComponent<DragWindowCntrl>();
                // DragWindowCntrl.ApplyDragWindowCntrl(GUIHirdmandr);

                // Create the text object
                g_name = GUIManager.Instance.CreateText(
                    text: m_humanoid.m_name,
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.25f, 1f),
                    anchorMax: new Vector2(0.25f, 1f),
                    position: new Vector2(0f, -60f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 450f,
                    height: 40f,
                    addContentSizeFitter: false);

                // Jarl line: "Jarl: <display name>" right under the NPC name. Rich text - the
                // label in the header orange, the name white, both at this size. Hidden while
                // the name is empty (welcome-only capture: blank until welcomed, and a legacy
                // save never had one stored).
                g_jarl = GUIManager.Instance.CreateText(
                    text: "",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.25f, 1f),
                    anchorMax: new Vector2(0.25f, 1f),
                    position: new Vector2(0f, -105f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 450f,
                    height: 30f,
                    addContentSizeFitter: false);
                RefreshJarlLine();

                // The "Say" line: a random line from the campfire murmur (peek - a queued
                // moment is NOT popped, the fire still says it later); idle pool when nothing
                // is queued.
                g_talk = GUIManager.Instance.CreateText(
                    text: m_hirdmandrnpc.m_socialize.PeekMurmur() ?? "",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.25f, 1f),
                    anchorMax: new Vector2(0.25f, 1f),
                    position: new Vector2(0f, -170f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 18,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 450f,
                    height: 80f,
                    addContentSizeFitter: false);

                GUIManager.Instance.CreateText(
                    text: "Skills",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.375f, 1f),
                    anchorMax: new Vector2(0.375f, 1f),
                    position: new Vector2(0f, -290f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 150f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_skillnames = GUIManager.Instance.CreateText(
                    text: skillnames,
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.375f, 1f),
                    anchorMax: new Vector2(0.375f, 1f),
                    position: new Vector2(0f, -440f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 150f,
                    height: 220f,
                    addContentSizeFitter: false);

                g_skillvalues = GUIManager.Instance.CreateText(
                    text: skillvalues,
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.475f, 1f),
                    anchorMax: new Vector2(0.475f, 1f),
                    position: new Vector2(0f, -440f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 25f,
                    height: 220f,
                    addContentSizeFitter: false);

                // ------------------------------------------------------
                // Column 2
                // ------------------------------------------------------

                g_role_t_header = GUIManager.Instance.CreateText(
                    text: "Role: ",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.52f, 1f),
                    anchorMax: new Vector2(0.52f, 1f),
                    position: new Vector2(0f, -60f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 70f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_role_ch_artisan = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_role_ch_artisanRect = g_role_ch_artisan.GetComponent<RectTransform>();
                g_role_ch_artisanRect.anchorMin = new Vector2(0.6f, 1f);
                g_role_ch_artisanRect.anchorMax = new Vector2(0.6f, 1f);
                g_role_ch_artisanRect.anchoredPosition = new Vector2(0f, -60);
                g_role_ch_artisanRect.ForceUpdateRectTransforms();

                var g_role_ch_artisanComp = g_role_ch_artisan.GetComponent<Toggle>();
                g_role_ch_artisanComp.isOn = m_hirdmandrnpc.m_roleArtisan;
                g_role_ch_artisanComp.onValueChanged.AddListener(RoleChanged);

                GUIManager.Instance.CreateText(
                    text: "Artisan",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.70f, 1f),
                    anchorMax: new Vector2(0.70f, 1f),
                    position: new Vector2(0f, -60f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 100f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_role_ch_warrior = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_role_ch_warriorRect = g_role_ch_warrior.GetComponent<RectTransform>();
                g_role_ch_warriorRect.anchorMin = new Vector2(0.8f, 1f);
                g_role_ch_warriorRect.anchorMax = new Vector2(0.8f, 1f);
                g_role_ch_warriorRect.anchoredPosition = new Vector2(0f, -60);
                g_role_ch_warriorRect.ForceUpdateRectTransforms();

                var g_role_ch_warriorComp = g_role_ch_warrior.GetComponent<Toggle>();
                g_role_ch_warriorComp.isOn = m_hirdmandrnpc.m_roleWarrior;
                g_role_ch_warriorComp.onValueChanged.AddListener(RoleChanged);

                GUIManager.Instance.CreateText(
                    text: "Warrior",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.90f, 1f),
                    anchorMax: new Vector2(0.90f, 1f),
                    position: new Vector2(0f, -60f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 100f,
                    height: 40f,
                    addContentSizeFitter: false);

                // ------------------------------------------------------
                // Column 2 - During Artisan
                // ------------------------------------------------------

                g_art_ch_woodburner = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_woodburnerRect = g_art_ch_woodburner.GetComponent<RectTransform>();
                g_art_ch_woodburnerRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_woodburnerRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_woodburnerRect.anchoredPosition = new Vector2(0f, -200);
                g_art_ch_woodburnerRect.ForceUpdateRectTransforms();

                var g_art_ch_woodburnerComp = g_art_ch_woodburner.GetComponent<Toggle>();
                g_art_ch_woodburnerComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("woodburner");
                g_art_ch_woodburnerComp.onValueChanged.AddListener(WoodToggle);

                g_art_t_woodburner = GUIManager.Instance.CreateText(
                    text: "Wood Burner",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.72f, 1f),
                    anchorMax: new Vector2(0.72f, 1f),
                    position: new Vector2(0f, -200f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_art_ch_furnaceoper = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_furnaceoperRect = g_art_ch_furnaceoper.GetComponent<RectTransform>();
                g_art_ch_furnaceoperRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_furnaceoperRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_furnaceoperRect.anchoredPosition = new Vector2(0f, -250);

                var g_art_ch_furnaceoperComp = g_art_ch_furnaceoper.GetComponent<Toggle>();
                g_art_ch_furnaceoperComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("furnaceoperator");
                g_art_ch_furnaceoperComp.onValueChanged.AddListener(FurnaceToggle);

                g_art_t_furnaceoper = GUIManager.Instance.CreateText(
                    text: "Furnace Operator",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.72f, 1f),
                    anchorMax: new Vector2(0.72f, 1f),
                    position: new Vector2(0f, -250f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_art_ch_farmer = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_farmerRect = g_art_ch_farmer.GetComponent<RectTransform>();
                g_art_ch_farmerRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_farmerRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_farmerRect.anchoredPosition = new Vector2(0f, -300);

                var g_art_ch_farmerComp = g_art_ch_farmer.GetComponent<Toggle>();
                g_art_ch_farmerComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("farmer");
                g_art_ch_farmerComp.onValueChanged.AddListener(FarmerToggle);

                g_art_t_farmer = GUIManager.Instance.CreateText(
                    text: "Farmer",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.72f, 1f),
                    anchorMax: new Vector2(0.72f, 1f),
                    position: new Vector2(0f, -300f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_art_ch_lamplighter = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_lamplighterRect = g_art_ch_lamplighter.GetComponent<RectTransform>();
                g_art_ch_lamplighterRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_lamplighterRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_lamplighterRect.anchoredPosition = new Vector2(0f, -150);

                var g_art_ch_lamplighterComp = g_art_ch_lamplighter.GetComponent<Toggle>();
                g_art_ch_lamplighterComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("lamplighter");
                g_art_ch_lamplighterComp.onValueChanged.AddListener(LamplighterToggle);

                g_art_t_lamplighter = GUIManager.Instance.CreateText(
                    text: "Lamplighter",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.72f, 1f),
                    anchorMax: new Vector2(0.72f, 1f),
                    position: new Vector2(0f, -150f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_art_ch_mender = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_menderRect = g_art_ch_mender.GetComponent<RectTransform>();
                g_art_ch_menderRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_menderRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_menderRect.anchoredPosition = new Vector2(0f, -100);

                var g_art_ch_menderComp = g_art_ch_mender.GetComponent<Toggle>();
                g_art_ch_menderComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("mender");
                g_art_ch_menderComp.onValueChanged.AddListener(MenderToggle);

                g_art_t_mender = GUIManager.Instance.CreateText(
                    text: "Mender",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.72f, 1f),
                    anchorMax: new Vector2(0.72f, 1f),
                    position: new Vector2(0f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                all_arts = new GameObject[10] {
                    g_art_ch_woodburner,
                    g_art_t_woodburner,
                    g_art_ch_furnaceoper,
                    g_art_t_furnaceoper,
                    g_art_ch_farmer,
                    g_art_t_farmer,
                    g_art_ch_lamplighter,
                    g_art_t_lamplighter,
                    g_art_ch_mender,
                    g_art_t_mender
                };

                // ------------------------------------------------------
                // Column 2 - During Warrior
                // ------------------------------------------------------

                g_war_ch_thegn = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_thegnRect = g_war_ch_thegn.GetComponent<RectTransform>();
                g_war_ch_thegnRect.anchorMin = new Vector2(0.55f, 1f);
                g_war_ch_thegnRect.anchorMax = new Vector2(0.55f, 1f);
                g_war_ch_thegnRect.anchoredPosition = new Vector2(0f, -100);
                g_war_ch_thegnRect.ForceUpdateRectTransforms();

                var g_war_ch_thegnComp = g_war_ch_thegn.GetComponent<Toggle>();
                g_war_ch_thegnComp.isOn = m_hirdmandrnpc.m_jobThegn;
                g_war_ch_thegnComp.onValueChanged.AddListener(CombatJobThegen);

                g_war_t_thegn = GUIManager.Instance.CreateText(
                    text: "Thegn (Town Guard)",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.72f, 1f),
                    anchorMax: new Vector2(0.72f, 1f),
                    position: new Vector2(0f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_war_ch_himthiki = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_himthikiRect = g_war_ch_himthiki.GetComponent<RectTransform>();
                g_war_ch_himthikiRect.anchorMin = new Vector2(0.55f, 1f);
                g_war_ch_himthikiRect.anchorMax = new Vector2(0.55f, 1f);
                g_war_ch_himthikiRect.anchoredPosition = new Vector2(0f, -150);

                var g_war_ch_himthikiComp = g_war_ch_himthiki.GetComponent<Toggle>();
                g_war_ch_himthikiComp.isOn = m_hirdmandrnpc.m_jobHimthiki;
                g_war_ch_himthikiComp.onValueChanged.AddListener(CombatJobHimthiki);

                g_war_t_himthiki = GUIManager.Instance.CreateText(
                    text: "Himthiki (Follower)",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.72f, 1f),
                    anchorMax: new Vector2(0.72f, 1f),
                    position: new Vector2(0f, -150f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                // Create the button object
                // Role-neutral follow toggle (D4): header row, to the left of the Artisan role
                // radio (anchor 0.4, same height as the radios). Not in all_arts/all_wars so it
                // is visible on every role page. Label is per-player (IsFollowing(m_user)) and
                // refreshed on open.
                g_b_follow = GUIManager.Instance.CreateButton(
                    text: m_hirdmandrnpc.IsFollowing(m_hirdmandrnpc.m_user) ? "Stop Following" : "Follow",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.4f, 1f),
                    anchorMax: new Vector2(0.4f, 1f),
                    position: new Vector2(0f, -50f),
                    width: 120f,
                    height: 40f);
                g_b_follow.SetActive(true);

                Button g_b_followComp = g_b_follow.GetComponent<Button>();
                g_b_followComp.onClick.AddListener(m_hirdmandrnpc.FollowToggle);
                
                g_war_ch_gatherer = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_gathererRect = g_war_ch_gatherer.GetComponent<RectTransform>();
                g_war_ch_gathererRect.anchorMin = new Vector2(0.55f, 1f);
                g_war_ch_gathererRect.anchorMax = new Vector2(0.55f, 1f);
                g_war_ch_gathererRect.anchoredPosition = new Vector2(0f, -200);

                var g_war_ch_gathererComp = g_war_ch_gatherer.GetComponent<Toggle>();
                g_war_ch_gathererComp.isOn = m_hirdmandrnpc.m_jobGatherer;
                g_war_ch_gathererComp.onValueChanged.AddListener(ToggleGatherer);

                g_war_t_gatherer = GUIManager.Instance.CreateText(
                    text: "Enable Gatherer Labor",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.72f, 1f),
                    anchorMax: new Vector2(0.72f, 1f),
                    position: new Vector2(0f, -200f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_war_t_styleHeader = GUIManager.Instance.CreateText(
                    text: "Combat Style",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.65f, 1f),
                    anchorMax: new Vector2(0.65f, 1f),
                    position: new Vector2(0f, -250f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_war_ch_styleDefense = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_styleDefenseRect = g_war_ch_styleDefense.GetComponent<RectTransform>();
                g_war_ch_styleDefenseRect.anchorMin = new Vector2(0.55f, 1f);
                g_war_ch_styleDefenseRect.anchorMax = new Vector2(0.55f, 1f);
                g_war_ch_styleDefenseRect.anchoredPosition = new Vector2(0f, -300);

                var g_war_ch_styleDefenseComp = g_war_ch_styleDefense.GetComponent<Toggle>();
                g_war_ch_styleDefenseComp.isOn = m_hirdmandrnpc.m_fightingStyleDefense;
                g_war_ch_styleDefenseComp.onValueChanged.AddListener(FightingStyle);

                g_war_t_styleDefense = GUIManager.Instance.CreateText(
                    text: "Defensive",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.65f, 1f),
                    anchorMax: new Vector2(0.65f, 1f),
                    position: new Vector2(0f, -300),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 150f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_war_ch_styleOffense = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_styleOffenseRect = g_war_ch_styleOffense.GetComponent<RectTransform>();
                g_war_ch_styleOffenseRect.anchorMin = new Vector2(0.80f, 1f);
                g_war_ch_styleOffenseRect.anchorMax = new Vector2(0.80f, 1f);
                g_war_ch_styleOffenseRect.anchoredPosition = new Vector2(0f, -300);

                var g_war_ch_styleOffenseComp = g_war_ch_styleOffense.GetComponent<Toggle>();
                g_war_ch_styleOffenseComp.isOn = m_hirdmandrnpc.m_fightingStyleOffense;
                g_war_ch_styleOffenseComp.onValueChanged.AddListener(FightingStyle);

                g_war_t_styleOffense = GUIManager.Instance.CreateText(
                    text: "Offensive",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.90f, 1f),
                    anchorMax: new Vector2(0.90f, 1f),
                    position: new Vector2(0f, -300),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 150f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_war_t_rangeHeader = GUIManager.Instance.CreateText(
                    text: "Engagement Range",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.65f, 1f),
                    anchorMax: new Vector2(0.65f, 1f),
                    position: new Vector2(0f, -350f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);
                
                g_war_ch_rangeClose = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_rangeCloseRect = g_war_ch_rangeClose.GetComponent<RectTransform>();
                g_war_ch_rangeCloseRect.anchorMin = new Vector2(0.55f, 1f);
                g_war_ch_rangeCloseRect.anchorMax = new Vector2(0.55f, 1f);
                g_war_ch_rangeCloseRect.anchoredPosition = new Vector2(0f, -400);

                var g_war_ch_rangeCloseComp = g_war_ch_rangeClose.GetComponent<Toggle>();
                g_war_ch_rangeCloseComp.isOn = m_hirdmandrnpc.m_fightingRangeClose;
                g_war_ch_rangeCloseComp.onValueChanged.AddListener(FightingRange);

                g_war_t_rangeClose = GUIManager.Instance.CreateText(
                    text: "Close",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.62f, 1f),
                    anchorMax: new Vector2(0.62f, 1f),
                    position: new Vector2(0f, -400),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 80f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_war_ch_rangeMid = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_rangeMidRect = g_war_ch_rangeMid.GetComponent<RectTransform>();
                g_war_ch_rangeMidRect.anchorMin = new Vector2(0.70f, 1f);
                g_war_ch_rangeMidRect.anchorMax = new Vector2(0.70f, 1f);
                g_war_ch_rangeMidRect.anchoredPosition = new Vector2(0f, -400);

                var g_war_ch_rangeMidComp = g_war_ch_rangeMid.GetComponent<Toggle>();
                g_war_ch_rangeMidComp.isOn = m_hirdmandrnpc.m_fightingRangeMid;
                g_war_ch_rangeMidComp.onValueChanged.AddListener(FightingRange);

                g_war_t_rangeMid = GUIManager.Instance.CreateText(
                    text: "Mid",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.77f, 1f),
                    anchorMax: new Vector2(0.77f, 1f),
                    position: new Vector2(0f, -400),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 80f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_war_ch_rangeFar = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_rangeFarRect = g_war_ch_rangeFar.GetComponent<RectTransform>();
                g_war_ch_rangeFarRect.anchorMin = new Vector2(0.85f, 1f);
                g_war_ch_rangeFarRect.anchorMax = new Vector2(0.85f, 1f);
                g_war_ch_rangeFarRect.anchoredPosition = new Vector2(0f, -400);

                var g_war_ch_rangeFarComp = g_war_ch_rangeFar.GetComponent<Toggle>();
                g_war_ch_rangeFarComp.isOn = m_hirdmandrnpc.m_fightingRangeFar;
                g_war_ch_rangeFarComp.onValueChanged.AddListener(FightingRange);

                g_war_t_rangeFar = GUIManager.Instance.CreateText(
                    text: "Far",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.92f, 1f),
                    anchorMax: new Vector2(0.92f, 1f),
                    position: new Vector2(0f, -400),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 80f,
                    height: 40f,
                    addContentSizeFitter: false);

                // --- Thegn "guard here" controls (warrior-page only; below the range row) ---
                g_war_t_guardHeader = GUIManager.Instance.CreateText(
                    text: "Guard Duty",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.65f, 1f),
                    anchorMax: new Vector2(0.65f, 1f),
                    position: new Vector2(0f, -450f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 40f,
                    addContentSizeFitter: false);

                // "Guard here" anchors the guard post to the NPC's current position and stops
                // following; the schedule then routes the Thegn to patrol (guard) during its window.
                g_b_guard = GUIManager.Instance.CreateButton(
                    text: "Guard here",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.55f, 1f),
                    anchorMax: new Vector2(0.55f, 1f),
                    position: new Vector2(0f, -500f),
                    width: 130f,
                    height: 40f);
                g_b_guard.SetActive(true);
                g_b_guard.GetComponent<Button>().onClick.AddListener(m_hirdmandrnpc.GuardHere);

                g_war_ch_thegnDay = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);
                var g_war_ch_thegnDayRect = g_war_ch_thegnDay.GetComponent<RectTransform>();
                g_war_ch_thegnDayRect.anchorMin = new Vector2(0.55f, 1f);
                g_war_ch_thegnDayRect.anchorMax = new Vector2(0.55f, 1f);
                g_war_ch_thegnDayRect.anchoredPosition = new Vector2(0f, -550);
                var g_war_ch_thegnDayComp = g_war_ch_thegnDay.GetComponent<Toggle>();
                g_war_ch_thegnDayComp.isOn = m_hirdmandrnpc.m_thegnDayshift;
                g_war_ch_thegnDayComp.onValueChanged.AddListener(ThegnShiftDay);

                g_war_t_thegnDay = GUIManager.Instance.CreateText(
                    text: "Day",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.63f, 1f),
                    anchorMax: new Vector2(0.63f, 1f),
                    position: new Vector2(0f, -550f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 80f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_war_ch_thegnNight = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);
                var g_war_ch_thegnNightRect = g_war_ch_thegnNight.GetComponent<RectTransform>();
                g_war_ch_thegnNightRect.anchorMin = new Vector2(0.78f, 1f);
                g_war_ch_thegnNightRect.anchorMax = new Vector2(0.78f, 1f);
                g_war_ch_thegnNightRect.anchoredPosition = new Vector2(0f, -550);
                var g_war_ch_thegnNightComp = g_war_ch_thegnNight.GetComponent<Toggle>();
                g_war_ch_thegnNightComp.isOn = m_hirdmandrnpc.m_thegnNightshift;
                g_war_ch_thegnNightComp.onValueChanged.AddListener(ThegnShiftNight);

                g_war_t_thegnNight = GUIManager.Instance.CreateText(
                    text: "Night",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.86f, 1f),
                    anchorMax: new Vector2(0.86f, 1f),
                    position: new Vector2(0f, -550f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 24,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 80f,
                    height: 40f,
                    addContentSizeFitter: false);

                all_wars = new GameObject[24] {
                    g_war_ch_thegn,
                    g_war_t_thegn,
                    g_b_guard,
                    g_war_ch_thegnDay,
                    g_war_t_thegnDay,
                    g_war_ch_thegnNight,
                    g_war_t_thegnNight,
                    g_war_t_guardHeader,
                    g_war_ch_himthiki,
                    g_war_t_himthiki,
                    g_war_ch_gatherer,
                    g_war_t_gatherer,
                    g_war_t_styleHeader,
                    g_war_ch_styleDefense,
                    g_war_t_styleDefense,
                    g_war_ch_styleOffense,
                    g_war_t_styleOffense,
                    g_war_t_rangeHeader,
                    
                    g_war_ch_rangeClose,
                    g_war_t_rangeClose,
                    g_war_ch_rangeMid,
                    g_war_t_rangeMid,
                    g_war_ch_rangeFar,
                    g_war_t_rangeFar
                };

                g_role_tg = g_role_t_header.AddComponent<ToggleGroup>();
                g_role_ch_artisanComp.group = g_role_tg;
                g_role_ch_warriorComp.group = g_role_tg;
                if (m_hirdmandrnpc.m_roleArtisan)
                {
                    g_role_ch_artisanComp.isOn = true;
                }
                if (m_hirdmandrnpc.m_roleWarrior)
                {
                    g_role_ch_warriorComp.isOn = true;
                }
                if (!g_role_ch_artisanComp.isOn && !g_role_ch_warriorComp.isOn)
                {
                    g_role_ch_artisanComp.isOn = true;
                    m_hirdmandrnpc.m_roleArtisan = true;
                }
                g_role_tg.allowSwitchOff = false;

                g_job_tg = g_war_t_thegn.AddComponent<ToggleGroup>();
                g_war_ch_thegnComp.group = g_job_tg;
                g_war_ch_himthikiComp.group = g_job_tg;
                if (m_hirdmandrnpc.m_roleWarrior)
                {
                    if (m_hirdmandrnpc.m_jobThegn && m_hirdmandrnpc.m_jobHimthiki)
                    {
                        m_hirdmandrnpc.InvokeSetting("jobThegn", true);
                    }
                    else if (!m_hirdmandrnpc.m_jobThegn && !m_hirdmandrnpc.m_jobHimthiki)
                    {
                        m_hirdmandrnpc.InvokeSetting("jobThegn", true);
                    }
                    else if (m_hirdmandrnpc.m_jobHimthiki)
                    {
                        m_hirdmandrnpc.InvokeSetting("jobHimthiki", true);
                    }
                }
                g_war_ch_thegnComp.SetIsOnWithoutNotify(m_hirdmandrnpc.m_jobThegn);
                g_war_ch_himthikiComp.SetIsOnWithoutNotify(m_hirdmandrnpc.m_jobHimthiki);
                g_job_tg.allowSwitchOff = false;

                // Initial page visibility: hide both, show the active role
                foreach (GameObject go in all_arts) go.SetActive(false);
                foreach (GameObject go in all_wars) go.SetActive(false);
                if (m_hirdmandrnpc.m_roleArtisan)
                {
                    foreach (GameObject go in all_arts) go.SetActive(true);
                }
                if (m_hirdmandrnpc.m_roleWarrior)
                {
                    foreach (GameObject go in all_wars) go.SetActive(true);
                }

                g_style_tg = g_war_t_styleHeader.AddComponent<ToggleGroup>();
                g_war_ch_styleDefenseComp.group = g_style_tg;
                g_war_ch_styleOffenseComp.group = g_style_tg;
                if (m_hirdmandrnpc.m_fightingStyleDefense)
                {
                    g_war_ch_styleDefenseComp.isOn = true;
                }
                if (m_hirdmandrnpc.m_fightingStyleOffense)
                {
                    g_war_ch_styleOffenseComp.isOn = true;
                }
                if (!g_war_ch_styleDefenseComp.isOn && !g_war_ch_styleOffenseComp.isOn)
                {
                    g_war_ch_styleDefenseComp.isOn = true;
                    m_hirdmandrnpc.m_fightingStyleDefense = true;
                }
                g_style_tg.allowSwitchOff = false;
                
                g_range_tg = g_war_t_rangeHeader.AddComponent<ToggleGroup>();
                g_war_ch_rangeCloseComp.group = g_range_tg;
                g_war_ch_rangeMidComp.group = g_range_tg;
                g_war_ch_rangeFarComp.group = g_range_tg;
                if (m_hirdmandrnpc.m_fightingRangeClose)
                {
                    g_war_ch_rangeCloseComp.isOn = true;
                }
                if (m_hirdmandrnpc.m_fightingRangeMid)
                {
                    g_war_ch_rangeMidComp.isOn = true;
                }
                if (m_hirdmandrnpc.m_fightingRangeFar)
                {
                    g_war_ch_rangeFarComp.isOn = true;
                }
                if (!g_war_ch_rangeCloseComp.isOn && !g_war_ch_rangeMidComp.isOn && !g_war_ch_rangeFarComp.isOn)
                {
                    g_war_ch_rangeCloseComp.isOn = true;
                }
                g_range_tg.allowSwitchOff = false;

            }

            // Switch the current state — derive from activeSelf (one source of truth; gui_research.md §4.4).
            bool state = !GUIHirdmandr.activeSelf;

                if (state)
            {
                // Refresh the name too (it was set once at create-time): picks up a future rename feature.
                g_name.GetComponent<Text>().text = m_humanoid.m_name;
                // A fresh murmur line each open (peek - the queued moments are not popped).
                g_talk.GetComponent<Text>().text = m_hirdmandrnpc.m_socialize.PeekMurmur() ?? "";
                RefreshJarlLine();
                g_b_follow.GetComponentInChildren<Text>().text = m_hirdmandrnpc.IsFollowing(m_hirdmandrnpc.m_user) ? "Stop Following" : "Follow";
                RefreshGuardControls();
            }
            else
            {
                // Persist the full skill state (floats + enable bools) on panel close, the same
                // way ZDOSaveGeneral persists role/job fields. Toggle-time persistence already
                // happens in ApplySettingLocal; this is the belt-and-braces snapshot at the
                // point the player is done configuring.
                m_hirdmandrnpc.m_skills.SaveSkills();
                m_hirdmandrnpc.ZDOSaveGeneral();
            }

            update_skills();
            // Set the active state of the panel
            GUIHirdmandr.SetActive(state);
            m_hirdmandrnpc.OpenInventory(state);
            m_hirdmandrnpc.EquipBest();

            // Toggle input for the player and camera while displaying the GUI
            GUIManager.BlockInput(state);

        }

        private void RoleChanged(bool toggle_value)
        {
            if (toggle_value) {
                m_hirdmandrnpc.InvokeSetting("roleArtisan", g_role_ch_artisan.GetComponent<Toggle>().isOn);
                m_hirdmandrnpc.InvokeSetting("roleWarrior", g_role_ch_warrior.GetComponent<Toggle>().isOn);

                if (m_hirdmandrnpc.m_roleArtisan)
                {
                    Jotunn.Logger.LogInfo("Role changed to Artisan");
                    foreach (GameObject go in all_arts)
                    {
                        go.SetActive(true);
                    }
                    foreach (GameObject go in all_wars)
                    {
                        go.SetActive(false);
                    }
                    var thisPlayer = m_hirdmandrnpc.m_user?.GetComponent<Player>();
                    if (thisPlayer == Player.m_localPlayer) 
                    {
                        HMTutorial.Show(HMTutorial.Artisan);
                    }
                }
                if (m_hirdmandrnpc.m_roleWarrior)
                {
                    Jotunn.Logger.LogInfo("Role changed to Warrior");
                    if (m_hirdmandrnpc.m_jobThegn && m_hirdmandrnpc.m_jobHimthiki)
                    {
                        m_hirdmandrnpc.InvokeSetting("jobThegn", true);
                    }
                    else if (!m_hirdmandrnpc.m_jobThegn && !m_hirdmandrnpc.m_jobHimthiki)
                    {
                        m_hirdmandrnpc.InvokeSetting("jobThegn", true);
                    }
                    foreach (GameObject go in all_arts)
                    {
                        go.SetActive(false);
                    }
                    foreach (GameObject go in all_wars)
                    {
                        go.SetActive(true);
                    }
                    g_war_ch_thegn.GetComponent<Toggle>().SetIsOnWithoutNotify(m_hirdmandrnpc.m_jobThegn);
                    g_war_ch_himthiki.GetComponent<Toggle>().SetIsOnWithoutNotify(m_hirdmandrnpc.m_jobHimthiki);
                }
            }
        }

        public void WoodToggle(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("skill:woodburner", toggle_value);
        }

        public void FurnaceToggle(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("skill:furnaceoperator", toggle_value);
        }

        public void FarmerToggle(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("skill:farmer", toggle_value);
        }

        public void LamplighterToggle(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("skill:lamplighter", toggle_value);
        }

        public void MenderToggle(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("skill:mender", toggle_value);
        }

        public void CombatJobThegen(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("jobThegn", toggle_value);
            SyncCombatTogglesFromFields();
            RefreshGuardControls();

            if (m_hirdmandrnpc.m_jobThegn)
            {
                Jotunn.Logger.LogInfo("Warrior job Thegn enabled");
                var thisPlayer = m_hirdmandrnpc.m_user?.GetComponent<Player>();
                if (thisPlayer == Player.m_localPlayer) 
                {
                    HMTutorial.Show(HMTutorial.Thegn);
                }
            }
            if (!m_hirdmandrnpc.m_jobThegn)
            {
                Jotunn.Logger.LogInfo("Warrior job Thegn disabled");
            }
        }

        public void CombatJobHimthiki(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("jobHimthiki", toggle_value);
            SyncCombatTogglesFromFields();
            RefreshGuardControls();

            if (m_hirdmandrnpc.m_jobHimthiki)
            {
                Jotunn.Logger.LogInfo("Warrior job Himthiki enabled");
                var thisPlayer = m_hirdmandrnpc.m_user?.GetComponent<Player>();
                if (thisPlayer == Player.m_localPlayer) 
                {
                    HMTutorial.Show(HMTutorial.Himthiki);
                }
            }
            if (!m_hirdmandrnpc.m_jobHimthiki)
            {
                Jotunn.Logger.LogInfo("Warrior job Himthiki disabled");
            }
        }

        public void ThegnShiftDay(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("thegnDayshift", toggle_value);
        }

        public void ThegnShiftNight(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("thegnNightshift", toggle_value);
        }

        // The Jarl line under the NPC name: "Jarl: " in the header orange, the display name
        // white, same size. Hidden while the name is empty (welcome-only capture).
        private void RefreshJarlLine()
        {
            if (g_jarl == null)
            {
                return;
            }
            string jarlName = m_hirdmandrnpc.m_jarlName;
            if (jarlName.Length == 0)
            {
                g_jarl.SetActive(false);
                return;
            }
            Color c = GUIManager.Instance.ValheimOrange;
            string label = string.Format("<color=#{0:x2}{1:x2}{2:x2}>Jarl: </color>",
                (int)(c.r * 255f), (int)(c.g * 255f), (int)(c.b * 255f));
            g_jarl.GetComponent<Text>().text = label + jarlName;
            g_jarl.SetActive(true);
        }

        // (Re)sync the guard controls from the NPC fields: set the Day/Night toggle states and enable
        // the button + toggles only while the active warrior job is Thegn. Called on panel open and
        // when the combat job changes.
        private void RefreshGuardControls()
        {
            if (g_b_guard == null || g_war_ch_thegnDay == null || g_war_ch_thegnNight == null) return;
            bool thegn = m_hirdmandrnpc.m_roleWarrior && m_hirdmandrnpc.m_jobThegn;
            g_war_ch_thegnDay.GetComponent<Toggle>().isOn = m_hirdmandrnpc.m_thegnDayshift;
            g_war_ch_thegnNight.GetComponent<Toggle>().isOn = m_hirdmandrnpc.m_thegnNightshift;
            g_b_guard.GetComponent<Button>().interactable = thegn;
            g_war_ch_thegnDay.GetComponent<Toggle>().interactable = thegn;
            g_war_ch_thegnNight.GetComponent<Toggle>().interactable = thegn;
        }

        public void ToggleGatherer(bool toggle_value)
        {
            m_hirdmandrnpc.InvokeSetting("jobGatherer", toggle_value);
            Jotunn.Logger.LogInfo("Gatherer enabled changed to " + toggle_value.ToString());
        }

        private void SyncCombatTogglesFromFields()
        {
            if (m_hirdmandrnpc.m_fightingStyleDefense)
            {
                g_war_ch_styleDefense.GetComponent<Toggle>().isOn = true;
            }
            else if (m_hirdmandrnpc.m_fightingStyleOffense)
            {
                g_war_ch_styleOffense.GetComponent<Toggle>().isOn = true;
            }

            if (m_hirdmandrnpc.m_fightingRangeClose)
            {
                g_war_ch_rangeClose.GetComponent<Toggle>().isOn = true;
            }
            else if (m_hirdmandrnpc.m_fightingRangeMid)
            {
                g_war_ch_rangeMid.GetComponent<Toggle>().isOn = true;
            }
            else if (m_hirdmandrnpc.m_fightingRangeFar)
            {
                g_war_ch_rangeFar.GetComponent<Toggle>().isOn = true;
            }
        }

        public void FightingStyle(bool toggle_value)
        {
            if (toggle_value) {
                m_hirdmandrnpc.InvokeSetting("fightingStyleDefense", g_war_ch_styleDefense.GetComponent<Toggle>().isOn);
                m_hirdmandrnpc.InvokeSetting("fightingStyleOffense", g_war_ch_styleOffense.GetComponent<Toggle>().isOn);

                if (m_hirdmandrnpc.m_fightingStyleDefense)
                {
                    Jotunn.Logger.LogInfo("Warrior fighting style changed to Defensive");
                }
                if (m_hirdmandrnpc.m_fightingStyleOffense)
                {
                    Jotunn.Logger.LogInfo("Warrior fighting style changed to Offensive");
                }
            }
        }
        public void FightingRange(bool toggle_value)
        {
            if (toggle_value) {
                m_hirdmandrnpc.InvokeSetting("fightingRangeClose", g_war_ch_rangeClose.GetComponent<Toggle>().isOn);
                m_hirdmandrnpc.InvokeSetting("fightingRangeMid", g_war_ch_rangeMid.GetComponent<Toggle>().isOn);
                m_hirdmandrnpc.InvokeSetting("fightingRangeFar", g_war_ch_rangeFar.GetComponent<Toggle>().isOn);

                if (m_hirdmandrnpc.m_fightingRangeClose)
                {
                    Jotunn.Logger.LogInfo("Warrior fighting range changed to Close");
                }
                if (m_hirdmandrnpc.m_fightingRangeMid)
                {
                    Jotunn.Logger.LogInfo("Warrior fighting range changed to Mid");
                }
                if (m_hirdmandrnpc.m_fightingRangeFar)
                {
                    Jotunn.Logger.LogInfo("Warrior fighting range changed to Far");
                }
            }
        }
        public void update_skills()
        {
            var first_line = true;

            var skillnames = "";
            var skillvalues = "";
            foreach (HMSkills.SkillData each_skill in m_hirdmandrnpc.m_skills.m_hmSkills)
            {
                if (!first_line)
                {
                    skillnames += "\n";
                    skillvalues += "\n";
                }
                skillnames += each_skill.m_readable;
                skillvalues += (int)each_skill.m_value;

                first_line = false;
            }
            g_skillnames.GetComponent<Text>().text = skillnames;
            g_skillvalues.GetComponent<Text>().text = skillvalues;
        }
    }
}
