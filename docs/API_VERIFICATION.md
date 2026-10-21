# API verification

Verified against the user's installed **RimWorld 1.7.4984 rev1242** at `C:/Program Files (x86)/Steam/steamapps/common/RimWorld`. Local assembly inspection and compilation are the authority for this implementation.

| Surface | Verified behavior |
| --- | --- |
| Runtime | The installed runtimeconfig specifies `net10.0`; game code is in root `Assembly-CSharp.dll`, and Unity-compatible UI types are in `UnityStubs.dll`. |
| Component construction | `Verse.Game.FillComponents()` discovers nonabstract GameComponent subclasses and invokes their constructor with the owning `Game`. No XML registration or patch is needed. |
| Component lifecycle | `GameComponent.ExposeData()` and `FinalizeInit()` are virtual. `Game.GetComponent<T>()` exists. Game serializes its component list using `LookMode.Deep`. |
| Serialization | `Scribe_Collections.Look<T>(ref List<T>, string, LookMode, params object[])`, `Scribe_Values.Look`, `IExposable`, and `LoadSaveMode.PostLoadInit` exist. A public parameterless record constructor supports deep loading. Actual Scribe round trips passed in the harness. |
| Discovery | `DefDatabase<ThingDef>.AllDefsListForReading`, `GetNamedSilentFail`, `ThingDef.category`, `race.Animal`, `hasGenders`, and `forceGender` exist. The local `Animal` getter uses the game's tool-user, organic-flesh, and entity classification rules. |
| Gender generation | The local PawnGenerator honors `forceGender` for races with genders. Sexless and forced-sex policies are deliberate Ark design decisions, not guarantees that arbitrary mods use those fields correctly. |
| Checklist | `RimWorld.MainTabWindow`, `RequestedTabSize`, `PostOpen`, `DoWindowContents`, `MainButtonDef.validWithoutMap`, and the used Widgets/Text/Rect/Vector2 APIs compile against the installed assemblies. Core MainButtons.xml uses the same definition structure. |
| Developer action | `DebugActionAttribute` and `AllowedGameStates` are in **LudeonTK**, not Verse. The verified attribute accepts category/name and the `allowedGameStates` field; `PlayingOnMap` exists. `Find.Selector.SingleSelectedThing`, `Prefs.DevMode`, and the used Messages overload compile. |
| Building/texture | `BuildingBase`, `Misc`, resources, affordance, and destruction sound are present in Core XML. The Core comms console uses the same texture path, graphic class, and draw size. Directional texture entries were found in the local Assets.zip. The Ark uses plain `Verse.Building`, with no console/power/container components. |

No invented API signatures, fallback game stubs, downloaded reference assemblies, or redistributed game binaries are used. The only bundled assembly is TheArk.dll.

## Remaining assumptions and verification limits

- The actual game window was not used for a colony test. Mod startup, XML inheritance/cross-reference resolution in a complete mod load, texture rendering, construction, tab layout, debug-menu interaction, and full colony-save loading still need the manual checklist. Compilation and isolated serializer tests cannot verify those interactions.
- The current implementation assumes ordinary mod definitions are stable once game loading finishes. Opening the tab refreshes the catalog; runtime definition edits while the tab remains open are not a supported contract.
- A modded race must identify itself through RimWorld's `race.Animal` classification. Animals disguised as another race type, special spawn-only eligibility rules, bespoke gender systems, and unique defName migrations need future policy/integration work.
- `defName` is the stable identifier available here. It cannot distinguish two mods deliberately replacing the same definition or infer that a renamed definition is the old species.
- Future 1.7 updates can change assemblies or APIs. Rebuild and rerun the tests against the installation being used. This is not a promise of compatibility with every later 1.7 build.

For background only, Ludeon's [official modding tutorial](https://www.ludeon.com/forums/index.php?topic=33219.0) describes linking XML definitions to C# classes. Its old runtime setup does not govern this 1.7 project; local assemblies do.
