using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

namespace Hirdmandr
{
// Token: 0x02000019 RID: 25
	public class NPCPlayerClone : Humanoid
	{
		// Token: 0x06000178 RID: 376 RVA: 0x0000B32C File Offset: 0x0000952C
		public override void Awake()
		{
			base.Awake();
			// this.SetupAwake();
			if (this.m_nview.GetZDO() == null)
			{
				return;
			}
			// Inventory inventory = this.m_inventory;
            //if (player.crouching == 0)
            //{
            //    player.crouching = zsyncanimation.gethash("crouching");
            //}

            float num = UnityEngine.Random.Range(0f, 6.2831855f);
			base.SetLookDir(new Vector3(Mathf.Cos(num), 0f, Mathf.Sin(num)), 0f);
			this.FaceLookDirection();
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000B6B0 File Offset: 0x000098B0
		public void SetPlayerID(long playerID, string name)
		{
			if (this.m_nview.GetZDO() == null)
			{
				return;
			}
			if (this.GetPlayerID() != 0L)
			{
				return;
			}
			this.m_nview.GetZDO().Set("playerID", playerID);
			this.m_nview.GetZDO().Set("playerName", name);
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0000B700 File Offset: 0x00009900
		public long GetPlayerID()
		{
			if (this.m_nview.IsValid())
			{
				return this.m_nview.GetZDO().GetLong("playerID", 0L);
			}
			return 0L;
		}

		// Token: 0x0600017C RID: 380 RVA: 0x0000B729 File Offset: 0x00009929
		public string GetPlayerName()
		{
			if (this.m_nview.IsValid())
			{
				return this.m_nview.GetZDO().GetString("playerName", "...");
			}
			return "";
		}

		public override string GetHoverText()
		{
			if (!m_hmnpc.m_isHirdmandr && !m_hmnpc.m_isRescued)
			{
				return m_name + "\n[<color=yellow><b>E</b></color>] Rescue";
			}
			else if (!m_hmnpc.m_isHirdmandr && m_hmnpc.m_isRescued)
			{
				return m_name + "\n[<color=yellow><b>E</b></color>] Talk";
			}
			else
			{
				return m_name + "\n[<color=yellow><b>E</b></color>] Talk\n[<color=yellow><b>LShift+E</b></color>] Manage";
			}
		}

		public override string GetHoverName()
		{
			return m_name;
		}


		// Token: 0x0600017F RID: 383 RVA: 0x0000B767 File Offset: 0x00009967
		public override void Start()
		{
			base.Start();
			this.m_nview.GetZDO();
		}

		//// Token: 0x06000181 RID: 385 RVA: 0x0000B890 File Offset: 0x00009A90
		//public override void FixedUpdate()
		//{
		//	base.FixedUpdate();
		//	float fixedDeltaTime = Time.fixedDeltaTime;
		//	this.UpdateAwake(fixedDeltaTime);
		//	if (this.m_nview.GetZDO() == null)
		//	{
		//		return;
		//	}
		//	if (this.m_nview.IsOwner())
		//	{
		//		if (this.IsDead())
		//		{
		//			return;
		//		}
		//		this.PlayerAttackInput(fixedDeltaTime);
		//		this.UpdateAttach();
		//		this.UpdateCrouch(fixedDeltaTime);
		//		this.UpdateDodge(fixedDeltaTime);
		//		this.UpdateCover(fixedDeltaTime);
		//		this.UpdateBaseValue(fixedDeltaTime);
		//		this.UpdateTeleport(fixedDeltaTime);
		//		this.UpdateBiome(fixedDeltaTime);
		//	}
		//}

		// Token: 0x06000181 RID: 385 RVA: 0x0000B890 File Offset: 0x00009A90
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (this.m_nview.GetZDO() == null)
			{
				return;
			}
			if (this.m_nview.IsOwner())
			{
				if (this.IsDead())
				{
					return;
				}
				this.UpdateAttach();
			}
		}


		// Token: 0x06000182 RID: 386 RVA: 0x0000B9C0 File Offset: 0x00009BC0
		public void Update()
		{

		}

		//// Token: 0x0600018A RID: 394 RVA: 0x0000C2AC File Offset: 0x0000A4AC
		//public void SetupAwake()
		//{
		//	if (this.m_nview.GetZDO() == null)
		//	{
		//		this.m_animator.SetBool("wakeup", false);
		//		return;
		//	}
		//	bool @bool = this.m_nview.GetZDO().GetBool("wakeup", true);
		//	this.m_animator.SetBool("wakeup", @bool);
		//	if (@bool)
		//	{
		//		this.m_wakeupTimer = 0f;
		//	}
		//}

		//// Token: 0x0600018B RID: 395 RVA: 0x0000C310 File Offset: 0x0000A510
		//public void UpdateAwake(float dt)
		//{
		//	if (this.m_wakeupTimer >= 0f)
		//	{
		//		this.m_wakeupTimer += dt;
		//		if (this.m_wakeupTimer > 1f)
		//		{
		//			this.m_wakeupTimer = -1f;
		//			this.m_animator.SetBool("wakeup", false);
		//			if (this.m_nview.IsOwner())
		//			{
		//				this.m_nview.GetZDO().Set("wakeup", false);
		//			}
		//		}
		//	}
		//}

		public void UpdateBiome(float dt)
		{
			if (this.InIntro())
			{
				return;
			}
			this.m_biomeTimer += dt;
			if (this.m_biomeTimer > 1f)
			{
				this.m_biomeTimer = 0f;
				Heightmap.Biome biome = Heightmap.FindBiome(base.transform.position);
				if (this.m_currentBiome != biome)
				{
					this.m_currentBiome = biome;
				}
			}
		}
		
		//// Token: 0x0600018D RID: 397 RVA: 0x0000C470 File Offset: 0x0000A670
		//public void AutoPickup(float dt)
		//{
		//	if (this.IsTeleporting())
		//	{
		//		return;
		//	}
		//	if (!this.m_enableAutoPickup)
		//	{
		//		return;
		//	}
		//	Vector3 vector = base.transform.position + Vector3.up;
		//	foreach (Collider collider in Physics.OverlapSphere(vector, this.m_autoPickupRange, this.m_autoPickupMask))
		//	{
		//		if (collider.attachedRigidbody)
		//		{
		//			ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
		//			if (!(component == null) && component.m_autoPickup && !this.HaveUniqueKey(component.m_itemData.m_shared.m_name) && component.GetComponent<ZNetView>().IsValid())
		//			{
		//				if (!component.CanPickup(true))
		//				{
		//					component.RequestOwn();
		//				}
		//				else if (!component.InTar())
		//				{
		//					component.Load();
		//					if (this.m_inventory.CanAddItem(component.m_itemData, -1) && component.m_itemData.GetWeight() + this.m_inventory.GetTotalWeight() <= this.GetMaxCarryWeight())
		//					{
		//						float num = Vector3.Distance(component.transform.position, vector);
		//						if (num <= this.m_autoPickupRange)
		//						{
		//							if (num < 0.3f)
		//							{
		//								base.Pickup(component.gameObject, true, true);
		//							}
		//							else
		//							{
		//								Vector3 vector2 = Vector3.Normalize(vector - component.transform.position);
		//								float num2 = 15f;
		//								component.transform.position = component.transform.position + vector2 * num2 * dt;
		//							}
		//						}
		//					}
		//				}
		//			}
		//		}
		//	}
		//}

		// Token: 0x0600018E RID: 398 RVA: 0x0000C61C File Offset: 0x0000A81C
		public void PlayerAttackInput(float dt)
		{
			if (this.InPlaceMode())
			{
				return;
			}
			ItemDrop.ItemData currentWeapon = base.GetCurrentWeapon();
			if (currentWeapon != null && currentWeapon.m_shared.m_holdDurationMin > 0f)
			{
				if (this.m_blocking || this.InMinorAction() || this.IsAttached())
				{
					this.m_attackHoldTime = -1f;
					if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
					{
						this.m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, false);
					}
					return;
				}
				float num = currentWeapon.GetHoldStaminaDrain();
				if ((double)base.GetAttackDrawPercentage() >= 1.0)
				{
					num *= 0.5f;
				}
				bool flag = num <= 0f || this.HaveStamina(0f);
				if (this.m_attackHoldTime < 0f)
				{
					if (!this.m_attackHold)
					{
						this.m_attackHoldTime = 0f;
						return;
					}
				}
				else
				{
					if (this.m_attackHold && flag && this.m_attackHoldTime >= 0f)
					{
						if (this.m_attackHoldTime == 0f)
						{
							if (!currentWeapon.m_shared.m_attack.StartDraw(this, currentWeapon))
							{
								this.m_attackHoldTime = -1f;
								return;
							}
							currentWeapon.m_shared.m_holdStartEffect.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
						}
						this.m_attackHoldTime += Time.fixedDeltaTime;
						if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
						{
							this.m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, true);
						}
						this.UseStamina(num * dt);
						return;
					}
					if (this.m_attackHoldTime > 0f)
					{
						if (flag)
						{
							this.StartAttack(null, false);
						}
						if (!string.IsNullOrEmpty(currentWeapon.m_shared.m_holdAnimationState))
						{
							this.m_zanim.SetBool(currentWeapon.m_shared.m_holdAnimationState, false);
						}
						this.m_attackHoldTime = 0f;
						return;
					}
				}
			}
			else
			{
				if (this.m_attack)
				{
					this.m_queuedAttackTimer = 0.5f;
					this.m_queuedSecondAttackTimer = 0f;
				}
				if (this.m_secondaryAttack)
				{
					this.m_queuedSecondAttackTimer = 0.5f;
					this.m_queuedAttackTimer = 0f;
				}
				this.m_queuedAttackTimer -= Time.fixedDeltaTime;
				this.m_queuedSecondAttackTimer -= Time.fixedDeltaTime;
				if ((this.m_queuedAttackTimer > 0f || this.m_attackHold) && this.StartAttack(null, false))
				{
					this.m_queuedAttackTimer = 0f;
				}
				if ((this.m_queuedSecondAttackTimer > 0f || this.m_secondaryAttackHold) && this.StartAttack(null, true))
				{
					this.m_queuedSecondAttackTimer = 0f;
				}
			}
		}

		//// Token: 0x0600018F RID: 399 RVA: 0x0000C8BE File Offset: 0x0000AABE
		//public override bool HaveQueuedChain()
		//{
		//	return (this.m_queuedAttackTimer > 0f || this.m_attackHold) && base.GetCurrentWeapon() != null && this.m_currentAttack != null && this.m_currentAttack.CanStartChainAttack();
		//}

		// Token: 0x06000190 RID: 400 RVA: 0x0000C8F4 File Offset: 0x0000AAF4
		public void UpdateBaseValue(float dt)
		{
			this.m_baseValueUpdatetimer += dt;
			if (this.m_baseValueUpdatetimer > 2f)
			{
				this.m_baseValueUpdatetimer = 0f;
				this.m_baseValue = EffectArea.GetBaseValue(base.transform.position, 20f);
				this.m_nview.GetZDO().Set("baseValue", this.m_baseValue);
				this.m_comfortLevel = CalculateNPCComfortLevel(this);
			}
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x00022BC4 File Offset: 0x00020DC4
		public static int CalculateNPCComfortLevel(NPCPlayerClone player)
		{
			List<Piece> list;
			if (Terminal.m_testList.ContainsKey("oldcomfort"))
			{
				list = SE_Rested.GetNearbyPieces(player.transform.position);
			}
			else
			{
				list = SE_Rested.GetNearbyComfortPieces(player.transform.position);
			}
			list.Sort(new Comparison<Piece>(SE_Rested.PieceComfortSort));
			int num = 1;
			if (player.InShelter())
			{
				num++;
				int i = 0;
				while (i < list.Count)
				{
					Piece piece = list[i];
					if (i <= 0)
					{
						goto IL_9B;
					}
					Piece piece2 = list[i - 1];
					if ((piece.m_comfortGroup == Piece.ComfortGroup.None || piece.m_comfortGroup != piece2.m_comfortGroup) && !(piece.m_name == piece2.m_name))
					{
						goto IL_9B;
					}
				IL_A4:
					i++;
					continue;
				IL_9B:
					num += piece.GetComfort();
					goto IL_A4;
				}
			}
			return num;
		}

		// Token: 0x06000191 RID: 401 RVA: 0x0000C969 File Offset: 0x0000AB69
		public int GetComfortLevel()
		{
			return this.m_comfortLevel;
		}

		// Token: 0x06000193 RID: 403 RVA: 0x0000C9AC File Offset: 0x0000ABAC
		public bool IsSafeInHome()
		{
			return this.m_safeInHome;
		}

		// Token: 0x06000195 RID: 405 RVA: 0x0000CA18 File Offset: 0x0000AC18
		public Heightmap.Biome GetCurrentBiome()
		{
			return this.m_currentBiome;
		}

		// Token: 0x06000198 RID: 408 RVA: 0x0000CC08 File Offset: 0x0000AE08
		public void UpdateEnvStatusEffects(float dt)
		{
			this.m_nearFireTimer += dt;
			HitData.DamageModifiers damageModifiers = base.GetDamageModifiers();
			bool flag = this.m_nearFireTimer < 0.25f;
			bool flag2 = this.m_seman.HaveStatusEffect("Burning");
			bool flag3 = this.InShelter();
			HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
			bool flag4 = EnvMan.instance.IsFreezing();
			bool flag5 = EnvMan.instance.IsCold();
			bool flag6 = EnvMan.instance.IsWet();
			bool flag8 = this.m_seman.HaveStatusEffect("Wet");
			bool flag9 = this.IsSitting();
			bool flag10 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.WarmCozyArea, 1f);
			bool flag11 = flag4 && !flag && !flag3;
			bool flag12 = (flag5 && !flag) || (flag4 && flag && !flag3) || (flag4 && !flag && flag3);
			if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant || flag10)
			{
				flag11 = false;
				flag12 = false;
			}
			if (flag6 && !this.m_underRoof)
			{
				this.m_seman.AddStatusEffect("Wet", true);
			}
			if (flag3)
			{
				this.m_seman.AddStatusEffect("Shelter", false);
			}
			else
			{
				this.m_seman.RemoveStatusEffect("Shelter", false);
			}
			if (flag)
			{
				this.m_seman.AddStatusEffect("CampFire", false);
			}
			else
			{
				this.m_seman.RemoveStatusEffect("CampFire", false);
			}
		}

		//// Token: 0x06000199 RID: 409 RVA: 0x0000CE70 File Offset: 0x0000B070
		//public bool CanEat(ItemDrop.ItemData item, bool showMessages)
		//{
		//	foreach (Player.Food food in this.m_foods)
		//	{
		//		if (food.m_item.m_shared.m_name == item.m_shared.m_name)
		//		{
		//			if (food.CanEatAgain())
		//			{
		//				return true;
		//			}
		//			this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_nomore", new string[]
		//			{
		//				item.m_shared.m_name
		//			}), 0, null);
		//			return false;
		//		}
		//	}
		//	using (List<Player.Food>.Enumerator enumerator = this.m_foods.GetEnumerator())
		//	{
		//		while (enumerator.MoveNext())
		//		{
		//			if (enumerator.Current.CanEatAgain())
		//			{
		//				return true;
		//			}
		//		}
		//	}
		//	if (this.m_foods.Count >= 3)
		//	{
		//		this.Message(MessageHud.MessageType.Center, "$msg_isfull", 0, null);
		//		return false;
		//	}
		//	return true;
		//}

		//// Token: 0x0600019A RID: 410 RVA: 0x0000CF84 File Offset: 0x0000B184
		//public Player.Food GetMostDepletedFood()
		//{
		//	Player.Food food = null;
		//	foreach (Player.Food food2 in this.m_foods)
		//	{
		//		if (food2.CanEatAgain() && (food == null || food2.m_time < food.m_time))
		//		{
		//			food = food2;
		//		}
		//	}
		//	return food;
		//}

		//// Token: 0x0600019B RID: 411 RVA: 0x0000CFF0 File Offset: 0x0000B1F0
		//public void ClearFood()
		//{
		//	this.m_foods.Clear();
		//}

		//// Token: 0x0600019C RID: 412 RVA: 0x0000CFFD File Offset: 0x0000B1FD
		//public bool RemoveOneFood()
		//{
		//	if (this.m_foods.Count == 0)
		//	{
		//		return false;
		//	}
		//	this.m_foods.RemoveAt(Random.Range(0, this.m_foods.Count));
		//	return true;
		//}

		//// Token: 0x0600019D RID: 413 RVA: 0x0000D02C File Offset: 0x0000B22C
		//public bool EatFood(ItemDrop.ItemData item)
		//{
		//	if (!this.CanEat(item, false))
		//	{
		//		return false;
		//	}
		//	string text = "";
		//	if (item.m_shared.m_food > 0f)
		//	{
		//		text = text + " +" + item.m_shared.m_food.ToString() + " $item_food_health ";
		//	}
		//	if (item.m_shared.m_foodStamina > 0f)
		//	{
		//		text = text + " +" + item.m_shared.m_foodStamina.ToString() + " $item_food_stamina ";
		//	}
		//	this.Message(MessageHud.MessageType.Center, text, 0, null);
		//	foreach (Player.Food food in this.m_foods)
		//	{
		//		if (food.m_item.m_shared.m_name == item.m_shared.m_name)
		//		{
		//			if (food.CanEatAgain())
		//			{
		//				food.m_time = item.m_shared.m_foodBurnTime;
		//				food.m_health = item.m_shared.m_food;
		//				food.m_stamina = item.m_shared.m_foodStamina;
		//				this.UpdateFood(0f, true);
		//				return true;
		//			}
		//			return false;
		//		}
		//	}
		//	if (this.m_foods.Count < 3)
		//	{
		//		Player.Food food2 = new Player.Food();
		//		food2.m_name = item.m_dropPrefab.name;
		//		food2.m_item = item;
		//		food2.m_time = item.m_shared.m_foodBurnTime;
		//		food2.m_health = item.m_shared.m_food;
		//		food2.m_stamina = item.m_shared.m_foodStamina;
		//		this.m_foods.Add(food2);
		//		this.UpdateFood(0f, true);
		//		return true;
		//	}
		//	Player.Food mostDepletedFood = this.GetMostDepletedFood();
		//	if (mostDepletedFood != null)
		//	{
		//		mostDepletedFood.m_name = item.m_dropPrefab.name;
		//		mostDepletedFood.m_item = item;
		//		mostDepletedFood.m_time = item.m_shared.m_foodBurnTime;
		//		mostDepletedFood.m_health = item.m_shared.m_food;
		//		mostDepletedFood.m_stamina = item.m_shared.m_foodStamina;
		//		this.UpdateFood(0f, true);
		//		return true;
		//	}
		//	return false;
		//}

		//// Token: 0x0600019E RID: 414 RVA: 0x0000D260 File Offset: 0x0000B460
		//public void UpdateFood(float dt, bool forceUpdate)
		//{
		//	this.m_foodUpdateTimer += dt;
		//	if (this.m_foodUpdateTimer >= 1f || forceUpdate)
		//	{
		//		this.m_foodUpdateTimer -= 1f;
		//		foreach (Player.Food food in this.m_foods)
		//		{
		//			food.m_time -= 1f;
		//			float num = Mathf.Clamp01(food.m_time / food.m_item.m_shared.m_foodBurnTime);
		//			num = Mathf.Pow(num, 0.3f);
		//			food.m_health = food.m_item.m_shared.m_food * num;
		//			food.m_stamina = food.m_item.m_shared.m_foodStamina * num;
		//			if (food.m_time <= 0f)
		//			{
		//				this.Message(MessageHud.MessageType.Center, "$msg_food_done", 0, null);
		//				this.m_foods.Remove(food);
		//				break;
		//			}
		//		}
		//		float health;
		//		float stamina;
		//		this.GetTotalFoodValue(out health, out stamina);
		//		this.SetMaxHealth(health, true);
		//		this.SetMaxStamina(stamina, true);
		//	}
		//	if (!forceUpdate)
		//	{
		//		this.m_foodRegenTimer += dt;
		//		if (this.m_foodRegenTimer >= 10f)
		//		{
		//			this.m_foodRegenTimer = 0f;
		//			float num2 = 0f;
		//			foreach (Player.Food food2 in this.m_foods)
		//			{
		//				num2 += food2.m_item.m_shared.m_foodRegen;
		//			}
		//			if (num2 > 0f)
		//			{
		//				float num3 = 1f;
		//				this.m_seman.ModifyHealthRegen(ref num3);
		//				num2 *= num3;
		//				base.Heal(num2, true);
		//			}
		//		}
		//	}
		//}

		//// Token: 0x0600019F RID: 415 RVA: 0x0000D454 File Offset: 0x0000B654
		//public void GetTotalFoodValue(out float hp, out float stamina)
		//{
		//	hp = this.m_baseHP;
		//	stamina = this.m_baseStamina;
		//	foreach (Player.Food food in this.m_foods)
		//	{
		//		hp += food.m_health;
		//		stamina += food.m_stamina;
		//	}
		//}

		//// Token: 0x060001A0 RID: 416 RVA: 0x0000D4C8 File Offset: 0x0000B6C8
		//public float GetBaseFoodHP()
		//{
		//	return this.m_baseHP;
		//}

		//// Token: 0x060001A1 RID: 417 RVA: 0x0000D4D0 File Offset: 0x0000B6D0
		//public List<Player.Food> GetFoods()
		//{
		//	return this.m_foods;
		//}

		//// Token: 0x060001A3 RID: 419 RVA: 0x0000D544 File Offset: 0x0000B744
		//public override bool CheckRun(Vector3 moveDir, float dt)
		//{
		//	if (!base.CheckRun(moveDir, dt))
		//	{
		//		return false;
		//	}
		//	bool flag = this.HaveStamina(0f);
		//	float skillFactor = this.m_skills.GetSkillFactor(Skills.SkillType.Run);
		//	float num = Mathf.Lerp(1f, 0.5f, skillFactor);
		//	float num2 = this.m_runStaminaDrain * num;
		//	this.m_seman.ModifyRunStaminaDrain(num2, ref num2);
		//	this.UseStamina(dt * num2);
		//	if (this.HaveStamina(0f))
		//	{
		//		this.m_runSkillImproveTimer += dt;
		//		if (this.m_runSkillImproveTimer > 1f)
		//		{
		//			this.m_runSkillImproveTimer = 0f;
		//			this.RaiseSkill(Skills.SkillType.Run, 1f);
		//		}
		//		this.AbortEquipQueue();
		//		return true;
		//	}
		//	if (flag)
		//	{
		//		Hud.instance.StaminaBarNoStaminaFlash();
		//	}
		//	return false;
		//}

		// Token: 0x060001A4 RID: 420 RVA: 0x0000D600 File Offset: 0x0000B800
		public void UpdateMovementModifier()
		{
			this.m_equipmentMovementModifier = 0f;
			if (this.m_rightItem != null)
			{
				this.m_equipmentMovementModifier += this.m_rightItem.m_shared.m_movementModifier;
			}
			if (this.m_leftItem != null)
			{
				this.m_equipmentMovementModifier += this.m_leftItem.m_shared.m_movementModifier;
			}
			if (this.m_chestItem != null)
			{
				this.m_equipmentMovementModifier += this.m_chestItem.m_shared.m_movementModifier;
			}
			if (this.m_legItem != null)
			{
				this.m_equipmentMovementModifier += this.m_legItem.m_shared.m_movementModifier;
			}
			if (this.m_helmetItem != null)
			{
				this.m_equipmentMovementModifier += this.m_helmetItem.m_shared.m_movementModifier;
			}
			if (this.m_shoulderItem != null)
			{
				this.m_equipmentMovementModifier += this.m_shoulderItem.m_shared.m_movementModifier;
			}
			if (this.m_utilityItem != null)
			{
				this.m_equipmentMovementModifier += this.m_utilityItem.m_shared.m_movementModifier;
			}
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x0000D71B File Offset: 0x0000B91B
		public void OnSkillLevelup(Skills.SkillType skill, float level)
		{
			this.m_skillLevelupEffects.Create(this.m_head.position, this.m_head.rotation, this.m_head, 1f, -1);
		}

		//// Token: 0x060001A6 RID: 422 RVA: 0x0000D74C File Offset: 0x0000B94C
		//public override void OnJump()
		//{
		//	this.AbortEquipQueue();
		//	float num = this.m_jumpStaminaUsage - this.m_jumpStaminaUsage * this.m_equipmentMovementModifier;
		//	this.m_seman.ModifyJumpStaminaUsage(num, ref num);
		//	this.UseStamina(num);
		//}

		//// Token: 0x060001AA RID: 426 RVA: 0x0000D98C File Offset: 0x0000BB8C
		//public bool RequiredCraftingStation(Recipe recipe, int qualityLevel, bool checkLevel)
		//{
		//	CraftingStation requiredStation = recipe.GetRequiredStation(qualityLevel);
		//	if (requiredStation != null)
		//	{
		//		if (this.m_currentStation == null)
		//		{
		//			return false;
		//		}
		//		if (requiredStation.m_name != this.m_currentStation.m_name)
		//		{
		//			return false;
		//		}
		//		if (checkLevel)
		//		{
		//			int requiredStationLevel = recipe.GetRequiredStationLevel(qualityLevel);
		//			if (this.m_currentStation.GetLevel() < requiredStationLevel)
		//			{
		//				return false;
		//			}
		//		}
		//	}
		//	else if (this.m_currentStation != null && !this.m_currentStation.m_showBasicRecipies)
		//	{
		//		return false;
		//	}
		//	return true;
		//}

		//// Token: 0x060001AB RID: 427 RVA: 0x0000DA10 File Offset: 0x0000BC10
		//public bool HaveRequirements(Recipe recipe, bool discover, int qualityLevel)
		//{
		//	if (discover)
		//	{
		//		if (recipe.m_craftingStation && !this.KnowStationLevel(recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
		//		{
		//			return false;
		//		}
		//	}
		//	else if (!this.RequiredCraftingStation(recipe, qualityLevel, true))
		//	{
		//		return false;
		//	}
		//	return (recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && this.HaveRequirements(recipe.m_resources, discover, qualityLevel);
		//}

		//// Token: 0x060001AC RID: 428 RVA: 0x0000DAA8 File Offset: 0x0000BCA8
		//public bool HaveRequirements(Piece.Requirement[] resources, bool discover, int qualityLevel)
		//{
		//	foreach (Piece.Requirement requirement in resources)
		//	{
		//		if (requirement.m_resItem)
		//		{
		//			if (discover)
		//			{
		//				if (requirement.m_amount > 0 && !this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
		//				{
		//					return false;
		//				}
		//			}
		//			else
		//			{
		//				int amount = requirement.GetAmount(qualityLevel);
		//				if (this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < amount)
		//				{
		//					return false;
		//				}
		//			}
		//		}
		//	}
		//	return true;
		//}

		//// Token: 0x060001AD RID: 429 RVA: 0x0000DB34 File Offset: 0x0000BD34
		//public bool HaveRequirements(Piece piece, Player.RequirementMode mode)
		//{
		//	if (piece.m_craftingStation)
		//	{
		//		if (mode == Player.RequirementMode.IsKnown || mode == Player.RequirementMode.CanAlmostBuild)
		//		{
		//			if (!this.m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
		//			{
		//				return false;
		//			}
		//		}
		//		else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position))
		//		{
		//			return false;
		//		}
		//	}
		//	if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
		//	{
		//		return false;
		//	}
		//	foreach (Piece.Requirement requirement in piece.m_resources)
		//	{
		//		if (requirement.m_resItem && requirement.m_amount > 0)
		//		{
		//			if (mode == Player.RequirementMode.IsKnown)
		//			{
		//				if (!this.m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
		//				{
		//					return false;
		//				}
		//			}
		//			else if (mode == Player.RequirementMode.CanAlmostBuild)
		//			{
		//				if (!this.m_inventory.HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
		//				{
		//					return false;
		//				}
		//			}
		//			else if (mode == Player.RequirementMode.CanBuild && this.m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount)
		//			{
		//				return false;
		//			}
		//		}
		//	}
		//	return true;
		//}

		//// Token: 0x060001AE RID: 430 RVA: 0x0000DC6C File Offset: 0x0000BE6C
		//public void ConsumeResources(Piece.Requirement[] requirements, int qualityLevel)
		//{
		//	foreach (Piece.Requirement requirement in requirements)
		//	{
		//		if (requirement.m_resItem)
		//		{
		//			int amount = requirement.GetAmount(qualityLevel);
		//			if (amount > 0)
		//			{
		//				this.m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, amount);
		//			}
		//		}
		//	}
		//}

		// Token: 0x060001B2 RID: 434 RVA: 0x0000DF62 File Offset: 0x0000C162
		public void FaceLookDirection()
		{
			base.transform.rotation = base.GetLookYaw();
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x0000292B File Offset: 0x00000B2B
		public override bool IsPlayer()
		{
			return true;
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x0000E288 File Offset: 0x0000C488
		public void CreateDeathEffects()
		{
			GameObject[] array = this.m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
			for (int i = 0; i < array.Length; i++)
			{
				Ragdoll component = array[i].GetComponent<Ragdoll>();
				if (component)
				{
					Vector3 velocity = this.m_body.velocity;
					if (this.m_pushForce.magnitude * 0.5f > velocity.magnitude)
					{
						velocity = this.m_pushForce * 0.5f;
					}
					component.Setup(velocity, 0f, 0f, 0f, null);
					this.OnRagdollCreated(component);
					this.m_ragdoll = component;
				}
			}
		}

		// Token: 0x060001BA RID: 442 RVA: 0x0000E340 File Offset: 0x0000C540
		public void UnequipDeathDropItems()
		{
			if (this.m_rightItem != null)
			{
				base.UnequipItem(this.m_rightItem, false);
			}
			if (this.m_leftItem != null)
			{
				base.UnequipItem(this.m_leftItem, false);
			}
			if (this.m_ammoItem != null)
			{
				base.UnequipItem(this.m_ammoItem, false);
			}
			if (this.m_utilityItem != null)
			{
				base.UnequipItem(this.m_utilityItem, false);
			}
		}

		//// Token: 0x060001BB RID: 443 RVA: 0x0000E3A4 File Offset: 0x0000C5A4
		//public void CreateTombStone()
		//{
		//	if (this.m_inventory.NrOfItems() == 0)
		//	{
		//		return;
		//	}
		//	base.UnequipAllItems();
		//	GameObject gameObject = Object.Instantiate<GameObject>(this.m_tombstone, base.GetCenterPoint(), base.transform.rotation);
		//	gameObject.GetComponent<Container>().GetInventory().MoveInventoryToGrave(this.m_inventory);
		//	TombStone component = gameObject.GetComponent<TombStone>();
		//	PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		//	component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
		//}

		//// Token: 0x060001D1 RID: 465 RVA: 0x0000F98C File Offset: 0x0000DB8C
		//public void UpdateStations(float dt)
		//{
		//	this.m_stationDiscoverTimer += dt;
		//	if (this.m_stationDiscoverTimer > 1f)
		//	{
		//		this.m_stationDiscoverTimer = 0f;
		//		CraftingStation.UpdateKnownStationsInRange(this);
		//	}
		//	if (!(this.m_currentStation != null))
		//	{
		//		if (this.m_inCraftingStation)
		//		{
		//			this.m_zanim.SetInt("crafting", 0);
		//			this.m_inCraftingStation = false;
		//			if (InventoryGui.IsVisible())
		//			{
		//				InventoryGui.instance.Hide();
		//			}
		//		}
		//		return;
		//	}
		//	if (!this.m_currentStation.InUseDistance(this))
		//	{
		//		InventoryGui.instance.Hide();
		//		this.SetCraftingStation(null);
		//		return;
		//	}
		//	if (!InventoryGui.IsVisible())
		//	{
		//		this.SetCraftingStation(null);
		//		return;
		//	}
		//	this.m_currentStation.PokeInUse();
		//	if (!this.AlwaysRotateCamera())
		//	{
		//		Vector3 normalized = (this.m_currentStation.transform.position - base.transform.position).normalized;
		//		normalized.y = 0f;
		//		normalized.Normalize();
		//		Quaternion quaternion = Quaternion.LookRotation(normalized);
		//		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, this.m_turnSpeed * dt);
		//	}
		//	this.m_zanim.SetInt("crafting", this.m_currentStation.m_useAnimation);
		//	this.m_inCraftingStation = true;
		//}

		//// Token: 0x060001D2 RID: 466 RVA: 0x0000FAD3 File Offset: 0x0000DCD3
		//public void SetCraftingStation(CraftingStation station)
		//{
		//	if (this.m_currentStation == station)
		//	{
		//		return;
		//	}
		//	if (station)
		//	{
		//		this.AddKnownStation(station);
		//		station.PokeInUse();
		//		base.HideHandItems();
		//	}
		//	this.m_currentStation = station;
		//}

		// Token: 0x060001D4 RID: 468 RVA: 0x0000FB0E File Offset: 0x0000DD0E
		public void UpdateCover(float dt)
		{
			this.m_updateCoverTimer += dt;
			if (this.m_updateCoverTimer > 1f)
			{
				this.m_updateCoverTimer = 0f;
				Cover.GetCoverForPoint(base.GetCenterPoint(), out m_coverPercentage, out m_underRoof);
			}
		}

		//// Token: 0x060001D6 RID: 470 RVA: 0x0000FB55 File Offset: 0x0000DD55
		//public override GameObject GetHoverObject()
		//{
		//	return this.m_hovering;
		//}

		// Token: 0x060001D7 RID: 471 RVA: 0x0000FB5D File Offset: 0x0000DD5D
		public override void OnNearFire(Vector3 point)
		{
			this.m_nearFireTimer = 0f;
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x0000FB6A File Offset: 0x0000DD6A
		public bool InShelter()
		{
			return this.m_coverPercentage >= 0.8f && this.m_underRoof;
		}

		// Token: 0x060001DC RID: 476 RVA: 0x0000FBA0 File Offset: 0x0000DDA0
		public void SetGodMode(bool godMode)
		{
			this.m_godMode = godMode;
		}

		// Token: 0x060001DD RID: 477 RVA: 0x0000FBA9 File Offset: 0x0000DDA9
		public override bool InGodMode()
		{
			return this.m_godMode;
		}

		// Token: 0x060001DE RID: 478 RVA: 0x0000FBB1 File Offset: 0x0000DDB1
		public void SetGhostMode(bool ghostmode)
		{
			this.m_ghostMode = ghostmode;
		}

		// Token: 0x060001DF RID: 479 RVA: 0x0000FBBA File Offset: 0x0000DDBA
		public override bool InGhostMode()
		{
			return this.m_ghostMode;
		}

		//// Token: 0x060001E5 RID: 485 RVA: 0x0000FD2C File Offset: 0x0000DF2C
		//public void Save(ZPackage pkg)
		//{
		//	pkg.Write(25);
		//	pkg.Write(base.GetMaxHealth());
		//	pkg.Write(base.GetHealth());
		//	pkg.Write(this.GetMaxStamina());
		//	pkg.Write(this.m_firstSpawn);
		//	pkg.Write(this.m_timeSinceDeath);
		//	pkg.Write(this.m_guardianPower);
		//	pkg.Write(this.m_guardianPowerCooldown);
		//	this.m_inventory.Save(pkg);
		//	pkg.Write(this.m_knownRecipes.Count);
		//	foreach (string data in this.m_knownRecipes)
		//	{
		//		pkg.Write(data);
		//	}
		//	pkg.Write(this.m_knownStations.Count);
		//	foreach (KeyValuePair<string, int> keyValuePair in this.m_knownStations)
		//	{
		//		pkg.Write(keyValuePair.Key);
		//		pkg.Write(keyValuePair.Value);
		//	}
		//	pkg.Write(this.m_knownMaterial.Count);
		//	foreach (string data2 in this.m_knownMaterial)
		//	{
		//		pkg.Write(data2);
		//	}
		//	pkg.Write(this.m_shownTutorials.Count);
		//	foreach (string data3 in this.m_shownTutorials)
		//	{
		//		pkg.Write(data3);
		//	}
		//	pkg.Write(this.m_uniques.Count);
		//	foreach (string data4 in this.m_uniques)
		//	{
		//		pkg.Write(data4);
		//	}
		//	pkg.Write(this.m_trophies.Count);
		//	foreach (string data5 in this.m_trophies)
		//	{
		//		pkg.Write(data5);
		//	}
		//	pkg.Write(this.m_knownBiome.Count);
		//	foreach (Heightmap.Biome data6 in this.m_knownBiome)
		//	{
		//		pkg.Write((int)data6);
		//	}
		//	pkg.Write(this.m_knownTexts.Count);
		//	foreach (KeyValuePair<string, string> keyValuePair2 in this.m_knownTexts)
		//	{
		//		pkg.Write(keyValuePair2.Key);
		//		pkg.Write(keyValuePair2.Value);
		//	}
		//	pkg.Write(this.m_beardItem);
		//	pkg.Write(this.m_hairItem);
		//	pkg.Write(this.m_skinColor);
		//	pkg.Write(this.m_hairColor);
		//	pkg.Write(this.m_modelIndex);
		//	pkg.Write(this.m_foods.Count);
		//	foreach (Player.Food food in this.m_foods)
		//	{
		//		pkg.Write(food.m_name);
		//		pkg.Write(food.m_time);
		//	}
		//	this.m_skills.Save(pkg);
		//}

		//// Token: 0x060001E6 RID: 486 RVA: 0x00010120 File Offset: 0x0000E320
		//public void Load(ZPackage pkg)
		//{
		//	this.m_isLoading = true;
		//	base.UnequipAllItems();
		//	int num = pkg.ReadInt();
		//	if (num >= 7)
		//	{
		//		this.SetMaxHealth(pkg.ReadSingle(), false);
		//	}
		//	float num2 = pkg.ReadSingle();
		//	float maxHealth = base.GetMaxHealth();
		//	if (num2 <= 0f || num2 > maxHealth || float.IsNaN(num2))
		//	{
		//		num2 = maxHealth;
		//	}
		//	base.SetHealth(num2);
		//	if (num >= 10)
		//	{
		//		float stamina = pkg.ReadSingle();
		//		this.SetMaxStamina(stamina, false);
		//		this.m_stamina = stamina;
		//	}
		//	if (num >= 8)
		//	{
		//		this.m_firstSpawn = pkg.ReadBool();
		//	}
		//	if (num >= 20)
		//	{
		//		this.m_timeSinceDeath = pkg.ReadSingle();
		//	}
		//	if (num >= 23)
		//	{
		//		string guardianPower = pkg.ReadString();
		//		this.SetGuardianPower(guardianPower);
		//	}
		//	if (num >= 24)
		//	{
		//		this.m_guardianPowerCooldown = pkg.ReadSingle();
		//	}
		//	if (num == 2)
		//	{
		//		pkg.ReadZDOID();
		//	}
		//	this.m_inventory.Load(pkg);
		//	int num3 = pkg.ReadInt();
		//	for (int i = 0; i < num3; i++)
		//	{
		//		string item = pkg.ReadString();
		//		this.m_knownRecipes.Add(item);
		//	}
		//	if (num < 15)
		//	{
		//		int num4 = pkg.ReadInt();
		//		for (int j = 0; j < num4; j++)
		//		{
		//			pkg.ReadString();
		//		}
		//	}
		//	else
		//	{
		//		int num5 = pkg.ReadInt();
		//		for (int k = 0; k < num5; k++)
		//		{
		//			string key = pkg.ReadString();
		//			int value = pkg.ReadInt();
		//			this.m_knownStations.Add(key, value);
		//		}
		//	}
		//	int num6 = pkg.ReadInt();
		//	for (int l = 0; l < num6; l++)
		//	{
		//		string item2 = pkg.ReadString();
		//		this.m_knownMaterial.Add(item2);
		//	}
		//	if (num < 19 || num >= 21)
		//	{
		//		int num7 = pkg.ReadInt();
		//		for (int m = 0; m < num7; m++)
		//		{
		//			string item3 = pkg.ReadString();
		//			this.m_shownTutorials.Add(item3);
		//		}
		//	}
		//	if (num >= 6)
		//	{
		//		int num8 = pkg.ReadInt();
		//		for (int n = 0; n < num8; n++)
		//		{
		//			string item4 = pkg.ReadString();
		//			this.m_uniques.Add(item4);
		//		}
		//	}
		//	if (num >= 9)
		//	{
		//		int num9 = pkg.ReadInt();
		//		for (int num10 = 0; num10 < num9; num10++)
		//		{
		//			string item5 = pkg.ReadString();
		//			this.m_trophies.Add(item5);
		//		}
		//	}
		//	if (num >= 18)
		//	{
		//		int num11 = pkg.ReadInt();
		//		for (int num12 = 0; num12 < num11; num12++)
		//		{
		//			Heightmap.Biome item6 = (Heightmap.Biome)pkg.ReadInt();
		//			this.m_knownBiome.Add(item6);
		//		}
		//	}
		//	if (num >= 22)
		//	{
		//		int num13 = pkg.ReadInt();
		//		for (int num14 = 0; num14 < num13; num14++)
		//		{
		//			string key2 = pkg.ReadString();
		//			string value2 = pkg.ReadString();
		//			this.m_knownTexts.Add(key2, value2);
		//		}
		//	}
		//	if (num >= 4)
		//	{
		//		string beard = pkg.ReadString();
		//		string hair = pkg.ReadString();
		//		base.SetBeard(beard);
		//		base.SetHair(hair);
		//	}
		//	if (num >= 5)
		//	{
		//		Vector3 skinColor = pkg.ReadVector3();
		//		Vector3 hairColor = pkg.ReadVector3();
		//		this.SetSkinColor(skinColor);
		//		this.SetHairColor(hairColor);
		//	}
		//	if (num >= 11)
		//	{
		//		int playerModel = pkg.ReadInt();
		//		this.SetPlayerModel(playerModel);
		//	}
		//	if (num >= 12)
		//	{
		//		this.m_foods.Clear();
		//		int num15 = pkg.ReadInt();
		//		for (int num16 = 0; num16 < num15; num16++)
		//		{
		//			if (num >= 14)
		//			{
		//				Player.Food food = new Player.Food();
		//				food.m_name = pkg.ReadString();
		//				if (num >= 25)
		//				{
		//					food.m_time = pkg.ReadSingle();
		//				}
		//				else
		//				{
		//					food.m_health = pkg.ReadSingle();
		//					if (num >= 16)
		//					{
		//						food.m_stamina = pkg.ReadSingle();
		//					}
		//				}
		//				GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(food.m_name);
		//				if (itemPrefab == null)
		//				{
		//					ZLog.LogWarning("FAiled to find food item " + food.m_name);
		//				}
		//				else
		//				{
		//					food.m_item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
		//					this.m_foods.Add(food);
		//				}
		//			}
		//			else
		//			{
		//				pkg.ReadString();
		//				pkg.ReadSingle();
		//				pkg.ReadSingle();
		//				pkg.ReadSingle();
		//				pkg.ReadSingle();
		//				pkg.ReadSingle();
		//				pkg.ReadSingle();
		//				if (num >= 13)
		//				{
		//					pkg.ReadSingle();
		//				}
		//			}
		//		}
		//	}
		//	if (num >= 17)
		//	{
		//		this.m_skills.Load(pkg);
		//	}
		//	this.m_isLoading = false;
		//	this.UpdateAvailablePiecesList();
		//	this.EquipIventoryItems();
		//}

		// Token: 0x060001E7 RID: 487 RVA: 0x0001055C File Offset: 0x0000E75C
		public void EquipIventoryItems()
		{
			foreach (ItemDrop.ItemData itemData in this.m_inventory.GetEquipedtems())
			{
				if (!base.EquipItem(itemData, false))
				{
					itemData.m_equiped = false;
				}
			}
		}

		//// Token: 0x060001E8 RID: 488 RVA: 0x000105C0 File Offset: 0x0000E7C0
		//public override bool CanMove()
		//{
		//	return !this.m_teleporting && !this.InCutscene() && (!this.IsEncumbered() || this.HaveStamina(0f)) && base.CanMove();
		//}

		// Token: 0x060001E9 RID: 489 RVA: 0x000105F3 File Offset: 0x0000E7F3
		public override bool IsEncumbered()
		{
			return this.m_inventory.GetTotalWeight() > this.GetMaxCarryWeight();
		}

		// Token: 0x060001EA RID: 490 RVA: 0x00010608 File Offset: 0x0000E808
		public float GetMaxCarryWeight()
		{
			float maxCarryWeight = this.m_maxCarryWeight;
			this.m_seman.ModifyMaxCarryWeight(maxCarryWeight, ref maxCarryWeight);
			return maxCarryWeight;
		}

		//// Token: 0x060001FB RID: 507 RVA: 0x00010C78 File Offset: 0x0000EE78
		//public override void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
		//{
		//	if (this.m_nview == null || !this.m_nview.IsValid())
		//	{
		//		return;
		//	}
		//	if (this.m_nview.IsOwner())
		//	{
		//		if (MessageHud.instance)
		//		{
		//			MessageHud.instance.ShowMessage(type, msg, amount, icon);
		//			return;
		//		}
		//	}
		//	else
		//	{
		//		this.m_nview.InvokeRPC("Message", new object[]
		//		{
		//			(int)type,
		//			msg,
		//			amount
		//		});
		//	}
		//}

		// Token: 0x060001FD RID: 509 RVA: 0x00010D24 File Offset: 0x0000EF24
		public static Player GetPlayer(long playerID)
		{
			foreach (Player player in Player.m_players)
			{
				if (player.GetPlayerID() == playerID)
				{
					return player;
				}
			}
			return null;
		}

		// Token: 0x060001FE RID: 510 RVA: 0x00010D80 File Offset: 0x0000EF80
		public static Player GetClosestPlayer(Vector3 point, float maxRange)
		{
			Player result = null;
			float num = 999999f;
			foreach (Player player in Player.m_players)
			{
				float num2 = Vector3.Distance(player.transform.position, point);
				if (num2 < num && num2 < maxRange)
				{
					num = num2;
					result = player;
				}
			}
			return result;
		}

		// Token: 0x060001FF RID: 511 RVA: 0x00010DF8 File Offset: 0x0000EFF8
		public static bool IsPlayerInRange(Vector3 point, float range, long playerID)
		{
			foreach (Player player in Player.m_players)
			{
				if (player.GetPlayerID() == playerID)
				{
					if (Vector3.Distance(player.transform.position, point) < range)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000200 RID: 512 RVA: 0x00010E68 File Offset: 0x0000F068
		public static void MessageAllInRange(Vector3 point, float range, MessageHud.MessageType type, string msg, Sprite icon = null)
		{
			foreach (Player player in Player.m_players)
			{
				if (Vector3.Distance(player.transform.position, point) < range)
				{
					player.Message(type, msg, 0, icon);
				}
			}
		}

		// Token: 0x06000202 RID: 514 RVA: 0x00010F38 File Offset: 0x0000F138
		public static void GetPlayersInRange(Vector3 point, float range, List<Player> players)
		{
			foreach (Player player in Player.m_players)
			{
				if (Vector3.Distance(player.transform.position, point) < range)
				{
					players.Add(player);
				}
			}
		}

		// Token: 0x06000203 RID: 515 RVA: 0x00010FA0 File Offset: 0x0000F1A0
		public static bool IsPlayerInRange(Vector3 point, float range)
		{
			using (List<Player>.Enumerator enumerator = Player.m_players.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (Vector3.Distance(enumerator.Current.transform.position, point) < range)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000206 RID: 518 RVA: 0x000110F4 File Offset: 0x0000F2F4
		public static List<Player> GetAllPlayers()
		{
			return Player.m_players;
		}

		// Token: 0x06000207 RID: 519 RVA: 0x000110FB File Offset: 0x0000F2FB
		public static Player GetRandomPlayer()
		{
			if (Player.m_players.Count == 0)
			{
				return null;
			}
			return Player.m_players[UnityEngine.Random.Range(0, Player.m_players.Count)];
		}

		//// Token: 0x06000210 RID: 528 RVA: 0x0001147C File Offset: 0x0000F67C
		//public void SetMouseLook(Vector2 mouseLook)
		//{
		//	this.m_lookYaw *= Quaternion.Euler(0f, mouseLook.x, 0f);
		//	this.m_lookPitch = Mathf.Clamp(this.m_lookPitch - mouseLook.y, -89f, 89f);
		//	this.UpdateEyeRotation();
		//	this.m_lookDir = this.m_eye.forward;
		//	if (this.m_lookTransitionTime > 0f && mouseLook != Vector2.zero)
		//	{
		//		this.m_lookTransitionTime = 0f;
		//	}
		//}

		//// Token: 0x06000211 RID: 529 RVA: 0x0001150D File Offset: 0x0000F70D
		//public override void UpdateEyeRotation()
		//{
		//	this.m_eye.rotation = this.m_lookYaw * Quaternion.Euler(this.m_lookPitch, 0f, 0f);
		//}

		// Token: 0x06000212 RID: 530 RVA: 0x0001153A File Offset: 0x0000F73A
		public Ragdoll GetRagdoll()
		{
			return this.m_ragdoll;
		}

		// Token: 0x06000213 RID: 531 RVA: 0x00011542 File Offset: 0x0000F742
		public void OnDodgeMortal()
		{
			this.m_dodgeInvincible = false;
		}

		// Token: 0x06000214 RID: 532 RVA: 0x0001154C File Offset: 0x0000F74C
		public void UpdateDodge(float dt)
		{
			this.m_queuedDodgeTimer -= dt;
			if (this.m_queuedDodgeTimer > 0f && base.IsOnGround() && !this.IsDead() && !this.InAttack() && !this.IsEncumbered() && !this.InDodge() && !base.IsStaggering())
			{
				float num = this.m_dodgeStaminaUsage - this.m_dodgeStaminaUsage * this.m_equipmentMovementModifier;
				if (this.HaveStamina(num))
				{
					this.AbortEquipQueue();
					this.m_queuedDodgeTimer = 0f;
					this.m_dodgeInvincible = true;
					base.transform.rotation = Quaternion.LookRotation(this.m_queuedDodgeDir);
					this.m_body.rotation = base.transform.rotation;
					this.m_zanim.SetTrigger("dodge");
					base.AddNoise(5f);
					this.UseStamina(num);
					this.m_dodgeEffects.Create(base.transform.position, Quaternion.identity, base.transform, 1f, -1);
				}
				else
				{
					Hud.instance.StaminaBarNoStaminaFlash();
				}
			}
			AnimatorStateInfo currentAnimatorStateInfo = this.m_animator.GetCurrentAnimatorStateInfo(0);
			AnimatorStateInfo nextAnimatorStateInfo = this.m_animator.GetNextAnimatorStateInfo(0);
			bool flag = this.m_animator.IsInTransition(0);
			bool flag2 = this.m_animator.GetBool("dodge") || (currentAnimatorStateInfo.tagHash == Player.m_animatorTagDodge && !flag) || (flag && nextAnimatorStateInfo.tagHash == Player.m_animatorTagDodge);
			bool value = flag2 && this.m_dodgeInvincible;
			this.m_nview.GetZDO().Set("dodgeinv", value);
			this.m_inDodge = flag2;
		}

		// Token: 0x06000215 RID: 533 RVA: 0x00011705 File Offset: 0x0000F905
		public override bool IsDodgeInvincible()
		{
			return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("dodgeinv", false);
		}

		// Token: 0x06000216 RID: 534 RVA: 0x0001172C File Offset: 0x0000F92C
		public override bool InDodge()
		{
			return this.m_nview.IsValid() && this.m_nview.IsOwner() && this.m_inDodge;
		}

		// Token: 0x06000217 RID: 535 RVA: 0x00011750 File Offset: 0x0000F950
		public override bool IsDead()
		{
			ZDO zdo = this.m_nview.GetZDO();
			return zdo != null && zdo.GetBool("dead", false);
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0001177A File Offset: 0x0000F97A
		public void Dodge(Vector3 dodgeDir)
		{
			this.m_queuedDodgeTimer = 0.5f;
			this.m_queuedDodgeDir = dodgeDir;
		}

		// Token: 0x0600021A RID: 538 RVA: 0x00011818 File Offset: 0x0000FA18
		public override bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
		{
			if (!this.m_nview.IsOwner())
			{
				this.m_nview.InvokeRPC("RPC_TeleportTo", new object[]
				{
					pos,
					rot,
					distantTeleport
				});
				return false;
			}
			if (this.IsTeleporting())
			{
				return false;
			}
			if (this.m_teleportCooldown < 2f)
			{
				return false;
			}
			this.m_teleporting = true;
			this.m_distantTeleport = distantTeleport;
			this.m_teleportTimer = 0f;
			this.m_teleportCooldown = 0f;
			this.m_teleportFromPos = base.transform.position;
			this.m_teleportFromRot = base.transform.rotation;
			this.m_teleportTargetPos = pos;
			this.m_teleportTargetRot = rot;
			return true;
		}

		// Token: 0x0600021B RID: 539 RVA: 0x000118D4 File Offset: 0x0000FAD4
		public void UpdateTeleport(float dt)
		{
			if (!this.m_teleporting)
			{
				this.m_teleportCooldown += dt;
				return;
			}
			this.m_teleportCooldown = 0f;
			this.m_teleportTimer += dt;
			if (this.m_teleportTimer > 2f)
			{
				Vector3 dir = this.m_teleportTargetRot * Vector3.forward;
				base.transform.position = this.m_teleportTargetPos;
				base.transform.rotation = this.m_teleportTargetRot;
				this.m_body.velocity = Vector3.zero;
				this.m_maxAirAltitude = base.transform.position.y;
				base.SetLookDir(dir, 0f);
				if ((this.m_teleportTimer > 8f || !this.m_distantTeleport) && ZNetScene.instance.IsAreaReady(this.m_teleportTargetPos))
				{
					float num = 0f;
					if (ZoneSystem.instance.FindFloor(this.m_teleportTargetPos, out num))
					{
						this.m_teleportTimer = 0f;
						this.m_teleporting = false;
						base.ResetCloth();
						return;
					}
					if (this.m_teleportTimer > 15f || !this.m_distantTeleport)
					{
						if (this.m_distantTeleport)
						{
							Vector3 position = base.transform.position;
							position.y = ZoneSystem.instance.GetSolidHeight(this.m_teleportTargetPos) + 0.5f;
							base.transform.position = position;
						}
						else
						{
							base.transform.rotation = this.m_teleportFromRot;
							base.transform.position = this.m_teleportFromPos;
							this.m_maxAirAltitude = base.transform.position.y;
							this.Message(MessageHud.MessageType.Center, "$msg_portal_blocked", 0, null);
						}
						this.m_teleportTimer = 0f;
						this.m_teleporting = false;
						base.ResetCloth();
					}
				}
			}
		}

		// Token: 0x0600021C RID: 540 RVA: 0x00011A9B File Offset: 0x0000FC9B
		public override bool IsTeleporting()
		{
			return this.m_teleporting;
		}

		// Token: 0x0600021D RID: 541 RVA: 0x00011AA3 File Offset: 0x0000FCA3
		public bool ShowTeleportAnimation()
		{
			return this.m_teleporting && this.m_distantTeleport;
		}

		// Token: 0x0600021E RID: 542 RVA: 0x00011AB5 File Offset: 0x0000FCB5
		public void SetPlayerModel(int index)
		{
			if (this.m_modelIndex == index)
			{
				return;
			}
			this.m_modelIndex = index;
			this.m_visEquipment.SetModel(index);
		}

		// Token: 0x0600021F RID: 543 RVA: 0x00011AD4 File Offset: 0x0000FCD4
		public int GetPlayerModel()
		{
			return this.m_modelIndex;
		}

		// Token: 0x06000220 RID: 544 RVA: 0x00011ADC File Offset: 0x0000FCDC
		public void SetSkinColor(Vector3 color)
		{
			if (color == this.m_skinColor)
			{
				return;
			}
			this.m_skinColor = color;
			this.m_visEquipment.SetSkinColor(this.m_skinColor);
		}

		// Token: 0x06000221 RID: 545 RVA: 0x00011B05 File Offset: 0x0000FD05
		public void SetHairColor(Vector3 color)
		{
			if (this.m_hairColor == color)
			{
				return;
			}
			this.m_hairColor = color;
			this.m_visEquipment.SetHairColor(this.m_hairColor);
		}

		// Token: 0x06000222 RID: 546 RVA: 0x00011B2E File Offset: 0x0000FD2E
		public override void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
		{
			base.SetupVisEquipment(visEq, isRagdoll);
			visEq.SetModel(this.m_modelIndex);
			visEq.SetSkinColor(this.m_skinColor);
			visEq.SetHairColor(this.m_hairColor);
		}

		//// Token: 0x06000223 RID: 547 RVA: 0x00011B5C File Offset: 0x0000FD5C
		//public override bool CanConsumeItem(ItemDrop.ItemData item)
		//{
		//	if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
		//	{
		//		return false;
		//	}
		//	if (item.m_shared.m_food > 0f && !this.CanEat(item, true))
		//	{
		//		return false;
		//	}
		//	if (item.m_shared.m_consumeStatusEffect)
		//	{
		//		StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
		//		if (this.m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.name) || this.m_seman.HaveStatusEffectCategory(consumeStatusEffect.m_category))
		//		{
		//			this.Message(MessageHud.MessageType.Center, "$msg_cantconsume", 0, null);
		//			return false;
		//		}
		//	}
		//	return true;
		//}

		//// Token: 0x06000224 RID: 548 RVA: 0x00011BF8 File Offset: 0x0000FDF8
		//public override bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item)
		//{
		//	if (!this.CanConsumeItem(item))
		//	{
		//		return false;
		//	}
		//	if (item.m_shared.m_consumeStatusEffect)
		//	{
		//		StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
		//		this.m_seman.AddStatusEffect(item.m_shared.m_consumeStatusEffect, true);
		//	}
		//	if (item.m_shared.m_food > 0f)
		//	{
		//		this.EatFood(item);
		//	}
		//	inventory.RemoveOneItem(item);
		//	return true;
		//}

		// Token: 0x06000229 RID: 553 RVA: 0x00011D2F File Offset: 0x0000FF2F
		public void SetMaxHealth(float health, bool flashBar)
		{
			if (flashBar && Hud.instance != null && health > base.GetMaxHealth())
			{
				Hud.instance.FlashHealthBar();
			}
			base.SetMaxHealth(health);
		}

		// Token: 0x0600022E RID: 558 RVA: 0x00011E10 File Offset: 0x00010010
		public bool StartEmote(string emote, bool oneshot = true)
		{
			if (!this.CanMove() || this.InAttack() || this.IsHoldingAttack() || this.IsAttached() || this.IsAttachedToShip())
			{
				return false;
			}
			this.SetCrouch(false);
			int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
			this.m_nview.GetZDO().Set("emoteID", @int + 1);
			this.m_nview.GetZDO().Set("emote", emote);
			this.m_nview.GetZDO().Set("emote_oneshot", oneshot);
			return true;
		}

		// Token: 0x0600022F RID: 559 RVA: 0x00011EAC File Offset: 0x000100AC
		public override void StopEmote()
		{
			if (this.m_nview.GetZDO().GetString("emote", "") != "")
			{
				int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
				this.m_nview.GetZDO().Set("emoteID", @int + 1);
				this.m_nview.GetZDO().Set("emote", "");
			}
		}

		// Token: 0x06000230 RID: 560 RVA: 0x00011F28 File Offset: 0x00010128
		public void UpdateEmote()
		{
			if (this.m_nview.IsOwner() && this.InEmote() && this.m_moveDir != Vector3.zero)
			{
				this.StopEmote();
			}
			int @int = this.m_nview.GetZDO().GetInt("emoteID", 0);
			if (@int != this.m_emoteID)
			{
				this.m_emoteID = @int;
				if (!string.IsNullOrEmpty(this.m_emoteState))
				{
					this.m_animator.SetBool("emote_" + this.m_emoteState, false);
				}
				this.m_emoteState = "";
				this.m_animator.SetTrigger("emote_stop");
				string @string = this.m_nview.GetZDO().GetString("emote", "");
				if (!string.IsNullOrEmpty(@string))
				{
					bool @bool = this.m_nview.GetZDO().GetBool("emote_oneshot", false);
					this.m_animator.ResetTrigger("emote_stop");
					if (@bool)
					{
						this.m_animator.SetTrigger("emote_" + @string);
						return;
					}
					this.m_emoteState = @string;
					this.m_animator.SetBool("emote_" + @string, true);
				}
			}
		}

		// Token: 0x06000231 RID: 561 RVA: 0x00012050 File Offset: 0x00010250
		public override bool InEmote()
		{
			return !string.IsNullOrEmpty(this.m_emoteState) || this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Player.m_animatorTagEmote;
		}

		// Token: 0x06000232 RID: 562 RVA: 0x00012088 File Offset: 0x00010288
		public override bool IsCrouching()
		{
			return this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Player.m_animatorTagCrouch;
		}

		// Token: 0x06000233 RID: 563 RVA: 0x000120B0 File Offset: 0x000102B0
		public void UpdateCrouch(float dt)
		{
			if (this.m_crouchToggled)
			{
				if (!this.HaveStamina(0f) || base.IsSwiming() || this.InBed() || this.InPlaceMode() || this.m_run || this.IsBlocking() || base.IsFlying())
				{
					this.SetCrouch(false);
				}
				bool flag = this.InAttack() || this.IsHoldingAttack();
				this.m_zanim.SetBool(Player.crouching, this.m_crouchToggled && !flag);
				return;
			}
			this.m_zanim.SetBool(Player.crouching, false);
		}

		// Token: 0x06000234 RID: 564 RVA: 0x0001214C File Offset: 0x0001034C
		public override void SetCrouch(bool crouch)
		{
			if (this.m_crouchToggled == crouch)
			{
				return;
			}
			this.m_crouchToggled = crouch;
		}

		// Token: 0x0600023B RID: 571 RVA: 0x000122F4 File Offset: 0x000104F4
		public override void AttachStart(Transform attachPoint, GameObject colliderRoot, bool hideWeapons, bool isBed, bool onShip, string attachAnimation, Vector3 detachOffset)
		{
			if (this.m_attached)
			{
				return;
			}
			this.m_attached = true;
			this.m_attachedToShip = onShip;
			this.m_attachPoint = attachPoint;
			this.m_detachOffset = detachOffset;
			this.m_attachAnimation = attachAnimation;
			this.m_zanim.SetBool(attachAnimation, true);
			this.m_nview.GetZDO().Set("inBed", isBed);
			if (colliderRoot != null)
			{
				this.m_attachColliders = colliderRoot.GetComponentsInChildren<Collider>();
				ZLog.Log("Ignoring " + this.m_attachColliders.Length.ToString() + " colliders");
				foreach (Collider collider in this.m_attachColliders)
				{
					Physics.IgnoreCollision(this.m_collider, collider, true);
				}
			}
			if (hideWeapons)
			{
				base.HideHandItems();
			}
			this.UpdateAttach();
			base.ResetCloth();
		}

		// Token: 0x0600023C RID: 572 RVA: 0x000123CC File Offset: 0x000105CC
		public void UpdateAttach()
		{
			if (this.m_attached)
			{
				if (this.m_attachPoint != null)
				{
					base.transform.position = this.m_attachPoint.position;
					base.transform.rotation = this.m_attachPoint.rotation;
					Rigidbody componentInParent = this.m_attachPoint.GetComponentInParent<Rigidbody>();
					this.m_body.useGravity = false;
					this.m_body.velocity = (componentInParent ? componentInParent.GetPointVelocity(base.transform.position) : Vector3.zero);
					this.m_body.angularVelocity = Vector3.zero;
					this.m_maxAirAltitude = base.transform.position.y;
					return;
				}
				this.AttachStop();
			}
		}

		// Token: 0x0600023D RID: 573 RVA: 0x00012491 File Offset: 0x00010691
		public override bool IsAttached()
		{
			return this.m_attached;
		}

		// Token: 0x0600023E RID: 574 RVA: 0x00012499 File Offset: 0x00010699
		public override bool IsAttachedToShip()
		{
			return this.m_attached && this.m_attachedToShip;
		}

		// Token: 0x06000240 RID: 576 RVA: 0x000124D2 File Offset: 0x000106D2
		public override bool InBed()
		{
			return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("inBed", false);
		}

		// Token: 0x06000241 RID: 577 RVA: 0x000124FC File Offset: 0x000106FC
		public override void AttachStop()
		{
			if (this.m_sleeping)
			{
				return;
			}
			if (this.m_attached)
			{
				if (this.m_attachPoint != null)
				{
					base.transform.position = this.m_attachPoint.TransformPoint(this.m_detachOffset);
				}
				if (this.m_attachColliders != null)
				{
					foreach (Collider collider in this.m_attachColliders)
					{
						if (collider)
						{
							Physics.IgnoreCollision(this.m_collider, collider, false);
						}
					}
					this.m_attachColliders = null;
				}
				this.m_body.useGravity = true;
				this.m_attached = false;
				this.m_attachPoint = null;
				this.m_zanim.SetBool(this.m_attachAnimation, false);
				this.m_nview.GetZDO().Set("inBed", false);
				base.ResetCloth();
			}
		}

		//// Token: 0x0600024A RID: 586 RVA: 0x00012794 File Offset: 0x00010994
		//public void SetControls(Vector3 movedir, bool attack, bool attackHold, bool secondaryAttack, bool secondaryAttackHold, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun)
		//{
		//	if ((this.IsAttached() || this.InEmote()) && (movedir != Vector3.zero || attack || secondaryAttack || block || blockHold || jump || crouch) && this.GetDoodadController() == null)
		//	{
		//		attack = false;
		//		attackHold = false;
		//		secondaryAttack = false;
		//		secondaryAttackHold = false;
		//		this.StopEmote();
		//		this.AttachStop();
		//	}
		//	if (this.m_doodadController != null)
		//	{
		//		this.SetDoodadControlls(ref movedir, ref this.m_lookDir, ref run, ref autoRun, blockHold);
		//		if (jump || attack || secondaryAttack)
		//		{
		//			attack = false;
		//			attackHold = false;
		//			secondaryAttack = false;
		//			secondaryAttackHold = false;
		//			this.StopDoodadControl();
		//		}
		//	}
		//	if (run)
		//	{
		//		this.m_walk = false;
		//	}
		//	if (!this.m_autoRun)
		//	{
		//		Vector3 lookDir = this.m_lookDir;
		//		lookDir.y = 0f;
		//		lookDir.Normalize();
		//		this.m_moveDir = movedir.z * lookDir + movedir.x * Vector3.Cross(Vector3.up, lookDir);
		//	}
		//	if (!this.m_autoRun && autoRun && !this.InPlaceMode())
		//	{
		//		this.m_autoRun = true;
		//		this.SetCrouch(false);
		//		this.m_moveDir = this.m_lookDir;
		//		this.m_moveDir.y = 0f;
		//		this.m_moveDir.Normalize();
		//	}
		//	else if (this.m_autoRun)
		//	{
		//		if (attack || jump || crouch || movedir != Vector3.zero || this.InPlaceMode() || attackHold || secondaryAttackHold)
		//		{
		//			this.m_autoRun = false;
		//		}
		//		else if (autoRun || blockHold)
		//		{
		//			this.m_moveDir = this.m_lookDir;
		//			this.m_moveDir.y = 0f;
		//			this.m_moveDir.Normalize();
		//			blockHold = false;
		//			block = false;
		//		}
		//	}
		//	this.m_attack = attack;
		//	this.m_attackHold = attackHold;
		//	this.m_secondaryAttack = secondaryAttack;
		//	this.m_secondaryAttackHold = secondaryAttackHold;
		//	this.m_blocking = blockHold;
		//	this.m_run = run;
		//	if (crouch)
		//	{
		//		this.SetCrouch(!this.m_crouchToggled);
		//	}
		//	if (jump)
		//	{
		//		if (this.m_blocking)
		//		{
		//			Vector3 dodgeDir = this.m_moveDir;
		//			if (dodgeDir.magnitude < 0.1f)
		//			{
		//				dodgeDir = -this.m_lookDir;
		//				dodgeDir.y = 0f;
		//				dodgeDir.Normalize();
		//			}
		//			this.Dodge(dodgeDir);
		//			return;
		//		}
		//		if (this.IsCrouching() || this.m_crouchToggled)
		//		{
		//			Vector3 dodgeDir2 = this.m_moveDir;
		//			if (dodgeDir2.magnitude < 0.1f)
		//			{
		//				dodgeDir2 = this.m_lookDir;
		//				dodgeDir2.y = 0f;
		//				dodgeDir2.Normalize();
		//			}
		//			this.Dodge(dodgeDir2);
		//			return;
		//		}
		//		base.Jump();
		//	}
		//}

		//// Token: 0x06000251 RID: 593 RVA: 0x00012B30 File Offset: 0x00010D30
		//public override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
		//{
		//	if (this.m_chestItem != null)
		//	{
		//		mods.Apply(this.m_chestItem.m_shared.m_damageModifiers);
		//	}
		//	if (this.m_legItem != null)
		//	{
		//		mods.Apply(this.m_legItem.m_shared.m_damageModifiers);
		//	}
		//	if (this.m_helmetItem != null)
		//	{
		//		mods.Apply(this.m_helmetItem.m_shared.m_damageModifiers);
		//	}
		//	if (this.m_shoulderItem != null)
		//	{
		//		mods.Apply(this.m_shoulderItem.m_shared.m_damageModifiers);
		//	}
		//}

		// Token: 0x06000252 RID: 594 RVA: 0x00012BB8 File Offset: 0x00010DB8
		public override float GetBodyArmor()
		{
			float num = 0f;
			if (this.m_chestItem != null)
			{
				num += this.m_chestItem.GetArmor();
			}
			if (this.m_legItem != null)
			{
				num += this.m_legItem.GetArmor();
			}
			if (this.m_helmetItem != null)
			{
				num += this.m_helmetItem.GetArmor();
			}
			if (this.m_shoulderItem != null)
			{
				num += this.m_shoulderItem.GetArmor();
			}
			return num;
		}

		//// Token: 0x06000256 RID: 598 RVA: 0x00012E40 File Offset: 0x00011040
		//public override bool InAttack()
		//{
		//	if (this.m_animator.IsInTransition(0))
		//	{
		//		return this.m_animator.GetNextAnimatorStateInfo(0).tagHash == Humanoid.m_animatorTagAttack || this.m_animator.GetNextAnimatorStateInfo(1).tagHash == Humanoid.m_animatorTagAttack;
		//	}
		//	return this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash == Humanoid.m_animatorTagAttack || this.m_animator.GetCurrentAnimatorStateInfo(1).tagHash == Humanoid.m_animatorTagAttack;
		//}

		//// Token: 0x06000257 RID: 599 RVA: 0x00012ED2 File Offset: 0x000110D2
		//public override float GetEquipmentMovementModifier()
		//{
		//	return this.m_equipmentMovementModifier;
		//}

		//// Token: 0x0600025A RID: 602 RVA: 0x00012F24 File Offset: 0x00011124
		//public override bool InMinorAction()
		//{
		//	return (this.m_animator.IsInTransition(1) ? this.m_animator.GetNextAnimatorStateInfo(1) : this.m_animator.GetCurrentAnimatorStateInfo(1)).tagHash == Player.m_animatorTagMinorAction;
		//}

		// Token: 0x0600025B RID: 603 RVA: 0x00012F68 File Offset: 0x00011168
		public override bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Vector3 relativeVel)
		{
			if (this.m_attached && this.m_attachPoint)
			{
				ZNetView componentInParent = this.m_attachPoint.GetComponentInParent<ZNetView>();
				if (componentInParent && componentInParent.IsValid())
				{
					parent = componentInParent.GetZDO().m_uid;
					if (componentInParent.GetComponent<Character>() != null)
					{
						attachJoint = this.m_attachPoint.name;
						relativePos = Vector3.zero;
					}
					else
					{
						attachJoint = "";
						relativePos = componentInParent.transform.InverseTransformPoint(base.transform.position);
					}
					relativeVel = Vector3.zero;
					return true;
				}
			}
			return base.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeVel);
		}

		//// Token: 0x06000260 RID: 608 RVA: 0x000130A8 File Offset: 0x000112A8
		//public override void DamageArmorDurability(HitData hit)
		//{
		//	List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		//	if (this.m_chestItem != null)
		//	{
		//		list.Add(this.m_chestItem);
		//	}
		//	if (this.m_legItem != null)
		//	{
		//		list.Add(this.m_legItem);
		//	}
		//	if (this.m_helmetItem != null)
		//	{
		//		list.Add(this.m_helmetItem);
		//	}
		//	if (this.m_shoulderItem != null)
		//	{
		//		list.Add(this.m_shoulderItem);
		//	}
		//	if (list.Count == 0)
		//	{
		//		return;
		//	}
		//	float num = hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage();
		//	if (num <= 0f)
		//	{
		//		return;
		//	}
		//	int index = UnityEngine.Random.Range(0, list.Count);
		//	ItemDrop.ItemData itemData = list[index];
		//	itemData.m_durability = Mathf.Max(0f, itemData.m_durability - num);
		//}




		//// Token: 0x06000269 RID: 617 RVA: 0x000134E0 File Offset: 0x000116E0
		//public void ResetCharacter()
		//{
		//	this.m_guardianPowerCooldown = 0f;
		//	Player.ResetSeenTutorials();
		//	this.m_knownRecipes.Clear();
		//	this.m_knownStations.Clear();
		//	this.m_knownMaterial.Clear();
		//	this.m_uniques.Clear();
		//	this.m_trophies.Clear();
		//	this.m_skills.Clear();
		//	this.m_knownBiome.Clear();
		//	this.m_knownTexts.Clear();
		//}



		// Hirdmandr Specific additions

		public HirdmandrNPC m_hmnpc;

		// Token: 0x0400016D RID: 365
		public float m_baseValueUpdatetimer;

		// Token: 0x0400017D RID: 381
		public float m_dodgeStaminaUsage = 10f;

		// Token: 0x0400017E RID: 382
		public float m_weightStaminaFactor = 0.1f;

		// Token: 0x0400017F RID: 383
		public float m_autoPickupRange = 2f;

		// Token: 0x04000180 RID: 384
		public float m_maxCarryWeight = 300f;

		// Token: 0x04000186 RID: 390
		public EffectList m_drownEffects = new EffectList();

		// Token: 0x04000187 RID: 391
		public EffectList m_spawnEffects = new EffectList();

		// Token: 0x04000188 RID: 392
		public EffectList m_removeEffects = new EffectList();

		// Token: 0x04000189 RID: 393
		public EffectList m_dodgeEffects = new EffectList();

		// Token: 0x0400018A RID: 394
		public EffectList m_autopickupEffects = new EffectList();

		// Token: 0x0400018B RID: 395
		public EffectList m_skillLevelupEffects = new EffectList();

		// Token: 0x0400018C RID: 396
		public EffectList m_equipStartEffects = new EffectList();

		// Token: 0x0400018D RID: 397
		public GameObject m_placeMarker;

		// Token: 0x0400018E RID: 398
		public GameObject m_tombstone;

		// Token: 0x0400019F RID: 415
		public bool m_debugFly;

		// Token: 0x040001A0 RID: 416
		public bool m_godMode;

		// Token: 0x040001A1 RID: 417
		public bool m_ghostMode;

		// Token: 0x040001A2 RID: 418
		public float m_lookPitch;

		// Token: 0x040001A3 RID: 419
		public float m_baseHP = 25f;

		// Token: 0x040001B2 RID: 434
		public float m_lastToolUseTime;

		// Token: 0x040001C1 RID: 449
		public bool m_pvp;

		// Token: 0x040001C2 RID: 450
		public float m_updateCoverTimer;

		// Token: 0x040001C3 RID: 451
		public float m_coverPercentage;

		// Token: 0x040001C4 RID: 452
		public bool m_underRoof = true;

		// Token: 0x040001C5 RID: 453
		public float m_nearFireTimer;

		// Token: 0x040001C6 RID: 454
		public bool m_isLoading;

		// Token: 0x040001C7 RID: 455
		public float m_queuedAttackTimer;

		// Token: 0x040001C8 RID: 456
		public float m_queuedSecondAttackTimer;

		// Token: 0x040001C9 RID: 457
		public float m_queuedDodgeTimer;

		// Token: 0x040001CA RID: 458
		public Vector3 m_queuedDodgeDir = Vector3.zero;

		// Token: 0x040001CB RID: 459
		public bool m_inDodge;

		// Token: 0x040001CC RID: 460
		public bool m_dodgeInvincible;

		// Token: 0x040001CD RID: 461
		public CraftingStation m_currentStation;

		// Token: 0x040001CE RID: 462
		public bool m_inCraftingStation;

		// Token: 0x040001CF RID: 463
		public Ragdoll m_ragdoll;

		// Token: 0x040001D1 RID: 465
		public string m_emoteState = "";

		// Token: 0x040001D2 RID: 466
		public int m_emoteID;

		// Token: 0x040001D5 RID: 469
		public bool m_crouchToggled;

		// Token: 0x040001D6 RID: 470
		public bool m_autoRun;

		// Token: 0x040001D7 RID: 471
		public bool m_safeInHome;

		// Token: 0x040001D9 RID: 473
		public bool m_attached;

		// Token: 0x040001DA RID: 474
		public string m_attachAnimation = "";

		// Token: 0x040001DB RID: 475
		public bool m_sleeping;

		// Token: 0x040001DC RID: 476
		public bool m_attachedToShip;

		// Token: 0x040001DD RID: 477
		public Transform m_attachPoint;

		// Token: 0x040001DE RID: 478
		public Vector3 m_detachOffset = Vector3.zero;

		// Token: 0x040001DF RID: 479
		public Collider[] m_attachColliders;

		// Token: 0x040001E0 RID: 480
		public int m_modelIndex;

		// Token: 0x040001E1 RID: 481
		public Vector3 m_skinColor = Vector3.one;

		// Token: 0x040001E2 RID: 482
		public Vector3 m_hairColor = Vector3.one;

		// Token: 0x040001E3 RID: 483
		public bool m_teleporting;

		// Token: 0x040001E4 RID: 484
		public bool m_distantTeleport;

		// Token: 0x040001E5 RID: 485
		public float m_teleportTimer;

		// Token: 0x040001E6 RID: 486
		public float m_teleportCooldown;

		// Token: 0x040001E7 RID: 487
		public Vector3 m_teleportFromPos;

		// Token: 0x040001E8 RID: 488
		public Quaternion m_teleportFromRot;

		// Token: 0x040001E9 RID: 489
		public Vector3 m_teleportTargetPos;

		// Token: 0x040001EA RID: 490
		public Quaternion m_teleportTargetRot;

		// Token: 0x040001EB RID: 491
		public Heightmap.Biome m_currentBiome;

		// Token: 0x040001EC RID: 492
		public float m_biomeTimer;

		// Token: 0x040001ED RID: 493
		public int m_baseValue;

		// Token: 0x040001EE RID: 494
		public int m_comfortLevel;

		// Token: 0x040001EF RID: 495
		public float m_drownDamageTimer;

		// Token: 0x040001F0 RID: 496
		public float m_timeSinceTargeted;

		// Token: 0x040001F1 RID: 497
		public float m_timeSinceSensed;

		// Token: 0x040001F2 RID: 498
		public float m_stealthFactorUpdateTimer;

		// Token: 0x040001F3 RID: 499
		public float m_stealthFactor;

		// Token: 0x040001F4 RID: 500
		public float m_stealthFactorTarget;

		// Token: 0x040001F5 RID: 501
		public Vector3 m_lastStealthPosition = Vector3.zero;

		// Token: 0x040001F6 RID: 502
		public float m_wakeupTimer = -1f;

		// Token: 0x040001F7 RID: 503
		public float m_timeSinceDeath = 999999f;

		// Token: 0x040001F8 RID: 504
		public DateTime m_wakeupTime;

		// Token: 0x040001F9 RID: 505
		public float m_runSkillImproveTimer;

		// Token: 0x040001FA RID: 506
		public float m_swimSkillImproveTimer;

		// Token: 0x040001FB RID: 507
		public float m_sneakSkillImproveTimer;

		// Token: 0x040001FC RID: 508
		public float m_equipmentMovementModifier;

		// Token: 0x040001FD RID: 509
		public static int crouching = 0;

		// Token: 0x040001FE RID: 510
		public static int m_attackMask = 0;

		// Token: 0x040001FF RID: 511
		public static int m_animatorTagDodge = Animator.StringToHash("dodge");

		// Token: 0x04000200 RID: 512
		public static int m_animatorTagCutscene = Animator.StringToHash("cutscene");

		// Token: 0x04000201 RID: 513
		public static int m_animatorTagCrouch = Animator.StringToHash("crouch");

		// Token: 0x04000202 RID: 514
		public static int m_animatorTagMinorAction = Animator.StringToHash("minoraction");

		// Token: 0x04000203 RID: 515
		public static int m_animatorTagEmote = Animator.StringToHash("emote");

    }
}
