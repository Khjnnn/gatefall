using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Controlled, non-human test plan for reproducing a real hunter respawn.
    public static class RespawnBattleReview {
        public static string Run() {
            var game=new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=CampaignCatalog.Stages[10],MarkerUnlocked=true,Loadout=263});
            string reason;
            foreach(int slot in new[]{4,1,0})if(!game.TryBuild(slot,TowerKind.Autocannon,null,out reason))throw new Exception(reason);
            game.Start();var lines=new List<string>();bool sawDown=false,sawRevive=false;
            for(int i=0;i<5000 && (game.State==RunState.Running || game.AwaitingCard);i++) {
                if(game.AwaitingCard)game.ChooseCard(game.CardOffers[0]);
                V2 target=new V2(16,3);V2 delta=target-game.PlayerPosition;
                V2 movement=game.Elapsed<12 && delta.Length>.2f ? delta.Normalized : new V2();
                game.Tick(.05f,movement);
                foreach(CombatEvent e in game.Events)if(e.Kind==EventKind.HunterDown || e.Kind==EventKind.HunterRevived) {
                    lines.Add(e.Kind+" time="+game.Elapsed.ToString("F2")+" core="+game.CoreHealth.ToString("F1")+" player="+game.PlayerHealth.ToString("F1"));
                    if(e.Kind==EventKind.HunterDown)sawDown=true;else if(sawDown)sawRevive=true;
                }
                game.Events.Clear();if(sawRevive)break;
            }
            if(!sawDown || !sawRevive || game.CoreHealth<=0)throw new Exception("Did not observe hunter death and revival with core intact.");
            lines.Add("result="+game.State+" t="+game.Elapsed.ToString("F2")+" core="+game.CoreHealth.ToString("F1")+" hunter="+game.PlayerHealth.ToString("F1")+" down="+sawDown+" revived="+sawRevive);
            return string.Join("\n",lines.ToArray());
        }
    }
}
