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
        public BaseAI m_hmBaseAI;
        public Humanoid m_hmHumanoid;
        public ZNetView m_znetv;
        public long m_nextDepressionUpdate = 0;

        public TopLevelSM topSM;

        public string className = "HirdmandrAI";

        public List<ZDO> m_ZDOsInRange;

        // General use fields
        public int pathAttempts = 0;
        public Vector3 moveToPos = Vector3.zero;
        public bool moveToReached = false;

        // Socialize fields
        public ZDO socTargetFire = null;
        public List<ZDO> socNoPathFires = new List<ZDO>();
        public Vector3 socMeetPoint = Vector3.zero;

        // WorkDay fields
        public List<string> workJobs = new List<string>();
        public Vector3 workJobSite = Vector3.zero;
        public string curJob = "";

        // Emergency fields
        public Vector3 callHelpPos = Vector3.zero;
        public float callHelpDuration = 0f;

        protected virtual void Awake()
        {
            m_hmnpc = GetComponent<HirdmandrNPC>();
            m_hmMonsterAI = GetComponent<MonsterAI>();
            m_hmBaseAI = GetComponent<BaseAI>();
            m_hmHumanoid = GetComponent<Humanoid>();
            m_znetv = GetComponent<ZNetView>();
            topSM = new TopLevelSM(GetComponent<HirdmandrAI>());

            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f));
            InvokeRepeating("EvaluateSM", UnityEngine.Random.Range(10f, 13f), 3f);
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

        protected virtual void FixedUpdate()
        {
            if (m_znetv.IsOwner())
            {
                var timeDelta = m_hmMonsterAI.GetWorldTimeDelta();
                if (moveToPos != Vector3.zero)
                {
                    moveToReached = m_hmMonsterAI.MoveTo(, moveToPos, 0f, false);
                    if (moveToReached)
                    {
                        moveToPos = Vector3.zero;
                        m_hmMonsterAI.StopMoving();
                        m_hmMonsterAI.SetPatrolPoint();
                    }
                }
                if (callHelpDuration > 0f)
                {
                    callHelpDuration -= timeDelta;
                    if (callHelpDuration < 0f)
                    {
                        callHelpDuration = 0;
                    }
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
            if (m_znetv.IsOwner())
            {
                Jotunn.Logger.LogInfo(m_hmHumanoid.m_name + " is evaluating their AI");
                m_hmMonsterAI.ResetRandomMovement();
                topSM.Evaluate();
            }
        }

        public List<ZDO> GetAllZDOsInRange(float range)
        {
            Vector3 center = transform.position;
            int MinX = (int)(center.x - range);
            int MinZ = (int)(center.z - range);
            int MaxX = (int)(center.x + range);
            int MaxZ = (int)(center.z + range);

            // Get sectors to check
            List<Vector2i> sectors;
            sectors = GetSectors(MinX, MinZ, MaxX, MaxZ);

            // Get zdo's
            var foundZDOs = new List<ZDO>();

            foreach (var sector in sectors)
            {
                ZDOMan.instance.FindObjects(sector, foundZDOs);
            }

            return foundZDOs;
        }

        public List<ZDO> GetPrefabZDOsInRange(string prefabName, float range)
        {
            int prefabStableHashCode = prefabName.GetStableHashCode();

            var foundZDOs = GetAllZDOsInRange(range);

            var matchedZDOs = new List<ZDO>();

            foreach (ZDO aZDO in foundZDOs)
            {
                if (aZDO.GetPrefab() == prefabStableHashCode)
                {
                    matchedZDOs.Add(aZDO);
                }
            }

            return matchedZDOs;
        }

        private static List<Vector2i> GetSectors(int minX, int minZ, int maxX, int maxZ)
        {
            List<Vector2i> sectors = new List<Vector2i>();

            int stepMinX = Zonify(minX);
            int stepMaxX = Zonify(maxX);

            int stepMinZ = Zonify(minZ);
            int stepMaxZ = Zonify(maxZ);

            for (int x = stepMinX; x <= stepMaxX; ++x)
            {
                for (int z = stepMinZ; z <= stepMaxZ; ++z)
                {
                    sectors.Add(new Vector2i(x, z));
                }
            }

            return sectors;

            int Zonify(int coordinate)
            {
                return Mathf.FloorToInt((coordinate + 32) / 64f);
            }
        }

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

                public NodeSchedule(TopLevelSM psm) : base(psm) { }

                public override void RunState()
                {
                    Jotunn.Logger.LogInfo("Starting NodeSchedule.RunState()");
                    float ToD = EnvMan.instance.m_smoothDayFraction;
                    Jotunn.Logger.LogInfo("  Got ToD");
                    if (hmNPC.m_roleArtisan) {
                        Jotunn.Logger.LogInfo("    If Artisan");

                        if (ToD >= 0.2483 && ToD < 0.3333) { parentSM.ChangeState("socialize"); }
                        else if (ToD >= 0.3333 && ToD < 0.7083) { parentSM.ChangeState("workDay"); }
                        else if (ToD >= 0.7083 && ToD < 0.909) { parentSM.ChangeState("socialize"); }
                        else if (ToD >= 0.909 || ToD < 0.2083) { parentSM.ChangeState("rest"); }
                        else if (ToD >= 0.2083 && ToD < 0.2483) { parentSM.ChangeState("selfCare"); }
                    }
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
                    if (hmNPC.m_roleArtisan)
                    {
                        if (ToD < 0.2483 && ToD >= 0.3333) { parentSM.ChangeState("schedule"); }
                    }
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
                    if (aState == parentSM.StateInt("schedule") || aState == parentSM.StateInt("socialize"))
                    {
                        workStateMachine.ChangeState("resetArtJob");
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

                    float ToD = EnvMan.instance.m_smoothDayFraction;
                    if (hmNPC.m_roleArtisan)
                    {
                        if (ToD < 0.3333 || ToD >= 0.7083) { parentSM.ChangeState("schedule"); }
                    }
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
                public override void RunState()
                {
                    restStateMachine.Evaluate();
                    if (restStateMachine.changeTopState.Length > 0)
                    {
                        parentSM.ChangeState(restStateMachine.changeTopState);
                        restStateMachine.changeTopState = "";
                    }

                    float ToD = EnvMan.instance.m_smoothDayFraction;
                    if (hmNPC.m_roleArtisan)
                    {
                        if (ToD < 0.909 && ToD >= 0.2083) { parentSM.ChangeState("schedule"); }
                    }
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
                    if (hmNPC.m_roleArtisan)
                    {
                        if (ToD < 0.2083 || ToD >= 0.2483) { parentSM.ChangeState("schedule"); }
                    }
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
                }
            }
            public class NodeRunInTerror : SMNode
            {
                public RunInTerrorSM terrorStateMachine;
                new public TopLevelSM parentSM;

                public NodeRunInTerror(TopLevelSM psm) : base(psm)
                {
                    terrorStateMachine = new RunInTerrorSM(hmAI);
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
                }
            }
            public class NodeHide : SMNode
            {
                public HideSM hideStateMachine;
                new public TopLevelSM parentSM;

                public NodeHide(TopLevelSM psm) : base(psm)
                {
                    hideStateMachine = new HideSM(hmAI);
                }

                public override void EnterFrom(int aState)
                {
                    hideStateMachine.ChangeState("findSafe");
                }
                public override void RunState()
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

                public NodeDefendHome(TopLevelSM psm) : base(psm)
                {
                    defendHomeStateMachine = new DefendHomeSM(hmAI);
                }

                public override void EnterFrom(int aState)
                {
                    if (hmMAI.IsAlerted())
                    {
                        defendHomeStateMachine.ChangeState("isAlerted");
                    }
                    else
                    {
                        defendHomeStateMachine.ChangeState("findNeedsHelp");
                    }
                }
                public override void RunState()
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
