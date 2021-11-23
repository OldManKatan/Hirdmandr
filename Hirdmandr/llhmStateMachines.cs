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
    public class SocializeSM : StateMachine 
    {
        public string changeTopState = "";

        public SocializeSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("findMeetPoint", new NodeFindMeetPoint(this));
            AddState("goMeetPoint", new NodeGoMeetPoint(this));
            AddState("goIdlePoint", new NodeGoIdlePoint(this));
            AddState("atIdlePoint", new NodeAtIdlePoint(this));
            AddState("setupSocialize", new NodeSetupSocialize(this));
            AddState("startSocialize", new NodeStartSocialize(this));
        }

        public class NodeFindMeetPoint : SMNode
        {
            public NodeFindMeetPoint(SocializeSM psm) : base(psm) { }

            public string no_imp = "SocializeSM.NodeFindMeetPoint not implemented";

            override public void EnterFrom(int aState)
            {
                hmAI.socMeetPoint = Vector3.zero;
                hmAI.moveToPos = Vector3.zero;
                if (aState != 0 && aState != 1)
                {
                    Jotunn.Logger.LogWarning("Resetting socNoPathFires");
                    hmAI.socNoPathFires = new List<ZDO>();
                }
            }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState()
            {
                hmAI.m_hmMonsterAI.StopMoving();

                hmAI.socTargetFire = null;

                List<ZDO> npc_campfires = hmAI.GetPrefabZDOsInRange("piece_npc_fire_pit", 100f);

                if (npc_campfires.Count == 0)
                {
                    parentSM.ChangeState("goIdlePoint");
                    return;
                }

                float closestSoFar = 999999f;
                float thisDistance;

                Jotunn.Logger.LogWarning("  Checking fires for valid moveTo target");
                foreach (ZDO aZDO in npc_campfires)
                {
                    thisDistance = Vector3.Distance(aZDO.GetPosition(), hmAI.transform.position);
                    if (thisDistance < closestSoFar && hmAI.socNoPathFires.IndexOf(aZDO) == -1)
                    {
                        closestSoFar = thisDistance;
                        hmAI.socTargetFire = aZDO;
                    }
                }

                if (!(hmAI.socTargetFire is null))
                {
                    Jotunn.Logger.LogWarning("  A valid fire was found");
                }

                if (!(hmAI.socTargetFire is null) && hmAI.socTargetFire.GetPosition() != Vector3.zero)
                {
                    Jotunn.Logger.LogWarning("  Setting hmAI.socMeetPoint to fire position");
                    hmAI.socMeetPoint = hmAI.socTargetFire.GetPosition();
                    parentSM.ChangeState("goMeetPoint");
                    return;
                }
                else
                {
                    Jotunn.Logger.LogWarning("  No fire found, looking for Idle position");
                    parentSM.ChangeState("goIdlePoint");
                    return;
                }
            }
        }
        public class NodeGoMeetPoint : SMNode
        {
            public NodeGoMeetPoint(SocializeSM psm) : base(psm) { }

            public string no_imp = "SocializeSM.NodeGoMeetPoint not implemented";

            override public void EnterFrom(int aState)
            {
                hmAI.moveToPos = Vector3.zero;
                hmAI.m_hmMonsterAI.ResetPatrolPoint();
                hmAI.m_hmBaseAI.ResetPatrolPoint();
            }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState()
            {
                if (hmAI.socMeetPoint != Vector3.zero)
                {
                    if (hmAI.moveToPos == Vector3.zero)
                    {
                        Jotunn.Logger.LogWarning("  Picking random position near hmAI.socMeetPoint");

                        int minDistance = 4;
                        int maxDistance = 10;

                        for (var i = 0; i < 5; i++)
                        {
                            Jotunn.Logger.LogWarning("  Attempt number " + (i + 1));

                            var goToPos = new Vector3(
                                hmAI.socMeetPoint.x + UnityEngine.Random.Range(-10f, 10f),
                                hmAI.socMeetPoint.y + 1.5f,
                                hmAI.socMeetPoint.z + UnityEngine.Random.Range(-10f, 10f)
                                );

                            goToPos.y = ZoneSystem.instance.GetSolidHeight(goToPos);

                            if (Vector3.Distance(hmAI.socMeetPoint, goToPos) > minDistance && Vector3.Distance(hmAI.socMeetPoint, goToPos) < maxDistance)
                            {
                                hmAI.moveToPos = goToPos;
                                Jotunn.Logger.LogWarning("  Successfully set hmAI.moveToPos!");
                                break;
                            }
                        }
                    }

                    if (Vector3.Distance(hmAI.transform.position, hmAI.moveToPos) < 3f)
                    {
                        Jotunn.Logger.LogWarning("  In range! Time to socialize!");
                        hmAI.moveToPos = Vector3.zero;
                        hmAI.m_hmMonsterAI.SetPatrolPoint();
                        hmAI.m_hmMonsterAI.StopMoving();

                        parentSM.ChangeState("setupSocialize");
                    }

                    // else
                    // {
                    //     // Jotunn.Logger.LogWarning("    hmAI.m_hmMonsterAI.FindPath(hmAI.moveToPos) = " + hmAI.m_hmMonsterAI.FindPath(hmAI.moveToPos));
                    // 
                    //     // hmAI.m_hmMonsterAI.SetPatrolPoint(hmAI.moveToPos);
                    //     // Jotunn.Logger.LogWarning("  Trying hmAI.m_hmMonsterAI.MoveTo");
                    //     // Jotunn.Logger.LogWarning("    hmAI.m_hmMonsterAI.MoveTo(3f, hmAI.moveToPos, 0f, false) = " + hmAI.m_hmMonsterAI.MoveTo(3f, hmAI.moveToPos, 0f, false));
                    // 
                    //     Jotunn.Logger.LogWarning("  Trying to find a path to hmAI.moveToPos");
                    //     if (!hmAI.m_hmBaseAI.FindPath(hmAI.moveToPos))
                    //     {
                    //         Jotunn.Logger.LogWarning("  Did not find path, starting over...");
                    //         parentSM.ChangeState("findMeetPoint");
                    //     }
                    //     else
                    //     {
                    //         Pathfinding.instance.GetPath(hmAI.transform.position, hmAI.moveToPos, hmAI.m_hmBaseAI.m_path, hmAI.m_hmBaseAI.m_pathAgentType);
                    //         Jotunn.Logger.LogWarning("  Trying hmAI.m_hmMonsterAI.MoveTo");
                    //         // bool arrived = hmAI.m_hmMonsterAI.MoveTo(3f, hmAI.m_hmMonsterAI.m_path[0], 0f, false);
                    //         // Jotunn.Logger.LogWarning("  hmAI.m_hmMonsterAI.MoveTo = " + arrived);
                    //         hmAI.m_hmBaseAI.SetPatrolPoint(hmAI.moveToPos);
                    //         if (Vector3.Distance(hmAI.transform.position, hmAI.moveToPos) < 3f)
                    //         {
                    //             Jotunn.Logger.LogWarning("  In range! Time to socialize!");
                    //             hmAI.m_hmBaseAI.StopMoving();
                    // 
                    //             parentSM.ChangeState("setupSocialize");
                    //         }
                    //     }
                    // 
                    // 
                    //     // if (!hmAI.m_hmMonsterAI.HavePath(hmAI.moveToPos))
                    //     // {
                    //     //     Jotunn.Logger.LogWarning("  Did not have path, trying to find path");
                    //     //     if (!hmAI.m_hmMonsterAI.FindPath(hmAI.moveToPos))
                    //     //     {
                    //     //         Jotunn.Logger.LogWarning("  Could not find path to hmAI.moveToPos! hmAI.pathAttempts = " + hmAI.pathAttempts);
                    //     // 
                    //     //         hmAI.pathAttempts++;
                    //     //         if (hmAI.pathAttempts > 4)
                    //     //         {
                    //     //             Jotunn.Logger.LogWarning("  Too many path attempts, trying to find another meet point");
                    //     //             hmAI.moveToPos = Vector3.zero;
                    //     //             hmAI.socNoPathFires.Add(hmAI.socTargetFire);
                    //     //             hmAI.socTargetFire = null;
                    //     //             parentSM.ChangeState("findMeetPoint");
                    //     //         }
                    //     //         else
                    //     //         {
                    //     //             Jotunn.Logger.LogWarning("  Retrying path attempt, using same meet point");
                    //     //             hmAI.moveToPos = Vector3.zero;
                    //     //         }
                    //     //     }
                    //     // }
                    //     // 
                    //     // if (Vector3.Distance(hmAI.transform.position, hmAI.moveToPos) < 3)
                    //     // {
                    //     //     Jotunn.Logger.LogWarning("  In range! Time to socialize!");
                    //     //     hmAI.m_hmMonsterAI.StopMoving();
                    //     // 
                    //     //     parentSM.ChangeState("setupSocialize");
                    //     // }
                    // }
                }
                else
                {
                    parentSM.ChangeState("findMeetPoint");
                }
            }
        }

        public class NodeGoIdlePoint : SMNode
        {
            public NodeGoIdlePoint(SocializeSM psm) : base(psm) { }

            public string no_imp = "SocializeSM.NodeGoIdlePoint not implemented";

            override public void EnterFrom(int aState) 
            {
                if (aState == 0)
                {
                    hmAI.moveToPos = Vector3.zero;
                }
            }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() 
            {
                Jotunn.Logger.LogWarning("  Going to a random idle position, NO MEET POINT");
                for (var i = 0; i < 5; i++)
                {
                    Jotunn.Logger.LogWarning("  Attempt number " + (i + 1));

                    hmAI.moveToPos = new Vector3(
                        hmAI.socMeetPoint.x + UnityEngine.Random.Range(-20f, 20f),
                        hmAI.socMeetPoint.y,
                        hmAI.socMeetPoint.z + UnityEngine.Random.Range(-20f, 20f)
                        );
                    if (hmAI.m_hmMonsterAI.FindPath(hmAI.moveToPos))
                    {
                        Jotunn.Logger.LogWarning("  Trying hmAI.m_hmMonsterAI.MoveTowards");

                        hmAI.m_hmMonsterAI.MoveTowards(hmAI.moveToPos, true);
                        if (Vector3.Distance(hmAI.transform.position, hmAI.moveToPos) < 3)
                        {
                            hmAI.m_hmMonsterAI.StopMoving();
                            Jotunn.Logger.LogWarning("  In range! Time to Idle and Complain!");

                            parentSM.ChangeState("atIdlePoint");
                            break;
                        }
                    }
                }
            }
        }
        public class NodeAtIdlePoint : SMNode
        {
            public NodeAtIdlePoint(SocializeSM psm) : base(psm) { }

            public string no_imp = "SocializeSM.NodeAtIdlePoint not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState()
            {
                hmAI.m_hmnpc.Say("I am Idling!");
            }
        }
        public class NodeSetupSocialize : SMNode
        {
            public NodeSetupSocialize(SocializeSM psm) : base(psm) { }

            public string no_imp = "SocializeSM.NodeSetupSocialize not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState()
            {
                hmAI.m_hmnpc.Say("I am socializing!");
            }
        }
        public class NodeStartSocialize : SMNode
        {
            public NodeStartSocialize(SocializeSM psm) : base(psm) { }

            public string no_imp = "SocializeSM.NodeStartSocialize not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class WorkDaySM : StateMachine 
    {
        public string changeTopState = "";

        public WorkDaySM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("resetArtJob", new NodeResetArtJob(this));
            AddState("setupArtJob", new NodeSetupArtJob(this));
            AddState("goArtJob", new NodeGoArtJob(this));
            AddState("doJob", new NodeDoJob(this));
        }

        public class NodeResetArtJob : SMNode
        {
            public string no_imp = "WorkDaySM.NodeResetArtJob not implemented";

            public NodeResetArtJob(WorkDaySM psm) : base(psm) { }

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState()
            {
                hmAI.workJobs = hmAI.m_hmnpc.m_skills.GetEnabledSkillsHighestFirst();
                hmAI.workJobSite = Vector3.zero;
                hmAI.curJob = "";

                // if (hmAI.m_hmnpc.m_)
                // 
                // 
                // 
                // 
                // hmAI.socTargetFire = null;
                // 
                // List<ZDO> npc_campfires = hmAI.GetPrefabZDOsInRange("piece_npc_fire_pit", 100f);
                // 
                // if (npc_campfires.Count == 0)
                // {
                //     parentSM.ChangeState("goIdlePoint");
                //     return;
                // }
                // 
                // float closestSoFar = 999999f;
                // float thisDistance;
                // 
                // Jotunn.Logger.LogWarning("  Checking fires for valid moveTo target");
                // foreach (ZDO aZDO in npc_campfires)
                // {
                //     thisDistance = Vector3.Distance(aZDO.GetPosition(), hmAI.transform.position);
                //     if (thisDistance < closestSoFar && hmAI.socNoPathFires.IndexOf(aZDO) == -1)
                //     {
                //         closestSoFar = thisDistance;
                //         hmAI.socTargetFire = aZDO;
                //     }
                // }

                parentSM.ChangeState("setupArtJob");
            }
        }
        public class NodeSetupArtJob : SMNode
        {
            public NodeSetupArtJob(WorkDaySM psm) : base(psm) { }

            public string no_imp = "WorkDaySM.NodeSetupArtJob not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState()
            {
                if (hmAI.curJob == "")
                {
                    if (hmAI.workJobs.Count > 0)
                    {
                        hmAI.curJob = hmAI.workJobs[0];
                        hmAI.workJobs.RemoveAt(0);
                        hmAI.curJobPrefabs = new List<string>();
                        foreach (string jobPrefab in hmAI.artisanJobs[hmAI.curJob])
                        {
                            hmAI.curJobPrefabs.Add(jobPrefab);
                        }
                    }
                    else
                    {
                        Jotunn.Logger.LogError("  setupArtJob could NOT find a valid job and available site, IDLE AND WHINE NOT IMPLEMENTED");
                    }
                }
                if (hmAI.curJobPrefabs.Count == 0)
                {
                    hmAI.curJob = "";
                }
                if (hmAI.curJob == "")
                {
                    return;
                }
                
                string lookForPrefab = hmAI.curJobPrefabs[0];
                hmAI.curJobPrefabs.RemoveAt(0);
                
                List<ZDO> foundNPCChests = new List<ZDO>();
                List<ZDO> validWorkPrefab = new List<ZDO>();
                List<ZDO> validWorksites = new List<ZDO>();

                foreach (string npcChests in new List<string>() { "piece_npc_chest" } )
                {
                    foreach (ZDO thisZDO in hmAI.GetPrefabZDOsInRange(npcChests, 100f))
                    {
                        foundNPCChests.Add(thisZDO);
                    }
                }
                
                foreach (ZDO anNPCChest in foundNPCChests)
                {
                    foreach (ZDO thisZDO in hmAI.GetPrefabZDOsInRange(lookForPrefab, 100f))
                    {
                        // && if anNPCChest.GetComponent<HirdmandrChest>().ownerZDOID == null ? Or something?
                        if (Vector3.Distance(anNPCChest.GetPosition(), thisZDO.GetPosition()) < 15.0f)
                        {
                            if (!validWorkPrefab.Contains(anNPCChest))
                            {
                                validWorkPrefab.Add(anNPCChest);
                            }
                        }
                    }
                }
                
                ZDO closestPrefab = null;
                float closestDistance = 999999f;
                if (validWorkPrefab.Count > 0)
                {
                    foreach (ZDO thisSiteZDO in validWorkPrefab)
                    {
                        var thisDist = Vector3.Distance(thisSiteZDO.GetPosition(), hmAI.transform.position);
                        if (thisDist < closestDistance)
                        {
                            closestDistance = thisDist;
                            closestPrefab = thisSiteZDO;
                        }
                    }
                }
                
                if(!(closestPrefab is null))
                {
                    hmAI.workJobSite = closestPrefab.GetPosition();
                    parentSM.ChangeState("goArtJob");
                }
            }
        }
        public class NodeGoArtJob : SMNode
        {
            public NodeGoArtJob(WorkDaySM psm) : base(psm) { }

            public string no_imp = "WorkDaySM.NodeGoArtJob not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeDoJob : SMNode
        {
            public NodeDoJob(WorkDaySM psm) : base(psm) { }

            public string no_imp = "WorkDaySM.NodeDoJob not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class RestSM : StateMachine 
    {
        public string changeTopState = "";

        public RestSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("findBed", new NodeFindBed(this));
            AddState("goBed", new NodeGoBed(this));
            AddState("atBed", new NodeAtBed(this));
        }

        public class NodeFindBed : SMNode
        {
            public NodeFindBed(RestSM psm) : base(psm) { }

            public string no_imp = "RestSM.NodeFindBed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoBed : SMNode
        {
            public NodeGoBed(RestSM psm) : base(psm) { }

            public string no_imp = "RestSM.NodeGoBed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtBed : SMNode
        {
            public NodeAtBed(RestSM psm) : base(psm) { }

            public string no_imp = "RestSM.NodeAtBed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class SelfCareSM : StateMachine 
    {
        public string changeTopState = "";

        public SelfCareSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("findFood", new NodeFindFood(this));
            AddState("goFood", new NodeGoFood(this));
            AddState("atFood", new NodeAtFood(this));
        }

        public class NodeFindFood : SMNode
        {
            public NodeFindFood(SelfCareSM psm) : base(psm) { }

            public string no_imp = "selfCareSM.NodeFindFood not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoFood : SMNode
        {
            public NodeGoFood(SelfCareSM psm) : base(psm) { }

            public string no_imp = "selfCareSM.NodeGoFood not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtFood : SMNode
        {
            public NodeAtFood(SelfCareSM psm) : base(psm) { }

            public string no_imp = "selfCareSM.NodeAtFood not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class PatrolSM : StateMachine 
    {
        public string changeTopState = "";

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

            public string no_imp = "PatrolSM.NodeSetupPatrol not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoPost : SMNode
        {
            public NodeGoPost(PatrolSM psm) : base(psm) { }

            public string no_imp = "PatrolSM.NodeGoPost not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtPost : SMNode
        {
            public NodeAtPost(PatrolSM psm) : base(psm) { }

            public string no_imp = "PatrolSM.NodeAtPost not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeIsAlerted : SMNode
        {
            public NodeIsAlerted(PatrolSM psm) : base(psm) { }

            public string no_imp = "PatrolSM.NodeIsAlerted not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class DepressedSM : StateMachine 
    {
        public string changeTopState = "";

        public DepressedSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("startDepressed", new NodeStartDepressed(this));
            AddState("findComfort", new NodeFindComfort(this));
            AddState("goComfort", new NodeGoComfort(this));
            AddState("whine", new NodeWhine(this));
        }

        public class NodeStartDepressed : SMNode
        {
            public NodeStartDepressed(DepressedSM psm) : base(psm) { }

            public string no_imp = "DepressedSM.NodeStartDepressed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeFindComfort : SMNode
        {
            public NodeFindComfort(DepressedSM psm) : base(psm) { }

            public string no_imp = "DepressedSM.NodeFindComfort not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoComfort : SMNode
        {
            public NodeGoComfort(DepressedSM psm) : base(psm) { }

            public string no_imp = "DepressedSM.NodeGoComfort not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeWhine : SMNode
        {
            public NodeWhine(DepressedSM psm) : base(psm) { }

            public string no_imp = "DepressedSM.NodeWhine not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class RunInTerrorSM : StateMachine 
    {
        public string changeTopState = "";

        public RunInTerrorSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("callForHelp", new NodeCallForHelp(this));
            AddState("escapeHelp", new NodeEscapeHelp(this));
            AddState("escapeAny", new NodeEscapeAny(this));
            AddState("panic", new NodePanic(this));
        }

        public class NodeCallForHelp : SMNode
        {
            public NodeCallForHelp(RunInTerrorSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodeCallForHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeEscapeHelp : SMNode
        {
            public NodeEscapeHelp(RunInTerrorSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodeEscapeHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeEscapeAny : SMNode
        {
            public NodeEscapeAny(RunInTerrorSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodeEscapeAny not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodePanic : SMNode
        {
            public NodePanic(RunInTerrorSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodePanic not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class HideSM : StateMachine 
    {
        public string changeTopState = "";

        public HideSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("findSafe", new NodeFindSafe(this));
            AddState("goSafe", new NodeGoSafe(this));
            AddState("atSafe", new NodeAtSafe(this));
        }

        public class NodeFindSafe : SMNode
        {
            public NodeFindSafe(HideSM psm) : base(psm) { }

            public string no_imp = "HideSM.NodeFindSafe not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoSafe : SMNode
        {
            public NodeGoSafe(HideSM psm) : base(psm) { }

            public string no_imp = "HideSM.NodeGoSafe not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtSafe : SMNode
        {
            public NodeAtSafe(HideSM psm) : base(psm) { }

            public string no_imp = "HideSM.NodeAtSafe not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class DefendHomeSM : StateMachine 
    {
        public string changeTopState = "";

        public DefendHomeSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("isAlerted", new NodeIsAlerted(this));
            AddState("findNeedsHelp", new NodeFindNeedsHelp(this));
            AddState("goNeedsHelp", new NodeGoNeedsHelp(this));
            AddState("findThreat", new NodeFindThreat(this));
            AddState("goThreat", new NodeGoThreat(this));
            AddState("caution", new NodeCaution(this));
        }

        public class NodeIsAlerted : SMNode
        {
            public NodeIsAlerted(DefendHomeSM psm) : base(psm) { }

            public string no_imp = "DefendHomeSM.NodeIsAlerted not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeFindNeedsHelp : SMNode
        {
            public NodeFindNeedsHelp(DefendHomeSM psm) : base(psm) { }

            public string no_imp = "DefendHomeSM.NodeFindNeedsHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoNeedsHelp : SMNode
        {
            public NodeGoNeedsHelp(DefendHomeSM psm) : base(psm) { }

            public string no_imp = "DefendHomeSM.NodeGoNeedsHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeFindThreat : SMNode
        {
            public NodeFindThreat(DefendHomeSM psm) : base(psm) { }

            public string no_imp = "DefendHomeSM.NodeFindThreat not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoThreat : SMNode
        {
            public NodeGoThreat(DefendHomeSM psm) : base(psm) { }

            public string no_imp = "DefendHomeSM.NodeGoThreat not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeCaution : SMNode
        {
            public NodeCaution(DefendHomeSM psm) : base(psm) { }

            public string no_imp = "DefendHomeSM.NodeCaution not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }
}
