using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class ChapterContinuationReview
    {
        public sealed class Battle
        {
            public string Stage, Outcome;
            public int Seed, Spawned, Total, Kills, Stars, Supplies, Cards, Catalysts, Spent, Remaining, Towers;
            public double End, BossWarning = -1, BossImpact = -1, BossKilled = -1, AllSpawned = -1, SealAt = -1;
            public float Core, Hunter;
            public bool BossDefeated;
            public string[] Picks, Purchases;
        }

        static void Require(bool value, string reason)
        {
            if (!value) throw new InvalidOperationException(reason);
        }

        static int Index(string id)
        {
            int index = CampaignCatalog.FindStage(id);
            Require(index >= 0, "Missing stage " + id);
            return index;
        }

        static readonly TowerKind Spear = TowerKind.Autocannon, Rupture = TowerKind.Rupture;

        public static CampaignProfile Fixture(int finishedThrough, int balance)
        {
            var p = new CampaignProfile { MagicCores = balance, Growth = new[]{5,3,2}, Research = new[]{3,2,2,2}, Loadout = 7, Muted = true };
            for (int i=0; i<=finishedThrough; i++) { p.BestStars[i]=2; p.FirstCleared[i]=true; p.BestErosion[i]=.18f; }
            p.StorySeenIds.Add("C02-S006");
            p.TutorialSeenIds.Add("C02-S006");
            p.UnmappedProgress.Add("C06-S001",new StageProgress());
            return p;
        }

        static int Site(DefenseSimulation game,int lane,float fraction,HashSet<int> used)
        {
            V2[] road=game.Roads[lane];
            V2 point=BattlefieldLayouts.Along(road,BattlefieldLayouts.RoadLength(road)*fraction,out _);
            return Enumerable.Range(0,game.BoardSockets.Length).Where(site=>!used.Contains(site))
                .OrderBy(site=>V2.DistanceSquared(point,game.BoardSockets[site])).First();
        }

        public static Battle Play(DefenseSimulation game, bool fuse)
        {
            var report = new Battle { Stage=game.Rules.Stage.Id, Seed=game.Seed, Total=game.Schedule.Sum(g=>g.Count) };
            int[] lanes = game.Schedule.GroupBy(g=>g.Lane).OrderBy(groups=>groups.Min(g=>g.Start)).Select(groups=>groups.Key).ToArray();
            var used = new HashSet<int>();
            var orders = new List<(int Site,TowerKind Kind)>();
            void Add(int lane,float fraction,TowerKind kind)
            {
                if(orders.Count>=14)return;
                int site=Site(game,lane,fraction,used);
                used.Add(site);
                orders.Add((site,kind));
            }
            if(fuse) {
                V2 preferred=game.BoardSockets[Site(game,lanes[0],.80f,used)];
                int anchor=Enumerable.Range(0,game.BoardSockets.Length)
                    .OrderByDescending(site=>lanes.Count(lane=>BattlefieldLayouts.RoadDistance(game.BoardSockets[site],game.Roads[lane])<=4.8f))
                    .ThenBy(site=>V2.DistanceSquared(game.BoardSockets[site],preferred)).First();
                orders.Add((anchor,Spear));used.Add(anchor);
                Add(lanes[0],.54f,TowerKind.Binding); Add(lanes[0],.45f,Rupture);
                foreach(int lane in lanes.Skip(1))Add(lane,.57f,Rupture);
                foreach(int lane in lanes.Skip(1))Add(lane,.75f,Spear);
            } else {
                foreach(int lane in lanes) { Add(lane,.55f,Rupture); Add(lane,.76f,Spear); }
                foreach(int lane in lanes) {
                    int earlySiege=game.Schedule.Where(g=>g.Lane==lane && g.Start<120 && g.Kind==EnemyKind.Siege).Sum(g=>g.Count);
                    if(earlySiege>=5) { Add(lane,.30f,Spear); Add(lane,.84f,Spear); Add(lane,.52f,Spear); }
                }
            }
            foreach(float portion in new[]{.36f,.87f,.48f,.66f,.26f,.92f})
                foreach(int lane in lanes)
                    Add(lane,portion,portion<=.48f ? Rupture : Spear);

            int next=0;
            bool first=false,seal=false;
            var cards=new List<string>();
            void Upgrade(int slot)
            {
                Tower t=game.TowerAt(slot);
                if(t!=null && t.Level<2 && t.Level<game.TowerLevelCap(t.Kind) && game.Credits>=TowerCatalog.Get(t.Kind).UpgradeCost)
                    Require(game.TryUpgrade(t.Id,null,out string why),why);
            }
            void Invest()
            {
                if(fuse && next>=3 && !seal) {
                    int anchor=orders[0].Site, binding=orders[1].Site, blast=orders[2].Site;
                    if(game.Elapsed>=30) {
                        Upgrade(anchor); Upgrade(first ? blast : binding);
                        Tower a=game.TowerAt(anchor), b=game.TowerAt(first ? blast : binding);
                        int recipe=first ? 3 : 0;
                        if(a!=null && b!=null && a.Level>=2 && b.Level>=2 && game.Cores>0 && game.Credits>=TowerCatalog.Recipes[recipe].Credits) {
                            Require(game.TryFuse(recipe,a.Id,b.Id,anchor,null,out string why),why);
                            Require(game.TryBuild(b.Socket,Spear,null,out why) || game.Credits<40,why);
                            if(first) { seal=true;report.SealAt=game.Elapsed; } else first=true;
                        }
                    }
                    if(!seal) {
                        // Keep the second recipe affordable while spending any surplus on other entrances.
                        if(first && next<orders.Count) {
                            var nextOrder=orders[next];
                            if(game.Credits>=80+TowerCatalog.Get(nextOrder.Kind).BuildCost) {
                                Require(game.TryBuild(nextOrder.Site,nextOrder.Kind,null,out string why),why);
                                next++;
                            }
                        }
                        return;
                    }
                }
                while(next<orders.Count) {
                    var order=orders[next];
                    if(game.Credits<TowerCatalog.Get(order.Kind).BuildCost || game.Towers.Count>=game.TowerLimit)break;
                    Require(game.TryBuild(order.Site,order.Kind,null,out string why),why);
                    next++;
                }
                if(next==orders.Count || game.Towers.Count>=game.TowerLimit)
                    foreach(Tower t in game.Towers)Upgrade(t.Socket);
            }

            Invest();
            game.Start();
            while(game.State!=RunState.Won && game.State!=RunState.Lost && game.Elapsed<500) {
                if(game.AwaitingCard) {
                    CardKind chosen=game.CardOffers[0];
                    Require(game.ChooseCard(chosen),"Cannot accept card");
                    cards.Add(chosen.ToString());
                }
                Invest();
                game.Events.Clear();
                V2 move=game.Enemies.Any(e=>e.Kind==EnemyKind.Boss && e.BossWindup>0)
                    ? new V2(game.PlayerPosition.X>8 ? -1 : 1,0)
                    : (new V2(game.PlayerPosition.X,game.PlayerPosition.Y+2.2f)*-1).Normalized;
                game.Tick(1f/60,move);
                foreach(CombatEvent e in game.Events) {
                    if(e.IsBossWarning && report.BossWarning<0)report.BossWarning=game.Elapsed;
                    if(e.Kind==EventKind.Hit && e.Source==-2 && report.BossImpact<0)report.BossImpact=game.Elapsed;
                    if(e.Kind==EventKind.Kill && e.Source==(int)EnemyKind.Boss)report.BossKilled=game.Elapsed;
                }
                if(game.AllWavesSpawned && report.AllSpawned<0)report.AllSpawned=game.Elapsed;
            }
            report.End=game.Elapsed; report.Outcome=game.State.ToString(); report.Core=game.CoreHealth; report.Hunter=game.PlayerHealth;
            report.Spawned=game.Telemetry.Spawned; report.Kills=game.Kills; report.BossDefeated=game.BossDefeated;
            report.Stars=CampaignCatalog.Stars(game.State==RunState.Won,game.MinimumCoreHealth,game.Balance.CoreHealth);
            report.Supplies=game.SuppliesGranted;report.Cards=game.Telemetry.CardOffers;report.Catalysts=game.Telemetry.CoresGranted;
            report.Spent=game.Telemetry.CreditsSpent;report.Remaining=game.Credits;report.Towers=game.Towers.Count;
            report.Picks=cards.ToArray();report.Purchases=game.Telemetry.Purchases.ToArray();
            return report;
        }

        public static object Run(string output, string baselinePath)
        {
            int start=Index("C02-S007"), gate=Index("C02-S022"), rail=Index("C03-S001");
            Require(rail==gate+1 && CampaignCatalog.ChapterCount(2)==22,"Chapter range");
            bool oldV3Checked = File.Exists(baselinePath);
            if(oldV3Checked) {
                var baseline=CampaignStore.Decode(File.ReadAllBytes(baselinePath));
                Require(baseline.Valid() && baseline.HasCleared("C02-S006") && baseline.IsStageOpen(start) && !baseline.IsStageOpen(rail),"Old v3 frontier");
            }
            var p=Fixture(start-1,506);
            var store=new CampaignStore(Path.Combine(output,"model-sequence.sav"));
            store.Save(p);
            var campaign=new CampaignService(store.Load(),store.Save);
            var rows=new List<Battle>();
            var awards=new List<object>();
            var shapes=new HashSet<string>();
            CampaignProfile beforeGate=null;
            DefenseSimulation gateVictory=null;
            for(int i=start;i<=rail;i++) {
                CampaignStage stage=CampaignCatalog.Stages[i];
                Require(stage.Index==i && shapes.Add(stage.Layout.Id) && stage.PredecessorId==CampaignCatalog.Stages[i-1].Id,"Layout/progression identity "+stage.Id);
                Require(campaign.Profile.IsStageOpen(i) && (i==rail || !campaign.Profile.IsStageOpen(i+1)),"Wrong frontier "+stage.Id);
                Require(campaign.Begin(i,false,out string id),campaign.Error);
                var g=new DefenseSimulation(new GameBalance(),7,campaign.Battle(i,false));
                foreach(var site in g.BoardSockets) {
                    float distance=stage.Lanes.Min(lane=>BattlefieldLayouts.RoadDistance(site,g.Roads[lane]));
                    Require(distance>=2.14f && distance<=3.36f,stage.Id+" unreachable site");
                }
                for(int a=0;a<g.BoardSockets.Length;a++)
                    for(int b=0;b<a;b++)
                        Require(V2.Distance(g.BoardSockets[a],g.BoardSockets[b])>=2.55f,stage.Id+" site overlap");
                Require(stage.Lanes.All(lane=>g.Roads[lane].Length>=2) && g.BoardSockets.Length==stage.SocketCount &&
                    g.WaveCount==stage.Waves.Length && g.Schedule.All(group=>stage.Lanes.Contains(group.Lane)),stage.Id+" invalid map");
                Battle battle=Play(g,false); rows.Add(battle);
                Require(g.State==RunState.Won && g.Kills==battle.Total && g.Enemies.Count==0 &&
                    g.SuppliesGranted==5 && g.Telemetry.CardOffers==3 && g.Telemetry.CoresGranted==2,
                    stage.Id+" "+g.State+" at "+g.Elapsed.ToString("F2")+" after "+g.Kills+"/"+battle.Total+" kills; spent "+battle.Spent);
                int before=campaign.Profile.MagicCores;
                Require(campaign.Complete(id,g,out int reward) && reward==stage.FirstReward &&
                    campaign.Profile.MagicCores==before+reward,campaign.Error);
                Require(campaign.Complete(id,g,out int duplicate) && duplicate==reward &&
                    campaign.Profile.MagicCores==before+reward,"Double credit "+stage.Id);
                awards.Add(new { stage.Id, Before=before, Reward=reward, After=campaign.Profile.MagicCores });
                if(stage.Id==CampaignCatalog.MarketResearchGate) {
                    Require(campaign.Research(0) && campaign.Profile.Research[0]==4 && campaign.Profile.MagicCores==before+reward-12,campaign.Error);
                }
                if(stage.Id==CampaignCatalog.MarketGrowthGate) {
                    Require(campaign.Grow(0) && campaign.Grow(1) && campaign.Grow(2) &&
                        campaign.Profile.Growth.SequenceEqual(new[]{6,4,3}) && campaign.Profile.MagicCores==before+reward-78,campaign.Error);
                }
                if(i==gate-1) {
                    File.WriteAllBytes(Path.Combine(output,"model-2-21-cleared.sav"),CampaignStore.Encode(campaign.Profile));
                    beforeGate=campaign.Profile.Copy();
                    Require(!campaign.Profile.IsStageOpen(rail),"3-1 unlocked without 2-22");
                }
                CampaignProfile loaded=store.Load();
                Require(loaded.Valid() && loaded.PendingRun=="" && loaded.HasCleared(stage.Id) &&
                    loaded.StorySeenIds.SetEquals(p.StorySeenIds) && loaded.TutorialSeenIds.SetEquals(p.TutorialSeenIds) &&
                    loaded.UnmappedProgress.ContainsKey("C06-S001"),"Progress lost on reload");
                if(i==gate) {
                    gateVictory=g;
                    Require(g.BossDefeated && battle.BossKilled>0 && battle.BossKilled<battle.AllSpawned &&
                        battle.BossWarning>0 && Math.Abs(battle.BossImpact-battle.BossWarning-1.5)<.06 &&
                        loaded.IsStageOpen(rail),"2-22 boss, cleanup or 3-1 gate");
                }
                campaign=new CampaignService(loaded,store.Save);
            }
            var replay=new CampaignService(campaign.Profile,null);
            Require(replay.Begin(gate,false,out string retryId) && replay.Complete(retryId,gateVictory,out int retryAward) &&
                retryAward==2 && replay.Profile.HasCleared("C03-S001"),"2-22 replay grant");
            var crisis=new CampaignService(beforeGate,null);
            Require(crisis.Begin(gate,true,out string crisisId),crisis.Error);
            var crisisGame=new DefenseSimulation(new GameBalance(),7,crisis.Battle(gate,true));
            Battle crisisResult=Play(crisisGame,false);
            Require(crisisResult.Outcome=="Won" && crisis.Complete(crisisId,crisisGame,out int crisisReward) &&
                crisisReward==2 && crisis.Profile.CrisisStars[gate]>0 && !crisis.Profile.IsStageOpen(rail),"2-22 Crisis opened normal rail");
            var failed=new CampaignService(beforeGate,value=>{
                if(value.PendingRun=="")throw new IOException("synthetic completion failure");
            });
            Require(failed.Begin(gate,false,out string failedId) && !failed.Complete(failedId,gateVictory,out int failedReward) &&
                failedReward==0 && failed.Profile.PendingRun==failedId && failed.Profile.MagicCores==beforeGate.MagicCores &&
                !failed.Profile.IsStageOpen(rail),"2-22 failed write leaked normal rail or reward");
            int total=CampaignCatalog.Stages.Skip(start).Take(gate-start+1).Sum(s=>s.FirstReward);
            var review=new List<Battle>();
            foreach(string id in new[]{"C02-S012","C02-S020","C02-S022"}) {
                var rules=campaign.Battle(Index(id),false);
                review.Add(Play(new DefenseSimulation(new GameBalance(),41,rules),true));
            }
            var growth = new List<object>();
            foreach(string id in new[]{"C02-S018","C02-S022","C03-S001"})
                foreach(int seed in new[]{7,41})
                    foreach(bool recommended in new[]{false,true}) {
                        var rules=campaign.Battle(Index(id),false);
                        if(!recommended) { rules.EarnedGrowth=5;rules.Research[0]=3; }
                        Battle result=Play(new DefenseSimulation(new GameBalance(),seed,rules),false);
                        growth.Add(new { Recommended=recommended, result.Stage, result.Seed, result.Outcome, result.Core, result.Spent, result.End });
                    }
            return new { Battles=rows.ToArray(), Awards=awards.ToArray(), Fusion=review.ToArray(), OldV3Checked=oldV3Checked, CrisisGate=crisisResult,
                ReplayChecked=true, FailedCommitChecked=true, AdditionalMarketCores=total,
                GrowthReview=growth.ToArray(), RailFirst=CampaignCatalog.Stages[rail].FirstReward, FirstMarketPurchase=12, LaterGrowthPurchases=78,
                StartingCores=506, CoresBeforeRail=506+total-90, AfterRail=campaign.Profile.MagicCores };
        }
    }
}
