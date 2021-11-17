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

        public enum hmStatesTop
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
        
        public StateMachine topSM = new StateMachine();
        
        topSM.AddState(hmStatesTop.schedule, new NodeSchedule());
        topSM.AddState(hmStatesTop.socialize, new NodeSocialize());
        topSM.AddState(hmStatesTop.workDay, new NodeWorkDay());
        topSM.AddState(hmStatesTop.rest, new NodeRest());
        topSM.AddState(hmStatesTop.selfCare, new NodeSelfCare());
        topSM.AddState(hmStatesTop.patrol, new NodePatrol());
        topSM.AddState(hmStatesTop.depressed, new NodeDepressed());
        topSM.AddState(hmStatesTop.runInTerror, new NodeRunInTerror());
        topSM.AddState(hmStatesTop.hide, new NodeHide());
        topSM.AddState(hmStatesTop.defendHome, new NodeDefendHome());

        protected virtual void Awake()
        {
            m_hmnpc = GetComponent<HirdmandrNPC>();
            m_hmMonsterAI = GetComponent<MonsterAI>();
            
            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f))
        }
        
        protected virtual void Update()
        {
            if (m_hmMonsterAI.IsAlerted())
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
                if (topSM.curState != hmStatesTop.depressed && topSM.curState != hmStatesTop.selfCare)
                {
                    topSM.ChangeState(hmStatesTop.depressed);
                }
            }
            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f))
       }
        
        // Create Nodes
        
        public class NodeSchedule : HMNode
        {
            public void RunState()
            {
                if (m_hmnpc.m_roleArtisan) {
                    if (Game.ToD >= 0.2483 && Game.ToD < 0.3333) { topSM.ChangeState(hmStatesTop.socialize); }
                    else if (Game.ToD >= 0.3333 && Game.ToD < 0.7083) { topSM.ChangeState(hmStatesTop.workDay); }
                    else if (Game.ToD >= 0.7083 && Game.ToD < 0.909) { topSM.ChangeState(hmStatesTop.socialize); }
                    else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState(hmStatesTop.rest); }
                    else if (Game.ToD >= 0.2083 && Game.ToD < 0.2483) { topSM.ChangeState(hmStatesTop.selfCare); }
                }
                if (m_hmnpc.m_roleWarrior && m_hmnpc.m_jobThegn)
                {
                    if (m_hmnpc.m_thegnDayshift)
                    {
                        if (Game.ToD >= 0.2083 && Game.ToD < 0.2608) { topSM.ChangeState(hmStatesTop.selfCare); }
                        else if (Game.ToD >= 0.2608 && Game.ToD < 0.7083) { topSM.ChangeState(hmStatesTop.patrol); }
                        else if (Game.ToD >= 0.7083 && Game.ToD < 0.909) { topSM.ChangeState(hmStatesTop.socialize); }
                        else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState(hmStatesTop.rest); }
                    }
                    else
                    {
                        if (Game.ToD >= 0.6083 && Game.ToD < 0.6983) { topSM.ChangeState(hmStatesTop.selfCare); }
                        else if (Game.ToD >= 0.6983 || Game.ToD < 0.2708) { topSM.ChangeState(hmStatesTop.patrol); }
                        else if (Game.ToD >= 0.2708 && Game.ToD < 0.3333) { topSM.ChangeState(hmStatesTop.socialize); }
                        else if (Game.ToD >= 0.3333 && Game.ToD < 0.6083) { topSM.ChangeState(hmStatesTop.rest); }
                    }
                }
                else if (m_hmnpc.m_roleWarrior && m_hmnpc.m_jobHimthiki)
                {
                    if (Game.ToD >= 0.2483 && Game.ToD < 0.909) { topSM.ChangeState(hmStatesTop.socialize); }
                    else if (Game.ToD >= 0.909 || Game.ToD < 0.2083) { topSM.ChangeState(hmStatesTop.rest); }
                    else if (Game.ToD >= 0.2083 && Game.ToD < 0.2483) { topSM.ChangeState(hmStatesTop.selfCare); }
                }
            }
        }
        
        public class NodeSocialize : HMNode
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
            
            public socialStateMachine = new SocializeSM(socialStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == hmStatesTop.schedule || aState == hmStatesTop.workDay)
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
        public class NodeWorkDay : HMNode
        {
            public enum workStates
            {
                resetArtJob,
                setupArtJob,
                goPout,
                atPout,
                goArtJob,
                setupSocialize,
                startSocialize
            };
            
            public workStateMachine = new WorkDaySM(workStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == hmStatesTop.schedule || aState == hmStatesTop.socialize)
                {
                    workStateMachine.ChangeState(workStates.setupArtJob);
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
        public class NodeRest : HMNode
        {
            public enum restStates
            {
                findBed,
                goBed,
                atBed,
                goPout,
                atPout
            };
            
            public restStateMachine = new RestSM(restStates);
            
            public void EnterFrom(int aState)
            {
                if (aState == hmStatesTop.schedule || aState == hmStatesTop.socialize)
                {
                    restStateMachine.ChangeState(restStates.setupArtJob);
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
        public class NodeSelfCare : HMNode
        { }
        public class NodePatrol : HMNode
        { }
        public class NodeDepressed : HMNode
        { }
        public class NodeRunInTerror : HMNode
        { }
        public class NodeHide : HMNode
        { }
        public class NodeDefendHome : HMNode
        { }

                
        schedule,
        socialize,
        workDay,
        rest,
        selfCare,
        patrol,
        depressed,
        runInTerror,
        hide,
        defendVillage

    }
}
