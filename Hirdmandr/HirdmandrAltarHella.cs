// Hirdmandr
// Altar to Hella (PMW #62): the resurrection altar.
//
// A stone piece cloned from the Eikthyrnir offering bowl. A player offers a
// hirdmandr_soul and its tribute (1 trophy, 1 bloodbag, 2 greydwarf eyes, 4 raw
// meat) to the altar - one item per UseItem interaction. When the offering is
// complete, any player can perform the ritual (Interact): the soul is
// resurrected as a full NPC that inherits everything the soul encoded (look,
// role, jobs, fighting style, skills), with the performer as the new jarl.
// The resurrected NPC is identity-only (no gear - the soul never carried any).
//
// Multiplayer: the altar's piece ZDO is the single source of truth for the
// offering state (per-item counts + the soul's identity payload, staged in the
// hmnpc_* keys - the same keys the NPC's own ZDO uses, so the copy at
// resurrection is a straight key-for-key transfer). The offering client
// consumes the item locally and RPCs the host; the performer client RPCs the
// host; only the host mutates the ZDO and spawns the NPC. All
// client-visible feedback goes through the local player's message HUD.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Jotunn.Managers;
using UnityEngine;

namespace Hirdmandr
{
    [Serializable]
    public class HirdmandrAltarHella : MonoBehaviour, Interactable, Hoverable
    {
        public ZNetView m_znetv;
        public Piece m_piece;

        // Offering state keys on the altar's ZDO (counts + soul presence).
        public const string KeyTrophy = "hmaltar_trophy";
        public const string KeyBloodbag = "hmaltar_bloodbag";
        public const string KeyEye = "hmaltar_eye";
        public const string KeyMeat = "hmaltar_meat";
        public const string KeySoul = "hmaltar_soul";

        // Tribute tokens (item shared names) - what UseItem matches the held item against.
        private const string TokenTrophy = "$item_trophy_skeleton";
        private const string TokenBloodbag = "$item_bloodbag";
        private const string TokenEye = "$item_greydwarfeye";
        private const string TokenMeat = "$item_boar_meat";

        protected virtual void Awake()
        {
            m_znetv = GetComponent<ZNetView>();
            m_piece = GetComponent<Piece>();

            if (m_znetv != null)
            {
                m_znetv.Register<string>("RPC_HMAltarCount", RPC_HMAltarCount);
                m_znetv.Register<string>("RPC_HMAltarSoul", RPC_HMAltarSoul);
                m_znetv.Register<long>("RPC_HMAltarResurrect", RPC_HMAltarResurrect);
                m_znetv.Register<string>("RPC_HMAltarAnnounce", RPC_HMAltarAnnounce);
            }
        }

        // Hover text: the game finds these by reflection on the hovered interactable (same
        // mechanism as HirdmandrBed / the vanilla Switch), so they are plain public methods,
        // not interface members. Shows the offering state so the player can see what the
        // altar has and when it is ready to perform.
        public string GetHoverName()
        {
            return Localization.instance.Localize("Altar to Hella");
        }

        public float GetHoverOffset()
        {
            // Vanilla pieces return a serialized m_hoverOffset (how far above the object the
            // hover text floats). We add the component at runtime and have no serialized field,
            // so a constant: the bowl sits on the ground, so the prompt stays at its base.
            return 0f;
        }

        public string GetHoverText()
        {
            if (m_znetv == null || m_znetv.GetZDO() == null)
            {
                return Localization.instance.Localize("Offer: 1 soul, 1 trophy, 1 bloodbag, 2 greydwarf eyes, 4 raw meat.");
            }
            if (OfferingComplete())
            {
                return Localization.instance.Localize("The offering is complete.\n[<color=yellow><b>$KEY_Use</b></color>] Perform the resurrection");
            }
            // Dynamic: show the live offering state (have/need) so the player sees progress as
            // they offer. Localize is still applied so the $KEY_Use token resolves to the key.
            ZDO zdo = m_znetv.GetZDO();
            string status = "Soul " + (zdo.GetBool(KeySoul) ? 1 : 0) + "/1"
                + "   Trophy " + zdo.GetLong(KeyTrophy, 0) + "/1"
                + "   Bloodbag " + zdo.GetLong(KeyBloodbag, 0) + "/1"
                + "   Eyes " + zdo.GetLong(KeyEye, 0) + "/2"
                + "   Meat " + zdo.GetLong(KeyMeat, 0) + "/4";
            return Localization.instance.Localize(status + "\n[<color=yellow><b>$KEY_Use</b></color>] Hold an offering and use it on the altar");
        }

        // === UseItem: offering path ===
        // Called by the game when the player presses interact while holding an item.
        // A COMPLETE altar performs the ritual (the held item is never consumed); otherwise
        // a soul/tribute item is offered (one consumed, staged on the host's ZDO).
        public bool UseItem(Humanoid humanoid, ItemDrop.ItemData itemData)
        {
            if (humanoid == null || itemData == null || m_znetv == null || m_znetv.GetZDO() == null)
            {
                return false;
            }
            Player player = humanoid.GetComponent<Player>();
            if (player == null)
            {
                return false;
            }

            if (OfferingComplete())
            {
                RequestResurrect(player);
                return true;
            }

            bool isSoul = itemData.m_customData != null && itemData.m_customData.ContainsKey(SoulItem.SoulTagKey);
            string countKey = null;
            if (!isSoul)
            {
                string token = (itemData.m_shared != null) ? itemData.m_shared.m_name : "";
                switch (token)
                {
                    case TokenTrophy: countKey = KeyTrophy; break;
                    case TokenBloodbag: countKey = KeyBloodbag; break;
                    case TokenEye: countKey = KeyEye; break;
                    case TokenMeat: countKey = KeyMeat; break;
                }
            }
            if (!isSoul && countKey == null)
            {
                TellLocal("This does not belong at the altar.");
                return false;
            }

            // Don't take more than the altar needs: reject (WITHOUT consuming) once this
            // offering is already satisfied. The owner's cap in ApplyCountLocal / ApplySoulLocal
            // is the authority; this is the local UX gate so a player's surplus isn't eaten.
            ZDO zdo = m_znetv.GetZDO();
            if (isSoul)
            {
                if (zdo.GetBool(KeySoul))
                {
                    TellLocal("A soul has already been offered.");
                    return false;
                }
            }
            else if (zdo.GetLong(countKey, 0) >= MaxCount(countKey))
            {
                TellLocal("Enough of " + OfferingName(countKey) + " has been offered.");
                return false;
            }

            // Consume exactly one from the local inventory: the offer is only real if the item is.
            ItemDrop.ItemData found = humanoid.m_inventory.GetItem(itemData.m_shared.m_name);
            if (found == null)
            {
                TellLocal("You have nothing to offer.");
                return false;
            }
            humanoid.m_inventory.RemoveOneItem(found);

            LogDiagnostic("UseItem offerer=" + player.GetPlayerID() + " isSoul=" + isSoul);

            // The session/owner is the single authority for the offering state. The offerer
            // consumes the item from their own inventory (local); the ZDO change is made by
            // the owner: if I am the owner I write it directly, otherwise I RPC the owner.
            // (InvokeRPC is never delivered back to its target/sender, so a non-owner must
            // ask the owner - and the owner's ZDO write auto-syncs to every client.)
            if (isSoul)
            {
                // Soul: transfer the identity payload (whitelisted hmnpc_* keys only).
                string payload = BuildSoulPayload(itemData.m_customData);
                Jotunn.Logger.LogInfo("[soultrace] UseItem: customData.count=" + (itemData.m_customData != null ? itemData.m_customData.Count.ToString() : "null")
                    + " customData.name='" + (itemData.m_customData != null && itemData.m_customData.ContainsKey("hmnpc_name") ? itemData.m_customData["hmnpc_name"] : "<absent>")
                    + " payload.len=" + payload.Length);
                if (m_znetv.IsOwner())
                {
                    ApplySoulLocal(payload);
                }
                else
                {
                    m_znetv.InvokeRPC("RPC_HMAltarSoul", payload);
                }
                TellLocal("You place the soul into the altar.");
            }
            else
            {
                if (m_znetv.IsOwner())
                {
                    ApplyCountLocal(countKey);
                }
                else
                {
                    m_znetv.InvokeRPC("RPC_HMAltarCount", countKey);
                }
                TellLocal("You offer " + OfferingName(countKey) + " to the altar.");
            }
            LogDiagnostic("UseItem after offer");
            return true;
        }

        // === Interact: resurrection trigger path ===
        // Called when the player presses interact NOT holding an accepted item (or via the
        // UseItem fallback). A complete altar performs the ritual; an incomplete one reports
        // what the offering needs.
        public bool Interact(Humanoid humanoid, bool hold, bool alt)
        {
            if (humanoid == null || m_znetv == null || m_znetv.GetZDO() == null)
            {
                return false;
            }
            if (hold)
            {
                return false;
            }
            Player player = humanoid.GetComponent<Player>();
            if (player == null)
            {
                return false;
            }

            LogDiagnostic("Interact");

            if (OfferingComplete())
            {
                RequestResurrect(player);
                return true;
            }

            TellLocal("The offering is incomplete.\n1 soul, 1 trophy, 1 bloodbag, 2 greydwarf eyes, 4 raw meat.");
            return true;
        }

        // Performer-side: request the ritual on the host (single authority). The host re-checks
        // the offering against its own ZDO before spawning, so a stale/desynced client view
        // can never trigger a resurrection on an incomplete altar.
        private void RequestResurrect(Player player)
        {
            long performer = player.GetPlayerID();
            TellLocal("The ritual begins...");
            // Same authority model as UseItem: the owner runs the ritual (locally if I am the
            // owner, else via RPC). The owner re-checks the offering against its own ZDO
            // before spawning, so a stale client view can never trigger it.
            if (m_znetv.IsOwner())
            {
                DoResurrect(performer);
            }
            else
            {
                m_znetv.InvokeRPC("RPC_HMAltarResurrect", performer);
            }
        }

        // Host-side: run the ritual. Spawns the NPC, inherits the staged identity, clears the
        // offering, plays the MP effect, and announces to every client.
        public void DoResurrect(long performerID)
        {
            if (m_znetv == null || !m_znetv.IsOwner())
            {
                return;
            }
            ZDO zdo = m_znetv.GetZDO();
            if (zdo == null || !OfferingComplete(zdo))
            {
                Jotunn.Logger.LogInfo("[HirdmandrAltar] DoResurrect: offering incomplete - aborting (stale client view or double trigger).");
                return;
            }

            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                Jotunn.Logger.LogError("[HirdmandrAltar] DoResurrect: no ZNetScene - aborting (offering preserved).");
                return;
            }
            GameObject prefab = scene.GetPrefab("npc_entity");
            if (prefab == null)
            {
                Jotunn.Logger.LogError("[HirdmandrAltar] DoResurrect: 'npc_entity' prefab not registered - aborting (offering preserved).");
                return;
            }

            // Ownership: the local session owns the NPC ZDO so OwnerInit runs the owner (load)
            // branch - exactly like RescueSpawner. On the host the local player is the host.
            Player local = Player.m_localPlayer;
            ZDO ownerZdo = null;
            if (local != null && local.m_nview != null)
            {
                ownerZdo = local.m_nview.GetZDO();
            }
            if (ownerZdo == null)
            {
                Jotunn.Logger.LogError("[HirdmandrAltar] DoResurrect: could not resolve the session owner - aborting (offering preserved).");
                return;
            }

            Vector3 pos = PickSpawnPos();
            Quaternion rot = transform.rotation;
            int prefabHash = scene.GetPrefabHash(prefab);

            ZDO npcZdo = ZDOMan.instance.CreateNewZDO(pos, prefabHash);
            npcZdo.SetPrefab(prefabHash);
            npcZdo.SetRotation(rot);
            npcZdo.SetOwner(ownerZdo.GetOwner());

            // Inherit: the NPC gets exactly what the soul encoded (staged on the altar ZDO),
            // with the performer as the new jarl and the rescued/hirdmandr flags on. OwnerInit
            // then takes the load path (name != "") and never randomizes anything.
            CopyIdentityTo(npcZdo);
            npcZdo.Set("hmnpc_jarlZOID", performerID);
            // The display name follows the ZOID: the soul carried the old Jarl's name, the
            // performer takes over (the manage GUI's "Jarl: " line reads this key).
            npcZdo.Set("hmnpc_jarlName", PerformerName(performerID));
            npcZdo.Set("hmnpc_isrescued", true);
            npcZdo.Set("hmnpc_isHirdmandr", true);

            GameObject instance = scene.CreateObject(npcZdo);
            if (instance == null)
            {
                Jotunn.Logger.LogError("[HirdmandrAltar] DoResurrect: CreateObject returned null - aborting (offering preserved).");
                return;
            }
            scene.AddInstance(npcZdo, instance.GetComponent<ZNetView>());

            string name = zdo.GetString("hmnpc_name");

            // The effect bus lives on the NPC (owner-local play + broadcast to every other
            // client). FireMpEffect is a no-op on non-owner clients, and this runs on the host,
            // so exactly one client fires it.
            HirdmandrNPC npc = instance.GetComponent<HirdmandrNPC>();
            if (npc != null)
            {
                npc.FireMpEffect(HMEffectKind.Resurrect, name);
            }

            // The offering is consumed: clear the staging area (counts + identity) so the altar
            // can hold the next soul. Done AFTER the NPC exists, so a failure above never eats the
            // tribute.
            ClearOffering(zdo);

            // Announce to every client (owner-local message + broadcast; InvokeRPC is not
            // delivered back to the sender).
            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, name + " has returned from Hella.");
            }
            m_znetv.InvokeRPC(performerID, "RPC_HMAltarAnnounce", name);

            Jotunn.Logger.LogInfo("[HirdmandrAltar] resurrected '" + name + "' (jarl " + performerID + ") at " + pos.ToString("F1") + ".");
        }

        // Display name of the performer for the resurrected NPC's Jarl line. Host-side: every
        // connected player's Player object exists on the host. Empty if the performer is gone
        // (the label hides while empty - same rule as a never-welcomed NPC).
        private static string PerformerName(long performerID)
        {
            foreach (Player p in Player.s_players)
            {
                if (p != null && p.GetPlayerID() == performerID)
                {
                    return p.GetPlayerName();
                }
            }
            return "";
        }

        // === RPC handlers (run on every client; the body is owner-gated) ===

        public void RPC_HMAltarCount(long sender, string key)
        {
            if (m_znetv == null || !m_znetv.IsOwner())
            {
                return;
            }
            ApplyCountLocal(key);
        }

        // Owner-authoritative: increment one tribute count (capped). The owner calls this
        // directly for a local offer, or it is reached via RPC for a remote offer. The ZDO
        // write auto-syncs to every client.
        private void ApplyCountLocal(string key)
        {
            ZDO zdo = m_znetv.GetZDO();
            if (zdo == null)
            {
                return;
            }
            // Cap at the required amount so a spammed client cannot bloat the counts.
            long cur = zdo.GetLong(key, 0);
            if (cur < MaxCount(key))
            {
                zdo.Set(key, cur + 1);
            }
        }

        public void RPC_HMAltarSoul(long sender, string payload)
        {
            if (m_znetv == null || !m_znetv.IsOwner())
            {
                return;
            }
            ApplySoulLocal(payload);
        }

        // Owner-authoritative: stage the soul's identity payload. The owner calls this directly
        // for a local offer, or it is reached via RPC for a remote offer. The ZDO write
        // auto-syncs to every client.
        private void ApplySoulLocal(string payload)
        {
            ZDO zdo = m_znetv.GetZDO();
            if (zdo == null || string.IsNullOrEmpty(payload))
            {
                return;
            }
            // payload = "key \u0001 value \u0001 key \u0001 value ..." (flat pairs). Each value is
            // parsed by type and staged on the altar ZDO; malformed/unknown keys are dropped.
            string[] parts = payload.Split('\u0001');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                ApplyStagedValue(zdo, parts[i], parts[i + 1]);
            }
            Jotunn.Logger.LogInfo("[soultrace] ApplySoulLocal: parts=" + parts.Length + " altar.name='" + zdo.GetString("hmnpc_name") + "'");
            zdo.Set(KeySoul, true);
        }

        public void RPC_HMAltarResurrect(long sender, long performerID)
        {
            if (m_znetv != null && m_znetv.IsOwner())
            {
                DoResurrect(performerID);
            }
        }

        public void RPC_HMAltarAnnounce(long sender, string name)
        {
            if (m_znetv != null && m_znetv.IsOwner())
            {
                return;
            }
            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, name + " has returned from Hella.");
            }
        }

        // === Offering state (read from the ZDO - synced to every client) ===

        public bool OfferingComplete()
        {
            return (m_znetv != null && m_znetv.GetZDO() != null) && OfferingComplete(m_znetv.GetZDO());
        }

        private static bool OfferingComplete(ZDO zdo)
        {
            return zdo.GetLong(KeyTrophy, 0) >= 1
                && zdo.GetLong(KeyBloodbag, 0) >= 1
                && zdo.GetLong(KeyEye, 0) >= 2
                && zdo.GetLong(KeyMeat, 0) >= 4
                && zdo.GetBool(KeySoul);
        }

        private static int MaxCount(string key)
        {
            switch (key)
            {
                case KeyTrophy: return 1;
                case KeyBloodbag: return 1;
                case KeyEye: return 2;
                case KeyMeat: return 4;
            }
            return 1;
        }

        private static string OfferingName(string key)
        {
            switch (key)
            {
                case KeyTrophy: return "Trophy Skeleton";
                case KeyBloodbag: return "Bloodbag";
                case KeyEye: return "Greydwarf Eye";
                case KeyMeat: return "Boar Meat";
            }
            return "an offering";
        }

        // Clear the whole staging area: tribute counts, soul presence, and every identity key
        // back to its default (so a second soul starts from a clean slate).
        private void ClearOffering(ZDO zdo)
        {
            zdo.Set(KeyTrophy, 0L);
            zdo.Set(KeyBloodbag, 0L);
            zdo.Set(KeyEye, 0L);
            zdo.Set(KeyMeat, 0L);
            zdo.Set(KeySoul, false);
            foreach (string key in IdentityKeys())
            {
                if (IsSkillEnable(key))
                {
                    zdo.Set(key, false);
                }
                else if (IsSkillValue(key))
                {
                    zdo.Set(key, 0f);
                }
                else
                {
                    switch (key)
                    {
                        case "hmnpc_name":
                        case "hmnpc_beard":
                        case "hmnpc_hair":
                        case "hmnpc_jarlName":
                            zdo.Set(key, "");
                            break;
                        case "hmnpc_haircolor":
                        case "hmnpc_skincolor":
                            zdo.Set(key, Vector3.one);
                            break;
                        case RescueLedger.HashKey:
                            zdo.Set(key, 0);
                            break;
                        case "hmnpc_jarlZOID":
                            zdo.Set(key, 0L);
                            break;
                        default:
                            zdo.Set(key, false);
                            break;
                    }
                }
            }
        }

        // === Identity: soul payload -> altar ZDO -> NPC ZDO ===
        // The exact keys a soul carries (see SoulItem.SpawnSoulFromNpc), plus the skill
        // value/enable pairs from the HMSkills registry. These are the ONLY keys the altar
        // stages and the ONLY keys it copies to the resurrected NPC - the effective
        // whitelist. (thegnNightshift/thegnGuardSet/ownedbed are read by ZDOLoadGeneral but
        // never carried by a soul, so they load at their defaults - the same as a fresh NPC.)

        private static string[] IdentityKeys()
        {
            var keys = new List<string>
            {
                "hmnpc_name", "hmnpc_female", "hmnpc_haircolor", "hmnpc_skincolor",
                "hmnpc_beard", "hmnpc_hair", RescueLedger.HashKey, "hmnpc_jarlZOID",
                "hmnpc_jarlName",
                "hmnpc_roleArtisan", "hmnpc_roleWarrior", "hmnpc_jobThegn",
                "hmnpc_thegnDayshift", "hmnpc_thegnNightshift", "hmnpc_thegnGuardSet",
                "hmnpc_jobHimthiki",
                "hmnpc_fightingStyleDefense", "hmnpc_fightingStyleOffense",
                "hmnpc_jobGatherer", "hmnpc_fightingRangeClose", "hmnpc_fightingRangeMid",
                "hmnpc_fightingRangeFar"
            };
            foreach (string skill in HMSkills.m_hmSkillNames)
            {
                keys.Add("hmnpc_skill" + skill);
                keys.Add("hmnpc_skill" + skill + "enable");
            }
            return keys.ToArray();
        }

        private static bool IsSkillValue(string key)
        {
            return key.StartsWith("hmnpc_skill");
        }

        private static bool IsSkillEnable(string key)
        {
            return key.StartsWith("hmnpc_skill") && key.EndsWith("enable");
        }

        // Copy the staged identity from the altar ZDO onto a new NPC ZDO (typed, with the same
        // defaults the NPC's own load path uses). Called host-side right before CreateObject.
        private void CopyIdentityTo(ZDO to)
        {
            ZDO from = m_znetv.GetZDO();
            foreach (string key in IdentityKeys())
            {
                if (IsSkillEnable(key))
                {
                    to.Set(key, from.GetBool(key, false));
                    continue;
                }
                if (IsSkillValue(key))
                {
                    to.Set(key, from.GetFloat(key, 0f));
                    continue;
                }
                switch (key)
                {
                    case "hmnpc_name":
                    case "hmnpc_beard":
                    case "hmnpc_hair":
                        to.Set(key, from.GetString(key));
                        break;
                    case "hmnpc_jarlName":
                        to.Set(key, from.GetString(key, ""));
                        break;
                    case "hmnpc_haircolor":
                    case "hmnpc_skincolor":
                        to.Set(key, from.GetVec3(key, Vector3.one));
                        break;
                    case RescueLedger.HashKey:
                        to.Set(key, from.GetInt(key, 0));
                        break;
                    case "hmnpc_jarlZOID":
                        to.Set(key, from.GetLong(key, 0L));
                        break;
                    default:
                        to.Set(key, from.GetBool(key, false));
                        break;
                }
            }
        }

        // Stage one soul-payload value on the altar ZDO, parsed by the key's type. Unknown or
        // malformed keys are dropped (the whitelist is enforced at copy time anyway: only
        // IdentityKeys() ever reach the NPC).
        private void ApplyStagedValue(ZDO zdo, string key, string raw)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith("hmnpc_"))
            {
                return;
            }
            try
            {
                if (IsSkillEnable(key))
                {
                    zdo.Set(key, ParseBool(raw));
                    return;
                }
                if (IsSkillValue(key))
                {
                    zdo.Set(key, float.Parse(raw, CultureInfo.InvariantCulture));
                    return;
                }
                switch (key)
                {
                    case "hmnpc_name":
                    case "hmnpc_beard":
                    case "hmnpc_hair":
                    case "hmnpc_jarlName":
                        zdo.Set(key, raw ?? "");
                        return;
                    case "hmnpc_haircolor":
                    case "hmnpc_skincolor":
                        zdo.Set(key, ParseV3(raw, Vector3.one));
                        return;
                    case RescueLedger.HashKey:
                        zdo.Set(key, int.Parse(raw, CultureInfo.InvariantCulture));
                        return;
                    case "hmnpc_jarlZOID":
                        zdo.Set(key, long.Parse(raw, CultureInfo.InvariantCulture));
                        return;
                    default:
                        // Any other hmnpc_* key the soul carried: stage as a bool (matches the
                        // soul's "1"/"0" encoding). Only IdentityKeys() are ever copied on.
                        zdo.Set(key, ParseBool(raw));
                        return;
                }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogInfo("[HirdmandrAltar] dropped malformed soul value for '" + key + "': " + e.Message);
            }
        }

        // Offering client: serialize the soul's custom-data payload to the flat
        // "key \u0001 value \u0001 ..." RPC string. Only hmnpc_* keys travel (the soul tag and
        // anything else are not identity).
        private static string BuildSoulPayload(Dictionary<string, string> customData)
        {
            var sb = new StringBuilder();
            if (customData == null)
            {
                return "";
            }
            foreach (KeyValuePair<string, string> kv in customData)
            {
                if (kv.Key == SoulItem.SoulTagKey)
                {
                    continue;
                }
                if (!kv.Key.StartsWith("hmnpc_"))
                {
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.Append('\u0001');
                }
                sb.Append(kv.Key).Append('\u0001').Append(kv.Value ?? "");
            }
            return sb.ToString();
        }

        private static bool ParseBool(string s)
        {
            return s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static Vector3 ParseV3(string s, Vector3 fallback)
        {
            try
            {
                string[] p = (s ?? "").Split(',');
                if (p.Length == 3)
                {
                    return new Vector3(
                        float.Parse(p[0], CultureInfo.InvariantCulture),
                        float.Parse(p[1], CultureInfo.InvariantCulture),
                        float.Parse(p[2], CultureInfo.InvariantCulture));
                }
            }
            catch (System.Exception)
            {
            }
            return fallback;
        }

        // The resurrected NPC stands a few metres in front of the altar, grounded (same
        // raycast heuristic as RescueSpawner.PickClearSpot, without the random offset).
        private Vector3 PickSpawnPos()
        {
            Vector3 p = transform.position + transform.forward * 3f;
            RaycastHit hit;
            Vector3 origin = p + Vector3.up * 6f;
            if (Physics.Raycast(origin, Vector3.down, out hit, 12f, Physics.DefaultRaycastLayers) && hit.distance <= 8f)
            {
                p = hit.point + Vector3.up * 0.5f;
            }
            return p;
        }

        private void TellLocal(string text)
        {
            if (Player.m_localPlayer != null)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, text);
            }
        }

        // TEMP diagnostic (remove once the owner + counts are confirmed in-game): logs who
        // owns the altar ZDO and the current offering state, to validate the authority model.
        private void LogDiagnostic(string context)
        {
            if (m_znetv == null || m_znetv.GetZDO() == null)
            {
                Jotunn.Logger.LogInfo("[HirdmandrAltar][diag] " + context + " : no znetv/zdo");
                return;
            }
            ZDO zdo = m_znetv.GetZDO();
            long owner = 0;
            try
            {
                owner = zdo.GetOwner();
            }
            catch (System.Exception)
            {
            }
            long local = (Player.m_localPlayer != null) ? Player.m_localPlayer.GetPlayerID() : -1;
            Jotunn.Logger.LogInfo("[HirdmandrAltar][diag] " + context + " : IsOwner=" + m_znetv.IsOwner()
                + " owner=" + owner
                + " session=" + ZDOMan.GetSessionID()
                + " local=" + local
                + " | trophy=" + zdo.GetLong(KeyTrophy, 0)
                + " bloodbag=" + zdo.GetLong(KeyBloodbag, 0)
                + " eye=" + zdo.GetLong(KeyEye, 0)
                + " meat=" + zdo.GetLong(KeyMeat, 0)
                + " soul=" + zdo.GetBool(KeySoul));
        }
    }
}
