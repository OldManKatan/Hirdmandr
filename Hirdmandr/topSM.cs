// Hirdmandr
// a Valheim mod skeleton using Jötunn
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

            var allStates = new List<string>()
            {
                "schedule",
                "socialize",
                "workDay",
                "rest",
                "selfCare",
                "patrol",
                "depressed",
                "threatened"
            };

            AddState("schedule", new NodeSchedule(this));
            AddState("socialize", new NodeSocialize(this));
            AddState("workDay", new NodeWorkDay(this));
            AddState("rest", new NodeRest(this));
            AddState("selfCare", new NodeSelfCare(this));
            AddState("patrol", new NodePatrol(this));
            AddState("depressed", new NodeDepressed(this));
            AddState("threatened", new NodeThreatened(this));
            AddState("himthikiFollow", new NodehimthikiFollow(this));
        }

        // Create Nodes

        public class NodeSchedule : SMNode
        {

            public NodeSchedule(TopLevelSM psm) : base(psm) { }

            public override void RunState()
            {
                Jotunn.Logger.LogInfo("Starting NodeSchedule.RunState()");
                float ToD = EnvMan.instance.m_smoothDayFraction;
                Jotunn.Logger.LogInfo("  Got ToD");
                if (hmNPC.m_roleWarrior && hmNPC.m_jobThegn)
                {
                    Jotunn.Logger.LogInfo("    If Warrior Thegn");
                    if (hmNPC.m_thegnDayshift)
                    {
                        if (ToD >= 0.2083 && ToD < 0.2608) { parentSM.ChangeState("selfCare"); }
                        else if (ToD >= 0.2608 && ToD < 0.7083) { parentSM.ChangeState("patrol"); }
                        else if (ToD >= 0.7083 && ToD < 0.909) { parentSM.ChangeState("socialize"); }
                        else if (ToD >= 0.909 || ToD < 0.2083) { parentSM.ChangeState("rest"); }
                    }
                    else
                    {
                        if (ToD >= 0.6083 && ToD < 0.6983) { parentSM.ChangeState("selfCare"); }
                        else if (ToD >= 0.6983 || ToD < 0.2708) { parentSM.ChangeState("patrol"); }
                        else if (ToD >= 0.2708 && ToD < 0.3333) { parentSM.ChangeState("socialize"); }
                        else if (ToD >= 0.3333 && ToD < 0.6083) { parentSM.ChangeState("rest"); }
                    }
                }
                else if (hmNPC.m_roleWarrior && hmNPC.m_jobHimthiki)
                {
                    Jotunn.Logger.LogInfo("    If Warrior Himthiki");
                    if (ToD >= 0.2483 && ToD < 0.909) { parentSM.ChangeState("socialize"); }
                    else if (ToD >= 0.909 || ToD < 0.2083) { parentSM.ChangeState("rest"); }
                    else if (ToD >= 0.2083 && ToD < 0.2483) { parentSM.ChangeState("selfCare"); }
                }
                else  // Role Artisan or unassigned
                {
                    Jotunn.Logger.LogInfo("    If Artisan");

                    if (ToD >= 0.2483 && ToD < 0.3333) { parentSM.ChangeState("socialize"); }
                    else if (ToD >= 0.3333 && ToD < 0.7083) { parentSM.ChangeState("workDay"); }
                    else if (ToD >= 0.7083 && ToD < 0.909) { parentSM.ChangeState("socialize"); }
                    else if (ToD >= 0.909 || ToD < 0.2083) { parentSM.ChangeState("rest"); }
                    else if (ToD >= 0.2083 && ToD < 0.2483) { parentSM.ChangeState("selfCare"); }
                }
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
                Jotunn.Logger.LogInfo(hmHum.m_name + " is evaluating their AI");

                socialStateMachine.Evaluate();
                if (socialStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(socialStateMachine.changeTopState);
                    socialStateMachine.changeTopState = "";
                }

                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (hmNPC.m_roleWarrior && hmNPC.m_jobThegn)
                {
                    if (hmNPC.m_thegnDayshift)
                    {
                        if (ToD < 0.7083 || ToD >= 0.909) { parentSM.ChangeState("schedule"); }
                    }
                    else
                    {
                        if (ToD < 0.2708 || ToD >= 0.3333) { parentSM.ChangeState("schedule"); }
                    }
                }
                else if (hmNPC.m_roleWarrior && hmNPC.m_jobHimthiki)
                {
                    if (ToD < 0.2483 || ToD >= 0.909) { parentSM.ChangeState("schedule"); }
                }
                else  // Role Artisan or unassigned
                {
                    if ((ToD < 0.2483 || ToD >= 0.3333) && (ToD < 0.7083 || ToD > 0.909)) { parentSM.ChangeState("schedule"); }
                }
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
            }

            public override void RunState()
            {
                workStateMachine.Evaluate();
                if (workStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(workStateMachine.changeTopState);
                    workStateMachine.changeTopState = "";
                }

                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (ToD < 0.3333 || ToD >= 0.7083) { parentSM.ChangeState("schedule"); }
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
                if (aState == parentSM.StateInt("schedule"))
                {
                    if (hmAI.restBedHMBed)
                    {
                        hmNPC.m_thoughts.AddThought(HMThoughts.tType.restedComfort, hmAI.restBedHMBed.GetComfortAtBed() * 50, "", Time.time + 3600);
                    }
                }
            }

            public override void RunState()
            {
                restStateMachine.Evaluate();
                if (restStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(restStateMachine.changeTopState);
                    restStateMachine.changeTopState = "";
                }

                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (hmNPC.m_roleWarrior && hmNPC.m_jobThegn)
                {
                    if (hmNPC.m_thegnDayshift)
                    {
                        if (ToD < 0.909 && ToD >= 0.2083) { parentSM.ChangeState("schedule"); }
                    }
                    else
                    {
                        if (ToD < 0.3333 || ToD >= 0.6083) { parentSM.ChangeState("schedule"); }
                    }
                }
                else if (hmNPC.m_roleWarrior && hmNPC.m_jobHimthiki)
                {
                    if (ToD < 0.909 && ToD >= 0.2083) { parentSM.ChangeState("schedule"); }
                }
                else  // Role Artisan or unassigned
                {
                    if (ToD < 0.909 && ToD >= 0.2083) { parentSM.ChangeState("schedule"); }
                }
            }
        }

        public class NodeSelfCare : SMNode
        {
            public SelfCareSM selfCareStateMachine;

            public NodeSelfCare(TopLevelSM psm) : base(psm)
            {
                selfCareStateMachine = new SelfCareSM(hmAI);
            }

            public override void EnterFrom(int aState)
            {
                if (aState == parentSM.StateInt("schedule") || aState == parentSM.StateInt("rest") || aState == parentSM.StateInt("depressed") || aState == parentSM.StateInt("hide"))
                {
                    selfCareStateMachine.ChangeState("findFood");
                }
            }
            public override void RunState()
            {
                selfCareStateMachine.Evaluate();
                if (selfCareStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(selfCareStateMachine.changeTopState);
                    selfCareStateMachine.changeTopState = "";
                }

                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (hmNPC.m_roleWarrior && hmNPC.m_jobThegn)
                {
                    if (hmNPC.m_thegnDayshift)
                    {
                        if (ToD < 0.2083 || ToD >= 0.2608) { parentSM.ChangeState("schedule"); }

                    }
                    else
                    {
                        if (ToD < 0.6083 || ToD >= 0.6983) { parentSM.ChangeState("schedule"); }
                    }
                }
                else if (hmNPC.m_roleWarrior && hmNPC.m_jobHimthiki)
                {
                    if (ToD < 0.2083 || ToD >= 0.2483) { parentSM.ChangeState("schedule"); }
                }
                else  // Role Artisan or unassigned
                {
                    if (ToD < 0.2083 || ToD >= 0.2483) { parentSM.ChangeState("schedule"); }
                }
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
            }
            public override void RunState()
            {
                patrolStateMachine.Evaluate();
                if (patrolStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(patrolStateMachine.changeTopState);
                    patrolStateMachine.changeTopState = "";
                }

                float ToD = EnvMan.instance.m_smoothDayFraction;
                if (hmNPC.m_roleWarrior && hmNPC.m_jobThegn)
                {
                    if (hmNPC.m_thegnDayshift)
                    {
                        if (ToD < 0.2608 || ToD >= 0.7083) { parentSM.ChangeState("schedule"); }
                    }
                    else
                    {
                        if (ToD < 0.6983 && ToD >= 0.2708) { parentSM.ChangeState("schedule"); }
                    }
                }
                else  // Role Artisan or unassigned
                {
                    if (ToD < 0.2608 || ToD >= 0.7083) { parentSM.ChangeState("schedule"); }
                }
            }
        }
        public class NodeDepressed : SMNode
        {
            public DepressedSM depressedStateMachine;

            public NodeDepressed(TopLevelSM psm) : base(psm)
            {
                depressedStateMachine = new DepressedSM(hmAI);
            }

            public override void EnterFrom(int aState)
            {
                if (aState == parentSM.StateInt("schedule"))
                {
                    depressedStateMachine.ChangeState("startDepressed");
                }
            }
            public override void RunState()
            {
                depressedStateMachine.Evaluate();
                if (depressedStateMachine.changeTopState.Length > 0)
                {
                    parentSM.ChangeState(depressedStateMachine.changeTopState);
                    depressedStateMachine.changeTopState = "";
                }

                parentSM.ChangeState("schedule");
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
                    parentSM.ChangeState("schedule");
                }
            }
        }

        public class NodehimthikiFollow : SMNode
        {
            public NodehimthikiFollow(TopLevelSM psm) : base(psm)
            {
            }

            public override void EnterFrom(int aState)
            {
                hmAI.moveToTrying = false;
            }
            public override void RunState()
            {
                if (!hmAI.m_hmnpc.m_himthikiFollowing)
                {
                    parentSM.ChangeState("schedule");
                }
            }
        }
    }
}
