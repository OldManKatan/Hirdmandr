# Hirdmandr — Common Warrior: Design & Decisions

> **Purpose of this doc:** Durable design memory for the *common warrior* — the shared combat
> foundation used by the Warrior role and its two combat personas (**Thegn**, **Himthiki**).
> It records the DECISIONS and the REASONS/MOTIVATIONS so the design is not re-derived after a
> `/compact` or cold start.
>
> **Companion:** the bite-size, sequential **implementation plan** lives in
> `common_warrior_impl_plan.md`. **State/progress** (what's done, open items) lives in **PMW**
> (project `hirdmandr`), NOT in markdown.
>
> **Status (2026-10-16):** Design **DECIDED** (Thomas). **NOT yet implemented.** Build baseline
> clean: `0 errors / 21 warnings`, SHA256 `BF20BA258394A9F0BEB676E13E7AF5863E35044ADF81C9E68FAB91D60842A650`
> (deploy target `D:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\Hirdmandr`). Line numbers are
> from the files as read 2026-10-16.
>
> **Revisions (2026-10-16, Thomas):** capability gates **dropped** — skills are pure proficiency /
> level scaling (§4, §6); distress-signal transport **reopened** as **ZDO flag vs RPC** (Thomas leans
> RPC; decide in P2, §9).
>
> **Read order:** §1 scope → §2 the model → §3 personas → §4 weapon selection → §5 combat model →
> §6 skills → §7 targeting → §8 protection states → §9 MP mechanism → §10 defaults → §11 code refs →
> §12 open questions → §13 build.

---

## 1. Scope & intent

**Scope:** the common combat foundation for the Warrior role — how a warrior is *configured*
(style/range/skills), how it *picks a weapon*, how it *targets*, and how it *protects the player and
other hirdmandrs*. Individual persona specifics build on this; a future third persona reuses it.

**Motivating threat (Thomas 2026-10-16):** *"Large-scale coordinated attacks on player bases, called
'raids', exist in Valheim. This knowledge informs these design decisions."* The protection system
exists so a hirdmandr retinue can survive raids and so the **player's investment in an NPC's skills
is not lost** (a low/invested NPC escapes rather than dying; warriors converge on a threatened
hirdmandr).

**Hard constraints:**
- **Vanilla-first.** Prefer existing `MonsterAI` / `Humanoid` behavior; only add what vanilla lacks.
- **MP-consistent.** Combat is per-client vanilla today; we keep it per-client and make our config
  **deterministic** so all clients converge (no owner-gating combat).
- **No base-Valheim / BepInEx / Jotunn edits** (deploy the mod's own plugin folder only).
- Prefer **Harmony** hooks (pre/post/override).

**Out of scope (this cluster):** the Gathering job (a separate labor toggle, designed later), the
Gatherer implementation, artisan-job specifics, individual persona *lore*.

---

## 2. The model — orthogonal axes, job sets defaults

**Decision:** A warrior is described by **independent axes**:

| Axis | Values | Sets |
|---|---|---|
| **Role** | Artisan / Warrior | top-level category |
| **Job / persona** | Thegn / Himthiki (radio: **one** persona) | sets *default* style/range/skills |
| **Style** | defensive / offensive | aggression + retreat behavior |
| **Range** | close / mid / far | preferred engagement distance |
| **Skills** | fighter / melee / range (+ levels) | **proficiency** — scales *how well* (no capability gate) |

- **Job sets defaults on first selection** (only if the axes are still at their defaults — a
  customized warrior is not reset by re-selecting the job).
- **Every axis is independently overridable** in the GUI after that.
- **Jobs = a radio** (one persona at a time). **Gatherer = a separate labor toggle** (not a persona;
  designed later).

**Why (Thomas + Qwen, 2026-10-16):** "Let's separate weapon selection from the persona conversation
(defense/offense or far/medium/close)." A persona is a *tendency* (default style + range), not a
fixed class. Orthogonal axes give a sensible default out of the box (a Thegn is a guard) while letting
the player shape any individual (a Thegn that actually kites at range).

**Not chosen (and why):**
- **Persona = fixed weapon class** — rejected; weapon is *situational* (§4).
- **Persona = fixed style+range, no override** — rejected; too rigid (Thomas wants override).
- **Jobs as multi-checkbox (current GUI)** — rejected for the *combat* personas; a persona is one
  identity, so they are mutually exclusive (radio). (Gatherer stays a checkbox — it's labor, not a persona.)

---

## 3. Personas

### Thegn — the base guard (defensive)
- **Defaults:** defensive / mid / melee + shield. **Schedule:** day/night-shift patrol (keep existing).
- **Role:** the base's protection. **Always prioritizes calls for help** (§8) — in practice it has no
  `m_followTarget`, so it is *always* a call responder.
- **Why:** the raid-protection role — the guard that converges on a threatened hirdmandr.

### Himthiki — the player's escort (aggressive)
- **Defaults:** offensive / far / ranged-capable. **Mode:** follows the player (`m_followTarget` = the player).
- **Role:** the player's sword *and* bow; **mirrors the player's engagement** (§4).
- **Why (Thomas 2026-10-16):** *"I would rather see a Himthiki pull out a bow if the player is
  shooting at a far away creature than run in, but if they don't have a bow, they don't have a choice
  but to pull their weapon and run in."*

### The persona→role split (the load-bearing decision)
- **Himthiki** = the *following* warrior. Its anchor is **the player** (§7). While following, it is
  **player-locked** — it does **not** break off to answer a call (the player is its priority; it's
  fighting alongside the player against the most dangerous/numerous enemy).
- **Thegn** = the *guarding* warrior. Its anchor is **any distressed hirdmandr** (a call). It
  **always prioritizes calls for help** (even mid-patrol).
- **The general rule (§8) keys off `m_followTarget`, not the persona:** a following Himthiki is
  locked; a non-following Himthiki or a Thegn responds. So the two personas differ *by default* but the
  rule is uniform.

---

## 4. Weapon selection — dynamic, not a persona property

**Decision:** The actual weapon is chosen **at runtime**:
`weapon = f(enemy distance, the player's current engagement, available weapons, Range setting)`
with **hysteresis** (so it doesn't flap as distance crosses the threshold).

Mechanics:
1. **`EquipBest` equips best-of-each-type** — the best melee *and* the best bow (+ arrows) — so both
   types are always *available* (the humanoid holds one; the runtime decision swaps which).
2. A **runtime decision** picks the desired **type** (melee vs bow):
    - use **bow** when (a bow is **available** AND enemy beyond the bow threshold AND (player is
      engaging at range OR Range = far));
    - else use **best melee**.
    - If the desired type ≠ currently equipped type → **re-equip** (respecting hysteresis).
    - **No capability gate** — a bow is used whenever it is *available*; the `range` skill only scales
      arrow power (weak at 0, strong at 50), never whether it can be used (§6).
 3. **`HM_SelectBestAttack`** (already a per-frame Harmony override of `MonsterAI.SelectBestAttack`)
    returns the **equipped** weapon's attack — *not* "max `GetTotalDamage()`" (that max-damage
    behavior is the current bug that makes bows unusable).
 4. **Availability:** no weapon of the desired type owned ⇒ the other type.
 5. **Far + no bow:** clamp to **best melee at natural reach** — never fake ranged.

**Why dynamic (Thomas 2026-10-16):** weapon choice should come down to *"what is available and how far
away the enemy is"* + what the player is doing — not a fixed class. (See the Himthiki example in §3.)

**Not chosen (and why):**
- **Keep `HM_SelectBestAttack` = max damage** — rejected; it's the current bug (bows never win).
- **Persona fixes the weapon class** — rejected; contradicts "pull the bow if the player's shooting far."
- **Equip only the default weapon** — rejected; both types must be *available* for the runtime swap.

---

## 5. Combat model — vanilla MAI, per-client, ONE source of truth

**Decision:**
- Combat stays the **vanilla `MonsterAI`**, **per-client** (vanilla is per-client; we stay
  vanilla-consistent and do **not** owner-gate combat).
- **`ApplyCombatConfig()`** is the **single source of truth** for every `MonsterAI` field, computed
  from `(role, persona, style, range, skills, available gear)`. It is applied on **all clients**:
  in `SetupNPC` after load, **and** on `RPC_HMSetting` / skill change. It is **deterministic**, so
  all clients converge → MP-safe.
- **`EquipBest` equips *items only* (no MAI writes)** — so MAI config lives in exactly one method.

**Style/Range → MAI mapping (MY DEFAULTS, tune in P3):**
- **Range →** `m_maxChaseDistance` (+ per-weapon `m_aiAttackRange`): close ≈ 10, mid ≈ 15, far ≈ 25.
- **Style →**
  - **defensive:** `m_fleeIfLowHealth` = true; more `m_circulateWhileCharging`; longer `m_minAttackInterval`.
  - **offensive:** `m_fleeIfLowHealth` = false; shorter `m_minAttackInterval`; less strafe.

**Why one source of truth:** today the MAI fields are set in ONE place (`SetupNPC` :799-810) and the
GUI's style/range are **dead** (persisted + synced but never consumed). Centralizing both the
initial config and the on-change config into one deterministic method fixes the dead controls AND
guarantees MP consistency.

**Not chosen (and why):**
- **Set MAI fields in multiple places** (SetupNPC + per-setting handlers) — rejected; they'd drift.
- **Owner-gate combat** — rejected; combat is per-client vanilla, and owner-gating it would diverge
  from vanilla (and from the other clients). We apply a *deterministic* config to all clients instead.

---

## 6. Skills — level scaling only (NO capability gates)

**Decision (REVISED 2026-10-16, Thomas — capability gates DROPPED):** a skill level scales *how well*
a warrior uses that thing — it never gates *whether* it can. A warrior may use any weapon it owns at
any skill level; at 0 it is simply worse at it than one at 50.

| Skill | Level scaling |
|---|---|
| `fighter` | toughness — **maxHP = 60 + level**; heal tick = 2 + level/3 (both **keep** — already wired) |
| `melee` | melee damage multiplier `1 + level/50` (revive the commented-out line) |
| `range` | arrow power (damage/accuracy) — a 0-range warrior fires a bow but *weakly*; 50-range fires it *well* |

- **No capability gate** (Thomas 2026-10-16): *"If I want an NPC to get good with the bow, but they
  can't equip a bow at 0 skill, I can never overcome that hurdle. I would rather allow them to use a
  bow and just be worse at using it than another NPC with ranged skill 50."*
- Whether a **warrior fights at all** is the *role*, not a skill — `fighter` is pure toughness.
- Level scaling revives the commented-out `melee` damage line (currently the only skill that does
  anything is `fighter`).

**Why (Thomas 2026-10-16):** *"for hirdmandr with artisan jobs, there will be significant player
investment in their skills."* Skills get *better* with investment — and the protection system (§8)
safeguards that investment.

**Not chosen (and why):**
- **Capability gate (skill must be > 0 to *use* the weapon type)** — **rejected** (Thomas
  2026-10-16); it made a 0-range NPC unable to ever get good with a bow (unwinnable hurdle).
- **Persona-locked skills** — rejected; skills stay orthogonal (overridable).

---

## 7. Targeting — the defense anchor (3 layers)

**Decision:**
- **L0 — friendly filter:** never target the player / allies (all modes). *(already the `BaseAI.IsEnemy`
  unrescued filter pattern; extend to "player/allies are never enemies.")*
- **L1 — vanilla aggro:** the base `MonsterAI` target behavior (unchanged).
- **L2 — defend the anchor (by proximity):** the NPC prioritizes the threat to **its anchor**:
  - a **following** warrior's anchor = **the player**;
  - a **responder's** anchor = **the distressed hirdmandr** (the call).

**Why:** this **unifies** "defend the player" (follow) and "defend the called hirdmandr" (call) as
**one mechanism** — *defend an entity within radius R*. The anchor is just "who am I protecting right
now."

**Implementation note (P2):** one Harmony **prefix on the base target-select** that, when there is an
anchor (a `m_followTarget`, or a distress signal), re-prioritizes the nearest hostile to the anchor.
The pure-vanilla guard path runs when there's no anchor. *The exact base `MonsterAI` target-select
method is confirmed in P2 (§12).*

**Not chosen (and why):**
- **A fully custom targeting state machine** — rejected; it would fight vanilla. We layer a *priority*
  on vanilla selection instead.
- **Key the defense on the persona** — rejected; it's keyed on *state* (following / a distress), which
  is what makes the two personas fall out of one rule.

---

## 8. Protection states (the `ThreatenedSM` stubs) + call-response rule

**Decision (state semantics — confirmed by Thomas 2026-10-16):**
- **`panic`** (both personas): the immediate *"I'm in danger"* reaction → **raise the call** +
  **disengage** (a warrior repositions to fight; an **artisan can't fight**, so it calls + escapes).
- **`callForHelp`**:
  - *victim:* broadcast the **distress** (§9);
  - *warrior responder:* move to the distressed hirdmandr + **defend** it (§7 L2 anchor).
- **`escapeHelp`**: retreat **toward a helper** (another hirdmandr / the player).
- **`escapeAny`**: retreat to **any safe spot** (a shelter / simply away from danger).
- **Escape priority (Thomas 2026-10-16):** *"prioritize nearest helper, nearest shelter, away from
  danger."* ⇒ `escapeHelp` (nearest helper) → `escapeAny` (nearest shelter, else away).

**"In danger" trigger (MY DEFAULT, tune P3):** a hirdmandr is *in danger* if it is **being attacked**
OR its **HP < 30%**.

**Call-response rule (Thomas 2026-10-16, refined):**
> **"anyone with a warrior job responds to calls for help, unless they have an `m_followTarget`."**

- So: a **following** warrior (has `m_followTarget` = the player) is **player-locked** (no call
  response). A **non-following** warrior (Thegn, or a Himthiki that isn't following) **responds**.
- **Who raises a call:** *any* hirdmandr in danger (an artisan, a Thegn, a Himthiki).
- **Who answers:** *nearby* warriors meeting the rule (within the call/response radius, §10).

**Why (Thomas 2026-10-16):** *"the purpose of the callForHelp and escape states is to safeguard that
investment by bringing warrior jobs to the area of violence (callForHelp) and trying to avoid the
danger (escape)."* Raids are the motivating threat (§1).

**Not chosen (and why):**
- **Leave the stubs "not implemented"** — rejected; Thomas explicitly wants the emergent behavior.
- **Owner-gate the protection** — rejected; it's per-client, driven by the shared distress signal (§9).

---

## 9. MP mechanism — the distress signal (OPEN: ZDO flag vs RPC — decide in P2)

**Decision (OPEN — revisit in Step 8 of the impl plan; Thomas leans **RPC**, 2026-10-16: "we lose
nothing by deciding later").** Either way the contract is:
- A distressed hirdmandr publishes a **distress marker + expiry ~10 s**, **re-asserted while still
  in danger**.
- A responding warrior **observes the distress** (nearby, within the call/response radius) →
  transitions to the responder state (`callForHelp` → move to + defend).
- **Deterministic + MP-consistent:** every client's per-client `MonsterAI` must make the same
  decision from the same signal.

**Option A — shared flag on the NPC's own ZDO** (the original vanilla-consistent design):
- The distressed hirdmandr sets a flag on its own ZDO; a responding warrior **already scans
  `m_ZDOsInRange`** (the existing per-client scan other jobs use) → it sees the flag and transitions.
- *Pros:* reuses the existing scan; no new netcode message; ZDO-synced.
- *Cons:* a ZDO flag is implicit shared state — the scan must be taught to *read* distress (not just
  aggro), and expiry/re-assertion lives in the per-client state machine.

**Option B — dedicated RPC** (Thomas leans this way):
- The distressed hirdmandr broadcasts a small **RPC** ("in distress") to nearby warriors with the
  ~10 s expiry, re-asserted while in danger.
- *Pros:* explicit, direct, easy to reason about; no reinterpretation of the ZDO scan.
- *Cons:* a new netcode message; must guarantee all clients converge (an RPC to *whom*? owner-broadcast
  vs a ZDO-attached RPC).

**Open (P2/P3, §12):** ZDO vs RPC (this section); exact expiry/cadence; confirm the scan surfaces
*distress* to a responder (not just aggro) if A; how a responder *moves* to the distress (reuse topSM
`MoveTo` vs a dedicated state).

---

## 10. Defaults (all **MY DEFAULTS** — tunable in P3, not Thomas decisions)
- **Call/response radius** ≈ 50 m ("nearby").
- **"In danger":** being attacked OR HP < 30%.
- **Defense-anchor R** (proximity) ≈ 12–15 m.
- **Weapon switch:** bow when enemy > ~18 m (or the player is engaging at range); back to melee < ~12 m
  (the hysteresis band is ~12–18 m).
- **Proficiency / level scaling (no capability gate):** `fighter` maxHP = 60 + level + heal tick
  (keep); `melee` → damage `1 + level/50`; `range` → arrow power.
- **Distress expiry** ≈ 10 s (re-asserted while in danger).

---

## 11. Code references (current state — 2026-10-16)
- `Hirdmandr.cs` :401 Goblin clone; :403 destroy the cloned `MonsterAI`; :424 add a **fresh** `MonsterAI`.
- `HirdmandrNPC.cs` —
  - style/range **fields** :81-86; **load** :1311-1316; **save** :1354-1359; **(dead) apply** :1607-1611.
  - `SetupNPC` MAI init :799-810 (view 15 / hear 15 / maxChase 10 / attackPlayerObjects false / smoothMovement true).
  - `PopulateCombatProps` :1656-1661 (**only `fighter` wired** → maxHP + heal tick; the `melee` damage line is commented out).
  - `EquipBest` :1887-2085 (best by max `GetTotalDamage`; shield unless 2H/bow; **bow path sets `m_maxChaseDistance = 0`**).
  - `HM_SelectBestAttack` :2213-2236 (**max damage** — the bug that starves bows).
  - `InvokeSetting` / `RPC_HMSetting` / `ApplySettingLocal`; follow via `SetFollowTarget` (:1711/1734/1776).
  - after-rescue `Faction.Players` :1727.
- `HirdmandrAI.cs` — owner-gated top-level wrapper; threatened entry from `IsAlerted()` :145-161.
- `topSM.cs` — schedule priority **Thegn (day/night) > Himthiki (day) > Artisan/unassigned**; a jobless
  Warrior falls to the Artisan/unassigned branch.
- `sub1SM-Threatened.cs` — the **stubs** (`callForHelp` / `escapeHelp` / `escapeAny` / `panic`), all "not implemented."
- `MonsterAIPatch.cs` — redirects `MonsterAI.SelectBestAttack` → `HM_SelectBestAttack`; `BaseAI.IsEnemy`
  unrescued filter.
- `HMSkills.cs` — skill registry (`fighter`/`melee`/`range`); **`melee`/`range` inert**, only `fighter` wired.
- `HirdmandrGUI.cs` — role / style / range = radio `ToggleGroup`s; jobs = checkboxes (**→ radio in P1**).
- `HirdmandrDebugMAI.cs` — the MAI + weapon `ItemData` tuning surface reference.

---

## 12. Open questions (resolve in P2/P3)
1. The exact base **`MonsterAI` target-select method** to hook for the L2 defense-anchor.
2. Fresh `MonsterAI` **default target type** (no explicit `m_aiTargetType` is set — confirm vanilla default).
3. Exact **`m_maxChaseDistance = 0`** semantics (currently set on the bow path in `EquipBest`).
4. Exact **`m_circulateWhileCharging`** / **`m_fleeIfLowHealth`** behavior.
5. **Base bow firing / ammo** behavior (does the fresh MAI fire the equipped bow + consume arrows?).
6. **Base disengage/recovery** behavior (what vanilla does when overmatched, before we add escape).
7. **ZDO load order at `Awake`** on all clients (does each client's `SetupNPC` run with the loaded settings?).
8. How a **responder moves** to the distress (reuse topSM `MoveTo` vs a dedicated state).
9. Whether a **Thegn can be put in follow mode** (default: Thegn = guard, no follow; the rule still
   holds via `m_followTarget`).
 10. Distress **expiry/cadence** + exact **"in danger"** detection.
 11. **Distress transport: ZDO flag vs RPC** — Thomas leans **RPC** (2026-10-16); decide in P2 (impl
     plan Step 8). *"we lose nothing by deciding later."*

---

## 13. Build / deploy (from `AGENTS.md`)
- Build (VS2022 Community MSBuild; no cargo/cmake/gcc on this box):
  ```
  & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\OpenCode\hirdmandr_project\hirmandr_repo\Hirdmandr.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU" /p:ExecutePrebuild=false /nologo /m
  ```
- `/p:ExecutePrebuild=false` skips the Jotunn prebuild (don't regenerate `publicized_assemblies`).
- On success, a post-build task runs `publish.ps1` → deploys to
  `D:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\Hirdmandr`.
- **New `.cs` files MUST be added to `Hirdmandr.csproj`** (`<Compile Include="Foo.cs" />`); an omitted
  file is silently left out of the assembly (→ `CS0103`).
- **Compile errors surface in waves** — fix, rebuild, repeat until clean.
- Baseline (pre-work): `0 errors / 21 warnings`, SHA256 `BF20BA258394A9F0BEB676E13E7AF5863E35044ADF81C9E68FAB91D60842A650`.
