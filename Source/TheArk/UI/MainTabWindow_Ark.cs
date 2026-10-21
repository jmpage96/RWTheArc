using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TheArk.Collection;
using UnityEngine;
using Verse;

namespace TheArk.UI
{
    public sealed class MainTabWindow_Ark : MainTabWindow
    {
        private Vector2 scroll;
        private string search = "";
        private const float RowHeight = 30f;
        public override Vector2 RequestedTabSize => new Vector2(920f, 620f);

        public override void PostOpen()
        {
            base.PostOpen();
            scroll = Vector2.zero;
            Current.Game?.GetComponent<ArkCollectionComponent>()?.RefreshCatalog();
        }

        public override void DoWindowContents(Rect rect)
        {
            ArkCollectionComponent collection = Current.Game?.GetComponent<ArkCollectionComponent>();
            if (collection == null) { Widgets.Label(rect, "Load a game to view The Ark collection."); return; }
            GameFont previousFont = Text.Font;
            try
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0, 0, rect.width, 34), "The Ark — collection");
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(0, 38, rect.width, 26),
                    $"{collection.Species.Count(collection.IsComplete)} / {collection.Species.Count} species complete. Progress belongs to this game.");
                Widgets.Label(new Rect(0, 66, rect.width, 26), "Sexless species need one animal. Forced-sex species need only their available sex.");
                Widgets.Label(new Rect(0, 100, 60, 28), "Search:");
                string nextSearch = Widgets.TextField(new Rect(64, 100, rect.width - 64, 28), search, 120);
                if (search != nextSearch) { search = nextSearch; scroll = Vector2.zero; }
                var rows = new List<Row>();
                foreach (ThingDef def in collection.Species)
                    rows.Add(new Row(def.defName, def.label ?? def.defName, def, collection.RegisteredSlots(def.defName)));
                // Keep missing and newly ineligible definitions visible without resolving a saved Def reference.
                foreach (SpeciesRecord record in collection.Records)
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(record.DefName);
                    if (!AnimalCatalog.IsEligible(def))
                        rows.Add(new Row(record.DefName, (def?.label ?? record.DefName) + " (inactive)", null, record.Registered));
                }
                rows = rows.Where(row => row.Label.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || row.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                float speciesWidth = Math.Max(120f, rect.width - 340f);
                DrawRow(new Rect(0, 140, rect.width - 20, 28), speciesWidth, "Species", "Male", "Female", "Sexless");
                var viewport = new Rect(0, 172, rect.width, Math.Max(20, rect.height - 206));
                var content = new Rect(0, 0, rect.width - 20, Math.Max(viewport.height, rows.Count * RowHeight));
                scroll.y = Mathf.Clamp(scroll.y, 0, Math.Max(0, content.height - viewport.height));
                Widgets.BeginScrollView(viewport, ref scroll, content);
                try
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        float y = i * RowHeight;
                        if (y + RowHeight < scroll.y || y > scroll.y + viewport.height) continue;
                        Row row = rows[i];
                        RegistrationSlots required = row.Def == null ? RegistrationSlots.None : AnimalCatalog.RequiredSlots(row.Def);
                        DrawRow(new Rect(0, y, content.width, 26), speciesWidth, row.Label,
                            Status(row, required, RegistrationSlots.Male), Status(row, required, RegistrationSlots.Female),
                            Status(row, required, RegistrationSlots.Sexless));
                        TooltipHandler.TipRegion(new Rect(0, y, speciesWidth, RowHeight),
                            row.Label + "\nSpecies ID: " + row.Id);
                    }
                }
                finally { Widgets.EndScrollView(); }
                Widgets.Label(new Rect(0, rect.height - 28, rect.width, 28), "Registered / Missing / N/A. Inactive records are preserved and excluded from completion.");
            }
            finally { Text.Font = previousFont; }
        }

        private static string Status(Row row, RegistrationSlots required, RegistrationSlots slot)
        {
            if ((row.Slots & slot) != 0) return "Registered";
            return (required & slot) != 0 ? "Missing" : "N/A";
        }

        private static void DrawRow(Rect rect, float speciesWidth, string label, string male, string female, string sexless)
        {
            Widgets.Label(new Rect(rect.x, rect.y, speciesWidth, rect.height), label);
            float column = (rect.width - speciesWidth) / 3;
            Widgets.Label(new Rect(speciesWidth, rect.y, column, rect.height), male);
            Widgets.Label(new Rect(speciesWidth + column, rect.y, column, rect.height), female);
            Widgets.Label(new Rect(speciesWidth + 2 * column, rect.y, column, rect.height), sexless);
        }

        private sealed class Row
        {
            public readonly string Id, Label;
            public readonly ThingDef Def;
            public readonly RegistrationSlots Slots;
            public Row(string id, string label, ThingDef def, RegistrationSlots slots)
            { Id = id; Label = label; Def = def; Slots = slots; }
        }
    }
}
