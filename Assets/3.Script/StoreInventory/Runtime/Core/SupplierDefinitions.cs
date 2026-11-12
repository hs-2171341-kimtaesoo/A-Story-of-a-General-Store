using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    [Serializable] public sealed class SupplierDatabase { public int seed=3721;public SupplierDefinition[] suppliers; }
    [Serializable] public sealed class SupplierDefinition
    {
        public string id,name,description,greeting,portraitKey;
        public bool initiallyUnlocked=true,initiallyBlocked;
        public int refreshDays=2;
        public SupplierOfferDefinition[] offers;
    }
    // 물품마다 수량/가격 범위를 다르게 둡니다. 종류별 JSON과 거래처의 매입 조건을 분리합니다.
    [Serializable] public sealed class SupplierOfferDefinition
    {
        public string offerId,itemId,quality;
        public int minStock=1,maxStock=5;
        public int priceBase;
        public double minPriceMultiplier=.85,maxPriceMultiplier=1.15;
    }
    public sealed class SupplierCatalog
    {
        readonly Dictionary<string,SupplierDefinition> entries=new Dictionary<string,SupplierDefinition>(StringComparer.Ordinal);
        readonly int seed;
        public SupplierCatalog(SupplierDatabase database,ItemCatalog items)
        {
            if(database==null||database.suppliers==null||database.suppliers.Length>64)throw new ArgumentException("거래처 JSON 오류입니다.");
            seed=database.seed;
            foreach(var s in database.suppliers)
            {
                if(s==null||string.IsNullOrWhiteSpace(s.id)||entries.ContainsKey(s.id)||s.refreshDays<1||s.refreshDays>365||s.offers==null||s.offers.Length>64)
                    throw new ArgumentException("거래처 ID/갱신 주기 오류입니다.");
                var ids=new HashSet<string>(StringComparer.Ordinal);
                foreach(var o in s.offers)
                {
                    if(o==null||string.IsNullOrWhiteSpace(o.offerId)||!ids.Add(o.offerId)||items.FindQuality(o.itemId,o.quality)==null
                        ||o.minStock<0||o.maxStock<o.minStock||o.maxStock>100||o.priceBase<0
                        ||double.IsNaN(o.minPriceMultiplier)||double.IsNaN(o.maxPriceMultiplier)||o.minPriceMultiplier<0
                        ||o.maxPriceMultiplier<o.minPriceMultiplier||double.IsInfinity(o.maxPriceMultiplier)
                        ||Math.Max(o.priceBase,items.ReferencePrice(o.itemId,o.quality))*o.maxPriceMultiplier>int.MaxValue)
                        throw new ArgumentException("거래처 물품 범위 오류: "+s.id);
                }
                entries.Add(s.id,s); // 이후 JSON 정의는 읽기 전용으로 사용합니다.
            }
        }
        public List<SupplierDefinition> All(){return new List<SupplierDefinition>(entries.Values);}
        public SupplierDefinition Find(string id){SupplierDefinition s;return id!=null&&entries.TryGetValue(id,out s)?s:null;}
        public SupplierOfferDefinition Offer(string supplierId,string offerId)
        {var s=Find(supplierId);return s==null?null:Array.Find(s.offers,o=>o.offerId==offerId);}
        public int Cycle(string id,int day){return (day-1)/Find(id).refreshDays;}
        // 고정 해시: 저장/재접속/화면 열기로 재고를 다시 뽑지 않습니다.
        uint Roll(string supplier,string offer,int cycle,uint salt)
        {
            unchecked
            {
                uint h=2166136261u^(uint)seed^salt;
                foreach(char c in supplier+"/"+offer){h^=c;h*=16777619u;}
                h^=(uint)cycle;h*=2246822519u;h^=h>>13;h*=3266489917u;return h^(h>>16);
            }
        }
        public SupplierStock CreateStock(string id,SupplierOfferDefinition offer,int cycle,ItemCatalog items)
        {
            int stock=offer.minStock+(int)(Roll(id,offer.offerId,cycle,17)%(uint)(offer.maxStock-offer.minStock+1));
            double t=Roll(id,offer.offerId,cycle,91)/(double)uint.MaxValue;
            double multiplier=offer.minPriceMultiplier+(offer.maxPriceMultiplier-offer.minPriceMultiplier)*t;
            int basis=offer.priceBase>0?offer.priceBase:items.ReferencePrice(offer.itemId,offer.quality);
            int price=(int)Math.Round(basis*multiplier,MidpointRounding.AwayFromZero);
            return new SupplierStock{offerId=offer.offerId,unitPrice=price,remaining=stock};
        }
    }
    [Serializable] public sealed class SupplierState
    {
        public string supplierId,blockReason;
        public bool unlocked,blocked;
        public int cycle=-1;
        public List<SupplierStock> stocks=new List<SupplierStock>();
        public SupplierState Copy(){var s=(SupplierState)MemberwiseClone();s.stocks=new List<SupplierStock>();foreach(var o in stocks)s.stocks.Add(o==null?null:o.Copy());return s;}
    }
    [Serializable] public sealed class SupplierStock
    {
        public string offerId;public int unitPrice,remaining;
        public SupplierStock Copy(){return (SupplierStock)MemberwiseClone();}
    }
    public sealed class SupplierPurchaseLine { public string offerId;public int quantity,expectedUnitPrice; }
}
