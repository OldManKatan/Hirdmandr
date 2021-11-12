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
    [Serializable]
    public class HirdmandrNPC : MonoBehaviour, Interactable
    {
        public Humanoid m_humanoid;
        public Character m_character;
        public MonsterAI m_monsterai;
        public VisEquipment m_visequip;
        public HirdmandrGUIRescue m_guirescuecomp;
        public ZNetView m_znet;
        public Humanoid m_user;
        public bool m_female;

        // Rescuing
        public bool m_isRescued;
        public bool m_isHirdmandr;
        public float m_rescueRange = 100f;

        // Mental State
        public float m_mentalcontentment;
        public float m_mentalstress;

        // Personality
        public float m_valueswork;
        public float m_valuesrelationships;
        public float m_valuesvalor;
        public float m_valueslearning;
        public float m_valuesauthority;
        public float m_valuescomfort;
        public float m_valuesstrength;
        public float m_valuescompassion;

        // Skills
        public float m_skillwoodburner;
        public float m_skillfurnaceoperator;
        public float m_skillfarmer;
        public float m_skillcook;
        public float m_skillbaker;
        public float m_skillmelee;
        public float m_skillrange;
        public float m_skillfighter;
        public float m_skillgatherer;

        // Pretty
        public LookAt m_lookAt;

        // Combat
        public float m_healTick = 2f;
        public EffectList m_rescueEffect = new EffectList();


        protected virtual void Awake()
        {
            Jotunn.Logger.LogInfo("An NPC Awoke");

            m_humanoid = GetComponent<Humanoid>();
            m_character = GetComponent<Character>();
            m_monsterai = GetComponent<MonsterAI>();
            m_visequip = GetComponent<VisEquipment>();
            m_guirescuecomp = GetComponent<HirdmandrGUIRescue>();
            m_znet = GetComponent<ZNetView>();

            On.Character.GetHoverText += OnGetHoverText;

            SetupNPC();

            var npc_name = m_znet.GetZDO().GetString("hmnpc_name");
            if (npc_name == "")
            {
                RandomizeAppearance();
                RandomizePersonality();
                RandomizeSkills();

                m_isRescued = false;
                m_isHirdmandr = false;
                InvokeRepeating("RandomTalkRescue", 30f, 30f);
            }
            else
            {
                ZDOtoAppearance();
                ZDOtoPersonality();
                ZDOtoSkills();
            }

            PopulateCombatProps();
            InvokeRepeating("HealIfHurt", 5f, 5f);

            if (!m_isRescued)
            {
                Sit();
            }

            m_rescueEffect.m_effectPrefabs = new EffectList.EffectData[1];
            m_rescueEffect.m_effectPrefabs[0] = new EffectList.EffectData
            {
                m_prefab = PrefabManager.Instance.GetPrefab("vfx_boar_love"),
                m_enabled = true,
                m_attach = false,
                m_inheritParentRotation = true,
                m_inheritParentScale = true,
                m_randomRotation = false,
                m_scale = true
            };
        }

        public void Update()
        {
            Player closestPlayer = Player.GetClosestPlayer(transform.position, 15);
            if ((bool)closestPlayer)
            {
                // if (!m_monsterai.IsAlerted())
                // {
                //     m_character.SetLookDir(closestPlayer.GetEyePoint());
                // }
                if (!m_isRescued && closestPlayer == Player.m_localPlayer)
                {
                    Player.m_localPlayer?.ShowTutorial("hirdmandr_find_rescue");
                }
            }
        }

        public void LookAt(Player closestPlayer)
        {
            m_character.m_eye.LookAt(closestPlayer.GetHeadPoint());
        }

        private static string OnGetHoverText(On.Character.orig_GetHoverText orig, Character self)
        {
            if (self.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                return HirdmandrComp.GetHoverText();
            }
            return orig(self);
        }

        public string GetHoverText()
        {
            if (!m_isHirdmandr && !m_isRescued)
            {
                return m_humanoid.m_name + "\n[<color=yellow><b>E</b></color>] Rescue";
            }
            else if (!m_isHirdmandr && m_isRescued)
            {
                return m_humanoid.m_name + "\n[<color=yellow><b>E</b></color>] Talk";
            }
            else
            {
                return m_humanoid.m_name + "\n[<color=yellow><b>E</b></color>] Talk\n[<color=yellow><b>LShift+E</b></color>] Manage";
            }
        }

        private static string OnGetHoverName(On.Character.orig_GetHoverName orig, Character self)
        {
            if (self.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                return HirdmandrComp.GetHoverText();
            }
            return orig(self);
        }

        public string GetHoverName()
        {
            return m_humanoid.m_name;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
            {
                return false;
            }
            Jotunn.Logger.LogInfo("Interact happened!");
            m_user = user;
            if (!m_isHirdmandr)
            {
                ZInput.ResetButtonStatus("Inventory");
                ZInput.ResetButtonStatus("JoyButtonB");
                ZInput.ResetButtonStatus("JoyButtonY");
                ZInput.ResetButtonStatus("Use");

                m_guirescuecomp.TogglePanel();
            }
            PopulateCombatProps();
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        private void SetupNPC()
        {

            // component.m_backShield = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackShield_attach").GetComponent<Transform>();
            // component.m_backMelee = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackOneHanded_attach").GetComponent<Transform>();
            // component.m_backTwohandedMelee = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackTwohanded_attach").GetComponent<Transform>();
            // component.m_backBow = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackBow_attach").GetComponent<Transform>();
            // component.m_backTool = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/BackTool_attach").GetComponent<Transform>();
            // component.m_backAtgeir = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackAtgeir_attach").GetComponent<Transform>();

            Humanoid NPCHumComp = GetComponent<Humanoid>();
            NPCHumComp.m_eye = transform.Find("EyePos");
            List<EffectList.EffectData> list = new List<EffectList.EffectData>
            {
                new EffectList.EffectData
                {
                    m_prefab = ZNetScene.instance.GetPrefab("vfx_player_death")
                }
            };
            bool alreadyExisted = false;
            EffectList.EffectData effectData = new EffectList.EffectData
            {
                m_prefab = PrefabManager.Instance.GetPrefab("Player_ragdoll")
            };
            list.Add(effectData);
            if (!alreadyExisted)
            {
                Ragdoll NPCHumComp3 = effectData.m_prefab.GetComponent<Ragdoll>();
                NPCHumComp3.m_ttl = 2f;
                NPCHumComp3.m_removeEffect = Utils.CloneEffectList(ZNetScene.instance.GetPrefab("Greydwarf_ragdoll").GetComponent<Ragdoll>().m_removeEffect);
                effectData.m_prefab.GetComponent<VisEquipment>().m_isPlayer = true;
            }

            var NPC_sfx_death = PrefabManager.Instance.GetPrefab("RRR_NPC_sfx_death");
            if (!NPC_sfx_death)
            {
                NPC_sfx_death = Utils.DesignSfxDeepen("sfx_goblin_death", "RRR_NPC_sfx_death");
            }
            list.Add(new EffectList.EffectData
            {
                m_prefab = NPC_sfx_death
            });
            NPCHumComp.m_deathEffects = new EffectList
            {
                m_effectPrefabs = list.ToArray()
            };

            NPCHumComp.m_unarmedWeapon = PrefabManager.Instance.GetPrefab("PlayerUnarmed").GetComponent<ItemDrop>();

            Character NPCCharComp = GetComponent<Character>();
            NPCCharComp.m_animator.SetBool("Stand", value: false);

            CharacterDrop NPCCharDropComp = GetComponent<CharacterDrop>();
            NPCCharDropComp.m_drops = new List<CharacterDrop.Drop>();

            m_znet.m_persistent = true;
            m_znet.m_ghost = true;

            m_monsterai.m_idleSound = new EffectList();
            m_monsterai.enabled = false;
            m_monsterai.m_attackPlayerObjects = false;
            m_monsterai.m_viewRange = 15;
            m_monsterai.m_hearRange = 15;
            m_monsterai.m_maxChaseDistance = 10;

        }

        private void RandomizeAppearance()
        {
            var randSex = UnityEngine.Random.Range(0, 2);
            if (randSex == 1)
            {
                m_female = true;
                m_znet.GetZDO().Set("hmnpc_female", true);
            }
            else
            {
                m_female = false;
                m_znet.GetZDO().Set("hmnpc_female", false);
            }

            if (!m_female)
            {
                var RandNames = new List<string> {
                    "Arne",
                    "Birger",
                    "Bjorn",
                    "Bo",
                    "Erik",
                    "Frode",
                    "Gorm",
                    "Halfdan",
                    "Harald",
                    "Knud",
                    "Kare",
                    "Leif",
                    "Njal",
                    "Roar",
                    "Rune",
                    "Sten",
                    "Skarde",
                    "Sune",
                    "Svend",
                    "Troels",
                    "Toke",
                    "Torsten",
                    "Trygve",
                    "Ulf",
                    "Odger",
                    "Age"
                };
                m_humanoid.m_name = RandNames[UnityEngine.Random.Range(0, RandNames.Count)];
            }
            else
            {
                // var skinMesh = transform.Find("Visual/body").GetComponent<SkinnedMeshRenderer>();
                // Jotunn.Logger.LogInfo(string.Format("skinMesh is: {0}", skinMesh));
                // skinMesh.sharedMesh = Resources.Load<Mesh>("bodyfem");
                // Jotunn.Logger.LogInfo(string.Format("Resources.Load<Mesh>: {0}", Resources.Load<Mesh>("bodyfem")));
                m_visequip.SetModel(1);
                var RandNames = new List<string> {
                    "Astrid",
                    "Bodil",
                    "Frida",
                    "Gertrud",
                    "Gro",
                    "Estrid",
                    "Hilda",
                    "Gudrun",
                    "Gunhild",
                    "Helga",
                    "Inga",
                    "Liv",
                    "Randi",
                    "Signe",
                    "Sigrid",
                    "Revna",
                    "Sif",
                    "Tora",
                    "Tove",
                    "Thyra",
                    "Thurid",
                    "Yrsa",
                    "Ulfhild",
                    "Ase"
                };
                m_humanoid.m_name = RandNames[UnityEngine.Random.Range(0, RandNames.Count)];
            }
            m_znet.GetZDO().Set("hmnpc_name", m_humanoid.m_name);

            var BeardItems = new List<string> { "BeardNone" };
            if (!m_female)
            {
                for (int i = 1; i <= 10; i++)
                {
                    BeardItems.Add($"Beard{i}");
                }
            }
            var HairItems = new List<string> { "HairNone" };
            for (int j = 1; j <= 14; j++)
            {
                HairItems.Add($"Hair{j}");
            }

            var hairBlondness = UnityEngine.Random.Range(0.1f, 1f);
            var hairColor1 = UnityEngine.Random.Range(0.48f, 0.93f);
            var hairColor2 = hairColor1 * 0.75f;
            var hairColorVec3 = new Vector3(hairBlondness, hairBlondness * hairColor1, hairBlondness * hairColor2);
            m_visequip.SetHairColor(hairColorVec3);
            m_znet.GetZDO().Set("hmnpc_haircolor", hairColorVec3);

            var base_color = UnityEngine.Random.Range(0.3f, 1f);
            var skinColor = new Vector3(base_color, base_color, base_color);
            m_visequip.SetSkinColor(new Vector3(base_color, base_color, base_color));
            m_znet.GetZDO().Set("hmnpc_skincolor", skinColor);

            var ThisBeard = BeardItems[UnityEngine.Random.Range(0, BeardItems.Count)];
            m_humanoid.SetBeard(ThisBeard);
            m_visequip.SetBeardItem(ThisBeard);
            m_znet.GetZDO().Set("hmnpc_beard", ThisBeard);

            var ThisHair = HairItems[UnityEngine.Random.Range(0, HairItems.Count)];
            m_humanoid.SetHair(ThisHair);
            m_visequip.SetHairItem(ThisHair);
            m_znet.GetZDO().Set("hmnpc_hair", ThisHair);

            string chest_string = "";
            int chestRand = UnityEngine.Random.Range(0, 4);
            if (chestRand == 1) { chest_string = "ArmorRagsChest"; }
            else if (chestRand > 2) { chest_string = "ArmorLeatherChest"; }

            string legs_string = "";
            int legsRand = UnityEngine.Random.Range(0, 4);
            if (legsRand == 1) { legs_string = "ArmorRagsLegs"; }
            else if (legsRand > 2) { legs_string = "ArmorLeatherLegs"; }

            string weapon_string = "";
            int wpnRand = UnityEngine.Random.Range(0, 5);
            bool GiveShield = true;
            if (wpnRand == 1) { weapon_string = "Club"; }
            else if (wpnRand == 2) { weapon_string = "SpearFlint"; }
            else if (wpnRand == 3) { weapon_string = "KnifeFlint"; }
            else if (wpnRand == 4) { weapon_string = "AxeFlint"; }

            string shield_string = "";
            if (GiveShield)
            {
                shield_string = "ShieldWood";
            }

            if (chest_string != "")
            {
                m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab(chest_string));
            }
            m_znet.GetZDO().Set("hmnpc_chest_string", chest_string);

            if (legs_string != "")
            {
                m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab(legs_string));
            }
            m_znet.GetZDO().Set("hmnpc_legs_string", legs_string);

            if (weapon_string != "")
            {
                m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab(weapon_string));
            }
            m_znet.GetZDO().Set("hmnpc_weapon_string", weapon_string);

            if (shield_string != "")
            {
                m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab(shield_string));
            }
            m_znet.GetZDO().Set("hmnpc_shield_string", shield_string);


            m_monsterai.SelectBestAttack(m_humanoid, 3.0f);
            // m_humanoid.SetupEquipment();
            // m_humanoid.SetupVisEquipment(m_visequip, false);
            m_znet.GetZDO().Set("hmnpc_isrescued", false);
        }

        public List<float> GetPValuesList()
        {
            return new List<float> {
                    m_valueswork,
                    m_valuesrelationships,
                    m_valuesvalor,
                    m_valueslearning,
                    m_valuesauthority,
                    m_valuescomfort,
                    m_valuesstrength,
                    m_valuescompassion
                };
        }

        public string GetHighestPValue()
        {
            var highest_value_str = "";
            var value_str = new List<string>
            {
                "valueswork", "valuesrelationships", "valuesvalor", "valueslearning", "valuesauthority", "valuescomfort", "valuesstrength", "valuescompassion"
            };

            float highest = -2f;
            var value_values = GetPValuesList();
            for (var i = 0; i < value_values.Count; i++)
            {
                if (value_values[i] > highest)
                {
                    highest = value_values[i];
                    highest_value_str = value_str[i];
                }
            }

            return highest_value_str;
        }

        public void ZDOtoAppearance()
        {
            m_female = m_znet.GetZDO().GetBool("hmnpc_female");
            m_humanoid.m_name = m_znet.GetZDO().GetString("hmnpc_name");

            Vector3 hairColorVec3 = m_znet.GetZDO().GetVec3("hmnpc_haircolor", new Vector3(1f, 1f, 1f));
            m_visequip.SetHairColor(hairColorVec3);

            Vector3 skinColor = m_znet.GetZDO().GetVec3("hmnpc_skincolor", new Vector3(1f, 1f, 1f));
            m_visequip.SetSkinColor(skinColor);

            string ThisBeard = m_znet.GetZDO().GetString("hmnpc_beard");
            m_humanoid.SetBeard(ThisBeard);
            m_visequip.SetBeardItem(ThisBeard);

            string ThisHair = m_znet.GetZDO().GetString("hmnpc_hair");
            m_humanoid.SetHair(ThisHair);
            m_visequip.SetHairItem(ThisHair);

            string chest_string = m_znet.GetZDO().GetString("hmnpc_chest_string");
            if (chest_string != "")
            {
                m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab(chest_string));
            }

            string legs_string = m_znet.GetZDO().GetString("hmnpc_legs_string");
            if (legs_string != "")
            {
                m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab(legs_string));
            }

            string weapon_string = m_znet.GetZDO().GetString("hmnpc_weapon_string");
            if (weapon_string != "")
            {
                m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab(weapon_string));
            }

            string shield_string = m_znet.GetZDO().GetString("hmnpc_shield_string");
            if (shield_string != "")
            {
                m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab(shield_string));
            }

            m_isRescued = m_znet.GetZDO().GetBool("hmnpc_isrescued");

        }

        public void RandomizePersonality()
        {
            m_mentalcontentment = 0f;
            m_mentalstress = 0f;

            for (int i = 0; i < 5; i++)
            {
                m_valueswork = UnityEngine.Random.Range(-1f, 1f);
                m_valuesrelationships = UnityEngine.Random.Range(-1f, 1f);
                m_valuesvalor = UnityEngine.Random.Range(-1f, 1f);
                m_valueslearning = UnityEngine.Random.Range(-1f, 1f);
                m_valuesauthority = UnityEngine.Random.Range(-1f, 1f);
                m_valuescomfort = UnityEngine.Random.Range(-1f, 1f);
                m_valuesstrength = UnityEngine.Random.Range(-1f, 1f);
                m_valuescompassion = UnityEngine.Random.Range(-1f, 1f);

                List<float> all_values = GetPValuesList();

                int num_positive = 0;
                for (int j = 0; j < all_values.Count; j++)
                {
                    if (all_values[j] >= 0f)
                    {
                        num_positive++;
                    }
                }
                if (num_positive >= (all_values.Count / 2))
                {
                    break;
                }
                ZDOSavePersonality();
            }
        }

        public void ZDOtoPersonality()
        {
            m_mentalcontentment = m_znet.GetZDO().GetFloat("hmnpc_mentalcontentment");
            m_mentalstress = m_znet.GetZDO().GetFloat("hmnpc_mentalstress");
            m_valueswork = m_znet.GetZDO().GetFloat("hmnpc_valueswork");
            m_valuesrelationships = m_znet.GetZDO().GetFloat("hmnpc_valuesrelationships");
            m_valuesvalor = m_znet.GetZDO().GetFloat("hmnpc_valuesvalor");
            m_valueslearning = m_znet.GetZDO().GetFloat("hmnpc_valueslearning");
            m_valuesauthority = m_znet.GetZDO().GetFloat("hmnpc_valuesauthority");
            m_valuescomfort = m_znet.GetZDO().GetFloat("hmnpc_valuescomfort");
            m_valuesstrength = m_znet.GetZDO().GetFloat("hmnpc_valuesstrength");
            m_valuescompassion = m_znet.GetZDO().GetFloat("hmnpc_valuescompassion");
        }

        public void ZDOSavePersonality()
        {
            // ZDOs
            m_znet.GetZDO().Set("hmnpc_mentalcontentment", m_mentalcontentment);
            m_znet.GetZDO().Set("hmnpc_mentalstress", m_mentalstress);
            m_znet.GetZDO().Set("hmnpc_valueswork", m_valueswork);
            m_znet.GetZDO().Set("hmnpc_valuesrelationships", m_valuesrelationships);
            m_znet.GetZDO().Set("hmnpc_valuesvalor", m_valuesvalor);
            m_znet.GetZDO().Set("hmnpc_valueslearning", m_valueslearning);
            m_znet.GetZDO().Set("hmnpc_valuesauthority", m_valuesauthority);
            m_znet.GetZDO().Set("hmnpc_valuescomfort", m_valuescomfort);
            m_znet.GetZDO().Set("hmnpc_valuesstrength", m_valuesstrength);
            m_znet.GetZDO().Set("hmnpc_valuescompassion", m_valuescompassion);
        }

        public List<float> GetSkillsList()
        {
            return new List<float> {
                m_skillwoodburner,
                m_skillfurnaceoperator,
                m_skillfarmer,
                m_skillcook,
                m_skillbaker,
                m_skillmelee,
                m_skillrange,
                m_skillfighter,
                m_skillgatherer
            };
        }

        public void RandomizeSkills()
        {
            Jotunn.Logger.LogInfo(string.Format("RandomizeSkills started..."));
            m_skillwoodburner = 0f;
            m_skillfurnaceoperator = 0f;
            m_skillfarmer = 0f;
            m_skillcook = 0f;
            m_skillbaker = 0f;
            m_skillmelee = 0f;
            m_skillrange = 0f;
            m_skillfighter = 0f;
            m_skillgatherer = 0f;

            var all_skill_str = new List<string> {
                "woodburner",
                "furnaceoperator",
                "farmer",
                "cook",
                "baker",
                "melee",
                "range",
                "fighter",
                "gatherer"
            };

            var floats_to_assign = new List<float>
            {
                10f,
                5f,
                5f
            };

            int skill_str_index;
            for (var i = 0; i < floats_to_assign.Count; i++)
            {
                skill_str_index = UnityEngine.Random.Range(0, all_skill_str.Count);
                ModifySkill(all_skill_str[skill_str_index], floats_to_assign[i]);
                Jotunn.Logger.LogInfo(string.Format("RandomizeSkills assigned {0} to skill {1}", floats_to_assign[i], all_skill_str[skill_str_index]));
                all_skill_str.RemoveAt(skill_str_index);
            }
            ZDOSaveSkills();
        }

        public void ZDOtoSkills()
        {
            m_skillwoodburner = m_znet.GetZDO().GetFloat("hmnpc_skillwoodburner");
            m_skillfurnaceoperator = m_znet.GetZDO().GetFloat("hmnpc_skillfurnaceoperator");
            m_skillfarmer = m_znet.GetZDO().GetFloat("hmnpc_skillfarmer");
            m_skillcook = m_znet.GetZDO().GetFloat("hmnpc_skillcook");
            m_skillbaker = m_znet.GetZDO().GetFloat("hmnpc_skillbaker");
            m_skillmelee = m_znet.GetZDO().GetFloat("hmnpc_skillmelee");
            m_skillrange = m_znet.GetZDO().GetFloat("hmnpc_skillrange");
            m_skillfighter = m_znet.GetZDO().GetFloat("hmnpc_skillfighter");
            m_skillgatherer = m_znet.GetZDO().GetFloat("hmnpc_skillgatherer");

        }

        public void ZDOSaveSkills()
        {
            // ZDOs
            m_znet.GetZDO().Set("hmnpc_skillwoodburner", m_skillwoodburner);
            m_znet.GetZDO().Set("hmnpc_skillfurnaceoperator", m_skillfurnaceoperator);
            m_znet.GetZDO().Set("hmnpc_skillfarmer", m_skillfarmer);
            m_znet.GetZDO().Set("hmnpc_skillcook", m_skillcook);
            m_znet.GetZDO().Set("hmnpc_skillbaker", m_skillbaker);
            m_znet.GetZDO().Set("hmnpc_skillmelee", m_skillmelee);
            m_znet.GetZDO().Set("hmnpc_skillrange", m_skillrange);
            m_znet.GetZDO().Set("hmnpc_skillfighter", m_skillfighter);
            m_znet.GetZDO().Set("hmnpc_skillgatherer", m_skillgatherer);
        }

        public string GetHighestSkill()
        {
            var highest_skill_str = "";
            var skill_str = new List<string>
            {
                "skillwoodburner", "skillfurnaceoperator", "skillfarmer", "skillcook", "skillbaker", "skillmelee", "skillrange", "skillfighter", "skillgatherer"
            };

            float highest = -1f;
            var skill_values = GetSkillsList();
            for (var i = 0; i < skill_values.Count; i++)
            {
                if (skill_values[i] > highest)
                {
                    highest = skill_values[i];
                    highest_skill_str = skill_str[i];
                }
            }

            return highest_skill_str;
        }

        public void ModifySkill(string skill_str, float change)
        {
            if (skill_str == "skillwoodburner" || skill_str == "woodburner") { m_skillwoodburner += change; }
            else if (skill_str == "skillfurnaceoperator" || skill_str == "furnaceoperator") { m_skillfurnaceoperator += change; }
            else if (skill_str == "skillfarmer" || skill_str == "farmer") { m_skillfarmer += change; }
            else if (skill_str == "skillcook" || skill_str == "cook") { m_skillcook += change; }
            else if (skill_str == "skillbaker" || skill_str == "baker") { m_skillbaker += change; }
            else if (skill_str == "skillmelee" || skill_str == "melee") { m_skillmelee += change; }
            else if (skill_str == "skillrange" || skill_str == "range") { m_skillrange += change; }
            else if (skill_str == "skillfighter" || skill_str == "fighter") { m_skillfighter += change; }
            else if (skill_str == "skillgatherer" || skill_str == "gatherer") { m_skillgatherer += change; }
            BoundSkills();
        }

        public void BoundSkills()
        {
            // woodburner
            if (m_skillwoodburner < 0) { m_skillwoodburner = 0; }
            else if (m_skillwoodburner > 100) { m_skillwoodburner = 0; }
            // furnaceoperator
            if (m_skillfurnaceoperator < 0) { m_skillfurnaceoperator = 0; }
            else if (m_skillfurnaceoperator > 100) { m_skillfurnaceoperator = 0; }
            // farmer
            if (m_skillfarmer < 0) { m_skillfarmer = 0; }
            else if (m_skillfarmer > 100) { m_skillfarmer = 0; }
            // cook
            if (m_skillcook < 0) { m_skillcook = 0; }
            else if (m_skillcook > 100) { m_skillcook = 0; }
            // baker
            if (m_skillbaker < 0) { m_skillbaker = 0; }
            else if (m_skillbaker > 100) { m_skillbaker = 0; }
            // melee
            if (m_skillmelee < 0) { m_skillmelee = 0; }
            else if (m_skillmelee > 100) { m_skillmelee = 0; }
            // range
            if (m_skillrange < 0) { m_skillrange = 0; }
            else if (m_skillrange > 100) { m_skillrange = 0; }
            // fighter
            if (m_skillfighter < 0) { m_skillfighter = 0; }
            else if (m_skillfighter > 100) { m_skillfighter = 0; }
            // gatherer
            if (m_skillgatherer < 0) { m_skillgatherer = 0; }
            else if (m_skillgatherer > 100) { m_skillgatherer = 0; }

            ZDOSaveSkills();
        }

        public void RandomTalkRescue()
        {
            if (Player.IsPlayerInRange(base.transform.position, m_rescueRange))
            {
                Chat.instance.SetNpcText(base.gameObject, Vector3.up * 1.5f, 20f, 5f, "", GetRandomRescueTalk(), large: false);
            }
        }

        public string GetRandomRescueTalk()
        {
            var RandomRescueTalk = new List<string>()
            {
                "Is someone there?",
                "What's happening now?!",
                "I hope that's not a troll...",
                "Who's there? Show yourself!",
                "Are you a friend?",
                "Please, not more monsters..."
            };

            return RandomRescueTalk[UnityEngine.Random.Range(0, RandomRescueTalk.Count)];
        }

        public string GetRescueText()
        {
            var abandon = new List<string>()
            {
                "I failed Odin, and he abandoned me. ",
                "Odin has forsaken me, if I die here I will never reach Valhalla! ",
                "I was not brave enough to reach Valhalla, now I feel Hella reaching for me. ",
                "I am without God or Lord. "
            };

            var rescueme = new List<string>()
            {
                "Please, protect me!",
                "Keep me from death, and I shall call you lord.",
                "Please don't let Hella take me.",
                "Have ye a stronghold?"
            };

            return abandon[UnityEngine.Random.Range(0, abandon.Count)] + rescueme[UnityEngine.Random.Range(0, abandon.Count)];
        }

        public void PopulateCombatProps()
        {
            m_healTick = 2f + (m_skillfighter / 3);
            // m_humanoid.m_currentAttack.m_damageMultiplier = 1f + (m_skillmelee / 50);
        }

        public void HealIfHurt()
        {
            if (m_humanoid.GetHealth() < m_humanoid.GetMaxHealth())
            {
                m_humanoid.Heal(m_healTick, true);
            }
        }

        public void Sit()
        {
            StartEmote("sit", oneshot: false);
        }

        public void Stand()
        {
            StopEmote();
        }


        public bool StartEmote(string emote, bool oneshot = true)
        {
            int @int = m_znet.GetZDO().GetInt("emoteID");
            m_znet.GetZDO().Set("emoteID", @int + 1);
            m_znet.GetZDO().Set("emote", emote);
            m_znet.GetZDO().Set("emote_oneshot", oneshot);
            return true;
        }

        public void StopEmote()
        {
            if (m_znet.GetZDO().GetString("emote") != "")
            {
                int @int = m_znet.GetZDO().GetInt("emoteID");
                m_znet.GetZDO().Set("emoteID", @int + 1);
                m_znet.GetZDO().Set("emote", "");
            }
        }

        public void Rescue()
        {
            Jotunn.Logger.LogInfo("Rescue was hit on " + m_humanoid.m_name);
            var NPCZNetView = GetComponent<ZNetView>();
            NPCZNetView.m_ghost = false;
            m_monsterai.enabled = true;
            // m_monsterai.arroundPointTarget = Vector3.zero;
            m_monsterai.ResetPatrolPoint();
            m_monsterai.SetFollowTarget(m_user.gameObject);
            m_guirescuecomp.TogglePanel();
            if (!m_isRescued)
            {
                m_rescueEffect.Create(base.transform.position, base.transform.rotation);
                m_user.GetComponent<Player>().ShowTutorial("hirdmandr_first_rescue");
            }
            m_isRescued = true;
            m_znet.GetZDO().Set("hmnpc_isrescued", true);
            CancelInvoke("RandomTalkRescue");
        }
        public void RescueWait()
        {
            Jotunn.Logger.LogInfo("RescueWait was hit on " + m_humanoid.m_name);
            m_monsterai.SetFollowTarget(null);
            m_monsterai.SetPatrolPoint();
            // m_monsterai.SetFollowTarget(null);
            // m_monsterai.m_spawnPoint = GetComponent<Transform>().position;
            m_guirescuecomp.TogglePanel();
        }

        public void WelcomeHome()
        {
            Jotunn.Logger.LogInfo("WelcomeHome was hit on " + m_humanoid.m_name);
        }

    }
}
