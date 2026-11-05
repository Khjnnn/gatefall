using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    // Bots that skip every decision the campaign means to teach: no lane reasoning, upgrades, fusions, sales,
    // wave calls, hunter movement, skills or account growth. When such a bot clears an operation with three stars,
    // that operation does not yet require those decisions. Report-only by default;
    // Tools/review-degenerate-strategies.ps1 -Enforce turns the target into a failing gate.
    // A lower bound from scripted play, not a human difficulty rating.
    public static class DegenerateStrategyReview
    {
        public sealed class Policy
        {
            public string Name, Description;
            public bool CoreFirst, Gated;
            public int MaxTowers;
        }
        public sealed class Row
        {
            public string Policy, Stage, Result;
            public int StageIndex, Seed, Stars, Towers, CreditsSpent, Kills;
            public float MinimumCore;
            public double Seconds;
        }

        public static readonly Policy[] Policies = {
            new Policy { Name = "spear-fill-core-first", Gated = true, CoreFirst = true, MaxTowers = int.MaxValue,
                Description = "Lv1 spear only; fill every site, nearest to the core first, whenever affordable" },
            new Policy { Name = "spear-fill-gates-first", Gated = true, CoreFirst = false, MaxTowers = int.MaxValue,
                Description = "Same, farthest from the core first" },
            new Policy { Name = "spear-8-core", Gated = true, CoreFirst = true, MaxTowers = 8,
                Description = "Eight Lv1 spears nearest to the core (the 320-credit preparation budget from 1-10), then nothing" },
            new Policy { Name = "spear-10-gates-control", Gated = false, CoreFirst = false, MaxTowers = 10,
                Description = "Control: ten Lv1 spears farthest from the core; shows whether placement matters once towers are scarce" },
            new Policy { Name = "no-towers-control", Gated = false, CoreFirst = true, MaxTowers = 0,
                Description = "Control: idle hunter only" }
        };
        public static readonly int[] Seeds = { 7, 19, 41 };

        public static Row[] Run()
        {
            var rows = new List<Row>();
            foreach (Policy policy in Policies)
                for (int stage = 0; stage < CampaignCatalog.Stages.Length; stage++)
                    foreach (int seed in Seeds) rows.Add(Play(policy, stage, seed));
            return rows.ToArray();
        }

        static Row Play(Policy policy, int stage, int seed)
        {
            // Kang Yun without growth or research, default loadout (spear, seal, collapse, barrier), normal operation.
            var g = new DefenseSimulation(new GameBalance(), seed, new CampaignBattle { Stage = CampaignCatalog.Stages[stage] });
            var order = new List<int>();
            for (int i = 0; i < g.BoardSockets.Length; i++) order.Add(i);
            order.Sort((a, b) => {
                int d = g.BoardSockets[a].Length.CompareTo(g.BoardSockets[b].Length);
                if (!policy.CoreFirst) d = -d;
                return d != 0 ? d : a.CompareTo(b);
            });
            int built = 0, limit = Math.Min(policy.MaxTowers, order.Count);
            Action act = () => {
                if (g.AwaitingCard) g.ChooseCard(g.CardOffers.Contains(CardKind.Credits) ? CardKind.Credits : g.CardOffers[0]);
                string why;
                while (built < limit && g.TryBuild(order[built], TowerKind.Autocannon, null, out why)) built++;
            };
            act(); g.Start();
            while (g.State != RunState.Won && g.State != RunState.Lost && g.Elapsed < 400) { act(); g.Tick(1f / 30, new V2()); g.Events.Clear(); }
            bool won = g.State == RunState.Won;
            return new Row {
                Policy = policy.Name, Stage = CampaignCatalog.StageId(stage), StageIndex = stage, Seed = seed,
                Result = g.State.ToString(), Stars = CampaignCatalog.Stars(won, g.MinimumCoreHealth, g.Balance.CoreHealth),
                MinimumCore = g.MinimumCoreHealth, Towers = g.Towers.Count, CreditsSpent = g.Telemetry.CreditsSpent,
                Kills = g.Kills, Seconds = Math.Round(g.Elapsed, 2)
            };
        }

        // One line per policy: wins, three-star count and the stars of every seed per operation (L = loss).
        public static string Summary(Row[] rows)
        {
            var lines = new List<string>();
            foreach (Policy policy in Policies) {
                int runs = 0, wins = 0, three = 0; var stages = new List<string>(); string cell = ""; int current = -1;
                foreach (Row row in rows) {
                    if (row.Policy != policy.Name) continue;
                    if (row.StageIndex != current) { if (current >= 0) stages.Add(cell); current = row.StageIndex; cell = (row.StageIndex + 1) + ":"; }
                    runs++; if (row.Result == "Won") wins++; if (row.Stars == 3) three++;
                    cell += row.Result == "Won" ? row.Stars.ToString() : "L";
                }
                if (current >= 0) stages.Add(cell);
                lines.Add(policy.Name + (policy.Gated ? " [gated]" : " [control]") + ": wins " + wins + "/" + runs + ", 3-star " + three + "/" + runs);
                lines.Add("    " + string.Join(" ", stages));
            }
            return string.Join("\n", lines);
        }

        // Gated policies must not exceed maxStars from operation fromStage (1-based) onwards.
        public static string[] Violations(Row[] rows, int fromStage, int maxStars)
        {
            var gated = new HashSet<string>();
            foreach (Policy policy in Policies) if (policy.Gated) gated.Add(policy.Name);
            var found = new List<string>();
            foreach (Row row in rows)
                if (gated.Contains(row.Policy) && row.StageIndex + 1 >= fromStage && row.Stars > maxStars)
                    found.Add(row.Policy + " " + row.Stage + " seed " + row.Seed + ": " + row.Stars + " stars, minimum core " + row.MinimumCore.ToString("F0"));
            return found.ToArray();
        }
    }
}
