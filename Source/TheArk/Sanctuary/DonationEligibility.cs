using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TheArk.Collection;
using Verse;

namespace TheArk.Sanctuary
{
    public static class DonationEligibility
    {
        public static bool Animal(Pawn animal, Thing ark)
        {
            return ark != null && ark.Spawned && !ark.Destroyed && ark.Faction == Faction.OfPlayer
                && ark.TryGetComp<Buildings.CompArkGrant>() != null
                && animal != null && animal.Spawned && !animal.Dead && !animal.Destroyed
                && animal.Map == ark.Map && animal.Faction == Faction.OfPlayer && animal.def.destroyable
                && AnimalCatalog.SlotFor(animal.def, animal.gender) != RegistrationSlots.None;
        }

        public static bool Worker(Pawn worker, Thing ark)
        {
            return worker != null && worker.Spawned && !worker.Dead && !worker.Downed
                && worker.Map == ark.Map && worker.IsColonistPlayerControlled
                && !worker.IsPrisoner && !worker.IsSlave && !worker.Drafted;
        }

        public static bool HasSpecialRelationships(Pawn animal)
        {
            return (animal.Name != null && !animal.Name.Numerical) || animal.playerSettings?.Master != null
                || (animal.relations?.DirectRelations.Count ?? 0) > 0
                || (animal.relations?.VirtualRelations.Count ?? 0) > 0 || ReverseRelations(animal).Any();
        }

        // Snapshot consent, not pawns. A new name/bond/master during the job invalidates
        // consent instead of silently extending permission to a different relationship.
        public static string ConsentFingerprint(Pawn animal)
        {
            var parts = new List<string> { animal.ThingID, animal.Name?.ToStringFull ?? "", animal.playerSettings?.Master?.ThingID ?? "" };
            if (animal.relations != null)
            {
                parts.AddRange(animal.relations.DirectRelations.Select(relation => relation.def.defName + ":" + relation.otherPawn?.ThingID));
                parts.AddRange(animal.relations.VirtualRelations.Select(relation =>
                    relation.def.defName + ":" + relation.record?.GetUniqueLoadID() + ":" + relation.startTicks));
            }
            parts.AddRange(ReverseRelations(animal));
            return string.Join("\n", parts.OrderBy(part => part, System.StringComparer.Ordinal));
        }

        private static IEnumerable<string> ReverseRelations(Pawn animal)
        {
            if (animal.Map == null) yield break;
            foreach (Pawn other in animal.Map.mapPawns.AllPawnsSpawned)
                if (other != animal && other.relations != null)
                    foreach (DirectPawnRelation relation in other.relations.DirectRelations)
                        if (relation.otherPawn == animal) yield return other.ThingID + ":" + relation.def.defName;
        }
    }
}
