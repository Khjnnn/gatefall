using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class SupplyChecks
    {
        static void Require(bool value,string why){if(!value)throw new Exception(why);}
        static bool All(TowerKind kind){return true;}
        static bool Common(TowerKind kind){return kind==TowerKind.Autocannon;}
        static int Zero(int max){return 0;}
        static int High(int max){return max-1;}
        static Func<int,int> Sequence(params int[] values)
        {int i=0;return max=>{Require(i<values.Length,"Unexpected RNG call");int n=values[i++];Require(n>=0 && n<max,"Invalid RNG fixture");return n;};}
        static SupplyState Capped()
        {var s=new SupplyState{OwnedCosmetics=SupplyCatalog.CosmeticMask};for(int i=0;i<15;i++){s.Grades[i]=6;s.Cards[i]=80;}return s;}
        static byte[] Bytes(SupplyState state)
        {using(var m=new MemoryStream()){using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true))state.Write(w);return m.ToArray();}}
        static SupplyState Read(byte[] bytes)
        {using(var m=new MemoryStream(bytes))using(var r=new BinaryReader(m))return SupplyState.Read(r);}
        static void Throws<T>(Action action,string why) where T:Exception
        {try{action();}catch(T){return;}throw new Exception(why);}
        static void Odds()
        {
            TowerKind[][] expected={new[]{TowerKind.Autocannon,TowerKind.Binding,TowerKind.Rupture,TowerKind.Barrier,TowerKind.Marker},
                new[]{TowerKind.Scatter,TowerKind.Piercer,TowerKind.Arc,TowerKind.Shatter,TowerKind.Gravity},
                new[]{TowerKind.Observer,TowerKind.Beacon,TowerKind.Barrage,TowerKind.Surge},new[]{TowerKind.Seal}};
            Require(expected.Sum(a=>a.Length)==15,"Catalog kind count");
            for(int r=0;r<4;r++)foreach(var k in expected[r])Require((int)SupplyCatalog.Rarity(k)==r,"Rarity mapping "+k);
            decimal[][] weights={new[]{79m,18m,2.7m,.3m},new[]{50m,35m,12m,3m}};
            foreach(BoxKind box in new[]{BoxKind.Normal,BoxKind.Premium}){
                var odds=SupplyCatalog.OddsTable(box,All);Require(odds.RollPercents.SequenceEqual(weights[(int)box]),"Plan odds");
                Require(odds.RollPercents.Sum()==100m && odds.RarityPercents.Sum()==100m && odds.PerKind.Sum()+odds.CosmeticPercent==100m,"Exact 100% disclosure");
                Require(odds.CosmeticPercent==weights[(int)box][3]/2 && odds.PerKind[(int)TowerKind.Seal]==odds.CosmeticPercent,"Legendary split");
                Require(odds.PityRule.Contains("천장") && odds.PityRule.Contains("초기화"),"Korean disclosure");
                var owned=SupplyCatalog.OddsTable(box,All,Capped());Require(owned.CosmeticPercent==0 && owned.PerKind.Sum()==100m,"Owned cosmetic odds");
                var down=SupplyCatalog.OddsTable(box,Common);Require(down.RarityPercents.SequenceEqual(new[]{100m,0m,0m,0m}) && down.PerKind[0]==100m,"Fallback disclosure");
            }
            // Exercise repeating decimal splits and every subset of the fifteen unlocks.
            for(int mask=1;mask<(1<<15);mask++){
                int bits=mask;if((bits&((1<<0)|(1<<1)|(1<<2)|(1<<7)|(1<<8)))==0)continue;
                var odds=SupplyCatalog.OddsTable(BoxKind.Premium,k=>(bits&(1<<(int)k))!=0);
                Require(odds.RarityPercents.Sum()==100m && odds.PerKind.Sum()+odds.CosmeticPercent==100m,"Subset rounding "+mask);
                for(int i=0;i<15;i++)if((bits&(1<<i))==0)Require(odds.PerKind[i]==0,"Locked disclosure");
            }
        }
        static string Simulation(BoxKind box)
        {
            const int samples=200000;var rng=new Random(731+(int)box);var s=Capped();var count=new int[4];var kinds=new int[15];
            int perBox=SupplyCatalog.CardsPerBox(box,3);var odds=SupplyCatalog.OddsTable(box,All,s);
            for(int n=0;n<samples;n+=perBox){
                // Measure the disclosed base distribution; pity is tested independently below.
                s.PremiumPity=0;var result=Supply.Open(s,box,All,3,rng);
                Require(result.Cards.Count==perBox && result.Cosmetics.Count==0,"Simulation slot count");
                Require(result.BaseCores>=SupplyCatalog.MinCores(box) && result.BaseCores<=SupplyCatalog.MaxCores(box),"Core range");
                foreach(var rarity in result.Rarities)count[(int)rarity]++;
                foreach(var card in result.Cards)kinds[(int)card]++;
            }
            for(int r=0;r<4;r++)Require(Math.Abs(count[r]*100m/samples-odds.RarityPercents[r])<=.3m,"Rarity distribution "+box+" "+r);
            for(int i=0;i<15;i++)Require(Math.Abs(kinds[i]*100m/samples-odds.PerKind[i])<=.3m,"Uniform kind distribution "+box+" "+i);
            return "PASS "+box+" 200000 cards: "+string.Join("/",count.Select(n=>(n*100m/samples).ToString("0.000")))+"% (each rarity within 0.3 percentage points)";
        }
        static void UnlocksAndPity()
        {
            foreach(BoxKind box in new[]{BoxKind.Normal,BoxKind.Premium}){
                var state=new SupplyState();var rng=new Random(29);
                for(int n=0;n<1000;n++){
                    var result=Supply.Open(state,box,Common,3,rng);
                    Require(result.Cards.All(Common) && result.Cosmetics.Count==0 && result.Rarities.All(r=>r==SupplyRarity.Common),"Locked reward");
                }
                for(int r=0;r<4;r++){
                    int roll=new[]{0,790,970,997}[r];if(box==BoxKind.Premium)roll=new[]{0,500,850,970}[r];
                    var down=Supply.Open(new SupplyState(),box,Common,1,max=>max==1000?roll:0);
                    Require(down.Cards.All(Common),"Repeated downward fallback");
                }
            }
            // Legendary -> empty epic -> rare. Epic -> rare. Common remains common.
            Func<TowerKind,bool> rare=k=>Common(k) || k==TowerKind.Arc;
            var middle=Supply.Open(new SupplyState(),BoxKind.Normal,rare,1,max=>max==1000?999:0);
            Require(middle.Cards.All(k=>k==TowerKind.Arc),"Nearest lower rarity");
            var s=new SupplyState();
            for(int n=1;n<=30;n++){
                var result=Supply.Open(s,BoxKind.Premium,All,1,Zero);
                Require(result.PityFired==(n==30),"Pity timing "+n);
                Require(s.PremiumPity==(n==30?0:n),"Pity count "+n);
                if(n==30)Require(result.Cards.Count(k=>k==TowerKind.Seal)==1 && result.Cosmetics.Count==0,"Pity guarantees a card");
            }
            s.PremiumPity=29;var natural=Supply.Open(s,BoxKind.Premium,All,1,Sequence(0,999,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0));
            Require(!natural.PityFired && s.PremiumPity==0 && natural.Cards.Contains(TowerKind.Seal),"Natural legendary resets before pity");
            s.PremiumPity=12;Supply.Open(s,BoxKind.Normal,All,1,Zero);Require(s.PremiumPity==12,"Normal did not preserve pity");
            var cosmetic=Supply.Open(s,BoxKind.Normal,All,1,High);
            Require(cosmetic.Cosmetics.Count==3 && cosmetic.Cosmetics.Distinct().Count()==3 && s.OwnedCosmetics==7 && s.PremiumPity==0,"Unique cosmetics and reset");
            var allOwned=Supply.Open(s,BoxKind.Normal,All,1,High);Require(allOwned.Cosmetics.Count==0 && allOwned.Cards.All(k=>k==TowerKind.Seal),"All-owned legendary card");
            s=new SupplyState{PremiumPity=29};var locked=Supply.Open(s,BoxKind.Premium,Common,1,Zero);
            Require(!locked.PityFired && s.PremiumPity==29,"Locked pity pending");
            var unlocked=Supply.Open(s,BoxKind.Premium,All,1,Zero);Require(unlocked.PityFired && s.PremiumPity==0,"Pending pity after unlock");
            s=new SupplyState();byte[] before=Bytes(s);
            Throws<ArgumentException>(()=>Supply.Open(s,BoxKind.Premium,k=>false,1,Zero),"No common unlock accepted");
            int calls=0;Throws<ArgumentOutOfRangeException>(()=>Supply.Open(s,BoxKind.Normal,All,1,max=>++calls==5?max:0),"Invalid RNG accepted");
            Require(before.SequenceEqual(Bytes(s)),"Rejected open partially mutated state");
        }
        static void GradesAndBoxes()
        {
            foreach(BoxKind box in new[]{BoxKind.Normal,BoxKind.Premium})for(int chapter=0;chapter<=6;chapter++){
                int expected=(box==BoxKind.Normal?3:8)+Math.Min(2,Math.Max(0,chapter-1));
                Require(SupplyCatalog.CardsPerBox(box,chapter)==expected,"Chapter card count");
                var result=Supply.Open(new SupplyState(),box,All,chapter,Zero);Require(result.Cards.Count==expected,"Opened chapter count");
            }
            Require(SupplyCatalog.CardsPerBox(BoxKind.Normal,int.MaxValue)==5,"Chapter overflow");
            Require(SupplyCatalog.MinCores(BoxKind.Normal)==5 && SupplyCatalog.MaxCores(BoxKind.Normal)==15 && SupplyCatalog.MinCores(BoxKind.Premium)==20 && SupplyCatalog.MaxCores(BoxKind.Premium)==40,"Core catalog");
            foreach(BoxKind box in new[]{BoxKind.Normal,BoxKind.Premium}){
                Require(Supply.Open(new SupplyState(),box,Common,1,Zero).BaseCores==SupplyCatalog.MinCores(box),"Inclusive minimum");
                Require(Supply.Open(new SupplyState(),box,Common,1,High).BaseCores==SupplyCatalog.MaxCores(box),"Inclusive maximum");
            }
            int[] cards={4,10,20,40,80}, costs={10,20,40,80,160};
            for(int i=0;i<15;i++){
                TowerKind kind=(TowerKind)i;var s=new SupplyState();int cores=1000;
                Require(Supply.DamageBonus(s,kind)==0,"Grade1 damage");
                for(int grade=1;grade<6;grade++){
                    s.Cards[i]=cards[grade-1]-1;int before=cores;
                    Require(!Supply.CanGradeUp(s,kind,cores) && Supply.GradeReason(s,kind,cores)=="카드 부족" && !Supply.GradeUp(s,kind,ref cores) && cores==before && s.Grades[i]==grade,"Card threshold");
                    s.Cards[i]++;int poor=costs[grade-1]-1;
                    Require(Supply.GradeReason(s,kind,poor)=="마핵 부족" && !Supply.GradeUp(s,kind,ref poor) && poor==costs[grade-1]-1,"Core threshold");
                    Require(SupplyCatalog.GradeCost(grade)==costs[grade-1] && SupplyCatalog.RequiredCards(grade+1)==cards[grade-1],"Grade catalog");
                    Require(Supply.GradeUp(s,kind,ref cores) && cores==before-costs[grade-1] && s.Grades[i]==grade+1 && s.Cards[i]==cards[grade-1] && s.Valid(),"Cumulative grade cost");
                    Require(Supply.DamageBonus(s,kind)==grade/50.0,"Grade bonus");
                }
                int capped=cores;Require(Supply.DamageBonus(s,kind)==.10 && !Supply.GradeUp(s,kind,ref cores) && cores==capped && Supply.GradeReason(s,kind,cores)=="최대 등급","Grade6 hard cap");
            }
            Require(new[]{SupplyRarity.Common,SupplyRarity.Rare,SupplyRarity.Epic,SupplyRarity.Legendary}.Select(SupplyCatalog.SurplusCores).SequenceEqual(new[]{1,2,5,20}),"Surplus catalog");
            foreach(TowerKind kind in new[]{TowerKind.Autocannon,TowerKind.Arc,TowerKind.Observer,TowerKind.Seal}){
                int i=(int)kind;var s=new SupplyState();s.Cards[i]=85;s.Grades[i]=5;int cores=160;
                Require(Supply.GradeUp(s,kind,ref cores) && cores==5*SupplyCatalog.SurplusCores(SupplyCatalog.Rarity(kind)) && s.Cards[i]==80 && s.Valid(),"Stored surplus on grade6");
            }
            var maxState=Capped();int[] rolls={0,500,850,970};int[] values={1,2,5,20};
            for(int r=0;r<4;r++){
                int roll=rolls[r];var result=Supply.Open(maxState,BoxKind.Premium,All,1,max=>max==1000?roll:0);
                Require(result.SurplusCores==8*values[r] && result.Cores==20+8*values[r] && maxState.Cards.All(n=>n==80) && maxState.Valid(),"Converted new surplus "+r);
            }
            var overflow=new SupplyState();overflow.Grades[0]=5;overflow.Cards[0]=int.MaxValue;int full=int.MaxValue;
            Require(!Supply.GradeUp(overflow,TowerKind.Autocannon,ref full) && full==int.MaxValue && overflow.Grades[0]==5,"Overflow grade atomicity");
            overflow.Cards[0]=int.MaxValue;overflow.Grades[0]=1;byte[] original=Bytes(overflow);
            Throws<OverflowException>(()=>Supply.Open(overflow,BoxKind.Normal,Common,1,Zero),"Card overflow accepted");
            Require(Bytes(overflow).SequenceEqual(original),"Card overflow mutated state");
        }
        static void StorageAndSeed()
        {
            var s=new SupplyState{PremiumPity=23,OwnedCosmetics=5};
            for(int i=0;i<15;i++){s.Grades[i]=i%6+1;s.Cards[i]=SupplyCatalog.RequiredCards(s.Grades[i])+(s.Grades[i]==6?0:i);}
            Require(s.Valid() && Bytes(s).SequenceEqual(Bytes(Read(Bytes(s)))),"State roundtrip");
            var copy=s.Copy();copy.Cards[0]++;copy.Grades[1]=1;Require(copy.Cards[0]!=s.Cards[0] && copy.Grades[1]!=s.Grades[1],"Deep copy");
            byte[] bad=Bytes(s);bad[0]=2;Throws<InvalidDataException>(()=>Read(bad),"Unknown version accepted");
            bad=Bytes(s);bad[5]=0;Throws<InvalidDataException>(()=>Read(bad),"Invalid payload accepted");
            Throws<EndOfStreamException>(()=>Read(new byte[]{1}),"Truncated payload accepted");
            using(var m=new MemoryStream()){
                using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true)){s.Write(w);w.Write(123456);}
                m.Position=0;using(var r=new BinaryReader(m)){SupplyState.Read(r);Require(r.ReadInt32()==123456,"Embedded payload consumed following bytes");}
            }
            var invalid=new SupplyState();invalid.Cards=null;Require(!invalid.Valid(),"Null cards valid");
            invalid=new SupplyState();invalid.Grades=new int[14];Require(!invalid.Valid(),"Wrong count valid");
            foreach(int n in new[]{-1,30}){invalid=new SupplyState{PremiumPity=n};Require(!invalid.Valid(),"Invalid pity");}
            foreach(int n in new[]{-1,8}){invalid=new SupplyState{OwnedCosmetics=n};Require(!invalid.Valid(),"Invalid cosmetics");}
            foreach(int n in new[]{0,7}){invalid=new SupplyState();invalid.Grades[0]=n;Require(!invalid.Valid(),"Invalid grade");}
            invalid=new SupplyState();invalid.Cards[0]=-1;Require(!invalid.Valid(),"Negative cards");
            invalid=new SupplyState();invalid.Grades[0]=6;invalid.Cards[0]=81;Require(!invalid.Valid(),"Unconverted capped state");
            var a=new SupplyState();var b=new SupplyState();var ra=new Random(600067);var rb=new Random(600067);
            for(int n=0;n<500;n++){
                var x=Supply.Open(a,(BoxKind)(n%2),All,n%5,ra);var y=Supply.Open(b,(BoxKind)(n%2),All,n%5,rb);
                Require(x.Cards.SequenceEqual(y.Cards) && x.Cosmetics.SequenceEqual(y.Cosmetics) && x.Rarities.SequenceEqual(y.Rarities) && x.Cores==y.Cores && x.PityFired==y.PityFired && Bytes(a).SequenceEqual(Bytes(b)),"Fixed seed determinism");
            }
        }
        public static string Run()
        {
            var lines=new List<string>();Odds();lines.Add("PASS catalog mapping, exact 100% odds for both boxes and every valid unlock subset");
            lines.Add(Simulation(BoxKind.Normal));lines.Add(Simulation(BoxKind.Premium));UnlocksAndPity();
            lines.Add("PASS locked rewards, repeated fallback, exactly 30th pity, pending unlock, resets, unique cosmetics, rejected-open atomicity");
            GradesAndBoxes();lines.Add("PASS chapter counts, core ranges, all fifteen cumulative grades/costs, +10% cap, surplus and overflow");
            StorageAndSeed();lines.Add("PASS versioned embedded roundtrip, deep copy, invalid state rejection and fixed seed");return string.Join(Environment.NewLine,lines);
        }
    }
}
