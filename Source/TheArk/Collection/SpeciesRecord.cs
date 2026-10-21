using System;
using Verse;

namespace TheArk.Collection
{
    [Flags]
    public enum RegistrationSlots
    {
        None = 0,
        Male = 1,
        Female = 2,
        Sexless = 4
    }

    public sealed class SpeciesRecord : IExposable
    {
        private string defName;
        private bool male;
        private bool female;
        private bool sexless;

        // Required by Scribe's LookMode.Deep loader.
        public SpeciesRecord() { }
        public SpeciesRecord(string defName) { this.defName = defName; }

        public string DefName => defName;
        public RegistrationSlots Registered => (male ? RegistrationSlots.Male : 0)
            | (female ? RegistrationSlots.Female : 0) | (sexless ? RegistrationSlots.Sexless : 0);

        internal bool Add(RegistrationSlots slots)
        {
            RegistrationSlots before = Registered;
            male |= (slots & RegistrationSlots.Male) != 0;
            female |= (slots & RegistrationSlots.Female) != 0;
            sexless |= (slots & RegistrationSlots.Sexless) != 0;
            return Registered != before;
        }

        public void ExposeData()
        {
            // Save strings, never Def references: absent mods must not erase records.
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref male, "male");
            Scribe_Values.Look(ref female, "female");
            Scribe_Values.Look(ref sexless, "sexless");
        }
    }
}
