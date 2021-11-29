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
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                hmAI.socTargetFire = null;

                List<ZDO> npc_campfires = Utils.GetPrefabZDOsInRange(hmAI.transform.position, 100f, "piece_npc_fire_pit");

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

                hmAI.TryToMove(hmAI.socMeetPoint, 2f, 3f, 6f, false, 30f);
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
                    parentSM.ChangeState("setupSocialize");
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
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                Jotunn.Logger.LogWarning("  Going to a random idle position, NO MEET POINT");
                for (var i = 0; i < 10; i++)
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

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                hmAI.m_hmnpc.Say("I am Idling!");
            }
        }
        public class NodeSetupSocialize : SMNode
        {
            public NodeSetupSocialize(SocializeSM psm) : base(psm) { }

            public string no_imp = "SocializeSM.NodeSetupSocialize not implemented";

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                hmAI.m_hmnpc.Say("I am socializing!");
            }
        }
        public class NodeStartSocialize : SMNode
        {
            public NodeStartSocialize(SocializeSM psm) : base(psm) { }

            public string no_imp = "SocializeSM.NodeStartSocialize not implemented";

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState() { }
        }
    }
}
