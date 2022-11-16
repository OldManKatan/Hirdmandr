// Hirdmandr
// a Valheim mod skeleton using Jötunn
// 
// File:    Hirdmandr.cs
// Project: Hirdmandr

using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Hirdmandr
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]

    internal class Hirdmandr : BaseUnityPlugin
    {
        public const string PluginGUID = "com.github.Hirdmandr";
        public const string PluginName = "Hirdmandr";
        public const string PluginVersion = "0.0.1";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            Harmony harmony = new Harmony("Hirdmandr");
            harmony.PatchAll();

            // Jotunn comes with MonoMod Detours enabled for hooking Valheim's code
            // https://github.com/MonoMod/MonoMod

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("Hirdmandr has landed");

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html

            PrefabManager.OnVanillaPrefabsAvailable += CreateNPCPlayer;
            PrefabManager.OnVanillaPrefabsAvailable += CreateNPCChests;
            PrefabManager.OnVanillaPrefabsAvailable += CreateNPCFires;
            PrefabManager.OnVanillaPrefabsAvailable += CreateNPCBeds;
            PrefabManager.OnVanillaPrefabsAvailable += CreateNPCBows;
            PrefabManager.OnVanillaPrefabsAvailable += DebugMons;

        }

        // public void OnContainerAwake(On.Container.orig_Awake orig, Container self)
        // {
        //     orig(self);
        //     if (!(self is null) && !(self.gameObject is null))
        //     {
        //         if (self.gameObject.name.Contains("piece_npc_chest"))
        //         {
        //             self.gameObject.AddComponent<HirdmandrChest>();
        //         }
        //     }
        // }

        // Implementation of cloned asset
        //private void CreateNPCChest()
        //{
        //    GameObject chestPrefab = BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintTestTable");
        //    CustomPieceTable CPT = new CustomPieceTable(chestPrefab);
        //    PieceManager.Instance.AddPieceTable(CPT);
        //}

        private void CreateNPCChests()
        {
            // Create NPC Chest first
            if (!PrefabManager.Instance.GetPrefab("piece_npc_chest"))
            {
                var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_chest", "piece_chest_wood");
                NPCChestPrefab.AddComponent<HirdmandrChest>();

                var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
                NPCChestContainer.m_name = "NPC Chest";

                var NPCChestPiece = new CustomPiece(NPCChestPrefab, fixReference: true,
                    new PieceConfig
                    {
                        Name = "NPC Chest",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("chest_wood"),
                        Requirements = new[]
                        {
                                 new RequirementConfig { Item = "Wood", Amount = 10, Recover = true }
                        }
                    });

                NPCChestPiece.Piece.m_canBeRemoved = true;
                NPCChestPiece.Piece.m_enabled = true;
                NPCChestPiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCChestPiece);
            }

            // // Create NPC Reinforced Chest
            // if (!PrefabManager.Instance.GetPrefab("piece_npc_chest_reinforced"))
            // {
            //     var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_chest_reinforced", "piece_chest");
            //     NPCChestPrefab.AddComponent<HirdmandrChest>();
            // 
            //     var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
            //     NPCChestContainer.m_name = "NPC Chest";
            // 
            //     var NPCChestPiece = new CustomPiece(NPCChestPrefab, fixReference: true,
            //         new PieceConfig
            //         {
            //             Name = "NPC Reinforced Chest",
            //             Description = "Testing Descriptions",
            //             PieceTable = "_HammerPieceTable",
            //             Category = "Hirdmandr",
            //             AllowedInDungeons = false,
            //             Icon = GUIManager.Instance.GetSprite("chest_iron"),
            //             Requirements = new[]
            //             {
            //                      new RequirementConfig { Item = "FineWood", Amount = 10, Recover = true },
            //                      new RequirementConfig { Item = "Iron", Amount = 2, Recover = true }
            //             }
            //         });
            // 
            //     NPCChestPiece.Piece.m_canBeRemoved = true;
            //     NPCChestPiece.Piece.m_enabled = true;
            //     NPCChestPiece.Piece.m_randomTarget = true;
            // 
            //     PieceManager.Instance.AddPiece(NPCChestPiece);
            // }

            // Create NPC Blackmetal Chest
            if (!PrefabManager.Instance.GetPrefab("piece_npc_chest_blackmetal"))
            {
                var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_chest_blackmetal", "piece_chest_blackmetal");
                NPCChestPrefab.AddComponent<HirdmandrChest>();

                var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
                NPCChestContainer.m_name = "NPC Chest";

                var NPCChestPiece = new CustomPiece(NPCChestPrefab, fixReference: true,
                    new PieceConfig
                    {
                        Name = "NPC Blackmetal Chest",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("chest_blackmetal"),
                        Requirements = new[]
                        {
                                 new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
                                 new RequirementConfig { Item = "Tar", Amount = 2, Recover = true },
                                 new RequirementConfig { Item = "BlackMetal", Amount = 6, Recover = true }
                        }
                    });

                NPCChestPiece.Piece.m_canBeRemoved = true;
                NPCChestPiece.Piece.m_enabled = true;
                NPCChestPiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCChestPiece);
            }

            Jotunn.Logger.LogInfo("CreateNPCChests completed successfully");
        }

        private void CreateNPCFires()
        {
            // Add NPC Campfire
            if (!PrefabManager.Instance.GetPrefab("piece_npc_fire_pit"))
            {
                var NPCCampfirePrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_fire_pit", "fire_pit");
                var NPCCampfireContainer = NPCCampfirePrefab.GetComponent<Fireplace>();
                NPCCampfireContainer.m_name = "NPC Campfire";

                var NPCCampfirePiece = new CustomPiece(NPCCampfirePrefab, fixReference: false,
                    new PieceConfig
                    {
                        Name = "NPC Campfire",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("firepit"),
                        Requirements = new[]
                        {
                                 new RequirementConfig { Item = "Stone", Amount = 5, Recover = true },
                                 new RequirementConfig { Item = "Wood", Amount = 1, Recover = false }
                        }
                    });

                NPCCampfirePiece.Piece.m_canBeRemoved = true;
                NPCCampfirePiece.Piece.m_enabled = true;
                NPCCampfirePiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCCampfirePiece);
            }

            // Add NPC Hearth
            if (!PrefabManager.Instance.GetPrefab("piece_npc_hearth"))
            {

                var NPCHearthPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_hearth", "hearth");
                var NPCHearthContainer = NPCHearthPrefab.GetComponent<Fireplace>();
                NPCHearthContainer.m_name = "NPC Hearth";

                var NPCHearthPiece = new CustomPiece(NPCHearthPrefab, fixReference: false,
                    new PieceConfig
                    {
                        Name = "NPC Hearth",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("hearth"),
                        Requirements = new[]
                        {
                                 new RequirementConfig { Item = "Stone", Amount = 20, Recover = true },
                                 new RequirementConfig { Item = "Wood", Amount = 1, Recover = false }
                        }
                    });

                NPCHearthPiece.Piece.m_canBeRemoved = true;
                NPCHearthPiece.Piece.m_enabled = true;
                NPCHearthPiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCHearthPiece);
            }

            // Add NPC Bonfire
            if (!PrefabManager.Instance.GetPrefab("piece_npc_bonfire"))
            {

                var NPCHearthPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_bonfire", "bonfire");
                var NPCHearthContainer = NPCHearthPrefab.GetComponent<Fireplace>();
                NPCHearthContainer.m_name = "NPC Bonfire";

                var NPCHearthPiece = new CustomPiece(NPCHearthPrefab, fixReference: false,
                    new PieceConfig
                    {
                        Name = "NPC Bonfire",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("bonfire"),
                        Requirements = new[]
                        {
                                 new RequirementConfig { Item = "SurtlingCore", Amount = 1, Recover = true },
                                 new RequirementConfig { Item = "ElderBark", Amount = 5, Recover = true },
                                 new RequirementConfig { Item = "RoundLog", Amount = 5, Recover = true },
                                 new RequirementConfig { Item = "FineWood", Amount = 5, Recover = true },
                        }
                    });

                NPCHearthPiece.Piece.m_canBeRemoved = true;
                NPCHearthPiece.Piece.m_enabled = true;
                NPCHearthPiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCHearthPiece);
            }

            Jotunn.Logger.LogInfo("CreateNPCFires completed successfully");
        }

        private void CreateNPCBeds()
        {
            // Add NPC Bed
            if (!PrefabManager.Instance.GetPrefab("piece_npc_bed"))
            {

                var NPCBedPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_bed", "bed");
                NPCBedPrefab.AddComponent<HirdmandrBed>();

                var NPCBedPiece = new CustomPiece(NPCBedPrefab, fixReference: false,
                    new PieceConfig
                    {
                        Name = "NPC Bed",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("bed"),
                        Requirements = new[]
                        {
                            new RequirementConfig { Item = "Wood", Amount = 8, Recover = true }
                        }
                    });

                NPCBedPiece.Piece.m_canBeRemoved = true;
                NPCBedPiece.Piece.m_enabled = true;
                NPCBedPiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCBedPiece);
            }

            // Add NPC Dragon Bed
            if (!PrefabManager.Instance.GetPrefab("piece_npc_bed02"))
            {

                var NPCBed02Prefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_bed02", "piece_bed02");
                NPCBed02Prefab.AddComponent<HirdmandrBed>();

                var NPCBed02Piece = new CustomPiece(NPCBed02Prefab, fixReference: false,
                    new PieceConfig
                    {
                        Name = "NPC Dragon Bed",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("bed02"),
                        Requirements = new[]
                        {
                                 new RequirementConfig { Item = "FineWood", Amount = 40, Recover = true },
                                 new RequirementConfig { Item = "DeerHide", Amount = 7, Recover = true },
                                 new RequirementConfig { Item = "WolfPelt", Amount = 4, Recover = true },
                                 new RequirementConfig { Item = "Feathers", Amount = 10, Recover = true },
                                 new RequirementConfig { Item = "IronNails", Amount = 15, Recover = true }
                        }
                    });

                NPCBed02Piece.Piece.m_canBeRemoved = true;
                NPCBed02Piece.Piece.m_enabled = true;
                NPCBed02Piece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCBed02Piece);
            }

            Jotunn.Logger.LogInfo("CreateNPCBeds completed successfully");
        }

        private void CreateNPCBows()
        {
            if (!PrefabManager.Instance.GetPrefab("npc_crude_bow"))
            {
                CustomItem crudeBowPrefab = new CustomItem("npc_crude_bow", "Bow");

                // Replace vanilla properties of the custom item
                var crudeBowItemDrop = crudeBowPrefab.ItemDrop;

                var skeletonBow = PrefabManager.Instance.GetPrefab("skeleton_bow");
                crudeBowItemDrop = skeletonBow.GetComponent<ItemDrop>();

                ItemManager.Instance.AddItem(crudeBowPrefab);
            }

            Jotunn.Logger.LogInfo("CreateNPCBows completed successfully");
        }

        private void CreateNPCPlayer()
        {
            // Create NPC Player entity
            if (!PrefabManager.Instance.GetPrefab("npc_entity"))
            {

                var NPCPlayerPrefab = PrefabManager.Instance.CreateClonedPrefab("npc_entity", "Goblin");

                for (int i = 0; i < NPCPlayerPrefab.transform.childCount; i++)
                {
                    UnityEngine.Object.Destroy(NPCPlayerPrefab.transform.GetChild(i).gameObject);
                }
                GameObject playerPrefab = PrefabManager.Instance.GetPrefab("Player");
                GameObject eyePos = null;
                for (int j = 0; j < playerPrefab.transform.childCount; j++)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate(playerPrefab.transform.GetChild(j).gameObject, NPCPlayerPrefab.transform);
                    gameObject.name = gameObject.name.TrimCloneTag();
                    if (gameObject.name == "EyePos")
                    {
                        eyePos = gameObject;
                    }
                }

                Humanoid hum_orig = NPCPlayerPrefab.GetComponent<Humanoid>();
                NPCPlayerPrefab.AddComponent<NPCPlayerClone>();
                NPCPlayerClone npcpc = NPCPlayerPrefab.GetComponent<NPCPlayerClone>();

                // Copy over Humanoid attributes
                npcpc.m_equipStaminaDrain = hum_orig.m_equipStaminaDrain;
                npcpc.m_blockStaminaDrain = hum_orig.m_blockStaminaDrain;
                npcpc.m_pickupEffects = hum_orig.m_pickupEffects;
                npcpc.m_dropEffects = hum_orig.m_dropEffects;
                npcpc.m_consumeItemEffects = hum_orig.m_consumeItemEffects;
                npcpc.m_equipEffects = hum_orig.m_equipEffects;
                npcpc.m_perfectBlockEffect = hum_orig.m_perfectBlockEffect;
                npcpc.m_inventory = hum_orig.m_inventory;
                npcpc.m_beardItem = hum_orig.m_beardItem;
                npcpc.m_hairItem = hum_orig.m_hairItem;
                npcpc.m_blockTimer = hum_orig.m_blockTimer;
                npcpc.m_lastCombatTimer = hum_orig.m_lastCombatTimer;
                npcpc.m_eqipmentStatusEffects = hum_orig.m_eqipmentStatusEffects;

                // Copy over Character attributes
                Character char_orig = NPCPlayerPrefab.GetComponent<Character>();

                npcpc.m_groundContactPoint = char_orig.m_groundContactPoint;
                npcpc.m_groundContactNormal = char_orig.m_groundContactNormal;
                npcpc.m_name = char_orig.m_name;
                npcpc.m_group = char_orig.m_group;
                npcpc.m_faction = char_orig.m_faction;
                npcpc.m_bossEvent = char_orig.m_bossEvent;
                npcpc.m_defeatSetGlobalKey = char_orig.m_defeatSetGlobalKey;
                npcpc.m_crouchSpeed = char_orig.m_crouchSpeed;
                npcpc.m_walkSpeed = char_orig.m_walkSpeed;
                npcpc.m_speed = char_orig.m_speed;
                npcpc.m_turnSpeed = char_orig.m_turnSpeed;
                npcpc.m_runSpeed = char_orig.m_runSpeed;
                npcpc.m_runTurnSpeed = char_orig.m_runTurnSpeed;
                npcpc.m_flySlowSpeed = char_orig.m_flySlowSpeed;
                npcpc.m_flyFastSpeed = char_orig.m_flyFastSpeed;
                npcpc.m_flyTurnSpeed = char_orig.m_flyTurnSpeed;
                npcpc.m_acceleration = char_orig.m_acceleration;
                npcpc.m_jumpForce = char_orig.m_jumpForce;
                npcpc.m_jumpForceTiredFactor = char_orig.m_jumpForceTiredFactor;
                npcpc.m_airControl = char_orig.m_airControl;
                npcpc.m_canSwim = char_orig.m_canSwim;
                npcpc.m_swimDepth = char_orig.m_swimDepth;
                npcpc.m_swimSpeed = char_orig.m_swimSpeed;
                npcpc.m_swimTurnSpeed = char_orig.m_swimTurnSpeed;
                npcpc.m_swimAcceleration = char_orig.m_swimAcceleration;
                npcpc.m_groundTiltSpeed = char_orig.m_groundTiltSpeed;
                npcpc.m_jumpStaminaUsage = char_orig.m_jumpStaminaUsage;
                npcpc.m_hitEffects = char_orig.m_hitEffects;
                npcpc.m_critHitEffects = char_orig.m_critHitEffects;
                npcpc.m_backstabHitEffects = char_orig.m_backstabHitEffects;
                npcpc.m_deathEffects = char_orig.m_deathEffects;
                npcpc.m_waterEffects = char_orig.m_waterEffects;
                npcpc.m_tarEffects = char_orig.m_tarEffects;
                npcpc.m_slideEffects = char_orig.m_slideEffects;
                npcpc.m_jumpEffects = char_orig.m_jumpEffects;
                npcpc.m_tolerateWater = char_orig.m_tolerateWater;
                npcpc.m_tolerateSmoke = char_orig.m_tolerateSmoke;
                npcpc.m_health = char_orig.m_health;
                npcpc.m_staggerWhenBlocked = char_orig.m_staggerWhenBlocked;
                npcpc.m_backstabTime = char_orig.m_backstabTime;
                npcpc.m_moveDir = char_orig.m_moveDir;
                npcpc.m_lookDir = char_orig.m_lookDir;
                npcpc.m_lookYaw = char_orig.m_lookYaw;
                npcpc.m_lastGroundNormal = char_orig.m_lastGroundNormal;
                npcpc.m_lastGroundPoint = char_orig.m_lastGroundPoint;
                npcpc.m_lastAttachPos = char_orig.m_lastAttachPos;
                npcpc.m_maxAirAltitude = char_orig.m_maxAirAltitude;
                npcpc.m_waterLevel = char_orig.m_waterLevel;
                npcpc.m_tarLevel = char_orig.m_tarLevel;
                npcpc.m_swimTimer = char_orig.m_swimTimer;
                npcpc.m_level = char_orig.m_level;
                npcpc.m_currentVel = char_orig.m_currentVel;
                npcpc.m_groundTiltNormal = char_orig.m_groundTiltNormal;
                npcpc.m_pushForce = char_orig.m_pushForce;
                npcpc.m_rootMotion = char_orig.m_rootMotion;
                npcpc.m_lodVisible = char_orig.m_lodVisible;

                Destroy(hum_orig);
                // Destroy(NPCPlayerPrefab.GetComponent<MonsterAI>());

                VisEquipment visEquipComp = NPCPlayerPrefab.GetComponent<VisEquipment>();
                visEquipComp.m_bodyModel = NPCPlayerPrefab.transform.Find("Visual/body").GetComponent<SkinnedMeshRenderer>();
                visEquipComp.m_leftHand = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand/LeftHand_Attach").GetComponent<Transform>();
                visEquipComp.m_rightHand = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand/RightHand_Attach").GetComponent<Transform>();
                visEquipComp.m_helmet = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/Spine2/Neck/Head/Helmet_attach").GetComponent<Transform>();
                // component.m_backShield = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackShield_attach").GetComponent<Transform>();
                // component.m_backMelee = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackOneHanded_attach").GetComponent<Transform>();
                // component.m_backTwohandedMelee = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackTwohanded_attach").GetComponent<Transform>();
                // component.m_backBow = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackBow_attach").GetComponent<Transform>();
                // component.m_backTool = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/BackTool_attach").GetComponent<Transform>();
                // component.m_backAtgeir = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackAtgeir_attach").GetComponent<Transform>();
                visEquipComp.m_clothColliders = new CapsuleCollider[5]
                {
                    NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/ClothCollider").GetComponent<CapsuleCollider>(),
                    NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/LeftUpLeg/ClothCollider").GetComponent<CapsuleCollider>(),
                    NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/RightUpLeg/ClothCollider").GetComponent<CapsuleCollider>(),
                    NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/ClothCollider (3)").GetComponent<CapsuleCollider>(),
                    NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/Spine2/ClothCollider (4)").GetComponent<CapsuleCollider>()
                };
                visEquipComp.m_models = playerPrefab.GetComponent<VisEquipment>().m_models;
                visEquipComp.m_isPlayer = true;
                CapsuleCollider capCollider = NPCPlayerPrefab.GetComponent<CapsuleCollider>();
                capCollider.center = (playerPrefab.GetComponent<CapsuleCollider>().center);
                capCollider.radius = (playerPrefab.GetComponent<CapsuleCollider>().radius);
                capCollider.height = (playerPrefab.GetComponent<CapsuleCollider>().height);
                NPCPlayerPrefab.GetComponent<Rigidbody>().interpolation = playerPrefab.GetComponent<Rigidbody>().interpolation;
                ZSyncAnimation NPCZSyncAnim = NPCPlayerPrefab.GetComponent<ZSyncAnimation>();
                ZSyncAnimation PlayerZSyncAnim = playerPrefab.GetComponent<ZSyncAnimation>();
                NPCZSyncAnim.m_syncBools = new List<string>(PlayerZSyncAnim.m_syncBools);
                NPCZSyncAnim.m_syncFloats = new List<string>(PlayerZSyncAnim.m_syncFloats);
                NPCZSyncAnim.m_syncInts = new List<string>(PlayerZSyncAnim.m_syncInts);
                FootStep NPCFootstepComp = NPCPlayerPrefab.GetComponent<FootStep>();
                FootStep PlayerFootstepComp = playerPrefab.GetComponent<FootStep>();
                NPCFootstepComp.m_footstepCullDistance = PlayerFootstepComp.m_footstepCullDistance;
                NPCFootstepComp.m_effects.Clear();
                foreach (FootStep.StepEffect effect in PlayerFootstepComp.m_effects)
                {
                    NPCFootstepComp.m_effects.Add(effect);
                }
                NPCFootstepComp.m_feet[0] = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot");
                NPCFootstepComp.m_feet[1] = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/RightUpLeg/RightLeg/RightFoot");

                Humanoid NPCHumComp = NPCPlayerPrefab.GetComponent<Humanoid>();
                Humanoid playerHumComp = playerPrefab.GetComponent<Humanoid>();
                NPCHumComp.m_crouchSpeed = playerHumComp.m_crouchSpeed;
                NPCHumComp.m_walkSpeed = playerHumComp.m_walkSpeed;
                NPCHumComp.m_speed = playerHumComp.m_speed;
                NPCHumComp.m_turnSpeed = playerHumComp.m_turnSpeed;
                NPCHumComp.m_runSpeed = playerHumComp.m_runSpeed;
                NPCHumComp.m_runTurnSpeed = playerHumComp.m_runTurnSpeed;
                NPCHumComp.m_acceleration = playerHumComp.m_acceleration;
                NPCHumComp.m_jumpForce = playerHumComp.m_jumpForce;
                NPCHumComp.m_jumpForceForward = playerHumComp.m_jumpForceForward;
                NPCHumComp.m_swimDepth = playerHumComp.m_swimDepth;
                NPCHumComp.m_swimSpeed = playerHumComp.m_swimSpeed;
                NPCHumComp.m_damageModifiers = playerHumComp.m_damageModifiers.Clone();
                NPCHumComp.m_defaultItems = new GameObject[0];
                NPCHumComp.m_randomWeapon = new GameObject[0];
                NPCHumComp.m_randomArmor = new GameObject[0];
                NPCHumComp.m_randomShield = new GameObject[0];
                NPCHumComp.m_randomSets = new Humanoid.ItemSet[0];
                NPCHumComp.m_hitEffects = Utils.CloneEffectList(playerHumComp.m_hitEffects);
                NPCHumComp.m_critHitEffects = Utils.CloneEffectList(playerHumComp.m_critHitEffects);
                NPCHumComp.m_backstabHitEffects = Utils.CloneEffectList(NPCHumComp.m_backstabHitEffects);
                NPCHumComp.m_waterEffects = Utils.CloneEffectList(playerHumComp.m_waterEffects);
                NPCHumComp.m_slideEffects = Utils.CloneEffectList(playerHumComp.m_slideEffects);
                NPCHumComp.m_jumpEffects = Utils.CloneEffectList(playerHumComp.m_jumpEffects);
                NPCHumComp.m_dropEffects = Utils.CloneEffectList(playerHumComp.m_dropEffects);
                NPCHumComp.m_consumeItemEffects = Utils.CloneEffectList(playerHumComp.m_consumeItemEffects);
                NPCHumComp.m_equipEffects = Utils.CloneEffectList(playerHumComp.m_equipEffects);
                NPCHumComp.m_perfectBlockEffect = Utils.CloneEffectList(playerHumComp.m_perfectBlockEffect);


                //Character NPCCharComp = NPCPlayerPrefab.GetComponent<Character>();
                //NPCCharComp.m_name = "Human";
                //NPCCharComp.m_health = 60f;
                //NPCCharComp.m_faction = Character.Faction.Players;

                //NPCHumComp.m_name = "Human";
                //NPCHumComp.m_health = 60f;
                //NPCHumComp.m_faction = Character.Faction.Players;

                NPCPlayerPrefab.AddComponent<HirdmandrNPC>();
                NPCPlayerPrefab.AddComponent<HirdmandrAI>();
                NPCPlayerPrefab.AddComponent<HirdmandrGUI>();
                NPCPlayerPrefab.AddComponent<HirdmandrGUIRescue>();

                NPCPlayerPrefab.SetActive(true);

                PrefabManager.Instance.AddPrefab(NPCPlayerPrefab);
                Jotunn.Logger.LogInfo("CreateNPCPlayer Prefab Added");
            }
            Jotunn.Logger.LogInfo("CreateNPCPlayer completed successfully");
        }

        public void DebugMons()
        {
            var DebugMonPrefab1 = PrefabManager.Instance.CreateClonedPrefab("debugSkeleton", "Skeleton");
            DebugMonPrefab1.AddComponent<DebugMAI>();
            PrefabManager.Instance.AddPrefab(DebugMonPrefab1);

            var DebugMonPrefab2 = PrefabManager.Instance.CreateClonedPrefab("debugDraugr", "Draugr_Ranged");
            DebugMonPrefab2.AddComponent<DebugMAI>();
            PrefabManager.Instance.AddPrefab(DebugMonPrefab2);

            var DebugMonPrefab3 = PrefabManager.Instance.CreateClonedPrefab("debugGoblinArcher", "GoblinArcher");
            DebugMonPrefab3.AddComponent<DebugMAI>();
            PrefabManager.Instance.AddPrefab(DebugMonPrefab3);

            var DebugMonPrefab4 = PrefabManager.Instance.CreateClonedPrefab("debugGoblin", "Goblin");
            DebugMonPrefab4.AddComponent<DebugMAI>();
            PrefabManager.Instance.AddPrefab(DebugMonPrefab4);

        }
    }
}
