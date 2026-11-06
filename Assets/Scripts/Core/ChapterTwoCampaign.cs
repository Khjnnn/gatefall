using System;
using System.Collections.Generic;

namespace Gatefall.Core
{
    // Authored wave data. Times are combat seconds; no random/infinite reinforcements.
    public sealed class CampaignWave
    {
        public double Start;
        public SpawnGroup[] Groups;
    }

    public static class ChapterTwoCampaign
    {
        static V2[] P(params float[] xy)
        { var p=new V2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new V2(xy[i*2],xy[i*2+1]);return p; }
        static readonly V2[] None=new V2[0];
        static BattlefieldLayout Map(string id,V2[] west,V2[] north,V2[] east,V2[] south)
        {return new BattlefieldLayout{Id=id,Theme=2,Roads=new[]{west,north,east,south}};}
        static SpawnGroup G(int lane,EnemyKind kind,int count,double delay,double span,bool swarm=false)
        {return new SpawnGroup{Lane=lane,Kind=kind,Count=count,Start=delay,Interval=count>1?span/(count-1):1,Swarm=swarm};}
        static CampaignWave W(double start,params SpawnGroup[] groups)
        {return new CampaignWave{Start=start,Groups=groups};}
        static SpawnGroup Pack(int lane,int count,double delay=0,double span=14)
        {return G(lane,EnemyKind.Grunt,count,delay,span,true);}
        static SpawnGroup Fast(int lane,int count,double delay=0)
        {return G(lane,EnemyKind.Runner,count,delay,8);}
        static SpawnGroup Armor(int lane,int count,double delay=0,bool siege=false)
        {return G(lane,siege?EnemyKind.Siege:EnemyKind.Brute,count,delay,6);}
        static CampaignStage S(int local,string name,int sites,int[] lanes,BattlefieldLayout map,
            string briefing,string lesson,string clear,params CampaignWave[] waves)
        {
            map.Theme=(local-1)%4;
            var counts=new int[waves.Length];for(int i=0;i<waves.Length;i++)foreach(var g in waves[i].Groups)counts[i]+=g.Count;
            return new CampaignStage{Index=13+local,Chapter=2,Number=local,StableId="C02-S"+local.ToString("000"),
                PredecessorId=local==1?"C01-S014":"C02-S"+(local-1).ToString("000"),
                Name=name,SocketCount=sites,Lanes=lanes,Counts=counts,Layout=map,Waves=waves,
                FirstReward=36+local*2,TowerLimit=14,SwarmHealth=2.7f,Threat=1.05f+(local-1)*.035f,
                HealthScale=1.45f,EliteArmor=6,HybridDamage=1.25f,BindingLevel2Targets=6,BindingLevel3Targets=8,
                RecommendedGrowth=5,RecommendedResearch=new[]{3,2,2,2},Briefing=briefing,Lesson=lesson,
                Story="유채린: 시장의 불을 다시 켜요. "+briefing.Split('\n')[0],ClearStory=clear};
        }
        public static CampaignStage[] Create()
        {
            return new[]{
                S(1,"다솜 남문",24,new[]{0,3},
                    Map("market-south-shortcut",P(-23,5,-17,5,-17,-3,-9,-3,-9,3,-3,3,0,0),None,None,P(16,-11,16,-5,9,-5,9,-1,3,-1,0,0)),
                    "긴 서쪽 길과 짧은 남쪽 길.\n남쪽을 먼저 보강하세요.","짧은 남쪽에 먼저 배치하세요. 양쪽 길이 만나는 곳의 붕괴도 유효합니다.",
                    "다솜 남문의 등불이 켜졌습니다. 곡물 창고로 보급선을 이으세요.",
                    W(5,Pack(0,64)),W(35,Pack(0,40),Fast(3,12,3),Pack(3,24,7)),
                    W(65,Pack(0,64),Armor(3,2,4)),W(95,Pack(3,64),Pack(0,40,6)),
                    W(125,Armor(0,3),Pack(0,64,5),Fast(3,12,8)),W(155,Pack(0,72),Pack(3,64,5),Armor(3,2,9))),
                S(2,"곡물 창고",24,new[]{0,2},
                    Map("granary-shared-loading-neck",P(-23,-5,-16,-5,-16,3,-8,3,-4,0,0,0),None,P(23,5,16,5,16,-4,7,-4,7,3,-8,3,-4,0,0,0),None),
                    "창고 앞 합류목에 군집이 모입니다.\n클리어 후 산탄 결계주 해금.","양쪽 입구를 나누거나 합류목에 붕괴를 두세요. 마지막에는 장갑 적이 이어집니다.",
                    "산탄 결계주 해금! 편성에서 선택하고 가까운 군집을 쓸어내세요.",
                    W(5,Pack(0,72)),W(35,Pack(2,72),Fast(0,12,8)),W(65,Pack(0,64),Pack(2,48,4)),
                    W(95,Pack(2,88),Armor(0,3,5)),W(125,Pack(0,80),Fast(2,18,3)),
                    W(155,Armor(2,4),Pack(0,72,3),Pack(2,72,6))),
                S(3,"야시장 삼거리",26,new[]{0,1,2},
                    Map("night-market-staggered-crossing",P(-23,-5,-16,-5,-16,2,-8,2,-8,-3,-3,-3,0,0),P(-5,11,-5,7,5,7,5,2,0,2,0,0),P(23,-4,17,-4,17,4,10,4,10,-2,4,-2,0,0),None),
                    "세 갈래의 도착 시점이 다릅니다.\n산탄은 가까운 부채꼴, 붕괴는 원형 폭격.","산탄을 길 옆에 두고 가까운 군집을 노리세요. 장갑 적에는 붕괴나 조합을 보강하세요.",
                    "야시장 삼거리를 되찾았습니다. 다음은 한 길이 두 번 돌아오는 순환로입니다.",
                    W(5,Pack(0,64)),W(35,Pack(1,64),Fast(2,12,4)),W(65,Pack(2,80),Armor(0,3,4)),
                    W(95,Pack(0,64),Pack(1,40,4)),W(125,Fast(1,18),Pack(2,72,3)),
                    W(155,Armor(2,3),Pack(0,72,3)),W(185,Pack(1,64),Pack(2,64,5),Armor(0,3,8))),
                S(4,"상가 순환로",24,new[]{0},
                    Map("shopping-arcade-return-loop",P(-23,5,-16,5,-16,-5,-6,-5,9,-5,9,5,-5,5,-5,0,0,0),None,None,None),
                    "긴 한 길을 두 번 노리는 배치.\n클리어 후 탄막 조합 해금.","순환로 안쪽의 사거리를 확인하세요. 빠른 적 뒤의 장갑 적을 처리할 화력이 필요합니다.",
                    "탄막 조합 해금! 산탄 Lv2 + 붕괴 Lv2를 합치세요.",
                    W(5,Pack(0,88)),W(35,Fast(0,24),Pack(0,64,5)),W(65,Armor(0,3),Pack(0,96,4)),
                    W(95,Pack(0,104),Fast(0,20,9)),W(125,Armor(0,3,0,true),Pack(0,88,4)),
                    W(155,Fast(0,24),Pack(0,96,5)),W(185,Armor(0,4),Pack(0,104,4))),
                S(5,"냉장 물류로",26,new[]{0,1,2},
                    Map("cold-logistics-visible-north",P(-23,-5,-17,-5,-17,3,-8,3,-8,-2,-3,-2,0,0),P(5,11,5,6,11,6,11,1,3,1,0,0),P(23,-5,16,-5,16,3,8,3,3,1,0,0),None),
                    "4웨이브부터 북쪽 문이 열립니다.\n탄막을 만들고 북쪽 보강도 남겨두세요.","북쪽 붕괴를 남겨 공성에 대비하세요. 서쪽 산탄은 다른 붕괴와 합치고 빈자리를 보충하세요.",
                    "상위 조합 해금! 탄막 Lv2 + 봉인 Lv2로 종결을 만들 수 있습니다.",
                    W(5,Pack(0,72)),W(35,Pack(2,80),Fast(0,12,5)),W(65,Pack(0,72),Armor(2,3,6)),
                    W(95,Pack(1,56),Armor(1,2,5,true),Pack(2,40,7)),W(125,Pack(2,80),Fast(1,18,4)),
                    W(155,Armor(0,3),Pack(0,72,4)),W(185,Pack(1,72),Pack(2,56,5)),
                    W(215,Pack(0,64),Armor(1,3,3,true),Pack(2,64,6))),
                S(6,"시장 시계광장",28,new[]{0,1,2},
                    Map("market-clock-forward-rear-courts",P(-23,5,-17,5,-17,-4,-9,-4,-9,2,-3,2,0,0),P(8,11,8,7,-5,7,-5,2,-3,2,0,0),P(23,-4,17,-4,17,5,10,5,10,-3,4,-3,4,2,-3,2,0,0),None),
                    "전방 화력과 후방 합류목을 나눠 지키세요.\n8웨이브 · 보스 없는 조합 점검전.","120초 이후 북쪽 합류목에 종결을 시도하세요. 다른 전선의 빈자리를 먼저 보충하세요.",
                    "시장 시계광장을 되찾았습니다. 2-7 이후 작전은 다음 업데이트에서 이어집니다.",
                    W(5,Pack(0,80)),W(35,Pack(1,72),Fast(2,16,5)),W(65,Pack(2,88),Armor(0,3,6)),
                    W(95,Pack(0,64),Pack(1,64,6)),W(125,Armor(2,3,0,true),Pack(2,88,5)),
                    W(155,Fast(1,24),Pack(0,80,4)),W(185,Pack(1,80),Pack(2,72,5)),
                    W(215,Armor(0,4),Pack(0,64,3),Pack(1,64,6),Pack(2,64,9)))
            };
        }
    }
}
