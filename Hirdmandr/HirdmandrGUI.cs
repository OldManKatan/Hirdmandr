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
        bool state = false;
        GameObject g_name;
        GameObject g_talk;
        GameObject g_mood;
        GameObject g_values;
        GameObject g_skillnames;
        GameObject g_skillvalues;
        GameObject g_role_t_header;
        GameObject g_art_ch_woodburner;
        GameObject g_art_t_woodburner;
        GameObject g_art_ch_furnaceoper;
        GameObject g_art_t_furnaceoper;
        GameObject g_art_ch_farmer;
        GameObject g_art_t_farmer;
        GameObject g_art_ch_cook;
        GameObject g_art_t_cook;
        GameObject g_art_ch_baker;
        GameObject g_art_t_baker;
        GameObject g_role_ch_artisan;
        GameObject g_role_ch_warrior;
        ToggleGroup g_role_tg;
        GameObject g_war_ch_thegn;
        GameObject g_war_t_thegn;
        GameObject g_war_ch_himthiki;
        GameObject g_war_t_himthiki;
        GameObject g_war_b_himthikiFollow;
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

                var valuestring = "";
                var first_line = true;
                foreach (HMPersonality.ValueData each_value in m_hirdmandrnpc.m_personality.m_hmValues)
                {
                    if (!first_line)
                    {
                        valuestring += "\n";
                    }
                    if (each_value.m_value > 0.7) { valuestring += "<color=green>Strongly values</color> " + each_value.m_readable; }
                    else if (each_value.m_value > 0.4) { valuestring += "<color=green>Values</color> " + each_value.m_readable; }
                    else if (each_value.m_value > 0.1) { valuestring += "Weakly values " + each_value.m_readable; }
                    else if (each_value.m_value > -0.1) { valuestring += "Doesn't care about " + each_value.m_readable; }
                    else if (each_value.m_value > -0.4) { valuestring += "Weakly disdains " + each_value.m_readable; }
                    else if (each_value.m_value > -0.7) { valuestring += "<color=red>Disdains</color> " + each_value.m_readable; }
                    else { valuestring += "<color=red>Strongly disdains</color> " + each_value.m_readable; }

                    first_line = false;
                }

                var skillnames = "";
                var skillvalues = "";
                first_line = true;
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
                    anchorMin: new Vector2(1f, 0.5f),
                    anchorMax: new Vector2(1f, 0.5f),
                    position: new Vector2(-550, 0),
                    width: 1000,
                    height: 500,
                    draggable: true);
                GUIHirdmandr.SetActive(true);

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
                    position: new Vector2(0f, -45f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 450f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_talk = GUIManager.Instance.CreateText(
                    text: m_hirdmandrnpc.GetRescueText(),
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.25f, 1f),
                    anchorMax: new Vector2(0.25f, 1f),
                    position: new Vector2(0f, -115f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 18,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 450f,
                    height: 80f,
                    addContentSizeFitter: false);

                g_mood = GUIManager.Instance.CreateText(
                    text: "This is my mood!",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.25f, 1f),
                    anchorMax: new Vector2(0.25f, 1f),
                    position: new Vector2(0f, -200f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 18,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 450f,
                    height: 80f,
                    addContentSizeFitter: false);

                GUIManager.Instance.CreateText(
                    text: "Personality",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.15f, 1f),
                    anchorMax: new Vector2(0.15f, 1f),
                    position: new Vector2(0f, -290f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 250f,
                    height: 40f,
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

                g_values = GUIManager.Instance.CreateText(
                    text: valuestring,
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.15f, 1f),
                    anchorMax: new Vector2(0.15f, 1f),
                    position: new Vector2(0f, -400f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 250f,
                    height: 180f,
                    addContentSizeFitter: false);

                g_skillnames = GUIManager.Instance.CreateText(
                    text: skillnames,
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.375f, 1f),
                    anchorMax: new Vector2(0.375f, 1f),
                    position: new Vector2(0f, -400f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 150f,
                    height: 180f,
                    addContentSizeFitter: false);

                g_skillvalues = GUIManager.Instance.CreateText(
                    text: skillvalues,
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.475f, 1f),
                    anchorMax: new Vector2(0.475f, 1f),
                    position: new Vector2(0f, -400f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 16,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 25f,
                    height: 180f,
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
                g_role_ch_warriorComp.isOn = m_hirdmandrnpc.m_roleArtisan;
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
                g_art_ch_woodburnerRect.anchoredPosition = new Vector2(0f, -100);
                g_art_ch_woodburnerRect.ForceUpdateRectTransforms();

                var g_art_ch_woodburnerComp = g_art_ch_woodburner.GetComponent<Toggle>();
                g_art_ch_woodburnerComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("woodburner");
                g_art_ch_woodburnerComp.onValueChanged.AddListener(WoodToggle);

                g_art_t_woodburner = GUIManager.Instance.CreateText(
                    text: "Wood Burner",
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

                g_art_ch_furnaceoper = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_furnaceoperRect = g_art_ch_furnaceoper.GetComponent<RectTransform>();
                g_art_ch_furnaceoperRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_furnaceoperRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_furnaceoperRect.anchoredPosition = new Vector2(0f, -150);

                var g_art_ch_furnaceoperComp = g_art_ch_furnaceoper.GetComponent<Toggle>();
                g_art_ch_furnaceoperComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("furnaceoperator");
                g_art_ch_furnaceoperComp.onValueChanged.AddListener(FurnaceToggle);

                g_art_t_furnaceoper = GUIManager.Instance.CreateText(
                    text: "Furnace Operator",
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

                g_art_ch_farmer = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_farmerRect = g_art_ch_farmer.GetComponent<RectTransform>();
                g_art_ch_farmerRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_farmerRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_farmerRect.anchoredPosition = new Vector2(0f, -200);

                var g_art_ch_farmerComp = g_art_ch_farmer.GetComponent<Toggle>();
                g_art_ch_farmerComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("farmer");
                g_art_ch_farmerComp.onValueChanged.AddListener(FarmerToggle);

                g_art_t_farmer = GUIManager.Instance.CreateText(
                    text: "Farmer",
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

                g_art_ch_cook = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_cookRect = g_art_ch_cook.GetComponent<RectTransform>();
                g_art_ch_cookRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_cookRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_cookRect.anchoredPosition = new Vector2(0f, -250);

                var g_art_ch_cookComp = g_art_ch_cook.GetComponent<Toggle>();
                g_art_ch_cookComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("cook");
                g_art_ch_cookComp.onValueChanged.AddListener(CookToggle);

                g_art_t_cook = GUIManager.Instance.CreateText(
                    text: "Cook",
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

                g_art_ch_baker = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_art_ch_bakerRect = g_art_ch_baker.GetComponent<RectTransform>();
                g_art_ch_bakerRect.anchorMin = new Vector2(0.55f, 1f);
                g_art_ch_bakerRect.anchorMax = new Vector2(0.55f, 1f);
                g_art_ch_bakerRect.anchoredPosition = new Vector2(0f, -300);

                var g_art_ch_bakerComp = g_art_ch_baker.GetComponent<Toggle>();
                g_art_ch_bakerComp.isOn = m_hirdmandrnpc.m_skills.isEnabled("baker");
                g_art_ch_bakerComp.onValueChanged.AddListener(BakerToggle);

                g_art_t_baker = GUIManager.Instance.CreateText(
                    text: "Baker",
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

                all_arts = new GameObject[10] {
                    g_art_ch_woodburner,
                    g_art_t_woodburner,
                    g_art_ch_furnaceoper,
                    g_art_t_furnaceoper,
                    g_art_ch_farmer,
                    g_art_t_farmer,
                    g_art_ch_cook,
                    g_art_t_cook,
                    g_art_ch_baker,
                    g_art_t_baker
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
                g_war_b_himthikiFollow = GUIManager.Instance.CreateButton(
                    text: "Follow",
                    parent: GUIHirdmandr.transform,
                    anchorMin: new Vector2(0.9f, 1f),
                    anchorMax: new Vector2(0.9f, 1f),
                    position: new Vector2(0f, -150f),
                    width: 100f,
                    height: 40f);
                g_war_b_himthikiFollow.SetActive(true);

                // Add a listener to the button to close the panel again
                Button g_war_b_himthikiFollowComp = g_war_b_himthikiFollow.GetComponent<Button>();
                g_war_b_himthikiFollowComp.onClick.AddListener(m_hirdmandrnpc.HimthikiFollow);
                
                g_war_ch_gatherer = GUIManager.Instance.CreateToggle(
                    parent: GUIHirdmandr.transform,
                    width: 30f,
                    height: 30f);

                var g_war_ch_gathererRect = g_war_ch_gatherer.GetComponent<RectTransform>();
                g_war_ch_gathererRect.anchorMin = new Vector2(0.55f, 1f);
                g_war_ch_gathererRect.anchorMax = new Vector2(0.55f, 1f);
                g_war_ch_gathererRect.anchoredPosition = new Vector2(0f, -200);

                var g_war_ch_gathererComp = g_war_ch_gatherer.GetComponent<Toggle>();
                g_war_ch_gathererComp.isOn = m_hirdmandrnpc.m_jobHimthiki;
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
                g_war_ch_styleDefenseComp.isOn = m_hirdmandrnpc.m_jobHimthiki;
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
                g_war_ch_styleOffenseComp.isOn = m_hirdmandrnpc.m_jobHimthiki;
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
                g_war_ch_rangeCloseComp.isOn = m_hirdmandrnpc.m_jobHimthiki;
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
                g_war_ch_rangeMidComp.isOn = m_hirdmandrnpc.m_jobHimthiki;
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
                g_war_ch_rangeFarComp.isOn = m_hirdmandrnpc.m_jobHimthiki;
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


                all_wars = new GameObject[18] {
                    g_war_ch_thegn,
                    g_war_t_thegn,
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
                g_style_tg.allowSwitchOff = false;

            }

            // Switch the current state
            // bool state = !GUIHirdmandr.activeSelf;
            state = !state;

            if (state)
            {
                string thoughtSnapshot = "";
                int startTht = m_hirdmandrnpc.m_thoughts.m_thoughts.Count - 5;
                if (startTht < 0)
                {
                    startTht = 0;
                }
                for (var i = startTht; i < m_hirdmandrnpc.m_thoughts.m_thoughts.Count; i++)
                {
                    string thtStr = "";
                    HMThoughts.Thought atht = m_hirdmandrnpc.m_thoughts.m_thoughts[i];
                    string thisStr = m_hirdmandrnpc.m_thoughts.m_thoughtStrings[atht.m_type]["thoughtStrings"][
                        UnityEngine.Random.Range(0, m_hirdmandrnpc.m_thoughts.m_thoughtStrings[atht.m_type]["thoughtStrings"].Count)
                        ];
                    thtStr = thtStr.Replace("%feltAbout%", m_hirdmandrnpc.m_thoughts.StrengthToFeelStr(atht.m_calcStrength));
                    thtStr = thtStr.Replace("%subject%", atht.m_subject);
                    thoughtSnapshot = thoughtSnapshot + thtStr;
                }
                g_mood.GetComponent<Text>().text = m_hirdmandrnpc.m_mood.ToString() + "\n" + thoughtSnapshot;
                g_talk.GetComponent<Text>().text = m_hirdmandrnpc.GetRescueText();
            }
            else
            {
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
                m_hirdmandrnpc.m_roleArtisan = g_role_ch_artisan.GetComponent<Toggle>().isOn;
                m_hirdmandrnpc.m_roleWarrior = g_role_ch_warrior.GetComponent<Toggle>().isOn;

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
                    var thisPlayer = m_hirdmandrnpc.m_user.GetComponent<Player>();
                    if (thisPlayer == Player.m_localPlayer) 
                    {
                        Player.m_localPlayer?.ShowTutorial("hirdmandr_artisan");
                    }
                }
                if (m_hirdmandrnpc.m_roleWarrior)
                {
                    Jotunn.Logger.LogInfo("Role changed to Warrior");
                    foreach (GameObject go in all_arts)
                    {
                        go.SetActive(false);
                    }
                    foreach (GameObject go in all_wars)
                    {
                        go.SetActive(true);
                    }
                }
            }
        }

        public void WoodToggle(bool toggle_value)
        {
            m_hirdmandrnpc.m_skills.SetSkillEnable("woodburner", toggle_value);
        }

        public void FurnaceToggle(bool toggle_value)
        {
            m_hirdmandrnpc.m_skills.SetSkillEnable("furnaceoperator", toggle_value);
        }

        public void FarmerToggle(bool toggle_value)
        {
            m_hirdmandrnpc.m_skills.SetSkillEnable("farmer", toggle_value);
        }

        public void CookToggle(bool toggle_value)
        {
            m_hirdmandrnpc.m_skills.SetSkillEnable("cook", toggle_value);
        }

        public void BakerToggle(bool toggle_value)
        {
            m_hirdmandrnpc.m_skills.SetSkillEnable("baker", toggle_value);
        }

        public void CombatJobThegen(bool toggle_value)
        {
            m_hirdmandrnpc.m_jobThegn = toggle_value;

            if (m_hirdmandrnpc.m_jobThegn)
            {
                Jotunn.Logger.LogInfo("Warrior job Thegn enabled");
                var thisPlayer = m_hirdmandrnpc.m_user.GetComponent<Player>();
                if (thisPlayer == Player.m_localPlayer) 
                {
                    Player.m_localPlayer?.ShowTutorial("hirdmandr_thegn");
                }
            }
            if (!m_hirdmandrnpc.m_jobThegn)
            {
                Jotunn.Logger.LogInfo("Warrior job Thegn disabled");
            }
        }

        public void CombatJobHimthiki(bool toggle_value)
        {
            m_hirdmandrnpc.m_jobHimthiki = toggle_value;

            if (m_hirdmandrnpc.m_jobHimthiki)
            {
                Jotunn.Logger.LogInfo("Warrior job Himthiki enabled");
                var thisPlayer = m_hirdmandrnpc.m_user.GetComponent<Player>();
                if (thisPlayer == Player.m_localPlayer) 
                {
                    Player.m_localPlayer?.ShowTutorial("hirdmandr_himthiki");
                }
            }
            if (!m_hirdmandrnpc.m_jobHimthiki)
            {
                Jotunn.Logger.LogInfo("Warrior job Himthiki disabled");
            }
        }

        public void ToggleGatherer(bool toggle_value)
        {
            m_hirdmandrnpc.m_jobGatherer = toggle_value;
            Jotunn.Logger.LogInfo("Gatherer enabled changed to " + toggle_value.ToString());
        }

        public void FightingStyle(bool toggle_value)
        {
            if (toggle_value) {
                m_hirdmandrnpc.m_fightingStyleDefense = g_war_ch_styleDefense.GetComponent<Toggle>().isOn;
                m_hirdmandrnpc.m_fightingStyleOffense = g_war_ch_styleOffense.GetComponent<Toggle>().isOn;

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
                m_hirdmandrnpc.m_fightingRangeClose = g_war_ch_rangeClose.GetComponent<Toggle>().isOn;
                m_hirdmandrnpc.m_fightingRangeMid = g_war_ch_rangeMid.GetComponent<Toggle>().isOn;
                m_hirdmandrnpc.m_fightingRangeFar = g_war_ch_rangeFar.GetComponent<Toggle>().isOn;

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
