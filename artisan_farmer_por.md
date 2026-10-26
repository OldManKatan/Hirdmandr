# Farmer Artisan Job — Plan of Record

## 1. Purpose
An artisan job that maintains a **player-created** crop field around the NPC's work chest: it **harvests mature crops** and **replants the same crop in the same spot**, keeping the field self-perpetuating. It does **not** plant empty fields and does **not** lay out or space fields — the player creates the field (cultivated soil + initial plants); the NPC only maintains it. It handles both tiers of the crop loop (food plants and seed plants).

## 2. The crop model (two-tier self-propagation)
Per crop family (carrot / turnip / onion are two-tier; barley / flax single-tier):

| Plant | Sapling prefab | Planted with (consumed) | Mature form | Yield |
|---|---|---|---|---|
| Carrot (food) | `sapling_carrot` | carrot **seed** | `Pickable` | ≥1 **carrot** |
| Seed-carrot | `sapling_seedcarrot` | **carrot** | `Pickable` | ≥3 carrot **seeds** |

Loop: `seed →(grow)→ carrot →(grow as seed-plant)→ seeds`. ~⅓ of the crop is reinvested into seeds, ~⅔ is yield. Reference: 10 seed-carrots + 30 carrot-plants → harvest ≥30 carrots + ≥30 seeds; replant consumes 30 seeds + 10 carrots; net +20 carrots.

**Growth mechanic (from IL):** a sapling is a `Plant` (`SlowUpdate`). It grows on a timer (`SUpdate` → when `TimeSincePlanted() > GetGrowTime()` and `m_status == Healthy`), and `Grow()` **destroys the sapling** and spawns `m_grownPrefabs[0]` = the **mature crop** (a `Pickable`). So "mature/ready" = a crop `Pickable` is present (the sapling is gone). `Plant.m_status` (Healthy / NoSun / NoSpace / WrongBiome / NotCultivated / TooHot / TooCold) is a *health* flag, **not** a ready flag — unhealthy plants never mature and are ignored (they despawn on their own).

## 3. Worksite validation (order matters)
For an NPC with the farmer job, a work chest is a valid worksite **iff**, in this order:
1. An NPC chest exists (the work site).
2. The chest contains a **cultivator** (the farmer's work tool; not spawnable — requires bronze — so it must be player-provided in the chest).
3. There is a **mature crop** (`Pickable`) within `FarmerRange` of the chest.
4. The chest contains the **replant item** for that mature crop (the plantable that grows it back).

Ordering rationale: you must know *which* crop is ready (3) before checking the chest has *that* crop's replant item (4); and no cultivator ⇒ no work at all. In practice setup = *find a mature crop in range whose replant item is in the chest*; that crop becomes the target.

**Core invariant — "cannot harvest without being able to replace":** a mature crop is only ever harvested if the chest **currently** holds the item needed to replant it; otherwise the crop is **left in place** (mature, unharvested) until the player restocks. This is checked **both at setup (3+4) and at the moment of each harvest**, because chest contents and the target crop change between runs.

## 4. Job flow (maps onto the WorkDay SM: setup → go → do)
**One `doJob` run = one plant:** harvest a mature crop, then replant the same crop at the exact same X,Y,Z. The SM then loops to setup for the next mature crop.

- **Setup (`setupArtJob`):** run the 4-condition validation; pick the target crop. If none → job not available (idle until next job time).
- **Go (`goArtJob`):** walk the target crop into interact range.
- **Do (`doJob`):**
  1. **Re-validate:** target still present & mature; chest still holds its replant item; cultivator still equipped. If any fails → abort this run, back to setup.
  2. Record the crop's world position.
  3. **Harvest:** `crop.pickable.Interact(npc, false, false)` → game fires `RPC_Pick` (owner-side) → drops `m_itemPrefab` (the yield) on the ground + `SetPicked(true)` → crop destroyed. The game's `m_pickEffector` (`EffectList.Create`, owner-spawned) plays the harvest VFX/SFX — MP-visible per the Mender MP finding, so onlooking players see it.
  4. **Consume** 1 replant item from the chest (the plantable).
  5. **Replant:** instantiate the matching `sapling_*` `Plant` prefab at the recorded position (its `Awake`/`Start` set it up on the already-cultivated ground).
  6. **Collect:** pick up the dropped yield `ItemDrop` from the ground (reuse the furnace job's ground-collect) → `LinkedPutItem` into the chest.
  7. **Skill:** raise farming skill (consistent with the other artisan jobs).
  8. Set `nextJobTime` throttle → back to setup.

## 5. Cultivator tool lifecycle
The cultivator is a required, non-spawnable work tool (needs bronze):
- **On job start:** take the cultivator from the work chest → into the NPC's inventory → **equip** it.
- **While working:** keep it equipped.
- **On transition out of the farmer job (any reason):** **Best-Equip** (unequip to best available, per the tool-boundary pattern) and **return the cultivator to the specific chest it was taken from** (track the source chest).

## 6. Item accounting (per harvest-replant)
Net chest delta per crop type:
- Food plant (e.g. carrot): **−1 seed, +≥1 carrot.**
- Seed plant (e.g. seed-carrot): **−1 carrot, +≥3 seeds.**

Yield amounts are whatever the `Pickable` drops (`m_amount` + scaling + bonus); the NPC collects all ground drops, so it need not hardcode counts. Everything keys off the **mature crop's yield item** (`Pickable.m_itemPrefab`):

```
cropTable (keyed by yield item):
  <carrot>        -> replant sapling_carrot,     consume <carrot seed>
  <carrot seed>   -> replant sapling_seedcarrot, consume <carrot>
  ...             (per family; barley/flax single-tier)
```

## 7. Config / constants (new)
- `FarmerRange` — search radius around the work chest (simple radius).
- Crop table: per family — food/seed sapling prefab, plantable item, yield item.
- Known sapling prefabs: `sapling_carrot`, `sapling_seedcarrot`, `sapling_turnip`, `sapling_seedturnip`, `sapling_onion`, `sapling_seedonion`, `sapling_barley`, `sapling_flax`.
- Exact item strings (each food, each seed, the cultivator) **TBD** — confirm at runtime by logging `Pickable.m_itemPrefab.name` and the seed/food item names.

## 8. Targeting / scope edges
- **Radius only:** a single `FarmerRange` around the chest; no field-boundary or layout logic.
- **Only mature crops:** the NPC interacts only with crop `Pickable`s; growing saplings and unhealthy plants are ignored (unhealthy ones despawn).
- **Crops only, not wild pickables:** target `Pickable`s whose `m_itemPrefab` is a known crop/seed item (the crop table), so wild berries/mushrooms in range are left alone.
- **No planting empty fields** — the field is pre-created by the player.

## 9. Implementation confirmations (deferred — low research, done in code)
1. **Mature-crop class:** confirm `Plant.m_grownPrefabs[0]` for crops is a `Pickable` (harvest via `Interact`/`RPC_Pick`); adjust the harvest call if not.
2. **Two-tier relationship:** confirm `sapling_X` (seed→food) vs `sapling_seedX` (food→seeds) per family, and that barley/flax are single-tier.
3. **Item strings:** exact names for each food, each seed, and the cultivator.
4. **`Pickable.Interact(npc, …)` for a non-Player Humanoid:** confirm it triggers the pick (owner = local player ⇒ drop+destroy proceed); fallback = drive the owner-side pick directly.
5. **Replant instantiate:** confirm instantiating the `sapling` `Plant` at the recorded position initializes correctly on cultivated ground (may need `PlaceAgainst`/attach).
6. **Harvest VFX in MP:** confirm the owner-spawned `m_pickEffector` shows for onlooking players; spawn explicitly if not.
7. **Skill:** confirm the farming skill type and that no skill gate applies (design specifies none).

## 10. Where it plugs in
- `HirdmandrChest.cs` — farmer worksite validation + cultivator check (extends `artisanJobPrefabs` / `IsValidWorksite`).
- `sub1SM-WorkDay.cs` — `setupArtJob` / `goArtJob` / `doJob` farmer branches (field scan for mature `Pickable`s; harvest+replant in `doJob`).
- `HirdmandrAI.cs` / `HirdmandrNPC.cs` — cultivator take/equip/return + source-chest tracking; farming skill.
- `Utils.cs` — `FarmerRange` + crop-table constants.

## 11. Out of scope (this job)
Planting empty fields, field layout/spacing, tilling/cultivating soil (player does these). Cook/baker (separate decision, deferred). Multi-field management beyond one chest's radius.
