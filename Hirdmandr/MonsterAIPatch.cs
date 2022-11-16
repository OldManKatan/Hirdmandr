using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hirdmandr
{
    internal class MonsterAIPatch
    {
        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.SelectBestAttack))]
        private static bool Prefix(Humanoid hum, float dt, MonsterAI __instance, ItemDrop.ItemData __result)
        {
            if (__instance.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                __result = HirdmandrComp.HM_SelectBestAttack();
                return false;
            }
            return true;
        }
    }
}
