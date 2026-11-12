using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    public sealed partial class InventoryModel
    {
        public readonly SupplierCatalog Suppliers;
        public List<SupplierState> SupplierStates()
        {var list=new List<SupplierState>();foreach(var s in state.suppliers)list.Add(s.Copy());return list;}
        public SupplierState Supplier(string id){var s=state.suppliers.Find(v=>v.supplierId==id);return s==null?null:s.Copy();}
        void ValidateSuppliers(InventorySave save)
        {
            if(save.suppliers==null||save.suppliers.Count>64)throw new ArgumentException("거래처 저장 오류입니다.");
            var ids=new HashSet<string>();
            foreach(var s in save.suppliers)
            {
                var def=s==null||Suppliers==null?null:Suppliers.Find(s.supplierId);
                if(def==null||!ids.Add(s.supplierId)||s.cycle<0||s.cycle>1000000||s.stocks==null||s.stocks.Count!=def.offers.Length)
                    throw new ArgumentException("없는/중복 거래처 또는 물품 저장 오류입니다.");
                var stockIds=new HashSet<string>();
                foreach(var stock in s.stocks)
                {
                    var o=stock==null?null:Suppliers.Offer(s.supplierId,stock.offerId);
                    if(o==null||!stockIds.Add(stock.offerId)||stock.unitPrice<0||stock.remaining<0||stock.remaining>100)
                        throw new ArgumentException("거래처 재고 저장 오류입니다.");
                    // 현재 주기 가격/잔여 재고는 저장값을 유지합니다.
                    // 범위 수치를 수정해도 재접속으로 다시 뽑지 않고 다음 주기에 새 정의를 적용합니다.
                }
            }
        }
        public bool RefreshSuppliersForDay(int day,out string error)
        {
            error="";if(Suppliers==null||day<1||day>1000000){error="거래처 카탈로그/날짜 오류입니다.";return false;}
            foreach(var old in state.suppliers)if(old.cycle>Suppliers.Cycle(old.supplierId,day)){error="거래처 날짜를 되돌릴 수 없습니다.";return false;}
            bool changed=false;InventorySave trial=null;
            foreach(var def in Suppliers.All())
            {
                var old=state.suppliers.Find(s=>s.supplierId==def.id);int cycle=Suppliers.Cycle(def.id,day);
                if(old!=null&&old.cycle==cycle)continue;
                if(trial==null)trial=state.Copy();
                var current=trial.suppliers.Find(s=>s.supplierId==def.id);
                if(current==null){current=new SupplierState{supplierId=def.id,unlocked=def.initiallyUnlocked,blocked=def.initiallyBlocked};trial.suppliers.Add(current);}
                current.cycle=cycle;current.stocks.Clear();
                foreach(var offer in def.offers)current.stocks.Add(Suppliers.CreateStock(def.id,offer,cycle,Catalog));
                changed=true;
            }
            if(changed)Commit(trial);return true;
        }
        public bool SetSupplierAccess(string id,bool unlocked,bool blocked,string reason,out string error)
        {
            error="";var old=state.suppliers.Find(entry=>entry.supplierId==id);
            if(old==null){error="거래처가 없습니다.";return false;}
            var trial=state.Copy();var s=trial.suppliers.Find(v=>v.supplierId==id);
            s.unlocked=unlocked;s.blocked=blocked;s.blockReason=reason??"";Commit(trial);return true;
        }
        // 재고 차감/돈 차감/다음 새벽 배송 예약을 하나로 확정합니다.
        public bool TryPurchaseFromSupplier(string id,IList<SupplierPurchaseLine> lines,int day,int expectedCycle,out string error)
        {
            error="";var supplier=state.suppliers.Find(s=>s.supplierId==id);
            if(Suppliers==null||supplier==null||!supplier.unlocked||supplier.blocked)
            {error="거래할 수 없는 거래처입니다.";return false;}
            if(day<state.lastSettledDay||day>=1000000||supplier.cycle!=expectedCycle||supplier.cycle!=Suppliers.Cycle(id,day))
            {error="매입 목록 갱신 시점이 지났습니다. 화면을 다시 확인하세요.";return false;}
            if(lines==null||lines.Count==0||lines.Count>64||state.deliveries.Count>=256){error="구매 목록/배송 예약 한도 오류입니다.";return false;}
            long total=0;int amount=0;var seen=new HashSet<string>();
            foreach(var line in lines)
            {
                var stock=line==null?null:supplier.stocks.Find(s=>s.offerId==line.offerId);
                if(stock==null||!seen.Add(line.offerId)||line.quantity<1||line.quantity>stock.remaining||line.expectedUnitPrice!=stock.unitPrice)
                {error="재고/가격/구매 수량이 바뀌었습니다. 목록을 확인하세요.";return false;}
                amount+=line.quantity;total+=(long)stock.unitPrice*line.quantity;
            }
            if(amount>100){error="한 번의 주문은 최대100개입니다. 나누어 주문하세요.";return false;}
            if(total>state.gold){error="돈이 부족합니다.";return false;}
            var trial=state.Copy();var vendor=trial.suppliers.Find(s=>s.supplierId==id);
            var order=new DeliveryOrder{orderId=Guid.NewGuid().ToString("N"),arrivalDay=day+1};
            foreach(var line in lines)
            {
                var offer=Suppliers.Offer(id,line.offerId);var stock=vendor.stocks.Find(s=>s.offerId==line.offerId);
                stock.remaining-=line.quantity;
                for(int n=0;n<line.quantity;n++)order.items.Add(new ItemRecord{instanceId=Guid.NewGuid().ToString("N"),itemId=offer.itemId,quality=offer.quality,purchasePrice=stock.unitPrice});
            }
            trial.gold-=total;trial.deliveries.Add(order);Commit(trial);return true;
        }
    }
}
