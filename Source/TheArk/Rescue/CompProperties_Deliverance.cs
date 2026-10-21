using System.Collections.Generic;
using RimWorld;

namespace TheArk.Rescue
{
    public sealed class CompProperties_Deliverance : CompProperties_AbilityEffect
    {
        public float baseChance = 0.35f;
        public float skillFactor = 0.035f;
        public float wildnessPenalty = 0.4f;
        public float minimumChance = 0.05f;
        public float maximumChance = 0.95f;
        public float manhunterChance = 0.25f;

        public CompProperties_Deliverance() { compClass = typeof(CompAbilityEffect_Deliverance); }

        public bool ValidConfiguration => float.IsFinite(baseChance) && float.IsFinite(skillFactor)
            && float.IsFinite(wildnessPenalty) && float.IsFinite(minimumChance) && float.IsFinite(maximumChance)
            && float.IsFinite(manhunterChance) && minimumChance > 0 && maximumChance < 1
            && minimumChance <= maximumChance && skillFactor >= 0 && wildnessPenalty >= 0
            && manhunterChance >= 0 && manhunterChance <= 1;

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef)) yield return error;
            if (!ValidConfiguration) yield return "Deliverance requires finite coefficients, 0 < minimumChance <= maximumChance < 1, nonnegative skill/wildness factors, and 0 <= manhunterChance <= 1.";
            if (parentDef.verbProperties == null || !float.IsFinite(parentDef.verbProperties.range) || parentDef.verbProperties.range <= 0)
                yield return "Deliverance requires a positive finite range.";
            if (parentDef.cooldownTicksRange.min <= 0 || parentDef.cooldownTicksRange.max < parentDef.cooldownTicksRange.min)
                yield return "Deliverance requires a positive cooldown range.";
        }
    }
}
