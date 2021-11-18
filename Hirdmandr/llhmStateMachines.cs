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
        SocializeSM()
        {
            // public HirdmandrAI.NodeSocialize.socialStates enum_states;
            
            public enum sts
            {
                findMeetPoint,
                goMeetPoint,
                goIdlePoint,
                atIdlePoint,
                setupSocialize,
                startSocialize
            };
            
            public int changeTopState = -1;
            
            AddState(sts.findMeetPoint, new NodeFindMeetPoint());
            AddState(sts.goMeetPoint, new NodeGoMeetPoint());
            AddState(sts.goIdlePoint, new NodeGoIdlePoint());
            AddState(sts.atIdlePoint, new NodeAtIdlePoint());
            AddState(sts.setupSocialize, new NodeSetupSocialize());
            AddState(sts.startSocialize, new NodeStartSocialize());
        }

        public class NodeFindMeetPoint : SMNode
        {
            public string no_imp = "SocializeSM.NodeFindMeetPoint not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoMeetPoint : SMNode
        {
            public string no_imp = "SocializeSM.NodeGoMeetPoint not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoIdlePoint : SMNode
        {
            public string no_imp = "SocializeSM.NodeGoIdlePoint not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtIdlePoint : SMNode
        {
            public string no_imp = "SocializeSM.NodeAtIdlePoint not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeSetupSocialize : SMNode
        {
            public string no_imp = "SocializeSM.NodeSetupSocialize not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeStartSocialize : SMNode
        {
            public string no_imp = "SocializeSM.NodeStartSocialize not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class WorkDaySM : StateMachine 
    {
        WorkDaySM(enum these_states)
        {
            public enum sts
            {
                resetArtJob,
                setupArtJob,
                goArtJob,
                doJob
            };
           
            public int changeTopState = -1;
            
            AddState(sts.resetArtJob, new NodeResetArtJob());
            AddState(sts.setupArtJob, new NodeSetupArtJob());
            AddState(sts.goArtJob, new NodeGoArtJob());
            AddState(sts.doJob, new NodeDoJob());
        }

        public class NodeResetArtJob : SMNode
        {
            public string no_imp = "WorkDaySM.NodeResetArtJob not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeSetupArtJob : SMNode
        {
            public string no_imp = "WorkDaySM.NodeSetupArtJob not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoArtJob : SMNode
        {
            public string no_imp = "WorkDaySM.NodeGoArtJob not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeDoJob : SMNode
        {
            public string no_imp = "WorkDaySM.NodeDoJob not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class RestSM : StateMachine 
    {
        RestSM(enum these_states)
        {
            public enum restStates
            {
                findBed,
                goBed,
                atBed
            };
           
            public int changeTopState = -1;
            
            AddState(sts.findBed, new NodeFindBed());
            AddState(sts.goBed, new NodeGoBed());
            AddState(sts.atBed, new NodeAtBed());
        }

        public class NodeFindBed : SMNode
        {
            public string no_imp = "RestSM.NodeFindBed not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoBed : SMNode
        {
            public string no_imp = "RestSM.NodeGoBed not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtBed : SMNode
        {
            public string no_imp = "RestSM.NodeAtBed not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class SelfCareSM : StateMachine 
    {
        SelfCareSM(enum these_states)
        {
            public enum sts
            {
                findFood,
                goFood,
                atFood
            };
           
            public int changeTopState = -1;
            
            AddState(sts.findFood, new NodeFindFood());
            AddState(sts.goFood, new NodeGoFood());
            AddState(sts.atFood, new NodeAtFood());
        }

        public class NodeFindFood : SMNode
        {
            public string no_imp = "selfCareSM.NodeFindFood not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoFood : SMNode
        {
            public string no_imp = "selfCareSM.NodeGoFood not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtFood : SMNode
        {
            public string no_imp = "selfCareSM.NodeAtFood not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class PatrolSM : StateMachine 
    {
        PatrolSM(enum these_states)
        {
            public enum sts
            {
                setupPatrol,
                goPost,
                atPost,
                isAlerted
            };

            public int changeTopState = -1;
            
            AddState(sts.setupPatrol, new NodeSetupPatrol());
            AddState(sts.goPost, new NodeGoPost());
            AddState(sts.atPost, new NodeAtPost());
            AddState(sts.isAlerted, new NodeIsAlerted());
        }

        public class NodeSetupPatrol : SMNode
        {
            public string no_imp = "PatrolSM.NodeSetupPatrol not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoPost : SMNode
        {
            public string no_imp = "PatrolSM.NodeGoPost not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtPost : SMNode
        {
            public string no_imp = "PatrolSM.NodeAtPost not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeIsAlerted : SMNode
        {
            public string no_imp = "PatrolSM.NodeIsAlerted not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class DepressedSM : StateMachine 
    {
        DepressedSM(enum these_states)
        {
            public enum sts
            {
                startDepressed,
                findComfort,
                goComfort,
                whine
            };

            public int changeTopState = -1;
            
            AddState(sts.startDepressed, new NodeStartDepressed());
            AddState(sts.findComfort, new NodeFindComfort());
            AddState(sts.goComfort, new NodeGoComfort());
            AddState(sts.whine, new NodeWhine());
        }

        public class NodeStartDepressed : SMNode
        {
            public string no_imp = "DepressedSM.NodeStartDepressed not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeFindComfort : SMNode
        {
            public string no_imp = "DepressedSM.NodeFindComfort not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoComfort : SMNode
        {
            public string no_imp = "DepressedSM.NodeGoComfort not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeWhine : SMNode
        {
            public string no_imp = "DepressedSM.NodeWhine not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class RunInTerrorSM : StateMachine 
    {
        RunInTerrorSM(enum these_states)
        {
            public enum sts
            {
                callForHelp,
                escapeHelp,
                escapeAny,
                panic
            };
           
            public int changeTopState = -1;
            
            AddState(sts.callForHelp, new NodeCallForHelp());
            AddState(sts.escapeHelp, new NodeEscapeHelp());
            AddState(sts.escapeAny, new NodeEscapeAny());
            AddState(sts.panic, new NodePanic());
        }

        public class NodeCallForHelp : SMNode
        {
            public string no_imp = "RunInTerrorSM.NodeCallForHelp not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeEscapeHelp : SMNode
        {
            public string no_imp = "RunInTerrorSM.NodeEscapeHelp not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeEscapeAny : SMNode
        {
            public string no_imp = "RunInTerrorSM.NodeEscapeAny not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodePanic : SMNode
        {
            public string no_imp = "RunInTerrorSM.NodePanic not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class HideSM : StateMachine 
    {
        HideSM(enum these_states)
        {
            public enum sts
            {
                findSafe,
                goSafe,
                atSafe
            };

            public int changeTopState = -1;
            
            AddState(sts.findSafe, new NodeFindSafe());
            AddState(sts.goSafe, new NodeGoSafe());
            AddState(sts.atSafe, new NodeAtSafe());
        }

        public class NodeFindSafe : SMNode
        {
            public string no_imp = "HideSM.NodeFindSafe not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoSafe : SMNode
        {
            public string no_imp = "HideSM.NodeGoSafe not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtSafe : SMNode
        {
            public string no_imp = "HideSM.NodeAtSafe not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class DefendHomeSM : StateMachine 
    {
        DefendHomeSM(enum these_states)
        {
            public enum sts
            {
                isAlerted,
                findNeedsHelp,
                goNeedsHelp,
                findThreat,
                goThreat,
                caution
            };

            public int changeTopState = -1;
            
            AddState(sts.isAlerted, new NodeIsAlerted());
            AddState(sts.findNeedsHelp, new NodeFindNeedsHelp());
            AddState(sts.goNeedsHelp, new NodeGoNeedsHelp());
            AddState(sts.findThreat, new NodeFindThreat());
            AddState(sts.goThreat, new NodeGoThreat());
            AddState(sts.caution, new NodeCaution());
        }

        public class NodeIsAlerted : SMNode
        {
            public string no_imp = "DefendHomeSM.NodeIsAlerted not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeFindNeedsHelp : SMNode
        {
            public string no_imp = "DefendHomeSM.NodeFindNeedsHelp not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoNeedsHelp : SMNode
        {
            public string no_imp = "DefendHomeSM.NodeGoNeedsHelp not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeFindThreat : SMNode
        {
            public string no_imp = "DefendHomeSM.NodeFindThreat not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoThreat : SMNode
        {
            public string no_imp = "DefendHomeSM.NodeGoThreat not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeCaution : SMNode
        {
            public string no_imp = "DefendHomeSM.NodeCaution not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }
}
