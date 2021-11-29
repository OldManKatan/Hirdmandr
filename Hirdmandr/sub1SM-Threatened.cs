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
    public class ThreatenedSM : StateMachine
    {
        public string changeTopState = "";

        public ThreatenedSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("callForHelp", new NodeCallForHelp(this));
            AddState("escapeHelp", new NodeEscapeHelp(this));
            AddState("escapeAny", new NodeEscapeAny(this));
            AddState("panic", new NodePanic(this));
        }

        public class NodeCallForHelp : SMNode
        {
            public NodeCallForHelp(ThreatenedSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodeCallForHelp not implemented";

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeEscapeHelp : SMNode
        {
            public NodeEscapeHelp(ThreatenedSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodeEscapeHelp not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodeEscapeAny : SMNode
        {
            public NodeEscapeAny(ThreatenedSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodeEscapeAny not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
        public class NodePanic : SMNode
        {
            public NodePanic(ThreatenedSM psm) : base(psm) { }

            public string no_imp = "RunInTerrorSM.NodePanic not implemented";

            override public void EnterFrom(int aState) { Jotunn.Logger.LogInfo("EnterFrom in " + no_imp); }
            override public void ExitTo(int aState) { Jotunn.Logger.LogInfo("ExitTo in " + no_imp); }
            override public void RunState() { Jotunn.Logger.LogInfo("RunState in " + no_imp); }
        }
    }
}
