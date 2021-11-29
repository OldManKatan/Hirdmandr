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
        public Container m_container;
        public ZNetView m_znetv;

        public float lastZDOCheck;
        public bool wasOwner;

        public List<ZDO> m_ZDOsInRange = new List<ZDO>();
        public List<Inventory> linkedInventories = new List<Inventory>();

        public Dictionary<string, string[]> artisanJobPrefabs = new Dictionary<string, string[]>();
        public Dictionary<string, List<ZDO>> artisanJobPieces = new Dictionary<string, List<ZDO>>();
        public Dictionary<string, ZDOID> artisanJobOwner = new Dictionary<string, ZDOID>();

        public string[] allKeys = new string[]
            {
                "woodburner",
                "furnaceoperator",
                "farmer",
                "cook",
                "baker"
            };

        protected virtual void Awake()
        {
            wasOwner = false;

            m_container = GetComponent<Container>();
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
        }

        protected virtual void Update() { }
        
        protected virtual void FixedUpdate() { }

        public bool checkOwnership()
        {
            if (m_znetv.IsOwner())
            {
                if (!wasOwner)
                {
                    foreach (string aKey in allKeys)
                    {
                        artisanJobOwner[aKey] = m_znetv.GetZDO().GetZDOID("hmnpc_owner" + aKey);
                    }
                    wasOwner = true;
                }
                return true;
            }
            if (wasOwner)
            {
                foreach (string aKey in allKeys)
                {
                    m_znetv.GetZDO().Set("hmnpc_owner" + aKey, artisanJobOwner[aKey]);
                }
                wasOwner = false;
            }
            return false;
        }
        
        public void GetValidWorksites()
        {
            foreach (string aKey in allKeys)
            {
                artisanJobPieces[aKey] = new List<ZDO>();
            }
            
            foreach (string aKey in allKeys)
            {
                foreach (string thisPrefab in artisanJobPrefabs[aKey])
                {
                    List<ZDO> nearZDOs = Utils.GetPrefabZDOsInRange(transform.position, 15f, thisPrefab);
                    if (nearZDOs.Count > 0)
                    {
                        foreach (ZDO thisZDO in nearZDOs)
                        {
                            if (aKey == "woodburner")
                            {
                                if (ValidWoodburnerInventory()) { artisanJobPieces[aKey].Add(thisZDO); }
                            }
                            else if (aKey == "furnaceoperator")
                            {
                                if (ValidFurnaceoperatorInventory()) { artisanJobPieces[aKey].Add(thisZDO); }
                            }
                            else
                            {
                                artisanJobPieces[aKey].Add(thisZDO);
                            }
                        }
                    }
                }
                
                bool jobAvail = false;
                if (artisanJobPieces[aKey].Count > 0)
                {
                    jobAvail = true;
                }
                m_znetv.GetZDO().Set("hmnpc_isSite" + aKey, jobAvail);
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
                    Jotunn.Logger.LogError("Worksite NPC Chest has been CLAIMED");
                    return true;
                }
                else
                {
                    Jotunn.Logger.LogError("Worksite NPC Chest COULD NOT be CLAIMED");
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
                    Jotunn.Logger.LogError("Worksite NPC Chest has been UNCLAIMED");
                    artisanJobOwner[artisanJob] = ZDOID.None;
                    return true;
                }
                else
                {
                    Jotunn.Logger.LogError("Worksite NPC Chest COULD NOT be UNCLAIMED");
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
                foreach (string aKey in allKeys)
                {
                    artisanJobOwner[aKey] = ZDOID.None;
                }
            }
        }

        public void LinkInventories()
        {
            linkedInventories = new List<Inventory>();
            linkedInventories.Add(m_container.m_inventory);

            foreach (string chestType in new string[3] { "piece_npc_chest", "piece_npc_chest_reinforced", "piece_npc_chest_blackmetal" })
            {
                foreach (ZDO chestZDO in Utils.GetPrefabZDOsInRange(transform.position, 10f, "piece_npc_chest"))
                {
                    linkedInventories.Add(ZNetScene.instance.FindInstance(chestZDO).GetComponent<Container>().m_inventory);
                }
            }
        }

        public bool LinkedHaveItem(string item)
        {
            foreach (Inventory linkedinv in linkedInventories)
            {
                if (linkedinv.HaveItem(item))
                {
                    return true;
                }
            }
            return false;
        }

        public bool LinkedRemoveOneItem(string item)
        {
            foreach (Inventory linkedinv in linkedInventories)
            {
                if (linkedinv.HaveItem(item))
                {
                    linkedinv.RemoveOneItem(linkedinv.GetItem(item));
                    return true;
                }
            }
            return false;
        }

        public bool LinkedPutItem(ItemDrop.ItemData item)
        {
            foreach (Inventory linkedinv in linkedInventories)
            {
                if (linkedinv.HaveItem(item.m_shared.m_name))
                {
                    if (linkedinv.AddItem(item))
                    {
                        return true;
                    }
                }
            }
            if (m_container.m_inventory.AddItem(item))
            {
                return true;
            }
            return false;
        }

        public bool ValidWoodburnerInventory()
        {
            LinkInventories();

            if (LinkedHaveItem("$item_wood"))
            {
                return true;
            }
            return false;
        }

        public bool ValidFurnaceoperatorInventory()
        {
            bool hasCoal = false;
            bool hasOre = false;

            if (LinkedHaveItem("$item_coal"))
            {
                hasCoal = true;
            }

            if (LinkedHaveItem("$item_copperore") ||
                LinkedHaveItem("$item_tinore") ||
                LinkedHaveItem("$item_ironscrap") ||
                LinkedHaveItem("$item_silverore"))
            {
                hasOre = true;
            }

            if (hasCoal && hasOre)
            {
                return true;
            }
            return false;
        }
    }
}
