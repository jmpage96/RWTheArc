using TheArk.Sanctuary;
using UnityEngine;
using Verse;

namespace TheArk.UI
{
    public sealed class Dialog_SanctuaryLedger : Window
    {
        private Vector2 scroll;
        public override Vector2 InitialSize => new Vector2(850, 620);
        public Dialog_SanctuaryLedger() { doCloseX = true; doCloseButton = true; }

        public override void DoWindowContents(Rect rect)
        {
            var ledger = Current.Game?.GetComponent<SanctuaryLedgerComponent>();
            if (ledger == null) { Widgets.Label(rect, "Load a game to view the Sanctuary Ledger."); return; }
            GameFont previous = Text.Font;
            try
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0, 0, rect.width - 30, 35), "Sanctuary Ledger");
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(0, 42, rect.width, 28), $"Animals donated: {ledger.TotalDonated:N0}    Fully trained: {ledger.TotalFullyTrained:N0}");
                Widgets.Label(new Rect(0, 72, rect.width, 28), $"Silver awarded (placed): {ledger.TotalSilverAwarded:N0}    Outstanding: {ledger.OutstandingSilver:N0}");
                Widgets.Label(new Rect(0, 102, rect.width, 48), "Outstanding silver can be claimed at any player-owned Ark when space is available.\nRecent arrivals show silver earned, including any outstanding payment.");
                Widgets.Label(new Rect(0, 158, rect.width, 26), $"Recent donations (latest {SanctuaryLedgerComponent.HistoryLimit})");
                Rect viewport = new Rect(0, 190, rect.width, Mathf.Max(20, rect.height - 245));
                Rect content = new Rect(0, 0, viewport.width - 20, Mathf.Max(viewport.height, ledger.Recent.Count * 58));
                Widgets.BeginScrollView(viewport, ref scroll, content);
                try
                {
                    for (int i = 0; i < ledger.Recent.Count; i++)
                    {
                        DonationEntry entry = ledger.Recent[i];
                        float y = i * 58;
                        if (y + 58 < scroll.y || y > scroll.y + viewport.height) continue;
                        Widgets.Label(new Rect(0, y, content.width - 150, 27), $"{entry.AnimalName} — {entry.SpeciesLabel}");
                        Widgets.Label(new Rect(content.width - 145, y, 145, 27), $"{entry.Silver:N0} silver");
                        string sex = entry.Sex == Gender.Male ? "Male" : entry.Sex == Gender.Female ? "Female" : "Sexless";
                        Widgets.Label(new Rect(0, y + 27, content.width, 27), $"{sex} · {(entry.FullyTrained ? "Fully trained" : "Standard reward")} · Day {entry.Tick / 60000 + 1}");
                        TooltipHandler.TipRegion(new Rect(0, y, content.width, 54), $"Species ID: {entry.SpeciesId}\nMarket value at donation: {entry.MarketValue:0.##}");
                    }
                    if (ledger.Recent.Count == 0) Widgets.Label(content, "No sanctuary donations yet.");
                }
                finally { Widgets.EndScrollView(); }
            }
            finally { Text.Font = previous; }
        }
    }
}
