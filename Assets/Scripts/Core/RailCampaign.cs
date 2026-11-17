namespace Gatefall.Core
{
    public static partial class ChapterTwoCampaign
    {
        static CampaignStage Rail(int index, int number, string name, int sites, int[] lanes, BattlefieldLayout map,
            int reward, float threat, string story, string briefing, string lesson, string clear, params CampaignWave[] waves)
        {
            var stage=S(number,name,sites,lanes,map,briefing,lesson,clear,waves);
            stage.Index=index;
            stage.Chapter=3;
            stage.StableId="C03-S"+number.ToString("000");
            stage.PredecessorId="C03-S"+(number-1).ToString("000");
            stage.Layout.Theme=1;
            stage.Story=story;
            stage.FirstReward=reward;
            stage.Threat=threat;
            stage.RecommendedGrowth=6;
            stage.RecommendedResearch[0]=4;
            if(number==6)stage.Layout.Sockets=P(-20.3f,2,-4.4f,-3,6.2f,-3.6f,16,1.6f);
            return stage;
        }

        // Threat scales runners, elites and siege (not the swarm). 3-3~3-8 were tuned on 2026-11-12 with
        // Tools/review-rail-choices.ps1: at 1.17~1.28 fourteen Lv1 spears earned three stars almost everywhere.
        // Single-road boards (3-4, 3-8) concentrate every tower on one lane and need the largest values.
        internal static CampaignStage[] RailContinuation(int index) => new[] {
            Rail(index++,2,"화물 승강장",24,new[]{0},
                Map("rail-freight-platform",P(-23,3,-5,3,-1,6,8,6,8,-4,-4,-4,-4,-1,0,0),None,None,None),
                40,1.17f,"세희: 적재선이 트였어. 화물차 뒤에서 몰려오는 것부터 막자.",
                "4파 서쪽 긴 군집 · 6파 장갑 정예.","직선 끝에 군집 화력을 두세요. 6파 전에는 장갑을 뚫을 붕괴를 보강하세요.",
                "관통 결계주 해금! 편성에서 선택해 선로에 늘어선 적을 노리세요.",
                W(5,Pack(0,68)),W(35,Pack(0,72),Fast(0,12,6)),W(65,Pack(0,80)),W(95,Pack(0,112,0,13)),
                W(125,Fast(0,22),Pack(0,72,6)),W(155,Armor(0,5),Pack(0,68,6)),W(185,Pack(0,88),Armor(0,3,7,true))),
            Rail(index++,3,"복선 정비로",26,new[]{0,1},
                Map("rail-parallel-maintenance",P(-23,-4,-6,-4,-6,-8,6,-8,6,-3,0,0),
                    P(-13,11,-13,5,5,5,5,1,0,0),None,None),
                40,2.2f,"유채린: 두 선로의 신호가 번갈아 바뀌어요.",
                "4파 서쪽 집중 · 5파 북쪽 집중.","4파 전에 서쪽 직선을 지키세요. 북쪽에도 5파를 맡을 포대를 남기세요.",
                "복선 정비로 확보. 다음 선로는 크게 굽어 있습니다.",
                W(5,Pack(0,68)),W(35,Pack(1,68)),W(65,Pack(0,58),Fast(1,16,6)),
                W(95,Pack(0,104,0,12),Armor(0,2,7)),W(125,Pack(1,104,0,12),Fast(1,16,7)),
                W(155,Armor(1,3,0,true),Pack(0,78,5)),W(185,Pack(0,60),Pack(1,64,5))),
            Rail(index++,4,"선로 굴곡부",24,new[]{2},
                Map("rail-east-s-curve",None,None,P(23,-5,15,-5,15,6,5,6,5,-6,-6,-6,-6,0,0,0),None),
                38,3f,"세희: 급하게 꺾이는 구간이야. 한 자리에서 두 굽이를 볼 수 있어.",
                "4파 동쪽 군집이 짧은 간격으로 진입합니다.","굽이 안쪽에 붕괴나 산탄을 두세요. 뒤따르는 장갑 적에는 집중 화력을 남기세요.",
                "쇄도 조합 해금! 관통 Lv2와 봉인 Lv2를 조합하세요.",
                W(5,Pack(2,78)),W(35,Fast(2,20),Pack(2,60,5)),W(65,Armor(2,3),Pack(2,78,5)),
                W(95,Pack(2,104,0,7)),W(125,Fast(2,20),Pack(2,70,4)),W(155,Armor(2,4,0,true),Pack(2,72,5)),
                W(185,Pack(2,88),Armor(2,3,7))),
            Rail(index++,5,"승강장 교차점",26,new[]{0,1},
                Map("rail-platform-crossing",P(-23,-4,-6,-4,5,-4,5,-8,11,-8,11,-2,0,0),
                    P(-1,11,-3,7,3,5,5,3,0,0),None,None),
                42,1.85f,"세희: 위쪽 경사로가 짧아. 선로에 집중하면 북쪽이 비어.",
                "4파 서쪽 군집 · 6파 북쪽 장갑.","쇄도를 쓴다면 서쪽 사격선에 두세요. 북쪽 경사로는 붕괴와 창으로 따로 막으세요.",
                "종결 경로 해금! 쇄도 Lv2와 붕괴 Lv2를 조합하세요.",
                W(5,Pack(0,68)),W(35,Pack(1,56),Fast(0,12,7)),W(65,Pack(0,78)),W(95,Pack(0,105,0,11)),
                W(125,Pack(1,66),Fast(0,18,5)),W(155,Armor(1,4,0,true),Pack(0,58,5)),
                W(185,Pack(0,68),Pack(1,56,6)),W(215,Armor(0,3),Pack(1,74,5))),
            Rail(index++,6,"환승 앞 분기장",28,new[]{0,1,2},
                Map("rail-transfer-junction",P(-23,6,-18,6,-18,2,-2,2,-2,-5,3,-5,3,0,0,0),
                    P(6,11,6,8,14,8,14,3,5,3,5,-1,0,-1,0,0),P(21,-1,0,-1,0,0),None),
                48,1.35f,"유채린: 동쪽 비상로가 바로 결계로 이어져요. 보급은 세 방향에 나눠 주세요.",
                "4파 서쪽 선로와 동쪽 비상로가 동시에 압박합니다.","서쪽은 직선 화력을 유지하세요. 종결로 합칠 때는 동쪽 빈자리를 바로 채우세요.",
                "환승 앞 분기장을 확보했습니다. 역무원 통로부터 정리하세요.",
                W(5,Pack(0,72)),W(35,Pack(0,60,0,12),Fast(2,14,5)),W(65,Pack(1,60,0,12),Armor(0,3,6),Pack(2,26,4,9)),
                W(95,Pack(0,72),Fast(2,18,7),Armor(1,2,4,true)),W(125,Pack(1,68),Armor(2,3,3,true),Fast(0,16,6)),
                W(155,Pack(2,48,0,12),Pack(0,60,3,14),Armor(1,3,4)),W(185,Pack(1,62,0,12),Pack(0,48,5,12),Fast(2,16,8)),
                W(215,Pack(0,72),Pack(1,56,5,12),Armor(2,3,6,true))),
            Rail(index++,7,"역무원 통로",24,new[]{0,3},
                Map("rail-staff-south-crossing",P(-23,1,-17,1,-13,-3,-7,-3,-4,1,-7,5,-3,5,0,0),None,None,P(6,-11,6,-5,0,-5,0,0)),
                38,2f,"세희: 직원 통로에 손전등이 보여. 남쪽 횡단로부터 안전하게 만들자.",
                "3파 남쪽 빠른 적 · 5파 서쪽 느린 무리.","남쪽 횡단로에 창을 먼저 놓으세요. 5파 전에 서쪽 외곽 화력을 늘리세요.",
                "역무원 통로 확보. 야간 정비고가 열렸습니다.",
                W(5,Pack(0,68)),W(35,Pack(0,72)),W(65,Fast(3,24),Pack(0,56,5)),W(95,Pack(3,64),Armor(0,2,7)),
                W(125,Armor(0,4,0,true),Pack(0,74,6)),W(155,Fast(3,24),Pack(0,76,6)),W(185,Pack(0,64),Pack(3,62,4))),
            Rail(index,8,"야간 정비고",26,new[]{1},
                Map("rail-night-depot",None,P(-10,11,-10,3,12,3,12,-6,-7,-6,-7,-1,0,0),None,None),
                46,2.8f,"유채린: 정비고 신호를 다시 켜면 선로 깊은 곳의 흔적을 따라갈 수 있어요.",
                "5파 북쪽 공성 뒤로 밀집 무리가 이어집니다.","공성은 장갑을 뚫는 화력으로 끊으세요. 뒤쪽 군집을 노릴 범위 포대를 따로 두세요.",
                "야간 정비고 확보. 각성자 성장 단계가 하나씩 열렸습니다.",
                W(5,Pack(1,72)),W(35,Pack(1,84),Fast(1,12,7)),W(65,Armor(1,3),Pack(1,80,5)),W(95,Pack(1,104,0,12)),
                W(125,Armor(1,5,0,true),Pack(1,94,10,11)),W(155,Fast(1,22),Pack(1,78,6)),
                W(185,Armor(1,4),Pack(1,88,6)),W(215,Armor(1,3,0,true),Pack(1,100,7)))
        };
    }
}
