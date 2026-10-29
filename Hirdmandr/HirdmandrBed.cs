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
    public class HirdmandrBed : MonoBehaviour
    {
        public ZNetView m_znetv;
        public Piece m_piece;
        public Bed m_bed;

        public int comfortAtBed;
        public ZDOID ownerZDOID;
        public bool wasOwner;
        
        protected virtual void Awake()
        {
            m_znetv = GetComponent<ZNetView>();
            m_piece = GetComponent<Piece>();
            m_bed = GetComponent<Bed>();

            ownerZDOID = ZDOID.None;
            wasOwner = false;
        }

        private float m_lastOwnerCheck = -60f;

        protected virtual void Update()
        {
            // Recurring stale-claim sweep: a deleted/dead owner leaves the bed claimed forever
            // unless we release it here (the NPC side has no reliable death/delete hook).
            if (Time.time - m_lastOwnerCheck >= 30f)
            {
                m_lastOwnerCheck = Time.time;
                hasOwner();
            }
        }

        protected virtual void FixedUpdate() { }

        public string GetHoverText()
        {
            string ownerName = m_bed.GetOwnerName();
            if (ownerName == "")
            {
                return Localization.instance.Localize("NPC $piece_bed_unclaimed\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_claim");
            }
            string text = ownerName + "'s NPC $piece_bed";
            if (m_bed.IsMine())
            {
                if (m_bed.IsCurrent())
                {
                    return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_sleep");
                }
                return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_setspawn");
            }
            return Localization.instance.Localize(text);
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize("NPC $piece_bed");
        }

        public bool checkOwnership()
        {
            if (m_znetv.IsOwner())
            {
                if (!wasOwner)
                {
                    ownerZDOID = m_znetv.GetZDO().GetZDOID("hmnpc_bedOwnerZDOID");
                    wasOwner = true;
                }
                return true;
            }
            if (wasOwner)
            {
                m_znetv.GetZDO().Set("hmnpc_bedOwnerZDOID", ownerZDOID);
                wasOwner = false;
            }
            return false;
        }
        
        public int GetComfortAtBed()
        {
            List<Piece> nearbyPieces = SE_Rested.GetNearbyComfortPieces(transform.position);
            nearbyPieces.Sort(SE_Rested.PieceComfortSort);
            int num = 1;
            // Check for shelter here
            num++;
            for (int i = 0; i < nearbyPieces.Count; i++)
            {
                Piece piece = nearbyPieces[i];
                if (i > 0)
                {
                    Piece piece2 = nearbyPieces[i - 1];
                    if ((piece.m_comfortGroup != 0 && piece.m_comfortGroup == piece2.m_comfortGroup) || piece.m_name == piece2.m_name)
                    {
                        continue;
                    }
                }
                num += piece.GetComfort();
            }
            Jotunn.Logger.LogInfo("GetComfortAtBed() found a comfort of " + num);
            return num;
        }

        public bool hasOwner()
        {
            if (checkOwnership())
            {
                if (ownerZDOID != ZDOID.None)
                {
                    // The owner NPC may have been deleted (its ZDO gone) or died. Either way the
                    // claim is stale: clear it in memory AND persist None so the bed becomes free.
                    Character ownerChar = null;
                    var ownerZDO = ZDOMan.instance.GetZDO(ownerZDOID);
                    if (ownerZDO != null)
                    {
                        var ownerGO = ZNetScene.instance.FindInstance(ownerZDO);
                        ownerChar = (ownerGO != null) ? ownerGO.GetComponent<Character>() : null;
                    }
                    if (ownerChar == null || ownerChar.GetHealth() <= 0f || ownerChar.IsDead())
                    {
                        // Owner is gone/dead: free the bed AND its NPC's ref (bi-directional sync)
                        // so the claim does not dangle on a dead owner.
                        ClearOwnerAndNPC(ownerZDOID);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Claim(ZDOID owner_id)
        {
            if (checkOwnership())
            {
                // Authoritative gate: read the owner from the ZDO, never the in-memory field, so a
                // desynced in-memory value can never let a second NPC claim an already-owned bed.
                if (m_znetv.GetZDO().GetZDOID("hmnpc_bedOwnerZDOID") == ZDOID.None)
                {
                    ownerZDOID = owner_id;
                    m_znetv.GetZDO().Set("hmnpc_bedOwnerZDOID", owner_id);
                    // Jotunn.Logger.LogWarning("HirdmandrBed.Claim: " + owner_id + " claimed the bed.");
                    return true;
                }
                Jotunn.Logger.LogWarning("HirdmandrBed.Claim REFUSED for " + owner_id
                    + ": bed already owned by " + m_znetv.GetZDO().GetZDOID("hmnpc_bedOwnerZDOID"));
            }
            return false;
        }

        public bool UnClaim(ZDOID owner_id)
        {
            if (checkOwnership())
            {
                if (ownerZDOID == owner_id)
                {
                    // Free the bed AND the owning NPC's ref (bi-directional sync).
                    ClearOwnerAndNPC(owner_id);
                    return true;
                }
            }
            return false;
        }

        public void RemoveOwner()
        {
            if (checkOwnership())
            {
                // Free the bed AND the owning NPC's ref (bi-directional sync) so the two sides
                // of the claim never desync.
                ClearOwnerAndNPC(ownerZDOID);
            }
        }

        // Clear the bed's owner (memory + ZDO) AND the owning NPC's ownedBedZDOID, so the two
        // sides of the claim can never desync. A bed freed while its NPC still points at it is
        // exactly how two NPCs end up in the same bed: the orphaned NPC walks to a bed it no
        // longer owns while another NPC legitimately re-claims the now-free bed.
        public void ClearOwnerAndNPC(ZDOID owner_id)
        {
            ownerZDOID = ZDOID.None;
            m_znetv.GetZDO().Set("hmnpc_bedOwnerZDOID", ZDOID.None);
            var ownerZDO = ZDOMan.instance.GetZDO(owner_id);
            if (ownerZDO != null)
            {
                var ownerGO = ZNetScene.instance.FindInstance(ownerZDO);
                var ownerNPC = (ownerGO != null) ? ownerGO.GetComponent<HirdmandrNPC>() : null;
                if (ownerNPC != null)
                {
                    Jotunn.Logger.LogWarning("HirdmandrBed: freeing owner " + owner_id
                        + " and its NPC-side bed ref (bi-directional sync).");
                    ownerNPC.ClearOwnedBedRef();
                }
            }
        }
    }
}
