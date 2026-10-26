using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hirdmandr
{
    internal class MonsterAIPatch
    {
        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.SelectBestAttack))]
        private static bool Prefix(Humanoid hum, float dt, MonsterAI __instance, ItemDrop.ItemData __result)
        {
            if (__instance.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                __result = HirdmandrComp.HM_SelectBestAttack(hum);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.DoAttack))]
    internal class MonsterAI_DoAttackPatch
    {
        private static bool Prefix(Character target, bool isFriend, MonsterAI __instance)
        {
            if (__instance != null && __instance.m_character != null
                && __instance.m_character.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                HirdmandrComp.HM_PrepareBowAttack();
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipBestWeapon))]
    internal class Humanoid_EquipBestWeaponPatch
    {
        private static bool Prefix(Character targetCreature, StaticTarget targetStatic, Character hurtFriend, Character friend, Humanoid __instance)
        {
            if (__instance != null && __instance.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                HirdmandrComp.HM_SelectBestAttack(targetCreature as Humanoid);
                return false;
            }
            return true;
        }
    }

    // D9: an unrescued Hirdmandr is not a valid mob target. Every BaseAI enemy search filters
    // candidates through IsEnemy, so returning false here makes monsters ignore it. IsEnemy has
    // two overloads (one-arg + two-arg); each gets its own patch class, both sharing
    // HirdmandrD9.IsUnrescuedTarget. Named "IsEnemy" as a string because nameof() is ambiguous
    // on an overloaded method.
    [HarmonyPatch(typeof(BaseAI), "IsEnemy", new[] { typeof(Character) })]
    internal class BaseAI_IsEnemyOtherPatch
    {
        private static bool Prefix(Character other, BaseAI __instance)
        {
            return !HirdmandrD9.IsUnrescuedTarget(__instance, other);
        }
    }

    [HarmonyPatch(typeof(BaseAI), "IsEnemy", new[] { typeof(Character), typeof(Character) })]
    internal class BaseAI_IsEnemyABPatch
    {
        private static bool Prefix(Character a, Character b, BaseAI __instance)
        {
            return !(HirdmandrD9.IsUnrescuedTarget(__instance, a) || HirdmandrD9.IsUnrescuedTarget(__instance, b));
        }
    }

    internal static class HirdmandrD9
    {
        public static bool IsUnrescuedTarget(BaseAI instance, Character candidate)
        {
            if (candidate == null)
            {
                return false;
            }
            // Never treat the AI's own character as a target: keeps the Hirdmandr's own
            // (post-rescue) MonsterAI from being suppressed by this patch.
            if (instance != null && candidate == instance.m_character)
            {
                return false;
            }
            return candidate.TryGetComponent<HirdmandrNPC>(out var comp) && !comp.m_isRescued;
        }
    }

    // Step 11 / #49k — L2 defense-anchor targeting (§7).
    //
    // Base target-select (resolves §12.1): MonsterAI.UpdateTarget picks the creature target via
    // BaseAI.FindEnemy() — the nearest enemy to SELF. For an anchored hirdmandr we re-prioritize
    // that pick to the nearest hostile WITHIN AnchorRange of the ANCHOR (a following warrior's
    // anchor = the player; a responder's = the distressed hirdmandr it is running to help), so the
    // NPC defends the anchor by proximity instead of the nearest to itself.
    //
    // Why a POSTFIX on FindEnemy (not a prefix, not on UpdateTarget): UpdateTarget owns all the
    // surrounding logic (re-target cadence, static-target fallback, flee-on-unreachable, MP
    // SetTargetInfo, and the canHear/canSee out-params), so we only swap the creature FindEnemy
    // returns, which keeps all of that intact and inherits vanilla's re-target cadence for free
    // (FindEnemy is only called on re-target ticks, not every frame). The first attempt was a
    // prefix that set __result and returned false, but in-game FindEnemy still came back <null>
    // while the gate (anchor + FindClosestEnemy) and every original-body filter passed at the
    // tick — the skipped-original __result never reached the caller. The postfix rewrites
    // __result after the original runs (a pure search, no side effects), which is the pattern
    // Harmony guarantees to honor. When nothing threatens the anchor we leave the original
    // result alone, so the NPC still defends itself.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.FindEnemy))]
    internal class BaseAI_FindEnemyAnchorPatch
    {
        // §10 default: defense-anchor R (proximity) ≈ 12-15 m. How far from the anchor the NPC
        // still picks up a threat. A MY-DEFAULT, tunable in P3.
        private const float AnchorRange = 15f;

        private static void Postfix(BaseAI __instance, Character __result)
        {
            if (__instance == null || __instance.m_character == null)
            {
                return;
            }
            if (!__instance.m_character.TryGetComponent<HirdmandrNPC>(out HirdmandrNPC h))
            {
                return; // not a hirdmandr -> vanilla targeting
            }
            Transform anchor = h.GetDefenseAnchor();
            if (anchor == null)
            {
                return; // no anchor (not following / not responding) -> vanilla targeting
            }
            Character threat = BaseAI.FindClosestEnemy(__instance.m_character, anchor.position, AnchorRange);
            if (threat == null || threat.IsDead())
            {
                return; // nothing alive near the anchor -> vanilla (defend self)
            }
            // FindClosestEnemy ignores its range arg (returns the nearest enemy of me ANYWHERE),
            // so enforce AnchorRange explicitly: beyond it the threat is not to the anchor.
            if (Vector3.Distance(anchor.position, threat.transform.position) > AnchorRange)
            {
                return; // nearest enemy is outside the anchor's defense radius -> vanilla
            }
            __result = threat;
        }
    }

    // #53: travel should RUN, not walk. While HirdmandrAI owns a move (moveEnabled, set by
    // TryToMove for patrol/rest/work/defend/socialize-goMeetPoint) our UpdateMoving issues
    // MoveTo(..., run:true) every FixedUpdate. But the native MonsterAI.UpdateAI ALSO runs
    // IdleMovement -> RandomMovement -> MoveTo(..., run:false) every frame for a targetless NPC,
    // whose SetRun(false) clobbers our SetRun(true) — so the NPC walks. IdleMovement is pure
    // movement (no other state), so skipping it while we own the move makes our run the sole
    // driver. When not traveling (idle/patrol) moveEnabled is false and the native wander/patrol
    // runs unchanged; combat and follow use other UpdateAI branches (moveEnabled is false there),
    // so both are unaffected.
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IdleMovement))]
    internal class BaseAI_IdleMovementPatch
    {
        private static bool Prefix(BaseAI __instance)
        {
            if (__instance == null)
            {
                return true;
            }
            HirdmandrAI ai = __instance.GetComponent<HirdmandrAI>();
            return !(ai != null && ai.moveEnabled);
        }
    }
}
