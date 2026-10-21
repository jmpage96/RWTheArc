using System;
using RimWorld;
using TheArk.Collection;
using TheArk.Rescue;
using Verse;

namespace TheArk.Sanctuary
{
    internal static class DonationService
    {
        internal static bool Complete(Pawn animal, Thing ark, Pawn worker)
        {
            if (!DonationEligibility.Animal(animal, ark) || !DonationEligibility.Worker(worker, ark)
                || !worker.CanReachImmediate(animal, Verse.AI.PathEndMode.Touch)) return false;
            var collection = Current.Game.GetComponent<ArkCollectionComponent>();
            var ledger = Current.Game.GetComponent<SanctuaryLedgerComponent>();
            if (collection == null || ledger == null) return false;
            DonationEntry entry;
            try { entry = DonationReward.Quote(animal); }
            catch (Exception exception)
            {
                Log.Error("[The Ark] Cannot value sanctuary donation: " + exception);
                Messages.Message("The sanctuary could not value this animal. Nothing was donated.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            RescueCommitResult result = DonationTransaction.Commit(collection, ledger, animal.def, animal.gender, entry,
                () => RescueRemoval.Remove(animal), () => animal.Destroyed && !animal.Spawned, out Exception error);
            if (error != null) Log.Error("[The Ark] Sanctuary removal encountered an exception: " + error);
            if (result != RescueCommitResult.Registered && result != RescueCommitResult.Duplicate)
            {
                Messages.Message("The sanctuary transfer failed. No silver was awarded.", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            long delivered = SanctuarySilver.Claim(ark, false);
            string balance = ledger.OutstandingSilver > 0
                ? $" {delivered:N0} silver placed; {ledger.OutstandingSilver:N0} remains owed. Use Claim sanctuary silver at any Ark."
                : "";
            Messages.Message($"Sanctuary donation: {entry.AnimalName} ({entry.SpeciesLabel}), {entry.Silver:N0} silver earned.{balance}",
                new TargetInfo(ark.Position, ark.Map), MessageTypeDefOf.PositiveEvent);
            return true;
        }
    }
}
