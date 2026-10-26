# Socialize — design (the "murmur at the fire")

> Read this FIRST before any socialize work. During the socialize window the NPC idles by an NPC
> fire (campfire/bonfire/hearth) and, over the course of it, says a few lines **appropriate to
> its experience**.
> Process: `D:\qwen\ITERATION.md`. Spawn design: `rescue_spawn_design.md`.

## The model
Each NPC keeps a small queue of *moments* (a topic tag plus a light payload) **on its ZDO,
network-synced** — so whichever player currently owns the NPC can read it. Daytime events drop
moments on; during socialize, while the NPC is **gathered** at the fire (not still walking to it),
it pops one every ~10–25s and says a line from that topic's in-voice pool via the existing `Say`.
When nothing real is pending it says a line from a light campfire-**idle** pool, so the fire
always murmurs.

- **Moment** = topic + payload. Topics: `worked`, `combat`, `guarded`, `skillup` (`idle` is a
  fallback pool, not queued).
- **Dedup:** one pending moment per topic — a long patrol does not stack ten `guarded` moments
  (an enqueue overwrites the same topic).
- **Immersion comes from pool membership + in-voice writing, not templating.** Payloads
  (`{monster}`, `{readable}`) and the **tier** fill the lines at pop time.

## Experience tiers
A skill value sets the *register* of the lines (humble → seasoned → authority). The tier names
below are **inspiration for the voice, not words to say**.
| Tier | Skill value | Register |
|---|---|---|
| 0 apprentice | 0–29 | learning, unsure, first-timers |
| 1 journeyman | 30–59 | competent, routine, steady |
| 2 master | 60–100 | authority, easy, no-nonsense |

`TierFor(v) = v < 30 ? 0 : v < 60 ? 1 : 2`. Tier source per topic:
`worked` → that job's skill; `combat`/`guarded` → `fighter`; `skillup` → the skill that just rose.

## Moments (topics)
| Topic | Source hook (already has the context) | Payload | Pool key | Tier from |
|---|---|---|---|---|
| `worked` | WorkDay job tick | job (`hmAI.curJob`) | (job, tier) | that job's skill |
| `combat` | enter Threatened | monster name, persona (melee/range) | (persona, tier) | `fighter` |
| `guarded` | enter Patrol | — | single pool | `fighter` (light) |
| `skillup` | `HMSkills.ModifySkill` level-up (line 180) | skill key + readable + level | (tier), with `{readable}` | the risen skill |

Pools with a `{monster}`/`{readable}` placeholder are filled at pop time. `worked` is the big lever
(6 artisan jobs × 3 tiers); the rest are small. If a job has no pool, fall back to a generic line.

## The talk loop
- Lives in `topSM.NodeSocialize.RunState` (runs every ~0.5s during socialize) → calls
  `hmNPC.m_socialize.MaybeTalk()`.
- `MaybeTalk()`: if `Time.time >= m_nextTalk` **and** `!hmAI.moveToTrying` (gathered, not walking):
  pop a moment (read + clear its ZDO field; or `idle` if none), pick a line (skip the last 1–2
  said from that pool), `Say(line)`, then re-roll `m_nextTalk = Time.time + Random(10, 25)`.
- `Time.time`-based → freezes on pause (matches every other timer here).
- **Stagger:** initialize `m_nextTalk` with a random phase so several NPCs at one fire do not
  bubble in unison; the 10–25s random interval + per-pool no-repeat makes it feel like a camp.
- **Saying:** existing `hmNPC.Say(line)` (bubble, 10m cull → only nearby players see it).

## Where it lives
- **New file `HMSocialize.cs`** (component, mirrors `HMSkills`): the ZDO-backed queue, `MaybeTalk()`,
  the tier helper, and the static pools. **Add to `Hirdmandr.csproj`**
  (`<Compile Include="HMSocialize.cs" />`) — new files are silently dropped from the assembly
  otherwise (CS0103).
- **Queue storage: 4 ZDO string fields on the NPC's ZDO, one per topic** —
  `hmnpc_moment_worked`, `hmnpc_moment_combat`, `hmnpc_moment_guarded`, `hmnpc_moment_skillup`.
  Each holds the payload (empty string = no pending moment). Enqueue = `Set` (overwrite → dedup for
  free). Pop (in `MaybeTalk`, on the owner) = pick a pending topic, fill the line from the payload,
  `Say`, clear the field. ZDO fields are **network-synced**, so the queue is accessible to whichever
  player owns the NPC at talk time (multiplayer-correct). Fresh NPC = fields absent = empty.
- **Local (in-memory, per owner):** the talk-cadence timer (`m_nextTalk`) and last-line-said dedup.
  These are cosmetic and fine to reset when ownership transfers (a new owner just resumes at a
  normal rate). `Time.time` is per-machine, so the cadence is deliberately local — not a synced
  timestamp.
- **Event hooks (one-liners, fire on the owner):** WorkDay job tick → `EnqueueMoment(worked,
  curJob)`; Threatened entry → `EnqueueMoment(combat, monsterName, persona)`; Patrol entry →
  `EnqueueMoment(guarded)`; `ModifySkill` level-up → `EnqueueMoment(skillup, skillKey, readable,
  level)`.

## Voice sample (starting set — refine with Thomas)
The lines are the feature; Qwen writes a strong starting set, Thomas shapes the voice. The tier
changes the *register*, never the facts.
- **worked / furnaceoperator** — apprentice: *"The furnace bites today. More wood gone than iron
  made."* · journeyman: *"Got the heat even this morning. Iron comes off clean."* · master:
  *"You feed it right and it gives back. I've had this furnace a long while."*
- **worked / farmer** — apprentice: *"The barley's slow. I keep turning the soil and hoping."*
- **worked / mender** — master: *"A mended road's a kind of strength. I keep them passable."*
- **combat / {monster} / melee** — journeyman: *"The {monster} came at dusk. I put it down."* ·
  master: *"A {monster} crossed the field. It's ash now."*
- **combat / {monster} / range** — journeyman: *"I brought down a {monster} from the treeline."*
- **guarded** — *"Quiet watch. I fed the fire and kept my eyes on the trees."*
- **skillup / {readable}** — journeyman: *"{readable} is coming along. Easier each day."*
- **idle** — *"Cold night. Good fire."* · *"The road's quiet."* · *"I miss the sea, I think."*

## Defaults I'm taking (all trivially changeable)
- **Monster name:** the `combat` moment carries the threat's name mapped from its prefab
  (Wolf/Drake/Troll/Elder/…). If the threat isn't cleanly identifiable, use a nameless line.
- **Guarded:** single in-voice pool for the MVP (a duty, not a trade); easy to tier by `fighter` later.
- **Cull distance:** keep `Say`'s 10m default (you must be near the fire to see the bubble).

## Build checklist
- [ ] `HMSocialize.cs`: ZDO-backed queue (4 string fields), `MaybeTalk`, tier helper, static pools,
      `{...}` fill.
- [ ] Add `HMSocialize.cs` to `Hirdmandr.csproj`.
- [ ] `HirdmandrNPC`: attach `m_socialize` (like `m_skills`); fresh NPC = fields absent = empty.
- [ ] 4 event hooks (WorkDay job tick, Threatened entry, Patrol entry, `ModifySkill` level-up).
- [ ] `topSM.NodeSocialize.RunState` → `m_socialize.MaybeTalk()`, gated on `!moveToTrying`.
- [ ] Starter pools for every topic/tier/persona; `{monster}`/`{readable}` fill.
- [ ] Build (0 errors), deploy, in-game: spawn a few, run a day, gather at the fire, confirm lines
      match job/tier/persona and the fire stays murmuring.

## Open (for Thomas)
- **Persistence side-effect (ZDO = saved):** the moment fields now persist to the world save, so a
  moment survives save/load (overrides the earlier "in-memory only" choice). I assess it's fine —
  Valheim preserves time-of-day across save/load, so a "worked today" moment is still valid, and it
  matches skills (which also persist). Options: **let it persist** (recommended, simpler) vs
  **clear-on-load** (fresh queue each load; small addition). Confirm which.
- Voice: the sample lines above are a starting voice — shape the tone/register as you review.
- Anything to cut from the MVP scope before I build.
