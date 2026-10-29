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

            override public void EnterFrom(int aState)
            {
                hmAI.moveToReached = false;
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                if (hmAI.m_hmnpc.ownedBedZDOID != ZDOID.None)
                {
                    // Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " has a bed!");
                    hmAI.restBedZDO = ZDOMan.instance.GetZDO(hmAI.m_hmnpc.ownedBedZDOID);
                    if (hmAI.restBedZDO is null)
                    {
                        // The bed's ZDO no longer resolves (it was destroyed): free our end of
                        // the claim and persist it, instead of dangling the stale ZDOID.
                        hmAI.m_hmnpc.ReleaseBed();
                    }
                    else
                    {
                        // Jotunn.Logger.LogWarning("  " + hmAI.m_hmHumanoid.m_name + " and it exists.");
                        parentSM.ChangeState("goBed");
                        return;
                    }
                }

                // Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " is trying to claim a bed!");
                foreach (string npcBedStr in new string[] { "piece_npc_bed02", "piece_npc_bed" })
                {
                    // 50 m is the bed "in range" distance: matches the goBed out-of-range release
                    // threshold, so a freshly claimed bed is always one goBed will actually walk
                    // to (no release -> re-find -> re-claim loop for beds just past the range).
                    foreach (ZDO thisBed in Utils.GetPrefabZDOsInRange(hmAI.transform.position, 50f, npcBedStr))
                    {
                        if (thisBed.GetZDOID("hmnpc_bedOwnerZDOID") == ZDOID.None)
                        {
                            // Jotunn.Logger.LogWarning("  " + hmAI.m_hmHumanoid.m_name + " found an unclaimed bed.");
                            ZNetView bedInstance = ZNetScene.instance.FindInstance(thisBed);
                            if (bedInstance.GetComponent<HirdmandrBed>().Claim(hmAI.m_znetv.GetZDO().m_uid))
                            {
                                // Jotunn.Logger.LogWarning("    " + hmAI.m_hmHumanoid.m_name + " has successfully claimed a bed!");
                                hmAI.restBedZDO = thisBed;
                                hmAI.m_hmnpc.SetBed(thisBed.m_uid);
                                parentSM.ChangeState("goBed");
                                return;
                            }
                            // Jotunn.Logger.LogWarning("    " + hmAI.m_hmHumanoid.m_name + " FAILED to claim a bed!");
                        }
                    }

                }

                if (hmAI.m_hmnpc.ownedBedZDOID == ZDOID.None)
                {
                    // Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " can't find a bed!!");
                }

            }
        }

        public class NodeGoBed : SMNode
        {
            public NodeGoBed(RestSM psm) : base(psm) { }

            override public void EnterFrom(int aState)
            {
                // Out-of-range safeguard (mirrors the worksite claim rules): a claimed bed more than
                // 50 m away is not worth the walk (and FindPath may not even reach it). Free the
                // bed (both sides, persisted) and hunt for a closer one instead of walking to a bed
                // we cannot use.
                if (hmAI.restBedZDO is null || Vector3.Distance(hmAI.transform.position, hmAI.restBedZDO.GetPosition()) > 50f)
                {
                    hmAI.m_hmnpc.ReleaseBed();
                    // First-time bed problem: the bed is gone or too far (>50 m) to be worth the walk.
                    HMTutorial.Show(HMTutorial.BedTooFar);
                    parentSM.ChangeState("findBed");
                    return;
                }
                hmAI.TryToMove(hmAI.restBedZDO.GetPosition(), 5f, 1f, 5f, true, 30f);

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
                    parentSM.ChangeState("atBed");
                }
                else
                {
                    // The 30 s walk to the bed never completed (unreachable / out of the pathable
                    // area): the claim is stale. Free the bed (both sides, persisted) and re-find,
                    // exactly like the worksite path does on a failed walk — otherwise findBed ->
                    // goBed loops forever with the claim retained.
                    hmAI.m_hmnpc.ReleaseBed();
                    // First-time bed problem: a bed was claimed but could not be reached.
                    HMTutorial.Show(HMTutorial.BedTooFar);
                    parentSM.ChangeState("findBed");
                }
            }
        }
        public class NodeAtBed : SMNode
        {
            public NodeAtBed(RestSM psm) : base(psm) { }

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState)
            {
                var human = hmAI.GetComponent<NPCPlayerClone>();
                human.m_sleeping = false;
                human.AttachStop();
                // #54: AttachStop leaves the NPC standing on the bed collider, where it cannot
                // path anywhere. Step it off to a clear floor spot beside the bed (no-op if it is
                // already off the bed or no open spot is found).
                hmAI.m_hmnpc.ScheduleStepOffBed("at-bed-exit (RestSM.NodeAtBed.ExitTo)", 2f);
            }

            override public void RunState() 
            {
                Humanoid human = hmAI.GetComponent<Humanoid>();
                GameObject bedObject = ZNetScene.instance.FindInstance(hmAI.m_hmnpc.ownedBedZDOID);
                Bed bedComp = (bedObject != null) ? bedObject.GetComponent<Bed>() : null;

                // The bed can vanish between findBed and here (destroyed, taken, or its ZDOID no
                // longer resolves to a live instance). Log exactly which link died, drop the stale
                // claim, and re-find instead of throwing NRE from RunState every tick.
                if (bedObject == null || bedComp == null)
                {
                    // Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " atBed: bed no longer usable "
                    //     + "(object=" + bedObject + ", bedComp=" + bedComp
                    //     + ", ownedBedZDOID=" + hmAI.m_hmnpc.ownedBedZDOID + "). Re-finding bed.");
                    hmAI.m_hmnpc.ReleaseBed();
                    parentSM.ChangeState("findBed");
                    return;
                }

                // Re-verify ownership right before lying down: the bed may have been freed and
                // re-claimed by another NPC between goBed and here (or our ref is stale). Never
                // attach to a bed we do not own — this is the last line of defense against two
                // NPCs ending up in the same bed.
                HirdmandrBed bedHm = bedObject.GetComponent<HirdmandrBed>();
                ZDOID bedOwner = (bedHm != null && bedHm.m_znetv != null)
                    ? bedHm.m_znetv.GetZDO().GetZDOID("hmnpc_bedOwnerZDOID")
                    : ZDOID.None;
                ZDOID myZDOID = hmAI.m_znetv.GetZDO().m_uid;
                if (bedOwner != myZDOID)
                {
                    // Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " atBed: bed owner mismatch "
                    //     + "(bedOwner=" + bedOwner + ", me=" + myZDOID + "). Releasing and re-finding.");
                    hmAI.m_hmnpc.ReleaseBed();
                    parentSM.ChangeState("findBed");
                    return;
                }

                // First-time: the hirdmandr is actually lying down to rest (once-only).
                HMTutorial.Show(HMTutorial.FirstRest);
                human.AttachStart(bedComp.m_spawnPoint, bedObject, true, true, false, "attach_bed", new Vector3(0f, 0.5f, 0f), null);
            }
        }
    }
}
