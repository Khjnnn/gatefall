using System;

namespace Gatefall.Core
{
    public enum TowerKind { Autocannon, Binding, Rupture, Arc, Shatter, Gravity, Seal, Barrier, Marker, Observer, Beacon, Scatter, Barrage, Piercer, Surge }
    [Flags] public enum TowerTag { None = 0, Ballistic = 1, Control = 2, Blast = 4, Ward = 8 }
    public enum CardKind { Ballistic, Control, Blast, Range, Credits, Hunter }
    public enum SealInheritance { None, ChainBurst, ShatterSlow, GravitySpear, MarkedBlast, FanSlow, LineBurst }

    // Values are Gatefall's approved v2 prototype design, independently implemented.
    public sealed class TowerStats
    {
        public float Damage, Secondary, Interval, Range, Radius, Slow, SlowSeconds, ChainRange, MarkBonus;
        public float ConeCos, LineHalfWidth;
        public int MaxTargets;
        public int TailBurstTargets, ConeSlowTargets;
        public float MarkedDamageMultiplier, MarkedRadiusMultiplier;
        public SealInheritance Inheritance;
        public float[] Chain;
        public TowerStats Copy()
        {
            TowerStats copy = (TowerStats)MemberwiseClone();
            if (Chain != null) copy.Chain = (float[])Chain.Clone();
            return copy;
        }
        public string Summary()
        {
            if(Damage==0 && Chain==null && MaxTargets==2)
                return "최대2명 · 기본3초 저지\n재충전 "+Interval.ToString("0.##")+"초\n범위 "+Range.ToString("0.#")+" · 면역4초";
            string damage = Chain != null ? string.Join("→", Array.ConvertAll(Chain, x => x.ToString("0.#"))) :
                Secondary > 0 ? Damage.ToString("0.#") + "/주변 " + Secondary.ToString("0.#") : Damage.ToString("0.#");
            return "피해 " + damage + " · " + Interval.ToString("0.##") + "초\n사거리 " + Range.ToString("0.#") +
                " · 최대 " + MaxTargets + "명" + (Radius > 0 ? " · 반경 " + Radius.ToString("0.#") : "") +
                (Slow > 0 ? "\n감속 " + (Slow * 100).ToString("0") + "% / " + SlowSeconds.ToString("0.#") + "초" : "") +
                (MarkedRadiusMultiplier>1 ? "\n표식 명중: 직격 ×"+MarkedDamageMultiplier.ToString("0.#")+" · 폭발 반경 ×"+MarkedRadiusMultiplier.ToString("0.#") : "");
        }
    }
    public sealed class TowerDefinition
    {
        public TowerKind Kind;
        public string Name, Role;
        public int Tier, BuildCost, UpgradeCost;
        public TowerTag Tags;
        public TowerStats[] Levels;
        public int UpgradePrice(int currentLevel) { return currentLevel == 1 ? UpgradeCost : UpgradeCost + 35; }
    }
    public sealed class FusionRecipe
    {
        public int Id, Credits;
        public TowerKind First, Second, Result;
        public SealInheritance Inheritance;
        public string ResultName => TowerCatalog.DisplayName(Result, Inheritance);
        public string InheritedRole => TowerCatalog.InheritedRole(Inheritance);
        public string Ingredients { get { return TowerCatalog.Get(First).Name + " Lv2+ / " + TowerCatalog.Get(Second).Name + " Lv2+"; } }
        public bool Matches(Tower a, Tower b)
        { return a != null && b != null && a.Id != b.Id && a.Level >= 2 && b.Level >= 2 &&
            ((a.Kind == First && b.Kind == Second) || (a.Kind == Second && b.Kind == First)); }
    }
    public static class TowerCatalog
    {
        private static TowerStats S(float damage, float interval, float range, int max = 1, float radius = 0,
            float slow = 0, float seconds = 0, float secondary = 0, float[] chain = null, float mark = 0, float cone = 0, float line = 0)
        { return new TowerStats { Damage = damage, Interval = interval, Range = range, MaxTargets = max, Radius = radius,
            Slow = slow, SlowSeconds = seconds, Secondary = secondary, Chain = chain, ChainRange = chain == null ? 0 : 2.5f, MarkBonus = mark, ConeCos=cone, LineHalfWidth=line }; }
        private static TowerDefinition D(TowerKind kind, string name, string role, int tier, int build, int upgrade, TowerTag tags, params TowerStats[] levels)
        { return new TowerDefinition { Kind = kind, Name = name, Role = role, Tier = tier, BuildCost = build, UpgradeCost = upgrade, Tags = tags, Levels = levels }; }
        public static readonly TowerDefinition[] All = {
            D(TowerKind.Autocannon,"창 결계주","선두 적 단일 빛창",1,40,30,TowerTag.Ballistic,S(10,.5f,5.2f),S(16,.5f,5.2f),S(22,.5f,5.5f)),
            D(TowerKind.Binding,"봉인 결계주","구속 사슬 · 감속",1,45,40,TowerTag.Control,S(3,.9f,4.8f,3,0,.35f,1.2f),S(3,.9f,4.8f,4,0,.45f,1.2f),S(5,.85f,5f,5,0,.50f,1.4f)),
            D(TowerKind.Rupture,"붕괴 결계주","밀집 지점 폭격",1,65,55,TowerTag.Blast,S(32,2,5,5,1.6f),S(48,2,5,6,1.9f),S(62,1.9f,5.2f,7,2.1f)),
            D(TowerKind.Arc,"뇌격 결계주","서로 다른 3명 연쇄",2,0,60,TowerTag.Ballistic|TowerTag.Control,
                S(24,.75f,5.4f,3,0,.25f,1,0,new float[]{24,16,10}),S(30,.65f,5.4f,3,0,.3f,1.3f,0,new float[]{30,20,14}),
                S(38,.62f,5.7f,3,0,.35f,1.4f,0,new float[]{38,26,17})),
            D(TowerKind.Shatter,"파쇄 결계주","주 대상 집중 + 폭발",2,0,60,TowerTag.Ballistic|TowerTag.Blast,
                S(64,1,5.4f,5,1.4f,0,0,32),S(84,.9f,5.4f,6,1.6f,0,0,42),S(105,.86f,5.7f,7,1.8f,0,0,52)),
            D(TowerKind.Gravity,"중력 결계주","범위 피해 + 강한 감속",2,0,60,TowerTag.Control|TowerTag.Blast,
                S(28,1.4f,5.2f,6,1.9f,.5f,1.5f),S(42,1.2f,5.2f,8,2.2f,.55f,1.5f),S(54,1.15f,5.4f,9,2.4f,.58f,1.65f)),
            D(TowerKind.Seal,"종결 결계주","최종 집중 방어",3,0,0,TowerTag.Ballistic|TowerTag.Control|TowerTag.Blast,
                S(120,1,6.4f,4,2,.35f,1.5f,60)),
            D(TowerKind.Barrier,"방벽 결계주","파괴 시 감속 충격파",1,40,35,TowerTag.Ward,S(0,8,3.4f,2),S(0,6,3.4f,2),S(0,5,3.4f,2)),
            D(TowerKind.Marker,"표식 결계주","아군 집중 사격 유도",1,45,40,TowerTag.None,S(4,.9f,5.2f,mark:3),S(6,.9f,5.2f,mark:5),S(8,.8f,5.5f,mark:6)),
            D(TowerKind.Observer,"관측창 결계주","표식 적 집중 공격",2,0,60,TowerTag.Ballistic,S(36,.8f,5.2f,mark:5),S(48,.7f,5.2f,mark:5),S(60,.65f,5.5f,mark:6)),
            D(TowerKind.Beacon,"추적 결계주","여러 적에게 표식과 감속",2,0,60,TowerTag.Control,
                S(5,1.1f,5.2f,3,0,.30f,1.2f,mark:5),S(6,.95f,5.2f,4,0,.35f,1.35f,mark:5),S(8,.85f,5.5f,4,0,.40f,1.5f,mark:6)),
            D(TowerKind.Scatter,"산탄 결계주","가까운 부채꼴 · 장갑에 약함",1,50,45,TowerTag.Ballistic,
                S(9,.65f,3.7f,4,cone:.8660254f),S(14,.60f,3.9f,5,cone:.8660254f),S(19,.55f,4.1f,6,cone:.8660254f)),
            D(TowerKind.Barrage,"탄막 결계주","넓은 부채꼴 · 장갑 관통",2,0,65,TowerTag.Ballistic,
                S(26,.65f,4.8f,7,cone:.7660444f),S(36,.60f,5f,8,cone:.7660444f),S(46,.55f,5.2f,9,cone:.7660444f)),
            D(TowerKind.Piercer,"관통 결계주","직선 관통 · 장갑에 약함",1,55,45,TowerTag.Ballistic,
                S(15,.95f,5.8f,3,line:.45f),S(22,.90f,6.1f,4,line:.5f),S(30,.85f,6.4f,4,line:.55f)),
            D(TowerKind.Surge,"쇄도 결계주","직선 피해와 감속",2,0,65,TowerTag.Ballistic|TowerTag.Control,
                S(28,1.05f,6.2f,4,slow:.20f,seconds:.8f,line:.55f),S(39,1f,6.5f,5,slow:.24f,seconds:.8f,line:.60f),
                S(49,.95f,6.8f,5,slow:.28f,seconds:.8f,line:.65f))
        };
        public static readonly TowerKind[] Buildable = { TowerKind.Autocannon,TowerKind.Binding,TowerKind.Rupture,
            TowerKind.Barrier,TowerKind.Marker,TowerKind.Scatter,TowerKind.Piercer };
        public static readonly FusionRecipe[] Recipes = {
            R(0,TowerKind.Autocannon,TowerKind.Binding,TowerKind.Arc,40),
            R(1,TowerKind.Autocannon,TowerKind.Rupture,TowerKind.Shatter,45),
            R(2,TowerKind.Binding,TowerKind.Rupture,TowerKind.Gravity,45),
            R(3,TowerKind.Arc,TowerKind.Rupture,TowerKind.Seal,80,SealInheritance.ChainBurst),
            R(4,TowerKind.Shatter,TowerKind.Binding,TowerKind.Seal,75,SealInheritance.ShatterSlow),
            R(5,TowerKind.Gravity,TowerKind.Autocannon,TowerKind.Seal,75,SealInheritance.GravitySpear),
            R(6,TowerKind.Marker,TowerKind.Autocannon,TowerKind.Observer,40),
            R(7,TowerKind.Marker,TowerKind.Binding,TowerKind.Beacon,50),
            R(8,TowerKind.Observer,TowerKind.Rupture,TowerKind.Seal,80,SealInheritance.MarkedBlast),
            R(9,TowerKind.Scatter,TowerKind.Rupture,TowerKind.Barrage,50),
            R(10,TowerKind.Barrage,TowerKind.Binding,TowerKind.Seal,80,SealInheritance.None),
            R(11,TowerKind.Piercer,TowerKind.Binding,TowerKind.Surge,50),
            R(12,TowerKind.Surge,TowerKind.Rupture,TowerKind.Seal,80,SealInheritance.LineBurst)
        };
        private static FusionRecipe R(int id, TowerKind a, TowerKind b, TowerKind result, int cost, SealInheritance inheritance = SealInheritance.None)
        { return new FusionRecipe { Id = id, First = a, Second = b, Result = result, Credits = cost, Inheritance = inheritance }; }

        static readonly TowerStats[] InheritedSealStats = {
            null,
            S(102,1.13f,6.4f,3,1.65f,secondary:48,chain:new[]{102f,80f,62f}),
            S(158,1f,6.4f,4,1.75f,.38f,1.05f,48),
            S(56,1.15f,6.4f,6,2.25f,.47f,1.35f,72),
            S(120,1f,6.4f,4,2f,.30f,1.5f,60,mark:5),
            S(57,1.02f,6f,7,slow:.32f,seconds:1.1f,cone:.7071068f),
            S(110,1.05f,7f,5,1.5f,.48f,.95f,60,line:.75f)
        };

        internal static TowerStats CopyInheritedSealStats(SealInheritance inheritance)
        {
            TowerStats stats = InheritedSealStats[(int)inheritance].Copy();
            stats.Inheritance = inheritance;
            if (inheritance == SealInheritance.ChainBurst) {
                stats.ChainRange = 3.2f;
                stats.TailBurstTargets = 3;
            }
            if (inheritance == SealInheritance.LineBurst) stats.TailBurstTargets = 3;
            if (inheritance == SealInheritance.FanSlow) stats.ConeSlowTargets = 2;
            if (inheritance == SealInheritance.MarkedBlast) {
                stats.MarkedDamageMultiplier = 1.4f;
                stats.MarkedRadiusMultiplier = 2;
            }
            return stats;
        }

        public static string DisplayName(TowerKind kind, SealInheritance inheritance)
        {
            switch (inheritance) {
                case SealInheritance.ChainBurst: return "연쇄 종결";
                case SealInheritance.ShatterSlow: return "파쇄 종결";
                case SealInheritance.GravitySpear: return "중력 종결";
                case SealInheritance.MarkedBlast: return "관측 종결";
                case SealInheritance.FanSlow: return "탄막 종결";
                case SealInheritance.LineBurst: return "쇄도 종결";
                default: return Get(kind).Name;
            }
        }

        public static string InheritedRole(SealInheritance inheritance)
        {
            switch (inheritance) {
                case SealInheritance.ChainBurst: return "연쇄 끝 폭발";
                case SealInheritance.ShatterSlow: return "정예 직격 · 주변 감속";
                case SealInheritance.GravitySpear: return "범위 감속 · 선두 빛창";
                case SealInheritance.MarkedBlast: return "표식 집중 · 표식 명중 반경 2배";
                case SealInheritance.FanSlow: return "부채꼴 · 선두 감속";
                case SealInheritance.LineBurst: return "직선 감속 · 끝 폭발";
                default: return "";
            }
        }

        public static TowerDefinition Get(TowerKind kind) { return All[(int)kind]; }
        public static TowerTag CardTag(CardKind kind)
        {
            switch (kind) {
                case CardKind.Ballistic: return TowerTag.Ballistic;
                case CardKind.Control: return TowerTag.Control;
                case CardKind.Blast: return TowerTag.Blast;
                default: return TowerTag.None;
            }
        }
        public static string CardName(CardKind kind)
        { return new[]{"빛창 증폭", "봉인 연장", "붕괴 확장", "감시망 확장", "긴급 보급", "각성자 가속"}[(int)kind]; }
        public static string CardText(CardKind kind)
        { return new[]{"탄도 태그 피해 +15%", "제어 태그 감속 지속 +0.4초", "폭발 태그 반경 +0.25", "모든 타워 사거리 +10%", "즉시 마석 +35", "헌터 공격 간격 -10%"}[(int)kind]; }
    }
}

