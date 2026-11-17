using System;

namespace Gatefall
{
    public struct HomeMenuEntry
    {
        public int Action, UnlockStage;
        public string Name, Detail, Glyph;
        public HomeMenuEntry(int action,string name,string detail,string glyph,int unlockStage=-1)
        { Action=action;Name=name;Detail=detail;Glyph=glyph;UnlockStage=unlockStage; }
    }
    public static class HomeMenu
    {
        public static readonly HomeMenuEntry[] Tabs={
            new HomeMenuEntry(680,"상점","카드 · 보급","ui-glyph-ward",2),
            new HomeMenuEntry(652,"결계주","카드 · 편성","ui-glyph-loadout",2),
            new HomeMenuEntry(606,"작전 출전","♥1","ui-glyph-play"),
            new HomeMenuEntry(602,"성장","헌터 · 연구","ui-glyph-hunter"),
            new HomeMenuEntry(600,"작전 지도","작전 · 기록","ui-glyph-map")};
        public static readonly HomeMenuEntry[] Right={
            new HomeMenuEntry(681,"광고 룰렛","하루 3회","ui-glyph-ward"),
            new HomeMenuEntry(682,"일일 임무","오늘의 기록","ui-glyph-map"),
            new HomeMenuEntry(683,"출석","7일 보상","ui-glyph-hunter")};
    }
    public interface IRewardedAdService
    {
        bool IsReady { get; }
        void Show(string placement,Action onRewarded,Action onFailed);
    }
    public sealed class FakeRewardedAdService : IRewardedAdService
    {
        public bool IsReady { get; set; } = true;
        public bool Succeeds { get; set; } = true;
        public void Show(string placement,Action onRewarded,Action onFailed)
        { if(IsReady && Succeeds)onRewarded();else onFailed(); }
    }
}
