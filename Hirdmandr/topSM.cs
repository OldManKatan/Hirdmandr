// Hirdmandr
// a Valheim mod skeleton using J�tunn
// 
// File:    Hirdmandr.cs
// Project: Hirdmandr

using BepInEx;
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
    public class TopLevelSM : StateMachine
    {
        public TopLevelSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            // All top-level states (for human readers; the AddState calls below are the
            // source of truth):
            //   "schedule"   "socialize"   "workDay"   "rest"
            //   "patrol"     "threatened"  "defend"    "follow"

            AddState("schedule", new NodeSchedule(this));
            AddState("socialize", new NodeSocialize(this));
            AddState("workDay", new NodeWorkDay(this));
            AddState("rest", new NodeRest(this));
            AddState("patrol", new NodePatrol(this));
            AddState("threatened", new NodeThreatened(this));
            AddState("defend", new NodeDefend(this));
            AddState("follow", new NodeFollow(this));
        }

        // Create Nodes

        public class NodeSchedule : SMNode
        {

            public NodeSchedule(TopLevelSM psm) : base(psm) { }

            public override void RunState()
            {
                Jotunn.Logger.LogInfo("Starting NodeSchedule.RunState()");
                float ToD = EnvMan.instance.m_smoothDayFraction;
                // Single router: all role/shift windows live in HMSchedule, reached via
                // GetStateBySchedule.
                parentSM.ChangeState(hmNPC.GetStateBySchedule(ToD));
            }
        }

        public class NodeSocialize : SMNode
        {
            public SocializeSM socialStateMachine;

            public NodeSocialize(TopLevelSM psm) : base(psm)
            {
                socialStateMachine = new SocializeSM(hmAI);
            }

            public override void EnterFrom(int aState)
            {
                if (aState == parentSM.StateInt("schedule") || aState == parentSM.StateInt("workDay"))
                {
                    socialStateMachine.ChangeState("findMeetPoint");
                }
            }
            public override void RunState()
            {
                socialStateMachine.Evaluate();
                if (socialStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(socialStateMachine.changeTopState);
                    socialStateMachine.changeTopState = "";
                }

                // The murmur lives in the SocializeSM "socialize" steady state (it calls
                // HMSocialize.MaybeTalk); this node only drives the sub-SM + the schedule check.

                // Schedule check: leave for the router whenever the schedule no longer targets
                // this state (stateIndexes[curState] is this node's own name while RunState runs).
                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (hmNPC.GetStateBySchedule(ToD) != parentSM.stateIndexes[parentSM.curState]) { parentSM.ChangeState("schedule"); }
            }
        }
        public class NodeWorkDay : SMNode
        {
            public WorkDaySM workStateMachine;

            public NodeWorkDay(TopLevelSM psm) : base(psm)
            {
                workStateMachine = new WorkDaySM(hmAI);
            }

            public override void EnterFrom(int aState)
            {
                workStateMachine.ChangeState("resetArtJob");
            }

            public override void ExitTo(int aState)
            {
                Jotunn.Logger.LogWarning("Exiting work day, Unclaiming assets...");
                if (hmAI.workJobSite)
                {
                    hmAI.workJobSite.GetComponent<HirdmandrChest>().UnClaim(hmAI.curJob, hmAI.m_znetv.GetZDO().m_uid);
                }
                hmAI.curJob = "";
                hmAI.workJobSite = null;
                // Leaving all artisan work (to rest/patrol/threatened/socialize/schedule): put the
                // combat equipment back on. Per-job gear (e.g. the Mender's hammer) is decided inside
                // the WorkDay state machine; this is the single "back to combat gear" boundary.
                if (hmAI.m_hmnpc != null)
                {
                    // The farmer's cultivator is non-spawnable (player-provided), so unlike the Mender's
                    // spawned hammer it must go back to the chest it was taken from. No-op if the
                    // farmer isn't holding one.
                    hmAI.m_hmnpc.ReleaseCultivator(hmAI.farmerSourceChest);
                    hmAI.farmerSourceChest = null;
                    hmAI.farmerHoldsCultivator = false;
                    hmAI.m_hmnpc.EquipBest();
                }
            }

            public override void RunState()
            {
                workStateMachine.Evaluate();
                if (workStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(workStateMachine.changeTopState);
                    workStateMachine.changeTopState = "";
                }

                // Schedule check: leave for the router whenever the schedule no longer targets
                // this state (stateIndexes[curState] is this node's own name while RunState runs).
                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (hmNPC.GetStateBySchedule(ToD) != parentSM.stateIndexes[parentSM.curState]) { parentSM.ChangeState("schedule"); }
            }
        }
        public class NodeRest : SMNode
        {
            public RestSM restStateMachine;

            public NodeRest(TopLevelSM psm) : base(psm)
            {
                restStateMachine = new RestSM(hmAI);
            }

            public override void EnterFrom(int aState)
            {
                if (aState == parentSM.StateInt("schedule") || aState == parentSM.StateInt("socialize") || aState == parentSM.StateInt("patrol"))
                {
                    restStateMachine.ChangeState("findBed");
                }
            }

            public override void ExitTo(int aState)
            {
                var human = hmAI.GetComponent<NPCPlayerClone>();
                human.m_sleeping = false;
                human.AttachStop();
                // #54: AttachStop leaves the NPC standing on the bed collider, where it cannot
                // path anywhere. Step it off to a clear floor spot beside the bed (no-op if it is
                // already off the bed or no open spot is found).
                hmAI.m_hmnpc.ScheduleStepOffBed("rest-exit (topSM.NodeRest.ExitTo)", 2f);
            }

            public override void RunState()
            {
                restStateMachine.Evaluate();
                if (restStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(restStateMachine.changeTopState);
                    restStateMachine.changeTopState = "";
                }

                // Schedule check: leave for the router whenever the schedule no longer targets
                // this state (stateIndexes[curState] is this node's own name while RunState runs).
                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (hmNPC.GetStateBySchedule(ToD) != parentSM.stateIndexes[parentSM.curState]) { parentSM.ChangeState("schedule"); }
            }
        }

        public class NodePatrol : SMNode
        {
            public PatrolSM patrolStateMachine;

            public NodePatrol(TopLevelSM psm) : base(psm)
            {
                patrolStateMachine = new PatrolSM(hmAI);
            }

            public override void EnterFrom(int aState)
            {
                if (aState == parentSM.StateInt("schedule"))
                {
                    patrolStateMachine.ChangeState("setupPatrol");
                }

                // Socialize: on guard duty — queue a "guarded" moment (dedup keeps it to one).
                hmNPC.m_socialize.EnqueueGuarded();
            }
            public override void RunState()
            {
                patrolStateMachine.Evaluate();
                if (patrolStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(patrolStateMachine.changeTopState);
                    patrolStateMachine.changeTopState = "";
                }

                // Schedule check: leave for the router whenever the schedule no longer targets
                // this state (stateIndexes[curState] is this node's own name while RunState runs).
                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (hmNPC.GetStateBySchedule(ToD) != parentSM.stateIndexes[parentSM.curState]) { parentSM.ChangeState("schedule"); }
            }
        }
        public class NodeThreatened : SMNode
        {
            public ThreatenedSM terrorStateMachine;

            public NodeThreatened(TopLevelSM psm) : base(psm)
            {
                terrorStateMachine = new ThreatenedSM(hmAI);
            }

            public override void EnterFrom(int aState)
            {
                terrorStateMachine.ChangeState("callForHelp");

                // Socialize: a fight just started — queue a "combat" moment. Carry the threat's
                // localized name (if identifiable) and the melee/range persona (from the NPC's
                // higher combat skill); the line is said later, at the fire.
                Character threat = null;
                if (hmMAI != null && hmMAI.m_character != null)
                {
                    threat = BaseAI.FindClosestEnemy(hmMAI.m_character, hmNPC.transform.position, 50f);
                }
                string monsterName = (threat != null && !threat.IsDead()) ? threat.GetHoverName() : "";
                string persona = (hmNPC.m_skills != null && hmNPC.m_skills.GetSkill("melee") < hmNPC.m_skills.GetSkill("range"))
                    ? "range" : "melee";
                hmNPC.m_socialize.EnqueueCombat(monsterName, persona);
            }
            public override void RunState()
            {
                terrorStateMachine.Evaluate();
                if (terrorStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(terrorStateMachine.changeTopState);
                    terrorStateMachine.changeTopState = "";
                }

                if (!hmAI.m_hmMonsterAI.IsAlerted())
                {
                    // Threat cleared. If we were following the player, resume the follow state;
                    // otherwise return to the normal schedule.
                    parentSM.ChangeState(hmAI.m_hmnpc.m_following ? "follow" : "schedule");
                }
            }
        }

        public class NodeDefend : SMNode
        {
            public NodeDefend(TopLevelSM psm) : base(psm) { }

            public override void EnterFrom(int aState)
            {
                // Start moving to the distressed party member right away. UpdateMoving (per-frame)
                // does the actual stepping from here on; we only (re)issue the destination.
                HirdmandrNPC t = hmAI.m_defendTarget;
                if (t != null)
                {
                    hmAI.TryToMove(t.transform.position, 3f, 1f, 3f, false, 30f);
                }
            }

            public override void RunState()
            {
                // Re-anchor each tick: the distressed NPC may be moving (fighting / fleeing).
                HirdmandrNPC t = hmAI.m_defendTarget;
                if (t == null)
                {
                    t = hmAI.FindDefendTarget();
                    hmAI.m_defendTarget = t;
                }
                if (t == null)
                {
                    // No one in distress anymore → resume the normal schedule.
                    hmAI.m_defendTarget = null;
                    parentSM.ChangeState("schedule");
                    return;
                }
                hmAI.TryToMove(t.transform.position, 3f, 1f, 3f, false, 30f);
            }
        }

        public class NodeFollow : SMNode
        {
            public NodeFollow(TopLevelSM psm) : base(psm)
            {
            }

            public override void EnterFrom(int aState)
            {
                // Entering follow always starts a fresh gather pick session (any in-flight pick
                // from before - e.g. interrupted by threat - is dropped, including its move and
                // pending sweep).
                hmAI.ClearGatherSession();
            }
            public override void RunState()
            {
                // If follow was stopped (manually, or by the owner auto-stop) while we were
                // parked here, return to the normal schedule.
                if (!hmAI.m_hmnpc.m_following)
                {
                    parentSM.ChangeState("schedule");
                    return;
                }
                // Gather (Himthiki Gatherer job): the pick session runs as a sub-behavior of
                // follow (S5). A no-op unless the session gates hold (GatherSessionGatesHold).
                hmAI.GatherPickStep();
            }
        }
    }
}
