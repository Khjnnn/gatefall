using System;
using System.Collections.Generic;

namespace Gatefall.Core
{
    public sealed partial class Enemy
    {
        public int BlockOwner;
        public float BlockRemaining, BlockImmunity, BossWindup, BossNext = 5;
        public V2 BossTarget;
    }
    public sealed partial class Tower { public float WardHealth; internal int HeldCount; internal float BlockRange; }
    public sealed partial class DefenseSimulation
    {
        public CampaignBattle Rules { get; private set; }
        public V2[] BoardSockets { get; private set; } = Sockets;
        public V2[] GatePositions { get; private set; } = Gates;
        public V2[][] Roads { get; private set; } = Paths;
        public int[] OpenLanes { get; private set; } = new[]{0,1,2};
        public bool ExpandedArena { get { return Rules != null && Rules.Stage != null && Rules.Stage.Index == 3; } }
        public float HalfWidth { get { return Rules!=null && Rules.Stage!=null ? BattlefieldLayouts.HalfWidth : ArenaHalfWidth; } }
        public float HalfHeight { get { return Rules!=null && Rules.Stage!=null ? BattlefieldLayouts.HalfHeight : ArenaHalfHeight; } }
        public float MinimumCoreHealth { get; private set; }
        public float PeakErosion { get { return Clamp(1-MinimumCoreHealth/Balance.CoreHealth,0,1); } }
        public bool BossSpawned { get; private set; }
        public bool BossDefeated { get; private set; }
        public float SkillCooldown { get; private set; }
        public float SupportCooldown { get; private set; }
        public float HunterShield { get; private set; }
        public float HunterSkillActiveRemaining => Rules?.Hunter == 1 ? sprint : HunterShield;
        public float CoreShield { get; private set; }
        float sprint;
        V2 lastMovement = new V2(0,1);
        public bool IsPreparing { get { return Rules!=null && State==RunState.Ready; } }
        public bool HasAdvancedTowers { get { return Rules != null && (Rules.Lab || Rules.Stage.HasAdvancedTowers); } }
        public bool Allows(TowerKind kind)
        {
            if(Rules==null)return kind!=TowerKind.Barrier && kind!=TowerKind.Marker && kind!=TowerKind.Observer && kind!=TowerKind.Beacon;
            if(Rules.Lab)return true;
            if(!Rules.Stage.Allows(kind))return false;
            if((kind==TowerKind.Marker || kind==TowerKind.Beacon) && !Rules.MarkerUnlocked || kind==TowerKind.Observer && !Rules.ObserverUnlocked)return false;
            if(Rules.Stage.Index>=6 && TowerCatalog.Get(kind).Tier==1 && (Rules.Loadout&(1<<(int)kind))==0)return false;
            return true;
        }
        // Family availability only: ignores credits, upgrade levels and catalysts.
        // Existing towers remain usable as ingredients even when their base families are absent.
        public int ReachableTowerFamilies()
        {
            int reachable=0;
            foreach(TowerDefinition d in TowerCatalog.All)
                if(d.Tier==1 && Allows(d.Kind))reachable|=1<<(int)d.Kind;
            foreach(Tower t in Towers)reachable|=1<<(int)t.Kind;
            for(int pass=0;pass<TowerCatalog.Recipes.Length;pass++) {
                int before=reachable;
                foreach(FusionRecipe r in TowerCatalog.Recipes) {
                    int needed=(1<<(int)r.First)|(1<<(int)r.Second);
                    if((reachable&needed)==needed && Allows(r.Result))reachable|=1<<(int)r.Result;
                }
                if(reachable==before)break;
            }
            return reachable;
        }
        GameBalance ConfigureBalance(GameBalance b)
        {
            var copy=new GameBalance {Duration=b.Duration,PlayerSpeed=b.PlayerSpeed,PlayerHealth=b.PlayerHealth,CoreHealth=b.CoreHealth,
                PlayerRange=b.PlayerRange,PlayerDamage=b.PlayerDamage,PlayerFireInterval=b.PlayerFireInterval,StartingCredits=b.StartingCredits,
                SpawnEnemies=b.SpawnEnemies,OfferCards=b.OfferCards};
            if(Rules==null)return copy;
            if(Rules.Stage!=null) copy.StartingCredits+=Rules.Stage.StartingCreditBonus;
            if(Rules.Hunter==1) {copy.PlayerSpeed=6.4f;copy.PlayerHealth=85;copy.PlayerDamage=10;copy.PlayerFireInterval=.3f;}
            if(Rules.Hunter==2) {copy.PlayerSpeed=4.4f;copy.PlayerHealth=160;copy.PlayerDamage=24;copy.PlayerFireInterval=.7f;copy.PlayerRange=2.8f;}
            if(Rules.Stage!=null)copy.PlayerFireInterval*=.85f;
            copy.PlayerHealth*=1+.025f*Rules.EarnedGrowth;copy.PlayerDamage*=1+.02f*Rules.EarnedGrowth;
            return copy;
        }
        void ConfigureRaon()
        {
            if(Rules==null || Rules.Stage==null)return;
            CampaignStage stage=Rules.Stage;OpenLanes=(int[])stage.Lanes.Clone();
            BattlefieldLayout layout=BattlefieldLayouts.Get(stage);
            Roads=new V2[layout.Roads.Length][];
            for(int i=0;i<Roads.Length;i++)Roads[i]=(V2[])layout.Roads[i].Clone();
            GatePositions=new V2[Roads.Length];
            foreach(int lane in OpenLanes)GatePositions[lane]=Roads[lane][0];
            BoardSockets=(V2[])layout.Sockets.Clone();
            Schedule.Clear();
            for(int wave=0;wave<6;wave++) {
                int multiplier=stage.SwarmMultiplier, formations=stage.Counts[wave]/multiplier;
                bool[] seenLane=new bool[Roads.Length];
                for(int j=0;j<formations;j++) {
                    int laneIndex=(wave+j+(Rules.Crisis?1:0))%stage.Lanes.Length;
                    // 1-2 has a main rush alternating between the two entrances, with a smaller flank.
                    if(stage.Index==1 && wave>=1)laneIndex=(wave+(j%4==3 ? 1 : 0)+(Rules.Crisis?1:0))%2;
                    // 1-5 sends small packs down alternating lanes: area attacks can intercept a cluster.
                    if(stage.Index==4 && wave>=3)laneIndex=(wave+j/3+(Rules.Crisis?1:0))%stage.Lanes.Length;
                    int lane=stage.Lanes[laneIndex];
                    EnemyKind kind=EnemyKind.Grunt;
                    if(stage.Index>=1 && j%4==0)kind=EnemyKind.Runner;
                    if(stage.Index==0 && wave>=2 && j%3==0)kind=EnemyKind.Runner;
                    if(stage.Index==1 && wave>=2 && j%3==0)kind=EnemyKind.Runner;
                    if(stage.Index>=2 && wave>=2 && j%9==0)kind=EnemyKind.Brute;
                    if(stage.Index==2 && wave>=3 && j%6==0)kind=EnemyKind.Brute;
                    if(stage.Index>=3 && wave>=3 && j%17==0)kind=EnemyKind.Siege;
                    if(stage.Index==4 && wave>=3 && j%12==0)kind=EnemyKind.Siege;
                    // 1-11 introduces the marker against a steady stream of tough targets on both roads.
                    if(stage.Index==10 && wave>=2 && j%6==0)kind=EnemyKind.Brute;
                    if(Rules.Crisis && wave>=3 && j%11==0)kind=EnemyKind.Brute;
                    // Later waves of the expanded operation arrive in a tighter pulse across all lanes.
                    double arrivalWindow=(ExpandedArena || stage.Index==0 || stage.Index==2) && wave>=3 ? 16.0 : 22.0;
                    if(stage.Index==1 && wave>=2)arrivalWindow=12.0;
                    if(stage.Index==8 && !Rules.Crisis && wave>=3)arrivalWindow=12.0;
                    if(stage.Index==13 && !Rules.Crisis && wave>=3)arrivalWindow=12.0;
                    if(stage.Index==10 && !Rules.Crisis && wave>=3)arrivalWindow=16.0;
                    double step=arrivalWindow/Math.Max(1,stage.Counts[wave]-1);
                    double offset=j*multiplier*step;
                    bool southPulse = stage.Index == 6 && lane == 3;
                    if(!Rules.Crisis && wave>=3 && (southPulse || stage.Index==8 || stage.Index==10 || stage.Index==13)) {
                        double originalStep=22.0/Math.Max(1,stage.Counts[wave]-1);
                        // Preserve the first entrance on each road and the established 1-14 timing.
                        double firstLaneOffset=(j%(stage.Index==13 ? 2 : stage.Lanes.Length))*multiplier*originalStep;
                        double pulseWindow = southPulse ? 12.0 : arrivalWindow;
                        double compression=(pulseWindow-firstLaneOffset)/(22.0-firstLaneOffset);
                        offset=firstLaneOffset+(j*multiplier*originalStep-firstLaneOffset)*compression;
                        step=originalStep*compression;
                    }
                    if(stage.Index==4 && wave>=3) {
                        double originalGroupStep=15.5/Math.Max(1,(formations-1)/3);
                        double groupOffset=(j/3)*originalGroupStep;
                        if(!Rules.Crisis) {
                            double firstGroupOffset=((j/3)%stage.Lanes.Length)*originalGroupStep;
                            groupOffset=firstGroupOffset+(groupOffset-firstGroupOffset)*(10.0-firstGroupOffset)/(15.5-firstGroupOffset);
                        }
                        offset=groupOffset+(j%3)*.25;step=.25/multiplier;
                    }
                    Group(wave,lane,kind,1,wave*30+5+offset,1);
                    Schedule[Schedule.Count-1].Warned=seenLane[lane];seenLane[lane]=true;
                    Group(wave,lane,EnemyKind.Grunt,multiplier-1,wave*30+5+offset+step,step);
                    Schedule[Schedule.Count-1].Warned=true;Schedule[Schedule.Count-1].Swarm=true;
                }
            }
            if(stage.Boss)Group(5,1,EnemyKind.Boss,1,150,1);
        }
        void MovePlayer(float dt,V2 movement)
        {
            if(HunterDown)return;
            if(float.IsNaN(movement.X) || float.IsNaN(movement.Y) || float.IsInfinity(movement.X) || float.IsInfinity(movement.Y))movement=new V2();
            if(movement.Length>1)movement=movement.Normalized;
            V2 next=PlayerPosition+movement*(Balance.PlayerSpeed*(sprint>0 ? 1.65f : 1)*dt);
            next.X=Clamp(next.X,-HalfWidth,HalfWidth);next.Y=Clamp(next.Y,-HalfHeight,HalfHeight);
            if(next.Length<1.35f)next=(next.Length>.001f ? next.Normalized : new V2(0,-1))*1.35f;
            PlayerPosition=next;
            if(movement.Length>.05f) {PlayerAim=movement.Normalized;lastMovement=movement.Normalized;}
        }
        TowerStats RawStats(TowerKind kind,int level)
        {
            TowerStats s=TowerCatalog.Get(kind).Levels[level-1].Copy();
            if(Rules!=null && kind==TowerKind.Autocannon) {s.Damage=level==1 ? 14 : level==2 ? 22 : 30;s.Interval=.7f;}
            if(Rules!=null && Rules.Stage!=null) {
                if(kind==TowerKind.Autocannon)s.Interval=.46f;
                else if(kind!=TowerKind.Barrier)s.Interval*=.85f;
            }
            return s;
        }
        void ApplyResearch(TowerKind kind,TowerStats s)
        {
            if(Rules==null)return;
            TowerTag tags=TowerCatalog.Get(kind).Tags;
            // Damage research uses the current level's unmodified value, not card-amplified damage.
            if((tags&TowerTag.Ballistic)!=0) {
                float fraction=.04f*Rules.Research[0];
                float cardScale=1+.15f*Cards[(int)CardKind.Ballistic];
                s.Damage+=s.Damage/cardScale*fraction;s.Secondary+=s.Secondary/cardScale*fraction;
                if(s.Chain!=null)for(int i=0;i<s.Chain.Length;i++)s.Chain[i]+=s.Chain[i]/cardScale*fraction;
            }
            if((tags&TowerTag.Control)!=0)s.SlowSeconds+=.05f*Rules.Research[1];
            if((tags&TowerTag.Blast)!=0)s.Radius+=.05f*Rules.Research[2];
            if(kind==TowerKind.Barrier)s.Interval*=1-.03f*Rules.Research[3];
        }
        bool ActiveForResonance(Tower t)
        { return t.Kind!=TowerKind.Marker && t.Commission<=0 && (t.Kind!=TowerKind.Barrier || t.Cooldown<=0) && TowerCatalog.Get(t.Kind).Tier==1; }
        public bool HasResonance(Tower tower)
        {
            if(Rules==null || !Rules.Lab && !Rules.Stage.HasAdjacency || !ActiveForResonance(tower))return false;
            foreach(Tower other in Towers)if(other.Id!=tower.Id && other.Kind==tower.Kind && ActiveForResonance(other) && V2.Distance(other.Position,tower.Position)<=6)return true;
            return false;
        }
        void ApplyAdjacency(Tower tower,TowerStats stats,int level)
        {
            if(!HasResonance(tower))return;
            TowerStats raw=RawStats(tower.Kind,level);
            if(tower.Kind==TowerKind.Autocannon || tower.Kind==TowerKind.Rupture)stats.Damage+=raw.Damage*.05f;
            if(tower.Kind==TowerKind.Binding)stats.SlowSeconds+=raw.SlowSeconds*.05f;
        }
        void TickRaonTimers(float dt)
        {
            SkillCooldown=Math.Max(0,SkillCooldown-dt);SupportCooldown=Math.Max(0,SupportCooldown-dt);
            HunterShield=Math.Max(0,HunterShield-dt);CoreShield=Math.Max(0,CoreShield-dt);sprint=Math.Max(0,sprint-dt);
        }
        public bool UseSkill()
        {
            if(Rules==null || State!=RunState.Running || HunterDown || SkillCooldown>0)return false;
            SkillCooldown=12;
            if(Rules.Hunter==2) { HunterShield=4;HunterInvulnerability=.6f; }
            else {
                if(Rules.Hunter==1)sprint=3;
                else MovePlayer(2.8f/Balance.PlayerSpeed,lastMovement);
                HunterInvulnerability=.8f;
            }
            Emit(EventKind.Upgrade,PlayerPosition,new V2(),0);return true;
        }
        public bool UseSupport()
        {
            if(Rules==null || State!=RunState.Running || SupportCooldown>0)return false;
            SupportCooldown=35;CoreShield=5;Emit(EventKind.Upgrade,new V2(),new V2(),0);return true;
        }
        public float WardMaximum(Tower t) { return t.Level==1 ? 55 : t.Level==2 ? 90 : 115; }
        void UpdateWard(Tower t,float dt)
        {
            if(t.Cooldown>0) {t.Cooldown=Math.Max(0,t.Cooldown-dt);if(t.Cooldown==0)t.WardHealth=WardMaximum(t);return;}
            if(!IsHolding(t))t.WardHealth=Math.Min(WardMaximum(t),t.WardHealth+dt*8);
        }
        // Runs for every ward on every step: a plain loop avoids a capturing predicate allocation.
        bool IsHolding(Tower ward)
        {
            foreach(Enemy e in Enemies)if(e.Health>0 && e.BlockOwner==ward.Id)return true;
            return false;
        }
        void ReleaseBlock(Enemy e)
        { if(e.BlockOwner>0) {Tower ward=TowerById(e.BlockOwner);if(ward!=null)ward.HeldCount=Math.Max(0,ward.HeldCount-1);}e.BlockOwner=0;e.BlockRemaining=0;e.Engaged=false;e.BlockImmunity=4; }
        public const int WardBurstSlowPercent = 35;
        public const float WardBurstSeconds = 1.6f;

        private void BreakWard(Tower ward)
        {
            TowerStats stats = Stats(ward);
            ward.WardHealth = 0;
            ward.Cooldown = stats.Interval;
            float radiusSquared = stats.Range * stats.Range;
            foreach (Enemy enemy in Enemies) {
                if (enemy.BlockOwner == ward.Id) ReleaseBlock(enemy);
                if (enemy.Health > 0 && enemy.Kind != EnemyKind.Boss &&
                    V2.DistanceSquared(enemy.Position, ward.Position) <= radiusSquared)
                    ApplySlow(enemy, ward.Id, WardBurstSlowPercent / 100f, WardBurstSeconds);
            }
            Emit(EventKind.WardBurst, ward.Position, new V2(), stats.Range, (int)TowerKind.Barrier);
        }
        void UpdateRaonEnemies(float dt)
        {
            foreach(Enemy e in Enemies) {
                e.BlockImmunity=Math.Max(0,e.BlockImmunity-dt);
                if(e.BlockOwner!=0) {
                    e.BlockRemaining-=dt;
                    Tower ward=e.BlockOwner>0 ? TowerById(e.BlockOwner) : null;
                    if(e.BlockRemaining<=0 || e.BlockOwner<0 && (HunterDown || V2.Distance(e.Position,PlayerPosition)>3) ||
                        e.BlockOwner>0 && (ward==null || ward.Cooldown>0 || ward.Commission>0))ReleaseBlock(e);
                }
            }
            // A ward's range comes from its level and range cards, which change only between steps; resonance never
            // affects range. Resolve it once per ward here instead of once per (enemy, ward) pair below: Stats() allocates.
            int engaged=0;foreach(Tower t in Towers) {t.HeldCount=0;if(t.Kind==TowerKind.Barrier)t.BlockRange=Stats(t).Range;}
            foreach(Enemy e in Enemies)if(e.Health>0) {
                if(e.BlockOwner<0)engaged++;
                else if(e.BlockOwner>0) {Tower ward=TowerById(e.BlockOwner);if(ward!=null)ward.HeldCount++;}
            }
            foreach(Enemy e in Enemies) {
                if(e.Health<=0)continue;
                e.HitFlash=Math.Max(0,e.HitFlash-dt);e.AttackCooldown-=dt;
                bool blockable=e.Kind!=EnemyKind.Siege && e.Kind!=EnemyKind.Boss;
                if(e.BlockOwner==0 && e.BlockImmunity<=0 && blockable) {
                    if(!HunterDown && engaged<2 && V2.Distance(e.Position,PlayerPosition)<2.5f) {e.BlockOwner=-1;e.BlockRemaining=3;e.Engaged=true;engaged++;}
                    else foreach(Tower t in Towers) {
                        if(t.Kind!=TowerKind.Barrier || t.Cooldown>0 || t.Commission>0 || V2.Distance(t.Position,e.Position)>t.BlockRange)continue;
                        if(t.HeldCount>=2)continue;
                        e.BlockOwner=t.Id;t.HeldCount++;e.BlockRemaining=HasResonance(t) ? 3.15f : 3;break;
                    }
                }
                float slow=e.SlowFraction;foreach(SlowEffect effect in e.Slows)effect.Remaining-=dt;e.Slows.RemoveAll(x=>x.Remaining<=0);
                if(e.Kind==EnemyKind.Boss)UpdateBoss(e,dt);
                V2[] path=Roads[Math.Max(0,Math.Min(Roads.Length-1,e.Lane))];e.Waypoint=Math.Max(1,Math.Min(e.Waypoint,path.Length-1));
                bool heldByWard=e.BlockOwner>0; bool atCore=e.BlockOwner==0 && e.Waypoint==path.Length-1;
                V2 target=e.BlockOwner<0 ? PlayerPosition : path[e.Waypoint];
                if(e.BlockOwner==0 && !atCore && e.LaneOffset!=0) {
                    V2 direction=(path[e.Waypoint]-path[e.Waypoint-1]).Normalized;
                    target+=new V2(-direction.Y,direction.X)*e.LaneOffset;
                }
                float stop=e.BlockOwner<0 ? .64f : atCore ? (e.Kind==EnemyKind.Boss ? 2.2f : 1.12f) : 0;
                V2 diff=target-e.Position;
                if(!heldByWard)e.Position+=diff.Normalized*Math.Min(e.Speed*(1-slow)*dt,Math.Max(0,diff.Length-stop));
                if(e.BlockOwner==0 && !atCore && V2.Distance(e.Position,target)<=.04f) {e.Waypoint++;continue;}
                if(e.AttackCooldown>0)continue;
                float damage=e.Kind==EnemyKind.Boss ? 22 : e.Kind==EnemyKind.Siege ? 24 : e.Kind==EnemyKind.Brute ? 13 : e.Swarm ? 1 : 7;
                if(heldByWard) {
                    Tower t=TowerById(e.BlockOwner);
                    if (t != null) {
                        t.WardHealth -= damage;
                        if (t.WardHealth <= 0) BreakWard(t);
                    }
                } else if(e.BlockOwner<0 && V2.Distance(e.Position,PlayerPosition)<=.67f)HurtHunter(damage);
                else if(atCore && V2.Distance(e.Position,target)<=stop+.02f) {
                    float hit = damage * (CoreShield > 0 ? .4f : 1);
                    float previousHealth = CoreHealth;
                    CoreHealth = Math.Max(0, CoreHealth - hit);
                    if (e.Lane >= 0 && e.Lane < Telemetry.CoreDamageByLane.Length)
                        Telemetry.CoreDamageByLane[e.Lane] += previousHealth - CoreHealth;
                    Emit(EventKind.CoreHurt, new V2(), new V2(), hit, e.Lane);
                } else continue;
                e.AttackCooldown=e.Kind==EnemyKind.Siege || e.Kind==EnemyKind.Boss ? 1.5f : 1.1f;
            }
            Enemies.RemoveAll(e=>e.Health<=0);
            // v0.5: 응급 방호 triggers itself instead of occupying a second skill button.
            if(SupportCooldown<=0 && CoreHealth>0 && CoreHealth<Balance.CoreHealth*.5f) {
                SupportCooldown=35;CoreShield=5;Emit(EventKind.Shield,new V2(),new V2(),5);
            }
        }
        void HurtHunter(float damage)
        {
            if(HunterDown || HunterInvulnerability>0)return;
            float hit=damage*(HunterShield>0 ? .25f : 1);PlayerHealth=Math.Max(0,PlayerHealth-hit);
            HunterInvulnerability=.65f;Emit(EventKind.HunterHurt,PlayerPosition,new V2(),hit);
            if(HunterDown) {
                HunterRespawnRemaining=HunterRespawnDelay;HunterShield=0;sprint=0;
                foreach(Enemy enemy in Enemies) {
                    if(enemy.BlockOwner<0)ReleaseBlock(enemy);
                    else if(Rules==null && enemy.Engaged) {enemy.Engaged=false;enemy.ReengageDelay=1;}
                }
                Emit(EventKind.HunterDown,PlayerPosition,new V2(),HunterRespawnDelay);
            }
        }
        void UpdateBoss(Enemy e,float dt)
        {
            if(e.BossWindup>0) {
                e.BossWindup=Math.Max(0,e.BossWindup-dt);
                if(e.BossWindup==0) {
                    if(V2.Distance(PlayerPosition,e.BossTarget)<2.2f)HurtHunter(32);
                    Emit(EventKind.Hit,e.BossTarget,e.Position,32,-2);e.BossNext=7;
                }
            } else {
                e.BossNext-=dt;
                if(e.BossNext<=0 && !HunterDown) {e.BossTarget=PlayerPosition;e.BossWindup=1.5f;Emit(EventKind.Warning,e.BossTarget,new V2(),3);}
            }
        }
    }
}

