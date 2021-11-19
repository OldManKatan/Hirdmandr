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
    [Serializable]
    public class HirdmandrAI : MonoBehaviour
    {
        public HirdmandrNPC m_hmnpc;
        public MonsterAI m_hmMonsterAI;
        public long m_nextDepressionUpdate = 0;

        public TopLevelSM topSM = new TopLevelSM();

        public string className = "HirdmandrAI";

        protected virtual void Awake()
        {
            m_hmnpc = GetComponent<HirdmandrNPC>();
            m_hmMonsterAI = GetComponent<MonsterAI>();
            topSM.hmNPC = m_hmnpc;
            topSM.hmMonAI = m_hmMonsterAI;

            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f));
            InvokeRepeating("EvalutateSM", 30f, 3f);
        }

        protected virtual void Update()
        {
            if (m_hmMonsterAI.IsAlerted() && topSM.curState != topSM.StateInt("patrol"))
            {
                if (m_hmnpc.m_roleWarrior)
                {
                    topSM.ChangeState(topSM.StateInt("defendHome"));
                }
                else
                {
                    topSM.ChangeState(topSM.StateInt("runInTerror"));
                }
            }
        }

        public void CheckDepression()
        {
            if (m_hmnpc.m_mentalcontentment < -2500 || m_hmnpc.m_mentalcontentment < m_hmnpc.m_mentalstress)
            {
                if (topSM.curState != topSM.StateInt("depressed") && topSM.curState != topSM.StateInt("selfCare"))
                {
                    topSM.ChangeState("depressed");
                }
            }
            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f));
        }

        public void EvaluateSM()
        {
            topSM.Evaluate();
        }
    
        public class TopLevelSM : StateMachine
        {
            public HirdmandrNPC hmNPC;
            public MonsterAI hmMonAI;

            public TopLevelSM()
            {

                var allStates = new List<string>()
                {
                    "schedule",
                    "socialize",
                    "workDay",
                    "rest",
                    "selfCare",
                    "patrol",
                    "depressed",
                    "runInTerror",
                    "hide",
                    "defendHome"
                };

                AddState("schedule", new NodeSchedule(this));
                AddState("socialize", new NodeSocialize(this));
                AddState("workDay", new NodeWorkDay(this));
                AddState("rest", new NodeRest(this));
                AddState("selfCare", new NodeSelfCare(this));
                AddState("patrol", new NodePatrol(this));
                AddState("depressed", new NodeDepressed(this));
                AddState("runInTerror", new NodeRunInTerror(this));
                AddState("hide", new NodeHide(this));
                AddState("defendHome", new NodeDefendHome(this));
            }
        
            // Create Nodes

            public class NodeSchedule : SMNode
            {
                public TopLevelSM parentSM;

                public NodeSchedule(TopLevelSM psm) 
                {
                    parentSM = psm;
                }

                new public void RunState()
                {
                    float ToD = EnvMan.instance.m_smoothDayFraction;
                    if (parentSM.hmNPC.m_roleArtisan) {
                        if (ToD >= 0.2483 && ToD < 0.3333) { parentSM.ChangeState("socialize"); }
                        else if (ToD >= 0.3333 && ToD < 0.7083) { parentSM.ChangeState("workDay"); }
                        else if (ToD >= 0.7083 && ToD < 0.909) { parentSM.ChangeState("socialize"); }
                        else if (ToD >= 0.909 || ToD < 0.2083) { parentSM.ChangeState("rest"); }
                        else if (ToD >= 0.2083 && ToD < 0.2483) { parentSM.ChangeState("selfCare"); }
                    }
                    if (parentSM.hmNPC.m_roleWarrior && parentSM.hmNPC.m_jobThegn)
                    {
                        if (parentSM.hmNPC.m_thegnDayshift)
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
                    else if (parentSM.hmNPC.m_roleWarrior && parentSM.hmNPC.m_jobHimthiki)
                    {
                        if (ToD >= 0.2483 && ToD < 0.909) { parentSM.ChangeState("socialize"); }
                        else if (ToD >= 0.909 || ToD < 0.2083) { parentSM.ChangeState("rest"); }
                        else if (ToD >= 0.2083 && ToD < 0.2483) { parentSM.ChangeState("selfCare"); }
                    }
                }
            }

            public class NodeSocialize : SMNode
            {
                public SocializeSM socialStateMachine;
                public TopLevelSM parentSM;

                public NodeSocialize(TopLevelSM psm)
                {
                    parentSM = psm;
                    socialStateMachine = new SocializeSM();
                }


                new public void EnterFrom(int aState)
                {
                    if (aState == parentSM.StateInt("schedule") || aState == parentSM.StateInt("workDay"))
                    {
                        socialStateMachine.ChangeState("findMeetPoint");
                    }
                }
                new public void RunState()
                {
                    socialStateMachine.Evaluate();
                    if (socialStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(socialStateMachine.changeTopState);
                        socialStateMachine.changeTopState = "";
                    }
                }
            }
            public class NodeWorkDay : SMNode
            {
                public WorkDaySM workStateMachine;
                public TopLevelSM parentSM;

                public NodeWorkDay(TopLevelSM psm)
                {
                    parentSM = psm;
                    workStateMachine = new WorkDaySM();
                }

                new public void EnterFrom(int aState)
                {
                    if (aState == parentSM.StateInt("schedule") || aState == parentSM.StateInt("socialize"))
                    {
                        workStateMachine.ChangeState("resetArtJob");
                    }
                }
                new public void RunState()
                {
                    workStateMachine.Evaluate();
                    if (workStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(workStateMachine.changeTopState);
                        workStateMachine.changeTopState = "";
                    }
                }
            }
            public class NodeRest : SMNode
            {
                public RestSM restStateMachine;
                public TopLevelSM parentSM;

                public NodeRest(TopLevelSM psm)
                {
                    parentSM = psm;
                    restStateMachine = new RestSM();
                }

                new public void EnterFrom(int aState)
                {
                    if (aState == parentSM.StateInt("schedule") || aState == parentSM.StateInt("socialize") || aState == parentSM.StateInt("patrol"))
                    {
                        restStateMachine.ChangeState("findBed");
                    }
                }
                new public void RunState()
                {
                    restStateMachine.Evaluate();
                    if (restStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(restStateMachine.changeTopState);
                        restStateMachine.changeTopState = "";
                    }
                }
            }
            public class NodeSelfCare : SMNode
            {
                public SelfCareSM selfCareStateMachine;
                public TopLevelSM parentSM;

                public NodeSelfCare(TopLevelSM psm)
                {
                    parentSM = psm;
                    selfCareStateMachine = new SelfCareSM();
                }

                new public void EnterFrom(int aState)
                {
                    if (aState == parentSM.StateInt("schedule") || aState == parentSM.StateInt("rest") || aState == parentSM.StateInt("depressed") || aState == parentSM.StateInt("hide"))
                    {
                        selfCareStateMachine.ChangeState("findFood");
                    }
                }
                new public void RunState()
                {
                    selfCareStateMachine.Evaluate();
                    if (selfCareStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(selfCareStateMachine.changeTopState);
                        selfCareStateMachine.changeTopState = "";
                    }
                }
            }
            public class NodePatrol : SMNode
            {
                public PatrolSM patrolStateMachine;
                public TopLevelSM parentSM;

                public NodePatrol(TopLevelSM psm)
                {
                    parentSM = psm;
                    patrolStateMachine = new PatrolSM();
                }

                new public void EnterFrom(int aState)
                {
                    if (aState == parentSM.StateInt("schedule"))
                    {
                        patrolStateMachine.ChangeState("setupPatrol");
                    }
                }
                new public void RunState()
                {
                    patrolStateMachine.Evaluate();
                    if (patrolStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(patrolStateMachine.changeTopState);
                        patrolStateMachine.changeTopState = "";
                    }
                }
            }
            public class NodeDepressed : SMNode
            {
                public DepressedSM depressedStateMachine;
                public TopLevelSM parentSM;

                public NodeDepressed(TopLevelSM psm)
                {
                    parentSM = psm;
                    depressedStateMachine = new DepressedSM();
                }

                new public void EnterFrom(int aState)
                {
                    if (aState == parentSM.StateInt("schedule"))
                    {
                        depressedStateMachine.ChangeState("startDepressed");
                    }
                }
                new public void RunState()
                {
                    depressedStateMachine.Evaluate();
                    if (depressedStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(depressedStateMachine.changeTopState);
                        depressedStateMachine.changeTopState = "";
                    }
                }
            }
            public class NodeRunInTerror : SMNode
            {
                public RunInTerrorSM terrorStateMachine;
                public TopLevelSM parentSM;

                public NodeRunInTerror(TopLevelSM psm)
                {
                    parentSM = psm;
                    terrorStateMachine = new RunInTerrorSM();
                }

                new public void EnterFrom(int aState)
                {
                    terrorStateMachine.ChangeState("callForHelp");
                }
                new public void RunState()
                {
                    terrorStateMachine.Evaluate();
                    if (terrorStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(terrorStateMachine.changeTopState);
                        terrorStateMachine.changeTopState = "";
                    }
                }
            }
            public class NodeHide : SMNode
            {
                public HideSM hideStateMachine;
                public TopLevelSM parentSM;

                public NodeHide(TopLevelSM psm)
                {
                    parentSM = psm;
                    hideStateMachine = new HideSM();
                }

                new public void EnterFrom(int aState)
                {
                    hideStateMachine.ChangeState("findSafe");
                }
                new public void RunState()
                {
                    hideStateMachine.Evaluate();
                    if (hideStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(hideStateMachine.changeTopState);
                        hideStateMachine.changeTopState = "";
                    }
                }
            }
            public class NodeDefendHome : SMNode
            {
                public DefendHomeSM defendHomeStateMachine;
                public TopLevelSM parentSM;

                public NodeDefendHome(TopLevelSM psm)
                {
                    parentSM = psm;
                    defendHomeStateMachine = new DefendHomeSM();
                }

                new public void EnterFrom(int aState)
                {
                    if (parentSM.hmMonAI.IsAlerted())
                    {
                        defendHomeStateMachine.ChangeState("isAlerted");
                    }
                    else
                    {
                        defendHomeStateMachine.ChangeState("findNeedsHelp");
                    }
                }
                new public void RunState()
                {
                    defendHomeStateMachine.Evaluate();
                    if (defendHomeStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(defendHomeStateMachine.changeTopState);
                        defendHomeStateMachine.changeTopState = "";
                    }
                }
            }
        }
    }
}
