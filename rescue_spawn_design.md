# Hirdmandr — NPC Spawn System: Design & Decisions

> **Purpose of this doc:** Durable design memory for the rescue-NPC *spawn* system.
> Read this FIRST after any context compression or cold start — it exists so you do NOT
> re-derive decisions or re-read the same files in a loop.
>
> **Status (2026-10-26):** **Fix A (MP 0-rescues) IMPLEMENTED + BUILT + DEPLOYED, MP VERIFIED (Thomas,
> 2026-10-26: "MP worked very well"); SP regression found + FIXED (same day, deployed); SP retest pending (Thomas).**
> The MP scan was dead: the pass required a *local player* (gate + scan center); the headless MP
> server has none, and clients never scan (`IsServer()` false) — so in MP nobody ever scanned and
> rescues never appeared (SP worked: the host *is* the player). Fix A: the pass is **server-only
> and player-centered** — `RunPass` centers on each scan center, `Ready()` drops the local-player
> gate, and `PlaceAtLocation` drops the local-player requirement (identity rotation; `CreateNewZDO`
> default owner, guarded player-owner copy). Scan centers = connected peers ∪ the local player
> (`GetScanCenters()`, 10m dedup): **SP regression found (2026-10-26)** — after Fix A, rescues no
> longer spawned in SP, because `GetPeers()` does NOT provide the host's position in SP (the
> "exactly one peer" assumption was wrong), so the peer loop scanned nothing; the local-player
> center fixes SP (dedicated MP unchanged: no local player there). Build **280,576 B, 0 errors /
> 27 warnings**, deployed byte-identical (SHA256 verified).
>
> **Status (2026-10-08):** Design DECIDED (Thomas), **IMPLEMENTED + BUILT + DEPLOYED (2026-10-07)**,
> **R7 in-game playtest PASSED (2026-10-07)**, and the last spawn bug — the unrescued **sitting pose
> (#26i) — is RESOLVED (2026-10-08)**. Current build **190,976 B, 0 errors**. The refactor (remove
> the dead hook, add `RescueLedger` + `RescueScanner` + welcome/death wiring) is in the tree; the old
> `RescueSpawnHook` + `RescueChance` are gone. The ledger rides the **world's global key space**
> (`welcomed_<H>` global keys) and **derives** the live set from loaded ZDOs each pass (no dedicated
> ZDO, no stored ZDOID, no sweep). Remaining close-out: PMW state corrections + a few OPEN calls
> (see §18.1).
> Line numbers: `RescueScanner.cs` / `RescueSpawn.cs` are from the Fix A rewrite (2026-10-26, incl. the SP-fix `GetScanCenters`).
> All other line numbers are from the files as read on 2026-10-07 — **approximate after the
> "Massive refactor for 0.9" (7f8b741) tree** (e.g. `HirdmandrNPC.OwnerInit` is now ~:267,
> `LoadZDO` ~:1141, `hmnpc_name` write ~:1542).
>
> **Read order:** §1 intent → §2 the pivot → §16 the U1 test (enabling evidence) →
> components §3–§14 → §18 open questions → §19 code index → §20 build checklist.

---

## 1. Product intent — what a "rescue" is

(from `rescue_requirements.md` §1, Thomas 2026-10-05)

A Hirdmandr spawns in the world in a "needs rescue" state — rare, any biome, ideally inside a
natural structure. The player finds one, rescues it (existing rescue panel), leads it back to a
base, triggers "welcome home," and the Hirdmandr is associated with that base. Skills + gear are
set by the biome where it was found.

**Core loop:** find → rescue → lead home → welcome → live at base. Rescued NPCs who die do not drop souls and rescue can be re-attempted. Welcomed NPCs who die drop souls that can be used to ressurrect the NPC and get them back.

**Why this matters for spawn design:** the spawn is the *source* of the loop. A rescue is a
one-shot "a person needs saving" that sits at a structure until it is either welcomed (→ soul)
or dies unrescued (→ re-offered, per this session's decision). The spawn system's whole job is to
*offer* these rescues at the right places, at the right rate — with no scheduler and no global
search.

---

## 2. THE PIVOT — from the PoI hook to ledger + scan (the core decision)

**Current design (IMPLEMENTED 2026-10-07; Fix A 2026-10-26):** A *periodic* (5s) scan of the 3×3
zone grid around **each connected peer**, **on the server only** (`ZNet.instance.IsServer()`).
(The original wording — "around the player, on the authoritative client only" — was the MP bug:
the headless MP server has no local player, so it never scanned, and clients never scan
(`IsServer()` false) → in MP nobody scanned. Fix A made it peer-centered; see §7.) For each
placed+whitelisted PoI in range, a **deterministic 20% hash** decides whether that structure
"has a rescue." If it does and the structure is **not claimed** (`RescueLedger.Claimed(H)`
false), spawn one. The only durable data is one **global key** per structure —
`hmnpc_rescue_welcomed_<H>` (set at welcome, never cleared) — in the world's **global key space**
(the same store holding boss-defeat flags). "Live" is not stored: each pass **derives** the live
set by reading `hmnpc_rescuepoiHash` off every currently-loaded ZDO (`CollectLive`).
`Claimed(H) = (H is live now) || welcomed_<H>`. No dedicated ZDO, no stored ZDOID, no sweep.

**Why the current solution was chosen (the pivot):**
- The old mechanism (the `PlaceLocations` post-hook) is **dead in singleplayer**. Thomas's playtest
  observation (2): Some PoIs were placed in `Ghost` mode and never `Full`, so the `mode != Full` gate
  never fired the roll. Other PoIs were placed at world generation and never trigger PlaceLocations. 
  Fixing the mode gate would still not be a band-aid; the deeper issue is that a *one-shot hook* 
  cannot evaluate structures that don't call the hooked method.
- A **periodic scan** is self-healing: it re-checks every few seconds, so a failed spawn retries,
  and it naturally expresses "offer a rescue whenever the structure is in range and eligible."
- **The ledger is a per-pass *derive*, not a stored table.** Each pass reads
  `hmnpc_rescuepoiHash` off every loaded ZDO (`CollectLive`) and unions it with the `welcomed_<H>`
  global keys. No separate ZDO to own/create/re-find, no ZDOID to keep in sync with the world —
  the live set is consistent with the world *by construction* (it IS the world, re-read). The cost
  is O(loaded ZDOs) per 5s pass, cheap; that buys away every stored-state failure mode.
- **U1 (tested = YES, see §16)** still matters: a far/unloaded rescue NPC still sits in `ZDOMan`
  (`m_objectsByID`), so `CollectLive` still counts its structure as live — returning to a
  structure where a rescue is still alive keeps it claimed (no double-spawn). A *dead* rescue's ZDO
  is destroyed, so it drops out of `CollectLive` on its own — that IS the safety net (no sweep).

**Not chosen (and why):**
- **Keep the `PlaceLocations` post-hook** — dead in SP (Ghost vs Full); one-shot (can't re-eval / self-heal).
- **Fix the hook's mode gate (roll on Ghost too)** — band-aid; still one-shot, still no self-heal,
  and the ghost→real edge case risks double-rolls.
- **Random 20% roll per placement (original D1)** — re-rolled every scan, so a periodic scan would
  give inconsistent answers; needs a "rolled" flag per structure. Rejected in favor of deterministic.
- **A stored-ZDOID ledger + sweep** (the pre-refactor design, §6 history) — needed a dedicated
  owned ZDO, a re-find-on-reload mechanism, and a sweep pass to clear dead ZDOIDs. Replaced by the
  derive-each-pass model: the derive *does* iterate loaded ZDOs each pass, but that O(N) is cheap
  at a 5s cadence and removes all the stored-state bookkeeping.
- **Scheduler / max-N / despawn / cap** — explicitly rejected (D1/D8): no scheduler, multiple
  concurrent rescues OK, no despawn, no cap.

**Code references (implemented 2026-10-07):**
- `RescueLedger.cs` — the ledger: `HashKey` (`hmnpc_rescuepoiHash`), `IsWelcomed`/`SetWelcomed`
  (global keys), `CollectLive(HashSet<int>)` (derive live from loaded ZDOs), `Claimed(h, live)`.
- `RescueScanner.cs` — the 5s pass: `Start(MonoBehaviour host)` (called from `Hirdmandr.cs`
  `Awake`), `RunPass`, `Ready` (authoritative-client gate), `HashFor`, `Hits`.
- `RescueSpawn.cs` `RescueSpawner.PlaceAtLocation(inst, reason, rescuepoiHash)` (stamps
  `hmnpc_rescuepoiHash` on the new ZDO), `PickClearSpot`. The old `RescueSpawnHook` + `RescueChance`
  are **removed**.
- Global-key API (the ledger's store): `ZoneSystem.instance.SetGlobalKey`/`GetGlobalKey` (string
  overloads) — see §15.
- Enabling test: §16 (U1) via `HirdmandrCommand.cs` `ZdoCensus()`.

**Questions (mine, even if answered):**
- Q: Is a 5s cadence the right feel? → **MY DEFAULT (tunable)**; not a Thomas decision.
- Q: Does a periodic scan change *when* a rescue appears (up to ~5s after the structure is in range)?
  → Accepted; the structure itself takes time to place too. **Accepted implicitly.**
- Q: (the pivot itself) → confirmed by Thomas choosing deterministic + re-offer, which only make
  sense with the scan model. **DECIDED.**

---

## 3. How Valheim places structures (PoI mechanics)

**Current understanding:** The game places a PoI (point-of-interest) structure when a player enters
render range. `ZoneSystem.instance.m_locationInstances` is a `Dictionary<Vector2s, LocationInstance>`
keyed by zone. Each `LocationInstance` (a **struct**) holds `m_location` (a `ZoneLocation`,
nullable class), `m_position` (Vector3), `m_placed` (bool). The placement routine is
`ZoneSystem.PlaceLocations(...)` (log: "Placed location X in zone Y"). `ZoneSystem.GetZone(Vector3)`
(static) returns the zone key (Vector2s); `GetZonePos` / the instance's `m_position` give the world
position. This does not apply to all PoIs, many PoIs are placed either at world generation, or through
an unknown mechaism that is not `ZoneSystem.PlaceLocations(...)`

**Why it matters:** The scan reads `m_locationInstances` to find placed+whitelisted PoIs in the
zones around the player — the same data the old hook read, but on our own timer instead of on the
placement call.

**Not chosen:** Keying off `Piece` components or a global `FindObjectsOfType` — the PoI/zone data is
the authoritative, cheap source for "what structures are placed where."

**Code references:**
- `rescue_requirements.md` §4 (ZoneSystem API), §5 (old hook design).
- `RescueSpawn.cs` `RescueSpawnHook.Postfix` :128–149 (how the old hook read
  `m_locationInstances[zoneID]`, checked `m_placed` + whitelist).
- `HirdmandrCommand.cs` `ScanLocations()` (the existing `rescue scan` 5×5 dump) — reuse its
  zone-grid PoI discovery for the scanner.

**Questions (mine, even if answered):**
- Q: Is `m_location` a nullable class and `LocationInstance` a struct (so no `==null` on the
  instance itself, only on `m_location`)? → Confirmed by the R3c build compiling against it; the
  code uses `inst.m_location == null` (class) and `!inst.m_placed` (struct field). **Type facts from
  a successful compile; scanner path not yet runtime-verified.**
- Q: Which zone radius does the game place PoIs within (to size the scan grid)? → The existing
  `rescue scan` uses 5×5; the scanner draft said 3×3. **OPEN (grid size, §7).**

---

## 4. The whitelist

**Current design:** `RescueWhitelist` — a 67-name string list; `Contains(ZoneLocation)` matches
`m_name` OR `m_prefabName`. Only whitelisted structures can host a rescue.

**Why chosen:** Thomas picked the whitelist from the R2 `rescue catalog` scan output (real, placed
structure names). Dungeons (crypt / DG_*) are excluded (Thomas: no rescues in dungeons).

**Source of truth:** `D:\OpenCode\hirdmandr_project\valid_hirdmandr_spawn_prefabs.md`
(67 structures / 7 biomes).

**Not chosen:** Biome-based spawning (any structure in a biome) — rejected; Thomas wants specific
structure types per biome.

**Code references:** `RescueWhitelist.cs` (67-name `string[]`, `Contains`);
`rescue_requirements.md` §7 R3b.

**Questions (mine, even if answered):**
- **OPEN (gap, flagged to Thomas):** NO Frozen North (DeepNorth) entries in the whitelist. 7 of 8
  biomes covered. Thomas to decide whether to add Frozen North structures or accept the gap.
- Q: Should `Contains` match `m_name` only, or `m_name` OR `m_prefabName`? → Currently both
  (defensive). **MY DEFAULT; low-stakes.**

---

## 5. The 20% gate — deterministic hash (DECIDED)

**Current design:** For a placed+whitelisted PoI, compute a **stable hash** from (PoI name + zone).
`hits = (hash & 0x7FFFFFFF) % 100 < 20`. If it hits, the structure "has a rescue." Deterministic:
the same structure gives the same answer every time, in every session.

**Why chosen (DECIDED by Thomas, 2026-10-07):**
- **Stable across sessions** — the same structures always have rescues, so the world feels
  consistent (that tower is always a rescue spot).
- **Pairs with the periodic scan** — every 5s the scan re-computes the hash; a deterministic hash
  gives the same answer, so the scan is idempotent. A random roll would disagree with itself.
- **No "rolled" flag needed** — the hash *is* the decision; nothing to remember.
- **Explored areas give consistent results** — Players passing through the same area later have 
  consistent results. This prevents running back and forth to try to get NPCs to spawn (unrealistic).

**Not chosen (and why):**
- **Random 20% roll (original D1)** — re-rolls; with a periodic scan it gives different answers each
  pass; needs a per-structure "already rolled" flag to stabilize. Rejected for deterministic.
- **Roll keyed on the `PlaceLocations` placement event** — that's the old (dead) hook; one-shot.
- **Hash on name only** — would make every same-name structure share one decision (all "WoodHouse4"
  or none). Chose name+zone so each *instance* is independently ~20% (even world-wide distribution).

**Implementation detail (IMPLEMENTED 2026-10-07; `RescueScanner.HashFor` + `Hits`):** hash input =
`GetStableHashCode(name + "|" + zone.x + "," + zone.y)` using the mod's stable hash
(`StringExtensionMethods.GetStableHashCode`, the same one the codebase uses for item name→hash);
`Hits(H) = (H & 0x7FFFFFFF) % 100 < 20`. `GetStableHashCode` is stable across processes/runs
(unlike `String.GetHashCode`), which is the whole point.

**Code references:**
- `rescue_requirements.md` §5 (old: "20% roll"), D1 (§8: "20% roll … one roll per location per zone").
- The mod's stable hash is used in item lookups (`StringExtensionMethods.GetStableHashCode`,
  per AGENTS.md "Notable signatures").
- The old random roll being replaced: `RescueSpawn.cs` :165 (`UnityEngine.Random.value >= RescueChance`).

**Tests:** None directly (this is a decision, not a runtime test). The census (§16) doesn't touch
the hash.

**Questions (mine, even if answered):**
- Q: name+zone vs name+quantized-position — which "zone" exactly? → **IMPLEMENTED:** the 3×3 grid
  zones around the player (`Vector2s`, the same key `m_locationInstances` uses) — `RescueScanner`
  hashes (name, zone) for each candidate zone.
- Q: Is `(name, zone)` unique per placed PoI instance (no collisions)? → Assumed yes (a zone holds
  one instance of a location type). **UNVERIFIED — if two same-type PoIs can share a zone, we need a
  fuller key (e.g., add quantized world pos).**
- Q: 20% exactly, or tunable? → 20% is the spec (D1). **Could be config; MY DEFAULT keep 0.20.**  *Thomas: Should be configurable, config is deferred until after release.*

---

## 6. The ledger

**Current design (DECIDED + IMPLEMENTED 2026-10-07):** No dedicated ledger ZDO. The ledger rides
two existing, world-scoped stores, keyed by the deterministic hash `H`:
- `hmnpc_rescue_welcomed_<H>` → **bool** in the **world's global key space**
  (`ZoneSystem.instance.SetGlobalKey`/`GetGlobalKey`, string overloads — the same persistent,
  cross-synced store holding boss-defeat flags). Set at welcome, **monotonic**, never cleared.
  This is the *only* durable data.
- **"live"** → **derived, not stored.** Each rescue NPC is stamped `hmnpc_rescuepoiHash = H` on its
  own ZDO at spawn. `CollectLive` reads that key off every currently-loaded ZDO
  (`ZDOMan.instance.m_objectsByID`), so "live structures" = { H : a rescue NPC stamped H is loaded
  right now }. A dead rescue's ZDO is destroyed → drops out automatically.
- `Claimed(H) = live.Contains(H) || IsWelcomed(H)`.

**Confirmed claim lifecycle (DECIDED 2026-10-07):**
Structure H is **live** while a rescue NPC stamped `hmnpc_rescuepoiHash = H` is loaded in the
world — derived each pass (`CollectLive`), **survives welcome** (welcome doesn't kill the NPC),
drops out when that NPC's ZDO is destroyed (death), **re-set on resurrection** (the soul spawns a
new NPC stamped H). `welcomed_<H>` = set at welcome, never cleared — the life-independent
"forever claimed" flag. (The pre-refactor design stored one ZDOID per live structure plus an
iterable list of living NPCs; the refactor derives a set of structure hashes instead — same
lifecycle, no bookkeeping, and the "list of living NPCs" idea, which had no use case, is gone.)

- **Spawn at H** (scan): NPC ZDO stamped `hmnpc_rescuepoiHash = H`. → H is live from the next pass
  onward → no re-spawn while the not-rescued NPC is alive.
- **Rescued** (`m_isRescued = true`): nothing changes in the ledger → H stays live while the
  rescued NPC is alive → no re-spawn.
  - **dies** (rescued-not-welcomed, `m_isHirdmandr == false`): **no soul**. The NPC's ZDO is
    destroyed → H drops out of the live set on the next `CollectLive` → `Claimed(H)` false → the
    next scan re-offers a new rescue at H (re-offer).
  - **survives → welcomed**: see below.
- **Welcomed** (`m_isHirdmandr = true`, lives at base): set `welcomed_<H>` (global key). The NPC is
  still live, but H is now **forever claimed** via `welcomed_<H>` — life-independent.
  - **welcomed NPC killed at base**: drops a soul (carries H). The NPC's ZDO is destroyed → H drops
    out of the live set; `welcomed_<H>` stays true → H stays claimed (no re-spawn).
  - **soul resurrected → new NPC**: the new NPC is stamped `hmnpc_rescuepoiHash = H` → H is live
    again. H is still forever claimed (`welcomed_<H>` true).

**Why chosen:**
- **Ride the world's global key space, don't own a ZDO.** `welcomed_<H>` is a string global key —
  the same persistent, cross-synced, world-scoped store as boss-defeat flags. No ZDO to create,
  own, position, or re-find on reload; it just *is*, on world load. (The pre-refactor design's
  whole "re-find the ledger ZDO on reload" open question evaporates.)
- **Derive "live" instead of storing it.** The live set is recomputed from the world each pass, so
  it can never disagree with the world (no stored ZDOID that dangles after a death). The trade is
  an O(loaded ZDOs) read per 5s pass — cheap, and it removes the stored-state failure modes.
- **bool (not ZDOID) for the welcomed flag** — the soul is a *pickable item* (`hirdmandr_soul`)
  that moves (floor → inventory → chest); its ZDOID is not stable, so it can't be tracked by ID.
  `welcomed_<H>` is the *fact* "a rescue from H was welcomed," stored as a monotonic bool.
- **Monotonic, life-independent `welcomed_<H>`** — set at welcome, never cleared. This is what
  makes H "forever claimed": it survives the welcomed NPC later dying (unlike "live," which drops
  out when the NPC's ZDO is destroyed). One successful rescue per structure.

**Not chosen (and why):**
- **Store the soul's ZDOID** — unstable (item moves); wrong tool. → bool instead.
- **Per-NPC ledger keys (key by NPC ZDOID, not by structure)** — we'd have to *find* which NPC
  belongs to which structure (the search we're avoiding). Keying by structure `H` makes lookup direct.
- **A global `List<ZDOID>` of all rescues** — no per-structure identity; can't answer "is structure
  H claimed?" without scanning the list. Keyed-by-H is O(1).
- **Store the welcomed flag on each structure's own ZDO** — natural structures are *placed* by the
  game and their ZDOs are game-managed (and not a stable, writable target for us); the world's
  global key space is simpler and fully under our control.

**Where it lives / how it's owned:** nowhere we manage. `welcomed_<H>` lives in the world's global
key space (persistent + cross-synced by the game, exactly like boss-defeat flags); "live" lives in
the world's loaded ZDOs (derived, never stored). There is nothing to create, own, position, or
re-find on reload — the old OPEN "re-find the ledger ZDO" question (§13) is gone by construction.

**Code references:**
- `RescueLedger.cs` — `HashKey` (`hmnpc_rescuepoiHash`), `WelcomedKey(h)`
  (`hmnpc_rescue_welcomed_<H>`), `IsWelcomed`/`SetWelcomed` (global keys), `CollectLive`,
  `Claimed(h, live)`.
- Global-key API (used): `ZoneSystem.instance.SetGlobalKey(string)` (sets true, idempotent),
  `GetGlobalKey(string)` → bool. The welcomed flag is monotonic — never removed.
- Derive source: `ZDOMan.instance.m_objectsByID` : `Dictionary<ZDOID, ZDO>`; `ZDO.GetInt(string, int)`.

**Questions (mine, even if answered):**
- Q: What identifies a structure in the ledger — the hash `H`? → Yes; `H` = the deterministic hash
  from §5. **DECIDED (consequence of the deterministic-hash choice).**
- Q: Key names — **DECIDED + IMPLEMENTED 2026-10-07:** `hmnpc_rescue_welcomed_<H>` (bool global
  key) + `hmnpc_rescuepoiHash` (Int32 on the rescue's own ZDO, and in the soul payload). The old
  `hmnpc_rescue_live_<H>` (ZDOID) key is **gone** (live is derived, not stored).
- Q: World-scoping / re-find? → **GONE (by construction):** the welcomed flag is a world global
  key (world-scoped, no cross-world leak, always present on load) and "live" is derived — there is
  no ledger ZDO to re-find. (Supersedes the old OPEN "re-find the ledger ZDO on reload.")
- Q: Stale-claim sweep? → **GONE:** "live" is derived each pass, so a dead rescue simply isn't in
  the set; there is no sweep cadence to tune.

---

## 7. The scanner

**Current design (Fix A, IMPLEMENTED 2026-10-26):** A 5s periodic coroutine, **on the server only**
(`ZNet.instance.IsServer()`; in SP that's the host). Each pass (`RescueScanner.RunPass` :58):
1. **Build the live set** once: `RescueLedger.CollectLive(s_live)` → `HashSet<int>` of structures
   that have a rescue NPC loaded right now (reads `hmnpc_rescuepoiHash` off every loaded ZDO).
2. **Center the pass on each scan center:** `GetScanCenters()` — one center per player on the
   server: each connected peer (`ZNet.instance.GetPeers()` → `GetRefPos()`) **plus the local
   player** (when this process has one — `Player.m_localPlayer`), deduped at 10m so a host listed
   both ways is scanned once. Dedicated MP: no local player → peers only (the server is
   **headless**). Hosted MP: peers + the host's own window. SP: `GetPeers()` does not provide the
   host's position (verified 2026-10-26 — the SP regression below), so the local player IS the
   center. An empty list = nothing to scan (dedicated server, nobody connected).
3. `ScanAround(center)` :85 — the candidate zone set: the **3×3** grid around `center`
   (`RescueScanner.GridRadius = 1`); for each `LocationInstance` that is placed AND
   `RescueWhitelist.Contains(m_location)`:
   a. `H = HashFor(name, zone)`. `if (!Hits(H)) continue;` (deterministic 20%, §5)
   b. `if (RescueLedger.Claimed(H, s_live)) continue;` (`Claimed = live.Contains(H) || IsWelcomed(H)`)
   c. Else: `RescueSpawner.PlaceAtLocation(inst, "rescue scan", H)` — the new NPC is stamped
      `hmnpc_rescuepoiHash = H`; on success `s_live.Add(H)` (:130) — two peers' windows can
      overlap within one pass, so claim the structure in the live set immediately (no separate
      live write; the live set is re-derived next pass anyway).
4. **No safety net:** a dead rescue's ZDO is destroyed, so its structure simply drops out of the
   live set — the next pass sees `Claimed(H)` false and re-offers (automatic re-offer on
   not-welcomed death). Never touches `welcomed_<H>` (monotonic); resurrection re-stamps H.

**Why chosen:**
- **Player-centered on the server (Fix A, DECIDED by Thomas, 2026-10-26):** the pre-Fix-A pass was
  "around the player" with a local-player requirement — correct in SP (the host *is* the player),
  but the headless MP server has *no* local player, so it never scanned, and clients never scan
  (`IsServer()` false) → **MP 0-rescues**. The connected peers are the position source the
  server actually has in MP. The gate itself (`IsServer()`) was already server-only and stays.
- **Local player added as a center (SP fix, 2026-10-26):** `GetPeers()` alone does not cover the
  host — verified 2026-10-26: after Fix A, rescues stopped spawning in SP (the peer loop had no
  center in SP), and a hosted MP host is a local player, not a remote peer, so it was missing its
  own window there too. Centers = `peers ∪ local player` (10m dedup): SP works through the
  local-player center; dedicated MP is unchanged (no local player); hosted MP gains the host's
  window.
- **Periodic** = self-healing (a failed spawn retries next pass) and re-evaluates as the player
  moves. No scheduler, no max-N (D1/D8).
- **Zone grid around the player** = only the PoIs the player is near are considered (the game only
  places PoIs in range anyway, so this is cheap and matches "find → rescue" locality).
- **Idempotent** = deterministic hash (§5) + `Claim` check means the scan won't double-spawn.

**Not chosen (and why):**
- **The `PlaceLocations` post-hook** — dead in SP, one-shot, no self-heal (§2).
- **A per-frame check** — wasteful; 5s is plenty for "in range."
- **Client-side scan (each client scans around itself)** — rejected: every client would see
  the same unclaimed structure and each spawn its own rescue (duplicate ZDOs); the server is
  the single authoritative scanner (Fix A).

**Code references:**
- `RescueScanner.cs` — `Start(MonoBehaviour host)` :33 (called from `Hirdmandr.cs` `Awake`),
  `Loop` :43 (5s), `RunPass` :60 (centers loop), `GetScanCenters` :84 (peers ∪ local player,
  10m dedup), `ScanAround` :118 (3×3 grid + in-pass claim `s_live.Add(h)` :163),
  `Ready` :174 (**server gate, no local-player gate**), `HashFor` :197, `Hits` :204; tunables
  `Interval`/`GridRadius`/`HitPercent` :22–24.
- Zone grid: `ZoneSystem.instance.m_locationInstances`, `GetZoneCoords(position, out x, out y)`,
  `m_locationInstances[zoneID]` (pattern in `HirdmandrCommand.cs` `ScanLocations()`, `rescue scan`).
- `RescueSpawner.PlaceAtLocation(inst, reason, rescuepoiHash)` (`RescueSpawn.cs` :32) — stamps
  `hmnpc_rescuepoiHash` on the new NPC.
- Ledger: `RescueLedger.Claimed(h, live)` + `CollectLive` (§6).

**Questions (mine, even if answered):**
- Grid size → **DECIDED: 3×3** (`RescueScanner.GridRadius = 1`). The `rescue scan` diagnostic's
  5×5 is just a debug view, not the real scanner.
- Cadence → **DECIDED: 5s** (`RescueScanner.Interval = 5f`), implemented.
- **DECIDED: server-only in MP (Fix A).** The scan runs only on the **server**
  (`ZNet.instance.IsServer()`; SP = host), so no client ever spawns a rescue. (Originally
  phrased "authoritative client only"; in MP the server is headless, so the pass is centered
  on peers, not on a local player.)
- Q: Does `GetPeers()` include the host peer in SP? → **NO (verified in-game, 2026-10-26):**
  after Fix A, rescues stopped spawning in SP — the peer loop had no center. The
  "SP server has exactly one peer" assumption was wrong; resolved by the local-player center
  (`GetScanCenters`).
- Safety-net cadence → **GONE:** "live" is derived each pass (`CollectLive`), so there is no
  safety net and no per-entry `GetZDO` call to cadence.
- Q: Does the scanner spawn a rescue the moment a structure is placed, or only when the player is
  in range? → Only when in range (the zone grid is centered on the player). **DECIDED (consequence
  of the locality choice).**

---

## 8. The spawn (PlaceAtLocation)

**Current design (Fix A, IMPLEMENTED 2026-10-26):** `RescueSpawner.PlaceAtLocation(LocationInstance
inst, string reason, int rescuepoiHash = 0)` in `RescueSpawn.cs` :18. It:
1. Validates `inst.m_location` + `inst.m_placed`, the `ZNetScene`, and the `npc_entity` prefab.
2. `PickClearSpot(inst)` (`RescueSpawn.cs` :100) — a short raycast to nudge the spawn point clear
   of geometry (the "+2 to caller's right" offset Thomas approved in R4); rotation =
   **`Quaternion.identity`** — the server has no local player to face, and the first-time branch
   sits the NPC down anyway (orientation is cosmetic for a sitting rescue).
3. `ZDOMan.instance.CreateNewZDO(pos, prefabHash)` → a new ZDO, `SetPrefab` + `SetRotation`.
4. **Ownership:** `CreateNewZDO` already stamps the **creating session** as owner — on the
   server (where the scanner runs) that's exactly the owner `HirdmandrNPC.OwnerInit` needs for
   its first-time branch (client replicas see `IsOwner()==false` and take `LoadZDO`). When a
   local player exists (SP, or the `/hirdmandr` test command on a client), copy the player
   ZDO's owner exactly like before — in MP the player ZDO is server-owned, so the ZDO stays
   server-owned even when the command runs on a client.
5. **Stamps the structure hash:** `zdo.Set("hmnpc_rescuepoiHash", rescuepoiHash)` (when
   `rescuepoiHash != 0`) — this is what makes the PoI's structure *live* in the ledger (§6, §7).
   `0` = untracked (the `/hirdmandr spawn` test command).
6. `ZNetScene.instance.CreateObject(zdo)` → the `GameObject`;
   `ZNetScene.instance.AddInstance(zdo, view)` → into the scene.
7. Adds components: `HirdmandrNPC`, `HirdmandrAI`, `HirdmandrGUI`, `HirdmandrGUIRescue`
   (in `Hirdmandr.cs` `CreateNPCPlayer` :386–570, the npc_entity prefab is cloned from "Goblin"
   at :391–394).

**Why chosen / kept:** It's the proven spawn path (used by the R3c hook, the `/hirdmandr spawn`
test command, and the R3c playtest). It works. The refactor keeps it; only the *trigger* changes
(hook → scan). **Fix A changes only what the function requires:** no local player needed (the
hard requirement is gone — the server path works headless), identity rotation, and
`CreateNewZDO` default ownership (the player-owner copy is a guarded, non-fatal extra for the
SP / test-command paths).

**Not chosen:** Re-implementing the spawn inside the scanner — no; the scanner just calls the
existing `PlaceAtLocation`.

**Code references:**
- `RescueSpawn.cs` `PlaceAtLocation` :18 (+ stamp `hmnpc_rescuepoiHash` :70–73, identity
  rotation :43, ownership :46–66), `PickClearSpot` :100. The old `RescueChance` (the 0.20f
  random threshold) is **REMOVED** — the 20% gate is now the deterministic hash §5.
- `Hirdmandr.cs` `CreateNPCPlayer` :386, npc_entity clone :391–394, `AddComponent` :567–570.
- Test command: `HirdmandrCommand.cs` `RescueForceSpawn` :486 (`/hirdmandr spawn`).

**Questions (mine, even if answered):**
- Q: The spawn offset (+2 to caller's right) — keep it for the scanner path too? → **YES, keep it**
  (Thomas approved it in R4 for testing; it works with obstructions). **DECIDED (R4).**
- Q: Biome for the spawned NPC — from position or from a ZDO key? → From position
  (`Heightmap.FindBiome(pos)`), per the dropped D2-strict. **DECIDED (D2-strict dropped; R4).**
- Q: Does `PlaceAtLocation` handle the "already a rescue here" case? → No — the *scan* checks
  `Claimed(H)` before calling it. `PlaceAtLocation` is dumb (just spawns). **DECIDED (separation
  of concerns).**

---

## 9. The unrescued state (D9)

**Current design (KEEP, built & deployed):** An unrescued NPC is (a) **invulnerable** and
(b) **not a mob target**. Both key off the same flag: `!HirdmandrComp.m_isRescued`.
- (a) `CharacterPatch.cs` :56 — a `[HarmonyPatch(typeof(Character), "Damage")]` prefix:
  `if (!comp.m_isRescued) return false;` (unrescued → no damage).
- (b) `MonsterAIPatch.cs` :61 — a `[HarmonyPatch(typeof(BaseAI), "IsEnemy")]` prefix:
  unrescued NPCs are never an enemy, so mobs don't aggro them.

**Why chosen:** D9 (Thomas): an unrescued NPC must not die before it can be rescued (it's waiting to
be found). Invulnerable + not-a-target guarantees it survives until the player rescues it. Once
rescued (`m_isRescued = true`), it behaves like a normal (rescuable → can be welcomed) NPC.

**Not chosen:**
- **Make it a non-combatant but damageable** — no; it must not die (D9).
- **Hide it from mobs only (not invulnerable)** — no; it must also not take environmental damage.

**Code references:**
- `CharacterPatch.cs` :56 (Damage prefix, `!m_isRescued`).
- `MonsterAIPatch.cs` :61 (IsEnemy prefix, `!comp.m_isRescued`).
- `HirdmandrNPC.cs` `m_isRescued` :39, first-time set `m_isRescued=false` :215,
  `Set("hmnpc_isrescued", false)` :774, `LoadState` reads it :860–878.
- `NPCPlayerClone.cs` :78–82 (`m_isHirdmandr`/`m_isRescued` init), `OnDeath` :954–960.

**Questions (mine, even if answered):**
- **OPEN (from R3c playtest observation (1)):** the unrescued NPC was *not sitting* when first
  encountered (expected unrescued idle/sitting). Parked for post-R3 investigation. **OPEN.**
- **DECIDED (D9 in-game poke):** Thomas confirmed in-game — "I've tried, haven't been able to kill
  or damage one yet." Unrescued NPCs are effectively invulnerable to mobs/environment (D9 holds).
- Q: Does `m_isRescued` get set to true *on rescue* (the `/hirdmandr rescue` action) or on
  *welcome*? → On **rescue** (the moment the player rescues it, it stops being invulnerable).
  **MY UNDERSTANDING; confirm the exact transition point in `Rescue()` (per reqs §3, `Rescue()`
  is at `HirdmandrNPC.cs` :1051).** **CONFIRM.**

---

## 10. The rescue → welcome flow

**Current design (KEEP, already built & working end-to-end per R3c playtest (3)):** The existing
skeleton in `HirdmandrNPC.cs`:
- `Rescue()` :1051 — the player rescues the NPC (rescue panel). Sets `m_isRescued = true`.
- `RescueWait()` :1075 — the "waiting to be led home" state.
- `WelcomeHome()` :1196 — triggered when the NPC is led to the base and the player welcomes it.
- `CheckWelcomeRanges()` :1120 — range checks for the welcome action.
- **Ledger write at welcome:** `WelcomeHome()` reads `H` from the NPC's `hmnpc_rescuepoiHash` and
  calls `RescueLedger.SetWelcomed(H)` (sets the `hmnpc_rescue_welcomed_<H>` global key, :1215–1218).
  "Live" is derived (not stored), so welcome changes nothing else. See §6 lifecycle.

**Why chosen / kept:** It's the proven flow (R3c playtest (3): natural spawn → full rescue → artisan
job all worked up to job assignment). The refactor does NOT touch this; it only changes the *spawn
trigger* and adds the *death/ledger* wiring.

**Not chosen:** Rewriting the flow — no; it works.

**Code references:**
- `HirdmandrNPC.cs` `Rescue` :1051, `RescueWait` :1075, `WelcomeHome` :1196 (ledger write
  :1215–1218), `CheckWelcomeRanges` :1120.
- GUI: `HirdmandrGUIRescue` (the rescue panel).

**Questions (mine, even if answered):**
- Q: When exactly does "welcome home" fire relative to the base? → `CheckWelcomeRanges` gates it on
  the NPC being within range of the base. **WORKS (R3c playtest; existing code).**
- Q: After welcome, does the NPC live at the base immediately, or die→soul→resurrect→live?
  → **DECIDED (Thomas, 2026-10-07): the NPC lives at the base immediately.** The soul +
  resurrection exist only to prevent permanent loss of NPCs the player invested in (a kill-recovery
  token, not the normal flow). See §12.

---

## 11. The death transition

**Current design (DECIDED 2026-10-07):** `NPCPlayerClone.OnDeath()` :954–960 calls
`m_hmnpc.HandleDeath()`. The state at death is read from the NPC's own ZDO
(`hmnpc_rescuepoiHash = H` + `m_isHirdmandr`). `HandleDeath` does **not** write the ledger
directly — when the NPC's ZDO is destroyed (immediately at death), its structure simply drops out
of the *live set* on the next `CollectLive`, so `Claimed(H)` reduces to `welcomed_<H>` alone. So
`HandleDeath` only drops the soul (if welcomed):
- **Not-rescued OR rescued-not-welcomed dies** (`m_isHirdmandr == false`): **no soul** (and no
  gear drop — the rescue wasn't welcomed). The NPC's ZDO is destroyed → H drops out of the live
  set; `welcomed_<H>` is false → `Claimed(H)` now false → **the next scan re-offers a new
  rescue** at H. (Thomas DECIDED: "re-offer a new rescue.") The not-rescued sub-case is rare —
  D9 invulnerability — and is the defensive path.
- **Welcomed dies** (`m_isHirdmandr == true`, killed at base): drop gear + spawn a soul
  (`SoulItem.SpawnSoulFromNpc`, §12), stamped with `H` from the NPC's `hmnpc_rescuepoiHash`.
  The NPC's ZDO is destroyed → H drops out of the live set; `welcomed_<H>` stays true →
  **H stays claimed** (no re-spawn).

**Why chosen:**
- **Welcomed → soul + permanent claim** — a successful rescue is a one-shot; `welcomed_<H>` makes
  the structure done (life-independent — it survives the welcomed NPC later dying). The soul is the
  resurrection token (future, §12).
- **Not-welcomed → re-offer** — a rescue that died before being welcomed shouldn't "burn" the
  structure; the world re-offers it. (Thomas DECIDED this over "permanent claim.")

**Not chosen (and why):**
- **Not-welcomed → permanent claim (structure never offers again)** — the rejected alternative;
  Thomas chose re-offer instead (a dead not-welcomed NPC shouldn't make the structure permanently
  empty).
- **Not-welcomed → immediate respawn at death (not next scan)** — no; the periodic scan handles it
  naturally (the dead NPC's structure drops out of the live set, the next pass sees
  `Claimed(H)==false` and re-spawns). Simpler, and consistent with the self-healing scan.
- **`HandleDeath` writes the ledger at all** — no; there is nothing to clear. The NPC's ZDO is gone
  at death, so its structure drops out of the live set on the next `CollectLive`. `HandleDeath`
  only drops the soul (if welcomed).

**Code references:**
- `NPCPlayerClone.cs` `OnDeath` :954–960 → `m_hmnpc.HandleDeath()`.
- `HirdmandrNPC.cs` `HandleDeath()` — welcomed: drop gear + soul (stamped with H); not-welcomed:
  nothing. It does **not** write the ledger — the dead structure drops out of the live set on its
  own. *Thomas: This functionality was tested previously and was working perfectly at that time.*
- `SoulItem.cs` `SpawnSoulFromNpc` :210 (the soul drop; carries `hmnpc_rescuepoiHash` = H in the
  soul's `m_customData`).
- Ledger: `RescueLedger` — `welcomed_<H>` global key + derived live set (§6).

**Questions (mine, even if answered):**
- Q: Does an unrescued NPC *ever* die in practice (D9 makes it invulnerable)? → In theory no (D9);
  the re-offer path is a defensive fallback for edge cases (e.g., a future mechanic that bypasses D9,
  or a reload race). **DECIDED (re-offer) as a defensive path.**
- Q: Does `HandleDeath` need the structure's `H`? → **Not for the ledger** (it writes nothing —
  the dead structure drops out of the live set on its own). It needs `H` only to **stamp the
  soul** in the welcomed case, read from the NPC's `hmnpc_rescuepoiHash`. **DECIDED (Thomas,
  2026-10-07): the key is `hmnpc_rescuepoiHash`, and it must also exist in the soul's
  `m_customData`** (so resurrection knows which structure to claim back).
- Q: Is the "welcomed" state distinguishable at death time? → **DECIDED (Thomas, 2026-10-07), by
  `m_isHirdmandr`. States are:**
  NOT rescued NPC: `m_isRescued == false` and `m_isHirdmandr == false`
  rescued and NOT welcomed NPC: `m_isRescued == true` and `m_isHirdmandr == false`
  welcomed NPC: `m_isHirdmandr == true`

---

## 12. The soul item

**Current design (foundation BUILT & VERIFIED by Thomas; resurrection NOT yet implemented):**
`hirdmandr_soul`, a pickable, stack-1 item cloned from `Eitr` (`SoulPrefabName` :26,
`SourcePrefabName` :29). Carries the NPC's identity in `m_customData` (`SoulTagKey = "hmnpc_soul"`
:32): name, sex, appearance, owner, role/jobs, skills (bed excluded — the soul releases the
NPC's bed/chest claims, :222–227), **and the PoI hash** `hmnpc_rescuepoiHash = H` (so
resurrection knows which structure to claim back). `SpawnSoulFromNpc` :210 drops it at the
NPC's death position.

**Why chosen:**
- **Pickable (Option B)** — Thomas chose pickable over a fixed-location or auto-consumed soul. A
  pickable item the player carries home is the "resurrection token" — it survives reload
  (`ItemDrop.ItemData.m_customData` persistence), moves with the player, and is where the (future)
  "resurrect at hearth" mechanic will read it from.
- **Cloned from Eitr** — a real, existing Valheim item, so it behaves like a normal item (picks up,
  stacks=1, persists) without a new item system.
- **Carries full NPC identity** — so resurrection can rebuild the exact NPC (name/appearance/skills
  /jobs/owner).

**Not chosen (and why):**
- **Option A: a fixed-location "soul shard" that can't move** — rejected; less flexible, and the
  player can't carry it home or carry it to a resurrection location.
- **A new item system (not cloned from Eitr)** — more work; Eitr clone is sufficient.
- **Auto-consume the soul on welcome (no pickup)** — rejected; the soul is the *token* for the
  future resurrection, so it must persist and be pickable.

**Code references:**
- `SoulItem.cs` `SoulPrefabName` :26, `SourcePrefabName` :29, `SoulTagKey` :32,
  `RegisterSoulItem` :41, `SpawnTestSoul` :129, `SpawnSoulFromNpc` :210 (stamps
  `hmnpc_rescuepoiHash` into the payload), payload :197–246, bed exclusion :222–227,
  `RemoveBoltEmitters` :273.
- Registered on `PrefabManager.OnVanillaPrefabsAvailable` (`Hirdmandr.cs` :75).

**Questions (mine, even if answered):**
- **DECIDED (Thomas, 2026-10-07):** A welcomed NPC lives at the base **immediately** — the soul is
  NOT part of the normal flow. The soul only exists as a game-play interaction when a **welcomed**
  NPC is **killed** (a kill-recovery token). Resurrection (future) is how a killed welcomed NPC is
  brought back. See §10 / §11.
- Q: Where does resurrection read the soul from (inventory? chest? a "hearth" interaction)? →
  **OPEN (future; not part of the spawn refactor).** *Thomas: Not designed yet.*
- Q: Does the resurrected NPC re-enter the ledger? → **DECIDED (2026-10-07): Yes.** On
  resurrection the new NPC is stamped `hmnpc_rescuepoiHash = H`, so it is *live* again (H is in
  the live set) — no stored value to re-set. H stays claimed (`welcomed_<H>` already true —
  forever claimed). Both the NPC and the soul carry the spawning PoI hash; that must be respected.
- Q: Soul stack size 1, never destroyed — confirmed? → Yes (stack-1, `fixReference:true`).
  **BUILT & VERIFIED (Thomas).**

---

## 13. Persistence model

**Current design (Fix A owner model, 2026-10-26):** NPCs are **persistent ZDOs** (they survive
reload). Ownership: `ZDOMan.CreateNewZDO` stamps the **creating session** as owner (the server,
in the scanner path) — `IsOwner()` is a bit flag on `m_dataFlags` (bit 5), **not** an ID
compare, so client replicas see `IsOwner()==false` and take `LoadZDO`; when a local player
exists (SP / test command) the player ZDO's owner is copied (in MP the player ZDO is
server-owned, so the ZDO stays server-owned). On reload, the first-time
`OwnerInit` branch re-initializes (`HirdmandrNPC.cs` :186 reads `hmnpc_name`; :215–216 sets
`m_isRescued=false`, `m_isHirdmandr=false` on first time; `LoadState` :860–878 restores
`hmnpc_isrescued`, `hmnpc_isHirdmandr`, `hmnpc_jarlZOID`, role/jobs, skills, ownedbed).

**Why it matters for the spawn design:**
- **ZDOID stability (U1 = YES)** — far/unloaded NPCs still sit in `ZDOMan` (`m_objectsByID`;
  census: 7 ZDOs, 1 loaded). So `CollectLive` still reads a far-but-alive rescue's
  `hmnpc_rescuepoiHash` — its structure stays *live* even when far, so returning to it keeps it
  claimed (no double-spawn). A dead rescue's ZDO is destroyed → drops out of the live set on its
  own (no safety-net pass).
- **No ledger ZDO to persist or re-find** — the welcomed flag is a world global key (always
  present on load) and "live" is derived from the world each pass. The pre-refactor OPEN
  "re-find the ledger ZDO on reload" question is gone by construction.
- **Biome from position** — because newly spawned NPCs are persistent and their position is stable,
  `Heightmap.FindBiome(pos)` gives a stable biome on reload too. Welcomed existing NPCs have already had their
  skills, equipment, and appearance set and it is loaded from ZDO (Biome irrelevant).

**Not chosen:**
- **Non-persistent (temporary) NPCs** — no; the NPC must survive reload (it's a "person").
- **Biome stored in a ZDO key (D2-strict)** — dropped; position-based biome for spawned NPCs is simpler and stable.

**Code references:**
- `HirdmandrNPC.cs` first-time :186, :215–216; `LoadState` :860–878.
- `SoulItem.cs` persistence via `ItemDrop.ItemData.m_customData`.
- Global-key store: `ZoneSystem.instance.SetGlobalKey`/`GetGlobalKey` (§15) — the same persistent,
  cross-synced, world-scoped store as boss-defeat flags.

**Questions (mine, even if answered):**
- ~~How does the ledger ZDO re-find itself on reload?~~ → **GONE (by construction):** there is no
  ledger ZDO — the welcomed flag is a world global key (always present on load) and "live" is
  derived from the world each pass.
- Q: Are owned ZDOs world-scoped (don't leak across worlds)? → **UNVERIFIED.** * Thomas: This is verified in how Valheim works. Even when characters are taken to different worlds, their progression and other global ZDO information does NOT go with the player's characterm it is specific to the loaded world.*
- Q: On reload, does the *scan* re-spawn a rescue that was already alive (double-spawn)? → No —
  the scan checks `Claimed(H, live)` (live OR welcomed) before spawning, so an already-alive rescue
  is not re-spawned. **DECIDED (consequence of the `Claimed` check).**

---

## 14. Biome → skills + gear (R4, DONE)

**Current design (BUILT, DEPLOYED, TESTED by Thomas):** Per-biome gear + skill tables in
`BiomeData.cs` (8 biomes). At spawn, the NPC's biome is determined by position
(`Heightmap.FindBiome(pos)`), and its gear + skills are set from the biome tables.

**Why chosen:** Thomas wants rescues to feel "native" to the biome they're found in (a Mistlands
rescue has Mistlands-appropriate gear/skills). Position-based (D2-strict dropped).

**Not chosen:** A fixed global loadout; biome-from-ZDO-key (D2-strict, dropped).

**Code references:**
- `BiomeData.cs` (8-biome gear + skill tables).
- `HMSkills.cs` `LoadSkills` :79–80 (`hmnpc_skill<name>`, `hmnpc_skill<name>enable`),
  `SaveSkills` :107–108.
- `rescue_requirements.md` §6 (gear data), §7 R4.

**Status: DONE (R4).** Test results in §16 (R4). Not part of the spawn refactor (it's a spawn
*consequence* and already works).

**Questions (mine, even if answered):**
- Q: Ashlands/Mistlands not visually tested (only `verifygear = 0 problems`). → **ACCEPTED**
  (names valid; visual check deferred). **Thomas accepted (R4).**

---

## 15. Verified game API surface (code against this)

Confirmed via Cecil + the U1 census (2026-10-07). Use these exact signatures:

**ZDOMan** (static `ZDOManager.instance`):
- `CreateNewZDO(Vector3, int prefabHash)` → ZDO
- `GetZDO(ZDOID)` → ZDO  ← **the O(1) existence check (U1-safe)**
- `m_objectsByID` : `Dictionary<ZDOID, ZDO>`
- `NrOfObjects()` → int
- `GetAllZDOsWithPrefabIterative(string, List<ZDO>, ref int)` → bool — **UNUSABLE one-shot**
  (returned 0 with more=False, cursor=92401, despite 7 existing ZDOs). Do NOT rely on it.

**ZDO:**
- `GetPrefab()` → int  ← **NOT `GetPrefabHash()`**
- `SetPrefab(int)`, `m_uid` (ZDOID), `m_position` (Vector3), `SetRotation`, `SetOwner`
- `GetString/GetBool/GetFloat/GetVec3/GetLong/GetZDOID(string)`, `Set(string, value)`

**ZNetScene:**
- `GetPrefab(string)` → GameObject, `GetPrefabHash(GameObject)` → int
- `CreateObject(ZDO)` → GameObject, `AddInstance(ZDO, ZNetView)`

**ZoneSystem:**
- `instance` (static), `m_locationInstances` : `Dictionary<Vector2s, LocationInstance>`
- `GetZone(Vector3)` (static) → Vector2s, `GetZonePos(Vector2s)`, `GetLocation(string)`,
  `FindClosestLocation`, `PlaceLocations` (the old hook target), `SpawnMode` (Full/Ghost/Client)

**LocationInstance (STRUCT):** `m_location` (ZoneLocation, nullable class), `m_position` (Vector3),
`m_placed` (bool).

**ZoneLocation:** `m_name`, `m_prefabName`, `m_biome` (Heightmap.Biome), `m_quantity`, `m_enable`,
`m_group`, `m_interiorRadius`, `m_exteriorRadius`, `m_unique`.

**Heightmap.Biome:** None=0, Meadows=1, Swamp=2, Mountain=4, BlackForest=8, Plains=16, AshLands=32,
DeepNorth=64, Ocean=256, Mistlands=512, All=895, Land=639. `Heightmap.FindBiome(Vector3)` (static),
`Heightmap.BiomeToString`.

**ZoneSystem global keys (the ledger's store — DECIDED + IMPLEMENTED 2026-10-07):**
- `ZoneSystem.instance.SetGlobalKey(string)` → sets a bool global key true (idempotent).
- `ZoneSystem.instance.GetGlobalKey(string)` → bool (false if unset / no instance).
- Persistent, cross-client-synced, world-scoped — the same store holding boss-defeat flags.
  The welcomed flag is `hmnpc_rescue_welcomed_<H>`. Monotonic (never removed).
- (Superseded precedent, still-valid API): `ZDO.Set(key, ZDOID)` / `ZDO.GetZDOID(key)`;
  `HirdmandrBed.cs` :42, :83–90, :127–138, `Claim` :150; `HirdmandrChest.cs` :91, :121–123,
  :138–148, `Claim` :257.

**Stable hash:** `StringExtensionMethods.GetStableHashCode(string)` (the mod's; stable across
processes — use for the deterministic 20% gate §5).

**ZNet / ZNetPeer (Fix A, 2026-10-26):**
- `ZNet.instance` (static), `IsServer()` → bool.
- `ZNet.instance.GetPeers()` → `List<ZNetPeer>` — the connected peers; **in SP the server has
  exactly one peer (the host)** (the SP regression test validates this).
- `ZNetPeer.GetRefPos()` → `Vector3` (the peer's reference position — the scan center);
  `m_playerID`, `m_characterID`.
- Usage precedent: `HirdmandrCommand.cs` (~:986) — `var peers = ZNet.instance.GetPeers();`.

**ZDO ownership (Fix A, 2026-10-26; via Cecil on 1.0.16):**
- `ZDOMan.CreateNewZDO(Vector3, int)` internally calls `SetOwnerInternal(ZDOMan.m_sessionID)`
  — the creating session owns the new ZDO (no separate `SetOwner` needed for the scanner path).
- `ZDO.IsOwner()` = `(m_dataFlags & 32) > 0` (bit 5 — a flag, **not** an ID compare). A
  server-created ZDO: `IsOwner()==true` on the server, `false` on client replicas →
  `HirdmandrNPC.OwnerInit` runs the first-time branch on the server only.
- `ZDO.GetOwner()` / `ZDO.SetOwner(ZDOID)`.
- **No `ZNetSession` type** in 1.0.16 (Cecil search; the session ID lives on `ZDOMan`).

---

## 16. Tests I ran for you (results)

**1. R3c playtest (2026-10-06):**
- (1) Unrescued NPC was **NOT sitting** when first encountered (expected unrescued idle/sitting).
  → **OPEN** (parked, §9).
- (2) Saw "StoneTowerRuins07 placed in mode **Ghost** (expected Full)" but **never** a
  Full-placement/roll log while standing at the structure. → **Led to the pivot** (the Ghost/Full
  gate is wrong for SP; the hook is effectively dead). → **DECIDED: pivot to scan.**
- (3) Natural 1-NPC spawn + full rescue + artisan job all worked up to job assignment. →
  **The rescue→welcome→job flow works end-to-end.** (Kept as-is, §10.)

**2. R4 gear/skills (2026-10-06):**
- `/hirdmandr spawn` in Meadows/BlackForest/Swamp/Mountain/Plains: logs + gear +
  scattering/incomplete loadouts all good.
- Spawn offset (+2 to caller's right, works with obstructions) **approved and KEPT** (§8).
- Ashlands/Mistlands not visually tested but `verifygear = 0 problems` (names valid).

**3. Increment A (soul item foundation):** Built+deployed, tested, verified by Thomas: sphere
appears, pickable, not stackable, custom data survives reload.

**4. Increment B (death → drop gear + soul):** Built+deployed, verified by Thomas.

**5. U1 ZDO census (this session, 2026-10-07) — THE ENABLING TEST:**
- Command: `/hirdmandr rescue zdo` (`HirdmandrCommand.cs` `ZdoCensus()`).
- Test method: spawn NPC at A, walk far to B, spawn NPC at B, run census.
- Results: (a) npc_entity ZDOs in ZDOMan = **7**; (b) loaded NPCs (`FindObjectsOfType`) = **1**;
  (c) `GetAllZDOsWithPrefabIterative` = **0** (more=False, cursor=92401).
- Interpretation: **U1 = YES** (far NPCs persist in ZDOMan — the 6 far ones are ZDOs, the 1 near
  one is loaded). `GetAllZDOsWithPrefabIterative` is **unusable** one-shot. 7 = accumulated from
  prior test sessions (R3c/R4 spawns), not just this session's 2.
- **Why it matters:** U1=YES means far/unloaded NPCs still sit in `ZDOMan` (`m_objectsByID`), so
  `CollectLive` still reads a far-but-alive rescue's `hmnpc_rescuepoiHash` → its structure stays
   live even when far (no false "gone"). A dead rescue's ZDO is destroyed → drops out of the live
   set on its own (§6, §13).

**6. R7 full playtest (2026-10-07, this session) — scanner + ledger, end to end:**
- **T1 scanner natural spawn — PASS.** Traversal + reloads (restart, leave/return) across
  Meadows/BlackForest/Plains/Mistland: good `no spawn (>20)` vs `spawned NPC` distribution, and
  every spawned NPC transitioned to `NPC already spawned`. **No double spawns** in log or game,
  across restarts/traversal; biome had no effect. ~40 whitelisted PoIs → 10 spawns ≈ 20% (the
  deterministic hash is working).
- **T2 no re-offer / determinism — PASS.** Leave + return: the same single rescue remains (same
  name + equipment, visual).
- **T3 welcome flow + "forever claimed" (R6) — PASS.** Wild welcome correctly blocked (center msg
  "Cannot welcome … near a Workbench and a Fire"); "Follow Me" follow worked (R5 — kept up, no nav
  or clipping); at base `WelcomeHome` fired (effect + job-capable); dropped out of the UNRESCUED
  list; returned to the origin PoI for ≥1 min → no re-offer.
- **T4 reload persistence — PASS.** After load: welcomed NPC still a hirdmandr with the same job
  config; unrescued world rescue(s) persisted (same name + equipment). `rescue zdo`/`rescue list`
  now print `hash=` (added this session) to close the hash-visibility gap behind Thomas's
  conditional-pass note.
- **T5 death paths — PASS.** welcomed → gear + soul. (unrescued → re-offer) not re-offered while in
  the area, but a new rescue WAS offered after leaving render distance + returning — Thomas: "don't
  troubleshoot, the important part is a new rescue WAS offered eventually." (Low-priority ambiguity,
  §18 item 5.)
- **Extra — welcomed-kill persistence — PASS.** Killed a welcomed NPC → quit → reload → return to the
  PoI → NOT re-offered; soul existed. (The `welcomed_<H>` global key survives reload; soul = a
  kill-recovery token, not the normal flow.)
- **Obs — unrescued NPCs ignored by monsters, then targeted after rescue (NOT a bug):** D9 working
  as designed — `!m_isRescued` → not-a-target + invulnerable; after `Rescue()` sets `m_isRescued`,
  the same monsters immediately engaged it. Confirms D9.
- **Obs — unrescued NPC standing, not sitting:** on first spawn it sits briefly then stands; on
  reload it stands from the start. Root cause found → §18 item 3.

---

## 17. Diagnostics / test commands

All under `/hirdmandr` (`HirdmandrSpawnCommand`, `HirdmandrCommand.cs` :12, `Name => "hirdmandr"`):
- `rescue zdo` — **the U1 census** (this session). Counts (a) npc_entity ZDOs in ZDOMan, (b)
  loaded NPCs, (c) `GetAllZDOsWithPrefabIterative` result; per ZDO it now prints `hash=`
  (the `hmnpc_rescuepoiHash` stamp, added 2026-10-07 for T4). `ZdoCensus()`.
- `rescue scan` — 5×5 zone-grid PoI dump around the player (`ScanLocations()`).
- `rescue catalog` — the structure-name catalog (used to build the whitelist).
- `rescue close` — close PoIs dump.
- `rescue find` — find a PoI by name.
- `rescue list` — list current rescues; per unrescued rescue it now prints `hash=` (added
  2026-10-07). (`RescueList()`)
- `rescue spawn` — force-spawn a rescue (`RescueForceSpawn` :486) — the manual spawn test.
- `rescue soulsoul` — spawn a test soul (`SpawnSoulTest` :404).
- `rescue soulinspect` — inspect a soul's custom data.
- `verifygear` — validate gear names per biome (R4; Ashlands/Mistlands = 0 problems).
- `invdump` — dump an NPC's inventory.
- **Scanner per-pass log (automatic) — REMOVED 2026-10-07 after the R7 playtest.** Added
  temporarily so each placed + whitelisted PoI logged one line per 5 s pass (`no spawn (>20)` |
  `spawned NPC` | `NPC already spawned` | `spawn FAILED`); it did its job and was removed as
  clutter (Thomas). `RunPass` is back to the clean no-log path.

---

## 18. Open questions & decisions log

**DECIDED (Thomas, 2026-10-07):**
- **Unrescued / not-welcomed death → re-offer a new rescue** (claim released, respawn next scan).
  (Rejected: permanent claim.)
- **20% gate → deterministic hash** (stable across sessions). (Rejected: random roll.)
- **The claim lifecycle (the ledger):** `welcomed_<H>` (a world global key) = set at welcome,
  monotonic, never cleared (the "forever claimed" flag). "Live" = **derived** each pass from the
  world (a structure is live while a rescue NPC stamped with its hash is loaded — no stored
  ZDOID). `Claimed(H) = live.Contains(H) || welcomed_<H>`. `HandleDeath` writes nothing — a dead
  structure drops out of the live set on its own. See §6.
- **Soul flow:** a welcomed NPC lives at the base immediately; the soul is only a kill-recovery
  token (dropped when a *welcomed* NPC is killed), NOT part of the normal flow. See §12.
- **Key name:** `hmnpc_rescuepoiHash` — stamped on the NPC ZDO AND carried in the soul's
  `m_customData` (so resurrection knows which structure to claim back).
- **MP:** the scan is gated to the instance owner → implemented as `ZNet.instance.IsServer()`
  (SP is always true).
- **Fix A (2026-10-26): the scanner is server-only + player-centered (MP 0-rescues).** Root cause
  (confirmed): the old pass required a local player (gate + scan center); the headless MP
  server has none, and clients never scan (`IsServer()` false) → in MP nobody scanned and
  rescues never appeared (SP worked — the host *is* the player). Fix: `RunPass` centers on each
  scan center (`GetScanCenters()` = peers ∪ local player); `Ready()` drops the local-player
  gate; `PlaceAtLocation` drops the local-player requirement (identity rotation; `CreateNewZDO`
  default owner, guarded player-owner copy). **MP in-game VERIFIED (Thomas, 2026-10-26: "MP
  worked very well")** — rescues spawn around each connected peer's 3×3 window.
- **SP regression + fix (2026-10-26): rescues no longer spawned in SP after Fix A.** Root
  cause: the "SP server has exactly one peer" assumption was wrong — in SP `GetPeers()` does
  not provide the host's position, so the peer loop scanned nothing. Fix: centers = `peers ∪
  local player` (`RescueScanner.GetScanCenters()`, 10m dedup) — SP works through the
  local-player center, dedicated MP unchanged. `rescue scan` now prints the actual centers
  (`STATE: scanCenters=N` + `CENTER` lines). **SP retest pending (Thomas).**
- **Ledger scoping:** world-scoped — the welcomed flag rides the **world global key space** and
  "live" is derived; no dedicated ledger ZDO, nothing to re-find, no cross-world leak.
- **D9:** confirmed working (Thomas tried to kill/damage an unrescued NPC; couldn't). Re-confirmed
  in the R7 playtest (2026-10-07): an unrescued rescue surrounded by monsters was **not attacked**
  (not-a-target + invulnerable); after `Rescue()` set `m_isRescued`, the same monsters
  **immediately engaged** it.
- **`HandleDeath`:** tested working previously (welcomed: drop gear+soul; not-welcomed: nothing).
- **20% rate:** configurable, but the config is deferred until after release.

**DECIDED (earlier, in `rescue_requirements.md` §8):**
- D1–D9 (20% rate, no scheduler, no cap, no despawn, multiple concurrent OK, D9 invulnerable, etc.).

**OPEN / NEEDS CONFIRMATION (need Thomas):**
1. ~~**Frozen North gap:** no DeepNorth structures in the whitelist.~~ **DECIDED (Thomas, 2026-10-08):
   intentional - NO hirdmandr spawn in Frozen North.** Fiction: the failed vikings never got this far,
   so the endgame is off-limits to rescued NPCs. The whitelist has no DeepNorth entries by design; the
   per-biome skill/gear scaffolding for DeepNorth is kept for the future. (PMW decision #39.)
2. ~~**`(name, zone)` uniqueness:** is it a unique key per placed PoI instance?~~ **RESOLVED
   (Thomas, 2026-10-08): leave the (name, zone) hash as-is (no position added).** `m_locationInstances`
   holds one `LocationInstance` per zone, so (name, zone) is already unique per scanner candidate; an
   NPC spawns in the zone and is tracked properly - the >1-same-type-in-a-zone case is not a
   correctness issue. (PMW decision #40.)
3. ~~**Unrescued NPC standing, not sitting** (R3c obs (1); re-confirmed in R7, 2026-10-07).~~
    **RESOLVED 2026-10-08 (#26i, DONE).** Two root causes, both fixed: (a) **emote case
    mismatch** — the spawn code called `StartEmote("Sit")` (capital) which poked animator bool
    `emote_Sit`, but the Player controller param is `emote_sit` (lowercase), so the sit bool never
    reached the animator (NPC stuck in `idle`); now `StartEmote("sit")` on both the fresh-spawn and
    the load path. (b) **base `MonsterAI` not disabled on the LOAD path** (named rescue) so loaded
    rescues wandered (only the fresh-spawn branch disabled it); now `m_monsterai.enabled=false` in
    both unrescued blocks, re-enabled on `WelcomeHome`. Plus: `HirdmandrNPC.Update()` drives the
    orphaned `NPCPlayerClone.UpdateEmote` pump (the decompiled clone's own `Update()` is empty) and
    `Rescue()` calls `StopEmote()` so the sit clears on welcome. Verified in-game (Thomas): fresh
    spawn + load both sit and stay put; rescue stands up. Diagnostics (SitDiag/SitDiagAnim/
    m_sitDiagSamples) + the dead `Sit()` removed; build 190,976 B, 0 errors.
4. **Resurrection UI/flow:** where/how the soul resurrects (inventory? chest? a hearth?) — **not
   designed yet** (future; not part of the spawn refactor).
5. **T5a re-offer timing (low priority, 2026-10-07):** an unrescued rescue at a PoI was *not*
   re-offered while the player stood in the area, but *was* after leaving render distance +
   returning. Thomas: don't troubleshoot ("the important part is a new rescue WAS offered
   eventually"). Possibly the claim was held (live set) in-area and only freed once the PoI
   re-scanned after a full load cycle, or the rescue was removed in the wild. No action unless it
   reproduces as a real double/ghost.

**Resolved by the implementation (2026-10-07):**
- ~~Scan grid size~~ → **3×3** (`RescueScanner.GridRadius = 1`).
- ~~Scan cadence~~ → **5s** (`RescueScanner.Interval = 5f`).
- ~~Ledger ZDO re-find on reload~~ → **gone by construction** (no ledger ZDO; global key + derive).
- ~~Stale-claim sweep cadence~~ → **gone** ("live" is derived, nothing to sweep).

**MY DEFAULTS (tunable, not Thomas decisions, unless he says otherwise):**
- Cadence 5s; grid 3×3; key names `hmnpc_rescue_welcomed_<H>` (global key) /
  `hmnpc_rescuepoiHash` (on the rescue ZDO + soul payload); hash input `name|zone.x,zone.y`; keep
  the R4 spawn offset; keep 20%.

---

### 18.1 Spawn close-out checklist (as of 2026-10-08)

What is left to close out the **world-NPC-spawn mechanic** (the rescue spawn feature), cross-checked
against PMW (`hirdmandr`) INBOX / DECISIONS / TASKS.

**Working end-to-end (R7 playtest PASSED 2026-10-07; no action):**
- Deterministic scanner spawn (no double-spawn, ~20%, biome-agnostic).
- No re-offer / "forever claimed" (welcome sets `welcomed_<H>`).
- Welcome flow (wild welcome blocked; follow; `WelcomeHome` + job; dropped from UNRESCUED list).
- Reload persistence (welcomed stays a hirdmandr; unrescued rescues persist; `hash=` visible via
  `rescue zdo`/`rescue list`).
- Death paths (welcomed → gear + soul; welcomed-kill + reload stays claimed, soul exists).
- D9 (unrescued invulnerable + not-a-target) — confirmed in R7.
- Unrescued sitting pose (#26i) — RESOLVED 2026-10-08 (item 3 above).

**PMW state corrections (work already DONE+verified, still not `done` in PMW):**
- **R2 (#26b)** — in-game API verification: BUILT + DEPLOYED, and its `rescue catalog`/`scan`
  output built the 67-name whitelist → DONE (was left in `pool`).
- **R4 (#26d)** — per-biome skills + gear: `BiomeData.cs` BUILT, `verifygear` = 0 problems, and
  Thomas tested `/hirdmandr spawn` in 5 biomes (R4 step 6) → DONE (was left in `pool`).
- **R5 (#26e) follow hardening** and **R6 (#26f) welcome home** — core is working (R7: follow kept
  up, no nav/clipping; `WelcomeHome` fired + job-capable; NPC stays at base + persists on reload).
  Before marking `done`, confirm the FULL acceptance criteria were exercised: R5 = **board
  raft/longship** + disembark (the run-away + portal/wait cases are confirmed in R7, ship-board is
  not explicitly logged); R6 = post-reload **daily routine** running. Verify, then close.

**DECIDED (Thomas, 2026-10-08) - both open calls closed:**
- **Frozen North (DeepNorth):** intentional no-NPC (fiction: the failed vikings never got this
  far; the endgame is off-limits to rescued NPCs). No DeepNorth entries in the whitelist by design;
  skill/gear scaffolding kept. (PMW #39.)
- **`(name, zone)` hash-key uniqueness:** leave as-is (no position added). One `LocationInstance`
  per zone, so (name, zone) is already unique per candidate; not a correctness issue. (PMW #40.)

**OPEN — genuine remaining spawn work (in PMW):**
- **SP retest (PMW #83)** — SP regression found 2026-10-26 (rescues stopped spawning in SP
  after Fix A: `GetPeers()` has no host peer in SP); fix BUILT + DEPLOYED 2026-10-26
  (`GetScanCenters` = peers ∪ local player, 10m dedup; `rescue scan` prints the centers).
  Pending Thomas's SP run: `/hirdmandr rescue` → expect `STATE: scanCenters=1` (the local
  player) → rescues appear around the player. (MP is verified — "MP worked very well".)
- **Soul item polish (#26h)** — refine `hirdmandr_soul` name/description/icon/weight before release
  (dependency of the rescue feature; P2 pool).
- **20% rate config** — deferred until after release (decision #27/D1).
- **Soul resurrection UI/flow** (§18 item 4) — not designed yet (future).

**Non-action:**
- **T5a re-offer timing** (§18 item 5, low priority) — no action unless it reproduces as a real
  double/ghost.

---

## 19. Key code locations (index)

| File | What's there | Lines |
|---|---|---|
| `RescueSpawn.cs` | `RescueSpawner` class; `PlaceAtLocation(inst, reason, rescuepoiHash)` :18 (stamps `hmnpc_rescuepoiHash` :70–73; identity rotation :43; `CreateNewZDO` default owner + guarded player-owner copy :46–66); `PickClearSpot` :100. (Old `RescueChance` + `RescueSpawnHook` **REMOVED**.) | 18, 100 |
| `RescueLedger.cs` | **NEW** — the ledger: `HashKey`; `IsWelcomed`/`SetWelcomed` (global keys); `CollectLive` (derive live from loaded ZDOs); `Claimed(h, live)` | — |
| `RescueScanner.cs` | **NEW** — the 5s pass (Fix A + SP fix, player-centered): `Start(host)` :33, `Loop` :43, `RunPass` :60 (centers loop), `GetScanCenters` :84 (peers ∪ local player, 10m dedup), `ScanAround` :118 (3×3 grid + in-pass `s_live.Add(h)` :163), `Ready` :174 (server gate, no local-player gate), `HashFor` :197, `Hits` :204; `Interval`/`GridRadius`/`HitPercent` :22–24 | 22–24, 33, 43, 60, 84, 118, 163, 174, 197, 204 |
| `HirdmandrCommand.cs` | `HirdmandrSpawnCommand` (`Name=>"hirdmandr"`); `Help`; IsCheat/IsNetwork; `SpawnSoulTest`; `RescueTest` dispatch (now has `zdo`); `RescueForceSpawn`; `ScanLocations` :907 (`rescue scan`; prints `STATE: peers=N` :987, `STATE: scanCenters=N` :999, `CENTER` lines :1003 — the scanner's actual decision); `RescueList`; **`ZdoCensus`** | 12, 16, 21–23, 404, ~436–480, 486, ~585+, 907, 987, 999, 1003 |
| `SoulItem.cs` | `SoulPrefabName`; `SourcePrefabName`; `SoulTagKey`; `RegisterSoulItem`; `SpawnTestSoul`; `SpawnSoulFromNpc` (stamps `hmnpc_rescuepoiHash` in payload); payload; bed exclusion; `RemoveBoltEmitters` | 26, 29, 32, 41, 129, 210, 197–246, 222–227, 273 |
| `Hirdmandr.cs` | `Awake` (starts the scanner: `RescueScanner.Start(this)`); `Harmony.PatchAll`; `OnVanillaPrefabsAvailable` (incl. `SoulItem.RegisterSoulItem`); `AddConsoleCommand`; `CreateNPCPlayer`; npc_entity clone (from "Goblin"); `AddComponent` (HirdmandrNPC/AI/GUI/GUIRescue) | 51, 56–57, 68–75, 80, 87, 386, 391–394, 567–570 |
| `HirdmandrNPC.cs` | `m_isRescued`/`m_isHirdmandr`; first-time branch (`hmnpc_name`, set flags); `Set("hmnpc_isrescued",false)`; `LoadState`; `Rescue`; `RescueWait`; `WelcomeHome` (ledger write: `SetWelcomed(H)`); `CheckWelcomeRanges`; `HandleDeath` (welcomed: drop gear+soul; not-welcomed: nothing; writes no ledger) | 39–40, 186, 215–216, 774, 860–878, 1051, 1075, 1196, 1215–1218, 1120 |
| `NPCPlayerClone.cs` | `m_isHirdmandr`/`m_isRescued` init; `OnDeath` → `m_hmnpc.HandleDeath()` | 78–82, 954–960 |
| `CharacterPatch.cs` | D9 invulnerability: `[HarmonyPatch(Character.Damage)]` prefix, `!comp.m_isRescued` | 56 |
| `MonsterAIPatch.cs` | D9 not-a-target: `[HarmonyPatch(BaseAI.IsEnemy)]` prefix, `!comp.m_isRescued` | 61 |
| `HirdmandrBed.cs` | Precedent: 30s stale-claim sweep; `hmnpc_bedOwnerZDOID`; `Claim`; `UnClaim` | 42, 83–90, 127–138, 150, 164 |
| `HirdmandrChest.cs` | Precedent: 30s dead-owner sweep; `hmnpc_owner<job>` ZDOID keys; `Claim`; `UnClaim` | 91, 121–123, 138–148, 257, 276 |
| `HMSkills.cs` | `LoadSkills` / `SaveSkills` (`hmnpc_skill<name>`, `hmnpc_skill<name>enable`) | 79–80, 107–108 |
| `RescueWhitelist.cs` | 67-name whitelist; `Contains` | — |
| `BiomeData.cs` | 8-biome gear + skill tables (R4, DONE) | — |
| `Hirdmandr.csproj` | **MUST register new `.cs` files** (`<Compile Include="..."/>`) | — |

**Deployed mod:** `D:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\Hirdmandr\Hirdmandr.dll`
(**280,576 bytes, 10/26/2026 11:50** — Fix A + SP fix: player-centered server scanner (peers ∪
local player, `GetScanCenters`); 0 errors / 27 warnings; deployed DLL byte-identical to
`bin\Debug\Hirdmandr.dll`, SHA256 `0148717718B2…` verified).
(The 190,976 → 279,552 growth is the "Massive refactor for 0.9" tree (7f8b741) — thought-string
debugs + combat behaviors — not spawn-system work.)
Prior builds: 190,976 B (10/8 07:35, #26i sitting-pose fix + diagnostics removed; 0 errors),
190,976 B (10/7 20:53, R7 playtest: per-pass scan log removed, `hash=` added to `rescue zdo` +
`rescue list`), 192,000 B (20:10, per-pass log added), 190,464 B (16:42, ledger + scanner
refactor).

**Build command:**
`& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\OpenCode\hirdmandr_project\hirmandr_repo\Hirdmandr.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU" /p:ExecutePrebuild=false /nologo /m`
Post-build auto-deploys to the BepInEx plugins path.

**Old-style .csproj gotcha:** New `.cs` files MUST be added to `Hirdmandr.csproj` as
`<Compile Include="Foo.cs" />` or they won't compile in.

---

## 20. What to remove / keep / add (implementation checklist)

**REMOVE (DONE 2026-10-07):**
- ~~`RescueSpawnHook` (`RescueSpawn.cs` :123–179)~~ — the dead `PlaceLocations` post-hook. **Removed.**
- ~~The 20% *random* roll (`RescueSpawn.cs` :165) and `RescueChance` as a random threshold (:17).~~
  **Removed.**

**KEEP (as-is):**
- `RescueSpawner.PlaceAtLocation` + `PickClearSpot` (`RescueSpawn.cs` :32, :95).
- `RescueWhitelist` (67 names).
- D9 patches: `CharacterPatch.cs` :56, `MonsterAIPatch.cs` :61.
- Soul item: `SoulItem.cs` (registration, `SpawnSoulFromNpc`, payload, persistence).
- The rescue→welcome flow: `Rescue`/`RescueWait`/`WelcomeHome`/`CheckWelcomeRanges`.
- Biome → skills + gear (R4, `BiomeData.cs`, `HMSkills.cs`).
- Persistence model (`HirdmandrNPC` first-time + `LoadState`).

**ADD (ALL DONE 2026-10-07, built + deployed, 0 errors):**
1. **`RescueLedger.cs`** — the ledger: `IsWelcomed`/`SetWelcomed` (`hmnpc_rescue_welcomed_<H>`
   global key), `CollectLive` (derive live from loaded ZDOs), `Claimed(h, live)`. No ZDO, no sweep.
2. **`RescueScanner.cs`** — 5s periodic coroutine (server-only); 3×3 zone grid; for
   each placed+whitelisted PoI: `H = HashFor(name, zone)`; `if (Hits(H) && !Claimed(H, live))`
   `PlaceAtLocation(inst, "scan", H)`. No safety net (live is derived).
3. **Spawn stamping** — `PlaceAtLocation` stamps `hmnpc_rescuepoiHash = H` on the NPC ZDO (and the
   soul's `m_customData` on drop).
4. **Welcome wiring** — `HirdmandrNPC.WelcomeHome()` :1215–1218: `RescueLedger.SetWelcomed(H)`
   (reads H from the NPC's `hmnpc_rescuepoiHash`).
5. **Death wiring** — `HirdmandrNPC.HandleDeath()`: welcomed → drop gear + soul (stamped with H);
   not-welcomed → nothing. No ledger write (dead structure drops out of the live set on its own).
6. **Deterministic 20% gate** — `Hits(H) = (H & 0x7FFFFFFF) % 100 < 20` (§5).
7. **Register new `.cs` files** in `Hirdmandr.csproj` (`RescueLedger.cs` :72, `RescueScanner.cs`
   :73, `RescueSpawn.cs` :74).
8. **Register the scanner** — `Hirdmandr.cs` `Awake` :87 calls `RescueScanner.Start(this)`. (No
   ledger ZDO to create/own.)
9. **Fix A (2026-10-26): player-centered server pass** — `RunPass` :60 loops the scan centers
   → `ScanAround(center)`; `Ready()` :174 drops the local-player gate; `PlaceAtLocation` :18
   drops the local-player requirement (identity rotation :43, `CreateNewZDO` default owner +
   guarded player-owner copy :46–66); in-pass `s_live.Add(h)` :163 on successful spawn
   (overlapping windows). **MP in-game VERIFIED (Thomas, 2026-10-26).**
10. **SP fix (2026-10-26): local-player scan center** — `GetScanCenters()` :84 = peers
   (`GetPeers()` → `GetRefPos()`) ∪ the local player (`Player.m_localPlayer`), 10m dedup; empty
   list = nothing to scan (dedicated MP, nobody connected). Root cause of the SP regression:
   `GetPeers()` does not provide the host's position in SP. `rescue scan` now prints
   `STATE: scanCenters=N` + `CENTER` lines (`HirdmandrCommand.cs` :999–1004).
   **SP retest pending (Thomas).**

**FUTURE (not this refactor):** resurrection UI/flow (soul → new NPC; the new NPC is stamped
`hmnpc_rescuepoiHash = H`, which makes it live again). §18.

**DO NOT** touch the rescue→welcome flow, the D9 patches, the soul item, the biome/gear tables,
or the persistence model — they work and are not part of the spawn refactor.

**After the build:** refresh PMW `current_state` (it's stale) to reflect the new design + the
implementation status.
