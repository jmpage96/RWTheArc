using System;
using TheArk.Collection;
using Verse;

namespace TheArk.Rescue
{
    internal enum RescueCommitResult { Registered, Duplicate, Invalid, RemovalFailed }

    internal static class RescueTransaction
    {
        // A synchronous transaction on the game thread; no saved pawn or deferred work.
        // The irreversible boundary is destruction, not a cosmetic effect or message.
        internal static RescueCommitResult Commit(ArkCollectionComponent collection, ThingDef def, Gender gender,
            Action remove, Func<bool> consumed, out Exception error)
        {
            error = null;
            RegistrationSlots before = collection.RegisteredSlots(def?.defName);
            RegistrationResult registration = collection.TryRegisterSpecies(def, gender);
            if (registration != RegistrationResult.Registered && registration != RegistrationResult.AlreadyRegistered)
                return RescueCommitResult.Invalid;
            try { remove(); }
            catch (Exception exception) { error = exception; }
            if (!consumed())
            {
                collection.RestoreRegistration(def.defName, before);
                return RescueCommitResult.RemovalFailed;
            }
            // A mod hook may throw after destruction. Keep the record once the animal
            // cannot be restored, rather than lose both animal and collection credit.
            return registration == RegistrationResult.Registered ? RescueCommitResult.Registered : RescueCommitResult.Duplicate;
        }
    }
}
