using System;
using System.Collections.Generic;
using Verse;

namespace TheArk.Sanctuary
{
    public sealed class SanctuaryLedgerComponent : GameComponent
    {
        public const int HistoryLimit = 50;
        private long totalDonated, totalSilverAwarded, totalFullyTrained, outstandingSilver;
        private List<DonationEntry> recent = new List<DonationEntry>();
        private bool donationInProgress, paymentInProgress;

        public SanctuaryLedgerComponent(Game game) { }
        public long TotalDonated => totalDonated;
        public long TotalSilverAwarded => totalSilverAwarded;
        public long TotalFullyTrained => totalFullyTrained;
        public long OutstandingSilver => outstandingSilver;
        public IReadOnlyList<DonationEntry> Recent => recent.AsReadOnly();

        internal bool CanRecord(DonationEntry entry)
        {
            return entry != null && entry.Silver >= 0 && totalDonated < long.MaxValue
                && totalFullyTrained < long.MaxValue && entry.Silver <= long.MaxValue - outstandingSilver
                && totalSilverAwarded <= long.MaxValue - outstandingSilver - entry.Silver;
        }

        internal void Record(DonationEntry entry)
        {
            if (!CanRecord(entry)) throw new InvalidOperationException("Invalid or overflowing sanctuary entry.");
            totalDonated++;
            if (entry.FullyTrained) totalFullyTrained++;
            outstandingSilver += entry.Silver;
            recent.Insert(0, entry);
            if (recent.Count > HistoryLimit) recent.RemoveRange(HistoryLimit, recent.Count - HistoryLimit);
        }

        internal void RecordDelivered(int amount)
        {
            if (amount < 0 || amount > outstandingSilver) throw new ArgumentOutOfRangeException(nameof(amount));
            outstandingSilver -= amount;
            totalSilverAwarded += amount;
        }

        internal bool BeginDonation() { if (donationInProgress) return false; donationInProgress = true; return true; }
        internal void EndDonation() { donationInProgress = false; }
        internal bool BeginPayment() { if (paymentInProgress) return false; paymentInProgress = true; return true; }
        internal void EndPayment() { paymentInProgress = false; }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref totalDonated, "sanctuaryTotalDonated");
            Scribe_Values.Look(ref totalSilverAwarded, "sanctuarySilverAwarded");
            Scribe_Values.Look(ref totalFullyTrained, "sanctuaryFullyTrained");
            Scribe_Values.Look(ref outstandingSilver, "sanctuaryOutstandingSilver");
            Scribe_Collections.Look(ref recent, "sanctuaryRecent", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                recent = recent ?? new List<DonationEntry>();
                recent.RemoveAll(entry => entry == null);
                if (recent.Count > HistoryLimit) recent.RemoveRange(HistoryLimit, recent.Count - HistoryLimit);
            }
        }
    }
}
