using System;
using RimWorld;
using Verse;

namespace TheArk.Sanctuary
{
    public static class SanctuarySilver
    {
        // Bounded work even when a mod assigns an enormous market value. Remaining
        // balance is a single saved number, never a list of spawned stacks or pawns.
        private const int StacksPerClaim = 100;

        internal static int DeliveredCount(int requested, int callbacks, bool spawned, bool destroyed, int remaining)
        {
            int observed = spawned || destroyed ? requested : requested - Math.Clamp(remaining, 0, requested);
            return Math.Clamp(Math.Max(callbacks, observed), 0, requested);
        }

        public static long Claim(Thing ark, bool notify)
        {
            var ledger = Current.Game?.GetComponent<SanctuaryLedgerComponent>();
            if (ledger == null || ark == null || !ark.Spawned || ark.Faction != Faction.OfPlayer
                || ark.TryGetComp<Buildings.CompArkGrant>() == null || !ledger.BeginPayment()) return 0;
            long delivered = 0;
            try
            {
                for (int stack = 0; stack < StacksPerClaim && ledger.OutstandingSilver > 0; stack++)
                {
                    int count = (int)Math.Min(ledger.OutstandingSilver, ThingDefOf.Silver.stackLimit);
                    if (count <= 0) break;
                    Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                    silver.stackCount = count;
                    int callbacks = 0;
                    bool placementError = false;
                    try
                    {
                        GenPlace.TryPlaceThing(silver, ark.Position, ark.Map, ThingPlaceMode.Near,
                            placedAction: (_, amount) => callbacks += amount,
                            extraValidator: cell => cell.InBounds(ark.Map) && cell.Standable(ark.Map) && !cell.Fogged(ark.Map));
                    }
                    catch (Exception exception)
                    {
                        placementError = true;
                        Log.Error("[The Ark] Sanctuary silver placement interrupted: " + exception);
                    }
                    int placed = DeliveredCount(count, callbacks, silver.Spawned, silver.Destroyed, silver.stackCount);
                    ledger.RecordDelivered(placed);
                    delivered += placed;
                    // Unplaced silver remains numerical debt, never an orphaned item.
                    if (!silver.Spawned && !silver.Destroyed) silver.Destroy(DestroyMode.Vanish);
                    if (placed < count || placementError) break;
                }
                if (notify)
                    Messages.Message($"Sanctuary payment: {delivered:N0} silver placed. Outstanding balance: {ledger.OutstandingSilver:N0}.",
                        new TargetInfo(ark.Position, ark.Map), MessageTypeDefOf.NeutralEvent);
                return delivered;
            }
            catch (Exception exception)
            {
                Log.Error("[The Ark] Sanctuary payout stopped; undelivered balance is retained: " + exception);
                if (notify) Messages.Message("Sanctuary silver could not all be placed. The remaining balance is retained in the ledger.", MessageTypeDefOf.RejectInput, false);
                return delivered;
            }
            finally { ledger.EndPayment(); }
        }
    }
}
