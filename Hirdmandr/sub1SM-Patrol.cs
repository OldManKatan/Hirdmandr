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
    public class PatrolSM : StateMachine
    {
        public string changeTopState = "";

        public PatrolSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("setupPatrol", new NodeSetupPatrol(this));
            AddState("goPost", new NodeGoPost(this));
            AddState("atPost", new NodeAtPost(this));
            AddState("isAlerted", new NodeIsAlerted(this));
        }

        public class NodeSetupPatrol : SMNode
        {
            public NodeSetupPatrol(PatrolSM psm) : base(psm) { }

            public string no_imp = "PatrolSM.NodeSetupPatrol not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoPost : SMNode
        {
            public NodeGoPost(PatrolSM psm) : base(psm) { }

            public string no_imp = "PatrolSM.NodeGoPost not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeAtPost : SMNode
        {
            public NodeAtPost(PatrolSM psm) : base(psm) { }

            public string no_imp = "PatrolSM.NodeAtPost not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeIsAlerted : SMNode
        {
            public NodeIsAlerted(PatrolSM psm) : base(psm) { }

            public string no_imp = "PatrolSM.NodeIsAlerted not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }
}