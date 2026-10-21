using System;
using System.Collections.Generic;
using Verse;

namespace TheArk.Collection
{
    public enum RegistrationResult { Registered, AlreadyRegistered, Ineligible, InvalidGender, NoAnimal }

    public sealed class ArkCollectionComponent : GameComponent
    {
        private List<SpeciesRecord> records = new List<SpeciesRecord>();
        private readonly Dictionary<string, SpeciesRecord> byDefName = new Dictionary<string, SpeciesRecord>(StringComparer.Ordinal);
        private IReadOnlyList<ThingDef> species;

        // Verse.Game.FillComponents constructs each component with its owning Game.
        public ArkCollectionComponent(Game game) { }

        public IReadOnlyList<ThingDef> Species => species ?? (species = AnimalCatalog.Discover());
        public IEnumerable<SpeciesRecord> Records => records.AsReadOnly();

        public void RefreshCatalog() { species = AnimalCatalog.Discover(); }
        public override void FinalizeInit() { RefreshCatalog(); }

        public RegistrationSlots RegisteredSlots(string defName)
        {
            return defName != null && byDefName.TryGetValue(defName, out SpeciesRecord record)
                ? record.Registered : RegistrationSlots.None;
        }

        public bool IsComplete(ThingDef def)
        {
            RegistrationSlots required = AnimalCatalog.RequiredSlots(def);
            return required != RegistrationSlots.None && (RegisteredSlots(def.defName) & required) == required;
        }

        // Debug registration and Deliverance share the same rules. No live pawn is retained.
        public RegistrationResult TryRegister(Pawn animal)
        {
            if (animal == null || animal.Dead || animal.Destroyed) return RegistrationResult.NoAnimal;
            return TryRegisterSpecies(animal.def, animal.gender);
        }

        internal RegistrationResult TryRegisterSpecies(ThingDef def, Gender gender)
        {
            if (!AnimalCatalog.IsEligible(def)) return RegistrationResult.Ineligible;
            RegistrationSlots slot = AnimalCatalog.SlotFor(def, gender);
            if (slot == RegistrationSlots.None) return RegistrationResult.InvalidGender;
            if (!byDefName.TryGetValue(def.defName, out SpeciesRecord record))
            {
                record = new SpeciesRecord(def.defName);
                byDefName.Add(def.defName, record);
                records.Add(record);
            }
            return record.Add(slot) ? RegistrationResult.Registered : RegistrationResult.AlreadyRegistered;
        }

        internal void RestoreRegistration(string defName, RegistrationSlots before)
        {
            if (!byDefName.TryGetValue(defName, out SpeciesRecord record)) return;
            if (before == RegistrationSlots.None) { byDefName.Remove(defName); records.Remove(record); }
            else record.Restore(before);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref records, "arkSpeciesRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) RebuildIndex();
        }

        private void RebuildIndex()
        {
            byDefName.Clear();
            var normalized = new List<SpeciesRecord>();
            foreach (SpeciesRecord record in records ?? new List<SpeciesRecord>())
            {
                if (record == null || string.IsNullOrEmpty(record.DefName)) continue;
                if (byDefName.TryGetValue(record.DefName, out SpeciesRecord existing)) existing.Add(record.Registered);
                else { byDefName.Add(record.DefName, record); normalized.Add(record); }
            }
            records = normalized;
            species = null;
        }
    }
}
