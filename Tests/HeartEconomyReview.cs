using System;
using System.Collections.Generic;
using System.Linq;
using Gatefall.Core;

namespace Gatefall.Tests
{
    // A clock/behaviour model, not a combat simulator or measured human win rate.
    public static class HeartEconomyReview
    {
        public sealed class Row
        {
            public string Economy, Player, Pattern, Policy, FirstShortage="", EndOperation;
            public int Seed, Day, Start, Used, Initial, Backfill, Regen, Clears, Attendance, Missions, Roulette, Pass, Shop, Ads, Crystals, End, FirstShortageClears=-1;
            public int Attempts, FreeLaunches, Wins, Losses, CrisisAttempts, Dialogs, ChapterOneDialogs, ChapterOneEndedWith=-1, PlayMinutes;
            public double WaitMinutes;
        }
        sealed class Pattern
        {
            public string Name;public int Minutes;public int[] Starts;
            public Pattern(string name,int minutes,params int[] starts){Name=name;Minutes=minutes;Starts=starts;}
        }
        // A two-hour day split over four visits. The contiguous case is a separate stress test.
        static readonly Pattern[] patterns={new Pattern("3x20",20,480,780,1140),new Pattern("3x30",30,480,780,1140),
            new Pattern("3x45",45,480,780,1140),new Pattern("4x30-heavy",30,480,720,960,1200),new Pattern("1x120-stress",120,1080)};
        sealed class RandomStream
        {
            uint state;
            public RandomStream(int seed){state=(uint)seed;}
            public int Next(int max){unchecked{state=state*1664525u+1013904223u;}return (int)(state%(uint)max);}
        }
        static void Require(bool pass,string why){if(!pass)throw new Exception("Heart economy: "+why);}
        static int Total(CampaignProfile p){return p.Hearts+p.ExtraHearts;}
        static int OldClear(int stage,bool first,bool three)
        {
            var s=CampaignCatalog.Stages[stage];return (three ? 1 : 0)+(first ? (s.Chapter==1 ? 10 : s.Chapter==2 ? 3 : 2)+
                (s.LocalNumber==CampaignCatalog.ChapterCount(s.Chapter) ? 10 : 0) : 0);
        }
        // Explicit assumptions: learned play improves each retry by 10 points (ceiling 95%).
        // Rail review bots bound difficulty but are not a human probability estimate.
        static int WinChance(int stage,bool crisis,int retries)
        {
            var s=CampaignCatalog.Stages[stage];int chance=s.Chapter==1 ? (s.LocalNumber<=3 ? 100 : 95) :
                s.Chapter==2 ? (s.LocalNumber<=11 ? 70 : 55) :
                s.LocalNumber==3 || s.LocalNumber==6 || s.LocalNumber==8 ? 50 : 70;
            return chance==100 && !crisis ? 100 : Math.Min(95,chance-(crisis ? 15 : 0)+10*retries);
        }
        static void Refresh(CampaignProfile p,long now,int day,Row row)
        {int before=Total(p);HeartRules.Refresh(p,now,day);row.Regen+=Total(p)-before;}
        static int Grant(CampaignProfile p,int amount)
        {int before=Total(p);HeartRules.Grant(p,amount);return Total(p)-before;}
        static int Reward(CampaignProfile p,DailyReward reward)
        {int before=Total(p);Require(DailyRules.Grant(p,reward),"daily capacity");return Total(p)-before;}
        static void Claims(CampaignProfile p,Row row)
        {
            var d=p.Daily;
            foreach(var m in DailyCatalog.Missions()){
                int i=(int)m.Mission;if(d.Progress[i]<m.Target || (d.MissionClaims&(1<<i))!=0)continue;
                row.Missions+=Reward(p,new DailyReward(DailyRewardKind.MagicCores,m.MagicCores));
                d.MissionClaims|=1<<i;DailyRules.Points(p,m.PassPoints);
                if(d.MissionClaims==63)Reward(p,new DailyReward(DailyRewardKind.NormalTicket,1));
            }
            foreach(var t in DailyCatalog.PassRewards())if(t.Tier*DailyCatalog.PointsPerTier<=d.PassPoints && (d.FreeClaims&(1<<(t.Tier-1)))==0){
                row.Pass+=Reward(p,t.Free);d.FreeClaims|=1<<(t.Tier-1);
            }
        }
        static Row[] Simulate(bool old,bool updated,Pattern pattern,string policy,int seed,int days)
        {
            var combat=new RandomStream(seed);var stars=new RandomStream(seed+101);var roulette=new RandomStream(seed+211);
            var p=new CampaignProfile();int next=0,crisisRemaining=0,crisisStage=0,retries=0,firstClears=-1;string first="";
            // Equivalent to the historical 108-star / +274 example: two 1-star, twenty 2-star, twenty-two 3-star clears.
            if(updated){next=p.FirstCleared.Length;for(int i=0;i<next;i++){p.FirstCleared[i]=true;p.BestStars[i]=i<2 ? 1 : i<22 ? 2 : 3;}}
            var rows=new List<Row>();
            for(int day=1;day<=days;day++){
                int localDay=DailyCatalog.SeasonEpochDay+day-1;
                var row=new Row{Economy=old ? "before" : "after",Player=updated ? "updated" : "new",Pattern=pattern.Name,Policy=policy,Seed=seed,Day=day,Start=day==1 ? 0 : Total(p)};
                if(day==1){row.Initial=5;if(updated){int before=Total(p);
                    if(old){for(int i=0;i<next;i++)HeartRules.Grant(p,OldClear(i,true,p.BestStars[i]==3));p.HeartRewardsBackfilled=true;}
                    else HeartRules.Backfill(p);row.Backfill=Total(p)-before;}}
                foreach(int start in pattern.Starts){
                    long now=(day-1)*86400L+start*60L,end=now+pattern.Minutes*60;
                    Refresh(p,now,localDay,row);
                    if(p.Daily.AttendanceDay<p.Daily.LocalDay){row.Attendance+=Reward(p,DailyCatalog.Attendance()[p.Daily.AttendancePosition]);
                        p.Daily.AttendanceDay=p.Daily.LocalDay;p.Daily.AttendancePosition=(p.Daily.AttendancePosition+1)%7;}
                    // Optional roulette once per visit when available, with its real cooldown and daily limit.
                    if(policy!="no-ads" && p.FirstCleared[2] && p.Daily.RouletteSpins<3 && (p.Daily.LastSpinUtc<0 || now-p.Daily.LastSpinUtc>=14400)){
                        var slot=DailyCatalog.Roll(roulette.Next);row.Roulette+=Reward(p,slot.Reward);p.Daily.RouletteSpins++;p.Daily.LastSpinUtc=now;
                        DailyRules.Progress(p,DailyMission.Ad,1);}
                    // One affordable normal box/day; cards are outside the heart model, only its cost/mission matter.
                    if(p.FirstCleared[2] && p.Daily.Progress[(int)DailyMission.Box]==0 && (p.Daily.NormalTickets>0 || p.MagicCores>=40)){
                        if(p.Daily.NormalTickets>0)p.Daily.NormalTickets--;else p.MagicCores-=40;
                        DailyRules.Progress(p,DailyMission.Box,1);}
                    Claims(p,row);
                    while(now+240<=end){
                        bool crisis=crisisRemaining>0 || next==CampaignCatalog.Stages.Length;
                        int stage=crisis ? crisisStage : next;
                        Refresh(p,now,localDay,row);
                        bool free=!old && HeartRules.IsFreeLaunch(stage,crisis);
                        if(!free && Total(p)==0){
                            row.Dialogs++;if(CampaignCatalog.Stages[stage].Chapter==1 && !crisis && next<14)row.ChapterOneDialogs++;
                            if(first==""){first="day "+day+" / "+CampaignCatalog.Stages[stage].Chapter+"-"+CampaignCatalog.Stages[stage].LocalNumber+(crisis ? " crisis" : " retry "+retries);firstClears=next;}
                            // An available shop heart is a voluntary core purchase, once/day without refresh ads.
                            DailyShopState.Ensure(p,localDay);
                            if(p.FirstCleared[2] && p.Shop.HeartsBought==0 && p.MagicCores>=20){
                                p.Shop.HeartsBought++;p.Shop.PurchasedMask|=1<<4;p.MagicCores-=20;row.Shop+=Grant(p,1);
                            }else if(policy!="no-ads" && HeartRules.AdRefill(p,now,localDay)){
                                row.Ads++;DailyRules.Progress(p,DailyMission.Ad,1);Claims(p,row);
                            }else{
                                int wait=Math.Min(HeartRules.NextRegenSeconds(p,now,localDay),(int)(end-now));
                                row.WaitMinutes+=wait/60d;now+=wait;Refresh(p,now,localDay,row);continue;
                            }
                        }
                        if(free)row.FreeLaunches++;else {Require(HeartRules.Spend(p,now,localDay),"launch after refill");row.Used++;}
                        row.Attempts++;row.PlayMinutes+=4;if(crisis)row.CrisisAttempts++;
                        now+=240;Refresh(p,now,localDay,row);
                        bool won=combat.Next(100)<WinChance(stage,crisis,retries);
                        if(!won){row.Losses++;retries++;continue;}
                        row.Wins++;retries=0;int star=stars.Next(100)<(crisis ? 40 : 70) ? 3 : 2;
                        p.MagicCores+=p.VictoryReward(stage,crisis);
                        if(!crisis){row.Clears+=Grant(p,old ? OldClear(stage,true,star==3) : HeartRules.ClearReward(stage,true,star==3));
                            p.FirstCleared[stage]=true;p.BestStars[stage]=star;next++;
                            var s=CampaignCatalog.Stages[stage];if(s.LocalNumber==CampaignCatalog.ChapterCount(s.Chapter)){
                                crisisRemaining=6;crisisStage=CampaignCatalog.ChapterStart(s.Chapter);
                                if(s.Chapter==1)row.ChapterOneEndedWith=Total(p);
                            }
                        }else {p.CrisisStars[stage]=Math.Max(p.CrisisStars[stage],star);
                            if(crisisRemaining>0)crisisRemaining--;crisisStage=(crisisStage+1)%p.FirstCleared.Length;}
                        DailyRules.Progress(p,DailyMission.Wins,1);if(star==3)DailyRules.Progress(p,DailyMission.ThreeStars,1);
                        if(stage>=4)DailyRules.Progress(p,DailyMission.Fusions,1);if(crisis)DailyRules.Progress(p,DailyMission.Crisis,1);
                        DailyRules.Points(p,DailyCatalog.VictoryPoints);Claims(p,row);
                    }
                    Refresh(p,end,localDay,row);
                }
                row.FirstShortage=first;row.FirstShortageClears=firstClears;row.End=Total(p);
                int at=Math.Min(next,CampaignCatalog.Stages.Length-1);row.EndOperation=CampaignCatalog.Stages[at].Chapter+"-"+CampaignCatalog.Stages[at].LocalNumber;
                Require(row.Start+row.Initial+row.Backfill+row.Regen+row.Clears+row.Attendance+row.Missions+row.Roulette+row.Pass+row.Shop+row.Ads+row.Crystals-row.Used==row.End,"daily heart conservation");
                Require(row.Ads<=10 && row.End<=1004 && row.PlayMinutes+row.WaitMinutes<=pattern.Minutes*pattern.Starts.Length,"quota/time bound");
                rows.Add(row);
            }return rows.ToArray();
        }
        public static Row[] Review(int days,int[] seeds)
        {
            if(days<1 || days>56 || seeds==null || seeds.Length==0)throw new ArgumentOutOfRangeException("review");
            var rows=new List<Row>();foreach(bool old in new[]{true,false})foreach(bool updated in new[]{false,true})
                foreach(var pattern in patterns)foreach(string policy in new[]{"ads","no-ads"})foreach(int seed in seeds)
                    rows.AddRange(Simulate(old,updated,pattern,policy,seed,days));return rows.ToArray();
        }
        public static string Summary(Row[] rows)
        {
            var lines=new List<string>{"economy/player/pattern/policy | first shortage by seed (input order) | max dialogs/day | max wait min/day | used | clear | backfill"};
            foreach(var group in rows.GroupBy(r=>r.Economy+"/"+r.Player+"/"+r.Pattern+"/"+r.Policy)){
                var first=group.GroupBy(r=>r.Seed).Select(g=>g.FirstOrDefault(r=>r.FirstShortage!="")).Select(r=>r==null ? "none" : r.FirstShortage);
                lines.Add(group.Key+" | "+string.Join("; ",first)+" | "+group.Max(r=>r.Dialogs)+" | "+group.Max(r=>r.WaitMinutes).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+
                    " | "+group.Sum(r=>r.Used)+" | "+group.Sum(r=>r.Clears)+" | "+group.Max(r=>r.Backfill));
            }return string.Join("\n",lines);
        }
        public static string Run()
        {
            var rows=Review(28,new[]{7,19,41});
            var repeated=Review(28,new[]{7,19,41});
            Require(rows.Length==repeated.Length && rows.Zip(repeated,(a,b)=>typeof(Row).GetFields().All(f=>object.Equals(f.GetValue(a),f.GetValue(b)))).All(x=>x),"repeatable daily rows and fixed integer streams");
            Require(rows.Where(r=>r.Economy=="after" && r.Player=="new").All(r=>r.ChapterOneDialogs==0),"chapter one protection");
            var median=rows.Where(r=>r.Economy=="after" && r.Player=="new" && r.Pattern=="3x30" && r.Policy=="no-ads")
                .GroupBy(r=>r.Seed).Select(g=>g.First(r=>r.FirstShortage!="").FirstShortageClears).OrderBy(n=>n).ToArray();
            // Owner 2026-11-17 retains the chapter-one completion +10 and free tutorial.
            // The former late-chapter-two shortage target is superseded: the medium model now
            // clears normal operations before its first shortage, during optional crisis play.
            Require(median[1]==CampaignCatalog.Stages.Length,"owner buffer protects medium normal progression");
            foreach(var group in rows.Where(r=>r.Economy=="after" && r.Player=="new").GroupBy(r=>r.Pattern+"/"+r.Policy+"/"+r.Seed))
                Require(group.Sum(r=>r.Clears)==24 && group.Sum(r=>r.FreeLaunches)==3,"owner first-clear total and free tutorial");
            Require(rows.Where(r=>r.Economy=="after" && r.Pattern=="4x30-heavy").All(r=>r.Dialogs<=4 && r.WaitMinutes<=30),"split heavy day limits");
            Require(rows.Where(r=>r.Economy=="after" && r.Player=="updated").All(r=>r.Backfill<=HeartRules.BackfillLimit),"bounded legacy grant");
            return "PASS deterministic 28-day economy, source conservation, clock/quotas, chapter one protection, owner +10 buffer/24 clear hearts/free tutorial and medium normal progression, split heavy-day limits and capped backfill";
        }
    }
}
