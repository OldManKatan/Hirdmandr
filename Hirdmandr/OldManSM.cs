using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OldManSM 
{
    public class StateMachine
    {
        public int lastState = 0;
        public int curState = 0;
        public int nextState = 0;
        
        public Dictionary<int, SMNode> states = new Dictionary<int, SMNode>();
        
        public State AddState(int stateInt, SMNode nodeObj)
        {
            if (stateInt && nodeObj)
            {
                states.Add(new KeyValuePair<int, SMNode>(stateInt, nodeObj));
            }
            else
            {
                Jotunn.Logger.LogError("AddState failed because either stateInt or nodeObj was null/invalid");
            }
        }
        
        public void ChangeState(int stateInt)
        {
            if States.ContainsKey(stateInt)
            {
                nextState = stateInt;
            }
            else
            {
                Jotunn.Logger.LogError("State Change requested to invalid state " + stateInt + "! State remains at " + curState);
            }
        }
        
        public void Evaluate()
        {
            if (nextState != curState)  // State transition is queued
            {
                if (states.TryGetValue(curState, out curNode) && states.TryGetValue(nextState, out nextNode))
                {
                    curNode.ExitTo(nextState);
                    nextNode.EnterFrom(curState);
                } 
                else 
                {
                    Jotunn.Logger.LogError("Invalid State Transition requested: nextState = " + nextState);
                }

            }
            else  // Normal state execution: RunState
            {
                if (states.TryGetValue(curState, out curNode))
                {
                    curNode.RunState();
                }
                else 
                {
                    Jotunn.Logger.LogError("This should never happen! Invalid current state?! curState = " + curState);
                }
            }
        }
    }
    
    public class SMNode
    {
        public void EnterFrom(int aState)
        {
        }
        public void ExitTo(int aState)
        {
        }
        public void RunState()
        {
        }
    }
}
