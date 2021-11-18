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
        
        public Dictionary<sts, SMNode> states = new Dictionary<sts, SMNode>();

        public void ChangeState(sts stateEnum)
        {
            if (states.ContainsKey(stateEnum))
            {
                nextState = stateEnum;
            }
            else
            {
                Jotunn.Logger.LogError("State Change requested to invalid state " + (int)stateEnum + "! State remains at " + (int)curState);
            }
        }
        
        public void Evaluate()
        {
            if (nextState != curState)  // State transition is queued
            {
                if (states.TryGetValue(curState, out SMNode curNode) && sts.TryGetValue(nextState, out SMNode nextNode))
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
                    Jotunn.Logger.LogError("This should never happen! Invalid current state?! curState = " + (int)curState);
                }
            }
        }
        
        public void AddState(sts stateEnum, SMNode nodeObj)
        {
            states.Add(stateEnum, nodeObj);
        }

    }
    
    public class SMNode
    {
        public string notImpPrefix = "Generic StateMachine : SMNode";
            
        public void EnterFrom(int aState)
        {
            Jotunn.Logger.LogError(notImpPrefix + " EnterFrom is not implemented");
        }
        public void ExitTo(int aState)
        {
            Jotunn.Logger.LogError(notImpPrefix + " ExitTo is not implemented");
        }
        public void RunState()
        {
            Jotunn.Logger.LogError(notImpPrefix + " RunState is not implemented");
        }
    }
}
