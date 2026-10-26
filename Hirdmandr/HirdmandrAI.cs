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
        public NPCPlayerClone m_hmHumanoid;
        public ZNetView m_znetv;
        public ZNetScene m_znetscene;
        public long m_nextDepressionUpdate = 0;
        // #76: next Time.time at which the owner re-asserts the distress timestamp while
        // actually alerted (heartbeat; keeps a live threat "fresh" for the reader-side TTL).
        public float m_nextDistressHeartbeat = 0f;
        // Step 10 / #49h: call-for-help defender response. A free (non-following) hirdmandr runs
        // to help a party member in distress within DEFEND_RANGE, re-checking every DEFEND_POLL s.
        public const float DEFEND_RANGE = 100f;
        public const float DEFEND_POLL = 0.5f;
        // Freshness lease (#76): the owner re-asserts (heartbeats) the distress flag +
        // timestamp while actually alerted, so a live threat's age stays <= ~DISTRESS_HEARTBEAT.
        // A flag whose timestamp is older than this is stale (threat ended / owner gone / a
        // save-load-stale flag) and readers ignore it. 30s tolerates ~5 missed heartbeats
        // (a serious stall) while de-escalating a broken flag within 30s.
        public const float DISTRESS_TTL = 30f;
        // How often the owner re-asserts the distress timestamp while actually alerted.
        public const float DISTRESS_HEARTBEAT = 5f;
        // Cross-client Time.time skew tolerance: a same-session timestamp can read a few
        // seconds in the past on a client whose clock lags. A flag whose age is MORE
        // negative than this is from a previous session (Time.time is session-relative),
        // not clock skew - stale by definition.
        public const float DISTRESS_CLOCK_TOL = 5f;
        public float m_nextDefendPoll = 0f;
        public HirdmandrNPC m_defendTarget = null;
        public TopLevelSM topSM;

        public string className = "HirdmandrAI";

        public List<ZDO> m_ZDOsInRange;

        // General use fields
        public int pathAttempts = 0;
        public Vector3 moveToTarget = Vector3.zero;
        public Vector3 moveToPos = Vector3.zero;
        // Thegn guard post (the "guard here" point, or the temp nearest-fire point) that the patrol
        // loop drives the NPC to and re-asserts as its native patrol point. Set by PatrolSM.
        public Vector3 patrolPost = Vector3.zero;
        public bool moveToReached;
        public bool moveToTrying;
        // Mender: true once we've issued the walk to the current menderTarget. Guards against
        // re-issuing TryToMove every tick (which made it waddle forever at an unreachable anchor).
        public bool menderMoveIssued;
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
          public string[] npcChests = new string[5]
              {
                  "piece_npc_chest_blackmetal",
                  "piece_npc_chest_reinforced",
                  "piece_npc_chest",
                  "piece_npc_barrel",
                  "piece_npc_chest_grausten"
              };
            public float nextJobTime;
            // setupArtJob re-setup gate: when no enabled job has a claimable site, WorkDaySM re-ranks
            // (via resetArtJob) on this gate instead of stalling to the end of the workday, so a
            // site freed mid-workday (another NPC resting / leaving / a chest restocked) is picked up.
            public float nextWorkSetupTime = 0f;
            // doJob re-eval gate (stretch #64): while working, if this job is no longer the
            // highest-skill enabled job, WorkDaySM goes back to setupArtJob on this gate to
            // re-rank and claim in ranked order.
            public float nextJobReEvalTime = 0f;
            public GameObject lamplighterTarget = null;
            public Piece menderTarget = null;
            public Vector3 menderMeetPoint = Vector3.zero;
            // Mender: the scan timer, separate from the work cooldown (nextJobTime). Every scan is
            // spaced 5s apart (menderNextScanTime); nextJobTime is the skill-based wait AFTER a
            // repair, before the next scan may start. Both must have passed to re-scan.
            public float menderNextScanTime = 0f;
           // Farmer: the selected target crop + its crop-table entry (Step 8/9), the work chest the
           // cultivator was taken from (so release returns it there), and the held-tool flag.
            public Pickable farmerTarget = null;
            public FarmerCrop farmerTargetEntry = default(FarmerCrop);
            public HirdmandrChest farmerSourceChest = null;
            public bool farmerHoldsCultivator = false;
            public bool farmerMoveIssued = false;
            public float farmerCollectNextTime = 0f;

        // Rest
        public ZDO restBedZDO = null;
        public HirdmandrBed restBedHMBed = null;

        // Emergency fields
        public Vector3 callHelpPos = Vector3.zero;
        public float callHelpDuration = 0f;

        protected virtual void Awake()
        {
            try
            {
            m_hmnpc = GetComponent<HirdmandrNPC>();
            m_hmMonsterAI = GetComponent<MonsterAI>();
            m_hmBaseAI = GetComponent<BaseAI>();
            m_hmHumanoid = GetComponent<NPCPlayerClone>();
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
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrAI.Awake threw:\n" + e);
                throw;
            }
        }

        protected virtual void Update()
        {
            try
            {
            // topSM is owner-authoritative (FixedUpdate is owner-gated too). Only the owner may
            // transition the state machine; a non-owner mutating its local topSM would desync the
            // NPC's behavior from the owner's (review F4).
            if (m_znetv.IsOwner())
            {
                // Auto-stop (D5): the only stop condition we manage. The follow target is
                // m_user's GameObject; when that player dies / logs out / disconnects the game
                // destroys the GameObject, so GetFollowTarget() goes null. Clear the transient
                // flag + native target so the NPC isn't left parked in "follow" with nothing to
                // chase. (Out-of-range and non-persistence resolve via the base game.)
                if (m_hmnpc.m_following && m_hmMonsterAI.GetFollowTarget() == null)
                {
                    m_hmnpc.StopFollowing();
                }

                // Drive the top-level state from (m_following, alerted). ChangeState only sets
                // nextState; OldManSM.Evaluate() fires EnterFrom/ExitTo only on an actual
                // transition, so re-requesting the current state every frame is safe (D1).
                bool alerted = m_hmMonsterAI.IsAlerted();
                // Step 9 / #49h, #76: keep this NPC's ZDO distress flag reconciled to its LIVE
                // alert state (level-based), not just on a flip. A flip-only write (the old
                // design) left a stale flag whenever the de-alert edge didn't fire - e.g. a
                // save/load (the alert state is transient and never persisted), an MP owner
                // transfer, or any missed clear - and a stale flag is a phantom defense target
                // for every free hirdmandr (#76). Level reconciliation self-heals within one
                // frame: the first owner frame after a load sees not-alerted and clears a
                // stale flag (also normalizing an unset key that reads back as true); any
                // later desync is corrected on the next frame. Owner-gated by the enclosing
                // IsOwner(); SetDistress re-checks ownership.
                //
                // While actually alerted, re-assert on a heartbeat so the freshness timestamp
                // stays current: a live threat reads "fresh" to readers indefinitely, while a
                // threat that has ended (or an owner that is gone) stops refreshing, and
                // readers de-escalate once the age passes DISTRESS_TTL.
                if (m_hmnpc != null)
                {
                    if (alerted)
                    {
                        if (Time.time >= m_nextDistressHeartbeat)
                        {
                            m_nextDistressHeartbeat = Time.time + DISTRESS_HEARTBEAT;
                            m_hmnpc.SetDistress(true);
                        }
                    }
                    else if (m_hmnpc.IsInDistress())
                    {
                        // No live threat: de-escalate. Clears a stale flag (load / missed
                        // de-alert / owner transfer); writes only when the flag is set.
                        m_hmnpc.SetDistress(false);
                    }
                }
                if (m_hmnpc.m_following)
                {
                    // Following: defend when threatened, otherwise hold the schedule-paused
                    // follow state. NodeThreatened routes back to "follow" on de-alert.
                    if (alerted)
                    {
                        topSM.ChangeState("threatened");
                    }
                    else
                    {
                        topSM.ChangeState("follow");
                    }
                }
                else if (alerted && topSM.curState != topSM.StateInt("patrol"))
                {
                    topSM.ChangeState("threatened");
                }
                else
                {
                    // Step 10 / #49h: free (non-following) hirdmandr answer a party member's
                    // distress call. Poll on a 0.5s cadence (same as the SM tick).
                    if (Time.time >= m_nextDefendPoll)
                    {
                        m_nextDefendPoll = Time.time + DEFEND_POLL;
                        m_defendTarget = FindDefendTarget();
                    }
                    if (m_defendTarget != null)
                    {
                        topSM.ChangeState("defend");
                    }
                    else if (topSM.curState == topSM.StateInt("defend"))
                    {
                        // The distress cleared while we were heading over / at the scene:
                        // resume the normal schedule (the "prior task" is re-derived from ToD).
                        m_defendTarget = null;
                        topSM.ChangeState("schedule");
                    }
                }
            }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrAI.Update threw:\n" + e);
            }
        }

        protected virtual void FixedUpdate()
        {
            try
            {
            if (m_znetv.IsOwner())
            {
                var timeDelta = m_hmMonsterAI.GetWorldTimeDelta();
                UpdateMoving(timeDelta);
                UpdateTimers(timeDelta);
            }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrAI.FixedUpdate threw:\n" + e);
            }
        }

        public void StartAI()
        {
            CancelInvoke("EvaluateSM");
            InvokeRepeating("EvaluateSM", UnityEngine.Random.Range(10f, 13f), 0.5f);

            // #76: the ZDO distress flag is transient AI state (the alert that raised it is
            // NOT persisted), so any flag surviving a save/load is stale by definition. It is
            // NOT cleared here: StartAI is called from OwnerInit/LoadZDO, which run BEFORE
            // this component's Awake() has assigned m_hmnpc - a clear here is a silent no-op.
            // That was the #76 bug: the old "clear on load" lived here and never actually ran,
            // so a stale flag survived and drove phantom defense. The flag is reconciled in
            // Update() instead, on the first owner frame after load, where m_hmnpc is
            // guaranteed set (Awake always precedes Update).
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
                            moveToPos = any_tries[UnityEngine.Random.Range(0, any_tries.Count)];
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
                            // run=true: travel to work/rest sites at run speed. run=false (walk) made the
                            // NPC visibly slower than the socialize SM, which already uses MoveTowards(..., true).
                            m_hmMonsterAI.MoveTo(timeDelta, moveToPos, 1f, true);

                            if (Vector3.Distance(m_hmnpc.transform.position, moveToPos) < moveCloseEnough)
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

        // Step 10 / #49h: the distressed party member this NPC should run to help, or null.
        // Only free (non-following) hirdmandr respond; the victim may be following or not.
        // FindNearestDistressed already excludes this NPC, so no self-check is needed here.
        public HirdmandrNPC FindDefendTarget()
        {
            if (m_hmnpc == null || m_hmnpc.m_following) return null;
            return m_hmnpc.FindNearestDistressed(DEFEND_RANGE);
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
