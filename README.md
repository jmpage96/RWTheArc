# The Ark — Milestone 2: Deliverance

A RimWorld **1.7** mod with a buildable Ark, a permanent animal collection, and an **Ark** checklist tab. The Ark grants **Deliverance** to chosen colonists: an ability that attempts to rescue living animals permanently, with a risk of manhunter retaliation on failure.

No DLC, Harmony, or other mod is required. Deliverance uses the base ability system, without Royalty, psyfocus, or psychic sensitivity. Rituals, economy, silver rewards, and victory logic are outside this milestone.

## Install on Windows

1. Use RimWorld 1.7. The local build was verified against **1.7.4984 rev1242**.
2. Close RimWorld and extract `artifacts/TheArk-Milestone2.zip` into its `Mods` directory, overwriting the previous TheArk files when upgrading. The result must contain `Mods/TheArk/About/About.xml` and `Mods/TheArk/Assemblies/TheArk.dll`. Copy the updated Defs as well as the DLL. Alternatively, build this repository and copy its `About`, `Defs`, and `Assemblies` directories into `Mods/TheArk`.
3. Enable **The Ark** after Core in the game's mod list and restart. Animal mods may be anywhere in the valid load order: discovery runs after definitions load.
4. Load a colony or start a new one. Build **The Ark** under **Architect → Misc** (150 steel, 100 wood; no research). It uses Core's comms-console artwork as a placeholder and has no console functionality.
5. Open the bottom **Ark** tab. It works before construction, after destruction, and without a current map.

The mod makes no changes to vanilla files. The collection is stored inside each game save; it is not account-wide. Removing The Ark itself and saving without it can discard its component data. Keeping animal records does not make unrelated changes to a modded save safe.

Existing Milestone 1 saves keep their collection. The grant command is a new component on the existing Ark definition, so previously built Arks acquire it when loaded. The granted ability and its cooldown are saved by RimWorld's pawn ability tracker; destroying or deconstructing the Ark does not revoke them.

## Use Deliverance

1. Select a player-owned Ark and choose **Grant Deliverance**. Select a free colonist from the list. Granting is immediate, works only on the Ark's current map, and does not require a job, resource, or skill threshold. Regranting does not reset an existing ability or cooldown.
2. Select that colonist and choose **Deliverance**, drafted or undrafted. Other colonists have no Deliverance command until granted it.
3. Target a living animal within **24.9 cells**, with line of sight. Wild and tame animals, young animals, sleeping animals, downed animals, and modded animals are accepted if they satisfy the Milestone 1 species/sex rules. Unspawned, dead, nonanimal, and non-destroyable targets are rejected.
4. Confirm the attempt. The dialog shows the current chance and warns explicitly if the entry is already registered. Even a missing entry can become a duplicate if another colonist completes it during warmup; the confirmation explains that case too. Canceling queues no cast and uses no cooldown.
5. After **1.5 seconds** of warmup, success records only missing progress and permanently removes the actual animal, including carried belongings. A positive message and a Core dust-puff effect mark the rescue. The checklist reads the registry directly and updates immediately.
6. Failure leaves the animal in place and rolls **25%** for manhunter retaliation. The normal mental-state worker and handler can refuse it, including when incapacitated, sleeping, already in that state, or otherwise blocked. Deliverance never forces a forbidden mental state. A success or failed probability roll starts the normal **600-tick / 10-second** cooldown. Invalidated targets are rechecked before activation and do not start a cooldown.

Chance is `clamp(0.35 + AnimalsSkill * 0.035 - Wildness * 0.4, 0.05, 0.95)`. Animals skill uses the game's effective level, bounded to 0–20; a missing or disabled skill contributes zero. Wildness uses the target pawn's current `StatDefOf.Wildness` stat, bounded to 0–1. A displayed 98% is `0.98`, not `98`.

With unmodified Core definitions:

| Target | Wildness | Animals 0 | Animals 20 |
| --- | ---: | ---: | ---: |
| Cat | 0 | 35% | 95% |
| Thrumbo | 0.98 | 5% | 65.8% |

The targeting label and confirmation round the displayed percentage; the actual roll uses the unrounded result. Neither success nor failure creates silver or runs any victory logic.

## Configure Deliverance

Edit `Defs/AbilityDefs/Deliverance.xml` and restart RimWorld; these are XML configuration fields, with no settings-menu dependency:

| XML field | Default | Meaning |
| --- | ---: | --- |
| `verbProperties/range` | 24.9 | Range in cells; positive and finite |
| `verbProperties/warmupTime` | 1.5 | Casting warmup in seconds |
| `cooldownTicksRange` | 600 | Cooldown ticks; a single integer or RimWorld IntRange |
| `comps/li/baseChance` | 0.35 | Base probability |
| `comps/li/skillFactor` | 0.035 | Probability added per Animals level |
| `comps/li/wildnessPenalty` | 0.4 | Probability subtracted per unit of wildness |
| `comps/li/minimumChance` | 0.05 | Lower probability cap, strictly above zero |
| `comps/li/maximumChance` | 0.95 | Upper probability cap, strictly below one |
| `comps/li/manhunterChance` | 0.25 | Retaliation roll on failure, from zero to one |

Formula coefficients must be finite; skill/wildness factors cannot be negative, and minimum cannot exceed maximum. Invalid probability configurations disable casting and produce a definition error. Range and cooldown also have definition validation. Cooldown values are game ticks, with 60 ticks per second at normal game speed. Existing cooldowns retain their saved duration; new casts use the updated definition.

## Rescue consistency

Registration is staged before removal in one synchronous game-thread operation. If removal refuses or throws before destruction, the previous species flags are restored, preserving earlier credit. If destruction has already happened, credit is kept even if a later cleanup hook throws. The removal routine uses `WorldPawns.PassToWorld(..., PawnDiscardDecideMode.Discard)` after despawning, rather than leaving a simulated world pawn. Its cleanup finishes discard after an interrupted destruction, or respawns the original pawn if destruction never happened.

No rescued pawn is saved in The Ark, queued for later processing, or simulated off-map. Save/load cannot occur halfway through the synchronous transaction. Exceptions are logged; a failed removal adds no progress, while a committed rescue still reports success. Mod hooks can still throw during engine cleanup or respawning; those integration failures need inspection of the log and cannot be made universally atomic by a mod. Destruction/discard uses vanilla notifications and may affect bonds or quests according to the game and other mods.

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

The retained Milestone 1 debug action adds a flag without rescuing or modifying the animal, and bypasses Deliverance's chance, cooldown, and granting. Repeating it for the same species and sex has no effect. Use Deliverance for normal play. Disable Development mode to hide the debug action.

## Collection rules

- Each eligible loaded `ThingDef` is a species. Eligibility uses `category == Pawn` and the game's `race.Animal` property. Ordinary insects and modded animals are included; humans, inorganic races, and anything the game does not classify as an animal are excluded. No biome, tameability, or vanilla-only whitelist is used.
- Species occupy a single checklist row. Hover over a name to see its internal species ID; search accepts both names and IDs.
- Gendered races require male and female. Sexless races (`hasGenders=false`) require one registration in the Sexless column; male/female show N/A. A race with `forceGender` needs only that sex. A gendered race's `Gender.None` pawn cannot fill either slot.
- Persistence uses exact, ordinal `ThingDef.defName` strings and independent male/female/sexless flags, never short hashes, translated labels, or live pawn/Def references.
- Added animal definitions appear on the next game load/tab open. Removed or newly ineligible definitions with progress appear as **inactive**, remain saved, and do not count toward completion. Reinstalling a mod with the same `defName` restores its progress. Renamed definitions need a future explicit migration; unrelated mods reusing a `defName` share its identity.
- Changes to a race's gender rules preserve historical flags but use the current rules for completion. Old sexless progress does not manufacture a male/female pair.
- Any number of Ark buildings shares one game component. Destroying buildings, leaving maps, and rebuilding do not alter the registry. Completing the checklist currently triggers nothing.

## Source layout and verification

`Source/TheArk/Collection` retains Milestone 1 discovery, records, and persistence. `Buildings` supplies the granting comp; `Rescue` contains the base Ability subclass, effect, configurable probability, transaction, and permanent removal adapter. `UI` displays the registry, and `Debug` retains the temporary registration adapter. There is no polling, Harmony patch, static progress store, or building-owned collection.

Run the headless checks against your game assemblies:

```powershell
dotnet run --project .\Tests\TheArk.Tests.csproj -c Release `
  '-p:RimWorldPath=D:\Games\RimWorld'
```

The harness uses synthetic definitions, RimWorld's actual Scribe serializer, and its XML parser. It tests Milestone 1 persistence, probability boundaries, skill/wildness examples, invalid configuration, duplicate rescue, and injected failures before/after removal. It also loads the real Deliverance XML to validate the custom class, verb, range, cooldown, and effect configuration. It does not invoke live casting, map removal, or mental-state transitions. Its isolated temporary configuration never reads or writes your colony saves. Reflection is limited to initializing headless test fixtures; the shipped mod uses public APIs.

See [manual tests](docs/MANUAL_TESTING.md) and [API verification and limits](docs/API_VERIFICATION.md). A passing headless harness is not a claim that the game UI or construction has been exercised.
