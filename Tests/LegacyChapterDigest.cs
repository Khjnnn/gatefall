using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class LegacyChapterDigest {
        public sealed class Row {public int Stage,Seed,Frames;public bool Crisis;public string Outcome,Hash;public double Seconds;}
        static void Value(StringBuilder b,object v){
            if(v==null){b.Append("null;");return;}var type=v.GetType();
            if(v is string){b.Append((string)v).Append(';');return;}
            if(v is float){b.Append(((float)v).ToString("R",CultureInfo.InvariantCulture)).Append(';');return;}
            if(v is double){b.Append(((double)v).ToString("R",CultureInfo.InvariantCulture)).Append(';');return;}
            if(type.IsEnum || type.IsPrimitive){b.Append(Convert.ToString(v,CultureInfo.InvariantCulture)).Append(';');return;}
            if(v is IEnumerable){b.Append('[');foreach(var item in (IEnumerable)v)Value(b,item);b.Append(']');return;}
            b.Append(type.Name).Append('{');
            foreach(var f in type.GetFields(BindingFlags.Public|BindingFlags.Instance).OrderBy(f=>f.Name)){
                // New armor is separately asserted zero on these early operations; older cores have no field.
                if(type.Name=="Enemy" && f.Name=="Armor" || f.Name=="ConeCos")continue;
                b.Append(f.Name).Append('=');Value(b,type.Name=="RunTelemetry" && f.Name=="TowerDamage" ? ((float[])f.GetValue(v)).Take(11).ToArray() : f.GetValue(v));
            }b.Append('}');
        }
        public static Row[] Run(){
            var rows=new List<Row>();
            for(int stage=-1;stage<14;stage++)foreach(bool crisis in stage<0 ? new[]{false} : new[]{false,true})foreach(int seed in new[]{7}){
                var g=new DefenseSimulation(new GameBalance(),seed,stage<0 ? new CampaignBattle() : new CampaignBattle{Stage=CampaignCatalog.Stages[stage],Crisis=crisis});
                g.WaveCallsEnabled=g.Rules.MobileWaveCalls;
                var random=new Random(seed+400);int frames=0;string why;
                using(var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256)){
                    g.Start();
                    for(int frame=0;frame<60*240 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++){
                        if(g.AwaitingCard)g.ChooseCard(g.CardOffers[frame%g.CardOffers.Count]);
                        if(frame%23==0){
                            int site=random.Next(g.BoardSockets.Length),action=random.Next(6);
                            if(action==0)g.TryBuild(site,(TowerKind)random.Next(3),null,out why);
                            else if(action==1 && g.Towers.Count>0)g.TryUpgrade(g.Towers[random.Next(g.Towers.Count)].Id,null,out why);
                            else if(action==2 && g.Towers.Count>1){var a=g.Towers[random.Next(g.Towers.Count)];var c=g.Towers[random.Next(g.Towers.Count)];g.TryFuse(random.Next(9),a.Id,c.Id,a.Socket,null,out why);}
                            else if(action==3 && g.Towers.Count>0)g.TrySell(g.Towers[random.Next(g.Towers.Count)].Id,null,out why);
                            else if(action==4)g.TryCallNextWave(g.NextCallWave,null,out why);
                            else g.UseSkill();
                        }
                        g.Tick(1f/60,new V2((float)(random.NextDouble()*2-1),(float)(random.NextDouble()*2-1)));
                        var b=new StringBuilder();
                        Value(b,new object[]{g.State,g.Elapsed,g.Credits,g.Cores,g.Kills,g.PlayerPosition,g.PlayerAim,g.PlayerHealth,g.CoreHealth,
                            g.MinimumCoreHealth,g.HunterRespawnRemaining,g.SkillCooldown,g.CoreShield,g.HunterShield,g.SupportCooldown,
                            g.Cards,g.CardOffers,g.Enemies,g.Towers,g.Projectiles,g.Schedule,g.Events,g.Telemetry});
                        hash.AppendData(Encoding.UTF8.GetBytes(b.ToString()));g.Events.Clear();frames++;
                    }
                    rows.Add(new Row{Stage=stage+1,Crisis=crisis,Seed=seed,Frames=frames,Outcome=g.State.ToString(),Seconds=g.Elapsed,
                        Hash=string.Concat(hash.GetHashAndReset().Select(x=>x.ToString("x2")))});
                }
            }return rows.ToArray();
        }
    }
}
