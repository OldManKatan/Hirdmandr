# Common Warrior — Implementation Plan

> **Companion to** `common_warrior_design.md` (design & decisions — read its sections as each step lists).
> **This file is the single source of truth for progress.** After any `/compact`, re-orient using the protocol below — do not rely on conversation memory.
> **State/progress of record** also lives in **PMW** (project `hirdmandr`): umbrella **`#49`** (id 103) + sub-tasks **`#49a`** (Step 1) … **`#49p`** (Step 16), all in POOL. The checkboxes here mirror the PMW items — keep them in sync.
>
> **Status (2026-10-17):** Plan **LOCKED**. **Steps 1–7 DONE** — verified by Thomas; the two-client MP check remains **deferred** (no MP access yet) to Step 13 / Step 16. Latest build clean: `0 errors / 22 warnings`. Latest deploy: `Hirdmandr.dll` `226816` bytes at `10/17/2026 10:47:10 AM`. **Round 19 test feature deployed (v2, freeze fix):** Hirdmandr NPC nameplates/health bars remain visible within vanilla's own show range (`EnemyHud.m_maxShowDistance`, ~30m) without hover, and the name color is white when the NPC's `m_jarlZOID` matches the local player's `GetPlayerID()`, otherwise grey. Fix: the plate no longer freezes on screen when leaving range (force-inactive on exit) and snaps back onto the NPC on re-entry (`m_hoverTimer` pinned to 0). Awaiting Thomas verification.

## How to use this plan (the loop)
Every step is a **self-contained, compiling increment**. Work **strictly sequentially** (no parallel work). One thing at a time, to the highest individual quality.

**Re-orientation protocol (run after EVERY `/compact`):**
1. Open this file; find the first step whose checkbox is **unchecked** → that is **the** step.
2. Read the design doc (`common_warrior_design.md`) sections that step lists.
3. Open the step's **Files**. The files are the source of truth for exact APIs — **mimic the named existing pattern**.
4. Perform the **Actions**.
5. **Build** (command below) — it **must** succeed before the step is done. (Compile errors surface in waves — fix, rebuild, repeat.)
6. If the step has an in-game **Verify**, Thomas runs it; record the result where noted.
7. Tick the step's checkbox + update the matching PMW item. Then `/compact` and start the next step.

**Do not begin implementation until Thomas gives the go-ahead for Step 1.**

**Build:**
```
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\OpenCode\hirdmandr_project\hirmandr_repo\Hirdmandr.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU" /p:ExecutePrebuild=false /nologo /m
```
**Deploy** (only when play-testing): run `publish.ps1` → `D:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\Hirdmandr\`.
**New `.cs` files MUST be added to `Hirdmandr.csproj`** as `<Compile Include="Foo.cs" />` (old-style project — a file not listed is silently omitted from the assembly → `CS0103`).

---

## Progress
- [x] **Step 1** — `ApplyCombatConfig` (single source of truth for MAI fields) — **DONE** (single-player verified; MP 2-client check deferred)
- [x] **Step 2** — Persona sets defaults — **DONE**
- [x] **Step 3** — Jobs → radio (one persona); Gatherer = separate toggle — **DONE**
- [x] **Step 4** — Proficiency / level scaling (NO capability gate) — **DONE**
- [x] **Step 5** — `EquipBest` best-of-each-type
- [x] **Step 6** — Dynamic weapon selection (the runtime swap)
- [x] **Step 7** — Far + no-bow clamp
- [ ] **Step 8** — Distress signal (ZDO vs RPC — decide here)
- [ ] **Step 9** — "In danger" detection + `panic`
- [ ] **Step 10** — Call response (move to + defend)
- [ ] **Step 11** — Defense-anchor generalization (L2)
- [ ] **Step 12** — Escape (`escapeHelp` / `escapeAny`)
- [ ] **Step 13** — MP + raid regression
- [ ] **Step 14** — Tune numbers
- [ ] **Step 15** — Edge-case QA
- [ ] **Step 16** — Final MP regression + rebuild + deploy

**Round 19 test feature (local):** `EnemyHudPatch.cs` forces Hirdmandr NPC nameplates/health bars to remain visible within vanilla's own show range (`EnemyHud.m_maxShowDistance`, ~30m) without hover, and colors the name locally per client: white when `HirdmandrNPC.m_jarlZOID == Player.m_localPlayer.GetPlayerID()`, otherwise grey. It patches `EnemyHud.TestShow` (force-true within range, overriding the player-crouch rule), `EnemyHud.LateUpdate` (ensure `ShowHud`), and `EnemyHud.UpdateHuds` (pin `m_hoverTimer=0` so vanilla's hover-expiry never fires while in range; force `m_gui` inactive on exit so the plate never freezes on screen; vanilla still does all screen projection, so it snaps back onto the NPC on re-entry). Uses vanilla's distance value read at runtime (fallback 30f) so our force-range exactly matches vanilla's. Harmony gotcha: this BepInEx Harmony version uses `ref bool __result` for postfix return access, not `__return`.

**Phase boundaries:** P1 = Steps 1–7 (combat customization, MP-safe, no stubs). P2 = Steps 8–13 (the protection states + MP). P3 = Steps 14–16 (tune / QA / ship).

---

## Phase 1 — Combat customization (MP-safe, no stubs)

### Step 1 — `ApplyCombatConfig` (single source of truth for MAI fields)
**Goal:** one method owns every `MonsterAI` field, computed from `(role, persona, style, range, skills, available gear)`, applied on **all clients** on load + on change.
**Design:** §5.
**Depends-on:** nothing.
**Files:** `Hirdmandr\HirdmandrNPC.cs` (`SetupNPC` MAI init :799-810; the **(dead) apply** :1607-1611; `RPC_HMSetting`/`ApplySettingLocal`).
**Actions:**
1. Add `void ApplyCombatConfig()` on `HirdmandrNPC`: compute every `MonsterAI` field from `(role, persona, style, range, skills, available gear)` and write to the fresh `MonsterAI`. Centralize the style/range→MAI mapping (§5 table) here.
2. Call it in `SetupNPC` after load (replacing/augmenting the one-shot :799-810), **on all clients**.
3. Call it on `ApplySettingLocal` (style/range/persona change) and on skill change, so all clients re-derive.
4. Remove the dead apply at :1607-1611 (or make it delegate to `ApplyCombatConfig`).
**Build:** must succeed.
**Verify (Thomas, in-game, 2 clients):** a Thegn (defensive/mid) + a Himthiki (offensive/far) load on 2 clients → the MAI fields are **identical + correct** on both (temp debug log).
**Exit state:** one method owns all MAI fields; it runs on load (all clients) + on change; the dead apply is gone.
**Status:** ✅ **DONE** — Thomas verified the desired single-player behavior in logs and observed the `m_fleeIfLowHealth` flee trigger in combat. The 2-client MP verification is **deferred** to Step 13 / Step 16 because multiplayer testing access is not available yet. `ApplyCombatConfig()` is the single MAI combat-config owner; it is called from owner load, non-owner `LoadZDO`, `SetupNPC`, and `ApplySettingLocal`. Dead `SetupNPC` one-shot MAI block replaced; bow-path MAI writes removed from `EquipBest`. Build clean: `0 errors / 21 warnings`. Temp verification logging removed. Compiler correction preserved: `m_fleeIfLowHealth` is a **float** health-fraction threshold, not a bool; current values are defensive `0.3f`, offensive `0.0f` (final tuning remains Step 14).

### Step 2 — Persona sets defaults
**Goal:** selecting a persona job sets default style/range/skills, **only if the axes are still at their defaults** (a customized warrior is not reset).
**Design:** §2, §3.
**Depends-on:** Step 1.
**Files:** `Hirdmandr\HirdmandrNPC.cs` (the job setting handler / `InvokeSetting`), `HMSkills.cs`.
**Actions:**
1. Define the persona-default table: **Thegn** = defensive / mid + melee; **Himthiki** = offensive / far + range.
2. On persona-job selection: if style/range/skills are still at their defaults, set them to the persona defaults.
**Build:** must succeed.
**Verify:** selecting Thegn → style/range become defensive/mid + melee skill; change them; deselect + reselect → **not** reset.
**Exit state:** persona selection seeds defaults; customization survives re-selection.
**Status:** ✅ **DONE** — Thomas verified in-game (10/16). Persona-job selection seeds defaults only while the combat axes are still at defaults: Thegn = defensive / mid + melee, Himthiki = offensive / far + range. Customization survives re-selection. GUI job toggles refresh the style/range toggle states after applying the persona. Build clean `0 errors / 21 warnings`; deployed DLL `215552` bytes at `10/16/2026 3:08:08 PM`.

### Step 3 — Jobs → radio (one persona); Gatherer = separate toggle
**Goal:** the GUI enforces **one** combat persona at a time; Gatherer stays its own checkbox.
**Design:** §2.
**Depends-on:** Step 2.
**Files:** `Hirdmandr\HirdmandrGUI.cs` (the job checkboxes → a persona radio `ToggleGroup`), + the job setting handlers.
**Actions:**
1. Make Thegn/Himthiki a **mutually exclusive radio group** (one persona).
2. Keep **Gatherer** as its own checkbox (labor, not a persona).
3. Wire selection to Step 2's "set defaults."
**Build:** must succeed.
**Verify:** selecting Thegn deselects Himthiki (and vice versa); Gatherer toggles independently; exactly one persona at a time.
**Exit state:** GUI enforces one persona; Gatherer independent.
**Status:** ✅ **DONE — Thomas verified 10/16/2026.** Thegn/Himthiki are a `ToggleGroup` radio with `allowSwitchOff = false`; Gatherer remains an independent checkbox. `HirdmandrNPC.ApplySettingLocal` enforces persona mutual exclusion for MP convergence, and `ZDOLoadGeneral` sanitizes legacy both-true saves (prefers Thegn). RoleChanged selects Thegn when the Warrior role is active and no valid persona is present. Clean build `0 errors / 21 warnings`; deployed DLL `216576` bytes at `10/16/2026 3:23:56 PM`.

### Step 4 — Proficiency / level scaling (NO capability gate)
**Goal:** skills scale *how well* a warrior uses something — never *whether* it can. Revive the commented-out melee damage line; add range arrow power; keep fighter toughness.
**Design:** §6 (REVISED — capability gates **DROPPED**).
**Depends-on:** Step 1.
**Files:** `Hirdmandr\HirdmandrNPC.cs` (`PopulateCombatProps` :1656-1661), `HMSkills.cs`.
**Actions:**
1. `melee` → melee damage `1 + level/50` (revive the commented-out line in `PopulateCombatProps`).
2. `range` → arrow power (damage/accuracy).
3. `fighter` → keep maxHP = 60 + level + heal tick = 2 + level/3 (already wired).
4. **No capability gate** — a weapon is usable whenever **owned**, regardless of skill level.
**Build:** must succeed.
**Verify:** level a skill → the stat changes (log maxHP / damage); a **0-skill** weapon still works (just weak).
**Exit state:** all three skills scale a stat; no hard gate.
**Status:** ✅ **DONE — Thomas verified 2026-10-16.** `fighter` scales `maxHP = 60 + 2 * fighter` and `m_healTick = 2 + fighter / 4`. `melee` sets the equipped melee weapon's `Attack.m_damageMultiplier = 1 + melee / 50`. `range` scales an equipped bow's `Attack.m_damageMultiplier = 1 + range / 50` and lerps `m_projectileAccuracy` / `m_projectileAccuracyMin` toward the old fixed 2/10 spread. `PopulateCombatProps` re-applies scaling on load/interact, `EquipBest` re-applies after equipment setup, and `HMSkills.ModifySkill` re-applies combat-skill changes live. No weapon is gated by skill level. `/hirdmandr combatstats <value>` was added for coarse testing. Build clean `0 errors / 22 warnings`; deployed DLL `219136` bytes at `10/16/2026 4:53:56 PM`.
**Note:** replaces the old "capability gate" step (dropped per Thomas 2026-10-16).

### Step 5 — `EquipBest` best-of-each-type
**Goal:** `EquipBest` equips the **best of each weapon type** (best melee + best bow + arrows if a bow is owned) so both types are always available; the persona-default-type weapon is equipped per the Range setting.
**Design:** §4.
**Depends-on:** Steps 1, 4.
**Files:** `Hirdmandr\HirdmandrNPC.cs` (`EquipBest` :1887-2085).
**Actions:**
1. `EquipBest` determines **best-of-each-type**: best melee by damage; best bow + best arrows (if a bow is owned).
2. Equip the **persona-default-type** weapon (per the Range setting), leaving both types available.
3. Remove the bow-special-case `m_maxChaseDistance = 0` (MAI is now owned by `ApplyCombatConfig`, Step 1).
4. No skill gate on equipping — a bow is equipped **if owned**.
**Build:** must succeed.
**Verify:** a Himthiki with a bow + a sword equips the bow (far default) + arrows; a Thegn equips the sword + shield.
**Exit state:** best-of-each known; default-type equipped; no MAI writes here.
**Status:** ✅ **DONE** — Thomas verified the Round 18 retest PASS (far/mid/close bow damage + draw, 14m/8m non-far hysteresis, player-bow engagement, far with/without arrows, fists/unarmed fallback). `EquipBest` now separates best melee (`OneHandedWeapon` / `TwoHandedWeapon`) from best `Bow`, caches the best-of-each results on `m_bestMeleeWeapon` / `m_bestBow` / `m_bestArrows` for Step 6, and equips the best bow when Range = far + a bow is owned. Otherwise it equips the best melee; if no melee is owned, it falls back to the best bow. A bow is only equipped/usable when arrows are owned; arrows are equipped only when a bow is owned. No `m_monsterai` writes remain in `EquipBest`; bow arrow power is still re-applied by `ApplyProficiencyScaling()` after `SetupEquipment()`. Build clean `0 errors / 22 warnings`; deployed DLL `226304` bytes at `10/17/2026 9:31:20 AM`. Round 17 fixes: `GetCurrentWeapon()` drives current-weapon detection; left-hand slot normalization handles bow/one-handed/two-handed transitions; `Humanoid.EquipBestWeapon` is patched for Hirdmandr NPCs; bow AI attack range is range-specific via `ApplyBowAiRange`. Round 18 critical bow fix: `MonsterAI.DoAttack` calls `HirdmandrNPC.HM_PrepareBowAttack()` for Hirdmandr NPCs so `Humanoid.m_attackDrawTime` is pre-charged before `Humanoid.StartAttack`.

### Step 6 — Dynamic weapon selection (the runtime swap)
**Goal:** the equipped weapon **type** follows the situation (distance + player engagement) with **hysteresis**; `HM_SelectBestAttack` returns the **equipped** weapon's attack (no longer max `GetTotalDamage()`).
**Design:** §4.
**Depends-on:** Step 5.
**Files:** `Hirdmandr\HirdmandrNPC.cs` (`HM_SelectBestAttack` :2213-2236 + a new per-frame desired-type decision), `MonsterAIPatch.cs`.
**Actions:**
1. Add a runtime **desired-type** decision: bow if (a bow is available AND enemy > bowThreshold OR (player is engaging at range) OR Range = far), else best melee; **hysteresis** (switch out at ~18 m, back in at ~12 m).
2. If desired-type ≠ currently-equipped type → **re-equip** (best of that type + arrows for bow).
3. `HM_SelectBestAttack` returns the **equipped** weapon's attack.
4. No skill gate — a bow is used whenever available; `range` only scales arrow power (Step 4).
**Build:** must succeed.
**Verify (Thomas, in-game):** a Himthiki following the player — a far creature → it draws the bow; move it close (or stop shooting) → it switches to melee; confirm it **doesn't flap** at the threshold.
**Exit state:** the weapon follows the situation (distance + player engagement) with hysteresis.
**Status:** ✅ **DONE** — Thomas verified the Round 18 retest PASS (far/mid/close bow damage + draw, 14m/8m non-far hysteresis, player-bow engagement, far with/without arrows, fists/unarmed fallback). `MonsterAIPatch` now passes the target humanoid into `HM_SelectBestAttack(Humanoid target)`. That method is the runtime desired-type decision: it returns the best available melee or bow, re-equips the right-hand weapon when the desired type changes, keeps shield/ammo slots consistent (shield for one-handed melee, no left item for bow/two-hand, arrows for bow), and applies a bow hysteresis band: draw bow at target distance > 14m, drop bow at < 8m, with bow forced by Range=far or by the player actively engaging with a bow. Thomas confirmed on 2026-10-17 that Range=far remains always-bow; the 14m/8m hysteresis applies to non-far range modes. Player-bow detection now checks `GetCurrentWeapon()` plus both `m_leftItem` / `m_rightItem`, so a player holding a bow in either hand is detected. A bow is never returned unless both bow and arrows are owned. Build clean `0 errors / 22 warnings`; deployed DLL `226304` bytes at `10/17/2026 9:31:20 AM`. Round 17 fixes: `GetCurrentWeapon()` drives current-weapon detection; left-hand slot normalization handles bow/one-handed/two-handed transitions; `Humanoid.EquipBestWeapon` is patched for Hirdmandr NPCs; bow AI attack range is range-specific via `ApplyBowAiRange`. Round 18 critical bow fix: `MonsterAI.DoAttack` calls `HirdmandrNPC.HM_PrepareBowAttack()` for Hirdmandr NPCs so `Humanoid.m_attackDrawTime` is pre-charged before `Humanoid.StartAttack`.
**Note:** biggest step — keep it whole; split (6a decision / 6b re-equip) only if unwieldy.

### Step 7 — Far + no-bow clamp
**Goal:** if the desired type is bow but **no bow is owned**, clamp to best melee at natural reach (never fake ranged).
**Design:** §4 (item 5).
**Depends-on:** Step 6.
**Files:** `Hirdmandr\HirdmandrNPC.cs` (the desired-type decision + `EquipBest`).
**Actions:**
1. In the desired-type decision (Step 6): if bow desired but no bow owned → best melee at natural reach.
2. Confirm the bow-less warrior **closes in** (`m_maxChaseDistance` = melee reach, not 0).
**Build:** must succeed.
**Verify:** a bow-less Himthiki at a far creature → closes in with melee (no fake ranged, no stand-still).
**Exit state:** the no-bow clamp works.
**Status:** ✅ **DONE** — Thomas verified the Round 18 retest PASS (far/mid/close bow damage + draw, 14m/8m non-far hysteresis, player-bow engagement, far with/without arrows, fists/unarmed fallback). "Bow available" now means bow + at least one arrow. If the desired type is bow but no bow/arrows are owned, `HM_SelectBestAttack` falls back to the best melee. `ApplyCombatConfig()` gives a far warrior the 25m ranged chase radius only while a bow is available; a bow-less far warrior clamps to the 10m natural melee chase radius so it closes in instead of standing at a fake ranged distance. Build clean `0 errors / 22 warnings`; deployed DLL `226304` bytes at `10/17/2026 9:31:20 AM`. Round 17 fixes: `GetCurrentWeapon()` drives current-weapon detection; left-hand slot normalization handles bow/one-handed/two-handed transitions; `Humanoid.EquipBestWeapon` is patched for Hirdmandr NPCs; bow AI attack range is range-specific via `ApplyBowAiRange`. Round 18 critical bow fix: `MonsterAI.DoAttack` calls `HirdmandrNPC.HM_PrepareBowAttack()` for Hirdmandr NPCs so `Humanoid.m_attackDrawTime` is pre-charged before `Humanoid.StartAttack`.

---

## Phase 2 — Protection / coordination (the stubs, MP)

### Step 8 — Distress signal (ZDO vs RPC — decide here)
**Goal:** a shared, MP-safe distress marker on a distressed hirdmandr. **REVISIT ZDO vs RPC** (Thomas leans **RPC**; "we lose nothing by deciding later").
**Design:** §9 (OPEN — ZDO vs RPC).
**Depends-on:** Phase 1 (Steps 1–7).
**Files:** `Hirdmandr\HirdmandrNPC.cs` (a distress field/flag), `HMSkills.cs` or a new helper.
**Actions:**
1. Implement the shared distress marker (flag + expiry ~10 s, **re-asserted while in danger**).
2. **Revisit ZDO vs RPC** — pick the MP-safe one the responder can read (a ZDO flag read via `m_ZDOsInRange`, or an RPC broadcast). Note the choice in this item's outcome.
**Build:** must succeed.
**Verify (Thomas, 2 clients):** a distressed hirdmandr's distress is visible to a second client; it expires after ~10 s.
**Exit state:** an MP-safe, deterministic distress signal exists (ZDO or RPC — recorded).
**Status:** ⬜ NOT STARTED (mechanism TBD)

### Step 9 — "In danger" detection + `panic`
**Goal:** define "in danger" + the `panic` state (raise the call + disengage).
**Design:** §8.
**Depends-on:** Step 8.
**Files:** `Hirdmandr\sub1SM-Threatened.cs` (`panic`), `HirdmandrAI.cs` (in-danger detection).
**Actions:**
1. Define **"in danger"**: being attacked OR HP < 30%.
2. On entry, `panic` = raise the distress (Step 8) + **disengage** (a warrior repositions; an **artisan can't fight** → escape, Step 12).
**Build:** must succeed.
**Verify:** an attacked Thegn → enters panic + raises the distress flag.
**Exit state:** in-danger detection + panic work.
**Status:** ⬜ NOT STARTED

### Step 10 — Call response (move to + defend)
**Goal:** nearby warriors (warrior job, **no** `m_followTarget`) converge on a distressed hirdmandr + defend it.
**Design:** §8 (the call-response rule), §7 (L2 anchor).
**Depends-on:** Steps 8, 9.
**Files:** `Hirdmandr\sub1SM-Threatened.cs` (`callForHelp`), `HirdmandrAI.cs` (the responder scan), `topSM.cs` (the transition).
**Actions:**
1. A warrior with (**warrior job** AND `m_followTarget == null`) that sees a nearby distress (Step 8) → transitions to `callForHelp` → moves to the distressed hirdmandr + **defends** it (L2 anchor = the distressed hirdmandr).
**Build:** must succeed.
**Verify:** a nearby Thegn (no follow target) comes to a distressed hirdmandr + fights the threat; a **following** Himthiki does **not** break off.
**Exit state:** the call-response rule works (responders converge; followers stay locked).
**Status:** ⬜ NOT STARTED

### Step 11 — Defense-anchor generalization (L2)
**Goal:** one Harmony prefix on the base target-select re-prioritizes the nearest hostile to the **anchor** (following = the player; responder = the distressed hirdmandr).
**Design:** §7.
**Depends-on:** Step 10.
**Files:** `Hirdmandr\MonsterAIPatch.cs` (the target-select prefix), `HirdmandrAI.cs`.
**Actions:**
1. One Harmony **prefix on the base target-select**: when there is an anchor (a `m_followTarget`, or a distress signal), re-prioritize the nearest hostile to the anchor.
2. Confirm the exact base `MonsterAI` target-select method (§12.1).
**Build:** must succeed.
**Verify:** the responding warrior targets the threat **to the called hirdmandr** (proximity), not just any mob.
**Exit state:** L2 anchor targeting works for both anchors.
**Status:** ⬜ NOT STARTED

### Step 12 — Escape (`escapeHelp` / `escapeAny`)
**Goal:** overmatched → retreat **nearest helper > nearest shelter > away from danger**.
**Design:** §8.
**Depends-on:** Step 9.
**Files:** `Hirdmandr\sub1SM-Threatened.cs` (`escapeHelp`, `escapeAny`).
**Actions:**
1. Overmatched (HP < threshold or losing) → retreat: `escapeHelp` (nearest helper) → `escapeAny` (nearest shelter, else away from danger).
**Build:** must succeed.
**Verify:** an overmatched warrior retreats toward the nearest helper/shelter, else away from the threat.
**Exit state:** escape works per the priority.
**Status:** ⬜ NOT STARTED

### Step 13 — MP + raid regression
**Goal:** 2-client raid scenario — calls propagate, Thegns respond, escape works, **all clients stay consistent**.
**Design:** §9, §1.
**Depends-on:** Steps 8–12.
**Files:** whatever the test reveals.
**Actions:**
1. A 2-client raid: many enemies on a base; confirm calls propagate, Thegn response, escape, and all clients stay consistent (no divergence). Fix any divergence.
**Build:** must succeed.
**Verify (Thomas, 2 clients):** a raid; the retinue behaves consistently on both clients.
**Exit state:** P2 works end-to-end in MP.
**Status:** ⬜ NOT STARTED

---

## Phase 3 — Feel / tuning / QA

### Step 14 — Tune numbers
**Goal:** tune radii, thresholds, hysteresis, skill curves against Thomas's feel.
**Design:** §10.
**Depends-on:** Phase 2.
**Files:** `HirdmandrNPC.cs` / `HMSkills.cs` (the constants).
**Actions:** tune the call/response radius, "in danger" threshold, defense-anchor R, weapon-switch thresholds/hysteresis, level-scaling curves.
**Build:** must succeed.
**Verify:** Thomas plays; numbers feel right.
**Exit state:** numbers tuned.
**Status:** ⬜ NOT STARTED

### Step 15 — Edge-case QA
**Goal:** no bow, low HP, many simultaneous calls, player idle, a non-following Himthiki (should respond), an artisan that can't fight (calls + escapes).
**Design:** §4, §8.
**Depends-on:** Step 14.
**Files:** whatever the test reveals.
**Actions:** exercise the edge cases; fix crashes / divergence.
**Build:** must succeed.
**Verify:** no crashes / no divergence; behaviors sensible.
**Exit state:** edge cases handled.
**Status:** ⬜ NOT STARTED

### Step 16 — Final MP regression + rebuild + deploy
**Goal:** full rebuild (0 errors), deploy, confirm SHA256, a final 2-client raid.
**Design:** —.
**Depends-on:** Step 15.
**Files:** —.
**Actions:** full rebuild; deploy via `publish.ps1`; confirm SHA256; a final 2-client raid.
**Exit state:** common warrior shipped (P1+P2+P3), deployed, MP-verified.
**Status:** ⬜ NOT STARTED
