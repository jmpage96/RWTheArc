# The Ark — Milestone 1

A RimWorld **1.7** mod with a buildable Ark, a game-wide animal registry, and an **Ark** checklist tab. The temporary developer action records a selected animal without moving, consuming, despawning, or otherwise changing it. Registration is only a milestone test mechanism; it does not yet represent a rescued animal.

No DLC, Harmony, or other mod is required. Capture, psycasts, rituals, rewards, and victory logic are outside this milestone.

## Install on Windows

1. Use RimWorld 1.7. The local build was verified against **1.7.4984 rev1242**.
2. Extract `artifacts/TheArk-Milestone1.zip` into your RimWorld `Mods` directory. The result must contain `Mods/TheArk/About/About.xml` and `Mods/TheArk/Assemblies/TheArk.dll`. Alternatively, build this repository and copy its `About`, `Defs`, and `Assemblies` directories into `Mods/TheArk`.
3. Enable **The Ark** after Core in the game's mod list and restart. Animal mods may be anywhere in the valid load order: discovery runs after definitions load.
4. Load a colony or start a new one. Build **The Ark** under **Architect → Misc** (150 steel, 100 wood; no research). It uses Core's comms-console artwork as a placeholder and has no console functionality.
5. Open the bottom **Ark** tab. It works before construction, after destruction, and without a current map.

The mod makes no changes to vanilla files. The collection is stored inside each game save; it is not account-wide. Removing The Ark itself and saving without it can discard its component data. Keeping animal records does not make unrelated changes to a modded save safe.

## Build on Windows

Install the **.NET 10 SDK** and a local RimWorld 1.7 installation. This 1.7 build uses Waylith/.NET 10, with `Assembly-CSharp.dll` and `UnityStubs.dll` in the installation root. Do not use a 1.6 `net472`/Unity project template.

From the repository root in PowerShell:

```powershell
dotnet build .\Source\TheArk\TheArk.csproj -c Release `
  '-p:RimWorldPath=C:\Program Files (x86)\Steam\steamapps\common\RimWorld'
```

Output: `Assemblies/TheArk.dll`. The game references have `Private=false`; copyrighted game assemblies are not copied or shipped. There are no third-party NuGet dependencies.

The default path is the Steam installation shown above. For another location, pass `-p:RimWorldPath=...`, set the `RIMWORLD_PATH` environment variable, or create the ignored file `Source/RimWorld.local.props`:

```xml
<Project>
  <PropertyGroup>
    <RimWorldPath>D:/Games/RimWorld</RimWorldPath>
  </PropertyGroup>
</Project>
```

Build, test, and package a distributable ZIP:

```powershell
.\Tools\Package.ps1 -Test -RimWorldPath 'D:\Games\RimWorld'
```

The ZIP includes the compiled mod, source, tests, and documentation. Local tools, inspected game code, local configuration, and game DLLs are excluded.

## Temporary registration

Enable **Development mode** in game options. Select exactly one living animal, open **Debug actions**, and choose **The Ark → Register selected animal (M1)**. Repeat with the other sex and inspect the Ark tab. Search the debug menu for the action name if necessary.

Wild and tame animals are accepted for testing, with no Ark, ownership, map, or adulthood requirement. Repeating the action for the same species and sex has no effect. Dead pawns, nonanimals, and invalid selections are rejected. Disable Development mode to hide the action; there is no normal-play registration button.

## Collection rules

- Each eligible loaded `ThingDef` is a species. Eligibility uses `category == Pawn` and the game's `race.Animal` property. Ordinary insects and modded animals are included; humans, inorganic races, and anything the game does not classify as an animal are excluded. No biome, tameability, or vanilla-only whitelist is used.
- Species occupy a single checklist row. Hover over a name to see its internal species ID; search accepts both names and IDs.
- Gendered races require male and female. Sexless races (`hasGenders=false`) require one registration in the Sexless column; male/female show N/A. A race with `forceGender` needs only that sex. A gendered race's `Gender.None` pawn cannot fill either slot.
- Persistence uses exact, ordinal `ThingDef.defName` strings and independent male/female/sexless flags, never short hashes, translated labels, or live pawn/Def references.
- Added animal definitions appear on the next game load/tab open. Removed or newly ineligible definitions with progress appear as **inactive**, remain saved, and do not count toward completion. Reinstalling a mod with the same `defName` restores its progress. Renamed definitions need a future explicit migration; unrelated mods reusing a `defName` share its identity.
- Changes to a race's gender rules preserve historical flags but use the current rules for completion. Old sexless progress does not manufacture a male/female pair.
- Any number of Ark buildings shares one game component. Destroying buildings, leaving maps, and rebuilding do not alter the registry. Completing the checklist currently triggers nothing.

## Source layout and verification

`Source/TheArk/Collection` owns discovery, eligibility, records, and persistence. `UI` displays the registry, and `Debug` is the temporary registration adapter. Future capture code can call `ArkCollectionComponent.TryRegister(Pawn)` after its own successful rescue operation. There is no polling, Harmony patch, static progress store, or building-owned collection.

Run the headless checks against your game assemblies:

```powershell
dotnet run --project .\Tests\TheArk.Tests.csproj -c Release `
  '-p:RimWorldPath=D:\Games\RimWorld'
```

The harness uses synthetic definitions and RimWorld's actual Scribe serializer. It tests discovery, eligibility, pair/sexless/forced-sex rules, duplicate handling, independent game state, save/load, absent definitions across a second save, restored definitions, duplicate record merging, and empty component data. Its isolated temporary configuration never reads or writes your colony saves. Reflection is limited to initializing headless test fixtures; the shipped mod uses public APIs.

See [manual tests](docs/MANUAL_TESTING.md) and [API verification and limits](docs/API_VERIFICATION.md). A passing headless harness is not a claim that the game UI or construction has been exercised.
