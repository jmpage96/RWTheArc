using System;
using RimWorld;
using TheArk.Collection;
using Verse;

namespace TheArk.Rescue
{
    public sealed class CompAbilityEffect_Deliverance : CompAbilityEffect
    {
        private CompProperties_Deliverance Settings => (CompProperties_Deliverance)props;
        private ArkCollectionComponent Collection => Current.Game?.GetComponent<ArkCollectionComponent>();
        public override bool CanCast => Settings.ValidConfiguration && parent.pawn.IsColonistPlayerControlled
            && parent.pawn.abilities?.GetAbility(parent.def) == parent && Collection != null;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn animal = target.Pawn;
            bool valid = animal != null && animal.Spawned && !animal.Dead && !animal.Destroyed
                && animal.Map == parent.pawn.Map && animal.def.destroyable
                && AnimalCatalog.SlotFor(animal.def, animal.gender) != RegistrationSlots.None;
            if (!valid && throwMessages)
                Messages.Message("Deliverance requires a living, eligible animal on this map.", MessageTypeDefOf.RejectInput, false);
            return valid;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => Valid(target);

        public override Window ConfirmationDialog(LocalTargetInfo target, Action confirmAction)
        {
            if (!Valid(target) || Collection == null) return null;
            Pawn animal = target.Pawn;
            string progress = IsDuplicate(animal)
                ? "This species and sex are ALREADY REGISTERED. Rescuing this animal adds no collection progress."
                : "This animal can add a missing collection entry. If another rescue fills it before casting finishes, it will still be removed without adding progress.";
            return Dialog_MessageBox.CreateConfirmation(
                $"Use Deliverance on {animal.LabelShortCap}?\n\nSuccess chance: {DeliveranceChance.For(parent.pawn, animal, Settings):P0}.\n\n{progress}\n\nOn success this animal and its carried belongings are permanently removed. On failure it remains and may become a manhunter. Both outcomes use the ability cooldown.",
                confirmAction);
        }

        private bool IsDuplicate(Pawn animal)
        {
            RegistrationSlots slot = AnimalCatalog.SlotFor(animal.def, animal.gender);
            return (Collection.RegisteredSlots(animal.def.defName) & slot) != 0;
        }

        public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
        {
            if (!Valid(target) || Collection == null || !Settings.ValidConfiguration) return null;
            return $"Rescue: {DeliveranceChance.For(parent.pawn, target.Pawn, Settings):P0}"
                + (IsDuplicate(target.Pawn) ? " — already registered" : "");
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!Valid(target, true) || Collection == null) return;
            Pawn animal = target.Pawn;
            string label = animal.LabelShortCap;
            if (!Rand.Chance(DeliveranceChance.For(parent.pawn, animal, Settings)))
            {
                bool retaliation = Rand.Chance(Settings.manhunterChance)
                    && animal.mindState?.mentalStateHandler != null
                    && MentalStateDefOf.Manhunter.Worker.StateCanOccur(animal)
                    && animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter,
                        reason: "A failed Deliverance attempt", forced: false);
                Messages.Message($"Deliverance failed: {label} remains." + (retaliation ? " The animal has become a manhunter!" : ""),
                    animal, retaliation ? MessageTypeDefOf.ThreatSmall : MessageTypeDefOf.NegativeEvent);
                return;
            }
            Map map = animal.Map;
            IntVec3 cell = animal.Position;
            RescueCommitResult result = RescueTransaction.Commit(Collection, animal.def, animal.gender,
                () => RescueRemoval.Remove(animal), () => animal.Destroyed && !animal.Spawned, out Exception error);
            if (error != null) Log.Error("[The Ark] Deliverance removal encountered an exception: " + error);
            if (result == RescueCommitResult.RemovalFailed || result == RescueCommitResult.Invalid)
            {
                Messages.Message("Deliverance could not remove the animal. No collection progress was added.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            // Cosmetics run only after the transaction commits, and cannot undo it.
            Messages.Message($"Deliverance: {label} has been rescued."
                + (result == RescueCommitResult.Duplicate ? " Its species and sex were already registered." : " Added to The Ark collection."),
                new TargetInfo(cell, map), MessageTypeDefOf.PositiveEvent);
            FleckMaker.ThrowDustPuff(cell, map, 1.5f);
        }
    }
}
