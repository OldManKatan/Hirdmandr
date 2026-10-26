# Farmer Artisan Job — Implementation Plan

> **Companion to** `artisan_farmer_por.md` (design/PoR). Each step references the PoR sections it needs.
> **This file is the single source of truth for progress.** After any `/compact`, re-orient using the protocol below — do not rely on conversation memory.

## How to use this plan (the loop)
Every step is a **self-contained, compiling increment**. Work **strictly sequentially** (no parallel work).

**Re-orientation protocol (run after EVERY `/compact`):**
1. Open this file; find the first step whose checkbox is **unchecked** → that is **the** step.
2. Read the PoR (`artisan_farmer_por.md`) sections that step lists.
3. Open the step's **Files**. The files are the source of truth for exact APIs — **mimic the named existing pattern**.
4. Perform the **Actions**.
5. **Build** (command below) — it **must** succeed before the step is done.
6. If the step has an in-game **Verify**, Thomas runs it; record the result where noted.
7. Tick the step's checkbox. Then `/compact` and start the next step.

**Do not begin implementation until Thomas gives the go-ahead for Step 1.**

**Build:**
```
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\OpenCode\hirdmandr_project\hirmandr_repo\Hirdmandr.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU" /p:ExecutePrebuild=false /nologo /m
```
**Deploy** (only when play-testing): run `publish.ps1` → `D:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\Hirdmandr\`.

---

## Confirmed constants (Step 1 ✅ — from in-game `/hirdmandr farmscan`, 2 areas)

**Mature crop class = `Pickable`** (confirmed: the loaded mature crops are `Pickable`s; harvest via `Pickable.Interact`). All player-planted crops report `m_respawnTimeMinutes = 0` (wild pickables like Cloudberry report 300) — a corroborating "player-planted" signal alongside crop-table membership.

**Cultivator (tool):** `$item_cultivator` (ObjectDB `Cultivator`).

### Crop table (replant is per mature crop; scope = **everything + mushrooms**)
> **Model (design doc / Thomas):** the NPC replants to reproduce the **same** mature crop at that spot. **Two-tier** crops cross-sustain — the food crop is replanted with the *seed* item, the seed crop with the *food* item (food tier eats seeds → +1 food; seed tier eats food → +3 seeds; net = seed growth). **One-tier** crops "drop themselves" — replanted with their own yield. **Sustainability is the player's job:** if a crop's replant item is not in the chest, the NPC **leaves that crop alone** (no harvest, no crash, no balancing). Consume is always **×1** (one item plants one sapling). `?` = sapling name to confirm before Step 2.

**Two-tier (food + seed) crops:**
| mature crop (Pickable) | yield (item ×n) | replant consume (×1) | sapling to spawn |
|---|---|---|---|
| Carrot — food | `Pickable_Carrot` → `$item_carrot` ×1 | `$item_carrotseeds` | `sapling_carrot` |
| Carrot — seed | `Pickable_SeedCarrot` → `$item_carrotseeds` ×3 | `$item_carrot` | `sapling_seedcarrot` |
| Turnip — food | `Pickable_Turnip` → `$item_turnip` ×1 | `$item_turnipseeds` | `sapling_turnip` |
| Turnip — seed | `Pickable_SeedTurnip` → `$item_turnipseeds` ×3 | `$item_turnip` | `sapling_seedturnip` |
| Onion — food | `Pickable_Onion` → `$item_onion` ×1 | `$item_onionseeds` | `sapling_onion` |
| Onion — seed | `Pickable_SeedOnion` → `$item_onionseeds` ×3 | `$item_onion` | `sapling_seedonion` |
| Kale — food | `Pickable_Kale` → `$item_kale` ×3 | `$item_kaleseeds` | `sapling_kale`? |
| Kale — seed | `Pickable_SeedKale` → `$item_kaleseeds` ×3 | `$item_kale` | `sapling_seedkale`? |

**One-tier crops (drop themselves):**
| mature crop (Pickable) | yield (item ×n) | replant consume (×1) | sapling to spawn |
|---|---|---|---|
| Oat | `Pickable_Oat` → `$item_oatseeds` ×3 | `$item_oatseeds` | `sapling_oat`? |
| Poteitr | `Pickable_Poteitr` → `$item_poteitr` ×3 | `$item_poteitr` | `sapling_poteitr`? |
| Barley | `Pickable_Barley` → `$item_barley` ×2 | `$item_barley` | `sapling_barley` |
| Flax | `Pickable_Flax` → `$item_flax` ×2 | `$item_flax` | `sapling_flax` |
| Mushroom Magecap | `Pickable_Mushroom_Magecap` → `$item_magecap` ×3 | `$item_magecap` | `sapling_…`? |
| Mushroom Jotun Puffs | `Pickable_Mushroom_JotunPuffs` → `$item_jotunpuffs` ×3 | `$item_jotunpuffs` | `sapling_…`? |

> **MUST-EXCLUDE** (these are `Pickable`s too, but NOT crops — the crop-table gate must reject them): `Pickable_Stone` (`$item_stone`, wild stone), `CloudberryBush` (`$item_cloudberries`, wild, respawn=300). *(Mushrooms are CROPS — see one-tier table.)*
>
> **Sapling prefabs — confirmed registered in `HirdmandrChest`:** `sapling_carrot`, `sapling_seedcarrot`, `sapling_turnip`, `sapling_seedturnip`, `sapling_onion`, `sapling_seedonion`, `sapling_barley`, `sapling_flax`. **Missing (names to confirm before Step 2):** `sapling_kale`, `sapling_seedkale`, `sapling_oat`, `sapling_poteitr`, + the two mushroom saplings. Confirm each sapling's `m_grownPrefabs[0]` grows into the matching mature crop.

### API facts (compiler + Cecil, Step 1 build)
- Mature crop = `Pickable`. `Pickable.m_itemPrefab` is a **`GameObject`** (the dropped prefab), NOT `ItemData` — internal item name = `m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name`; ObjectDB name = `m_itemPrefab.name`.
- Other `Pickable` fields: `m_amount` (int), `m_respawnTimeMinutes` (float), `m_harvestable` (bool), `m_harvestStat` (**`PlayerStatType`** = the farming skill).
- Harvest entry = `Pickable.Interact(Humanoid, bool, bool)`. `ItemDrop.ItemData` amount field is **`m_stack`** (not `m_itemAmount`).
- **Worksit note:** `HirdmandrChest`'s `artisanJobPrefabs["farmer"]` keys on the *growing* `sapling_*` prefabs (15 m). But the harvest target is a *mature* `Pickable` (the sapling is destroyed on `Grow()`). Steps 3/4 must detect mature `Pickable`s, not saplings.

---

## Progress
- [x] **Step 1 — Discovery scan (`/hirdmandr farmscan`)** (item strings + mature-crop class confirmed)
- [x] **Step 2 — Config** (`FarmerRange` + crop table in `Utils.cs`)
- [x] **Step 3 — Mature-crop detection** (find crop `Pickable`s in the field)
- [x] **Step 4 — Target selection + worksite validation** (the 4 conditions)
- [x] **Step 5 — Cultivator tool lifecycle** (acquire/equip/return; mirrors mender)
- [x] **Step 6 — Harvest helper** (`FarmerHarvest`)
- [x] **Step 7 — Replant helper** (`FarmerReplant`)
- [x] **Step 8 — doJob farmer branch** (harvest + consume + replant + collect + skill)
- [x] **Step 9 — setup + go farmer branches** (validate+pick; walk to target)
- [x] **Step 10 — Job selection + idle** (make farmer selectable; idle when no target)
- [x] **Step 11 — End-to-end in-game test + hardening** (core day loop verified in-game on 2 NPCs; full all-crop coverage + MP spun off to PMW #47)

> **Plan status (2026-10-14):** Steps 1–11 complete for the **core** farmer job (carrot / seed-carrot two-tier loop, verified in-game on 2 NPCs across a full day). Remaining farmer scope — **full all-crop coverage** (confirm the 6 convention-guessed saplings: kale, seedkale, oat, poteitr, + 2 mushrooms) + **MP** (harvest VFX to a 2nd player) — is tracked in **PMW #47** (P3, pool). `farmscan` + `FarmScan()`/`ObjName()` are **intentionally kept** in `HirdmandrCommand.cs` for #47's full-plant pass. Cook/Baker were dropped (10-13); their leftover code is removed (#6b/#6c done).

---

## Step 1 — Discovery scan (`/hirdmandr farmscan`)
**Goal:** In-game, log the facts everything else depends on: each mature crop's yield name + `Pickable.m_itemPrefab.name`, each seed item name, and the cultivator item name. Confirm the mature-crop class.
**PoR:** §2 (crop model), §7/§9.1/§9.3 (confirmations).
**Depends-on:** nothing.
**Files:** `Hirdmandr\HirdmandrCommand.cs` (one-shot, gated console command — NOT a per-frame scan).
**Design:** a `/hirdmandr farmscan` console command (fires only when Thomas types it; no log flooding). It dumps, relative to the local player: (1) all loaded `Pickable`s within 40 m — each crop's ObjectDB name (`m_itemPrefab.name`), internal yield name (`m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name`), `m_amount`, `m_respawnTimeMinutes`, position+dist — plus a per-yield count; (2) every `Container` within 20 m (chest contents → seed/food + cultivator item names, internal + ObjectDB). Each item printed both internal (`m_shared.m_name`, e.g. `$item_carrot` — what `LinkedHaveItem`/`HaveItem` key on) and ObjectDB name (e.g. `Carrot`).
**Status:** ✅ **DONE** — implemented + built clean + auto-deployed (code is `FarmScan()`/`ObjName()` in `HirdmandrCommand.cs`, wired via `case "farmscan"` in `Run`); **captured in-game (2 areas)**; "Confirmed constants" table filled above. `farmscan` + `FarmScan()`/`ObjName()` helpers are **intentionally kept** (NOT removed) for PMW #47's full-plant pass.
**Verify (Thomas, in-game):** stand in a small field with a few **mature** crops next to an NPC chest holding a cultivator + seeds + food; open console (F1), type `/hirdmandr farmscan`, then report the `FARMSCAN:` log lines.
**Exit state:** "Confirmed constants" table above is filled with real strings + the mature-crop class. The `farmscan` command + `FarmScan()`/`ObjName()` helpers are **intentionally kept** (not removed) for PMW #47's full-plant pass.

## Step 2 — Config
**Goal:** Add `FarmerRange` + the crop table, using Step 1's confirmed strings.
**PoR:** §6 (yield→plantable lookup), §7 (config).
**Depends-on:** Step 1 (confirmed strings).
**Files:** `Hirdmandr\Utils.cs`.
**Actions:**
1. `public static float FarmerRange = <N>;` (default ~15; Thomas may tune).
2. A crop-table type (e.g. `FarmerCrop { string yieldItem; string sapling; string plantable; }`) + a `static` collection populated with confirmed entries:
   - yield `<carrot>` → sapling `sapling_carrot`, plantable `<carrot seed>`
   - yield `<carrot seed>` → sapling `sapling_seedcarrot`, plantable `<carrot>`
   - (+ turnip/onion pairs; barley/flax single entries, per Step 1)
3. `public static string FarmerCultivatorItem = <confirmed>;`
4. Lookup helpers: `FarmerCrop ByYield(string itemName)` + a set of known yield items (used to filter in Step 3).
**Build:** must succeed.
**Exit state:** `FarmerRange`, crop table + `ByYield` + known-yield set, and `FarmerCultivatorItem` exist and compile. No behavior change.
**Status:** ✅ **DONE** — `FarmerRange = 15f` + `FarmerCrop` struct + `FarmerCrops` static class (14-entry table + `TryGetByYield`/`IsKnownYield` + `CultivatorItem`) added to `Utils.cs`; built clean (0 errors), auto-deployed. Note: `FarmerCrops.CultivatorItem` is the single cultivator const (replaces a separate `FarmerCultivatorItem`). **6 sapling names are convention-guessed** (kale, seedkale, oat, poteitr, mushroom_magecap, mushroom_jotunpuffs) — confirmed at Step 11 or a farmscan probe; Step 7's instantiation is null-guarded so a wrong name can't crash.

## Step 3 — Mature-crop detection
**Goal:** Find mature crop `Pickable`s within `FarmerRange` of a chest, filtered to crop-table yield items (excludes wild pickables and growing `Plant` saplings).
**PoR:** §2 (mature = `Pickable`), §8 (crops only).
**Depends-on:** Step 2.
**Files:** `Hirdmandr\HirdmandrChest.cs`.
**Actions:**
1. Add a chest-scoped query (e.g. `List<ZDO> GetMatureCropsInRange()`):
   - Scan for `Pickable` ZDOs within `FarmerRange` of the chest (reuse the "ZDOs in range" helper other jobs already use — mimic it).
   - Keep only those whose `m_itemPrefab.name` is a known crop-table yield item.
**Build:** must succeed.
**Exit state:** a working "mature crops in field" query. (Step 11 confirms the mature crop is really a `Pickable` in practice; if not, adjust the scan here — PoR §9.1.)
**Status:** ✅ **DONE** — `GetMatureCropsInRange()` + `YieldsOf()` added to `HirdmandrChest.cs`; built clean (0 errors), auto-deployed. **Design note:** Cecil confirmed `Pickable` extends `MonoBehaviour` (NOT `ZDO`), so the ZDO sector scan wouldn't surface a crop; the query uses `FindObjectsOfType<Pickable>()` (the mechanism Step 1's farmscan validated) + distance-to-chest filter + `FarmerCrops.IsKnownYield(yield)`. Saplings excluded for free (a `Plant` is not a `Pickable`). Not yet wired to the job (Step 9).

## Step 4 — Target selection + worksite validation
**Goal:** Implement the 4-condition worksite validation and select a harvestable target = a mature crop whose replant item is in the chest.
**PoR:** §3 (the 4 conditions in order + the "cannot harvest without being able to replace" invariant), §6 (yield→plantable).
**Depends-on:** Step 3, Step 2.
**Files:** `Hirdmandr\HirdmandrChest.cs`.
**Actions:**
1. Add `bool SelectFarmerTarget(out <target>)` (`<target>` = the crop ZDO + its crop-table entry):
   - (cond 2) `if (!LinkedHaveItem(FarmerCultivatorItem)) return false;`
   - (cond 3) `var crops = GetMatureCropsInRange(); if (crops.Count == 0) return false;`
   - (cond 3+4) for each crop: `var e = FarmerCrops.ByYield(crop.m_itemPrefab.name); if (LinkedHaveItem(e.plantable)) { target = (crop, e); return true; }`
   - `return false;`
   - (cond 1 "chest exists" is implicit — this runs on the chest.)
**Build:** must succeed.
**Exit state:** `SelectFarmerTarget` exists; the 4 conditions + invariant are implemented.
**Status:** ✅ **DONE** — `SelectFarmerTarget(out Pickable, out FarmerCrop)` added to `HirdmandrChest.cs`; built clean (0 errors), auto-deployed. Order: (1) chest implicit → (2) `LinkedHaveItem(CultivatorItem)` → (3) `GetMatureCropsInRange().Count>0` → (4) first crop whose `consumeItem` is in the chest. Invariant enforced by only returning a replantable crop. Note the plan pseudo-code's `ByYield`/`e.plantable` map to the real API `TryGetByYield(YieldsOf(pk), out e)` / `e.consumeItem`. Not yet called from the job (Step 9).

## Step 5 — Cultivator tool lifecycle
**Goal:** Acquire+equip the cultivator on farmer-job entry; Best-Equip + return it to the **source** chest on exit (any reason). Mirror the mender hammer boundary.
**PoR:** §5.
**Depends-on:** Step 2 (cultivator item); existing mender tool boundary (`HirdmandrNPC.EquipMenderHammer`/`EquipBest`, `topSM.cs` exit).
**Files:** `Hirdmandr\HirdmandrAI.cs` (fields), `Hirdmandr\HirdmandrNPC.cs` (acquire/release), `Hirdmandr\topSM.cs` (exit boundary).
**Actions:**
1. Add AI/NPC fields: `farmerSourceChest` + `farmerHoldsCultivator`.
2. **Acquire (entry):** `AcquireCultivator(chest)`: `LinkedRemoveOneItem(cultivator)` from chest → NPC inventory → equip (mimic `EquipMenderHammer`); set `farmerSourceChest = chest`, flag true.
3. **Release (exit, in the WorkDay exit boundary where the mender hammer is handled):** if `farmerHoldsCultivator` → `LinkedPutItem(cultivator)` back to `farmerSourceChest`, then `EquipBest()`; clear flag.
**Build:** must succeed.
**Exit state:** cultivator is taken+equipped on entry, returned+best-equipped on exit, to the correct chest.
**Status:** ✅ **DONE** (methods + fields + exit-release; the **acquire *call*** is Step 9). AI fields `farmerTarget`/`farmerTargetEntry`/`farmerSourceChest`/`farmerHoldsCultivator` added. `HirdmandrNPC.AcquireCultivator(chest)` (move tool chest→NPC via `Inventory.MoveItemToThis`, then `EquipItem`) + `ReleaseCultivator(chest)` (unequip, move NPC→source chest) added. `topSM.NodeWorkDay.ExitTo` now calls `ReleaseCultivator(farmerSourceChest)` before `EquipBest()`. Built clean (0 errors), auto-deployed. **Move primitive:** `Inventory.MoveItemToThis(fromInventory, item)` (Cecil-confirmed) — a true move, not copy, so the single non-spawnable tool round-trips without duplication. **To verify in Step 11:** `MoveItemToThis` on an equipped item, `UnequipItem` not dropping, and the chest slot fitting on return.

## Step 6 — Harvest helper
**Goal:** `FarmerHarvest` — record a mature crop's position, then `Pickable.Interact(npc, false, false)` (drops the yield on the ground + destroys the crop; the game's owner-spawned pick effect is MP-visible).
**PoR:** §4 (do step 3), §2, §9.4/§9.6.
**Depends-on:** Step 2 (yield lookup), Step 3/4 (the target crop ZDO).
**Files:** `Hirdmandr\sub1SM-WorkDay.cs` (private helper, near the job actions).
**Actions:**
1. Add `FarmerHarvest(npc, cropZDO, out yieldItem, out position)`:
   - `position = cropZDO.transform.position` **before** interact.
   - `var pk = cropZDO.GetComponent<Pickable>(); pk.Interact(npc, false, false);`
   - `yieldItem = cropZDO`'s `m_itemPrefab.name` (or the crop-table entry's yield).
2. **Confirm (PoR §9.4):** in Step 11, verify `Interact` fires for an NPC `Humanoid` (owner = local player ⇒ drop+destroy). If it does **not** fire for a non-Player, apply the fallback: drive the owner-side pick directly.
**Build:** must succeed.
**Exit state:** `FarmerHarvest` exists (in-game verified in Step 11).
**Status:** ✅ **DONE** (compiled; in-game behavior verified Step 11) — `FarmerHarvest(NPCPlayerClone, Pickable, out yieldItem, out position)` added to `sub1SM-WorkDay.cs` `NodeDoJob`. Records `crop.transform.position` **before** `Interact`, reads the yield internal name off `crop.m_itemPrefab`'s `ItemDrop`, then `crop.Interact(npc, false, false)`. `Interact(Humanoid, bool, bool)` Cecil-confirmed. Not yet called (Step 8).

## Step 7 — Replant helper
**Goal:** `FarmerReplant` — instantiate the matching `sapling_*` `Plant` at the recorded position (on already-cultivated ground).
**PoR:** §4 (do step 5), §9.5.
**Depends-on:** Step 2 (sapling name).
**Files:** `Hirdmandr\sub1SM-WorkDay.cs` (private helper).
**Actions:**
1. Add `FarmerReplant(position, saplingName)`: load the `sapling_*` prefab, `Instantiate` at `position`; let its `Awake`/`Start` set up (cultivated-ground check).
2. (PoR §9.5) If it won't attach/grow, apply `PlaceAgainst`/attach or set the needed `Plant` fields.
3. Plantable consumption (−1 from chest) stays with the caller (Step 8) so this helper stays pure.
**Build:** must succeed.
**Exit state:** `FarmerReplant` exists (in-game verified in Step 11).
**Status:** ✅ **DONE** (compiled; in-game behavior verified Step 11) — `FarmerReplant(Vector3, string saplingName)` added to `sub1SM-WorkDay.cs` `NodeDoJob`. Spawns the `sapling_*` `Plant` as a proper **networked ZDO** via `ZNetScene.instance.GetPrefab`/`GetPrefabHash` + `ZDOMan.instance.CreateNewZDO` → `SetPrefab`/`SetRotation`/`SetOwner(local player)` → `scene.CreateObject` → `scene.AddInstance` (mirrors the proven `RescueSpawner.PlaceAtLocation` ZDO pattern, **not** a blind `Instantiate`, so it persists + syncs in MP). `Plant` extends `SlowUpdate`(→`ZDO`) — Cecil-confirmed. Null-guards: empty name / null scene / null prefab / null CreateObject each log + return false (so an unconfirmed sapling name can't crash). Not yet called (Step 8).

## Step 8 — doJob farmer branch
**Goal:** The `doJob` farmer branch — **one plant per run**: re-validate → harvest → consume plantable → replant → collect yield → store in chest → raise farming skill → set `nextJobTime` → back to setup.
**PoR:** §4 (doJob), §6 (accounting), §9.7 (skill).
**Depends-on:** Steps 3,4 (target), 5 (cultivator held), 6 (harvest), 7 (replant), + existing ground-collect + chest + skill helpers.
**Files:** `Hirdmandr\sub1SM-WorkDay.cs` (`NodeDoJob` / `doJob` — add a farmer branch alongside furnace/kindler/mender; mimic the **furnace** branch for the collect step).
**Actions:**
1. In `doJob`, when `curJob == farmer` and a target is set:
   1. Re-validate: target still a valid mature crop in range + chest still has the plantable + cultivator still held. If any fails → back to setup.
   2. `FarmerHarvest(npc, target.crop, out yield, out pos)`.
   3. Consume 1 plantable: `LinkedRemoveOneItem(target.plantable)` from chest.
   4. `FarmerReplant(pos, target.sapling)`.
   5. Collect the dropped yield from the ground (reuse the furnace ground-collect: short wait, gather `ItemDrop`s, `LinkedPutItem` into chest).
   6. Raise farming skill (mimic how the other jobs raise theirs; PoR §9.7).
   7. Set `nextJobTime`; transition back to **setup** (pick the next crop).
**Build:** must succeed.
**Exit state:** a full harvest+replant+collect cycle runs in `doJob` (in-game verified in Step 11).
**Note:** largest step, but bounded by reusing Steps 6/7 + existing helpers. Keep it whole; only split (8a harvest+consume+replant / 8b collect+skill) if it becomes unwieldy.
**Status:** ✅ **DONE** (compiled + deployed) — the **do-loop** inside `NodeDoJob` (Thomas's directive: `setup → go → do`, then loop `goNextCrop → doFarmerStuff` inside `do`, NOT bouncing to setup per crop). **Model = the Lamplighter** (claim chest in setup, walk to it in go, do-loop in do) — *not* the Mender (the Mender is the least-standard artisan job: no chest, no tool; a poor example for tool/item jobs). The cultivator is acquired in **setupArtJob** (Step 9, from the claimed chest) and returned on exit — `NodeDoJob.EnterFrom` only resets do-loop state. Branch order: (a) no-cultivator → `resetArtJob`; (b) decoupled `FarmerCollectYields` on its own 1s cadence (like the furnace banks ingots); (c) pick via `PickFarmerTarget` (throttled 2s) → (d) idle at chest if none → (e) in-reach `DoFarmerStuff` (re-check consumeItem → `FarmerHarvest` → `LinkedRemoveOneItem` → `FarmerReplant`) + `ModifySkill("farmer", 0.5f)` + skill-based cooldown `(18f - (skill/100*9f))` → (f) out-of-reach `TryToMove` (one `farmerMoveIssued` attempt; abandon + re-scan if the walk settles out of reach). `SelectFarmerTarget` (Step 4) drops the chest-cultivator check — the cultivator is held for the whole job, so the chest no longer has one by do-loop time; its availability is gated at claim time by `ValidFarmerInventory` (Step 9). In the shared work-site block the farmer sets `workSiteObject = workJobSite` (its claimed chest), like the Lamplighter.

## Step 9 — setup + go farmer branches
**Goal:** `setup` farmer branch (acquire cultivator + validate + pick target) and `go` farmer branch (walk to the target crop).
**PoR:** §4 (setup/go), §3 (validation), §5 (acquire on entry).
**Depends-on:** Step 4, Step 5.
**Files:** `Hirdmandr\sub1SM-WorkDay.cs` (`NodeSetupArtJob` + `NodeGoArtJob`).
**Actions:**
1. **setup:** when `curJob == farmer`: on the transition **into** the farmer job, `AcquireCultivator` (Step 5) once; then `SelectFarmerTarget(out target)`; if none → not available (idle / next job time).
2. **go:** when `curJob == farmer` and a target is set → walk to `target.crop.transform.position` (interact range), mimicking how `go` walks to other jobs' fixtures.
**Build:** must succeed.
**Exit state:** the setup→go→do farmer loop is wired in the SM.
**Status:** ✅ **DONE** (compiled + deployed) — the farmer rides the **generic** setup→go path (it is *not* the Mender's skip-to-doJob special case), so: **setup** `NodeSetupArtJob` claims the chest (the claim is what stops two farmers taking the same chest/cultivator) and, on a successful claim, calls `AcquireCultivator` on the claimed chest (held for the whole job, returned on exit via topSM); **go** `NodeGoArtJob` walks the NPC to the claimed chest (generic `TryToMove`), so by `doJob` the crops are close. The farmer is a **special-case** in `GetValidWorksites` (like the Lamplighter's Fireplace scan, *not* the generic prefab loop): it's a valid worksite iff `ValidFarmerInventory()` (chest holds a cultivator) **AND** `GetMatureCropsInRange().Count > 0` (a mature crop within `FarmerRange`), adding the chest's own ZDO to `artisanJobPieces["farmer"]` + setting `hmnpc_isSite` (the `IsValidWorksite` count). Per Thomas: the farmer works on **mature** crops (its do-loop replants saplings, but only mature crops are harvestable), so the worksite check looks at mature crops, **not** growing saplings — a fully-mature field is claimable, a growing-only field is not yet. This replaced an earlier sapling-only check (`else if (aKey == "farmer")` in the generic loop, keyed on a near `sapling_*` ZDO) that would have left a fully-mature field unclaimable. `ValidFarmerInventory()` still gates the claim (a cultivator-less chest is never claimed). The pick stays in the `doJob` do-loop (Step 8) per Thomas's directive, not in setup.

## Step 10 — Job selection + idle
**Goal:** Make the farmer job selectable/assignable to an NPC (like furnace/kindler/mender) and define idle behavior when there is no target.
**PoR:** §4 (loop), §3 (no target ⇒ idle until next job time).
**Depends-on:** Steps 4,8,9; existing job-assignment system.
**Files:** `Hirdmandr\HirdmandrAI.cs` (`workJobs`/`curJob` job set + selection), + wherever the player designates jobs.
**Actions:**
1. Register `farmer` in the job set / `workJobs` (mimic furnace/kindler/mender).
2. Selection picks farmer when its worksite is valid (Step 4) and drops to idle / next job time when not (no target / no cultivator).
3. Ensure the transition **out** of farmer triggers the Step 5 release (Best-Equip + return cultivator).
**Build:** must succeed.
**Exit state:** an NPC with the farmer job enabled works a valid field and idles correctly.
**Status:** ✅ **DONE** (verified — no new code; the farmer rides the pre-existing job-selection infra, all present): (1) `farmer` is in `m_hmSkillNames` (`HMSkills.cs`); (2) the GUI has a `Farmer` toggle (`HirdmandrGUI.cs` `g_art_ch_farmer`/`FarmerToggle` → `InvokeSetting("skill:farmer", …)` → `SetSkillEnable("farmer")`); (3) `NodeResetArtJob` sets `workJobs = GetEnabledSkillsHighestFirst()` (an enabled farmer lands in `workJobs`); (4) `NodeSetupArtJob` picks `curJob = workJobs[0]` (the **highest-level** enabled skill — `GetEnabledSkillsHighestFirst` sorts by `GetSkill`), so a farmer at the top is selected exactly like woodburner/lamplighter/mender; (5) the SM setup (claim a mature-crop worksite) + do-loop (Step 8) handle `farmer`, and **idle-when-no-target** = the do-loop idles at the claimed chest until a crop is mature (`PickFarmerTarget` re-scans on its 2s cadence, so it auto-resumes when a replanted crop matures). Transition-out release (return cultivator + Best-Equip) is Step 5 (`topSM.NodeWorkDay.ExitTo`).

## Step 11 — End-to-end in-game test + hardening
**Goal:** Verify the full loop in-game and resolve the deferred confirmations.
**PoR:** §9 (all confirmations), §8 (edges).
**Depends-on:** Steps 1–10.
**Files:** whatever the test reveals (likely `sub1SM-WorkDay.cs`, `HirdmandrChest.cs`).
**Actions:**
1. In-game (field of mature carrot + seed-carrot plants near an NPC chest holding a cultivator + seeds + carrots). Verify:
   - A mature crop is harvested, replanted **in place**, and its yield lands in the chest.
   - Accounting holds across a cycle (−1 plantable, +yield) — the self-propagation loop.
   - The invariant holds: a crop whose replant item is absent is **left alone**.
   - The cultivator is equipped while working and returned to the **correct** chest on exit.
   - Harvest VFX is visible to an onlooking player (MP) — PoR §9.6.
   - Wild berries/mushrooms are **not** harvested; growing saplings + unhealthy plants are ignored.
2. Fix any deferred confirmation that failed: Interact-for-NPC fallback (§9.4), replant instantiate/attach (§9.5), mature-crop class (§9.1), two-tier strings (§9.2/9.3), skill (§9.7).
3. **Remove the Step 1 debug logging.**
**Build:** must succeed.
**Status:** ✅ **DONE (core day loop)** — verified in-game on **2 NPCs** across a full day: setup → do → interrupt → resume → cleanup (carrot + seed-carrot two-tier core loop; accounting + invariant hold; cultivator equipped while working and returned on exit). **Remaining scope spun off to PMW #47** (P3, pool): full all-crop coverage (6 convention-guessed sapling names still to confirm — kale, seedkale, oat, poteitr, + 2 mushrooms) + **MP** (harvest VFX visible to a second player, §9.6). **`farmscan` + `FarmScan()`/`ObjName()` intentionally kept** in `HirdmandrCommand.cs` for #47's full-plant pass (supersedes Step 1's "remove in Step 11").
**Exit state:** farmer job works end-to-end for the core loop; remaining full-plant + MP scope is #47; debug `farmscan` retained for #47.
