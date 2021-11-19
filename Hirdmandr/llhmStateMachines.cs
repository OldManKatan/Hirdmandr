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

        public SocializeSM()
        {
            AddState("findMeetPoint", new NodeFindMeetPoint(this));
            AddState("goMeetPoint", new NodeGoMeetPoint(this));
            AddState("goIdlePoint", new NodeGoIdlePoint(this));
            AddState("atIdlePoint", new NodeAtIdlePoint(this));
            AddState("setupSocialize", new NodeSetupSocialize(this));
            AddState("startSocialize", new NodeStartSocialize(this));
        }

        public class NodeFindMeetPoint : SMNode
        {
            public SocializeSM parentSM;

            public NodeFindMeetPoint(SocializeSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "SocializeSM.NodeFindMeetPoint not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoMeetPoint : SMNode
        {
            public SocializeSM parentSM;

            public NodeGoMeetPoint(SocializeSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "SocializeSM.NodeGoMeetPoint not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoIdlePoint : SMNode
        {
            public SocializeSM parentSM;

            public NodeGoIdlePoint(SocializeSM psm)
            {
                parentSM = psm;
            }
            
            public string no_imp = "SocializeSM.NodeGoIdlePoint not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtIdlePoint : SMNode
        {
            public SocializeSM parentSM;

            public NodeAtIdlePoint(SocializeSM psm)
            {
                parentSM = psm;
            }
            
            public string no_imp = "SocializeSM.NodeAtIdlePoint not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeSetupSocialize : SMNode
        {
            public SocializeSM parentSM;

            public NodeSetupSocialize(SocializeSM psm)
            {
                parentSM = psm;
            }
            
            public string no_imp = "SocializeSM.NodeSetupSocialize not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeStartSocialize : SMNode
        {
            public SocializeSM parentSM;

            public NodeStartSocialize(SocializeSM psm)
            {
                parentSM = psm;
            }
            
            public string no_imp = "SocializeSM.NodeStartSocialize not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class WorkDaySM : StateMachine 
    {
        public string changeTopState = "";

        public WorkDaySM()
        {
            AddState("resetArtJob", new NodeResetArtJob(this));
            AddState("setupArtJob", new NodeSetupArtJob(this));
            AddState("goArtJob", new NodeGoArtJob(this));
            AddState("doJob", new NodeDoJob(this));
        }

        public class NodeResetArtJob : SMNode
        {
            public WorkDaySM parentSM;

            public NodeResetArtJob(WorkDaySM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "WorkDaySM.NodeResetArtJob not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeSetupArtJob : SMNode
        {
            public WorkDaySM parentSM;

            public NodeSetupArtJob(WorkDaySM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "WorkDaySM.NodeSetupArtJob not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoArtJob : SMNode
        {
            public WorkDaySM parentSM;

            public NodeGoArtJob(WorkDaySM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "WorkDaySM.NodeGoArtJob not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeDoJob : SMNode
        {
            public WorkDaySM parentSM;

            public NodeDoJob(WorkDaySM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "WorkDaySM.NodeDoJob not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class RestSM : StateMachine 
    {
        public string changeTopState = "";

        public RestSM()
        {
            AddState("findBed", new NodeFindBed(this));
            AddState("goBed", new NodeGoBed(this));
            AddState("atBed", new NodeAtBed(this));
        }

        public class NodeFindBed : SMNode
        {
            public RestSM parentSM;

            public NodeFindBed(RestSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "RestSM.NodeFindBed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoBed : SMNode
        {
            public RestSM parentSM;

            public NodeGoBed(RestSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "RestSM.NodeGoBed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtBed : SMNode
        {
            public RestSM parentSM;

            public NodeAtBed(RestSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "RestSM.NodeAtBed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class SelfCareSM : StateMachine 
    {
        public string changeTopState = "";

        public SelfCareSM()
        {
            AddState("findFood", new NodeFindFood(this));
            AddState("goFood", new NodeGoFood(this));
            AddState("atFood", new NodeAtFood(this));
        }

        public class NodeFindFood : SMNode
        {
            public SelfCareSM parentSM;

            public NodeFindFood(SelfCareSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "selfCareSM.NodeFindFood not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoFood : SMNode
        {
            public SelfCareSM parentSM;

            public NodeGoFood(SelfCareSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "selfCareSM.NodeGoFood not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtFood : SMNode
        {
            public SelfCareSM parentSM;

            public NodeAtFood(SelfCareSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "selfCareSM.NodeAtFood not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class PatrolSM : StateMachine 
    {
        public string changeTopState = "";

        public PatrolSM()
        {
            AddState("setupPatrol", new NodeSetupPatrol(this));
            AddState("goPost", new NodeGoPost(this));
            AddState("atPost", new NodeAtPost(this));
            AddState("isAlerted", new NodeIsAlerted(this));
        }

        public class NodeSetupPatrol : SMNode
        {
            public PatrolSM parentSM;

            public NodeSetupPatrol(PatrolSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "PatrolSM.NodeSetupPatrol not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoPost : SMNode
        {
            public PatrolSM parentSM;

            public NodeGoPost(PatrolSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "PatrolSM.NodeGoPost not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtPost : SMNode
        {
            public PatrolSM parentSM;

            public NodeAtPost(PatrolSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "PatrolSM.NodeAtPost not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeIsAlerted : SMNode
        {
            public PatrolSM parentSM;

            public NodeIsAlerted(PatrolSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "PatrolSM.NodeIsAlerted not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class DepressedSM : StateMachine 
    {
        public string changeTopState = "";

        public DepressedSM()
        {
            AddState("startDepressed", new NodeStartDepressed(this));
            AddState("findComfort", new NodeFindComfort(this));
            AddState("goComfort", new NodeGoComfort(this));
            AddState("whine", new NodeWhine(this));
        }

        public class NodeStartDepressed : SMNode
        {
            public DepressedSM parentSM;

            public NodeStartDepressed(DepressedSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DepressedSM.NodeStartDepressed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeFindComfort : SMNode
        {
            public DepressedSM parentSM;

            public NodeFindComfort(DepressedSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DepressedSM.NodeFindComfort not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoComfort : SMNode
        {
            public DepressedSM parentSM;

            public NodeGoComfort(DepressedSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DepressedSM.NodeGoComfort not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeWhine : SMNode
        {
            public DepressedSM parentSM;

            public NodeWhine(DepressedSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DepressedSM.NodeWhine not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class RunInTerrorSM : StateMachine 
    {
        public string changeTopState = "";

        public RunInTerrorSM()
        {
            AddState("callForHelp", new NodeCallForHelp(this));
            AddState("escapeHelp", new NodeEscapeHelp(this));
            AddState("escapeAny", new NodeEscapeAny(this));
            AddState("panic", new NodePanic(this));
        }

        public class NodeCallForHelp : SMNode
        {
            public RunInTerrorSM parentSM;

            public NodeCallForHelp(RunInTerrorSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "RunInTerrorSM.NodeCallForHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeEscapeHelp : SMNode
        {
            public RunInTerrorSM parentSM;

            public NodeEscapeHelp(RunInTerrorSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "RunInTerrorSM.NodeEscapeHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeEscapeAny : SMNode
        {
            public RunInTerrorSM parentSM;

            public NodeEscapeAny(RunInTerrorSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "RunInTerrorSM.NodeEscapeAny not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodePanic : SMNode
        {
            public RunInTerrorSM parentSM;

            public NodePanic(RunInTerrorSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "RunInTerrorSM.NodePanic not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class HideSM : StateMachine 
    {
        public string changeTopState = "";

        public HideSM()
        {
            AddState("findSafe", new NodeFindSafe(this));
            AddState("goSafe", new NodeGoSafe(this));
            AddState("atSafe", new NodeAtSafe(this));
        }

        public class NodeFindSafe : SMNode
        {
            public HideSM parentSM;

            public NodeFindSafe(HideSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "HideSM.NodeFindSafe not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoSafe : SMNode
        {
            public HideSM parentSM;

            public NodeGoSafe(HideSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "HideSM.NodeGoSafe not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtSafe : SMNode
        {
            public HideSM parentSM;

            public NodeAtSafe(HideSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "HideSM.NodeAtSafe not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }

    public class DefendHomeSM : StateMachine 
    {
        public string changeTopState = "";

        public DefendHomeSM()
        {
            AddState("isAlerted", new NodeIsAlerted(this));
            AddState("findNeedsHelp", new NodeFindNeedsHelp(this));
            AddState("goNeedsHelp", new NodeGoNeedsHelp(this));
            AddState("findThreat", new NodeFindThreat(this));
            AddState("goThreat", new NodeGoThreat(this));
            AddState("caution", new NodeCaution(this));
        }

        public class NodeIsAlerted : SMNode
        {
            public DefendHomeSM parentSM;

            public NodeIsAlerted(DefendHomeSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DefendHomeSM.NodeIsAlerted not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeFindNeedsHelp : SMNode
        {
            public DefendHomeSM parentSM;

            public NodeFindNeedsHelp(DefendHomeSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DefendHomeSM.NodeFindNeedsHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoNeedsHelp : SMNode
        {
            public DefendHomeSM parentSM;

            public NodeGoNeedsHelp(DefendHomeSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DefendHomeSM.NodeGoNeedsHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeFindThreat : SMNode
        {
            public DefendHomeSM parentSM;

            public NodeFindThreat(DefendHomeSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DefendHomeSM.NodeFindThreat not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoThreat : SMNode
        {
            public DefendHomeSM parentSM;

            public NodeGoThreat(DefendHomeSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DefendHomeSM.NodeGoThreat not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeCaution : SMNode
        {
            public DefendHomeSM parentSM;

            public NodeCaution(DefendHomeSM psm)
            {
                parentSM = psm;
            }

            public string no_imp = "DefendHomeSM.NodeCaution not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }
}
