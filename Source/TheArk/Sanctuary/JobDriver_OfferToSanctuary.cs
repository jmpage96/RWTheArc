using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheArk.Sanctuary
{
    public sealed class JobDriver_OfferToSanctuary : JobDriver
    {
        private string approvedFingerprint;
        private bool committed;
        private Window confirmation;
        private Pawn Animal => job.targetA.Pawn;
        private Thing Ark => job.targetB.Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref approvedFingerprint, "sanctuaryConsent");
            Scribe_Values.Look(ref committed, "sanctuaryCommitted");
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return DonationEligibility.Animal(Animal, Ark) && DonationEligibility.Worker(pawn, Ark)
                && pawn.Reserve(Animal, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !committed && (!DonationEligibility.Animal(Animal, Ark) || !DonationEligibility.Worker(pawn, Ark)));
            AddFinishAction(_ => { if (confirmation != null) { Window closing = confirmation; confirmation = null; Find.WindowStack.TryRemove(closing); } });
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.WaitWith(TargetIndex.A, 120, useProgressBar: true);
            Toil consent = ToilMaker.MakeToil("SanctuaryConsent");
            consent.defaultCompleteMode = ToilCompleteMode.Never;
            consent.initAction = RequestConsent;
            // Restores the prompt after loading a game saved while waiting for consent.
            consent.tickIntervalAction = _ => { if (confirmation == null) RequestConsent(); };
            yield return consent;
            yield return Toils_General.Do(() =>
            {
                if (committed) return;
                if (!DonationEligibility.Animal(Animal, Ark) || approvedFingerprint != DonationEligibility.ConsentFingerprint(Animal))
                {
                    Messages.Message("This animal or its relationships changed. Order the offering again to confirm it.", MessageTypeDefOf.RejectInput, false);
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                committed = DonationService.Complete(Animal, Ark, pawn);
                if (!committed) EndJobWith(JobCondition.Incompletable);
            });
        }

        private void RequestConsent()
        {
            if (committed || pawn.CurJob != job || !DonationEligibility.Animal(Animal, Ark)) return;
            if (approvedFingerprint != null) { ReadyForNextToil(); return; }
            DonationEntry quote;
            try { quote = DonationReward.Quote(Animal); }
            catch (Exception exception)
            {
                Log.Error("[The Ark] Cannot value sanctuary donation: " + exception);
                Messages.Message("The sanctuary could not value this animal. Nothing was donated.", MessageTypeDefOf.RejectInput, false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            string fingerprint = DonationEligibility.ConsentFingerprint(Animal);
            string warning = DonationEligibility.HasSpecialRelationships(Animal)
                ? "\n\nThis is a named animal, bonded pet, assigned animal, or has special relationships. The transfer is permanent and may affect those relationships."
                : "";
            confirmation = Dialog_MessageBox.CreateConfirmation(
                $"Offer {quote.AnimalName} ({quote.SpeciesLabel}) to the sanctuary?\n\nCurrent market value: {quote.MarketValue:0.##}. Multiplier: {(quote.FullyTrained ? "1.50 (fully trained)" : "1.25 (standard)")}.\nSilver earned: {quote.Silver:N0}. The final amount uses its value when the offering completes.\n\nThe animal and its carried belongings will be permanently removed. If silver cannot fit near the Ark, the balance remains claimable in the ledger.{warning}",
                () =>
                {
                    if (pawn.CurJob != job || !DonationEligibility.Animal(Animal, Ark)) return;
                    approvedFingerprint = fingerprint;
                    confirmation = null;
                    ReadyForNextToil();
                },
                () =>
                {
                    confirmation = null;
                    if (pawn.CurJob == job) EndJobWith(JobCondition.Incompletable);
                });
            Find.WindowStack.Add(confirmation);
        }
    }
}
