using Hirdmandr;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;


namespace Hirdmandr
{
    public class HirdmandrGUIRescue : MonoBehaviour
    {
        GameObject GUIRescue;
        Humanoid m_humanoid;
        HirdmandrNPC m_hirdmandrnpc;
        GameObject g_name;
        GameObject g_speak;
        GameObject g_b_follow;
        GameObject g_b_stay;
        GameObject g_b_newhome;

        void Update()
        {
            if (GUIRescue)
            {
                if (
                    GUIRescue.activeSelf &&
                        (
                            ZInput.GetButtonDown("Inventory") ||
                            ZInput.GetButtonDown("JoyButtonB") ||
                            ZInput.GetButtonDown("JoyButtonY") ||
                            Input.GetKeyDown(KeyCode.Escape) ||
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
            // Create the panel if it does not exist
            if (!GUIRescue)
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

                m_humanoid = GetComponent<Humanoid>();
                m_hirdmandrnpc = GetComponent<HirdmandrNPC>();

                // Create the panel object
                GUIRescue = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0),
                    width: 500,
                    height: 500,
                    draggable: true);
                GUIRescue.SetActive(false);

                // Add the Jötunn draggable Component to the panel
                // Note: This is normally automatically added when using CreateWoodpanel()
                // GUIRescue.AddComponent<DragWindowCntrl>();
                // DragWindowCntrl.ApplyDragWindowCntrl(GUIRescue);

                // Create the text object
                g_name = GUIManager.Instance.CreateText(
                    text: m_humanoid.m_name,
                    parent: GUIRescue.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -45f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 450f,
                    height: 40f,
                    addContentSizeFitter: false);

                g_speak = GUIManager.Instance.CreateText(
                    text: m_hirdmandrnpc.GetRescueText(),
                    parent: GUIRescue.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -210f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 18,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 450f,
                    height: 240f,
                    addContentSizeFitter: false);


                // Create the button object
                g_b_follow = GUIManager.Instance.CreateButton(
                    text: "Follow Me, I'll take you to safety!",
                    parent: GUIRescue.transform,
                    anchorMin: new Vector2(0.5f, 0.0f),
                    anchorMax: new Vector2(0.5f, 0.0f),
                    position: new Vector2(0f, 140f),
                    width: 300f,
                    height: 60f);
                g_b_follow.SetActive(true);

                // Add a listener to the button to close the panel again
                Button b_followComp = g_b_follow.GetComponent<Button>();
                b_followComp.onClick.AddListener(m_hirdmandrnpc.Rescue);

                // Create the button object
                g_b_stay = GUIManager.Instance.CreateButton(
                    text: "Stay Here, It's still dangerous...",
                    parent: GUIRescue.transform,
                    anchorMin: new Vector2(0.5f, 0.0f),
                    anchorMax: new Vector2(0.5f, 0.0f),
                    position: new Vector2(0f, 55f),
                    width: 300f,
                    height: 60f);
                g_b_stay.SetActive(true);

                // Add a listener to the button to close the panel again
                Button b_stayComp = g_b_stay.GetComponent<Button>();
                b_stayComp.onClick.AddListener(m_hirdmandrnpc.RescueWait);

                // Create the button object
                g_b_newhome = GUIManager.Instance.CreateButton(
                    text: "Welcome to your new home!",
                    parent: GUIRescue.transform,
                    anchorMin: new Vector2(0.5f, 0.0f),
                    anchorMax: new Vector2(0.5f, 0.0f),
                    position: new Vector2(0f, 55f),
                    width: 300f,
                    height: 60f);
                g_b_newhome.SetActive(false);

                // Add a listener to the button to close the panel again
                Button b_newhomeComp = g_b_stay.GetComponent<Button>();
                b_newhomeComp.onClick.AddListener(m_hirdmandrnpc.WelcomeHome);

            }

            // Switch the current state
            bool state = !GUIRescue.activeSelf;

            if (state)
            {
                g_speak.GetComponent<Text>().text = m_hirdmandrnpc.GetRescueText();
            }
            // Set the active state of the panel
            GUIRescue.SetActive(state);

            // Toggle input for the player and camera while displaying the GUI
            GUIManager.BlockInput(state);

        }
    }
}
