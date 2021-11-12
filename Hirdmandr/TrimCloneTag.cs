using Jotunn.Managers;
using System;
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

    }
}