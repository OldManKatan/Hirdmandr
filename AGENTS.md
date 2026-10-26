# Hirdmandr — Agent Notes

## PMW

This project is managed in the PMW service.

- Project name: `hirdmandr`
- API base: `http://127.0.0.1:18463`
- MCP tool: `pmw_introduction` (pass `project="hirdmandr"`) - call it before
  doing project-management work; it returns the full PMW workflow + API guide.
- Web dashboard: `http://127.0.0.1:18463/app/projects/hirdmandr`

Capture, triage, and record through the API/MCP, not by hand-editing files.

**Recording an item with a long `outcome` (the working recipe)** — in this shell the PowerShell
web cmdlets are unreliable for PATCH and `Get-Content -Raw` returns a wrapper object (not a
string), so do it with .NET file I/O + `curl.exe`:

```powershell
$txt  = [System.IO.File]::ReadAllText("D:\...\pmw_record_<code>.txt")  # real [string]; NOT Get-Content -Raw
$body = @{ state = "done"; outcome = $txt; outcome_date = "YYYY-MM-DD" } | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText("D:\...\pmw_<code>_body.json", $body, [System.Text.UTF8Encoding]::new($false))  # no BOM
curl.exe -s -i -X PATCH -H "Content-Type: application/json" --data-binary "@D:\...\pmw_<code>_body.json" "http://127.0.0.1:18463/api/projects/hirdmandr/items/<code>"
# verify: GET /items/<code> -> state=done, outcome_len == source length, outcome_date set
```

Why: `Get-Content -Raw` here yields a `PSCustomObject` (props `value`, `PSPath`, …), so `ConvertTo-Json`
emits `"outcome": {"value": "…"}` and the API 422s ("Input should be a valid string"); `Invoke-RestMethod`
/ `Invoke-WebRequest -Method Patch -Body` intermittently throws "Read and Prompt functionality is not
available" or sends a mangled body. `curl.exe --data-binary @file` is the reliable transport.

## Cold start
1. Read this file.
2. Call `pmw_introduction` (project="hirdmandr") → read "Current state" + open DECISIONS.
3. Project state lives in PMW, not in markdown.

## Build
- Toolchain: VS2022 Community MSBuild (no cargo/cmake/gcc on this box).
```
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\OpenCode\hirdmandr_project\hirmandr_repo\Hirdmandr.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU" /p:ExecutePrebuild=false /nologo /m
```
- `/p:ExecutePrebuild=false` skips JotunnBuildTask (it would regenerate `publicized_assemblies` inside the game install; the existing set is current for 1.0.16).
- On success a post-build task runs `publish.ps1` → deploys the mod to `D:\SteamLibrary\steamapps\common\Valheim\BepInEx\plugins\Hirdmandr` (the mod's own plugin folder, not game code).
- **Compile errors surface in waves** — csc stops after early binding failures, so fix, rebuild, repeat until clean.
- **New `.cs` files must be added to `Hirdmandr.csproj`** as `<Compile Include="Foo.cs" />` (old-style project; SDK auto-include does NOT apply). A new file that isn't listed is silently left out of the assembly, so any reference to its types fails with `CS0103: The name 'X' does not exist in the current context`.

## Valheim 1.0.16 API gotchas
- Compile reference: `$(VALHEIM_MANAGED)/publicized_assemblies/assembly_valheim_publicized.dll` (in the game install; current for 1.0.16). `Hirdmandr/bin/Release/assembly_valheim_publicized.dll` is a stale 2022 build — never validate against it.
- **Assembly updated 2026-09-25** (exe 6000.0.75.2503836): `MapGenerator`, `SEShip`, `SEShipBoard`, `SEPortal`, `SEStructure`, `ZDOType`, `Visequip`/`AddNewItem` are GONE. Current surface: `WorldGenerator`/`Heightmap.Biome` (biomes), `Ship` (boarding: `IsPlayerInBoat`, `m_players`), `Piece` (structures, `IsPlacedByPlayer`), `ZDOMan` (ZDO create/find), `VisEquipment` per-slot `Set*Item(int hash, ...)` setters, `Character.TeleportTo(pos, rot, bool)`. Verified signatures: `rescue_requirements.md` §4.
- Most game types moved to the **global (empty) namespace** in 1.0.16: `Character`, `Humanoid`, `Player`, `HitData`, `VisEquipment`, `SEMan`, `MonsterAI`, `ItemDrop`, ... (not `Valheim.X`).
- To inspect the game API without a decompiler: use Mono.Cecil from `packages/JotunnLib.2.30.2/build/Mono.Cecil.dll` (PowerShell `Add-Type`) reading the publicized assembly's metadata. Do NOT use reflection `LoadFrom` — missing Unity deps, and an AssemblyResolve handler stack-overflows.
  - Field-writer scan (answers "will this field value survive?"): iterate every type's method bodies, match instructions with `$instr.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Stfld` and check `Operand` (`FieldReference`) name. Gotchas in this bundled Cecil: the code enum is `Mono.Cecil.Cil.Code` (NOT `OpCodeCode`), and `OpCode` class statics are unreachable via PowerShell `::` (they silently return `$null`). ALWAYS run a control scan first on a field with a known writer (e.g. `m_running` → `Character::UpdateWalking`); a broken comparison silently matches nothing.
- Notable 1.0.16 signatures:
  - `HitData`: `int m_statusEffectHash` (string `m_statusEffect` gone). `SEMan.AddStatusEffect(int hash, bool resetTime, int itemLevel, float skillLevel, short variant)`; `SEMan.GetStatusEffect(int hash)`.
  - `Character.AddFire/Spirit/Poison/Frost/LightningDamage(float, short variant)`.
  - `VisEquipment.SetHairItem(int)` / `SetBeardItem(int)` — name→hash via `StringExtensionMethods.GetStableHashCode(string)` (same as game's `SetupNpcHair`/`SetupNpcBeard`). `Humanoid.SetHair(string)` / `SetBeard(string)` remain name-based.
  - `ItemDrop.ItemData.SharedData`: `m_holdDurationMin` / `m_holdStaminaDrain` / `m_holdAnimationState` removed (hold/charge fields).
  - Class `Visequip` is now `VisEquipment`.
- **ZDO boolean keys: an unset (never-`Set`) boolean key can load back as `true`, not `false`.** Observed 2026-10-22: a fresh rescue NPC with `hmnpc_isHirdmandr` never `Set` loaded with `m_isHirdmandr=true` (management GUI instead of the rescue dialog); explicitly `Set("hmnpc_isHirdmandr", false)` on fresh spawn fixed it. **Always `Set` boolean ZDO keys explicitly on fresh spawn — never rely on an absent key defaulting to `false`.** (The `ZDO.GetBool` IL *reads* like absent→`defaultValue(false)`, but runtime differs — trust the in-game test over the IL here.)

## UnityExplorer (in-game Unity inspector)
- Installed under the game's `BepInEx/plugins/` as its own mod: inspects live in-game Unity objects — exact hierarchy names, parent/child structure, components, prefab vs scene object.
- **Use it first for any runtime Unity fact** (a scene object's name, a child's exact name, components on a live object). Cheaper and more reliable than IL string scans — scene-object names are data-driven and often never appear as strings in the assemblies (learned the hard way on the Eikthyrnir/bowl naming).
- Workflow: launch game with it loaded → find the object in the world → read the exact name → use it in mod code (`PrefabManager` clone / child search).

## Layout
- `Hirdmandr/` — source. `Hirdmandr.cs` = plugin entry; `HirdmandrNPC.cs` = core NPC component; `NPCPlayerClone.cs` = decompiled Player clone (already 1.0.16-compatible); `topSM.cs` = state-machine root (7 states, `sub1SM-*.cs`); `HMSkills.cs` = job/skill flags.
- `packages/JotunnLib.2.30.2` — locally rebuilt Jotunn (2026-09-21); only contains Jotunn.dll.
- `Hirdmandr/StateMachines/` — 0-byte dead copies (pending removal). `JotunnModUnity/` — empty skeleton.
- `rescue_requirements.md` (repo root) — Rescue feature: requirements, verified API, plan (R2-R7). Read before any rescue work.
- `rescue_spawn_design.md` (repo root) — **The NPC SPAWN design & decisions (read this FIRST after any compression/cold start).** Ledger + deterministic-hash + periodic-scan spawn refactor; decisions, rejected alternatives, code refs, U1 test, open questions, build checklist.
- `Environment.props`: `VALHEIM_INSTALL`, `MOD_DEPLOYPATH`. `DoPrebuild.props`: `ExecutePrebuild`.

## Rules (Thomas)
- No git operations, ever.
- Never modify base Valheim or distributed BepInEx/Jotunn (deploying the mod's own plugin folder is normal workflow).
- Prefer BepInEx Harmony hooks (pre/post/override) over other patching.
- Ask Thomas before touching the Windows VS Code installation.
- Scratch/temp files go on D: (`D:\OpenCode\`), never C:.
