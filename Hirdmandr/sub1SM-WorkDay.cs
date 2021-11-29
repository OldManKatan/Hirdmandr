using BepInEx;
using Hirdmandr;
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
    public class WorkDaySM : StateMachine
    {
        public string changeTopState = "";

        public WorkDaySM(HirdmandrAI hmai)
        {
            hmAI = hmai;

            AddState("resetArtJob", new NodeResetArtJob(this));
            AddState("setupArtJob", new NodeSetupArtJob(this));
            AddState("goArtJob", new NodeGoArtJob(this));
            AddState("doJob", new NodeDoJob(this));

            hmAI.nextJobTime = 0;
        }

        public class NodeResetArtJob : SMNode
        {
            public string no_imp = "WorkDaySM.NodeResetArtJob not implemented";

            public NodeResetArtJob(WorkDaySM psm) : base(psm) { }

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                hmAI.workJobs = hmAI.m_hmnpc.m_skills.GetEnabledSkillsHighestFirst();
                hmAI.workJobSite = null;
                hmAI.curJob = "";

                parentSM.ChangeState("setupArtJob");
            }
        }
        public class NodeSetupArtJob : SMNode
        {
            public NodeSetupArtJob(WorkDaySM psm) : base(psm) { }

            public string no_imp = "WorkDaySM.NodeSetupArtJob not implemented";

            override public void EnterFrom(int aState) { }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                // Jotunn.Logger.LogWarning(hmAI.m_hmHumanoid.m_name + " is setting up Art Job");
                // Jotunn.Logger.LogWarning("  " + hmAI.m_hmHumanoid.m_name + " hmAI.curJob = " + hmAI.curJob);
                foreach (string aJob in hmAI.workJobs)
                {
                    Jotunn.Logger.LogWarning("    " + hmAI.m_hmHumanoid.m_name + " enabled Job = " + hmAI.curJob);
                }
                if (hmAI.curJob == "")
                {
                    if (hmAI.workJobs.Count > 0)
                    {
                        hmAI.curJob = hmAI.workJobs[0];
                        hmAI.workJobs.RemoveAt(0);
                    }
                    else
                    {
                        // Jotunn.Logger.LogError("  setupArtJob could NOT find a valid job and available site, IDLE AND WHINE NOT IMPLEMENTED");
                        return;
                    }
                }

                List<GameObject> foundWorkSites = new List<GameObject>();

                foreach (string chestString in hmAI.npcChests)
                {
                    // Jotunn.Logger.LogWarning("  NodeSetupArtJob checking for " + chestString + " chests...");
                    foreach (ZDO thisZDO in Utils.GetPrefabZDOsInRange(hmAI.transform.position, 100f, chestString))
                    {
                        ZNetView thisZNetV = ZNetScene.instance.FindInstance(thisZDO);
                        var hmChest = thisZNetV.GetComponent<HirdmandrChest>();
                        // Jotunn.Logger.LogWarning("  NodeSetupArtJob is evaluating chest " + hmChest);

                        if (hmChest.IsValidWorksite(hmAI.curJob))
                        {
                            // Jotunn.Logger.LogWarning("    NodeSetupArtJob hmChest.IsValidWorksite(hmAI.curJob) = " + hmChest.IsValidWorksite(hmAI.curJob));
                            foundWorkSites.Add(hmChest.gameObject);
                        }
                    }
                }

                // Jotunn.Logger.LogWarning("    NodeSetupArtJob foundWorkSites.Count = " + foundWorkSites.Count);
                var fwNum = foundWorkSites.Count;
                for (var i = 0; i < fwNum; i++)
                {
                    GameObject closestSite = null;
                    float closestSiteDist = 999999;

                    foreach (GameObject thisGO in foundWorkSites)
                    {
                        float thisDist = Vector3.Distance(hmAI.transform.position, thisGO.transform.position);
                        if (thisDist < closestSiteDist)
                        {
                            closestSite = thisGO;
                            closestSiteDist = thisDist;
                        }
                    }

                    if (closestSite is null)
                    {
                        // Jotunn.Logger.LogInfo("    Could not locate valid job site for " + hmAI.curJob);
                        hmAI.curJob = "";
                        return;
                    }
                    else
                    {
                        if (closestSite.GetComponent<HirdmandrChest>().Claim(hmAI.curJob, hmAI.m_znetv.GetZDO().m_uid))
                        {
                            Jotunn.Logger.LogInfo("  " + hmAI.m_hmHumanoid.m_name + " claimed worksite " + closestSite.transform.name + " for job " + hmAI.curJob);
                            hmAI.workJobSite = closestSite;
                            hmAI.workSiteHMChest = closestSite.GetComponent<HirdmandrChest>();
                            hmAI.workSiteObject = null;
                            parentSM.ChangeState("goArtJob");
                            return;
                        }
                        else
                        {
                            foundWorkSites.RemoveAt(foundWorkSites.IndexOf(closestSite));
                        }
                    }
                }
            }
        }
        public class NodeGoArtJob : SMNode
        {
            public NodeGoArtJob(WorkDaySM psm) : base(psm) { }

            public string no_imp = "WorkDaySM.NodeGoArtJob not implemented";

            override public void EnterFrom(int aState)
            {
                if (hmAI.workJobSite)
                {
                    hmAI.TryToMove(hmAI.workJobSite.transform.position, 4f, 0f, 2f, false, 30f);
                }
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                if (hmAI.moveToTrying)
                {
                    return;
                }
                else if (!hmAI.moveToTrying && hmAI.moveToReached)
                {
                    parentSM.ChangeState("doJob");
                }
                else
                {
                    if (hmAI.workJobSite)
                    {
                        hmAI.workJobSite.GetComponent<HirdmandrChest>().UnClaim(hmAI.curJob, hmAI.m_znetv.GetZDO().m_uid);
                    }
                    hmAI.curJob = "";
                    hmAI.workJobSite = null;

                    parentSM.ChangeState("resetArtJob");
                }
            }
        }
        public class NodeDoJob : SMNode
        {
            public NodeDoJob(WorkDaySM psm) : base(psm) { }

            public string no_imp = "WorkDaySM.NodeDoJob not implemented";

            public bool genericToggle = false;

            override public void EnterFrom(int aState)
            {
                hmAI.workSiteObject = null;
            }
            override public void ExitTo(int aState) { }
            override public void RunState()
            {
                if (!hmAI.workJobSite)
                {
                    hmAI.workJobSite = null;
                    parentSM.ChangeState("resetArtJob");

                }

                if (!hmAI.workSiteObject)
                {
                    Jotunn.Logger.LogWarning("Does not have a valid hmAI.workSiteObject, looking for another");
                    if (hmAI.curJob == "woodburner")
                    {
                        List<ZDO> kilns = Utils.GetPrefabZDOsInRange(hmAI.transform.position, 15f, "charcoal_kiln");
                        foreach (ZDO kilnZDO in kilns)
                        {
                            Smelter kilnSmelter = ZNetScene.instance.FindInstance(kilnZDO).GetComponent<Smelter>();
                            if (kilnSmelter.GetQueueSize() < 25)
                            {
                                Jotunn.Logger.LogWarning("  Found a valid hmAI.workSiteObject");
                                hmAI.workSiteObject = kilnSmelter.gameObject;
                                hmAI.TryToMove(hmAI.workSiteObject.transform.position + (hmAI.workSiteObject.transform.forward * 2f), 0.5f, 0f, 0f, false, 10f);
                            }
                        }
                    }
                    if (hmAI.curJob == "furnaceoperator")
                    {
                        List<ZDO> smelters = Utils.GetPrefabZDOsInRange(hmAI.transform.position, 15f, "smelter");
                        foreach (ZDO smelterZDO in smelters)
                        {
                            Smelter smelterSmelter = ZNetScene.instance.FindInstance(smelterZDO).GetComponent<Smelter>();
                            if (smelterSmelter.GetQueueSize() < 10 || smelterSmelter.GetFuel() < 19)
                            {
                                Jotunn.Logger.LogWarning("  Found a valid hmAI.workSiteObject");
                                hmAI.workSiteObject = smelterSmelter.gameObject;
                                hmAI.TryToMove(hmAI.workSiteObject.transform.position + (hmAI.workSiteObject.transform.right * 2f), 0.5f, 0f, 0f, false, 10f);
                            }
                        }
                    }
                }

                if (!hmAI.m_hmnpc.m_skills.isEnabled(hmAI.curJob))
                {
                    parentSM.ChangeState("resetArtJob");
                }

                if (Time.time > hmAI.nextJobTime)
                {
                    var workSiteInventory = hmAI.workJobSite.GetComponent<Container>().m_inventory;

                    if (hmAI.curJob == "woodburner")
                    {
                        hmAI.nextJobTime = Time.time + (18f - ((hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob) / 100f) * 9f));
                        if (hmAI.workSiteObject)
                        {
                            if (hmAI.workSiteHMChest.LinkedHaveItem("$item_wood"))
                            {
                                Jotunn.Logger.LogWarning("Wood was found");
                                Smelter kilnSmelter = hmAI.workSiteObject.GetComponent<Smelter>();
                                if (kilnSmelter.GetQueueSize() < 25)
                                {
                                    hmAI.m_hmMonsterAI.LookAt(hmAI.workSiteObject.transform.position);
                                    Jotunn.Logger.LogWarning("One wood was removed from chest");
                                    hmAI.workSiteHMChest.LinkedRemoveOneItem("$item_wood");
                                    Jotunn.Logger.LogWarning("One wood was added to kiln");
                                    kilnSmelter.QueueOre("Wood");
                                }
                                else
                                {
                                    Jotunn.Logger.LogWarning("Kiln full, looking for another...");
                                    hmAI.workSiteObject = null;
                                }
                            }
                        }
                        List<ZDO> products = Utils.GetPrefabZDOsInRange(hmAI.transform.position, 15f, "Coal");
                        foreach (ZDO coalZDO in products)
                        {
                            Jotunn.Logger.LogWarning("Found a coal");
                            GameObject eachCoal = ZNetScene.instance.FindInstance(coalZDO).gameObject;

                            Jotunn.Logger.LogWarning("Trying to add item");
                            if (hmAI.workSiteHMChest.LinkedPutItem(eachCoal.GetComponent<ItemDrop>().m_itemData))
                            {
                                Jotunn.Logger.LogWarning("Item was added");
                                Jotunn.Logger.LogWarning("Destroying item");
                                if (hmAI.m_znetv.GetZDO() == null)
                                {
                                    UnityEngine.Object.Destroy((UnityEngine.Object)(object)eachCoal);
                                }
                                else
                                {
                                    ZNetScene.instance.Destroy(eachCoal);
                                }
                            }
                        }
                    }

                    else if (hmAI.curJob == "furnaceoperator")
                    {
                        hmAI.nextJobTime = Time.time + (18f - ((hmAI.m_hmnpc.m_skills.GetSkill(hmAI.curJob) / 100f) * 9f));
                        genericToggle = !genericToggle;
                        bool oreFull = false;
                        bool fuelFull = false;

                        if (hmAI.workSiteObject)
                        {
                            Smelter smelterSmelter = hmAI.workSiteObject.GetComponent<Smelter>();

                            if (genericToggle)
                            {
                                if (smelterSmelter.GetQueueSize() < 10)
                                {
                                    hmAI.m_hmMonsterAI.LookAt(hmAI.workSiteObject.transform.position);

                                    Jotunn.Logger.LogWarning("Queue size less than 10");
                                    if (hmAI.workSiteHMChest.LinkedRemoveOneItem("$item_silverore"))
                                    {
                                        Jotunn.Logger.LogWarning("One SilverOre was added to Smelter");
                                        smelterSmelter.QueueOre("SilverOre");
                                    }
                                    else if (hmAI.workSiteHMChest.LinkedRemoveOneItem("$item_ironscrap"))
                                    {
                                        Jotunn.Logger.LogWarning("One IronOre was added to Smelter");
                                        smelterSmelter.QueueOre("IronScrap");
                                    }
                                    else if (hmAI.workSiteHMChest.LinkedRemoveOneItem("$item_copperore"))
                                    {
                                        Jotunn.Logger.LogWarning("One CopperOre was added to Smelter");
                                        smelterSmelter.QueueOre("CopperOre");
                                    }
                                    else if (hmAI.workSiteHMChest.LinkedRemoveOneItem("$item_tinore"))
                                    {
                                        Jotunn.Logger.LogWarning("One TinOre was added to Smelter");
                                        smelterSmelter.QueueOre("TinOre");
                                    }
                                }
                                else
                                {
                                    oreFull = true;
                                }
                            }
                                
                            if (smelterSmelter.GetFuel() < (float)(smelterSmelter.m_maxFuel - 1))
                            {
                                if (hmAI.workSiteHMChest.LinkedRemoveOneItem("$item_coal"))
                                {
                                    Jotunn.Logger.LogWarning("One Coal was added to Smelter");
                                    smelterSmelter.m_nview.InvokeRPC("AddFuel");
                                }
                            }
                            else
                            {
                                fuelFull = true;
                            }

                            if (oreFull && fuelFull)
                            {
                                Jotunn.Logger.LogWarning("Smelter full, looking for another...");
                                hmAI.workSiteObject = null;
                            }
                        }

                        // Clean up the ground around the smelters

                        List<ZDO> products = new List<ZDO>();
                        foreach (string ingotPrefab in new string[4] { "Silver", "Iron", "Copper", "Tin" })
                        {
                            foreach (ZDO productZDO in Utils.GetPrefabZDOsInRange(hmAI.transform.position, 15f, ingotPrefab))
                            {
                                products.Add(productZDO);
                            }
                        }
                        foreach (ZDO productZDO in products)
                        {
                            Jotunn.Logger.LogWarning("Found a coal");
                            GameObject eachProduct = ZNetScene.instance.FindInstance(productZDO).gameObject;

                            Jotunn.Logger.LogWarning("Trying to add item");
                            if (hmAI.workSiteHMChest.LinkedPutItem(eachProduct.GetComponent<ItemDrop>().m_itemData))
                            {
                                Jotunn.Logger.LogWarning("Item was added");
                                Jotunn.Logger.LogWarning("Destroying item");
                                if (hmAI.m_znetv.GetZDO() == null)
                                {
                                    UnityEngine.Object.Destroy((UnityEngine.Object)(object)eachProduct);
                                }
                                else
                                {
                                    ZNetScene.instance.Destroy(eachProduct);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
