using System;
using TheArk.Collection;
using TheArk.Rescue;
using Verse;

namespace TheArk.Sanctuary
{
    internal static class DonationTransaction
    {
        internal static RescueCommitResult Commit(ArkCollectionComponent collection, SanctuaryLedgerComponent ledger,
            ThingDef species, Gender sex, DonationEntry entry, Action remove, Func<bool> consumed, out Exception error)
        {
            error = null;
            if (consumed() || !ledger.CanRecord(entry) || species == null || entry.SpeciesId != species.defName
                || !ledger.BeginDonation()) return RescueCommitResult.Invalid;
            try
            {
                RescueCommitResult result = RescueTransaction.Commit(collection, species, sex, remove, consumed, out error);
                if (result == RescueCommitResult.Registered || result == RescueCommitResult.Duplicate) ledger.Record(entry);
                return result;
            }
            finally { ledger.EndDonation(); }
        }
    }
}
