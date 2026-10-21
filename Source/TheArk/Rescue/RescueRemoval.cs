using System;
using RimWorld.Planet;
using Verse;

namespace TheArk.Rescue
{
    internal static class RescueRemoval
    {
        internal static void Remove(Pawn animal)
        {
            Map map = animal.Map;
            IntVec3 position = animal.Position;
            Rot4 rotation = animal.Rotation;
            WorldPawns world = Find.WorldPawns;
            try
            {
                animal.DeSpawn();
                // Discard mode destroys while marked as being discarded: Pawn.Destroy
                // therefore does not retain the animal as a simulated world pawn.
                world.PassToWorld(animal, PawnDiscardDecideMode.Discard);
                if (!animal.Destroyed) throw new InvalidOperationException("Animal destruction was refused.");
            }
            finally
            {
                if (animal.Destroyed)
                {
                    // Finish cleanup if a mod's destruction hook interrupted the call.
                    if (world.Contains(animal)) world.RemoveAndDiscardPawnViaGC(animal);
                    else if (!animal.Discarded) animal.Discard(true);
                }
                else if (!animal.Spawned)
                {
                    // Despawn is reversible until destruction. Never undo a successful
                    // rescue or fabricate a replacement pawn.
                    if (world.Contains(animal)) world.RemovePawn(animal);
                    GenSpawn.Spawn(animal, position, map, rotation);
                }
            }
        }
    }
}
