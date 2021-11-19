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
    public class StateMachine
    {
        public int lastState = 0;
        public int curState = 0;
        public int nextState = 0;

        public List<string> stateIndexes = new List<string>();
        public Dictionary<int, SMNode> states = new Dictionary<int, SMNode>();
        
        public StateMachine parentSM = null;

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

        public void ChangeState(string stateName)
        {
            var thisIndex = stateIndexes.IndexOf(stateName);
            if (thisIndex > -1)
            {
                nextState = thisIndex;
            }
            else
            {
                Jotunn.Logger.LogError("State Change requested to invalid state " + thisIndex + "! State remains at " + curState);
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
        
        public void AddState(string stateName, SMNode nodeObj)
        {
            if (stateIndexes.IndexOf(stateName) == -1)
            {
                stateIndexes.Add(stateName);
                states.Add(stateIndexes.IndexOf(stateName), nodeObj);
            }
            
        }

        public int StateInt(string stateName)
        {
            return stateIndexes.IndexOf(stateName);
        }

        public virtual void InitializeAtState(int StartState)
        {
            lastState = StartState;
            curState = StartState;
            nextState = StartState;
        }

        public virtual void InitializeAtState(string stateName)
        {
            var thisIndex = stateIndexes.IndexOf(stateName);
            if (thisIndex > -1)
            {
                lastState = thisIndex;
                curState = thisIndex;
                nextState = thisIndex;
            }
            else
            {
                Jotunn.Logger.LogError("Invalid State value requested during InitializeAtState: stateName = '" + stateName + "'");
            }
        }
    }

    public class SMNode
    {
        public string notImpPrefix = "Generic StateMachine : SMNode";
            
        public virtual void EnterFrom(int aState)
        {
            Jotunn.Logger.LogError(notImpPrefix + " EnterFrom is not implemented");
        }
        public virtual void ExitTo(int aState)
        {
            Jotunn.Logger.LogError(notImpPrefix + " ExitTo is not implemented");
        }
        public virtual void RunState()
        {
            Jotunn.Logger.LogError(notImpPrefix + " RunState is not implemented");
        }
    }
}
