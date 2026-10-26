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
    /// <summary>
    /// Main entry point for the Hirdmandr mod. 
    /// This mod adds functional NPCs (hirdmandr) to Valheim, inspired by Norse culture.
    /// Rescued warriors become loyal followers who can work artisan jobs or defend as warriors.
    /// They have daily schedules, and respond to player direction.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]

    internal class Hirdmandr : BaseUnityPlugin
    {
        /// <summary>Unique identifier for registry and multiplayer compatibility.</summary>
        public const string PluginGUID = "com.github.Hirdmandr";
        
        /// <summary>Pure display name shown in game's mod manager.</summary>
        public const string PluginName = "Hirdmandr";
        
        /// <summary>Must exactly match Package/manifest.json version_number (BepInEx 5 chainloader requirement).</summary>
        public const string PluginVersion = "1.0.16";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

    /// <summary>
    /// Initializes the mod: sets up Harmony patches and registers prefab creation delegates.
    /// We wait for OnVanillaPrefabsAvailable to ensure core Valheim assets exist before cloning/modifying them.
    /// </summary>
    private void Awake()
    {
        try
        {
        // Initialize Harmony for applying all mod patches
        Harmony harmony = new Harmony("Hirdmandr");
        harmony.PatchAll();

        // Confirms successful initialization (appears in Valheim log)
        Jotunn.Logger.LogInfo("Hirdmandr has landed");

        // Register callbacks to create NPC prefabs once Vanilla assets are available:
        // - CreateNPCPlayer: Creates the rescued warrior NPC entity
        // - CreateNPCChests: Adds chest/barrel pieces (wood, reinforced, blackmetal, barrel, grausten) for worksites
        // - CreateNPCFires: Adds campfire/hearth/bonfire pieces for gathering/socializing
        // - CreateNPCBeds: Adds bed pieces for sleeping/rest mechanics  
        // - DebugMons: Creates debug monster prefabs (unused in final build?)
        PrefabManager.OnVanillaPrefabsAvailable += CreateNPCPlayer;
        PrefabManager.OnVanillaPrefabsAvailable += CreateNPCChests;
        PrefabManager.OnVanillaPrefabsAvailable += CreateNPCFires;
        PrefabManager.OnVanillaPrefabsAvailable += CreateNPCBeds;
        PrefabManager.OnVanillaPrefabsAvailable += DebugMons;
        // Soul item (R3c): the pickable record a rescued NPC becomes when it dies. Registered
        // alongside the other cloned prefabs; spawned on demand (test) / on NPC death.
        PrefabManager.OnVanillaPrefabsAvailable += SoulItem.RegisterSoulItem;
        // Altar to Hella (PMW #62): the resurrection altar piece, cloned from the Eikthyrnir
        // offering bowl. Registered at scene load (OnPrefabsRegistered), not menu time: its
        // ZNetView/Piece Awake and the stonecutter station lookup all need ZNet up.
        PrefabManager.OnPrefabsRegistered += CreateAltarHella;

        // Register the in-game console commands (e.g. "/hirdmandr spawn"). This must run
        // before a level loads (so the game's Terminal picks the command up) - Awake is that
        // point. CommandManager.Instance is lazily created, so it is safe here.
        CommandManager.Instance.AddConsoleCommand(new HirdmandrSpawnCommand());

        // Rescue spawn (R3c refactor): start the periodic world-rescue scanner. It runs a
        // coroutine on this (the persistent plugin MonoBehaviour); it idles until a level loads,
        // then scans the zones around the player every few seconds and offers an unrescued rescue
        // at a deterministic subset of whitelisted, unclaimed structures. See RescueScanner +
        // rescue_spawn_design.md.
        RescueScanner.Start(this);
        }
        catch (System.Exception e)
        {
            Jotunn.Logger.LogError("Hirdmandr.Awake threw:\n" + e);
            throw;
        }
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

        /// <summary>
        /// Creates NPC chests (wood, reinforced, blackmetal, barrel, grausten) that NPCs will use
        /// as worksites. Multiple NPCs near these chests can be assigned jobs like wood burner or
        /// furnace operator. Chests are linked via HirdmandrChest component to manage inventory
        /// pooling. Each clone keeps the exact vanilla cost/requirements of its source piece.
        /// </summary>
        private void CreateNPCChests()
        {
            // Register first the basic wooden chest for common worksites (farmers, etc.)
            if (!PrefabManager.Instance.GetPrefab("piece_npc_chest"))
            {
                var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_chest", "piece_chest_wood");
                NPCChestPrefab.AddComponent<HirdmandrChest>();

                var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
                NPCChestContainer.m_name = "NPC Chest"; // Generic container name for inventory pooling

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

            // Create NPC Reinforced Chest
            if (!PrefabManager.Instance.GetPrefab("piece_npc_chest_reinforced"))
            {
                var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_chest_reinforced", "piece_chest");
                NPCChestPrefab.AddComponent<HirdmandrChest>();

                var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
                NPCChestContainer.m_name = "NPC Reinforced Chest"; // Generic container name for inventory pooling

                var NPCChestPiece = new CustomPiece(NPCChestPrefab, fixReference: true,
                    new PieceConfig
                    {
                        Name = "NPC Reinforced Chest",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("chest_iron"),
                        Requirements = new[]
                        {
                                 new RequirementConfig { Item = "FineWood", Amount = 10, Recover = true },
                                 new RequirementConfig { Item = "Iron", Amount = 2, Recover = true }
                        }
                    });

                NPCChestPiece.Piece.m_canBeRemoved = true;
                NPCChestPiece.Piece.m_enabled = true;
                NPCChestPiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCChestPiece);
            }

            // Create NPC Blackmetal Chest
            if (!PrefabManager.Instance.GetPrefab("piece_npc_chest_blackmetal"))
            {
                var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_chest_blackmetal", "piece_chest_blackmetal");
                NPCChestPrefab.AddComponent<HirdmandrChest>();

                var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
                NPCChestContainer.m_name = "NPC Blackmetal Chest";

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

            // Create NPC Barrel (vanilla barrel piece is named "piece_chestbarrel")
            if (!PrefabManager.Instance.GetPrefab("piece_npc_barrel"))
            {
                var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_barrel", "piece_chest_barrel");
                NPCChestPrefab.AddComponent<HirdmandrChest>();

                var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
                NPCChestContainer.m_name = "NPC Barrel"; // Generic container name for inventory pooling

                var NPCChestPiece = new CustomPiece(NPCChestPrefab, fixReference: true,
                    new PieceConfig
                    {
                        Name = "NPC Barrel",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("chest_barrel"),
                        Requirements = new[]
                        {
                                 new RequirementConfig { Item = "Wood", Amount = 10, Recover = true },
                                 new RequirementConfig { Item = "BarrelRings", Amount = 1, Recover = true }
                        }
                    });

                NPCChestPiece.Piece.m_canBeRemoved = true;
                NPCChestPiece.Piece.m_enabled = true;
                NPCChestPiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCChestPiece);
            }

            // Create NPC Grausten Chest
            if (!PrefabManager.Instance.GetPrefab("piece_npc_chest_grausten"))
            {
                var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_chest_grausten", "piece_chest_grausten");
                NPCChestPrefab.AddComponent<HirdmandrChest>();

                var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
                NPCChestContainer.m_name = "NPC Grausten Chest"; // Generic container name for inventory pooling

                var NPCChestPiece = new CustomPiece(NPCChestPrefab, fixReference: true,
                    new PieceConfig
                    {
                        Name = "NPC Grausten Chest",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = GUIManager.Instance.GetSprite("chest_grausten"),
                        Requirements = new[]
                        {
							new RequirementConfig { Item = "Grausten", Amount = 10, Recover = true },
							new RequirementConfig { Item = "TrophyCharredMelee", Amount = 5, Recover = true },
							new RequirementConfig { Item = "FlametalNew", Amount = 2, Recover = true }
                        }
                    });

                NPCChestPiece.Piece.m_canBeRemoved = true;
                NPCChestPiece.Piece.m_enabled = true;
                NPCChestPiece.Piece.m_randomTarget = true;

                PieceManager.Instance.AddPiece(NPCChestPiece);
            }

            Jotunn.Logger.LogInfo("CreateNPCChests completed successfully");
        }

        /// <summary>
        /// Creates NPC fires (campfires, hearths, bonfires) and HirdmandrBed.
        /// Fires act as social gathering spots and warm the environment for NPC comfort; 
        /// beds track who owns them so NPCs can sleep/rest properly.
        /// </summary>
        private void CreateNPCFires()
        {
            // Add NPC Campfire (basic fire type, used by most villagers)
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

        private void CreateNPCPlayer()
        {
            try
            {
            // Create NPC Player entity
            if (!PrefabManager.Instance.GetPrefab("npc_entity"))
            {

                var NPCPlayerPrefab = PrefabManager.Instance.CreateClonedPrefab("npc_entity", "Goblin");
                Destroy(NPCPlayerPrefab.GetComponent<Humanoid>());
                Destroy(NPCPlayerPrefab.GetComponent<MonsterAI>());


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

                NPCPlayerPrefab.AddComponent<NPCPlayerClone>();
                NPCPlayerClone npcpc = NPCPlayerPrefab.GetComponent<NPCPlayerClone>();
                NPCPlayerPrefab.AddComponent<MonsterAI>();

                //// Copy over Humanoid attributes
                //npcpc.m_equipStaminaDrain = 10f;
                //npcpc.m_blockStaminaDrain = 25f;
                //npcpc.m_pickupEffects = new EffectList();
                //npcpc.m_dropEffects = new EffectList();
                //npcpc.m_consumeItemEffects = new EffectList();
                //npcpc.m_equipEffects = new EffectList();
                //npcpc.m_perfectBlockEffect = new EffectList();
                //npcpc.m_inventory = new Inventory("Inventory", null, 8, 4);
                //npcpc.m_beardItem = "Beard0";
                //npcpc.m_hairItem = "Hair0";
                //npcpc.m_blockTimer = 9999f;
                //npcpc.m_lastCombatTimer = 999f;
                //npcpc.m_eqipmentStatusEffects = new HashSet<StatusEffect>();

                //npcpc.m_groundContactPoint = Vector3.zero;
                //npcpc.m_groundContactNormal = Vector3.zero;
                //npcpc.m_name = "Human";
                //npcpc.m_group = "";
                //npcpc.m_faction = Character.Faction.Players;
                //npcpc.m_bossEvent = "";
                //npcpc.m_defeatSetGlobalKey = "";
                //npcpc.m_crouchSpeed = 2f;
                //npcpc.m_walkSpeed = 5f;
                //npcpc.m_speed = 10f;
                //npcpc.m_turnSpeed = 300f;
                //npcpc.m_runSpeed = 20f;
                //npcpc.m_runTurnSpeed = 300f;
                //npcpc.m_flySlowSpeed = 5f;
                //npcpc.m_flyFastSpeed = 12f;
                //npcpc.m_flyTurnSpeed = 12f;
                //npcpc.m_acceleration = 1f;
                //npcpc.m_jumpForce = 10f;
                //npcpc.m_jumpForceTiredFactor = 0.7f;
                //npcpc.m_airControl = 0.1f;
                //npcpc.m_canSwim = true;
                //npcpc.m_swimDepth = 2f;
                //npcpc.m_swimSpeed = 2f;
                //npcpc.m_swimTurnSpeed = 100f;
                //npcpc.m_swimAcceleration = 0.05f;
                //npcpc.m_groundTiltSpeed = 50f;
                //npcpc.m_jumpStaminaUsage = 10f;
                //npcpc.m_hitEffects = new EffectList();
                //npcpc.m_critHitEffects = new EffectList();
                //npcpc.m_backstabHitEffects = new EffectList();
                //npcpc.m_deathEffects = new EffectList();
                //npcpc.m_waterEffects = new EffectList();
                //npcpc.m_tarEffects = new EffectList();
                //npcpc.m_slideEffects = new EffectList();
                //npcpc.m_jumpEffects = new EffectList();
                //npcpc.m_tolerateWater = char_orig.m_tolerateWater;
                //npcpc.m_tolerateSmoke = char_orig.m_tolerateSmoke;
                //npcpc.m_health = 60f;
                //npcpc.m_staggerWhenBlocked = true;
                //npcpc.m_backstabTime = char_orig.m_backstabTime;
                //npcpc.m_moveDir = Vector3.zero;
                //npcpc.m_lookDir = Vector3.forward;
                //npcpc.m_lookYaw = Quaternion.identity;
                //npcpc.m_lastGroundNormal = Vector3.up;
                //npcpc.m_lastGroundPoint = Vector3.zero;
                //npcpc.m_lastAttachPos = Vector3.zero;
                //npcpc.m_maxAirAltitude = char_orig.m_maxAirAltitude;
                //npcpc.m_waterLevel = char_orig.m_waterLevel;
                //npcpc.m_tarLevel = char_orig.m_tarLevel;
                //npcpc.m_swimTimer = char_orig.m_swimTimer;
                //npcpc.m_level = 1;
                //npcpc.m_currentVel = Vector3.zero;
                //npcpc.m_groundTiltNormal = Vector3.up;
                //npcpc.m_pushForce = Vector3.zero;
                //npcpc.m_rootMotion = Vector3.zero;
                //npcpc.m_lodVisible = true;


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
                visEquipComp.SetupCloth(NPCPlayerPrefab);
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

                // Must be the live NPCPlayerClone: GetComponent<Humanoid>() returns the Goblin
                // Humanoid destroyed above (Destroy is deferred to end of frame), so copies on
                // it die with it and instantiated NPCs are left at Character..ctor defaults.
                Humanoid NPCHumComp = npcpc;
                Humanoid playerHumComp = playerPrefab.GetComponent<Humanoid>();
                // #Speed: the NPC's run-speed factor is 1 (Humanoid/NPCPlayerClone never override
                // GetRunSpeedFactor), so its run speed is just m_runSpeed. The player's run speed is
                // m_runSpeed * (1 + runSkill*0.25) * (1 + equipMoveMod*1.5) (Player.GetRunSpeedFactor),
                // and runSkill = Clamp01(skillLevel/100) (Skills.GetSkillFactor). npcSpeedFactor is
                // set so the NPC exactly matches a player at 66% Run skill with no gear:
                //   npcSpeedFactor = 1 + 0.66*0.25 = 1.165
                // so the NPC outruns any player below 66% Run skill and is even at 66%. Only the
                // SPEED fields get the factor; turn/accel/jump/depth are not speeds and stay 1:1.
                // (Note: the NPC's effective speed is additionally scaled by the world enemy-speed
                // settings because IsPlayer() is false - see Character.UpdateWalking.)
                float npcSpeedFactor = 1.165f;
                NPCHumComp.m_crouchSpeed = playerHumComp.m_crouchSpeed * npcSpeedFactor;
                NPCHumComp.m_walkSpeed = playerHumComp.m_walkSpeed * npcSpeedFactor;
                NPCHumComp.m_speed = playerHumComp.m_speed * npcSpeedFactor;
                NPCHumComp.m_turnSpeed = playerHumComp.m_turnSpeed;
                NPCHumComp.m_runSpeed = playerHumComp.m_runSpeed * npcSpeedFactor;
                NPCHumComp.m_runTurnSpeed = playerHumComp.m_runTurnSpeed;
                NPCHumComp.m_acceleration = playerHumComp.m_acceleration;
                NPCHumComp.m_jumpForce = playerHumComp.m_jumpForce;
                NPCHumComp.m_jumpForceForward = playerHumComp.m_jumpForceForward;
                // #43: hardcoded to the vanilla player value (mimic the vanilla game).
                NPCHumComp.m_swimDepth = 1.5f;
                NPCHumComp.m_swimSpeed = playerHumComp.m_swimSpeed * npcSpeedFactor;
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

                // NOTE: the NPC's skill-levelup effect (NPCPlayerClone.m_skillLevelupEffects) is
                // populated per-instance in HirdmandrNPC.Awake() (not on this template) because it is
                // a non-serialized field that resets to empty on every instantiated NPC.


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
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("Hirdmandr.CreateNPCPlayer threw:\n" + e);
                throw;
            }
        }

        public void DebugMons()
        {
            // OnVanillaObjectsAvailable can fire more than once. On a later pass the debug clones
            // already exist, so CreateClonedPrefab returns null and the .AddComponent below NRE'd.
            if (PrefabManager.Instance.GetPrefab("debugSkeleton"))
            {
                return;
            }

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

        /// <summary>
        /// Altar to Hella (PMW #62): the resurrection altar piece.
        ///
        /// Clones the Eikthyrnir offering bowl ("altar") and exposes it as a standalone piece
        /// (Jotunn cannot register a whole building as a piece). Runs from
        /// OnPrefabsRegistered (scene load) rather than the menu-time event, because
        /// ZNetView.Awake (which creates and registers the piece ZDO), Piece.Awake (which
        /// binds to the view) and the stonecutter CraftingStation lookup all need ZNet up.
        /// The clone root carries no network component of its own, so ZNetView and Piece
        /// are added here (ZNetView first, so Piece.Awake can see it). The vanilla
        /// OfferingBowl component is destroyed from the clone - it is a boss-spawn ritual
        /// with its own ZNetView and we only want the bowl's visuals.
        /// </summary>
        private void CreateAltarHella()
        {
            if (PrefabManager.Instance.GetPrefab("piece_hm_altar_hella"))
            {
                return;
            }
            // Usual prefab copy, same as every other piece in this mod. If 'altar' is not
            // resolvable at this point, Jotunn logs "Failed to clone prefab, can not find
            // base prefab with name: ..." and returns null.
            GameObject altar = PrefabManager.Instance.CreateClonedPrefab("piece_hm_altar_hella", "altar");
            if (altar == null)
            {
                Jotunn.Logger.LogError("[Hirdmandr] CreateAltarHella: could not clone 'altar' - cannot create the Altar to Hella.");
                return;
            }
            /*
            // Previous approach: resolve the 'Eikthyrnir' scene prefab and find its offering
            // bowl child. Kept commented for easy revert.
            if (ZNetScene.instance == null)
            {
                Jotunn.Logger.LogError("[Hirdmandr] CreateAltarHella: ZNetScene.instance is null - retrying on the next scene load.");
                return;
            }
            GameObject eikthyrnir = ZNetScene.instance.GetPrefab("Eikthyrnir");
            if (eikthyrnir == null)
            {
                Jotunn.Logger.LogError("[Hirdmandr] CreateAltarHella: 'Eikthyrnir' prefab not registered - cannot create the Altar to Hella.");
                return;
            }
            // Real hierarchy (UnityExplorer): Eikthyrnir/offeraltar_deer/altar.
            Transform deer = FindChildRecursive(eikthyrnir.transform, "offeraltar_deer");
            Transform bowl = deer != null ? FindChildRecursive(deer, "altar") : null;
            if (bowl == null)
            {
                // List the direct children so the right name/depth is visible from the log.
                var names = new List<string>();
                foreach (Transform child in eikthyrnir.transform)
                {
                    names.Add(child.name);
                }
                Jotunn.Logger.LogError("[Hirdmandr] CreateAltarHella: no 'offeraltar_deer/altar' under Eikthyrnir (direct children: " + string.Join(", ", names) + ") - aborting.");
                return;
            }
            altar = UnityEngine.Object.Instantiate(bowl.gameObject);
            altar.name = "piece_hm_altar_hella";
            */
            // The child's local transform carries its offset within the building; as a
            // standalone piece prefab the root must sit at the origin with an identity pose.
            altar.transform.localPosition = Vector3.zero;
            altar.transform.localRotation = Quaternion.identity;
            altar.transform.localScale = Vector3.one;
            altar.SetActive(true);

            // The vanilla bowl is a boss-spawn offering with its own ZNetView; only the visuals
            // are wanted.
            OfferingBowl offeringBowl = altar.GetComponent<OfferingBowl>();
            if (offeringBowl != null)
            {
                UnityEngine.Object.DestroyImmediate(offeringBowl);
            }
            // The clone root carries no network component of its own. Add ZNetView first -
            // Piece.Awake binds to it. The m_persistent fix runs after Awake: the stored
            // clone's own ZDO keeps its placeholder value, the field is what every placed
            // instance's ZNetView.Awake will read (placed pieces must survive a world save).
            if (altar.GetComponent<ZNetView>() == null)
            {
                altar.AddComponent<ZNetView>();
            }
            altar.GetComponent<ZNetView>().m_persistent = true;
            if (altar.GetComponent<Piece>() == null)
            {
                altar.AddComponent<Piece>();
            }
            altar.AddComponent<HirdmandrAltarHella>();

            // The build ghost and placement need the piece footprint, which is the root
            // collider. The vanilla bowl clone root has none, so add a BoxCollider that
            // wraps the visible bowl (2 x 1 x 1). The collider height also drives the
            // terrain snap: at 2m the piece floated 1m, at 1m it sits on the ground.
            BoxCollider footprint = altar.GetComponent<BoxCollider>();
            if (footprint == null)
            {
                footprint = altar.AddComponent<BoxCollider>();
                footprint.size = new Vector3(2f, 1f, 1f);
                footprint.center = new Vector3(0f, 0.5f, 0f);
                footprint.isTrigger = false;
            }

            var config = new PieceConfig
            {
                Name = "Altar to Hella",
                Description = "A stone altar to Hella. Offer a soul and its tribute to call the dead home.",
                PieceTable = "_HammerPieceTable",
                Category = "Hirdmandr",
                AllowedInDungeons = false,
                // Icon = GUIManager.Instance.GetSprite("hearth"),
				Icon = RenderManager.Instance.Render(altar, RenderManager.IsometricRotation),
                Requirements = new[]
                {
                    new RequirementConfig { Item = "Stone", Amount = 20, Recover = true },
                    new RequirementConfig { Item = "SurtlingCore", Amount = 2, Recover = true },
                    new RequirementConfig { Item = "AmberPearl", Amount = 2, Recover = true },
                    new RequirementConfig { Item = "Ruby", Amount = 1, Recover = true }
                }
            };
            var piece = new CustomPiece(altar, fixReference: false, config);

            piece.Piece.m_canBeRemoved = true;
            piece.Piece.m_enabled = true;
            piece.Piece.m_randomTarget = true;

            // Stonecutter gate: the same CraftingStation the stonecutter workshop uses (Jotunn's
            // CraftingStations.Stonecutter = "piece_stonecutter"). If it cannot be resolved,
            // log and leave the gate off rather than abort the whole piece.
            GameObject stonecutter = ZNetScene.instance.GetPrefab("piece_stonecutter");
            CraftingStation station = stonecutter != null ? stonecutter.GetComponent<CraftingStation>() : null;
            if (station != null)
            {
                piece.Piece.m_craftingStation = station;
            }
            else
            {
                Jotunn.Logger.LogWarning("[Hirdmandr] CreateAltarHella: 'piece_stonecutter' has no CraftingStation - the Altar to Hella will not be stonecutter-gated.");
            }

            PieceManager.Instance.AddPiece(piece);
            Jotunn.Logger.LogInfo("[Hirdmandr] Altar to Hella piece registered.");
        }

        /*
        /// <summary>
        /// First descendant of <paramref name="root"/> (or the root itself) whose name equals
        /// <paramref name="name"/> (case-insensitive), or null.
        /// </summary>
        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }
            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }
            foreach (Transform child in root)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
        */
    }
}
