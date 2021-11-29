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
    public class DepressedSM : StateMachine
    {
        public string changeTopState = "";

        public DepressedSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("startDepressed", new NodeStartDepressed(this));
            AddState("findComfort", new NodeFindComfort(this));
            AddState("goComfort", new NodeGoComfort(this));
            AddState("whine", new NodeWhine(this));
        }

        public class NodeStartDepressed : SMNode
        {
            public NodeStartDepressed(DepressedSM psm) : base(psm) { }

            public string no_imp = "DepressedSM.NodeStartDepressed not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeFindComfort : SMNode
        {
            public NodeFindComfort(DepressedSM psm) : base(psm) { }

            public string no_imp = "DepressedSM.NodeFindComfort not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeGoComfort : SMNode
        {
            public NodeGoComfort(DepressedSM psm) : base(psm) { }

            public string no_imp = "DepressedSM.NodeGoComfort not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeWhine : SMNode
        {
            public NodeWhine(DepressedSM psm) : base(psm) { }

            public string no_imp = "DepressedSM.NodeWhine not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }
}