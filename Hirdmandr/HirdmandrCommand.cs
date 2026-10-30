// Hirdmandr
// /hirdmandr spawn - spawn the (otherwise never-spawned) npc_entity prefab near the
// calling player. This unblocks in-game testing of the NPC: the mod registers the prefab
// but previously had no code path to actually place one in the world.

using System.Collections.Generic;
using Jotunn.Entities;
using UnityEngine;

namespace Hirdmandr
{
    internal class HirdmandrSpawnCommand : ConsoleCommand
    {
        public override string Name => "hirdmandr";

        public override string Help => "hirdmandr spawn - spawn a Hirdmandr NPC near the calling player; hirdmandr destroy - destroy all Hirdmandr NPCs; hirdmandr list - list each Hirdmandr NPC with its distance from the player and cardinal direction (N/S/E/W), one per line (leash test aid); hirdmandr monster <skeleton|draugr|goblinarcher|goblin> - spawn a test-rig monster near the calling player; hirdmandr move [stat|run <0|1>|walk <0|1>|speed|runspeed|walkspeed|crouchspeed|accel|turn <float>] - live movement diagnostics/tuning for all Hirdmandr NPCs; hirdmandr rescue [scan|catalog|ship|close|find <name>|spawn|list|zdo] - rescue diagnostics: placed PoI locations near player / full location catalog / ship + rigidbody dump / nearest placed PoI by name / compass direction+distance to a named PoI / force-place a rescue in the nearest placed whitelisted PoI / list unrescued rescues / ZDOMan census of all npc_entity ZDOs (near+far); hirdmandr soulsoul - drop a test soul item in front of the player (R3c pickable-soul foundation); hirdmandr soulinspect - dump the Eitr source prefab's GameObject/component tree to the log (diagnostic); hirdmandr combatstats <value> - set fighter/melee/range on all loaded Hirdmandr NPCs to <value> (0-100); hirdmandr skill <mender|lamplighter|woodburner|furnaceoperator|farmer|gatherer> <value> - set that job skill to <value> (0-100) on all loaded Hirdmandr NPCs (job skill test); hirdmandr chestscan - dump every loaded chest (Piece+Container) with its IsPlacedByPlayer flag and every loaded pickable's within-40m-of-a-placed-chest zone membership (gather safe-zone / Hole 1 flag-persistence test); hirdmandr gather - dump each Hirdmandr NPC's gather scan cache (role/job/following flags, pickable/anchor/excluded counts, nearest eligible target)";

        // Non-cheat so it works without enabling cheats (testing); networked so it executes
        // on the server in multiplayer (in singleplayer the local player IS the server, so the
        // caller's position is available).
        public override bool IsCheat => false;
        public override bool IsNetwork => true;
        public override bool OnlyServer => false;
        public override bool IsSecret => false;

        public override void Run(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Log("Usage: /hirdmandr [spawn|destroy]");
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "spawn":
                    SpawnNearCaller();
                    break;
                case "destroy":
                    DestroyAllNPCs();
                    break;
                case "list":
                    ListNPCs();
                    break;
                case "monster":
                    SpawnMonsterNearCaller(args);
                    break;
                case "move":
                    MoveTest(args);
                    break;
                case "rescue":
                    RescueTest(args);
                    break;
                case "soulsoul":
                    SpawnSoulTest();
                    break;
                case "combatstats":
                    SetCombatStats(args);
                    break;
                case "skill":
                    SetJobSkill(args);
                    break;
                case "soulinspect":
                    SoulItem.InspectSoulPrefab();
                    break;
                case "farmscan":
                    FarmScan();
                    break;
                case "chestscan":
                    ChestScan();
                    break;
                case "gather":
                    GatherDump();
                    break;
                default:
                    Log("Unknown subcommand: " + args[0]);
                    Log("Usage: /hirdmandr [spawn|destroy|list|monster <name>|move ...|rescue ...|soulsoul|soulinspect|combatstats <value>|skill <job> <value>|farmscan|chestscan|gather]");
                    break;
            }
        }

        private static void Log(string message)
        {
            Jotunn.Logger.LogInfo("[Hirdmandr] " + message);
        }

        // /hirdmandr list - list each Hirdmandr NPC in the world, one per line, with its distance
        // from the local player and a cardinal direction (N/S/E/W). Test aid for the follow leash
        // (D6 = 100m): the vanilla game has no distance readout, so this shows how close each NPC
        // is, so the 100m leash threshold can be observed. Distance is the same 3D magnitude the
        // leash uses (Vector3.Distance), so it predicts exactly when the leash fires.
        private static void ListNPCs()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Log("/hirdmandr list: no local player (run from a client in singleplayer).");
                    return;
                }
                Vector3 p = player.transform.position;
                var npcs = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
                int total = 0;
                foreach (var npc in npcs)
                {
                    if (npc == null || npc.transform == null)
                    {
                        continue;
                    }
                    total++;
                    Vector3 to = npc.transform.position - p;
                    string name = (npc.m_humanoid != null && !string.IsNullOrEmpty(npc.m_humanoid.m_name))
                        ? npc.m_humanoid.m_name
                        : "?";
                    Log(string.Format("  {0}  {1:F1}m  {2}  pos=({3:F1},{4:F1},{5:F1})",
                        name, to.magnitude, Cardinal(to), to.x, to.y, to.z));
                }
                Log(string.Format("LIST: {0} hirdmandr NPC(s); player pos=({1:F1},{2:F1},{3:F1}).",
                    total, p.x, p.y, p.z));
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.ListNPCs threw:\n" + e);
                throw;
            }
        }

        // /hirdmandr combatstats <value> - coarse scaling test: set fighter/melee/range to the
        // same value on every loaded HirdmandrNPC and re-apply the derived combat stats.
        private static void SetCombatStats(string[] args)
        {
            try
            {
                if (args == null || args.Length < 2)
                {
                    Log("Usage: /hirdmandr combatstats <value 0-100>");
                    return;
                }

                float value;
                if (!float.TryParse(args[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                {
                    Log("combatstats: could not parse '" + args[1] + "' as a number.");
                    return;
                }
                if (value < 0f)
                {
                    value = 0f;
                }
                if (value > 100f)
                {
                    value = 100f;
                }

                var npcs = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
                int total = 0;
                foreach (var npc in npcs)
                {
                    if (npc == null || npc.m_skills == null)
                    {
                        continue;
                    }

                    string name = (npc.m_humanoid != null && !string.IsNullOrEmpty(npc.m_humanoid.m_name))
                        ? npc.m_humanoid.m_name
                        : "?";
                    npc.m_skills.SetSkillValue("fighter", value);
                    npc.m_skills.SetSkillValue("melee", value);
                    npc.m_skills.SetSkillValue("range", value);
                    total++;
                    Log(string.Format("  {0}: fighter/melee/range = {1}", name, value.ToString("0.##")));
                }

                Log(string.Format("COMBAT STATS: {0} hirdmandr NPC(s) set to fighter/melee/range = {1}", total, value.ToString("0.##")));
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.SetCombatStats threw:\n" + e);
                throw;
            }
        }

        // /hirdmandr skill <mender|lamplighter|woodburner|furnaceoperator|farmer|gatherer> <value> - artisan
        // cooldown test setter: set the named artisan skill to an exact value (0-100) on every
        // loaded HirdmandrNPC, bypassing the leveled ModifySkill math (and persisting to the ZDO).
        // The cooldown formula is 18s - (value/100 * 18s), so 0 -> 18s, 50 -> 9s, 100 -> instant.
        // Affects the NEXT job cycle (the running cooldown keeps its old value until it elapses).
        private static void SetJobSkill(string[] args)
        {
            try
            {
                if (args == null || args.Length < 3)
                {
                    Log("Usage: /hirdmandr skill <mender|lamplighter|woodburner|furnaceoperator|farmer|gatherer> <value 0-100>");
                    return;
                }

                string job = args[1].ToLowerInvariant();
                string[] validJobs = { "mender", "lamplighter", "woodburner", "furnaceoperator", "farmer", "gatherer" };
                if (System.Array.IndexOf(validJobs, job) < 0)
                {
                    Log("skill: unknown job '" + args[1] + "'. Valid jobs: mender | lamplighter | woodburner | furnaceoperator | farmer | gatherer.");
                    return;
                }

                float value;
                if (!float.TryParse(args[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                {
                    Log("skill: could not parse '" + args[2] + "' as a number.");
                    return;
                }
                if (value < 0f)
                {
                    value = 0f;
                }
                if (value > 100f)
                {
                    value = 100f;
                }

                var npcs = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
                int total = 0;
                foreach (var npc in npcs)
                {
                    if (npc == null || npc.m_skills == null)
                    {
                        continue;
                    }

                    string name = (npc.m_humanoid != null && !string.IsNullOrEmpty(npc.m_humanoid.m_name))
                        ? npc.m_humanoid.m_name
                        : "?";
                    float old = npc.m_skills.GetSkill(job);
                    npc.m_skills.SetSkillValue(job, value);
                    total++;
                    Log(string.Format("  {0}: {1} = {2} (was {3})", name, job, value.ToString("0.##"), old.ToString("0.##")));
                }

                Log(string.Format("SKILL SET: {0} hirdmandr NPC(s) {1} = {2}", total, job, value.ToString("0.##")));
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.SetJobSkill threw:\n" + e);
                throw;
            }
        }

        // Cardinal direction (N/S/E/W) of a world-space delta, using Valheim's map convention
        // (north = +Z, east = +X, south = -Z, west = -X). Returns the dominant axis, so a
        // diagonal reads as N/S or E/W rather than, e.g., NE.
        private static string Cardinal(Vector3 to)
        {
            if (Mathf.Abs(to.x) > Mathf.Abs(to.z))
            {
                return to.x > 0f ? "E" : "W";
            }
            return to.z > 0f ? "N" : "S";
        }

        private static void SpawnNearCaller()
        {
            try
            {
            // The caller is the local player in singleplayer (listen server). A dedicated
            // server has no local player, so the caller's position cannot be inferred here
            // yet (deferred) - report it instead of throwing.
            Player player = Player.m_localPlayer;
            if (player == null || player.transform == null)
            {
                Log("/hirdmandr spawn: no local player (run from a client in singleplayer); "
                    + "dedicated-server caller position is not supported yet.");
                return;
            }

            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                Log("/hirdmandr spawn: ZNetScene.instance is null (not in a level).");
                return;
            }

            // The npc_entity prefab is registered by the mod (Hirdmandr.CreateNPCPlayer) via
            // Jotunn's PrefabManager, which forwards it to Valheim's ZNetScene prefab table.
            GameObject prefab = scene.GetPrefab("npc_entity");
            if (prefab == null)
            {
                Log("/hirdmandr spawn: 'npc_entity' prefab is not registered yet.");
                return;
            }

            // Spawn a networked instance offset to the side of the caller (so it is not
            // inside them).
            Vector3 position = player.transform.position + player.transform.right * 2f;
            Quaternion rotation = player.transform.rotation;
            int prefabHash = scene.GetPrefabHash(prefab);

            ZDO zdo = ZDOMan.instance.CreateNewZDO(position, prefabHash);
            zdo.SetPrefab(prefabHash);
            zdo.SetRotation(rotation);

            // CreateNewZDO does NOT establish ownership: the ZDO is created ownerless, so
            // HirdmandrNPC.Awake takes the non-owner branch (no randomized appearance or
            // skills, no sit emote, and MonsterAI left enabled -> a wandering raw monster).
            // Claim the local session as owner so the owner-only setup runs. The local
            // player's own character ZDO is owned by the local session, so we copy that owner.
            var playerView = player.m_nview;
            var playerZdo = (playerView != null) ? playerView.GetZDO() : null;
            if (playerZdo != null)
            {
                zdo.SetOwner(playerZdo.GetOwner());
            }
            else
            {
                Log("/hirdmandr spawn: could not determine local session; NPC will spawn as non-owner.");
            }

            // CreateObject instantiates the prefab at the ZDO's position and binds the prefab's
            // ZNetView to the ZDO (via the static init-ZDO mechanism); AddInstance then registers
            // the (ZDO, ZNetView) pair in the scene so it is tracked and synced.
            GameObject instance = scene.CreateObject(zdo);
            if (instance == null)
            {
                Log("/hirdmandr spawn: CreateObject failed for 'npc_entity' (prefab hash "
                    + prefabHash + ").");
                return;
            }

            scene.AddInstance(zdo, instance.GetComponent<ZNetView>());

            // Attach the self-terminating diagnostic that logs the local player's control state
            // (and this NPC's AI state) once per second, so we can see what freezes the local
            // player the moment the NPC exists. It dies with the NPC (e.g. via /hirdmandr destroy).
            var npc = instance.GetComponent<HirdmandrNPC>();
            if (npc != null)
            {
                var diag = instance.AddComponent<HirdmandrDiag>();
                diag.m_localPlayerAtSpawn = player;
            }

            Log("Spawned hirdmandr NPC near the caller (ZDO " + zdo.m_uid + ").");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.SpawnNearCaller threw:\n" + e);
                throw;
            }
        }

        private static void DestroyAllNPCs()
        {
            try
            {
            var npcs = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
            int count = 0;
            foreach (var npc in npcs)
            {
                if (npc == null)
                {
                    continue;
                }
                // Destroy via ZNetScene so the ZDO is removed from the world (and replicated)
                // instead of orphaning the ZDO with Object.Destroy(gameObject) (review F3).
                if (npc.m_znet != null && npc.m_znet.GetZDO() != null)
                {
                    ZNetScene.instance.Destroy(npc.gameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(npc.gameObject);
                }
                count++;
            }
            Log("Destroyed " + count + " hirdmandr NPC instance(s).");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.DestroyAllNPCs threw:\n" + e);
                throw;
            }
        }

        // Test-rig monsters: the mod registers four debug monster prefabs (see Hirdmandr.DebugMons)
        // so combat/AI can be exercised on demand. They are NOT auto-spawned anywhere; this
        // subcommand is the only code path that places one in the world.
        private static readonly string[] m_debugMonsterAliases = { "skeleton", "draugr", "goblinarcher", "goblin" };

        private static string ResolveDebugMonsterPrefab(string alias)
        {
            switch (alias)
            {
                case "skeleton":
                    return "debugSkeleton";
                case "draugr":
                    return "debugDraugr";
                case "goblinarcher":
                    return "debugGoblinArcher";
                case "goblin":
                    return "debugGoblin";
                default:
                    return null;
            }
        }

        private static void SpawnMonsterNearCaller(string[] args)
        {
            try
            {
            string which = (args.Length > 1) ? args[1].ToLowerInvariant() : "";
            string prefabName = ResolveDebugMonsterPrefab(which);
            if (prefabName == null)
            {
                Log("Usage: /hirdmandr monster [" + string.Join("|", m_debugMonsterAliases) + "]");
                Log("Test-rig monsters are spawned on demand only; nothing auto-spawns.");
                return;
            }

            Player player = Player.m_localPlayer;
            if (player == null || player.transform == null)
            {
                Log("/hirdmandr monster: no local player (run from a client in singleplayer).");
                return;
            }

            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                Log("/hirdmandr monster: ZNetScene.instance is null (not in a level).");
                return;
            }

            GameObject prefab = scene.GetPrefab(prefabName);
            if (prefab == null)
            {
                Log("/hirdmandr monster: '" + prefabName + "' prefab is not registered yet.");
                return;
            }

            // Offset to the side of the caller so it is not inside them.
            Vector3 position = player.transform.position + player.transform.right * 3f;
            Quaternion rotation = player.transform.rotation;
            int prefabHash = scene.GetPrefabHash(prefab);

            ZDO zdo = ZDOMan.instance.CreateNewZDO(position, prefabHash);
            zdo.SetPrefab(prefabHash);
            zdo.SetRotation(rotation);

            // Monster prefabs have no owner-only init, so the ZDO owner is irrelevant to them;
            // they behave as normal world monsters.
            GameObject instance = scene.CreateObject(zdo);
            if (instance == null)
            {
                Log("/hirdmandr monster: CreateObject failed for '" + prefabName + "'.");
                return;
            }

            scene.AddInstance(zdo, instance.GetComponent<ZNetView>());
            Log("Spawned test-rig monster '" + prefabName + "' near the caller (ZDO " + zdo.m_uid + ").");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.SpawnMonsterNearCaller threw:\n" + e);
                throw;
            }
        }

        // Live movement diagnostics/tuning for the slow work-movement investigation. IL analysis
        // left several plausible speed gates (m_run/m_walk flags, m_runSpeed/m_walkSpeed values,
        // acceleration, turn speed, run/jog factors) that all funnel into Character's velocity
        // integrator, so instead of guessing we read and poke the live values while the NPC is
        // actually walking. Targets every Hirdmandr NPC in the world.
        //   /hirdmandr move stat                          - dump each NPC's movement state
        //   /hirdmandr move run <0|1>                     - Character.SetRun
        //   /hirdmandr move walk <0|1>                    - Character.SetWalk
        //   /hirdmandr move <speed|runspeed|walkspeed|crouchspeed|accel|turn> <float>
        private static void MoveTest(string[] args)
        {
            try
            {
                var npcs = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
                if (npcs == null || npcs.Length == 0)
                {
                    Log("/hirdmandr move: no Hirdmandr NPCs in the world.");
                    return;
                }

                string sub = (args.Length > 1) ? args[1].ToLowerInvariant() : "stat";

                if (sub == "stat")
                {
                    foreach (var npc in npcs)
                    {
                        if (npc == null)
                        {
                            continue;
                        }
                        Character ch = npc.GetComponent<Character>();
                        if (ch == null)
                        {
                            continue;
                        }
                        Log(string.Format(
                            "MOVESTAT {0}: m_run={1} m_walk={2} m_running={3} moveDir={4} (mag {5:F3}) "
                            + "m_speed={6:F2} m_runSpeed={7:F2} m_walkSpeed={8:F2} m_crouchSpeed={9:F2} "
                            + "m_accel={10:F2} m_turn={11:F2} runFactor={12:F2} jogFactor={13:F2}",
                            ch.m_name, ch.m_run, ch.m_walk, ch.m_running,
                            ch.m_moveDir, ch.m_moveDir.magnitude,
                            ch.m_speed, ch.m_runSpeed, ch.m_walkSpeed, ch.m_crouchSpeed,
                            ch.m_acceleration, ch.m_turnSpeed,
                            ch.GetRunSpeedFactor(), ch.GetJogSpeedFactor()));
                    }
                    return;
                }

                if (args.Length < 3)
                {
                    Log("Usage: /hirdmandr move [stat|run <0|1>|walk <0|1>|speed|runspeed|walkspeed|crouchspeed|accel|turn <float>]");
                    return;
                }

                if (sub == "run" || sub == "walk")
                {
                    bool flag;
                    if (!bool.TryParse(args[2], out flag))
                    {
                        Log("Expected 0/1 (or true/false) for '" + sub + "'.");
                        return;
                    }
                    foreach (var npc in npcs)
                    {
                        if (npc == null)
                        {
                            continue;
                        }
                        Character ch = npc.GetComponent<Character>();
                        if (ch == null)
                        {
                            continue;
                        }
                        if (sub == "run")
                        {
                            ch.SetRun(flag);
                        }
                        else
                        {
                            ch.SetWalk(flag);
                        }
                    }
                    Log("Set " + sub + "=" + flag + " on " + npcs.Length + " NPC(s).");
                    return;
                }

                float value;
                if (!float.TryParse(args[2], out value))
                {
                    Log("Expected a number for '" + sub + "'.");
                    return;
                }
                foreach (var npc in npcs)
                {
                    if (npc == null)
                    {
                        continue;
                    }
                    Character ch = npc.GetComponent<Character>();
                    if (ch == null)
                    {
                        continue;
                    }
                    switch (sub)
                    {
                        case "speed": ch.m_speed = value; break;
                        case "runspeed": ch.m_runSpeed = value; break;
                        case "walkspeed": ch.m_walkSpeed = value; break;
                        case "crouchspeed": ch.m_crouchSpeed = value; break;
                        case "accel": ch.m_acceleration = value; break;
                        case "turn": ch.m_turnSpeed = value; break;
                        default:
                            Log("Unknown move field: " + sub);
                            return;
                    }
                }
                Log("Set " + sub + "=" + value + " on " + npcs.Length + " NPC(s).");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.MoveTest threw:\n" + e);
                throw;
            }
        }

        // Test aid (R3c Increment A): drop a soul item a couple of metres in front of the local
        // player. Lets Thomas verify the pickable-soul foundation end to end: the green sphere
        // appears, can be picked up, sits in inventory as its own (non-stackable) item, and its
        // custom data survives a world reload. No NPC is involved yet - just the item.
        private static void SpawnSoulTest()
        {
            try
            {
                Player player = Player.m_localPlayer;
                SoulItem.SpawnTestSoul(player);
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.SpawnSoulTest threw:\n" + e);
                throw;
            }
        }

        // Rescue feature diagnostics (R2). Read-only dumps, no behavior change.
        //   /hirdmandr rescue scan     - placed PoI location instances in the 5x5 zone area
        //                               around the player (name / prefab / biome / position)
        //   /hirdmandr rescue catalog  - full location definition catalog (all PoI types)
        //   /hirdmandr rescue ship     - nearest Ship child transform tree + player ship/
        //                               teleport state + Character rigidbody kinematic state
        //   /hirdmandr rescue close    - the single nearest placed PoI in the whole world
        //                               (identify the structure you are standing next to by name)
        //   /hirdmandr rescue find <n> - compass direction + distance from the player to a
        //                               named PoI (e.g. "WoodHouse4 is 94 units W")
        //   /hirdmandr rescue verifygear - validate the per-biome gear tables (BiomeData /
        //                                  PMW #29) against ObjectDB: each item exists + its
        //                                  ItemType matches the slot it is listed under
        //   /hirdmandr rescue invdump    - dump the LOCAL PLAYER's current inventory: each
        //                                  item's exact ObjectDB prefab name (m_shared.m_name -
        //                                  the same string GetItemPrefab keys on), ItemType +
        //                                  quality. Used to read off the real names for the
        //                                  verifygear MISSING/MISMATCH problems.
        //   /hirdmandr rescue zdo        - ZDOMan census of every 'npc_entity' ZDO (uid/pos/
        //                                  dist/name) + contrasts: ZDOs in m_objectsByID vs
        //                                  loaded GameObjects vs the game's own prefab search.
        //                                  Answers U1: do far/unloaded NPCs stay in ZDOMan?
        private static void RescueTest(string[] args)
        {
            try
            {
                string sub = (args.Length > 1) ? args[1].ToLowerInvariant() : "scan";
                switch (sub)
                {
                    case "scan":
                        ScanLocations();
                        break;
                    case "catalog":
                        ScanLocationCatalog();
                        break;
                    case "ship":
                        ScanShip();
                        break;
                    case "close":
                        ScanClosestLocation();
                        break;
                    case "find":
                        FindLocationByName(args);
                        break;
                    case "spawn":
                        RescueForceSpawn();
                        break;
                    case "list":
                        RescueList();
                        break;
                    case "zdo":
                        ZdoCensus();
                        break;
                    case "verifygear":
                        VerifyGear();
                        break;
                    case "invdump":
                        DumpInventory();
                        break;
                    default:
                        Log("Usage: /hirdmandr rescue [scan|catalog|ship|close|find <PoI Name>|spawn|list|zdo|verifygear|invdump]");
                        break;
                }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.RescueTest threw:\n" + e);
                throw;
            }
        }

        // Test aid (R3c): force-place one unrescued rescue inside the nearest PLACED whitelisted
        // PoI to the player, bypassing the 20% roll. Stand in/near a whitelisted structure that is
        // in render range to exercise the inside-structure placement. (Note: /hirdmandr spawn
        // already covers a plain near-player spawn; this one tests the PoI path.)
        private static void RescueForceSpawn()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Log("/hirdmandr rescue spawn: no local player (run from a client in singleplayer).");
                    return;
                }

                var zs = ZoneSystem.instance;
                if (zs == null || zs.m_locationInstances == null)
                {
                    Log("/hirdmandr rescue spawn: ZoneSystem not ready (not in a level).");
                    return;
                }

                Vector3 ppos = player.transform.position;
                ZoneSystem.LocationInstance best = default(ZoneSystem.LocationInstance);
                float bestSqr = float.MaxValue;
                bool found = false;
                foreach (var kv in zs.m_locationInstances)
                {
                    var inst = kv.Value;
                    if (inst.m_location == null || !inst.m_placed)
                    {
                        continue;
                    }
                    if (!RescueWhitelist.Contains(inst.m_location))
                    {
                        continue;
                    }
                    float d = (inst.m_position - ppos).sqrMagnitude;
                    if (d < bestSqr)
                    {
                        bestSqr = d;
                        best = inst;
                        found = true;
                    }
                }

                if (!found)
                {
                    Log("/hirdmandr rescue spawn: no placed whitelisted PoI in the world yet - enter render range of one (see /hirdmandr rescue scan).");
                    return;
                }

                var npc = RescueSpawner.PlaceAtLocation(best, "test force");
                if (npc != null)
                {
                    Log("/hirdmandr rescue spawn: force-placed a rescue in '" + best.m_location.m_name + "' (" + Mathf.Sqrt(bestSqr).ToString("F1") + "m from you).");
                }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.RescueForceSpawn threw:\n" + e);
                throw;
            }
        }

        // Test aid (R3c): list every hirdmandr NPC in the world and flag the unrescued ones
        // (the world rescues), with their position + distance to the player.
        private static void RescueList()
        {
            try
            {
                Player player = Player.m_localPlayer;
                var npcs = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
                int total = 0;
                int unrescued = 0;
                foreach (var npc in npcs)
                {
                    if (npc == null)
                    {
                        continue;
                    }
                    total++;
                    if (npc.m_isRescued)
                    {
                        continue;
                    }
                    unrescued++;
                    string dist = (player != null && player.transform != null)
                        ? ((npc.transform.position - player.transform.position).magnitude.ToString("F1") + "m")
                        : "?";
                    string hash = "?";
                    try
                    {
                        if (npc.m_znet != null && npc.m_znet.GetZDO() != null)
                        {
                            hash = npc.m_znet.GetZDO().GetInt(RescueLedger.HashKey, 0).ToString();
                        }
                    }
                    catch { }
                    Log(string.Format("  UNRESCUED '{0}' hash={1} pos=({2:F1},{3:F1},{4:F1}) {5} from player",
                        (npc.m_humanoid != null) ? npc.m_humanoid.m_name : "?",
                        hash,
                        npc.transform.position.x, npc.transform.position.y, npc.transform.position.z, dist));
                }
                Log(string.Format("RESCUE LIST: {0} hirdmandr NPC(s) in world, {1} unrescued (rescues).", total, unrescued));
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.RescueList threw:\n" + e);
                throw;
            }
        }

        // /hirdmandr rescue zdo - ZDOMan census of Hirdmandr NPCs (every 'npc_entity' ZDO).
        // One-shot U1 probe (spawn refactor): does a far/unloaded NPC stay in ZDOMan? For every
        // npc_entity ZDO in ZDOMan.instance.m_objectsByID it prints uid, position, horizontal
        // distance from the player, zone, and name. It then contrasts three counts:
        //   (a) authoritative  - npc_entity ZDOs in m_objectsByID (near + far, if far ZDOs kept)
        //   (b) loaded only    - HirdmandrNPC GameObjects via FindObjectsOfType (near/loaded)
        //   (c) cross-check    - the game's own GetAllZDOsWithPrefabIterative("npc_entity")
        // If (a) > (b): the gap is far NPCs still tracked in ZDOMan with no GameObject -> ZDOMan
        // keeps far NPCs (U1=yes). If (a) == (b): far ZDOs are evicted (U1=no).
        private static void ZdoCensus()
        {
            try
            {
                ZNetScene scene = ZNetScene.instance;
                if (scene == null)
                {
                    Log("/hirdmandr rescue zdo: ZNetScene.instance is null (not in a level).");
                    return;
                }
                GameObject prefab = scene.GetPrefab("npc_entity");
                if (prefab == null)
                {
                    Log("/hirdmandr rescue zdo: 'npc_entity' prefab is not registered yet.");
                    return;
                }
                int npcHash = scene.GetPrefabHash(prefab);

                var zdoman = ZDOMan.instance;
                if (zdoman == null)
                {
                    Log("/hirdmandr rescue zdo: ZDOMan.instance is null.");
                    return;
                }
                var objectsByID = zdoman.m_objectsByID;
                if (objectsByID == null)
                {
                    Log("/hirdmandr rescue zdo: m_objectsByID is null (world not loaded?).");
                    return;
                }

                Player player = Player.m_localPlayer;
                bool havePlayer = (player != null && player.transform != null);
                Vector3 ppos = havePlayer ? player.transform.position : Vector3.zero;

                int totalZdos = zdoman.NrOfObjects();
                Log("ZDO CENSUS: enumerating npc_entity ZDOs in ZDOMan (total ZDOs in ZDOMan = " + totalZdos + "):");

                int inZDOMan = 0;
                int idx = 0;
                foreach (var kv in objectsByID)
                {
                    ZDO zdo = kv.Value;
                    if (zdo == null || zdo.GetPrefab() != npcHash)
                    {
                        continue;
                    }
                    inZDOMan++;
                    idx++;
                    Vector3 pos = zdo.m_position;
                    string dist = havePlayer
                        ? (new Vector3(pos.x - ppos.x, 0f, pos.z - ppos.z).magnitude.ToString("F1") + "m")
                        : "?";
                    string name = "?";
                    string zone = "";
                    int hash = 0;
                    try
                    {
                        string n = zdo.GetString("hmnpc_name");
                        name = string.IsNullOrEmpty(n) ? "?" : n;
                        hash = zdo.GetInt(RescueLedger.HashKey, 0);
                    }
                    catch { }
                    try
                    {
                        Vector2s zs = ZoneSystem.GetZone(pos);
                        zone = " zone=(" + zs.x + "," + zs.y + ")";
                    }
                    catch { }
                    Log(string.Format("  #{0} uid={1} name='{2}' hash={3} pos=({4:F1},{5:F1},{6:F1}) dist={7}{8}",
                        idx, zdo.m_uid, name, hash, pos.x, pos.y, pos.z, dist, zone));
                }

                // (b) Loaded (instantiated) NPCs only - near the player, have a live GameObject.
                var loaded = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
                int loadedCount = (loaded != null) ? loaded.Length : 0;

                // (c) Cross-check via the game's own prefab enumeration (string key = prefab name).
                int crossCount = 0;
                bool crossMore = false;
                int crossCursor = 0;
                try
                {
                    var list = new List<ZDO>();
                    int cursor = 0;
                    crossMore = zdoman.GetAllZDOsWithPrefabIterative("npc_entity", list, ref cursor);
                    crossCount = list.Count;
                    crossCursor = cursor;
                }
                catch (System.Exception ce)
                {
                    Log("  (cross-check threw: " + ce.Message + ")");
                }

                Log(string.Format(
                    "ZDO CENSUS (npc_entity hash={0}): (a) npc_entity ZDOs in ZDOMan={1} | (b) loaded NPCs (FindObjectsOfType)={2} | (c) GetAllZDOsWithPrefabIterative={3} (more={4}, cursor={5})",
                    npcHash, inZDOMan, loadedCount, crossCount, crossMore, crossCursor));
                Log("ZDO CENSUS: (a)>(b) => the gap is far NPCs still tracked in ZDOMan with no GameObject (U1=yes). (a)==(b) => far ZDOs evicted (U1=no).");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.ZdoCensus threw:\n" + e);
                throw;
            }
        }

        // Placed PoI (point-of-interest) locations near the player. The game places one
        // location per zone when a player enters render range ("Placed location X in zone Y"
        // log, ZoneSystem.PlaceLocations). This dump is the ground truth for the rescue
        // whitelist: it reports each placed location's name, prefab, biome and position.
        private static void ScanLocations()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Log("/hirdmandr rescue scan: no local player (run from a client in singleplayer).");
                    return;
                }

                var zs = ZoneSystem.instance;
                if (zs == null)
                {
                    Log("/hirdmandr rescue scan: ZoneSystem.instance is null (not in a level).");
                    return;
                }

                Vector3 ppos = player.transform.position;
                Vector2s pzone = ZoneSystem.GetZone(ppos);
                Log("RESCUE SCAN: player zone (" + pzone.x + "," + pzone.y + "), scanning the 5x5 zone area.");

                int count = 0;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        Vector2s zone = new Vector2s((short)(pzone.x + dx), (short)(pzone.y + dy));
                        ZoneSystem.LocationInstance inst;
                        if (!zs.m_locationInstances.TryGetValue(zone, out inst))
                        {
                            continue;
                        }
                        if (!inst.m_placed || inst.m_location == null)
                        {
                            continue;
                        }
                        var loc = inst.m_location;
                        Log(string.Format(
                            "  LOC {0} prefab={1} biome={2} zone=({3},{4}) pos=({5:F1},{6:F1},{7:F1}) interiorR={8:F1} exteriorR={9:F1}",
                            loc.m_name, loc.m_prefabName, Heightmap.BiomeToString(loc.m_biome),
                            zone.x, zone.y,
                            inst.m_position.x, inst.m_position.y, inst.m_position.z,
                            loc.m_interiorRadius, loc.m_exteriorRadius));
                        count++;
                    }
                }
                Log("RESCUE SCAN: " + count + " placed location(s) found.");
                // MP world-data diagnostics (2026-10-26): the loop above only sees what THIS process
                // (the server, since the command is networked) holds in m_locationInstances right now.
                // In 1.0.16 that dictionary is filled by two server-side sources only:
                //   1. ZoneSystem.Load (from ZNet.LoadWorld) - restores instances from the world
                //      save ("ZoneSystem.Load => Loaded N locations" in the log).
                //   2. GenerateLocations - a time-sliced coroutine started by
                //      ZNet.ServerLoadWorld -> GenerateLocationsIfNeeded when
                //      LocationsGenerated == false ("Setting up locations" in the log).
                // So an empty scan means one of: the save held no instances, generation is still
                // running, or the game believes locations are already generated with none placed.
                int totalInst = 0;
                int placedInst = 0;
                foreach (var kv in zs.m_locationInstances)
                {
                    totalInst++;
                    if (kv.Value.m_placed)
                    {
                        placedInst++;
                    }
                }
                Log(string.Format(
                    "STATE: IsServer={0} LocationsGenerated={1} progress={2:F3} genCoroutine={3} instances(total={4},placed={5}) locVersion={6}",
                    ZNet.instance != null && ZNet.instance.IsServer(),
                    zs.LocationsGenerated,
                    zs.GenerateLocationsProgress,
                    zs.m_generateLocationsCoroutine != null,
                    totalInst,
                    placedInst,
                    zs.m_locationVersion));
                if (ZNet.instance != null)
                {
                    var peers = ZNet.instance.GetPeers();
                    Log("STATE: peers=" + peers.Count);
                    foreach (var peer in peers)
                    {
                        Vector3 rp = peer.GetRefPos();
                        Vector2s pz = ZoneSystem.GetZone(rp);
                        Log(string.Format("  PEER id={0} refpos=({1:F1},{2:F1},{3:F1}) zone=({4},{5})",
                            peer.m_playerID, rp.x, rp.y, rp.z, pz.x, pz.y));
                    }
                    // The scanner's actual decision: the centers RunPass() will scan right now
                    // (peers + local player, 10m dedup) - ground truth for "why did/didn't the
                    // scanner scan here".
                    var centers = RescueScanner.GetScanCenters();
                    Log("STATE: scanCenters=" + centers.Count + " (peers + local player, 10m dedup)");
                    foreach (Vector3 center in centers)
                    {
                        Vector2s cz = ZoneSystem.GetZone(center);
                        Log(string.Format("  CENTER refpos=({0:F1},{1:F1},{2:F1}) zone=({3},{4})",
                            center.x, center.y, center.z, cz.x, cz.y));
                    }
                }
                Log("NOTE: crypt / DG_* entries are dungeon entrances - exclude those from the rescue whitelist (rescues never spawn in dungeons).");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.ScanLocations threw:\n" + e);
                throw;
            }
        }

        // Full location definition catalog (every PoI type the world can place). Lets Thomas
        // build the rescue whitelist without visiting every biome first.
        private static void ScanLocationCatalog()
        {
            try
            {
                var zs = ZoneSystem.instance;
                if (zs == null)
                {
                    Log("/hirdmandr rescue catalog: ZoneSystem.instance is null (not in a level).");
                    return;
                }

                var defs = zs.m_locations;
                if (defs == null || defs.Count == 0)
                {
                    Log("RESCUE CATALOG: no location definitions loaded yet.");
                    return;
                }

                Log("RESCUE CATALOG: " + defs.Count + " location definition(s):");
                foreach (var loc in defs)
                {
                    if (loc == null)
                    {
                        continue;
                    }
                    Log(string.Format("  DEF {0} prefab={1} biome={2} qty={3} enable={4} group={5}",
                        loc.m_name, loc.m_prefabName, Heightmap.BiomeToString(loc.m_biome),
                        loc.m_quantity, loc.m_enable, loc.m_group));
                }
                Log("RESCUE CATALOG: done.");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.ScanLocationCatalog threw:\n" + e);
                throw;
            }
        }

        // The single nearest PLACED PoI anywhere in the world (unlike `scan`, which is limited
        // to the 5x5 zone area around the player). Lets Thomas identify the exact structure he
        // is standing next to by its in-game location name, for the rescue whitelist.
        private static void ScanClosestLocation()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Log("/hirdmandr rescue close: no local player (run from a client in singleplayer).");
                    return;
                }

                var zs = ZoneSystem.instance;
                if (zs == null)
                {
                    Log("/hirdmandr rescue close: ZoneSystem.instance is null (not in a level).");
                    return;
                }

                Vector3 ppos = player.transform.position;
                ZoneSystem.ZoneLocation bestLoc = null;
                Vector3 bestPos = Vector3.zero;
                float bestDist = float.MaxValue;
                foreach (var kv in zs.m_locationInstances)
                {
                    var inst = kv.Value;
                    if (!inst.m_placed || inst.m_location == null)
                    {
                        continue;
                    }
                    float dist = HorizontalDist(ppos, inst.m_position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestLoc = inst.m_location;
                        bestPos = inst.m_position;
                    }
                }

                if (bestLoc == null)
                {
                    Log("RESCUE CLOSE: no placed locations in the world yet (walk around / wait for zones to generate).");
                    return;
                }

                Log(string.Format(
                    "RESCUE CLOSE: nearest PoI is '{0}' (prefab={1}, biome={2}) at {3:0.00} units {4}  pos=({5:F1},{6:F1},{7:F1})",
                    bestLoc.m_name, bestLoc.m_prefabName, Heightmap.BiomeToString(bestLoc.m_biome),
                    bestDist, CompassDir(bestPos - ppos),
                    bestPos.x, bestPos.y, bestPos.z));
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.ScanClosestLocation threw:\n" + e);
                throw;
            }
        }

        // /hirdmandr rescue find <PoI Name> - report the compass direction + horizontal
        // distance from the player to a named PoI (e.g. "WoodHouse4 is 94 units W").
        // Searches PLACED instances first (they have a world position); if the name only
        // exists in the definition catalog (not placed yet), reports that instead.
        private static void FindLocationByName(string[] args)
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Log("/hirdmandr rescue find: no local player (run from a client in singleplayer).");
                    return;
                }

                var zs = ZoneSystem.instance;
                if (zs == null)
                {
                    Log("/hirdmandr rescue find: ZoneSystem.instance is null (not in a level).");
                    return;
                }

                if (args.Length < 3)
                {
                    Log("Usage: /hirdmandr rescue find <PoI Name>   (e.g. /hirdmandr rescue find WoodHouse4)");
                    return;
                }

                string name = string.Join(" ", args, 2, args.Length - 2).Trim();
                if (name.Length == 0)
                {
                    Log("Usage: /hirdmandr rescue find <PoI Name>   (e.g. /hirdmandr rescue find WoodHouse4)");
                    return;
                }

                Vector3 ppos = player.transform.position;
                int found = 0;

                foreach (var kv in zs.m_locationInstances)
                {
                    var inst = kv.Value;
                    if (!inst.m_placed || inst.m_location == null)
                    {
                        continue;
                    }
                    var loc = inst.m_location;
                    if (!NameMatches(loc, name))
                    {
                        continue;
                    }
                    found++;
                    Log(string.Format(
                        "  {0} is {1:0.00} units {2}  (prefab={3}, biome={4}, pos=({5:F1},{6:F1},{7:F1}))",
                        loc.m_name, HorizontalDist(ppos, inst.m_position), CompassDir(inst.m_position - ppos),
                        loc.m_prefabName, Heightmap.BiomeToString(loc.m_biome),
                        inst.m_position.x, inst.m_position.y, inst.m_position.z));
                }

                if (found == 0 && zs.m_locations != null)
                {
                    foreach (var loc in zs.m_locations)
                    {
                        if (loc == null || !NameMatches(loc, name))
                        {
                            continue;
                        }
                        found++;
                        Log(string.Format(
                            "  {0} (prefab={1}, biome={2}) is in the catalog but NOT placed yet - no world position until a player enters its render range.",
                            loc.m_name, loc.m_prefabName, Heightmap.BiomeToString(loc.m_biome)));
                    }
                }

                if (found == 0)
                {
                    Log(string.Format("RESCUE FIND: no location matching '{0}' (case-insensitive, substring OK; checked placed instances + catalog).", name));
                }
                else
                {
                    Log("RESCUE FIND: " + found + " match(es) for '" + name + "'.");
                }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.FindLocationByName threw:\n" + e);
                throw;
            }
        }

        // Compass direction (N/NE/E/SE/S/SW/W/NW) of a world-space delta, horizontal only,
        // using the Unity convention north = -Z, east = +X.
        private static string CompassDir(Vector3 delta)
        {
            float horizontal = new Vector3(delta.x, 0f, delta.z).magnitude;
            if (horizontal < 1f)
            {
                return "?";
            }
            float north = -delta.z;
            float east = delta.x;
            double deg = System.Math.Atan2(east, north) * 180.0 / System.Math.PI;
            if (deg < 0.0)
            {
                deg += 360.0;
            }
            int sector = (int)((deg + 22.5) / 45.0) % 8;
            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return dirs[sector];
        }

        // Case-insensitive match of `name` against a ZoneLocation's m_name or m_prefabName.
        // Exact first, then substring, so both "WoodHouse4" and "WoodHouse" work.
        private static bool NameMatches(ZoneSystem.ZoneLocation loc, string name)
        {
            if (loc == null || string.IsNullOrEmpty(name))
            {
                return false;
            }
            if (loc.m_name != null && loc.m_name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (loc.m_prefabName != null && loc.m_prefabName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (loc.m_name != null && loc.m_name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            return loc.m_prefabName != null && loc.m_prefabName.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Horizontal (XZ-plane) distance between two world points.
        private static float HorizontalDist(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, 0f, a.z - b.z).magnitude;
        }

        // /hirdmandr rescue verifygear - validate the per-biome gear tables (BiomeData / PMW #29)
        // against the live game. For every item name: (1) ObjectDB has a prefab, (2) the
        // ItemDrop's ItemType matches the slot it is listed under. Run IN A WORLD (ObjectDB is
        // loaded). "None" (empty-slot option) and duplicates are skipped. Surfaces typos and
        // wrong-slot entries before the R4 gear-refactor (step 2).
        private static void VerifyGear()
        {
            try
            {
                if (ObjectDB.instance == null)
                {
                    Log("RESCUE VERIFYGEAR: ObjectDB.instance is null - enter a world first.");
                    return;
                }
                int problems = 0;
                foreach (var gear in BiomeData.All)
                {
                    Log("[" + gear.name + " skills=" + FormatSkills(gear.skills) + "]");
                    problems += CheckGearList("weapon1h", ItemDrop.ItemData.ItemType.OneHandedWeapon, gear.weapon1h);
                    problems += CheckGearList("weapon2h", ItemDrop.ItemData.ItemType.TwoHandedWeapon, gear.weapon2h);
                    problems += CheckGearList("weaponRanged", ItemDrop.ItemData.ItemType.Bow, gear.weaponRanged);
                    problems += CheckGearList("ammo", ItemDrop.ItemData.ItemType.Ammo, gear.ammo);
                    problems += CheckGearList("shield", ItemDrop.ItemData.ItemType.Shield, gear.shield);
                    problems += CheckGearList("head", ItemDrop.ItemData.ItemType.Helmet, gear.head);
                    problems += CheckGearList("chest", ItemDrop.ItemData.ItemType.Chest, gear.chest);
                    problems += CheckGearList("legs", ItemDrop.ItemData.ItemType.Legs, gear.legs);
                }
                Log("RESCUE VERIFYGEAR: done. " + problems + " problem(s).");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.VerifyGear threw:\n" + e);
                throw;
            }
        }

        private static string FormatSkills(float[] skills)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < skills.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(skills[i]);
            }
            return sb.ToString();
        }

        // Diagnostic: dump the LOCAL PLAYER's current inventory. Each line prints the item's
        // exact ObjectDB prefab name (ItemData.m_shared.m_name - the same string
        // GetItemPrefab() keys on, which is CASE-SENSITIVE), its ItemType, and quality.
        // Thomas spawns the verifygear problem items (vanilla `spawn` devcommand), runs this,
        // and reads off the real names to fix the BiomeData tables.
        private static void DumpInventory()
        {
            try
            {
                Player p = Player.m_localPlayer;
                if (p == null)
                {
                    Log("INVDUMP: no local player");
                    return;
                }
                var inv = p.m_inventory;
                if (inv == null)
                {
                    Log("INVDUMP: no inventory");
                    return;
                }
                if (ObjectDB.instance == null)
                {
                    Log("INVDUMP: ObjectDB.instance is null - enter a world first.");
                    return;
                }
                // Round-trip: map each item's internal name (m_shared.m_name, e.g.
                // "$item_axe_blackmetal") to its ObjectDB prefab name - the PascalCase
                // string GetItemPrefab keys on - built once from ObjectDB.GetAllItems.
                var nameMap = new System.Collections.Generic.Dictionary<string, string>();
                foreach (GameObject prefab in ObjectDB.instance.m_items)
                {
                    if (prefab == null)
                    {
                        continue;
                    }
                    var drop = prefab.GetComponent<ItemDrop>();
                    if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                    {
                        continue;
                    }
                    nameMap[drop.m_itemData.m_shared.m_name] = prefab.name;
                }
                Log("INVDUMP: inventory (shared=internal name, prefab=GetItemPrefab key):");
                foreach (ItemDrop.ItemData item in inv.GetAllItems())
                {
                    if (item == null || item.m_shared == null)
                    {
                        continue;
                    }
                    string shared = item.m_shared.m_name;
                    string prefab;
                    if (!nameMap.TryGetValue(shared, out prefab))
                    {
                        Log("  shared='" + shared + "'  =>  <no ObjectDB match>  type=" + item.m_shared.m_itemType);
                        continue;
                    }
                    // Close the loop: confirm GetItemPrefab(prefab) resolves to this same item.
                    GameObject resolved = ObjectDB.instance.GetItemPrefab(prefab);
                    string confirm = "ROUNDTRIP:MISS";
                    if (resolved != null)
                    {
                        var rdrop = resolved.GetComponent<ItemDrop>();
                        string rshared = (rdrop != null && rdrop.m_itemData != null && rdrop.m_itemData.m_shared != null) ? rdrop.m_itemData.m_shared.m_name : "?";
                        confirm = (rshared == shared) ? "ROUNDTRIP:OK" : ("ROUNDTRIP:DIFF(" + rshared + ")");
                    }
                    Log("  shared='" + shared + "'  =>  prefab='" + prefab + "'  type=" + item.m_shared.m_itemType + "  " + confirm);
                }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.DumpInventory threw:\n" + e);
                throw;
            }
        }

        // FARMER DISCOVERY (Step 1) - FARMER-DEBUG, remove in Step 11.
        // Read-only dump of everything near the local player that the farmer job will touch:
        //   (1) every loaded Pickable within 40m -> the mature-crop yields (m_itemPrefab), and
        //       whether a mature crop is really a Pickable (in a field, the count must be > 0);
        //   (2) every Container within 20m       -> the chest contents, so Thomas can read off
        //       the seed/food item strings and the cultivator tool string.
        // Each item is printed two ways: the internal name (m_shared.m_name, e.g. "$item_carrot" -
        // what HaveItem keys on) and the ObjectDB name (e.g. "Carrot").
        private static void FarmScan()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Log("/hirdmandr farmscan: no local player (run from a client in singleplayer).");
                    return;
                }
                Vector3 ppos = player.transform.position;

                // internal name -> ObjectDB name (same map DumpInventory builds).
                var nameMap = new Dictionary<string, string>();
                if (ObjectDB.instance != null)
                {
                    foreach (GameObject prefab in ObjectDB.instance.m_items)
                    {
                        if (prefab == null)
                        {
                            continue;
                        }
                        var drop = prefab.GetComponent<ItemDrop>();
                        if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                        {
                            continue;
                        }
                        nameMap[drop.m_itemData.m_shared.m_name] = prefab.name;
                    }
                }

                // (1) Mature crops: loaded Pickables within 40m. One line each + a yield summary.
                Log("FARMSCAN: crops (loaded Pickables within 40m):");
                var pickups = UnityEngine.Object.FindObjectsOfType<Pickable>();
                int pickCount = 0;
                var yieldCount = new Dictionary<string, int>();
                var yieldOb = new Dictionary<string, string>();
                foreach (var pk in pickups)
                {
                    if (pk == null || pk.transform == null)
                    {
                        continue;
                    }
                    float d = HorizontalDist(ppos, pk.transform.position);
                    if (d > 40f)
                    {
                        continue;
                    }
                    pickCount++;
                    // m_itemPrefab is a GameObject (the prefab dropped when picked); the
                    // internal item name (what HaveItem keys on) comes off its ItemDrop.
                    GameObject ip = pk.m_itemPrefab;
                    string itemName = "?null?";
                    string obName = "?null?";
                    if (ip != null)
                    {
                        obName = ip.name;
                        var idrop = ip.GetComponent<ItemDrop>();
                        if (idrop != null && idrop.m_itemData != null && idrop.m_itemData.m_shared != null)
                        {
                            itemName = idrop.m_itemData.m_shared.m_name;
                        }
                    }
                    if (!yieldCount.ContainsKey(itemName))
                    {
                        yieldCount[itemName] = 0;
                        yieldOb[itemName] = obName;
                    }
                    yieldCount[itemName]++;
                    Log(string.Format(
                        "  CROP go='{0}' ob='{1}' item='{2}' amount={3} respawn={4} pos=({5:F1},{6:F1},{7:F1}) dist={8:F1}",
                        pk.name, obName, itemName, pk.m_amount, pk.m_respawnTimeMinutes,
                        pk.transform.position.x, pk.transform.position.y, pk.transform.position.z, d));
                }
                Log(string.Format("FARMSCAN: {0} Pickable(s) in range.", pickCount));
                foreach (var kv in yieldCount)
                {
                    Log(string.Format("  YIELD item='{0}' ob='{1}' x{2}", kv.Key, yieldOb[kv.Key], kv.Value));
                }

                // (2) Containers within 20m: dump contents (spot the seeds/food + the tool).
                Log("FARMSCAN: containers (within 20m):");
                int chestCount = 0;
                var containers = UnityEngine.Object.FindObjectsOfType<Container>();
                foreach (var ct in containers)
                {
                    if (ct == null || ct.transform == null)
                    {
                        continue;
                    }
                    float d = HorizontalDist(ppos, ct.transform.position);
                    if (d > 20f)
                    {
                        continue;
                    }
                    chestCount++;
                    Log(string.Format("  CONTAINER '{0}' pos=({1:F1},{2:F1},{3:F1}) dist={4:F1}",
                        ct.name, ct.transform.position.x, ct.transform.position.y, ct.transform.position.z, d));
                    if (ct.m_inventory == null)
                    {
                        Log("    (no inventory)");
                        continue;
                    }
                    foreach (var it in ct.m_inventory.GetAllItems())
                    {
                        if (it == null || it.m_shared == null)
                        {
                            continue;
                        }
                        string s = it.m_shared.m_name;
                        Log(string.Format("    ITEM item='{0}' ob='{1}' x{2}", s, ObjName(nameMap, s), it.m_stack));
                    }
                }
                Log(string.Format("FARMSCAN: {0} container(s) in range.", chestCount));

                // (3) Registered sapling/mushroom prefabs (to find the correct sapling names for
                //     the farmer crop table). A wrong name logs "not registered" and the replant
                //     is skipped; this lists every registered prefab whose name carries a
                //     sapling/mushroom/magecap/jotunpuff token so the right names can be read off.
                Log("FARMSCAN: registered sapling/mushroom prefabs:");
                var saplings = new List<string>();
                if (ZNetScene.instance != null)
                {
                    foreach (string pn in ZNetScene.instance.GetPrefabNames())
                    {
                        if (pn == null)
                        {
                            continue;
                        }
                        string low = pn.ToLowerInvariant();
                        if (low.Contains("sapling") || low.Contains("mushroom") || low.Contains("magecap") || low.Contains("jotunpuff") || low.Contains("puff"))
                        {
                            saplings.Add(pn);
                        }
                    }
                }
                saplings.Sort();
                foreach (string sn in saplings)
                {
                    Log("  SAPLING '" + sn + "'");
                }
                Log(string.Format("FARMSCAN: {0} sapling/mushroom prefab(s).", saplings.Count));
                Log("FARMSCAN: done. Identify -> (a) crop yields, (b) seed/food items, (c) the cultivator tool, (d) correct sapling names.");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.FarmScan threw:\n" + e);
                throw;
            }
        }

        // GATHER DISCOVERY (Step 1) - GATHER-DEBUG, remove after S1 validation.
        // Read-only dump of the two things the gather safe zone keys on:
        //   (A) every loaded chest = a Piece carrying a Container -> its IsPlacedByPlayer()
        //       flag. This is the HOLE 1 test: on a long-lived world, player-placed chests
        //       must report placed=true and world-spawned chests placed=false, AND the same
        //       chest must report the same flag before and after a world reload. If the flag
        //       does not discriminate / does not persist, the 40m safe zone cannot key on it.
        //   (B) every loaded Pickable -> its yield and whether it falls within 40m (the zone
        //       radius) of ANY placed chest, so the exclusion can be eyeballed spatially.
        // No world mutation; run from the in-game console (singleplayer).
        private static void ChestScan()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Log("/hirdmandr chestscan: no local player (run from a client in singleplayer).");
                    return;
                }

                const float zoneRadius = 40f; // the gather safe-zone radius (Utils.GatherSafeRadius).

                // (A) Chests = Pieces carrying a Container. Log the IsPlacedByPlayer() flag per
                //     chest (Hole 1) and collect the placed-chest positions for the zone test.
                Log("CHESTSCAN: chests (Piece + Container) -- placed=IsPlacedByPlayer(), hm=has HirdmandrChest:");
                var placedChestPos = new List<Vector3>();
                int chestTotal = 0, chestPlaced = 0, chestHm = 0;
                var pieces = UnityEngine.Object.FindObjectsOfType<Piece>();
                foreach (var pc in pieces)
                {
                    if (pc == null || pc.transform == null)
                    {
                        continue;
                    }
                    var ct = pc.GetComponent<Container>();
                    if (ct == null)
                    {
                        continue;
                    }
                    chestTotal++;
                    bool placed = pc.IsPlacedByPlayer();
                    bool hm = pc.GetComponent<HirdmandrChest>() != null;
                    if (placed)
                    {
                        chestPlaced++;
                        placedChestPos.Add(pc.transform.position);
                    }
                    if (hm)
                    {
                        chestHm++;
                    }
                    int itemCount = 0;
                    if (ct.m_inventory != null)
                    {
                        foreach (var it in ct.m_inventory.GetAllItems())
                        {
                            if (it != null)
                            {
                                itemCount += (int)it.m_stack;
                            }
                        }
                    }
                    Vector3 cp = pc.transform.position;
                    Log(string.Format("  CHEST '{0}' placed={1} hm={2} items={3} pos=({4:F1},{5:F1},{6:F1})",
                        pc.name, placed, hm, itemCount, cp.x, cp.y, cp.z));
                }
                Log(string.Format("CHESTSCAN: {0} chest(s) total, {1} player-placed, {2} Hirdmandr.",
                    chestTotal, chestPlaced, chestHm));

                // (B) Pickables -> yield + zone membership (within 40m of any placed chest).
                Log("CHESTSCAN: pickables (loaded) -- inZone=within 40m of a placed chest:");
                var pickups = UnityEngine.Object.FindObjectsOfType<Pickable>();
                int pickTotal = 0, pickInZone = 0;
                foreach (var pk in pickups)
                {
                    if (pk == null || pk.transform == null)
                    {
                        continue;
                    }
                    pickTotal++;
                    GameObject ip = pk.m_itemPrefab;
                    string itemName = "?null?";
                    if (ip != null)
                    {
                        var idrop = ip.GetComponent<ItemDrop>();
                        if (idrop != null && idrop.m_itemData != null && idrop.m_itemData.m_shared != null)
                        {
                            itemName = idrop.m_itemData.m_shared.m_name;
                        }
                    }
                    // Nearest placed chest (XZ). -1 = no placed chest to measure against.
                    float nearest = -1f;
                    bool inZone = false;
                    if (placedChestPos.Count > 0)
                    {
                        Vector3 pp = pk.transform.position;
                        nearest = float.MaxValue;
                        for (int i = 0; i < placedChestPos.Count; i++)
                        {
                            float d = HorizontalDist(pp, placedChestPos[i]);
                            if (d < nearest)
                            {
                                nearest = d;
                            }
                        }
                        inZone = nearest <= zoneRadius;
                        if (inZone)
                        {
                            pickInZone++;
                        }
                    }
                    Vector3 kp = pk.transform.position;
                    Log(string.Format("  PICKABLE '{0}' item='{1}' inZone={2} nearestChest={3:F1} pos=({4:F1},{5:F1},{6:F1})",
                        pk.name, itemName, inZone, nearest, kp.x, kp.y, kp.z));
                }
                Log(string.Format("CHESTSCAN: {0} pickable(s) total, {1} within 40m of a placed chest.",
                    pickTotal, pickInZone));
                Log("CHESTSCAN: done. HOLE 1 = does 'placed' discriminate player vs world chests, and does it survive a reload?");
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.ChestScan threw:\n" + e);
                throw;
            }
        }

        // /hirdmandr gather - dump each loaded Hirdmandr NPC's Gather scan cache (S4 test
        // harness): role/job/following flags, cached pickable/anchor/excluded counts, and the
        // nearest eligible target under S5's rule (not excluded, within 20m of the center,
        // center = the follow target while following, else the NPC).
        private static void GatherDump()
        {
            try
            {
                var npcs = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
                int dumpCount = 0;
                foreach (var npc in npcs)
                {
                    if (npc == null || npc.transform == null || npc.m_hirdmandrAI == null)
                    {
                        continue;
                    }
                    var ai = npc.m_hirdmandrAI;
                    string name = (npc.m_humanoid != null && !string.IsNullOrEmpty(npc.m_humanoid.m_name))
                        ? npc.m_humanoid.m_name
                        : npc.name;
                    Log(string.Format("GATHER '{0}' warrior={1} himthiki={2} gatherer={3} following={4} pickables={5} anchors={6} excluded={7} (lastScan={8:F1}s ago)",
                        name, npc.m_roleWarrior, npc.m_jobHimthiki, npc.m_jobGatherer, npc.m_following,
                        ai.m_gatherPickables.Count, ai.m_gatherAnchorCount, ai.m_gatherExcluded.Count,
                        UnityEngine.Time.time - ai.m_lastGatherScan));
                    dumpCount++;
                    if (ai.m_gatherPickables.Count == 0)
                    {
                        continue;
                    }
                    Vector3 center = npc.transform.position;
                    if (npc.m_following && ai.m_hmMonsterAI != null && ai.m_hmMonsterAI.GetFollowTarget() != null)
                    {
                        center = ai.m_hmMonsterAI.GetFollowTarget().transform.position;
                    }
                    Pickable best = null;
                    float bestD = float.MaxValue;
                    foreach (var pk in ai.m_gatherPickables)
                    {
                        if (pk == null || pk.transform == null)
                        {
                            continue;
                        }
                        if (ai.m_gatherExcluded.Contains(pk))
                        {
                            continue;
                        }
                        float d = HorizontalDist(center, pk.transform.position);
                        if (d > 20f) // S5 target radius (the follow leash).
                        {
                            continue;
                        }
                        if (d < bestD)
                        {
                            bestD = d;
                            best = pk;
                        }
                    }
                    if (best != null)
                    {
                        Log(string.Format("  NEAREST-ELIGIBLE '{0}' {1:F1}m (center=({2:F1},{3:F1},{4:F1}))",
                            best.name, bestD, center.x, center.y, center.z));
                    }
                    else
                    {
                        Log("  NEAREST-ELIGIBLE (none within 20m of center)");
                    }
                }
                Log(string.Format("GATHER: dumped {0} NPC(s).", dumpCount));
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.GatherDump threw:\n" + e);
                throw;
            }
        }

        // Map an internal item name to its ObjectDB (PascalCase) name for the log.
        private static string ObjName(Dictionary<string, string> map, string s)
        {
            string v;
            return (map != null && map.TryGetValue(s, out v)) ? v : "?";
        }

        // Check one slot's item list against ObjectDB. Returns the number of problems.
        private static int CheckGearList(string slot, ItemDrop.ItemData.ItemType expected, List<string> items)
        {
            int problems = 0;
            if (items == null || items.Count == 0)
            {
                Log("  " + slot + ": (none)");
                return 0;
            }
            var seen = new HashSet<string>();
            foreach (var raw in items)
            {
                if (seen.Contains(raw))
                {
                    continue;
                }
                seen.Add(raw);
                if (string.IsNullOrEmpty(raw) || raw == "None")
                {
                    continue;
                }
                var prefab = ObjectDB.instance.GetItemPrefab(raw);
                if (prefab == null)
                {
                    problems++;
                    Log("  " + slot + ": MISSING '" + raw + "' (no ObjectDB prefab)");
                    continue;
                }
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null)
                {
                    problems++;
                    Log("  " + slot + ": NO ItemDrop '" + raw + "'");
                    continue;
                }
                var actual = drop.m_itemData.m_shared.m_itemType;
                if (actual != expected)
                {
                    problems++;
                    Log("  " + slot + ": MISMATCH '" + raw + "' is " + actual + " (expected " + expected + ")");
                }
                else
                {
                    Log("  " + slot + ": ok '" + raw + "'");
                }
            }
            return problems;
        }

        // Nearest ship diagnostics: child transform tree (to identify the deck/board node),
        // player ship/teleport state, and Character rigidbody kinematic state (board-glue
        // approach decision for R5).
        private static void ScanShip()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player == null || player.transform == null)
                {
                    Log("/hirdmandr rescue ship: no local player (run from a client in singleplayer).");
                    return;
                }

                var controlled = player.GetControlledShip();
                Log("SHIP: player.IsAttachedToShip=" + player.IsAttachedToShip()
                    + " GetControlledShip=" + (controlled != null ? controlled.name : "null")
                    + " IsTeleporting=" + player.IsTeleporting());

                var ships = UnityEngine.Object.FindObjectsOfType<Ship>();
                if (ships == null || ships.Length == 0)
                {
                    Log("SHIP: no Ship objects in the world.");
                    return;
                }

                Ship nearest = null;
                float nearestDist = float.MaxValue;
                foreach (var s in ships)
                {
                    if (s == null || s.transform == null)
                    {
                        continue;
                    }
                    float d = Vector3.Distance(player.transform.position, s.transform.position);
                    int playerCount = (s.m_players != null) ? s.m_players.Count : -1;
                    Log(string.Format(
                        "SHIP: {0} pos=({1:F1},{2:F1},{3:F1}) dist={4:F1} players={5} localPlayerInBoat={6} hasOnboard={7} hasControlling={8}",
                        s.name, s.transform.position.x, s.transform.position.y, s.transform.position.z, d,
                        playerCount, s.IsPlayerInBoat(player), s.HasPlayerOnboard(), s.HaveControllingPlayer()));
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearest = s;
                    }
                }

                if (nearest != null)
                {
                    Log("SHIP: nearest = " + nearest.name + " (dist " + nearestDist.ToString("F1") + "), dumping child transform tree (depth<=4):");
                    int budget = 200;
                    DumpTransformTree(nearest.transform, 0, 4, ref budget);
                }

                var pRB = player.GetComponent<Rigidbody>();
                Log("SHIP: player Character rigidbody=" + (pRB != null ? ("isKinematic=" + pRB.isKinematic) : "NONE"));
                var npcs = UnityEngine.Object.FindObjectsOfType<HirdmandrNPC>();
                if (npcs != null)
                {
                    foreach (var npc in npcs)
                    {
                        if (npc == null)
                        {
                            continue;
                        }
                        Character ch = npc.GetComponent<Character>();
                        if (ch == null)
                        {
                            continue;
                        }
                        var nRB = ch.GetComponent<Rigidbody>();
                        Log("SHIP: NPC " + ch.m_name + " rigidbody=" + (nRB != null ? ("isKinematic=" + nRB.isKinematic) : "NONE"));
                    }
                }
            }
            catch (System.Exception e)
            {
                Jotunn.Logger.LogError("HirdmandrCommand.ScanShip threw:\n" + e);
                throw;
            }
        }

        // Recursive transform dump for ScanShip. Budget caps the total line count so a
        // longship with hundreds of parts cannot flood the log.
        private static void DumpTransformTree(Transform t, int depth, int maxDepth, ref int budget)
        {
            if (t == null || budget <= 0)
            {
                return;
            }
            budget--;
            Log(new string(' ', depth * 2) + "- " + t.name + " local=(" + t.localPosition.ToString("F1") + ")");
            if (depth >= maxDepth)
            {
                return;
            }
            foreach (Transform c in t)
            {
                DumpTransformTree(c, depth + 1, maxDepth, ref budget);
                if (budget <= 0)
                {
                    return;
                }
            }
        }
    }
}
