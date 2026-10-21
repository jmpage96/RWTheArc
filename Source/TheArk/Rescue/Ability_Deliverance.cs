using RimWorld;
using Verse;

namespace TheArk.Rescue
{
    public sealed class Ability_Deliverance : Ability
    {
        public Ability_Deliverance(Pawn pawn) : base(pawn) { }
        public Ability_Deliverance(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // Revalidate at execution, including loaded/queued jobs and targets that
            // died or left the map during warmup. Invalid casts do not consume cooldown.
            if (!CanCast || !verb.ValidateTarget(target, true)) return false;
            return base.Activate(target, dest);
        }
    }
}
