using System;
using System.Collections.Generic;

namespace Gatefall.Core
{
    [Serializable] public sealed class GameBalance
    {
        public float Duration = 180, PlayerSpeed = 5.2f, PlayerHealth = 100, CoreHealth = 300;
        public float PlayerRange = 5.6f, PlayerDamage = 12, PlayerFireInterval = .36f;
        public int StartingCredits = 140;
        public bool SpawnEnemies = true, OfferCards = true;
    }
    public struct V2
    {
        public float X, Y;
        public V2(float x, float y) { X = x; Y = y; }
        public float Length { get { return (float)Math.Sqrt(X * X + Y * Y); } }
        public V2 Normalized { get { return Length > .0001f ? this / Length : new V2(); } }
        public static V2 operator +(V2 a, V2 b) { return new V2(a.X + b.X, a.Y + b.Y); }
        public static V2 operator -(V2 a, V2 b) { return new V2(a.X - b.X, a.Y - b.Y); }
        public static V2 operator *(V2 a, float f) { return new V2(a.X * f, a.Y * f); }
        public static V2 operator /(V2 a, float f) { return new V2(a.X / f, a.Y / f); }
        public static float Distance(V2 a, V2 b) { return (a - b).Length; }
        public static float DistanceSquared(V2 a,V2 b) { float x=a.X-b.X,y=a.Y-b.Y;return x*x+y*y; }
    }
    public enum RunState { Ready, Running, Paused, Won, Lost }
    public enum EnemyKind { Grunt, Runner, Brute, Siege, Boss }
    public enum EventKind { HunterShot, TowerShot, Hit, Kill, HunterHurt, CoreHurt, Build, Supply, Wave, End, Upgrade, Fusion, Sale, Warning, Blast, Shield, HunterDown, HunterRevived, WardBurst, MarkFollowUp }
    public struct CombatEvent
    {
        public EventKind Kind; public V2 Position, Target; public float Value;
        // Hit/shot: TowerKind source (-1 hunter/none, -2 boss). Kill: EnemyKind. Campaign CoreHurt: entrance lane.
        public int Source;
        // Scheduled entrance wave, or the wave whose supply was prepaid; -1 otherwise.
        public int Wave;
        // Kill: identity of the defeated enemy for continuity with its last rendered sprite.
        public int EnemyId;
        public bool IsEntranceWarning { get { return Kind == EventKind.Warning && Wave >= 0 && Value >= 0; } }
        public bool IsBossWarning { get { return Kind == EventKind.Warning && Wave < 0 && Value == 3; } }
        public CombatEvent(EventKind kind, V2 position, V2 target, float value, int source = -1, int wave = -1, int enemyId = 0)
        { Kind = kind; Position = position; Target = target; Value = value; Source = source; Wave = wave; EnemyId = enemyId; }
    }
    public sealed class SlowEffect { public int Source; public float Fraction, Remaining; }
    public sealed partial class Enemy
    {
        public int Id, Lane, Waypoint = 1;
        public EnemyKind Kind; public V2 Position;
        public float Health, MaxHealth, Speed, AttackCooldown, HitFlash, ReengageDelay;
        public float MarkRemaining, MarkBonus;
        public bool Engaged;
        public bool Swarm;
        public float LaneOffset, IncomingDamage;
        internal int ClusterScore;
        public readonly List<SlowEffect> Slows = new List<SlowEffect>();
        public float SlowFraction { get { float best = 0; foreach (SlowEffect s in Slows) if (s.Remaining > 0) best = Math.Max(best,s.Fraction);
            return best * (Kind == EnemyKind.Siege ? .4f : Kind == EnemyKind.Brute ? .65f : 1); } }
    }
    public sealed partial class Tower
    {
        public int Id, Socket, Level = 1, PaidCredits, InvestedCores;
        public TowerKind Kind; public V2 Position, Aim;
        public float Cooldown, Commission; public string Lineage;
    }
    public sealed class Projectile
    {
        public int Id, TargetId, SourceId; public V2 Position, ImpactPosition;
        public float Damage, Speed, Life = 3, ReservedDamage;
        public bool FromTower, FixedImpact; public TowerKind Kind;
        // Launch snapshot: no reference to a sold/consumed tower on impact.
        public TowerStats Stats;
    }
    public sealed class SpawnGroup
    { public int Wave, Lane, Count, Spawned; public EnemyKind Kind; public double Start, Interval; public bool Warned, Swarm; }
    public sealed class WaveBriefing
    {
        public readonly int Wave, Total;
        public readonly double FirstSpawn, LastSpawn;
        readonly int[] lanes, kinds;
        internal WaveBriefing(int wave,int total,double first,double last,int[] laneCounts,int[] kindCounts)
        { Wave=wave;Total=total;FirstSpawn=first;LastSpawn=last;lanes=laneCounts;kinds=kindCounts; }
        public int LaneCount(int lane) {return lane>=0 && lane<lanes.Length ? lanes[lane] : 0;}
        public int KindCount(EnemyKind kind) {int i=(int)kind;return i>=0 && i<kinds.Length ? kinds[i] : 0;}
        // Priority describes an important counter, rather than the most numerous enemy.
        public EnemyKind Threat {get {for(int i=kinds.Length-1;i>=0;i--)if(kinds[i]>0)return (EnemyKind)i;return EnemyKind.Grunt;}}
        public string Summary {
            get {
                string[] directions={"서","북","동","남"}, threats={"일반","빠른 적","정예","공성","문지기"};
                var parts=new List<string>();for(int i=0;i<lanes.Length;i++)if(lanes[i]>0)parts.Add(directions[i]+lanes[i]);
                return "웨이브 "+(Wave+1)+" · "+string.Join("·",parts)+" · "+threats[(int)Threat];
            }
        }
    }
    public sealed class RunTelemetry
    {
        public int CreditsSpent, Refunds, CoresGranted, Fusions, Spawned;
        public int WaveCalls, CardOffers;
        public double WaveAdvanceSeconds;
        public readonly List<string> WaveCallLog = new List<string>();
        public int MarkedHits;
        public float MarkBonusDamage;
        public double FirstFusion = -1;
        public readonly int[] SpawnedKinds = new int[5];
        public readonly float[] TowerDamage = new float[TowerCatalog.All.Length];
        public readonly float[] CoreDamageByLane = new float[4];
        public readonly List<string> Purchases = new List<string>();

        public int MostDamagingLane
        {
            get {
                int lane = -1;
                float damage = 0;
                for (int i = 0; i < CoreDamageByLane.Length; i++) {
                    if (CoreDamageByLane[i] <= damage) continue;
                    lane = i;
                    damage = CoreDamageByLane[i];
                }
                return lane;
            }
        }
    }
    // Unity-free model: validate every command before modifying economy or board.
    public sealed partial class DefenseSimulation
    {
        public const float ArenaHalfWidth = 18, ArenaHalfHeight = 10, BuildDistance = 2.15f;
        public const float HunterRespawnDelay = 10f;
        public static readonly V2[] Sockets = { new V2(-11,2.5f), new V2(-6,5.4f), new V2(-4,.5f), new V2(4,1),
            new V2(0,-3.9f), new V2(7,-5.5f), new V2(11,-2.5f), new V2(-4,-4.5f) };
        public static readonly V2[] Gates = { new V2(-17,0), new V2(0,9), new V2(17,0) };
        public static readonly V2[][] Paths = {
            new[]{Gates[0],new V2(-11,0),new V2(-7,3),new V2(-3,3),new V2()},
            new[]{Gates[1],new V2(-7,7),new V2(-7,3),new V2(-3,3),new V2()},
            new[]{Gates[2],new V2(11,0),new V2(7,-3),new V2(3,-3),new V2()}
        };
        public readonly GameBalance Balance;
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Tower> Towers = new List<Tower>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<CombatEvent> Events = new List<CombatEvent>();
        public readonly List<SpawnGroup> Schedule = new List<SpawnGroup>();
        readonly List<Enemy> targetScratch = new List<Enemy>(256);
        int swarmCreditRemainder;
        public readonly List<CardKind> CardOffers = new List<CardKind>();
        public readonly int[] Cards = new int[6];
        public readonly RunTelemetry Telemetry = new RunTelemetry();
        public RunState State { get; private set; }
        public V2 PlayerPosition { get; private set; }
        public V2 PlayerAim { get; private set; }
        public float PlayerHealth { get; private set; }
        public float CoreHealth { get; private set; }
        public double Elapsed { get; private set; }
        public int Credits { get; private set; }
        public int Cores { get; private set; }
        public int Kills { get; private set; }
        public int Wave { get; private set; }
        public int ActiveGate { get { if(Rules!=null && Rules.Stage!=null)return Rules.Stage.Lanes[Wave%Rules.Stage.Lanes.Length]; return Wave == 0 ? 0 : Wave == 2 ? 1 : 2; } }
        public bool AwaitingCard { get { return CardOffers.Count > 0; } }
        public bool WaveClearVictory { get { return Rules!=null && Rules.Stage!=null; } }
        public int WaveCount { get { int count=1;foreach(SpawnGroup g in Schedule)count=Math.Max(count,g.Wave+1);return count; } }
        public bool AllWavesSpawned { get { foreach(SpawnGroup g in Schedule)if(g.Spawned<g.Count)return false;return true; } }
        // Opt-in: mobile campaigns enable this only for normal operations from 1-4.
        public bool WaveCallsEnabled { get; set; }
        public const double MaxWaveCallAdvance = 6;
        public int SuppliesGranted { get { return supplyIndex; } }
        public int NextCallWave { get { return Wave+1; } }
        public double CallAdvanceSeconds { get {WaveBriefing next=GetWaveBriefing(NextCallWave);return next==null?0:Math.Max(0,Math.Min(MaxWaveCallAdvance,next.FirstSpawn-Elapsed-3));} }
        public float WaveProgressFraction(int wave)
        {
            if(wave<0 || wave>=6)return 0;
            if(WaveClearVictory && AllWavesSpawned)return 1;
            double start=wave==0?0:supplyDue[wave-1];
            double end=wave<5?supplyDue[wave]:Math.Max(start+1,Balance.Duration-Telemetry.WaveAdvanceSeconds);
            return (float)Math.Max(0,Math.Min(1,(Elapsed-start)/Math.Max(.00001,end-start)));
        }
        public string CanCallNextWave()
        {
            if(!WaveCallsEnabled || !WaveClearVictory || Rules.Stage.Index<3)return "이 작전은 호출을 지원하지 않습니다.";
            if(State!=RunState.Running || AwaitingCard)return "전투 중 카드 선택을 마친 뒤 가능합니다.";
            if(Wave>=WaveCount-1 || AllWavesSpawned)return "다음 웨이브가 없습니다.";
            WaveBriefing current=GetWaveBriefing(Wave),next=GetWaveBriefing(NextCallWave);
            if(current==null || next==null)return "다음 출현 정보를 확인할 수 없습니다.";
            if(Elapsed<current.FirstSpawn+10)return "현재 웨이브를 먼저 방어하세요.";
            double advance=next.FirstSpawn-Elapsed-3;
            return advance<=.00001 || advance>10.00001 ? "다음 출현 3~13초 전에 호출할 수 있습니다." : "";
        }
        public bool TryCallNextWave(int expectedWave,string command,out string reason)
        {
            if(!Command(command,out reason))return false;
            if(expectedWave!=NextCallWave) {reason="웨이브 정보가 바뀌었습니다.";return false;}
            reason=CanCallNextWave();if(reason!="")return false;
            int next=NextCallWave;double advance=Math.Min(MaxWaveCallAdvance,GetWaveBriefing(next).FirstSpawn-Elapsed-3);
            // Never compress the current wave's remaining spawns or skip actor timers.
            foreach(SpawnGroup group in Schedule)if(group.Wave>=next)group.Start-=advance;
            for(int i=next-1;i<supplyDue.Length;i++)supplyDue[i]-=advance;
            supplyDue[next-1]=Elapsed;
            if(command!=null)commands.Add(command);
            Telemetry.WaveCalls++;Telemetry.WaveAdvanceSeconds+=advance;
            Telemetry.WaveCallLog.Add(Elapsed.ToString("F2")+" wave="+(next+1)+" advance="+advance.ToString("F2"));
            if(GrantDueSupplies(true))OfferCard();
            return true;
        }
        public WaveBriefing GetWaveBriefing(int wave)
        {
            if(wave<0)return null;
            int total=0;int[] lanes=new int[GatePositions.Length],kinds=new int[5];
            double first=double.PositiveInfinity,last=double.NegativeInfinity;
            foreach(SpawnGroup g in Schedule) {
                if(g.Wave!=wave || g.Count<=0)continue;
                total+=g.Count;
                if(g.Lane>=0 && g.Lane<lanes.Length)lanes[g.Lane]+=g.Count;
                int kind=(int)g.Kind;if(kind>=0 && kind<kinds.Length)kinds[kind]+=g.Count;
                first=Math.Min(first,g.Start);last=Math.Max(last,g.Start+(g.Count-1)*g.Interval);
            }
            return total==0 ? null : new WaveBriefing(wave,total,first,last,lanes,kinds);
        }
        public string DefeatReason { get; private set; }
        public float HunterInvulnerability { get; private set; }
        public float HunterRespawnRemaining { get; private set; }
        public bool HunterDown { get { return PlayerHealth <= 0; } }
        public float Remaining { get { return Math.Max(0,Balance.Duration-(float)Elapsed); } }
        private readonly Random random;
        private readonly HashSet<string> commands = new HashSet<string>();
        private int nextId, supplyIndex; private float fireCooldown;
        private readonly double[] supplyDue = {30,60,90,120,150};
        public DefenseSimulation(GameBalance balance, int seed, CampaignBattle rules = null)
        {
            if (balance == null) throw new ArgumentNullException("balance");
            if (balance.Duration <= 0 || balance.PlayerHealth <= 0 || balance.CoreHealth <= 0 || balance.PlayerFireInterval <= 0)
                throw new ArgumentException("Duration, health and attack interval must be positive.");
            Rules=rules==null ? null : rules.Copy(); Balance = ConfigureBalance(balance); balance=Balance; random = new Random(seed);
            PlayerPosition = new V2(0,-2.2f); PlayerAim = new V2(0,1);
            PlayerHealth = balance.PlayerHealth; CoreHealth = balance.CoreHealth; MinimumCoreHealth=CoreHealth;
            Credits = balance.StartingCredits; State = RunState.Ready; DefeatReason = "";
            // Telegraph every group three seconds ahead. Scheduled spawns end at 168.3s.
            Group(0,0,EnemyKind.Grunt,14,5,1.6);
            Group(1,0,EnemyKind.Grunt,14,33,1.5); Group(1,2,EnemyKind.Runner,8,43,1.4);
            Group(2,1,EnemyKind.Grunt,18,63,1.15); Group(2,0,EnemyKind.Brute,3,64,6);
            Group(3,0,EnemyKind.Grunt,16,93,1.3); Group(3,2,EnemyKind.Runner,10,94,1.7); Group(3,1,EnemyKind.Brute,2,100,8);
            Group(4,0,EnemyKind.Grunt,14,123,1.4); Group(4,2,EnemyKind.Runner,6,125,2);
            Group(4,1,EnemyKind.Brute,4,124,5); Group(4,2,EnemyKind.Siege,2,123,10);
            Group(5,0,EnemyKind.Grunt,18,153,.9); Group(5,2,EnemyKind.Runner,10,153,1.6);
            Group(5,1,EnemyKind.Brute,4,153,4); Group(5,2,EnemyKind.Siege,4,153,5);
            ConfigureRaon();
        }
        private void Group(int wave, int lane, EnemyKind kind, int count, double start, double interval)
        { Schedule.Add(new SpawnGroup { Wave=wave,Lane=lane,Kind=kind,Count=count,Start=start,Interval=interval }); }
        public void Start() { if (State == RunState.Ready) State = RunState.Running; }
        public void TogglePause() { if (State == RunState.Running) State = RunState.Paused; else if (State == RunState.Paused && !AwaitingCard) State = RunState.Running; }
        public int NearestSocket(bool emptyOnly = false)
        {
            int found = -1; float distance = BuildDistance;
            for (int i=0;i<BoardSockets.Length;i++) { if (emptyOnly && TowerAt(i)!=null) continue;
                float d=V2.Distance(PlayerPosition,BoardSockets[i]); if (d<=distance) { found=i; distance=d; } } return found;
        }
        public int NearestEmptySocket() { return NearestSocket(true); }
        // Called from per-step and per-frame paths: plain loops avoid a capturing predicate allocation per lookup.
        public Tower TowerAt(int socket) { foreach(Tower t in Towers)if(t.Socket==socket)return t; return null; }
        public Tower TowerById(int id) { foreach(Tower t in Towers)if(t.Id==id)return t; return null; }
        public TowerStats Stats(Tower tower) { return Stats(tower,tower.Level); }
        // Preview a level at this socket without changing the tower or spending currency.
        public TowerStats Stats(Tower tower,int level) { TowerStats s=Stats(tower.Kind,level); ApplyAdjacency(tower,s,level); return s; }
        public TowerStats Stats(TowerKind kind, int level)
        {
            TowerDefinition def=TowerCatalog.Get(kind); TowerStats s=RawStats(kind,level);
            if ((def.Tags&TowerTag.Ballistic)!=0) { float scale=1+.15f*Cards[(int)CardKind.Ballistic]; s.Damage*=scale; s.Secondary*=scale;
                if (s.Chain!=null) for (int i=0;i<s.Chain.Length;i++) s.Chain[i]*=scale; }
            if ((def.Tags&TowerTag.Control)!=0) s.SlowSeconds+=.4f*Cards[(int)CardKind.Control];
            if ((def.Tags&TowerTag.Blast)!=0) s.Radius+=.25f*Cards[(int)CardKind.Blast];
            s.Range*=1+.1f*Cards[(int)CardKind.Range]; ApplyResearch(kind,s); return s;
        }
        private string CanAct() { return State!=RunState.Running && State!=RunState.Paused && !(State==RunState.Ready && Rules!=null) ? "전투 중에만 가능합니다." : AwaitingCard ? "성장 카드를 먼저 선택하세요." : ""; }
        // Campaign and lab boards are fully visible on mobile: any socket can be commanded by tapping it.
        // Only the legacy rule-less simulation keeps the walk-up requirement.
        private string NearSocket(int socket) { return socket<0 || socket>=BoardSockets.Length ? "건설 지점을 선택하세요." : Rules==null && V2.Distance(PlayerPosition,BoardSockets[socket])>BuildDistance ? "지점 "+(socket+1)+"에 접근하세요 (2.15 이내)." : ""; }
        private string Funds(int amount) { return Credits<amount ? "마석 "+(amount-Credits)+" 부족" : ""; }
        private bool Command(string id,out string reason) { reason=id!=null && commands.Contains(id) ? "이미 처리한 명령입니다." : ""; return reason==""; }
        private void Paid(int amount,string id,string description) { Credits-=amount; Telemetry.CreditsSpent+=amount; if(id!=null) commands.Add(id); Telemetry.Purchases.Add(Elapsed.ToString("F1")+" "+description); }
        public string CanBuild(int socket,TowerKind kind)
        {
            string reason=CanAct(); if(reason!="")return reason; reason=NearSocket(socket); if(reason!="")return reason;
            if((int)kind<0 || (int)kind>=TowerCatalog.All.Length || TowerCatalog.Get(kind).Tier!=1)return "상위 결계주는 융합으로 만드세요.";
            if(!Allows(kind))return "아직 해금되지 않은 결계주입니다.";
            if(TowerAt(socket)!=null)return "이미 타워가 있는 지점입니다.";
            if(TowerLimit>0 && Towers.Count>=TowerLimit)return "결계주는 최대 "+TowerLimit+"기입니다. 조합하거나 판매해 자리를 만드세요.";
            return Funds(TowerCatalog.Get(kind).BuildCost);
        }
        // Towers allowed at once (0 = every site). Fusion turns two towers into one and sale removes one.
        public int TowerLimit { get { return Rules!=null && Rules.Stage!=null ? Rules.Stage.TowerLimit : 0; } }
        public bool TryBuild(int socket,out string reason) { return TryBuild(socket,TowerKind.Autocannon,null,out reason); }
        public bool TryBuild(int socket,TowerKind kind,string command,out string reason)
        {
            if(!Command(command,out reason))return false; reason=CanBuild(socket,kind); if(reason!="")return false;
            int cost=TowerCatalog.Get(kind).BuildCost;
            Tower tower=new Tower { Id=++nextId,Socket=socket,Kind=kind,Position=BoardSockets[socket],Aim=new V2(0,1),PaidCredits=cost,Lineage=kind.ToString() };
            tower.WardHealth=WardMaximum(tower); Paid(cost,command,"build "+kind+" @"+socket); Towers.Add(tower); Emit(EventKind.Build,tower.Position,new V2(),cost); return true;
        }
        public int TowerLevelCap(TowerKind kind)
        {
            int levels = TowerCatalog.Get(kind).Levels.Length;
            return HasAdvancedTowers ? levels : Math.Min(2, levels);
        }
        public string CanUpgrade(int id)
        {
            string reason=CanAct(); if(reason!="")return reason; Tower t=TowerById(id); if(t==null)return "타워가 없습니다.";
            TowerDefinition definition=TowerCatalog.Get(t.Kind);
            if(t.Level>=definition.Levels.Length)return "최대 강화 단계입니다.";
            if(t.Level>=TowerLevelCap(t.Kind))return "Lv3 강화는 1-11부터 가능합니다.";
            reason=NearSocket(t.Socket); return reason!="" ? reason : Funds(definition.UpgradePrice(t.Level));
        }
        public bool TryUpgrade(int id,string command,out string reason)
        {
            if(!Command(command,out reason))return false; reason=CanUpgrade(id); if(reason!="")return false;
            Tower t=TowerById(id); int cost=TowerCatalog.Get(t.Kind).UpgradePrice(t.Level); float fraction=Math.Max(0,t.Cooldown)/Stats(t).Interval;
            t.Level++; t.Cooldown=fraction*Stats(t).Interval; t.PaidCredits+=cost; if(t.Kind==TowerKind.Barrier)t.WardHealth=WardMaximum(t);
            Paid(cost,command,"upgrade "+t.Kind+" @"+t.Socket); Emit(EventKind.Upgrade,t.Position,new V2(),cost); return true;
        }
        public string CanFuse(int recipeId,int first,int second,int anchor)
        {
            string reason=CanAct(); if(reason!="")return reason;
            if(recipeId<0 || recipeId>=TowerCatalog.Recipes.Length)return "존재하지 않는 조합식입니다.";
            if(!Allows(TowerCatalog.Recipes[recipeId].Result))return "이 융합식은 작전 클리어 후 해금됩니다.";
            Tower a=TowerById(first),b=TowerById(second); if(a==null || b==null)return "재료 타워 두 개를 선택하세요.";
            if(a.Id==b.Id)return "서로 다른 타워가 필요합니다.";
            FusionRecipe r=TowerCatalog.Recipes[recipeId]; if(!r.Matches(a,b))return "재료 종류와 Lv2 이상 조건을 확인하세요.";
            if(anchor!=a.Socket && anchor!=b.Socket)return "재료 타워의 위치를 선택하세요.";
            reason=NearSocket(anchor); if(reason!="")return reason; reason=Funds(r.Credits); if(reason!="")return reason;
            return Cores<1 ? (WaveCallsEnabled ? "공명 촉매 부족 (3·5웨이브 보급)" : "공명 촉매 부족 (60초 / 120초 지급)") : "";
        }
        public bool TryFuse(int recipeId,int first,int second,int anchor,string command,out string reason)
        {
            if(!Command(command,out reason))return false; reason=CanFuse(recipeId,first,second,anchor); if(reason!="")return false;
            Tower a=TowerById(first),b=TowerById(second); FusionRecipe r=TowerCatalog.Recipes[recipeId];
            Tower result=new Tower { Id=++nextId,Socket=anchor,Position=BoardSockets[anchor],Kind=r.Result,Aim=new V2(0,1),
                PaidCredits=a.PaidCredits+b.PaidCredits+r.Credits,InvestedCores=a.InvestedCores+b.InvestedCores+1,
                Commission=1,Cooldown=Stats(r.Result,1).Interval,Lineage="("+a.Lineage+"+"+b.Lineage+")" };
            Paid(r.Credits,command,"fusion "+result.Lineage+" -> "+r.Result+" @"+anchor);
            Cores--; Towers.Remove(a); Towers.Remove(b); Towers.Add(result); Telemetry.Fusions++;
            if(Telemetry.FirstFusion<0)Telemetry.FirstFusion=Elapsed; Emit(EventKind.Fusion,result.Position,new V2(),recipeId); return true;
        }
        // Allocation-free per-frame HUD query: true when the fusion menu would list a recipe for this tower with an empty
        // reason. Like the menu, it validates the first Lv2+ partner; partner-specific rules are only kind and level.
        // Credits and catalysts are checked first because CanFuse formats a new message string when they are short.
        public bool HasReadyFusion(Tower tower)
        {
            if(tower==null || tower.Level<2 || Cores<1)return false;
            foreach(FusionRecipe r in TowerCatalog.Recipes) {
                if((r.First!=tower.Kind && r.Second!=tower.Kind) || Credits<r.Credits)continue;
                TowerKind other=r.First==tower.Kind ? r.Second : r.First;
                if(!Allows(r.Result) || !Allows(other))continue;
                foreach(Tower partner in Towers) {
                    if(partner.Id==tower.Id || partner.Kind!=other || partner.Level<2)continue;
                    if(CanFuse(r.Id,tower.Id,partner.Id,tower.Socket)=="")return true;
                    break;
                }
            }
            return false;
        }
        public int SaleRefund(Tower t) { return State==RunState.Ready && Rules!=null ? t.PaidCredits : t.PaidCredits*3/4; }
        public string CanSell(int id) { string r=CanAct(); Tower t=TowerById(id); return r!="" ? r : t==null ? "타워가 없습니다." : NearSocket(t.Socket); }
        public bool TrySell(int id,string command,out string reason)
        {
            if(!Command(command,out reason))return false; reason=CanSell(id); if(reason!="")return false;
            Tower t=TowerById(id); int refund=SaleRefund(t); Credits+=refund; Cores+=t.InvestedCores; Telemetry.Refunds+=refund;
            if(command!=null)commands.Add(command); Telemetry.Purchases.Add(Elapsed.ToString("F1")+" sell "+t.Kind+" @"+t.Socket);
            Towers.Remove(t); Emit(EventKind.Sale,t.Position,new V2(),refund); return true;
        }
        public bool ChooseCard(CardKind kind)
        {
            if(State!=RunState.Paused || !CardOffers.Contains(kind))return false;
            if(kind==CardKind.Credits)Credits+=35; else { if(Cards[(int)kind]>=2)return false; Cards[(int)kind]++; }
            CardOffers.Clear(); State=RunState.Running; return true;
        }
        private void OfferCard()
        {
            int reachable = Rules == null || Rules.Lab ? -1 : ReachableTowerFamilies();
            TowerTag availableTags = TowerTag.None;
            foreach (TowerDefinition tower in TowerCatalog.All)
                if ((reachable & (1 << (int)tower.Kind)) != 0) availableTags |= tower.Tags;
            List<CardKind> eligible=new List<CardKind>();
            foreach (CardKind kind in Enum.GetValues(typeof(CardKind))) {
                TowerTag tag = TowerCatalog.CardTag(kind);
                if ((kind == CardKind.Credits || Cards[(int)kind] < 2) && (tag == TowerTag.None || (availableTags & tag) != 0))
                    eligible.Add(kind);
            }
            while(CardOffers.Count<3 && eligible.Count>0) { int i=random.Next(eligible.Count); CardOffers.Add(eligible[i]); eligible.RemoveAt(i); }
            Telemetry.CardOffers++;State=RunState.Paused;
        }
        private bool GrantDueSupplies(bool prepaid=false)
        {
            int stage=0;while(stage<supplyDue.Length && Elapsed+.00001>=supplyDue[stage])stage++;
            if(stage!=Wave) {Wave=stage;Emit(EventKind.Wave,GatePositions[ActiveGate],new V2(),Wave+1);}
            bool offer=false;
            while(supplyIndex<5 && Elapsed+.00001>=supplyDue[supplyIndex] && (supplyIndex+1)*30<Balance.Duration) {
                supplyIndex++;int supply=WaveSupplyCredits;Credits+=supply;
                if((supplyIndex==2 || supplyIndex==4) && (Rules==null || Rules.Lab || Rules.Stage.HasCatalysts)) {Cores++;Telemetry.CoresGranted++;}
                offer|=Balance.OfferCards && supplyIndex%2==1;
                Emit(EventKind.Supply,PlayerPosition,new V2(),supply,wave:prepaid?supplyIndex:-1);
            }
            return offer;
        }
        public int WaveSupplyCredits { get { return Rules!=null && Rules.Stage!=null ? Rules.Stage.SupplyCredits : 40; } }
        public void Tick(float deltaTime,V2 movement)
        {
            if(State==RunState.Ready && Rules!=null) { if(deltaTime>0 && !float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime))MovePlayer(Math.Min(deltaTime,.1f),movement); return; }
            if(State!=RunState.Running || deltaTime<=0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))return;
            if(float.IsNaN(movement.X) || float.IsNaN(movement.Y) || float.IsInfinity(movement.X) || float.IsInfinity(movement.Y))movement=new V2();
            if(movement.Length>1)movement=movement.Normalized;
            double left=deltaTime;
            while(left>.0000001 && State==RunState.Running) { float dt=(float)Math.Min(left,1.0/60);
                if(!WaveClearVictory)dt=Math.Min(dt,Remaining);
                if(dt<=0) { Finish(true); break; } Step(dt,movement); left-=dt; }
        }
        private void Step(float dt,V2 movement)
        {
            TickRaonTimers(dt);
            if(HunterDown) {
                HunterRespawnRemaining=Math.Max(0,HunterRespawnRemaining-dt);
                if(HunterRespawnRemaining<=0) {
                    PlayerPosition=new V2(0,-2.2f);PlayerHealth=Balance.PlayerHealth;HunterInvulnerability=2;
                    Emit(EventKind.HunterRevived,PlayerPosition,new V2(),0);
                }
            }
            foreach(Enemy e in Enemies) { e.MarkRemaining=Math.Max(0,e.MarkRemaining-dt); if(e.MarkRemaining<=0)e.MarkBonus=0; }
            Elapsed=WaveClearVictory ? Elapsed+dt : Math.Min(Balance.Duration,Elapsed+dt); HunterInvulnerability=Math.Max(0,HunterInvulnerability-dt);
            MovePlayer(dt,movement);
            bool offer=GrantDueSupplies();
            if(Balance.SpawnEnemies)foreach(SpawnGroup group in Schedule) {
                if(!group.Warned && Elapsed>=group.Start-3) { group.Warned=true; Emit(EventKind.Warning,GatePositions[group.Lane],new V2(),group.Lane,wave:group.Wave); }
                while(group.Spawned<group.Count && Elapsed>=group.Start+group.Spawned*group.Interval) { Spawn(group); group.Spawned++; }
            }
            fireCooldown-=dt;
            if(!HunterDown && fireCooldown<=0) { List<Enemy> targets=Targets(PlayerPosition,Balance.PlayerRange,true);
                if(targets.Count>0) { Enemy e=targets[0]; PlayerAim=(e.Position-PlayerPosition).Normalized; Shoot(PlayerPosition,e,null,null);
                    fireCooldown=Balance.PlayerFireInterval*(1-.1f*Cards[(int)CardKind.Hunter]); } }
            foreach(Tower tower in Towers) {
                if(tower.Kind==TowerKind.Barrier) { UpdateWard(tower,dt); continue; }
                if(tower.Commission>0) { tower.Commission=Math.Max(0,tower.Commission-dt); continue; }
                tower.Cooldown-=dt; if(tower.Cooldown>0)continue;
                TowerStats s=Stats(tower); List<Enemy> targets=Targets(tower.Position,s.Range,true); if(targets.Count==0)continue;
                if(tower.Kind==TowerKind.Binding)targets.Sort((a,b)=> { int d=a.SlowFraction.CompareTo(b.SlowFraction); return d!=0 ? d : Lead(a,b); });
                if(tower.Kind==TowerKind.Rupture) {foreach(Enemy candidate in targets)candidate.ClusterScore=Cluster(candidate,s.Radius);targets.Sort((a,b)=> { int d=b.ClusterScore.CompareTo(a.ClusterScore);return d!=0 ? d : Lead(a,b); });}
                // Prefer the lead target another active attack tower can reach; otherwise keep normal lead targeting.
                if(tower.Kind==TowerKind.Marker) {
                    Enemy supported=targets.Find(e=>Towers.Exists(t=>t.Id!=tower.Id && t.Kind!=TowerKind.Marker && t.Kind!=TowerKind.Beacon && t.Kind!=TowerKind.Barrier &&
                        t.Commission<=0 && V2.Distance(t.Position,e.Position)<=Stats(t).Range));
                    if(supported!=null) {targets.Remove(supported);targets.Insert(0,supported);}
                }
                if(tower.Kind==TowerKind.Observer)targets.Sort((a,b)=> { int d=(b.MarkRemaining>0).CompareTo(a.MarkRemaining>0); return d!=0 ? d : Lead(a,b); });
                tower.Aim=(targets[0].Position-tower.Position).Normalized;
                int n=tower.Kind==TowerKind.Binding || tower.Kind==TowerKind.Beacon ? Math.Min(s.MaxTargets,targets.Count) : 1;
                for(int i=0;i<n;i++)Shoot(tower.Position,targets[i],tower,s); tower.Cooldown=s.Interval;
            }
            UpdateProjectiles(dt); UpdateEnemies(dt); MinimumCoreHealth=Math.Min(MinimumCoreHealth,CoreHealth);
            if(CoreHealth<=0)Finish(false);
            else if(WaveClearVictory) {
                if(AllWavesSpawned && Enemies.Count==0 && (!Rules.Stage.Boss || BossDefeated))Finish(true);
                else if(offer)OfferCard();
            }
            else if(Elapsed>=Balance.Duration-.00001) { Elapsed=Balance.Duration; Finish(true); }
            else if(offer)OfferCard();
        }
        private void Spawn(SpawnGroup g)
        {
            float hp=g.Kind==EnemyKind.Siege ? 400 : g.Kind==EnemyKind.Brute ? 220 : g.Kind==EnemyKind.Runner ? 32 : 60;
            float speed=g.Kind==EnemyKind.Siege ? .8f : g.Kind==EnemyKind.Brute ? .85f : g.Kind==EnemyKind.Runner ? 2 : 1.15f;
            if(Rules!=null && Rules.Stage!=null) {
                hp*=.9f+.05f*Rules.Stage.Index; if(Rules.Crisis)hp*=1.25f;
                if(g.Kind==EnemyKind.Grunt) {hp*=.12f;speed*=1.35f;}
                else if(g.Kind==EnemyKind.Runner) {hp*=.75f;speed*=1.2f;}
                else if(g.Kind==EnemyKind.Brute)speed*=1.15f;
                else if(g.Kind==EnemyKind.Siege)speed*=1.1f;
                    if(Rules.Stage.Index==8 && !Rules.Crisis && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=1.25f;
                    if(Rules.Stage.Index==4 && !Rules.Crisis && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=1.15f;
                // Operation data: both are 1 before the first seal. The boss's health is set below regardless.
                hp*=g.Swarm ? Rules.Stage.SwarmHealth : Rules.Stage.Threat;
            }
            if(Rules!=null && Rules.Stage!=null && !Rules.Crisis && (Rules.Stage.Index==10 || Rules.Stage.Index==11) && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege)){speed*=1.6f;hp*=Rules.Stage.Index==10 ? 1.35f : 1.6f;}
            if(g.Kind==EnemyKind.Boss) { hp=Rules!=null && Rules.Crisis ? 1100 : 800; speed=.65f; }
            Enemy enemy=new Enemy { Id=++nextId,Kind=g.Kind,Lane=g.Lane,Position=GatePositions[g.Lane],Health=hp,MaxHealth=hp,Speed=speed,Swarm=g.Swarm };
            if(g.Swarm) {
                enemy.LaneOffset=(g.Spawned%3-1)*.32f;
                V2 direction=(Roads[g.Lane][1]-Roads[g.Lane][0]).Normalized;
                enemy.Position+=new V2(-direction.Y,direction.X)*enemy.LaneOffset;
            }
            if(g.Kind==EnemyKind.Boss) {
                int next; V2[] road=Roads[g.Lane];
                enemy.Position=BattlefieldLayouts.Along(road,Math.Max(0,BattlefieldLayouts.RoadLength(road)-6),out next);
                enemy.Waypoint=next; BossSpawned=true;
            }
            Enemies.Add(enemy);
            Telemetry.Spawned++; Telemetry.SpawnedKinds[(int)g.Kind]++;
        }
        private static int Lead(Enemy a,Enemy b) { int d=a.Position.Length.CompareTo(b.Position.Length); return d!=0 ? d : a.Id.CompareTo(b.Id); }
        // Consumed synchronously; impacts do not nest a target query inside the radius loop.
        private List<Enemy> Targets(V2 p,float range,bool availableOnly=false) {
            targetScratch.Clear();float r2=range*range;
            foreach(Enemy enemy in Enemies)if(enemy.Health>0 && (!availableOnly || enemy.IncomingDamage<enemy.Health) && V2.DistanceSquared(p,enemy.Position)<=r2)targetScratch.Add(enemy);
            targetScratch.Sort(Lead);return targetScratch;
        }
        private int Cluster(Enemy t,float radius) { int count=0;float r2=radius*radius;foreach(Enemy e in Enemies)if(e.Health>0 && V2.DistanceSquared(t.Position,e.Position)<=r2)count++;return count; }
        public Enemy LivingEnemy(int id) {foreach(Enemy e in Enemies)if(e.Id==id && e.Health>0)return e;return null;}
        private void Shoot(V2 origin,Enemy target,Tower tower,TowerStats stats)
        {
            float reservation=Rules!=null && Rules.Stage!=null && (stats==null || stats.Radius<=0 && stats.Chain==null) ? (tower==null ? Balance.PlayerDamage : stats.Damage) : 0;
            target.IncomingDamage+=reservation;
            Projectiles.Add(new Projectile { Id=++nextId,Position=origin,TargetId=target.Id,ImpactPosition=target.Position,
                SourceId=tower==null ? 0 : tower.Id,Kind=tower==null ? TowerKind.Autocannon : tower.Kind,
                Damage=tower==null ? Balance.PlayerDamage : stats.Damage,Speed=Rules!=null && Rules.Stage!=null ? (tower==null ? 30 : 28) : (tower==null ? 21 : 18),
                FromTower=tower!=null,Stats=stats==null ? null : stats.Copy(),FixedImpact=tower!=null && tower.Kind==TowerKind.Rupture,ReservedDamage=reservation });
            Emit(tower==null ? EventKind.HunterShot : EventKind.TowerShot,origin,target.Position,tower==null ? Balance.PlayerDamage : stats.Damage,tower==null ? -1 : (int)tower.Kind);
        }
        private void UpdateProjectiles(float dt)
        {
            for(int i=Projectiles.Count-1;i>=0;i--) {
                Projectile p=Projectiles[i]; p.Life-=dt; Enemy target=LivingEnemy(p.TargetId);
                if((!p.FixedImpact && target==null) || p.Life<=0) {if(target!=null)target.IncomingDamage=Math.Max(0,target.IncomingDamage-p.ReservedDamage);Projectiles.RemoveAt(i);continue;}
                V2 end=p.FixedImpact ? p.ImpactPosition : target.Position,diff=end-p.Position;
                if(diff.Length<=p.Speed*dt+.15f) {if(target!=null)target.IncomingDamage=Math.Max(0,target.IncomingDamage-p.ReservedDamage);Impact(p,target,end);Projectiles.RemoveAt(i);} else p.Position+=diff.Normalized*(p.Speed*dt);
            }
        }
        private void Impact(Projectile p,Enemy primary,V2 position)
        {
            TowerStats s=p.Stats; if(s==null) { Damage(primary,p.Damage,p,p.Position); return; }
            if(s.Chain!=null) {
                HashSet<int> hit=new HashSet<int>(); Enemy e=primary; V2 from=p.Position;
                for(int i=0;i<s.Chain.Length && e!=null;i++) { hit.Add(e.Id); V2 last=e.Position; Damage(e,s.Chain[i],p,from); from=last;
                    List<Enemy> options=Targets(last,s.ChainRange); e=options.Find(x=>!hit.Contains(x.Id)); } return;
            }
            if(s.Radius>0) {
                Emit(EventKind.Blast,position,new V2(),s.Radius,(int)p.Kind);
                List<Enemy> targets=Targets(position,s.Radius); int count=0;
                if(!p.FixedImpact && primary!=null) { Damage(primary,s.Damage,p,p.Position); targets.Remove(primary); count++; }
                foreach(Enemy e in targets) { if(count>=s.MaxTargets)break; Damage(e,s.Secondary>0 ? s.Secondary : s.Damage,p,position); count++; }
            } else Damage(primary,s.Damage,p,p.Position);
        }
        private void Damage(Enemy e,float damage,Projectile p,V2 from)
        {
            if(e==null || e.Health<=0)return;
            if(p.FromTower && p.Kind!=TowerKind.Marker && p.Kind!=TowerKind.Beacon && e.MarkRemaining>0) {
                // Record only bonus damage that can actually reduce the target's health.
                Telemetry.MarkedHits++;
                float bonus = Math.Min(e.MarkBonus, Math.Max(0, e.Health - damage));
                Telemetry.MarkBonusDamage += bonus;
                if (bonus > 0) Emit(EventKind.MarkFollowUp, e.Position, from, bonus, (int)p.Kind);
                damage+=e.MarkBonus;
            }
            if(p.FromTower && p.Stats!=null && p.Stats.MarkBonus>0) {
                e.MarkRemaining=3; e.MarkBonus=Math.Max(e.MarkBonus,p.Stats.MarkBonus);
            }
            if(p.FromTower)Telemetry.TowerDamage[(int)p.Kind]+=Math.Min(e.Health,damage);
            e.Health-=damage; e.HitFlash=.065f; Emit(EventKind.Hit,e.Position,from,damage,p.FromTower ? (int)p.Kind : -1);
            if(e.Health<=0) { if(e.Kind==EnemyKind.Boss)BossDefeated=true; Kills++; int reward=e.Kind==EnemyKind.Siege ? 5 : e.Kind==EnemyKind.Brute ? 3 : 1;
                if(e.Swarm) {swarmCreditRemainder++;reward=swarmCreditRemainder/4;swarmCreditRemainder%=4;}
                Credits+=reward; Emit(EventKind.Kill,e.Position,from,reward,(int)e.Kind,enemyId:e.Id); return; }
            if (p.Stats != null && p.Stats.Slow > 0)
                ApplySlow(e, p.SourceId, p.Stats.Slow, p.Stats.SlowSeconds);
        }
        private static void ApplySlow(Enemy enemy, int source, float fraction, float duration)
        {
            SlowEffect effect = enemy.Slows.Find(x => x.Source == source);
            if (effect == null) { effect = new SlowEffect { Source = source }; enemy.Slows.Add(effect); }
            effect.Fraction = fraction;
            effect.Remaining = duration;
        }
        private void UpdateEnemies(float dt)
        {
            if(Rules!=null) { UpdateRaonEnemies(dt); return; }
            int engaged=0;
            foreach(Enemy e in Enemies) {
                if(e.Health<=0) { e.Engaged=false; continue; } e.ReengageDelay=Math.Max(0,e.ReengageDelay-dt);
                if(e.Engaged && (HunterDown || e.Kind==EnemyKind.Siege || V2.Distance(e.Position,PlayerPosition)>3)) { e.Engaged=false; e.ReengageDelay=1; }
                if(e.Engaged)engaged++;
            }
            foreach(Enemy e in Enemies) {
                if(e.Health<=0)continue; e.HitFlash=Math.Max(0,e.HitFlash-dt); e.AttackCooldown-=dt;
                if(!HunterDown && !e.Engaged && engaged<2 && e.Kind!=EnemyKind.Siege && e.ReengageDelay<=0 && V2.Distance(e.Position,PlayerPosition)<2.5f) { e.Engaged=true; engaged++; }
                float slow=e.SlowFraction; foreach(SlowEffect s in e.Slows)s.Remaining-=dt; e.Slows.RemoveAll(s=>s.Remaining<=0);
                V2[] path=Roads[Math.Max(0,Math.Min(Roads.Length-1,e.Lane))]; e.Waypoint=Math.Max(1,Math.Min(e.Waypoint,path.Length-1));
                V2 target=e.Engaged ? PlayerPosition : path[e.Waypoint]; bool atCoreLeg=!e.Engaged && e.Waypoint==path.Length-1;
                float stop=e.Engaged ? .64f : atCoreLeg ? 1.12f : 0; V2 diff=target-e.Position;
                e.Position+=diff.Normalized*Math.Min(e.Speed*(1-slow)*dt,Math.Max(0,diff.Length-stop));
                if(!e.Engaged && !atCoreLeg && V2.Distance(e.Position,target)<=.04f) { e.Waypoint++; continue; }
                if((!e.Engaged && !atCoreLeg) || V2.Distance(e.Position,target)>stop+.02f || e.AttackCooldown>0)continue;
                float damage=e.Kind==EnemyKind.Siege ? 24 : e.Kind==EnemyKind.Brute ? 13 : 7;
                if(e.Engaged) HurtHunter(damage);
                else { CoreHealth=Math.Max(0,CoreHealth-damage); Emit(EventKind.CoreHurt,new V2(),new V2(),damage); }
                e.AttackCooldown=e.Kind==EnemyKind.Siege ? 1.5f : 1.1f;
            }
            Enemies.RemoveAll(e=>e.Health<=0);
        }
        private void Finish(bool won) { bool missed=won && Rules!=null && Rules.Stage!=null && Rules.Stage.Boss && !BossDefeated; if(missed)won=false; State=won ? RunState.Won : RunState.Lost; DefeatReason=won ? "" : missed ? "잿빛 문지기를 처치하지 못했습니다." : CoreHealth<=0 ? "도시 결계가 붕괴했습니다." : "헌터가 쓰러졌습니다."; Emit(EventKind.End,PlayerPosition,new V2(),won ? 1 : 0); }
        private void Emit(EventKind k,V2 p,V2 t,float v,int source=-1,int wave=-1,int enemyId=0)
        { Events.Add(new CombatEvent(k,p,t,v,source,wave,enemyId)); }
        private static float Clamp(float x,float a,float b) { return Math.Max(a,Math.Min(b,x)); }
    }
}

