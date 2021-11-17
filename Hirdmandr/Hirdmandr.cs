// Hirdmandr
// a Valheim mod skeleton using Jötunn
// 
// File:    Hirdmandr.cs
// Project: Hirdmandr

using BepInEx;
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
            // Jotunn comes with MonoMod Detours enabled for hooking Valheim's code
            // https://github.com/MonoMod/MonoMod
            On.FejdStartup.Awake += FejdStartup_Awake;

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("Hirdmandr has landed");

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html

            PrefabManager.OnVanillaPrefabsAvailable += CreateNPCPieces;
            PrefabManager.OnVanillaPrefabsAvailable += CreateNPCPlayer;

            On.Tutorial.Awake += OnTutorialAwake;
        }

        public void OnTutorialAwake(On.Tutorial.orig_Awake orig, Tutorial self)
        {
            orig(self);

            var tut_adds = new List<string>
            {
                "hirdmandr_find_rescue",
                "hirdmandr_first_rescue",
                "hirdmandr_welcome_home",
                "hirdmandr_thegn",
                "hirdmandr_himthiki",
                "hirdmandr_artisan"
            };
            for (var i = 0; i < self.m_texts.Count; i++)
            {
                if (tut_adds.Contains(self.m_texts[i].m_name))
                {
                    tut_adds.RemoveAt(tut_adds.IndexOf(self.m_texts[i].m_name));
                }
            }

            if (tut_adds.Contains("hirdmandr_find_rescue"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_find_rescue",
                    m_topic = "You are not the first...",
                    m_label = "This is the label",
                    m_text = "Odin's Valkyires have been bringing warriors to Valheim for centuries. Warriors who succeed in the tasks laid out by Odin are " +
                    "carried to Valhalla by the Valkyries. Those who fail lose Odin's blessing and are no longer to be reborn on Valheim when they die, instead " +
                    "they are claimed by Hel and the Valkyries bring them to Helheim upon their death in Valheim.\n\n" +
                    "If you find these cast out warriors and offer them protection: they will join your cause as one of your Hirdmandr and serve you faithfully!"
                };

                self.m_texts.Add(hm_temp);
            }
            
            if (tut_adds.Contains("hirdmandr_first_rescue"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_first_rescue",
                    m_topic = "Becoming a Jarl of Valheim",
                    m_label = "This is the label",
                    m_text = "You have rescued a lost warrior! \n\n" +
                    "You have taken your first step to becoming a mighty Jarl of Valheim! To be a Jarl is to be responsible for the health, happiness, and security of your Hirdmandr. " +
                    "Your first task as a Jarl is to lead this lost warrior to one of your strongholds or camps. You may welcome them to their new home only within range of a " +
                    "workbench and a fire \n\n" +
                    "Whether grand castle or humble shack, your new friend will make their home in that place as a Hirdmandr, loyal to their benevolent Jarl: You."
                };

                self.m_texts.Add(hm_temp);
            }
            
            if (tut_adds.Contains("hirdmandr_welcome_home"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_welcome_home",
                    m_topic = "A Jarl's Hirdmandr",
                    m_label = "This is the label",
                    m_text = "<color=yellow><b>Hugin</b></color> and <color=yellow><b>Munin</b></color> join you in welcoming this lost warrior home! \n\n" +
                    "Take some time to speak [<color=yellow><b>E</b></color>] with your new hirdmandr, and discuss their duties [<color=yellow><b>LShift+E</b></color>] " +
                    "at length. Learn about their personality, a good Jarl assigns duties by each person's stengths! You can also discover their skills, " +
                    "which may help you. Remember: Each of your hirdmandr may be assigned a role of Artisan or Warrior, but not both."
                };

                self.m_texts.Add(hm_temp);
            }
            
            if (tut_adds.Contains("hirdmandr_thegn"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_thegn",
                    m_topic = "Your Garrison and Militia",
                    m_label = "This is the label",
                    m_text = "Thegn are young warriors, full of drive but lacking in experience. Warriors assigned to the Thegn duty will remain " + 
                    "in your stronghold and protect your hirdmandr from the threats of wandering creatures and attacks by the forsaken. Once the " + 
                    "mod author figures out how to do patrols, there will be a note about it here!"
                };

                self.m_texts.Add(hm_temp);
            }

            if (tut_adds.Contains("hirdmandr_himthiki"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_himthiki",
                    m_topic = "A Jarl's Elite Warriors",
                    m_label = "This is the label",
                    m_text = "A Himthiki is an elite warrior! They have been the backbone of every Jarl's royal guard for centuries. At your command, " +
                    "your Himthiki will follow you out of your stronghold and into glorious battle. Take care with their trust! Valheim " +
                    "is dangerous, and a Himthiki who dies is not reborn. Himthiki with the Gatherer job enabled will pick up useful things on your " + 
                    "journeys, everything from stone to cloudberries."
                };

                self.m_texts.Add(hm_temp);
            }

            if (tut_adds.Contains("hirdmandr_artisan"))
            {
                Tutorial.TutorialText hm_temp = new Tutorial.TutorialText()
                {
                    m_name = "hirdmandr_artisan",
                    m_topic = "Artisans and Laborers",
                    m_label = "This is the label",
                    m_text = "While any hirdmandr can learn any skill, many hirdmandr are best suited to the quiet, industrious life of an Artisan. " +
                    "You may give any number of artisan duties to each hirdmandr, but they always prefer the jobs they are best at. Each artisan " +
                    "requires a workspace before they can work. A Wood Burner, for example, requires an NPC Chest (any kind) close to a Kiln."
                };

                self.m_texts.Add(hm_temp);
            }

        }

        private void FejdStartup_Awake(On.FejdStartup.orig_Awake orig, FejdStartup self)
        {
            // This code runs before Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup is going to awake");

            // Call this method so the original game method is invoked
            orig(self);

            // This code runs after Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup has awoken");
        }

        // Implementation of cloned asset
        //private void CreateNPCChest()
        //{
        //    GameObject chestPrefab = BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintTestTable");
        //    CustomPieceTable CPT = new CustomPieceTable(chestPrefab);
        //    PieceManager.Instance.AddPieceTable(CPT);
        //}

        private void CreateNPCPieces()
        {
            // Create an instance of GUIManager to go get vanilla sprites
            var gm = new GUIManager();

            // Create NPC Chest first
            if (!PrefabManager.Instance.GetPrefab("piece_npc_chest"))
            {
                var NPCChestPrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_chest", "piece_chest_wood");
                NPCChestPrefab.AddComponent<Piece>();
                var NPCChestContainer = NPCChestPrefab.GetComponent<Container>();
                NPCChestContainer.m_name = "NPC Chest";

                var NPCChestPiece = new CustomPiece(NPCChestPrefab, fixReference: false,
                    new PieceConfig
                    {
                        Name = "NPC Chest",
                        Description = "Testing Descriptions",
                        PieceTable = "_HammerPieceTable",
                        Category = "Hirdmandr",
                        AllowedInDungeons = false,
                        Icon = gm.GetSprite("chest_wood"),
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

            // Add NPC Campfire
            if (!PrefabManager.Instance.GetPrefab("piece_npc_fire_pit"))
            {
                var NPCCampfirePrefab = PrefabManager.Instance.CreateClonedPrefab("piece_npc_fire_pit", "fire_pit");
                NPCCampfirePrefab.AddComponent<Piece>();
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
                        Icon = gm.GetSprite("Campfire"),
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
                NPCHearthPrefab.AddComponent<Piece>();
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
                        Icon = gm.GetSprite("hearth"),
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


                Character NPCCharComp = NPCPlayerPrefab.GetComponent<Character>();
                NPCCharComp.m_name = "Human";
                NPCCharComp.m_health = 60f;
                NPCCharComp.m_faction = Character.Faction.Players;

                NPCPlayerPrefab.AddComponent<HirdmandrNPC>();
                NPCPlayerPrefab.AddComponent<HirdmandrGUI>();
                NPCPlayerPrefab.AddComponent<HirdmandrGUIRescue>();

                // NPCPlayerPrefab.AddComponent<HMInteract>();

                NPCPlayerPrefab.SetActive(true);

                PrefabManager.Instance.AddPrefab(NPCPlayerPrefab);
            }
        }
    }
}
