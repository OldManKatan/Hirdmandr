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
        public bool wasOwner = false;
        
        public Dictionary<string, string[]> artisanJobPrefabs = new Dictionary<string, string[]>();
        public Dictionary<string, ZDO[]> artisanJobPieces = new Dictionary<string, ZDO[]>();
        public Dictionary<string, int> artisanJobOwner = new Dictionary<string, int>();

        protected virtual void Awake()
        {
            m_znetv = GetComponent<ZNetView>();
            
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
            
            artisanJobPieces.Add("woodburner", new ZDO[] { } );
            artisanJobPieces.Add("furnaceoperator", new ZDO[] { } );
            artisanJobPieces.Add("farmer", new ZDO[] { } );
            artisanJobPieces.Add("cook", new ZDO[] { } );
            artisanJobPieces.Add("baker", new ZDO[] { } );

            artisanJobOwner.Add("woodburner", 0 );
            artisanJobOwner.Add("furnaceoperator", 0 );
            artisanJobOwner.Add("farmer", 0 );
            artisanJobOwner.Add("cook", 0 );
            artisanJobOwner.Add("baker", 0 );
            
            m_znetv.GetZDO().Serialize("hmnpc_chestOwners", artisanJobOwner);
        }

        protected virtual void Update() { }
        
        protected virtual void FixedUpdate() { }

        public bool checkOwnership()
        {
            if (m_znetv.IsOwner())
            {
                if (!wasOwner)
                {
                    artisanJobOwner = m_znetv.GetZDO().Deserialize("hmnpc_chestOwners");
                    wasOwner = true;
                }
                return true;
            }
            if (wasOwner)
            {
                m_znetv.GetZDO().Serialize("hmnpc_chestOwners", artisanJobOwner);
                wasOwner = false;
            }
            return false;
        }
        
        public void GetValidWorksites()
        {
            artisanJobPieces["woodburner"] = new ZDO[] { };
            artisanJobPieces["furnaceoperator"] = new ZDO[] { };
            artisanJobPieces["farmer"] = new ZDO[] { };
            artisanJobPieces["cook"] = new ZDO[] { };
            artisanJobPieces["baker"] = new ZDO[] { };
            
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
                if (artisanJobPieces[entry.Key].Length)
                {
                    jobAvail = true;
                }
                m_znet.GetZDO().Set("hmnpc_isSite" + entry.Key, jobAvail);
            }
        }
        
        public bool IsValidWorksite(string artisanJob)
        {
            if (Game.time - lastZDOCheck > 5f)
            {
                GetValidWorksites();
            }
            if (artisanJobPieces[artisanJob].Length > 0)
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
                if (artisanJobOwner[artisanJob] == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        
        public bool Claim(string artisanJob, ZDOID owner_id)
        {
            if (checkOwnership())
            {
                if (artisanJobOwner[artisanJob] == 0)
                {
                    artisanJobOwner[artisanJob] = owner_id;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        
        public bool UnClaim(string artisanJob, ZDOID owner_id)
        {
            if (checkOwnership())
            {
                if (artisanJobOwner[artisanJob] == owner_id)
                {
                    artisanJobOwner[artisanJob] = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        
        public void RemoveOwner(string artisanJob)
        {
            if (checkOwnership())
            {
                artisanJobOwner[artisanJob] = 0;
            }
        }

        public void RemoveAllOwners()
        {
        if (checkOwnership())
            {
                foreach (KeyValuePair<string, int> entry in artisanJobOwner)
                {
                    artisanJobOwner[entry.Key] = 0;
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
