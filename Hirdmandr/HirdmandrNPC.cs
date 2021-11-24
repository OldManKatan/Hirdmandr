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
        public HirdmandrAI m_hirdmandrAI;
        public VisEquipment m_visequip;
        public HirdmandrGUI m_guiHirdmandr;
        public HirdmandrGUIRescue m_guirescuecomp;
        public ZNetView m_znet;

        public Humanoid m_user;

        // Rescuing
        public bool m_isRescued;
        public bool m_isHirdmandr;
        public float m_rescueRange = 100f;
        public long m_jarlZOID;

        // Mental State
        public float m_mentalcontentment;
        public float m_mentalstress;

        // Personality
        public HMPersonality m_personality = new HMPersonality();

        // Skills
        public HMSkills m_skills = new HMSkills();

        // Visual
        public bool m_female;
        public LookAt m_lookAt;
        public EffectList m_rescueEffect = new EffectList();
        public EffectList m_welcomeEffect = new EffectList();

        // Combat
        public float m_healTick = 2f;

        // Jobs
        public bool m_roleArtisan = true;
        public bool m_roleWarrior = false;
        public bool m_jobThegn = false;
        public bool m_thegnDayshift = true;
        public bool m_jobHimthiki = false;
        public bool m_fightingStyleDefense = true;
        public bool m_fightingStyleOffense = false;
        public bool m_jobGatherer = false;
        public bool m_fightingRangeClose = true;
        public bool m_fightingRangeMid = false;
        public bool m_fightingRangeFar = false;

        // Bed
        public ZDOID ownedBedZDOID = ZDOID.None;

        protected virtual void Awake()
        {
            Jotunn.Logger.LogInfo("An NPC Awoke");

            m_humanoid = GetComponent<Humanoid>();
            m_character = GetComponent<Character>();
            m_monsterai = GetComponent<MonsterAI>();
            m_hirdmandrAI = GetComponent<HirdmandrAI>();
            m_visequip = GetComponent<VisEquipment>();
            m_guiHirdmandr = GetComponent<HirdmandrGUI>();
            m_guirescuecomp = GetComponent<HirdmandrGUIRescue>();
            m_znet = GetComponent<ZNetView>();
            if (m_znet is null)
            {
                Jotunn.Logger.LogInfo("m_znet is null!");
                gameObject.AddComponent<ZNetView>();
                m_znet = GetComponent<ZNetView>();
            }
            m_humanoid.m_nview = m_znet;
            m_character.m_nview = m_znet;
            m_skills.m_znetv = m_znet;
            m_personality.m_znetv = m_znet;

            On.Character.GetHoverText += OnGetHoverText;
            On.Character.GetHoverName += OnGetHoverName;
            On.Character.RPC_Damage += OnChar_RPC_Damage;

            SetupNPC();

            m_rescueEffect.m_effectPrefabs = new EffectList.EffectData[2];
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
            m_rescueEffect.m_effectPrefabs[1] = new EffectList.EffectData
            {
                m_prefab = PrefabManager.Instance.GetPrefab("sfx_boar_love"),
                m_enabled = true,
                m_attach = false,
                m_inheritParentRotation = true,
                m_inheritParentScale = true,
                m_randomRotation = false,
                m_scale = true
            };

            m_welcomeEffect.m_effectPrefabs = new EffectList.EffectData[2];
            m_welcomeEffect.m_effectPrefabs[0] = new EffectList.EffectData
            {
                m_prefab = PrefabManager.Instance.GetPrefab("vfx_odin_despawn"),
                m_enabled = true,
                m_attach = false,
                m_inheritParentRotation = true,
                m_inheritParentScale = true,
                m_randomRotation = false,
                m_scale = true
            };
            m_welcomeEffect.m_effectPrefabs[1] = new EffectList.EffectData
            {
                m_prefab = PrefabManager.Instance.GetPrefab("sfx_cooking_station_done"),
                m_enabled = true,
                m_attach = false,
                m_inheritParentRotation = true,
                m_inheritParentScale = true,
                m_randomRotation = false,
                m_scale = true
            };

            if (m_znet.IsOwner())
            {
                var npc_name = m_znet.GetZDO().GetString("hmnpc_name");
                if (npc_name == "")
                {
                    m_monsterai.enabled = false;

                    RandomizeAppearance();

                    m_skills.LoadSkills();
                    var all_skill_str = new List<string> { };
                    foreach (HMSkills.SkillData skill_data in m_skills.m_hmSkills)
                    {
                        all_skill_str.Add(skill_data.m_name);
                    }
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
                        m_skills.ModifySkill(all_skill_str[skill_str_index], floats_to_assign[i]);
                        Jotunn.Logger.LogInfo(string.Format("RandomizeSkills assigned {0} to skill {1}", floats_to_assign[i], all_skill_str[skill_str_index]));
                        all_skill_str.RemoveAt(skill_str_index);
                    }

                    m_isRescued = false;
                    m_isHirdmandr = false;
                    InvokeRepeating("RandomTalkRescue", 30f, 30f);
                }
                else
                {
                    ZDOtoAppearance();
                }

                m_personality.LoadValues();
                m_skills.LoadSkills();
                ZDOLoadMental();
                ZDOLoadGeneral();

                PopulateCombatProps();
                InvokeRepeating("HealIfHurt", 5f, 5f);

                if (!m_isRescued)
                {
                    Sit();
                    InvokeRepeating("RescueTutorialCheck", 10f, 1f);
                }

                if (!m_isHirdmandr)
                {
                    m_hirdmandrAI.enabled = false;
                }
                else
                {
                    m_hirdmandrAI.enabled = true;
                }
            }
            else
            {
                Invoke("LoadZDO", 1f);
            }

            m_znet.Register<string>("NPCTalkText", RPC_ReceiveTalkText);
        }

        public void Update()
        {

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

            m_monsterai.m_idleSound = new EffectList();
            m_monsterai.m_attackPlayerObjects = false;
            m_monsterai.m_viewRange = 15;
            m_monsterai.m_hearRange = 15;
            m_monsterai.m_maxChaseDistance = 10;

        }

        public void LoadZDO()
        {
            ZDOtoAppearance();
            m_personality.LoadValues();
            m_skills.LoadSkills();
            ZDOLoadMental();
            ZDOLoadGeneral();
            PopulateCombatProps();

            if (!m_isRescued)
            {
                Sit();
                InvokeRepeating("RescueTutorialCheck", 10f, 1f);
            }

            if (!m_isHirdmandr)
            {
                m_hirdmandrAI.enabled = false;
            }
            else
            {
                m_hirdmandrAI.enabled = true;
            }
        }

        public void RescueTutorialCheck()
        {
            if (Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) < 15)
            {
                Player.m_localPlayer?.ShowTutorial("hirdmandr_find_rescue");
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
                return HirdmandrComp.GetHoverName();
            }
            return orig(self);
        }

        public string GetHoverName()
        {
            return m_humanoid.m_name;
        }

        private void OnChar_RPC_Damage(On.Character.orig_RPC_Damage orig, Character self, long __1sender, HitData __2hit)
        {
            if (self.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                Hirdmandr_RPC_Damage(self, __1sender, __2hit, HirdmandrComp);
            }
            orig(self, __1sender, __2hit);
        }

        private void Hirdmandr_RPC_Damage(Character character, long sender, HitData hit, HirdmandrNPC HirdmandrComp)
        {
            if (character.IsDebugFlying() || !character.GetComponent<ZNetView>().IsOwner() || character.GetHealth() <= 0f || character.IsDead() || character.IsTeleporting() || character.InCutscene() || (hit.m_dodgeable && character.IsDodgeInvincible()))
            {
                return;
            }
            Character attacker = hit.GetAttacker();
            if ((hit.HaveAttacker() && attacker == null) || (character.IsPlayer() && !character.IsPVPEnabled() && attacker != null && attacker.IsPlayer()))
            {
                return;
            }
            if (attacker != null && !attacker.IsPlayer())
            {
                float difficultyDamageScalePlayer = Game.instance.GetDifficultyDamageScalePlayer(character.transform.position);
                hit.ApplyModifier(difficultyDamageScalePlayer);
            }
            character.m_seman.OnDamaged(hit, attacker);
            if (character.m_baseAI != null && !character.m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f && Time.time - character.m_backstabTime > 300f)
            {
                character.m_backstabTime = Time.time;
                hit.ApplyModifier(hit.m_backstabBonus);
                character.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, character.transform);
            }
            if (character.IsStaggering() && !character.IsPlayer())
            {
                hit.ApplyModifier(2f);
                character.m_critHitEffects.Create(hit.m_point, Quaternion.identity, character.transform);
            }
            if (hit.m_blockable && character.IsBlocking())
            {
                character.BlockAttack(hit, attacker);
            }
            character.ApplyPushback(hit);
            if (!string.IsNullOrEmpty(hit.m_statusEffect))
            {
                StatusEffect statusEffect = character.m_seman.GetStatusEffect(hit.m_statusEffect);
                if (statusEffect == null)
                {
                    statusEffect = character.m_seman.AddStatusEffect(hit.m_statusEffect);
                }
                else
                {
                    statusEffect.ResetTime();
                }
                if (statusEffect != null && attacker != null)
                {
                    statusEffect.SetAttacker(attacker);
                }
            }
            HitData.DamageModifiers damageModifiers = character.GetDamageModifiers();
            hit.ApplyResistance(damageModifiers, out var significantModifier);

            // Begin modifications
            float bodyArmor = 0f;
            if (m_humanoid.m_chestItem != null)
            {
                bodyArmor += m_humanoid.m_chestItem.GetArmor();
            }
            if (m_humanoid.m_legItem != null)
            {
                bodyArmor += m_humanoid.m_legItem.GetArmor();
            }
            if (m_humanoid.m_helmetItem != null)
            {
                bodyArmor += m_humanoid.m_helmetItem.GetArmor();
            }
            if (m_humanoid.m_shoulderItem != null)
            {
                bodyArmor += m_humanoid.m_shoulderItem.GetArmor();
            }
            Jotunn.Logger.LogInfo("NPC body armor is " + bodyArmor);
            hit.ApplyArmor(bodyArmor);
            // End modifications

            float poison = hit.m_damage.m_poison;
            float fire = hit.m_damage.m_fire;
            float spirit = hit.m_damage.m_spirit;
            hit.m_damage.m_poison = 0f;
            hit.m_damage.m_fire = 0f;
            hit.m_damage.m_spirit = 0f;
            character.ApplyDamage(hit, showDamageText: true, triggerEffects: true, significantModifier);
            character.AddFireDamage(fire);
            character.AddSpiritDamage(spirit);
            character.AddPoisonDamage(poison);
            character.AddFrostDamage(hit.m_damage.m_frost);
            character.AddLightningDamage(hit.m_damage.m_lightning);
        }
        
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
            {
                return false;
            }
            Jotunn.Logger.LogInfo("Interact happened!");
            m_user = user;
            ZInput.ResetButtonStatus("Inventory");
            ZInput.ResetButtonStatus("JoyButtonB");
            ZInput.ResetButtonStatus("JoyButtonY");
            ZInput.ResetButtonStatus("Use");

            if (!m_isHirdmandr)
            {
                m_guirescuecomp.TogglePanel();
            }
            else
            {
                m_guiHirdmandr.TogglePanel();
            }
            PopulateCombatProps();
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
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

        }

        public void ZDOLoadMental()
        {
            // ZDOs
            m_mentalcontentment = m_znet.GetZDO().GetFloat("hmnpc_mentalcontentment", 0f);
            m_mentalstress = m_znet.GetZDO().GetFloat("hmnpc_mentalstress", 0f);
        }

        public void ZDOSaveMental()
        {
            // ZDOs
            m_znet.GetZDO().Set("hmnpc_mentalcontentment", m_mentalcontentment);
            m_znet.GetZDO().Set("hmnpc_mentalstress", m_mentalstress);
        }

        public void ZDOLoadGeneral()
        {
            m_isRescued = m_znet.GetZDO().GetBool("hmnpc_isrescued");
            m_isHirdmandr = m_znet.GetZDO().GetBool("hmnpc_isHirdmandr");

            m_jarlZOID = m_znet.GetZDO().GetLong("hmnpc_jarlZOID", 0);

            m_roleArtisan = m_znet.GetZDO().GetBool("hmnpc_roleArtisan");
            m_roleWarrior = m_znet.GetZDO().GetBool("hmnpc_roleWarrior");
            m_jobThegn = m_znet.GetZDO().GetBool("hmnpc_jobThegn");
            m_thegnDayshift = m_znet.GetZDO().GetBool("hmnpc_thegnDayshift");
            m_jobHimthiki = m_znet.GetZDO().GetBool("hmnpc_jobHimthiki");

            m_fightingStyleDefense = m_znet.GetZDO().GetBool("hmnpc_fightingStyleDefense");
            m_fightingStyleOffense = m_znet.GetZDO().GetBool("hmnpc_fightingStyleOffense");
            m_jobGatherer = m_znet.GetZDO().GetBool("hmnpc_jobGatherer");
            m_fightingRangeClose = m_znet.GetZDO().GetBool("hmnpc_fightingRangeClose");
            m_fightingRangeMid = m_znet.GetZDO().GetBool("hmnpc_fightingRangeMid");
            m_fightingRangeFar = m_znet.GetZDO().GetBool("hmnpc_fightingRangeFar");

            ownedBedZDOID = m_znet.GetZDO().GetZDOID("hmnpc_ownedbed");
        }

        public void ZDOSaveGeneral()
        {
            m_znet.GetZDO().Set("hmnpc_isrescued", m_isRescued);
            m_znet.GetZDO().Set("hmnpc_isHirdmandr", m_isHirdmandr);
            
            m_znet.GetZDO().Set("hmnpc_jarlZOID", m_jarlZOID);
            
            m_znet.GetZDO().Set("hmnpc_roleArtisan", m_roleArtisan);
            m_znet.GetZDO().Set("hmnpc_roleWarrior", m_roleWarrior);
            m_znet.GetZDO().Set("hmnpc_jobThegn", m_jobThegn);
            m_znet.GetZDO().Set("hmnpc_thegnDayshift", m_thegnDayshift);
            m_znet.GetZDO().Set("hmnpc_jobHimthiki", m_jobHimthiki);
            
            m_znet.GetZDO().Set("hmnpc_fightingStyleDefense", m_fightingStyleDefense);
            m_znet.GetZDO().Set("hmnpc_fightingStyleOffense", m_fightingStyleOffense);
            m_znet.GetZDO().Set("hmnpc_jobGatherer", m_jobGatherer);
            m_znet.GetZDO().Set("hmnpc_fightingRangeClose", m_fightingRangeClose);
            m_znet.GetZDO().Set("hmnpc_fightingRangeMid", m_fightingRangeMid);
            m_znet.GetZDO().Set("hmnpc_fightingRangeFar", m_fightingRangeFar);
        }

        public void RandomTalkRescue()
        {
            if (m_znet.IsOwner())
            {
                if (Player.IsPlayerInRange(base.transform.position, m_rescueRange))
                {
                    m_znet.InvokeRPC(Game.instance.GetPlayerProfile().GetPlayerID(), "NPCTalkText", GetRandomRescueTalk());
                    // Chat.instance.SetNpcText(base.gameObject, Vector3.up * 1.5f, 20f, 5f, "", GetRandomRescueTalk(), large: false);
                }
            }
        }

        public void RPC_ReceiveTalkText(long uid, string talkText)
        {
            Chat.instance.SetNpcText(base.gameObject, Vector3.up * 1.5f, 20f, 5f, "", talkText, large: false);
        }

        public void Say(string toSay)
        {
            Chat.instance.SetNpcText(base.gameObject, Vector3.up * 1.5f, 10f, 5f, "", toSay, large: false);
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
            m_healTick = 2f + (m_skills.GetSkill("fighter") / 3);
            m_humanoid.SetMaxHealth(60f + m_skills.GetSkill("fighter"));
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
            // StartEmote("sit", oneshot: false);
            var m_animator = transform.Find("Visual").GetComponent<Animator>();
            m_animator.SetBool("emote_sit", value: false);
        }
        public void TryEmote(string a_string)
        {
            var m_animator = GetComponent<Animator>();
            m_animator.SetBool("emote_" + a_string, value: false);
        }

        public void Stand()
        {
            StopEmote();
        }


        public bool StartEmote(string emote, bool oneshot = true)
        {
            // m_animator.SetBool("emote_" + m_emoteState, value: false);
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
                m_jarlZOID = m_user.GetComponent<Player>().GetPlayerID();
                CancelInvoke("RescueTutorialCheck");
            }
            m_isRescued = true;
            m_znet.GetZDO().Set("hmnpc_isrescued", true);
            CancelInvoke("RandomTalkRescue");
            m_character.m_faction = Character.Faction.Players;

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
            m_guirescuecomp.TogglePanel();

            if (CheckWelcomeRanges())
            {
                if (!m_isRescued)
                {
                    Rescue();
                }
                Jotunn.Logger.LogInfo("WelcomeHome triggered an actual home, " + m_humanoid.m_name);
                m_isHirdmandr = true;
                m_hirdmandrAI.enabled = true;
                m_znet.GetZDO().Set("hmnpc_isHirdmandr", m_isHirdmandr);
                m_welcomeEffect.Create(base.transform.position, base.transform.rotation);
                m_monsterai.SetFollowTarget(null);
                m_monsterai.SetPatrolPoint();
                
                var thisPlayer = m_user.GetComponent<Player>();
                if (thisPlayer == Player.m_localPlayer) 
                {
                    Player.m_localPlayer?.ShowTutorial("hirdmandr_welcome_home");
                }
            }
            else
            {
                Jotunn.Logger.LogInfo("WelcomeHome triggered an error " + m_humanoid.m_name);
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Cannot welcome " + m_humanoid.m_name + "home.\nHirdmandr must be near a Workbench and a Fire.");
            }
        }

        public bool CheckWelcomeRanges()
        {
            Jotunn.Logger.LogInfo("CheckWelcomeRanges() starting...");
            CraftingStation[] station_array = UnityEngine.Object.FindObjectsOfType<CraftingStation>();
            Jotunn.Logger.LogInfo("Length of station_array = " + station_array.Length);
            foreach (CraftingStation station in station_array)
            {
                if (station.m_name == "$piece_workbench")
                {
                    if (Vector3.Distance(station.transform.position, base.transform.position) < 15)
                    {
                        Fireplace[] fire_array = UnityEngine.Object.FindObjectsOfType<Fireplace>();
                        foreach (Fireplace fire in fire_array)
                        {
                            if (Vector3.Distance(fire.transform.position, transform.position) < 15)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void TestPowerUpGear()
        {
            m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab("SwordBlackmetal"));
            m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab("ShieldBlackmetal"));
            m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab("HelmetPadded"));
            m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab("ArmorPaddedCuirass"));
            m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab("ArmorPaddedGreaves"));
            m_humanoid.GiveDefaultItem(PrefabManager.Instance.GetPrefab("CapeLox"));
        }

        public void SetBed(ZDOID bedZDOID)
        {
            ownedBedZDOID = bedZDOID;
            m_znet.GetZDO().Set("hmnpc_ownedbed", bedZDOID);
        }
    }
}
