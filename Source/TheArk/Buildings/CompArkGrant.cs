using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using TheArk.Sanctuary;
using TheArk.UI;

namespace TheArk.Buildings
{
    public sealed class CompProperties_ArkGrant : CompProperties
    {
        public CompProperties_ArkGrant() { compClass = typeof(CompArkGrant); }
    }

    // A comp also attaches to existing M1 Verse.Building instances when loading.
    public sealed class CompArkGrant : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!parent.Spawned || parent.Faction != Faction.OfPlayer) yield break;
            yield return new Command_Action
            {
                defaultLabel = "Grant Deliverance",
                defaultDesc = "Choose a colonist on this map to receive Deliverance permanently. No psyfocus is needed, and the gift survives the loss of the Ark.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ReleaseAnimals"),
                action = ChooseRecipient
            };
            yield return new Command_Action
            {
                defaultLabel = "Offer to Sanctuary",
                defaultDesc = "Choose a colony animal and a colonist to donate it permanently. Donations always succeed and earn silver based on current market value and completed training.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ReleaseAnimals"),
                action = () => DonationWorkflow.Begin(parent)
            };
            yield return new Command_Action
            {
                defaultLabel = "Sanctuary Ledger",
                defaultDesc = "View donations, silver, and recent sanctuary arrivals.",
                action = () => Find.WindowStack.Add(new Dialog_SanctuaryLedger())
            };
            SanctuaryLedgerComponent ledger = Current.Game?.GetComponent<SanctuaryLedgerComponent>();
            if (ledger != null && ledger.OutstandingSilver > 0)
                yield return new Command_Action
                {
                    defaultLabel = "Claim sanctuary silver",
                    defaultDesc = "Place outstanding sanctuary payments near this Ark. Any balance that cannot fit stays in the ledger.",
                    action = () => SanctuarySilver.Claim(parent, true)
                };
        }

        private void ChooseRecipient()
        {
            if (!parent.Spawned || parent.Faction != Faction.OfPlayer) return;
            AbilityDef ability = DefDatabase<AbilityDef>.GetNamed("TheArk_Deliverance");
            var options = new List<FloatMenuOption>();
            foreach (Pawn colonist in parent.Map.mapPawns.FreeColonistsSpawned)
            {
                if (!CanGrantTo(colonist)) continue;
                bool alreadyGranted = colonist.abilities?.GetAbility(ability) != null;
                options.Add(new FloatMenuOption(colonist.LabelShortCap + (alreadyGranted ? " (already granted)" : ""),
                    alreadyGranted ? null : () => Grant(colonist, ability)));
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("No available colonist", null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private bool CanGrantTo(Pawn pawn)
        {
            return parent.Spawned && parent.Faction == Faction.OfPlayer && pawn != null && pawn.Spawned
                && pawn.Map == parent.Map && !pawn.Dead && pawn.IsColonistPlayerControlled
                && !pawn.IsPrisoner && !pawn.IsSlave;
        }

        private void Grant(Pawn pawn, AbilityDef ability)
        {
            if (!CanGrantTo(pawn)) return;
            // Existing saves without a tracker are supported without depending on a DLC.
            if (pawn.abilities == null) pawn.abilities = new Pawn_AbilityTracker(pawn);
            if (pawn.abilities.GetAbility(ability) != null) return;
            pawn.abilities.GainAbility(ability);
            Messages.Message($"The Ark grants Deliverance to {pawn.LabelShortCap}.", pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
