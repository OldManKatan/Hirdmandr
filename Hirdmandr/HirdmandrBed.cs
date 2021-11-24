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

            On.Bed.GetHoverText += OnGetHoverText;
            On.Bed.GetHoverName += OnGetHoverName;
        }

        protected virtual void Update() { }

        protected virtual void FixedUpdate() { }

        private static string OnGetHoverText(On.Bed.orig_GetHoverText orig, Bed self)
        {
            if (self.TryGetComponent<HirdmandrBed>(out var HirdmandrComp))
            {
                return HirdmandrComp.GetHoverText();
            }
            return orig(self);
        }

        private static string OnGetHoverName(On.Bed.orig_GetHoverName orig, Bed self)
        {
            if (self.TryGetComponent<HirdmandrBed>(out var HirdmandrComp))
            {
                return HirdmandrComp.GetHoverName();
            }
            return orig(self);
        }

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
            List<Piece> nearbyPieces = SE_Rested.GetNearbyPieces(transform.position);
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
                if (ownerZDOID == ZDOID.None)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public bool Claim(ZDOID owner_id)
        {
            if (checkOwnership())
            {
                if (ownerZDOID == ZDOID.None)
                {
                    ownerZDOID = owner_id;
                    m_znetv.GetZDO().Set("hmnpc_bedOwnerZDOID", owner_id);
                    return true;
                }
            }
            return false;
        }

        public bool UnClaim(ZDOID owner_id)
        {
            if (checkOwnership())
            {
                if (ownerZDOID == owner_id)
                {
                    ownerZDOID = ZDOID.None;
                    m_znetv.GetZDO().Set("hmnpc_bedOwnerZDOID", ZDOID.None);
                    return true;
                }
            }
            return false;
        }

        public void RemoveOwner()
        {
            if (checkOwnership())
            {
                ownerZDOID = ZDOID.None;
                m_znetv.GetZDO().Set("hmnpc_bedOwnerZDOID", ZDOID.None);
            }
        }
    }
}
