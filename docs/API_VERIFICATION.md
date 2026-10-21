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
| M2 granting | `ThingComp.CompGetGizmosExtra`, `MapPawns.FreeColonistsSpawned`, `Pawn_AbilityTracker(Pawn)`, `GetAbility`, and `GainAbility` compile. `ThingWithComps.ExposeData` initializes comps from the current definition when loading, supporting existing M1 buildings. |
| M2 ability/casting | `Ability`, `Verb_CastAbility`, `CompAbilityEffect.Valid/CanApplyOn/Apply`, and `ConfirmationDialog(LocalTargetInfo, Action)` were inspected. Base `QueueCastingJob` displays the comp confirmation before ordering the casting job. The real XML parser reads the supplied custom Ability/effect types, range, and cooldown. No Psycast subclass or Royalty-required definition is used. |
| M2 cooldown/persistence | Base `Ability.PreActivate` starts the configured cooldown before applying effects, so a failed probability roll also consumes it. Base `Ability.ExposeData` saves the end tick/duration and definition. `Pawn.ExposeData` deep-scribes its ability tracker; `Pawn.Tick` ticks abilities without a Royalty gate. The custom constructor `(Pawn)` supports tracker loading. |
| M2 wildness | Core `Stats_Pawns_General.xml` defines Wildness as `PercentZero`, with min 0/max 1. Core Cat has Wildness 0 and Thrumbo 0.98. The effect calls `animal.GetStatValue(StatDefOf.Wildness)` so stat modifications apply. Effective Animals skill comes from `SkillRecord.Level`. |
| M2 retaliation | `TryStartMentalState(MentalStateDef, string reason = null, bool forced = false, ...)` checks the state's worker and handler restrictions when not forced. The effect explicitly checks `MentalStateDefOf.Manhunter.Worker.StateCanOccur` and calls with `forced:false`. It never sets the mental-state field directly. |
| M2 removal | `Pawn.Destroy(Vanish)` can call `WorldPawns.PassToWorld` unless already being discarded. `PassToWorld(pawn, PawnDiscardDecideMode.Discard)` invokes destruction and discard while marking the pawn as being discarded, without adding it to a simulated world-pawn list. `RemoveAndDiscardPawnViaGC`, `Contains`, `RemovePawn`, `Pawn.Discard`, and `GenSpawn.Spawn(Thing, IntVec3, Map, Rot4, ...)` were inspected and compile for cleanup/rollback. |
| M2 art | `UI/Commands/ReleaseAnimals` exists in Core Assets.zip; grant and ability reuse it. `FleckMaker.ThrowDustPuff(IntVec3, Map, float)` supplies the rescue effect. |

No invented API signatures, fallback game stubs, downloaded reference assemblies, or redistributed game binaries are used. The only bundled assembly is TheArk.dll.

## Remaining assumptions and verification limits

- The user confirmed M1 in-game functionality. The agent has not run M2 in a colony. Granting, live targeting/confirmation, job execution, actual map despawn/discard, manhunter transition, ability/cooldown save/load, and M1 building upgrades still need the M2 manual checklist. Headless tests exercise probability, transaction failure paths, registry persistence, and isolated ability XML parsing; they do not emulate these live interactions.
- Rescue staging and rollback are tested with injected removal callbacks. Ordinary engine calls are synchronous, but arbitrary mod hooks can throw even during cleanup/respawn. In that case the engine may already have applied non-reversible side effects (e.g. quest or bond notifications); the mod preserves credit after confirmed destruction and logs exceptions rather than claiming universal rollback.
- The current implementation assumes ordinary mod definitions are stable once game loading finishes. Opening the tab refreshes the catalog; runtime definition edits while the tab remains open are not a supported contract.
- A modded race must identify itself through RimWorld's `race.Animal` classification. Animals disguised as another race type, special spawn-only eligibility rules, bespoke gender systems, and unique defName migrations need future policy/integration work.
- `defName` is the stable identifier available here. It cannot distinguish two mods deliberately replacing the same definition or infer that a renamed definition is the old species.
- Future 1.7 updates can change assemblies or APIs. Rebuild and rerun the tests against the installation being used. This is not a promise of compatibility with every later 1.7 build.

For background only, Ludeon's [official modding tutorial](https://www.ludeon.com/forums/index.php?topic=33219.0) describes linking XML definitions to C# classes. Its old runtime setup does not govern this 1.7 project; local assemblies do.
