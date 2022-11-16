using BepInEx;
using Hirdmandr;
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
        
        public StateMachine parentSM;
        public HirdmandrAI hmAI;

        public StateMachine() 
        {
            parentSM = null;
        }
        public StateMachine(StateMachine psm) 
        {
            parentSM = psm;
        }

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
            // Jotunn.Logger.LogInfo("Evaluate() started");
            if (nextState != curState)  // State transition is queued
            {
                Jotunn.Logger.LogInfo("  Detected state transition from " + stateIndexes[curState] + " to " + stateIndexes[nextState]);

                if (states.TryGetValue(curState, out SMNode curNode) && states.TryGetValue(nextState, out SMNode nextNode))
                {
                    // Jotunn.Logger.LogInfo("    Calling current node ExitTo");
                    curNode.ExitTo(nextState);
                    // Jotunn.Logger.LogInfo("    Calling next node EnterFrom");
                    nextNode.EnterFrom(curState);

                    lastState = curState;
                    curState = nextState;
                } 
                else 
                {
                    Jotunn.Logger.LogError("Invalid State Transition requested: nextState = " + nextState);
                }

            }
            else  // Normal state execution: RunState
            {
                // Jotunn.Logger.LogInfo("  Running state with non-transition evaluation");
                if (states.TryGetValue(curState, out SMNode curNode))
                {
                    // Jotunn.Logger.LogInfo("    Calling curNode.RunState() on " + stateIndexes[curState]);
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

        public StateMachine parentSM;

        public HirdmandrAI hmAI;
        public HirdmandrNPC hmNPC;
        public MonsterAI hmMAI;
        public NPCPlayerClone hmHum;

        public SMNode(StateMachine psm)
        {
            parentSM = psm;
            hmAI = parentSM.hmAI;
            hmNPC = hmAI.m_hmnpc;
            hmMAI = hmAI.m_hmMonsterAI;
            hmHum = hmAI.m_hmHumanoid;
        }

        public virtual void EnterFrom(int aState)
        {
        }
        public virtual void ExitTo(int aState)
        {
        }
        public virtual void RunState()
        {
        }
    }
}
