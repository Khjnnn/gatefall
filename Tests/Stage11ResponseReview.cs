using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class Stage11ResponseReview
    {
        private sealed class Purchase
        {
            public float Time;
            public string Command;
            public TowerKind Kind;
            public int Socket;
        }

        public sealed class Result
        {
            public int Seed, Spent, Credits, Towers, Kills, Spawned, Remaining, Completed;
            public bool EastFirst, AllSpawned;
            public float Core, MaximumDelay, MarkBonus;
            public int MarkedHits;
            public double Elapsed, FirstEastDamage = -1, FirstWestDamage = -1;
            public string State;
            public string[] Cards, Ledger;
            public readonly int[] Breaches = new int[4];
            public readonly float[] DamageByLane = new float[4];
            public StageThreatObservation Observation;
        }

        private static Purchase[] ReadLedger(string ledger)
        {
            var actions = new List<Purchase>();
            foreach (Match match in Regex.Matches(ledger, @"(?m)^([\d.]+) (build|upgrade|fusion) (.+) @(\d+)\r?$")) {
                string command = match.Groups[2].Value;
                string kind = match.Groups[3].Value;
                if (command == "fusion" && kind != "(Marker+Binding) -> Beacon")
                    throw new ArgumentException("Unexpected source recipe");
                actions.Add(new Purchase {
                    Time = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    Command = command,
                    Kind = command == "fusion" ? TowerKind.Beacon : (TowerKind)Enum.Parse(typeof(TowerKind), kind),
                    Socket = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture)
                });
            }
            if (actions.Count != 18 || actions[13].Kind != TowerKind.Beacon ||
                actions[14].Socket != 14 || actions[15].Socket != 22 || actions[16].Socket != 1 || actions[17].Socket != 15)
                throw new ArgumentException("Source purchases do not match the expected decision");
            return actions.ToArray();
        }

        private static bool TryPurchase(DefenseSimulation game, Purchase purchase, out string why)
        {
            if (purchase.Command == "build") return game.TryBuild(purchase.Socket, purchase.Kind, null, out why);
            Tower tower = game.TowerAt(purchase.Socket);
            if (tower == null) throw new InvalidOperationException("Missing tower in fixed purchase sequence");
            if (purchase.Command == "fusion") return game.TryFuse(7, tower.Id, game.TowerAt(3).Id, purchase.Socket, null, out why);
            if (tower.Kind != purchase.Kind) throw new InvalidOperationException("Wrong tower in fixed purchase sequence");
            return game.TryUpgrade(tower.Id, null, out why);
        }

        private static Result Run(int seed, bool eastFirst, Purchase[] actions)
        {
            const float dt = .05f;
            var game = new DefenseSimulation(new GameBalance(), seed, new CampaignBattle {
                Stage = CampaignCatalog.Stages[10], Hunter = 2, Loadout = 263, MarkerUnlocked = true
            });
            var result = new Result { Seed = seed, EastFirst = eastFirst, Observation = new StageThreatObservation(game.BoardSockets.Length) };
            var choices = new List<string>();
            var preferences = new[] { CardKind.Range, CardKind.Ballistic, CardKind.Range };
            int n = 0;
            string why;
            while (actions[n].Time == 0) {
                if (!TryPurchase(game, actions[n], out why)) throw new InvalidOperationException(why);
                n++;
            }
            if (n != 7 || game.Telemetry.CreditsSpent != 315 || game.Credits != 5)
                throw new InvalidOperationException("The common preparation does not match the source");
            game.Start();

            for (int tick = 0; tick < 6500 && game.State != RunState.Won && game.State != RunState.Lost; tick++) {
                if (game.AwaitingCard) {
                    CardKind preferred = preferences[choices.Count];
                    CardKind picked = game.CardOffers.Contains(preferred) ? preferred : game.CardOffers[0];
                    if (!game.ChooseCard(picked)) throw new InvalidOperationException("Card rejected");
                    choices.Add(picked.ToString());
                }

                if (n < actions.Length && game.Elapsed >= actions[n].Time) {
                    int order = !eastFirst || n < 14 ? n : 14 + (n - 14 + 2) % 4;
                    if (TryPurchase(game, actions[order], out why)) {
                        result.MaximumDelay = Math.Max(result.MaximumDelay, (float)game.Elapsed - actions[n].Time);
                        n++;
                    }
                }

                game.Tick(dt, new V2());
                result.Observation.Sample(game, dt);
                foreach (CombatEvent effect in game.Events) {
                    if (effect.Kind != EventKind.CoreHurt) continue;
                    int lane = effect.Source;
                    if (lane != 0 && lane != 2) throw new InvalidOperationException("Unexpected active lane");
                    result.Breaches[lane]++;
                    result.DamageByLane[lane] += effect.Value;
                    if (lane == 0 && result.FirstWestDamage < 0) result.FirstWestDamage = game.Elapsed;
                    if (lane == 2 && result.FirstEastDamage < 0) result.FirstEastDamage = game.Elapsed;
                }
                game.Events.Clear();
            }
            result.State = game.State.ToString();
            result.Core = game.MinimumCoreHealth;
            result.Elapsed = game.Elapsed;
            result.Spent = game.Telemetry.CreditsSpent;
            result.Credits = game.Credits;
            result.Towers = game.Towers.Count;
            result.Kills = game.Kills;
            result.Spawned = game.Telemetry.Spawned;
            result.Remaining = game.Enemies.Count;
            result.AllSpawned = game.AllWavesSpawned;
            result.Completed = n;
            result.Cards = choices.ToArray();
            result.Ledger = game.Telemetry.Purchases.ToArray();
            result.MarkedHits = game.Telemetry.MarkedHits;
            result.MarkBonus = game.Telemetry.MarkBonusDamage;
            return result;
        }

        public static Result[] Compare(string ledger)
        {
            Purchase[] actions = ReadLedger(ledger);
            var results = new List<Result>();
            foreach (int seed in new[] { 7, 19, 41 }) {
                results.Add(Run(seed, false, actions));
                results.Add(Run(seed, true, actions));
            }
            return results.ToArray();
        }
    }
}
