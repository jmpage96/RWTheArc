using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using RimWorld;
using TheArk.Collection;
using Verse;

internal static class Program
{
    private static int assertions;
    private static readonly string TestRoot = Path.Combine(Path.GetTempPath(), "TheArkTests-" + Guid.NewGuid());

    private static void Main()
    {
        string gamePath = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RimWorldPath").Value;
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            string path = Path.Combine(gamePath, name.Name + ".dll");
            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path)) : null;
        };
        try
        {
            Run();
            Console.WriteLine($"PASS: {assertions} assertions against the installed RimWorld assemblies.");
        }
        finally
        {
            // TestRoot is an absolute, uniquely created child of the temporary directory.
            if (Directory.Exists(TestRoot)) Directory.Delete(TestRoot, true);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        // Avoid touching the player's real config; no engine bootstrap in this harness.
        Directory.CreateDirectory(Path.Combine(TestRoot, "Data", "Core", "About"));
        Directory.CreateDirectory(Path.Combine(TestRoot, "Mods"));
        Directory.CreateDirectory(Path.Combine(TestRoot, "Config"));
        File.WriteAllText(Path.Combine(TestRoot, "Data", "Core", "About", "About.xml"),
            "<ModMetaData><name>Core</name><packageId>ludeon.rimworld</packageId><author>Ludeon</author><supportedVersions><li>1.7</li></supportedVersions></ModMetaData>");
        File.WriteAllText(Path.Combine(TestRoot, "Config", "ModsConfig.xml"),
            "<ModsConfigData><activeMods><li>ludeon.rimworld</li></activeMods></ModsConfigData>");
        foreach (var entry in new[] { ("saveDataPath", TestRoot), ("modsFolderPath", Path.Combine(TestRoot, "Mods")), ("officialModsFolderPath", Path.Combine(TestRoot, "Data")) })
            typeof(GenFilePaths).GetField(entry.Item1, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, entry.Item2);
        typeof(Prefs).GetField("data", BindingFlags.Static | BindingFlags.NonPublic)
            .SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(PrefsData)));
        // Synthetic definitions exercise modded races without loading or modifying game files.
        ThingDef animal = Animal("ArkTest_ModAnimal");
        ThingDef sexless = Animal("ArkTest_Sexless", false);
        ThingDef femaleOnly = Animal("ArkTest_FemaleOnly", true, Gender.Female);
        ThingDef human = Animal("ArkTest_Human");
        human.race.intelligence = Intelligence.Humanlike;
        ThingDef machine = Animal("ArkTest_Machine");
        SetFlesh(machine.race, false);
        foreach (ThingDef def in new[] { animal, sexless, femaleOnly, human, machine }) DefDatabase<ThingDef>.Add(def);
        Check(AnimalCatalog.Discover().Count == 3, "discover modded animals, excluding humans and inorganic races");
        Check(!AnimalCatalog.IsEligible(null), "null def excluded");
        var collection = new ArkCollectionComponent(null);
        Check(collection.TryRegister(null) == RegistrationResult.NoAnimal, "no selected animal");
        Check(collection.TryRegisterSpecies(human, Gender.Male) == RegistrationResult.Ineligible, "human rejected");
        Check(collection.TryRegisterSpecies(animal, Gender.None) == RegistrationResult.InvalidGender, "genderless pawn in gendered race rejected");
        Check(collection.TryRegisterSpecies(animal, Gender.Male) == RegistrationResult.Registered, "male accepted");
        Check(!collection.IsComplete(animal), "male alone is incomplete");
        Check(collection.TryRegisterSpecies(animal, Gender.Male) == RegistrationResult.AlreadyRegistered, "duplicate is idempotent");
        Check(collection.TryRegisterSpecies(animal, Gender.Female) == RegistrationResult.Registered && collection.IsComplete(animal), "pair complete");
        Check(collection.TryRegisterSpecies(sexless, Gender.None) == RegistrationResult.Registered && collection.IsComplete(sexless), "sexless requires one registration");
        Check(collection.TryRegisterSpecies(femaleOnly, Gender.Male) == RegistrationResult.InvalidGender, "forced gender enforced");
        Check(collection.TryRegisterSpecies(femaleOnly, Gender.Female) == RegistrationResult.Registered && collection.IsComplete(femaleOnly), "forced female can complete");
        Check(!new ArkCollectionComponent(null).IsComplete(animal), "new game has independent progress");

        string path = Path.Combine(Path.GetTempPath(), "TheArkTests-" + Guid.NewGuid() + ".xml");
        try
        {
            Save(path, collection);
            Check(File.ReadAllText(path).Contains("ArkTest_ModAnimal"), "stable defName saved");
            ArkCollectionComponent loaded = Load(path);
            Check(loaded.IsComplete(animal) && loaded.IsComplete(sexless) && loaded.IsComplete(femaleOnly), "real Scribe save/load round trip");
            Check(loaded.TryRegisterSpecies(animal, Gender.Male) == RegistrationResult.AlreadyRegistered, "index restored after load");

            // Remove every test Def, load and save again, then restore a new Def with the same identifier.
            DefDatabase<ThingDef>.Clear();
            loaded = Load(path);
            Check(loaded.Species.Count == 0 && loaded.Records.Count() == 3, "missing mods preserved outside active catalog");
            Save(path, loaded);
            loaded = Load(path);
            ThingDef restored = Animal("ArkTest_ModAnimal");
            DefDatabase<ThingDef>.Add(restored);
            loaded.RefreshCatalog();
            Check(loaded.Species.Count == 1 && loaded.IsComplete(restored), "reinstalled mod recovers pair by identifier");

            File.WriteAllText(path, "<test><collection><arkSpeciesRecords>" +
                "<li><defName>ArkTest_ModAnimal</defName><male>True</male></li>" +
                "<li><defName>ArkTest_ModAnimal</defName><female>True</female></li>" +
                "<li><male>True</male></li></arkSpeciesRecords></collection></test>");
            loaded = Load(path);
            Check(loaded.Records.Count() == 1 && loaded.IsComplete(restored), "duplicate records merge and invalid identifiers are ignored");
            File.WriteAllText(path, "<test><collection /></test>");
            loaded = Load(path);
            Check(!loaded.Records.Any() && !loaded.IsComplete(restored), "empty/older component data initializes safely");
        }
        finally { Scribe.ForceStop(); File.Delete(path); }
    }

    private static ThingDef Animal(string id, bool genders = true, Gender forced = Gender.None)
    {
        var def = new ThingDef
        {
            defName = id, label = id, category = ThingCategory.Pawn,
            race = new RaceProperties
            {
                intelligence = Intelligence.Animal, hasGenders = genders, forceGender = forced
            }
        };
        SetFlesh(def.race, true);
        return def;
    }

    // Test fixtures only: the XML loader normally populates this private field.
    private static void SetFlesh(RaceProperties race, bool organic)
    {
        typeof(RaceProperties).GetField("fleshType", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(race, new FleshTypeDef { defName = "ArkTest_Flesh", isOrganic = organic });
    }

    private static void Save(string path, ArkCollectionComponent collection)
    {
        Scribe.saver.InitSaving(path, "test");
        Scribe_Deep.Look(ref collection, "collection", new object[] { null });
        Scribe.saver.FinalizeSaving();
    }

    private static ArkCollectionComponent Load(string path)
    {
        Scribe.loader.InitLoading(path);
        ArkCollectionComponent collection = null;
        Scribe_Deep.Look(ref collection, "collection", new object[] { null });
        Scribe.loader.FinalizeLoading();
        return collection;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAILED: " + message);
        assertions++;
    }
}
