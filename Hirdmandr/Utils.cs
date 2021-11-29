using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hirdmandr
{
    public static class Utils
    {
        public static string TrimCloneTag(this string s)
        {
            if (!s.EndsWith("(Clone)"))
            {
                return s;
            }
            return s.Substring(0, s.Length - "(Clone)".Length);
        }

        public static EffectList CloneEffectList(EffectList elPP)
        {
            EffectList effectList = new EffectList();
            effectList.m_effectPrefabs = new EffectList.EffectData[elPP.m_effectPrefabs.Length];
            for (int i = 0; i < elPP.m_effectPrefabs.Length; i++)
            {
                EffectList.EffectData effectData = elPP.m_effectPrefabs[i];
                effectList.m_effectPrefabs[i] = new EffectList.EffectData
                {
                    m_prefab = effectData.m_prefab,
                    m_enabled = effectData.m_enabled,
                    m_attach = effectData.m_attach,
                    m_inheritParentRotation = effectData.m_inheritParentRotation,
                    m_inheritParentScale = effectData.m_inheritParentScale,
                    m_randomRotation = effectData.m_randomRotation,
                    m_scale = effectData.m_scale
                };
            }
            return effectList;
        }

        public static GameObject DesignSfxDeepen(string originalName, string newName)
        {
            bool alreadyExisted = false;
            GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab(newName, originalName);
            if (alreadyExisted)
            {
                return gameObject;
            }
            ZSFX component = gameObject.GetComponent<ZSFX>();
            component.m_minPitch = 0.8f;
            component.m_maxPitch = 0.9f;
            return gameObject;
        }

        public static List<ZDO> GetAllZDOsInRange(Vector3 point, float range)
        {
            int MinX = (int)(point.x - range);
            int MinZ = (int)(point.z - range);
            int MaxX = (int)(point.x + range);
            int MaxZ = (int)(point.z + range);

            // Get sectors to check
            List<Vector2i> sectors;
            sectors = GetSectors(MinX, MinZ, MaxX, MaxZ);

            // Get zdo's
            var foundZDOs = new List<ZDO>();

            foreach (var sector in sectors)
            {
                ZDOMan.instance.FindObjects(sector, foundZDOs);
            }

            return foundZDOs;
        }

        public static List<ZDO> GetPrefabZDOsInRange(Vector3 point, float range, string prefabName)
        {
            int prefabStableHashCode = prefabName.GetStableHashCode();

            var foundZDOs = GetAllZDOsInRange(point, range);

            var matchedZDOs = new List<ZDO>();

            foreach (ZDO aZDO in foundZDOs)
            {
                if (aZDO.GetPrefab() == prefabStableHashCode)
                {
                    matchedZDOs.Add(aZDO);
                }
            }

            return matchedZDOs;
        }

        private static List<Vector2i> GetSectors(int minX, int minZ, int maxX, int maxZ)
        {
            List<Vector2i> sectors = new List<Vector2i>();

            int stepMinX = Zonify(minX);
            int stepMaxX = Zonify(maxX);

            int stepMinZ = Zonify(minZ);
            int stepMaxZ = Zonify(maxZ);

            for (int x = stepMinX; x <= stepMaxX; ++x)
            {
                for (int z = stepMinZ; z <= stepMaxZ; ++z)
                {
                    sectors.Add(new Vector2i(x, z));
                }
            }

            return sectors;

            int Zonify(int coordinate)
            {
                return Mathf.FloorToInt((coordinate + 32) / 64f);
            }
        }
    }
}