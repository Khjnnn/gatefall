using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class StageTwoReinforcementReview
    {
        public sealed class Result
        {
            public int Seed, Hunter, AddedSite, WestSite, Kills, Towers, Spent, Credits, Cards;
            public string Sequence, State;
            public double Seconds;
            public float Core, WestDamage, EastDamage;
        }

        public static Result[] Run()
        {
            var results = new List<Result>();
            foreach (int seed in new[] { 7, 19, 41 })
            foreach (int hunter in new[] { 0, 2 })
            foreach (bool ballistic in new[] { false, true })
            foreach (int addedSite in new[] { -1, 3, 13, 17 }) {
                CardKind[] selected = ballistic ? new[] { CardKind.Control, CardKind.Ballistic, CardKind.Range }
                    : new[] { CardKind.Control, CardKind.Range, CardKind.Hunter };
                results.Add(RunCase(seed, hunter, selected, addedSite, 30, -1));
            }
            return results.ToArray();
        }

        public static Result[] RunWestFollowup()
        {
            var results = new List<Result>();
            var selected = new[] { CardKind.Control, CardKind.Credits, CardKind.Hunter };
            foreach (int seed in new[] { 7, 19, 41 })
            foreach (int westSite in new[] { -1, 0, 6, 12, 14 })
                results.Add(RunCase(seed, 2, selected, 3, 36.6, westSite));
            return results.ToArray();
        }

        private static Result RunCase(int seed, int hunter, CardKind[] selected, int eastSite, double eastTime, int westSite)
        {
            var game = new DefenseSimulation(new GameBalance(), seed,
                new CampaignBattle { Stage = CampaignCatalog.Stages[1], Hunter = hunter });
            string why;
            if (!game.TryBuild(16, TowerKind.Binding, null, out why)) throw new Exception(why);
            foreach (int site in new[] { 2, 11, 7 })
                if (!game.TryBuild(site, TowerKind.Autocannon, null, out why)) throw new Exception(why);
            foreach (int site in new[] { 2, 11 })
                if (!game.TryUpgrade(game.TowerAt(site).Id, null, out why)) throw new Exception(why);

            int card = 0;
            bool eastBuilt = false, westBuilt = false;
            game.Start();
            while (game.Elapsed < 230 && (game.State == RunState.Running || game.AwaitingCard)) {
                if (game.AwaitingCard) {
                    // Keep the RNG draws, but control the chosen cards across placements.
                    CardKind choice = selected[card++];
                    game.CardOffers.Clear();
                    game.CardOffers.Add(choice);
                    if (!game.ChooseCard(choice)) throw new Exception("Controlled card was not applied");
                }
                if (!eastBuilt && eastSite >= 0 && card >= 1 && game.Elapsed >= eastTime) {
                    if (!game.TryBuild(eastSite, TowerKind.Autocannon, null, out why)) throw new Exception(why);
                    eastBuilt = true;
                }
                if (!westBuilt && westSite >= 0 && card >= 2) {
                    if (!game.TryBuild(westSite, TowerKind.Autocannon, null, out why)) throw new Exception(why);
                    westBuilt = true;
                }
                game.Tick(1f / 60f, new V2());
                game.Events.Clear();
            }
            return new Result {
                Seed = seed, Hunter = hunter, AddedSite = eastSite, WestSite = westSite, Sequence = string.Join("/", selected),
                State = game.State.ToString(), Seconds = game.Elapsed, Kills = game.Kills, Towers = game.Towers.Count,
                Spent = game.Telemetry.CreditsSpent, Credits = game.Credits, Cards = card, Core = game.MinimumCoreHealth,
                WestDamage = game.Telemetry.CoreDamageByLane[0], EastDamage = game.Telemetry.CoreDamageByLane[2]
            };
        }
    }
}
