using System;
using System.IO;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    // v0.5 flow: City (operation map + briefing in one) -> Battle (preparation, defence, result) -> City.
    // Growth merges hunters and workshop research; Settings holds sound, records and the developer lab.
    public enum CampaignScreen { Battle, City, Growth, Settings, Loadout }
    public sealed partial class GatefallGame
    {
        internal const int OperationsPerPage = 6;
        internal static int MapPageCount { get { return (CampaignCatalog.Stages.Length + OperationsPerPage - 1) / OperationsPerPage; } }
        public CampaignScreen ScreenPage { get; private set; } = CampaignScreen.City;
        public CampaignService Campaign { get; private set; }
        public CampaignProfile Profile { get { return Campaign.Profile; } }
        public int SelectedStage { get; private set; }
        public int MapPage { get; private set; }
        public bool CrisisSelection { get; private set; }
        public int ResultReward { get; private set; }
        public bool ResultSaved { get; private set; }
        public string CampaignError { get; private set; } = "";
        public bool SaveBlocked { get; private set; }
        public string CurrentRunId { get; private set; } = "";
        CampaignBattle activeBattle = new CampaignBattle();
        public bool IsCampaign { get { return Simulation != null && Simulation.Rules != null && !Simulation.Rules.Lab; } }
        public bool ShowLesson { get { return Profile.ShouldShowLesson(SelectedStage); } }
        void InitCampaign()
        {
            var store = new CampaignStore(Path.Combine(Application.persistentDataPath, "raon-profile.sav"));
            CampaignProfile p;
            try { p = store.Load(); CampaignError = store.Warning; }
            catch (Exception e) { p = new CampaignProfile(); SaveBlocked = true; CampaignError = e.Message; }
            Campaign = new CampaignService(p, profile => {
                if (SaveBlocked) throw new IOException("프로필 복구가 필요합니다. 원본을 보존 중입니다.");
                store.Save(profile);
            });
            sound.Muted = Profile.Muted;
            SelectFrontier();
        }
        // The most advanced open operation that is not yet cleared (or the last one).
        void SelectFrontier()
        {
            SelectedStage = 0;
            for (int i = 0; i < CampaignCatalog.Stages.Length; i++) if (Profile.IsStageOpen(i)) SelectedStage = i;
            SelectMapStage(SelectedStage);
        }
        private void SelectMapStage(int stage)
        {
            SelectedStage = stage;
            MapPage = stage / OperationsPerPage;
        }
        private int LatestOpenOnPage(int page)
        {
            if (page < 0 || page >= MapPageCount) return -1;
            int start = page * OperationsPerPage;
            for (int stage = Math.Min(start + OperationsPerPage, CampaignCatalog.Stages.Length) - 1; stage >= start; stage--)
                if (Profile.IsStageOpen(stage)) return stage;
            return -1;
        }
        internal bool CanMoveMapPage(int direction) { return LatestOpenOnPage(MapPage + direction) >= 0; }
        void SetPage(CampaignScreen page) { ScreenPage = page; Menu = GameMenu.None; TouchInput.Reset(); }
        public void BeginPreparation()
        {
            string id;
            if (!Campaign.Begin(SelectedStage, CrisisSelection, out id)) { CampaignError = Campaign.Error; return; }
            CurrentRunId = id; activeBattle = Campaign.Battle(SelectedStage, CrisisSelection);
            ResultReward = 0; ResultSaved = false; CampaignError = ""; NewRun(false); SetPage(CampaignScreen.Battle);
            ShowNotice("배치 준비 · 시간이 멈춰 있습니다", 2.4f);
        }
        void BeginLab()
        {
            CurrentRunId = ""; activeBattle = new CampaignBattle(); ResultReward = 0; ResultSaved = false;
            NewRun(false); SetPage(CampaignScreen.Battle); ShowNotice("전투 실험실 · 모든 결계주 해금 · 보상 없음", 2.6f);
        }
        void ReturnCity()
        {
            if(ScreenPage==CampaignScreen.Battle && IsCampaign && CurrentRunId!="") {
                bool ended=Simulation.State==RunState.Won || Simulation.State==RunState.Lost;
                if(ended) {
                    if(!ResultSaved)SaveCampaignOutcome();
                    if(!ResultSaved)return; // Keep the earned result and its existing save-retry screen.
                } else {
                    if(Simulation.State==RunState.Running)Simulation.TogglePause();
                    if(!Campaign.Abandon(CurrentRunId)) {
                        CampaignError=Campaign.Error;Menu=GameMenu.Pause;TouchInput.Reset();return;
                    }
                    CampaignError="";
                }
            }
            if (Simulation.State == RunState.Running) Simulation.TogglePause();
            CurrentRunId = ""; SetPage(CampaignScreen.City);
        }
        void SaveCampaignOutcome()
        {
            if (!IsCampaign) { ResultSaved = true; return; }
            int reward; ResultSaved = Campaign.Complete(CurrentRunId, Simulation, out reward);
            ResultReward = reward; CampaignError = ResultSaved ? "" : Campaign.Error;
        }
        bool CampaignAction(int action)
        {
            if (action < 600) return false;
            if (action == 600) { ReturnCity(); return true; }
            if (action == 601) { BeginLab(); return true; }
            if (action == 602) { SetPage(CampaignScreen.Growth); return true; }
            if (action == 604) { SetPage(CampaignScreen.Settings); return true; }
            if (action == 605 && Profile.FirstSealComplete) { CrisisSelection = !CrisisSelection; return true; }
            if (action == 606) { BeginPreparation(); return true; }
            if (action == 607 && IsCampaign && ResultSaved && SelectedStage < CampaignCatalog.Stages.Length-1 && Profile.IsStageOpen(SelectedStage + 1)) {
                SelectMapStage(SelectedStage + 1); ReturnCity(); return true;
            }
            if (action >= 610 && action < 616 && Profile.IsStageOpen(action - 610)) { SelectMapStage(action - 610); return true; }
            if(action==652 && Profile.FirstCleared[9]) { SetPage(CampaignScreen.Loadout);return true; }
            if (action == 655 || action == 656) {
                int stage = LatestOpenOnPage(MapPage + (action == 655 ? -1 : 1));
                if (stage >= 0) SelectMapStage(stage);
                return true;
            }
            if(action>=710 && action<710+CampaignCatalog.Stages.Length) { int stage=action-710;if(Profile.IsStageOpen(stage))SelectMapStage(stage);return true; }
            if(action>=700 && action<=704) { int[] kinds={0,1,2,7,8};int bit=1<<kinds[action-700];if(!Campaign.SetLoadout(Profile.Loadout^bit))CampaignError=Campaign.Error;else CampaignError="";return true; }
            if (action >= 620 && action < 623) { if (!Campaign.SelectHunter(action - 620)) CampaignError = Campaign.Error; return true; }
            if (action == 623) {
                for (int k = 1; k <= 3; k++) {
                    int next = (Profile.SelectedHunter + k) % 3;
                    if (Profile.IsHunterOpen(next)) { if (next != Profile.SelectedHunter && !Campaign.SelectHunter(next)) CampaignError = Campaign.Error; break; }
                }
                return true;
            }
            if (action >= 630 && action < 633) { if (!Campaign.Grow(action - 630)) CampaignError = Campaign.Error; return true; }
            if (action >= 640 && action < 644) { if (!Campaign.Research(action - 640)) CampaignError = Campaign.Error; return true; }
            if (action == 645) { if (!Campaign.Respec()) CampaignError = Campaign.Error; return true; }
            if (action == 651) { ToggleSound(); return true; }
            if (action == 670) { Simulation.UseSkill(); return true; }
            if (action == 690) { SaveCampaignOutcome(); return true; }
            return true;
        }
    }
}
