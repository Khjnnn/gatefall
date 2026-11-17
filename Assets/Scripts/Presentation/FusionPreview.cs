using System;
using Gatefall.Core;

namespace Gatefall
{
    // Unity-free presentation model, also exercised by the host HUD checks.
    public sealed class FusionPreview
    {
        public const string HighFit="선로 겹침 높음", MediumFit="선로 겹침 보통", LowFit="선로 겹침 낮음";
        public const string MarkFit="범위 내 표식 지원 있음", NoMarkFit="범위 내 표식 지원 없음";
        public const float HighOverlap=.60f, MediumOverlap=.25f;
        readonly int[] cards=new int[6];
        DefenseSimulation board;
        FusionRecipe recipe;
        SealInheritance inheritance;
        TowerKind resultKind;
        int blastResearch;
        TowerStats stats, sampledStats;
        V2 sampledOrigin, sampledAim;
        float overlap;
        public TowerStats Stats(DefenseSimulation game,FusionRecipe result)
        {
            int research=game.Rules==null ? 0 : game.Rules.Research[2];
            bool changed=board!=game || recipe!=result || inheritance!=result.Inheritance || resultKind!=result.Result || blastResearch!=research;
            for(int i=0;i<cards.Length;i++)if(cards[i]!=game.Cards[i])changed=true;
            if(changed || stats==null) {
                board=game;recipe=result;inheritance=result.Inheritance;resultKind=result.Result;blastResearch=research;stats=game.Stats(result);
                for(int i=0;i<cards.Length;i++)cards[i]=game.Cards[i];
            }
            return stats;
        }
        public float Overlap(DefenseSimulation game,TowerStats s,V2 origin,V2 aim)
        {
            if(sampledStats!=s || V2.DistanceSquared(origin,sampledOrigin)>.00001f || V2.DistanceSquared(aim,sampledAim)>.0001f) {
                overlap=RoadOverlap(game,s,origin,aim);sampledStats=s;sampledOrigin=origin;sampledAim=aim;
            }
            return overlap;
        }
        public static bool Marked(TowerStats s) { return s.MarkBonus>0 && s.LineHalfWidth<=0 && s.ConeCos<=0; }
        public static bool HasMarker(DefenseSimulation game,TowerStats s,V2 origin,int anchorSocket,int partnerSocket=-1)
        {
            foreach(Tower tower in game.Towers)
                if(tower.Socket!=anchorSocket && tower.Socket!=partnerSocket && tower.Commission<=0 &&
                    (tower.Kind==TowerKind.Marker || tower.Kind==TowerKind.Beacon) && V2.DistanceSquared(origin,tower.Position)<=s.Range*s.Range)return true;
            return false;
        }
        public static string Situation(TowerStats s)
        {
            if(s.Inheritance==SealInheritance.None)return "";
            if(s.LineHalfWidth>0)return "일렬 선로에 강함";
            if(s.ConeCos>0)return "퍼진 적에 강함";
            if(Marked(s))return "뒤쪽 표식 적 우선";
            if(s.Chain!=null)return s.TailBurstTargets>0 ? "연쇄 끝 밀집에 강함" : "이어진 적에 강함";
            if(s.Radius>0)return s.Slow>0 ? "밀집 적 감속에 강함" : "밀집 적에 강함";
            return "선두 적 집중 공격";
        }
        // Target query is private in DefenseSimulation. Mirror its availability and Lead ordering
        // (distance to core, then id), including marked preference; never mutate its scratch list.
        public static V2 Aim(DefenseSimulation game,TowerStats s,V2 origin,out V2 impact)
        {
            Enemy best=null;float range2=s.Range*s.Range;
            foreach(Enemy enemy in game.Enemies) {
                if(enemy.Health<=0 || enemy.IncomingDamage>=enemy.Health || V2.DistanceSquared(origin,enemy.Position)>range2)continue;
                if(best==null || Preferred(game,s,enemy,best))best=enemy;
            }
            if(best!=null) {impact=best.Position;return (impact-origin).Normalized;}
            float nearest=float.MaxValue, lead=float.MaxValue;V2 tangent=new V2(1,0);impact=origin;
            foreach(int lane in game.OpenLanes) {
                V2[] road=game.Roads[lane];
                for(int i=1;i<road.Length;i++) {
                    V2 a=road[i-1], edge=road[i]-a;float length2=Dot(edge,edge);
                    if(length2<=.00001f)continue;
                    float projection=Dot(origin-a,edge)/length2;
                    float t=Math.Max(0,Math.Min(1,projection));
                    V2 point=a+edge*t;float distance=V2.DistanceSquared(origin,point);
                    if(distance>range2)continue;
                    if(s.ConeCos>0) {
                        // Closest-to-core reachable road point stands in for the leading enemy.
                        float perpendicular=V2.DistanceSquared(origin,a+edge*projection);
                        float reach=(float)Math.Sqrt(Math.Max(0,(range2-perpendicular)/length2));
                        float end=Math.Min(1,projection+reach), start=Math.Max(0,projection-reach);
                        float core=Math.Max(start,Math.Min(end,-Dot(a,edge)/length2));
                        point=a+edge*core;float coreDistance=Dot(point,point);
                        if(coreDistance>=lead)continue;lead=coreDistance;
                    } else {if(distance>=nearest)continue;nearest=distance;}
                    impact=point;tangent=edge.Normalized;
                }
            }
            V2 aim=(impact-origin).Normalized;
            return aim.Length>.0001f ? aim : tangent;
        }
        static bool Before(Enemy a,Enemy b,bool marked)
        {
            if(marked && (a.MarkRemaining>0)!=(b.MarkRemaining>0))return a.MarkRemaining>0;
            float x=Dot(a.Position,a.Position), y=Dot(b.Position,b.Position);
            return x<y || x==y && a.Id<b.Id;
        }
        static bool Preferred(DefenseSimulation game,TowerStats s,Enemy a,Enemy b)
        {
            if(s.Inheritance==SealInheritance.GravitySpear) {
                int first=Cluster(game,a,s.Radius), second=Cluster(game,b,s.Radius);
                if(first!=second)return first>second;
            }
            if(s.Inheritance==SealInheritance.ShatterSlow && (a.Kind>=EnemyKind.Brute)!=(b.Kind>=EnemyKind.Brute))return a.Kind>=EnemyKind.Brute;
            return Before(a,b,Marked(s));
        }
        static int Cluster(DefenseSimulation game,Enemy centre,float radius)
        {
            int count=0;
            foreach(Enemy enemy in game.Enemies)if(enemy.Health>0 && V2.DistanceSquared(centre.Position,enemy.Position)<=radius*radius)count++;
            return count;
        }
        static float Dot(V2 a,V2 b) {return a.X*b.X+a.Y*b.Y;}
        public static string Fit(float share) {return share>=HighOverlap ? HighFit : share>=MediumOverlap ? MediumFit : LowFit;}
        // Equal-area quadrature: 32x8 strip cells or 24x16 fan cells. Road includes its
        // authored half-width, open lanes only. >=60% high, >=25% medium, otherwise low.
        public static float RoadOverlap(DefenseSimulation game,TowerStats s,V2 origin,V2 aim)
        {
            if(s.Range<=0 || s.LineHalfWidth<=0 && s.ConeCos<=0)return 0;
            aim=aim.Normalized;if(aim.Length<.0001f)return 0;
            // A line hits the enemies that walk along it, so measure how much of its length follows the road
            // centre (within its own half-width). Area overlap made a strip crossing a road at right angles look "보통".
            if(s.LineHalfWidth>0) {
                int along=0;const int slices=48;
                for(int i=0;i<slices;i++) {
                    V2 centre=origin+aim*(s.Range*(i+.5f)/slices);
                    foreach(int lane in game.OpenLanes)
                        if(BattlefieldLayouts.RoadDistance(centre,game.Roads[lane])<=s.LineHalfWidth){along++;break;}
                }
                return along/(float)slices;
            }
            V2 side=new V2(-aim.Y,aim.X);int hits=0, count=0;
            int rows=s.LineHalfWidth>0 ? 32 : 24, columns=s.LineHalfWidth>0 ? 8 : 16;
            float angle=(float)Math.Acos(Math.Max(-1,Math.Min(1,s.ConeCos)));
            for(int i=0;i<rows;i++)for(int j=0;j<columns;j++) {
                V2 point;
                if(s.LineHalfWidth>0)point=origin+aim*(s.Range*(i+.5f)/rows)+side*(s.LineHalfWidth*(2*(j+.5f)/columns-1));
                else {
                    float r=s.Range*(float)Math.Sqrt((i+.5f)/rows), a=angle*(2*(j+.5f)/columns-1);
                    point=origin+aim*(r*(float)Math.Cos(a))+side*(r*(float)Math.Sin(a));
                }
                foreach(int lane in game.OpenLanes)
                    if(BattlefieldLayouts.RoadDistance(point,game.Roads[lane])<=BattlefieldLayouts.RoadHalfWidth){hits++;break;}
                count++;
            }
            return hits/(float)count;
        }
    }
}
