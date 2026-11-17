using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class MobileHud
    {
        void DailyPage()
        {
            Fill(Full(),GatefallUiStyle.ColorMaterialGraphite);
            var d=controller.Campaign.DailyStatus();
            string title=controller.ScreenPage==CampaignScreen.Roulette?"광고 룰렛":controller.ScreenPage==CampaignScreen.Missions?"일일 임무":controller.ScreenPage==CampaignScreen.Attendance?"출석":"방어 기록 패스";
            Button(603,layout.DailyBack(),"‹",!controller.DailyAdPending,Look.Secondary,null,24);
            Text(HomeRect(layout.DailyTitle()),title+(controller.ScreenPage==CampaignScreen.Pass?" · 시즌 "+DailyCatalog.SeasonNumber(d.LocalDay)+" · "+DailyLabels.DaysLeft(d)+"일 남음":""),24,Paper,TextAnchor.MiddleLeft,2);
            if(controller.ScreenPage==CampaignScreen.Roulette)Roulette(d);
            else if(controller.ScreenPage==CampaignScreen.Missions)Missions(d);
            else if(controller.ScreenPage==CampaignScreen.Attendance)Attendance(d);
            else Pass(d);
            if(!controller.RouletteDisclosureOpen && controller.ScreenPage!=CampaignScreen.Pass)
                Text(HomeRect(layout.DailyFooter()),controller.CampaignError,14,Threat,TextAnchor.MiddleCenter,1);
            if(controller.DailyAdPending)controller.TouchInput.Regions.Clear();
        }
        void Roulette(DailyState d)
        {
            if(controller.RouletteDisclosureOpen) {
                Text(HomeRect(layout.DailySummary()),"확률형 아이템 포함 · 모든 결과의 확률",18,Muted,TextAnchor.MiddleLeft,1);
                var rows=DailyCatalog.RouletteDisclosure();int visible=layout.DailyVisibleRows(),first=Mathf.Min(controller.DailyFirst,Mathf.Max(0,rows.Length-visible));
                for(int i=0;i<visible && first+i<rows.Length;i++) {
                    var row=rows[first+i];Rect r=HomeRect(layout.DailyRow(i));Skin(r,"ui-chip");
                    Text(new Rect(r.x+16,r.y,r.width-32,r.height),DailyLabels.Reward(row.Reward)+" · "+row.Percent.ToString("0.##")+"%",18,Paper,TextAnchor.MiddleLeft,1);
                }
                DailyScroll(first,rows.Length);return;
            }
            Rect summary=HomeRect(layout.DailySummary());summary.width-=144;
            Text(summary,"오늘 남은 "+(DailyCatalog.RouletteDailyLimit-d.RouletteSpins)+"/3 · 4시간 간격",20,Paper,TextAnchor.MiddleLeft,1);
            Button(882,layout.RouletteOdds(),"확률",true,Look.Secondary,null,18);
            Text(new Rect(48,180,W-96,48),"다음 룰렛 · "+HeartTime(controller.RouletteWaitSeconds),26,Amber,TextAnchor.MiddleCenter,2);
            Text(new Rect(48,244,W-96,48),controller.RouletteResult==""?"광고를 완료하면 룰렛 보상을 받습니다":controller.RouletteResult,22,Paper,TextAnchor.MiddleCenter,2);
            Text(new Rect(48,308,W-96,48),"확률형 아이템 포함 · 상자 보상은 이용권으로 지급",16,Muted,TextAnchor.MiddleCenter,0);
            string reason=controller.Campaign.SpinReason();bool ready=controller.RewardedAds!=null && controller.RewardedAds.IsReady;
            string label=controller.DailyAdPending?"광고 확인 중":reason!=""?reason:!ready?"광고 준비 중":"광고 보고 돌리기";
            Button(881,layout.DailyPrimary(),label,reason=="" && ready && !controller.DailyAdPending && !controller.SaveBlocked,Look.Primary,null,20);
        }
        void Missions(DailyState d)
        {
            Text(HomeRect(layout.DailySummary()),"오늘 수령 "+DailyLabels.ClaimedMissions(d)+"/6 · 임무 보상은 직접 받으세요",18,Muted,TextAnchor.MiddleLeft,1);
            var rows=DailyCatalog.Missions();
            for(int i=0;i<rows.Length;i++) {
                Rect r=HomeRect(layout.MissionRow(i));Skin(r,"ui-chip");var row=rows[i];bool claimed=(d.MissionClaims&(1<<i))!=0;
                Text(new Rect(r.x+12,r.y,W/2-92,r.height),DailyLabels.Missions[i]+" · "+d.Progress[i]+"/"+row.Target,17,claimed?Muted:Paper,TextAnchor.MiddleLeft,1);
                Text(new Rect(W/2-48,r.y,W/2-132,r.height),"마핵 "+row.MagicCores+" · 기록 점수 "+row.PassPoints,15,Amber,TextAnchor.MiddleLeft,1);
                Button(900+i,layout.MissionClaim(i),claimed?"수령 완료":d.Progress[i]>=row.Target?"받기":"진행 중",!claimed && !controller.SaveBlocked,DailyLabels.MissionReady(controller.Profile,d,i)?Look.Primary:Look.Secondary,null,16);
            }
            Text(HomeRect(layout.MissionAllClear()),"전체 완료 · 일반 상자 이용권 1 · "+(d.MissionClaims==63?"수령 완료":"6개 보상 수령 시 자동 지급"),17,d.MissionClaims==63?Ally:Muted,TextAnchor.MiddleCenter,1);
        }
        void Attendance(DailyState d)
        {
            Text(HomeRect(layout.DailySummary()),"7일 보상 · 하루 빠져도 이어집니다",18,Muted,TextAnchor.MiddleLeft,1);
            var rows=DailyCatalog.Attendance();
            for(int i=0;i<rows.Length;i++) {
                Rect r=HomeRect(layout.AttendanceDay(i));Skin(r,"ui-chip");string state=DailyLabels.AttendanceState(d,i);
                Text(new Rect(r.x+4,r.y+12,r.width-8,28),(i+1)+"일",20,state=="오늘"?Amber:Paper,TextAnchor.MiddleCenter,2);
                Text(new Rect(r.x+8,r.y+52,r.width-16,64),DailyLabels.Reward(rows[i]),16,Paper,TextAnchor.MiddleCenter,1);
                Text(new Rect(r.x+4,r.yMax-38,r.width-8,28),state,14,state=="오늘"?Ally:Muted,TextAnchor.MiddleCenter,1);
            }
            bool claimed=d.AttendanceDay>=d.LocalDay;
            Button(883,layout.DailyPrimary(),claimed?"오늘 수령 완료":"오늘 보상 받기",!claimed && !controller.SaveBlocked,Look.Primary,null,20);
        }
        void Pass(DailyState d)
        {
            Rect summary=HomeRect(layout.DailySummary());summary.width-=292;
            Text(summary,"기록 "+d.PassPoints+" · 오늘 "+d.PointsToday+"/"+(DailyCatalog.DailyPointCap*(d.CapRaised?2:1)),18,Paper,TextAnchor.MiddleLeft,1);
            Button(884,layout.PassCap(),d.CapRaised?"오늘 한도 확장 완료":"결정 "+DailyCatalog.CapCrystalPrice+" · 한도 +"+DailyCatalog.DailyPointCap,!d.CapRaised && !controller.SaveBlocked,Look.Secondary,null,16);
            var rows=DailyCatalog.PassRewards();int visible=layout.DailyVisibleRows(),first=Mathf.Min(controller.DailyFirst,Mathf.Max(0,rows.Length-visible));
            for(int i=0;i<visible && first+i<rows.Length;i++) {
                var row=rows[first+i];Rect r=HomeRect(layout.DailyRow(i));Skin(r,"ui-chip");
                Text(new Rect(r.x+12,r.y,84,r.height),row.Tier+"단계\n"+(row.Tier*DailyCatalog.PointsPerTier)+"점",14,Paper,TextAnchor.MiddleLeft,1);
                for(int track=0;track<2;track++) {
                    bool paid=track==1,claimed=((paid?d.PaidClaims:d.FreeClaims)&(1<<(row.Tier-1)))!=0,unlocked=d.PassPoints>=row.Tier*DailyCatalog.PointsPerTier;
                    Box2 button=layout.PassClaim(i,paid);float x=paid?W/2+24:132;
                    Text(new Rect(x,r.y+2,button.X-x-12,52),(paid?d.PassPurchased?"유료":"유료 · 잠김":"무료")+"\n"+DailyLabels.Reward(paid?row.Paid:row.Free),14,paid && !d.PassPurchased?Muted:Paper,TextAnchor.MiddleLeft,1);
                    string label=paid && !d.PassPurchased?"출시 준비 중":claimed?"수령 완료":!unlocked?"점수 잠김":"받기";
                    Button((paid?950:920)+row.Tier-1,button,label,unlocked && !claimed && (!paid || d.PassPurchased) && !controller.SaveBlocked,DailyLabels.PassReady(controller.Profile,d,row,paid)?Look.Primary:Look.Secondary,null,14);
                }
            }
            DailyScroll(first,rows.Length);
        }
        void DailyScroll(int first,int count)
        {
            int last=Mathf.Min(first+layout.DailyVisibleRows(),count);
            Button(885,layout.DailyScroll(false),"위로",first>0,Look.Secondary,null,17);
            Button(886,layout.DailyScroll(true),"아래로",last<count,Look.Secondary,null,17);
            Text(HomeRect(layout.DailyPageLabel()),controller.CampaignError!=""?controller.CampaignError:(first+1)+"–"+last+" / "+count,14,controller.CampaignError!=""?Threat:Muted,TextAnchor.MiddleCenter,1);
        }
    }
}
