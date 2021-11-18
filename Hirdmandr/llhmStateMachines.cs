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
    public class SocializeSM extends StateMachine 
    {
        SocializeSM(HirdmandrAI.NodeSocialize.socialStates these_states)
        {
            public HirdmandrAI.NodeSocialize.socialStates enum_states;
            // public enum socialStates
            // {
            //     findMeetPoint,
            //     goMeetPoint,
            //     goIdlePoint,
            //     atIdlePoint,
            //     setupSocialize,
            //     startSocialize
            // };
            
            public int changeTopState = -1;
            
            AddState((int)enum_states.findMeetPoint, new NodeFindMeetPoint());
            AddState((int)enum_states.goMeetPoint, new NodeGoMeetPoint());
            AddState((int)enum_states.goIdlePoint, new NodeGoIdlePoint());
            AddState((int)enum_states.atIdlePoint, new NodeAtIdlePoint());
            AddState((int)enum_states.setupSocialize, new NodeSetupSocialize());
            AddState((int)enum_states.startSocialize, new NodeStartSocialize());
        }

        public class NodeFindMeetPoint : HMNode
        {
            public string no_imp = "SocializeSM.NodeFindMeetPoint not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoMeetPoint : HMNode
        {
            public string no_imp = "SocializeSM.NodeGoMeetPoint not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoIdlePoint : HMNode
        {
            public string no_imp = "SocializeSM.NodeGoIdlePoint not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtIdlePoint : HMNode
        {
            public string no_imp = "SocializeSM.NodeAtIdlePoint not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeSetupSocialize : HMNode
        {
            public string no_imp = "SocializeSM.NodeSetupSocialize not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeStartSocialize : HMNode
        {
            public string no_imp = "SocializeSM.NodeStartSocialize not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class WorkDaySM : OldManSM 
    {
        WorkDaySM(enum these_states)
        {
            public enum enum_states = these_states;
            public enum workStates
            {
                resetArtJob,
                setupArtJob,
                goArtJob,
                doJob
            };
           
            public int changeTopState = -1;
            
            AddState(enum_states.resetArtJob, new NodeResetArtJob());
            AddState(enum_states.setupArtJob, new NodeSetupArtJob());
            AddState(enum_states.goArtJob, new NodeGoArtJob());
            AddState(enum_states.doJob, new NodeDoJob());
        }

        public class NodeResetArtJob : HMNode
        {
            public string no_imp = "WorkDaySM.NodeResetArtJob not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeSetupArtJob : HMNode
        {
            public string no_imp = "WorkDaySM.NodeSetupArtJob not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoArtJob : HMNode
        {
            public string no_imp = "WorkDaySM.NodeGoArtJob not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeDoJob : HMNode
        {
            public string no_imp = "WorkDaySM.NodeDoJob not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class RestSM : OldManSM 
    {
        RestSM(enum these_states)
        {
            public enum enum_states = these_states;
            public enum restStates
            {
                findBed,
                goBed,
                atBed
            };
           
            public int changeTopState = -1;
            
            AddState(enum_states.findBed, new NodeFindBed());
            AddState(enum_states.goBed, new NodeGoBed());
            AddState(enum_states.atBed, new NodeAtBed());
        }

        public class NodeFindBed : HMNode
        {
            public string no_imp = "RestSM.NodeFindBed not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoBed : HMNode
        {
            public string no_imp = "RestSM.NodeGoBed not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtBed : HMNode
        {
            public string no_imp = "RestSM.NodeAtBed not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class SelfCareSM : OldManSM 
    {
        SelfCareSM(enum these_states)
        {
            public enum enum_states = these_states;
            public enum selfCareStates
            {
                findFood,
                goFood,
                atFood
            };
           
            public int changeTopState = -1;
            
            AddState(enum_states.findFood, new NodeFindFood());
            AddState(enum_states.goFood, new NodeGoFood());
            AddState(enum_states.atFood, new NodeAtFood());
        }

        public class NodeFindFood : HMNode
        {
            public string no_imp = "selfCareSM.NodeFindFood not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoFood : HMNode
        {
            public string no_imp = "selfCareSM.NodeGoFood not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtFood : HMNode
        {
            public string no_imp = "selfCareSM.NodeAtFood not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class PatrolSM : OldManSM 
    {
        PatrolSM(enum these_states)
        {
            public enum enum_states = these_states;
            public enum patrolStates
            {
                setupPatrol,
                goPost,
                atPost,
                isAlerted
            };
           
            public int changeTopState = -1;
            
            AddState(enum_states.setupPatrol, new NodeSetupPatrol());
            AddState(enum_states.goPost, new NodeGoPost());
            AddState(enum_states.atPost, new NodeAtPost());
            AddState(enum_states.isAlerted, new NodeIsAlerted());
        }

        public class NodeSetupPatrol : HMNode
        {
            public string no_imp = "PatrolSM.NodeSetupPatrol not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoPost : HMNode
        {
            public string no_imp = "PatrolSM.NodeGoPost not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtPost : HMNode
        {
            public string no_imp = "PatrolSM.NodeAtPost not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeIsAlerted : HMNode
        {
            public string no_imp = "PatrolSM.NodeIsAlerted not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class DepressedSM : OldManSM 
    {
        DepressedSM(enum these_states)
        {
            public enum enum_states = these_states;
            public enum depressedStates
            {
                startDepressed,
                findComfort,
                goComfort,
                whine
            };
           
            public int changeTopState = -1;
            
            AddState(enum_states.startDepressed, new NodeStartDepressed());
            AddState(enum_states.findComfort, new NodeFindComfort());
            AddState(enum_states.goComfort, new NodeGoComfort());
            AddState(enum_states.whine, new NodeWhine());
        }

        public class NodeStartDepressed : HMNode
        {
            public string no_imp = "DepressedSM.NodeStartDepressed not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeFindComfort : HMNode
        {
            public string no_imp = "DepressedSM.NodeFindComfort not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoComfort : HMNode
        {
            public string no_imp = "DepressedSM.NodeGoComfort not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeWhine : HMNode
        {
            public string no_imp = "DepressedSM.NodeWhine not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class RunInTerrorSM : OldManSM 
    {
        RunInTerrorSM(enum these_states)
        {
            public enum enum_states = these_states;
            public enum terrorStates
            {
                callForHelp,
                escapeHelp,
                escapeAny,
                panic
            };
           
            public int changeTopState = -1;
            
            AddState(enum_states.callForHelp, new NodeCallForHelp());
            AddState(enum_states.escapeHelp, new NodeEscapeHelp());
            AddState(enum_states.escapeAny, new NodeEscapeAny());
            AddState(enum_states.panic, new NodePanic());
        }

        public class NodeCallForHelp : HMNode
        {
            public string no_imp = "RunInTerrorSM.NodeCallForHelp not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeEscapeHelp : HMNode
        {
            public string no_imp = "RunInTerrorSM.NodeEscapeHelp not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeEscapeAny : HMNode
        {
            public string no_imp = "RunInTerrorSM.NodeEscapeAny not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodePanic : HMNode
        {
            public string no_imp = "RunInTerrorSM.NodePanic not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class HideSM : OldManSM 
    {
        HideSM(enum these_states)
        {
            public enum enum_states = these_states;
            public enum hideStates
            {
                findSafe,
                goSafe,
                atSafe
            };
           
            public int changeTopState = -1;
            
            AddState(enum_states.findSafe, new NodeFindSafe());
            AddState(enum_states.goSafe, new NodeGoSafe());
            AddState(enum_states.atSafe, new NodeAtSafe());
        }

        public class NodeFindSafe : HMNode
        {
            public string no_imp = "HideSM.NodeFindSafe not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoSafe : HMNode
        {
            public string no_imp = "HideSM.NodeGoSafe not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeAtSafe : HMNode
        {
            public string no_imp = "HideSM.NodeAtSafe not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }

    public class DefendHomeSM : OldManSM 
    {
        DefendHomeSM(enum these_states)
        {
            public enum enum_states = these_states;
            public enum defendHomeStates
            {
                isAlerted,
                findNeedsHelp,
                goNeedsHelp,
                findThreat,
                goThreat,
                caution
            };
           
            public int changeTopState = -1;
            
            AddState(enum_states.isAlerted, new NodeIsAlerted());
            AddState(enum_states.findNeedsHelp, new NodeFindNeedsHelp());
            AddState(enum_states.goNeedsHelp, new NodeGoNeedsHelp());
            AddState(enum_states.findThreat, new NodeFindThreat());
            AddState(enum_states.goThreat, new NodeGoThreat());
            AddState(enum_states.caution, new NodeCaution());
        }

        public class NodeIsAlerted : HMNode
        {
            public string no_imp = "DefendHomeSM.NodeIsAlerted not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeFindNeedsHelp : HMNode
        {
            public string no_imp = "DefendHomeSM.NodeFindNeedsHelp not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoNeedsHelp : HMNode
        {
            public string no_imp = "DefendHomeSM.NodeGoNeedsHelp not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeFindThreat : HMNode
        {
            public string no_imp = "DefendHomeSM.NodeFindThreat not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeGoThreat : HMNode
        {
            public string no_imp = "DefendHomeSM.NodeGoThreat not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
        public class NodeCaution : HMNode
        {
            public string no_imp = "DefendHomeSM.NodeCaution not implemented";

            public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp) }
            public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp) }
            public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp) }
        }
    }
}
