using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hirdmandr
{
    //[HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
    //internal class CharacterHoverTextPatch
    //{
    //    static void Postfix(ref string __result, ref Character __instance)
    //    {
    //        if (__instance.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
    //        {
    //            __result = HirdmandrComp.GetHoverText();
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(Character), nameof(Character.GetHoverName))]
    //internal class CharacterHoverNamePatch
    //{
    //    private static void Postfix(ref string __result, ref Character __instance)
    //    {
    //        if (__instance.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
    //        {
    //            __result = HirdmandrComp.GetHoverName();
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal class CharacterRPC_DamagePatch
    {
        private static bool Prefix(long sender, HitData hit, Character __instance)
        {
            if (__instance.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                HirdmandrComp.Hirdmandr_RPC_Damage(__instance, sender, hit, HirdmandrComp);
                return false;
            }
            return true;
        }
    }

    // Warrior training (attacker side): when a rescued hirdmandr's attack damages an enemy,
    // the attacker's melee/range skill trains (see HM_WarriorHitSkillup for the gates).
    // Non-skipping prefix on the same method as CharacterRPC_DamagePatch: it observes every
    // damage event regardless of that patch's receiver-side skip, and its subject is the
    // hit's ATTACKER (usually a plain monster, hence the separate patch class).
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal class CharacterRPC_DamageAttackerPatch
    {
        private static void Prefix(long sender, HitData hit, Character __instance)
        {
            if (hit != null)
            {
                Character attacker = hit.GetAttacker();
                if (attacker != null && attacker.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
                {
                    HirdmandrComp.HM_WarriorHitSkillup(hit, __instance);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal class CharacterDamagePatch
    {
        // D9: an unrescued Hirdmandr is invulnerable. The Hirdmandr's normal damage path is
        // RPC_Damage -> Hirdmandr_RPC_Damage -> ApplyDamage, which already early-returns when
        // not rescued; this guard covers the direct Character.Damage path (non-RPC sources).
        private static bool Prefix(Character __instance)
        {
            if (__instance.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp) && !HirdmandrComp.m_isRescued)
            {
                return false;
            }
            return true;
        }
    }
}
