# Milestone 2 manual checklist

Use a disposable colony/save. Start with Core + The Ark, all DLC disabled, and inspect the development log for errors.

## Deliverance

- [ ] Upgrade an M1 save by replacing About, Defs, and TheArk.dll with the M2 package and restarting. Previous progress remains. A previously built Ark has **Grant Deliverance**; rebuilding is unnecessary.
- [ ] Select an Ark, use **Grant Deliverance**, choose one free colonist. Only that colonist gains Deliverance. Repeat the grant: it does not duplicate the ability or clear its cooldown. Prisoners and slaves are not offered.
- [ ] Use two handlers with effective Animals levels 0 and 20. With unmodified Core stats, a cat (wildness 0) shows 35% and 95%, respectively. A thrumbo (0.98) shows 5% and about 66% (actual 65.8%). Confirm these through the targeting label and dialog. Repeat with a wild and a tamed animal.
- [ ] Cast drafted and undrafted. Range is 24.9 cells, line of sight is required, and warmup is 1.5 seconds. Try a distant animal, one behind a wall, a human, a mechanoid, and a corpse. Invalid targets cannot be rescued. Kill or despawn a valid target during warmup: no registration or cooldown begins.
- [ ] Confirm a successful rescue. The animal disappears without a corpse and cannot reappear as a world pawn; its species/sex flag updates and a positive message/effect appears. Save, restart, and load: the animal stays gone and the flag remains.
- [ ] Obtain a failed attempt (repeat level-0 thrumbo casts, using the vanilla **DEV: Reset cooldown** gizmo if needed). The original animal remains and no new flag is added. The ability has a 600-tick cooldown on success and failure. Save while cooling down and reload: the remaining cooldown is preserved.
- [ ] In a disposable test copy of Deliverance.xml, set `manhunterChance` to `1`, restart, and fail a rescue on an awake, mobile animal. It enters the normal manhunter mental state. Repeat on a sleeping or downed animal: it remains present and the state handler refuses the inappropriate transition. Restore the value to `0.25` after testing. A value of `0` must prevent retaliation.
- [ ] Rescue another animal with the same species/sex. The confirmation explicitly says **ALREADY REGISTERED** and warns of no progress. Cancel: animal, registry, and cooldown are unchanged. Confirm and succeed: the duplicate vanishes, but counts do not increase.
- [ ] Order two handlers to rescue two animals of the same species/sex before either completes. The confirmation for the initially missing entry explains the potential duplicate. Both may succeed, with exactly one new collection entry.
- [ ] With a compatible animal mod, grant and use Deliverance on its species. Test its male/female pair, plus sexless/forced-sex fixtures if available. The same M1 eligibility and slot rules apply; a sexless animal fills only the Sexless slot.
- [ ] Destroy all Arks after granting Deliverance. The handler keeps the ability and can still rescue. Save/reload with no Ark: ability, cooldown, and collection remain. An ungranted colonist still cannot cast it. Building another Ark can grant it to a new colonist.
- [ ] On a test copy, change range/cooldown/formula coefficients, restart, and verify subsequent casts use them. No Royalty, psyfocus meter, or DLC gate appears. There is no silver reward, ritual, economy, or victory trigger.

For forced success/failure testing without changing the probability caps to 0/1, set both minimum and maximum to `0.999` or `0.001` temporarily. Outcomes remain probabilistic; repeat attempts rather than assuming a guarantee. Restore the shipped XML afterwards.

## Milestone 1 regression

- [ ] The mod enables as 1.7-compatible. Architect → Misc contains The Ark. Place, construct, rotate, select, deconstruct, and destroy it; the Core console placeholder renders without a missing texture or power/interaction behavior.
- [ ] Open Ark before building anything. It lists loaded animal species with Male/Female/Sexless statuses in compact, single-line rows. Hover over a species name to see its internal ID. Search by label and defName, scroll to the last species, and check the tab at a smaller resolution/UI scale.
- [ ] Select a living male animal. With Development mode on, run **The Ark → Register selected animal (M1)**. Only its male slot changes. Register again: no duplicate. Register a female of that species: both slots complete and the completed-species count increases by one.
- [ ] Try no selection, multiple selected animals, a human, a mechanoid, and a corpse. None adds a record. The action does not consume, despawn, move, tame, or kill the animal. Turn Development mode off: the action is unavailable.
- [ ] Register animals without any Ark, with two Arks, and after destroying every Ark. Progress remains identical. Save, exit to desktop, restart, and reload: slots and count persist. Build another Ark and repeat on another map.
- [ ] Start a second colony: its registry is empty. Load the first again: its progress returns. Also add The Ark to a preexisting save and verify an initially empty registry without load errors.
- [ ] Add a compatible animal mod and restart. Its animal ThingDefs appear. Register a pair and save. On a disposable copy only, remove that animal mod and reload/save: its records appear inactive and do not count toward completion. Reinstall it, restart, reload: the same defNames recover their flags. The removed animal mod may independently make that save unloadable; use a simple test mod if necessary.
- [ ] With a compatible fixture mod supplying a sexless animal, register one `Gender.None` pawn. Male/female show N/A; Sexless becomes Registered and the species completes. A forced-female fixture completes with a female alone. Fixture definitions must otherwise be valid 1.7 animal races.
- [ ] The retained M1 debug action still only registers flags. It neither removes animals nor triggers rewards, rituals, or victory effects.

Execution status: M1 was confirmed working in game by the user. M2 automated checks ran against local 1.7.4984 assemblies; the M2 interactive checks above have not yet been run by the agent.
