using BepInEx;
using Hirdmandr;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Managers;
using Jotunn.Utils;
using OldManSM;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


namespace Hirdmandr
{
    public class ThreatenedSM : StateMachine
    {
        public string changeTopState = "";

        // Step 12 / #49l — Escape tuning (MY-DEFAULT; tune P3 / §10).
        //   kEscapeEnterHP: start fleeing once HP drops below this fraction (≈ the "in danger" HP floor).
        //   kEscapeExitHP : stop fleeing (re-engage) once HP is back above this fraction. The gap
        //                   vs kEscapeEnterHP is hysteresis so the state doesn't flap at one boundary.
        //   kHelperRange : how far an overmatched hirdmandr will run toward a helper (backup).
        //   kAwayScan    : how far to look for the threat we are fleeing from.
        //   kAwayRunDist : how far away from the threat to run, per escapeAny move.
        private const float kEscapeEnterHP = 0.3f;
        private const float kEscapeExitHP = 0.4f;
        private const float kHelperRange = 60f;
        private const float kAwayScan = 50f;
        private const float kAwayRunDist = 25f;

        public ThreatenedSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("callForHelp", new NodeCallForHelp(this));
            AddState("escapeHelp", new NodeEscapeHelp(this));
            AddState("escapeAny", new NodeEscapeAny(this));
            AddState("panic", new NodePanic(this));
        }

        // Shared: this NPC's current HP as a fraction of max (0..1). Guards nulls + divide-by-zero.
        private static float HPFraction(HirdmandrNPC n)
        {
            if (n == null || n.m_humanoid == null) return 0f;
            float max = n.m_humanoid.GetMaxHealth();
            if (max <= 0f) return 0f;
            return n.m_humanoid.GetHealth() / max;
        }

        // Shared: stop the mod-driven retreat move so the native (alerted) AI can take over again
        // (used when an escaping hirdmandr heals back to fighting shape).
        private static void StopRetreat(HirdmandrAI ai)
        {
            ai.moveEnabled = false;
            ai.moveToPos = Vector3.zero;
            ai.moveToReached = false;
            ai.moveToTrying = false;
        }

        public class NodeCallForHelp : SMNode
        {
            public NodeCallForHelp(ThreatenedSM psm) : base(psm) { }

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }

            override public void RunState()
            {
                // The victim is in danger (the ZDO distress flag was already raised by
                // HirdmandrAI.Update() on the alert flip, Step 9). If we are overmatched (HP below
                // the escape floor) we stop trading hits and start fleeing; otherwise a warrior just
                // fights natively (the alerted MonsterAI handles the chase/attack) and we hold here.
                if (HPFraction(hmNPC) < kEscapeEnterHP)
                {
                    parentSM.ChangeState("escapeHelp");
                }
            }
        }

        public class NodeEscapeHelp : SMNode
        {
            public NodeEscapeHelp(ThreatenedSM psm) : base(psm) { }

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }

            override public void RunState()
            {
                // Re-engage once healed back above the exit threshold (hysteresis band).
                if (HPFraction(hmNPC) > kEscapeExitHP)
                {
                    StopRetreat(hmAI);
                    parentSM.ChangeState("callForHelp");
                    return;
                }
                // Priority 1: run toward the nearest helper (another hirdmandr / the player).
                Transform helper = hmNPC.FindNearestHelper(kHelperRange);
                if (helper != null)
                {
                    // Only (re)issue the run while still meaningfully far; once at the helper we hold
                    // (we are "with" the backup). Re-issuing every tick tracks a moving helper, and
                    // UpdateMoving (per-FixedUpdate) does the actual stepping.
                    if (Vector3.Distance(hmNPC.transform.position, helper.position) > 4f)
                    {
                        hmAI.TryToMove(helper.position, 3f, 1f, 3f, false, 30f);
                    }
                }
                else
                {
                    // No helper close enough: fall through to the generic "get to safety" escape.
                    parentSM.ChangeState("escapeAny");
                }
            }
        }

        public class NodeEscapeAny : SMNode
        {
            public NodeEscapeAny(ThreatenedSM psm) : base(psm) { }

            override public void EnterFrom(int aState)
            {
                // Start the "get to safety" retreat immediately on entry.
                hmAI.TryToMove(GetAwayPoint(), 3f, 1f, 3f, false, 30f);
            }
            override public void ExitTo(int aState) { }

            // A point AWAY from the nearest threat: current position pushed kAwayRunDist along the
            // (self - threat) direction, flattened to the ground plane. Falls back to the facing
            // direction if no live threat is within kAwayScan (defensive — we should still be fleeing
            // from *something*, but this keeps us moving instead of stalling).
            private Vector3 GetAwayPoint()
            {
                Vector3 self = hmNPC.transform.position;
                Vector3 danger = self;
                if (hmMAI != null && hmMAI.m_character != null)
                {
                    Character threat = BaseAI.FindClosestEnemy(hmMAI.m_character, self, kAwayScan);
                    if (threat != null && !threat.IsDead())
                    {
                        danger = threat.transform.position;
                    }
                }
                Vector3 dir = self - danger;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f)
                {
                    dir = hmNPC.transform.forward;
                    dir.y = 0f;
                }
                dir = dir.normalized;
                return self + dir * kAwayRunDist;
            }

            override public void RunState()
            {
                // Re-engage once healed (hysteresis band).
                if (HPFraction(hmNPC) > kEscapeExitHP)
                {
                    StopRetreat(hmAI);
                    parentSM.ChangeState("callForHelp");
                    return;
                }
                // Priority 2/3: retreat away from the danger, preferring cover. requireShelter=false
                // so UpdateMoving always finds a point to run to — it prefers a roof/cover near the
                // away point if one exists (the "shelter"), else open ground (the "away from danger").
                // Re-issue only once the previous move finished, so the NPC keeps retreating instead
                // of re-pathing to a fresh point every tick.
                if (!hmAI.moveEnabled || hmAI.moveToReached)
                {
                    hmAI.TryToMove(GetAwayPoint(), 3f, 1f, 3f, false, 30f);
                }
            }
        }

        public class NodePanic : SMNode
        {
            public NodePanic(ThreatenedSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodePanic not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }
}
