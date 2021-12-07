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
        public ZNetScene m_znetscene;
        public long m_nextDepressionUpdate = 0;

        public TopLevelSM topSM;

        public string className = "HirdmandrAI";

        public List<ZDO> m_ZDOsInRange;

        // General use fields
        public int pathAttempts = 0;
        public Vector3 moveToTarget = Vector3.zero;
        public Vector3 moveToPos = Vector3.zero;
        public bool moveToReached;
        public bool moveToTrying;
        public bool moveEnabled;
        public float moveToTimeout;
        public float moveMinDist;
        public float moveMaxDist;
        public float moveCloseEnough;
        public bool moveRequireShelter;
        Dictionary<string, float> hazardTypes = new Dictionary<string, float>();
        public List<Tuple<Vector3, float>> hazardZones = new List<Tuple<Vector3, float>>();
        public float nextHazardCheck = 0;

        // Socialize fields
        public ZDO socTargetFire = null;
        public List<ZDO> socNoPathFires = new List<ZDO>();
        public Vector3 socMeetPoint = Vector3.zero;

        // WorkDay fields
        public List<string> workJobs = new List<string>();
        public GameObject workJobSite = null;
        public HirdmandrChest workSiteHMChest = null;
        public GameObject workSiteObject;
        public string curJob = "";
        public string[] npcChests = new string[3] 
            {
                "piece_npc_chest_blackmetal",
                "piece_npc_chest_reinforced",
                "piece_npc_chest"
            };
        public float nextJobTime;

        // Rest
        public ZDO restBedZDO = null;
        public HirdmandrBed restBedHMBed = null;

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

            hazardTypes.Add("fire_pit", 1f);
            hazardTypes.Add("hearth", 2f);
            hazardTypes.Add("bonfire", 2f);
            hazardTypes.Add("piece_npc_fire_pit", 1f);
            hazardTypes.Add("piece_npc_hearth", 2f);
            hazardTypes.Add("piece_npc_bonfire", 2f);
            hazardTypes.Add("piece_sharpstakes", 1.5f);
        }

        protected virtual void Update()
        {
            if (m_hmMonsterAI.IsAlerted() && topSM.curState != topSM.StateInt("patrol"))
            {
                if (m_hmnpc.m_roleWarrior)
                {
                    topSM.ChangeState(topSM.StateInt("threatened"));
                }
                else
                {
                    topSM.ChangeState(topSM.StateInt("threatened"));
                }
            }
            if (m_hmnpc.m_himthikiFollowing)
            {
                topSM.ChangeState(topSM.StateInt("himthikiFollow"));
            }
        }

        protected virtual void FixedUpdate()
        {
            if (m_znetv.IsOwner())
            {
                var timeDelta = m_hmMonsterAI.GetWorldTimeDelta();
                UpdateMoving(timeDelta);
                UpdateTimers(timeDelta);
            }
        }

        public void StartAI()
        {
            CancelInvoke("CheckDepression");
            CancelInvoke("EvaluateSM");
            Invoke("CheckDepression", UnityEngine.Random.Range(60f, 300f));
            InvokeRepeating("EvaluateSM", UnityEngine.Random.Range(10f, 13f), 3f);
        }
        
        public void UpdateMoving(float timeDelta)
        {
            if (moveEnabled)
            {
                if (Time.time > nextHazardCheck)
                {
                    nextHazardCheck = Time.time + 60f;
                    hazardZones = new List<Tuple<Vector3, float>>();

                    foreach (KeyValuePair<string, float> entry in hazardTypes)
                    {
                        List<ZDO> hazards = Utils.GetPrefabZDOsInRange(transform.position, 100f, entry.Key);
                        foreach (ZDO thisZDO in hazards)
                        {
                            hazardZones.Add(new Tuple<Vector3, float>(thisZDO.m_position, entry.Value) { });
                        }
                    }
                }

                if (Time.time > moveToTimeout)
                {
                    moveToReached = false;
                    moveToTrying = false;
                    moveEnabled = false;
                }
                else
                {
                    if (moveToPos == Vector3.zero)
                    {
                        List<Vector3> shelter_tries = new List<Vector3>();
                        List<Vector3> any_tries = new List<Vector3>();

                        float coverPercentage;
                        bool underRoof;

                        int[] flip = new int[2] { -1, 1 };

                        for (var i = 0; i < 10; i++)
                        {
                            // Jotunn.Logger.LogWarning("    Numtries = " + (i + 1));

                            Vector3 thisV3 = new Vector3(
                                moveToTarget.x + (UnityEngine.Random.Range(moveMinDist, moveMaxDist) * flip[UnityEngine.Random.Range(0, 2)]),
                                moveToTarget.y,
                                moveToTarget.z + (UnityEngine.Random.Range(moveMinDist, moveMaxDist) * flip[UnityEngine.Random.Range(0, 2)])
                            );

                            Cover.GetCoverForPoint(thisV3, out coverPercentage, out underRoof);

                            if (underRoof && coverPercentage >= 0.8f)
                            {
                                moveToPos = thisV3;
                                // Jotunn.Logger.LogWarning("Position with ROOF and COVER found.");
                                return;
                            }
                            else if (underRoof)
                            {
                                shelter_tries.Add(thisV3);
                                // Jotunn.Logger.LogWarning("Position with ROOF found.");
                            }
                            else if (!moveRequireShelter)
                            {
                                any_tries.Add(thisV3);
                                // Jotunn.Logger.LogWarning("Position with OUTSIDE found.");
                            }
                        }

                        if (shelter_tries.Count > 0)
                        {
                            moveToPos = shelter_tries[UnityEngine.Random.Range(0, shelter_tries.Count)];
                        }
                        else if (any_tries.Count > 0)
                        {
                            moveToPos = any_tries[UnityEngine.Random.Range(0, shelter_tries.Count)];
                        }
                        else
                        {
                        }

                        if (moveToPos != Vector3.zero)
                        {
                            foreach (Tuple<Vector3, float> hazard in hazardZones)
                            {
                                if (Vector3.Distance(moveToPos, hazard.Item1) < hazard.Item2)
                                {
                                    Jotunn.Logger.LogWarning("Collision with hazard detected, trying again...");
                                    moveToPos = Vector3.zero;
                                }
                            }
                        }
                    }
                    else if (moveToPos != Vector3.zero)
                    {
                        if (m_hmMonsterAI.FindPath(moveToPos))
                        {
                            m_hmMonsterAI.MoveTo(timeDelta, moveToPos, 0f, false);

                            if (Vector3.Distance(m_hmnpc.transform.position, moveToPos) < 1.2f)
                            {
                                m_hmMonsterAI.StopMoving();
                                m_hmMonsterAI.SetPatrolPoint();
                                moveToPos = Vector3.zero;
                                moveToReached = true;
                                moveEnabled = false;
                                moveToTrying = false;
                            }
                        }
                        else
                        {
                            moveToPos = Vector3.zero;
                            // Jotunn.Logger.LogWarning("Move can't find path, trying again...");
                        }
                    }
                }
            }
        }

        public void UpdateTimers(float timeDelta)
        {
            if (callHelpDuration > 0f)
            {
                callHelpDuration -= timeDelta;
                if (callHelpDuration < 0f)
                {
                    callHelpDuration = 0;
                }
            }
        }

        public void TryToMove(Vector3 point, float targetRadius, float minDist, float maxDist, bool requireShelter, float timeout)
        {
            moveToPos = Vector3.zero;
            moveToTarget = point;
            moveCloseEnough = targetRadius;
            moveMinDist = minDist;
            moveMaxDist = maxDist;
            moveToTimeout = Time.time + timeout;
            moveRequireShelter = requireShelter;
            moveEnabled = true;
            moveToReached = false;
            moveToTrying = true;
        }

    public void CheckDepression()
        {
            if (m_hmnpc.m_mood < -2500 || m_hmnpc.m_mood < m_hmnpc.m_mentalstress)
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
                m_hmMonsterAI.ResetRandomMovement();
                topSM.Evaluate();
            }
        }
    }
}
