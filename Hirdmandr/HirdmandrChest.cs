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
    public class HirdmandrChest : MonoBehaviour
    {
        public ZNetView m_znetv;
        
        public List<ZDO> m_ZDOsInRange;
        public float lastZDOCheck;
        public bool wasOwner;
        
        public Dictionary<string, string[]> artisanJobPrefabs;
        public Dictionary<string, List<ZDO>> artisanJobPieces;
        public Dictionary<string, ZDOID> artisanJobOwner;

        protected virtual void Awake()
        {
            wasOwner = false;
                
            m_znetv = GetComponent<ZNetView>();

            artisanJobPrefabs = new Dictionary<string, string[]>();
            artisanJobPieces = new Dictionary<string, List<ZDO>>();
            artisanJobOwner = new Dictionary<string, ZDOID>();

            artisanJobPrefabs.Add("woodburner", new string[] { "charcoal_kiln" });
            artisanJobPrefabs.Add("furnaceoperator", new string[] { "smelter", "blastfurnace" });
            artisanJobPrefabs.Add("farmer", new string[] 
                { 
                    "sapling_carrot", 
                    "sapling_seedcarrot", 
                    "sapling_turnip", 
                    "sapling_seedturnip", 
                    "sapling_onion", 
                    "sapling_seedonion", 
                    "sapling_barley", 
                    "sapling_flax" 
                });
            artisanJobPrefabs.Add("cook", new string[] { "piece_cauldron" });
            artisanJobPrefabs.Add("baker", new string[] { "piece_oven" });
            
            artisanJobPieces.Add("woodburner", new List<ZDO>() );
            artisanJobPieces.Add("furnaceoperator", new List<ZDO>() );
            artisanJobPieces.Add("farmer", new List<ZDO>() );
            artisanJobPieces.Add("cook", new List<ZDO>() );
            artisanJobPieces.Add("baker", new List<ZDO>() );

            artisanJobOwner.Add("woodburner", ZDOID.None );
            artisanJobOwner.Add("furnaceoperator", ZDOID.None);
            artisanJobOwner.Add("farmer", ZDOID.None);
            artisanJobOwner.Add("cook", ZDOID.None);
            artisanJobOwner.Add("baker", ZDOID.None);
            
            foreach (KeyValuePair<string, ZDOID> entry in artisanJobOwner)
            {
                m_znetv.GetZDO().Set("hmnpc_owner" + entry.Key, entry.Value);
            }
        }

        protected virtual void Update() { }
        
        protected virtual void FixedUpdate() { }

        public bool checkOwnership()
        {
            if (m_znetv.IsOwner())
            {
                if (!wasOwner)
                {
                    foreach (KeyValuePair<string, ZDOID> entry in artisanJobOwner)
                    {
                        artisanJobOwner[entry.Key] = m_znetv.GetZDO().GetZDOID("hmnpc_owner" + entry.Key);
                    }
                    wasOwner = true;
                }
                return true;
            }
            if (wasOwner)
            {
                foreach (KeyValuePair<string, ZDOID> entry in artisanJobOwner)
                {
                    m_znetv.GetZDO().Set("hmnpc_owner" + entry.Key, entry.Value);
                }
                wasOwner = false;
            }
            return false;
        }
        
        public void GetValidWorksites()
        {
            foreach (KeyValuePair<string, ZDOID> entry in artisanJobOwner)
            {
                artisanJobPieces[entry.Key] = new List<ZDO>();
            }
            
            foreach (KeyValuePair<string, string[]> entry in artisanJobPrefabs)
            {
                foreach (string thisPrefab in entry.Value)
                {
                    List<ZDO> nearZDOs = GetPrefabZDOsInRange(thisPrefab, 15f);
                    if (nearZDOs.Count > 0)
                    {
                        foreach (ZDO thisZDO in nearZDOs)
                        {
                            artisanJobPieces[entry.Key].Add(thisZDO);
                        }
                    }
                }
                
                bool jobAvail = false;
                if (artisanJobPieces[entry.Key].Count > 0)
                {
                    jobAvail = true;
                }
                m_znetv.GetZDO().Set("hmnpc_isSite" + entry.Key, jobAvail);
            }
        }
        
        public bool IsValidWorksite(string artisanJob)
        {
            if (Time.time - lastZDOCheck > 5f)
            {
                GetValidWorksites();
                lastZDOCheck = Time.time;
            }
            if (artisanJobPieces[artisanJob].Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public bool IsInUse(string artisanJob)
        {
            if (checkOwnership())
            {
                if (artisanJobOwner[artisanJob] == ZDOID.None)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return true;
        }
        
        public bool Claim(string artisanJob, ZDOID owner_id)
        {
            if (checkOwnership())
            {
                if (artisanJobOwner[artisanJob] == ZDOID.None)
                {
                    artisanJobOwner[artisanJob] = owner_id;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public bool UnClaim(string artisanJob, ZDOID owner_id)
        {
            if (checkOwnership())
            {
                if (artisanJobOwner[artisanJob] == owner_id)
                {
                    artisanJobOwner[artisanJob] = ZDOID.None;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public void RemoveOwner(string artisanJob)
        {
            if (checkOwnership())
            {
                artisanJobOwner[artisanJob] = ZDOID.None;
            }
        }

        public void RemoveAllOwners()
        {
        if (checkOwnership())
            {
                foreach (KeyValuePair<string, ZDOID> entry in artisanJobOwner)
                {
                    artisanJobOwner[entry.Key] = ZDOID.None;
                }
            }
        }

        public List<ZDO> GetAllZDOsInRange(float range)
        {
            Vector3 center = transform.position;
            int MinX = (int)(center.x - range);
            int MinZ = (int)(center.z - range);
            int MaxX = (int)(center.x + range);
            int MaxZ = (int)(center.z + range);

            // Get sectors to check
            List<Vector2i> sectors;
            sectors = GetSectors(MinX, MinZ, MaxX, MaxZ);

            // Get zdos
            var foundZDOs = new List<ZDO>();

            foreach (var sector in sectors)
            {
                ZDOMan.instance.FindObjects(sector, foundZDOs);
            }

            return foundZDOs;
        }

        public List<ZDO> GetPrefabZDOsInRange(string prefabName, float range)
        {
            int prefabStableHashCode = prefabName.GetStableHashCode();

            var foundZDOs = GetAllZDOsInRange(range);

            var matchedZDOs = new List<ZDO>();

            foreach (ZDO aZDO in foundZDOs)
            {
                if (aZDO.GetPrefab() == prefabStableHashCode)
                {
                    matchedZDOs.Add(aZDO);
                }
            }

            return matchedZDOs;
        }

        private static List<Vector2i> GetSectors(int minX, int minZ, int maxX, int maxZ)
        {
            List<Vector2i> sectors = new List<Vector2i>();

            int stepMinX = Zonify(minX);
            int stepMaxX = Zonify(maxX);

            int stepMinZ = Zonify(minZ);
            int stepMaxZ = Zonify(maxZ);

            for (int x = stepMinX; x <= stepMaxX; ++x)
            {
                for (int z = stepMinZ; z <= stepMaxZ; ++z)
                {
                    sectors.Add(new Vector2i(x, z));
                }
            }

            return sectors;

            int Zonify(int coordinate)
            {
                return Mathf.FloorToInt((coordinate + 32) / 64f);
            }
        }
    }
}
