using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Hirdmandr
{
    internal static class HirdmandrHudRules
    {
        // Used only if EnemyHud.m_maxShowDistance can't be read at runtime.
        public const float FallbackShowDistance = 30f;

        private static readonly FieldInfo s_maxShowDistanceField =
            typeof(EnemyHud).GetField("m_maxShowDistance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        // Read vanilla's own non-boss show distance so our "always show" range
        // matches exactly what vanilla would show. We only override the
        // player-crouch rule and the hover-timeout, never the range itself.
        public static float GetMaxShowDistance(EnemyHud hud)
        {
            if (hud == null || s_maxShowDistanceField == null)
                return FallbackShowDistance;
            object value = s_maxShowDistanceField.GetValue(hud);
            return value is float ? (float)value : FallbackShowDistance;
        }

        public static bool IsHirdmandrNPC(Character character, out HirdmandrNPC npc)
        {
            npc = null;
            return character != null && character.TryGetComponent<HirdmandrNPC>(out npc);
        }

        public static bool ShouldAlwaysShow(Character character, HirdmandrNPC npc, float maxDistance)
        {
            if (character == null || npc == null)
                return false;
            if (npc.m_humanoid != null && npc.m_humanoid.IsDead())
                return false;

            Player local = Player.m_localPlayer;
            if (local == null)
                return false;

            return Vector3.Distance(character.transform.position, local.transform.position) <= maxDistance;
        }

        public static Color GetLocalNameColor(HirdmandrNPC npc)
        {
            Player local = Player.m_localPlayer;
            if (local != null && npc.m_jarlZOID == local.GetPlayerID())
                return Color.white;
            return Color.gray;
        }
    }

    [HarmonyPatch(typeof(EnemyHud), "TestShow")]
    internal static class EnemyHud_TestShowPatch
    {
        private static void Postfix(Character c, bool isVisible, ref bool __result, EnemyHud __instance)
        {
            HirdmandrNPC npc;
            if (HirdmandrHudRules.IsHirdmandrNPC(c, out npc) &&
                HirdmandrHudRules.ShouldAlwaysShow(c, npc, HirdmandrHudRules.GetMaxShowDistance(__instance)))
            {
                c.m_hideHud = false;
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(EnemyHud), "LateUpdate")]
    internal static class EnemyHud_LateUpdatePatch
    {
        private static FieldInfo s_instanceField = typeof(EnemyHud).GetField("m_instance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private static MethodInfo s_showHudMethod = typeof(EnemyHud).GetMethod("ShowHud", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        private static void Postfix()
        {
            if (s_instanceField == null || s_showHudMethod == null)
                return;

            EnemyHud enemyHud = (EnemyHud)s_instanceField.GetValue(null);
            if (enemyHud == null)
                return;

            float maxDistance = HirdmandrHudRules.GetMaxShowDistance(enemyHud);
            List<Character> characters = Character.GetAllCharacters();
            if (characters == null)
                return;

            for (int i = 0; i < characters.Count; i++)
            {
                Character character = characters[i];
                HirdmandrNPC npc;
                if (!HirdmandrHudRules.IsHirdmandrNPC(character, out npc))
                    continue;
                if (!HirdmandrHudRules.ShouldAlwaysShow(character, npc, maxDistance))
                    continue;

                character.m_hideHud = false;
                s_showHudMethod.Invoke(enemyHud, new object[] { character, false });
            }
        }
    }

    [HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
    internal static class EnemyHud_UpdateHudsPatch
    {
        private static bool s_initialized;
        private static FieldInfo s_hudsField;
        private static FieldInfo s_guiField;
        private static FieldInfo s_hoverTimerField;
        private static FieldInfo s_nameField;
        private static PropertyInfo s_nameColorProperty;

        private static void Initialize()
        {
            if (s_initialized)
                return;
            s_initialized = true;

            Type enemyHudType = typeof(EnemyHud);
            s_hudsField = enemyHudType.GetField("m_huds", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Type hudDataType = enemyHudType.GetNestedType("HudData", BindingFlags.NonPublic | BindingFlags.Public);
            if (hudDataType == null)
                return;

            s_guiField = hudDataType.GetField("m_gui", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            s_hoverTimerField = hudDataType.GetField("m_hoverTimer", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            s_nameField = hudDataType.GetField("m_name", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (s_nameField != null)
            {
                s_nameColorProperty = s_nameField.FieldType.GetProperty("color", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            }
        }

        // Runs after vanilla UpdateHuds.
        //
        // Vanilla only runs its update/position path for a HUD entry when TestShow
        // returns true, and for a plain (non-player) enemy it hides the plate once
        // m_hoverTimer exceeds m_hoverShowDuration, after which it stops
        // repositioning. When we leave the show range, TestShow goes false and
        // vanilla just leaves m_gui frozen in its last state (active, icons as
        // they were) -- that is the "plate stuck to the left of the player" bug.
        //
        // So we take ownership of Hirdmandr plates here:
        //   in range  -> reset m_hoverTimer to 0 so vanilla's hover-timeout never
        //                fires; vanilla keeps drawing + screen-projecting the plate
        //                exactly like a normal one, so it snaps back onto the NPC
        //                the moment we re-enter range. We only recolor the name.
        //   out of range -> force the plate inactive so nothing freezes on screen.
        private static void Postfix(EnemyHud __instance)
        {
            Initialize();
            if (s_hudsField == null || s_guiField == null)
                return;

            float maxDistance = HirdmandrHudRules.GetMaxShowDistance(__instance);

            IDictionary huds = (IDictionary)s_hudsField.GetValue(__instance);
            if (huds == null)
                return;

            foreach (DictionaryEntry entry in huds)
            {
                Character character = entry.Key as Character;
                HirdmandrNPC npc;
                if (!HirdmandrHudRules.IsHirdmandrNPC(character, out npc))
                    continue;

                object hudData = entry.Value;
                GameObject gui = (GameObject)s_guiField.GetValue(hudData);

                if (HirdmandrHudRules.ShouldAlwaysShow(character, npc, maxDistance))
                {
                    if (s_hoverTimerField != null)
                    {
                        // Keep the vanilla "show briefly" timer pinned to zero so the
                        // plate never expires while we are within range.
                        s_hoverTimerField.SetValue(hudData, 0f);
                    }

                    if (s_nameField != null && s_nameColorProperty != null)
                    {
                        object nameText = s_nameField.GetValue(hudData);
                        if (nameText != null)
                        {
                            s_nameColorProperty.SetValue(nameText, HirdmandrHudRules.GetLocalNameColor(npc), null);
                        }
                    }
                }
                else if (gui != null && gui.activeSelf)
                {
                    // We just left range (or the NPC died): make sure the plate is
                    // hidden instead of being left frozen on screen.
                    gui.SetActive(false);
                }
            }
        }
    }
}
