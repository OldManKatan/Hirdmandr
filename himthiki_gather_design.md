# Himthiki "Gather" Job — Design (round 2)

Status: **S1 passed (hole 1 closed); S2 done; S3 done; S4 done (awaiting in-game
`/hirdmandr gather` test); S5 next.**
The `m_jobGatherer` warrior-job setting + "Enable Gatherer Labor" GUI toggle
already existed in the original framework (pre-plumbed, no behavior) — reused,
not created. The design doc's `m_jobGather` name is stale; `m_jobGatherer` is
canonical.
Vision (already shipped as tutorial text, HMTutorial.cs:110):
"A Himthiki with the Gatherer job enabled will pick up useful things on your
journeys, everything from stone to cloudberries."

## Scope (locked)
- A follower Himthiki (warrior job) auto-gathers while following: it picks
  pickables and, ~1s after each pick, sweeps the ground (~10m around the pick
  point) into its own inventory.
- **Safe zone (the only exclusion):** nothing is *picked* within 40m of a
  player-placed chest (or a Hirdmandr chest). The ground sweep is pick-triggered,
  so at home there are no picks and therefore no sweeps.
- **Wild versions of farmables ARE picked** — the only original source of any
  farmable is the wild pick, and wild mushrooms are among the most valuable
  picks. No yield/identity filtering of any kind.
- **Weight cap 300** = the NPC's own vanilla carry weight
  (`GetMaxCarryWeight()`, `m_maxCarryWeight = 300f`, NPCPlayerClone.cs:2243).
  At the cap: no picking, no sweeping — pure follow.
- **Out of scope, now and future:** delivery / deposit / hand-off. The player
  takes items from the NPC's inventory via the inventory GUI. Full
  item-handling scope = ground → NPC inventory + `m_container.Save()`.
- **MP rule:** picking and pickup go through the game's own mechanisms (the
  same ones a player triggers), so regrowth, drop despawn, and inventory
  changes sync for all connected clients. No custom drop/regrowth code.

## Decision record
1. Gather is disabled whenever the Himthiki job is disabled (req-1 typo confirmed).
2. Auto-disable = **setting-cleared** (persisted off; re-toggle to re-enable).
3. Wild pickables only "where possible" → resolved by the safe zone (below).
4. Hirdmandr field crops: never picked (100%).
5. Loop: chain picks (closest eligible) while no danger and the NPC is within
   20m of the player; back to follow when the player moves >20m or nothing's
   left; danger → existing threatened routing.
6. At 300 weight: stop ALL gather work. No delivery mechanic — player takes
   items from the NPC's inventory via the GUI ("part of the fun").
7. Picking MUST use the same mechanism as the player (proper regrowth + RPCs
   calling the same event/state of the world for all connected clients).
8. Gameplay incentive: the ONLY original source of any farmable is picking its
   wild version → the gatherer SHOULD pick wild versions of farmables
   (especially mushrooms). Yield/identity filtering rejected.
9. Safety zone: nothing is picked within 40m of player-constructed areas —
   "gathering away from home".
10. **Anchor set = player-placed CHESTS only**: `Piece` with
    `IsPlacedByPlayer == true` that has a `Container` (barrels count — storage
    too) ∪ explicit `HirdmandrChest` objects. Workstations rejected (no type
    list to maintain). 40m radius.
11. The chest is the chosen single "player infrastructure" proxy — inventory
    management is core Valheim pressure; player behavior revolves around
    chests. Accepted trade: a farm >40m from the nearest player-placed chest is
    exposed (documented known limitation, observable in playtest).
12. Flag persistence (hole 1) is the **immediate S1 test**: Thomas's existing
    80+ day world has both player-placed and non-player-placed chests; the
    chestscan logs the flags, world is reloaded, logged again, compared.
13. Cost policy: pay-per-scan + cache (the 5s gate IS the cache); don't
    overcomplicate enumeration. Both `FindObjectsOfType<Piece>` and
    `FindObjectsOfType<Container>` walk the whole scene (equal cost); the
    Piece route is used because the rule needs the Piece flag anyway.
14. **Sweep = unconditional ~1s after each pick** (Invoke-delayed) — margin for
    the RPC/server to update the pickable and spawn the yield drops. ALL zone/
    gate logic applies to WHAT gets picked; the sweep "just happens".
    Sweep is centered on the PICK POINT (where the yield lands), not the NPC's
    current position.

## How it works

### Setting plumbing (DONE — reused the pre-existing framework)
- **Already in the original framework (reused, not created):**
  `HirdmandrNPC.m_jobGatherer` (bool, :110), ZDO key `hmnpc_jobGatherer`
  (HirdmandrAltarHella.cs:556), load (:1689), save (`ZDOSaveGeneral` :1720),
  `ApplySettingLocal` case `"jobGatherer"` (:2101), SoulItem payload
  (SoulItem.cs:223), and the warrior-page GUI toggle "Enable Gatherer Labor"
  (`g_war_ch_gatherer`, `ToggleGatherer` → `InvokeSetting("jobGatherer", v)`).
- **Clear hooks (setting-cleared) — ADDED in S2:** every place `m_jobHimthiki`
  turns off also clears + persists `m_jobGatherer`:
  - `case "jobHimthiki"` value=false → `m_jobGatherer = false`.
  - `case "jobThegn"` value=true forcing `m_jobHimthiki=false` → also clears it.
  - Load-time repair: `!m_jobHimthiki && m_jobGatherer` → clear + `Set(false)`
    (mirrors the Thegn/Himthiki repair at :1678-1685).
  Note: role→artisan does NOT clear warrior jobs in the current framework (jobs
  persist; only the page hides), so Gatherer persists across a role switch and
  is re-gated by `m_jobHimthiki` — consistent with Thegn/Himthiki.
- **GUI (S3, DONE):** gate the existing "Enable Gatherer Labor" toggle's
  `interactable` on `m_jobHimthiki` (Thegn shift-toggle convention,
  `RefreshGuardControls`, HirdmandrGUI.cs). It's already in the warrior
  page (`all_wars`), so it's role-hidden for free. Implemented as one line in
  `RefreshGuardControls` (the method that already refreshes the Thegn shift
  toggles' `interactable`), so it refreshes on the same triggers (panel open,
  role change, combat-job change).

### Scan (time-gated, 5s, cached)
- `lastGatherScan` float on HirdmandrAI (`lastZDOCheck` pattern,
  HirdmandrChest.cs:23). Every 5s:
  1. **Pickables:** `FindObjectsOfType<Pickable>()` — the loaded set IS
     "player's simulated range" (unstreamed objects don't exist to Unity).
  2. **Anchors:** `FindObjectsOfType<Piece>()` filtered to
     `IsPlacedByPlayer && GetComponent<Container>() != null` ∪
     `FindObjectsOfType<HirdmandrChest>()`. Scale: ~0 a short walk from bases;
     tops out ~100–200 inside large bases.
  3. **Exclusion flags:** a pickable within 40m (`GatherSafeRadius`, Utils,
     like FarmerRange) of any anchor is marked excluded. Cost: N×anchors
     (500 × 200 = 100k distance checks ≈ <1ms) — no spatial index needed.
- The cache is a starting point: eligibility is re-checked at pick time (bush
  may be picked/destroyed, player moved, weight, danger).
- Total scan cost: a few ms every 5s (three finds + flag pass). Per frame:
  nothing (flags are read, never recomputed).

### Pick session (sub-behavior of the existing "follow" top state)
Lives inside the "follow" state (HirdmandrAI.cs:267) — that state already holds
the schedule paused and routes back from `NodeThreatened` on de-alert, so the
danger requirement is satisfied by construction; the sub-behavior just has to
be interruptible.
- **Assumption (flag for review):** the session runs only while following
  ("on your journeys"). Req 4(2) ("if not following, center = the NPC itself")
  is kept as the evaluation-radius definition — if the follow target goes null
  mid-session the session continues centered on the NPC; it does NOT mean
  "gather while socializing at the homestead".
- **Session gates (all true, checked every frame):**
   `m_jobGatherer && m_jobHimthiki && m_following && !isAlerted &&
   dist(NPC→player) ≤ 20m && weight < 300`.
  (The ≤20m gate also means "never chase a running player": far behind = no
  gather.)
- **Loop (while gates hold):**
  1. Target = closest cached pickable, not excluded (safe zone), within 20m of
     the center (center = follow target while following, else the NPC).
     **Re-validate immediately before Interact** (5s-stale cache): object still
     exists, still eligible. On failure → skip to next closest (with a short
     per-target cooldown to prevent a tug loop on obstructed targets).
  2. Move there (TryToMove, the farmer's 15s timeout + 4m interact reach).
     Straight-line walk to a ≤20m pickable provably stays within the 20m leash
     (distance-to-a-point along a segment is convex; max is at an endpoint) —
     no mid-walk bail in the clear case. Obstructed routes that breach the
     leash bail to follow + cooldown (no tug loop).
  3. **Pick: `crop.Interact(npc, false, false)`** — the exact call the live
     farmer uses (FarmerHarvest, sub1SM-WorkDay.cs:1207). The game's own
     mechanism: spawns the yield drop (a real networked ItemDrop), drives the
     pickable's own pick-count/regrowth state, plays the pick VFX.
  4. **Sweep: unconditional, ~1s later** (Invoke), centered on the pick point
     (the pre-Interact position — FarmerHarvest already captures it), radius
     ~10m: the vanilla Player auto-pickup algorithm
     (NPCPlayerClone.cs:220-252, the commented `AutoPickup` block):
     `ItemDrop` → `m_autoPickup`, `!HaveUniqueKey`, `ZNetView.IsValid()`,
     `CanPickup(true)`,
     `itemData.GetWeight() + inv.GetTotalWeight() <= GetMaxCarryWeight()`
     (300) → `base.Pickup(go, true, true)` (the game's own pickup: adds to
     inventory, despawns the drop, syncs).
  5. `m_container.Save()` (the NPC's container, HirdmandrNPC.cs:46).
  6. **Chain to the next closest** (Q5) until: nothing left in range, player
     >20m away, weight at cap, or danger → existing threatened transition.
     Recommended deterministic order: pick → 1s → sweep → next target (the
     1s/pick cadence is negligible vs the 2–4s walks between bushes).
- **Exit:** back to normal follow behavior. A later scan (≤5s) can start a new
  session — gathering is opportunistic and continuous.

## MP correctness (the new requirement)
- Pick = the game's Interact (the farmer's path — PoR step 11 confirmed
  Interact fires for NPC humanoids; proven on crops, wild bushes verified on
  first in-game pick in S5).
- Pickup = the game's Pickup (vanilla-synced inventory add + drop despawn).
- Persistence = `m_container.Save()`.
- MP AI ownership = the existing architecture (same as the live farmer — no
  new risk class).

## Implementation steps (one per deploy, in order)
- **S1 — chestscan probe (deploy FIRST).** `/hirdmandr chestscan` debug
  command (pattern: existing `/hirdmandr farmscan`), logs: every `Piece` with
  a `Container` — name, `IsPlacedByPlayer`, Hirdmandr?, position + counts
  (placed vs world); every loaded `Pickable` — prefab name, yield
  (`m_itemPrefab` → item name), position, 40m-of-placed-chest flag (the
  future exclusion preview). Thomas runs it in the 80-day world (has
  player-placed AND world-spawned chests), reloads, runs again → the log
  answers hole 1 (flag exists + survives reload + false on world chests).
- **S2 — Setting plumbing (DONE).** Reused the pre-existing `m_jobGatherer`
  (field/ZDO key/load/save/SetSetting/SoulItem/GUI all already in the original
  framework). Added the clear hooks: turning off the Himthiki job (Himthiki
  off, or Thegn displacing it) also clears + persists `m_jobGatherer`, plus a
  load-time repair. No behavior yet.
- **S3 — GUI (DONE).** Reused the pre-existing "Enable Gatherer Labor"
  warrior-page toggle; gated its `interactable` on `m_jobHimthiki` (one line
  in `RefreshGuardControls`). No new toggle.
- **S4 — Scan cache (DONE — pending in-game test).** On `HirdmandrAI`:
  `m_lastGatherScan` (5s gate, `lastZDOCheck` pattern), `m_gatherPickables`
  (loaded `Pickable`s), `m_gatherExcluded` (pickables within
  `Utils.GatherSafeRadius`=40m of a placed-chest anchor), `m_gatherAnchorCount`.
  `GatherScan()` runs only while
  `m_roleWarrior && m_jobHimthiki && m_jobGatherer && m_following`; the cache
  is dropped otherwise (S5 re-gates every frame, so a stale cache is harmless).
  Test harness: `/hirdmandr gather` dumps per-NPC flags + cached
  pickable/anchor/excluded counts + nearest eligible target under S5's rule
  (not excluded, ≤20m of the follow center).
- **S5 — Pick sub-state + early follow-detour test.** In the follow state:
  target selection, move, Interact. FIRST MINI-TEST before building the full
  loop: does a TryToMove detour coexist with / override the native MonsterAI
  follow-walk? (First job that detours mid-follow — no live precedent.) Also
  verify the first wild-bush pick (Interact proven on crops, bushes are the
  same API, different prefabs).
- **S6 — Sweep + chain.** 1s-delayed sweep at the pick point, weight gate,
  Save, session chaining + exit conditions.
- **S7 — Playtest.** Single + MP, Thomas's scenario. Watch specifically:
  depleting fields (the hole-4 exposure), tug loops, home-zone behavior.

## Remaining risks (challenge pass, 2026-10-29) — all have resolutions
1. **Follow-detour interaction (biggest unknown):** the gather is the first
   job that detours while the native follow AI owns movement. S5 mini-tests
   this before the full loop; worst case is a non-picking Himthiki that
   follows normally (no crash/stuck paths).
2. **Sweep centering:** must be the pick point, not the NPC's current
   position (the yield lands at the bush; the NPC may be walking to the next
   target by sweep time). The deterministic order (pick → 1s → sweep → next)
   keeps the NPC at the bush during the sweep anyway.
3. **Stale-target tug loop:** a target destroyed/obstructed between scan and
   arrival → re-validate pre-Interact + short per-target cooldown.
4. **Interact on wild bushes:** proven on crops (farmer), same API on bushes
   — verified on first in-game pick (S5).
5. **Leash convexity (good news):** straight walks to ≤20m pickables stay
   within the 20m leash — no mid-walk bail in the clear case; only
   obstacle-routing bails, handled by 3.
6. **Weight leftovers at the cap:** items that don't fit stay on the ground
   at the bush (player's to collect); session ends, Himthiki follows.
   By design.

## Open
- (none — S1 passed; S2/S3/S4 deployed; S4 awaits its in-game `/hirdmandr gather`
  test; then S5.)

## Appendix — S1 facts (chestscan, 2026-10-29)
- Run 1 (at camp): 10 chests — 4 world treasure `placed=False`, 6 player-placed
  `placed=True` (5 Hm `piece_npc_chest*` + 1 player `piece_chest_blackmetal`).
- Run 2 (~80m out): same 6 player-placed chests (identical positions/flags) +
  2 extra world chests (new area loaded, `False`). → flag **persists** +
  **discriminates**. Hole 1 closed.
- Run 3 (camp out of render range): 3 world chests, 0 player-placed — camp
  chests **unload** outside render range. Feature: safe zone (= 40m of a loaded
  player-placed chest) ≈ "home"; gathering away from home = zone off.
- Cost: up to 746 pickables × ~6 placed anchors ≈ 4.5k distance checks (vs the
  500×200=100k design assumption). Trivial.
- Hm chests report `placed=True` → the "∪ HirdmandrChest" union is redundant
  (kept as defensive insurance).

## Appendix — S4 test plan (`/hirdmandr gather`, 2026-10-29)
Preconditions: a warrior-role NPC with the **Himthiki** job + **Enable
Gatherer Labor** on, actively **following** the player (the scan only runs
while all four hold). Expected log lines:
- **Away from home** (80m+ out, camp unloaded): `anchors=0`, `excluded=0`,
  `pickables=<loaded count>`, `lastScan` ≤5s, `NEAREST-ELIGIBLE` ≤20m while
  following (center = the player).
- **At the camp** (6 player-placed chests loaded): `anchors≈6`,
  `excluded>0` (camp pickables zoned out), `NEAREST-ELIGIBLE` avoids the camp
  core.
- **Gatherer off / Thegn job / not following:** `pickables=0` (cache dropped),
  `lastScan` frozen (no refresh).
