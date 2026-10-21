using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheArk.Sanctuary
{
    public static class DonationReward
    {
        public static bool IsFullyTrained(Pawn animal)
        {
            if (animal?.training == null) return false;
            return AllApplicableLearned(DefDatabase<TrainableDef>.AllDefsListForReading,
                training => training != TrainableDefOf.Tameness
                    && Pawn_TrainingTracker.CanAssignToTrain(training, animal.def, out _),
                training => animal.training.HasLearned(training));
        }

        // Species-level applicability includes not-yet-learned prerequisites, uses
        // the race's adult base body size, and never treats tameness as bonus training.
        internal static bool AllApplicableLearned<T>(IEnumerable<T> categories, Func<T, bool> applicable, Func<T, bool> learned)
        {
            bool any = false;
            foreach (T category in categories)
            {
                if (!applicable(category)) continue;
                any = true;
                if (!learned(category)) return false;
            }
            return any;
        }

        public static long Calculate(float currentMarketValue, bool fullyTrained)
        {
            if (!float.IsFinite(currentMarketValue) || currentMarketValue < 0)
                throw new ArgumentOutOfRangeException(nameof(currentMarketValue), "Market value must be finite and nonnegative.");
            decimal reward = decimal.Round((decimal)currentMarketValue * (fullyTrained ? 1.50m : 1.25m), 0, MidpointRounding.AwayFromZero);
            if (reward > long.MaxValue) throw new OverflowException("Sanctuary reward exceeds the supported silver total.");
            return (long)reward;
        }

        public static DonationEntry Quote(Pawn animal)
        {
            bool fullyTrained = IsFullyTrained(animal);
            float marketValue = animal.GetStatValue(StatDefOf.MarketValue, cacheStaleAfterTicks: 0);
            return new DonationEntry(animal.Name?.ToStringFull ?? animal.LabelShort, animal.def.defName,
                animal.def.label ?? animal.def.defName, animal.gender, Calculate(marketValue, fullyTrained),
                fullyTrained, marketValue, Find.TickManager.TicksGame);
        }
    }
}
