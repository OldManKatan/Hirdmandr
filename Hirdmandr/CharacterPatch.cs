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
}
