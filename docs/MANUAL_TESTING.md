# Milestone 1 manual checklist

Use a disposable colony/save. Start with Core + The Ark, all DLC disabled, and inspect the development log for errors.

- [ ] The mod enables as 1.7-compatible. Architect → Misc contains The Ark. Place, construct, rotate, select, deconstruct, and destroy it; the Core console placeholder renders without a missing texture or power/interaction behavior.
- [ ] Open Ark before building anything. It lists loaded animal species with Male/Female/Sexless statuses in compact, single-line rows. Hover over a species name to see its internal ID. Search by label and defName, scroll to the last species, and check the tab at a smaller resolution/UI scale.
- [ ] Select a living male animal. With Development mode on, run **The Ark → Register selected animal (M1)**. Only its male slot changes. Register again: no duplicate. Register a female of that species: both slots complete and the completed-species count increases by one.
- [ ] Try no selection, multiple selected animals, a human, a mechanoid, and a corpse. None adds a record. The action does not consume, despawn, move, tame, or kill the animal. Turn Development mode off: the action is unavailable.
- [ ] Register animals without any Ark, with two Arks, and after destroying every Ark. Progress remains identical. Save, exit to desktop, restart, and reload: slots and count persist. Build another Ark and repeat on another map.
- [ ] Start a second colony: its registry is empty. Load the first again: its progress returns. Also add The Ark to a preexisting save and verify an initially empty registry without load errors.
- [ ] Add a compatible animal mod and restart. Its animal ThingDefs appear. Register a pair and save. On a disposable copy only, remove that animal mod and reload/save: its records appear inactive and do not count toward completion. Reinstall it, restart, reload: the same defNames recover their flags. The removed animal mod may independently make that save unloadable; use a simple test mod if necessary.
- [ ] With a compatible fixture mod supplying a sexless animal, register one `Gender.None` pawn. Male/female show N/A; Sexless becomes Registered and the species completes. A forced-female fixture completes with a female alone. Fixture definitions must otherwise be valid 1.7 animal races.
- [ ] Completing any checklist entries has no capture, reward, ritual, psycast, or victory effect.

Execution status: automated tests ran against local 1.7.4984 assemblies; the interactive checks above remain for an in-game session.
