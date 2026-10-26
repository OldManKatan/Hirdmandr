# Hirdmandr GUI Research — PMW #44 (GUI refresh)

Append-only research log. Each finding = `### G-NN` (date, category, file:line refs).
Grep-friendly: search "G-NN", category words, or file names to re-locate context after summarization.

Status: RESEARCH COMPLETE (2026-10-14) — findings G-01–G-14, OQ resolved, Recommendations, and §4 defect-analysis & planned-changes. Re-equip-on-toggle = INTENDED (Thomas, 2026-10-14). **4.1–4.7 IMPLEMENTED & build-verified 2026-10-14 (MSBuild 0 errors, deployed 11:47 AM)** (4.7 partial: fractional skill display left as `(int)` by design; name-refresh added as insurance for a future rename feature). **UI tweak (Thomas, 2026-10-14):** both panels now spawn centered in view (anchor `(0.5,0.5)` / position `(0,0)`) instead of right-anchored. **In-game smoke test PASSED (Thomas, 2026-10-14):** 4.3 follow (defends when alerted, resumes on threat clear, death auto-stop) + 4.5 death-teardown (open panel → kill/despawn NPC → no ghost panel + control restored).

## 0. Scope & map

All GUI is runtime-built UGUI via Jotunn `GUIManager` (no IMGUI, no scene/prefab assets, no .unity files).

| File | Size | Role |
|---|---|---|
| `HirdmandrGUI.cs` | 1103 lines | Main NPC panel: name, talk text, skill list, role (artisan/warrior), 7 artisan checkboxes, 3 warrior classes, style (defense/offense), range (close/mid/far), 3 ToggleGroups |
| `HirdmandrGUIRescue.cs` | 179 lines | Rescue panel: name, speech text, Follow / Stay / NewHome buttons |
| `Hirdmandr.cs` | ~32KB | Plugin entry. Adds both GUI components to NPC prefab (L576-577). Item icons via `GUIManager.GetSprite` (L140-372) |
| `HirdmandrNPC.cs` | ~93KB | Interaction logic: Use→TogglePanel (L995/999), rescue button handlers (L1584/1610/1617), panel re-open (L1698/1704), `GetRescueText` (L1507), `OpenInventory` (L1726), `EquipBest` (L1740) |
| `NPCPlayerClone.cs` | ~72KB | Decompiled Valheim `Player` clone on every NPC (interaction entry point? verify) |
| `HMSkills.cs` | ~10KB | Skill data model behind the panel skill list (`m_hmSkills`, `m_readable`, `m_value`) |
| `topSM.cs` | ~15KB | `EquipBest` on WorkDay exit (L183) — GUI-adjacent side effect |
| `sub1SM-WorkDay.cs` | ~61KB | Cultivator re-equip guard after GUI toggle / EquipBest swap (L258) |

Shared conventions (both panels, confirmed by reading):
- **Lazy create-once**: panel GameObject created inside first `TogglePanel()` call (`if (!GUIHirdmandr)`), cached in field, never destroyed — only `SetActive` toggled.
- **Parent**: `GUIManager.CustomGUIFront` (Jotunn custom-GUI layer). `CreateWoodpanel(...)` with `draggable: true`.
- **Close keys** (Update() poll): `Inventory` / `JoyButtonB` / `JoyButtonY` / `Use` while panel active; each followed by `ZInput.ResetButtonStatus(...)`; `Escape` is commented out in both files.
- **Toggle side effects** (both open AND close): `m_hirdmandrnpc.OpenInventory(state)`, `m_hirdmandrnpc.EquipBest()`, `GUIManager.BlockInput(state)`.

## 1. Findings

### G-01 (2026-10-14) [architecture] Panels are create-once, never destroyed
- `HirdmandrGUI.cs:91` / `HirdmandrGUIRescue.cs:49` — create-once pattern; no `Destroy(...)` anywhere seen so far.
- Panel is a child of `CustomGUIFront` (a persistent global layer), NOT of the NPC. If the NPC dies/despawns/unloads, the panel object likely leaks (or worse: next TogglePanel on a dead NPC NREs on `GetComponent<...>` results). VERIFY: NPC death/despawn path + any OnDestroy.

### G-02 (2026-10-14) [data staleness] Main panel skill text built once at creation
- `HirdmandrGUI.cs:108-120` — `skillnames`/`skillvalues` strings built only inside the create-once block; fed to `CreateText` at L187/L202.
- On re-open, only `g_talk` is refreshed (L904). `g_name` / `g_skillnames` / `g_skillvalues` appear NOT refreshed (VERIFY no refresh method in L800-1103).
- Skill values displayed as `(int)each_skill.m_value` (L119) — fractional progress hidden, and values never update while panel is cached.

### G-03 (2026-10-14) [side effects] TogglePanel runs EquipBest() on open AND close
- `HirdmandrGUI.cs:920`, `HirdmandrGUIRescue.cs:172` — closing the panel re-equips the NPC.
- Compensating guards already exist downstream: `topSM.cs:183`, `sub1SM-WorkDay.cs:258`, `HirdmandrNPC.cs:1867/1980-2020` (farmer cultivator re-equip). This coupling is a fragility: GUI toggle has world-state side effects beyond show/hide.

### G-04 (2026-10-14) [naming] Main panel "talk" text is initialized from `GetRescueText()`
- `HirdmandrGUI.cs:158` — `g_talk` created with `m_hirdmandrnpc.GetRescueText()`. The "Rescue"-named method is the generic speech text for BOTH panels. Method defined `HirdmandrNPC.cs:1507` (read pending).

### G-05 (2026-10-14) [input] Close-key handling includes "Use" + ResetButtonStatus hack
- Both `Update()`s: `GetButtonDown("Inventory"|"JoyButtonB"|"JoyButtonY"|"Use")` then `ZInput.ResetButtonStatus` per key (HirdmandrGUI.cs:63-85, HirdmandrGUIRescue.cs:20-42).
- `Input.GetKeyDown(KeyCode.Escape)` is commented out (L73 / L30) — reason unknown (probably Escape already handled by game or caused double-toggle). VERIFY intent.
- Same-key open/close race: if "Use" opens the panel, does the same press (or held state) immediately re-trigger TogglePanel and close it? ResetButtonStatus may be the band-aid. VERIFY open path.

## 2. Open questions (dispatched to sub-agents 2026-10-14)
- OQ-1: What do the role/artisan/warrior toggles DO — event handlers, what HirdmandrNPC state they write, mutual-exclusion logic? (HirdmandrGUI.cs ~L800-903)
- OQ-2: Exact interact/Use flow that opens each panel; state guards; MP guards. (HirdmandrNPC.cs ~L950-1010, L1550-1720; NPCPlayerClone.cs)
- OQ-3: `OpenInventory(bool)` — what does it actually do (character inventory UI?), why does the rescue panel open it too?
- OQ-4: Multiplayer: panels created per-client? NPC death/despawn while panel cached? `HirdmandrNPC.cs:694` comment hints at remote-stall awareness.
- OQ-5: Save/load + world load: components re-added on NPC prefab (Hirdmandr.cs:576-577) — do cached panels/roles survive? Where do role/skill settings persist (HMSkills flags)?
- OQ-6: Contents of HirdmandrGUI.cs L800-1103 (post-creation methods: refresh? helpers? destroy?).
- OQ-7: Any Destroy/OnDestroy/cleanup of panel objects anywhere in the mod.

### 1b. Additional findings (deep-read pass, 2026-10-14)

### G-06 (2026-10-14) [correctness] `allowSwitchOff` typo — Engagement Range group never locked
- `HirdmandrGUI.cs:894` sits inside the Engagement-Range block (L874-894) but sets `g_style_tg.allowSwitchOff = false;` instead of `g_range_tg`. Net effect: `g_range_tg` is never locked → player can switch OFF all three range toggles (no range selected); `g_style_tg.allowSwitchOff` is set twice (L872 + L894, harmless). Parity targets: role L842, style L872.

### G-07 (2026-10-14) [correctness/polish] "Follow" button not hidden in Artisan page
- `HirdmandrGUI.cs:804-824` builds `all_wars` (18 objs) WITHOUT `g_war_b_himthikiFollow` (Follow button, L585-597). Role page show/hide loops (L845-854, L936-960) iterate `all_wars`, so the Follow button is never `SetActive(false)` when switching to Artisan — it lingers in the Artisan page (visible but inert unless the NPC is a himthiki).

### G-08 (2026-10-14) [correctness, latent] `GetRescueText` indexes `rescueme` by `abandon.Count`
- `HirdmandrNPC.cs:1525`: `rescueme[Random.Range(0, abandon.Count)]`. Masked today because both lists are size 4; editing either list to a different length → IndexOutOfRange / wrong pick. Should be `rescueme.Count`.

### G-09 (2026-10-14) [fragility] Panel state tracked two different ways + close-side-effects
- Main panel: open-state in FIELD `bool state` (`HirdmandrGUI.cs:15`), flipped `:900`; the `activeSelf`-derived version is commented out at `:899`. Rescue panel: derived from `activeSelf` (`HirdmandrGUIRescue.cs:162`). Same pattern, two strategies.
- Create-once differs: main `SetActive(true)` on build (L134) vs rescue `SetActive(false)` (L75).
- `update_skills()` runs unconditionally `:916` (open AND close) — rebuilds skill text into widgets being hidden on close (wasteful, harmless).

### G-10 (2026-10-14) [fragility] Action buttons "close" by calling TogglePanel → full side-effect chain
- Rescue buttons: `Rescue`/`RescueWait`/`WelcomeHome` → `m_guirescuecomp.TogglePanel()` (`HirdmandrNPC.cs:1584/1610/1617`). Main "Follow" button: `HimthikiFollow` → `m_guiHirdmandr.TogglePanel()` (`HirdmandrNPC.cs:1698/1704`).
- Consequence: every action click also fires `OpenInventory(false)` (hides inventory) + `EquipBest()` (re-equip best gear) + (main only) `SaveSkills()` + `ZDOSaveGeneral()`. An action click has world-state side effects beyond its action.

### G-11 (2026-10-14) [fragility] Panel GameObject leak — parented to global UI layer, no cleanup
- Panels parented to `GUIManager.CustomGUIFront` (`HirdmandrGUI.cs:127`, `HirdmandrGUIRescue.cs:68`), NOT the NPC. No `OnDestroy`/cleanup in `HirdmandrGUI`, `HirdmandrGUIRescue`, or `HirdmandrNPC` (grep: none). Cached `GUIHirdmandr`/`GUIRescue` is not destroyed on NPC death/despawn/unload → on re-add the field is null again → a SECOND panel is created, stacking under CustomGUIFront; a zombie panel keeps its `Update()` close-key poll running.

### G-12 (2026-10-14) [architecture] GUI open hook = native `Interactable` interface (no Harmony)
- `HirdmandrNPC : MonoBehaviour, Interactable` (`HirdmandrNPC.cs:23`). `Interact(Humanoid user, bool hold, bool alt)` (L978-1009): returns `false` on `hold` (L984), resets 4 input buttons (L988-991), `m_isHirdmandr ? m_guiHirdmandr.TogglePanel() : m_guirescuecomp.TogglePanel()` (L993-1000), `PopulateCombatProps()` (L1001), returns `false` (L1002). `UseItem` (L1011-1014) = no-op.
- `PopulateCombatProps` (L1528-1533) re-derives `m_healTick` + max health from `fighter` skill on EVERY interact.
- `sub1SM-WorkDay.cs:984` `crop.Interact(npc,false,false)` is the unrelated game crop-harvest path (not the GUI).

### G-13 (2026-10-14) [coupling] `OpenInventory` shared by both panels (resolves OQ-3)
- `OpenInventory(bool)` (`HirdmandrNPC.cs:1726-1738`): show/hide `InventoryGui.instance` on the NPC's `m_container`, then `m_container.Save()` + `m_visequip.UpdateEquipmentVisuals()`. Both panels call it in TogglePanel → opening EITHER panel also opens the NPC's inventory; closing hides it. Shared coupling, not per-panel intent.

### G-14 (2026-10-14) [correction to G-02] Skill text IS refreshed on every toggle
- G-02 said `g_skillnames`/`g_skillvalues` are "NOT refreshed" on re-open. CORRECTION: they ARE refreshed on every toggle by `update_skills()` (`HirdmandrGUI.cs:916` → `1081-1101`). Genuine fidelity gaps remain: (a) `g_name` (NPC name, L142-155) is created once, never refreshed; (b) skill values render `(int)m_value` (L119, L1095) → fractional progress truncated (3.7 shows 3). `g_talk`(main)/`g_speak`(rescue) ARE refreshed on open (L904 / L166).

## 2b. Open questions — resolved (2026-10-14)
- **OQ-1 (role/artisan/warrior toggle behaviour):** role = `g_role_tg` (ToggleGroup, allowSwitchOff=false). `RoleChanged` (`HirdmandrGUI.cs:927-963`) reads both `isOn`, `InvokeSetting("roleArtisan"/"roleWarrior")`, show/hide `all_arts`/`all_wars`, fires `ShowTutorial("hirdmandr_artisan")` for local player. Artisan toggles → `InvokeSetting("skill:<name>")` (Wood/Furnace/Farmer/Cook/Baker/Lamplighter/Mender, L965-998). Warrior: `CombatJobThegen`/`CombatJobHimthiki` (L1000-1036) → `InvokeSetting("jobThegn"/"jobHimthiki")` + tutorials; `ToggleGatherer` (L1038) → `jobGatherer`; `FightingStyle` (L1044) → fightingStyleDefense/Offense; `FightingRange` (L1060) → fightingRangeClose/Mid/Far. All → `HirdmandrNPC.InvokeSetting` (local/RPC split).
- **OQ-2 (interact/Use flow):** open = native `Interact` (G-12); close = `Update()` key-poll (Inventory/JoyB/JoyY/Use, both files) OR an action button (G-10). MP: panel is client-local (CustomGUIFront, created on first local toggle); MP guards are the `m_user == Player.m_localPlayer` tutorial checks; interaction itself is local.
- **OQ-3 (OpenInventory):** G-13.
- **OQ-4 (MP / death / remote):** client-local panel; death/despawn → leak (G-11); the `HirdmandrNPC.cs:694` remote-stall comment is on an AI path, not the GUI.
- **OQ-5 (save/load):** role/job/skill/enable bools persist via `ZDOSaveGeneral()` + `HMSkills.SaveSkills()` (main close L912-913; each Rescue/RescueWait/WelcomeHome). On world load, components re-added on the prefab (Hirdmandr.cs:576-577); the cached panel does NOT survive (rebuilt per component instance); persisted bools re-seed the toggles at create-once (L248/312/537/829-841/…).
- **OQ-6 (L800-1103 contents):** create-once tail (toggle groups L826-894, initial page show L844-854) + `RoleChanged`, 7 artisan `*Toggle` handlers, `CombatJobThegen/Himthiki`, `ToggleGatherer`, `FightingStyle`, `FightingRange`, `update_skills`. No other refresh, no destroy.
- **OQ-7 (Destroy/OnDestroy/cleanup):** NONE in any of the three files (see G-11).

## 3. Recommendations

Grouped by severity. (Research only — no code changes made this turn.)

**P0 — correctness (safe, low-risk fixes)**
1. `HirdmandrNPC.cs:1525` — index `rescueme` by `rescueme.Count` (G-08). Removes a landmine.
2. `HirdmandrGUI.cs:894` — `g_range_tg.allowSwitchOff = false;` (G-06). Parity with role/style groups.
3. **[DECISION — Thomas]** Unify the two panels' open-state strategy. Recommend deriving from `activeSelf` (like rescue) rather than a field (G-09). If keeping the field, delete the dead `:899` comment and audit writers.

**P1 — fragility / coupling (the core "refresh" work)**
4. ~~Decouple "close" from the side-effect chain (G-03/G-10)~~ — **RETRACTED (Thomas, 2026-10-14).** Re-equipping best gear on toggle is **intended**: Jarls change an NPC's gear *during* GUI interaction, and the NPC should equip the best gear immediately when it receives new gear. See §4.0/§4.8.
5. ~~Move `EquipBest()` out of the open/close path (G-03)~~ — **RETRACTED:** same reason. The compensating guards in `topSM.cs:183`/`sub1SM-WorkDay.cs:258` are the accepted cost of that intent. See §4.8.
6. Add `g_war_b_himthikiFollow` to `all_wars` (or `SetActive(false)` it in `RoleChanged` when entering Artisan) so the Follow button doesn't linger (G-07).
7. Panel lifecycle (G-11): add cleanup. Options: destroy the panel GameObject on close and re-create on open (simplest; removes leak; widgets are cheap runtime objects), or add `OnDestroy` on the NPC that destroys the cached panel. (Parenting to the NPC is wrong — it's a UI-layer object.)
8. Null-guard `m_user` before `.GetComponent<Player>()` in `RoleChanged`/`CombatJob*` (`HirdmandrGUI.cs:944/1007/1026`).

**P2 — polish / fidelity**
9. Refresh `g_name` on open; render skill values with the fractional part when non-integer (`HirdmandrGUI.cs:119/1095`) (G-14).
10. Skip `update_skills()` on close (only refresh on open) (G-09).
11. De-duplicate the two panels' boilerplate (identical `Update()` close-key block + null-check guards) via a small shared helper.
12. Replace the flat `all_arts`/`all_wars` arrays with per-page parent GameObjects ("artisan page"/"war page") and `SetActive` the page root — less error-prone than the flat arrays (root cause of G-07).

## 4. Defect analysis & planned changes (2026-10-14) — 4.1/4.2 implemented & build-verified; 4.3+ pending

For each issue: **What** / **Why it's a defect** (the failure mode) / **Planned change** / **Risk**.

### 4.0 Decision recorded (Thomas, 2026-10-14)
- **Re-equip on toggle = INTENDED.** Any `TogglePanel` (including Rescue/Follow action-button closes) re-equips best gear on purpose: Jarls change an NPC's equipment *during* GUI interaction, and the NPC should equip the best available gear immediately when it receives new gear. → **G-03 / G-10 / G-13 are by design, not defects.** The "de-couple / move `EquipBest` out" recommendations are **retracted**; the `topSM.cs:183` / `sub1SM-WorkDay.cs:258` guards are the accepted cost.

### 4.1 `HirdmandrNPC.cs:1525` — `GetRescueText` wrong index (G-08)
- **What:** `rescueme[Random.Range(0, abandon.Count)]` indexes the `rescueme` list with the *other* list's count.
- **Why it's a defect:** `abandon` and `rescueme` are independent fragment lists. Both happen to be length 4 today, so `Range(0,4)` is valid for both — but only by coincidence of equal lengths. The moment either list is edited to a different length (a natural content tweak), `rescueme` is indexed by the wrong bound → `IndexOutOfRangeException` on Interact/panel open, or a silently wrong line. Latent, but a one-line zero-risk fix.
- **Planned change:** `rescueme[Random.Range(0, rescueme.Count)]`.
- **Risk:** none.
- **Status:** IMPLEMENTED 2026-10-14 — applied at `HirdmandrNPC.cs:1525`; MSBuild rebuild 0 errors.

### 4.2 `HirdmandrGUI.cs:894` — `g_range_tg.allowSwitchOff` never set (G-06)
- **What:** The Engagement-Range group is built (L874) and wired, but the line meant to lock it accidentally targets `g_style_tg` (already locked at L872).
- **Why it's a defect:** A `ToggleGroup` defaults to `allowSwitchOff = true`, so the player can click the selected range toggle and turn it *off*, leaving **zero** of Close/Mid/Far selected — an invalid combat state (no engagement range). The role (L842) and style (L872) groups are correctly locked; range is the one that slipped. Same "one-of-N radio" invariant the other two enforce.
- **Planned change:** `g_style_tg.allowSwitchOff = false;` → `g_range_tg.allowSwitchOff = false;` at L894.
- **Risk:** none.
- **Status:** IMPLEMENTED 2026-10-14 — applied at `HirdmandrGUI.cs:894`; MSBuild rebuild 0 errors.

### 4.3 Follow — generalization + toggle (RE-SCOPED by Thomas, 2026-10-14; supersedes the original G-07 page-membership fix)
**New intent:** any **welcomed** NPC (any role/job, not just himthiki) can be made to follow the player at any time, as a transient toggle: button reads "Follow" (not following) → "Stop Follow" (following). While following, the NPC's normal hmAI state machine is paused — but it still defends itself when threatened (D1). Himthiki job stays (its schedule/identity); follow just stops being gated on it. Non-welcomed NPCs are unchanged (they use the rescue panel).

**Key discovery (reframes the task from "build follow" to "generalize + polish"):**
- Follow engine = Valheim-native `MonsterAI.SetFollowTarget(GameObject)` / `GetFollowTarget()` / `ResetPatrolPoint()` / `SetPatrolPoint()`. Target-generic. Defined in the game, NOT the mod (`MonsterAIPatch.cs` only patches `SelectBestAttack` + `IsEnemy`; grep finds no mod-side definition, and the code compiles). Owner-driven, **transient** (not in `ZDOSaveGeneral` → does not survive save/load/owner change).
- Hold-state already exists: `topSM` state `himthikiFollow` (`topSM.cs:47,376-393`). While `m_himthikiFollowing` is true it parks the NPC out of the `schedule` router (pauses work/rest/patrol/socialize); `HirdmandrAI.Update` (owner) force-enters it every tick while the flag is set (`HirdmandrAI.cs:143-145`); it returns to `schedule` when the flag clears. This IS the "pause other functions" behaviour, and it already works.
- A toggle already exists but is himthiki-locked: `HimthikiFollow()` (`HirdmandrNPC.cs:1690-1708`) flips `m_himthikiFollowing` and sets/clears the follow target — wrapped in `if (m_jobHimthiki)`.
- The button is already created for **all** NPCs (build is flat, no role gate; `HirdmandrGUI.cs:585`) and is in neither `all_arts` nor `all_wars` → always visible — but dead for anyone but himthiki warriors (handler gate) and its label never changes.

**Proposed change (generalize + polish; small):**
1. `HirdmandrNPC.cs` — rename `HimthikiFollow()` → `FollowToggle()`; **remove the `if (m_jobHimthiki)` gate** (works for any NPC). Keep the body: on start `ResetPatrolPoint()` + `SetFollowTarget(m_user.gameObject)`; on stop `SetPatrolPoint()` + `SetFollowTarget(null)`; flip flag; close panel.
2. `HirdmandrNPC.cs` — rename field `m_himthikiFollowing` → `m_following` (transient; no save impact).
3. `HirdmandrAI.cs:143-145` + `topSM.cs:47,376-393` — use the renamed flag; optionally rename state `himthikiFollow` → `follow` (`NodehimthikiFollow` → `NodeFollow`).
4. `HirdmandrGUI.cs` — (a) rename field `g_war_b_himthikiFollow` → `g_b_follow`; (b) reposition to a role-neutral spot (panel header/top; stays out of both `all_arts`/`all_wars` so it's visible on every role page); (c) **set the label on open** (L902-905 branch) to `m_following ? "Stop Follow" : "Follow"`, and optionally refresh in `Update()`. Listener stays `FollowToggle`.
- This also resolves original G-07: the button becomes *intentionally* always-visible, not accidentally lingering.

**Scope boundaries (assumptions):**
- Main (config) panel = **welcomed** NPCs only: `Interact` opens the rescue panel for `!m_isHirdmandr` and the main panel otherwise (`HirdmandrNPC.cs:993-999`), so the Follow button is naturally welcomed-only. The rescue panel's "Follow Me, I'll take you to safety!" (`Rescue()`) is a separate lead-home action — unchanged.
- Follow is a "come with me" command: **transient** (ends on save/load/owner change/death), owner-authoritative. Multiplayer (following a non-local player) is the existing himthiki behaviour — verify in-game.

**DECISIONS (final — Thomas, 2026-10-14):**
- **D1 Combat while following:** a following NPC **defends itself** — it enters `threatened` (base `MonsterAI` alerted/attacked), leaving the follow hold-state while threatened, and **resumes following** when the threat clears. (Not pacified.)
- **D2 Persistence:** **transient** (no ZDO persistence); nothing saved. Ends naturally when the follow target vanishes (D5).
- **D3 Naming:** do the clean renames — `m_following`, state `follow` / `NodeFollow`, `FollowToggle`, `g_b_follow`. The himthiki **job** (`m_jobHimthiki` / `jobHimthiki` / `CombatJobHimthiki`) is separate and stays.
- **D4 Placement:** role-neutral **header**, visible on every role page (stays out of `all_arts`/`all_wars`). Final anchor: `(0.4,1)` pos `(0,-50)`, width `120` — i.e. left of the Artisan role radio (`0.6`), clear of the "Role:" header (`0.52`) and the name (`0.25`); the earlier `(0.9,1)` sat over the "Warrior" label and was moved (Thomas, 2026-10-14).
- **D5 Auto-stop (simplified, Thomas 2026-10-14):** because follow is transient + owner-driven, the base game handles logout/disconnect/out-of-range. The **only** stop condition we manage: *owner, while following, if `GetFollowTarget() == null` → `StopFollowing()`.* This single check covers player **death** (Valheim destroys the player's `Humanoid` GameObject on death and spawns a separate ragdoll/tombstone, so the target goes null and the NPC does **not** follow a "dead body") **and** logout/disconnect. No distance-based auto-stop added (the existing ~50f follow leash / ~87f sim render range handles that via the base game).

**Implementation (2026-10-14):**
1. `HirdmandrNPC.cs` — `m_himthikiFollowing` → `m_following` (L62); `HimthikiFollow()` → `FollowToggle()` + new `StartFollowing()`/`StopFollowing()` helpers (job gate removed; `StartFollowing` no-ops if `m_user == null`).
2. `HirdmandrAI.cs` — owner `Update` now (a) auto-stops when `m_following && GetFollowTarget() == null`; (b) drives state per-frame from `(m_following, alerted)`: following+alerted → `threatened`, following+!alerted → `follow`, else !following+alerted → `threatened` (original `curState != patrol` guard kept for the non-follow path). Re-requesting a state each frame is safe because `OldManSM.Evaluate()` gates `EnterFrom`/`ExitTo` behind `nextState != curState`.
3. `topSM.cs` — state `himthikiFollow` → `follow`, `NodehimthikiFollow` → `NodeFollow` (uses `m_following`); `NodeThreatened` routes to `follow` on de-alert when `m_following` else `schedule`.
4. `HirdmandrGUI.cs` — `g_war_b_himthikiFollow` → `g_b_follow`; header placement `(0.4,1)`/`(0,-50)` width `120` (left of the Artisan radio); label set on open from `m_following` (`"Stop Follow"` / `"Follow"`) via `GetComponentInChildren<Text>()`; listener → `FollowToggle`. Name/talk nipped down 5px (name `-45`→`-50`, talk `-115`→`-120`) so the name label's center aligns with the follow button's center.
- This also resolves original G-07: the button is now *intentionally* always-visible in the header, not accidentally lingering.

**Status:** IMPLEMENTED & build-verified (2026-10-14, MSBuild 0 errors). **Smoke test PASSED (Thomas, 2026-10-14):** followed NPC defends when alerted, resumes following on threat clear, and death auto-stop works (does not follow the ragdoll/tombstone).

### 4.4 Panel state tracked two ways + create-once asymmetry (G-09)
- **What:** Main panel (`HirdmandrGUI`) tracks open-state in a **field** `bool state` (L15), flipped at L900 (the `activeSelf`-derived version is commented out at L899). Rescue panel derives open-state from `activeSelf` (L162). Create-once also differs: main `SetActive(true)` on build (L134), rescue `SetActive(false)` (L75).
- **Why it's a fragility (no crash today):** Two panels, same pattern, two implementations. The field-based one can **desync** from `GUIHirdmandr.activeSelf` if anything *other* than `TogglePanel` changes the GameObject's active state (a future cleanup, re-parenting, or the leak path in G-11). Then `Update()` (gates on `activeSelf`) and `TogglePanel` (flips the field) disagree → double-toggle, or a panel that won't close. "Works until it doesn't," and hard to debug because the two sources of truth look identical.
- **Planned change:** unify both panels on the rescue approach — derive open-state from the panel's `activeSelf` (`bool state = !panel.activeSelf`), drop the field. One source of truth, matches the `Update()` gate, removes the desync class. (My default; flagged for your veto.)
- **Risk:** low-medium. Touches the toggle core; will re-read every `TogglePanel` caller (Interact, Update, action buttons) to confirm the derived-state path is equivalent. No behaviour change expected.
- **Status:** IMPLEMENTED 2026-10-14 — main `HirdmandrGUI` unified on the derived-state approach: removed the `bool state` field, create-once now `SetActive(false)`, and `TogglePanel` derives `bool state = !GUIHirdmandr.activeSelf;` (one source of truth, matches the `Update()` gate). Rescue panel already used this pattern, so it's untouched. No behaviour change.

### 4.5 Panel GameObject leak — no cleanup (G-11)
- **What:** Both panels are children of the global `GUIManager.CustomGUIFront` UI layer (not the NPC), cached in a component field, and **nothing destroys them** (no `OnDestroy` in any of the three files).
- **Why it's a defect:** On NPC death / despawn / unload the panel is not a child of the NPC, so it is **not** destroyed — it persists under `CustomGUIFront`. On the next interaction (or re-add) the field is null again → a **second** panel is created and stacked on the first. Over time: stacked duplicate panels, orphaned widgets, and each zombie panel keeps running its `Update()` close-key poll. A genuine leak with a visible symptom (ghost/duplicate panels) after enough NPC churn.
- **Decision (Thomas, 2026-10-14):** option **(b) — per-NPC `OnDestroy` teardown**, NOT destroy-on-close (a). Rationale: the recreate hitch was a **one-time** GUIManager/shader/asset warm-up (the 2nd NPC's panel doesn't hitch), and a singleton would have paid that same first-open cost anyway; per-NPC widget creation is cheap, so (a)'s recreate-per-open cost is the wrong trade. (a) rejected.
- **Implementation (2026-10-14):** `OnDestroy()` added to BOTH `HirdmandrGUI` and `HirdmandrGUIRescue` — destroys the cached panel (`GUIHirdmandr` / `GUIRescue`) when the NPC dies/despawns/unloads. If the panel was open it first restores the player via `InventoryGui.instance.Hide()` + `GUIManager.BlockInput(false)` (deliberately **not** `m_hirdmandrnpc.OpenInventory(false)`, which would call `m_container.Save()` + `m_visequip.UpdateEquipmentVisuals()` on the dying NPC).
- **Status:** IMPLEMENTED 2026-10-14 — MSBuild 0 errors. **Smoke test PASSED (Thomas, 2026-10-14):** open a panel → kill/despawn that NPC → no ghost/duplicate panel, and (if it was open) player control + inventory restored.

### 4.6 `m_user` deref without null-guard (rec #8)
- **What:** `RoleChanged` / `CombatJobThegen` / `CombatJobHimthiki` do `m_user.GetComponent<Player>()` (L944, L1007, L1026). `m_user` is set in `Interact` (L987).
- **Why it's a fragility:** If any future path opens the panel *without* `Interact` (e.g. a "reopen last panel" convenience or admin command), these handlers NRE. Today every path goes through `Interact`, so it's latent, not active.
- **Planned change:** guard `if (m_user == null) return;` (or `?.`) before the tutorial `.GetComponent<Player>()` calls.
- **Risk:** none.
- **Status:** IMPLEMENTED 2026-10-14 — used `?.` (not a top-of-method `return`, which would wrongly skip the role UI update in `RoleChanged`): `m_hirdmandrnpc.m_user?.GetComponent<Player>()` in all three (`RoleChanged` / `CombatJobThegen` / `CombatJobHimthiki`). A null `m_user` → null `thisPlayer` → the existing `thisPlayer == Player.m_localPlayer` check skips the tutorial (no NRE, no control-flow change). Built 0 errors, deployed.

### 4.7 Skill/name display fidelity (G-14 / rec #9)
- **What:** `g_name` is built once at create-time (L142-155) and never refreshed; skill values render as `(int)m_value` (L119, L1095), truncating fractional progress.
- **Why it's a defect (minor):** (a) if an NPC's name ever changes while the panel is cached, the panel shows the old name; (b) `3.7` displays as `3`, so sub-integer skill progress is invisible on a panel whose job is to show skills. Cosmetic but real.
- **Planned change:** refresh `g_name` in the open branch alongside `g_talk`; format skill values to show the fractional part when non-integer (e.g. `m_value.ToString("0.#")`).
- **Risk:** none.
- **Status:** PARTIAL / closed (2026-10-14). **(b) fractional skill display = INTENTIONAL design** (Thomas) — left as `(int)` in both the create-time skill string and the per-skill label; not a defect. **(a) name staleness = fixed as cheap insurance:** `g_name` is now refreshed in the open branch (`g_name.GetComponent<Text>().text = m_humanoid.m_name;`) so a *future* rename feature (not in MVP) can't strand a stale name; there is no live rename path today, so this is latent protection, no visible change. Built 0 errors, deployed.

### 4.8 RetRACTED / by-design — do NOT change
- **G-03 / G-10 (EquipBest + SaveSkills + OpenInventory on toggle):** INTENDED (4.0). No change.
- **G-12 (native `Interactable` hook, `Interact` returns `false`):** open path is the game's native interaction, not Harmony — correct and intended. One open sub-question: `Interact` returns `false` (L1002) even though it *acted*. In Valheim that usually means "not consumed as a default interaction"; it works in practice (panel opens). If we ever see the game also fire a default action on the same press, the fix is to return `true` when handled. **Low priority — verify in-game, no code now.**

### 4.9 Suggested order (for when Thomas says go)
1. **4.1 + 4.2 + 4.3** — the three one-line/one-array correctness fixes; independent, safe.
2. **4.4 then 4.5** — both touch the toggle core; do together, then re-test open/close/reopen + action buttons + NPC death.
3. **4.6 + 4.7** — guards + fidelity; small.

Each step followed by the MSBuild rebuild (AGENTS.md) + in-game smoke test.
