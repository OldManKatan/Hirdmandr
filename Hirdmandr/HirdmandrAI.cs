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
        
        public string className = "HirdmandrAI"
        
        protected virtual void Awake()
        {
            m_hmnpc = GetComponent<HirdmandrNPC>();
            m_hmMonsterAI = GetComponent<MonsterAI>();
            
            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f));
            InvokeRepeating("EvalutateSM", 30f, 3f);
        }
        
        protected virtual void Update()
        {
            if (m_hmMonsterAI.IsAlerted() && topSM.curState != topSM.sts.patrol)
            {
                if (m_hmnpc.m_roleWarrior)
                {
                    topSM.ChangeState(topSM.sts.defendHome);
                }
                else
                {
                    topSM.ChangeState(topSM.sts.runInTerror);
                }
            }
        }
        
        public void CheckDepression()
        {
            if (m_hmnpc.m_mentalcontentment < -2500 || m_hmnpc.m_mentalcontentment < m_mentalstress)
            {
                if (topSM.curState != topSM.sts.depressed && topSM.curState != topSM.sts.selfCare)
                {
                    topSM.ChangeState(topSM.sts.depressed);
                }
            }
            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f))
        }
        
        public void EvaluateSM()
        {
            topSM.Evaluate()
        }
        
        public class TopLevelSM : StateMachine
        {
            TopLevelSM()
            {
                public enum sts
                {
                    schedule,
                    socialize,
                    workDay,
                    rest,
                    selfCare,
                    patrol,
                    depressed,
                    runInTerror,
                    hide,
                    defendHome
                };
                
                AddState(sts.schedule, new NodeSchedule());
                AddState(sts.socialize, new NodeSocialize());
                AddState(sts.workDay, new NodeWorkDay());
                AddState(sts.rest, new NodeRest());
                AddState(sts.selfCare, new NodeSelfCare());
                AddState(sts.patrol, new NodePatrol());
                AddState(sts.depressed, new NodeDepressed());
                AddState(sts.runInTerror, new NodeRunInTerror());
                AddState(sts.hide, new NodeHide());
                AddState(sts.defendHome, new NodeDefendHome());

                InitializeAtState(sts.schedule);
            }
        
            // Create Nodes

            public class NodeSchedule : SMNode
            {
                public void RunState()
                {
                    if (m_hmnpc.m_roleArtisan) {
                        if (Game.ToD >= 0.2483 && Game.ToD < 0.3333) { topSM.ChangeState(sts.socialize); }
                        else if (Game.ToD >= 0.3333 && Game.ToD < 0.7083) { topSM.ChangeState(sts.workDay); }
                        else if (Game.ToD >= 0.7083 && Game.ToD < 0.909) { topSM.ChangeState(sts.socialize); }
                        else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState(sts.rest); }
                        else if (Game.ToD >= 0.2083 && Game.ToD < 0.2483) { topSM.ChangeState(sts.selfCare); }
                    }
                    if (m_hmnpc.m_roleWarrior && m_hmnpc.m_jobThegn)
                    {
                        if (m_hmnpc.m_thegnDayshift)
                        {
                            if (Game.ToD >= 0.2083 && Game.ToD < 0.2608) { topSM.ChangeState(sts.selfCare); }
                            else if (Game.ToD >= 0.2608 && Game.ToD < 0.7083) { topSM.ChangeState(sts.patrol); }
                            else if (Game.ToD >= 0.7083 && Game.ToD < 0.909) { topSM.ChangeState(sts.socialize); }
                            else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState(sts.rest); }
                        }
                        else
                        {
                            if (Game.ToD >= 0.6083 && Game.ToD < 0.6983) { topSM.ChangeState(sts.selfCare); }
                            else if (Game.ToD >= 0.6983 || Game.ToD < 0.2708) { topSM.ChangeState(sts.patrol); }
                            else if (Game.ToD >= 0.2708 && Game.ToD < 0.3333) { topSM.ChangeState(sts.socialize); }
                            else if (Game.ToD >= 0.3333 && Game.ToD < 0.6083) { topSM.ChangeState(sts.rest); }
                        }
                    }
                    else if (m_hmnpc.m_roleWarrior && m_hmnpc.m_jobHimthiki)
                    {
                        if (Game.ToD >= 0.2483 && Game.ToD < 0.909) { topSM.ChangeState(sts.socialize); }
                        else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState(sts.rest); }
                        else if (Game.ToD >= 0.2083 && Game.ToD < 0.2483) { topSM.ChangeState(sts.selfCare); }
                    }
                }
            }

            public class NodeSocialize : SMNode
            {
                public SocializeSM socialStateMachine = new SocializeSM();
                socialStateMachine.parentSM = topSM;
                
                public void EnterFrom(sts aState)
                {
                    if (aState == sts.schedule || aState == sts.workDay)
                    {
                        socialStateMachine.ChangeState(socialStateMachine.sts.findMeetPoint);
                    }
                }
                public void RunState()
                {
                    socialStateMachine.Evaluate()
                    if (socialStateMachine.changeTopState)
                    {
                        topSM.ChangeState(socialStateMachine.changeTopState);
                        socialStateMachine.changeTopState = null;
                    }
                }
            }
            public class NodeWorkDay : SMNode
            {
                public WorkDaySM workStateMachine = new WorkDaySM();
                workStateMachine.parentSM = topSM;

                public void EnterFrom(sts aState)
                {
                    if (aState == sts.schedule || aState == sts.socialize)
                    {
                        workStateMachine.ChangeState(workStateMachine.workStates.resetArtJob);
                    }
                }
                public void RunState()
                {
                    workStateMachine.Evaluate()
                    if (workStateMachine.changeTopState)
                    {
                        topSM.ChangeState(workStateMachine.changeTopState);
                        workStateMachine.changeTopState = null;
                    }
                }
            }
            public class NodeRest : SMNode
            {
                public RestSM restStateMachine = new RestSM();
                restStateMachine.parentSM = topSM;

                public void EnterFrom(sts aState)
                {
                    if (aState == sts.schedule || aState == sts.socialize || aState == sts.patrol)
                    {
                        restStateMachine.ChangeState(restStateMachine.restStates.findBed);
                    }
                }
                public void RunState()
                {
                    restStateMachine.Evaluate()
                    if (restStateMachine.changeTopState)
                    {
                        topSM.ChangeState(restStateMachine.changeTopState);
                        restStateMachine.changeTopState = null;
                    }
                }
            }
            public class NodeSelfCare : SMNode
            {
                public SelfCareSM selfCareStateMachine = new SelfCareSM(selfCareStates);
                selfCareStateMachine.parentSM = topSM;

                public void EnterFrom(sts aState)
                {
                    if (aState == sts.schedule || aState == sts.rest || aState == sts.depressed || aState == sts.hide)
                    {
                        selfCareStateMachine.ChangeState(selfCareStateMachine.selfCareStates.findFood);
                    }
                }
                public void RunState()
                {
                    selfCareStateMachine.Evaluate()
                    if (selfCareStateMachine.changeTopState)
                    {
                        topSM.ChangeState(selfCareStateMachine.changeTopState);
                        selfCareStateMachine.changeTopState = null;
                    }
                }
            }
            public class NodePatrol : SMNode
            {
                public PatrolSM patrolStateMachine = new PatrolSM();
                patrolStateMachine.parentSM = topSM;

                public void EnterFrom(sts aState)
                {
                    if (aState == sts.schedule)
                    {
                        patrolStateMachine.ChangeState(patrolStateMachine.patrolStates.setupPatrol);
                    }
                }
                public void RunState()
                {
                    patrolStateMachine.Evaluate()
                    if (patrolStateMachine.changeTopState)
                    {
                        topSM.ChangeState(patrolStateMachine.changeTopState);
                        patrolStateMachine.changeTopState = null;
                    }
                }
            }
            public class NodeDepressed : SMNode
            {
                public DepressedSM depressedStateMachine = new DepressedSM();
                depressedStateMachine.parentSM = topSM;

                public void EnterFrom(sts aState)
                {
                    if (aState == sts.schedule)
                    {
                        depressedStateMachine.ChangeState(depressedStateMachine.depressedStates.startDepressed);
                    }
                }
                public void RunState()
                {
                    depressedStateMachine.Evaluate()
                    if (depressedStateMachine.changeTopState)
                    {
                        topSM.ChangeState(depressedStateMachine.changeTopState);
                        depressedStateMachine.changeTopState = null;
                    }
                }
            }
            public class NodeRunInTerror : SMNode
            {
                public RunInTerrorSM terrorStateMachine = new RunInTerrorSM();
                terrorStateMachine.parentSM = topSM;

                public void EnterFrom(sts aState)
                {
                    terrorStateMachine.ChangeState(terrorStateMachine.terrorStates.callForHelp);
                }
                public void RunState()
                {
                    terrorStateMachine.Evaluate()
                    if (terrorStateMachine.changeTopState)
                    {
                        topSM.ChangeState(terrorStateMachine.changeTopState);
                        terrorStateMachine.changeTopState = null;
                    }
                }
            }
            public class NodeHide : SMNode
            {
                public HideSM hideStateMachine = new HideSM();
                hideStateMachine.parentSM = topSM;

                public void EnterFrom(sts aState)
                {
                    hideStateMachine.ChangeState(hideStateMachine.hideStates.findSafe);
                }
                public void RunState()
                {
                    hideStateMachine.Evaluate()
                    if (hideStateMachine.changeTopState)
                    {
                        topSM.ChangeState(hideStateMachine.changeTopState);
                        hideStateMachine.changeTopState = null;
                    }
                }
            }
            public class NodeDefendHome : SMNode
            {
                public DefendHomeSM defendHomeStateMachine = new DefendHomeSM();
                defendHomeStateMachine.parentSM = topSM;

                public void EnterFrom(sts aState)
                {
                    if (m_hmMonsterAI.IsAlerted())
                    {
                        defendHomeStateMachine.ChangeState(defendHomeStateMachine.defendHomeStates.isAlerted);
                    }
                    else
                    {
                        defendHomeStateMachine.ChangeState(defendHomeStateMachine.defendHomeStates.findNeedsHelp);
                    }
                }
                public void RunState()
                {
                    defendHomeStateMachine.Evaluate()
                    if (defendHomeStateMachine.changeTopState)
                    {
                        topSM.ChangeState(defendHomeStateMachine.changeTopState);
                        defendHomeStateMachine.changeTopState = null;
                    }
                }
            }
        }
    }
}
