using System;

namespace Gatefall.Core
{
    public enum TowerKind { Autocannon, Binding, Rupture, Arc, Shatter, Gravity, Seal, Barrier, Marker, Observer, Beacon }
    [Flags] public enum TowerTag { None = 0, Ballistic = 1, Control = 2, Blast = 4, Ward = 8 }
    public enum CardKind { Ballistic, Control, Blast, Range, Credits, Hunter }

    // Values are Gatefall's approved v2 prototype design, independently implemented.
    public sealed class TowerStats
    {
        public float Damage, Secondary, Interval, Range, Radius, Slow, SlowSeconds, ChainRange, MarkBonus;
        public int MaxTargets;
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
                (Slow > 0 ? "\n감속 " + (Slow * 100).ToString("0") + "% / " + SlowSeconds.ToString("0.#") + "초" : "");
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
        public string Ingredients { get { return TowerCatalog.Get(First).Name + " Lv2+ / " + TowerCatalog.Get(Second).Name + " Lv2+"; } }
        public bool Matches(Tower a, Tower b)
        { return a != null && b != null && a.Id != b.Id && a.Level >= 2 && b.Level >= 2 &&
            ((a.Kind == First && b.Kind == Second) || (a.Kind == Second && b.Kind == First)); }
    }
    public static class TowerCatalog
    {
        private static TowerStats S(float damage, float interval, float range, int max = 1, float radius = 0,
            float slow = 0, float seconds = 0, float secondary = 0, float[] chain = null, float mark = 0)
        { return new TowerStats { Damage = damage, Interval = interval, Range = range, MaxTargets = max, Radius = radius,
            Slow = slow, SlowSeconds = seconds, Secondary = secondary, Chain = chain, ChainRange = chain == null ? 0 : 2.5f, MarkBonus = mark }; }
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
                S(5,1.1f,5.2f,3,0,.30f,1.2f,mark:5),S(6,.95f,5.2f,4,0,.35f,1.35f,mark:5),S(8,.85f,5.5f,4,0,.40f,1.5f,mark:6))
        };
        public static readonly FusionRecipe[] Recipes = {
            R(0,TowerKind.Autocannon,TowerKind.Binding,TowerKind.Arc,40),
            R(1,TowerKind.Autocannon,TowerKind.Rupture,TowerKind.Shatter,45),
            R(2,TowerKind.Binding,TowerKind.Rupture,TowerKind.Gravity,45),
            R(3,TowerKind.Arc,TowerKind.Rupture,TowerKind.Seal,80),
            R(4,TowerKind.Shatter,TowerKind.Binding,TowerKind.Seal,75),
            R(5,TowerKind.Gravity,TowerKind.Autocannon,TowerKind.Seal,75),
            R(6,TowerKind.Marker,TowerKind.Autocannon,TowerKind.Observer,40),
            R(7,TowerKind.Marker,TowerKind.Binding,TowerKind.Beacon,50),
            R(8,TowerKind.Observer,TowerKind.Rupture,TowerKind.Seal,80)
        };
        private static FusionRecipe R(int id, TowerKind a, TowerKind b, TowerKind result, int cost)
        { return new FusionRecipe { Id = id, First = a, Second = b, Result = result, Credits = cost }; }
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

