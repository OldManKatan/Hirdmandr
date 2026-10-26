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

        public Dictionary<string, string[]> artisanJobPrefabs = new Dictionary<string, string[]>();
        public Dictionary<string, List<ZDO>> artisanJobPieces = new Dictionary<string, List<ZDO>>();
        public Dictionary<string, ZDOID> artisanJobOwner = new Dictionary<string, ZDOID>();

        public string[] allKeys = new string[]
            {
                "woodburner",
                "furnaceoperator",
                "farmer",
                "lamplighter"
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
            // The Lamplighter refuels light sources / fires, i.e. any piece carrying a Fireplace
            // component (torch, iron torch blue/green, iron brazier, hanging brazier, sconce,
            // iron fire pit, campfire, bonfire, hearth). Discovery is done by Fireplace
            // component, not by these prefab names, so the list stays empty here; see
            // GetValidWorksites and WorkDaySM.NodeDoJob.
            artisanJobPrefabs.Add("lamplighter", new string[0]);

            artisanJobPieces.Add("woodburner", new List<ZDO>() );
            artisanJobPieces.Add("furnaceoperator", new List<ZDO>() );
            artisanJobPieces.Add("farmer", new List<ZDO>() );
            artisanJobPieces.Add("lamplighter", new List<ZDO>() );

            artisanJobOwner.Add("woodburner", ZDOID.None );
            artisanJobOwner.Add("furnaceoperator", ZDOID.None);
            artisanJobOwner.Add("farmer", ZDOID.None);
            artisanJobOwner.Add("lamplighter", ZDOID.None);
        }

        private float m_lastOwnerCheck = -60f;

        // Cached "mature crops in range" result, refreshed by GetValidWorksites' farmer
        // branch (same 5s cadence as lastZDOCheck). Read by HasMatureCropsInRange() for
        // the farmer-with-its-own-tool worksite path in NodeSetupArtJob (which rechecks
        // every frame on the stall path, so it must never run the global scan hot).
        private bool m_farmerCropsInRange = false;

        protected virtual void Update()
        {
            // Recurring stale-claim sweep: a deleted/dead owner leaves job slots claimed forever
            // unless we release them here (the NPC side has no reliable death/delete hook).
            if (Time.time - m_lastOwnerCheck >= 30f)
            {
                m_lastOwnerCheck = Time.time;
                CleanupDeadOwners();
            }
        }

        private void CleanupDeadOwners()
        {
            if (!checkOwnership())
            {
                return;
            }
            foreach (string aKey in allKeys)
            {
                if (artisanJobOwner[aKey] == ZDOID.None)
                {
                    continue;
                }
                Character ownerChar = null;
                var ownerZDO = ZDOMan.instance.GetZDO(artisanJobOwner[aKey]);
                if (ownerZDO != null)
                {
                    var ownerGO = ZNetScene.instance.FindInstance(ownerZDO);
                    ownerChar = (ownerGO != null) ? ownerGO.GetComponent<Character>() : null;
                }
                if (ownerChar == null || ownerChar.GetHealth() <= 0f || ownerChar.IsDead())
                {
                    Jotunn.Logger.LogWarning("HirdmandrChest: owner " + artisanJobOwner[aKey] + " of job '" + aKey + "' is gone, releasing claim");
                    artisanJobOwner[aKey] = ZDOID.None;
                    m_znetv.GetZDO().Set("hmnpc_owner" + aKey, ZDOID.None);
                }
            }
        }
        
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
                // The Lamplighter is special: its "work sites" are any fuel fixtures (pieces with a
                // Fireplace component) within a wide radius, and the chest must hold fuel.
                if (aKey == "lamplighter")
                {
                    foreach (ZDO thisZDO in Utils.GetAllZDOsInRange(transform.position, Utils.LamplighterRange))
                    {
                        ZNetView thisView = ZNetScene.instance.FindInstance(thisZDO);
                        if (thisView != null && thisView.GetComponent<Fireplace>() != null)
                        {
                            artisanJobPieces[aKey].Add(thisZDO);
                        }
                    }
                    bool lamplighterAvail = (artisanJobPieces[aKey].Count > 0) && ValidLamplighterInventory();
                    // ZDO Set is owner-only; a non-owner's write is a no-op, so only bother on the owner.
                    if (m_znetv.IsOwner())
                    {
                        m_znetv.GetZDO().Set("hmnpc_isSite" + aKey, lamplighterAvail);
                    }
                    continue;
                }

                if (aKey == "farmer")
                {
                    // The Farmer works on MATURE crops (Pickables), not growing saplings: its do-loop
                    // harvests a mature crop and replants a sapling in place. So a valid Farmer worksite
                    // is a chest that holds a cultivator (its work tool) AND has at least one mature crop
                    // within FarmerRange. A field of only growing saplings has nothing to harvest yet.
                    // (Mature-only check per Thomas: cheaper + correct than the sapling-only check this
                    // replaces; a growing field becomes valid once its first crop is mature.)
                    // Cache the crop result for HasMatureCropsInRange() (the farmer-with-its-own-tool
                    // worksite path) - the scan is already happening here on the 5s cadence, so it's free.
                    m_farmerCropsInRange = (this.GetMatureCropsInRange().Count > 0);
                    bool farmerAvail = this.ValidFarmerInventory() && m_farmerCropsInRange;
                    if (farmerAvail)
                    {
                        // The chest is the farmer's worksite object (the NPC walks to it, then to crops).
                        artisanJobPieces[aKey].Add(this.m_znetv.GetZDO());
                    }
                    // ZDO Set is owner-only; a non-owner's write is a no-op, so only bother on the owner.
                    if (m_znetv.IsOwner())
                    {
                        m_znetv.GetZDO().Set("hmnpc_isSite" + aKey, farmerAvail);
                    }
                    continue;
                }

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
                // ZDO Set is owner-only; a non-owner's write is a no-op, so only bother on the owner.
                if (m_znetv.IsOwner())
                {
                    m_znetv.GetZDO().Set("hmnpc_isSite" + aKey, jobAvail);
                }
            }
        }
        
        public bool IsValidWorksite(string artisanJob)
        {
            if (Time.time - lastZDOCheck > 5f)
            {
                GetValidWorksites();
                lastZDOCheck = Time.time;
            }
            // ContainsKey guard: a job with no worksite dictionary entry is simply "not a valid
            // site", never a KeyNotFoundException (keeps a stray non-worksite key from crashing
            // the setup walk).
            List<ZDO> sitePieces;
            if (artisanJobPieces.TryGetValue(artisanJob, out sitePieces) && sitePieces.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Cached "is there a mature crop in this chest's field" at the same 5s cadence as
        // IsValidWorksite (shares its lastZDOCheck gate; the actual FindObjectsOfType<Pickable>
        // scan runs inside GetValidWorksites' farmer branch). Exposed for WorkDay setup: a
        // farmer that already holds its own cultivator can use this chest as a worksite if (and
        // only if) there is crop work to do. Safe to call every frame (the setup stall path
        // does) - it never runs the global scan outside the 5s gate.
        public bool HasMatureCropsInRange()
        {
            if (Time.time - lastZDOCheck > 5f)
            {
                GetValidWorksites();
                lastZDOCheck = Time.time;
            }
            return m_farmerCropsInRange;
        }

        public bool IsInUse(string artisanJob)
        {
            // Worksites only exist for the allKeys jobs; a foreign job key (e.g. a warrior
            // combat skill) is "not in use" here, never a KeyNotFoundException.
            if (Array.IndexOf(allKeys, artisanJob) < 0)
            {
                return false;
            }
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
            // Non-owner: claim data (artisanJobOwner) is owner-local and not synced, so a
            // non-owner has no claim and conservatively reports "not in use" (the old
            // unconditional `return true` marked every job as in use).
            return false;
        }
        
        public bool Claim(string artisanJob, ZDOID owner_id)
        {
            if (Array.IndexOf(allKeys, artisanJob) < 0)
            {
                return false;
            }
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
            if (Array.IndexOf(allKeys, artisanJob) < 0)
            {
                return false;
            }
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
            if (Array.IndexOf(allKeys, artisanJob) < 0)
            {
                return;
            }
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

        public bool ValidWoodburnerInventory()
        {
            return m_container.m_inventory.HaveItem("$item_wood");
        }

        public bool ValidFurnaceoperatorInventory()
        {
            bool hasCoal = m_container.m_inventory.HaveItem("$item_coal");

            bool hasOre = m_container.m_inventory.HaveItem("$item_copperore") ||
                m_container.m_inventory.HaveItem("$item_tinore") ||
                m_container.m_inventory.HaveItem("$item_ironscrap") ||
                m_container.m_inventory.HaveItem("$item_silverore");

            if (hasCoal && hasOre)
            {
                return true;
            }
            return false;
        }

        public bool ValidLamplighterInventory()
        {
            // Fuel fixtures take wood (wood torch, campfire) or coal (iron torches, braziers,
            // sconce, iron fire pit, hearth, bonfire). The chest is a valid Lamplighter worksite if
            // it holds either fuel type.
            if (m_container.m_inventory.HaveItem("$item_wood") || m_container.m_inventory.HaveItem("$item_coal"))
            {
                return true;
            }
            return false;
        }

        public bool ValidFarmerInventory()
        {
            // The Farmer's work tool is a player-provided cultivator (non-spawnable). The chest is a
            // valid Farmer worksite if the cultivator is in the chest OR in the assigned NPC's own
            // inventory (m_container): the farmer takes it out on entry (setup) and returns it on
            // exit, so while the NPC is mid-job the tool lives in m_container, not the chest.
            if (m_container.m_inventory.HaveItem(FarmerCrops.CultivatorItem))
            {
                return true;
            }
            // Mid-job case: the assigned farmer is holding it. artisanJobOwner is owner-local
            // (populated by checkOwnership), so on a non-owner client the owner is None and this
            // no-ops (the chest check above still applies).
            checkOwnership();
            ZDOID ownerId = artisanJobOwner["farmer"];
            if (ownerId != ZDOID.None)
            {
                var ownerZDO = ZDOMan.instance.GetZDO(ownerId);
                if (ownerZDO != null)
                {
                    var ownerGO = ZNetScene.instance.FindInstance(ownerZDO);
                    if (ownerGO != null)
                    {
                        var npc = ownerGO.GetComponent<HirdmandrNPC>();
                        if (npc != null && npc.m_container != null && npc.m_container.m_inventory != null)
                        {
                            if (npc.m_container.m_inventory.GetItem(FarmerCrops.CultivatorItem) != null)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        // Mature crops in this chest's field: the Pickables within FarmerRange of the chest whose
        // yield item is a crop-table yield. This is the farmer's work list (harvest these, Steps 6/7).
        //   - Wild pickables (cloudberry, stone, ...) are excluded: their yield item isn't in the
        //     crop table.
        //   - Growing saplings are excluded for free: a sapling is a Plant, not a Pickable, so
        //     FindObjectsOfType<Pickable> never returns one.
        // Pickable is a MonoBehaviour (NOT a ZDO -- confirmed via Cecil), so the ZDO sector scan
        // (Utils.GetAllZDOsInRange) wouldn't directly surface a crop; FindObjectsOfType<Pickable>
        // is the same mechanism Step 1's farmscan validated. Called at job/task cadence (not
        // per-frame), so the global find is acceptable; the distance filter keeps it local.
        public List<Pickable> GetMatureCropsInRange()
        {
            List<Pickable> crops = new List<Pickable>();
            Vector3 pos = this.transform.position;
            float range = Utils.FarmerRange;
            foreach (Pickable pk in UnityEngine.Object.FindObjectsOfType<Pickable>())
            {
                if (pk == null || pk.transform == null)
                {
                    continue;
                }
                Vector3 ppos = pk.transform.position;
                float dx = ppos.x - pos.x;
                float dz = ppos.z - pos.z;
                if (Mathf.Sqrt(dx * dx + dz * dz) > range)
                {
                    continue;
                }
                if (!FarmerCrops.IsKnownYield(YieldsOf(pk)))
                {
                    continue;
                }
                crops.Add(pk);
            }
            return crops;
        }

        // Target selection for the farmer do-loop (PoR §3): the first mature crop in the field whose
        // replant item the chest can supply. The core invariant "cannot harvest without being able to
        // replace" is enforced here: we only return a crop whose consumeItem (the replant item) is
        // present, so a crop the player hasn't stocked is left in place (mature, unharvested). Step 8
        // re-checks the consumeItem at harvest time too, since chest contents change between pick and
        // harvest.
        // NOTE: the cultivator is NOT checked here. The farmer holds it for the whole job (acquired
        // out of the chest on doJob entry, Step 8; returned on exit), so by the time this runs the
        // chest no longer has one. Its availability is checked at acquire time (Step 9 setup).
        public bool SelectFarmerTarget(out Pickable targetCrop, out FarmerCrop targetEntry)
        {
            targetCrop = null;
            targetEntry = default(FarmerCrop);
            List<Pickable> crops = GetMatureCropsInRange();
            if (crops.Count == 0)
            {
                return false;
            }
            // Nearest first: of the crops the chest can actually replant (consumeItem present), work
            // the closest one, so the NPC doesn't cross the field to a farther crop it can also do.
            // (FindObjectsOfType order is arbitrary; distance makes the walk sensible.)
            Vector3 chestPos = this.transform.position;
            crops.Sort(delegate(Pickable a, Pickable b)
            {
                float adx = a.transform.position.x - chestPos.x;
                float ady = a.transform.position.y - chestPos.y;
                float adz = a.transform.position.z - chestPos.z;
                float daSqr = adx * adx + ady * ady + adz * adz;
                float bdx = b.transform.position.x - chestPos.x;
                float bdy = b.transform.position.y - chestPos.y;
                float bdz = b.transform.position.z - chestPos.z;
                float dbSqr = bdx * bdx + bdy * bdy + bdz * bdz;
                return daSqr.CompareTo(dbSqr);
            });
            foreach (Pickable pk in crops)
            {
                FarmerCrop e;
                if (!FarmerCrops.TryGetByYield(YieldsOf(pk), out e))
                {
                    continue; // defensive: GetMatureCropsInRange already filters to known yields
                }
                if (m_container.m_inventory.HaveItem(e.consumeItem))
                {
                    targetCrop = pk;
                    targetEntry = e;
                    return true;
                }
            }
            return false;
        }

        // The internal item name a mature crop drops -- its detection key. m_itemPrefab is a
        // GameObject (the prefab dropped when picked); its ItemDrop component carries the item
        // data. null if the pickable has no ItemDrop / no item data (defensive: never throws).
        private string YieldsOf(Pickable pk)
        {
            GameObject drop = pk.m_itemPrefab;
            if (drop == null)
            {
                return null;
            }
            ItemDrop it = drop.GetComponent<ItemDrop>();
            if (it == null || it.m_itemData == null || it.m_itemData.m_shared == null)
            {
                return null;
            }
            return it.m_itemData.m_shared.m_name;
        }
    }
}
