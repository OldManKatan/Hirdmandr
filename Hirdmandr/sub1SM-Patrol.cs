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
    public class PatrolSM : StateMachine
    {
        public string changeTopState = "";

        // Return gate: if a holding guard ends up more than this far from its assigned post
        // (knockback, being shoved, native-patrol wander), atPost walks it back. Below this the
        // re-asserted native patrol point holds it on its own.
        private const float kPostReturnRadius = 5f;

        public PatrolSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("setupPatrol", new NodeSetupPatrol(this));
            AddState("goPost", new NodeGoPost(this));
            AddState("atPost", new NodeAtPost(this));
            AddState("isAlerted", new NodeIsAlerted(this));
        }

        public class NodeSetupPatrol : SMNode
        {
            public NodeSetupPatrol(PatrolSM psm) : base(psm) { }

            // Entry: (re)pick the post for this shift, anchor the native patrol point to it, then
            // drive to it. Reached both on the top-level schedule->patrol entry and as a retry after
            // a failed goPost move.
            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                // The work lives in RunState, NOT EnterFrom: this is the FIRST state of PatrolSM, so
                // the sub-SM is constructed at curState=0 (=setupPatrol). The top's
                // ChangeState("setupPatrol") sets nextState=0 == curState, so Evaluate() finds no
                // pending transition and runs RunState instead of EnterFrom (which would never fire).
                // Same pattern as RestSM.NodeFindBed / WorkDaySM.NodeResetArtJob. Runs exactly once:
                // ChangeState("goPost") below defers the transition to the next Evaluate.
                hmAI.patrolPost = PickPost();
                hmMAI.SetPatrolPoint(hmAI.patrolPost);
                parentSM.ChangeState("goPost");
            }

            // Where the guard stands this shift. A persistent "guard here" post wins; otherwise a
            // temporary point a few meters from the nearest campfire (re-picked each shift, never
            // persisted); else the NPC's current position.
            private Vector3 PickPost()
            {
                if (hmNPC.m_thegnGuardSet && hmNPC.m_thegnGuardPoint != Vector3.zero)
                {
                    return hmNPC.m_thegnGuardPoint;
                }
                Vector3 fire = FindNearestFire();
                if (fire != Vector3.zero)
                {
                    // Stand 4-16m from the fire in a random direction: holds the campfire without
                    // sitting inside its hazard zone (which would stall the move in UpdateMoving).
                    float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    float rad = UnityEngine.Random.Range(4f, 16f);
                    fire.x += Mathf.Cos(ang) * rad;
                    fire.z += Mathf.Sin(ang) * rad;
                    return fire;
                }
                return hmAI.transform.position;
            }

            // Nearest NPC gathering fire within 100f, or Vector3.zero if none. Same source as
            // Socialize/WorkDay, so the guard anchors to the same gathering spot.
            private Vector3 FindNearestFire()
            {
                return Utils.FindNearestNpcGatheringFire(hmAI.transform.position, 100f);
            }
        }
        public class NodeGoPost : SMNode
        {
            public NodeGoPost(PatrolSM psm) : base(psm) { }

            // Issue the move to the post. UpdateMoving (per-frame) does the actual stepping; we only
            // (re)issue the destination here.
            override public void EnterFrom(int aState)
            {
                hmAI.TryToMove(hmAI.patrolPost, 1.5f, 0f, 1f, false, 30f);
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                if (hmAI.moveToTrying) return; // still moving
                if (hmAI.moveToReached)
                {
                    // Arrived: re-assert the native patrol point (UpdateMoving's no-arg SetPatrolPoint
                    // reset it to the arrival spot) and hold.
                    hmMAI.SetPatrolPoint(hmAI.patrolPost);
                    parentSM.ChangeState("atPost");
                }
                else
                {
                    // Timed out / no path: re-pick the post (may be a fresh temp point) and retry.
                    parentSM.ChangeState("setupPatrol");
                }
            }
        }
        public class NodeAtPost : SMNode
        {
            public NodeAtPost(PatrolSM psm) : base(psm) { }

            override public void EnterFrom(int aState)
            {
                hmMAI.SetPatrolPoint(hmAI.patrolPost);
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                // Hold the post natively (the alerted MonsterAI idles at the patrol point). Keep the
                // native patrol point anchored; hand off to isAlerted the moment a threat registers.
                hmMAI.SetPatrolPoint(hmAI.patrolPost);

                if (hmMAI.IsAlerted())
                {
                    // In combat: hand off immediately and do NOT start walking back to the post,
                    // which would abandon the fight.
                    parentSM.ChangeState("isAlerted");
                    return;
                }

                // Return gate: a holding guard more than kPostReturnRadius from its post walks back.
                // Runs once per patrol-SM run (this RunState). Only re-issues the move when none is in
                // flight (!moveToTrying) so the path isn't reset every frame; a timed-out / failed move
                // simply re-arms on the next run.
                float distToPost = Vector3.Distance(hmAI.transform.position, hmAI.patrolPost);
                if (hmAI.patrolPost != Vector3.zero &&
                    distToPost > kPostReturnRadius &&
                    !hmAI.moveToTrying)
                {
                    // Log the moment the return move STARTS (a fresh TryToMove). While that move is
                    // still in flight (moveToTrying) this branch is skipped, so it does not spam per run.
                    Jotunn.Logger.LogInfo("[HirdmandrPatrol] " + hmNPC.m_humanoid.m_name + ": " + distToPost.ToString("0.##") + "m from guard post, walking back to " + hmAI.patrolPost);
                    hmAI.TryToMove(hmAI.patrolPost, 1.5f, 0f, 1f, false, 30f);
                }
            }
        }
        public class NodeIsAlerted : SMNode
        {
            public NodeIsAlerted(PatrolSM psm) : base(psm) { }

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                // Overmatched (HP below the escape floor)? Hand off to the top-level threatened/escape
                // state (#49l). The threshold mirrors ThreatenedSM.kEscapeEnterHP so both agree on what
                // "in danger" means. The top NodePatrol forwards changeTopState up to the top level.
                if (hmNPC != null && hmNPC.m_humanoid != null)
                {
                    float max = hmNPC.m_humanoid.GetMaxHealth();
                    if (max > 0f && hmNPC.m_humanoid.GetHealth() / max < 0.3f)
                    {
                        // parentSM is statically typed as the base StateMachine; the changeTopState
                        // field lives on PatrolSM, so cast. The top NodePatrol forwards it up to the
                        // top level (which owns the "threatened" state).
                        ((PatrolSM)parentSM).changeTopState = "threatened";
                        return;
                    }
                }
                // Threat cleared: return to holding the post (re-assert the patrol point first).
                if (!hmMAI.IsAlerted())
                {
                    hmMAI.SetPatrolPoint(hmAI.patrolPost);
                    parentSM.ChangeState("atPost");
                }
            }
        }
    }
}