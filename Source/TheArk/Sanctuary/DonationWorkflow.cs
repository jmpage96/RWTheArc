using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheArk.Sanctuary
{
    public static class DonationWorkflow
    {
        public static void Begin(Thing ark)
        {
            if (ark == null || !ark.Spawned) return;
            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = true, canTargetAnimals = true, canTargetHumans = false,
                canTargetMechs = false, canTargetBuildings = false, canTargetLocations = false,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = target => DonationEligibility.Animal(target.Thing as Pawn, ark)
            }, target => ChooseWorker(ark, target.Pawn));
        }

        private static void ChooseWorker(Thing ark, Pawn animal)
        {
            if (!DonationEligibility.Animal(animal, ark)) return;
            var options = new List<FloatMenuOption>();
            foreach (Pawn worker in ark.Map.mapPawns.FreeColonistsSpawned)
            {
                if (!DonationEligibility.Worker(worker, ark)) continue;
                bool reachable = worker.CanReserveAndReach(animal, PathEndMode.Touch, Danger.Some)
                    && worker.CanReach(ark, PathEndMode.Touch, Danger.Some);
                options.Add(new FloatMenuOption(worker.LabelShortCap + (reachable ? "" : " (unavailable or unreachable)"),
                    reachable ? () => Order(worker, ark, animal) : null));
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("No available undrafted colonist", null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void Order(Pawn worker, Thing ark, Pawn animal)
        {
            if (!DonationEligibility.Animal(animal, ark) || !DonationEligibility.Worker(worker, ark)
                || !worker.CanReserveAndReach(animal, PathEndMode.Touch, Danger.Some))
            {
                Messages.Message("The sanctuary offering is no longer available.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TheArk_OfferToSanctuary"), animal, ark);
            job.playerForced = true;
            worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}
