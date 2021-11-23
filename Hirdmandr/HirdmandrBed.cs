using BepInEx;
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
    [Serializable]
    public class HirdmandrBed : MonoBehaviour
    {
        public ZNetView m_znetv;
        
        public int comfortAtBed;
        public int ownerZDOID = 0;
        
        protected virtual void Awake()
        {
            m_znetv = GetComponent<ZNetView>();
        }

        protected virtual void Update() { }

        protected virtual void FixedUpdate() { }

        public int GetComfort()
        {
            comfortAtBed = m_znetv.GetComfort(transform.position);
            return comfortAtBed;
        }

        public bool hasOwner()
        {
            if (ownerZDOID == 0)
            {
                return false;
            }
            else
            {
                return true
            }
        }
        
        public bool Claim(ZDOID owner_id)
        {
            if (ownerZDOID == 0)
            {
                ownerZDOID = owner_id;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public bool UnClaim(ZDOID owner_id)
        {
            if (ownerZDOID == owner_id)
            {
                ownerZDOID = 0;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public void RemoveOwner(string artisanJob)
        {
            ownerZDOID = 0;
        }
    }
}
