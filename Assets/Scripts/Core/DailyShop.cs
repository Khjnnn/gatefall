using System;
using System.IO;
using System.Collections.Generic;

namespace Gatefall.Core
{
    public enum ShopOfferKind { Card, Heart, Free }
    public sealed class DailyShopOffer
    {
        public int Slot, Price;
        public ShopOfferKind Kind;
        public TowerKind Tower;
        public bool Purchased;
    }
    public sealed class DailyShopState
    {
        public int LocalDay=-1, RefreshCount, PurchasedMask, HeartsBought;
        public int[] CardKinds={-1,-1,-1,-1};
        public DailyShopState Copy()
        {var p=(DailyShopState)MemberwiseClone();p.CardKinds=(int[])CardKinds.Clone();return p;}
        public bool Valid()
        {
            if(LocalDay< -1 || RefreshCount<0 || RefreshCount>2 || PurchasedMask<0 || PurchasedMask>63 ||
                HeartsBought<0 || HeartsBought>3 || CardKinds==null || CardKinds.Length!=4)return false;
            if(LocalDay==-1 && (RefreshCount!=0 || PurchasedMask!=0 || HeartsBought!=0))return false;
            foreach(int kind in CardKinds)if(LocalDay==-1 ? kind!=-1 : !SupplyCatalog.IsKind((TowerKind)kind))return false;
            return true;
        }
        public DailyShopOffer[] Offers()
        {
            if(LocalDay<0)throw new InvalidOperationException("일일 상점을 먼저 생성하세요.");
            var offers=new DailyShopOffer[6];int[] prices={10,20,40,60};
            for(int i=0;i<offers.Length;i++)offers[i]=new DailyShopOffer{Slot=i,Kind=i<4 ? ShopOfferKind.Card : i==4 ? ShopOfferKind.Heart : ShopOfferKind.Free,
                Tower=i<4 ? (TowerKind)CardKinds[i] : TowerKind.Autocannon,Price=i<4 ? prices[(int)SupplyCatalog.Rarity((TowerKind)CardKinds[i])] : i==4 ? 20 : 0,
                Purchased=(PurchasedMask&(1<<i))!=0};
            return offers;
        }
        internal void Write(BinaryWriter w)
        {w.Write(LocalDay);w.Write(RefreshCount);w.Write(PurchasedMask);w.Write(HeartsBought);foreach(int kind in CardKinds)w.Write(kind);}
        internal static DailyShopState Read(BinaryReader r)
        {
            var p=new DailyShopState{LocalDay=r.ReadInt32(),RefreshCount=r.ReadInt32(),PurchasedMask=r.ReadInt32(),HeartsBought=r.ReadInt32()};
            for(int i=0;i<4;i++)p.CardKinds[i]=r.ReadInt32();if(!p.Valid())throw new InvalidDataException("잘못된 일일 상점");return p;
        }
        // Fixed integer PRNG keeps offers stable across .NET/Unity versions. Sample without replacement where possible.
        internal void Generate(CampaignProfile p,int day,int refresh)
        {
            if(day<0)throw new ArgumentOutOfRangeException("day");
            if(LocalDay!=day)HeartsBought=0;
            LocalDay=day;RefreshCount=refresh;PurchasedMask=0;
            uint random;unchecked{random=(uint)p.ShopSeed*747796405u+(uint)day*2891336453u+(uint)refresh*277803737u+1u;}
            var all=new List<int>();for(int i=0;i<SupplyCatalog.TowerCount;i++)if(p.IsTowerUnlocked((TowerKind)i))all.Add(i);
            var pool=new List<int>(all);
            for(int i=0;i<4;i++){
                if(pool.Count==0)pool.AddRange(all);
                unchecked{random=random*1664525u+1013904223u;}
                int pick=(int)(random%(uint)pool.Count);CardKinds[i]=pool[pick];pool.RemoveAt(pick);
            }
        }
        internal static void Ensure(CampaignProfile p,int day)
        {if(day<0)throw new ArgumentOutOfRangeException("day");if(p.Shop.LocalDay<day)p.Shop.Generate(p,day,0);}
    }
}
