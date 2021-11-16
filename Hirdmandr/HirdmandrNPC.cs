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
        public bool m_jobHimthiki = false;
        public bool m_fightingStyleDefense = true;
        public bool m_fightingStyleOffense = false;
        public bool m_jobGatherer = false;
        public bool m_fightingRangeClose = true;
        public bool m_fightingRangeMid = false;
        public bool m_fightingRangeFar = false;


        protected virtual void Awake()
        {
            Jotunn.Logger.LogInfo("An NPC Awoke");

            m_humanoid = GetComponent<Humanoid>();
            m_character = GetComponent<Character>();
            m_monsterai = GetComponent<MonsterAI>();
            m_visequip = GetComponent<VisEquipment>();
            m_guiHirdmandr = GetComponent<HirdmandrGUI>();
            m_guirescuecomp = GetComponent<HirdmandrGUIRescue>();
            m_znet = GetComponent<ZNetView>();
            m_skills.m_znetv = m_znet;
            m_personality.m_znetv = m_znet;

            On.Character.GetHoverText += OnGetHoverText;
            On.Character.GetHoverName += OnGetHoverName;

            SetupNPC();

            var npc_name = m_znet.GetZDO().GetString("hmnpc_name");
            if (npc_name == "")
            {
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

            PopulateCombatProps();
            InvokeRepeating("HealIfHurt", 5f, 5f);

            if (!m_isRescued)
            {
                Sit();
                InvokeRepeating("RescueTutorialCheck", 10f, 1f);
            }

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

        }

        public void Update()
        {

        }

        public void RescueTutorialCheck()
        {
            if (Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) < 15)
            {
                Player.m_localPlayer?.ShowTutorial("hirdmandr_find_rescue");
            }
            // Player closestPlayer = Player.GetClosestPlayer(transform.position, 15);
            // if ((bool)closestPlayer)
            // {
            //     if (!m_isRescued && closestPlayer == Player.m_localPlayer)
            //     {
            //         Player.m_localPlayer?.ShowTutorial("hirdmandr_find_rescue");
            //     }
            // }
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

        private void OnChar_RPC_Damage(On.Character.orig_RPC_Damage orig, Character self, long __1sender, HitData __2hit)
        {
            if (self.TryGetComponent<HirdmandrNPC>(out var HirdmandrComp))
            {
                Hirdmandr_RPC_Damage(self, __1sender, __2hit);
            }
            orig(self, __1sender, __2hit);
        }

        private void Hirdmandr_RPC_Damage(Character character, long sender, HitData hit)
        {
            if (m_character.IsDebugFlying() || !m_character.m_nview.IsOwner() || m_character.GetHealth() <= 0f || m_character.IsDead() || m_character.IsTeleporting() || m_character.InCutscene() || (hit.m_dodgeable && m_character.IsDodgeInvincible()))
            {
                return;
            }
            Character attacker = hit.GetAttacker();
            if ((hit.HaveAttacker() && attacker == null) || (m_character.IsPlayer() && !m_character.IsPVPEnabled() && attacker != null && attacker.IsPlayer()))
            {
                return;
            }
            if (attacker != null && !attacker.IsPlayer())
            {
                float difficultyDamageScalePlayer = Game.instance.GetDifficultyDamageScalePlayer(base.transform.position);
                hit.ApplyModifier(difficultyDamageScalePlayer);
            }
            m_character.m_seman.OnDamaged(hit, attacker);
            if (m_character.m_baseAI != null && !m_character.m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f && Time.time - m_character.m_backstabTime > 300f)
            {
                m_character.m_backstabTime = Time.time;
                hit.ApplyModifier(hit.m_backstabBonus);
                m_character.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, base.transform);
            }
            if (m_character.IsStaggering() && !m_character.IsPlayer())
            {
                hit.ApplyModifier(2f);
                m_character.m_critHitEffects.Create(hit.m_point, Quaternion.identity, base.transform);
            }
            if (hit.m_blockable && m_character.IsBlocking())
            {
                m_character.BlockAttack(hit, attacker);
            }
            m_character.ApplyPushback(hit);
            if (!string.IsNullOrEmpty(hit.m_statusEffect))
            {
                StatusEffect statusEffect = m_character.m_seman.GetStatusEffect(hit.m_statusEffect);
                if (statusEffect == null)
                {
                    statusEffect = m_character.m_seman.AddStatusEffect(hit.m_statusEffect);
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
            HitData.DamageModifiers damageModifiers = m_character.GetDamageModifiers();
            hit.ApplyResistance(damageModifiers, out var significantModifier);

            // Begin modifications
            float bodyArmor = m_character.GetBodyArmor();
            hit.ApplyArmor(bodyArmor);
            // End modifications

            float poison = hit.m_damage.m_poison;
            float fire = hit.m_damage.m_fire;
            float spirit = hit.m_damage.m_spirit;
            hit.m_damage.m_poison = 0f;
            hit.m_damage.m_fire = 0f;
            hit.m_damage.m_spirit = 0f;
            m_character.ApplyDamage(hit, showDamageText: true, triggerEffects: true, significantModifier);
            m_character.AddFireDamage(fire);
            m_character.AddSpiritDamage(spirit);
            m_character.AddPoisonDamage(poison);
            m_character.AddFrostDamage(hit.m_damage.m_frost);
            m_character.AddLightningDamage(hit.m_damage.m_lightning);
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
            m_isHirdmandr = m_znet.GetZDO().GetBool("hmnpc_isHirdmandr");

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
            m_healTick = 2f + (m_skills.GetSkill("fighter") / 3);
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
                m_jarlZOID = m_user.GetComponent<Player>().GetPlayerID();
                CancelInvoke("RescueTutorialCheck");
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
            m_guirescuecomp.TogglePanel();

            if (CheckWelcomeRanges())
            {
                Jotunn.Logger.LogInfo("WelcomeHome triggered an actual home, " + m_humanoid.m_name);
                m_isHirdmandr = true;
                m_znet.GetZDO().Set("hmnpc_isHirdmandr", m_isHirdmandr);
                m_welcomeEffect.Create(base.transform.position, base.transform.rotation);
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
    }
}
