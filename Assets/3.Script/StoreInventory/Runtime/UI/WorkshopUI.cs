using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class WorkshopUI:MonoBehaviour
    {
        public InventoryPanel panel;public GameObject window;public RectTransform frame,content;
        public Text account,recipeLabel,quantityLabel,warehouseLabel,probabilityLabel,hireLabel;
        public WorkshopJobView[] rows;public Button addButton;
        public string StationId{get;private set;}="processing";
        public InventoryModel Model{get{return panel.Model;}}public WorkshopDefinition Definition{get{return Model.Workshops.Find(StationId);}}
        readonly List<WorkshopRecipe> recipes=new List<WorkshopRecipe>();List<string> warehouses;int recipeIndex,quantity=1,warehouseIndex=1;string selected;
        bool ready,dirty=true,progressDirty;InventoryModel subscribedModel;InventoryRuntime subscribedRuntime;
        bool Busy{get{return panel.drag.IsDragging||panel.IsWindowDragging||panel.shop.HasPendingPlacement||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging);}}
        string Warehouse{get{return warehouses!=null&&warehouses.Count>0?warehouses[Mathf.Clamp(warehouseIndex,0,warehouses.Count-1)]:"warehouse";}}
        void Start()
        {
            string error;if(panel==null||window==null||frame==null||rows==null||!panel.inventory.TryInitialize(out error)||Model.Workshops==null){Debug.LogError("공방 데이터/데모 참조가 없습니다. Create Latest Demo로 생성하세요.",this);enabled=false;return;}
            foreach(var row in rows)row.Initialize(this);subscribedModel=Model;subscribedRuntime=panel.inventory;Model.Changed+=MarkDirty;Model.WorkshopsChanged+=MarkProgress;panel.inventory.Loaded+=Reload;ready=true;RefreshRecipes();MarkDirty();
        }
        void MarkDirty(){dirty=true;}
        void MarkProgress(){progressDirty=true;}
        void Reload(){selected=null;MarkDirty();}
        void LateUpdate()
        {
            if(!ready||!window.activeSelf)return;RefreshLayout();if(dirty){dirty=false;progressDirty=false;Render();}else if(progressDirty){progressDirty=false;RenderRows(Model.GetWorkshop(StationId));}
        }
        void RefreshLayout()
        {
            var rect=(RectTransform)window.transform;float scale=Mathf.Min(1,Mathf.Min(Mathf.Max(1,rect.rect.height-40)/860,Mathf.Max(1,rect.rect.width-210)/560));
            var p=new Vector2(Mathf.Max(104,rect.rect.width-560*scale-120),-Mathf.Max(20,(rect.rect.height-860*scale)*.5f));
            if(frame.anchoredPosition!=p)frame.anchoredPosition=p;if(frame.localScale.x!=scale)frame.localScale=new Vector3(scale,scale,1);
        }
        void RefreshRecipes(){recipes.Clear();foreach(var r in Model.Workshops.Data.recipes)if(r.workshopId==StationId)recipes.Add(r);recipeIndex=0;quantity=1;}
        public void Open(){window.SetActive(true);if(ready)OpenMaterialBox();else panel.shop.drawer.Open();MarkDirty();}
        public void Close(){if(!Busy)window.SetActive(false);}
        public void Processing(){Switch("processing");}public void Alchemy(){Switch("alchemy");}public void Smith(){Switch("smith");}
        void Switch(string id){if(!ready||Busy)return;StationId=id;selected=null;RefreshRecipes();content.anchoredPosition=Vector2.zero;MarkDirty();}
        public void PreviousRecipe(){StepRecipe(-1);}public void NextRecipe(){StepRecipe(1);}
        void StepRecipe(int d){if(Busy||recipes.Count==0)return;recipeIndex=(recipeIndex+d+recipes.Count)%recipes.Count;quantity=1;MarkDirty();}
        public void CycleQuantity(){if(Busy||recipes.Count==0)return;int max=StationId=="processing"?5/recipes[recipeIndex].outputCount:1;quantity=quantity%max+1;MarkDirty();}
        public void CycleWarehouse(){if(Busy||warehouses==null||warehouses.Count==0)return;warehouseIndex=(warehouseIndex+1)%warehouses.Count;MarkDirty();}
        public void Hire()
        {if(!ready||Busy)return;var w=Model.GetWorkshop(StationId);string error;bool ok=Model.TryHireWorkshop(StationId,!w.hired,panel.inventory.Clock.Day,out error);Message(ok,w.hired?"고용 중지. 이미 발생한 일급은 유지됩니다.":"고용 시작. 일급은 새벽에 정산합니다.",error);}
        public void AddJob()
        {if(!ready||Busy||recipes.Count==0)return;string id,error;bool ok=Model.TryCreateWorkshopJob(StationId,recipes[recipeIndex].id,quantity,out id,out error);if(ok)selected=id;Message(ok,"재료를5개 슬롯에 넣고 시작을 누르세요.",error);}
        public void Put(string job,int slot,ItemProposal proposal)
        {if(!ready||Busy)return;string error;bool ok=Model.TryPutWorkshopMaterial(StationId,job,slot,proposal.containerId,proposal.instanceId,proposal.revision,out error);selected=job;Message(ok,"재료를 투입했습니다.",error);}
        public void Take(string job,int slot,bool result)
        {if(!ready||Busy)return;string error;bool ok=Model.TryTakeWorkshopItem(StationId,job,slot,result,Warehouse,out error);selected=job;Message(ok,"선택한 상자로 회수했습니다.",error);}
        public void StartJob(string job)
        {if(!ready||Busy||job==null)return;string error;bool ok=Model.TryStartWorkshopJob(StationId,job,out error);selected=job;Message(ok,"작업을 예약했습니다. 맨 위 작업부터 진행합니다.",error);}
        public void CancelJob(string job)
        {if(!ready||Busy||job==null)return;string error;bool ok=Model.TryCancelWorkshopJob(StationId,job,Warehouse,out error);Message(ok,"남은 재료와 결과물을 회수하고 작업을 취소했습니다.",error);}
        public void MoveJob(string job,int direction)
        {if(!ready||Busy||job==null)return;string error;bool ok=Model.TryMoveWorkshopJob(StationId,job,direction,out error);selected=job;Message(ok,"진행도를 유지하고 순서를 변경했습니다.",error);}
        public void Instant(string job)
        {if(!ready||Busy||job==null)return;string error;bool ok=Model.TryInstantAlchemy(job,out error);Message(ok,"즉시 조제 완료. 새벽 추가금 +"+Definition.instantFee+" G",error);}
        public void SelectJob(string job){selected=job;MarkDirty();}
        public void UnlockAllForDemo(){if(!ready||Busy)return;string error;bool ok=Model.TryUnlockAllWorkshopsForDemo(out error);Message(ok,"테스트: 공방3곳 해금. 각 공방에서 고용을 시작하세요.",error);}
        public void OpenMaterialBox(){if(!ready||Busy)return;panel.GetComponent<WarehouseUI>().Select(Warehouse);panel.shop.drawer.Open();}
        void Message(bool success,string text,string error){panel.ShowMessage(success?text:error);MarkDirty();}
        public string ItemName(ItemRecord item){var q=panel.Catalog.FindQuality(item.itemId,item.quality);return q==null?item.itemId:q.name;}
        void Render()
        {
            var w=Model.GetWorkshop(StationId);var d=Definition;warehouses=Model.WarehouseIds();warehouseIndex=Mathf.Clamp(warehouseIndex,0,Mathf.Max(0,warehouses.Count-1));warehouseLabel.text="수령/회수: 상자 "+(warehouseIndex+1);
            hireLabel.text=w.hired?"고용 중지":"고용 시작";
            account.text=d.name+" · "+(w.unlocked?(w.hired?"고용 중":"미고용"):"잠김 · 손님 거래 "+Model.SuccessfulCustomerSales+"/"+d.unlockSales)+"\n일급 "+d.dailyWage+" G · 이번 정산 "+w.wageDue+" + 추가 "+w.extraDue+" G"+(StationId=="alchemy"?" · 즉시 +"+d.instantFee+" G":"");
            if(recipes.Count>0){var r=recipes[recipeIndex];var sb=new StringBuilder(r.name+" ×"+r.outputCount+" / "+r.minutes+" 게임 분\n");foreach(var i in r.ingredients){var def=panel.Catalog.Find(i.itemId);sb.Append(def.qualities[0].name).Append(" ×").Append(i.count).Append("  ");}recipeLabel.text=sb.ToString();}else recipeLabel.text="등록된 조합 없음";
            quantityLabel.text=StationId=="processing"?quantity+"회 제작":"1회 제작";addButton.interactable=w.unlocked&&w.hired&&w.jobs.Count<d.queueCapacity&&recipes.Count>0;
            RenderRows(w);
            var job=w.jobs.Find(x=>x.id==selected);if(job==null&&w.jobs.Count>0){job=w.jobs[0];selected=job.id;}
            if(job==null){probabilityLabel.text="재료 품질에 따라 결과 품질 확률이 달라집니다.";return;}
            var materials=Model.WorkshopBatchMaterials(job);if(materials.Count==0){probabilityLabel.text="한 회차 재료를 넣으면 품질 확률이 표시됩니다.";return;}
            var output=panel.Catalog.Find(Model.Workshops.Recipe(job.recipeId).outputItemId);var chances=Model.Workshops.Probabilities(output,materials);var text=new StringBuilder("선택 작업 품질 확률: ");for(int n=0;n<chances.Length;n++)text.Append(QualityLabels.Title(output.qualities[n].quality)).Append(' ').Append((chances[n]*100).ToString("0.0")).Append("%  ");probabilityLabel.text=text.ToString();
        }
        void RenderRows(WorkshopState w)
        {
            int capacity=Definition.queueCapacity;var size=new Vector2(content.sizeDelta.x,capacity*232);if(content.sizeDelta!=size)content.sizeDelta=size;
            for(int n=0;n<rows.Length;n++){bool visible=n<capacity;if(rows[n].gameObject.activeSelf!=visible)rows[n].gameObject.SetActive(visible);if(visible)rows[n].Bind(this,n<w.jobs.Count?w.jobs[n]:null,n,w.jobs.Count);}
        }
        void OnDestroy(){if(!ready)return;subscribedModel.Changed-=MarkDirty;subscribedModel.WorkshopsChanged-=MarkProgress;if(subscribedRuntime!=null)subscribedRuntime.Loaded-=Reload;}
    }
}
