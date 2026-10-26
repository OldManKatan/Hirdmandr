# Hirdmandr Rescue System — Requirements, Research & Plan

Status: R2 DIAGNOSTICS DEPLOYED (2026-10-05). Read this before any rescue work.
**Spawn design (U1 confirmed, pivot to ledger + scan decided 2026-10-07): see `rescue_spawn_design.md` — read it FIRST after any compression/cold start.**
PMW: project `hirdmandr` — umbrella #26 + sub-items R2-R7 + DECISIONS D1-D9
(D1/D2/D4/D5/D6/D8 decided 2026-10-05; D3 gear + D9 waiting behaviour open).

## 1. What this feature is
Hirdmandr spawn in the world in a "needing rescue" state (rare, in any biome, ideally inside a
natural structure). The player finds one, rescues it (existing rescue panel), leads it back to a
base, and triggers the "welcome home" event, which associates the Hirdmandr with that base.
Skills + equipment are determined by the biome where it was found.

## 2. Requirements (Thomas, 2026-10-05)
1. Rare world spawn in a "needing rescue" state; player rescues, then must bring it back to a base
   for the "welcome home" event, associating the hirdmandr with that base location.
2. Must be able to spawn in every biome; ideally INSIDE structures (abandoned village in the plains,
   ruined tower in the black forest, etc.).
3. Biome of encounter determines skills + random equipment. Always wearing armor, with weapons and
   shield consistent with the biome's progression level (e.g. iron or root armor in the swamp).
   Gear lists per biome to be PROVIDED BY THOMAS (skeleton in §6).
4. Biome progression: Meadows, Black Forest, Swamp, Mountain, Plains, Mistlands, Ashlands, Frozen North (8 biomes).
5. Skills: 3 skills at first, 5 at Plains and higher, random selection "like the spawn command":
    - Meadows (10, 5, 5)
    - Black Forest (20, 10, 5)
    - Swamp (30, 15, 15)
    - Mountain (40, 20, 20)
    - Plains (50, 25, 25, 10, 10)
    - Mistlands (60, 30, 30, 10, 10)
    - Ashlands (70, 35, 35, 20, 20)
    - Frozen North (80, 40, 40, 20, 20)
6. Not-yet-welcomed (rescue script) hirdmandr must NOT follow players through portals.
   (Follow-through-portal for welcomed hirdmandr = future feature, out of scope.)
7. A following hirdmandr must attach itself to a ship (raft, carved ship, longship — any boat) when
   the player it follows attaches to the ship.
8. If a rescue-phase following hirdmandr gets too far from its player, teleport it to an open space
   within 10f of the player.

## 3. What already exists (do not rebuild)
- Rescue skeleton: `HirdmandrNPC.Rescue()` :1051 (MonsterAI `SetFollowTarget(m_user)`, faction
  Players, `m_jarlZOID` set, rescue effect, tutorial), `RescueWait()` :1075, `WelcomeHome()` :1086.
- Welcome gate: `CheckWelcomeRanges()` :1120 — Workbench + Fire both within 15f. (This IS the
  existing "base" definition; DECISION D5 whether to keep it.)
- Rescue GUI: `HirdmandrGUIRescue.cs` — wood panel, rescue text, buttons:
  "Follow Me, I'll take you to safety!" → Rescue, "Stay Here..." → RescueWait,
  "Welcome to your new home!" → WelcomeHome. Opens from `HirdmandrNPC.Interact` when not yet rescued.
- Follow = game `MonsterAI.SetFollowTarget` (proven in HimthikiFollow + Himthiki job).
- Spawn path: `/hirdmandr spawn` → `SpawnNearCaller` (HirdmandrCommand.cs:59): prefab `npc_entity`
  (registered in Hirdmandr.cs:383, cloned from "Goblin"), `ZDOMan.instance.CreateNewZDO(pos, hash)`,
  owner = local player's session owner, `scene.CreateObject(zdo)` + `AddInstance`.
- NPC skills today: `OwnerInit` gives 3 random skills from the full 10-skill pool at +10/+5/+5 —
  that is exactly the Meadows tier, so per-biome skills = generalize this (R4).
- Welcome effect + tutorial already exist (`m_welcomeEffect`, `hirdmandr_welcome_home`).
- No ship/portal/biome/teleport-initiation code exists yet in the mod (only NPCPlayerClone
  teleport fields from the decompiled Player).

## 4. Verified game API (this install — assembly dated 2026-09-25, exe 6000.0.75.2503836)
IMPORTANT: the installed game's API is NEWER than the old "1.0.16" notes. These are GONE:
`MapGenerator`, `SEShip`, `SEShipBoard`, `SEPortal`, `SEStructure`, `ZDOType`, `Visequip`,
`Visequip.AddNewItem`. Current surface (verified by Cecil, controls passed):

- Biome: `Heightmap.Biome` enum (nested in `Heightmap`): None, Meadows, Swamp, Mountain,
  BlackForest, Plains, AshLands, DeepNorth, Ocean, Mistlands, All, Land. Values (subagent,
  control-verified): None=0, Meadows=1, Swamp=2, Mountain=4, BlackForest=8, Plains=16,
  AshLands=32, DeepNorth=64, Ocean=256, Mistlands=512, All=895, Land=639. All 8 land biomes
  are IN SCOPE (Mistlands confirmed by Thomas 2026-10-05).
  - Lookup: `WorldGenerator.instance.GetBiome(Vector3)` (instance; also Vector2s + (wx,wy,level,bool)),
    static `Heightmap.FindBiome(Vector3)`, `Heightmap.BiomeToString(Biome)`,
    `WorldGenerator.GetBiomeArea(Vector3)`.
- Teleport: `Character.TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)` → bool,
  `Character.IsTeleporting()`, `RPC_TeleportTo(long, Vector3, Quaternion, bool)`.
  `Player.IsAttachedToShip()`, `Player.GetControlledShip()` → Ship, `Player.TeleportTo(...)`,
  `Player.IsTeleporting()`, `Player.m_teleporting`.
- Ship (MonoBehaviour, replaces SEShip): `List<Player> m_players`,
  `RefreshPlayerList()`, `IsPlayerInBoat(ZDOID|Player|long)`, `HasPlayerOnboard()`,
  `HaveControllingPlayer()`, `GetLocalShip()`. NO board-transform field → the deck/board position is
  a child GameObject at runtime (name TBD in R2 ship scan).
- Structures: `Piece` component (replaces SEStructure): `ZNetView m_nview`, `long m_creator`,
  `GetCreator()`, `IsPlacedByPlayer()`, `OnPlaced()`, `CanBeRemoved()`. Natural ruins are expected
  to be placed `Piece` ZDOs (creator 0) — CONFIRM IN R2 scan.
- PoI / locations (verified 2026-10-05): `ZoneSystem.instance` (static property).
  `m_locations` (List<ZoneLocation> — all location definitions), `m_locationInstances`
  (Dictionary<Vector2s, LocationInstance> — placed per zone), `ZoneSystem.GetZone(Vector3)`
  (STATIC) → zone key, `GetZonePos(Vector2s)` → Vector3, `GetLocation(String)`,
  `FindClosestLocation(String, Vector3, LocationInstance&)`, `FindLocations(String, List&)`.
  `ZoneSystem.ZoneLocation`: `m_name`, `m_prefabName`, `m_biome` (Heightmap.Biome),
  `m_quantity`, `m_enable`, `m_group`, `m_interiorRadius`, `m_exteriorRadius`, `m_unique`.
  `ZoneSystem.LocationInstance` (STRUCT): `m_location`, `m_position` (Vector3), `m_placed`.
  `ZoneSystem.SpawnMode`: Full, Client, Ghost.
  **R3 HOOK TARGET:** `ZoneSystem.PlaceLocations(Vector2s, Vector3, Transform, Heightmap,
  List<ZoneLocation>, SpawnMode, List)` — the game's PoI placement routine (log: "Placed
  location {0} in zone {1} duration {2} ms"); called when a player enters render range of a
  PoI (before that only a marker exists). Post-hook here to spawn rescues (see §5).
  - `SimulationDistance` is a class (not an enum) with static field `OriginalDistance`.
- Monster targeting / aggro (verified 2026-10-05, call-site scan): `BaseAI` (MonsterAI
  parent) — `FindEnemy()`, `FindClosestEnemy(Character, Vector3, Single)`,
  `FindRandomEnemy(Character, Vector3, Single)`, `HaveEnemyInRange(Character, Vector3, Single)`,
  `CanSeeTarget`/`CanHearTarget`/`CanSenseTarget(Character)`, and the chokepoint
  `Boolean IsEnemy(Character)` / `IsEnemy(Character, Character)` (checks GetGroup,
  GetFaction, IsTamed, IsAggravated) — ALL enemy-finding methods filter candidates through
  IsEnemy, so a prefix there makes a character untargetable by every monster AI.
  `MonsterAI.UpdateAI()` (per tick) → `UpdateTarget(Humanoid, float, bool&, bool&)`.
- Faction: `Character.m_faction` is the nested enum `Character.Faction`: Players,
  AnimalsVeg, ForestMonsters, Undead, Demon, MountainMonsters, SeaMonsters, PlainsMonsters,
  Boss, MistlandsMonsters, Dverger, PlayerSpawned, TrainingDummy, DeepNorth.
  Existing mod: constructor HirdmandrNPC.cs:99 sets Players for ALL hirdmandr;
  `Rescue()` HirdmandrNPC.cs:1071 re-sets Players (that is the D9 "becomes targetable" moment).
- Damage entry (D9 invulnerability hook): network `Character.RPC_Damage(long, HitData)` →
  `Character.Damage(HitData)` → `ApplyDamage(HitData, bool, bool, DamageModifier)` →
  `OnDamaged(HitData)` (Humanoid overrides OnDamaged). Prefix `Character.Damage` to block.
- ZDO queries: `ZDOMan.instance` — `CreateNewZDO(Vector3, int prefabHash)`, `GetZDO(ZDOID)`,
  `FindSectorObjects(Vector2s, SimulationDistance, List<ZDO>, List<ZDO>)`,
  `FindObjects(Vector2s, List<ZDO>, HashSet<SectorIndex>)`,
  `GetAllZDOsWithPrefabIterative(string prefab, List<ZDO>, ref int)`, `NrOfObjects()`.
  Sectors/areas are now `ZoneSystem` (SectorIndex). No `ZDOType` enum.
- Equipment: `VisEquipment` (per-slot, hash-based; name→hash via
  `StringExtensionMethods.GetStableHashCode(string)`):
  `SetChestItem(int itemHash)`, `SetLegItem(int)`, `SetHelmetItem(int)`,
  `SetShoulderItem(int hash, int variant, int quality)`, `SetLeftItem(int hash, int variant, int quality)`
  (left hand = shield), `SetRightItem(int hash, int quality)` (right hand = weapon),
  `SetLeftBackItem/SetRightBackItem(int, int, int)`, `SetUtilityItem(int)`, `SetTrinketItem(string)`,
  `AttachArmor(int, int, int)` → List, `AttachItem(int, int, Transform, bool, bool, int)`,
  `UpdateVisuals()`. Item persistence = container inventory (`m_container.m_inventory.AddItem`,
  existing pattern HirdmandrNPC.cs:740-755).
- Movement: Character is Rigidbody-velocity based (set_linearVelocity in UpdateWalking),
  run/walk via SetRun/SetWalk. Teleport via TeleportTo is the game's own mechanism — use it for
  the leash, not raw transform sets (raw sets fight the physics).

## 5. Spawn design — PoI hook (decided 2026-10-05, D1/D2/D8)
Valheim's PoI (point-of-interest) mechanic: when a player enters render range of a PoI, the
game PLACES the structure in the world (log: "Placed location X in zone Y duration Z ms");
before that only a marker exists. Rescues ride the same mechanism — no scheduler, no search.
Design (decided):
- Post-hook `ZoneSystem.PlaceLocations` (§4). When a location is placed:
  - if `ZoneLocation.m_name` is on the WHITELIST (built from R2 scan output; Thomas picks)
    AND a 20% roll hits → spawn one rescue hirdmandr.
  - biome / skills / gear = the placed location's `ZoneLocation.m_biome` (D2 — no selection).
  - spawn point: non-obstructed spot INSIDE the structure (within m_interiorRadius; height/
    raycast check; not in water).
  - NO scheduler, NO max-1, NO despawn, NO cap: multiple concurrent rescue targets are OK;
    unrescued/unfound NPCs persist until discovered (D8).
  - Dungeons EXCLUDED: crypt / DG_* names never make the whitelist (Thomas: rescues do not
    spawn inside dungeons).
- One roll per location placement = the roll granularity (one location per zone).
- Test command: `/hirdmandr rescue spawn [biome]` (manual, near player) for fast iteration.
Expected natural structures per biome (wiki hints — R2 scan output is the authority; the
whitelist is built from the scan):
- Meadows: small abandoned village
- Black Forest: ruined tower
- Swamp: abandoned settlement / ruins (± old shipwreck)
- Mountain: old shipwreck (beached longship)
- Plains: abandoned village (larger, longship)
- Mistlands: mistlands ruin (R2 scan)
- Ashlands: abandoned longship / ashlands ruins
- Frozen North: abandoned longship (frozen shore)

## 6. Gear data (THOMAS TO FILL IN — R4)
Format: per biome, armor (chest + legs [+ helmet/shoulder]), weapon, shield. Item names must be
Valheim prefab names (resolved via `StringExtensionMethods.GetStableHashCode`).
- Meadows:      armor: ____ weapon: ____ shield: ____
- Black Forest: armor: ____ weapon: ____ shield: ____
- Swamp:        armor: ____ weapon: ____ shield: ____ (Thomas example: iron or root armor)
- Mountain:     armor: ____ weapon: ____ shield: ____
- Plains:       armor: ____ weapon: ____ shield: ____
- Mistlands:    armor: ____ weapon: ____ shield: ____
- Ashlands:     armor: ____ weapon: ____ shield: ____
- Frozen North: armor: ____ weapon: ____ shield: ____
Until the list lands, placeholder = Meadows tier (root armor/weapon/shield) so R3 can ship.

## 7. Implementation plan (serial; each milestone = one build + one test)
- R1 (DONE this turn): requirements, research, plan, PMW, this doc.
- R2 In-game API verification (diagnostics only, no behavior change) — CODE DONE 2026-10-05,
  built + deployed; awaiting Thomas's in-game run:
  - `/hirdmandr rescue scan` — placed PoI locations in the 5x5 zone area around the player:
    name, prefab, biome, zone, position, interior/exterior radii (ZoneSystem, not Piece).
  - `/hirdmandr rescue catalog` — full location-definition catalog from `m_locations`
    (every PoI type; the source for the whitelist).
  - `/hirdmandr rescue ship` — nearest ship's child transform tree (find the deck/board node),
    player ship/teleport state, `Character` rigidbody isKinematic.
  - `/hirdmandr rescue close` — the single nearest PLACED PoI in the whole world (by horizontal
    distance) with its in-game name/prefab/biome/dir/pos (added 2026-10-06; identifies the
    structure you are standing next to — complements the 5x5 `scan`).
  - `/hirdmandr rescue find <PoI Name>` — compass direction (N/NE/E/SE/S/SW/W/NW) + horizontal
    distance from the player to a named PoI (added 2026-10-06). Matches m_name or m_prefabName
    case-insensitively (exact then substring); placed instances first, else catalog-only note.
    Example output: `WoodHouse4 is 94 units W`.
  Acceptance: Thomas runs scan + catalog + close/find in ≥3 biomes → whitelist + structure-per-biome
  table; deck child identified; kinematic answer recorded here.
- R3 World spawn via PoI hook (design finalized 2026-10-05; no scheduler):
  - Harmony post-hook on `ZoneSystem.PlaceLocations` (§4): whitelist by `m_name` + 20% roll
    → spawn at a non-obstructed point inside the placed structure.
  - Spawn reuses SpawnNearCaller path; NPC starts unrescued, passive (idle at spot), no jobs.
    Biome = the location's `m_biome`.
  - ZDO keys added at spawn: `hmnpc_biome` (int), rescue state, `hmnpc_jarlZOID` = 0 until rescue.
    Persists across world reload (no despawn, no cap; multiple concurrent OK — D8).
  - Unrescued state (D9): INVULNERABLE (prefix `Character.Damage`) + NOT a mob target
    (prefix `BaseAI.IsEnemy` → false). Both keyed on m_isRescued; existing Rescue() lifts
    them (faction Players + follow target). No aggro = the IsEnemy filter, no new AI code.
  - Test command: `/hirdmandr rescue spawn [biome]` (manual, near player) for fast iteration.
   Acceptance: entering render range of a whitelisted PoI → ~20% chance of a rescue inside the
   structure; survives world reload unrescued; `/hirdmandr rescue list` shows active rescues.
   STATUS 2026-10-06: R3a (D9) built+deployed (Character.Damage + BaseAI.IsEnemy prefixes; PMW #22).
   R3b (whitelist) DONE: D:\OpenCode\hirdmandr_project\valid_hirdmandr_spawn_prefabs.md = 67
   structures / 7 biomes (Meadows/BlackForest/Swamp/Mountain/Plains/Mistlands/Ashlands); GAP flagged
   to Thomas: NO Frozen North (DeepNorth) entries. Confirmed hook target (Cecil):
   ZoneSystem.PlaceLocations(Vector2s, Vector3, Transform, Heightmap, List<ClearArea>, SpawnMode,
   List<GameObject>) -> void; ZoneSystem.instance.m_locationInstances[zoneID] ->
   LocationInstance{m_location, m_position, m_placed}; ZoneLocation{m_name, m_prefabName, m_biome,
    m_interiorRadius, m_exteriorRadius}.
    STATUS 2026-10-06 (R3c BUILT + DEPLOYED, 0 errors): RescueWhitelist.cs (67-name string[]
    whitelist, Contains(ZoneSystem.ZoneLocation) on m_name/m_prefabName) + RescueSpawn.cs
    (RescueSpawner.PlaceAtLocation/PickClearSpot down-raycast inside interiorRadius + RescueSpawnHook
    Harmony Postfix on PlaceLocations gated SpawnMode.Full, 20% roll, one-time non-Full log, try/catch
    swallow) + HirdmandrCommand.cs (`/hirdmandr rescue spawn` = force-place nearest placed
    whitelisted PoI, `/hirdmandr rescue list` = flag unrescued). NOTE: LocationInstance is a STRUCT
    (no ==null; m_location is the nullable class). TEMP diagnostic added 2026-10-06: ROLL MISSED log
    (name + %) on every Full-placement miss, to count real rolls while traversing.
    OBSERVATIONS 2026-10-06 (Thomas first playtest, PARKED for post-test investigation):
    (1) Unrescued NPC was NOT sitting when first encountered (expected unrescued idle/sitting
    state per OwnerInit) - verify why the sitting pose isn't applied on spawn.
    (2) Saw 'StoneTowerRuins07 placed in mode Ghost (expected Full)' (one-time non-Full log) but
    never a Full-placement/roll log while standing at the structure. If ROLL MISSED also never
    appears at a whitelisted PoI while standing there, the mode != Full gate is wrong for
    singleplayer -> switch gate (roll on Ghost / drop mode gate / key on m_placed transition).
    (3) Natural 1-NPC spawn + full rescue + artisan job all worked up to job assignment.
    REMINDER (Thomas): poke the D9 in-game test AFTER the R3 sprint (not during R3c build/test).
- R4 Per-biome skills + gear (APPROACH DECIDED 2026-10-06: Option B, position-based biome).
  Biome source: `Heightmap.FindBiome(transform.position)` at the first-time block
  (`hmnpc_name == ""`, HirdmandrNPC.cs:186) — NOT a ZDO key (the D2-strict `hmnpc_biome` idea
  is dropped; position already gives the right biome and covers /hirdmandr spawn + R3).
  Work SEQUENTIALLY (Thomas): verify item names in-world BEFORE coding gear (lists from
  wiki/in-game, may have typos).
  Steps:
    1. [data + verify] `BiomeData.cs`: 8 biome tables (weapon1h/2h/ranged, ammo, shield, head,
       chest, legs lists + skill float[]), verbatim from §6 / PMW #29 (typos kept so the check
       catches them). + `/hirdmandr rescue verifygear`: per item name, ObjectDB exists? +
       ItemType matches the slot it's listed under. -> Thomas runs in-world, reports problems.
    2. [fix data] apply step-1 corrections to BiomeData.cs.
    3. [skills] OwnerInit skill loop (HirdmandrNPC.cs:205-212): replace hardcoded [10,5,5] with
       `BiomeData.ByBiome(biome).skills`. Loop shape already matches Thomas's pseudocode.
    4. [gear] `RandomizeAppearance` (595-765): replace hardcoded gear with biome-list picks per
       §6 rules (weapon 1h/2h/ranged; 50% shield for 1h; 65% armor per slot; "None"=empty;
       duplicate=weight; 20-40 ammo for ranged). Still `m_container.m_inventory.AddItem(ObjectDB
       ...GetItemPrefab(name).m_itemData)` -> `EquipBest()` equips (EquipItem). All quality 1.
       (EquipBest ALREADY suppresses shield for TwoHandedWeapon/Bow, line 1360-1364 — matches §6.)
    5. [wire] pass `biome` (from FindBiome) into RandomizeAppearance + skill loop.
    6. [build+test] build+deploy; Thomas: `/hirdmandr rescue spawn` in several biomes -> correct
       skill levels + biome gear on body/inventory; persists across reload.
  Acceptance: `/hirdmandr rescue spawn` per biome -> correct skill levels + biome gear (quality 1)
  on body and in inventory; persists across reload.
   STATUS 2026-10-06: R4 COMPLETE. steps 1-5 built+deployed (see PMW note #21); step 6 ACHIEVED -
   Thomas tested /hirdmandr spawn in Meadows/BlackForest/Swamp/Mountain/Plains: logs + gear +
   scattering/incomplete loadouts all good; spawn offset (+2 to caller right, works with
   obstructions) approved and KEPT for testing. (Ashlands/Mistlands not visually tested but their
   names passed verifygear = 0 problems.)
- R5 Follow hardening (rescue phase only, in `HirdmandrAI.Update` while following):
   - Leash: distance > leash (D6, 50f) → find open space within 10f of player (ring sampling 3-8f,
     raycast down, on land, clear of non-water colliders) → `m_humanoid.TeleportTo(pos, rot, false)`.
  - Portal guard: player `IsTeleporting()` or position jump > 50f between ticks → STOP following,
    go to wait (RescueWait semantics), stay in place. No teleport across the jump. (D4)
  - Ship board: player `IsAttachedToShip()` → each tick stop NPC locomotion and place NPC at
    player pos + small deck offset (project onto deck by short raycast); on disembark → resume
    follow. (v1 "glue" approach — safe regardless of rigidbody mode; transform-parenting only if
    R2 shows a cleaner path and Thomas wants a fixed deck spot.)
   Acceptance: Thomas checklist — run away → NPC hops to him; portal away → NPC stays and waits;
   board raft/longship → NPC rides along; disembark → resumes following.
   STATUS 2026-10-08: R5 COMPLETE (ship + leash, tested by Thomas). Ship: non-parented deck-friction
   carry (FindOnDeckSpot ship-prefab-centered, one-shot board via m_aboardShip, in-water safety net).
   Leash: 50f threshold (D6), FindOpenSpotNear ring 3-8f / 16 bearings, down-raycast, height <10f,
   clear 0.7m sphere that IGNORES the WaterVolume collider (a spot on the terrain beside water is
   fine - the NPC stands on land or swims if shallow). Both verified: a boxed/stranded NPC teleports
    out to ~5m; NPC rides raft/longship; disembark resumes follow.
    STATUS 2026-10-09: Portal guard UPGRADED (was reactive-only; now proactive + reactive, split by scope). Portal API (1.0.16, SEPortal gone): TeleportWorld (public m_activationRange, m_exitDistance) + TeleportWorldTrigger. RESCUE NPC (m_isRescued && !m_isHirdmandr): proactive (NPC within PortalGuardRadius = max(7, activationRange + exitDistance) of the nearest TeleportWorld) OR reactive (player IsTeleporting / >50f jump) -> PortalStallWait (clone of RescueWait: stop + rescue-wait state so the rescue GUI re-starts the follow; + StopMoving + cleared last-player baseline). WELCOMED NPC (m_isHirdmandr): future feature - TeleportWelcomedNPCWithPlayer scaffold (no-op) keeps the safe default (stop at portal). FindNearestPortal mirrors FindShipPlayerIn. Boat + leash unchanged (rescue-OR-welcomed). Built 0 errors, deployed 10/9 7:58 AM.
    STATUS 2026-10-09 (portal-guard refinements, 9:50 AM): (1) no GUI on stall - PortalStallWait no longer calls TogglePanel (the panel must not open on a remote stall); the player re-opens the rescue GUI by interacting with the NPC. (2) speech above the NPC's head via Say (SetNpcText): 'I cannot use portals without Oden's blessing', then 3s later (Invoke PortalWaitText) 'I will wait here.'; cancelled on re-Rescue. (3) guard radius floor 15f -> 7f. Built 0 errors, deployed 9:50 AM.
    STATUS 2026-10-09 (portal-guard refinements 2, 10:22 AM): (1) stall no longer stops in place - PortalStallWait now sets the patrol point to ~16m in front of the portal (BaseAI.SetPatrolPoint(Vector3)) so the NPC WALKS there (pathfinding, walks around walls) on the side a returning player emerges from; this also keeps it outside the guard radius so a re-Rescue does not immediately re-trigger. (2) guard radius floor 7f -> 9f. Built 0 errors, deployed 10:22 AM.
    STATUS 2026-10-09 (portal-guard refinements 3, 11:47 AM): text readability - the two portal lines were only visible ~1s. Root cause (confirmed in UpdateNpcTexts IL): the bubble is SetVisible(false) when camera-NPC distance > cullDistance, and the NPC now walks ~16m to its patrol spot, crossing the old 10m cull almost instantly. Say() gained a cullDistance param (default 10f, so Socialize's 'I am Idling!' is unchanged); the portal lines pass 30f so the text stays for the full 5s TTL. Note: each line TTL is 5s (not 3s); the two lines are 3s apart (Invoke PortalWaitText 3f), and line 2 replaces line 1 at t=3s. Built 0 errors, deployed 11:47 AM (confirmed good enough by Thomas, 10-09).
    STATUS 2026-10-09 (WORLD SPAWN LINE COMPLETE): #26 'Rescue system: world spawn -> follow -> welcome home' closed - all subtasks done (R1-R7 + #26i); portal guard text readability confirmed good enough. Soul item (#26h) relocated out of #26 scope to #42 (parked; part of the future resurrection system, not yet designed). Full loop verified by Thomas: world spawn -> rescue -> follow -> welcome home.
- R6 Welcome home association + event:
  - Keep gate per D5 (CheckWelcomeRanges: $piece_workbench + Fireplace within 15f); on success:
     base association = `hmnpc_jarlZOID` (set to the welcoming player: Rescue for the never-rescued case, WelcomeHome for the rescued-but-not-yet-hirdmandr case) + the NPC's own ZDO position persisting at
    the base (NO separate `hmnpc_homeBase` key — the position IS the base), ensure NPC reloads AT
    the base, welcome effect/tutorial (exist), then normal role assignment via existing GUI.
  Acceptance: full loop — world spawn → rescue → lead to base → welcome → quit → rejoin → NPC at
  base with jarl/role/skills/gear intact, running its daily routine.
  STATUS 2026-10-08: R6 COMPLETE (code-verified + Thomas GUI test). WelcomeHome (HirdmandrNPC.cs:1466)
  sets m_isHirdmandr (persisted), StartAI (daily routine), welcome effect + tutorial, and marks the
  structure welcomed (RescueLedger.SetWelcomed). Load path re-StartAI for a welcomed NPC
   (HirdmandrNPC.cs:248-256) so it reloads at the base running its routine. Role assignment = the
   pre-existing job GUI (not R6 scope).
   STATUS 2026-10-09: jarl-at-welcome fix (note #22) - WelcomeHome now assigns m_jarlZOID to the
   welcoming player (m_user) in the rescued-but-not-yet-hirdmandr branch (HirdmandrNPC.cs:1477-1485),
   so the Jarl is whoever clicks Welcome Home, not just the original rescuer. Built 0 errors, deployed.
- R7 Playtest round with Thomas (rapid mode): fix what breaks, tune leash/radius values.

## 8. DECISIONS (PMW — status 2026-10-05)
- D1 Spawn rarity/schedule — DECIDED: no scheduler, no max-1 (Valheim devs proved it does not
  work). Spawn happens when a whitelisted PoI location is placed (ZoneSystem.PlaceLocations
  hook) and a 20% roll hits. One roll per location per zone.
- D2 Biome selection — DECIDED: no selection. The biome of the structure where the NPC spawns
  determines its skills/gear (ZoneLocation.m_biome).
- D3 Gear lists per biome — DECIDED 2026-10-06: full per-biome gear + skill data captured in
  PMW #29 this turn (§6 skeleton). Applied in R4; verify item names in-world via
  `/hirdmandr rescue verifygear` before coding the gear picks.
- D4 Player portal-leaves during rescue — DECIDED: portal away OR death → auto "wait here"
  (RescueWait). NPC stays where the player last knew it was.
- D5 Welcome-home "base" definition — DECIDED: keep existing Workbench + Fire within 15f
  (CheckWelcomeRanges).
- D6 Leash distance ("too far" threshold) — DECIDED 2026-10-08: 50f. (Was 100f "re-test later".)
  In-game a following NPC stops being simulated at ~87-90m (it "sleeps"/desyncs and the owner
  Update stops ticking), so d>100 was unreachable and the leash could never fire. 50f fires well
  before the cutoff, so a boxed/stranded NPC is pulled back while still awake. Leash ONLY active
  while following (so the D4 wait always fires before any leash teleport). Teleport target radius
  fixed 10f (spec); the spot-finder ignores the WaterVolume collider.
- D8 MP concurrent rescue targets — DECIDED: multiple concurrent rescue targets are OK;
  unrescued/unfound NPCs persist (no despawn, no cap). Rescues do not spawn inside dungeons
  (crypt/DG_* excluded from the whitelist).
- D9 Waiting (unrescued) hirdmandr behaviour — DECIDED (2026-10-05): unrescued =
  INVULNERABLE + NOT a valid mob target (mobs do not aggro). When the player starts the
  rescue: loses invulnerability, becomes targetable by monsters, player-team mob (existing
  Rescue() already sets faction Players + follow target). Design intent: the player protects
  the rescue on the way home. Implementation (API-verified): prefix `Character.Damage(HitData)`
  (block if unrescued) + prefix `BaseAI.IsEnemy(Character)` (false if candidate unrescued —
  chokepoint verified: FindEnemy/FindClosestEnemy/FindRandomEnemy all filter via IsEnemy);
  both keyed on m_isRescued so Rescue() lifts them automatically.

## 9. Gotchas / reminders (for future Qwen)
- The game assembly is the 2026-09-25 build. Do NOT code against MapGenerator/SEShip/SEPortal/
  ZDOType/Visequip-AddNewItem — they don't exist. §4 is the current surface.
- Cecil quirk in this bundled build: enum field `ConstantValue` reads null (use values verified in
  §4 or print `Heightmap.BiomeToString` in-game). `OpCode`/`Code` enum gotchas as in AGENTS.md.
- Use `Character.TeleportTo` for NPC teleports (game-native, handles animation/physics), never raw
  transform sets.
- Follow is MonsterAI (game), not custom pathing — leash/portal/ship logic wraps it, doesn't replace it.
- `OwnerInit` only runs for the session owner; world-spawned NPC must be created with an owner
  (same as SpawnNearCaller) or init will retry-loop 30x and give up.
- Skill float persistence = direct ZDO.Set (ModifySkill); bool persistence = SetSkillEnable direct
  write (RPC path is dead in SP) — don't regress either.
- Everything in the Hirdmandr ZDO uses `hmnpc_` key prefix.
