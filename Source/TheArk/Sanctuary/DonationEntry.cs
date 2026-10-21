using Verse;

namespace TheArk.Sanctuary
{
    public sealed class DonationEntry : IExposable
    {
        private string animalName, speciesId, speciesLabel;
        private Gender sex;
        private long silver;
        private bool fullyTrained;
        private float marketValue;
        private int tick;

        public DonationEntry() { }
        public DonationEntry(string name, string id, string label, Gender sex, long silver, bool trained, float value, int tick)
        {
            animalName = name; speciesId = id; speciesLabel = label; this.sex = sex;
            this.silver = silver; fullyTrained = trained; marketValue = value; this.tick = tick;
        }
        public string AnimalName => animalName ?? speciesLabel ?? speciesId ?? "Unknown animal";
        public string SpeciesId => speciesId ?? "Unknown";
        public string SpeciesLabel => speciesLabel ?? speciesId ?? "Unknown species";
        public Gender Sex => sex;
        public long Silver => silver;
        public bool FullyTrained => fullyTrained;
        public float MarketValue => marketValue;
        public int Tick => tick;
        public void ExposeData()
        {
            Scribe_Values.Look(ref animalName, "animalName");
            Scribe_Values.Look(ref speciesId, "speciesId");
            Scribe_Values.Look(ref speciesLabel, "speciesLabel");
            Scribe_Values.Look(ref sex, "sex");
            Scribe_Values.Look(ref silver, "silver");
            Scribe_Values.Look(ref fullyTrained, "fullyTrained");
            Scribe_Values.Look(ref marketValue, "marketValue");
            Scribe_Values.Look(ref tick, "tick");
        }
    }
}
