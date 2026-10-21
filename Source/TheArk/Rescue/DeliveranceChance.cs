using System;
using RimWorld;
using Verse;

namespace TheArk.Rescue
{
    public static class DeliveranceChance
    {
        public static float Calculate(int animalsSkill, float wildness, CompProperties_Deliverance settings)
        {
            if (!settings.ValidConfiguration) throw new ArgumentException("Invalid Deliverance configuration.");
            // Wildness is a fraction, not a displayed percentage. Malformed mod values
            // are conservative, and the final probability always stays inside the caps.
            if (!float.IsFinite(wildness)) wildness = 1f;
            double raw = settings.baseChance + Math.Clamp(animalsSkill, 0, 20) * (double)settings.skillFactor
                - Math.Clamp(wildness, 0f, 1f) * (double)settings.wildnessPenalty;
            return (float)Math.Clamp(raw, settings.minimumChance, settings.maximumChance);
        }

        public static float For(Pawn caster, Pawn animal, CompProperties_Deliverance settings)
        {
            int skill = caster.skills?.GetSkill(SkillDefOf.Animals)?.Level ?? 0;
            return Calculate(skill, animal.GetStatValue(StatDefOf.Wildness), settings);
        }
    }
}
