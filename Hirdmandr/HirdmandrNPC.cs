// Hirdmandr
// a Valheim mod skeleton using JÃ¶tunn
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
    // MP-safe effect bus: which local effect to play. Carried over RPC_HMFX as a log-only
    // "kind"; state (skill level, rescued flag) lives in the ZDO, never in the RPC.
    public enum HMEffectKind
    {
        None = 0,
        SkillLevelup,
        Rescue,
        Welcome,
        Resurrect,
        BonusYield
    }

    [Serializable]
    public class HirdmandrNPC : MonoBehaviour, Interactable
    {
        public NPCPlayerClone m_npcPlayerClone;
        public NPCPlayerClone m_humanoid;
        public NPCPlayerClone m_character;
        public MonsterAI m_monsterai;
        public HirdmandrAI m_hirdmandrAI;
        public VisEquipment m_visequip;
        public HirdmandrGUI m_guiHirdmandr;
        public HirdmandrGUIRescue m_guirescuecomp;
        public ZNetView m_znet;
        public Container m_container;

        public Humanoid m_user;

        // Rescuing
        public bool m_isRescued;
        public bool m_isHirdmandr;
        public float m_rescueRange = 100f;
        public long m_jarlZOID;
        // Display name of the Jarl (m_jarlZOID is the stable ID; the name is what the manage
        // GUI shows). Welcome-only capture - see WelcomeHome.
        public string m_jarlName = "";

        // Skills
        public HMSkills m_skills;

        // Socialize (the fire murmur): ZDO-backed moment queue + talk loop (see HMSocialize).
        public HMSocialize m_socialize;

        // Visual
        public bool m_female;
        public LookAt m_lookAt;
        public EffectList m_rescueEffect = new EffectList();
        public EffectList m_welcomeEffect = new EffectList();
        public EffectList m_bonusYieldEffect = new EffectList();

        // Combat
        public float m_healTick = 2f;

        // Step 5: best-of-each combat gear, recalculated by EquipBest. Step 6 uses these for runtime swaps.
        public ItemDrop.ItemData m_bestMeleeWeapon = null;
        public ItemDrop.ItemData m_bestBow = null;
        public ItemDrop.ItemData m_bestArrows = null;

        // Step 6: weapon-type hysteresis. A bow is drawn when the target crosses the upper
        // threshold (or the player is already engaging at range); it is only dropped when the
        // target has closed past the lower threshold, so the swap does not flap at one distance.
        private const float m_bowEngageDistance = 14f;
        private const float m_bowDisengageDistance = 8f;
        private const float m_playerRangeEngageTime = 2f;

        // Jobs
        public bool m_roleArtisan = true;
        public bool m_roleWarrior = false;
        public bool m_jobThegn = false;
        public bool m_thegnDayshift = true;
        public bool m_thegnNightshift = false;
        // Thegn "guard here" post (persistent): when set, the guard anchors to this world position
        // for its patrol shifts; when unset the patrol picks a temporary post near the nearest
        // campfire each shift (temporary posts are never persisted).
        public bool m_thegnGuardSet = false;
        public Vector3 m_thegnGuardPoint = Vector3.zero;
        public bool m_jobHimthiki = false;
        public bool m_following = false;

        // Follow hardening (R5, MVP): ship attachment + leash. Owner-authoritative state, driven
        // from Update() only while the rescued NPC follows the player (rescued, not yet homed).
        private Vector3 m_lastPlayerPos = Vector3.zero;
        private bool m_haveLastPlayerPos = false;
        private float m_leashCooldown = 0f;
        private Ship m_aboardShip; // the ship the NPC is currently riding; null on land
        private float m_reboardCooldown = 0f; // rate-limits the in-water safety-net re-teleport
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
            try
            {
            m_npcPlayerClone = GetComponent<NPCPlayerClone>();
            m_humanoid = GetComponent<NPCPlayerClone>();
            m_character = GetComponent<NPCPlayerClone>();
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
            m_humanoid.m_hmnpc = this;

            m_character.m_name = "Human";
            m_character.m_health = 60f;
            m_character.m_faction = Character.Faction.Players;

            ZDOLoadGeneral();

            m_container = gameObject.AddComponent<Container>();
            m_container.m_width = 8;
            m_container.m_height = 4;
            m_container.m_name = "Hirdmandr NPC";
            m_container.m_inventory = new Inventory(m_container.m_name, m_container.m_bkg, m_container.m_width, m_container.m_height);
            m_container.Load();

            // On.MonsterAI.SelectBestAttack += OnMAI_SelectBestAttack;

            SetupNPC();

            m_rescueEffect.m_effectPrefabs = new EffectList.EffectData[2];
            m_rescueEffect.m_effectPrefabs[0] = new EffectList.EffectData
            {
                m_prefab = PrefabManager.Instance.GetPrefab("vfx_odin_despawn"),
                m_enabled = true,
                m_attach = false,
                m_inheritParentRotation = true,
                m_inheritParentScale = true,
                m_randomRotation = false,
                m_scale = true
            };
            m_rescueEffect.m_effectPrefabs[1] = new EffectList.EffectData
            {
                m_prefab = PrefabManager.Instance.GetPrefab("sfx_cooking_station_done"),
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
                m_prefab = PrefabManager.Instance.GetPrefab("vfx_boar_love"),
                m_enabled = true,
                m_attach = false,
                m_inheritParentRotation = true,
                m_inheritParentScale = true,
                m_randomRotation = false,
                m_scale = true
            };
			// Commented out the sound effect for "boar love". 
			// An oinking NPC was off-putting!
            // m_welcomeEffect.m_effectPrefabs[1] = new EffectList.EffectData
            // {
            //     m_prefab = PrefabManager.Instance.GetPrefab("sfx_boar_love"),
            //     m_enabled = true,
            //     m_attach = false,
            //     m_inheritParentRotation = true,
            //     m_inheritParentScale = true,
            //     m_randomRotation = false,
            //     m_scale = true
            // };
			m_bonusYieldEffect.m_effectPrefabs = new EffectList.EffectData[1];
            m_bonusYieldEffect.m_effectPrefabs[0] = new EffectList.EffectData
            {
                m_prefab = PrefabManager.Instance.GetPrefab("fx_BonusYield"),
                m_enabled = true,
                m_attach = false,
                m_inheritParentRotation = true,
                m_inheritParentScale = true,
                m_randomRotation = false,
                m_scale = true
            };
            // Skill-levelup effect (MP-safe): NPCPlayerClone.m_skillLevelupEffects is a non-serialized
            // field that resets to an empty EffectList on every instantiated NPC, so it is (re)populated
            // per-instance here, the same way m_rescueEffect / m_welcomeEffect are set up above. We clone
            // the player's own skill-levelup effect list, which already carries both the sparkle
            // (vfx_skilllevelup) and the chime (sfx_levelup); it is fired at runtime by
            // PlaySkillLevelupEffect().
            Player hmPlayerSkillFx = PrefabManager.Instance.GetPrefab("Player").GetComponent<Player>();
            m_character.m_skillLevelupEffects = Utils.CloneEffectList(hmPlayerSkillFx.m_skillLevelupEffects);

            m_skills = new HMSkills(this, m_znet);
            m_socialize = new HMSocialize(this, m_znet);

            DecideOwnershipInit();

            m_container.m_name = m_humanoid.m_name + "'s Inventory";

            m_monsterai.SetPatrolPoint();

            m_znet.Register<string>("NPCTalkText", RPC_ReceiveTalkText);
            m_znet.Register<string>("RPC_HMSetting", RPC_HMSetting);
            m_znet.Register<string>("RPC_HMFX", RPC_HMFX);
            m_znet.Register<long>("RPC_HMFollowToggle", RPC_HMFollowToggle);
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrNPC.Awake threw:\n" + e);
                throw;
            }
        }

        // Unify the NPC's two inventories into a single grid. Humanoid.m_inventory is readonly
        // (and is the inventory Valheim itself uses for equipment/weight/armor), so we point the
        // CONTAINER's grid AT the humanoid's inventory, not the reverse. The container becomes a
        // view over the humanoid's carry; its ZDO stays the sole persistence owner (Load/Save
        // operate on whatever m_container.m_inventory references, and Humanoid does not serialize
        // m_inventory on its own - no second save to keep in sync). Idempotent: EquipBest calls it
        // as a safety net, so it must not Load or otherwise mutate the grid contents.
        private void UnifyInventories()
        {
            if (m_humanoid != null && m_humanoid.m_inventory != null && m_container != null
                && m_container.m_inventory != m_humanoid.m_inventory)
            {
                m_container.m_inventory = m_humanoid.m_inventory;
                // Match the GUI grid to the humanoid's actual inventory size so every item shows.
                m_container.m_width = m_humanoid.m_inventory.m_width;
                m_container.m_height = m_humanoid.m_inventory.m_height;
            }
        }

        private void Start()
        {
            // Start() runs after every Awake() in the scene, so m_humanoid.m_inventory (created
            // by NPCPlayerClone base.Awake) exists. Alias the container to it, then load the
            // persisted gear into that grid (Awake's Load() populated the now-orphaned 8x4).
            UnifyInventories();
            if (m_container != null)
            {
                m_container.Load();
            }
        }

        private bool m_ownerInitDone;
        private int m_ownerInitRetries;
        // Bounded retry counter for ForceSpawnEquipBest (see that method).
        private int m_spawnEquipRetry;

        // Runs the owner-side initialization. Extracted from Awake because Awake fires before
        // ZNetScene.CreateObject wires the ZDO onto this ZNetView, so m_znet.IsOwner() is
        // false at Awake time and the owner-only setup would be skipped.
        private void OwnerInit()
        {
            try
            {
            var npc_name = m_znet.GetZDO().GetString("hmnpc_name");
            if (npc_name == "")
            {
                m_monsterai.enabled = false;

                // Position-based biome (Option B): the starting kit + skills come from the
                // biome the NPC spawns in. Resolve() falls back to Meadows for biomes with no
                // table (e.g. a spawn over Ocean).
                var spawn_biome = Heightmap.FindBiome(transform.position);
                var spawn_gear = BiomeData.Resolve(spawn_biome);

                RandomizeAppearance(spawn_biome);

                m_skills.LoadSkills();
                var all_skill_str = new List<string> { };
                foreach (HMSkills.SkillData skill_data in m_skills.m_hmSkills)
                {
                    all_skill_str.Add(skill_data.m_name);
                }
                var floats_to_assign = new List<float>(spawn_gear.skills);
                int skill_str_index;
                for (var i = 0; i < floats_to_assign.Count && i < all_skill_str.Count; i++)
                {
                    skill_str_index = UnityEngine.Random.Range(0, all_skill_str.Count);
                    m_skills.ModifySkill(all_skill_str[skill_str_index], floats_to_assign[i], leveled: false);
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
                m_skills.LoadSkills();
                ZDOLoadGeneral();
            }

            m_monsterai.m_patrol = false;
            PopulateCombatProps();
            ApplyCombatConfig(); // owner load: re-derive MAI from the persisted settings just loaded
            InvokeRepeating("HealIfHurt", 5f, 5f);

            if (!m_isRescued)
            {
                // Keep the base MonsterAI off so unrescued rescues sit instead of wander. The
                // fresh-spawn branch above already disabled it; this covers the LOAD path (a
                // named rescue), which otherwise leaves the base MonsterAI running.
                m_monsterai.enabled = false;
                m_npcPlayerClone.StartEmote("sit", false);
                InvokeRepeating("RescueTutorialCheck", 10f, 1f);
            }

            if (!m_isHirdmandr)
            {
                m_hirdmandrAI.enabled = false;
            }
            else
            {
                m_hirdmandrAI.enabled = true;
                m_hirdmandrAI.StartAI();
            }
            EquipBest();

            // The synchronous spawn-frame EquipBest() above runs before the humanoid/visual
            // pipeline and the item model prefabs are ready, so it equips into empty visuals and
            // the NPC looks naked until something re-runs EquipBest later (the rescue GUI does
            // exactly this when it opens). Re-run the REAL EquipBest over a short, self-terminating
            // window so the gear attaches as soon as the pipeline + prefabs are loaded.
            m_spawnEquipRetry = 0;
            InvokeRepeating("ForceSpawnEquipBest", 0.5f, 0.5f);
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrNPC.OwnerInit threw:\n" + e);
                throw;
            }
        }

        // Decide owner vs non-owner and run the matching init. IsOwner() is unreliable on the
        // first frame (ZDO not yet bound), so re-check over a short window; only fall back to
        // the remote-load path once we have definitively NOT become the owner.
        private void DecideOwnershipInit()
        {
            try
            {
            if (m_ownerInitDone)
            {
                return;
            }

            // The ZDO is wired onto this ZNetView only AFTER Awake (ZNetScene.CreateObject
            // instantiates the prefab - firing Awake - and binds the ZDO on a later call).
            // ZNetView.IsOwner() throws on an unbound view, so check GetZDO() first: a null
            // ZDO means "not bound yet", which we defer on rather than evaluate ownership.
            var zdo = (m_znet != null) ? m_znet.GetZDO() : null;
            bool own = (zdo != null && zdo.IsOwner());
            if (zdo != null)
            {
                zdo.Persistent = true;
            }

            if (own)
            {
                m_ownerInitDone = true;
                OwnerInit();
                return;
            }
            if (m_ownerInitRetries < 30)
            {
                m_ownerInitRetries++;
                Invoke("DecideOwnershipInit", 0f);
                return;
            }
            Invoke("LoadZDO", 1f);
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrNPC.DecideOwnershipInit threw:\n" + e);
                throw;
            }
        }

        // Re-run the real EquipBest() a few times after spawn, until the weapon has a visible
        // model or the window closes. The spawn-frame EquipBest() runs before the humanoid/visual
        // pipeline and the item model prefabs are ready, so it equips into empty visuals (the NPC
        // looks naked). EquipBest is idempotent and safe to re-run: it re-equips the held/worn
        // slots in place (items stay in the grid), re-runs SetupEquipment, and re-attaches the
        // visuals - the same method the rescue GUI calls when it opens, which is why the gear
        // appears when the GUI opens.
        //
        // Success = the weapon is attached in EITHER hand. Melee equips to the right hand, but
        // (per the earlier investigation) a BOW attaches to the LEFT hand, so checking only the
        // right hand would never see a bow as "done". The weapon is always present (100% roll).
        private void ForceSpawnEquipBest()
        {
            m_spawnEquipRetry++;
            if (m_visequip == null)
            {
                CancelInvoke("ForceSpawnEquipBest");
                return;
            }
            bool weaponVisible = (m_visequip.m_rightItemInstance != null) || (m_visequip.m_leftItemInstance != null);
            if (!weaponVisible)
            {
                EquipBest();
            }
            if ((m_visequip.m_rightItemInstance != null) || (m_visequip.m_leftItemInstance != null))
            {
                CancelInvoke("ForceSpawnEquipBest");
                return;
            }
            if (m_spawnEquipRetry >= 10)
            {
                int invCount = 0;
                if (m_container != null && m_container.m_inventory != null)
                {
                    foreach (var it in m_container.m_inventory.GetAllItems())
                    {
                        if (it != null) invCount++;
                    }
                }
                Jotunn.Logger.LogInfo(string.Format(
                    "[Hirdmandr] ForceSpawnEquipBest: weapon still not attached after 10 passes. inventory items={0}; rightItemInstance={1}; leftItemInstance={2}; visequip.m_rightItem={3}; visequip.m_leftItem={4}",
                    invCount,
                    (m_visequip.m_rightItemInstance != null),
                    (m_visequip.m_leftItemInstance != null),
                    m_visequip.m_rightItem.ToString(),
                    m_visequip.m_leftItem.ToString()));
                CancelInvoke("ForceSpawnEquipBest");
            }
        }

        public void Update()
        {
            // The decompiled Player clone empties its own Update(), so the base emote pump
            // (NPCPlayerClone.UpdateEmote) never runs - a StartEmote("sit") lands in the ZDO
            // but is never pushed to the animator (emote stays ""). Drive it here so the
            // unrescued-rescue sit pose actually plays. UpdateEmote is self-limiting (only
            // acts when emoteID changes) and auto-cancels the emote once the NPC moves
            // (owner + InEmote + m_moveDir != 0), which is the welcome->stand transition.
            if (m_npcPlayerClone != null)
            {
                m_npcPlayerClone.UpdateEmote();
            }

            // Follow hardening (R5) is owner-authoritative (TeleportTo) so gate on ownership.
            // IsOwner() throws on an unbound ZDO, so check GetZDO() first (same pattern as
            // DecideOwnershipInit). This tick runs on all clients, but only the owner acts.
            if (m_znet != null)
            {
                var zdo = m_znet.GetZDO();
                if (zdo != null && zdo.IsOwner())
                {
                    UpdateFollowHardening();
                }
            }
        }

        // (removed: #43 swim-probe diagnostic, root-caused and closed)

        // R5 follow hardening (MVP): ride the player's ship + leash. Runs EVERY frame (owner-only),
        // while the rescued NPC follows the player (rescued and not yet homed).
        // Three behaviours:
        //   1. Portal guard (D4): if the player jumped >50f in a tick (portal/teleport) or is
        //      mid-teleport, STOP following and wait - NEVER teleport the NPC across the jump.
        //   2. Ship: while the player is aboard their ship, hold the NPC on the deck behind them.
        //   3. Leash (D6 = 100f): if the NPC fell too far behind, snap it to an open spot near
        //      the player.
        // #77 perf: the old per-frame cost was two full FindObjectsOfType object-table scans per
        // call; those are now the 5s-TTL cache (RefreshShipPortalCache), so this body is cheap
        // and runs EVERY frame. It MUST stay per-frame: the portal reactive layer has to sample
        // on the player's jump tick while the NPC is still simulated (past ~87m from the owner
        // its update stops), so it cannot be throttled - a 10Hz gate here made the teleport
        // check fire only in a narrow window and broke the return-leg portal carry.
        private void UpdateFollowHardening()
        {
            // Gate (null-guarded): per the agreed design, the follow-hardening (ship + leash) applies
            // whenever a rescued OR welcomed NPC is following a player, for any reason:
            //   (m_isHirdmandr OR m_isRescued)  -> this is a real hirdmandr (not a sitting wild rescue)
            //   AND isFollowingAPlayer          -> the MonsterAI follow target is the assigned user.
            // Follow is driven through SetFollowTarget(m_user) by every follow mode (Rescue,
            // Himthiki, ...), so the follow target IS the generic "following a player" signal.
            GameObject followTarget = (m_monsterai != null) ? m_monsterai.GetFollowTarget() : null;
            bool isHirdmandr = m_isRescued || m_isHirdmandr;
            bool isFollowingPlayer = (m_user != null && followTarget == m_user.gameObject);

            if (!(isHirdmandr && isFollowingPlayer))
            {
                return;
            }
            if (m_user == null || m_humanoid == null || m_monsterai == null)
            {
                return;
            }
            Player player = m_user.GetComponent<Player>();
            if (player == null || m_humanoid.IsDead())
            {
                return;
            }

            // Portal handling. A portal is a world teleport: the player walks into a TeleportWorld
            // and appears at the linked exit (far away). Boat + leash below stay rescue-OR-welcomed;
            // only the PORTAL guard is split by scope:
            //   RESCUE NPC (m_isRescued && !m_isHirdmandr): must NEVER be carried through a portal
            //     (req 6) -> stall at the portal boundary (rescue-wait state, so the rescue GUI can
            //     re-start the follow when the player is back).
            //   WELCOMED NPC (m_isHirdmandr): teleports WITH the player through the portal
            //     (TeleportWelcomedNPCWithPlayer, #63) and re-follows on the far side.
            //
            // Two detection layers:
            //   proactive - the NPC itself is within the portal's guard radius (about to enter).
            //   reactive  - the player teleported this tick (IsTeleporting or a >50m jump).
            // m_lastPlayerPos is re-baselined every tick and cleared on a stall, so a resumed follow
            // never false-fires the reactive check on a stale position.
            TeleportWorld nearestPortal = null;
            float portalDist;
            bool nearPortal = FindNearestPortal(m_humanoid.transform.position, out nearestPortal, out portalDist)
                && portalDist <= PortalGuardRadius(nearestPortal);
            bool playerJumped = m_haveLastPlayerPos &&
                Vector3.Distance(player.transform.position, m_lastPlayerPos) > 50f;
            bool playerTeleported = m_haveLastPlayerPos && (player.IsTeleporting() || playerJumped);
            m_lastPlayerPos = player.transform.position;
            m_haveLastPlayerPos = true;

            if (m_isRescued && !m_isHirdmandr)
            {
                // RESCUE NPC: never follow through a portal.
                if (nearPortal || playerTeleported)
                {
                    PortalStallWait();
                    return;
                }
            }
            else
            {
                // WELCOMED NPC: teleport WITH the player through the portal (#63). The method owns
                // the whole flow (hold at the source -> place at the destination -> re-follow) and
                // acts on this same tick on purpose: past ~87-90m from the player this update
                // stops being simulated, so nothing here may rely on a later tick.
                if (playerTeleported)
                {
                    TeleportWelcomedNPCWithPlayer(player, nearestPortal, playerJumped);
                }
            }

            // Ship: while the player is aboard a ship, keep the NPC on that ship's deck. We key off
            // IsPlayerInBoat (the one signal true for every aboard case - standing, helm, seat, mast;
            // "none" on shore).
            //
            // Ride mechanic: on the board transition we land the NPC on the deck, then PARENT it to
            // the ship and make its rigidbody KINEMATIC. Parented + kinematic means the deck carries
            // it rigidly (no sliding) AND its rigidbody velocity is 0, so the character locomotion
            // animation - which reads velocity - idles instead of playing "walking backwards" from
            // the ship's world velocity. While aboard it simply stands on the deck.
            Ship ship = FindShipPlayerIn(player);
            if (ship != null)
            {
                bool needsBoard = (m_aboardShip != ship);
                // Safety net: if the NPC is in the water (fell overboard, or the board teleport
                // missed the deck), pull it back onto the deck. On the deck the water depth is ~0,
                // so a healthy on-deck NPC never trips this (no flicker). Rate-limited so a bad
                // spot can't spam a per-frame teleport.
                bool inWater = m_humanoid.IsSwimming();
                if (needsBoard || (inWater && Time.time >= m_reboardCooldown))
                {
                    Vector3 pp = player.transform.position;
                    Quaternion spotRot;
                    Vector3 target = FindOnDeckSpot(ship, pp, out spotRot);
                    if (target == Vector3.zero)
                    {
                        target = pp; // the player is aboard, so their spot is on-ship (fallback)
                    }
                    m_humanoid.TeleportTo(target, spotRot, false);
                    m_humanoid.m_body.velocity = Vector3.zero;
                    m_aboardShip = ship;
                    m_reboardCooldown = Time.time + 0.3f;
                }
                // Stand still on the deck - do not walk off.
                m_monsterai.StopMoving();
                return;
            }

            // Deboard: the player left the ship (or the ship went away). Clear the aboard state so
            // the NPC follows the player on land normally.
            if (m_aboardShip != null)
            {
                m_aboardShip = null;
            }

            // Leash: the NPC fell too far behind; snap it to an open spot near the player. Cooldown
            // so it does not re-teleport every frame right after the snap.
            //
            // The threshold MUST stay below the distance at which a following NPC stops being
            // simulated (observed ~87-90m: the NPC "sleeps"/desyncs and this Update stops ticking).
            // At 100f the code never runs past that point, so d>100 was unreachable and the leash
            // could never fire. 50f fires well before the cutoff, so a boxed/stranded NPC is pulled
            // back while it is still awake.
            float d = Vector3.Distance(m_humanoid.transform.position, player.transform.position);
            if (d > 50f && Time.time > m_leashCooldown)
            {
                Quaternion spotRot;
                Vector3 spot = FindOpenSpotNear(player.transform.position, player.transform.position.y, out spotRot);
                if (spot != Vector3.zero)
                {
                    m_humanoid.TeleportTo(spot, spotRot, false);
                    m_leashCooldown = Time.time + 5f;
                }
            }
        }

        // Sample a ring of 16 bearings around `center` (2-4 m out) and, for each, raycast straight
        // down 10 m starting 1 m above the feet. That start height is low enough that a roof/loft
        // above is never the first hit, and the 10 m reach clears a bunk bed / loft while stopping
        // short of a cliff below. The first bearing whose ray hits the TOP of a real standing
        // surface wins: bare terrain (heightmap / stones / rocks / logs - anything that is not a
        // piece) or a floor piece. We return that hit point lifted slightly so the teleport lands
        // ON the surface rather than inside it. No clear-sphere: the surface the ray hit IS the
        // standing surface, so a floor piece (not an obstacle) must not block the spot.
        // Vector3.zero if no bearing produced a valid surface.
        private Vector3 FindOpenSpotNear(Vector3 center, float centerY, out Quaternion rot)
        {
            rot = Quaternion.identity;
            for (int i = 0; i < 16; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, (360f / 16f) * i, 0f) * Vector3.forward;
                float r = UnityEngine.Random.Range(2f, 4f);
                Vector3 origin = new Vector3(center.x + dir.x * r, centerY + 1f, center.z + dir.z * r);
                RaycastHit hit;
                if (!Physics.Raycast(origin, Vector3.down, out hit, 10f))
                {
                    continue;
                }
                if (hit.collider == null)
                {
                    continue;
                }
                // Water is not a standing surface.
                if (hit.collider.name.IndexOf("Water", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }
                // A piece must be a floor; anything that is not a piece is bare terrain (fine to stand on).
                Piece piece = hit.collider.GetComponentInParent<Piece>();
                if (piece != null && piece.name.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                rot = Quaternion.LookRotation(center - hit.point, Vector3.up);
                return hit.point + Vector3.up * 0.3f;
            }
            return Vector3.zero;
        }

        // #54: after the NPC leaves its bed (AttachStop) it is left standing ON the bed collider
        // (m_spawnPoint + 0.5m up), where FindPath cannot reach it and it stands frozen in place.
        // If it is on a bed but NOT actively sleeping, teleport it to a clear floor spot beside the
        // bed so pathfinding can reach it. Called from the rest-exit hooks (the normal wake) and
        // from the HirdmandrAI periodic safety net (a save loaded with the NPC asleep, or the bed
        // vanishing). Returns true if it moved, false if it was already off the bed, actively
        // sleeping, or no open spot was found.
        public bool StepOffBed(string source = "unspecified")
        {
            if (m_humanoid == null)
            {
                return false;
            }
            // Actively sleeping (attached to the bed) is normal - never pull a sleeper up.
            if (m_humanoid.IsAttached())
            {
                return false;
            }
            if (!IsStandingOnBed())
            {
                return false;
            }
            Quaternion rot;
            Vector3 spot = FindOpenSpotNear(m_humanoid.transform.position, m_humanoid.transform.position.y, out rot);
            if (spot == Vector3.zero)
            {
                return false;
            }
            m_humanoid.TeleportTo(spot, rot, false);
            return true;
        }

        // #54: the wake (AttachStop) leaves the NPC standing on the bed collider briefly before the
        // step-off teleport lands, which reads as a visual glitch. Defer StepOffBed by 1s via a
        // one-shot Invoke so the detach settles first; CancelInvoke + re-Invoke makes it last-wins so
        // overlapping rest-exit hooks collapse into a single step-off.
        private string m_pendingStepOffSource = "unspecified";

        public void ScheduleStepOffBed(string source, float delay)
        {
            m_pendingStepOffSource = source;
            CancelInvoke("DoStepOffBed");
            Invoke("DoStepOffBed", delay);
        }

        private void DoStepOffBed()
        {
            StepOffBed(m_pendingStepOffSource);
        }

        // #54: true if the first real surface under this NPC (ignoring its own colliders) is a bed.
        // That is the "standing on the bed but not lying in it" state the wake path leaves behind.
        private bool IsStandingOnBed()
        {
            if (m_humanoid == null)
            {
                return false;
            }
            Transform self = m_humanoid.transform;
            // RaycastAll from the feet, closest first. The origin is inside our own collider, so
            // the very first hit is usually our own body; skip it (and any other of our own
            // colliders) to test the surface UNDER us, not our body.
            RaycastHit[] hits = Physics.RaycastAll(self.position, Vector3.down, 12f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null)
                {
                    continue;
                }
                bool isSelf = col.transform.IsChildOf(self);
                if (isSelf)
                {
                    continue;
                }
                // First non-self surface under us decides: is it a bed?
                bool isBed = col.GetComponentInParent<Bed>() != null;
                return isBed;
            }
            return false;
        }

        // Find an on-deck standing spot on `ship`, a couple meters off `playerPos` (the player, who
        // is aboard). A down-raycast that hits THIS ship's deck near the player's deck level is a
        // clean "on the ship" test (water and the shore ground never match). We prefer the most
        // central clear spot (furthest from the deck edge, so it does not slip off), falling back
        // to the most central on-deck spot when the deck is too crowded to be "clear". Vector3.zero
        // only if there is no on-deck candidate at all (the caller then uses the player's spot).
        private Vector3 FindOnDeckSpot(Ship ship, Vector3 playerPos, out Quaternion rot)
        {
            rot = Quaternion.identity;
            Vector3 right = ship.transform.right;
            Vector3 fwd = ship.transform.forward;
            Vector3 center = ship.transform.position;
            Vector3[] offsets =
            {
                right * 1.4f,
                right * -1.4f,
                right * 1.4f + fwd * 1.0f,
                right * -1.4f + fwd * 1.0f,
                fwd * 2.0f,
                right * 1.0f + fwd * 1.5f,
                right * -1.0f + fwd * 1.5f,
            };
            Vector3 bestDeck = Vector3.zero;
            float bestDeckD = float.MaxValue;
            Vector3 bestClear = Vector3.zero;
            float bestClearD = float.MaxValue;
            foreach (Vector3 off in offsets)
            {
                // Candidates are centered on the SHIP (spread around its center in its own frame),
                // kept at the player's deck level so the downward ray lands on the deck.
                Vector3 c = center + off;
                c.y = playerPos.y;
                RaycastHit hit;
                // On-deck = the ray hits THIS ship's deck near the player's deck level (not the
                // shore ground, not another ship).
                bool onDeck = Physics.Raycast(c + Vector3.up * 1.5f, Vector3.down, out hit, 5f) &&
                    hit.collider != null &&
                    hit.collider.GetComponentInParent<Ship>() == ship &&
                    Mathf.Abs(hit.point.y - playerPos.y) < 2f;
                if (!onDeck)
                {
                    continue;
                }
                float d = Vector3.Distance(hit.point, center);
                if (d < bestDeckD)
                {
                    bestDeckD = d;
                    bestDeck = hit.point;
                }
                if (d < bestClearD && !Physics.CheckSphere(hit.point + Vector3.up * 0.9f, 0.7f))
                {
                    bestClearD = d;
                    bestClear = hit.point;
                }
            }
            Vector3 pick = (bestClear != Vector3.zero) ? bestClear : bestDeck;
            if (pick != Vector3.zero)
            {
                rot = Quaternion.LookRotation(playerPos - pick, Vector3.up);
                // The raycast point sits on the deck surface, so lift the spawn a little to be
                // guaranteed above the deck collider - the NPC drops onto the deck cleanly
                // instead of materializing inside the collision mesh.
                pick.y += 0.3f;
                return pick;
            }
            return Vector3.zero;
        }

        // #77 perf: shared ship/portal lists, refreshed for ALL NPCs at most once per interval
        // (the old code ran a full FindObjectsOfType object-table scan per NPC per frame).
        // Ships and portals are static world objects; a destroyed ship is Unity-fake-null and
        // is skipped by the consumers below. 5s staleness is invisible: a ship built or
        // destroyed in MP self-heals on the next refresh.
        private static Ship[] s_shipList;
        private static TeleportWorld[] s_portalList;
        private static float s_shipPortalRefreshTime = -99999f;
        private const float m_shipPortalRefreshInterval = 5f;

        private static void RefreshShipPortalCache()
        {
            if (Time.time < s_shipPortalRefreshTime + m_shipPortalRefreshInterval)
            {
                return;
            }
            s_shipPortalRefreshTime = Time.time;
            s_shipList = UnityEngine.Object.FindObjectsOfType<Ship>();
            s_portalList = UnityEngine.Object.FindObjectsOfType<TeleportWorld>();
        }

        // #84 perf: shared list of all HirdmandrNPCs, refreshed for ALL NPCs at most once per
        // interval (the old code ran a full FindObjectsOfType object-table scan per free NPC per
        // 0.5s defense/escape poll). Same pattern as RefreshShipPortalCache (#77), but 1s
        // instead of 5s: this list gates defense response, so staleness here is a slow
        // reaction to danger, not invisible noise (ship/portals stay at 5s). NPC membership
        // only changes on spawn/destroy; a destroyed NPC is Unity-fake-null and is skipped by
        // the consumers below. 1s staleness is a non-issue: an NPC spawned inside the window
        // self-heals on the next refresh.
        private static HirdmandrNPC[] s_npcList;
        private static float s_npcListRefreshTime = -99999f;
        private const float m_npcListRefreshInterval = 1f;

        private static void RefreshNpcList()
        {
            if (Time.time < s_npcListRefreshTime + m_npcListRefreshInterval)
            {
                return;
            }
            s_npcListRefreshTime = Time.time;
            s_npcList = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
        }

        // Find the Ship the player is currently in, or null if they are not aboard any ship.
        // IsPlayerInBoat is true for standing on deck, at the helm, in a seat, or on the mast -
        // the one check that covers every aboard case (and is false on shore).
        private static Ship FindShipPlayerIn(Player p)
        {
            RefreshShipPortalCache();
            foreach (Ship s in s_shipList)
            {
                if (s != null && s.IsPlayerInBoat(p))
                {
                    return s;
                }
            }
            return null;
        }

        // Find the nearest TeleportWorld (portal) to `pos`. Sets `portal` and `dist` (distance to it,
        // float.MaxValue if none) and returns whether one was found. Mirrors FindShipPlayerIn (the
        // Ship scan). Uses the shared 5s-TTL cache (RefreshShipPortalCache, #77) instead of a
        // per-call FindObjectsOfType object-table scan.
        private static bool FindNearestPortal(Vector3 pos, out TeleportWorld portal, out float dist)
        {
            RefreshShipPortalCache();
            portal = null;
            float best = float.MaxValue;
            foreach (TeleportWorld p in s_portalList)
            {
                if (p == null)
                {
                    continue;
                }
                float d = Vector3.Distance(pos, p.transform.position);
                if (d < best)
                {
                    best = d;
                    portal = p;
                }
            }
            dist = best;
            return (portal != null);
        }

        // How close a following RESCUE NPC may get to a portal before it stalls. Derived from the
        // portal's own activation + exit distances (a big portal gets a bigger exclusion zone),
        // floored at 9m so small portals still have a usable buffer. (The stall spot is 16m out,
        // comfortably outside this radius, so re-Rescuing does not re-trigger.)
        private static float PortalGuardRadius(TeleportWorld portal)
        {
            float fromPortal = (portal != null) ? (portal.m_activationRange + portal.m_exitDistance) : 0f;
            return Mathf.Max(9f, fromPortal);
        }

        // Portal-stall: the player used a portal (or the rescue NPC reached the portal boundary).
        // A clone of RescueWait (same stop + rescue-wait state) so the rescue GUI can re-start the
        // follow when the player is back, plus a hard StopMoving and a cleared last-player baseline
        // (so a resumed follow does not false-fire the reactive portal check on a stale position).
        private void PortalStallWait()
        {
            Jotunn.Logger.LogInfo("[follow-portal] PortalStallWait on " + m_humanoid.m_name + " (portal guard) - walking to a spot in front of the portal, rescue-wait state");
            m_monsterai.SetFollowTarget(null);
            // Walk (do NOT teleport) to ~16m in front of the portal - the side a returning player
            // emerges from - so they can spot the NPC right after teleporting in. BaseAI pathfinds
            // there normally (walks around walls/buildings as close as it can get). 16m keeps it
            // outside the guard radius (9f), so a re-Rescue does not immediately re-trigger.
            TeleportWorld p = null;
            float pd;
            FindNearestPortal(m_humanoid.transform.position, out p, out pd);
            if (p != null)
            {
                m_monsterai.SetPatrolPoint(p.transform.position + p.transform.forward * 16f);
            }
            else
            {
                m_monsterai.SetPatrolPoint();
            }
            m_haveLastPlayerPos = false;
            // Speech above the NPC's head (reuses Say / the rescue-talk SetNpcText pattern). No
            // TogglePanel here - the panel must NOT open on a remote stall; the player re-opens the
            // rescue GUI by interacting with the NPC when they are back.
            // cullDistance 30f: the NPC walks ~16m to its patrol spot right after this, so the
            // default 10m cull would hide the line almost instantly. 30m keeps it up for the full
            // 5s TTL so a player at the portal can actually read it.
            Say("I cannot use portals without Oden's blessing", 30f);
            CancelInvoke("PortalWaitText");
            Invoke("PortalWaitText", 3f);
            ZDOSaveGeneral();
        }

        private void PortalWaitText()
        {
            Say("I will wait here.", 30f);
        }

        // #63: a WELCOMED hirdmandr teleports WITH its player when the player uses a portal, so it
        // can follow the player across maps. Called every tick while the reactive layer fires
        // (playerTeleported). TeleportWorld::Teleport moves the player synchronously (to the
        // linked portal's exit: exitPos + forward*m_exitDistance + up), so on the tick the jump
        // is seen the player is ALREADY at the destination: we place the NPC at an open spot at
        // the player's current position and re-follow. Before that jump tick (IsTeleporting()
        // alone, player still at the source) we do NOTHING on purpose: no held state, no cleared
        // follow target, no StopMoving - the D5 auto-stop in HirdmandrAI turns a cleared follow
        // target into StopFollowing, which would drop this very update's gate and strand the NPC.
        // The NPC simply keeps following the player at the source, and if the teleport never
        // jumps there is nothing to undo. No deferred state machine possible anyway: past
        // ~87-90m from the player this update stops being simulated (see the leash comment), so
        // the teleport must happen on the jump tick itself.
        private void TeleportWelcomedNPCWithPlayer(Player player, TeleportWorld portal, bool playerJumped)
        {
            if (!playerJumped)
            {
                // IsTeleporting() alone: the player has not moved yet. Keep following them at the
                // source; the jump lands on a later tick and triggers the teleport below.
                return;
            }
            // The jump is in: the player's current position IS the destination. Place the NPC at
            // an open spot there and re-follow.
            Quaternion spotRot;
            Vector3 spot = FindOpenSpotNear(player.transform.position, player.transform.position.y, out spotRot);
            if (spot == Vector3.zero)
            {
                spot = player.transform.position;
                spotRot = player.transform.rotation;
            }
            m_humanoid.TeleportTo(spot, spotRot, false);
            m_humanoid.m_body.velocity = Vector3.zero;
            // Re-baseline so the reactive layer does not fire again on the old source position.
            m_lastPlayerPos = player.transform.position;
            m_haveLastPlayerPos = true;
            m_monsterai.SetFollowTarget(m_user.gameObject);
            ZDOSaveGeneral();
            Jotunn.Logger.LogInfo("[follow-portal] " + m_humanoid.m_name + " - teleported with the player through the portal to " + spot.ToString("F2"));
        }

        private void SetupNPC()
        {

            // component.m_backShield = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackShield_attach").GetComponent<Transform>();
            // component.m_backMelee = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackOneHanded_attach").GetComponent<Transform>();
            // component.m_backTwohandedMelee = NPCPlayerPrefab.transform.Find("Visual/Armature/Hips/Spine/Spine1/BackTwohanded_attach").GetComponent<Transform>();
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

            // MAI combat fields are owned by ApplyCombatConfig() (design 5); running it here
            // configures the fresh MonsterAI on all clients at load. It re-applies on change.
            ApplyCombatConfig();

            // Movement speed fix (2026-10-05, Thomas live-tuning session): Character's velocity
            // integrator approaches target speed at m_acceleration units/second, so the ctor
            // default of 1.0 took ~20s to reach walk/run speed and every movement looked
            // slow-mo. 4.0 (~5s to full speed) is Thomas's chosen value from in-game A/B.
            // IL-verified: only Character::.ctor writes this field (MonsterAI never touches
            // it), so this one-shot set is permanent and survives conflict states.
			// Note from Thomas: Experimenting with other acceleration values, 4f felt too fast in general play.
            NPCCharComp.m_acceleration = 0.8f;

        }

        public void LoadZDO()
        {
            try
            {
            var npc_name = m_znet.GetZDO().GetString("hmnpc_name");
            if (npc_name == "")
            {
                Invoke("LoadZDO", 1f);
            }
            else
            {
                ZDOtoAppearance();
                m_skills.LoadSkills();
                ZDOLoadGeneral();
                PopulateCombatProps();
                ApplyCombatConfig(); // non-owner load: re-derive MAI from the persisted settings just loaded

                if (!m_isRescued)
                {
                    m_monsterai.enabled = false;
                    m_npcPlayerClone.StartEmote("sit", false);
                    InvokeRepeating("RescueTutorialCheck", 10f, 1f);
                }

                if (!m_isHirdmandr)
                {
                    m_hirdmandrAI.enabled = false;
                }
                else
                {
                    m_hirdmandrAI.enabled = true;
                    m_hirdmandrAI.StartAI();
                }
            }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrNPC.LoadZDO threw:\n" + e);
                throw;
            }
        }

        public void RescueTutorialCheck()
        {
            if (Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) < 15)
            {
                HMTutorial.Show(HMTutorial.FindRescue);
            }
        }

        public void LookAt(Player closestPlayer)
        {
            m_character.m_eye.LookAt(closestPlayer.GetHeadPoint());
        }

        //public string GetHoverText()
        //{
        //    if (!m_isHirdmandr && !m_isRescued)
        //    {
        //        return m_humanoid.m_name + "\n[<color=yellow><b>E</b></color>] Rescue";
        //    }
        //    else if (!m_isHirdmandr && m_isRescued)
        //    {
        //        return m_humanoid.m_name + "\n[<color=yellow><b>E</b></color>] Talk";
        //    }
        //    else
        //    {
        //        return m_humanoid.m_name + "\n[<color=yellow><b>E</b></color>] Talk\n[<color=yellow><b>LShift+E</b></color>] Manage";
        //    }
        //}

        //public string GetHoverName()
        //{
        //    return m_humanoid.m_name;
        //}

        public void Hirdmandr_RPC_Damage(Character character, long sender, HitData hit, HirdmandrNPC HirdmandrComp)
        {
            try
            {
            if (!HirdmandrComp.m_isRescued)
            {
                return;
            }
            // First battle: the first time a rescued hirdmandr actually takes combat damage (once-only).
            // RPC_Damage runs on every client, so scope to the owner of the fighting hirdmandr; in solo
            // the local player is always the owner, so this is a no-op there.
            if (character.GetComponent<ZNetView>().IsOwner())
            {
                HMTutorial.Show(HMTutorial.FirstBattle);
            }
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
            if (hit.m_statusEffectHash != 0)
            {
                StatusEffect statusEffect = character.m_seman.GetStatusEffect(hit.m_statusEffectHash);
                if (statusEffect == null)
                {
                    statusEffect = character.m_seman.AddStatusEffect(hit.m_statusEffectHash, true, hit.m_itemLevel, hit.m_skillLevel, hit.m_variant);
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
            // Warrior training (damage taken): measure the health actually lost across every
            // damage type (impact + the fire/spirit/poison/frost/lightning re-applied below).
            float hpBefore = character.GetHealth();
            character.ApplyDamage(hit, showDamageText: true, triggerEffects: true, significantModifier);
            character.AddFireDamage(fire, 0);
            character.AddSpiritDamage(spirit, 0);
            character.AddPoisonDamage(poison, 0);
            character.AddFrostDamage(hit.m_damage.m_frost, 0);
            character.AddLightningDamage(hit.m_damage.m_lightning, 0);

            // Fighter trains on every point actually taken, from any source (enemy hit, fall,
            // fire, poison/burn ticks, ...). The health delta cannot exceed the remaining HP,
            // so the skillup is naturally capped at 5.0: a single hit for ~20% of max health
            // ~= 1f, a near-kill ~= 5f. Floor 0.5 keeps small/DoT ticks training something.
            // Owner-gated like the ZDO writes (the IsOwner guard above).
            float damageTaken = hpBefore - character.GetHealth();
            if (damageTaken > 0f)
            {
                m_skills.ModifySkill("fighter", Mathf.Max(0.5f, damageTaken / character.GetMaxHealth() * 5f));
            }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrNPC.Hirdmandr_RPC_Damage threw:\n" + e);
                throw;
            }
        }

        // Warrior training (hit landed): this NPC's melee/range skill trains when one of its
        // attacks actually damages an enemy. Observed from CharacterPatch on every
        // Character.RPC_Damage event (which runs on all clients), so every gate below mirrors
        // what the damage pipeline itself enforces - a dodged, blocked, or otherwise invalid
        // hit trains nothing, and only a DIRECT character hit counts (status-effect ticks like
        // poison/burn and environmental damage are not "hits" - they train the receiver's
        // fighter instead). The melee-vs-range split uses the game's own per-hit m_ranged
        // flag, so there is no weapon-timing race (an arrow in flight trains "range" even if
        // the NPC has already swapped back to melee).
        public void HM_WarriorHitSkillup(HitData hit, Character receiver)
        {
            if (hit == null || receiver == null || !m_isRescued)
            {
                return;
            }
            if (receiver == m_character)
            {
                return;
            }
            // Direct hits on characters only (EnemyHit/PlayerHit); ticks/environment are not.
            if (hit.m_hitType != HitData.HitType.EnemyHit && hit.m_hitType != HitData.HitType.PlayerHit)
            {
                return;
            }
            // MP: exactly one client applies - the owner of this NPC, where its AI and ZDO
            // writes live (same authority as the artisan skillups).
            if (!m_character.GetComponent<ZNetView>().IsOwner())
            {
                return;
            }
            // Same invalid-hit gates the damage pipeline applies before real damage lands.
            if (receiver.GetHealth() <= 0f || receiver.IsDead())
            {
                return;
            }
            if (hit.m_dodgeable && receiver.IsDodgeInvincible())
            {
                return;
            }
            if (hit.m_blockable && receiver.IsBlocking())
            {
                return;
            }
            // The receiver must be an enemy of this NPC (vanilla's own notion: other followers,
            // friends, and PVP-off players are not enemies).
            if (m_humanoid.m_baseAI == null || !m_humanoid.m_baseAI.IsEnemy(receiver))
            {
                return;
            }
            m_skills.ModifySkill(hit.m_ranged ? "range" : "melee", 1.0f);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            try
            {
            if (hold)
            {
                return false;
            }
            // Jotunn.Logger.LogInfo("Interact happened!");
            m_user = user;
            ZInput.ResetButtonStatus("Inventory");
            ZInput.ResetButtonStatus("JoyButtonB");
            ZInput.ResetButtonStatus("JoyButtonY");
            ZInput.ResetButtonStatus("Use");

            if (!m_isHirdmandr)
            {
                // PMW #52: a generated (not-yet-rescued) NPC can only be rescued once the
                // previous biome's boss is defeated AND its trophy sacrificed to Oden at the
                // altar. While that biome's boss-defeat global key is unset, suppress the
                // RescueGUI and have the NPC say what must be done. Gated on !m_isRescued (not
                // !m_isHirdmandr): an already-rescued follower must still be welcome-able even
                // after being led into a different biome, where FindBiome(position) is no longer
                // its spawn biome.
                if (!m_isRescued)
                {
                    Heightmap.Biome gateBiome = Heightmap.FindBiome(transform.position);
                    if (BiomeData.GateBlocked(gateBiome))
                    {
                        Say(BiomeData.GateLine(gateBiome), 30f);
                        return false;
                    }
                }
                m_guirescuecomp.TogglePanel();
            }
            else
            {
                // Shift+E: the manage panel, exactly as before. Plain E: toggle "follow me" for
                // the pressing player (owner-routed, see RequestFollowToggle), so in MP any
                // player can start/stop/take over the follow, not just the owner.
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    m_guiHirdmandr.TogglePanel();
                }
                else
                {
                    RequestFollowToggle(user);
                }
            }
            PopulateCombatProps();
            return false;
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrNPC.Interact threw:\n" + e);
                throw;
            }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        private void RandomizeAppearance(Heightmap.Biome biome)
        {
            var randSex = UnityEngine.Random.Range(0, 2);
            if (randSex == 1)
            {
                m_female = true;
            }
            else
            {
                m_female = false;
            }

            m_znet.GetZDO().Set("hmnpc_female", m_female);

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
                m_humanoid.SetPlayerModel(1);
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
            m_humanoid.SetHairColor(hairColorVec3);
            m_znet.GetZDO().Set("hmnpc_haircolor", hairColorVec3);

            var base_color = UnityEngine.Random.Range(0.3f, 1f);
            var skinColor = new Vector3(base_color, base_color, base_color);
            m_humanoid.SetSkinColor(skinColor);
            m_znet.GetZDO().Set("hmnpc_skincolor", skinColor);

            var ThisBeard = BeardItems[UnityEngine.Random.Range(0, BeardItems.Count)];
            m_humanoid.SetBeard(ThisBeard);
            m_visequip.SetBeardItem(StringExtensionMethods.GetStableHashCode(ThisBeard));
            m_znet.GetZDO().Set("hmnpc_beard", ThisBeard);

            var ThisHair = HairItems[UnityEngine.Random.Range(0, HairItems.Count)];
            m_humanoid.SetHair(ThisHair);
            m_visequip.SetHairItem(StringExtensionMethods.GetStableHashCode(ThisHair));
            m_znet.GetZDO().Set("hmnpc_hair", ThisHair);

            // === Starting kit, driven by the biome (PMW #29, D3) ===
            var gear = BiomeData.Resolve(biome);

            // WEAPON: pick uniformly from weapon1h + weapon2h + weaponRanged (100% have one).
            var weaponPool = new List<string>();
            weaponPool.AddRange(gear.weapon1h);
            weaponPool.AddRange(gear.weapon2h);
            weaponPool.AddRange(gear.weaponRanged);
            string weaponName = null;
            bool isWeapon1h = false;
            bool isWeaponRanged = false;
            if (weaponPool.Count > 0)
            {
                int poolIndex = UnityEngine.Random.Range(0, weaponPool.Count);
                weaponName = weaponPool[poolIndex];
                int n1h = gear.weapon1h.Count;
                int n2h = n1h + gear.weapon2h.Count;
                if (poolIndex < n1h) { isWeapon1h = true; }
                else if (poolIndex >= n2h) { isWeaponRanged = true; }
                AddItemToContainer(weaponName);
            }

            // SHIELD: only for 1h weapons, 50% chance.
            string shieldName = null;
            if (isWeapon1h && gear.shield.Count > 0 && UnityEngine.Random.value < 0.5f)
            {
                shieldName = gear.shield[UnityEngine.Random.Range(0, gear.shield.Count)];
                AddItemToContainer(shieldName);
            }

            // AMMO: only for ranged weapons, 100%, 20-40 of one random ammo type.
            string ammoName = null;
            int ammoCount = 0;
            if (isWeaponRanged && gear.ammo.Count > 0)
            {
                ammoName = gear.ammo[UnityEngine.Random.Range(0, gear.ammo.Count)];
                ammoCount = UnityEngine.Random.Range(20, 41); // 20..40 inclusive
                AddItemToContainer(ammoName, ammoCount);
            }

            // ARMOR (head, chest, legs): each slot independently 65% chance to roll an item
            // from its list (duplicates = weighting); a rolled "None" - or the 35% no-roll -
            // leaves the slot empty.
            string headName = AddArmorToContainer(gear.head);
            string chestName = AddArmorToContainer(gear.chest);
            string legsName = AddArmorToContainer(gear.legs);

            m_container.Save();

            Jotunn.Logger.LogInfo(string.Format(
                "Hirdmandr gear ({0}): weapon={1} shield={2} ammo={3}x{4} head={5} chest={6} legs={7}",
                biome,
                weaponName ?? "none",
                shieldName ?? "none",
                (ammoName ?? "none"), ammoCount,
                headName ?? "empty",
                chestName ?? "empty",
                legsName ?? "empty"));

            m_znet.GetZDO().Set("hmnpc_isrescued", false);
            m_znet.GetZDO().Set("hmnpc_isHirdmandr", false);

            m_visequip.UpdateBaseModel();
            m_visequip.UpdateColors();
            m_visequip.UpdateEquipmentVisuals();
            m_visequip.UpdateVisuals();
        }

        // Instantiates an ObjectDB item prefab and drops it into the NPC's container. name is
        // the exact ObjectDB prefab key; stack is how many copies in the cell (default 1).
        private void AddItemToContainer(string name, int stack = 1)
        {
            if (string.IsNullOrEmpty(name) || name == "None")
            {
                return;
            }
            var prefab = ObjectDB.instance.GetItemPrefab(name);
            if (prefab == null)
            {
                Jotunn.Logger.LogWarning(string.Format("Hirdmandr AddItemToContainer: no ObjectDB prefab for '{0}'", name));
                return;
            }
            var itemData = UnityEngine.Object.Instantiate(prefab).GetComponent<ItemDrop>().m_itemData;
            itemData.m_stack = stack;
            m_container.m_inventory.AddItem(itemData);
        }

        // Armor slot per D3: 35% chance the slot is left empty outright, otherwise roll one
        // element from the list (duplicates = weighting); a rolled "None" also leaves it empty.
        // Returns the item name added, or null when the slot stayed empty.
        private string AddArmorToContainer(IList<string> slotList)
        {
            if (slotList == null || slotList.Count == 0)
            {
                return null;
            }
            if (UnityEngine.Random.value <= 0.35f)
            {
                return null;
            }
            string pick = slotList[UnityEngine.Random.Range(0, slotList.Count)];
            if (pick == "None")
            {
                return null;
            }
            AddItemToContainer(pick);
            return pick;
        }

        public void ZDOtoAppearance()
        {
            m_female = m_znet.GetZDO().GetBool("hmnpc_female");
            if (m_female) 
            {
                m_humanoid.SetPlayerModel(1);
            }
            else
            {
                m_humanoid.SetPlayerModel(0);
            }
            m_humanoid.m_name = m_znet.GetZDO().GetString("hmnpc_name");

            Vector3 hairColorVec3 = m_znet.GetZDO().GetVec3("hmnpc_haircolor", new Vector3(1f, 1f, 1f));
            m_humanoid.SetHairColor(hairColorVec3);

            Vector3 skinColor = m_znet.GetZDO().GetVec3("hmnpc_skincolor", new Vector3(1f, 1f, 1f));
            m_humanoid.SetSkinColor(skinColor);

            string ThisBeard = m_znet.GetZDO().GetString("hmnpc_beard");
            m_humanoid.SetBeard(ThisBeard);
            m_visequip.SetBeardItem(StringExtensionMethods.GetStableHashCode(ThisBeard));

            string ThisHair = m_znet.GetZDO().GetString("hmnpc_hair");
            m_humanoid.SetHair(ThisHair);
            m_visequip.SetHairItem(StringExtensionMethods.GetStableHashCode(ThisHair));

            m_visequip.UpdateBaseModel();
            m_visequip.UpdateColors();
        }

        public void ZDOLoadGeneral()
        {
            if (m_znet.GetZDO() == null)
            {
                return;
            }
            m_isRescued = m_znet.GetZDO().GetBool("hmnpc_isrescued");
            m_isHirdmandr = m_znet.GetZDO().GetBool("hmnpc_isHirdmandr");

            m_jarlZOID = m_znet.GetZDO().GetLong("hmnpc_jarlZOID", 0);
            m_jarlName = m_znet.GetZDO().GetString("hmnpc_jarlName", "");

            m_roleArtisan = m_znet.GetZDO().GetBool("hmnpc_roleArtisan");
            m_roleWarrior = m_znet.GetZDO().GetBool("hmnpc_roleWarrior");
            m_jobThegn = m_znet.GetZDO().GetBool("hmnpc_jobThegn");
            m_thegnDayshift = m_znet.GetZDO().GetBool("hmnpc_thegnDayshift");
            m_thegnNightshift = m_znet.GetZDO().GetBool("hmnpc_thegnNightshift");
            m_thegnGuardSet = m_znet.GetZDO().GetBool("hmnpc_thegnGuardSet");
            m_thegnGuardPoint = m_znet.GetZDO().GetVec3("hmnpc_thegnGuardPoint", Vector3.zero);
            if (m_thegnGuardSet && m_thegnGuardPoint == Vector3.zero)
            {
                m_thegnGuardSet = false; // a guard flag with no valid point is corrupt: treat as unset
            }
            m_jobHimthiki = m_znet.GetZDO().GetBool("hmnpc_jobHimthiki");

            if (m_jobThegn && m_jobHimthiki)
            {
                m_jobHimthiki = false;
                if (m_znet.IsOwner())
                {
                    m_znet.GetZDO().Set("hmnpc_jobHimthiki", false);
                }
            }

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
            if (m_znet.GetZDO() == null)
            {
                return;
            }
            m_znet.GetZDO().Set("hmnpc_isrescued", m_isRescued);
            m_znet.GetZDO().Set("hmnpc_isHirdmandr", m_isHirdmandr);
            
            m_znet.GetZDO().Set("hmnpc_jarlZOID", m_jarlZOID);
            m_znet.GetZDO().Set("hmnpc_jarlName", m_jarlName);
            
            m_znet.GetZDO().Set("hmnpc_roleArtisan", m_roleArtisan);
            m_znet.GetZDO().Set("hmnpc_roleWarrior", m_roleWarrior);
            m_znet.GetZDO().Set("hmnpc_jobThegn", m_jobThegn);
            m_znet.GetZDO().Set("hmnpc_thegnDayshift", m_thegnDayshift);
            m_znet.GetZDO().Set("hmnpc_thegnNightshift", m_thegnNightshift);
            m_znet.GetZDO().Set("hmnpc_thegnGuardSet", m_thegnGuardSet);
            m_znet.GetZDO().Set("hmnpc_thegnGuardPoint", m_thegnGuardPoint);
            m_znet.GetZDO().Set("hmnpc_jobHimthiki", m_jobHimthiki);
            
            m_znet.GetZDO().Set("hmnpc_fightingStyleDefense", m_fightingStyleDefense);
            m_znet.GetZDO().Set("hmnpc_fightingStyleOffense", m_fightingStyleOffense);
            m_znet.GetZDO().Set("hmnpc_jobGatherer", m_jobGatherer);
            m_znet.GetZDO().Set("hmnpc_fightingRangeClose", m_fightingRangeClose);
            m_znet.GetZDO().Set("hmnpc_fightingRangeMid", m_fightingRangeMid);
            m_znet.GetZDO().Set("hmnpc_fightingRangeFar", m_fightingRangeFar);
        }

        // --- Death transition (R3c Increment B) ---
        // Called from NPCPlayerClone.OnDeath when this NPC dies. Gate: only a fully welcomed
        // NPC (welcome home succeeded -> m_isHirdmandr) drops its gear and leaves a soul. A
        // rescued-but-not-welcomed (or never rescued) NPC drops nothing, so the failed rescue
        // does not persist and the PoI can later regenerate a fresh NPC (Increment C/D).
        public void HandleDeath()
        {
            try
            {
                Vector3 deathPos = transform.position;
                if (m_isHirdmandr && m_jobHimthiki)
                {
                    // A welcomed Himthiki has fallen (once-only).
                    HMTutorial.Show(HMTutorial.HimthikiDeath);
                }
                if (m_isHirdmandr)
                {
                    // The full gear pool lives in the container. EquipBest equips the best
                    // item FROM the container but never removes it, so worn gear is still in
                    // the container too - drop ONLY the container, or the worn items
                    // double-drop.
                    DropContainerItems(deathPos);
                    // Leave a soul carrying the NPC's full durable identity.
                    SoulItem.SpawnSoulFromNpc(this, deathPos);
                }
                // The corpse is NOT destroyed here on purpose (PMW #62): NPCPlayerClone runs
                // HandleDeath BEFORE base.OnDeath, and the game's own Character.OnDeath (that
                // base call) ends with ZNetScene.instance.Destroy(gameObject). Destroying here
                // first would kill the ZDO before the soul above could read the identity off it.
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrNPC.HandleDeath threw:\n" + e);
            }
        }

        private void DropContainerItems(Vector3 pos)
        {
            if (m_container == null || m_container.m_inventory == null)
            {
                return;
            }
            Vector3 dropPos = pos + Vector3.up * 0.2f;
            int dropped = 0;
            foreach (ItemDrop.ItemData item in m_container.m_inventory.GetAllItems())
            {
                if (item == null)
                {
                    continue;
                }
                ItemDrop.ItemData clone = item.Clone();
                clone.m_stack = item.m_stack;
                ItemDrop.DropItem(clone, clone.m_stack, dropPos, Quaternion.identity);
                dropped++;
            }
            Jotunn.Logger.LogInfo("[Hirdmandr] HandleDeath: dropped " + dropped + " container item(s) at " + dropPos.ToString("F1") + ".");
        }

        public void RandomTalkRescue()
        {
            if (m_znet.IsOwner())
            {
                if (Player.IsPlayerInRange(base.transform.position, m_rescueRange))
                {
                    // Say shows it locally (owner) and broadcasts to the other clients.
                    Say(GetRandomRescueTalk(), 20f);
                }
            }
        }

        // "NPCTalkText" receive (fired by Say): show the line on this (non-calling) client.
        public void RPC_ReceiveTalkText(long uid, string payload)
        {
            try
            {
            long localPlayer = 0;
            if (Game.instance != null && Game.instance.GetPlayerProfile() != null)
            {
                localPlayer = Game.instance.GetPlayerProfile().GetPlayerID();
            }
            // The calling client already showed the line locally; only show it on other clients.
            if (uid == localPlayer)
            {
                return;
            }
            // Payload is "cullDistance|text" - the cull is before the first '|'.
            float cullDistance = 10f;
            string toSay = payload;
            int sep = payload.IndexOf('|');
            if (sep > 0)
            {
                float.TryParse(payload.Substring(0, sep), out cullDistance);
                toSay = payload.Substring(sep + 1);
            }
            SayLocal(toSay, cullDistance);
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrNPC.RPC_ReceiveTalkText threw:\n" + e);
                throw;
            }
        }

        // --- GUI setting sync (review F2) ---
        // The GUI toggles run on whichever client opened the panel, but these fields live
        // on the owner's HirdmandrNPC and the ZDO Set is owner-only. So a toggle is applied
        // to the local cache immediately AND sent as an RPC; the owner's handler re-applies
        // it and persists. This is what makes a non-owner's toggle actually stick.

        public void InvokeSetting(string key, bool value)
        {
            // First skill: the first time any skill is enabled from the panel (once-only).
            if (key != null && key.StartsWith("skill:") && value)
            {
                HMTutorial.Show(HMTutorial.FirstSkill);
            }
            ApplySettingLocal(key, value);
            long sender = 0;
            if (Game.instance != null && Game.instance.GetPlayerProfile() != null)
            {
                sender = Game.instance.GetPlayerProfile().GetPlayerID();
            }
            m_znet.InvokeRPC(sender, "RPC_HMSetting", key + ":" + (value ? "1" : "0"));
        }

        public void RPC_HMSetting(long sender, string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return;
            }
            int sep = payload.LastIndexOf(':');
            if (sep <= 0 || sep >= payload.Length - 1)
            {
                return;
            }
            string key = payload.Substring(0, sep);
            bool value = payload.Substring(sep + 1) == "1";
            Jotunn.Logger.LogInfo("[HirdmandrNPC.RPC_HMSetting] key='" + key + "' value=" + value + " isOwner=" + m_znet.IsOwner());
            ApplySettingLocal(key, value);
            if (m_znet.IsOwner())
            {
                // Owner is now current; persist so every client's load picks up the change.
                m_skills.SaveSkills();
                ZDOSaveGeneral();
            }
        }

        // ---- MP-safe effect bus ----
        // One RPC (RPC_HMFX) plays a named local effect on every client. The owner plays it
        // locally in FireMpEffect (InvokeRPC is NOT delivered back to the owner) and then
        // broadcasts; the handler below plays it on every non-owner client. The payload is
        // "kindKey:logInfo" - log-only detail (state lives in the ZDO, never the RPC) - so each
        // client's log shows useful context during multiplayer testing.

        private static string EffectKindKey(HMEffectKind kind)
        {
            switch (kind)
            {
                case HMEffectKind.SkillLevelup:
                    return "skilllevelup";
                case HMEffectKind.Rescue:
                    return "rescue";
                case HMEffectKind.Welcome:
                    return "welcome";
                case HMEffectKind.Resurrect:
                    return "resurrect";
                case HMEffectKind.BonusYield:
                    return "bonusyield";
            }
            return "none";
        }

        private static HMEffectKind ParseEffectKind(string key)
        {
            switch (key)
            {
                case "skilllevelup":
                    return HMEffectKind.SkillLevelup;
                case "rescue":
                    return HMEffectKind.Rescue;
                case "welcome":
                    return HMEffectKind.Welcome;
                case "resurrect":
                    return HMEffectKind.Resurrect;
                case "bonusyield":
                    return HMEffectKind.BonusYield;
            }
            return HMEffectKind.None;
        }

        // Play the named local effect on THIS client only (no RPC). Each client must be able to
        // instantiate its own VFX/sound, so the effect lists are populated per-instance in Awake.
        public void PlayMpEffect(HMEffectKind kind)
        {
            switch (kind)
            {
                case HMEffectKind.SkillLevelup:
                    if (m_character != null)
                    {
                        m_character.PlaySkillLevelupEffect();
                    }
                    break;
                case HMEffectKind.Rescue:
                    if (m_rescueEffect != null)
                    {
                FireMpEffect(HMEffectKind.Rescue, m_humanoid.m_name);
                    }
                    break;
                case HMEffectKind.Welcome:
                    if (m_welcomeEffect != null)
                    {
                FireMpEffect(HMEffectKind.Welcome, m_humanoid.m_name);
                    }
                    break;
                case HMEffectKind.Resurrect:
                    // Local play only: the resurrection VFX/sound at the NPC's spawn position.
                    // Deliberately does NOT FireMpEffect (unlike Rescue/Welcome) - PlayMpEffect is
                    // already the local play, so re-entering FireMpEffect here would recurse.
                    // FireMpEffect(Resurrect, ...) is called from HirdmandrAltarHella on the
                    // owner; this case is the shared local play on every client.
                    if (m_rescueEffect != null)
                    {
                        m_rescueEffect.Create(transform.position, transform.rotation);
                    }
                    break;
                case HMEffectKind.BonusYield:
                    // Local play only (like Resurrect): the fx_BonusYield VFX at the NPC - it is
                    // standing at the work site when the bonus rolls, so this reads as "at the yield".
                    if (m_bonusYieldEffect != null)
                    {
                        m_bonusYieldEffect.Create(transform.position, transform.rotation);
                    }
                    break;
            }
        }

        // Fire the named effect for this NPC from the owner: play locally + broadcast to every
        // other client. No-ops on non-owner clients (their RPC_HMFX handler plays the effect).
        public void FireMpEffect(HMEffectKind kind, string info)
        {
            if (m_znet == null || !m_znet.IsOwner())
            {
                return;
            }
            string kindKey = EffectKindKey(kind);
            Jotunn.Logger.LogInfo("[HirdmandrFX] " + kindKey + " owner-local: " + info);
            PlayMpEffect(kind);
            long sender = 0;
            if (Game.instance != null && Game.instance.GetPlayerProfile() != null)
            {
                sender = Game.instance.GetPlayerProfile().GetPlayerID();
            }
            m_znet.InvokeRPC(sender, "RPC_HMFX", kindKey + ":" + info);
        }

        // Artisan job-success animation: the NPC plays the "interact" animation when it completes a
        // job action (one wood to the kiln, one ore to the smelter, one crop harvested, one
        // fixture refuelled, one piece repaired). Owner-only (the state machine that fires it is
        // IsOwner-gated; the IsOwner check is the same defensive no-op as FireMpEffect): play
        // locally, then broadcast. InvokeRPC is never delivered back to the sender, so the local
        // SetTrigger is what this client sees; every other client runs ZSyncAnimation's
        // RPC_SetTrigger on receipt (same owner-local + broadcast shape as the effect bus).
        // Deliberately NOT fired for bonus yields - one anim per job action, no double-call.
        public void FireArtisanInteractAnim()
        {
            if (m_znet == null || !m_znet.IsOwner())
            {
                return;
            }
            m_npcPlayerClone.m_zanim.SetTrigger("interact");
            long sender = 0;
            if (Game.instance != null && Game.instance.GetPlayerProfile() != null)
            {
                sender = Game.instance.GetPlayerProfile().GetPlayerID();
            }
            m_znet.InvokeRPC(sender, "RPC_SetTrigger", "interact");
            Jotunn.Logger.LogInfo("[HirdmandrFX] artisan interact anim: owner-local SetTrigger + RPC_SetTrigger broadcast");
        }

        // Effect bus handler: runs on every client EXCEPT the owner (the owner already played it
        // locally in FireMpEffect). payload = "kindKey:logInfo" (log-only; state lives in the ZDO).
        public void RPC_HMFX(long sender, string payload)
        {
            if (m_znet != null && m_znet.IsOwner())
            {
                return;
            }
            string kindKey = "none";
            string info = "";
            if (!string.IsNullOrEmpty(payload))
            {
                int sep = payload.IndexOf(':');
                if (sep > 0)
                {
                    kindKey = payload.Substring(0, sep);
                    info = payload.Substring(sep + 1);
                }
                else
                {
                    kindKey = payload;
                }
            }
            Jotunn.Logger.LogInfo("[HirdmandrFX] " + kindKey + " on this client: " + info);
            PlayMpEffect(ParseEffectKind(kindKey));
        }

        private void ApplySettingLocal(string key, bool value)
        {
            if (key.StartsWith("skill:"))
            {
                // Persist immediately via a direct ZDO.Set (same mechanism ModifySkill uses for
                // the float values). The old SetSkillEnableLocal only touched the local cache and
                // relied on RPC_HMSetting -> SaveSkills to persist, but that RPC is never
                // delivered in single-player, so the enable bool was never written to the ZDO.
                m_skills.SetSkillEnable(key.Substring("skill:".Length), value);
                return;
            }
            switch (key)
            {
                case "roleArtisan": m_roleArtisan = value; break;
                case "roleWarrior": m_roleWarrior = value; break;
                case "jobThegn":
                    if (value)
                    {
                        ApplyPersonaDefaultsIfDefaulted("Thegn");
                    }
                    m_jobThegn = value;
                    if (value && m_jobHimthiki)
                    {
                        m_jobHimthiki = false;
                    }
                    break;
                case "jobHimthiki":
                    if (value)
                    {
                        ApplyPersonaDefaultsIfDefaulted("Himthiki");
                    }
                    m_jobHimthiki = value;
                    if (value && m_jobThegn)
                    {
                        m_jobThegn = false;
                    }
                    break;
                case "thegnDayshift": m_thegnDayshift = value; break;
                case "thegnNightshift": m_thegnNightshift = value; break;
                case "jobGatherer": m_jobGatherer = value; break;
                case "fightingStyleDefense": m_fightingStyleDefense = value; break;
                case "fightingStyleOffense": m_fightingStyleOffense = value; break;
                case "fightingRangeClose": m_fightingRangeClose = value; break;
                case "fightingRangeMid": m_fightingRangeMid = value; break;
                case "fightingRangeFar": m_fightingRangeFar = value; break;
            }
            // The persisted settings just changed; re-derive the MAI combat config on this client
            // so every client converges to the same deterministic values (design 5). No-op for the
            // non-combat keys (roleArtisan, jobGatherer); skills are handled above and do not
            // drive MAI fields.
            ApplyCombatConfig();
        }

        private bool AreCombatAxesAtDefaults()
        {
            bool styleDefault = m_fightingStyleDefense && !m_fightingStyleOffense;
            bool rangeDefault = m_fightingRangeClose && !m_fightingRangeMid && !m_fightingRangeFar;
            bool skillDefault = m_skills == null || (!m_skills.isEnabled("melee") && !m_skills.isEnabled("range"));
            return styleDefault && rangeDefault && skillDefault;
        }

        private void SetCombatAxisLocal(string key, bool value)
        {
            switch (key)
            {
                case "fightingStyleDefense": m_fightingStyleDefense = value; break;
                case "fightingStyleOffense": m_fightingStyleOffense = value; break;
                case "fightingRangeClose": m_fightingRangeClose = value; break;
                case "fightingRangeMid": m_fightingRangeMid = value; break;
                case "fightingRangeFar": m_fightingRangeFar = value; break;
            }

            if (m_znet != null)
            {
                var zdo = m_znet.GetZDO();
                if (zdo != null)
                {
                    zdo.Set("hmnpc_" + key, value);
                }
            }
        }

        private void SetPersonaSkillLocal(string skill, bool value)
        {
            if (m_skills == null)
            {
                return;
            }

            if (m_znet != null && m_znet.GetZDO() != null)
            {
                m_skills.SetSkillEnable(skill, value);
            }
            else
            {
                m_skills.SetSkillEnableLocal(skill, value);
            }
        }

        private void ApplyPersonaDefaultsIfDefaulted(string persona)
        {
            if (!AreCombatAxesAtDefaults())
            {
                return;
            }

            if (persona == "Thegn")
            {
                SetCombatAxisLocal("fightingStyleDefense", true);
                SetCombatAxisLocal("fightingStyleOffense", false);
                SetCombatAxisLocal("fightingRangeClose", false);
                SetCombatAxisLocal("fightingRangeMid", true);
                SetCombatAxisLocal("fightingRangeFar", false);
                SetPersonaSkillLocal("melee", true);
                SetPersonaSkillLocal("range", false);
                Jotunn.Logger.LogInfo("[HirdmandrNPC] Thegn persona defaults applied: defensive/mid + melee.");
            }
            else if (persona == "Himthiki")
            {
                SetCombatAxisLocal("fightingStyleDefense", false);
                SetCombatAxisLocal("fightingStyleOffense", true);
                SetCombatAxisLocal("fightingRangeClose", false);
                SetCombatAxisLocal("fightingRangeMid", false);
                SetCombatAxisLocal("fightingRangeFar", true);
                SetPersonaSkillLocal("melee", false);
                SetPersonaSkillLocal("range", true);
                Jotunn.Logger.LogInfo("[HirdmandrNPC] Himthiki persona defaults applied: offensive/far + range.");
            }
        }

        // Speech above the NPC's head. Initiator-agnostic: the calling client shows the line
        // locally and it is broadcast ("NPCTalkText") to every other client, so simulation talk
        // (socialize / rescue / portal-wait) AND player-initiated lines (e.g. the biome gate)
        // are visible to every player within cullDistance. The cull is packed into the payload
        // so remote clients show the bubble with the same range.
        public void Say(string toSay, float cullDistance = 10f)
        {
            SayLocal(toSay, cullDistance);
            if (m_znet == null)
            {
                return;
            }
            long sender = 0;
            if (Game.instance != null && Game.instance.GetPlayerProfile() != null)
            {
                sender = Game.instance.GetPlayerProfile().GetPlayerID();
            }
            m_znet.InvokeRPC(sender, "NPCTalkText", cullDistance + "|" + toSay);
        }

        // The actual local bubble. Called by Say (on the calling client) and by
        // RPC_ReceiveTalkText (on every other client).
        private void SayLocal(string toSay, float cullDistance)
        {
            Chat.instance.SetNpcText(base.gameObject, Vector3.up * 1.5f, cullDistance, 5f, "", toSay, large: false);
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

            return abandon[UnityEngine.Random.Range(0, abandon.Count)] + rescueme[UnityEngine.Random.Range(0, rescueme.Count)];
        }

        public void PopulateCombatProps()
        {
            if (m_humanoid == null)
            {
                return;
            }

            m_healTick = 2f + (m_skills.GetSkill("fighter") / 4f);
            m_humanoid.SetMaxHealth(60f + 2f * m_skills.GetSkill("fighter"));
            ApplyProficiencyScaling();
        }

        // Proficiency scaling only (design 6): a skill level changes HOW WELL a weapon is used,
        // never whether the weapon can be used. Melee scales melee damage; range scales the
        // equipped bow's arrow damage/accuracy. There is intentionally no capability gate here.
        public void ApplyProficiencyScaling()
        {
            if (m_skills == null || m_humanoid == null)
            {
                return;
            }

            var item = m_humanoid.GetCurrentWeapon();
            if (item == null || item.m_shared == null)
            {
                return;
            }

            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon
                || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
            {
                var attack = m_humanoid.m_currentAttack != null ? m_humanoid.m_currentAttack : item.m_shared.m_attack;
                if (attack != null)
                {
                    attack.m_damageMultiplier = 1f + (m_skills.GetSkill("melee") / 50f);
                }
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
            {
                ApplyBowAiRange(item);
                var attack = m_humanoid.m_currentAttack != null ? m_humanoid.m_currentAttack : item.m_shared.m_attack;
                if (attack != null)
                {
                    ApplyRangePowerToAttack(attack, m_skills.GetSkill("range"));
                }
            }
        }

        // 0 range fires weakly; 50 range restores the previous fixed arrow power. Damage keeps
        // scaling past 50, while accuracy is capped at the old 50-range "fires it well" baseline.
        private void ApplyBowAiRange(ItemDrop.ItemData bow)
        {
            if (bow == null || bow.m_shared == null)
            {
                return;
            }

            float range = m_fightingRangeFar ? 25f : (m_fightingRangeMid ? 15f : 10f);
            bow.m_shared.m_aiAttackRange = range;
            bow.m_shared.m_aiAttackRangeMin = 0f;
            bow.m_shared.m_aiAttackMaxAngle = 5f;
        }

        private static void ApplyRangePowerToAttack(Attack attack, float rangeLevel)
        {
            if (attack == null)
            {
                return;
            }

            float t = Mathf.Clamp01(rangeLevel / 50f);
            attack.m_damageMultiplier = 1f + (rangeLevel / 50f);
            attack.m_projectileAccuracy = Mathf.Lerp(10f, 2f, t);
            attack.m_projectileAccuracyMin = Mathf.Lerp(20f, 10f, t);
        }

        // Step 6 helpers: best-of-each availability + the runtime bow/melee decision. A bow is only
        // "available" when both the bow AND at least one arrow are owned; this is what stops a
        // bow-less or ammo-less NPC from selecting a ranged attack it cannot use.
        private bool IsMeleeWeaponItem(ItemDrop.ItemData item)
        {
            return item != null && item.m_shared != null
                && (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon
                    || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon);
        }

        private bool IsBowItem(ItemDrop.ItemData item)
        {
            return item != null && item.m_shared != null
                && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
        }

        private ItemDrop.ItemData ScanBestMelee()
        {
            ItemDrop.ItemData best = null;
            if (m_container != null && m_container.m_inventory != null)
            {
                foreach (ItemDrop.ItemData item in m_container.m_inventory.GetAllItems())
                {
                    if (IsMeleeWeaponItem(item) && (best == null || best.m_shared.m_damages.GetTotalDamage() < item.m_shared.m_damages.GetTotalDamage()))
                    {
                        best = item;
                    }
                }
            }
            return best;
        }

        private ItemDrop.ItemData ScanBestBow()
        {
            ItemDrop.ItemData best = null;
            if (m_container != null && m_container.m_inventory != null)
            {
                foreach (ItemDrop.ItemData item in m_container.m_inventory.GetAllItems())
                {
                    if (IsBowItem(item) && (best == null || best.m_shared.m_damages.GetTotalDamage() < item.m_shared.m_damages.GetTotalDamage()))
                    {
                        best = item;
                    }
                }
            }
            return best;
        }

        private ItemDrop.ItemData ScanBestAmmo()
        {
            ItemDrop.ItemData best = null;
            if (m_container != null && m_container.m_inventory != null)
            {
                foreach (ItemDrop.ItemData item in m_container.m_inventory.GetAllItems())
                {
                    if (item.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo && item.m_stack > 0)
                    {
                        if (best == null || best.m_shared.m_damages.GetTotalDamage() < item.m_shared.m_damages.GetTotalDamage())
                        {
                            best = item;
                        }
                    }
                }
            }
            return best;
        }

        private ItemDrop.ItemData ScanBestShield()
        {
            ItemDrop.ItemData best = null;
            if (m_container != null && m_container.m_inventory != null)
            {
                foreach (ItemDrop.ItemData item in m_container.m_inventory.GetAllItems())
                {
                    if (item.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                    {
                        if (best == null || best.m_shared.m_armor < item.m_shared.m_armor)
                        {
                            best = item;
                        }
                    }
                }
            }
            return best;
        }

        private ItemDrop.ItemData GetCurrentBestMelee()
        {
            if (m_bestMeleeWeapon != null && m_bestMeleeWeapon.m_shared != null)
            {
                return m_bestMeleeWeapon;
            }
            m_bestMeleeWeapon = ScanBestMelee();
            return m_bestMeleeWeapon;
        }

        private ItemDrop.ItemData GetCurrentBestBow()
        {
            if (m_bestBow != null && m_bestBow.m_shared != null)
            {
                return m_bestBow;
            }
            m_bestBow = ScanBestBow();
            return m_bestBow;
        }

        private ItemDrop.ItemData GetCurrentBestArrows()
        {
            if (m_bestArrows != null && m_bestArrows.m_shared != null && m_bestArrows.m_stack > 0)
            {
                return m_bestArrows;
            }
            m_bestArrows = ScanBestAmmo();
            return m_bestArrows;
        }

        private bool IsBowAvailable()
        {
            return GetCurrentBestBow() != null && GetCurrentBestArrows() != null;
        }

        // The Himthiki "mirror the player" signal (design 4): the player is the engagement anchor,
        // so if the player is actively using a bow, the NPC treats ranged as the desired type even
        // when the raw distance has not crossed the bow threshold yet.
        private bool PlayerIsEngagingAtRange()
        {
            if (m_user == null || m_user.IsDead())
            {
                return false;
            }

            ItemDrop.ItemData playerWeapon = m_user.GetCurrentWeapon();
            if (!IsBowItem(playerWeapon)
                && !IsBowItem(m_user.m_leftItem)
                && !IsBowItem(m_user.m_rightItem))
            {
                return false;
            }

            return m_user.InAttack() || m_user.GetTimeSinceLastAttack() <= m_playerRangeEngageTime;
        }

        // Step 5/7 bow fix: MonsterAI does not run the Player bow-draw input path, so
        // Humanoid.m_attackDrawTime stays 0 and Humanoid.StartAttack passes a 0 draw
        // percentage to Attack.Start. Pre-charge the draw here so NPC bow shots use full
        // draw power/damage instead of firing like an immediate release.
        public void HM_PrepareBowAttack()
        {
            if (m_humanoid == null)
            {
                return;
            }

            var weapon = m_humanoid.GetCurrentWeapon();
            if (weapon == null || weapon.m_shared == null || weapon.m_shared.m_attack == null
                || !weapon.m_shared.m_attack.m_bowDraw)
            {
                return;
            }

            m_humanoid.m_attackDrawTime = 9999f;
        }

        // Single source of truth for every MonsterAI COMBAT field, derived deterministically from
        // the persisted persona/style/range settings - see common_warrior_design.md section 5.
        // Applied on ALL clients at load (SetupNPC, OwnerInit, LoadZDO) and on every setting
        // change (ApplySettingLocal), so every client converges to the same config (MP-safe).
        // EquipBest equips items only and must NOT write MAI fields; MAI config lives here, only.
        private void ApplyCombatConfig()
        {
            if (m_monsterai == null)
            {
                return;
            }

            // Fixed baseline (kept from the old one-shot in SetupNPC; tune in P3, Step 14).
            m_monsterai.m_idleSound = new EffectList();
            m_monsterai.m_attackPlayerObjects = false; // NPCs never target player-placed objects
            m_monsterai.m_viewRange = 15f;
            m_monsterai.m_hearRange = 15f;
            // Drift forward while turning instead of stop-and-go (old SetupNPC note, kept at 90).
            m_monsterai.m_smoothMovement = true;
            m_monsterai.m_moveMinAngle = 90f;

            // Range -> m_maxChaseDistance (design 5: close ~10, mid ~15, far ~25). Driven by the
            // range bools, NOT the equipped weapon, so it never depends on equip order. NOTE
            // (design open item 3): the old EquipBest bow path forced this to 0; that special case
            // is now owned here. Far only gets the 25m ranged chase radius while a bow is actually
            // available (bow + arrows); a bow-less far warrior clamps to the natural melee reach so
            // it closes in instead of standing at a fake ranged distance (Step 7).
            bool bowAvailable = IsBowAvailable();
            if (m_fightingRangeFar && bowAvailable)
            {
                m_monsterai.m_maxChaseDistance = 25f;
            }
            else if (m_fightingRangeFar)
            {
                m_monsterai.m_maxChaseDistance = 10f;
            }
            else if (m_fightingRangeMid)
            {
                m_monsterai.m_maxChaseDistance = 15f;
            }
            else
            {
                m_monsterai.m_maxChaseDistance = 10f; // close is the default
            }

            // Style -> aggression/retreat (design 5). defensive (default): retreat when hurt,
            // strafe more, attack slower. offensive: press forward, strafe less, attack faster.
            bool defensive = m_fightingStyleDefense;
            // m_fleeIfLowHealth is a float health-fraction threshold (NOT the bool the design
            // doc assumed): 0.0 = never flee (offensive, press forward); 0.3 = retreat at 30%
            // health (defensive). Verify the scale in-game; tune in Step 14.
            m_monsterai.m_fleeIfLowHealth = defensive ? 0.3f : 0.0f;
            m_monsterai.m_circulateWhileCharging = defensive;
            m_monsterai.m_circleTargetInterval = 0f; // (design open item 4; 0 = the old bow value)
            m_monsterai.m_minAttackInterval = defensive ? 1.0f : 0.5f;
        }

        public void HealIfHurt()
        {
            if (m_humanoid.GetHealth() < m_humanoid.GetMaxHealth())
            {
                m_humanoid.Heal(m_healTick, true);
            }
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
                HMTutorial.Show(HMTutorial.FirstRescue, m_user.GetComponent<Player>());
                m_jarlZOID = m_user.GetComponent<Player>().GetPlayerID();
                CancelInvoke("RescueTutorialCheck");
            }
            m_isRescued = true;
            // Clear the unrescued sit emote now that it's a hirdmandr (ZDO-synced, all clients
            // stand up). UpdateEmote also auto-cancels on first movement as a backup.
            StopEmote();
            ZDOSaveGeneral();
            CancelInvoke("RandomTalkRescue");
            CancelInvoke("PortalWaitText");
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
            ZDOSaveGeneral();
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
                else
                {
                    // The NPC is a rescued follower being welcomed for the first time (rescue=true,
                    // hirdmandr=false - the rescue panel is only reachable in that state). The Jarl is
                    // whoever actually welcomes it (m_user, set in Interact), not necessarily the
                    // original rescuer, so (re)assign it here; Rescue() above already did this for the
                    // never-rescued case. Persists via the ZDOSaveGeneral() at the end of this method.
                    m_jarlZOID = m_user.GetComponent<Player>().GetPlayerID();
                }
                // The Jarl's display name for the manage GUI. Welcome-only by design: the ZOID
                // is also set at first rescue, but the name lands here, at the welcome (a
                // resurrected NPC is set up welcomed by the altar, which re-lands both with
                // the performer as the new Jarl).
                m_jarlName = m_user.GetComponent<Player>().GetPlayerName();
                Jotunn.Logger.LogInfo("WelcomeHome triggered an actual home, " + m_humanoid.m_name);
                m_isHirdmandr = true;
                m_hirdmandrAI.enabled = true;
                m_hirdmandrAI.StartAI();
                m_znet.GetZDO().Set("hmnpc_isHirdmandr", m_isHirdmandr);
                // Ledger: mark this structure as welcomed home so it never re-offers a rescue.
                // H == 0 for a manually-spawned rescue (no structure) - in that case there is
                // nothing to mark. SetWelcomed is idempotent and rides the world's global keys.
                int rescuepoiHash = m_znet.GetZDO().GetInt(RescueLedger.HashKey, 0);
                if (rescuepoiHash != 0)
                {
                    RescueLedger.SetWelcomed(rescuepoiHash);
                }
                m_welcomeEffect.Create(base.transform.position, base.transform.rotation);
                m_monsterai.SetFollowTarget(null);
                m_monsterai.SetPatrolPoint();
                
                var thisPlayer = m_user.GetComponent<Player>();
                if (thisPlayer == Player.m_localPlayer) 
                {
                    HMTutorial.Show(HMTutorial.WelcomeHome);
                }
            }
            else
            {
                Jotunn.Logger.LogInfo("WelcomeHome triggered an error " + m_humanoid.m_name);
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Cannot welcome " + m_humanoid.m_name + "home.\nHirdmandr must be near a Workbench and a Fire.");
                // Owner-scoped, matching the success path above: WelcomeHome can run on a non-owner
                // client, so only the welcoming Jarl (local player == m_user) sees the failure note.
                if (m_user != null && m_user.GetComponent<Player>() == Player.m_localPlayer)
                {
                    HMTutorial.Show(HMTutorial.WelcomeFailed);
                }
            }
            ZDOSaveGeneral();
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

        // GUI toggle for the transient "follow me" state. Deliberately role/job-agnostic: any
        // welcomed NPC can follow, regardless of the himthiki warrior job (D3/D4). Routes
        // through the owner like the plain-E key, so a non-owner opening the panel in MP
        // still gets the follow target applied authoritatively.
        public void FollowToggle()
        {
            RequestFollowToggle(m_user);
            // Keep the existing behaviour: clicking the button closes the panel.
            m_guiHirdmandr.TogglePanel();
        }

        // Begin following m_user (the player who opened the panel). Follow is transient and
        // owner-driven: m_following is not ZDO-persisted, and the actual chasing is the native
        // MonsterAI follow (SetFollowTarget), which the game syncs to all clients.
        public void StartFollowing()
        {
            if (m_user == null)
            {
                return;
            }
            m_monsterai.ResetPatrolPoint();
            m_monsterai.SetFollowTarget(m_user.gameObject);
            m_following = true;
            // Show the hint to the ACTING player (m_user), not this client's local player: the
            // owner applies the toggle on behalf of whoever requested it.
            HMTutorial.Show(HMTutorial.FirstFollow, m_user.GetComponent<Player>());
        }

        // Stop following and restore the patrol point so the NPC's normal schedule can move it
        // again. Idempotent; called from the GUI and from the owner auto-stop path.
        public void StopFollowing()
        {
            m_monsterai.SetFollowTarget(null);
            m_monsterai.SetPatrolPoint();
            m_following = false;
        }

        // Per-player: is this NPC currently following the given player? Keys off the native
        // follow target (which the game syncs to all clients), NOT the owner-local m_following
        // bool, so every client's hover text / panel label is correct in MP.
        public bool IsFollowing(Humanoid actor)
        {
            if (actor == null || m_monsterai == null)
            {
                return false;
            }
            Player actorPlayer = actor.GetComponent<Player>();
            return (actorPlayer != null && m_monsterai.GetFollowTarget() == actorPlayer.gameObject);
        }

        // Request a follow toggle on behalf of the acting player (the one who pressed E, or
        // who has the panel open). The follow target is ZDO state owned by this NPC's
        // ZNetView, so only the owner may write it: the owner applies directly (a sender never
        // receives its own RPC, so the owner path is a plain call, not a self-RPC); everyone
        // else's request is routed to the owner via RPC_HMFollowToggle.
        public void RequestFollowToggle(Humanoid actor)
        {
            if (actor == null || m_znet == null)
            {
                return;
            }
            Player actorPlayer = actor.GetComponent<Player>();
            if (actorPlayer == null)
            {
                return;
            }
            long actorID = actorPlayer.GetPlayerID();
            if (m_znet.IsOwner())
            {
                ApplyFollowToggle(actorID);
            }
            else
            {
                m_znet.InvokeRPC("RPC_HMFollowToggle", actorID);
            }
        }

        // Owner-side apply of a follow toggle (direct call when the owner acts, or via
        // RPC_HMFollowToggle for everyone else). Per-player: if the NPC is already following
        // this player, stop; otherwise start following them, taking over from whoever was
        // followed before (Thomas's decision: my E always targets ME).
        private void ApplyFollowToggle(long playerID)
        {
            Player targetPlayer = NPCPlayerClone.GetPlayer(playerID);
            if (targetPlayer == null)
            {
                return;
            }
            // m_user is the shared "current acting player" the follow/leash/combat logic keys
            // off (followTarget == m_user.gameObject), so point it at the follow target.
            m_user = targetPlayer;
            if (m_monsterai != null && m_monsterai.GetFollowTarget() == targetPlayer.gameObject)
            {
                StopFollowing();
            }
            else
            {
                StartFollowing();
            }
        }

        // RPC_HMFollowToggle: a non-owner requested a follow toggle. Runs on the owner only.
        public void RPC_HMFollowToggle(long sender, long playerID)
        {
            if (m_znet != null && m_znet.IsOwner())
            {
                ApplyFollowToggle(playerID);
            }
        }

        // GUI "Guard here" button (Thegn): anchor the guard post to this NPC's current position,
        // stop following, and let the schedule route the Thegn to patrol (guarding) during its work
        // window. The post is persisted via ZDO so it survives save/load. Owner-only, like the
        // follow toggle (the panel is owner-driven).
        public void GuardHere()
        {
            if (m_znet == null || m_znet.GetZDO() == null || !m_znet.IsOwner())
            {
                return;
            }
            m_thegnGuardPoint = transform.position;
            m_thegnGuardSet = true;
            HMTutorial.Show(HMTutorial.FirstGuardHere);
            m_znet.GetZDO().Set("hmnpc_thegnGuardSet", m_thegnGuardSet);
            m_znet.GetZDO().Set("hmnpc_thegnGuardPoint", m_thegnGuardPoint);
            StopFollowing();
            m_guiHirdmandr.TogglePanel();
        }

        // Centralized schedule router: returns the top-level state this NPC should be in at
        // time-of-day ToD. Single source of truth used by NodeSchedule (entry) and each
        // work-state's exit check, so the routing can never disagree with itself. All times
        // come from HMSchedule.
        //   warrior Thegn, day && night -> patrol (guard 100% of the time, no breaks);
        //   warrior Thegn, day only     -> the day shift;
        //   warrior Thegn, night only   -> the night shift;
        //   warrior Thegn, neither      -> day shift (historical default; keeps the Thegn working);
        //   warrior Himthiki            -> socialize by day, rest at night;
        //   role Artisan or unassigned  -> morn socialize -> work -> evening socialize -> rest.
        public string GetStateBySchedule(float ToD)
        {
            if (m_roleWarrior && m_jobThegn)
            {
                if (m_thegnDayshift && m_thegnNightshift)
                {
                    return "patrol";
                }
                if (!m_thegnDayshift && m_thegnNightshift)
                {
                    if (HMSchedule.ToDInWindow(ToD, HMSchedule.ThegnNightPatrolStart, HMSchedule.ThegnNightSocialStart)) return "patrol";
                    if (HMSchedule.ToDInWindow(ToD, HMSchedule.ThegnNightSocialStart, HMSchedule.ThegnNightRestStart)) return "socialize";
                    return "rest";
                }
                // Day shift, or neither shift selected: the day shift keeps the Thegn working.
                if (HMSchedule.ToDInWindow(ToD, HMSchedule.ThegnDayPatrolStart, HMSchedule.ThegnDaySocialStart)) return "patrol";
                if (HMSchedule.ToDInWindow(ToD, HMSchedule.ThegnDaySocialStart, HMSchedule.ThegnDayRestStart)) return "socialize";
                return "rest";
            }
            if (m_roleWarrior && m_jobHimthiki)
            {
                if (HMSchedule.ToDInWindow(ToD, HMSchedule.HimthikiSocialStart, HMSchedule.HimthikiRestStart)) return "socialize";
                return "rest";
            }
            // Role Artisan or unassigned.
            if (HMSchedule.ToDInWindow(ToD, HMSchedule.ArtisanMornSocialStart, HMSchedule.ArtisanWorkdayStart)) return "socialize";
            if (HMSchedule.ToDInWindow(ToD, HMSchedule.ArtisanWorkdayStart, HMSchedule.ArtisanEveningSocialStart)) return "workDay";
            if (HMSchedule.ToDInWindow(ToD, HMSchedule.ArtisanEveningSocialStart, HMSchedule.ArtisanRestStart)) return "socialize";
            return "rest";
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

        // Release our claim on the bed and clear both sides of the bi-directional reference.
        // Mirrors the worksite (chest) UnClaim path: the bed side is freed unconditionally
        // (RemoveOwner) because a bed has at most one owner and it is always us; the NPC side is
        // cleared AND persisted so the ZDOID does not dangle at a dead/foreign bed.
        public void ReleaseBed()
        {
            if (ownedBedZDOID == ZDOID.None)
            {
                return;
            }
            GameObject bedGO = ZNetScene.instance.FindInstance(ownedBedZDOID);
            if (bedGO != null)
            {
                HirdmandrBed hmbed = bedGO.GetComponent<HirdmandrBed>();
                if (hmbed != null)
                {
                    hmbed.RemoveOwner();
                }
            }
            ClearOwnedBedRef();
        }

        // Clear this NPC's end of the bed claim (memory + ZDO) WITHOUT touching the bed. The bed
        // clears its own side separately; keeping this one-directional avoids re-entry when the
        // bed frees an owner (see HirdmandrBed.ClearOwnerAndNPC).
        public void ClearOwnedBedRef()
        {
            ownedBedZDOID = ZDOID.None;
            m_znet.GetZDO().Set("hmnpc_ownedbed", ZDOID.None);
        }

        public void OpenInventory(bool shouldOpen)
        {
            if (shouldOpen)
            {
                InventoryGui.instance.Show(m_container);
            }
            else
            {
                InventoryGui.instance.Hide();
            }
            m_container.Save();
            m_visequip.UpdateEquipmentVisuals();
        }

        public void EquipBest()
        {
            // Safety net: ensure the carry and the pool are the same grid before we read/equip.
            // Start() already unifies; this covers any early/synchronous OwnerInit call.
            UnifyInventories();

            // The single grid (m_container.m_inventory == m_humanoid.m_inventory) is BOTH the
            // pool and the carry. We compute best-per-type below, then equip those items IN PLACE.
            // No RemoveAll (it would wipe the only copy of the pool); no AddItem (the items are
            // already in the grid - re-adding would duplicate them). Equipped items stay in the
            // grid so the GUI keeps showing the full pool.

            ItemDrop.ItemData bestHelmet = null;
            ItemDrop.ItemData bestChest = null;
            ItemDrop.ItemData bestLegs = null;
            ItemDrop.ItemData bestCape = null;
            ItemDrop.ItemData bestShield = null;
            ItemDrop.ItemData bestMelee = null;
            ItemDrop.ItemData bestBow = null;
            ItemDrop.ItemData bestArrows = null;

            foreach (ItemDrop.ItemData item in m_container.m_inventory.GetAllItems())
            {
                if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet)
                {
                    if (bestHelmet == null)
                    {
                        bestHelmet = item;
                    }
                    else
                    {
                        if (bestHelmet.m_shared.m_armor < item.m_shared.m_armor)
                        {
                            bestHelmet = item;
                        }
                    }
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
                {
                    if (bestChest == null)
                    {
                        bestChest = item;
                    }
                    else
                    {
                        if (bestChest.m_shared.m_armor < item.m_shared.m_armor)
                        {
                            bestChest = item;
                        }
                    }
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
                {
                    if (bestLegs == null)
                    {
                        bestLegs = item;
                    }
                    else
                    {
                        if (bestLegs.m_shared.m_armor < item.m_shared.m_armor)
                        {
                            bestLegs = item;
                        }
                    }
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
                {
                    if (bestCape == null)
                    {
                        bestCape = item;
                    }
                    else
                    {
                        if (bestCape.m_shared.m_armor < item.m_shared.m_armor)
                        {
                            bestCape = item;
                        }
                    }
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                {
                    if (bestShield == null)
                    {
                        bestShield = item;
                    }
                    else
                    {
                        if (bestShield.m_shared.m_armor < item.m_shared.m_armor)
                        {
                            bestShield = item;
                        }
                    }
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon
                    || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
                {
                    if (bestMelee == null)
                    {
                        bestMelee = item;
                    }
                    else
                    {
                        if (bestMelee.m_shared.m_damages.GetTotalDamage() < item.m_shared.m_damages.GetTotalDamage())
                        {
                            bestMelee = item;
                        }
                    }
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
                {
                    if (bestBow == null)
                    {
                        bestBow = item;
                    }
                    else
                    {
                        if (bestBow.m_shared.m_damages.GetTotalDamage() < item.m_shared.m_damages.GetTotalDamage())
                        {
                            bestBow = item;
                        }
                    }
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo && item.m_stack > 0)
                {
                    if (bestArrows == null)
                    {
                        bestArrows = item;
                    }
                    else
                    {
                        if (bestArrows.m_shared.m_damages.GetTotalDamage() < item.m_shared.m_damages.GetTotalDamage())
                        {
                            bestArrows = item;
                        }
                    }
                }
            }

            m_bestMeleeWeapon = bestMelee;
            m_bestBow = bestBow;
            m_bestArrows = bestArrows;

            // A bow is only equipped when it is actually usable: a bow without arrows would make
            // the NPC select a ranged attack it cannot fire. Without usable bow + arrows, the best
            // melee is the default; with no melee either, the NPC stays unarmed rather than
            // attempting a dry bow.
            ItemDrop.ItemData bestWeapon = null;
            if (m_fightingRangeFar && bestBow != null && bestArrows != null)
            {
                bestWeapon = bestBow;
            }
            else if (bestMelee != null)
            {
                bestWeapon = bestMelee;
            }
            else if (bestBow != null && bestArrows != null)
            {
                bestWeapon = bestBow;
            }

            // Clear the HELD slots (right/left/utility/ammo) WITHOUT removing the items from the
            // grid. The old m_humanoid.UnequipAllItems() passed removeFromInventory=true, which ran
            // m_inventory.RemoveOneItem on whatever was equipped -- and since the grid IS the pool,
            // that deleted the farmer's cultivator (the only copy) the first time EquipBest ran after
            // acquire (e.g. the GUI toggle). Mirror the codebase's own UnequipDeathDropItems:
            // UnequipItem(x, false) clears the slot but keeps the item in the grid. Armor is left
            // alone here: EquipItem(best...) below overwrites each armor slot and EquipItem never
            // removes from the grid, so the old armor stays in the pool.
            if (m_humanoid.m_rightItem != null) m_humanoid.UnequipItem(m_humanoid.m_rightItem, false);
            if (m_humanoid.m_leftItem != null) m_humanoid.UnequipItem(m_humanoid.m_leftItem, false);
            if (m_humanoid.m_ammoItem != null) m_humanoid.UnequipItem(m_humanoid.m_ammoItem, false);
            if (m_humanoid.m_utilityItem != null) m_humanoid.UnequipItem(m_humanoid.m_utilityItem, false);
            if (bestHelmet != null)
            {
                m_humanoid.EquipItem(bestHelmet);
            }
            if (bestChest != null)
            {
                m_humanoid.EquipItem(bestChest);
            }
            if (bestLegs != null)
            {
                m_humanoid.EquipItem(bestLegs);
            }
            if (bestCape != null)
            {
                m_humanoid.EquipItem(bestCape);
            }

            bool equipShield = true;
            if (bestWeapon != null)
            {
                if (bestWeapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
                {
                    // MAI chase/strafe fields (m_maxChaseDistance, m_circulateWhileCharging,
                    // m_circleTargetInterval) now live in ApplyCombatConfig (Step 1); items only here.
                
                    ApplyBowAiRange(bestWeapon);
                    bestWeapon.m_shared.m_attack.m_speedFactor = 0f;
                    bestWeapon.m_shared.m_attack.m_projectileVel = 30;
                    bestWeapon.m_shared.m_attack.m_projectileVelMin = 2;
                    bestWeapon.m_shared.m_attack.m_projectileAccuracy = 2f;
                    bestWeapon.m_shared.m_attack.m_projectileAccuracyMin = 10f;
                    bestWeapon.m_shared.m_attack.m_speedFactorRotation = 0.2f;
                    bestWeapon.m_shared.m_attack.m_attackAngle = 0;
                    bestWeapon.m_shared.m_attack.m_useCharacterFacing = false;
                    bestWeapon.m_shared.m_attack.m_useCharacterFacingYAim = true;
                }

                m_humanoid.EquipItem(bestWeapon);

                if (bestWeapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                    || bestWeapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
                {
                    equipShield = false;
                }
            }

            if (equipShield)
            {
                if (bestShield != null)
                {
                    m_humanoid.EquipItem(bestShield);
                }
            }

            if (bestBow != null && bestArrows != null)
            {
                m_humanoid.EquipItem(bestArrows);
            }

            m_humanoid.SetupEquipment();

            // Re-apply proficiency after SetupEquipment so m_currentAttack is populated for the
            // newly equipped weapon (melee damage multiplier / bow arrow power).
            ApplyProficiencyScaling();

            m_visequip.UpdateEquipmentVisuals();

            // Gear availability changed (e.g. arrows added/removed), so ask the single MAI owner to
            // re-derive the chase radius. EquipBest itself still performs no direct MAI writes.
            ApplyCombatConfig();
        }

        // Farmer: take the cultivator OUT of the work chest (player-provided, non-spawnable) and
        // equip it. The physical tool moves from the chest into the NPC's OWN visible inventory
        // (m_container, via Inventory.MoveItemToThis), then is equipped. true = the NPC now holds
        // + wears it. (m_container is the stored/visible inventory; the carry slot is separate.)
        public bool AcquireCultivator(HirdmandrChest chest)
        {
            if (chest == null || chest.m_container == null || chest.m_container.m_inventory == null) return false;
            string name = FarmerCrops.CultivatorItem;
            ItemDrop.ItemData cult = chest.m_container.m_inventory.GetItem(name);
            if (cult == null) return false;
            // Move the tool chest -> the NPC's OWN visible inventory (m_container), then equip +
            // refresh the hand visual. m_container (not m_humanoid.m_inventory, the carry slot):
            // the visible/stored inventory is m_container, so an interrupt mid-flow can't strand
            // the tool in the invisible carry inventory and lose it.
            m_container.m_inventory.MoveItemToThis(chest.m_container.m_inventory, cult);
            chest.m_container.Save(); // owner-side push of the chest change
            // Persist the NPC's OWN container now that the tool is in its grid. The auto-save
            // (Container.OnContainerChanged) is orphaned — Awake registered it on the inventory
            // it created, but UnifyInventories() re-aliased m_container.m_inventory to the
            // humanoid's — so without this the ZDO blob still holds the pre-acquire grid and the
            // next CheckForChanges → Load wipes the tool out. (The old humanoid-ZDO save here was
            // the wrong target; m_container is the container whose ZDO Load/Save actually runs.)
            m_container.Save();
            m_humanoid.EquipItem(cult, false);
            if (m_visequip != null) m_visequip.UpdateEquipmentVisuals();
            return true;
        }

        // Farmer: return the cultivator to the chest it was taken from, then let the caller re-equip
        // best combat gear (EquipBest, from the WorkDay exit boundary). No-op unless the NPC is
        // actually holding one (self-healing: keyed on the item's presence, not a flag).
        //
        // Post-unification the "two inventories" are one grid (m_container.m_inventory is an alias
        // of m_humanoid.m_inventory), so this just finds the cultivator wherever it currently is
        // and moves it back to the chest. EquipBest no longer RemoveAlls the grid, so a stranded
        // tool can't be destroyed by re-equip.
        public void ReleaseCultivator(HirdmandrChest chest)
        {
            if (chest == null || chest.m_container == null || chest.m_container.m_inventory == null) return;
            ItemDrop.ItemData cult = null;
            Inventory src = null;
            if (m_container != null && m_container.m_inventory != null)
            {
                cult = m_container.m_inventory.GetItem(FarmerCrops.CultivatorItem);
                if (cult != null) src = m_container.m_inventory;
            }
            if (cult == null && m_humanoid != null && m_humanoid.m_inventory != null)
            {
                cult = m_humanoid.m_inventory.GetItem(FarmerCrops.CultivatorItem);
                if (cult != null) src = m_humanoid.m_inventory;
            }
            if (cult == null)
            {
                Jotunn.Logger.LogWarning("ReleaseCultivator: NOT holding a cultivator (checked m_container + carry) -- nothing to return.");
                return;
            }
            m_humanoid.UnequipItem(cult, false);
            chest.m_container.m_inventory.MoveItemToThis(src, cult);
            chest.m_container.Save();
            // Mirror the grid change to the NPC's OWN container ZDO. The tool has left the grid (it's
            // in the chest now), but the auto-save is orphaned (see AcquireCultivator), so without
            // this the ZDO blob still holds the grid with the tool and a later Load would put it back.
            m_container.Save();
            if (m_visequip != null) m_visequip.UpdateEquipmentVisuals();
        }

        // Farmer: put the cultivator back in the right hand if a GUI toggle / EquipBest swapped it
        // out mid-job (combat gear in), which looks wrong while farming. The tool is still in our
        // grid (EquipBest no longer RemoveAlls it), so this is a re-equip only. No-op if it's
        // already in hand, or if it's not in our grid yet (not acquired / not a farmer job).
        // Called from WorkDaySM.NodeDoJob.EnterFrom (the one-shot doJob entry), mirroring
        // EquipMenderHammer for the Mender's hammer.
        public void EnsureCultivatorEquipped()
        {
            string name = FarmerCrops.CultivatorItem;
            // Already in the right hand? Nothing to do.
            if (m_humanoid.m_rightItem != null && m_humanoid.m_rightItem.m_shared != null
                && m_humanoid.m_rightItem.m_shared.m_name == name)
            {
                return;
            }
            // Is it in our grid (the single shared inventory)?
            ItemDrop.ItemData cult = null;
            if (m_container != null && m_container.m_inventory != null)
            {
                cult = m_container.m_inventory.GetItem(name);
            }
            if (cult == null) return; // not in our grid - leave it (not acquired / mid-release)
            m_humanoid.EquipItem(cult, false);
            if (m_visequip != null) m_visequip.UpdateEquipmentVisuals();
        }

        // Farmer: is the cultivator currently in this NPC's own grid (m_container is the
        // carry grid post-unification)? Lets NodeSetupArtJob treat a chest with crops but
        // no tool as a valid worksite when the NPC already carries the tool (player
        // stocked the NPC directly, or it's still in hand). Pure query, no logs.
        public bool HasCultivator()
        {
            if (this.m_container != null && this.m_container.m_inventory != null)
            {
                if (this.m_container.m_inventory.GetItem(FarmerCrops.CultivatorItem) != null)
                {
                    return true;
                }
            }
            if (this.m_humanoid != null && this.m_humanoid.m_inventory != null)
            {
                if (this.m_humanoid.m_inventory.GetItem(FarmerCrops.CultivatorItem) != null)
                {
                    return true;
                }
            }
            return false;
        }

        // Step 6: the runtime weapon-type decision. This is the only place the "use bow vs use
        // melee" choice is made; MonsterAI.SelectBestAttack is patched to call it every frame.
        // It returns the weapon that should be equipped, and it keeps the humanoid's left/ammo
        // slots consistent with that weapon type. A bow is never returned unless arrows are owned.
        public ItemDrop.ItemData HM_SelectBestAttack(Humanoid target)
        {
            if (m_humanoid == null)
            {
                return null;
            }

            ItemDrop.ItemData current = m_humanoid.GetCurrentWeapon();
            bool currentlyBow = IsBowItem(current);
            bool bowAvailable = IsBowAvailable();
            ItemDrop.ItemData bestMelee = GetCurrentBestMelee();
            bool meleeAvailable = (bestMelee != null);

            if (!bowAvailable && !meleeAvailable)
            {
                return null;
            }

            bool desiredBow;
            if (bowAvailable && !meleeAvailable)
            {
                desiredBow = true;
            }
            else if (!bowAvailable)
            {
                // Step 7: a far warrior with no usable bow clamps to melee at natural reach.
                desiredBow = false;
            }
            else
            {
                float targetDistance = (target != null) ? Vector3.Distance(m_humanoid.transform.position, target.transform.position) : 0f;
                bool playerEngagingAtRange = PlayerIsEngagingAtRange();

                if (m_fightingRangeFar || playerEngagingAtRange)
                {
                    desiredBow = true;
                }
                else if (currentlyBow)
                {
                    // Hysteresis: once a bow is out, keep it until the target has really closed.
                    desiredBow = targetDistance >= m_bowDisengageDistance;
                }
                else
                {
                    // Hysteresis: draw the bow only after the target is clearly out of melee range.
                    desiredBow = targetDistance > m_bowEngageDistance;
                }
            }

            ItemDrop.ItemData desired = desiredBow ? GetCurrentBestBow() : bestMelee;
            if (desired == null)
            {
                desired = desiredBow ? bestMelee : GetCurrentBestBow();
            }
            if (desired == null)
            {
                return null;
            }

            // Final safety: if the desired weapon became unusable between the decision and here
            // (e.g. the last arrow was consumed), fall back to the other type.
            if (IsBowItem(desired) && GetCurrentBestArrows() == null)
            {
                desired = bestMelee;
            }
            if (desired == null)
            {
                return null;
            }

            bool weaponChanged = false;
            bool leftChanged = false;
            bool actualBow = IsBowItem(desired);

            if (actualBow)
            {
                // A bow lives in the left hand. If it is currently equipped in the right hand,
                // unequip it first so EquipItem moves it into the left-hand slot.
                if (m_humanoid.m_leftItem != desired)
                {
                    if (m_humanoid.m_rightItem == desired)
                    {
                        m_humanoid.UnequipItem(desired, false);
                    }
                    m_humanoid.EquipItem(desired, false);
                    weaponChanged = true;
                }

                // Any old right-hand weapon would make GetCurrentWeapon() report melee instead
                // of the bow, so clear it.
                if (m_humanoid.m_rightItem != null && m_humanoid.m_rightItem != desired)
                {
                    m_humanoid.UnequipItem(m_humanoid.m_rightItem, false);
                    weaponChanged = true;
                }
            }
            else
            {
                // Melee/two-handed weapons live in the right hand. If the desired weapon is
                // currently sitting in the left hand, move it to the right hand.
                if (m_humanoid.m_rightItem != desired)
                {
                    if (m_humanoid.m_leftItem == desired)
                    {
                        m_humanoid.UnequipItem(desired, false);
                    }
                    m_humanoid.EquipItem(desired, false);
                    weaponChanged = true;
                }
            }

            if (actualBow)
            {
                // A bow occupies the left hand; do not leave a shield or stale weapon there.
                if (m_humanoid.m_leftItem != null && m_humanoid.m_leftItem != desired)
                {
                    m_humanoid.UnequipItem(m_humanoid.m_leftItem, false);
                    leftChanged = true;
                }
            }
            else if (desired.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
            {
                if (m_humanoid.m_leftItem != null)
                {
                    m_humanoid.UnequipItem(m_humanoid.m_leftItem, false);
                    leftChanged = true;
                }
            }
            else if (desired.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
            {
                // A one-handed melee weapon wants the best shield in the left hand, not a bow
                // or another stale left-hand item.
                if (m_humanoid.m_leftItem != null && m_humanoid.m_leftItem.m_shared != null
                    && m_humanoid.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield)
                {
                    m_humanoid.UnequipItem(m_humanoid.m_leftItem, false);
                    leftChanged = true;
                }

                if (m_humanoid.m_leftItem == null)
                {
                    ItemDrop.ItemData shield = ScanBestShield();
                    if (shield != null)
                    {
                        m_humanoid.EquipItem(shield, false);
                        leftChanged = true;
                    }
                }
            }

            bool ammoChanged = false;
            if (IsBowItem(desired))
            {
                ItemDrop.ItemData arrows = GetCurrentBestArrows();
                if (arrows != null && !ReferenceEquals(m_humanoid.m_ammoItem, arrows))
                {
                    m_humanoid.EquipItem(arrows, false);
                    ammoChanged = true;
                }
            }
            else if (m_humanoid.m_ammoItem != null)
            {
                m_humanoid.UnequipItem(m_humanoid.m_ammoItem, false);
                ammoChanged = true;
            }

            if (weaponChanged || leftChanged || ammoChanged)
            {
                ApplyProficiencyScaling();
                if (m_visequip != null)
                {
                    m_visequip.UpdateEquipmentVisuals();
                }
            }

            return desired;
        }

        // ===================== Distress signal (Step 8 / #49h) =====================
        // ZDO-flag distress mechanism, polled by defenders (Step 10) instead of an RPC.
        // The flag lives on this NPC's character ZDO (m_znet.GetZDO()) so it syncs to every
        // client, including cross-owner clients — the only way a poll can reach a
        // cross-owner defender. Owner writes (SetDistress); any client reads.
        private const string kDistressKey = "hmnpc_distress";          // bool: in distress?
        private const string kDistressTimeKey = "hmnpc_distress_time"; // float: Time.time when entered

        // Owner-side: mark this NPC in/out of distress. No-op unless this client owns the
        // NPC (ZDO writes are owner-authoritative). Step 9 will call this from the owner.
        public void SetDistress(bool inDistress)
        {
            if (m_znet == null || m_znet.GetZDO() == null) return;
            if (!m_znet.IsOwner()) return;
            m_znet.GetZDO().Set(kDistressKey, inDistress);
            if (inDistress)
            {
                m_znet.GetZDO().Set(kDistressTimeKey, Time.time);
            }
        }

        // Any client: is this NPC currently in distress? (reads the synced flag)
        public bool IsInDistress()
        {
            if (m_znet == null || m_znet.GetZDO() == null) return false;
            return m_znet.GetZDO().GetBool(kDistressKey);
        }

        // Any client: seconds since this NPC's distress was last asserted (0 if not in
        // distress). Time.time is session-relative; a flag still carrying a PREVIOUS
        // session's timestamp reads as a negative age, which readers treat as stale (see
        // FindNearestDistressed). The owner's Update reconcile clears such flags within one
        // frame of load.
        public float DistressAge()
        {
            if (!IsInDistress()) return 0f;
            return Time.time - m_znet.GetZDO().GetFloat(kDistressTimeKey, 0f);
        }

        // Any client: nearest in-danger hirdmandr (any owner) within maxRange, or null.
        // Uses the shared 1s-TTL NPC list (RefreshNpcList, #84) instead of a per-call
        // FindObjectsOfType object-table scan - at the 0.5s poll cadence that was one full
        // scan per free NPC per poll. Per-NPC state (distress flag, health, position) is
        // still read live, so only discovery is cached.
        public HirdmandrNPC FindNearestDistressed(float maxRange)
        {
            HirdmandrNPC best = null;
            float bestDist = maxRange;
            Vector3 myPos = transform.position;
            RefreshNpcList();
            foreach (HirdmandrNPC other in s_npcList)
            {
                if (other == null || other == this) continue;
                if (!other.IsInDistress()) continue;
                // A non-hirdmandr NPC can never legitimately be in distress: its HirdmandrAI
                // is disabled, and only an enabled AI writes the distress flag. Any true read
                // on such an NPC is an unset/stale key, not a live threat.
                if (!other.m_isHirdmandr) continue;
                // A dead/dying victim is not a valid defense target (the welcomed-NPC death path
                // destroys the ZDO, but the pre-destroy window is real).
                if (other.m_humanoid == null || other.m_humanoid.GetHealth() <= 0f) continue;
                // Stale-flag backstop (see HirdmandrAI.DISTRESS_TTL): a flag this old is
                // broken, not live. Also reject a flag whose timestamp is from a PREVIOUS
                // session: Time.time is session-relative, so a last-session timestamp reads
                // as a large-negative age - stale by definition, even before the owner's
                // first reconcile has cleared it (e.g. the first frame after a load).
                float age = other.DistressAge();
                if (age > HirdmandrAI.DISTRESS_TTL || age < -HirdmandrAI.DISTRESS_CLOCK_TOL) continue;
                float d = Vector3.Distance(myPos, other.transform.position);
                if (d <= bestDist)
                {
                    bestDist = d;
                    best = other;
                }
            }
            return best;
        }

        // Step 12 / #49l — Any client: nearest potential helper (another live hirdmandr, or any
        // live player) within maxRange, or null. A helper is who an overmatched hirdmandr retreats
        // toward (escapeHelp). The hirdmandr half uses the shared 1s-TTL NPC list
        // (RefreshNpcList, #84); the player half stays a direct FindObjectsOfType (the player
        // list is tiny and escape is a rare path). Returns a Transform so the caller can treat a
        // fellow hirdmandr and a player uniformly as "run to this position."
        public Transform FindNearestHelper(float maxRange)
        {
            Vector3 myPos = transform.position;
            float bestDist = maxRange;
            Transform best = null;
            RefreshNpcList();
            foreach (HirdmandrNPC other in s_npcList)
            {
                if (other == null || other == this) continue;
                if (other.m_humanoid == null || other.m_humanoid.GetHealth() <= 0f) continue;
                float d = Vector3.Distance(myPos, other.transform.position);
                if (d <= bestDist)
                {
                    bestDist = d;
                    best = other.transform;
                }
            }
            Player[] players = UnityEngine.Object.FindObjectsOfType<Player>();
            foreach (Player p in players)
            {
                if (p == null || p.GetHealth() <= 0f) continue;
                float d = Vector3.Distance(myPos, p.transform.position);
                if (d <= bestDist)
                {
                    bestDist = d;
                    best = p.transform;
                }
            }
            return best;
        }

        // Step 11 / #49k — L2 defense anchor (§7). Answers "who am I protecting right now?":
        //   a following warrior -> the player it follows (the MonsterAI follow target);
        //   a responder         -> the distressed party member it is running to help (Step 10).
        // Returns null when there is no anchor (neither following nor responding); the caller then
        // keeps pure vanilla targeting. Read only by the owner's MonsterAI (per-client AI), so
        // touching owner-side state (the follow target / m_defendTarget) here is safe.
        public Transform GetDefenseAnchor()
        {
            if (m_monsterai != null)
            {
                GameObject followTarget = m_monsterai.GetFollowTarget();
                if (followTarget != null)
                {
                    return followTarget.transform;
                }
            }
            if (m_hirdmandrAI != null && m_hirdmandrAI.m_defendTarget != null)
            {
                return m_hirdmandrAI.m_defendTarget.transform;
            }
            return null;
        }
    }
}
