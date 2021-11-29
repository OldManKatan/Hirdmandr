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
    public class SelfCareSM : StateMachine
    {
        public string changeTopState = "";

        public SelfCareSM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("findFood", new NodeFindFood(this));
            AddState("goFood", new NodeGoFood(this));
            AddState("atFood", new NodeAtFood(this));
        }

        public class NodeFindFood : SMNode
        {
            public NodeFindFood(SelfCareSM psm) : base(psm) { }

            public string no_imp = "selfCareSM.NodeFindFood not implemented";

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState() { }
        }
        public class NodeGoFood : SMNode
        {
            public NodeGoFood(SelfCareSM psm) : base(psm) { }

            public string no_imp = "selfCareSM.NodeGoFood not implemented";

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState() { }
        }
        public class NodeAtFood : SMNode
        {
            public NodeAtFood(SelfCareSM psm) : base(psm) { }

            public string no_imp = "selfCareSM.NodeAtFood not implemented";

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState() { }
        }
    }
}