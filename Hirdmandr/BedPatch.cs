using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hirdmandr
{
    [HarmonyPatch(typeof(Bed), nameof(Bed.GetHoverText))]
    internal class BedHoverTextPatch
    {
        private static void Postfix(ref string __result, ref Bed __instance)
        {
            if (__instance.TryGetComponent<HirdmandrBed>(out var HirdmandrComp))
            {
                __result = HirdmandrComp.GetHoverText();
            }
        }
    }

    [HarmonyPatch(typeof(Bed), nameof(Bed.GetHoverName))]
    internal class BedHoverNamePatch
    {
        private static void Postfix(ref string __result, ref Bed __instance)
        {
            if (__instance.TryGetComponent<HirdmandrBed>(out var HirdmandrComp))
            {
                __result = HirdmandrComp.GetHoverName();
            }
        }
    }
}
