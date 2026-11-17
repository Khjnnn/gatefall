using System;
using System.IO;
using System.Collections.Generic;

namespace Gatefall.Core
{
    public enum BoxKind { Normal, Premium }
    public enum SupplyRarity { Common, Rare, Epic, Legendary }

    public sealed class SupplyState
    {
        public int[] Cards=new int[SupplyCatalog.TowerCount], Grades=new int[SupplyCatalog.TowerCount];
        public int PremiumPity, OwnedCosmetics;
        public SupplyState(){for(int i=0;i<Grades.Length;i++)Grades[i]=1;}
        public bool Valid()
        {
            if(Cards==null || Grades==null || Cards.Length!=SupplyCatalog.TowerCount || Grades.Length!=Cards.Length ||
                PremiumPity<0 || PremiumPity>=SupplyCatalog.PityBoxes || (OwnedCosmetics&~SupplyCatalog.CosmeticMask)!=0)return false;
            for(int i=0;i<Cards.Length;i++)if(Grades[i]<1 || Grades[i]>6 || Cards[i]<SupplyCatalog.RequiredCards(Grades[i]) ||
                (Grades[i]==6 && Cards[i]!=SupplyCatalog.RequiredCards(6)))return false;
            return true;
        }
        public SupplyState Copy()
        {var s=(SupplyState)MemberwiseClone();s.Cards=(int[])Cards.Clone();s.Grades=(int[])Grades.Clone();return s;}
        // Embedded payload v1: version, fifteen (cumulative cards, grade) pairs, pity, cosmetics.
        public void Write(BinaryWriter writer)
        {
            if(writer==null)throw new ArgumentNullException("writer");
            if(!Valid())throw new InvalidDataException("잘못된 보급 상태");
            writer.Write((byte)1);for(int i=0;i<Cards.Length;i++){writer.Write(Cards[i]);writer.Write((byte)Grades[i]);}
            writer.Write((byte)PremiumPity);writer.Write(OwnedCosmetics);
        }
        public static SupplyState Read(BinaryReader reader)
        {
            if(reader==null)throw new ArgumentNullException("reader");
            if(reader.ReadByte()!=1)throw new InvalidDataException("지원하지 않는 보급 버전");
            var s=new SupplyState();for(int i=0;i<s.Cards.Length;i++){s.Cards[i]=reader.ReadInt32();s.Grades[i]=reader.ReadByte();}
            s.PremiumPity=reader.ReadByte();s.OwnedCosmetics=reader.ReadInt32();
            if(!s.Valid())throw new InvalidDataException("잘못된 보급 상태");return s;
        }
    }

    public sealed class SupplyResult
    {
        public readonly List<TowerKind> Cards=new List<TowerKind>();
        public readonly List<int> Cosmetics=new List<int>();
        // Effective rarity in slot order, including cosmetics and converted cards.
        public readonly List<SupplyRarity> Rarities=new List<SupplyRarity>();
        public int BaseCores, SurplusCores;
        public int Cores { get { return BaseCores+SurplusCores; } }
        public bool PityFired;
    }
    public sealed class SupplyOdds
    {
        public decimal[] RollPercents, RarityPercents, PerKind;
        public decimal CosmeticPercent;
        public string PityRule;
    }

    public static class SupplyCatalog
    {
        public const int TowerCount=15, PityBoxes=30, CosmeticCount=3, CosmeticMask=(1<<CosmeticCount)-1;
        static readonly SupplyRarity[] rarities={SupplyRarity.Common,SupplyRarity.Common,SupplyRarity.Common,
            SupplyRarity.Rare,SupplyRarity.Rare,SupplyRarity.Rare,SupplyRarity.Legendary,SupplyRarity.Common,
            SupplyRarity.Common,SupplyRarity.Epic,SupplyRarity.Epic,SupplyRarity.Rare,SupplyRarity.Epic,
            SupplyRarity.Rare,SupplyRarity.Epic};
        // Thousandths avoid float drift in rolls and disclosure.
        static readonly int[][] weights={new[]{790,180,27,3},new[]{500,350,120,30}};
        static readonly int[] required={0,4,10,20,40,80}, costs={10,20,40,80,160};
        static readonly int[] baseCards={3,8}, coreMin={5,20}, coreMax={15,40}, surplus={1,2,5,20};
        static readonly string[] cosmetics={"청록 결계", "금빛 결계", "자주 결계"};
        public static bool IsKind(TowerKind kind){return (int)kind>=0 && (int)kind<TowerCount;}
        public static SupplyRarity Rarity(TowerKind kind)
        {if(!IsKind(kind))throw new ArgumentOutOfRangeException("kind");return rarities[(int)kind];}
        public static string CosmeticName(int id)
        {if(id<0 || id>=CosmeticCount)throw new ArgumentOutOfRangeException("id");return cosmetics[id];}
        static int BoxIndex(BoxKind kind)
        {if(kind!=BoxKind.Normal && kind!=BoxKind.Premium)throw new ArgumentOutOfRangeException("kind");return (int)kind;}
        public static int CardsPerBox(BoxKind kind,int chapterReached)
        {return baseCards[BoxIndex(kind)]+(chapterReached<=1?0:chapterReached==2?1:2);}
        public static int MinCores(BoxKind kind){return coreMin[BoxIndex(kind)];}
        public static int MaxCores(BoxKind kind){return coreMax[BoxIndex(kind)];}
        public static int SurplusCores(SupplyRarity rarity)
        {if((int)rarity<0 || (int)rarity>3)throw new ArgumentOutOfRangeException("rarity");return surplus[(int)rarity];}
        public static int RequiredCards(int grade)
        {if(grade<1 || grade>6)throw new ArgumentOutOfRangeException("grade");return required[grade-1];}
        public static int GradeCost(int currentGrade)
        {if(currentGrade<1 || currentGrade>6)throw new ArgumentOutOfRangeException("currentGrade");return currentGrade==6?0:costs[currentGrade-1];}
        public static double GradeBonus(int grade)
        {if(grade<1 || grade>6)throw new ArgumentOutOfRangeException("grade");return (grade-1)/50.0;}
        internal static List<TowerKind>[] Unlocked(Func<TowerKind,bool> unlocked)
        {
            if(unlocked==null)throw new ArgumentNullException("unlocked");
            var all=new List<TowerKind>[4];for(int r=0;r<4;r++)all[r]=new List<TowerKind>();
            for(int i=0;i<TowerCount;i++)if(unlocked((TowerKind)i))all[(int)rarities[i]].Add((TowerKind)i);
            // A common roll cannot fall any further. Never substitute a locked or higher-rarity card.
            if(all[0].Count==0)throw new ArgumentException("일반 결계주를 하나 이상 해금하세요.","unlocked");return all;
        }
        internal static int EffectiveRarity(int rarity,List<TowerKind>[] unlocked)
        {while(unlocked[rarity].Count==0)rarity--;return rarity;}
        internal static int Roll(BoxKind kind,int roll)
        {int[] row=weights[BoxIndex(kind)];for(int r=0;r<4;r++){if(roll<row[r])return r;roll-=row[r];}throw new ArgumentOutOfRangeException("roll");}
        // Two-argument disclosure assumes no cosmetics owned; state overload shows current ownership.
        // These are per-slot base odds, before the explicitly disclosed conditional pity override.
        public static SupplyOdds OddsTable(BoxKind kind,Func<TowerKind,bool> unlocked)
        {return OddsTable(kind,unlocked,new SupplyState());}
        public static SupplyOdds OddsTable(BoxKind kind,Func<TowerKind,bool> unlocked,SupplyState state)
        {
            if(state==null || !state.Valid())throw new ArgumentException("잘못된 보급 상태","state");
            var all=Unlocked(unlocked);var table=new SupplyOdds{RollPercents=new decimal[4],RarityPercents=new decimal[4],PerKind=new decimal[TowerCount]};
            int[] row=weights[BoxIndex(kind)];
            for(int r=0;r<4;r++){table.RollPercents[r]=row[r]/10m;table.RarityPercents[EffectiveRarity(r,all)]+=table.RollPercents[r];}
            for(int r=0;r<4;r++){
                decimal total=table.RarityPercents[r];if(total==0)continue;
                if(r==3 && state.OwnedCosmetics!=CosmeticMask){table.CosmeticPercent=total/2;total-=table.CosmeticPercent;}
                decimal each=total/all[r].Count, assigned=0;
                for(int i=0;i<all[r].Count;i++){decimal p=i==all[r].Count-1?total-assigned:each;table.PerKind[(int)all[r][i]]=p;assigned+=p;}
            }
            table.PityRule=kind==BoxKind.Normal?"일반 상자는 천장 횟수를 늘리지 않습니다. 전설 보상 획득 시 고급 천장 횟수를 초기화합니다.":
                "고급 상자에서 전설 카드·외형 없이 29회 연속 개봉하면 30번째에 종결 카드 1장을 보장합니다. 전설 카드·외형 획득 시 초기화합니다. 종결 미해금 시 하위 희귀도로 내리고 천장은 29회에서 대기합니다.";
            table.PityRule+=" 전설은 종결 카드와 미보유 외형 전체가 각 50%이며 외형 안에서는 균등합니다. 외형을 모두 보유하면 종결 카드 100%입니다. 표시 확률은 천장 적용 전 카드 슬롯 기준입니다.";
            return table;
        }
    }

    public static class Supply
    {
        public static string GradeReason(SupplyState state,TowerKind kind,int magicCores)
        {
            if(state==null || !state.Valid())return "잘못된 보급 상태";
            if(!SupplyCatalog.IsKind(kind))return "없는 결계주입니다.";
            int i=(int)kind,grade=state.Grades[i];if(grade==6)return "최대 등급";
            if(state.Cards[i]<SupplyCatalog.RequiredCards(grade+1))return "카드 부족";
            if(magicCores<SupplyCatalog.GradeCost(grade))return "마핵 부족";
            long after=(long)magicCores-SupplyCatalog.GradeCost(grade);
            if(grade==5)after+=(long)(state.Cards[i]-SupplyCatalog.RequiredCards(6))*SupplyCatalog.SurplusCores(SupplyCatalog.Rarity(kind));
            return after>int.MaxValue?"마핵 보유 한도":"";
        }
        public static bool CanGradeUp(SupplyState state,TowerKind kind,int magicCores)
        {return GradeReason(state,kind,magicCores)=="";}
        public static bool GradeUp(SupplyState state,TowerKind kind,ref int magicCores)
        {
            if(!CanGradeUp(state,kind,magicCores))return false;
            int i=(int)kind;magicCores-=SupplyCatalog.GradeCost(state.Grades[i]);state.Grades[i]++;
            if(state.Grades[i]==6){magicCores+=(int)((long)(state.Cards[i]-SupplyCatalog.RequiredCards(6))*SupplyCatalog.SurplusCores(SupplyCatalog.Rarity(kind)));state.Cards[i]=SupplyCatalog.RequiredCards(6);}return true;
        }
        public static double DamageBonus(SupplyState state,TowerKind kind)
        {
            if(state==null || !state.Valid())throw new ArgumentException("잘못된 보급 상태","state");
            if(!SupplyCatalog.IsKind(kind))throw new ArgumentOutOfRangeException("kind");return SupplyCatalog.GradeBonus(state.Grades[(int)kind]);
        }
        static int Next(Func<int,int> rng,int max)
        {int n=rng(max);if(n<0 || n>=max)throw new ArgumentOutOfRangeException("rng","난수는 0 이상 상한 미만이어야 합니다.");return n;}
        public static SupplyResult Open(SupplyState state,BoxKind kind,Func<TowerKind,bool> unlocked,int chapterReached,Random rng)
        {if(rng==null)throw new ArgumentNullException("rng");return Open(state,kind,unlocked,chapterReached,rng.Next);}
        // rng(maxExclusive) must return [0,maxExclusive). Work on a copy so rejected input cannot partially grant rewards.
        public static SupplyResult Open(SupplyState state,BoxKind kind,Func<TowerKind,bool> unlocked,int chapterReached,Func<int,int> rng)
        {
            if(state==null || !state.Valid())throw new ArgumentException("잘못된 보급 상태","state");
            if(rng==null)throw new ArgumentNullException("rng");int count=SupplyCatalog.CardsPerBox(kind,chapterReached);
            var all=SupplyCatalog.Unlocked(unlocked);var copy=state.Copy();var result=new SupplyResult();bool legendary=false;
            result.BaseCores=SupplyCatalog.MinCores(kind)+Next(rng,SupplyCatalog.MaxCores(kind)-SupplyCatalog.MinCores(kind)+1);
            for(int slot=0;slot<count;slot++){
                int rarity;
                if(slot==count-1 && kind==BoxKind.Premium && copy.PremiumPity==SupplyCatalog.PityBoxes-1 && !legendary && all[3].Count>0){rarity=3;result.PityFired=true;}
                else rarity=SupplyCatalog.EffectiveRarity(SupplyCatalog.Roll(kind,Next(rng,1000)),all);
                result.Rarities.Add((SupplyRarity)rarity);if(rarity==3)legendary=true;
                if(rarity==3 && !result.PityFired && copy.OwnedCosmetics!=SupplyCatalog.CosmeticMask && Next(rng,2)==1){
                    var available=new List<int>();for(int i=0;i<SupplyCatalog.CosmeticCount;i++)if((copy.OwnedCosmetics&(1<<i))==0)available.Add(i);
                    int cosmetic=available[Next(rng,available.Count)];copy.OwnedCosmetics|=1<<cosmetic;result.Cosmetics.Add(cosmetic);continue;
                }
                TowerKind card=all[rarity][Next(rng,all[rarity].Count)];result.Cards.Add(card);int index=(int)card;
                if(copy.Grades[index]==6)result.SurplusCores+=SupplyCatalog.SurplusCores((SupplyRarity)rarity);
                else copy.Cards[index]=checked(copy.Cards[index]+1);
            }
            if(legendary)copy.PremiumPity=0;
            else if(kind==BoxKind.Premium)copy.PremiumPity=Math.Min(SupplyCatalog.PityBoxes-1,copy.PremiumPity+1);
            Array.Copy(copy.Cards,state.Cards,copy.Cards.Length);state.OwnedCosmetics=copy.OwnedCosmetics;state.PremiumPity=copy.PremiumPity;return result;
        }
    }
}
