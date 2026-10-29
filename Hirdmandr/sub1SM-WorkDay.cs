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

            hmAI.nextJobTime = 0;
        }

        // Return the farmer's cultivator (acquired in setupArtJob, out of the claimed chest) so a
        // job boundary never runs a non-farmer job with the tool in hand. No-op unless the farmer
        // is holding one (keyed on the flag the setup path set); returns it to farmerSourceChest.
        // Static + hmAI param so the nested nodes can call it without a typed parent reference.
        private static void ReleaseHeldCultivator(HirdmandrAI hmAI)
        {
            if (hmAI.farmerHoldsCultivator)
            {
                if (hmAI.m_hmnpc != null)
                {
                    hmAI.m_hmnpc.ReleaseCultivator(hmAI.farmerSourceChest);
                }
                hmAI.farmerSourceChest = null;
                hmAI.farmerHoldsCultivator = false;
            }
        }

        // Give up the claimed work site without leaving workDay: return the farmer's cultivator (if
        // held), unclaim the chest, clear the site refs - so the next job's setupArtJob starts
        // clean. Used when a claimed job is abandoned mid-way (unreachable walk, periodic re-rank);
        // topSM.NodeWorkDay.ExitTo is the leave-the-whole-workday boundary.
        private static void AbandonWorkSite(HirdmandrAI hmAI)
        {
            ReleaseHeldCultivator(hmAI);
            if (hmAI.workJobSite)
            {
                hmAI.workJobSite.GetComponent<HirdmandrChest>().UnClaim(hmAI.curJob, hmAI.m_znetv.GetZDO().m_uid);
            }
            hmAI.curJob = "";
            hmAI.workJobSite = null;
            hmAI.workSiteHMChest = null;
            hmAI.workSiteObject = null;
        }

        public class NodeResetArtJob : SMNode
        {
            public NodeResetArtJob(WorkDaySM psm) : base(psm) { }

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                // A finished farmer job still holds its cultivator (acquired in setupArtJob, out of
                // the claimed chest). Return it before the next sub-job starts, so a non-farmer job
                // never runs with the cultivator in hand. This is the within-workday boundary for the
                // tool; topSM.NodeWorkDay.ExitTo is the leave-the-whole-workday boundary.
                WorkDaySM.ReleaseHeldCultivator(hmAI);

                hmAI.workJobs = hmAI.m_hmnpc.m_skills.GetEnabledSkillsHighestFirst();
                hmAI.workJobSite = null;
                hmAI.curJob = "";

                parentSM.ChangeState("setupArtJob");
            }
        }
        public class NodeSetupArtJob : SMNode
        {
            public NodeSetupArtJob(WorkDaySM psm) : base(psm) { }

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                if (hmAI.curJob == "")
                {
                    if (hmAI.workJobs.Count > 0)
                    {
                        hmAI.curJob = hmAI.workJobs[0];
                        hmAI.workJobs.RemoveAt(0);
                        // Jotunn.Logger.LogInfo("  setupArtJob " + hmAI.m_hmHumanoid.m_name + ": picked job '" + hmAI.curJob + "' (remaining workJobs=" + hmAI.workJobs.Count + ")");
                    }
                    else
                    {
                        // No job to do: head to (or hold at) the social gathering point, instead of
                        // standing wherever the workday left us. The meet point is the nearest
                        // NPC gathering fire, held in socMeetPoint (the same field socialize
                        // uses) so it is STEADY: found once (socialize's findMeetPoint zeroes it
                        // on entry, so a fresh find happens after socializing), then the walk is
                        // issued once and the NPC holds at its stand point. No fire in range:
                        // stay put, as before (and re-check each tick, like findMeetPoint).
                        if (hmAI.socMeetPoint == Vector3.zero)
                        {
                            hmAI.socMeetPoint = Utils.FindNearestNpcGatheringFire(hmAI.transform.position, 100f);
                        }

                        if (hmAI.socMeetPoint != Vector3.zero)
                        {
                            // A settled stand point is up to 6f off per axis (8.5f diagonal) plus
                            // the 2f arrival radius, so within 11f of the fire means "there".
                            // Holding is what keeps the spot steady: each TryToMove call re-rolls
                            // the stand point (moveToPos is reset), which is what made the NPC
                            // mill around the fire. moveToReached is shared by all moves, so it
                            // can't tell "arrived at the fire" from "arrived at a work site" -
                            // the distance check can.
                            if (Vector3.Distance(hmAI.transform.position, hmAI.socMeetPoint) > 11f && !hmAI.moveToTrying)
                            {
                                hmAI.TryToMove(hmAI.socMeetPoint, 2f, 3f, 6f, false, 30f);
                            }
                            // else: at the meet point, or a walk in flight (UpdateMoving drives it).
                        }

                        // workJobs is empty: no job is enabled, or every enabled job was just tried
                        // and had no claimable site. Don't stall to the end of the workday - re-rank
                        // from scratch on a 10s gate so a site that frees up mid-workday (another
                        // NPC resting / leaving / a chest restocked) gets picked up.
                        if (Time.time >= hmAI.nextWorkSetupTime)
                        {
                            hmAI.nextWorkSetupTime = Time.time + 10f;
                            parentSM.ChangeState("resetArtJob");
                        }
                        return;
                    }
                }

                // Mender needs no chest / work site (it carries no materials): skip the worksite
                // claim and go straight to doJob, where it mends damaged pieces around it.
                if (hmAI.curJob == "mender")
                {
                    parentSM.ChangeState("doJob");
                    return;
                }

                List<GameObject> foundWorkSites = new List<GameObject>();

                foreach (string chestString in hmAI.npcChests)
                {
                    // Jotunn.Logger.LogWarning("  NodeSetupArtJob checking for " + chestString + " chests...");
                    foreach (ZDO thisZDO in Utils.GetPrefabZDOsInRange(hmAI.transform.position, 100f, chestString))
                    {
                        ZNetView thisZNetV = ZNetScene.instance.FindInstance(thisZDO);
                        var hmChest = thisZNetV.GetComponent<HirdmandrChest>();
                        // Jotunn.Logger.LogWarning("  NodeSetupArtJob is evaluating chest " + hmChest);

                        bool siteValid = hmChest.IsValidWorksite(hmAI.curJob);
                        // Farmer: a cultivator already in the NPC's own grid also makes this chest
                        // a valid worksite - the player may have stocked the NPC directly (or the
                        // tool is still in hand), so the chest only needs the WORK (mature crops in
                        // range), not the tool. IsValidWorksite can't see this case: it's evaluated
                        // before the claim, so ValidFarmerInventory's "owner holds it" branch never
                        // fires here (no owner yet).
                        if (!siteValid && hmAI.curJob == "farmer" && hmAI.m_hmnpc != null
                            && hmAI.m_hmnpc.HasCultivator() && hmChest.HasMatureCropsInRange())
                        {
                            siteValid = true;
                        }
                        if (siteValid)
                        {
                            // Jotunn.Logger.LogWarning("    NodeSetupArtJob hmChest.IsValidWorksite(hmAI.curJob) = " + hmChest.IsValidWorksite(hmAI.curJob));
                            foundWorkSites.Add(hmChest.gameObject);
                        }
                    }
                }

                // Jotunn.Logger.LogInfo("  setupArtJob: found " + foundWorkSites.Count + " valid worksite(s) for job '" + hmAI.curJob + "'");
                var fwNum = foundWorkSites.Count;
                for (var i = 0; i < fwNum; i++)
                {
                    GameObject closestSite = null;
                    float closestSiteDist = 999999;
                    int closestTier = int.MaxValue;

                    foreach (GameObject thisGO in foundWorkSites)
                    {
                        float thisDist = Vector3.Distance(hmAI.transform.position, thisGO.transform.position);
                        // Lamplighter: rank by the fuel the chest holds, most-specific first (resin =
                        // its own fuel > wood > coal), so a resin chest beats a nearer wood/coal
                        // chest. Other jobs: tier is a constant 0, so this is nearest-only (the
                        // original behaviour, unchanged).
                        int thisTier = (hmAI.curJob == "lamplighter")
                            ? thisGO.GetComponent<HirdmandrChest>().LamplighterFuelPriority()
                            : 0;
                        if (thisTier < closestTier || (thisTier == closestTier && thisDist < closestSiteDist))
                        {
                            closestTier = thisTier;
                            closestSiteDist = thisDist;
                            closestSite = thisGO;
                        }
                    }

                    if (closestSite is null)
                    {
                        // Every candidate for this job is already claimed: clear curJob so the next
                        // tick picks the next ranked job (workJobs[0]).
                        Jotunn.Logger.LogWarning("  setupArtJob: no claimable worksite left for job '" + hmAI.curJob + "' - falling through to next ranked job");
                        hmAI.curJob = "";
                        return;
                    }
                    else
                    {
                        if (closestSite.GetComponent<HirdmandrChest>().Claim(hmAI.curJob, hmAI.m_znetv.GetZDO().m_uid))
                        {
                        Jotunn.Logger.LogInfo("  " + hmAI.m_hmHumanoid.m_name + " claimed worksite " + closestSite.transform.name + " for job " + hmAI.curJob);
                        hmAI.workJobSite = closestSite;
                        hmAI.workSiteHMChest = closestSite.GetComponent<HirdmandrChest>();
                        hmAI.workSiteObject = null;
                        // First-time: an artisan has claimed a worksite and is about to work it (once-only).
                        HMTutorial.Show(HMTutorial.FirstWorksite);
                            // Farmer: record the source chest (so the tool comes back HERE on release,
                            // even if it's already in the NPC's grid) and make sure the held flag is
                            // right. The tool is either in this chest (normal case: AcquireCultivator
                            // moves it into the NPC's grid and equips it) or already in the NPC's own
                            // inventory (player stocked the NPC directly / still in hand) - then
                            // acquire finds nothing in the chest and we just mark it held. The claim
                            // is what keeps two farmers from grabbing the same chest. Held for the
                            // whole job; returned on exit via topSM.
                            if (hmAI.curJob == "farmer" && hmAI.m_hmnpc != null)
                            {
                                hmAI.farmerSourceChest = hmAI.workSiteHMChest;
                                if (!hmAI.farmerHoldsCultivator)
                                {
                                    bool cultAcquired = hmAI.m_hmnpc.AcquireCultivator(hmAI.workSiteHMChest);
                                    if (cultAcquired || hmAI.m_hmnpc.HasCultivator())
                                    {
                                        hmAI.farmerHoldsCultivator = true;
                                    }
                                }
                            }
                            parentSM.ChangeState("goArtJob");
                            return;
                        }
                        else
                        {
                            // Jotunn.Logger.LogWarning("  setupArtJob: could not claim worksite " + closestSite.transform.name + " for job '" + hmAI.curJob + "' (already claimed?), trying next");
                            foundWorkSites.RemoveAt(foundWorkSites.IndexOf(closestSite));
                        }
                    }
                }
                if (hmAI.workJobSite == null)
                {
                    // No claimable site for this job: every candidate is already claimed (or none in
                    // range). Fall through to the next ranked job - clear curJob so the next tick
                    // picks workJobs[0] (the next highest-skill enabled job). When workJobs runs
                    // out, the pick block above re-ranks on the gate.
                    // Jotunn.Logger.LogWarning("  setupArtJob: no claimable worksite for job '" + hmAI.curJob + "' (exhausted all " + fwNum + " candidate(s)) - falling through to next ranked job");
                    hmAI.curJob = "";
                }
            }
        }
        public class NodeGoArtJob : SMNode
        {
            public NodeGoArtJob(WorkDaySM psm) : base(psm) { }

            override public void EnterFrom(int aState)
            {
                if (hmAI.workJobSite)
                {
                    hmAI.TryToMove(hmAI.workJobSite.transform.position, 4f, 0f, 2f, false, 30f);
                }
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                if (hmAI.moveToTrying)
                {
                    return;
                }
                else if (!hmAI.moveToTrying && hmAI.moveToReached)
                {
                    parentSM.ChangeState("doJob");
                }
                else
                {
                    // The claimed site is unreachable (path failed / timed out). A full resetArtJob
                    // re-rank would pick the same job (skills unchanged) and re-claim the same
                    // unreachable site - an infinite walk-fail loop. Give up this job and fall
                    // through to the next ranked job instead: the picked job is already out of
                    // workJobs, so setupArtJob moves on (or re-ranks on the gate if this was the
                    // last one).
                    WorkDaySM.AbandonWorkSite(hmAI);

                    parentSM.ChangeState("setupArtJob");
                }
            }
        }
        public class NodeDoJob : SMNode
        {
            public NodeDoJob(WorkDaySM psm) : base(psm) { }

            public bool genericToggle = false;

            override public void EnterFrom(int aState)
            {
                hmAI.workSiteObject = null;
                hmAI.lamplighterTarget = null;
                hmAI.menderTarget = null;
                hmAI.menderMoveIssued = false;
                hmAI.farmerTarget = null;
                hmAI.farmerMoveIssued = false;
                hmAI.farmerCollectNextTime = 0f;
                if (hmAI.curJob == "mender")
                {
                    // One-shot on doJob entry: make sure the Mender has a hammer in hand.
                    EquipMenderHammer();
                }
                if (hmAI.curJob == "farmer" && hmAI.m_hmnpc != null)
                {
                    // A GUI toggle / EquipBest may have swapped the cultivator out mid-job;
                    // put it back in the right hand before farming.
                    hmAI.m_hmnpc.EnsureCultivatorEquipped();
                }
                // The Farmer's cultivator is acquired in setupArtJob (from the claimed chest) and
                // returned on exit; doJob only resets the do-loop state above.

                // Socialize: a job is about to run — queue a "worked" moment (the job key). The
                // talk is said later, at the fire.
                if (hmAI.curJob != "")
                {
                    hmNPC.m_socialize.EnqueueWorked(hmAI.curJob);
                }
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                // The Mender has no work site (no chest): don't bounce it back to resetArtJob.
                if (!hmAI.workJobSite && hmAI.curJob != "mender")
                {
                    hmAI.workJobSite = null;
                    parentSM.ChangeState("resetArtJob");

                }

                // The Mender has no work site (no chest): skip the find-work-object block (which would
                // log every tick) and let its branch run. The Farmer, like the Lamplighter, works the
                // crops around its claimed chest, so the chest IS its work-site object.
                if (!hmAI.workSiteObject && hmAI.curJob != "mender")
                {
                    Jotunn.Logger.LogWarning("Does not have a valid hmAI.workSiteObject, looking for another");
                    if (hmAI.curJob == "lamplighter")
                    {
                        // The Lamplighter works at its own chest and refuels fixtures around it, so
                        // the chest is the work-site object (keeps this block from re-firing).
                        hmAI.workSiteObject = hmAI.workJobSite;
                    }
                    if (hmAI.curJob == "farmer")
                    {
                        // The Farmer works the crops around its claimed chest, so the chest is the
                        // work-site object (keeps this block from re-finding / logging every tick).
                        hmAI.workSiteObject = hmAI.workJobSite;
                    }
                    if (hmAI.curJob == "woodburner")
                    {
                        List<ZDO> kilns = Utils.GetPrefabZDOsInRange(hmAI.transform.position, 15f, "charcoal_kiln");
                        foreach (ZDO kilnZDO in kilns)
                        {
                            Smelter kilnSmelter = ZNetScene.instance.FindInstance(kilnZDO).GetComponent<Smelter>();
                            if (kilnSmelter.GetQueueSize() < 25)
                            {
                                Jotunn.Logger.LogWarning("  Found a valid hmAI.workSiteObject");
                                hmAI.workSiteObject = kilnSmelter.gameObject;
                                hmAI.TryToMove(hmAI.workSiteObject.transform.position + (hmAI.workSiteObject.transform.forward * 2f), 0.5f, 0f, 0f, false, 10f);
                            }
                        }
                    }
                    if (hmAI.curJob == "furnaceoperator")
                    {
                        List<ZDO> smelters = Utils.GetPrefabZDOsInRange(hmAI.transform.position, 15f, "smelter");
                        foreach (ZDO smelterZDO in smelters)
                        {
                            Smelter smelterSmelter = ZNetScene.instance.FindInstance(smelterZDO).GetComponent<Smelter>();
                            if (smelterSmelter.GetQueueSize() < 10 || smelterSmelter.GetFuel() < 19)
                            {
                                Jotunn.Logger.LogWarning("  Found a valid hmAI.workSiteObject");
                                hmAI.workSiteObject = smelterSmelter.gameObject;
                                hmAI.TryToMove(hmAI.workSiteObject.transform.position + (hmAI.workSiteObject.transform.right * 2f), 0.5f, 0f, 0f, false, 10f);
                            }
                        }
                    }
                }

                if (!hmAI.m_hmnpc.m_skills.isEnabled(hmAI.curJob))
                {
                    parentSM.ChangeState("resetArtJob");
                }

                // Stretch (#64): re-evaluate the job occasionally while working. If this job is no
                // longer the highest-skill enabled job (a better job got enabled / the ranking
                // moved), give up this site (and the farmer's cultivator, if held) and go back to
                // setupArtJob to re-rank and claim in ranked order. Gated so an artisan works a
                // while between re-evaluations; return so the job blocks below don't run this tick
                // with the site already abandoned.
                if (Time.time >= hmAI.nextJobReEvalTime)
                {
                    hmAI.nextJobReEvalTime = Time.time + 60f;
                    List<string> topJobs = hmAI.m_hmnpc.m_skills.GetEnabledSkillsHighestFirst();
                    if (topJobs.Count > 0 && topJobs[0] != hmAI.curJob)
                    {
                        var thisJob = hmAI.curJob;
                        WorkDaySM.AbandonWorkSite(hmAI);
                        Jotunn.Logger.LogWarning("  doJob: re-evaluating jobs - curJob '" + thisJob + "' is no longer the highest-skill enabled job '" + topJobs[0] + "'");
                        parentSM.ChangeState("setupArtJob");
                        return;
                    }
                }

                if (hmAI.curJob == "lamplighter")
                {
                    // The lamplighter refuels one fixture at a time, at its own pace (unlike the woodburner /
                    // furnace, which work a single static site). Each cycle: wait out the work cooldown,
                    // pick the nearest fixture that needs fuel (and that we carry fuel for), walk to it,
                    // add ONE fuel item, roll the skill-based no-consume chance, then loop.
                    // Actual pathing is driven every physics frame in HirdmandrAI.UpdateMoving; this node
                    // just picks targets and fires the refuel when we're in range, so it runs every tick.
                    if (!hmAI.lamplighterTarget)
                    {
                        // No active target: wait out the cooldown, then pick one fixture to refuel.
                        if (Time.time > hmAI.nextJobTime)
                        {
                            GameObject thisTarget = PickLamplighterTarget(hmAI, hmAI.workSiteHMChest);
                            if (thisTarget != null)
                            {
                                hmAI.lamplighterTarget = thisTarget;
                                // Walk to the fixture. Pathing is best-effort (some fixtures hang from
                                // ceilings / walls), so we refuel on horizontal range, not on "reached".
                                hmAI.TryToMove(thisTarget.transform.position, 4f, 0f, 2f, false, 15f);
                                Jotunn.Logger.LogWarning("Lamplighter: moving to " + thisTarget.name);
                            }
                            else
                            {
                                // Nothing to refuel right now; rest a bit so we don't re-scan every tick.
                                hmAI.nextJobTime = Time.time + 5f;
                            }
                        }
                    }
                    else
                    {
                        // Have a target: refuel once we're horizontally within ~4f (height-agnostic so
                        // ceiling / wall / floor fixtures all work), then roll and loop.
                        Vector3 npcFlat = new Vector3(hmAI.transform.position.x, 0f, hmAI.transform.position.z);
                        Vector3 tgtFlat = new Vector3(hmAI.lamplighterTarget.transform.position.x, 0f, hmAI.lamplighterTarget.transform.position.z);
                        if (Vector3.Distance(npcFlat, tgtFlat) <= 4f)
                        {
                            bool refueled = RefuelLamplighterTarget(hmAI.lamplighterTarget);
                            float lamplighterSkill = hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob);

                            if (refueled)
                            {
                                    Jotunn.Logger.LogInfo("Lamplighter: refuelled " + hmAI.lamplighterTarget.name);
                                    hmAI.m_hmnpc.m_skills.ModifySkill(hmAI.curJob, 1.0f);
                                    // Job done: the Lamplighter plays the "interact" anim (owner-local + all clients).
                                    hmAI.m_hmnpc.FireArtisanInteractAnim();
                                // Skill-based no-consume roll: free refuel chance 20% (lvl 1) -> 80% (lvl 100),
                                // i.e. higher skill uses less fuel. Only spend a chest item on a "paid" refuel.
                                float noConsumeChance = 0.20f + ((lamplighterSkill - 1f) * (0.60f / 99f));
                                if (UnityEngine.Random.value < noConsumeChance)
                                {
                                    // No-consume roll hit: this refuel cost no fuel - the lamplighter's
                                    // bonus yield. Announce it on the MP effect bus (all clients).
                                    hmAI.m_hmnpc.FireMpEffect(HMEffectKind.BonusYield, "lamplighter bonus yield (free refuel of " + hmAI.lamplighterTarget.name + ")");
                                }
                                else
                                {
                                    Fireplace thisFire = hmAI.lamplighterTarget.GetComponent<Fireplace>();
                                    string fuelName = (thisFire != null && thisFire.m_fuelItem != null && thisFire.m_fuelItem.m_itemData != null)
                                        ? thisFire.m_fuelItem.m_itemData.m_shared.m_name
                                        : "$item_coal";
                                    if (hmAI.workSiteHMChest.m_container.m_inventory.HaveItem(fuelName))
                                    {
                                        hmAI.workSiteHMChest.m_container.m_inventory.RemoveOneItem(hmAI.workSiteHMChest.m_container.m_inventory.GetItem(fuelName));
                                        hmAI.workSiteHMChest.m_container.Save();
                                    }
                                }
                            }

                            hmAI.lamplighterTarget = null;
                            // Stop the (possibly still-in-progress) walk and rest out the cooldown.
                            hmAI.m_hmMonsterAI.StopMoving();
                            hmAI.moveEnabled = false;
                            hmAI.moveToTrying = false;
                            hmAI.moveToPos = Vector3.zero;
                            hmAI.nextJobTime = Time.time + (18f - ((lamplighterSkill / 100f) * 18f));
                        }
                        else if (!hmAI.moveToTrying)
                        {
                            // Path timed out without getting close (blocked / inaccessible fixture):
                            // abandon this one and rest a bit before trying again.
                            hmAI.lamplighterTarget = null;
                            hmAI.nextJobTime = Time.time + 5f;
                        }
                    }
                    return; // lamplighter fully handled here; skip the shared woodburner / furnace block below
                }

                // ---- Mender (walk-out, no work site): repairs damaged player-built pieces within
                // MenderRange; idles at the nearest meeting point (campfire) when there's nothing
                // to mend. Own pick->move->repair loop; returns so it never hits the shared block. ----
                if (hmAI.curJob == "mender")
                {
                    float menderNow = Time.time;

                    // No live target: (re)scan. Two timers gate this: the fixed 5s scan cooldown
                    // (menderNextScanTime) and the skill-based work cooldown (nextJobTime, set by
                    // the last repair) -- the job cycle can't restart until both have passed.
                    // The meeting-point walk lives in the else below on purpose: the Mender only
                    // goes to the campfire when a scan just looked and found nothing. Between
                    // scans (e.g. the work cooldown after a repair, with fresh damage out there)
                    // it holds where it stands -- a per-tick meet-point block made it detour to
                    // the campfire mid-cooldown and jitter back out when the next scan picked a
                    // piece.
                    if (!hmAI.menderTarget)
                    {
                        if (menderNow >= hmAI.nextJobTime && menderNow >= hmAI.menderNextScanTime)
                        {
                            hmAI.menderTarget = PickMenderTarget();
                            hmAI.menderMoveIssued = false;
                            hmAI.menderNextScanTime = menderNow + 5f;
                            if (hmAI.menderTarget != null)
                            {
                                // A piece was just picked: re-assert the hammer in hand (the
                                // visequip can drift while the Mender idles/walks, so re-equip
                                // per pick).
                                EquipMenderHammer();
                            }
                            else
                            {
                                // Just looked and found nothing: head to (or hold at) the meeting
                                // point. Same steady pattern as the jobless-artisan gather:
                                // menderMeetPoint is found once, the walk is issued once, and the
                                // Mender holds at its stand point (each TryToMove call re-rolls
                                // the stand point, which is what made it mill around the fire).
                                if (hmAI.menderMeetPoint == Vector3.zero)
                                {
                                    hmAI.menderMeetPoint = FindMenderMeetPoint();
                                }

                                if (hmAI.menderMeetPoint != Vector3.zero)
                                {
                                    // Same 11f settled radius as the gather hold: a stand point is
                                    // up to 8.5f diagonal from the fire plus the 2f arrival radius.
                                    if (Vector3.Distance(hmAI.transform.position, hmAI.menderMeetPoint) > 11f && !hmAI.moveToTrying)
                                    {
                                        hmAI.TryToMove(hmAI.menderMeetPoint, 2f, 3f, 6f, false, 30f);
                                    }
                                    // else: at the meet point, or a walk in flight (UpdateMoving drives it).
                                }
                            }
                        }

                        // Still no live target: hold here (or let UpdateMoving finish the walk to
                        // the meeting point). A target picked THIS tick falls through to the
                        // live-target code below; a null one must never reach it.
                        if (!hmAI.menderTarget)
                        {
                            return;
                        }
                    }

                    // A live target: get horizontally close (XZ only, for ceiling/roof pieces), then
                    // mend. Repair is an instant RPC (no physical contact), so we do NOT require the
                    // NPC to reach the piece's ANCHOR -- a long/thick piece (a ship's hull) has its
                    // anchor inside the geometry, and the Mender can only get to the surface. So: mend
                    // when in reach, OR once the walk to the piece has settled (reached a stand point
                    // or the pathfinder gave up) while still out of anchor-reach.
                    Vector3 menderNpcFlat = new Vector3(hmAI.transform.position.x, 0f, hmAI.transform.position.z);
                    Vector3 menderTgtFlat = new Vector3(hmAI.menderTarget.transform.position.x, 0f, hmAI.menderTarget.transform.position.z);
                    float menderDist = Vector3.Distance(menderNpcFlat, menderTgtFlat);
                    if (menderDist <= Utils.MenderRepairReach)
                    {
                        // In reach: mend on the work cooldown.
                        if (menderNow >= hmAI.nextJobTime)
                        {
                            if (RepairMenderTarget(hmAI.menderTarget))
                            {
                                // Jotunn.Logger.LogInfo("Mender: repaired " + hmAI.menderTarget.name);
                                hmAI.m_hmnpc.m_skills.ModifySkill(hmAI.curJob, 1.0f);
                                // Full restore done: clear the target and rest out the skill-based
                                // work cooldown before the next scan may start.
                                hmAI.menderTarget = null;
                                hmAI.menderMoveIssued = false;
                                hmAI.nextJobTime = menderNow + (18f - ((hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob) / 100f) * 18f));
                            }
                            else
                            {
                                // Piece already whole (or gone) since we scanned: drop it. No new
                                // cooldown here -- the old scan/work timers gate the next scan.
                                hmAI.menderTarget = null;
                                hmAI.menderMoveIssued = false;
                            }
                        }
                        return;
                    }

                    // Out of anchor-reach: walk to the piece (ONE attempt per target), then mend from
                    // wherever the walk settles. Bounded by MenderRepairMoveTimeout so an unreachable
                    // anchor (a ship's bow) can't make the Mender waddle here forever.
                    if (!hmAI.menderMoveIssued)
                    {
                        hmAI.menderMoveIssued = true;
                        hmAI.TryToMove(hmAI.menderTarget.transform.position, 2f, 3f, 6f, false, Utils.MenderRepairMoveTimeout);
                    }
                    else if (!hmAI.moveToTrying)
                    {
                        // Walk ended (reached the nearest reachable point, or the pathfinder gave up)
                        // but we're still out of anchor-reach -> the anchor is unreachable, mend here.
                        if (menderNow >= hmAI.nextJobTime)
                        {
                            if (RepairMenderTarget(hmAI.menderTarget))
                            {
                                // Jotunn.Logger.LogInfo("Mender: repaired " + hmAI.menderTarget.name + " (from " + menderDist.ToString("0") + "f)");
                                hmAI.m_hmnpc.m_skills.ModifySkill(hmAI.curJob, 1.0f);
                                hmAI.menderTarget = null;
                                hmAI.menderMoveIssued = false;
                                hmAI.nextJobTime = menderNow + (18f - ((hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob) / 100f) * 18f));
                            }
                            else
                            {
                                // Piece already whole (or gone) since we scanned: drop it. No new
                                // cooldown here -- the old scan/work timers gate the next scan.
                                hmAI.menderTarget = null;
                                hmAI.menderMoveIssued = false;
                            }
                        }
                    }
                    // else: walk in progress (moveToTrying) -- UpdateMoving drives it; nothing to do.
                    return; // mender fully handled here
                }

                // ---- Farmer (walk-out around its chest): the mender/lamplighter "do loop", one crop
                // per pass: bank any yields on the ground -> pick a harvestable crop -> walk to it ->
                // doFarmerStuff (harvest + consume replant item + replant in place) -> pick the next.
                // The cultivator is held for the whole job (acquired on entry, returned on exit).
                // Returns so it never hits the shared woodburner / furnace block below. ----
                if (hmAI.curJob == "farmer")
                {
                    float farmerNow = Time.time;

                    // No cultivator held (the player didn't stock one, or it's still being taken):
                    // this isn't a working farmer. Bounce to reset so the NPC can do other jobs / idle.
                    if (!hmAI.farmerHoldsCultivator)
                    {
                        parentSM.ChangeState("resetArtJob");
                        return;
                    }

                    // A GUI gear-swap mid-job leaves farmerHoldsCultivator true but a different tool in
                    // hand; re-equip the cultivator every pass so farming still works without a state hop.
                    hmAI.m_hmnpc.EnsureCultivatorEquipped();

                    // Bank any harvest yields sitting on the ground (decoupled from the harvest itself,
                    // on its own short cadence, like the furnace banks its ingots): a drop that lands a
                    // frame late, or from an earlier pass, is swept up here, so the -replant-item /
                    // +yield accounting always settles out.
                    if (farmerNow >= hmAI.farmerCollectNextTime)
                    {
                        FarmerCollectYields();
                        hmAI.farmerCollectNextTime = farmerNow + 1f;
                    }

                    // No live target (or first entry): (re)scan the field for a harvestable crop.
                    if (!hmAI.farmerTarget)
                    {
                        if (farmerNow >= hmAI.nextJobTime)
                        {
                            hmAI.farmerTarget = PickFarmerTarget();
                            hmAI.farmerMoveIssued = false;
                            hmAI.nextJobTime = farmerNow + 2f;
                        }
                    }

                    // Still nothing to harvest: hold at the work chest and wait for the next scan.
                    if (!hmAI.farmerTarget)
                    {
                        if (hmAI.workJobSite)
                        {
                            hmAI.TryToMove(hmAI.workJobSite.transform.position, 4f, 0f, 2f, false, 30f);
                        }
                        return;
                    }

                    // Have a target: get in reach, then do the farmer work.
                    Vector3 farmerNpcFlat = new Vector3(hmAI.transform.position.x, 0f, hmAI.transform.position.z);
                    Vector3 farmerTgtFlat = new Vector3(hmAI.farmerTarget.transform.position.x, 0f, hmAI.farmerTarget.transform.position.z);
                    float farmerDist = Vector3.Distance(farmerNpcFlat, farmerTgtFlat);
                    if (farmerDist <= Utils.FarmerInteractReach)
                    {
                        // In reach: work on the (skill-based) cooldown.
                        if (farmerNow >= hmAI.nextJobTime)
                        {
                            if (DoFarmerStuff(hmAI.farmerTarget))
                            {
                                // Jotunn.Logger.LogInfo("Farmer: harvested " + hmAI.farmerTargetEntry.yieldItem + " -> replanted " + hmAI.farmerTargetEntry.sapling);
                                hmAI.m_hmnpc.m_skills.ModifySkill(hmAI.curJob, 1.0f);
                                hmAI.farmerTarget = null; // pick the next crop
                                hmAI.farmerMoveIssued = false;
                                hmAI.nextJobTime = farmerNow + (18f - ((hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob) / 100f) * 18f));
                            }
                            else
                            {
                                // Can't complete the work right now (replant item gone from the chest,
                                // or the crop was already picked): abandon this crop and re-scan.
                                hmAI.farmerTarget = null;
                                hmAI.farmerMoveIssued = false;
                                hmAI.nextJobTime = farmerNow + 2f;
                            }
                        }
                        return;
                    }

                    // Out of reach: walk to the crop (one attempt). If the walk settles without getting
                    // in reach, the crop is blocked -- abandon it and re-scan.
                    if (!hmAI.farmerMoveIssued)
                    {
                        hmAI.farmerMoveIssued = true;
                        hmAI.TryToMove(hmAI.farmerTarget.transform.position, 2f, 3f, 6f, false, Utils.FarmerMoveTimeout);
                    }
                    else if (!hmAI.moveToTrying && farmerNow >= hmAI.nextJobTime)
                    {
                        hmAI.farmerTarget = null;
                        hmAI.farmerMoveIssued = false;
                        hmAI.nextJobTime = farmerNow + 2f;
                    }
                    return; // farmer fully handled here
                }

                if (Time.time > hmAI.nextJobTime)
                {
                    var workSiteInventory = hmAI.workJobSite.GetComponent<Container>().m_inventory;

                    if (hmAI.curJob == "woodburner")
                    {
                        hmAI.nextJobTime = Time.time + (18f - ((hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob) / 100f) * 18f));
                        if (hmAI.workSiteObject)
                        {
                            if (hmAI.workSiteHMChest.m_container.m_inventory.HaveItem("$item_wood"))
                            {
                                // Jotunn.Logger.LogWarning("Wood was found");
                                Smelter kilnSmelter = hmAI.workSiteObject.GetComponent<Smelter>();
                                if (kilnSmelter.GetQueueSize() < 25)
                                {
                                    hmAI.m_hmMonsterAI.LookAt(hmAI.workSiteObject.transform.position);
                                    // Jotunn.Logger.LogWarning("One wood was removed from chest");
                                    if (hmAI.workSiteHMChest.m_container.m_inventory.HaveItem("$item_wood"))
                                    {
                                        hmAI.workSiteHMChest.m_container.m_inventory.RemoveOneItem(hmAI.workSiteHMChest.m_container.m_inventory.GetItem("$item_wood"));
                                        hmAI.workSiteHMChest.m_container.Save();
                                    }
                                     // Jotunn.Logger.LogWarning("One wood was added to kiln");
                                     kilnSmelter.m_nview.InvokeRPC("RPC_AddOre", "Wood", false);
                                     // Job done: the Woodburner plays the "interact" anim (owner-local + all clients).
                                     hmAI.m_hmnpc.FireArtisanInteractAnim();
                                     // Vanilla-style "+1 output": chance = (int)skill / 400 (0% at 0
                                    // skill, 25% at 100, linear in whole points). A bonus adds one
                                    // more wood to the kiln queue WITHOUT consuming a second wood -
                                    // the free extra output, one extra coal later.
                                    if (RollArtisanBonus(hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob)))
                                    {
                                        kilnSmelter.m_nview.InvokeRPC("RPC_AddOre", "Wood", false);
                                        hmAI.m_hmnpc.FireMpEffect(HMEffectKind.BonusYield, "woodburner bonus yield (extra wood to kiln)");
                                    }
                                    hmAI.m_hmnpc.m_skills.ModifySkill(hmAI.curJob, 1.0f);
                                }
                                else
                                {
                                    Jotunn.Logger.LogWarning("Kiln full, looking for another...");
                                    hmAI.workSiteObject = null;
                                }
                            }
                        }
                        List<ZDO> products = Utils.GetPrefabZDOsInRange(hmAI.transform.position, 15f, "Coal");
                        foreach (ZDO coalZDO in products)
                        {
                            // Jotunn.Logger.LogWarning("Found a coal");
                            GameObject eachCoal = ZNetScene.instance.FindInstance(coalZDO).gameObject;

                            // Jotunn.Logger.LogWarning("Trying to add item");
                            if (hmAI.workSiteHMChest.m_container.m_inventory.AddItem(eachCoal.GetComponent<ItemDrop>().m_itemData))
                            {
                                hmAI.workSiteHMChest.m_container.Save();
                                // Jotunn.Logger.LogWarning("Item was added");
                                // Jotunn.Logger.LogWarning("Destroying item");
                                if (hmAI.m_znetv.GetZDO() == null)
                                {
                                    UnityEngine.Object.Destroy((UnityEngine.Object)(object)eachCoal);
                                }
                                else
                                {
                                    ZNetScene.instance.Destroy(eachCoal);
                                }
                            }
                        }
                    }

                    else if (hmAI.curJob == "furnaceoperator")
                    {
                        hmAI.nextJobTime = Time.time + (18f - ((hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob) / 100f) * 18f));
                        genericToggle = !genericToggle;
                        bool oreFull = false;
                        bool fuelFull = false;
                        bool jobAnimPlayed = false;

                        if (hmAI.workSiteObject)
                        {
                            Smelter smelterSmelter = hmAI.workSiteObject.GetComponent<Smelter>();
                            Inventory smelterChestInv = hmAI.workSiteHMChest.m_container.m_inventory;

                            if (genericToggle)
                            {
                                if (smelterSmelter.GetQueueSize() < 10)
                                {
                                    hmAI.m_hmMonsterAI.LookAt(hmAI.workSiteObject.transform.position);

                                    // Jotunn.Logger.LogWarning("Queue size less than 10");
                                    string addedOreName = AddOneOreToSmelter(smelterSmelter);
                                    if (addedOreName != null)
                                    {
                                        // Job done: the Furnace Operator plays the "interact" anim (owner-local + all clients).
                                        hmAI.m_hmnpc.FireArtisanInteractAnim();
                                        jobAnimPlayed = true;
                                        // Vanilla-style "+1 output": chance = (int)skill / 400 (0% at 0
                                        // skill, 25% at 100, linear in whole points). A bonus adds one
                                        // more of the SAME ore to the smelter queue WITHOUT consuming a
                                        // second ore - the free extra output, one extra ingot later.
                                        if (RollArtisanBonus(hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob)))
                                        {
                                            smelterSmelter.m_nview.InvokeRPC("RPC_AddOre", addedOreName, false);
                                            hmAI.m_hmnpc.FireMpEffect(HMEffectKind.BonusYield, "furnaceoperator bonus yield (extra " + addedOreName + ")");
                                        }
                                        hmAI.m_hmnpc.m_skills.ModifySkill(hmAI.curJob, 1.0f);
                                    }
                                }
                                else
                                {
                                    oreFull = true;
                                }
                            }
                                
                            if (smelterSmelter.GetFuel() < (float)(smelterSmelter.m_maxFuel - 1))
                            {
                                if (smelterChestInv.HaveItem("$item_coal"))
                                {
                                     smelterChestInv.RemoveOneItem(smelterChestInv.GetItem("$item_coal"));
                                     hmAI.workSiteHMChest.m_container.Save();
                                      // Jotunn.Logger.LogWarning("One Coal was added to Smelter");
                                      smelterSmelter.m_nview.InvokeRPC("RPC_AddFuel", 0L);
                                      // Fuel-only cycle (no ore added this pass): play the anim here too, so a
                                      // fuel-only pass still shows one "interact" (never a double-call).
                                      if (!jobAnimPlayed)
                                      {
                                          hmAI.m_hmnpc.FireArtisanInteractAnim();
                                          jobAnimPlayed = true;
                                      }
                                }
                            }
                            else
                            {
                                fuelFull = true;
                            }

                            if (oreFull && fuelFull)
                            {
                                Jotunn.Logger.LogWarning("Smelter full, looking for another...");
                                hmAI.workSiteObject = null;
                            }
                        }

                        // Clean up the ground around the smelters

                        List<ZDO> products = new List<ZDO>();
                        foreach (string ingotPrefab in new string[4] { "Silver", "Iron", "Copper", "Tin" })
                        {
                            foreach (ZDO productZDO in Utils.GetPrefabZDOsInRange(hmAI.transform.position, 15f, ingotPrefab))
                            {
                                products.Add(productZDO);
                            }
                        }
                        foreach (ZDO productZDO in products)
                        {
                            // Jotunn.Logger.LogWarning("Found a coal");
                            GameObject eachProduct = ZNetScene.instance.FindInstance(productZDO).gameObject;

                            // Jotunn.Logger.LogWarning("Trying to add item");
                            if (hmAI.workSiteHMChest.m_container.m_inventory.AddItem(eachProduct.GetComponent<ItemDrop>().m_itemData))
                            {
                                hmAI.workSiteHMChest.m_container.Save();
                                // Jotunn.Logger.LogWarning("Item was added");
                                // Jotunn.Logger.LogWarning("Destroying item");
                                if (hmAI.m_znetv.GetZDO() == null)
                                {
                                    UnityEngine.Object.Destroy((UnityEngine.Object)(object)eachProduct);
                                }
                                else
                                {
                                    ZNetScene.instance.Destroy(eachProduct);
                                }
                            }
                        }
                    }

                }
            }

            // Find the closest in-range fixture that needs fuel and whose fuel type the chest
            // carries. Static so BOTH the live loop and the catch-up batch share this exact same
            // picker (the catch-up runs from a static context and can't call instance methods).
            private static GameObject PickLamplighterTarget(HirdmandrAI hmAI, HirdmandrChest chest)
            {
                if (chest == null)
                {
                    return null;
                }

                GameObject best = null;
                float bestDist = float.MaxValue;

                foreach (ZDO thisZDO in Utils.GetAllZDOsInRange(hmAI.transform.position, Utils.LamplighterRange))
                {
                    ZNetView thisView = ZNetScene.instance.FindInstance(thisZDO);
                    if (thisView == null)
                    {
                        continue;
                    }
                    Fireplace thisFire = thisView.GetComponent<Fireplace>();
                    if (thisFire == null || thisFire.m_nview == null || thisFire.m_nview.GetZDO() == null)
                    {
                        continue;
                    }
                    // Only target fixtures where adding one fuel (RPC_AddFuel = +1.0) can't reach or
                    // exceed the max. Refueling a near-full fixture clips the overflow (wasted fuel) and
                    // the same piece would just keep getting re-picked as it burns back below "full".
                    if (thisFire.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f) + 1f >= thisFire.m_maxFuel)
                    {
                        continue;
                    }
                    string fuelName = (thisFire.m_fuelItem != null && thisFire.m_fuelItem.m_itemData != null)
                        ? thisFire.m_fuelItem.m_itemData.m_shared.m_name
                        : "$item_coal";
                    if (!chest.m_container.m_inventory.HaveItem(fuelName))
                    {
                        continue; // we don't carry this fuel type
                    }

                    float dist = Vector3.Distance(hmAI.transform.position, thisZDO.m_position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = thisFire.gameObject;
                    }
                }

                return best;
            }

            // Shared by BOTH refuelers: a fixture has room for one more fuel (+1) if adding it
            // can't reach or exceed the max. Refueling a near-full fixture clips the overflow
            // (wasted fuel) and the same piece would just keep getting re-picked as it burns back
            // below "full". (The picker runs its own copy of this same check in its skip-branch.)
            private static bool LamplighterTargetHasRoom(Fireplace fire)
            {
                return fire != null && fire.m_nview != null && fire.m_nview.GetZDO() != null
                    && (fire.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f) + 1f < fire.m_maxFuel);
            }

            // LIVE: add exactly one fuel item to the fixture (the game's canonical "one fuel").
            // Static so the catch-up batch can reuse the same picker + full-check.
            private static bool RefuelLamplighterTarget(GameObject target)
            {
                Fireplace thisFire = target.GetComponent<Fireplace>();
                // Re-check at add time (fuel may have burned between pick and now).
                if (!LamplighterTargetHasRoom(thisFire))
                {
                    return false;
                }
                // Owner-gated and networked; plays the spark effect and updates the visuals.
                // (The player-id argument is unused by the game.)
                thisFire.m_nview.InvokeRPC("RPC_AddFuel", 0L);
                return true;
            }

            // CATCH-UP: the same +1 fuel, written straight to the ZDO var (no RPC). The game's
            // RPC_AddFuel is owner-gated (it opens with "if (!m_nview.IsOwner()) return;") and
            // bails before it writes in the catch-up context, so the RPC never lands the fuel -
            // which is why the chest fuel was spent with nothing reaching the fire. The direct
            // ZDO.Set is exactly how Container.Save() pushes the inventory (non-owner-gated), and
            // the world save then persists it. No spark effect / UpdateState: the catch-up is
            // state-only, and the fire re-reads its fuel var every frame anyway.
            private static bool RefuelLamplighterCatchup(GameObject target)
            {
                Fireplace thisFire = target.GetComponent<Fireplace>();
                if (!LamplighterTargetHasRoom(thisFire))
                {
                    return false;
                }
                float fuel = thisFire.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
                float newFuel = Mathf.Clamp(fuel + 1f, 0f, thisFire.m_maxFuel);
                thisFire.m_nview.GetZDO().Set(ZDOVars.s_fuel, newFuel);
                return true;
            }

            // The "one ore" action: take the first ore the chest carries (silver > iron > copper >
            // tin, the original chain's order) and put it into the smelter queue. Returns the RPC
            // ore name added (so a "+1 output" bonus can add a second of the SAME ore), null = the
            // chest carries no smelt-able ore.
            private string AddOneOreToSmelter(Smelter smelter)
            {
                Inventory chestInv = hmAI.workSiteHMChest.m_container.m_inventory;
                if (chestInv.HaveItem("$item_silverore"))
                {
                    chestInv.RemoveOneItem(chestInv.GetItem("$item_silverore"));
                    hmAI.workSiteHMChest.m_container.Save();
                    // Jotunn.Logger.LogWarning("One SilverOre was added to Smelter");
                    smelter.m_nview.InvokeRPC("RPC_AddOre", "SilverOre", false);
                    return "SilverOre";
                }
                if (chestInv.HaveItem("$item_ironscrap"))
                {
                    chestInv.RemoveOneItem(chestInv.GetItem("$item_ironscrap"));
                    hmAI.workSiteHMChest.m_container.Save();
                    // Jotunn.Logger.LogWarning("One IronOre was added to Smelter");
                    smelter.m_nview.InvokeRPC("RPC_AddOre", "IronScrap", false);
                    return "IronScrap";
                }
                if (chestInv.HaveItem("$item_copperore"))
                {
                    chestInv.RemoveOneItem(chestInv.GetItem("$item_copperore"));
                    hmAI.workSiteHMChest.m_container.Save();
                    // Jotunn.Logger.LogWarning("One CopperOre was added to Smelter");
                    smelter.m_nview.InvokeRPC("RPC_AddOre", "CopperOre", false);
                    return "CopperOre";
                }
                if (chestInv.HaveItem("$item_tinore"))
                {
                    chestInv.RemoveOneItem(chestInv.GetItem("$item_tinore"));
                    hmAI.workSiteHMChest.m_container.Save();
                    // Jotunn.Logger.LogWarning("One TinOre was added to Smelter");
                    smelter.m_nview.InvokeRPC("RPC_AddOre", "TinOre", false);
                    return "TinOre";
                }
                return null;
            }

            // Vanilla-style "+1 output" roll (the artisan balance effect): 0% at 0 skill, 25% at
            // 100 skill, linear in whole skill points - Random.value < (int)skill / 400.
            private static bool RollArtisanBonus(float skill)
            {
                return UnityEngine.Random.value < ((int)skill / 400f);
            }

            // Mender: pick the worst-damaged player-built piece within MenderRange. Priority =
            // (health/maxHealth) x horizontal distance, so a badly-broken piece near the Mender
            // mends before a barely-scratched one far away. Lowest score wins; null = none.
            private Piece PickMenderTarget()
            {
                Vector3 pos = hmAI.transform.position;
                float bestScore = float.MaxValue;
                Piece bestPiece = null;
                for (int i = 0; i < WearNTear.s_allInstances.Count; i++)
                {
                    WearNTear wnt = WearNTear.s_allInstances[i];
                    if (wnt == null || wnt.m_piece == null) continue;
                    Piece piece = wnt.m_piece;
                    if (!piece.IsPlacedByPlayer()) continue;
                    float hp = wnt.GetHealthPercentage();
                    if (hp >= 1f) continue; // already whole
                    Vector3 ppos = piece.transform.position;
                    float dx = ppos.x - pos.x;
                    float dz = ppos.z - pos.z;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist > Utils.MenderRange) continue;
                    float score = hp * dist;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPiece = piece;
                    }
                }
                return bestPiece;
            }

            // Mender: full-restore a piece (1 action = full, per spec). The "repair animation" is
            // the piece's own fix effect from RPC_Repair, not an NPC swing. true = repaired.
            private bool RepairMenderTarget(Piece piece)
            {
                WearNTear wnt = piece.GetComponent<WearNTear>();
                if (wnt == null) return false;
                if (wnt.GetHealthPercentage() >= 1f) return false; // already whole
                // Re-assert the hammer in hand at the moment of the repair, so it's visibly held
                // when the fix happens (idempotent: no-op if the hammer is already in hand).
                EquipMenderHammer();
                // Full restore + networked health (mirrors the Lamplighter's RPC_AddFuel).
                // RPC_Repair does NOT itself play a visible effect, so fire the piece's impact
                // effect at the repair spot -- that's what a real hammer hit produces.
                wnt.m_nview.InvokeRPC("RPC_Repair", 0L);
                // Job done: the Mender plays the "interact" anim (owner-local + all clients); the
                // piece's own fix effect below is what a real hammer hit produces.
                hmAI.m_hmnpc.FireArtisanInteractAnim();
                if (wnt.m_hitEffect != null)
                {
                    // Mirror the game's own WearNTear.RPC_Damage hit effect: an owner-spawned ZDO, so it
                    // is visible to every player (exactly how a real hammer hit is MP). This node runs
                    // only on the owner (the NPC state machine is IsOwner-gated), same as RPC_Damage.
                    // Place the puff on the near surface facing the Mender (the raw anchor sits inside
                    // thick pieces like a hull) and use identity rotation, matching the game.
                    Vector3 anchor = wnt.transform.position;
                    Vector3 impact = Vector3.Lerp(anchor, hmAI.transform.position, 0.5f);
                    wnt.m_hitEffect.Create(impact, Quaternion.identity, wnt.transform, 1f, -1, default(ZDOID));
                }
                return true;
            }

            // Mender: where to idle when there's nothing to mend - the nearest NPC gathering
            // fire within 100f, same source as Socialize. zero = none found.
            private Vector3 FindMenderMeetPoint()
            {
                return Utils.FindNearestNpcGatheringFire(hmAI.transform.position, 100f);
            }

            // Mender: make sure the NPC has a hammer in hand. If it's not in the inventory, give
            // one; then equip it so the hammer is visibly held while mending.
            private void EquipMenderHammer()
            {
                GameObject hammerPrefab = PrefabManager.Instance.GetPrefab("Hammer");
                if (hammerPrefab == null) return;
                ItemDrop.ItemData proto = hammerPrefab.GetComponent<ItemDrop>().m_itemData;
                if (proto == null || proto.m_shared == null) return;
                string hammerName = proto.m_shared.m_name;

                // Idempotent: already holding the hammer in the main hand?
                if (hmAI.m_hmHumanoid.m_rightItem != null
                    && hmAI.m_hmHumanoid.m_rightItem.m_shared != null
                    && hmAI.m_hmHumanoid.m_rightItem.m_shared.m_name == hammerName)
                {
                    return;
                }

                // Use a hammer already in the inventory, or pick one up. PickupPrefab (like
                // GiveDefaultItem) builds a proper ItemData WITH a valid m_dropPrefab; the raw
                // prefab m_itemData has a null drop prefab, which makes SetupVisEquipment NRE.
                ItemDrop.ItemData hammer = null;
                foreach (ItemDrop.ItemData item in hmAI.m_hmHumanoid.m_inventory.GetAllItems())
                {
                    if (item != null && item.m_shared != null && item.m_shared.m_name == hammerName)
                    {
                        hammer = item;
                        break;
                    }
                }
                if (hammer == null)
                {
                    hammer = hmAI.m_hmHumanoid.PickupPrefab(hammerPrefab, 0, false);
                }
                if (hammer == null) return;

                hmAI.m_hmHumanoid.EquipItem(hammer, false);
                if (hmAI.m_hmnpc != null && hmAI.m_hmnpc.m_visequip != null)
                {
                    hmAI.m_hmnpc.m_visequip.UpdateEquipmentVisuals();
                }
            }

            // Farmer: harvest a mature crop. Record the crop's position FIRST (that's the replant
            // spot), then drive the game's own pick. Interact(npc, repeat, alt) fires RPC_Pick on the
            // owner, which drops the yield (m_itemPrefab) on the ground + destroys the crop + plays
            // the owner-spawned pick VFX (MP-visible, like the Mender's fix effect).
            // (PoR §9.4: Step 11 confirms Interact fires for an NPC Humanoid; if not, drive the
            // owner-side pick directly as the fallback.)
            private static bool FarmerHarvest(NPCPlayerClone npc, Pickable crop, out string yieldItem, out Vector3 position)
            {
                position = Vector3.zero;
                yieldItem = null;
                if (npc == null || crop == null || crop.transform == null)
                {
                    return false;
                }
                position = crop.transform.position; // BEFORE Interact: it destroys the crop
                // The yield item = the internal name the crop's drop prefab carries (source of truth).
                GameObject drop = crop.m_itemPrefab;
                if (drop != null)
                {
                    ItemDrop it = drop.GetComponent<ItemDrop>();
                    if (it != null && it.m_itemData != null && it.m_itemData.m_shared != null)
                    {
                        yieldItem = it.m_itemData.m_shared.m_name;
                    }
                }
                crop.Interact(npc, false, false);
                return true;
            }

            // Farmer: replant a crop at the exact spot a mature one was just harvested from (the same
            // already-cultivated ground). Spawns the matching sapling_* Plant as a proper NETWORKED
            // ZDO (mirrors RescueSpawner.PlaceAtLocation: CreateNewZDO -> CreateObject -> AddInstance),
            // not a blind Instantiate, so it persists + syncs in MP. The Plant's own SlowUpdate grows
            // it over m_growTime, at which point it spawns its mature crop (m_grownPrefabs[0]) and
            // clears itself - so the SAME crop regrows at this spot.
            // Plantable consumption (the -1 from the chest) is the CALLER's job (Step 8), so this
            // helper stays pure. (PoR §9.5: Step 11 confirms it attaches + grows on cultivated ground;
            // if not, PlaceAgainst the ground and/or set the cultivated-ground fields.)
            private static bool FarmerReplant(Vector3 position, string saplingName)
            {
                if (string.IsNullOrEmpty(saplingName)) return false;
                ZNetScene scene = ZNetScene.instance;
                if (scene == null) return false;
                GameObject prefab = scene.GetPrefab(saplingName);
                if (prefab == null)
                {
                    Jotunn.Logger.LogInfo("[Hirdmandr] FarmerReplant: prefab '" + saplingName + "' not registered (unconfirmed sapling name?).");
                    return false;
                }
                int prefabHash = scene.GetPrefabHash(prefab);
                ZDO zdo = ZDOMan.instance.CreateNewZDO(position, prefabHash);
                zdo.SetPrefab(prefabHash);
                zdo.SetRotation(Quaternion.identity);
                // CreateNewZDO defaults to Persistent=false (m_dataFlags=0), and the ghost-init
                // Awake path (CreateObject with an existing ZDO) never copies the prefab's
                // m_persistent. The save collector (ZDOMan.AddObjectsPerChunk) only clones ZDOs
                // where get_Persistent() is true, so a non-persistent sapling is silently dropped
                // from the chunk file and vanishes on load. Set it explicitly, same as
                // HirdmandrNPC.DecideOwnershipInit does for rescued NPCs.
                zdo.Persistent = true;
                // Own it by the local session so it persists with the field, like a placed object.
                Player player = Player.m_localPlayer;
                if (player != null)
                {
                    var playerView = player.m_nview;
                    var playerZdo = (playerView != null) ? playerView.GetZDO() : null;
                    if (playerZdo != null)
                    {
                        zdo.SetOwner(playerZdo.GetOwner());
                    }
                }
                GameObject instance = scene.CreateObject(zdo);
                if (instance == null)
                {
                    Jotunn.Logger.LogInfo("[Hirdmandr] FarmerReplant: CreateObject returned null for '" + saplingName + "'.");
                    return false;
                }
                scene.AddInstance(zdo, instance.GetComponent<ZNetView>());
                // Diagnostic (PoR §9.5): the Plant's own SUpdate -> UpdateHealth gates growth on
                // Heightmap.IsCultivated(transform.position); if the placed transform is off the tilled
                // soil (Y too high / XZ off-plot) it sticks at m_status=NotCultivated and never grows.
                // Log the ACTUAL placed transform + whether it reads as cultivated so we can see it.
                Vector3 placedPos = instance.transform.position;
                Heightmap hm = Heightmap.FindHeightmap(placedPos);
                bool cultivated = (hm != null) ? hm.IsCultivated(placedPos) : false;
                // Jotunn.Logger.LogInfo("[Hirdmandr] FarmerReplant: placed '" + saplingName + "' at zdo(" + position.x.ToString("0") + "," + position.y.ToString("0") + "," + position.z.ToString("0") + ") transform(" + placedPos.x.ToString("0.00") + "," + placedPos.y.ToString("0.00") + "," + placedPos.z.ToString("0.00") + ") heightmap=" + (hm != null) + " cultivated=" + cultivated + " (ZDO " + zdo.m_uid + ")");
                return true;
            }

            // Farmer do-loop: pick the next harvestable crop from the work field (via the chest's
            // SelectFarmerTarget), recording the crop-table entry + source chest. null = none.
            private Pickable PickFarmerTarget()
            {
                if (hmAI.workSiteHMChest == null)
                {
                    return null;
                }
                Pickable crop;
                FarmerCrop entry;
                if (hmAI.workSiteHMChest.SelectFarmerTarget(out crop, out entry))
                {
                    hmAI.farmerTargetEntry = entry;
                    hmAI.farmerSourceChest = hmAI.workSiteHMChest;
                    return crop;
                }
                return null;
            }

            // Farmer: the "one plant" work. Re-check the replant item at harvest time (the chest may
            // have changed since the target was picked), harvest (drops the yield + destroys the crop
            // + plays the pick VFX), consume the replant item from the chest, and replant in place.
            // false if we can no longer replace it (or the crop is gone) -- caller abandons + re-scans.
            private bool DoFarmerStuff(Pickable crop)
            {
                if (crop == null || crop.transform == null || hmAI.workSiteHMChest == null)
                {
                    return false;
                }
                FarmerCrop entry = hmAI.farmerTargetEntry;
                // Re-check the invariant at harvest time: only proceed if we can still replace it.
                if (!hmAI.workSiteHMChest.m_container.m_inventory.HaveItem(entry.consumeItem))
                {
                    return false;
                }
                // The yield's ItemData, captured BEFORE the harvest: Interact destroys the crop,
                // and a "+1 output" bonus needs it to bank one extra full yield into the chest.
                ItemDrop.ItemData bonusYield = null;
                GameObject bonusDrop = crop.m_itemPrefab;
                if (bonusDrop != null)
                {
                    ItemDrop bonusIt = bonusDrop.GetComponent<ItemDrop>();
                    if (bonusIt != null && bonusIt.m_itemData != null)
                    {
                        bonusYield = bonusIt.m_itemData;
                    }
                }

                string yieldItem;
                Vector3 position;
                if (!FarmerHarvest(hmAI.m_hmHumanoid, crop, out yieldItem, out position))
                {
                    return false;
                }
                if (hmAI.workSiteHMChest.m_container.m_inventory.HaveItem(entry.consumeItem))
                {
                    hmAI.workSiteHMChest.m_container.m_inventory.RemoveOneItem(hmAI.workSiteHMChest.m_container.m_inventory.GetItem(entry.consumeItem));
                    hmAI.workSiteHMChest.m_container.Save();
                }
                FarmerReplant(position, entry.sapling);
                // Job done (harvest + replant): the Farmer plays the "interact" anim (owner-local + all clients).
                hmAI.m_hmnpc.FireArtisanInteractAnim();

                // Vanilla-style "+1 output": chance = (int)skill / 400 (0% at 0 skill, 25% at
                // 100, linear in whole points). A bonus harvest banks one extra FULL yield into
                // the work chest (a normal seed_onion harvest gives 3 seeds, so a bonus harvest
                // gives 6 total). The ground drop from the harvest itself is still banked by
                // FarmerCollectYields; no extra skill for the bonus.
                if (bonusYield != null && RollArtisanBonus(hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob)))
                {
                    if (hmAI.workSiteHMChest.m_container.m_inventory.AddItem(bonusYield))
                    {
                        hmAI.workSiteHMChest.m_container.Save();
                        // Jotunn.Logger.LogWarning("Farmer bonus: extra " + yieldItem + " yield added to chest");
                    }
                    hmAI.m_hmnpc.FireMpEffect(HMEffectKind.BonusYield, "farmer bonus yield (extra " + yieldItem + ")");
                }
                return true;
            }

            // Farmer: bank any crop-yield drops sitting on the ground around the field into the work
            // chest (the "collect" step). Matches by the drop's INTERNAL item name (a known crop yield),
            // not its prefab name, so it doesn't depend on item-prefab naming. Mirrors the furnace
            // ground-collect teardown (AddItem then a networked Destroy).
            private void FarmerCollectYields()
            {
                if (hmAI.workSiteHMChest == null)
                {
                    return;
                }
                foreach (ZDO zdo in Utils.GetAllZDOsInRange(hmAI.transform.position, Utils.FarmerRange))
                {
                    ZNetView view = ZNetScene.instance.FindInstance(zdo);
                    if (view == null)
                    {
                        continue;
                    }
                    GameObject go = view.gameObject;
                    ItemDrop it = go.GetComponent<ItemDrop>();
                    if (it == null || it.m_itemData == null || it.m_itemData.m_shared == null)
                    {
                        continue;
                    }
                    if (!FarmerCrops.IsKnownYield(it.m_itemData.m_shared.m_name))
                    {
                        continue;
                    }
                    if (hmAI.workSiteHMChest.m_container.m_inventory.AddItem(it.m_itemData))
                    {
                        hmAI.workSiteHMChest.m_container.Save();
                        if (hmAI.m_znetv.GetZDO() == null)
                        {
                            UnityEngine.Object.Destroy((UnityEngine.Object)(object)go);
                        }
                        else
                        {
                            ZNetScene.instance.Destroy(go);
                        }
                    }
                }
            }

            // ============================================================================
            // #94 CATCH-UP: missed artisan work, run as fast catch-up when a hirdmandr
            // re-enters simulation after an unsimulated gap (load / sleep / relog / owner
            // transfer). HirdmandrAI.CatchupCheckAndHeartbeat detects the gap and calls
            // RunCatchupWork (via the WorkDaySM forwarder below). Everything here is static;
            // the only per-NPC state is the HirdmandrAI passed in.
            //
            //   work_seconds = seconds inside the [workday] ToD window along the gap's path
            //   n            = floor(work_seconds / time_per_job)
            //   time_per_job = max(0.5, 18 - 0.18 * skill)          (the live job pacing)
            //   then n jobs of the NPC's TOP enabled job run FAST (no walk / anim / cooldowns)
            //   with the real world effects (chest inventory, RPCs, replanted crops, fuel),
            //   so it looks like the NPC simply kept working.
            //
            // Skill: n steps of the live ModifySkill curve (per spec, even if the world
            // absorbed fewer jobs), applied once. Bonus yield: (int)(nEff * p) extra per
            // effective job, p = (int)skill / 400 (the "free bonus" expected value). Log: one line.
            // ============================================================================

            // Work is counted only inside the [ArtisanWorkdayStart, ArtisanEveningSocialStart)
            // window of each day (HMSchedule is the source of truth), with a small overhead so
            // a caught-up workday is slightly shorter than a real one.
            private const float CATCHUP_WORK_OVERHEAD = 0.933f;

            // Item tables, built once from ObjectDB (EnsureCatchupItemTables). The ObjectDB name
            // (prefab.name, e.g. "Silver") is NOT the internal name (ItemData.m_shared.m_name,
            // e.g. "$item_silver"); the world runs on internal names, so ingots are resolved here
            // at runtime (the internal names are data, not compile-time constants).
            private static Dictionary<string, ItemDrop.ItemData> s_catchupProtoByInternal = new Dictionary<string, ItemDrop.ItemData>();
            private static Dictionary<string, string> s_catchupDBToInternal = new Dictionary<string, string>();
            private static bool s_catchupTablesBuilt = false;

            // Build the item tables once, from the ObjectDB's registered item prefabs.
            private static void EnsureCatchupItemTables()
            {
                if (s_catchupTablesBuilt) return;
                s_catchupTablesBuilt = true;
                foreach (GameObject prefab in ObjectDB.instance.m_items)
                {
                    ItemDrop drop = prefab.GetComponent<ItemDrop>();
                    if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                    {
                        continue;
                    }
                    string internalName = drop.m_itemData.m_shared.m_name;
                    s_catchupProtoByInternal[internalName] = drop.m_itemData;
                    s_catchupDBToInternal[prefab.name] = internalName;
                }
            }

            // ObjectDB name ("Silver") -> internal name ("$item_silver"); pass-through if the
            // argument is already an internal name; null if unknown.
            private static string ResolveInternal(string dbOrInternal)
            {
                string internalName;
                if (s_catchupDBToInternal.TryGetValue(dbOrInternal, out internalName))
                {
                    return internalName;
                }
                if (s_catchupProtoByInternal.ContainsKey(dbOrInternal))
                {
                    return dbOrInternal;
                }
                return null;
            }

            // A FRESH one-unit ItemData for internalName, cloned from the ObjectDB prefab's
            // ItemData (same pattern as SoulItem: Clone() keeps m_shared/m_dropPrefab intact,
            // and we never mutate the prefab's template itself).
            private static ItemDrop.ItemData GetFreshItemData(string internalName)
            {
                ItemDrop.ItemData proto;
                if (internalName == null || !s_catchupProtoByInternal.TryGetValue(internalName, out proto))
                {
                    return null;
                }
                ItemDrop.ItemData data = proto.Clone();
                data.m_stack = 1;
                return data;
            }

            // Add `count` units of internalName to the chest, one fresh one-unit ItemData at a
            // time (a single big-stack AddItem can overflow the item's max stack). No Save here:
            // the caller commits the whole chest with ONE Save at the end of the catch-up
            // executor. (Saving after each unit - like the live loops do across separate frames -
            // did NOT persist the additions when all done in a single frame; one final Save does.)
            // Stops early if the chest fills up or the prefab is missing.
            private static void AddItemsCount(HirdmandrChest chest, string internalName, int count)
            {
                if (chest == null || count <= 0 || internalName == null) return;
                Inventory inv = chest.m_container.m_inventory;
                for (int i = 0; i < count; i++)
                {
                    ItemDrop.ItemData data = GetFreshItemData(internalName);
                    if (data == null) return;
                    if (!inv.AddItem(data)) return; // chest full
                }
            }

            // Work seconds inside the [workday] window along the gap's ToD trajectory. The gap
            // may span whole days, so walk day-by-day: day k covers [k, k+1) in ToD space and
            // its work window is [k + ArtisanWorkdayStart, k + ArtisanEveningSocialStart).
            private static float CatchupWorkSeconds(float gapSeconds, float fromToD)
            {
                float a = fromToD;
                float b = fromToD + gapSeconds / HirdmandrAI.CATCHUP_DAY_SECONDS;
                float workFrac = 0f;
                for (int k = Mathf.FloorToInt(a); k <= Mathf.FloorToInt(b); k++)
                {
                    float lo = Mathf.Max(a, k + HMSchedule.ArtisanWorkdayStart);
                    float hi = Mathf.Min(b, k + HMSchedule.ArtisanEveningSocialStart);
                    if (hi > lo)
                    {
                        workFrac += hi - lo;
                    }
                }
                return workFrac * HirdmandrAI.CATCHUP_DAY_SECONDS * CATCHUP_WORK_OVERHEAD;
            }

            // The nearest chest this NPC knows that accepts job as a valid worksite (no claim:
            // catch-up must not steal a worksite from a currently-working NPC).
            private static HirdmandrChest FindCatchupWorksite(HirdmandrAI hmAI, string job)
            {
                HirdmandrChest best = null;
                float bestDist = float.MaxValue;
                foreach (string chestString in hmAI.npcChests)
                {
                    if (string.IsNullOrEmpty(chestString)) continue;
                    foreach (ZDO zdo in Utils.GetPrefabZDOsInRange(hmAI.transform.position, 100f, chestString))
                    {
                        ZNetView cv = ZNetScene.instance.FindInstance(zdo);
                        if (cv == null) continue;
                        GameObject go = cv.gameObject;
                        if (go == null) continue;
                        HirdmandrChest chest = go.GetComponent<HirdmandrChest>();
                        if (chest == null) continue;
                        bool siteOk = (job == "farmer") ? chest.IsValidCatchupFarmerSite(hmAI) : chest.IsValidWorksite(job);
                        if (!siteOk) continue;
                        float d = Vector3.Distance(hmAI.transform.position, chest.transform.position);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            best = chest;
                        }
                    }
                }
                return best;
            }

            // WOODBURNER: n jobs, each burning 1 wood -> then deliver the coal (nEff + bonus).
            // No kiln state to touch (the live loop's kiln animation is cosmetic; the output
            // is what matters). World-capped: stops when the chest runs out of wood.
            private static void CatchupWoodburner(HirdmandrChest chest, int n, float p, out int nEff)
            {
                nEff = 0;
                Inventory inv = chest.m_container.m_inventory;
                // Remove the wood first. No per-iteration Save (many Saves in one frame did not
                // persist; the chest is committed once at the end). World-capped: stop when the
                // chest runs out of wood.
                while (nEff < n && inv.HaveItem("$item_wood"))
                {
                    inv.RemoveOneItem(inv.GetItem("$item_wood"));
                    nEff++;
                }
                // Deliver the coal for every unit actually burned (nEff + free bonus), then
                // commit the whole chest once.
                AddItemsCount(chest, "$item_coal", nEff + (int)(nEff * p));
                chest.m_container.Save();
            }

            // FURNACEOPERATOR: n jobs, each smelting 1 ore (priority: silver > iron > copper >
            // tin) + 2 coal -> then deliver the ingots (nEff per kind + bonus). 2 coal per ingot
            // matches the live loop's steady state: coal is consumed EVERY job tick (fuel top-up
            // while fuel < max-1) but ore is added on ALTERNATING ticks (genericToggle), so the
            // simulation burns 1 ore + 2 coal per ingot - catchup must consume the same.
            // No smelter state to touch; the chest is both input and output, exactly like the
            // live loop.
            private static void CatchupFurnaceoperator(HirdmandrChest chest, int n, float p, out int nEff)
            {
                nEff = 0;
                Inventory inv = chest.m_container.m_inventory;
                int silver = 0;
                int iron = 0;
                int copper = 0;
                int tin = 0;
                // Remove 1 ore + 2 coal per job. No per-iteration Save (the chest is committed
                // once at the end). World-capped: stop when ore, coal, or the job count runs
                // out.
                while (nEff < n)
                {
                    string oreName = null;
                    if (inv.HaveItem("$item_silverore")) oreName = "$item_silverore";
                    else if (inv.HaveItem("$item_ironscrap")) oreName = "$item_ironscrap";
                    else if (inv.HaveItem("$item_copperore")) oreName = "$item_copperore";
                    else if (inv.HaveItem("$item_tinore")) oreName = "$item_tinore";
                    if (oreName == null || !inv.HaveItem("$item_coal") || inv.GetItem("$item_coal").m_stack < 2) break;
                    inv.RemoveOneItem(inv.GetItem(oreName));
                    inv.RemoveOneItem(inv.GetItem("$item_coal"));
                    inv.RemoveOneItem(inv.GetItem("$item_coal"));
                    nEff++;
                    if (oreName == "$item_silverore") silver++;
                    else if (oreName == "$item_ironscrap") iron++;
                    else if (oreName == "$item_copperore") copper++;
                    else tin++;
                }
                // Deliver the ingots for every ore actually smelted (per kind + free bonus),
                // then commit the whole chest once.
                AddItemsCount(chest, ResolveInternal("Silver"), silver + (int)(silver * p));
                AddItemsCount(chest, ResolveInternal("Iron"), iron + (int)(iron * p));
                AddItemsCount(chest, ResolveInternal("Copper"), copper + (int)(copper * p));
                AddItemsCount(chest, ResolveInternal("Tin"), tin + (int)(tin * p));
                chest.m_container.Save();
            }

            // LAMPLIGHTER: n jobs, each refueling the NEAREST fixture that still needs fuel -
            // exactly the live loop's per-cycle pick (shared picker + refueler), so the batch tops
            // up MANY fixtures (torches, sconces, braziers, firepits, hearths - anything with a
            // Fireplace component), not just one. Fuel is only consumed on the "paid" roll
            // (1 - no-consume chance), mirroring the live loop. World-capped: stops when no fixture
            // needs fuel, or the chest runs out of the fuel type a fixture wants.
            private static void CatchupLamplighter(HirdmandrAI hmAI, HirdmandrChest chest, int n, float skill, float p, out int nEff)
            {
                nEff = 0;
                Inventory inv = chest.m_container.m_inventory;
                // No-consume chance rises with skill (live: 0.20 at 1 -> 0.80 at 100); only a
                // "paid" refuel spends a chest item. Live re-reads the skill per refuel (before
                // that refuel's ModifySkill), so the chance climbs slightly as refuels land -
                // mirror that with a local copy advanced on the exact live curve (+1/floor(value),
                // the same step ApplyCatchupSkill applies), rolled BEFORE the advance, matching
                // the live order (roll, then ModifySkill).
                float curSkill = skill;
                for (int i = 0; i < n; i++)
                {
                    float noConsume = 0.20f + ((curSkill - 1f) * (0.60f / 99f));
                    // Per job: nearest fixture that still needs fuel AND whose fuel we carry (the
                    // shared live picker). None left -> the batch is done.
                    GameObject target = PickLamplighterTarget(hmAI, chest);
                    if (target == null) break;
                    if (!RefuelLamplighterCatchup(target)) break; // full / gone: nothing left to do
                    Fireplace fire = target.GetComponent<Fireplace>();
                    string fuelName = (fire != null && fire.m_fuelItem != null && fire.m_fuelItem.m_itemData != null)
                        ? fire.m_fuelItem.m_itemData.m_shared.m_name
                        : "$item_coal";
                    // "Paid" roll: spend one of the fuel THIS fixture actually burns.
                    if (UnityEngine.Random.value >= noConsume && inv.HaveItem(fuelName))
                    {
                        inv.RemoveOneItem(inv.GetItem(fuelName));
                    }
                    nEff++;
                    // Advance the roll's skill AFTER the roll (live: ModifySkill after the roll),
                    // exact live curve: +1/floor(value), +1 below level 1, capped at 100.
                    if (Math.Floor(curSkill) > 0f)
                    {
                        curSkill += (float)(1.0 / Math.Floor(curSkill));
                    }
                    else
                    {
                        curSkill += 1.0f;
                    }
                    if (curSkill > 100f) curSkill = 100f;
                }
                // Commit the chest once: the single final Save persists the fuel removals.
                chest.m_container.Save();
            }

            // MENDER: n jobs, each = 1 full repair (RPC_Repair, MP-visible) of the most
            // damaged-in-range player-placed piece (score = damage * distance). No chest
            // involved. World-capped: stops when no damaged piece is in range.
            private static void CatchupMender(HirdmandrAI hmAI, int n, out int nEff)
            {
                nEff = 0;
                for (int i = 0; i < n; i++)
                {
                    WearNTear target = null;
                    float bestScore = float.MaxValue;
                    foreach (WearNTear wnt in WearNTear.s_allInstances)
                    {
                        if (wnt == null) continue;
                        Piece piece = wnt.m_piece;
                        if (piece == null || !piece.IsPlacedByPlayer()) continue;
                        float hp = wnt.GetHealthPercentage();
                        if (hp <= 0f || hp >= 1f) continue; // unrepairable / already whole
                        float dx = piece.transform.position.x - hmAI.transform.position.x;
                        float dz = piece.transform.position.z - hmAI.transform.position.z;
                        float d = Mathf.Sqrt(dx * dx + dz * dz); // flat XZ, like the live scan
                        if (d > Utils.MenderRange) continue;
                        float score = hp * d;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            target = wnt;
                        }
                    }
                    if (target == null) return;
                    target.m_nview.InvokeRPC("RPC_Repair", 0L);
                    nEff++;
                }
            }

            // FARMER: n jobs, each = 1 harvest + replant of a mature crop in range (the chest
            // supplies the cultivator, exactly like the live loop). The base yields fall to
            // the ground (the game's own pick) and are swept into the chest at the end; the
            // FREE-BONUS yield is delivered straight to the chest. World-capped: stops when
            // crops or cultivators run out.
            private static void CatchupFarmer(HirdmandrAI hmAI, HirdmandrChest chest, int n, float p, out int nEff)
            {
                nEff = 0;
                Inventory inv = chest.m_container.m_inventory;
                List<Pickable> crops = chest.GetMatureCropsInRange();
                crops.Sort(delegate(Pickable a, Pickable b)
                {
                    float da = (a != null && a.transform != null) ? Vector3.Distance(chest.transform.position, a.transform.position) : float.MaxValue;
                    float db = (b != null && b.transform != null) ? Vector3.Distance(chest.transform.position, b.transform.position) : float.MaxValue;
                    return da.CompareTo(db);
                });
                Dictionary<string, int> yieldCounts = new Dictionary<string, int>();
                foreach (Pickable crop in crops)
                {
                    if (nEff >= n) break;
                    if (crop == null || crop.transform == null) continue;
                    // No NPC-reach check: the live-loop farmer WALKS to each crop inside the 15m
                    // chest work area (FarmerMoveTimeout), so the catch-up matches that work area
                    // (crops within FarmerRange of the chest, from GetMatureCropsInRange) rather
                    // than requiring the crop to be within reach of the NPC's current position.
                    string yi = chest.YieldsOf(crop);
                    if (yi == null) continue;
                    FarmerCrop entry;
                    if (!FarmerCrops.TryGetByYield(yi, out entry)) continue;
                    if (!inv.HaveItem(entry.consumeItem)) continue;
                    Vector3 spot;
                    if (!FarmerHarvest(hmAI.m_hmHumanoid, crop, out yi, out spot)) continue;
                    inv.RemoveOneItem(inv.GetItem(entry.consumeItem));
                    FarmerReplant(spot, entry.sapling);
                    int count;
                    yieldCounts.TryGetValue(yi, out count);
                    yieldCounts[yi] = count + 1;
                    nEff++;
                }
                foreach (KeyValuePair<string, int> kv in yieldCounts)
                {
                    FarmerCrop entry;
                    if (!FarmerCrops.TryGetByYield(kv.Key, out entry)) continue;
                    // FREE-BONUS: expected-value bonus yield for this yield type, delivered
                    // straight to the chest (the base yield went in via the ground sweep).
                    AddItemsCount(chest, kv.Key, (int)(kv.Value * p) * entry.yieldAmount);
                }
                CatchupCollectYields(hmAI, chest);
                // Commit the whole chest once (cultivator removed + bonus yields + ground sweep);
                // one final Save persists, many per-iteration Saves did not.
                chest.m_container.Save();
            }

            // The catch-up version of FarmerCollectYields: sweep the ground drops near the NPC
            // into the chest (same range / filter / destroy as the live loop).
            private static void CatchupCollectYields(HirdmandrAI hmAI, HirdmandrChest chest)
            {
                foreach (ZDO zdo in Utils.GetAllZDOsInRange(hmAI.transform.position, Utils.FarmerRange))
                {
                    ZNetView cv = ZNetScene.instance.FindInstance(zdo);
                    if (cv == null) continue;
                    GameObject go = cv.gameObject;
                    if (go == null) continue;
                    ItemDrop it = go.GetComponent<ItemDrop>();
                    if (it == null || it.m_itemData == null || it.m_itemData.m_shared == null) continue;
                    if (!FarmerCrops.IsKnownYield(it.m_itemData.m_shared.m_name)) continue;
                    if (chest.m_container.m_inventory.AddItem(it.m_itemData))
                    {
                        if (hmAI.m_znetv.GetZDO() == null)
                        {
                            UnityEngine.Object.Destroy((UnityEngine.Object)(object)go);
                        }
                        else
                        {
                            ZNetScene.instance.Destroy(go);
                        }
                    }
                }
            }

            // Skill: n steps of the live ModifySkill curve (per spec, regardless of how many
            // jobs the world actually absorbed), applied once via SetSkillValue (which also
            // persists to the ZDO). If the floor crosses an integer, fire the level-up effect
            // + the socialize moment, exactly like ModifySkill does.
            private static void ApplyCatchupSkill(HirdmandrAI hmAI, string job, int n, float startSkill)
            {
                if (n <= 0) return;
                HMSkills skills = hmAI.m_hmnpc.m_skills;
                if (skills == null) return;
                float cur = startSkill;
                for (int i = 0; i < n; i++)
                {
                    // The exact live curve: +1 below level 1, else +1/floor(value) (change=1).
                    if (Math.Floor(cur) > 0f)
                    {
                        cur += (float)(1.0 / Math.Floor(cur));
                    }
                    else
                    {
                        cur += 1.0f;
                    }
                    if (cur < 0f) cur = 0f;
                    if (cur > 100f) cur = 100f;
                }
                skills.SetSkillValue(job, cur);
                int floorStart = (int)Math.Floor(startSkill);
                int floorEnd = (int)Math.Floor(cur);
                if (floorEnd > floorStart)
                {
                    hmAI.m_hmnpc.FireMpEffect(HMEffectKind.SkillLevelup, job + ":" + floorEnd);
                    if (job != "fighter" && job != "melee" && job != "range")
                    {
                        hmAI.m_hmnpc.m_socialize.EnqueueSkillup(job, SkillReadable(job), floorEnd);
                    }
                }
            }

            // The readable name for a job key ("woodburner" -> "Wood Burner"); falls back to
            // the key itself if the skill table doesn't know it.
            private static string SkillReadable(string job)
            {
                foreach (HMSkills.SkillDef def in HMSkills.m_skillDefs)
                {
                    if (def.key == job) return def.readable;
                }
                return job;
            }

            // #94 entry point (HirdmandrAI.RunCatchupWork -> WorkDaySM.RunCatchupWork -> here).
            // Runs the top enabled job for the work seconds the unsim gap covers, FAST, with
            // the real world effects.
            public static void RunCatchupWork(HirdmandrAI hmAI, float gapSeconds, float storedToD)
            {
                if (hmAI == null || hmAI.m_hmnpc == null || gapSeconds <= 0f) return;
                EnsureCatchupItemTables();
                HMSkills skills = hmAI.m_hmnpc.m_skills;
                if (skills == null) return;
                List<string> jobs = skills.GetEnabledSkillsHighestFirst();
                if (jobs == null || jobs.Count == 0) return;
                string job = jobs[0];
                float skill = skills.GetSkill(job);
                float timePerJob = 18f - 0.18f * skill;
                if (timePerJob < 0.5f) timePerJob = 0.5f;
                float workSeconds = CatchupWorkSeconds(gapSeconds, storedToD);
                int n = (int)Mathf.Floor(workSeconds / timePerJob);
                if (n < 1) return;
                HirdmandrChest chest = null;
                if (job != "mender")
                {
                    chest = FindCatchupWorksite(hmAI, job);
                    if (chest == null) return;
                }
                float p = (int)skill / 400f;
                int nEff = 0;
                if (job == "woodburner")
                {
                    CatchupWoodburner(chest, n, p, out nEff);
                }
                else if (job == "furnaceoperator")
                {
                    CatchupFurnaceoperator(chest, n, p, out nEff);
                }
                else if (job == "lamplighter")
                {
                    CatchupLamplighter(hmAI, chest, n, skill, p, out nEff);
                }
                else if (job == "mender")
                {
                    CatchupMender(hmAI, n, out nEff);
                }
                else if (job == "farmer")
                {
                    CatchupFarmer(hmAI, chest, n, p, out nEff);
                }
                else
                {
                    return; // non-artisan job: nothing to catch up
                }
                // nEff (not n): the world only absorbed nEff jobs (capped by wood/seeds/crops in
                // range), so the artisan did nEff jobs (idle for the rest) -> skill + log use nEff.
                ApplyCatchupSkill(hmAI, job, nEff, skill);
                Jotunn.Logger.LogInfo(hmAI.m_hmHumanoid.m_name + " processed " + nEff + " " + SkillReadable(job) + " while not simulated.");
            }
        }

        // #94 catch-up forwarder: HirdmandrAI.RunCatchupWork lands here (the AI class doesn't
        // reach into the work-day internals); the actual work logic is on NodeDoJob.
        public static void RunCatchupWork(HirdmandrAI hmAI, float gapSeconds, float storedToD)
        {
            NodeDoJob.RunCatchupWork(hmAI, gapSeconds, storedToD);
        }
    }
}
