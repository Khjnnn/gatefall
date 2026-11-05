using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Gatefall.Core
{
    public sealed class CampaignStage
    {
        public int Index, SocketCount, FirstReward;
        public string Name, Briefing, Story, ClearStory, Lesson;
        public int[] Lanes, Counts;
        public bool Boss { get { return Index == 5; } }
        public bool HasAdjacency { get { return Index >= 1; } }
        public bool HasCatalysts { get { return Index >= 2; } }
        public bool HasAdvancedTowers { get { return Index >= 10; } }
        // Relative to the original pre-swarm campaign budget (v0.12.1).
        public int SwarmMultiplier { get { return Index < 3 ? 5 : Index < 9 ? 6 : 8; } }
        public int StartingCreditBonus { get { return Index < 3 ? 100 : Index < 9 ? 140 : 180; } }
        public int SupplyCredits { get { return Index < 3 ? 80 : 100; } }
        public string CityBriefing(CampaignProfile profile)
        {
            if (Index != 13 || !profile.FirstCleared[Index]) return Briefing;
            const int observerMaterials = (1 << (int)TowerKind.Marker) | (1 << (int)TowerKind.Autocannon);
            string instruction = (profile.Loadout & observerMaterials) == observerMaterials
                ? "관측창 조합 가능 · 표식 Lv2 + 창 Lv2"
                : "관측창 해금됨 · 편성에 표식과 창을 넣으세요.";
            return CampaignCatalog.ObserverRoutes + "\n" + instruction;
        }
        public string DeploymentLesson(CampaignProfile profile)
        {
            const int traceMaterials = (1 << (int)TowerKind.Marker) | (1 << (int)TowerKind.Binding);
            if (Index == 10 && (!profile.FirstCleared[9] || (profile.Loadout & traceMaterials) != traceMaterials))
                return "4웨이브에는 동남쪽을 먼저 보강하세요. 추적을 쓰려면 편성에 표식과 봉인을 넣으세요.";
            return Lesson;
        }
        public bool Allows(TowerKind kind)
        {
            return kind == TowerKind.Autocannon || (kind == TowerKind.Binding && Index >= 1) ||
                (kind == TowerKind.Rupture && Index >= 3) || (kind == TowerKind.Barrier && Index >= 4) ||
                (kind == TowerKind.Arc && Index >= 2) ||
                ((kind == TowerKind.Shatter || kind == TowerKind.Gravity) && Index >= 3) ||
                (kind == TowerKind.Seal && Index >= 4) ||
                ((kind == TowerKind.Marker || kind == TowerKind.Observer) && Index >= 6) ||
                (kind == TowerKind.Beacon && HasAdvancedTowers);
        }
    }
    public sealed class CampaignBattle
    {
        public CampaignStage Stage;
        public int Hunter;
        public int EarnedGrowth;
        public int[] Research = new int[4];
        public int Loadout = 135;
        public bool MarkerUnlocked, ObserverUnlocked;
        public bool Crisis;
        public bool MobileWaveCalls { get { return Stage!=null && Stage.Index>=3 && !Crisis; } }
        public bool Lab { get { return Stage == null; } }
        public string VictorySummary()
        {
            if (Stage == null) return "";
            if (Crisis) return "위기 작전 별점에 기록됩니다.";
            // These flags describe what was unlocked when this run started.
            if (Stage.Index == 9 && MarkerUnlocked) return "네 방향에서 밀려온 범람을 다시 막았습니다.";
            if (Stage.Index == 13 && ObserverUnlocked) return "북쪽 지름길과 서·동 우회로를 다시 지켰습니다.";
            return Stage.ClearStory.Split('\n')[0];
        }
        public CampaignBattle Copy()
        { return new CampaignBattle { Stage = Stage, Hunter = Hunter, EarnedGrowth = EarnedGrowth, Research = (int[])Research.Clone(), Crisis = Crisis, Loadout=Loadout, MarkerUnlocked=MarkerUnlocked, ObserverUnlocked=ObserverUnlocked }; }
    }
    public static class CampaignCatalog
    {
        public static readonly string[] Chapters = { "잿빛 탑", "굶주림의 탑", "심연의 탑", "범람의 탑", "녹슨 탑", "하늘의 탑", "왕좌의 탑" };
        public static readonly string[] Districts = { "종현동 구도심", "다솜동 새벽시장", "라온역 환승센터", "라온강 나루·항만", "한울 산업단지", "누리 마천루지구", "시청·중앙광장" };
        public static readonly string[] Hunters = { "강윤", "세희", "백도하" };
        public static readonly string[] HunterRoles = { "미분류 귀환자 · 균형 사격 / 회피", "C급 정찰병 · 빠른 이동 / 질주", "B급 대장 · 근접 방호 / 방패" };
        public static readonly string[] Grades = { "F", "E", "D", "C", "B", "A", "S" };
        public static readonly int[] HunterStart = { 0, 3, 4 }, GrowthCosts = { 4, 6, 8, 12, 18, 26 };
        // Legacy long-campaign reference; current purchases use implemented operation milestones.
        public static readonly int[] GradeChapter = { 1, 1, 1, 2, 3, 5, 7 };
        public static readonly int[] ResearchCosts = { 2, 4, 6 };
        public static readonly string[] ResearchNames = { "창 · 피해", "봉인 · 감속 지속", "붕괴 · 폭발 반경", "방벽 · 재충전" };
        public static readonly string[] ResearchEffects = { "+4 / 8 / 12%", "+0.05 / 0.10 / 0.15초", "+0.05 / 0.10 / 0.15", "시간 -3 / 6 / 9%" };
        internal const string ObserverRoutes = "북쪽 지름길과 서·동 우회로를 막으세요.";
        public const string Prologue = "낙하 7주년. 잿빛 탑의 범람 속에서 한 남자가 걸어 나왔다.\n7년 전 실종자, 강윤. 측정기는 그의 등급을 읽지 못했다.\n백도하: 신원 확인은 나중이다. 우선 뒤쪽 사람들부터 지켜.";
        public static readonly CampaignStage[] Stages = {
            S(0,"종현 사거리",14,new[]{0},new[]{20,35,50,65,75,85},
              "서쪽 한 방향 방어.\n후반 빠른 마수에 대비해 창을 보강하세요.",
              "유채린: 파란 표식에 창 결계주를 세워요. 마석은 제가 공급할게요.",
              "백도하: 대피로 확보. 이제 봉인 결계주와 공방을 쓸 수 있다.",
              "서쪽 길목에 창을 세우고 강화하세요. 마석이 쌓이면 추가 배치도 시도해 보세요."),
            S(1,"구도심 지하보도",18,new[]{0,2},new[]{30,50,70,90,100,120},
              "서·동에서 번갈아 몰려옵니다.\n4·6웨이브 전에 동쪽을 보강하세요.",
              "세희: 지하보도 양쪽이 뚫렸어. 한쪽에만 몰리면 반대쪽을 놓쳐!",
              "유채린: 두 결계주를 하나로 묶는 공명 융합을 준비했어요.",
              "봉인으로 빠른 적을 늦추세요. 4·6웨이브 전에 동쪽에 창을 더 세우세요."),
            S(2,"종현 배급소",14,new[]{0,1},new[]{40,50,70,90,100,100},
              "서·북 범람과 정예 마수.\n60초 촉매로 뇌격 조합을 시도하세요.",
              "강윤: 저 울음소리… 다음 무리가 어디로 올지 알아요.",
              "세희: 배급소는 안전해. 다음 작전부터 나도 함께 갈게!\n붕괴 결계주와 파쇄·중력 융합 해금.",
              "서·북을 지키세요. 창·봉인 Lv2 + 60초 촉매 + 마석 40으로 뇌격을 만듭니다."),
            S(3,"방위대 임시초소",24,new[]{0,1,2},new[]{60,84,108,156,168,192},
              "세 방향 범람.\n공성 마수는 각성자의 몸막기를 무시합니다.",
              "백도하: 한 곳을 오래 지키는 것보다, 무너질 곳을 먼저 봐라.",
              "유채린: 방벽 설계 완료. 최종 종결 융합도 사용할 수 있어요.",
              "서·북·동을 나눠 막고 보급으로 계속 보강하세요. 먼 지점은 전체 보기로 관리하세요."),
            S(4,"잿빛 탑 외곽",24,new[]{0,2},new[]{72,96,120,144,168,180},
              "좌우로 밀집 부대가 교대합니다.\n공성 마수는 방벽을 통과합니다.",
              "유채린: 공명 융합으로 비는 지점이 생겨요. 다른 전선을 꼭 확인하세요.",
              "백도하: 문지기가 움직인다. 마지막 봉인식을 시작한다.\n결계 공방 연구2단계 해금.",
              "방벽은 일반 적을 저지하지만 공성 마수는 통과합니다. 밀집 부대에는 붕괴의 범위 공격을 활용하세요."),
            S(5,"잿빛 탑 전면광장",24,new[]{0,1,2},new[]{72,108,144,162,180,216},
              "마지막 웨이브에 문지기가 나옵니다.\n문지기와 남은 마수를 모두 처치하세요.",
              "강윤: 저 문이 닫히면… 내가 돌아온 이유도 알 수 있을까.",
              "백도하: 불이 켜졌다. 종현동에… 다시 불이 켜졌어.\n최초의 탑 봉인. 백도하와 위기 작전 해금.\n다솜동에서 실종자들의 소지품이 발견되었다. 다음 구역은 제작 중입니다.",
              "마지막 웨이브에 문지기 등장. 붉은 원을 피하고 남은 마수까지 모두 처치하세요."),
            S(6,"복귀 대피로",24,new[]{0,2,3},new[]{48,60,84,108,120,132},"서·동의 긴 우회로와 남쪽 지름길.\n짧은 남쪽을 먼저 막고 양옆을 보강하세요.","세희: 남쪽 대피로가 더 짧아. 먼저 막고 양옆의 시민들을 지키자!","유채린: 대피로의 잔광이 줄었어요.","남쪽 지름길을 먼저 지키고 서·동은 길게 돌아오는 동안 보강하세요. 세 방향에 화력을 나누세요."),
            S(7,"남쪽 골목",22,new[]{0,1,2},new[]{48,72,96,120,132,144},"서·북·동의 빠른 마수 뒤로 정예·공성이 이어집니다.","강윤: 균열은 닫혀도 흔적이 남는군요.","세희: 다음 골목은 내가 살필게.","서·북·동에 창을 나눠 세우세요. 북쪽은 짧고 서·동은 길어 도착 시점이 다릅니다."),
            S(8,"임시 배급로",24,new[]{0,2},new[]{60,72,96,120,132,144},"후반에는 정예와 마수가 양쪽에서 짧게 몰려옵니다.","유채린: 이곳을 지키면 보급이 다시 이어져요.","백도하: 방어선이 조금씩 돌아오는군.","후반에는 양쪽에서 짧게 몰려옵니다. 붕괴의 범위 공격과 정예를 상대할 화력을 준비하세요."),
            S(9,"관측소 확보",24,new[]{0,1,2,3},new[]{80,96,128,160,192,208},"사방의 긴 우회로에서 범람이 합류합니다.\n길목을 나눠 관측소를 지키세요.","유채린: 남쪽 균열도 열렸어요. 네 방향의 접근로를 확인해 주세요.","표식 결계주 해금! 편성에서 선택할 수 있습니다.","서·북·동·남의 바깥 길목부터 나눠 지키고 합류부를 보강하세요. 클리어 후 표식 결계주가 열립니다."),
            S(10,"표식 훈련로",24,new[]{0,2},new[]{64,96,128,160,176,192},"서쪽 우회로와 동남쪽 진입로를 지키세요.\nLv3 강화 · 표식과 봉인으로 추적 조합","유채린: 표식된 적은 다른 결계주가 더 큰 피해를 줘요.","세희: 표식을 따라 쏘니 정예가 빨리 쓰러져.","4웨이브에는 동남쪽을 먼저 보강하세요. 표식과 봉인으로 추적을 만든 뒤 길목의 결계주를 Lv3까지 강화하세요."),
            S(11,"서쪽 교차로",24,new[]{0,1},new[]{80,96,128,176,192,208},"서쪽과 북쪽을 번갈아 보강하세요.\n후반 공성이 일반 마수와 함께 몰려옵니다.","강윤: 앞선 적에게 화력을 모아야겠어요.","유채린: 표식과 창을 더 강하게 연결할 수 있겠어요.","붕괴는 입구 쪽에 두세요. 표식과 창은 같은 길을 노리세요."),
            S(12,"북쪽 연결로",24,new[]{0,1,2},new[]{80,112,144,176,192,224},"분산 방어와 집중 공격을 함께 준비하세요.","백도하: 표적 하나에 집중하더라도 다른 길은 비우지 마라.","세희: 다음 균열은 관측창 설계의 마지막 시험이야.","집중 화력과 다른 전선의 방어를 함께 고려하세요."),
            S(13,"관측창 설계 시험",24,new[]{0,1,2},new[]{80,112,144,176,208,224},ObserverRoutes+"\n클리어 후 관측창 조합이 열립니다.","유채린: 이 자료가 있으면 표식과 창을 합칠 수 있어요.","관측창 조합 해금! 표식 Lv2와 창 Lv2를 합치세요.","짧은 북쪽을 먼저 지키고 서·동을 보강하세요. 표식과 창은 같은 길을 노리세요.")
        };
        static CampaignStage S(int i,string name,int sockets,int[] lanes,int[] counts,string briefing,string story,string clear,string lesson)
        { return new CampaignStage { Index=i,Name=name,SocketCount=sockets,Lanes=lanes,Counts=counts,FirstReward=10+2*i,Briefing=briefing,Story=story,ClearStory=clear,Lesson=lesson }; }
        public static int Stars(bool won, float minimumHealth, float maximumHealth)
        {
            if(!won || maximumHealth<=0 || float.IsNaN(minimumHealth))return 0;
            if(minimumHealth+0.0001f>=maximumHealth*.9f)return 3;
            return minimumHealth+0.0001f>=maximumHealth*.7f ? 2 : 1;
        }
        public static string StageId(int stage) { return "1-"+(stage+1); }
    }
    public sealed class CampaignProfile
    {
        public int MagicCores, SelectedHunter, StoryMask, TutorialMask;
        public int[] BestStars = new int[14], CrisisStars = new int[14], Research = new int[4], Growth = new int[3];
        public float[] BestErosion = { 1,1,1,1,1,1,1,1,1,1,1,1,1,1 };
        public bool[] FirstCleared = new bool[14];
        public int Loadout = 135;
        public string PendingRun = "", LastRewardRun = "";
        public int PendingStage = -1, LastReward;
        public bool PendingCrisis, Muted;
        public bool IsStageOpen(int i) { return i>=0 && i<CampaignCatalog.Stages.Length && (i==0 || BestStars[i-1]>0); }
        // A failed first attempt should not permanently hide its learning cues.
        public bool ShouldShowLesson(int i) { return i>=0 && i<FirstCleared.Length && (!FirstCleared[i] || (TutorialMask & (1 << i)) == 0); }
        public bool FirstSealComplete { get { return BestStars[5]>0; } }
        public int VictoryReward(int stage, bool crisis)
        { return crisis || FirstCleared[stage] ? 2 : CampaignCatalog.Stages[stage].FirstReward; }
        public bool IsHunterOpen(int i) { return i==0 || i==1 && BestStars[2]>0 || i==2 && FirstSealComplete; }
        // Compatibility accessors for older tools. They do not imply a playable second world.
        public bool ChapterCleared { get { return FirstSealComplete; } }
        public int ChapterReached { get { return FirstSealComplete ? 2 : 1; } }
        public int StarsTotal => CountStars(BestStars);
        public int CrisisStarsTotal => CountStars(CrisisStars);
        public const int MinimumLoadoutCount = 1, MaximumLoadoutCount = 4;
        public int LoadoutCount
        {
            get {
                int count = 0;
                for (int i = 0; i < 9; i++) if ((Loadout & (1 << i)) != 0) count++;
                return count;
            }
        }

        static int CountStars(int[] stars)
        {
            int total = 0;
            foreach (int count in stars) total += count;
            return total;
        }
        public string Grade(int hunter)
        { return hunter==0 ? "미분류 · 안정화 "+(Growth[0]+1) : CampaignCatalog.Grades[CampaignCatalog.HunterStart[hunter]+Growth[hunter]]+"급"; }
        // Results reachable from the selected families, independently of a battle's resources.
        public List<TowerKind> LoadoutResults()
        {
            int reachable=Loadout;
            var results=new List<TowerKind>();
            // Repeated passes allow an existing tier-2 result to be used in the final fusion.
            for(int pass=0;pass<3;pass++)foreach(FusionRecipe r in TowerCatalog.Recipes) {
                if(r.Result==TowerKind.Observer && !FirstCleared[13])continue;
                if(r.Result==TowerKind.Beacon && !FirstCleared[9])continue;
                int needed=(1<<(int)r.First)|(1<<(int)r.Second);
                if((reachable&needed)!=needed)continue;
                int resultBit=1<<(int)r.Result;
                if((reachable&resultBit)==0) {reachable|=resultBit;results.Add(r.Result);}
            }
            return results;
        }
        public CampaignProfile Copy()
        {
            CampaignProfile p=(CampaignProfile)MemberwiseClone();
            p.BestStars=(int[])BestStars.Clone(); p.CrisisStars=(int[])CrisisStars.Clone();
            p.Research=(int[])Research.Clone(); p.Growth=(int[])Growth.Clone();
            p.BestErosion=(float[])BestErosion.Clone(); p.FirstCleared=(bool[])FirstCleared.Clone(); return p;
        }
        public bool Valid()
        {
            if(MagicCores<0 || MagicCores>100000000 || SelectedHunter<0 || SelectedHunter>2 || PendingStage< -1 || PendingStage>=CampaignCatalog.Stages.Length)return false;
            if(BestStars.Length!=14 || CrisisStars.Length!=14 || Research.Length!=4 || Growth.Length!=3 || BestErosion.Length!=14 || FirstCleared.Length!=14)return false;
            int allowed=(1<<0)|(1<<1)|(1<<2)|(1<<7)|(1<<8), n=LoadoutCount;
            if((Loadout&~allowed)!=0 || n<MinimumLoadoutCount || n>MaximumLoadoutCount || (Loadout&(1<<8))!=0 && !FirstCleared[9])return false;
            for(int i=0;i<14;i++)if(BestStars[i]<0 || BestStars[i]>3 || CrisisStars[i]<0 || CrisisStars[i]>3 ||
                float.IsNaN(BestErosion[i]) || BestErosion[i]<0 || BestErosion[i]>1 ||
                FirstCleared[i]!=(BestStars[i]>0) || i>0 && BestStars[i]>0 && BestStars[i-1]==0)return false;
            for(int i=0;i<4;i++)if(Research[i]<0 || Research[i]>3)return false;
            for(int i=0;i<3;i++)if(Growth[i]<0 || Growth[i]+CampaignCatalog.HunterStart[i]>6)return false;
            return IsHunterOpen(SelectedHunter) && PendingRun.Length<=64 && LastRewardRun.Length<=64;
        }
    }
    // Small versioned local format with a checksum, atomic replace and a previous-good backup.
    // No account/network dependency; checksum detects corruption, not deliberate save editing.
    public sealed class CampaignStore
    {
        readonly string path;
        public string Warning { get; private set; }
        public CampaignStore(string path) { this.path=path; }
        public static byte[] Encode(CampaignProfile p)
        {
            if(!p.Valid())throw new InvalidDataException("Invalid campaign profile");
            byte[] payload;
            using(var mem=new MemoryStream()) {
                using(var w=new BinaryWriter(mem,Encoding.UTF8,true)) {
                    w.Write("GATEFALL-RAON"); w.Write(2);
                    w.Write(p.MagicCores); w.Write(p.SelectedHunter); w.Write(p.StoryMask); w.Write(p.TutorialMask); w.Write(p.Muted);
                    for(int i=0;i<6;i++) { w.Write(p.BestStars[i]); w.Write(p.CrisisStars[i]); w.Write(p.BestErosion[i]); w.Write(p.FirstCleared[i]); }
                    foreach(int r in p.Research)w.Write(r); foreach(int g in p.Growth)w.Write(g);
                    w.Write(p.PendingRun); w.Write(p.PendingStage); w.Write(p.PendingCrisis); w.Write(p.LastRewardRun); w.Write(p.LastReward);
                    for(int i=6;i<14;i++) { w.Write(p.BestStars[i]); w.Write(p.CrisisStars[i]); w.Write(p.BestErosion[i]); w.Write(p.FirstCleared[i]); }
                    w.Write(p.Loadout);
                } payload=mem.ToArray();
            }
            byte[] hash; using(var sha=SHA256.Create())hash=sha.ComputeHash(payload);
            byte[] all=new byte[32+payload.Length]; Buffer.BlockCopy(hash,0,all,0,32); Buffer.BlockCopy(payload,0,all,32,payload.Length); return all;
        }
        public static CampaignProfile Decode(byte[] data)
        {
            if(data==null || data.Length<64 || data.Length>16384)throw new InvalidDataException("Invalid profile length");
            byte[] hash; using(var sha=SHA256.Create())hash=sha.ComputeHash(data,32,data.Length-32);
            for(int i=0;i<32;i++)if(hash[i]!=data[i])throw new InvalidDataException("Profile checksum mismatch");
            using(var mem=new MemoryStream(data,32,data.Length-32,false))using(var r=new BinaryReader(mem,Encoding.UTF8)) {
                if(r.ReadString()!="GATEFALL-RAON")throw new InvalidDataException("Unknown profile format");
                int version=r.ReadInt32();if(version!=1 && version!=2)throw new InvalidDataException("Unknown profile version");
                var p=new CampaignProfile { MagicCores=r.ReadInt32(),SelectedHunter=r.ReadInt32(),StoryMask=r.ReadInt32(),TutorialMask=r.ReadInt32(),Muted=r.ReadBoolean() };
                for(int i=0;i<6;i++) { p.BestStars[i]=r.ReadInt32();p.CrisisStars[i]=r.ReadInt32();p.BestErosion[i]=r.ReadSingle();p.FirstCleared[i]=r.ReadBoolean(); }
                for(int i=0;i<4;i++)p.Research[i]=r.ReadInt32(); for(int i=0;i<3;i++)p.Growth[i]=r.ReadInt32();
                p.PendingRun=r.ReadString();p.PendingStage=r.ReadInt32();p.PendingCrisis=r.ReadBoolean();p.LastRewardRun=r.ReadString();p.LastReward=r.ReadInt32();
                if(version==2) { for(int i=6;i<14;i++) {p.BestStars[i]=r.ReadInt32();p.CrisisStars[i]=r.ReadInt32();p.BestErosion[i]=r.ReadSingle();p.FirstCleared[i]=r.ReadBoolean();}p.Loadout=r.ReadInt32(); }
                if(mem.Position!=mem.Length || !p.Valid())throw new InvalidDataException("Invalid profile values"); return p;
            }
        }
        public CampaignProfile Load()
        {
            Warning=""; if(!File.Exists(path) && !File.Exists(path+".bak"))return new CampaignProfile();
            try { return Decode(File.ReadAllBytes(path)); }
            catch(Exception) {
                try { var p=Decode(File.ReadAllBytes(path+".bak")); Warning="저장 파일을 백업에서 복구했습니다."; return p; }
                catch(Exception e) { throw new InvalidDataException("프로필과 백업을 읽을 수 없습니다. 원본 파일을 보존했습니다.",e); }
            }
        }
        public void Save(CampaignProfile profile)
        {
            byte[] bytes=Encode(profile); Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp=path+".tmp"; File.WriteAllBytes(tmp,bytes); Decode(File.ReadAllBytes(tmp));
            if(File.Exists(path)) {
                // Preserve a valid backup even when the primary had been recovered from corruption.
                bool valid=true; try { Decode(File.ReadAllBytes(path)); } catch { valid=false; }
                File.Replace(tmp,path,valid ? path+".bak" : null);
            } else File.Move(tmp,path);
        }
    }
    public sealed class CampaignService
    {
        readonly Action<CampaignProfile> persist;
        public CampaignProfile Profile { get; private set; }
        public string Error { get; private set; }
        public CampaignService(CampaignProfile profile,Action<CampaignProfile> persist)
        { if(profile==null || !profile.Valid())throw new ArgumentException("Invalid profile"); Profile=profile.Copy();this.persist=persist; }
        bool Commit(CampaignProfile p)
        {
            try { if(persist!=null)persist(p); Profile=p; Error="";return true; }
            catch(Exception e) { Error="저장 실패: "+e.Message;return false; }
        }
        public bool Begin(int stage,bool crisis,out string id)
        {
            id=""; if(!Profile.IsStageOpen(stage) || crisis && !Profile.FirstSealComplete) { Error="아직 해금되지 않은 작전입니다.";return false; }
            var p=Profile.Copy();p.PendingRun=Guid.NewGuid().ToString("N");p.PendingStage=stage;p.PendingCrisis=crisis;
            if(!Commit(p))return false;id=p.PendingRun;return true;
        }
        public bool Complete(string id,DefenseSimulation game,out int reward)
        {
            reward=0;
            if(Profile.LastRewardRun==id && !string.IsNullOrEmpty(id)) { reward=Profile.LastReward; return true; }
            if(string.IsNullOrEmpty(id) || Profile.PendingRun!=id || game.Rules==null || game.Rules.Stage==null ||
                game.Rules.Stage.Index!=Profile.PendingStage || game.Rules.Crisis!=Profile.PendingCrisis ||
                (game.State!=RunState.Won && game.State!=RunState.Lost)) { Error="작전 결과가 현재 기록과 일치하지 않습니다.";return false; }
            bool won=game.State==RunState.Won;int stage=Profile.PendingStage;
            int stars=CampaignCatalog.Stars(won,game.MinimumCoreHealth,game.Balance.CoreHealth);
            var p=Profile.Copy();
            if(won) {
                reward=p.VictoryReward(stage,p.PendingCrisis);
                if(p.PendingCrisis) p.CrisisStars[stage]=Math.Max(p.CrisisStars[stage],stars);
                else {
                    p.BestStars[stage]=Math.Max(p.BestStars[stage],stars);p.FirstCleared[stage]=true;
                    p.BestErosion[stage]=Math.Min(p.BestErosion[stage],game.PeakErosion);
                }
                p.MagicCores+=reward;
            }
            p.LastReward=reward;p.LastRewardRun=id;p.PendingRun="";p.PendingStage=-1;
            if(!Commit(p)) { reward=0;return false; } return true;
        }
        public bool Abandon(string id)
        {
            if(string.IsNullOrEmpty(id) || Profile.PendingRun!=id) { Error="진행 중인 작전과 기록이 다릅니다.";return false; }
            var p=Profile.Copy();p.PendingRun="";p.PendingStage=-1;p.PendingCrisis=false;
            return Commit(p);
        }
        public bool SelectHunter(int id)
        { if(id<0 || id>2 || !Profile.IsHunterOpen(id))return false;var p=Profile.Copy();p.SelectedHunter=id;return Commit(p); }
        public string GrowthReason(int hunter)
        {
            if(hunter<0 || hunter>2 || !Profile.IsHunterOpen(hunter))return "작전 클리어로 해금하세요.";
            int index=CampaignCatalog.HunterStart[hunter]+Profile.Growth[hunter];
            if(index>=6)return "최대 성장 단계";
            int growth=Profile.Growth[hunter];
            if(hunter==0 && growth==2 && !Profile.FirstSealComplete)return "1-6 첫 탑 봉인 후 성장";
            if(hunter==0 && growth>=3 || hunter!=0 && growth>=1)return "후속 구역 업데이트 후 추가 성장";
            return Profile.MagicCores<CampaignCatalog.GrowthCosts[index] ? "마핵 부족" : "";
        }
        public int GrowthPrice(int hunter)
        { int index=CampaignCatalog.HunterStart[hunter]+Profile.Growth[hunter];return index<6 ? CampaignCatalog.GrowthCosts[index] : 0; }
        public bool Grow(int hunter)
        {
            Error=GrowthReason(hunter);if(Error!="")return false;
            var p=Profile.Copy();p.MagicCores-=GrowthPrice(hunter);p.Growth[hunter]++;return Commit(p);
        }
        public string ResearchReason(int line)
        {
            if(line<0 || line>3)return "없는 연구입니다.";
            if(Profile.BestStars[0]==0)return "1-1 클리어 후 공방 해금";
            if(line==2 && Profile.BestStars[2]==0 || line==3 && Profile.BestStars[3]==0)return "해당 결계주를 먼저 해금하세요.";
            int rank=Profile.Research[line];if(rank>=3)return "최대 연구 단계";
            if(rank==1 && Profile.BestStars[4]==0)return "1-5 클리어 후 연구2단계";
            if(rank==2)return "2챕터 클리어 후 연구3단계";
            return Profile.MagicCores<CampaignCatalog.ResearchCosts[rank] ? "마핵 부족" : "";
        }
        public bool Research(int line)
        {
            Error=ResearchReason(line);if(Error!="")return false;
            var p=Profile.Copy();p.MagicCores-=CampaignCatalog.ResearchCosts[p.Research[line]];p.Research[line]++;return Commit(p);
        }
        public int ResearchInvestment()
        { int n=0;foreach(int rank in Profile.Research)for(int j=0;j<rank;j++)n+=CampaignCatalog.ResearchCosts[j];return n; }
        public bool Respec()
        { var p=Profile.Copy();p.MagicCores+=ResearchInvestment();Array.Clear(p.Research,0,4);return Commit(p); }
        public bool MarkStory(int index)
        { var p=Profile.Copy();p.StoryMask|=1<<index;return Commit(p); }
        public bool MarkTutorial(int index)
        { var p=Profile.Copy();p.TutorialMask|=1<<index;return Commit(p); }
        public bool SetMuted(bool muted) {var p=Profile.Copy();p.Muted=muted;return Commit(p);}
        public bool SetLoadout(int mask) { var p=Profile.Copy();p.Loadout=mask;if(!p.Valid()) {Error="결계주를 1~4개 선택하세요.";return false;}return Commit(p); }
        public CampaignBattle Battle(int stage,bool crisis)
        { return new CampaignBattle {Stage=CampaignCatalog.Stages[stage],Crisis=crisis,Hunter=Profile.SelectedHunter,EarnedGrowth=Profile.Growth[Profile.SelectedHunter],Research=(int[])Profile.Research.Clone(), Loadout=Profile.Loadout, MarkerUnlocked=Profile.FirstCleared[9], ObserverUnlocked=Profile.FirstCleared[13]}; }
    }
}



