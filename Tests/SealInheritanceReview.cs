using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class SealInheritanceReview
    {
        public sealed class Row
        {
            public string Stage, Route, Policy, Outcome, Variant;
            public int Seed, Budget, Spent, Stars, Kills, SealShots, SealWaves, Refill, PrimaryLane;
            public int TowersAtFork, TowersAtEnd;
            public bool RefillFailed;
            public double Fork, SealAt, End;
            public float MinimumCore, CoreAtFork, SealDamage;
            public float[] LaneDamage;
            public string[] Opening;
        }
        public sealed class Volley
        {
            public string Formation, Attack, Route, Policy;
            public int Seed, Kills, Slowed, Spent;
            public float Damage, ThreatHp;
        }

        static (FusionRecipe First, FusionRecipe Final) Recipes(int route, bool expanded)
        {
            int[] first = expanded ? new[]{6,9,11} : new[]{0,1,2};
            int[] final = expanded ? new[]{8,10,12} : new[]{3,4,5};
            return (TowerCatalog.Recipes[first[route]],TowerCatalog.Recipes[final[route]]);
        }

        static int Site(DefenseSimulation game, int lane, float portion, HashSet<int> taken)
        {
            V2[] road = game.Roads[lane];
            V2 point = BattlefieldLayouts.Along(road, BattlefieldLayouts.RoadLength(road) * portion, out _);
            int socket = Enumerable.Range(0, game.BoardSockets.Length).Where(i => !taken.Contains(i))
                .OrderBy(i => V2.DistanceSquared(point, game.BoardSockets[i])).First();
            taken.Add(socket);
            return socket;
        }

        static Row Play(int stage, int primaryLane, int seed, int budget, int route, string policy, bool expanded, bool reserved)
        {
            var (first, final)=Recipes(route,expanded);
            CampaignBattle rules = RailChoiceReview.Battle(stage);
            rules.Loadout = expanded ? 5 | (1 << (int)first.First) | (1 << (int)first.Second) | (1 << (int)final.Second) : 135;
            var game = new DefenseSimulation(new GameBalance(), seed, rules, policy != "legacy");
            var row = new Row {
                Stage = CampaignCatalog.StageId(stage), Route = first.Result.ToString(), Policy = policy,
                Seed = seed, Budget = budget, PrimaryLane = primaryLane, Fork = -1, SealAt = -1,
                Variant = reserved ? "reserved" : "ceiling"
            };
            int[] lanes = game.OpenLanes.OrderBy(lane => lane == primaryLane ? 1 : 0).ThenBy(lane => lane).ToArray();
            var taken = new HashSet<int>();
            int anchor = Site(game, primaryLane, .68f, taken);
            int partner = Site(game, primaryLane, .52f, taken);
            // Put the last material on another road when there is one. Expanding and
            // keeping that post is the opportunity cost of committing to the capstone.
            int materialLane = lanes[0];
            int material = Site(game, materialLane, .70f, taken);
            var posts = new List<(int Socket, TowerKind Kind)>();
            foreach (float portion in new[] { .83f, .55f, .39f, .91f, .27f })
                foreach (int lane in lanes) {
                    TowerKind kind=portion == .83f || portion == .39f || portion == .27f ? TowerKind.Autocannon : TowerKind.Rupture;
                    if(expanded && route==0 && lane==primaryLane && portion==.83f)kind=TowerKind.Marker;
                    posts.Add((Site(game,lane,portion,taken),kind));
                }

            int reserve=reserved ? TowerCatalog.Get(TowerKind.Autocannon).BuildCost : 0;
            bool Afford(int cost) => game.Credits >= cost && game.Telemetry.CreditsSpent + cost <= budget-(row.Fork<0 ? reserve : 0);
            bool Place(int socket, TowerKind kind)
            {
                if (game.TowerAt(socket) != null) return true;
                return game.Towers.Count < game.TowerLimit && Afford(TowerCatalog.Get(kind).BuildCost) &&
                    game.TryBuild(socket,kind,null,out _);
            }
            bool Level(int socket,int level)
            {
                Tower tower = game.TowerAt(socket);
                if (tower == null || level > game.TowerLevelCap(tower.Kind)) return false;
                while (tower.Level < level)
                    if (!Afford(TowerCatalog.Get(tower.Kind).UpgradePrice(tower.Level)) || !game.TryUpgrade(tower.Id,null,out _)) return false;
                return true;
            }
            bool Fuse(FusionRecipe recipe,int other)
            {
                Tower a = game.TowerAt(anchor), b = game.TowerAt(other);
                return a != null && b != null && game.Cores > 0 && Afford(recipe.Credits) &&
                    game.TryFuse(recipe.Id,a.Id,b.Id,anchor,null,out _);
            }

            int phase = 0;
            int nextPost = 0;
            int openingPosts = lanes.Length == 1 ? 2 : lanes.Length;
            // Leave room for the full recipe and the first fusion's replacement even at 650.
            // Observer's opening Marker costs five more than an Autocannon.
            if(expanded)openingPosts=Math.Min(openingPosts,(budget-new[]{455,495,500}[route]-40-reserve-(route==0 ? 5 : 0))/40);
            var firingWaves = new HashSet<int>();
            void FillPosts(int limit)
            {
                while (nextPost < Math.Min(limit, posts.Count)) {
                    var post = posts[nextPost];
                    if (!Place(post.Socket,post.Kind)) break;
                    nextPost++;
                }
            }
            void Invest()
            {
                if (phase == 0) {
                    Place(anchor,first.First);
                    Place(material,final.Second);
                    FillPosts(openingPosts);
                    if (!Place(partner,first.Second) || !Level(anchor,2) || !Level(partner,2)) return;
                    if (!Fuse(first,partner)) return;
                    phase++;
                }
                if (phase == 1) {
                    if (!Place(partner,TowerKind.Autocannon) || !Level(anchor,2) || !Level(material,2)) return;
                    // Optional posts must never spend the money needed to reach the branch.
                    if(!expanded || nextPost<posts.Count && game.Telemetry.CreditsSpent+final.Credits+reserve+
                        TowerCatalog.Get(posts[nextPost].Kind).BuildCost<=budget)FillPosts(openingPosts+1);
                    if (game.Cores == 0 || !Afford(final.Credits) || game.Credits<final.Credits+reserve) return;
                    row.Fork = game.Elapsed;
                    row.CoreAtFork = game.MinimumCoreHealth;
                    row.Opening = game.Telemetry.Purchases.ToArray();
                    row.TowersAtFork = game.Towers.Count;
                    if (policy != "hold") {
                        if (!Fuse(final,material)) throw new InvalidOperationException("Final fusion failed after the shared opening.");
                        row.SealAt = game.Elapsed;
                    }
                    phase++;
                }
                if (game.TowerAt(material) == null) {
                    if (!Place(material,TowerKind.Autocannon)) return;
                    row.Refill = TowerCatalog.Get(TowerKind.Autocannon).BuildCost;
                }
                // Reserved comparison keeps the shared fork's tower count for all three policies.
                if(!reserved)FillPosts(posts.Count);
                foreach (var post in posts)
                    Level(post.Socket,2);
                Level(material,2);
            }

            Invest();
            game.Start();
            while (game.State != RunState.Won && game.State != RunState.Lost && game.Elapsed < 420) {
                if (game.AwaitingCard) {
                    CardKind card = game.CardOffers.Contains(CardKind.Range) ? CardKind.Range :
                        game.CardOffers.Contains(CardKind.Hunter) ? CardKind.Hunter : game.CardOffers[0];
                    game.ChooseCard(card);
                }
                Invest();
                game.Tick(1f/30,new V2());
                // One capstone in this replay. A cone volley emits one shot event per projectile on the same step.
                if (game.Events.Exists(effect => effect.Kind==EventKind.TowerShot && effect.Source==(int)TowerKind.Seal)) {
                    row.SealShots++;
                    firingWaves.Add(game.Wave+1);
                }
                game.Events.Clear();
            }
            row.Outcome = game.State.ToString();
            row.Stars = CampaignCatalog.Stars(game.State == RunState.Won,game.MinimumCoreHealth,game.Balance.CoreHealth);
            row.MinimumCore = game.MinimumCoreHealth;
            row.Kills = game.Kills;
            row.End = Math.Round(game.Elapsed,2);
            row.Spent = game.Telemetry.CreditsSpent;
            row.LaneDamage = game.Telemetry.CoreDamageByLane;
            row.SealDamage = game.Telemetry.TowerDamage[(int)TowerKind.Seal];
            row.SealWaves = firingWaves.Count;
            row.TowersAtEnd = game.Towers.Count;
            row.RefillFailed = policy!="hold" && row.Fork>=0 && row.Refill==0;
            return row;
        }

        public static Row[] Run(bool expanded = false)
        {
            foreach(CampaignStage stage in CampaignCatalog.Stages)BattlefieldLayouts.Get(stage);
            var rows = new System.Collections.Concurrent.ConcurrentBag<Row>();
            var jobs = new List<(int Stage,int Lane,int Seed,int Budget,int Route,bool Reserved)>();
            var cases = expanded ? new[] { ("C02-S006",1,2), ("C03-S004",2,2), ("C03-S006",0,3), ("C03-S006",2,3),
                ("C03-S008",1,3) } :
                new[] { ("C01-S014",1,3), ("C03-S003",0,3), ("C03-S004",2,3), ("C03-S006",2,3), ("C03-S008",1,3) };
            foreach (var (id, lane, routes) in cases) {
                int stage = CampaignCatalog.FindStage(id);
                foreach (bool reserved in expanded ? new[]{false,true} : new[]{false})
                foreach (int budget in expanded ? new[]{650,750,925} : new[]{850,1000})
                    foreach (int seed in expanded ? new[]{7,19,41,101,202} : RailChoiceReview.Seeds)
                        for (int route=0;route<routes;route++) {
                            jobs.Add((stage,lane,seed,budget,route,reserved));
                        }
            }
            Parallel.ForEach(jobs,new ParallelOptions {MaxDegreeOfParallelism=4},job=> {
                var legacy=Play(job.Stage,job.Lane,job.Seed,job.Budget,job.Route,"legacy",expanded,job.Reserved);
                var inherited=Play(job.Stage,job.Lane,job.Seed,job.Budget,job.Route,"inherited",expanded,job.Reserved);
                var hold=Play(job.Stage,job.Lane,job.Seed,job.Budget,job.Route,"hold",expanded,job.Reserved);
                if (legacy.Fork != inherited.Fork || legacy.Fork != hold.Fork ||
                    legacy.CoreAtFork != inherited.CoreAtFork || legacy.CoreAtFork != hold.CoreAtFork ||
                    !(legacy.Opening ?? new string[0]).SequenceEqual(inherited.Opening ?? new string[0]) ||
                    !(legacy.Opening ?? new string[0]).SequenceEqual(hold.Opening ?? new string[0]))
                    throw new InvalidOperationException("The three policies must fork from the identical opening.");
                if(expanded && legacy.Fork<0)throw new InvalidOperationException("Expanded comparison never reached its branch: "+legacy.Stage+" lane "+job.Lane+" budget "+job.Budget+" seed "+job.Seed+" route "+job.Route+" reserved "+job.Reserved+" spent "+legacy.Spent);
                foreach (Row row in new[]{legacy,inherited,hold}) {
                    if(row.Spent>job.Budget)throw new InvalidOperationException("Spending exceeded the common ceiling.");
                    if(job.Reserved && (row.RefillFailed || row.TowersAtEnd!=legacy.TowersAtFork))
                        throw new InvalidOperationException("Reserved comparison must refill and retain the shared tower count.");
                    rows.Add(row);
                }
            });
            return rows.OrderBy(r=>r.Stage).ThenBy(r=>r.PrimaryLane).ThenBy(r=>r.Variant).ThenBy(r=>r.Budget)
                .ThenBy(r=>r.Seed).ThenBy(r=>r.Route).ThenBy(r=>r.Policy).ToArray();
        }

        // Each route and its legacy control spend exactly the same amount on the same slot.
        // Keep the enemies stationary so these
        // fixtures measure targeting, hit thresholds and geometry, independently of follow-up purchases.
        public static Volley[] RunVolleys(bool expanded = false)
        {
            var rows = new List<Volley>();
            var formations = new Dictionary<string,(float X,float Y,float Health,EnemyKind Kind)[]> {
                ["spaced"] = new[] { (2f,0f,95f,EnemyKind.Grunt), (4.8f,0f,80f,EnemyKind.Grunt),
                    (7.5f,0f,60f,EnemyKind.Grunt), (8.1f,.5f,45f,EnemyKind.Grunt) },
                ["elite-and-escort"] = new[] { (2f,0f,80f,EnemyKind.Runner), (4f,0f,150f,EnemyKind.Brute),
                    (4.5f,.5f,75f,EnemyKind.Grunt), (4.8f,-.5f,75f,EnemyKind.Grunt) },
                ["crowded junction"] = new[] { (3f,0f,118f,EnemyKind.Runner), (3.8f,.3f,56f,EnemyKind.Grunt),
                    (4.2f,-.3f,56f,EnemyKind.Grunt), (4.6f,.5f,56f,EnemyKind.Grunt),
                    (4.7f,-.5f,56f,EnemyKind.Grunt), (4.9f,.1f,56f,EnemyKind.Grunt) },
                ["compact quartet"] = new[] { (3f,0f,115f,EnemyKind.Runner), (3.3f,.7f,60f,EnemyKind.Grunt),
                    (3.5f,-.4f,60f,EnemyKind.Grunt), (4f,.1f,60f,EnemyKind.Grunt) }
            };
            if(expanded)formations=new Dictionary<string,(float X,float Y,float Health,EnemyKind Kind)[]> {
                ["marked rear"] = new[] { (2f,0f,122f,EnemyKind.Runner), (5f,0f,155f,EnemyKind.Brute),
                    (5.2f,.6f,46f,EnemyKind.Grunt), (5.5f,-.5f,46f,EnemyKind.Grunt) },
                ["wide fan"] = new[] { (2f,0f,125f,EnemyKind.Runner), (3.2f,-1.75f,65f,EnemyKind.Grunt),
                    (4.5f,2.1f,54f,EnemyKind.Grunt), (4f,-2.8f,54f,EnemyKind.Grunt),
                    (4.7f,-.25f,54f,EnemyKind.Grunt), (4.6f,1.75f,54f,EnemyKind.Grunt) },
                ["rail column"] = new[] { (2f,0f,122f,EnemyKind.Runner), (3.8f,.15f,66f,EnemyKind.Grunt),
                    (4.8f,-.2f,66f,EnemyKind.Grunt), (6.2f,.15f,65f,EnemyKind.Grunt),
                    (6.9f,0f,66f,EnemyKind.Grunt), (7.5f,.75f,34f,EnemyKind.Grunt) },
                ["compact quartet"] = formations["compact quartet"]
            };
            foreach (int seed in expanded ? new[]{7,19,41,101,202} : RailChoiceReview.Seeds)
                foreach (var formation in formations)
                    for (int attack=0;attack<(expanded ? 6 : 4);attack++) {
                        int route=expanded ? attack/2 : Math.Max(0,attack-1);
                        bool inherited=expanded ? attack%2==1 : attack!=0;
                        var (first,final)=Recipes(route,expanded);
                        int cost=expanded ? new[]{455,495,500}[route] : 455;
                        int index=CampaignCatalog.FindStage(expanded ? "C03-S008" : "C01-S014");
                        var stage=CampaignCatalog.Stages[index];
                        CampaignBattle rules=new CampaignBattle { Stage=stage, ObserverUnlocked=expanded, MarkerUnlocked=expanded,
                            ScatterUnlocked=expanded, BarrageUnlocked=expanded, BarrageSealUnlocked=expanded,
                            PiercerUnlocked=expanded, SurgeUnlocked=expanded, SurgeSealUnlocked=expanded };
                        rules.Loadout=1 | (1 << (int)first.First) | (1 << (int)first.Second) | (1 << (int)final.Second);
                        var game=new DefenseSimulation(new GameBalance { StartingCredits=cost-stage.StartingCreditBonus,
                            PlayerDamage=0, PlayerRange=.1f, SpawnEnemies=false, OfferCards=false },seed,
                            rules,inherited);
                        int[] sockets={0,1,2};
                        TowerKind[] kinds={first.First,first.Second,final.Second};
                        for(int i=0;i<sockets.Length;i++) {
                            if(!game.TryBuild(sockets[i],kinds[i],null,out string why) ||
                                !game.TryUpgrade(game.TowerAt(sockets[i]).Id,null,out why))
                                throw new InvalidOperationException(why);
                        }
                        game.Start();
                        game.Tick(60.01f,new V2());
                        if(!game.TryFuse(first.Id,game.TowerAt(0).Id,game.TowerAt(1).Id,0,null,out string reason) ||
                            !game.TryUpgrade(game.TowerAt(0).Id,null,out reason))throw new InvalidOperationException(reason);
                        game.Tick(60f,new V2());
                        if(!game.TryFuse(final.Id,game.TowerAt(0).Id,game.TowerAt(2).Id,0,null,out reason))
                            throw new InvalidOperationException(reason);
                        Tower seal=game.TowerAt(0);
                        if(game.Telemetry.CreditsSpent!=cost || game.Cores!=0)throw new InvalidOperationException("Volley fixture cost changed.");
                        // The real core fires one shot toward a group arranged independently of an existing map.
                        seal.Position=new V2(0,-5);
                        seal.Commission=0; seal.Cooldown=0;
                        var enemies=formation.Value.Select((entry,i)=>new Enemy {
                            Id=1000+i, Lane=game.OpenLanes[0], Waypoint=1, Kind=entry.Kind,
                            Health=entry.Health, MaxHealth=entry.Health, Position=seal.Position+new V2(entry.X,entry.Y)
                        }).ToArray();
                        if(formation.Key=="marked rear") { enemies[1].MarkRemaining=3; enemies[1].MarkBonus=5; }
                        game.Enemies.AddRange(enemies);
                        game.Tick(.01f,new V2());
                        seal.Cooldown=10;
                        game.Tick(.32f,new V2());
                        Enemy threat=formation.Key=="elite-and-escort" || formation.Key=="marked rear" ? enemies[1] : enemies[0];
                        rows.Add(new Volley { Formation=formation.Key, Seed=seed, Attack=seal.Inheritance.ToString(), Route=first.Result.ToString(),
                            Policy=inherited ? "inherited" : "legacy",
                            Kills=game.Kills, Slowed=enemies.Count(enemy=>enemy.Health>0 && enemy.SlowFraction>0),
                            Damage=game.Telemetry.TowerDamage[(int)TowerKind.Seal], ThreatHp=Math.Max(0,threat.Health),
                            Spent=game.Telemetry.CreditsSpent });
                    }
            return rows.ToArray();
        }
    }
}
