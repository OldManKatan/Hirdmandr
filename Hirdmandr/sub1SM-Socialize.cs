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

            // The walk-to-the-fire part: findMeetPoint -> goMeetPoint (arrived) or
            // findMeetPoint -> goIdlePoint (no fire, arrived) both land in the steady "socialize"
            // state, which is where the murmur actually happens. The talk loop itself lives in
            // HMSocialize.MaybeTalk; the state machine owns WHEN it runs, the helper owns WHAT
            // is said (line pools + the ZDO moment queue) -- same split as HMSkills.
            AddState("findMeetPoint", new NodeFindMeetPoint(this));
            AddState("goMeetPoint", new NodeGoMeetPoint(this));
            AddState("goIdlePoint", new NodeGoIdlePoint(this));
            AddState("socialize", new NodeSocialize(this));
        }

        public class NodeFindMeetPoint : SMNode
        {
            public NodeFindMeetPoint(SocializeSM psm) : base(psm) { }

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

                List<ZDO> npc_fires = Utils.GetNpcGatheringFiresInRange(hmAI.transform.position, 100f);

                if (npc_fires.Count == 0)
                {
                    parentSM.ChangeState("goIdlePoint");
                    return;
                }

                float closestSoFar = 999999f;
                float thisDistance;

                foreach (ZDO aZDO in npc_fires)
                {
                    thisDistance = Vector3.Distance(aZDO.GetPosition(), hmAI.transform.position);
                    if (thisDistance < closestSoFar && hmAI.socNoPathFires.IndexOf(aZDO) == -1)
                    {
                        closestSoFar = thisDistance;
                        hmAI.socTargetFire = aZDO;
                    }
                }

                if (!(hmAI.socTargetFire is null) && hmAI.socTargetFire.GetPosition() != Vector3.zero)
                {
                    hmAI.socMeetPoint = hmAI.socTargetFire.GetPosition();
                    parentSM.ChangeState("goMeetPoint");
                    return;
                }
                else
                {
                    parentSM.ChangeState("goIdlePoint");
                    return;
                }
            }
        }
        public class NodeGoMeetPoint : SMNode
        {
            public NodeGoMeetPoint(SocializeSM psm) : base(psm) { }

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
                    parentSM.ChangeState("socialize");
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
                for (var i = 0; i < 10; i++)
                {
                    hmAI.moveToPos = new Vector3(
                        hmAI.socMeetPoint.x + UnityEngine.Random.Range(-20f, 20f),
                        hmAI.socMeetPoint.y,
                        hmAI.socMeetPoint.z + UnityEngine.Random.Range(-20f, 20f)
                        );
                    if (hmAI.m_hmMonsterAI.FindPath(hmAI.moveToPos))
                    {
                        hmAI.m_hmMonsterAI.MoveTowards(hmAI.moveToPos, true);
                        if (Vector3.Distance(hmAI.transform.position, hmAI.moveToPos) < 3)
                        {
                            hmAI.m_hmMonsterAI.StopMoving();

                            parentSM.ChangeState("socialize");
                            break;
                        }
                    }
                }
            }
        }

        // Steady "gathered" state: the NPC has reached the fire (or, failing that, a nearby idle
        // point) and is no longer walking. This is where the socialize actually happens -- the
        // murmur. The talk loop (cadence + moment-pop + line pick) lives in HMSocialize.MaybeTalk;
        // this node just runs it once per tick. The dead no-op states this replaces (atIdlePoint /
        // setupSocialize / startSocialize) never did any work; the talking was always external.
        public class NodeSocialize : SMNode
        {
            public NodeSocialize(SocializeSM psm) : base(psm) { }

            override public void EnterFrom(int aState)
            {
                // Spread this NPC's first line over a 25s window from the moment it gathers, so
                // several NPCs at one fire do not all talk in unison. m_nextTalk is private, so the
                // SM drives the helper's stagger rather than touching the field.
                hmNPC.m_socialize.StaggerGather();
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                // MaybeTalk is self-gating: a no-op while still walking (moveToTrying) and on its
                // own 20-40s cadence, so one call per tick is all that's needed.
                hmNPC.m_socialize.MaybeTalk();
            }
        }
    }
}
