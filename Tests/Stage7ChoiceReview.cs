using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class Stage7ChoiceReview
    {
        private const float DecisionTime = 72.9f;
        private const float Step = .05f;

        private sealed class Purchase
        {
            public float Time;
            public bool Upgrade;
            public TowerKind Kind;
            public int Site;
        }

        public sealed class Result
        {
            public int Seed, Spent, Credits, Towers, Kills, Spawned, Remaining, Fusions;
            public float Core, Hunter, MaxPurchaseDelay;
            public double Elapsed, Decision = -1;
            public string Plan, State;
            public bool AllSpawned;
            public string[] Cards, Purchases;
            public float[] KindDamage;
            public readonly int[] BreachesByLane = new int[4];
            public StageThreatObservation Observation;
        }

        private static Purchase[] CommonOpening(string nativeLedger)
        {
            MatchCollection matches = Regex.Matches(nativeLedger,
                @"(?m)^([\d.]+) (build|upgrade) (\w+) @(\d+)\r?$");
            if (matches.Count != 20 || !nativeLedger.Contains("72.9 fusion (Autocannon+Binding) -> Arc @11"))
                throw new ArgumentException("Unexpected first 1-7 purchase ledger");

            var actions = new Purchase[9];
            for (int i = 0; i < actions.Length; i++) {
                Match m = matches[i];
                actions[i] = new Purchase {
                    Time = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    Upgrade = m.Groups[2].Value == "upgrade",
                    Kind = (TowerKind)Enum.Parse(typeof(TowerKind), m.Groups[3].Value),
                    Site = int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)
                };
            }
            return actions;
        }

        private static bool Execute(DefenseSimulation game, Purchase action, out string reason)
        {
            if (!action.Upgrade) return game.TryBuild(action.Site, action.Kind, null, out reason);
            Tower tower = game.TowerAt(action.Site);
            if (tower == null || tower.Kind != action.Kind) throw new InvalidOperationException("Missing shared upgrade");
            return game.TryUpgrade(tower.Id, null, out reason);
        }

        private static Result Run(int seed, string plan, Purchase[] actions)
        {
            var game = new DefenseSimulation(new GameBalance(), seed,
                new CampaignBattle { Stage = CampaignCatalog.Stages[6], Hunter = 2 });
            var result = new Result { Seed = seed, Plan = plan, Observation = new StageThreatObservation(game.BoardSockets.Length) };
            var cards = new List<string>();
            var desiredCards = new[] { CardKind.Range, CardKind.Credits, CardKind.Control };

            int next = 0;
            string reason;
            while (next < actions.Length && actions[next].Time == 0) {
                if (!Execute(game, actions[next], out reason)) throw new InvalidOperationException(reason);
                next++;
            }
            if (next != 6 || game.Telemetry.CreditsSpent != 270 || game.Credits != 10)
                throw new InvalidOperationException("Opening budget differs");
            game.Start();

            for (int frame = 0; frame < 6500 && game.State != RunState.Won && game.State != RunState.Lost; frame++) {
                if (game.AwaitingCard) {
                    CardKind preferred = desiredCards[cards.Count];
                    CardKind picked = game.CardOffers.Contains(preferred) ? preferred : game.CardOffers[0];
                    if (!game.ChooseCard(picked)) throw new InvalidOperationException("Card rejected");
                    cards.Add(picked.ToString());
                }

                if (next < actions.Length && game.Elapsed >= actions[next].Time && Execute(game, actions[next], out reason)) {
                    result.MaxPurchaseDelay = Math.Max(result.MaxPurchaseDelay, (float)game.Elapsed - actions[next].Time);
                    next++;
                }
                if (next == actions.Length && result.Decision < 0 && game.Elapsed >= DecisionTime) {
                    bool completed = plan == "hold";
                    if (plan == "spear") completed = game.TryBuild(17, TowerKind.Autocannon, null, out reason);
                    if (plan == "arc") {
                        Tower spear = game.TowerAt(11), binding = game.TowerAt(14);
                        completed = game.TryFuse(0, spear.Id, binding.Id, 11, null, out reason);
                    }
                    if (completed) result.Decision = game.Elapsed;
                }

                game.Tick(Step, new V2());
                result.Observation.Sample(game, Step);
                foreach (CombatEvent effect in game.Events)
                    if (effect.Kind == EventKind.CoreHurt && effect.Source >= 0 && effect.Source < result.BreachesByLane.Length)
                        result.BreachesByLane[effect.Source]++;
                game.Events.Clear();
            }

            int expectedSpent = plan == "hold" ? 380 : 420;
            int expectedTowers = plan == "hold" ? 7 : plan == "spear" ? 8 : 6;
            if (next != actions.Length || result.Decision < 0 || game.Telemetry.CreditsSpent != expectedSpent ||
                game.Towers.Count != expectedTowers || game.Telemetry.Fusions != (plan == "arc" ? 1 : 0))
                throw new InvalidOperationException("Branch commands or budget differ");
            result.Spent = game.Telemetry.CreditsSpent;
            result.Credits = game.Credits;
            result.Towers = game.Towers.Count;
            result.Kills = game.Kills;
            result.Spawned = game.Telemetry.Spawned;
            result.Remaining = game.Enemies.Count;
            result.Fusions = game.Telemetry.Fusions;
            result.Core = game.MinimumCoreHealth;
            result.Hunter = game.PlayerHealth;
            result.Elapsed = game.Elapsed;
            result.State = game.State.ToString();
            result.AllSpawned = game.AllWavesSpawned;
            result.Cards = cards.ToArray();
            result.Purchases = game.Telemetry.Purchases.ToArray();
            result.KindDamage = (float[])game.Telemetry.TowerDamage.Clone();
            return result;
        }

        public static Result[] Compare(string nativeLedger)
        {
            Purchase[] actions = CommonOpening(nativeLedger);
            var results = new List<Result>();
            foreach (int seed in new[] { 7, 19, 41 })
                foreach (string plan in new[] { "hold", "spear", "arc" })
                    results.Add(Run(seed, plan, actions));
            return results.ToArray();
        }
    }
}
