using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Collections.Generic;
using Gatefall.Core;
namespace Gatefall.Tests
{
    public static class ChapterTwoContentContract
    {
        public sealed class Row {public int Stage,Growth;public bool Crisis;public string Hash;}
        static void Write(StringBuilder b,object value)
        {
            if(value==null){b.Append("null;");return;}
            var type=value.GetType();
            if(value is string){b.Append(((string)value).Length).Append(':').Append(value).Append(';');return;}
            if(value is float){b.Append(((float)value).ToString("R",CultureInfo.InvariantCulture)).Append(';');return;}
            if(value is double){b.Append(((double)value).ToString("R",CultureInfo.InvariantCulture)).Append(';');return;}
            if(type.IsPrimitive || type.IsEnum){b.Append(Convert.ToString(value,CultureInfo.InvariantCulture)).Append(';');return;}
            if(value is IEnumerable){b.Append('[');foreach(var item in (IEnumerable)value)Write(b,item);b.Append(']');return;}
            foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance).OrderBy(x=>x.Name,StringComparer.Ordinal)){
                if(type==typeof(CampaignStage) && field.Name=="Lesson")continue; // the only approved text-only rule-data change
                b.Append(field.Name).Append('=');Write(b,field.GetValue(value));
            }
        }
        public static Row[] Capture()
        {
            var result=new List<Row>();var profile=ChapterTwoChecks.Entry();
            for(int i=14;i<20;i++){profile.FirstCleared[i]=true;profile.BestStars[i]=3;}profile.Loadout=2055;
            for(int stage=14;stage<20;stage++)foreach(int growth in new[]{4,5})foreach(bool crisis in new[]{false,true}){
                profile.Growth[0]=growth;profile.Research=growth==4?new[]{2,1,1,1}:new[]{3,2,2,2};
                var game=new DefenseSimulation(new GameBalance(),7,new CampaignService(profile,null).Battle(stage,crisis));
                var stats=new List<TowerStats>();foreach(var definition in TowerCatalog.All)for(int level=1;level<=definition.Levels.Length;level++)stats.Add(game.Stats(definition.Kind,level));
                var b=new StringBuilder();Write(b,new object[]{game.Rules,game.Balance,game.Schedule,game.Roads,game.BoardSockets,TowerCatalog.All,TowerCatalog.Recipes,stats.ToArray()});
                using(var sha=SHA256.Create())result.Add(new Row{Stage=stage,Growth=growth,Crisis=crisis,Hash=string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString())).Select(x=>x.ToString("x2")))});
            }return result.ToArray();
        }
    }
}
