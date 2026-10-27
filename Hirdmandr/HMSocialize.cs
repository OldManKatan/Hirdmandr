using System;
using System.Collections.Generic;
using UnityEngine;


namespace Hirdmandr
{
    // Socialize: the "murmur at the fire." While an NPC is gathered at the socialize fire (not
    // still walking to it) it pops one *moment* and says a single in-voice line from that topic's
    // pool. Daytime events enqueue moments on the NPC's ZDO (network-synced, so whichever player
    // owns the NPC at talk time can read them); when nothing real is pending it falls back to a
    // light campfire-idle pool, so the fire always murmurs.
    //
    // Mirrors HMSkills: a plain (non-MonoBehaviour) helper bound to the NPC's ZNetView, created in
    // HirdmandrNPC.Awake. The queue is 4 ZDO string keys (one per topic; empty = none pending);
    // an enqueue is a Set (overwrite = free per-topic dedup). Cadence + last-line dedup are local
    // (per-owner, cosmetic) and Time.time-based, so they freeze on pause.
    [Serializable]
    public class HMSocialize
    {
        public ZNetView m_znetv;
        public HirdmandrNPC m_hmnpc;

        // ZDO keys: one pending moment per topic. The stored value is the payload ("" = none).
        const string kWorked = "hmnpc_moment_worked";
        const string kCombat = "hmnpc_moment_combat";
        const string kGuarded = "hmnpc_moment_guarded";
        const string kSkillup = "hmnpc_moment_skillup";

        // Pool keys (see BuildPools).
        const string kIdlePool = "i";

        // Local (per-owner, cosmetic) talk state. Time.time-based so it freezes on pause.
        private float m_nextTalk;
        // Recent line indexes per pool key (newest first, capped at 2) so the fire does not repeat
        // the same line twice in a row.
        private readonly Dictionary<string, List<int>> m_recent = new Dictionary<string, List<int>>();

        // All pools, keyed as documented in BuildPools. Shared by every NPC (static, built once).
        static readonly Dictionary<string, string[]> m_pools = BuildPools();

        public HMSocialize(HirdmandrNPC npc, ZNetView znet)
        {
            m_hmnpc = npc;
            m_znetv = znet;
            // Safety-net first-talk time. Normally StaggerGather re-anchors this at the moment the
            // NPC gathers at the fire (the real anti-sync); this only matters if an NPC is somehow
            // already socializing at spawn.
            m_nextTalk = Time.time + UnityEngine.Random.Range(5f, 30f);
        }

        // ---- Enqueue (owner-only; the state machine that fires these runs on the owner). ----
        // Overwriting the same topic is the dedup: a long patrol does not stack ten "guarded".

        public void EnqueueWorked(string job)
        {
            if (job == null || job.Length == 0) return;
            Set(kWorked, job);
        }

        public void EnqueueCombat(string monsterName, string persona)
        {
            Set(kCombat, (monsterName == null ? "" : monsterName) + "|" + persona);
        }

        public void EnqueueGuarded()
        {
            Set(kGuarded, "1");
        }

        public void EnqueueSkillup(string skillKey, string readable, int level)
        {
            Set(kSkillup, skillKey + "|" + (readable == null ? "" : readable) + "|" + level);
        }

        private void Set(string key, string payload)
        {
            var zdo = m_znetv.GetZDO();
            if (zdo == null) return;
            zdo.Set(key, payload);
        }

        // Re-anchor the first talk to the moment this NPC gathers at the fire, so each NPC's
        // first line lands in a 25s window from arrival rather than all at once. The constructor's
        // m_nextTalk is anchored to spawn time, which is long past by the time an NPC walks in --
        // without this, every first line would fire on the first tick of the "socialize" state
        // (a synchronized burst). Called from NodeSocialize.EnterFrom; socialize is index 3, so
        // EnterFrom fires on every entry into the steady state.
        public void StaggerGather()
        {
            m_nextTalk = Time.time + UnityEngine.Random.Range(0f, 25f);
        }

        // ---- The talk loop (SocializeSM's "socialize" steady state calls this ~0.5s while gathered). ----

        public void MaybeTalk()
        {
            var zdo = m_znetv.GetZDO();
            if (zdo == null) return;

            // Only talk while gathered at the fire, not still walking to it.
            var ai = m_hmnpc.m_hirdmandrAI;
            if (ai != null && ai.moveToTrying) return;

            if (Time.time < m_nextTalk) return;

            // Gather whichever moments are pending (topic key + its payload).
            string worked = zdo.GetString(kWorked, "");
            string combat = zdo.GetString(kCombat, "");
            string guarded = zdo.GetString(kGuarded, "");
            string skillup = zdo.GetString(kSkillup, "");

            var keys = new List<string>();
            var payloads = new List<string>();
            if (worked.Length > 0) { keys.Add(kWorked); payloads.Add(worked); }
            if (combat.Length > 0) { keys.Add(kCombat); payloads.Add(combat); }
            if (guarded.Length > 0) { keys.Add(kGuarded); payloads.Add(guarded); }
            if (skillup.Length > 0) { keys.Add(kSkillup); payloads.Add(skillup); }

            string line;
            if (keys.Count > 0)
            {
                // Pick one pending moment at random (interleaves topics like a camp conversation),
                // pop it (clear its ZDO field), then fill + say its line.
                int pick = UnityEngine.Random.Range(0, keys.Count);
                string key = keys[pick];
                string payload = payloads[pick];
                zdo.Set(key, "");
                line = ComposeLine(key, payload);
            }
            else
            {
                // Nothing real pending: the fire still murmurs (light idle pool).
                line = PickLine(kIdlePool);
            }

            // 30f cull: Chat.UpdateNpcTexts hides AND destroys the bubble once the camera is
            // beyond cullDistance, so the default 10f made far-away lines vanish in <1s.
            // 30f keeps the murmur readable across the fire/base (matches the portal/gate lines).
            m_hmnpc.Say(line, 30f);
            // Re-roll the interval (30-90s) so the next line is its own beat. Widened from 20-40 so
            // the ongoing murmur stays spread instead of stacking: at 8 NPCs the expected
            // simultaneous speakers (N*5s/avgP) is ~0.67 and P(2+ at once) ~15%; the wide 30s
            // half-width also keeps each NPC's cycle from re-aligning with the others.
            m_nextTalk = Time.time + UnityEngine.Random.Range(30f, 90f);
        }

        // GUI peek (the manage panel's "Say" line): a line the fire might be murmuring right
        // now, WITHOUT popping. Pick one pending moment at random and compose its line; with
        // nothing queued, fall back to the idle pool like MaybeTalk does. Read-only on the
        // ZDO - the moments stay queued for the fire to actually say them later.
        public string PeekMurmur()
        {
            var zdo = m_znetv.GetZDO();
            if (zdo == null) return null;

            string worked = zdo.GetString(kWorked, "");
            string combat = zdo.GetString(kCombat, "");
            string guarded = zdo.GetString(kGuarded, "");
            string skillup = zdo.GetString(kSkillup, "");

            var keys = new List<string>();
            var payloads = new List<string>();
            if (worked.Length > 0) { keys.Add(kWorked); payloads.Add(worked); }
            if (combat.Length > 0) { keys.Add(kCombat); payloads.Add(combat); }
            if (guarded.Length > 0) { keys.Add(kGuarded); payloads.Add(guarded); }
            if (skillup.Length > 0) { keys.Add(kSkillup); payloads.Add(skillup); }

            if (keys.Count > 0)
            {
                int pick = UnityEngine.Random.Range(0, keys.Count);
                return ComposeLine(keys[pick], payloads[pick]);
            }
            return PickLine(kIdlePool);
        }

        // Resolve a pending (topic, payload) to a pool key and return the filled line.
        private string ComposeLine(string key, string payload)
        {
            string monster = "";
            string readable = "";
            string poolKey;

            if (key == kWorked)
            {
                // payload = job key. Tier from that job's skill; generic pool if the job has none.
                string job = payload;
                int tier = TierFor(SkillValue(job));
                poolKey = "w:" + job + ":" + tier;
                if (!m_pools.ContainsKey(poolKey)) poolKey = "w:*:" + tier;
            }
            else if (key == kCombat)
            {
                // payload = "monster|persona". Tier from fighter. A named pool names the monster;
                // if we could not cleanly identify it, use a nameless pool.
                var parts = payload.Split('|');
                monster = parts.Length > 0 ? parts[0] : "";
                string persona = parts.Length > 1 ? parts[1] : "melee";
                int tier = TierFor(SkillValue("fighter"));
                poolKey = (monster.Length > 0) ? ("c:" + persona + ":" + tier) : ("cn:" + tier);
            }
            else if (key == kGuarded)
            {
                // Single pool: a duty, not a trade.
                poolKey = "g";
            }
            else // kSkillup
            {
                // payload = "skillKey|readable|level". Tier from the risen skill's level; {readable}
                // fill.
                var parts = payload.Split('|');
                readable = parts.Length > 1 ? parts[1] : "";
                int level;
                level = (parts.Length > 2 && int.TryParse(parts[2], out level)) ? level : 0;
                poolKey = "s:" + TierFor(level);
            }

            string line = PickLine(poolKey);
            if (monster.Length > 0) line = line.Replace("{monster}", monster);
            if (readable.Length > 0) line = line.Replace("{readable}", readable);
            return line;
        }

        // Pick a line from a pool, preferring one not said in the last couple of picks from that pool.
        private string PickLine(string poolKey)
        {
            string[] pool;
            if (!m_pools.TryGetValue(poolKey, out pool) || pool == null || pool.Length == 0)
            {
                // Defensive only: every referenced key has a pool in BuildPools.
                return "…";
            }

            int n = pool.Length;
            List<int> recent;
            m_recent.TryGetValue(poolKey, out recent);
            if (recent == null) recent = new List<int>();

            // Candidates = lines not said recently; if every line is recent, just pick any.
            var cand = new List<int>();
            for (int i = 0; i < n; i++)
            {
                if (!recent.Contains(i)) cand.Add(i);
            }
            int idx;
            if (cand.Count > 0) idx = cand[UnityEngine.Random.Range(0, cand.Count)];
            else idx = UnityEngine.Random.Range(0, n);

            // Record this index as most-recent (keep only the last 2).
            recent.Remove(idx);
            recent.Insert(0, idx);
            while (recent.Count > 2) recent.RemoveAt(recent.Count - 1);
            m_recent[poolKey] = recent;

            return pool[idx];
        }

        private float SkillValue(string skill)
        {
            var s = m_hmnpc.m_skills;
            return (s != null) ? s.GetSkill(skill) : 0f;
        }

        // Register, not a spoken word: 0 apprentice (learning), 1 journeyman (steady), 2 master
        // (authority). Mirrors the design's TierFor.
        public static int TierFor(float v)
        {
            return v < 30f ? 0 : (v < 60f ? 1 : 2);
        }

        // ---- The voice. ----
        // The lines are the feature. Pool keys:
        //   w:<job>:<tier>  worked, per job x tier  (w:*:<tier> = generic fallback)
        //   c:<persona>:<tier>  combat, names the monster ({monster})
        //   cn:<tier>  combat, nameless (threat not cleanly identifiable)
        //   g  guarded, single pool
        //   s:<tier>  skillup, {readable} fill
        //   i  idle fallback
        // Register per tier: 0 humble/first-timer, 1 competent/routine, 2 worldly/authority.
        static Dictionary<string, string[]> BuildPools()
        {
            var d = new Dictionary<string, string[]>();

            // ---- worked: mender ----
            d["w:mender:0"] = new[]
            {
                "I'm still learning the road. Some stones I've got to set twice.",
                "The bridge holds. I think. I've checked it twice now."
            };
            d["w:mender:1"] = new[]
            {
                "Mended the path this morning. Keeps it passable for the carts.",
                "Re-laid the fallen stones. The road's steady again."
            };
            d["w:mender:2"] = new[]
            {
                "A mended road's a kind of strength. I keep them passable.",
                "I've set this stretch a hundred times. It'll hold a winter."
            };

            // ---- worked: lamplighter ----
            d["w:lamplighter:0"] = new[]
            {
                "First time trimming so many wicks. I kept the oil close.",
                "The beacons are lit. I hope the wicks hold through the night."
            };
            d["w:lamplighter:1"] = new[]
            {
                "Walked the line and refilled every lamp. They burn even.",
                "Trimmed the wicks and fed the oil. The light holds."
            };
            d["w:lamplighter:2"] = new[]
            {
                "Every light on the line is lit. I don't leave a dark gap.",
                "You keep the lamps full and the night keeps its distance."
            };

            // ---- worked: woodburner ----
            d["w:woodburner:0"] = new[]
            {
                "The kiln's a slow friend. I'm still learning its temper.",
                "Packed the kiln tight. I'm hoping for charcoal, not ash."
            };
            d["w:woodburner:1"] = new[]
            {
                "Ran the kiln clean this morning. Good charcoal, little waste.",
                "Fed the kiln steady. The charcoal comes off dark and clean."
            };
            d["w:woodburner:2"] = new[]
            {
                "You coax charcoal out of plain wood. I've had this kiln a long while.",
                "The kiln gives back what you put in. I put in patience."
            };

            // ---- worked: furnaceoperator ----
            d["w:furnaceoperator:0"] = new[]
            {
                "The furnace bites today. More wood gone than iron made.",
                "I'm still reading the heat. The iron's coming, slow."
            };
            d["w:furnaceoperator:1"] = new[]
            {
                "Got the heat even this morning. Iron comes off clean.",
                "Kept the fire honest all morning. The bloom's good."
            };
            d["w:furnaceoperator:2"] = new[]
            {
                "You feed it right and it gives back. I've had this furnace a long while.",
                "The iron's ready. Learn the fire's moods and it serves you."
            };

            // ---- worked: farmer ----
            d["w:farmer:0"] = new[]
            {
                "The barley's slow. I keep turning the soil and hoping.",
                "First crop for me. I turn the rows and pray for rain."
            };
            d["w:farmer:1"] = new[]
            {
                "Turned the soil and kept the rows clean. The barley's coming up.",
                "A good tending day. The crop's steady in the ground."
            };
            d["w:farmer:2"] = new[]
            {
                "The field answers when you work it right. I've fed this ground a while.",
                "You don't rush a crop. You keep at it, and it comes."
            };

            // ---- worked: gatherer ----
            d["w:gatherer:0"] = new[]
            {
                "Gathered what the land gave. My hands are still new to it.",
                "Picked the berries and carried the wood. I'm learning the places."
            };
            d["w:gatherer:1"] = new[]
            {
                "Walked the field and gathered what's ready. The store's fuller.",
                "Tended the woodpile and the stores. Enough for the week."
            };
            d["w:gatherer:2"] = new[]
            {
                "The land gives to those who know where to look. I know the places.",
                "I know when the wood's worth cutting. Learn that, or you starve."
            };

            // ---- worked: generic fallback (a job with no dedicated pool) ----
            d["w:*:0"] = new[]
            {
                "Put in a day's work. My hands are sore in the right way.",
                "Got what I set out to do. Mostly. I'll do it again tomorrow."
            };
            d["w:*:1"] = new[]
            {
                "A day's work done. The kind that shows itself later.",
                "Kept at it all morning. It's the honest sort of tiring."
            };
            d["w:*:2"] = new[]
            {
                "Worked the day through. You feel it in the shoulders by evening.",
                "Another day's labor done. The sort that builds a home."
            };

            // ---- combat: melee, names the monster ----
            d["c:melee:0"] = new[]
            {
                "A {monster} came at me. I held the line, barely.",
                "The {monster} got close. My arms are shaking, but it's dead."
            };
            d["c:melee:1"] = new[]
            {
                "The {monster} came at dusk. I put it down.",
                "Met a {monster} on the path. It's ash now."
            };
            d["c:melee:2"] = new[]
            {
                "A {monster} crossed the field. It's ash now.",
                "The {monster} thought it had me. It doesn't."
            };

            // ---- combat: range, names the monster ----
            d["c:range:0"] = new[]
            {
                "I brought down a {monster}. My hands were steady, I think.",
                "A {monster} showed up. I loosed off before it reached me."
            };
            d["c:range:1"] = new[]
            {
                "I brought down a {monster} from the treeline.",
                "Saw the {monster} early. One shot, it fell."
            };
            d["c:range:2"] = new[]
            {
                "The {monster} never knew I was there. It's already ash.",
                "I count the {monster}s I've dropped from range. That one was easy."
            };

            // ---- combat: nameless (could not cleanly identify the threat) ----
            d["cn:0"] = new[]
            {
                "Something came for me today. I'm still standing.",
                "I met a beast on the road. I held my ground."
            };
            d["cn:1"] = new[]
            {
                "There was trouble on the path. Handled.",
                "A beast came at dusk. It's dealt with."
            };
            d["cn:2"] = new[]
            {
                "Something crossed my watch. It's ash now.",
                "The road had a threat. It's gone."
            };

            // ---- guarded: single pool (a duty) ----
            d["g"] = new[]
            {
                "Quiet watch. I fed the fire and kept my eyes on the trees.",
                "Stood the post. Nothing came, and that's the whole point.",
                "Kept the watch. The night stayed quiet, which is its own reward.",
                "Walked the perimeter twice. All sound. I like it that way.",
                "A quiet shift on guard. The fire's low, the dark is still."
            };

            // ---- skillup: names the risen skill ----
            d["s:0"] = new[]
            {
                "{readable} is a new friend. I'm only just getting a feel for it.",
                "I'm learning {readable}. A little easier today than yesterday."
            };
            d["s:1"] = new[]
            {
                "{readable} is coming along. Easier each day.",
                "I've gotten steady at {readable}. The work shows."
            };
            d["s:2"] = new[]
            {
                "{readable} is second nature now. You can't rush that, but I didn't.",
                "I've turned my hand to {readable}. It shows in the work."
            };

            // ---- idle: the fire always murmurs ----
            d[kIdlePool] = new[]
            {
                "Cold night. Good fire.",
                "The road's quiet.",
                "I miss the sea, I think.",
                "The fire's low and honest.",
                "Dark's come in. Good for a rest of the eyes.",
                "Not much to say for a night like this."
            };

            return d;
        }
    }
}
