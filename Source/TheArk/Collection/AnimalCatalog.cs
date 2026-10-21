using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TheArk.Collection
{
    public static class AnimalCatalog
    {
        // Deliberately no wild-biome, tameability, DLC, or PawnKind whitelist.
        public static bool IsEligible(ThingDef def)
        {
            return def != null && !string.IsNullOrEmpty(def.defName)
                && def.category == ThingCategory.Pawn && def.race != null && def.race.Animal;
        }

        public static IReadOnlyList<ThingDef> Discover()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading.Where(IsEligible)
                .OrderBy(def => def.label ?? def.defName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(def => def.defName, StringComparer.Ordinal).ToList();
        }

        public static RegistrationSlots RequiredSlots(ThingDef def)
        {
            if (!IsEligible(def)) return RegistrationSlots.None;
            if (!def.race.hasGenders) return RegistrationSlots.Sexless;
            if (def.race.forceGender == Gender.Male) return RegistrationSlots.Male;
            if (def.race.forceGender == Gender.Female) return RegistrationSlots.Female;
            return RegistrationSlots.Male | RegistrationSlots.Female;
        }

        public static RegistrationSlots SlotFor(ThingDef def, Gender gender)
        {
            if (!IsEligible(def)) return RegistrationSlots.None;
            RegistrationSlots slot = !def.race.hasGenders ? RegistrationSlots.Sexless
                : gender == Gender.Male ? RegistrationSlots.Male
                : gender == Gender.Female ? RegistrationSlots.Female : RegistrationSlots.None;
            return (RequiredSlots(def) & slot) != 0 ? slot : RegistrationSlots.None;
        }
    }
}
