using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    public sealed partial class InventoryModel
    {
        public readonly WorkshopCatalog Workshops;
        public event Action WorkshopsChanged;
        ShopPhase workshopPhase=ShopPhase.Day;int workshopDay=1;
        void NotifyWorkshops(){var e=WorkshopsChanged;if(e!=null)e();}
        public int SuccessfulCustomerSales{get{return state.successfulCustomerSales;}}
        public void SetWorkshopTime(ShopPhase phase,int day)
        {if(workshopPhase==phase&&workshopDay==day)return;workshopPhase=phase;workshopDay=day;NotifyWorkshops();}
        public bool WorkshopCanOperate(string id)
        {
            var w=Station(state,id);if(w==null||!w.hired||Workshops==null)return false;var d=Workshops.Data;
            if(!d.usePhaseSchedule)return true;if(workshopPhase==ShopPhase.Day)return d.dayWork;
            if(workshopPhase==ShopPhase.Dawn)return d.dawnWork;
            return d.nightWork||(d.paidNightWorkEnabled&&w.nightWorkDay==workshopDay);
        }
        public long NightWorkFee(string id)
        {var d=Workshops==null?null:Workshops.Find(id);return d==null?0:(long)Math.Ceiling((decimal)d.dailyWage*(decimal)Workshops.Data.nightWageMultiplier);}
        public bool TryPayNightWork(string id,int day,ShopPhase phase,out string error)
        {
            WorkshopState w;if(!CommandStation(id,out w,out error))return false;
            if(!Workshops.Data.usePhaseSchedule||!Workshops.Data.paidNightWorkEnabled||phase!=ShopPhase.Night||day!=state.lastSettledDay||!w.hired)
            {error="밤에 고용 중인 공방의 야간 작업을 신청하세요.";return false;}
            if(w.nightWorkDay==day){error="오늘 밤 수당은 이미 등록되었습니다.";return false;}
            var next=state.Copy();w=Station(next,id);try{w.extraDue=checked(w.extraDue+NightWorkFee(id));}catch(OverflowException){error="수당 범위 초과";return false;}
            w.nightWorkDay=day;Commit(next);SetWorkshopTime(phase,day);return true;
        }
        void PrepareWorkshops(InventorySave save)
        {
            if(Workshops==null)return;
            foreach(var definition in Workshops.Data.workshops)
            {
                var w=Station(save,definition.id);if(w==null){w=new WorkshopState{id=definition.id,unlocked=save.successfulCustomerSales>=definition.unlockSales};save.workshops.Add(w);}
                foreach(var j in w.jobs)if(j!=null)
                {
                    if(j.cycles>1&&j.repeatMode==0)j.repeatMode=1;
                    if(save.version<11&&j.started)
                    {
                        if(j.completed>=j.cycles||j.usedSlots.Count==0){j.started=false;j.elapsed=0;j.exhausted=j.completed<j.cycles;}
                        else
                        {
                            var excess=new List<ItemRecord>();foreach(var material in j.inputs)if(!j.usedSlots.Contains(material.slot))excess.Add(material.item);
                            j.inputs.RemoveAll(material=>!j.usedSlots.Contains(material.slot));AppendTemporary(save,excess);
                        }
                    }
                }
                while(w.jobs.Count<definition.queueCapacity)w.jobs.Add(NewWorkshopDraft());
            }
        }
        static WorkshopJob NewWorkshopDraft(){return new WorkshopJob{id=Guid.NewGuid().ToString("N"),recipeId=""};}
        void UnlockEarnedWorkshops(InventorySave save)
        {if(Workshops!=null)foreach(var w in save.workshops)if(save.successfulCustomerSales>=Workshops.Find(w.id).unlockSales)w.unlocked=true;}
        public WorkshopState GetWorkshop(string id){var w=Station(state,id);return w==null?null:w.Copy();}
        public long WorkshopDue(){long value=0;foreach(var w in state.workshops)value=checked(value+w.wageDue+w.extraDue);return value;}
        void BeginWorkshopDay(InventorySave save,int day)
        {foreach(var w in save.workshops){w.wageDue=w.extraDue=0;if(w.hired){w.wageDue=Workshops.Find(w.id).dailyWage;w.wageDay=day;}}}
        WorkshopState Station(InventorySave save,string id){return save.workshops.Find(w=>w!=null&&w.id==id);}
        bool CommandStation(string id,out WorkshopState station,out string error)
        {station=Station(state,id);error="";if(station==null||!station.unlocked){error="손님과의 거래로 공방을 먼저 해금하세요.";return false;}return true;}
        bool FindJob(string station,string id,out WorkshopState owner,out WorkshopJob job,out string error)
        {job=null;if(!CommandStation(station,out owner,out error))return false;job=owner.jobs.Find(j=>j.id==id);if(job==null){error="작업 슬롯이 없습니다.";return false;}return true;}
        void ValidateWorkshopItem(ItemRecord item,HashSet<string> ids)
        {if(item==null||string.IsNullOrWhiteSpace(item.instanceId)||!ids.Add(item.instanceId)||Catalog.FindQuality(item.itemId,item.quality)==null||item.rotation<0||item.rotation>3||item.purchasePrice<0)throw new ArgumentException("공방 물품/UID 오류");}
        void ValidateWorkshops(InventorySave save,HashSet<string> ids)
        {
            if(save.successfulCustomerSales<0||save.workshops==null)throw new ArgumentException("공방 저장 오류");var stations=new HashSet<string>();var jobs=new HashSet<string>();
            foreach(var w in save.workshops)
            {
                var definition=w==null||Workshops==null?null:Workshops.Find(w.id);
                if(definition==null||!stations.Add(w.id)||w.hired&&!w.unlocked||w.wageDue<0||w.extraDue<0||w.wageDay<0||w.wageDay>save.lastSettledDay||w.nightWorkDay<0||w.nightWorkDay>save.lastSettledDay||w.jobs==null||w.jobs.Count!=definition.queueCapacity)throw new ArgumentException("공방 고용/작업층 저장 오류");
                foreach(var j in w.jobs)
                {
                    if(j==null||string.IsNullOrWhiteSpace(j.id)||!jobs.Add(j.id)||j.inputs==null||j.inputs.Count>5||j.results==null||j.usedSlots==null||j.usedSlots.Count>5||j.repeatMode<0||j.repeatMode>2||j.cycles<1||j.completed<0||j.completed>j.cycles||!WorkshopCatalog.Finite(j.elapsed)||j.elapsed<0)throw new ArgumentException("공방 작업 저장 오류");
                    var recipe=Workshops.Recipe(j.recipeId);
                    if(!string.IsNullOrEmpty(j.recipeId)&&(recipe==null||recipe.workshopId!=w.id))throw new ArgumentException("공방 조합 참조 오류");
                    if(j.started&&(recipe==null||j.completed>=j.cycles||j.elapsed>=recipe.Duration||j.usedSlots.Count==0||!Workshops.Match(w.id,j.inputs).Exists(r=>r.id==j.recipeId)))throw new ArgumentException("제작 중 재료/진행도 오류");
                    if(!j.started&&(j.elapsed!=0||j.usedSlots.Count!=0)||j.rushed&&w.id!="alchemy")throw new ArgumentException("대기 작업 진행도 오류");
                    var slots=new HashSet<int>();foreach(var input in j.inputs){if(input==null||input.slot<0||input.slot>4||!slots.Add(input.slot))throw new ArgumentException("재료 슬롯 중복");ValidateWorkshopItem(input.item,ids);}
                    var used=new HashSet<int>();foreach(int slot in j.usedSlots)if(!used.Add(slot)||!slots.Contains(slot))throw new ArgumentException("가공 중 재료 잠금 오류");
                    if(j.started&&j.usedSlots.Count!=j.inputs.Count)throw new ArgumentException("작업 재료 잠금 누락");
                    foreach(var result in j.results)ValidateWorkshopItem(result,ids);
                }
            }
            if(Workshops!=null&&stations.Count!=Workshops.Data.workshops.Length)throw new ArgumentException("공방 누락");
        }
        public bool TryHireWorkshop(string id,bool hire,int day,out string error)
        {
            WorkshopState w;if(!CommandStation(id,out w,out error))return false;
            if(day!=state.lastSettledDay){error="현재 정산 날짜와 다릅니다.";return false;}
            var next=state.Copy();w=Station(next,id);w.hired=hire;
            try{if(hire&&w.wageDay!=day){w.wageDue=checked(w.wageDue+Workshops.Find(id).dailyWage);w.wageDay=day;}}catch(OverflowException){error="일급 범위 초과";return false;}
            Commit(next);return true;
        }
        public bool TryUnlockWorkshop(string id,out string error)
        {error="";if(Workshops==null||Workshops.Find(id)==null){error="공방 ID가 없습니다.";return false;}var next=state.Copy();Station(next,id).unlocked=true;Commit(next);return true;}
        public bool TryUnlockAllWorkshopsForDemo(out string error)
        {error="";if(Workshops==null){error="공방 데이터 없음";return false;}var next=state.Copy();foreach(var w in next.workshops)w.unlocked=true;Commit(next);return true;}
        public List<WorkshopRecipe> MatchWorkshopRecipes(string station,string jobId)
        {var w=Station(state,station);var j=w==null?null:w.jobs.Find(job=>job.id==jobId);return j==null?new List<WorkshopRecipe>():Workshops.Match(station,j.inputs);}
        void RefreshWorkshopRecipe(WorkshopJob j,string station)
        {var matches=Workshops.Match(station,j.inputs);if(!matches.Exists(r=>r.id==j.recipeId))j.recipeId=matches.Count>0?matches[0].id:"";j.elapsed=0;j.completed=0;j.rushed=j.exhausted=false;j.usedSlots.Clear();}
        public bool TryChooseWorkshopRecipe(string station,string id,string recipeId,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;
            if(j.started||!Workshops.Match(station,j.inputs).Exists(r=>r.id==recipeId)){error="재료에 맞는 결과 후보를 선택하세요.";return false;}
            var next=state.Copy();Station(next,station).jobs.Find(v=>v.id==id).recipeId=recipeId;Commit(next);return true;
        }
        // Compatibility entry point; the new UI discovers recipes from the five physical inputs.
        public bool TryCreateWorkshopJob(string station,string recipeId,int cycles,out string jobId,out string error)
        {
            jobId=null;WorkshopState w;if(!CommandStation(station,out w,out error))return false;var recipe=Workshops.Recipe(recipeId);
            var draft=w.jobs.Find(j=>!j.started&&j.inputs.Count==0);
            if(recipe==null||recipe.workshopId!=station||draft==null||cycles<1||cycles>9999||station!="processing"&&cycles!=1){error="작업/반복 횟수 오류";return false;}
            var next=state.Copy();var job=Station(next,station).jobs.Find(j=>j.id==draft.id);job.recipeId=recipeId;job.repeatMode=cycles>1?1:0;job.cycles=cycles;job.completed=0;jobId=job.id;Commit(next);return true;
        }
        public bool TryPutWorkshopMaterial(string station,string id,int slot,string source,string uid,long revision,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;var item=GetItem(source,uid);
            if(revision!=Revision||j.started||item==null||IsReserved(uid)||!IsWarehouse(source)&&source!=TemporaryStorageId||slot<0||slot>4||j.inputs.Exists(m=>m.slot==slot)){error="상자/임시 저장고에서 빈 재료 슬롯으로 옮기세요. 작업 중에는 수정할 수 없습니다.";return false;}
            var next=state.Copy();RemoveItem(next,source,uid);j=Station(next,station).jobs.Find(v=>v.id==id);item.x=item.y=0;j.inputs.Add(new WorkshopMaterial{slot=slot,item=item});RefreshWorkshopRecipe(j,station);Commit(next);return true;
        }
        public bool TryTakeWorkshopItem(string station,string id,int slot,bool result,string ignoredWarehouse,out string error)
        {
            if(result){int stored,overflow;return TryCollectWorkshopResults(station,out stored,out overflow,out error);}
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;
            var material=j.inputs.Find(m=>m.slot==slot);if(j.started||material==null){error="빈 슬롯이거나 가공 중인 재료입니다.";return false;}
            var next=state.Copy();j=Station(next,station).jobs.Find(v=>v.id==id);j.inputs.RemoveAll(m=>m.slot==slot);RefreshWorkshopRecipe(j,station);int storedCount,overflowCount;
            ReceiveItems(next,new[]{material.item},out storedCount,out overflowCount);Commit(next);return true;
        }
        public bool TrySetProcessingRepeat(string id,int mode,int count,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob("processing",id,out w,out j,out error))return false;
            if(j.started||mode<0||mode>2||count<1||count>9999){error="반복 설정을 확인하세요. 작업 중에는 변경할 수 없습니다.";return false;}
            var next=state.Copy();j=Station(next,"processing").jobs.Find(v=>v.id==id);j.repeatMode=mode;j.cycles=mode==0?1:mode==2?int.MaxValue:count;Commit(next);return true;
        }
        public bool CanStartWorkshopJob(string station,string id)
        {
            var w=Station(state,station);var j=w==null?null:w.jobs.Find(v=>v.id==id);
            return w!=null&&w.unlocked&&w.hired&&j!=null&&!j.started&&Workshops.Match(station,j.inputs).Exists(r=>r.id==j.recipeId);
        }
        public bool TryStartWorkshopJob(string station,string id,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;
            if(!CanStartWorkshopJob(station,id)){error="5개 슬롯의 조합과 고용 상태를 확인하세요.";return false;}
            var next=state.Copy();j=Station(next,station).jobs.Find(v=>v.id==id);j.started=true;j.rushed=j.exhausted=false;j.elapsed=0;j.completed=0;j.usedSlots.Clear();foreach(var input in j.inputs)j.usedSlots.Add(input.slot);
            if(station!="processing"){j.repeatMode=0;j.cycles=1;}else if(j.repeatMode==0)j.cycles=1;
            Commit(next);return true;
        }
        public bool TryMoveWorkshopJob(string station,string id,int direction,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;int index=w.jobs.IndexOf(j),target=index+direction;
            if(station!="smith"||Math.Abs(direction)!=1||target<0||target>=w.jobs.Count){error="작업 순서 범위 오류";return false;}
            var next=state.Copy();w=Station(next,station);j=w.jobs[index];w.jobs.RemoveAt(index);w.jobs.Insert(target,j);Commit(next);return true;
        }
        public bool TryCancelWorkshopJob(string station,string id,string ignoredWarehouse,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob(station,id,out w,out j,out error))return false;var next=state.Copy();j=Station(next,station).jobs.Find(v=>v.id==id);
            var items=new List<ItemRecord>(j.results);foreach(var input in j.inputs)items.Add(input.item);
            j.results.Clear();j.inputs.Clear();j.usedSlots.Clear();j.recipeId="";j.started=j.rushed=j.exhausted=false;j.elapsed=0;j.completed=0;int stored,overflow;
            ReceiveItems(next,items,out stored,out overflow);Commit(next);return true;
        }
        public int WorkshopResultCount(string station)
        {var w=Station(state,station);int count=0;if(w!=null)foreach(var j in w.jobs)count+=j.results.Count;return count;}
        public bool TryCollectWorkshopResults(string station,out int stored,out int overflow,out string error)
        {
            stored=overflow=0;WorkshopState w;if(!CommandStation(station,out w,out error))return false;
            if(WorkshopResultCount(station)==0){error="받을 완성품이 없습니다.";return false;}
            var next=state.Copy();var items=new List<ItemRecord>();foreach(var j in Station(next,station).jobs){items.AddRange(j.results);j.results.Clear();}
            ReceiveItems(next,items,out stored,out overflow);Validate(next);Commit(next);return true;
        }
        WorkshopJob ActiveWorkshopJob(WorkshopState w)
        {return w.jobs.Find(j=>j.started&&j.completed<j.cycles);}
        bool FillRepeatBatch(InventorySave next,WorkshopJob j,WorkshopRecipe recipe)
        {
            var picks=new List<ItemRecord>();var owners=new List<ContainerRecord>();var used=new HashSet<string>();
            foreach(var ingredient in recipe.ingredients)
            {
                int needed=ingredient.count;
                foreach(var box in next.containers)
                {
                    if(!IsWarehouse(box.containerId)&&!(box.containerId==TemporaryStorageId&&Workshops.Data.repeatUsesTemporary))continue;
                    foreach(var quality in Catalog.Find(ingredient.itemId).qualities)
                    {
                        foreach(var item in box.items)if(needed>0&&item.itemId==ingredient.itemId&&item.quality==quality.quality&&!IsReserved(item.instanceId)&&used.Add(item.instanceId)){picks.Add(item);owners.Add(box);needed--;}
                        if(needed==0)break;
                    }
                    if(needed==0)break;
                }
                if(needed>0)return false;
            }
            j.inputs.Clear();j.usedSlots.Clear();
            for(int n=0;n<picks.Count;n++){owners[n].items.Remove(picks[n]);var item=picks[n].Copy();item.x=item.y=0;j.inputs.Add(new WorkshopMaterial{slot=n,item=item});j.usedSlots.Add(n);}
            TrimDynamic(next,TemporaryStorageId);return true;
        }
        ItemRecord MakeWorkshopOutput(InventorySave save,WorkshopRecipe recipe,IList<ItemRecord> materials)
        {
            var definition=Catalog.Find(recipe.outputItemId);var probabilities=Workshops.Probabilities(definition,materials);uint bits=unchecked((uint)save.workshopRandom);if(bits==0)bits=1337;
            bits^=bits<<13;bits^=bits>>17;bits^=bits<<5;save.workshopRandom=unchecked((int)bits);double roll=bits/4294967296.0,sum=0;int chosen=probabilities.Length-1;
            for(int n=0;n<probabilities.Length;n++){sum+=probabilities[n];if(roll<sum){chosen=n;break;}}
            long cost=0;foreach(var material in materials)cost+=material.purchasePrice;
            return new ItemRecord{instanceId=Guid.NewGuid().ToString("N"),itemId=recipe.outputItemId,quality=definition.qualities[chosen].quality,purchasePrice=(int)Math.Min(int.MaxValue,cost)};
        }
        public bool TryInstantAlchemy(string id,out string error)
        {
            WorkshopState w;WorkshopJob j;if(!FindJob("alchemy",id,out w,out j,out error))return false;
            if(!w.hired||!j.started||j.rushed||!WorkshopCanOperate("alchemy")){error="가동 중인 조제에 한 번만 긴급 제작을 적용할 수 있습니다.";return false;}
            var next=state.Copy();w=Station(next,"alchemy");try{w.extraDue=checked(w.extraDue+Workshops.Find("alchemy").instantFee);}catch(OverflowException){error="긴급 제작료 범위 초과";return false;}
            w.jobs.Find(v=>v.id==id).rushed=true;Commit(next);return true;
        }
        public double WorkshopRemaining(WorkshopJob job)
        {var recipe=Workshops.Recipe(job.recipeId);return recipe==null?0:Math.Max(0,recipe.Duration-job.elapsed)*(job.rushed?Workshops.Data.emergencyRemainingFraction:1);}
        public void AdvanceWorkshops(double minutes){AdvanceWorkshopsDuring(minutes,minutes*60,ShopPhase.Day,state.lastSettledDay);}
        public void AdvanceWorkshopsDuring(double gameMinutes,double realSeconds,ShopPhase phase,int day)
        {
            if(!WorkshopCatalog.Finite(gameMinutes)||gameMinutes<0||gameMinutes>10080||!WorkshopCatalog.Finite(realSeconds)||realSeconds<0||realSeconds>604800||day<1||(int)phase<0||(int)phase>2)throw new ArgumentException("공방 시간 증가 오류");
            SetWorkshopTime(phase,day);if(Workshops==null)return;bool changed=false;
            foreach(var definition in Workshops.Data.workshops)
            {
                if(!WorkshopCanOperate(definition.id))continue;var active=ActiveWorkshopJob(Station(state,definition.id));if(active==null)continue;
                var first=Workshops.Recipe(active.recipeId);double remaining=first.workSeconds>0?realSeconds:gameMinutes;
                while(remaining>0)
                {
                    var w=Station(state,definition.id);var j=ActiveWorkshopJob(w);if(j==null)break;var recipe=Workshops.Recipe(j.recipeId);
                    double rate=j.rushed?1/Workshops.Data.emergencyRemainingFraction:1;double step=Math.Min(remaining,(recipe.Duration-j.elapsed)/rate);
                    remaining-=step;j.elapsed+=step*rate;changed|=step>0;
                    if(j.elapsed+1e-8<recipe.Duration)break;
                    var next=state.Copy();j=Station(next,w.id).jobs.Find(v=>v.id==j.id);var materials=new List<ItemRecord>();foreach(var input in j.inputs)materials.Add(input.item);
                    j.results.Add(MakeWorkshopOutput(next,recipe,materials));j.inputs.Clear();j.usedSlots.Clear();j.elapsed=0;j.completed++;j.rushed=false;
                    bool repeat=w.id=="processing"&&j.repeatMode!=0&&j.completed<j.cycles;
                    if(!repeat)j.started=false;
                    else if(!FillRepeatBatch(next,j,recipe)){j.started=false;j.exhausted=true;}
                    Commit(next);
                }
            }
            if(changed)NotifyWorkshops();
        }
        public string WorkshopJobStatus(string station,WorkshopJob j)
        {
            if(j.exhausted)return "재료 소진 · 반복 종료";
            if(!j.started)return j.results.Count>0?"완성품 수령 대기":Workshops.Match(station,j.inputs).Count==0?"재료 조합 대기":"제작 준비";
            var w=Station(state,station);if(!w.hired)return "고용 중지";if(!WorkshopCanOperate(station))return workshopPhase==ShopPhase.Night?"야간 휴무":"새벽 휴무";
            if(ActiveWorkshopJob(w)!=w.jobs.Find(v=>v.id==j.id))return "순서 대기";return j.rushed?"긴급 제작 중":"제작 중";
        }
        public List<ItemRecord> WorkshopBatchMaterials(WorkshopJob job)
        {var result=new List<ItemRecord>();foreach(var input in job.inputs)result.Add(input.item.Copy());return result;}
    }
}
