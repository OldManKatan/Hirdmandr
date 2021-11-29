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

            public string no_imp = "RestSM.NodeFindBed not implemented";

            override public void EnterFrom(int aState)
            {
                hmAI.moveToReached = false;
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                if (hmAI.m_hmnpc.ownedBedZDOID != ZDOID.None)
                {
                    Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " has a bed!");
                    hmAI.restBedZDO = ZDOMan.instance.GetZDO(hmAI.m_hmnpc.ownedBedZDOID);
                    if (hmAI.restBedZDO is null)
                    {
                        hmAI.m_hmnpc.ownedBedZDOID = ZDOID.None;
                    }
                    else
                    {
                        Jotunn.Logger.LogWarning("  " + hmAI.m_hmHumanoid.m_name + " and it exists.");
                        parentSM.ChangeState("goBed");
                        return;
                    }
                }

                Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " is trying to claim a bed!");
                foreach (string npcBedStr in new string[] { "piece_npc_bed02", "piece_npc_bed" })
                {
                    foreach (ZDO thisBed in Utils.GetPrefabZDOsInRange(hmAI.transform.position, 100f, npcBedStr))
                    {
                        if (thisBed.GetZDOID("hmnpc_bedOwnerZDOID") == ZDOID.None)
                        {
                            Jotunn.Logger.LogWarning("  " + hmAI.m_hmHumanoid.m_name + " found an unclaimed bed.");
                            ZNetView bedInstance = ZNetScene.instance.FindInstance(thisBed);
                            if (bedInstance.GetComponent<HirdmandrBed>().Claim(hmAI.m_znetv.GetZDO().m_uid))
                            {
                                Jotunn.Logger.LogWarning("    " + hmAI.m_hmHumanoid.m_name + " has successfully claimed a bed!");
                                hmAI.restBedZDO = thisBed;
                                hmAI.m_hmnpc.SetBed(thisBed.m_uid);
                                parentSM.ChangeState("goBed");
                                return;
                            }
                            Jotunn.Logger.LogWarning("    " + hmAI.m_hmHumanoid.m_name + " FAILED to claim a bed!");
                        }
                    }

                }

                if (hmAI.m_hmnpc.ownedBedZDOID == ZDOID.None)
                {
                    Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " can't find a bed!!");
                }

            }
        }

        public class NodeGoBed : SMNode
        {
            public NodeGoBed(RestSM psm) : base(psm) { }

            public string no_imp = "RestSM.NodeGoBed not implemented";

            override public void EnterFrom(int aState)
            {
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
                    parentSM.ChangeState("findBed");
                }
            }
        }
        public class NodeAtBed : SMNode
        {
            public NodeAtBed(RestSM psm) : base(psm) { }

            public string no_imp = "RestSM.NodeAtBed not implemented";

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }

            override public void RunState() { }
        }
    }
}
