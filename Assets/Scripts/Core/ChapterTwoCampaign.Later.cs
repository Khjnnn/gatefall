namespace Gatefall.Core
{
    public static partial class ChapterTwoCampaign
    {
        static CampaignStage Market(int local, string name, int sites, int[] lanes, BattlefieldLayout map,
            int reward, float threat, string briefing, string lesson, string clear, params CampaignWave[] waves)
        {
            CampaignStage stage = S(local,name,sites,lanes,map,briefing,lesson,clear,waves);
            stage.FirstReward = reward;
            stage.Threat = threat;
            stage.RecommendedGrowth = local >= 13 ? 6 : 5;
            stage.RecommendedResearch[0] = local >= 9 ? 4 : 3;
            return stage;
        }

        static CampaignStage[] Later()
        {
            var gate = Market(22,"굶주림의 탑",28,new[]{0,1,2},
                Map("hunger-tower-flanking-retinues",
                    P(-23,6,-13,6,-13,-6,-5,-6,-5,3,0,3,0,0),P(0,11,0,0),
                    P(23,-6,13,-6,13,6,5,6,5,-3,0,-3,0,0),None),
                52,1.225f,"7파 북쪽 문지기 · 양옆 근위대가 뒤따릅니다.",
                "북쪽에 집중 화력을 두세요. 문지기를 쓰러뜨린 뒤에도 양옆을 지키세요.",
                "굶주림의 탑 봉인. 회수한 운송표의 목적지인 라온역이 열렸습니다.",
                W(5,Pack(0,72)),W(35,Pack(2,72,0,12)),W(65,Pack(0,60,0,12),Armor(2,3,5)),
                W(95,Pack(2,64,0,12),G(0,EnemyKind.Runner,18,6,8)),W(125,Pack(0,72),Armor(2,3,5,true)),
                W(155,Pack(2,64),G(0,EnemyKind.Runner,22,4,8)),
                W(185,G(1,EnemyKind.Boss,1,0,0),Pack(0,64,4,12)),
                W(215,Pack(2,72),Armor(0,3,6,true)),W(245,Pack(0,64),Pack(2,64,4,12)));
            gate.BossRequired = true;
            gate.BossEntryDistance = 0;
            gate.BossName = "집하관 — 끝없는 입";
            gate.BossVictoryTitle = "굶주림의 탑 봉인";
            gate.Story = "세희: 운송표가 전부 라온역을 가리켜. 먼저 여기서 집하관을 막자.";
            return new[] {
                Market(7,"새벽 배송길",24,new[]{0,2},
                    Map("dawn-delivery-short-east",P(-23,4,-18,4,-18,-4,-10,-4,-10,2,-4,2,0,0),
                        None,P(22,-3,15,-3,15,-1,4,-1,0,0),None),34,1.18f,
                    "서·동의 작은 무리가 교대로 옵니다.","짧은 동쪽에 창을 먼저 두세요. 서쪽은 다음 보급까지 나눠 강화하세요.",
                    "새벽 배송길을 열었습니다. 중앙 천막 앞에서 장갑 무리가 움직입니다.",
                    W(5,Pack(0,58,0,14)),W(35,Pack(2,50,0,13),Fast(2,8,5)),
                    W(65,Pack(0,64),Armor(0,2,7)),W(95,Pack(2,60),Fast(0,10,7)),
                    W(125,Pack(0,64),Pack(2,34,8)),W(155,Armor(2,2,0,true),Pack(0,66,4)),
                    W(185,Pack(2,64),Pack(0,50,5))),
                Market(8,"천막 중앙로",26,new[]{0,2},
                    Map("canopy-armor-before-neck",P(-23,-6,-16,-6,-16,-2,-8,-2,-8,4,-2,4,-2,0,0,0),
                        None,P(23,5,17,5,17,-4,10,-4,10,4,-2,4,-2,0,0,0),None),40,1.27f,
                    "합류 전에 양쪽에서 장갑 적이 나옵니다.","합류 전에 장갑 적의 체력을 깎으세요. 양쪽에 하나씩 전방 포대를 남기세요.",
                    "천막 중앙로 확보. 결계 연구 4단계가 열렸습니다.",
                    W(5,Pack(0,68)),W(35,Pack(2,68),Armor(0,2,8)),W(65,Armor(2,3,0),Pack(0,56,6)),
                    W(95,Pack(2,82),Armor(0,3,3)),W(125,Armor(2,3,0,true),Pack(0,66,5)),
                    W(155,Pack(0,65),Pack(2,65,5)),W(185,Armor(0,3,0,true),Armor(2,3,5),Pack(2,68,6))),
                Market(9,"배급 교차로",26,new[]{0,1,2},
                    Map("rations-counterflow",P(-23,6,-17,6,-17,-4,-8,-4,-8,1,-3,1,0,0),
                        P(-1,11,-1,7,7,7,7,2,2,2,0,0),P(23,-5,14,-5,14,-1,4,-1,0,0),None),36,1.25f,
                    "느린 서쪽 정예 뒤 동쪽 속공이 옵니다.","서쪽 정예를 늦추고 동쪽 속공에는 창을 남기세요.",
                    "배급 교차로의 물류가 다시 움직입니다.",
                    W(5,Pack(0,72)),W(35,Pack(1,68)),W(65,Armor(0,4,0),Pack(0,60,5),Fast(2,18,11)),
                    W(95,Pack(1,76),Fast(2,16,8)),W(125,Armor(0,3,0,true),Pack(2,60,6)),
                    W(155,Pack(0,64),Fast(1,22,8)),W(185,Pack(1,68),Pack(2,62,5),Armor(0,3,6))),
                Market(10,"폐점 순환길",26,new[]{2},
                    Map("closed-shops-east-loop",None,None,
                        P(23,6,17,6,17,-5,4,-5,-10,-5,-10,5,1,5,1,0,0,0),None),38,1.27f,
                    "긴 고리에서 군집과 공성의 도착이 갈립니다.","안쪽 포대는 두 구간을 노립니다. 마지막 공성을 맡을 창을 출구에 남기세요.",
                    "순환로가 열렸습니다. 남쪽 하역문의 움직임을 조사하세요.",
                    W(5,Pack(2,70)),W(35,Pack(2,82),Fast(2,18,6)),W(65,Armor(2,3,0,true),Pack(2,64,11)),
                    W(95,Pack(2,92)),W(125,Armor(2,3,0,true),Fast(2,18,12)),
                    W(155,Pack(2,104),Armor(2,3,7)),W(185,Pack(2,88),Fast(2,24,4)),
                    W(215,Pack(2,76),Armor(2,3,0,true))),
                Market(11,"상인 뒷문",26,new[]{0,2,3},
                    Map("merchant-late-south-door",P(-23,3,-15,3,-15,6,-7,6,-7,0,0,0),
                        None,P(23,5,15,5,15,-3,9,-3,9,2,3,2,0,0),
                        P(-8,-11,-8,-6,-3,-6,-3,-2,0,0)),38,1.27f,
                    "4파까지 동쪽 주력 · 5파 남쪽 뒷문 개방.","남쪽 속공이 오기 전에 빈 지점과 보강 마석을 남겨 두세요.",
                    "상인 뒷문을 확보했습니다. 검문선의 두 하역로를 지켜야 합니다.",
                    W(5,Pack(2,72)),W(35,Pack(0,62),Pack(2,36,5)),W(65,Pack(2,80),Armor(2,3,4)),
                    W(95,Armor(2,3,0,true),Pack(0,68,4)),W(125,Fast(3,24,0),Pack(2,64,6)),
                    W(155,Pack(3,64),Fast(0,18,8)),W(185,Armor(2,4,0,true),Pack(2,64,6)),
                    W(215,Pack(0,72),Fast(3,24,4),Pack(2,56,6))),
                Market(12,"집하장 검문선",28,new[]{0,1,2},
                    Map("loading-checkpoint-staggered-armor",P(-23,-4,-17,-4,-17,4,-9,4,-9,-1,-3,-1,0,0),
                        P(3,11,3,7,12,7,12,1,5,1,0,0),P(23,-5,16,-5,16,3,9,3,5,1,0,0),None),44,1.32f,
                    "서쪽 정예와 북쪽 공성이 시간차로 진입합니다.","합친 자리의 화력과 비운 자리를 함께 보세요. 공성 전에 빈 곳을 보강하세요.",
                    "검문선을 되찾았습니다. 세희와 백도하의 추가 성장이 열렸습니다.",
                    W(5,Pack(0,76)),W(35,Pack(2,70),Armor(0,2,7)),W(65,Pack(0,76),Armor(1,3,6)),
                    W(95,Armor(0,4,0),Pack(2,74,7)),W(125,Armor(1,3,0,true),Pack(1,64,5)),
                    W(155,Pack(0,80),Fast(2,22,8)),W(185,Pack(2,74),Armor(0,4,6,true)),
                    W(215,Pack(1,80),Pack(0,62,7)),W(245,Armor(0,4,0),Armor(1,4,5,true),Pack(2,70,5))),
                Market(13,"재개장 거리",24,new[]{0,3},
                    Map("reopening-short-confluence",P(-23,5,-16,5,-16,0,-10,0,-10,-4,-5,-4,-2,-1,0,0),
                        None,None,P(12,-11,12,-6,7,-6,7,2,1,2,1,-4,-5,-4,-2,-1,0,0)),34,1.18f,
                    "두 길이 결계 앞에서 합류합니다.","합류목 붕괴를 먼저 강화하고 앞쪽에는 선두를 처리할 창을 두세요.",
                    "다솜동 가게가 하나씩 문을 열고 있습니다.",
                    W(5,Pack(0,66)),W(35,Pack(3,60)),W(65,Pack(0,74),Fast(3,12,5)),W(95,Pack(3,72)),
                    W(125,Pack(0,62),Armor(3,2,5)),W(155,Pack(3,72),Pack(0,60,6)),W(185,Pack(0,68),Armor(3,2,5,true))),
                Market(14,"동서 물류광장",28,new[]{0,1,2,3},
                    Map("freight-four-entry-alternating",P(-23,2,-16,2,-16,-3,-8,-3,-4,-1,0,0),
                        P(-6,11,-6,6,-2,6,-2,2,0,0),P(23,-4,16,-4,16,2,7,2,3,0,0,0),
                        P(9,-11,9,-6,4,-6,4,-2,0,0)),40,1.30f,
                    "서·동과 북·남이 번갈아 압박합니다.","다음 두 입구를 확인하세요. 주력 한 곳을 강화하되 다른 길에 창을 남기세요.",
                    "물류광장을 지켰습니다. 골목 안에 남은 장갑 정예를 수색합니다.",
                    W(5,Pack(0,54),Pack(2,44,5)),W(35,Pack(1,58),Fast(3,16,5)),
                    W(65,Pack(2,58),Armor(0,3,4)),W(95,Pack(3,66),Pack(1,52,4)),
                    W(125,Armor(2,3,0,true),Pack(0,72,5)),W(155,Pack(1,66),Fast(3,22,7)),
                    W(185,Pack(0,66),Pack(2,65,5)),W(215,Armor(3,3,0,true),Pack(1,74,5),Pack(3,48,8))),
                Market(15,"포식자 골목",24,new[]{0,1},
                    Map("predator-elite-alley",P(-23,-4,-17,-4,-17,4,-8,4,-8,-1,-4,-1,0,0),
                        P(10,11,10,7,16,7,16,-3,8,-3,5,1,0,0),None,None),40,1.34f,
                    "군집은 적지만 장갑 정예가 연달아 진입합니다.","표식이나 조합으로 한 강적에 집중하세요. 군집용 범위 공격은 보조로 두세요.",
                    "골목을 청소했습니다. 냉동 보관동에서 공성 소리가 들립니다.",
                    W(5,Pack(0,46),Armor(0,3,4)),W(35,Armor(1,4,0),Pack(1,46,6)),
                    W(65,Armor(0,5,0),Pack(0,46,6)),W(95,Armor(1,3,0,true),Fast(0,16,8)),
                    W(125,Armor(0,5,0),Armor(1,3,7)),W(155,Pack(1,52),Armor(0,4,6,true)),
                    W(185,Armor(1,5,0),Pack(0,54,6)),W(215,Armor(0,4,0),Armor(1,4,5,true),Pack(1,44,6))),
                Market(16,"냉동 보관동",28,new[]{0,2},
                    Map("freezer-crossing-siege",P(-23,5,-17,5,-17,-5,-10,-5,-10,1,-4,1,0,0),
                        None,P(23,4,18,4,18,6,9,6,9,-4,3,-4,0,0),None),40,1.31f,
                    "밀집 무리 뒤 반대쪽 공성이 들어옵니다.","봉인으로 군집을 묶으세요. 공성에 쓸 화력은 반대편에 따로 남기세요.",
                    "냉동 보관동의 전력이 돌아왔습니다.",
                    W(5,Pack(0,78)),W(35,Pack(2,78,0,10)),W(65,Pack(0,94,0,10),Armor(2,2,8,true)),
                    W(95,Pack(2,92,0,9),Armor(0,3,9,true)),W(125,Pack(0,88,0,10),Fast(2,22,8)),
                    W(155,Pack(2,84,0,10),Armor(0,3,9,true)),W(185,Pack(0,86,0,10),Armor(2,3,9,true)),
                    W(215,Pack(2,88,0,10),Armor(0,4,8,true))),
                Market(17,"빈 장터 외곽",24,new[]{3},
                    Map("empty-market-south-double-pass",None,None,None,
                        P(-11,-11,-11,-5,-2,-5,12,-5,12,5,3,5,-4,5,-4,0,0,0)),38,1.27f,
                    "남쪽 고리를 빠른 적이 연달아 돌아옵니다.","고리 안쪽에 봉인을 배치하세요. 같은 포대가 앞뒤 구간을 다시 노릴 수 있습니다.",
                    "빈 장터 외곽을 정리했습니다. 탑 아래 배급선에 접근합니다.",
                    W(5,Pack(3,74)),W(35,Fast(3,22,0),Pack(3,58,5)),W(65,Fast(3,24,0),Pack(3,66,4)),
                    W(95,Armor(3,3,0),Fast(3,24,7)),W(125,Pack(3,92),Fast(3,18,9)),
                    W(155,Armor(3,3,0,true),Pack(3,74,6)),W(185,Pack(3,86),Fast(3,26,5))),
                Market(18,"탑 아래 배급선",28,new[]{0,1,2},
                    Map("tower-rations-north-siege",P(-23,4,-17,4,-17,-4,-9,-4,-9,2,-4,2,0,0),
                        P(1,11,1,6,-3,6,-3,2,0,0),P(23,-4,16,-4,16,3,8,3,3,1,0,0),None),46,1.32f,
                    "2~4파 북쪽 공성 · 5파 첫 서쪽 무리.","초반에는 북쪽 공성로에 창을 더 세우세요. 5파 전에 서쪽 건설비를 남기세요.",
                    "유채린: 배급 장부에 적힌 수량과 실제 하역 기록이 맞지 않아요.",
                    W(5,Pack(2,72)),W(35,Pack(1,66),Armor(1,2,7,true)),W(65,Pack(2,76),Armor(1,3,6,true)),
                    W(95,Armor(1,4,0,true),Pack(2,74,6)),W(125,Pack(0,84),Fast(0,16,5)),
                    W(155,Pack(0,82),Armor(1,3,6,true)),W(185,Pack(2,80),Fast(0,24,6)),
                    W(215,Armor(1,4,0,true),Pack(0,92,6))),
                Market(19,"붉은 차양길",24,new[]{0,2},
                    Map("red-canopy-staging",P(-23,4,-17,4,-17,-4,-7,-4,-3,-1,0,0),
                        None,P(23,-4,15,-4,15,4,7,4,3,1,0,0),None),36,1.20f,
                    "작은 선두 뒤 밀집한 후속대가 붙습니다.","창을 먼저 강화하고 뒤에 올 밀집 무리용 조합 재료를 준비하세요.",
                    "차양길을 정비했습니다. 집하장 안팎의 두 전선을 대비하세요.",
                    W(5,Pack(0,50)),W(35,Pack(2,48)),W(65,Fast(0,16,0),Pack(0,68,6)),
                    W(95,Fast(2,18,0),Pack(2,72,6)),W(125,Armor(0,3,0),Pack(0,74,5)),
                    W(155,Pack(0,72),Armor(2,2,5,true)),W(185,Pack(2,78),Pack(0,58,6))),
                Market(20,"마지막 집하장",28,new[]{0,1,2},
                    Map("last-yard-front-and-rear",P(-23,6,-18,6,-18,-5,-11,-5,-11,0,-5,0,0,0),
                        P(8,11,8,7,-6,7,-6,3,-5,0,0,0),P(23,-5,16,-5,16,4,8,4,8,-2,2,-2,0,0),None),48,1.34f,
                    "전방 군집과 후방 합류가 세 차례 압박합니다.","종결은 합류목에 두고 재료가 빠진 전선을 메우세요. 강한 두 기를 유지해도 됩니다.",
                    "집하장을 확보했습니다. 탑 앞 하역장이 마지막 방어선입니다.",
                    W(5,Pack(0,76)),W(35,Pack(2,76),Armor(0,3,6)),W(65,Pack(1,76),Fast(2,18,6)),
                    W(95,Pack(0,76),Pack(1,52,7)),W(125,Armor(2,4,0,true),Pack(2,72,6)),
                    W(155,Pack(0,80),Pack(1,66,5)),W(185,Armor(0,4,0,true),Fast(1,24,7)),
                    W(215,Pack(2,88),Pack(0,60,7)),W(245,Pack(1,76),Armor(2,4,5,true),Pack(0,56,7))),
                Market(21,"탑 전면 하역장",28,new[]{0,1,2},
                    Map("tower-loading-north-and-east",P(-23,-4,-16,-4,-16,2,-9,2,-9,-1,-3,-1,0,0),
                        P(-1,11,-1,7,6,7,6,2,2,2,0,0),P(23,-5,16,-5,16,-1,7,-1,3,1,0,0),None),44,1.34f,
                    "북쪽 공성과 동쪽 속공이 엇갈립니다.","조합 전에 동쪽 보충 자리를 확인하세요. 북쪽 공성은 남은 집중 포대로 막으세요.",
                    "전면 하역장 확보. 다음 작전에서 굶주림의 탑을 봉인합니다.",
                    W(5,Pack(0,76)),W(35,Pack(1,66),Armor(1,2,6,true)),W(65,Pack(2,72),Fast(2,18,6)),
                    W(95,Armor(1,3,0,true),Pack(0,76,4)),W(125,Fast(2,26,0),Pack(2,74,6)),
                    W(155,Armor(1,4,0,true),Pack(1,64,5)),W(185,Pack(0,86),Fast(2,26,8)),
                    W(215,Armor(1,4,0,true),Pack(0,62,5),Fast(2,26,9))),
                gate
            };
        }

        internal static CampaignStage RailEntry(int index)
        {
            var stage = S(1,"역 앞 구조로",24,new[]{0,3},
                Map("rail-station-rescue-crossing",P(-23,4,-18,4,-18,-1,-4,-1,-4,5,4,5,4,1,0,0),
                    None,None,P(11,-11,11,-6,5,-6,2,-6,2,-2,0,0)),
                "긴 서쪽 선로 · 3파 남쪽 횡단로 속공.",
                "남쪽에 창을 한 기 먼저 두세요. 이후 서쪽 선로의 포대를 강화하세요.",
                "역 앞 구조로를 확보했습니다. 화물 승강장으로 이동하세요.",
                W(5,Pack(0,60)),W(35,Pack(0,72)),W(65,Fast(3,18,0),Pack(0,58,5)),
                W(95,Pack(0,78),Armor(0,3,7)),W(125,Pack(3,58),Armor(0,2,6,true)),
                W(155,Pack(0,80),Fast(3,16,6)),W(185,Pack(0,72),Armor(0,3,5),Fast(3,18,7)));
            stage.Index = index;
            stage.Chapter = 3;
            stage.StableId = "C03-S001";
            stage.PredecessorId = "C02-S022";
            stage.Story = "세희: 운송표의 마지막 도장은 여기야. 남쪽 횡단로부터 안전하게 열자.";
            stage.FirstReward = 38;
            stage.RecommendedGrowth = 6;
            stage.RecommendedResearch[0] = 4;
            stage.Layout.Theme = 1;
            stage.Threat = 1.225f;
            return stage;
        }
    }
}
