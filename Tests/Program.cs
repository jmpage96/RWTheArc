using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using RimWorld;
using TheArk.Collection;
using TheArk.Rescue;
using TheArk.Sanctuary;
using Verse.AI;
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
            TestDeliverance();
            TestSanctuary();
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestDeliverance()
    {
        var settings = new CompProperties_Deliverance();
        Check(settings.ValidConfiguration, "default rescue configuration valid");
        Check(Near(DeliveranceChance.Calculate(0, 0, settings), 0.35f), "level 0, zero wildness: 35%");
        Check(Near(DeliveranceChance.Calculate(20, 0, settings), 0.95f), "level 20, zero wildness: capped at 95%");
        Check(Near(DeliveranceChance.Calculate(0, 0.98f, settings), 0.05f), "level 0, thrumbo wildness: floor at 5%");
        Check(Near(DeliveranceChance.Calculate(20, 0.98f, settings), 0.658f), "level 20, thrumbo wildness: 65.8%");
        Check(Near(DeliveranceChance.Calculate(0, 0.3f, settings), 0.23f), "level 0, 30% wildness: 23%");
        Check(Near(DeliveranceChance.Calculate(20, 0.3f, settings), 0.93f), "level 20, 30% wildness: 93%");
        for (int level = 0; level <= 20; level++)
            for (int w = 0; w <= 100; w++)
            {
                float chance = DeliveranceChance.Calculate(level, w / 100f, settings);
                if (!float.IsFinite(chance) || chance < 0.05f || chance > 0.95f)
                    throw new Exception("Probability escaped caps.");
            }
        Check(true, "all normal skill/wildness combinations remain inside caps");
        Check(DeliveranceChance.Calculate(200, float.NaN, settings) is > 0 and < 1, "malformed inputs remain finite and bounded");
        settings.minimumChance = 0;
        Check(!settings.ValidConfiguration, "zero probability cap rejected");
        settings.minimumChance = 0.05f;
        settings.maximumChance = 1;
        Check(!settings.ValidConfiguration, "guaranteed success cap rejected");
        settings.maximumChance = 0.95f;
        settings.manhunterChance = 2;
        Check(!settings.ValidConfiguration, "invalid retaliation probability rejected");

        ThingDef def = Animal("ArkTest_Rescue");
        var collection = new ArkCollectionComponent(null);
        bool removed = false;
        var result = RescueTransaction.Commit(collection, def, Gender.Male, () => removed = true, () => removed, out Exception error);
        Check(result == RescueCommitResult.Registered && error == null, "rescue commits a missing entry");
        Check(collection.RegisteredSlots(def.defName) == RegistrationSlots.Male, "rescue records correct sex");
        result = RescueTransaction.Commit(collection, def, Gender.Male, () => { }, () => true, out error);
        Check(result == RescueCommitResult.Duplicate && collection.Records.Count() == 1, "duplicate rescue adds no progress");
        result = RescueTransaction.Commit(collection, def, Gender.Female, () => throw new Exception("before destruction"), () => false, out error);
        Check(result == RescueCommitResult.RemovalFailed && error != null
            && collection.RegisteredSlots(def.defName) == RegistrationSlots.Male, "failed removal rolls back new sex without losing existing sex");
        var fresh = new ArkCollectionComponent(null);
        result = RescueTransaction.Commit(fresh, def, Gender.Male, () => { }, () => false, out error);
        Check(result == RescueCommitResult.RemovalFailed && !fresh.Records.Any(), "silent refusal rolls back the new record");
        result = RescueTransaction.Commit(collection, def, Gender.Male, () => throw new Exception("duplicate removal failed"), () => false, out error);
        Check(result == RescueCommitResult.RemovalFailed && collection.RegisteredSlots(def.defName) == RegistrationSlots.Male, "failed duplicate removal preserves prior credit");
        result = RescueTransaction.Commit(collection, def, Gender.Female, () => throw new Exception("after destruction"), () => true, out error);
        Check(result == RescueCommitResult.Registered && error != null && collection.IsComplete(def), "irreversible removal retains credit despite late hook exception");
        bool called = false;
        result = RescueTransaction.Commit(collection, def, Gender.None, () => called = true, () => false, out error);
        Check(result == RescueCommitResult.Invalid && !called, "invalid gender never reaches removal");
        ThingDef sexless = Animal("ArkTest_RescueSexless", false);
        result = RescueTransaction.Commit(collection, sexless, Gender.None, () => { }, () => true, out error);
        Check(result == RescueCommitResult.Registered && collection.IsComplete(sexless), "sexless rescue uses M1 rules");

        string repo = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RepositoryPath").Value;
        var xml = new System.Xml.XmlDocument();
        xml.Load(Path.Combine(repo, "Defs", "AbilityDefs", "Deliverance.xml"));
        // Register our already loaded assembly as a running mod so Verse's type
        // resolver sees custom XML types just as it does during an actual game load.
        var pack = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        pack.assemblies = new ModAssemblyHandler(pack);
        pack.assemblies.loadedAssemblies.Add(typeof(Ability_Deliverance).Assembly);
        LoadedModManager.RunningModsListForReading.Add(pack);
        GenTypes.ClearCache();
        AbilityDef ability = DirectXmlToObject.ObjectFromXml<AbilityDef>(xml.SelectSingleNode("/Defs/AbilityDef"), false);
        Check(ability.abilityClass == typeof(Ability_Deliverance) && !ability.IsPsycast, "ability XML loads without Psycast dependency");
        Check(ability.verbProperties.verbClass == typeof(Verb_CastAbility) && Near(ability.verbProperties.range, 24.9f)
            && ability.cooldownTicksRange.min == 600, "actual XML parser reads range, verb, and cooldown");
        Check(ability.comps.Single() is CompProperties_Deliverance parsed && parsed.ValidConfiguration,
            "actual XML parser reads rescue configuration");
        Check(!ability.ConfigErrors().Any(), "Deliverance definition passes game configuration validation");

        string gamePath = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RimWorldPath").Value;
        var coreXml = new System.Xml.XmlDocument();
        coreXml.Load(Path.Combine(gamePath, "Data", "Core", "Defs", "ThingDefs_Buildings", "Buildings_Base.xml"));
        var buildingXml = new System.Xml.XmlDocument();
        buildingXml.Load(Path.Combine(repo, "Defs", "ThingDefs_Buildings", "TheArk.xml"));
        var buildingNode = buildingXml.SelectSingleNode("/Defs/ThingDef");
        XmlInheritance.Clear();
        XmlInheritance.TryRegister(coreXml.SelectSingleNode("/Defs/ThingDef[@Name='BuildingBase']"), null);
        XmlInheritance.TryRegister(buildingNode, null);
        XmlInheritance.Resolve();
        ThingDef ark = DirectXmlToObject.ObjectFromXml<ThingDef>(buildingNode, false);
        Check(ark.thingClass == typeof(Building) && ark.category == ThingCategory.Building,
            "Ark XML resolves Core BuildingBase without changing saved building type");
        Check(ark.comps.Single() is TheArk.Buildings.CompProperties_ArkGrant,
            "Ark XML loads granting component");
        var existingBuilding = new Building { def = ark };
        existingBuilding.InitializeComps();
        Check(existingBuilding.GetComp<TheArk.Buildings.CompArkGrant>() != null,
            "granting comp attaches to a plain existing Building instance");

        string rescueSave = Path.Combine(TestRoot, "rescued.xml");
        Save(rescueSave, collection);
        ArkCollectionComponent rescuedReload = Load(rescueSave);
        Check(rescuedReload.IsComplete(def) && rescuedReload.IsComplete(sexless),
            "transaction results survive the actual Scribe save/load cycle");
    }

    private static bool Near(float actual, float expected) => Math.Abs(actual - expected) < 0.00001f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestSanctuary()
    {
        Check(DonationReward.Calculate(100, false) == 125, "standard donation multiplier");
        Check(DonationReward.Calculate(100, true) == 150, "fully trained donation multiplier");
        Check(DonationReward.Calculate(2, false) == 3, "silver halves round away from zero");
        Check(DonationReward.Calculate(0, true) == 0, "zero market value gives zero silver");
        Check(!DonationReward.AllApplicableLearned(Array.Empty<int>(), _ => true, _ => true), "no training categories gets standard reward");
        Check(DonationReward.AllApplicableLearned(new[] { 1 }, _ => true, _ => true), "one completed category is fully trained");
        Check(!DonationReward.AllApplicableLearned(new[] { 1, 2 }, _ => true, category => category == 1), "partially trained gets standard reward");
        Check(DonationReward.AllApplicableLearned(new[] { 1, 2 }, category => category == 1, category => category == 1), "inapplicable category does not block full bonus");
        Check(!DonationReward.AllApplicableLearned(new[] { 1 }, _ => false, _ => true), "only inapplicable categories gets standard reward");
        bool rejected = false;
        try { DonationReward.Calculate(float.NaN, false); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "invalid mod market value rejected");

        // Use the actual vanilla training tracker and eligibility API, including tags.
        DefDatabase<TrainableDef>.Clear();
        var intelligence = new TrainabilityDef { defName = "ArkTest_Trainability", intelligenceOrder = 2 };
        var tameness = new TrainableDef { defName = "ArkTest_Tameness", steps = 5, defaultTrainable = true };
        var first = new TrainableDef { defName = "ArkTest_Guard", steps = 3, defaultTrainable = true, requiredTrainability = intelligence };
        var second = new TrainableDef { defName = "ArkTest_Haul", steps = 7, defaultTrainable = true, requiredTrainability = intelligence };
        foreach (TrainableDef training in new[] { tameness, first, second }) DefDatabase<TrainableDef>.Add(training);
        TrainableDefOf.Tameness = tameness;
        ThingDef species = Animal("ArkTest_DonatedSpecies");
        species.race.trainability = intelligence;
        Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Current.Game.tickManager = (TickManager)RuntimeHelpers.GetUninitializedObject(typeof(TickManager));
        var animal = new Pawn { def = species };
        animal.Name = new NameSingle("Mercy");
        Check(DonationEligibility.HasSpecialRelationships(animal), "named pet requires relationship warning");
        string consentBefore = DonationEligibility.ConsentFingerprint(animal);
        animal.Name = new NameSingle("Hope");
        Check(consentBefore != DonationEligibility.ConsentFingerprint(animal), "renaming invalidates old donation consent");
        animal.training = new Pawn_TrainingTracker(animal);
        Check(!DonationReward.IsFullyTrained(animal), "actual untrained tracker is standard");
        animal.training.Train(first, null, true);
        Check(!DonationReward.IsFullyTrained(animal), "actual partial tracker is standard");
        animal.training.Train(second, null, true);
        Check(DonationReward.IsFullyTrained(animal), "actual full tracker qualifies without requiring tameness for bonus");
        second.defaultTrainable = false;
        animal.training = new Pawn_TrainingTracker(animal);
        animal.training.Train(first, null, true);
        Check(DonationReward.IsFullyTrained(animal), "actual single applicable category can qualify");
        first.defaultTrainable = false;
        Check(!DonationReward.IsFullyTrained(animal), "actual tameness-only species gets no training bonus");

        var ledger = new SanctuaryLedgerComponent(null);
        var book = new ArkCollectionComponent(null);
        DonationEntry entry = new DonationEntry("Mercy", species.defName, "test species", Gender.Female, 125, false, 100, 1234);
        bool consumed = false;
        RescueCommitResult result = DonationTransaction.Commit(book, ledger, species, Gender.Female, entry,
            () => throw new Exception("refused before removal"), () => consumed, out Exception error);
        Check(result == RescueCommitResult.RemovalFailed && ledger.TotalDonated == 0 && ledger.OutstandingSilver == 0
            && book.RegisteredSlots(species.defName) == RegistrationSlots.None, "failed donation creates neither progress nor reward");
        result = DonationTransaction.Commit(book, ledger, species, Gender.Female, entry, () => consumed = true, () => consumed, out error);
        Check(result == RescueCommitResult.Registered && ledger.TotalDonated == 1 && ledger.OutstandingSilver == 125,
            "successful donation creates credit and outstanding reward");
        Check(book.RegisteredSlots(species.defName) == RegistrationSlots.Female, "donation updates Book of Life sex entry");
        result = DonationTransaction.Commit(book, ledger, species, Gender.Female, entry, () => consumed = true, () => consumed, out error);
        Check(result == RescueCommitResult.Invalid && ledger.TotalDonated == 1 && ledger.OutstandingSilver == 125,
            "same already consumed animal cannot donate or pay twice");
        consumed = false;
        result = DonationTransaction.Commit(book, ledger, species, Gender.Female, entry, () => consumed = true, () => consumed, out error);
        Check(result == RescueCommitResult.Duplicate && ledger.TotalDonated == 2 && ledger.OutstandingSilver == 250
            && book.Records.Count() == 1, "different duplicate-species animal still earns silver without duplicate progress");
        Check(ledger.BeginDonation() && !ledger.BeginDonation(), "reentrant donation guard");
        ledger.EndDonation();
        Check(ledger.BeginPayment() && !ledger.BeginPayment(), "reentrant payout guard");
        ledger.EndPayment();

        Check(SanctuarySilver.DeliveredCount(100, 0, false, false, 100) == 0, "blocked placement pays nothing");
        Check(SanctuarySilver.DeliveredCount(100, 30, false, false, 70) == 30, "partial merge accounted once");
        Check(SanctuarySilver.DeliveredCount(100, 0, true, false, 100) == 100, "spawn before callback exception is accounted");
        Check(SanctuarySilver.DeliveredCount(100, 100, false, true, 0) == 100, "fully absorbed stack accounted once");
        Check(SanctuarySilver.DeliveredCount(100, 20, true, false, 80) == 100, "partial merge then spawn pays original count only");
        ledger.RecordDelivered(30);
        Check(ledger.TotalSilverAwarded == 30 && ledger.OutstandingSilver == 220, "partial payment debits only delivered amount");
        ledger.RecordDelivered(220);
        Check(ledger.TotalSilverAwarded == 250 && ledger.OutstandingSilver == 0, "claim pays remaining balance once");

        var full = new DonationEntry("Hope", "MissingMod_Species", "modded creature", Gender.None, 150, true, 100, 2000);
        for (int i = 0; i < 60; i++) ledger.Record(full);
        Check(ledger.Recent.Count == SanctuaryLedgerComponent.HistoryLimit && ledger.TotalDonated == 62 && ledger.TotalFullyTrained == 60,
            "history is bounded while lifetime counters keep all donations");
        string save = Path.Combine(TestRoot, "sanctuary.xml");
        Scribe.saver.InitSaving(save, "test");
        Scribe_Deep.Look(ref ledger, "ledger", new object[] { null });
        Scribe.saver.FinalizeSaving();
        SanctuaryLedgerComponent restored = null;
        Scribe.loader.InitLoading(save);
        Scribe_Deep.Look(ref restored, "ledger", new object[] { null });
        Scribe.loader.FinalizeLoading();
        Check(restored.TotalDonated == 62 && restored.TotalFullyTrained == 60 && restored.TotalSilverAwarded == 250
            && restored.OutstandingSilver == 9000 && restored.Recent.Count == 50, "ledger counters, debt and bounded history survive real Scribe roundtrip");
        Check(restored.Recent[0].AnimalName == "Hope" && restored.Recent[0].SpeciesId == "MissingMod_Species"
            && restored.Recent[0].Sex == Gender.None && restored.Recent[0].Silver == 150, "history survives without a loaded species definition");
        File.WriteAllText(save, "<test><ledger /></test>");
        restored = null;
        Scribe.loader.InitLoading(save);
        Scribe_Deep.Look(ref restored, "ledger", new object[] { null });
        Scribe.loader.FinalizeLoading();
        Check(restored.TotalDonated == 0 && restored.OutstandingSilver == 0 && restored.Recent.Count == 0,
            "empty older ledger data initializes safely");

        string repo = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RepositoryPath").Value;
        var xml = new System.Xml.XmlDocument();
        xml.Load(Path.Combine(repo, "Defs", "JobDefs", "Sanctuary.xml"));
        var node = xml.SelectSingleNode("/Defs/JobDef");
        foreach (System.Xml.XmlNode child in node.ChildNodes)
            if (child.NodeType == System.Xml.XmlNodeType.Element)
                Check(typeof(JobDef).GetField(child.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null,
                    "verified job XML field: " + child.Name);
        JobDef job = DirectXmlToObject.ObjectFromXml<JobDef>(node, false);
        Check(job.driverClass == typeof(JobDriver_OfferToSanctuary) && job.playerInterruptible,
            "actual game parser loads the interruptible donation JobDef");
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
