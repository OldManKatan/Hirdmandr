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
    public class DebugMAI : MonoBehaviour
    {
        public MonsterAI mai;
        public Humanoid hum;
        public ItemDrop.ItemData item;

        protected virtual void Awake()
        {
            mai = GetComponent<MonsterAI>();
            hum = GetComponent<Humanoid>();

            Invoke("PrintMAI", 3f);
        }

        public void PrintMAI()
        {
            Jotunn.Logger.LogError("==== " + mai.gameObject.name + " ====");

            Jotunn.Logger.LogError("Shared Data");

			item = hum.GetCurrentWeapon();

			var sh = item.m_shared;
			Jotunn.Logger.LogError("  m_name = " + sh.m_name);
			Jotunn.Logger.LogError("  m_itemType = " + sh.m_itemType.ToString());

			Jotunn.Logger.LogError("TextArea");
			Jotunn.Logger.LogError("  m_description = " + sh.m_description);
			Jotunn.Logger.LogError("  m_maxStackSize = " + sh.m_maxStackSize);
			Jotunn.Logger.LogError("  m_maxQuality = " + sh.m_maxQuality);
			Jotunn.Logger.LogError("  m_weight = " + sh.m_weight);
			Jotunn.Logger.LogError("  m_equipDuration = " + sh.m_equipDuration);
			Jotunn.Logger.LogError("  m_variants = " + sh.m_variants);
			Jotunn.Logger.LogError("  m_setName = " + sh.m_setName);
			Jotunn.Logger.LogError("  m_setSize = " + sh.m_setSize);
			Jotunn.Logger.LogError("  m_movementModifier = " + sh.m_movementModifier);
			Jotunn.Logger.LogError("Armor settings");
			Jotunn.Logger.LogError("  m_armor = " + sh.m_armor);
			Jotunn.Logger.LogError("  m_armorPerLevel = " + sh.m_armorPerLevel);
			Jotunn.Logger.LogError("  m_damageModifiers = " + sh.m_damageModifiers);
			Jotunn.Logger.LogError("Shield settings");
			Jotunn.Logger.LogError("  m_blockPower = " + sh.m_blockPower);
			Jotunn.Logger.LogError("  m_blockPowerPerLevel = " + sh.m_blockPowerPerLevel);
			Jotunn.Logger.LogError("  m_deflectionForce = " + sh.m_deflectionForce);
			Jotunn.Logger.LogError("  m_deflectionForcePerLevel = " + sh.m_deflectionForcePerLevel);
			Jotunn.Logger.LogError("  m_timedBlockBonus = " + sh.m_timedBlockBonus);
			Jotunn.Logger.LogError("Weapon");
			Jotunn.Logger.LogError("  m_animationState = " + sh.m_animationState.ToString());
			Jotunn.Logger.LogError("  m_skillType = " + sh.m_skillType.ToString());
			Jotunn.Logger.LogError("  m_toolTier = " + sh.m_toolTier);
			Jotunn.Logger.LogError("  m_damages = " + sh.m_damages);
			Jotunn.Logger.LogError("  m_damagesPerLevel = " + sh.m_damagesPerLevel);
			Jotunn.Logger.LogError("  m_attackForce = " + sh.m_attackForce);
			Jotunn.Logger.LogError("  m_backstabBonus = " + sh.m_backstabBonus);
			Jotunn.Logger.LogError("  m_dodgeable = " + sh.m_dodgeable);
			Jotunn.Logger.LogError("  m_blockable = " + sh.m_blockable);
			Jotunn.Logger.LogError("  m_tamedOnly = " + sh.m_tamedOnly);
			Jotunn.Logger.LogError("  m_attackStatusEffect = " + sh.m_attackStatusEffect);
			Jotunn.Logger.LogError("  m_spawnOnHit = " + sh.m_spawnOnHit);
			Jotunn.Logger.LogError("  m_spawnOnHitTerrain = " + sh.m_spawnOnHitTerrain);
			Jotunn.Logger.LogError("Attacks");
			Jotunn.Logger.LogError("  m_attack = " + sh.m_attack);
			Jotunn.Logger.LogError("  m_secondaryAttack = " + sh.m_secondaryAttack);
			Jotunn.Logger.LogError("  m_tamedOnly = " + sh.m_tamedOnly);
			Jotunn.Logger.LogError("Hold");
			Jotunn.Logger.LogError("  m_holdDurationMin = " + sh.m_holdDurationMin);
			Jotunn.Logger.LogError("  m_holdStaminaDrain = " + sh.m_holdStaminaDrain);
			Jotunn.Logger.LogError("  m_holdAnimationState = " + sh.m_holdAnimationState);
			Jotunn.Logger.LogError("Ammo");
			Jotunn.Logger.LogError("  m_ammoType = " + sh.m_ammoType);
			Jotunn.Logger.LogError("AI");
			Jotunn.Logger.LogError("  m_aiAttackRange = " + sh.m_aiAttackRange);
			Jotunn.Logger.LogError("  m_aiAttackRangeMin = " + sh.m_aiAttackRangeMin);
			Jotunn.Logger.LogError("  m_aiAttackInterval = " + sh.m_aiAttackInterval);
			Jotunn.Logger.LogError("  m_aiAttackMaxAngle = " + sh.m_aiAttackMaxAngle);
			Jotunn.Logger.LogError("  m_aiWhenFlying = " + sh.m_aiWhenFlying);
			Jotunn.Logger.LogError("  m_aiWhenWalking = " + sh.m_aiWhenWalking);
			Jotunn.Logger.LogError("  m_aiWhenSwiming = " + sh.m_aiWhenSwiming);
			Jotunn.Logger.LogError("  m_aiPrioritized = " + sh.m_aiPrioritized);
			Jotunn.Logger.LogError("  m_aiTargetType = " + sh.m_aiTargetType);
			Jotunn.Logger.LogError("  m_ammoType = " + sh.m_ammoType);
			Jotunn.Logger.LogError("  m_ammoType = " + sh.m_ammoType);

			// Jotunn.Logger.LogError("General");
			// Jotunn.Logger.LogError("  m_alertRange = " + mai.m_alertRange);
			// Jotunn.Logger.LogError("  m_alertOthersRange = " + MonsterAI.m_alertOthersRange);
			// Jotunn.Logger.LogError("  m_fleeIfHurtWhenTargetCantBeReached = " + mai.m_fleeIfHurtWhenTargetCantBeReached);
			// Jotunn.Logger.LogError("  m_fleeIfNotAlerted = " + mai.m_fleeIfNotAlerted);
			// Jotunn.Logger.LogError("  m_fleeIfLowHealth = " + mai.m_fleeIfLowHealth);
			// Jotunn.Logger.LogError("  m_circulateWhileCharging = " + mai.m_circulateWhileCharging);
			// Jotunn.Logger.LogError("  m_circulateWhileChargingFlying = " + mai.m_circulateWhileChargingFlying);
			// Jotunn.Logger.LogError("  m_enableHuntPlayer = " + mai.m_enableHuntPlayer);
			// Jotunn.Logger.LogError("  m_attackPlayerObjects = " + mai.m_attackPlayerObjects);
			// Jotunn.Logger.LogError("  m_unreachableTargetRadius = " + MonsterAI.m_unreachableTargetRadius);
			// Jotunn.Logger.LogError("  m_interceptTimeMax = " + mai.m_interceptTimeMax);
			// Jotunn.Logger.LogError("  m_interceptTimeMin = " + mai.m_interceptTimeMin);
			// Jotunn.Logger.LogError("  m_maxChaseDistance = " + mai.m_maxChaseDistance);
			// Jotunn.Logger.LogError("  m_minAttackInterval = " + mai.m_minAttackInterval);
			// Jotunn.Logger.LogError("Circle target");
			// Jotunn.Logger.LogError("  m_circleTargetInterval = " + mai.m_circleTargetInterval);
			// Jotunn.Logger.LogError("  m_circleTargetDuration = " + mai.m_circleTargetDuration);
			// Jotunn.Logger.LogError("  m_circleTargetDistance = " + mai.m_circleTargetDistance);
			// Jotunn.Logger.LogError("Sleep");
			// Jotunn.Logger.LogError("  m_sleeping = " + mai.m_sleeping);
			// Jotunn.Logger.LogError("  m_noiseWakeup = " + mai.m_noiseWakeup);
			// Jotunn.Logger.LogError("  m_noiseRangeScale = " + mai.m_noiseRangeScale);
			// Jotunn.Logger.LogError("  m_wakeupRange = " + mai.m_wakeupRange);
			// Jotunn.Logger.LogError("  m_wakeupEffects = " + mai.m_wakeupEffects);
			// Jotunn.Logger.LogError("Other");
			// Jotunn.Logger.LogError("  m_avoidLand = " + mai.m_avoidLand);
			// Jotunn.Logger.LogError("Consume items");
			// Jotunn.Logger.LogError("  m_consumeItems = " + mai.m_consumeItems);
			// Jotunn.Logger.LogError("  m_consumeRange = " + mai.m_consumeRange);
			// Jotunn.Logger.LogError("  m_consumeSearchRange = " + mai.m_consumeSearchRange);
			// Jotunn.Logger.LogError("  m_consumeSearchInterval = " + mai.m_consumeSearchInterval);
			// Jotunn.Logger.LogError("  m_consumeTarget = " + mai.m_consumeTarget);
			// Jotunn.Logger.LogError("  m_consumeSearchTimer = " + mai.m_consumeSearchTimer);
			// Jotunn.Logger.LogError("  m_itemMask = " + MonsterAI.m_itemMask);
			// Jotunn.Logger.LogError("  m_aiStatus = " + mai.m_aiStatus);
			// Jotunn.Logger.LogError("  m_despawnInDay = " + mai.m_despawnInDay);
			// Jotunn.Logger.LogError("  m_eventCreature = " + mai.m_eventCreature);
			// Jotunn.Logger.LogError("  m_targetCreature = " + mai.m_targetCreature);
			// Jotunn.Logger.LogError("  m_lastKnownTargetPos = Vector3 " + mai.m_lastKnownTargetPos.ToString());
			// Jotunn.Logger.LogError("  m_beenAtLastPos = " + mai.m_beenAtLastPos);
			// Jotunn.Logger.LogError("  m_targetStatic = " + mai.m_targetStatic);
			// Jotunn.Logger.LogError("  m_timeSinceAttacking = " + mai.m_timeSinceAttacking);
			// Jotunn.Logger.LogError("  m_timeSinceSensedTargetCreature = " + mai.m_timeSinceSensedTargetCreature);
			// Jotunn.Logger.LogError("  m_updateTargetTimer = " + mai.m_updateTargetTimer);
			// Jotunn.Logger.LogError("  m_updateWeaponTimer = " + mai.m_updateWeaponTimer);
			// Jotunn.Logger.LogError("  m_lastAttackTime = " + mai.m_lastAttackTime);
			// Jotunn.Logger.LogError("  m_interceptTime = " + mai.m_interceptTime);
			// Jotunn.Logger.LogError("  m_pauseTimer = " + mai.m_pauseTimer);
			// Jotunn.Logger.LogError("  m_sleepTimer = " + mai.m_sleepTimer);
			// Jotunn.Logger.LogError("  m_unableToAttackTargetTimer = " + mai.m_unableToAttackTargetTimer);
			Jotunn.Logger.LogInfo("    Done");
        }
    }
}
