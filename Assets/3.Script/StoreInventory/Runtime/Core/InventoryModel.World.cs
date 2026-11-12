using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    public sealed partial class InventoryModel
    {
        public long UnpaidCosts { get { return state.unpaidCosts; } }
        public List<FloorItemRecord> FloorItems()
        {var list=new List<FloorItemRecord>();foreach(var f in state.floorItems)list.Add(f.Copy());return list;}
        bool ValidLooseItem(ItemRecord i,HashSet<string> ids)
        {
            return i!=null&&!string.IsNullOrWhiteSpace(i.instanceId)&&ids.Add(i.instanceId)
                &&Catalog.FindQuality(i.itemId,i.quality)!=null&&i.purchasePrice>=0&&i.rotation>=0&&i.rotation<4
                &&(i.rotation==0||Catalog.Find(i.itemId).canRotate);
        }
        static bool Normalized(float n){return !float.IsNaN(n)&&!float.IsInfinity(n)&&n>=0&&n<=1;}
        void ValidateLooseItems(InventorySave save,HashSet<string> ids)
        {
            if(save.floorItems.Count>4096||save.deliveries.Count>256||save.parcels.Count>256)throw new ArgumentException("바닥/배송 데이터가 너무 많습니다.");
            foreach(var f in save.floorItems)
                if(f==null||!ValidLooseItem(f.item,ids)||!Normalized(f.normalizedX)||!Normalized(f.normalizedY)||!FiniteVelocity(f.velocityX)||!FiniteVelocity(f.velocityY))throw new ArgumentException("바닥 물건 오류입니다.");
            var orders=new HashSet<string>();
            int parcelItems=0;
            foreach(var p in save.parcels)
            {
                if(p==null||string.IsNullOrEmpty(p.parcelId)||!orders.Add(p.parcelId)||!Normalized(p.normalizedX)||!Normalized(p.normalizedY)
                    ||p.items==null||p.items.Count<1||p.items.Count>4096)throw new ArgumentException("택배 상자 오류입니다.");
                parcelItems+=p.items.Count;
                if(parcelItems>4096)throw new ArgumentException("합배송 내용물 한도 초과입니다.");
                foreach(var i in p.items)if(!ValidLooseItem(i,ids))throw new ArgumentException("택배 내용물 오류입니다.");
            }
            foreach(var d in save.deliveries)
            {
                if(d==null||string.IsNullOrEmpty(d.orderId)||!orders.Add(d.orderId)||d.arrivalDay<1||d.arrivalDay>1000000
                    ||d.items==null||d.items.Count<1||d.items.Count>100)throw new ArgumentException("배송 예약 오류입니다.");
                foreach(var i in d.items)if(!ValidLooseItem(i,ids))throw new ArgumentException("배송 물건 오류입니다.");
            }
        }
        // 아직 열지 않은 내용물은 하나로 합칩니다. 도착일이 미래인 주문은 건드리지 않습니다.
        static void MergeParcels(InventorySave save)
        {
            if(save.parcels.Count==0)return;
            var box=save.parcels[0];box.normalizedX=.84f;
            for(int n=1;n<save.parcels.Count;n++)box.items.AddRange(save.parcels[n].items);
            if(save.parcels.Count>1)save.parcels.RemoveRange(1,save.parcels.Count-1);
        }
        // 출고가 아니라 위치 변경입니다. 실패하면 원본을 전혀 바꾸지 않습니다.
        public bool TryDropToFloor(string sourceId,string uid,int rotation,float x,float y,long revision,out string error)
        {
            error="";
            if(revision!=Revision||IsReserved(uid)||!Normalized(x)||!Normalized(y))
            {error="재고가 바뀌었거나 잠긴 물건/바닥 범위 오류입니다.";return false;}
            var trial=state.Copy();var source=Find(trial,sourceId);var item=source==null?null:source.items.Find(i=>i.instanceId==uid);
            if(item==null||rotation<0||rotation>3||(rotation!=0&&!Catalog.Find(item.itemId).canRotate))
            {error="바닥에 놓을 물건/회전 정보가 없습니다.";return false;}
            if(sourceId!="floor"&&trial.floorItems.Count>=4096){error="바닥 물건 한도입니다.";return false;}
            RemoveItem(trial,sourceId,uid);trial.listings.RemoveAll(l=>l.instanceId==uid);
            item.rotation=rotation;item.x=item.y=0;
            trial.floorItems.Add(new FloorItemRecord{item=item,normalizedX=x,normalizedY=y,airborne=true});Commit(trial);return true;
        }
        // 실제 거래처 화면 대신 제공하는 테스트/연결 API입니다. 매입비는 여기서 한 번만 차감합니다.
        public bool TryOrderNextDawn(int currentDay,string itemId,string quality,int amount,int unitCost,out string error)
        {
            error="";
            if(currentDay<state.lastSettledDay||currentDay>=1000000||amount<1||amount>100||unitCost<0||Catalog.FindQuality(itemId,quality)==null||state.deliveries.Count>=256)
            {error="배송 주문 정보 오류입니다.";return false;}
            long cost=(long)amount*unitCost;if(cost>state.gold){error="주문할 돈이 부족합니다.";return false;}
            var trial=state.Copy();var order=new DeliveryOrder{orderId=Guid.NewGuid().ToString("N"),arrivalDay=currentDay+1};
            for(int n=0;n<amount;n++)order.items.Add(new ItemRecord{instanceId=Guid.NewGuid().ToString("N"),itemId=itemId,quality=quality,purchasePrice=unitCost});
            trial.gold-=cost;trial.deliveries.Add(order);Commit(trial);return true;
        }
        // 비용과 배송은 새벽 전환에서 한 번에 확정합니다. 실패하면 양쪽 모두 적용하지 않습니다.
        public bool TryBeginDawn(int day,long dailyCost,out long paid,out int delivered,out string error)
        {
            paid=0;delivered=0;error="";
            if(day<1||day>1000000||dailyCost<0){error="새벽 정산 정보 오류입니다.";return false;}
            if(day<=state.lastSettledDay)return true; // 같은 새벽의 중복 호출/입금 및 지출 방지
            int count=0;foreach(var d in state.deliveries)if(d.arrivalDay<=day)count+=d.items.Count;
            int waiting=0;foreach(var p in state.parcels)waiting+=p.items.Count;
            if(waiting+count>4096){error="합배송 상자 내용물 한도로 배송/정산을 보류했습니다.";return false;}
            long pay=Math.Min(state.gold,dailyCost);long debt;
            try{debt=checked(state.unpaidCosts+(dailyCost-pay));}catch(OverflowException){error="미납 금액 범위 초과";return false;}
            var trial=state.Copy();trial.gold-=pay;trial.unpaidCosts=debt;trial.lastSettledDay=day;
            MergeParcels(trial);
            ParcelRecord box=trial.parcels.Count==0?null:trial.parcels[0];
            foreach(var d in trial.deliveries)if(d.arrivalDay<=day)
            {
                if(box==null)
                {box=new ParcelRecord{parcelId=d.orderId,normalizedX=.84f,normalizedY=.15f};trial.parcels.Add(box);}
                box.items.AddRange(d.items);
            }
            trial.deliveries.RemoveAll(d=>d.arrivalDay<=day);paid=pay;delivered=count;Commit(trial);return true;
        }
    }
}
