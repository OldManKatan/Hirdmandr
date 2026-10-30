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
        // GUI freeze: true while ANY client's management panel (HirdmandrGUI) is open - the NPC
        // must stand still so the panel isn't dragged off-screen by a moving NPC. Every client
        // sets its own panel state (HirdmandrGUI.Update -> SetLocalGuiFreeze); a non-owner client
        // ALSO fires RPC_HMGuiFreeze so the OWNER's copy is set, because the owner is the
        // movement authority (FixedUpdate/UpdateMoving are owner-gated). m_guiFreeze = this
        // client's own panel (m_guiFreezeLocal) OR a peer's panel (m_guiFreezeRemote, owner-side,
        // set by the RPC), so the owner stays frozen whether its own or a peer's panel is open.
        // While true, UpdateMoving halts the current path every FixedUpdate (StopMoving) and the
        // IdleMovement/Follow prefixes suppress the native wander/follow branches; in-place
        // combat is preserved (DoAttack/SelectBestAttack are untouched).
        public bool m_guiFreeze;
        // This client's own panel open (HirdmandrGUI.Update, per-frame from activeSelf).
        private bool m_guiFreezeLocal;
        // A non-owner client's panel open (owner-side only; set by RPC_HMGuiFreeze).
        private bool m_guiFreezeRemote;
        public float moveToTimeout;
        public float moveMinDist;
        public float moveMaxDist;
        public float moveCloseEnough;
        public bool moveRequireShelter;
        // #85: stuck-on-furniture detection + recovery. Wake (AttachStop) can leave the NPC standing
        // on a bed collider whose floor navmesh is carved out (piece is solid), so FindPath is
        // false forever and MoveTo treats no-path as "arrived" - the old code re-rolled the spot
        // every tick until the move timeout, a silent standstill. Two flavors are detected:
        // FindPath failing continuously (findpath-fail) and a found path that yields no progress
        // (progress-stall). Recovery is vanilla's own MoveAndAvoid drive (which carries the 1.5s
        // stuck detect + 4s random corner-escape) plus a StepOffBed teleport fast path.
        // MoveAndAvoid raycasts straight and never calls FindPath, so FindPath's 5s same-target
        // false cache cannot poison it.
        public const float STUCK_STALL_TIME = 1.5f;
        public const float STUCK_STALL_DIST = 0.2f;
        public const float STUCK_RECOVERY_TIME = 5f;
        public float m_stallTimer;
        public Vector3 m_stallLastPos;
        public float m_pathFailSince; // Time.time when the current FindPath-fail streak began (0=none)
        public float m_recoveryUntil; // Time.time when the current recovery drive ends (0=none)
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

        // Gather fields (Himthiki Gatherer job): the 5s scan cache. m_gatherPickables = the
        // loaded pickables, m_gatherExcluded = the ones inside GatherSafeRadius of a
        // player-placed chest (the safe zone), m_gatherAnchorCount = anchors found in the
        // last scan. The cache is a starting point: eligibility is re-validated at pick
        // time (S5).
        public float m_lastGatherScan = -60f;
        public List<Pickable> m_gatherPickables = new List<Pickable>();
        public HashSet<Pickable> m_gatherExcluded = new HashSet<Pickable>();
        public int m_gatherAnchorCount;

        // Gather (S5/S6) pick session (a sub-behavior of the follow state): m_gatherTarget = the
        // pickable being moved to / picked, m_gatherMoveIssued = a TryToMove was issued for it,
        // m_gatherSweepPending = a pick just happened and its ~1s ground sweep is in flight (the
        // NPC holds at the pick point; the Follow patch suppresses native Follow while this OR
        // m_gatherTarget is set), m_gatherTargetPos = the pre-Interact pick point the sweep is
        // centered on (where the yield lands), and the per-target cooldown so a blocked
        // pickable isn't immediately re-selected (tug-loop guard).
        public Pickable m_gatherTarget = null;
        public bool m_gatherMoveIssued = false;
        public bool m_gatherSweepPending = false;
        public Vector3 m_gatherTargetPos = Vector3.zero;
        public Pickable m_gatherCooldownTarget = null;
        public float m_gatherCooldownUntil = 0f;

        // Gather (reselect pause): the earliest time the NPC may select a new target, after a
        // pick / abandon. A short beat between picks so it doesn't rush to the next (possibly
        // opposite-side) pickable and shuttle back and forth.
        public float m_gatherReselectAt = 0f;

        // #94 Catch-up: the "last seen simulating" heartbeat. m_catchupLastSimTime/m_catchupLastToD
        // are in-memory (world sim seconds via ZNet.instance.GetTimeSeconds() + ToD at the last
        // Update); the ZDO key "hmnpc_catchupToD" (read/written via m_catchupLastZDOWrite)
        // persists the ToD across load / relog / owner transfer. On re-entry after an unsim
        // gap >= CATCHUP_MIN_GAP the missed artisan work is banked into m_unspentCatchupSeconds
        // (m_catchupStartToD = the ToD at the start of the earliest banked gap) and runs as fast
        // catch-up CATCHUP_DEFER s later via a single-shot Invoke, so the just-loaded area's
        // ZDOs / m_container have settled before the catch-up mutates them (the #94 race); a
        // load/unload inside that window just accrues more banked seconds (see
        // WorkDaySM/NodeDoJob.RunCatchupWork).
        // CATCHUP_DAY_SECONDS: one in-game day is 1800 s of real time (ToD advances 1 per day).
		public float m_catchupLastSimTime = 0f;
		public float m_catchupLastToD = 0f;
		public float m_catchupLastZDOWrite = 0f;
		public float m_unspentCatchupSeconds = 0f;
		public float m_catchupStartToD = 0f;
		public const float CATCHUP_MIN_GAP = 60f;
		public const float CATCHUP_HEARTBEAT = 30f;
		public const float CATCHUP_DAY_SECONDS = 1800f;
		public const float CATCHUP_DEFER = 3f;

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
            // GUI freeze MP: a non-owner client's HirdmandrGUI fires RPC_HMGuiFreeze at THIS
            // view's owner (the movement authority) on panel open/close; the owner applies
            // its own panel state directly and never RPCs.
            if (m_znetv != null)
            {
                m_znetv.Register<bool>("RPC_HMGuiFreeze", RPC_HMGuiFreeze);
            }
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
                // #94 Catch-up: heartbeat the sim presence (in-memory + throttled ZDO) and run
                // the missed-work catchup when this NPC re-enters after an unsim gap.
                CatchupCheckAndHeartbeat();

                // Auto-stop (D5): the only stop condition we manage. The follow target is
                // m_user's GameObject; when that player dies / logs out / disconnects the game
                // destroys the GameObject, so GetFollowTarget() goes null. Clear the transient
                // flag + native target so the NPC isn't left parked in "follow" with nothing to
                // chase. (Out-of-range and non-persistence resolve via the base game.)
                if (m_hmnpc.m_following && m_hmMonsterAI.GetFollowTarget() == null)
                {
                    m_hmnpc.StopFollowing();
                }

                // Gather (Himthiki Gatherer job): refresh the 5s scan cache while the job can
                // actually run (warrior role + Himthiki job + Gatherer enabled + following);
                // otherwise drop it (the S5 session gate is re-checked every frame, so a stale
                // cache is never harmful, just wasted memory).
                if (m_hmnpc != null && m_hmnpc.m_roleWarrior && m_hmnpc.m_jobHimthiki
                    && m_hmnpc.m_jobGatherer && m_hmnpc.m_following)
                {
                    if (Time.time - m_lastGatherScan > 5f)
                    {
                        GatherScan();
                        m_lastGatherScan = Time.time;
                    }
                }
                else if (m_gatherPickables != null && m_gatherPickables.Count > 0)
                {
                    m_gatherPickables.Clear();
                    if (m_gatherExcluded != null)
                    {
                        m_gatherExcluded.Clear();
                    }
                    m_gatherAnchorCount = 0;
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

        // Gather (Himthiki Gatherer job): rebuild the 5s scan cache.
        // Pickables = the loaded set (the player's simulated range - unstreamed objects don't
        // exist to Unity). Anchors = player-placed storage (Piece + IsPlacedByPlayer +
        // Container) union HirdmandrChest (defensive redundancy, confirmed in S1). A pickable
        // within GatherSafeRadius of any anchor is marked excluded (the "away from home"
        // safe zone).
        public void GatherScan()
        {
            // A fresh scan only refreshes the cache (destroyed pickables drop out, streamed-in
            // pickables appear). The pick session itself is continuous (S6 chain): picks are
            // paced by the ~1s ground sweep (m_gatherSweepPending), not by the scan cadence.
            m_gatherPickables.Clear();
            var pickups = UnityEngine.Object.FindObjectsOfType<Pickable>();
            foreach (var pk in pickups)
            {
                // CanBePicked() = the game's own "pickable right now?" check: the hide-when-
                // picked visual is active in hierarchy, or else !m_picked && enabled. Picked
                // pickables stay in the world as GameObjects (a stone is hidden until it
                // respawns), so without this they'd stay cached and get chased forever.
                if (pk != null && pk.transform != null && pk.CanBePicked())
                {
                    // Gather Rules blacklist: never cache an ignored resource - a blacklisted
                    // pickable is simply not a target (no walk-to, no pick, no sweep).
                    if (GatherBlacklisted(pk)) continue;
                    m_gatherPickables.Add(pk);
                }
            }

            m_gatherExcluded.Clear();
            m_gatherAnchorCount = 0;
            var anchors = new List<Vector3>();
            var pieces = UnityEngine.Object.FindObjectsOfType<Piece>();
            foreach (var pc in pieces)
            {
                if (pc == null || pc.transform == null)
                {
                    continue;
                }
                if (pc.GetComponent<Container>() == null)
                {
                    continue;
                }
                if (pc.IsPlacedByPlayer() || pc.GetComponent<HirdmandrChest>() != null)
                {
                    anchors.Add(pc.transform.position);
                }
            }
            m_gatherAnchorCount = anchors.Count;
            if (anchors.Count > 0)
            {
                float r2 = Utils.GatherSafeRadius * Utils.GatherSafeRadius;
                foreach (var pk in m_gatherPickables)
                {
                    if (pk == null || pk.transform == null)
                    {
                        continue;
                    }
                    Vector3 pp = pk.transform.position;
                    for (int i = 0; i < anchors.Count; i++)
                    {
                        Vector3 ap = anchors[i];
                        float dx = pp.x - ap.x;
                        float dz = pp.z - ap.z;
                        if (dx * dx + dz * dz <= r2)
                        {
                            m_gatherExcluded.Add(pk);
                            break;
                        }
                    }
                }
            }
        }

        // Gather Rules blacklist (Thomas): does this pickable match an ignored resource? Matched
        // on the pickable's prefab name - wood = "Pickable_Branch", stone = "Pickable_Stone"
        // (StartsWith so Pickable_Stone_Large counts as stone too). Exact runtime names confirmed
        // in-game.
        public bool GatherBlacklisted(Pickable pk)
        {
            if (pk == null || m_hmnpc == null) return false;
            string n = pk.name;
            if (n == null) return false;
            if (m_hmnpc.m_gatherIgnoreWood && n.StartsWith("Pickable_Branch")) return true;
            if (m_hmnpc.m_gatherIgnoreStone && n.StartsWith("Pickable_Stone")) return true;
            return false;
        }

        // Called from the GUI (via ApplySettingLocal) when the Gather Rules blacklist changes:
        // refresh the scan cache now (don't wait for the 5s cadence) and drop the current target
        // if it is now blacklisted, so the NPC re-selects a valid target on the next tick.
        public void GatherBlacklistChanged()
        {
            GatherScan();
            if (m_gatherTarget != null && GatherBlacklisted(m_gatherTarget))
            {
                // Drop just the target + its in-flight move (not the full session): the next
                // GatherPickStep re-selects a valid target.
                m_gatherTarget = null;
                m_gatherMoveIssued = false;
                if (moveEnabled)
                {
                    moveEnabled = false;
                    moveToPos = Vector3.zero;
                    moveToTrying = false;
                    moveToReached = false;
                    if (m_hmMonsterAI != null) m_hmMonsterAI.StopMoving();
                }
            }
        }

        // Gather (S5): the session gates - all must hold for a pick to be considered. Checked on
        // the 0.5s SM tick (NodeFollow) and re-validated before Interact. (S5 has no sweep, so
        // weight never rises here; the weight gate is future-proofed for S6.)
        public bool GatherSessionGatesHold()
        {
            if (m_hmnpc == null) return false;
            if (!m_hmnpc.m_roleWarrior || !m_hmnpc.m_jobHimthiki || !m_hmnpc.m_jobGatherer) return false;
            if (!m_hmnpc.m_following) return false;
            if (m_hmMonsterAI == null || m_hmMonsterAI.IsAlerted()) return false;
            if (m_hmMonsterAI.GetFollowTarget() == null) return false;
            // The follow leash: never gather while the player is >20m away ("never chase a
            // running player"; far behind = no gather).
            if (GatherHDist(transform.position, m_hmMonsterAI.GetFollowTarget().transform.position) > Utils.GatherTargetRadius) return false;
            if (m_hmHumanoid == null || m_hmHumanoid.m_inventory == null) return false;
            return m_hmHumanoid.m_inventory.GetTotalWeight() < m_hmHumanoid.GetMaxCarryWeight();
        }

        // Gather (S5): the closest cached pickable to the center, not excluded (safe zone),
        // within GatherTargetRadius of the center, and not on cooldown. Center = the follow
        // target while following, else this NPC (the design's evaluation radius).
        public Pickable SelectGatherTarget()
        {
            if (m_gatherPickables == null || m_gatherPickables.Count == 0) return null;
            Vector3 center = transform.position;
            if (m_hmnpc != null && m_hmnpc.m_following && m_hmMonsterAI != null && m_hmMonsterAI.GetFollowTarget() != null)
            {
                center = m_hmMonsterAI.GetFollowTarget().transform.position;
            }
            float r2 = Utils.GatherTargetRadius * Utils.GatherTargetRadius;
            Pickable best = null;
            float bestD2 = float.MaxValue;
            for (int i = 0; i < m_gatherPickables.Count; i++)
            {
                Pickable pk = m_gatherPickables[i];
                if (pk == null || pk.transform == null) continue;
                // Live re-check: the cache is built on the scan cadence, but a pickable can be
                // picked (hidden) any time between scans (by this NPC or by the player).
                if (!pk.CanBePicked()) continue;
                if (m_gatherExcluded != null && m_gatherExcluded.Contains(pk)) continue;
                if (pk == m_gatherCooldownTarget) continue;
                float dx = pk.transform.position.x - center.x;
                float dz = pk.transform.position.z - center.z;
                float d2 = dx * dx + dz * dz;
                if (d2 > r2) continue;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = pk;
                }
            }
            return best;
        }

        // Gather (S5): one tick of the pick session, driven from NodeFollow on the 0.5s SM tick.
        // A single pick (target selection + move + Interact); no sweep / no chain yet (S6).
        public void GatherPickStep()
        {
            // A gate dropped mid-session (job off, not following, alerted, player >20m, weight
            // cap): drop the move cleanly. (Danger routes to the top-level threatened transition,
            // not here.)
            if (!GatherSessionGatesHold())
            {
                // The session is over (job off, alerted, player >20m, weight cap): clear it fully,
                // including stopping the in-flight gather move - a stale MoveTo would keep driving
                // the NPC to the pickable and fight the native combat movement (erratic/stunted
                // pathing in a fight). The 0.5s SM tick is "immediate" enough (Thomas).
                ClearGatherSession();
                return;
            }
            // A pick just happened and its ~1s ground sweep is in flight: hold position at the
            // pick point so the sweep can reach the yield drops (native Follow is suppressed by
            // the Follow patch via m_gatherSweepPending). The sweep clears m_gatherSweepPending,
            // and the next tick chains to the next target.
            if (m_gatherSweepPending)
            {
                return;
            }

            // No live target: (re)select. m_gatherTarget being null also covers "the previous
            // target was destroyed mid-flight" (Unity fake-null) - any in-flight move is dropped.
            if (m_gatherTarget == null)
            {
                m_gatherMoveIssued = false;
                // Reselect pause: after a pick / abandon, hold for GatherReselectPause before
                // selecting the next target, so the NPC doesn't rush to the next (possibly
                // opposite-side) pickable and shuttle back and forth.
                if (Time.time < m_gatherReselectAt) return;
                if (m_gatherCooldownTarget != null && Time.time >= m_gatherCooldownUntil)
                {
                    m_gatherCooldownTarget = null;
                }
                Pickable t = SelectGatherTarget();
                if (t == null)
                {
                    return; // nothing eligible -> just keep following.
                }
                m_gatherTarget = t;
                m_gatherMoveIssued = true;
                // Farmer-style walk: aim a bit closer than the 4m interact reach, 15s timeout.
                TryToMove(t.transform.position, 2f, 3f, 6f, false, Utils.FarmerMoveTimeout);
                return;
            }

            // Live target: get in reach, then pick.
            // It may have been picked out from under us (the player) since selection: Interact
            // on a picked pickable is a silent no-op (no drop, no RPC). Drop it cleanly - no
            // cooldown needed, the scan/select CanBePicked filter keeps it out from now on.
            if (!m_gatherTarget.CanBePicked())
            {
                m_gatherTarget = null;
                m_gatherMoveIssued = false;
                // Picked out from under us (the player): pause before the next selection so the
                // NPC doesn't immediately turn and rush to another target (shuttle guard).
                m_gatherReselectAt = Time.time + Utils.GatherReselectPause;
                return;
            }
            float dist = GatherHDist(transform.position, m_gatherTarget.transform.position);
            if (dist <= Utils.FarmerInteractReach)
            {
                DoGatherPick(m_gatherTarget); // captures the pick point, Interacts, schedules the ~1s sweep
                m_gatherTarget = null;
                m_gatherMoveIssued = false;
                // m_gatherSweepPending is set inside DoGatherPick; the sweep clears it (S6 chain).
                return;
            }

            // Out of reach: walk to it (one attempt). If the walk settles without getting in
            // reach, the target is blocked -> abandon + cooldown (tug-loop guard).
            if (!m_gatherMoveIssued)
            {
                m_gatherMoveIssued = true;
                TryToMove(m_gatherTarget.transform.position, 2f, 3f, 6f, false, Utils.FarmerMoveTimeout);
            }
            else if (!moveToTrying)
            {
                AbandonGatherTarget();
            }
        }

        // Gather (S5/S6): pick a pickable - the game's own Interact (the farmer's exact call):
        // drops the yield (a networked ItemDrop), drives the pickable's own pick-count/regrowth,
        // plays the pick VFX. Capture the pick point BEFORE Interact (it destroys the pickable);
        // the ~1s ground sweep (S6) is centered on m_gatherTargetPos. The sweep is Invoke-delayed
        // so the RPC/server has time to spawn the yield drops; m_gatherSweepPending holds the NPC
        // at the pick point (native Follow suppressed) for that window.
        public void DoGatherPick(Pickable target)
        {
            if (target == null || target.transform == null || m_hmHumanoid == null) return;
            m_gatherTargetPos = target.transform.position; // BEFORE Interact: it destroys the pickable
            // Bonus yield (Thomas): linear 0% @ 0 skill -> 25% @ 100 skill chance to spawn the
            // base yield a second time (skill/400f). Issued BEFORE Interact: Interact's
            // owner-directed RPC_Pick -> RPC_SetPicked -> SetPicked destroys single-pick
            // pickables (tree/stone/flower), so target is gone by the time Interact returns and
            // a post-Interact Drop would have nothing to spawn on. The drop lands at the pick
            // point, where the ~1s ground sweep (m_gatherSweepPending) is centered, so it is
            // picked up like the base yield. The stack mirrors the game's own base drop exactly
            // (Game.ScaleDrops of m_amount); offset 0 = no scatter, right on the pick point.
            // Roll BEFORE the skill-up below, matching the WorkDay live pattern (roll, then
            // ModifySkill), so the pick's +1.0 does not inflate its own bonus chance.
            if (target.m_itemPrefab != null && UnityEngine.Random.value <= m_hmnpc.m_skills.GetSkill("gatherer") / 400f)
            {
                target.Drop(target.m_itemPrefab, 0, Game.instance.ScaleDrops(target.m_itemPrefab, target.m_amount));
                // Announce it on the MP effect bus (all clients), like the other jobs' bonus yields.
                m_hmnpc.FireMpEffect(HMEffectKind.BonusYield, "gatherer bonus yield (extra yield from " + target.name + ")");
            }
            target.Interact(m_hmHumanoid, false, false);
            // Skill up: 1.0f per pickable Interact (the ground-sweep pickup gets none).
            m_hmnpc.m_skills.ModifySkill("gatherer", 1.0f);
            // Job done: the Gatherer plays the "interact" anim (owner-local + all clients), so it
            // looks like it is picking by hand like a player. One anim per pick, never per bonus
            // drop (see FireArtisanInteractAnim: deliberately not fired for bonus yields).
            m_hmnpc.FireArtisanInteractAnim();
            // Schedule the ~1s ground sweep (S6). Defensive cancel so a re-pick can't double-schedule.
            CancelInvoke("GatherSweep");
            Invoke("GatherSweep", 1f);
            m_gatherSweepPending = true;
            // Reselect pause: hold for GatherReselectPause before the next selection (gated by
            // m_gatherReselectAt in GatherPickStep) so the NPC doesn't rush to the next pickable.
            m_gatherReselectAt = Time.time + Utils.GatherReselectPause;
        }

        // Gather (S6): the ~1s ground sweep, centered on the pick point (m_gatherTargetPos, where
        // the yield lands). Mirrors the vanilla Player auto-pickup (NPCPlayerClone.cs AutoPickup):
        // for each auto-pickup ItemDrop within GatherSweepRadius that isn't a unique key, has a
        // valid ZNetView, passes CanPickup, fits the grid, and fits under the carry weight, call
        // the game's own Pickup (adds to the inventory, despawns the drop, syncs for all
        // clients). Then Save() persists the NPC inventory (m_container's grid IS the humanoid's
        // inventory, via UnifyInventories). Finally clear m_gatherSweepPending so the chain
        // continues to the next target on the next tick.
        public void GatherSweep()
        {
            m_gatherSweepPending = false;
            if (m_hmnpc == null || m_hmHumanoid == null || m_hmHumanoid.m_inventory == null) return;

            Vector3 center = m_gatherTargetPos;
            float r = Utils.GatherSweepRadius;
            // Same 3-arg OverlapSphere call the vanilla AutoPickup uses (NPCPlayerClone.cs): the
            // own call catches the ItemDrop pickup colliders, so match it exactly (Physics.AllLayers
            // stands in for the Player's m_autoPickupMask; we filter by GetComponent<ItemDrop>).
            Collider[] cols = Physics.OverlapSphere(center, r, Physics.AllLayers);
            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                if (col == null || col.attachedRigidbody == null) continue;
                ItemDrop drop = col.attachedRigidbody.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null) continue;
                if (!drop.m_autoPickup) continue;
                if (m_hmHumanoid.HaveUniqueKey(drop.m_itemData.m_shared.m_name)) continue;
                ZNetView nview = drop.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid()) continue;
                if (!drop.CanPickup(true)) continue;
                if (!m_hmHumanoid.m_inventory.CanAddItem(drop.m_itemData, -1)) continue;
                // Whole-drop weight (m_stack units), not per-unit: matches what Pickup would add.
                if (drop.m_itemData.GetWeight(drop.m_itemData.m_stack) + m_hmHumanoid.m_inventory.GetTotalWeight() > m_hmHumanoid.GetMaxCarryWeight()) continue;
                if (Vector3.Distance(drop.transform.position, center) > r) continue;
                m_hmHumanoid.Pickup(drop.gameObject, true, true);
            }
            if (m_hmnpc.m_container != null)
            {
                m_hmnpc.m_container.Save();
            }
        }

        // Gather (S5): give up on a target whose walk settled out of reach (blocked). Cooldown it
        // so the next scan doesn't immediately re-select it (tug loop).
        public void AbandonGatherTarget()
        {
            if (m_gatherTarget != null)
            {
                m_gatherCooldownTarget = m_gatherTarget;
                m_gatherCooldownUntil = Time.time + Utils.GatherTargetCooldown;
            }
            m_gatherTarget = null;
            m_gatherMoveIssued = false;
            // The walk settled out of reach (blocked): pause before the next selection so the
            // NPC doesn't immediately turn and rush to another target (shuttle guard).
            m_gatherReselectAt = Time.time + Utils.GatherReselectPause;
        }

        // Gather (S5/S6): clear the pick session fully - drop the target, stop the in-flight
        // gather move, cancel the pending ground sweep, and reset the reselect pause. Called when
        // the session ends mid-flight (the GatherPickStep gate-fail branch: job off, alerted,
        // player >20m, weight cap) and on a fresh session start (NodeFollow.EnterFrom). Stopping
        // the move is the important part: a stale gather MoveTo would keep driving the NPC to
        // the pickable and fight the native combat movement (erratic/stunted pathing that can
        // get the NPC killed in a fight). The per-target cooldown is intentionally kept: a
        // blocked pickable is still blocked across a session restart.
        public void ClearGatherSession()
        {
            m_gatherTarget = null;
            m_gatherMoveIssued = false;
            m_gatherSweepPending = false;
            m_gatherReselectAt = 0f;
            CancelInvoke("GatherSweep");
            // In the follow state gather is the only TryToMove user, so a live move IS the
            // gather move: clear it and StopMoving so it can't fight the native combat movement.
            bool hadLiveMove = moveEnabled;
            moveEnabled = false;
            moveToPos = Vector3.zero;
            moveToTrying = false;
            moveToReached = false;
            if (hadLiveMove && m_hmMonsterAI != null)
            {
                m_hmMonsterAI.StopMoving();
            }
        }

        // Horizontal (XZ) distance - the scan/exclusion/leash all measure XZ, not 3D.
        static float GatherHDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
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
        
        // GUI freeze MP: this client's own panel state (HirdmandrGUI.Update, per-frame). The
        // effective freeze is the OR of the local (own panel) and remote (peer's panel,
        // owner-side) states.
        public void SetLocalGuiFreeze(bool open)
        {
            m_guiFreezeLocal = open;
            m_guiFreeze = m_guiFreezeLocal || m_guiFreezeRemote;
        }

        // GUI freeze MP: owner-side, set by RPC_HMGuiFreeze from a non-owner client's panel
        // open/close (HirdmandrGUI fires it on transition). Owner-gated: only the movement
        // authority applies it.
        public void RPC_HMGuiFreeze(long sender, bool open)
        {
            if (m_znetv == null || !m_znetv.IsOwner())
            {
                return;
            }
            m_guiFreezeRemote = open;
            m_guiFreeze = m_guiFreezeLocal || m_guiFreezeRemote;
        }

        public void UpdateMoving(float timeDelta)
        {
            if (m_guiFreeze)
            {
                // GUI freeze: the management panel is open, so the NPC stands still. StopMoving
                // each FixedUpdate cancels the path issued before the freeze (a stale m_moveDir
                // would otherwise keep the character walking); the IdleMovement/Follow prefixes
                // suppress the native wander/follow branches while frozen. UpdateTimers still
                // runs (called after UpdateMoving).
                if (m_hmMonsterAI != null)
                {
                    m_hmMonsterAI.StopMoving();
                }
                return;
            }
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
                else if (Time.time < m_recoveryUntil)
                {
                    // #85 stuck recovery: drive with vanilla's own MoveAndAvoid, which raycasts
                    // straight to the target (no navmesh) and carries the 1.5s stuck detect +
                    // 4s random corner-escape. IdleMovement is suppressed while moveEnabled, so
                    // this is the sole driver. Arrival here ends the move cleanly; expiry falls
                    // back to normal flow, where the FindPath-fail streak re-triggers recovery
                    // until the move timeout cancels the move.
                    m_hmMonsterAI.MoveAndAvoid(timeDelta, moveToTarget, 0f, true);

                    if (Vector3.Distance(m_hmnpc.transform.position, moveToTarget) < moveCloseEnough)
                    {
                        m_hmMonsterAI.StopMoving();
                        m_hmMonsterAI.SetPatrolPoint();
                        moveToPos = Vector3.zero;
                        moveToReached = true;
                        moveEnabled = false;
                        moveToTrying = false;
                        m_recoveryUntil = 0f;
                        Jotunn.Logger.LogInfo("[HirdmandrStuck] " + transform.name + ": reached target via stuck recovery.");
                    }
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
                            // A path exists again: any findpath-fail streak is over.
                            m_pathFailSince = 0f;

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
                            else
                            {
                                // #85 progress-stall: a path is found but we barely move over
                                // STUCK_STALL_TIME (e.g. grinding against a wall the path can't
                                // clear). Measures progress over the window, not total
                                // displacement since the move, so re-baseline each check.
                                m_stallTimer += timeDelta;
                                if (m_stallTimer >= STUCK_STALL_TIME)
                                {
                                    m_stallTimer = 0f;
                                    if (Vector3.Distance(m_hmnpc.transform.position, m_stallLastPos) < STUCK_STALL_DIST)
                                    {
                                        EnterStuckRecovery("progress-stall");
                                    }
                                    m_stallLastPos = m_hmnpc.transform.position;
                                }
                            }
                        }
                        else
                        {
                            moveToPos = Vector3.zero;
                            // #85 findpath-fail: replaces the old silent re-roll loop. Track the
                            // streak across ticks; the re-roll lands within 1m of the previous
                            // spot each tick, so FindPath's 5s same-target cache keeps this
                            // returning false and the streak is continuous. After
                            // STUCK_STALL_TIME of that, enter recovery.
                            if (m_pathFailSince == 0f)
                            {
                                m_pathFailSince = Time.time;
                            }
                            else if (Time.time - m_pathFailSince >= STUCK_STALL_TIME)
                            {
                                EnterStuckRecovery("findpath-fail");
                            }
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

            // #85: each new move starts with a clean stuck slate; a fresh destination must not
            // inherit the previous move's fail streak or recovery window.
            m_stallTimer = 0f;
            m_stallLastPos = transform.position;
            m_pathFailSince = 0f;
            m_recoveryUntil = 0f;
        }

        // #85: begin stuck recovery for a move that can't get unstuck on its own. source is the
        // trigger flavor (findpath-fail / progress-stall) for the log. First try StepOffBed:
        // the wake case is exactly "standing on a bed whose floor navmesh is carved out", and
        // the teleport is the only thing that works when the NPC is boxed in. If it finds no
        // open spot, fall back to the vanilla MoveAndAvoid drive (recovery window), which
        // carries the random corner-escape.
        public void EnterStuckRecovery(string source)
        {
            m_pathFailSince = 0f;
            m_stallTimer = 0f;
            m_stallLastPos = m_hmnpc.transform.position;

            Jotunn.Logger.LogInfo("[HirdmandrStuck] " + transform.name + " (" + source + "): stuck recovery at " + m_hmnpc.transform.position.ToString("F2"));

            if (m_hmnpc.StepOffBed("stuck-recovery(" + source + ")"))
            {
                // Teleported to an open spot: the navmesh under us is valid again, so normal
                // flow (re-roll + FindPath) can proceed; no recovery window needed.
                Jotunn.Logger.LogInfo("[HirdmandrStuck] " + transform.name + " (" + source + "): StepOffBed moved us off the bed; resuming normal flow.");
                m_recoveryUntil = 0f;
                return;
            }

            m_recoveryUntil = Time.time + STUCK_RECOVERY_TIME;
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

        // #94 Catch-up: "did this NPC just come back from unsim?" check + the sim-presence
        // heartbeat. Called from Update on the owner every frame (cheap: float compares).
        //  - FRESH path: m_catchupLastSimTime <= 0 (first spawn, or re-entered sim after load /
        //    relog / owner transfer): the last-known ToD came from the ZDO, so the gap is the
        //    ToD delta since the heartbeat was written.
        //  - PERSISTENT path: already seen simulating this session: the gap is wall-clock
        //    since the last Update.
        // The ZDO heartbeat is throttled (CATCHUP_HEARTBEAT) and is ALWAYS written immediately
        // on a catchup fire, so a relog in the middle of a catchup cannot double-count the gap.
        // #94 Catch-up diagnostic (test build): log every candidate time clock so a single
        // in-game session shows which clock a 2-day time-skip actually advances.
        //   net   = ZNet.m_netTime seconds (our current clock, via GetTimeSeconds)
        //   ticks = ZNet.GetTime().Ticks (the smelter's clock; GetTime() = new DateTime(m_netTime * 1e7))
        //   time  = ZNet.GetTime() as a readable DateTime
        //   tot   = EnvMan.m_totalSeconds (second game-time accumulator, FixedUpdate-written)
        //   day   = EnvMan.GetCurrentDay() (= m_totalSeconds / m_dayLengthSec)
        //   tod/todSec = EnvMan.m_smoothDayFraction (0..1) and *1800 (mod unit)
        //   dayLen = EnvMan.m_dayLengthSec
        //   wrap  = ZNet.GetWrappedDayTimeSeconds() (= m_netTime % 86400)
        private void DiagLogClocks(string ctx)
        {
            Jotunn.Logger.LogInfo("[HirdmandrAI] diag " + ctx
                + " net=" + ZNet.instance.GetTimeSeconds()
                + " ticks=" + ZNet.instance.GetTime().Ticks
                + " time=" + ZNet.instance.GetTime().ToString("yyyy-MM-dd HH:mm:ss")
                + " tot=" + EnvMan.instance.m_totalSeconds
                + " day=" + EnvMan.instance.GetCurrentDay()
                + " tod=" + EnvMan.instance.m_smoothDayFraction
                + " todSec=" + (EnvMan.instance.m_smoothDayFraction * CATCHUP_DAY_SECONDS)
                + " dayLen=" + EnvMan.instance.m_dayLengthSec
                + " wrap=" + ZNet.instance.GetWrappedDayTimeSeconds());
        }

        // #94 Catch-up: atomically stamp the ZDO with the monotonic sim clock (m_netTime seconds)
        // and the time-of-day fraction at the same instant. The sim clock drives the GAP
        // MAGNITUDE (it is monotonic and advances through time-skips, so multi-day unsim gaps
        // measure exactly); the ToD fraction is only the banked period's ToD-walk START
        // (RunCatchupWork). Writing both together keeps the gap and its path start consistent.
        private void StampCatchupZDO(ZDO zdo, float now, float nowToD)
        {
            zdo.Set("hmnpc_catchupSimSec", now);
            zdo.Set("hmnpc_catchupToD", nowToD);
            m_catchupLastZDOWrite = now;
        }

        private void CatchupCheckAndHeartbeat()
        {
            float now = (float)ZNet.instance.GetTimeSeconds();
            float nowToD = EnvMan.instance.m_smoothDayFraction;
            ZDO zdo = m_znetv.GetZDO();
            if (zdo == null)
            {
                m_catchupLastSimTime = now;
                m_catchupLastToD = nowToD;
                return;
            }
            if (m_catchupLastSimTime <= 0f)
            {
                // FRESH: never seen simulating this session (first spawn, or the area unloaded
                // and re-instantiated this component after an unsim gap). The ZDO holds the
                // last-known monotonic sim clock (m_netTime seconds) and the ToD at that instant.
                float storedSimSec = zdo.GetFloat("hmnpc_catchupSimSec", -1f);
                float storedToD = zdo.GetFloat("hmnpc_catchupToD", -1f);
                // DiagLogClocks("FRESH storedSimSec=" + storedSimSec + " storedToD=" + storedToD + (storedSimSec < 0f ? " (firstSpawn)" : ""));
                if (storedSimSec < 0f || storedToD < 0f)
                {
                    // First spawn ever (or a stamp written before the sim-clock key existed):
                    // write the monotonic baseline so future unsim gaps are measurable. There is
                    // no measurable gap yet, so nothing to bank.
                    StampCatchupZDO(zdo, now, nowToD);
                }
                else
                {
                    // Re-entered after unsim. The GAP MAGNITUDE comes from the monotonic sim
                    // clock (m_netTime seconds): it does not wrap, so a multi-day unsim gap
                    // measures exactly. (The old ToD-delta approach wrapped at one day, so a
                    // 2-day gap read as only the residual ~fraction of a day.) The banked
                    // period's ToD walk in RunCatchupWork starts at storedToD.
                    float gapSeconds = now - storedSimSec;
                    if (gapSeconds >= CATCHUP_MIN_GAP)
                    {
                        BankCatchup(gapSeconds, storedToD);
                    }
                    // Always re-stamp immediately: the catchup (if any) is now "paid for" up to
                    // the current sim clock, so a later relog must not measure the same gap again.
                    StampCatchupZDO(zdo, now, nowToD);
                }
            }
            else if (now - m_catchupLastSimTime >= CATCHUP_MIN_GAP)
            {
                // PERSISTENT: already simulating this session and this is a real mid-session
                // unsim gap (sleep / owner transfer without an area unload). Gap from the
                // in-memory sim clock (m_netTime seconds, same monotonic clock as FRESH).
                // DiagLogClocks("PERSISTENT gap=" + (now - m_catchupLastSimTime) + " frozen=" + m_catchupLastSimTime);
                BankCatchup(now - m_catchupLastSimTime, m_catchupLastToD);
                StampCatchupZDO(zdo, now, nowToD);
            }
            else if (now - m_catchupLastZDOWrite >= CATCHUP_HEARTBEAT)
            {
                // Routine heartbeat: keep the ZDO monotonic clock + ToD fresh (throttled).
                StampCatchupZDO(zdo, now, nowToD);
            }
            m_catchupLastSimTime = now;
            m_catchupLastToD = nowToD;
        }

        // #94 Catch-up: bank an unsim gap for deferred execution instead of running it on the
        // re-entry frame. Re-entry is exactly when the area's ZDOs / m_container are still
        // loading (and get overwritten by the ZDOMan in the same frame), so the catch-up must
        // not mutate containers until the area has settled. A pending bank is signalled by
        // m_unspentCatchupSeconds > 0 (its single-shot Invoke is still outstanding); a
        // load/unload inside the CATCHUP_DEFER window just accrues more banked seconds, and
        // the deferred run always sees the whole bank.
        private void BankCatchup(float gapSeconds, float startToD)
        {
            // DiagLogClocks("BANK gap=" + gapSeconds + " startToD=" + startToD + " totalBankAfter=" + (m_unspentCatchupSeconds + gapSeconds));
            if (m_unspentCatchupSeconds <= 0f)
            {
                // First bank in this run: remember where the banked period started (the deferred
                // run walks the ToD path from there) and arm the single-shot deferred run.
                m_catchupStartToD = startToD;
                Invoke(nameof(CatchupDeferredRun), CATCHUP_DEFER);
            }
            m_unspentCatchupSeconds += gapSeconds;
        }

        // #94 Catch-up: the single-shot deferred run, CATCHUP_DEFER s after the gap was banked.
        // Runs the whole bank as fast catch-up, then clears it.
        private void CatchupDeferredRun()
        {
            if (m_unspentCatchupSeconds <= 0f) return;
            float bankedSeconds = m_unspentCatchupSeconds;
            float startToD = m_catchupStartToD;
            m_unspentCatchupSeconds = 0f;
            RunCatchupWork(bankedSeconds, startToD);
        }

        // #94 Catch-up: forward to the work-day state machine (the work logic lives there with
        // the rest of the artisan jobs). gapSeconds = how long this NPC was unsimulated;
        // storedToD = the ToD at the START of the gap (ZDO heartbeat, or in-memory stamp for
        // the persistent path) -- the work-window math walks the ToD path from there.
        public void RunCatchupWork(float gapSeconds, float storedToD)
        {
            WorkDaySM.RunCatchupWork(this, gapSeconds, storedToD);
        }
    }
}
