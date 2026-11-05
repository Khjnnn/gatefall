using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class TargetQueryAllocationReview {
        public sealed class Row { public int Enemies,Queries,Checksum;public long Allocated;public string OrderHash; }
        public static Row Run(int count) {
            var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7);
            var rng=new Random(417);
            for(int i=0;i<count;i++)g.Enemies.Add(new Enemy{Id=i+1,Health=50,MaxHealth=50,Position=new V2((float)rng.NextDouble()*10-5,(float)rng.NextDouble()*10-5)});
            var method=typeof(DefenseSimulation).GetMethod("Targets",BindingFlags.NonPublic|BindingFlags.Instance);
            var query=(Func<V2,float,bool,List<Enemy>>)method.CreateDelegate(typeof(Func<V2,float,bool,List<Enemy>>),g);
            var text=new StringBuilder();
            foreach(float range in new[]{0f,2f,20f})foreach(bool available in new[]{false,true}){
                var targets=query(new V2(),range,available);foreach(var e in targets)text.Append(e.Id).Append(',');text.Append('|');
            }
            for(int i=0;i<64;i++)query(new V2(),20,true);
            const int queries=20000;int checksum=0;long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<queries;i++)checksum+=query(new V2(),20,true).Count;
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;string hash;
            using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","").ToLowerInvariant();
            if(checksum!=count*queries)throw new Exception("Target query fixture changed count");
            return new Row{Enemies=count,Queries=queries,Checksum=checksum,Allocated=allocated,OrderHash=hash};
        }
    }
}
