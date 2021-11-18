using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OldManSM 
{
    public interface StateMachine
    {
        public enum sts {};
        
        public sts lastState = 0;
        public sts curState = 0;
        public sts nextState = 0;
        
        public Dictionary<int, SMNode> states = new Dictionary<int, SMNode>();

        public void ChangeState(int stateInt)
        {
            if (states.ContainsKey(stateInt))
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
                if (states.TryGetValue(curState, out SMNode curNode) && states.TryGetValue(nextState, out SMNode nextNode))
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
                if (states.TryGetValue(curState, out SMNode curNode))
                {
                    curNode.RunState();
                }
                else 
                {
                    Jotunn.Logger.LogError("This should never happen! Invalid current state?! curState = " + curState);
                }
            }
        }
        
        public void AddState(int stateInt, SMNode nodeObj)
        {
            states.Add(stateInt, nodeObj);
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
