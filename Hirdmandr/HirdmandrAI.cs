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

        enum sts
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
        
        public sts lastState = 0;
        public sts curState = 0;
        public sts nextState = 0;
        
        public string className = "HirdmandrAI"
        
        protected virtual void Awake()
        {
            topSM.AddState((int)hmStatesTop.schedule, new NodeSchedule());
            topSM.AddState((int)hmStatesTop.socialize, new NodeSocialize());
            topSM.AddState((int)hmStatesTop.workDay, new NodeWorkDay());
            topSM.AddState((int)hmStatesTop.rest, new NodeRest());
            topSM.AddState((int)hmStatesTop.selfCare, new NodeSelfCare());
            topSM.AddState((int)hmStatesTop.patrol, new NodePatrol());
            topSM.AddState((int)hmStatesTop.depressed, new NodeDepressed());
            topSM.AddState((int)hmStatesTop.runInTerror, new NodeRunInTerror());
            topSM.AddState((int)hmStatesTop.hide, new NodeHide());
            topSM.AddState((int)hmStatesTop.defendHome, new NodeDefendHome());

            m_hmnpc = GetComponent<HirdmandrNPC>();
            m_hmMonsterAI = GetComponent<MonsterAI>();
            
            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f))
        }
        
        protected virtual void Update()
        {
            if (m_hmMonsterAI.IsAlerted() && topSM.curState != (int)hmStatesTop.patrol)
            {
                if (m_hmnpc.m_roleWarrior)
                {
                    topSM.ChangeState(hmStatesTop.defendHome);
                }
                else
                {
                    topSM.ChangeState(hmStatesTop.runInTerror);
                }
            }
        }
              
        public void CheckDepression()
        {
            if (m_hmnpc.m_mentalcontentment < -2500 || m_hmnpc.m_mentalcontentment < m_mentalstress)
            {
                if (topSM.curState != (int)hmStatesTop.depressed && topSM.curState != (int)hmStatesTop.selfCare)
                {
                    topSM.ChangeState((int)hmStatesTop.depressed);
                }
            }
            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f))
        }
        
        // Create Nodes
        
        public class NodeSchedule : SMNode
        {
            public void RunState()
            {
                if (m_hmnpc.m_roleArtisan) {
                    if (Game.ToD >= 0.2483 && Game.ToD < 0.3333) { topSM.ChangeState((int)hmStatesTop.socialize); }
                    else if (Game.ToD >= 0.3333 && Game.ToD < 0.7083) { topSM.ChangeState((int)hmStatesTop.workDay); }
                    else if (Game.ToD >= 0.7083 && Game.ToD < 0.909) { topSM.ChangeState((int)hmStatesTop.socialize); }
                    else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState((int)hmStatesTop.rest); }
                    else if (Game.ToD >= 0.2083 && Game.ToD < 0.2483) { topSM.ChangeState((int)hmStatesTop.selfCare); }
                }
                if (m_hmnpc.m_roleWarrior && m_hmnpc.m_jobThegn)
                {
                    if (m_hmnpc.m_thegnDayshift)
                    {
                        if (Game.ToD >= 0.2083 && Game.ToD < 0.2608) { topSM.ChangeState((int)hmStatesTop.selfCare); }
                        else if (Game.ToD >= 0.2608 && Game.ToD < 0.7083) { topSM.ChangeState((int)hmStatesTop.patrol); }
                        else if (Game.ToD >= 0.7083 && Game.ToD < 0.909) { topSM.ChangeState((int)hmStatesTop.socialize); }
                        else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState((int)hmStatesTop.rest); }
                    }
                    else
                    {
                        if (Game.ToD >= 0.6083 && Game.ToD < 0.6983) { topSM.ChangeState((int)hmStatesTop.selfCare); }
                        else if (Game.ToD >= 0.6983 || Game.ToD < 0.2708) { topSM.ChangeState((int)hmStatesTop.patrol); }
                        else if (Game.ToD >= 0.2708 && Game.ToD < 0.3333) { topSM.ChangeState((int)hmStatesTop.socialize); }
                        else if (Game.ToD >= 0.3333 && Game.ToD < 0.6083) { topSM.ChangeState((int)hmStatesTop.rest); }
                    }
                }
                else if (m_hmnpc.m_roleWarrior && m_hmnpc.m_jobHimthiki)
                {
                    if (Game.ToD >= 0.2483 && Game.ToD < 0.909) { topSM.ChangeState((int)hmStatesTop.socialize); }
                    else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState((int)hmStatesTop.rest); }
                    else if (Game.ToD >= 0.2083 && Game.ToD < 0.2483) { topSM.ChangeState((int)hmStatesTop.selfCare); }
                }
            }
        }
        
        public class NodeSocialize : SMNode
        {
            public enum socialStates
            {
                findMeetPoint,
                goMeetPoint,
                goIdlePoint,
                atIdlePoint,
                setupSocialize,
                startSocialize
            };
            
            public SocializeSM socialStateMachine = new SocializeSM(socialStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == (int)hmStatesTop.schedule || aState == (int)hmStatesTop.workDay)
                {
                    socialStateMachine.ChangeState(socialStates.findMeetPoint);
                }
            }
            public void RunState()
            {
                socialStateMachine.Evaluate()
                if (socialStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(socialStateMachine.changeTopState);
                    socialStateMachine.changeTopState = -1;
                }
            }
        }
        public class NodeWorkDay : SMNode
        {
            public enum workStates
            {
                resetArtJob,
                setupArtJob,
                goArtJob,
                doJob
            };
            
            public workStateMachine = new WorkDaySM(workStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == hmStatesTop.schedule || aState == hmStatesTop.socialize)
                {
                    workStateMachine.ChangeState(workStates.resetArtJob);
                }
            }
            public void RunState()
            {
                workStateMachine.Evaluate()
                if (workStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(workStateMachine.changeTopState);
                    workStateMachine.changeTopState = -1;
                }
            }
        }
        public class NodeRest : SMNode
        {
            public enum restStates
            {
                findBed,
                goBed,
                atBed
            };
            
            public restStateMachine = new RestSM(restStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == hmStatesTop.schedule || aState == hmStatesTop.socialize || aState == hmStatesTop.patrol)
                {
                    restStateMachine.ChangeState(restStates.findBed);
                }
            }
            public void RunState()
            {
                restStateMachine.Evaluate()
                if (restStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(restStateMachine.changeTopState);
                    restStateMachine.changeTopState = -1;
                }
            }
        }
        public class NodeSelfCare : SMNode
        {
            public enum selfCareStates
            {
                findFood,
                goFood,
                atFood
            };
            
            public selfCareStateMachine = new SelfCareSM(selfCareStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == hmStatesTop.schedule || aState == hmStatesTop.rest || aState == hmStatesTop.depressed || aState == hmStatesTop.hide)
                {
                    selfCareStateMachine.ChangeState(selfCareStates.findFood);
                }
            }
            public void RunState()
            {
                selfCareStateMachine.Evaluate()
                if (selfCareStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(selfCareStateMachine.changeTopState);
                    selfCareStateMachine.changeTopState = -1;
                }
            }
        }
        public class NodePatrol : SMNode
        {
            public enum patrolStates
            {
                setupPatrol,
                goPost,
                atPost,
                isAlerted
            };
            
            public patrolStateMachine = new PatrolSM(patrolStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == hmStatesTop.schedule)
                {
                    patrolStateMachine.ChangeState(patrolStates.setupPatrol);
                }
            }
            public void RunState()
            {
                patrolStateMachine.Evaluate()
                if (patrolStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(patrolStateMachine.changeTopState);
                    patrolStateMachine.changeTopState = -1;
                }
            }
        }
        public class NodeDepressed : SMNode
        {
            public enum depressedStates
            {
                startDepressed,
                findComfort,
                goComfort,
                whine
            };
            
            public depressedStateMachine = new DepressedSM(depressedStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == hmStatesTop.schedule)
                {
                    depressedStateMachine.ChangeState(depressedStates.startDepressed);
                }
            }
            public void RunState()
            {
                depressedStateMachine.Evaluate()
                if (depressedStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(depressedStateMachine.changeTopState);
                    depressedStateMachine.changeTopState = -1;
                }
            }
        }
        public class NodeRunInTerror : SMNode
        {
            public enum terrorStates
            {
                callForHelp,
                escapeHelp,
                escapeAny,
                panic
            };
            
            public terrorStateMachine = new RunInTerrorSM(terrorStates);
            
            public void EnterFrom(int aState)
            {
                terrorStateMachine.ChangeState(terrorStates.callForHelp);
            }
            public void RunState()
            {
                terrorStateMachine.Evaluate()
                if (terrorStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(terrorStateMachine.changeTopState);
                    terrorStateMachine.changeTopState = -1;
                }
            }
        }
        public class NodeHide : SMNode
        {
            public enum hideStates
            {
                findSafe,
                goSafe,
                atSafe
            };
            
            public hideStateMachine = new HideSM(hideStates);
            
            public void EnterFrom(int aState)
            {
                hideStateMachine.ChangeState(hideStates.findSafe);
            }
            public void RunState()
            {
                hideStateMachine.Evaluate()
                if (hideStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(hideStateMachine.changeTopState);
                    hideStateMachine.changeTopState = -1;
                }
            }
        }
        public class NodeDefendHome : SMNode
        {
            public enum defendHomeStates
            {
                isAlerted,
                findNeedsHelp,
                goNeedsHelp,
                findThreat,
                goThreat,
                caution
            };
            
            public defendHomeStateMachine = new DefendHomeSM(defendHomeStates);
            
            public void EnterFrom(int aState)
            {
                if (m_hmMonsterAI.IsAlerted())
                {
                    defendHomeStateMachine.ChangeState(defendHomeStates.isAlerted);
                }
                else
                {
                    defendHomeStateMachine.ChangeState(defendHomeStates.findNeedsHelp);
                }
            }
            public void RunState()
            {
                defendHomeStateMachine.Evaluate()
                if (defendHomeStateMachine.changeTopState > -1)
                {
                    topSM.ChangeState(defendHomeStateMachine.changeTopState);
                    defendHomeStateMachine.changeTopState = -1;
                }
            }
        }
    }
}
