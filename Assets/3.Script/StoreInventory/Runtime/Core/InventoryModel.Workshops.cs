using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    public sealed partial class InventoryModel
    {
        public readonly WorkshopCatalog Workshops;
        public event Action WorkshopsChanged;
        public int SuccessfulCustomerSales{get{return state.successfulCustomerSales;}}
        void NotifyWorkshops(){var e=WorkshopsChanged;if(e!=null)e();}
        void PrepareWorkshops(InventorySave save)
        {
            if(Workshops==null)return;
            foreach(var d in Workshops.Data.workshops)if(!save.workshops.Exists(w=>w!=null&&w.id==d.id))save.workshops.Add(new WorkshopState{id=d.id,unlocked=save.successfulCustomerSales>=d.unlockSales});
        }
        void UnlockEarnedWorkshops(InventorySave save)
        {if(Workshops!=null)foreach(var w in save.workshops)if(save.successfulCustomerSales>=Workshops.Find(w.id).unlockSales)w.unlocked=true;}
        public WorkshopState GetWorkshop(string id){var w=state.workshops.Find(s=>s.id==id);return w==null?null:w.Copy();}
        public long WorkshopDue(){long n=0;foreach(var w in state.workshops)n=checked(n+w.wageDue+w.extraDue);return n;}
        void BeginWorkshopDay(InventorySave save,int day)
        {foreach(var w in save.workshops){w.wageDue=0;w.extraDue=0;if(w.hired){w.wageDue=Workshops.Find(w.id).dailyWage;w.wageDay=day;}}}
        void ValidateWorkshops(InventorySave save,HashSet<string> ids)
        {
            if(save.successfulCustomerSales<0||save.successfulCustomerSales>1000000000||save.workshops==null)throw new ArgumentException("공방 저장 오류");
            var stations=new HashSet<string>();var jobs=new HashSet<string>();
            foreach(var w in save.workshops)
            {
                var d=w==null||Workshops==null?null:Workshops.Find(w.id);
                if(d==null||!stations.Add(w.id)||w.hired&&!w.unlocked||w.wageDue<0||w.extraDue<0||w.wageDay<0||w.wageDay>save.lastSettledDay||w.jobs==null||w.jobs.Count>0&&!w.unlocked||w.jobs.Count>d.queueCapacity)throw new ArgumentException("공방 고용/일급 저장 오류");
                foreach(var j in w.jobs)
                {
                    var recipe=j==null?null:Workshops.Recipe(j.recipeId);
                    if(recipe==null||recipe.workshopId!=w.id||string.IsNullOrWhiteSpace(j.id)||!jobs.Add(j.id)||j.cycles<1||j.cycles>(w.id=="processing"?5/recipe.outputCount:1)||j.completed<0||j.completed>j.cycles||!WorkshopCatalog.Finite(j.elapsed)||j.elapsed<0||j.elapsed>=recipe.minutes||j.inputs==null||j.inputs.Count>5||j.results==null||j.results.Count>5||j.usedSlots==null||j.usedSlots.Count>5||!j.started&&(j.elapsed!=0||j.completed!=0||j.usedSlots.Count!=0)||j.completed==j.cycles&&(j.elapsed!=0||j.usedSlots.Count!=0))throw new ArgumentException("공방 작업 저장 오류");
                    var slots=new HashSet<int>();foreach(var i in j.inputs)
                    {
                        if(i==null||i.slot<0||i.slot>4||!slots.Add(i.slot))throw new ArgumentException("공방 재료 슬롯 오류");ValidateWorkshopItem(i.item,ids);
                        var ingredient=Array.Find(recipe.ingredients,x=>x.itemId==i.item.itemId);if(ingredient==null)throw new ArgumentException("조합과 다른 재료");
                        int remaining=ingredient.count*(j.cycles-j.completed);if(j.inputs.FindAll(x=>x.item!=null&&x.item.itemId==ingredient.itemId).Count>remaining)throw new ArgumentException("주문보다 많은 공방 재료");
                    }
                    foreach(var i in j.results){ValidateWorkshopItem(i,ids);if(i.itemId!=recipe.outputItemId)throw new ArgumentException("공방 결과 품목 오류");}
                    var used=new HashSet<int>();foreach(int s in j.usedSlots)if(!used.Add(s)||!slots.Contains(s))throw new ArgumentException("가공 중 재료 잠금 오류");
                    if(j.usedSlots.Count>0){foreach(var ing in recipe.ingredients)if(j.inputs.FindAll(x=>j.usedSlots.Contains(x.slot)&&x.item.itemId==ing.itemId).Count!=ing.count)throw new ArgumentException("가공 중 재료 구성 오류");}
                    else if(j.elapsed>0)throw new ArgumentException("재료 없는 진행도");
                }
            }
            if(Workshops!=null&&stations.Count!=Workshops.Data.workshops.Length)throw new ArgumentException("공방 누락");
        }
        void ValidateWorkshopItem(ItemRecord item,HashSet<string> ids)
        {if(item==null||string.IsNullOrWhiteSpace(item.instanceId)||!ids.Add(item.instanceId)||Catalog.FindQuality(item.itemId,item.quality)==null||item.rotation<0||item.rotation>3||item.purchasePrice<0)throw new ArgumentException("공방 물품/UID 오류");}
        WorkshopState Station(InventorySave save,string id){return save.workshops.Find(x=>x.id==id);}
        bool CommandStation(string id,out WorkshopState w,out string error)
        {w=Station(state,id);error="";if(w==null||!w.unlocked){error="손님과의 거래를 통해 공방을 해금하세요.";return false;}return true;}
        public bool TryHireWorkshop(string id,bool hire,int day,out string error)
        {
            WorkshopState w;if(!CommandStation(id,out w,out error))return false;
            if(day!=state.lastSettledDay){error="공방 고용 날짜가 현재 정산일과 다릅니다.";return false;}
            try{var next=state.Copy();w=Station(next,id);w.hired=hire;if(hire&&w.wageDay!=day){w.wageDue=checked(w.wageDue+Workshops.Find(id).dailyWage);w.wageDay=day;}Commit(next);return true;}
            catch(OverflowException){error="일급 금액 범위를 초과했습니다.";return false;}
        }
        public bool TryUnlockWorkshop(string id,out string error)
        {error="";if(Workshops==null||Workshops.Find(id)==null){error="공방 ID가 없습니다.";return false;}var next=state.Copy();Station(next,id).unlocked=true;Commit(next);return true;}
        public bool TryUnlockAllWorkshopsForDemo(out string error)
        {error="";if(Workshops==null){error="공방 데이터가 없습니다.";return false;}var n=state.Copy();foreach(var w in n.workshops)w.unlocked=true;Commit(n);return true;}
        public bool TryCreateWorkshopJob(string station,string recipeId,int cycles,out string jobId,out string error)
        {
            jobId=null;WorkshopState w;if(!CommandStation(station,out w,out error))return false;var recipe=Workshops.Recipe(recipeId);
            if(!w.hired||recipe==null||recipe.workshopId!=station||cycles<1||cycles>(station=="processing"?5/recipe.outputCount:1)||w.jobs.Count>=Workshops.Find(station).queueCapacity){error="고용 상태/조합/수량/작업 자리를 확인하세요.";return false;}
            var n=state.Copy();jobId=Guid.NewGuid().ToString("N");Station(n,station).jobs.Add(new WorkshopJob{id=jobId,recipeId=recipeId,cycles=cycles});Commit(n);return true;
        }
        bool FindJob(string station,string id,out WorkshopState w,out WorkshopJob j,out string error)
        {j=null;if(!CommandStation(station,out w,out error))return false;j=w.jobs.Find(x=>x.id==id);if(j==null){error="작업이 없습니다.";return false;}return true;}
        public bool TryPutWorkshopMaterial(string station,string id,int slot,string source,string uid,long revision,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;var item=GetItem(source,uid);
            if(revision!=Revision||item==null||IsReserved(uid)||!IsWarehouse(source)&&source!=DeliveryContainerId||slot<0||slot>4||j.inputs.Exists(x=>x.slot==slot)||j.completed>=j.cycles){error="투입할 재료/빈 슬롯/재고 상태를 확인하세요. 상자 또는 택배에서 넣으세요.";return false;}
            var recipe=Workshops.Recipe(j.recipeId);var ing=Array.Find(recipe.ingredients,x=>x.itemId==item.itemId);
            if(ing==null||j.inputs.FindAll(x=>x.item.itemId==item.itemId).Count>=ing.count*(j.cycles-j.completed)){error="이 주문에 필요한 재료만 넣을 수 있습니다.";return false;}
            var n=state.Copy();RemoveItem(n,source,uid);item.x=item.y=0;Station(n,station).jobs.Find(x=>x.id==id).inputs.Add(new WorkshopMaterial{slot=slot,item=item});Commit(n);return true;
        }
        bool PutBack(InventorySave save,string warehouse,ItemRecord original)
        {
            var c=Find(save,warehouse);if(c==null||!IsWarehouse(warehouse))return false;var item=original.Copy();var def=Catalog.Find(item.itemId);
            for(int rot=0;rot<(def.canRotate?4:1);rot++){item.rotation=rot;for(int y=0;y<c.rows;y++)for(int x=0;x<c.columns;x++){item.x=x;item.y=y;if(InspectPlacement(save,c,item,null).allowed){c.items.Add(item);return true;}}}return false;
        }
        public bool TryTakeWorkshopItem(string station,string id,int slot,bool result,string warehouse,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;
            ItemRecord item;if(result){item=slot>=0&&slot<j.results.Count?j.results[slot]:null;}
            else{var m=j.inputs.Find(x=>x.slot==slot);item=m==null?null:m.item;if(j.usedSlots.Contains(slot)){error="현재 가공 중인 재료입니다. 작업 취소로 회수하세요.";return false;}}
            if(item==null){error="빈 슬롯입니다.";return false;}var n=state.Copy();if(!PutBack(n,warehouse,item)){error="선택한 상자에 공간이 없습니다. 결과물은 공방에 유지됩니다.";return false;}
            j=Station(n,station).jobs.Find(x=>x.id==id);if(result)j.results.RemoveAt(slot);else j.inputs.RemoveAll(x=>x.slot==slot);
            if(j.completed==j.cycles&&j.results.Count==0&&j.inputs.Count==0)Station(n,station).jobs.Remove(j);Commit(n);return true;
        }
        public bool TryCancelWorkshopJob(string station,string id,string warehouse,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;var n=state.Copy();j=Station(n,station).jobs.Find(x=>x.id==id);
            foreach(var i in j.inputs)if(!PutBack(n,warehouse,i.item)){error="재료를 회수할 상자 공간이 부족합니다.";return false;}
            foreach(var i in j.results)if(!PutBack(n,warehouse,i)){error="결과물까지 회수할 상자 공간이 부족합니다.";return false;}
            Station(n,station).jobs.Remove(j);Commit(n);return true;
        }
        bool SelectBatch(WorkshopJob j,WorkshopRecipe recipe)
        {
            if(j.usedSlots.Count>0)return true;
            foreach(var ing in recipe.ingredients){int available=0;foreach(var m in j.inputs)if(m.item.itemId==ing.itemId)available++;if(available<ing.count)return false;}
            foreach(var ing in recipe.ingredients)
            {
                int needed=ing.count;for(int slot=0;slot<5&&needed>0;slot++){var m=j.inputs.Find(x=>x.slot==slot);if(m!=null&&m.item.itemId==ing.itemId){j.usedSlots.Add(m.slot);needed--;}}
            }
            return true;
        }
        public bool TryStartWorkshopJob(string station,string id,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;
            var n=state.Copy();j=Station(n,station).jobs.Find(x=>x.id==id);var recipe=Workshops.Recipe(j.recipeId);
            if(!w.hired||j.completed>=j.cycles||!SelectBatch(j,recipe)){error="고용 상태 또는 한 회차의 재료를 확인하세요.";return false;}j.started=true;Commit(n);return true;
        }
        public bool TryMoveWorkshopJob(string station,string id,int direction,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;int index=w.jobs.IndexOf(j),target=index+direction;
            if(station!="smith"||Math.Abs(direction)!=1||target<0||target>=w.jobs.Count){error="이동할 작업 순서를 확인하세요.";return false;}
            var n=state.Copy();w=Station(n,station);j=w.jobs[index];w.jobs.RemoveAt(index);w.jobs.Insert(target,j);Commit(n);return true;
        }
        ItemRecord MakeWorkshopOutput(InventorySave save,WorkshopRecipe recipe,IList<ItemRecord> inputs)
        {
            var def=Catalog.Find(recipe.outputItemId);var probabilities=Workshops.Probabilities(def,inputs);
            uint bits=unchecked((uint)save.workshopRandom);if(bits==0)bits=1337;bits^=bits<<13;bits^=bits>>17;bits^=bits<<5;save.workshopRandom=unchecked((int)bits);
            double roll=bits/4294967296.0,total=0;int chosen=probabilities.Length-1;for(int i=0;i<probabilities.Length;i++){total+=probabilities[i];if(roll<total){chosen=i;break;}}
            long materialCost=0;foreach(var i in inputs)materialCost+=i.purchasePrice;
            return new ItemRecord{purchasePrice=(int)Math.Min(int.MaxValue,materialCost/recipe.outputCount),instanceId=Guid.NewGuid().ToString("N"),itemId=recipe.outputItemId,quality=def.qualities[chosen].quality};
        }
        void CompleteWorkshopBatch(InventorySave save,WorkshopJob j,WorkshopRecipe recipe)
        {
            var used=new List<ItemRecord>();foreach(var m in j.inputs)if(j.usedSlots.Contains(m.slot))used.Add(m.item);
            for(int n=0;n<recipe.outputCount;n++)j.results.Add(MakeWorkshopOutput(save,recipe,used));
            j.inputs.RemoveAll(m=>j.usedSlots.Contains(m.slot));j.usedSlots.Clear();j.elapsed=0;j.completed++;
        }
        public bool TryInstantAlchemy(string id,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob("alchemy",id,out w,out j,out error))return false;var recipe=Workshops.Recipe(j.recipeId);
            if(!w.hired||j.completed>=j.cycles||j.results.Count+recipe.outputCount>5){error="즉시 조제할 작업/고용/결과 공간을 확인하세요.";return false;}
            try{var n=state.Copy();w=Station(n,"alchemy");j=w.jobs.Find(x=>x.id==id);if(!SelectBatch(j,recipe)){error="조제 재료가 부족합니다.";return false;}w.extraDue=checked(w.extraDue+Workshops.Find("alchemy").instantFee);j.started=true;CompleteWorkshopBatch(n,j,recipe);Commit(n);return true;}
            catch(OverflowException){error="추가 조제료 금액 범위를 초과했습니다.";return false;}
        }
        // Called by the game clock, not per-view Update. Progress never changes inventory Revision.
        public void AdvanceWorkshops(double minutes)
        {
            if(Workshops==null)return;if(!WorkshopCatalog.Finite(minutes)||minutes<0||minutes>10080)throw new ArgumentException("공방 시간 증가 오류");bool changed=false;
            foreach(var def in Workshops.Data.workshops)
            {
                double remaining=minutes;while(remaining>0)
                {
                    var w=Station(state,def.id);if(!w.hired||w.jobs.Count==0)break;var j=w.jobs[0];var recipe=Workshops.Recipe(j.recipeId);
                    if(!j.started||j.completed>=j.cycles||j.results.Count+recipe.outputCount>5||!SelectBatch(j,recipe))break;
                    double step=Math.Min(remaining,recipe.minutes-j.elapsed);remaining-=step;j.elapsed+=step;changed|=step>0;
                    if(j.elapsed+1e-8>=recipe.minutes)
                    {var next=state.Copy();var job=Station(next,def.id).jobs[0];CompleteWorkshopBatch(next,job,recipe);Commit(next);}
                    else break;
                }
            }
            if(changed)NotifyWorkshops();
        }
        public string WorkshopJobStatus(string station,WorkshopJob j)
        {
            var w=Station(state,station);if(j.completed==j.cycles)return "완료 · 결과물 수령 대기";if(!w.hired)return "고용 중지";if(!j.started)return "시작 대기";
            if(w.jobs.Count>0&&w.jobs[0].id!=j.id)return "순서 대기";var r=Workshops.Recipe(j.recipeId);if(j.results.Count+r.outputCount>5)return "결과물 수령 대기";
            if(j.usedSlots.Count==0){foreach(var ing in r.ingredients)if(j.inputs.FindAll(x=>x.item.itemId==ing.itemId).Count<ing.count)return "재료 대기";}return "제작 중";
        }
        public List<ItemRecord> WorkshopBatchMaterials(WorkshopJob job)
        {
            var items=new List<ItemRecord>();var copy=job.Copy();if(SelectBatch(copy,Workshops.Recipe(copy.recipeId)))foreach(var m in copy.inputs)if(copy.usedSlots.Contains(m.slot))items.Add(m.item);return items;
        }
    }
}
