using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class WorkshopUI:MonoBehaviour
    {
        public InventoryPanel panel;public GameObject window,repeatControls;public RectTransform frame,content;
        public Text titleLabel,account,probabilityLabel,hireLabel,collectLabel,repeatLabel,nightLabel;
        public Button collectButton,nightButton,repeatModeButton;public InputField repeatCountInput;public WorkshopJobView[] rows;
        public string StationId{get;private set;}="processing";
        public InventoryModel Model{get{return panel.Model;}}public WorkshopDefinition Definition{get{return Model.Workshops.Find(StationId);}}
        readonly Dictionary<string,WorkshopJobView> bound=new Dictionary<string,WorkshopJobView>();
        bool ready,dirty=true,progressDirty;string selected;int repeatMode;float shownPanelHeight;
        InventoryModel subscribedModel;InventoryRuntime subscribedRuntime;
        bool Busy{get{return panel.drag.IsDragging||panel.IsWindowDragging||panel.shop.HasPendingPlacement||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging);}}
        void Start()
        {
            string error;if(panel==null||window==null||frame==null||rows==null||rows.Length<3||titleLabel==null||collectButton==null||!panel.inventory.TryInitialize(out error)||Model.Workshops==null){Debug.LogError("공방 UI 참조를 갱신하세요. 기존 씬 업데이트 메뉴 또는 Create Latest Demo를 사용하세요.",this);enabled=false;return;}
            foreach(var row in rows)row.Initialize(this);subscribedModel=Model;subscribedRuntime=panel.inventory;Model.Changed+=MarkDirty;Model.WorkshopsChanged+=MarkProgress;panel.inventory.Loaded+=Reload;ready=true;repeatMode=Model.GetWorkshop("processing").jobs[0].repeatMode;RestoreRepeatCount();MarkDirty();
        }
        void MarkDirty(){dirty=true;}void MarkProgress(){progressDirty=true;}
        void Reload(){selected=null;bound.Clear();repeatMode=Model.GetWorkshop("processing").jobs[0].repeatMode;RestoreRepeatCount();MarkDirty();}
        void LateUpdate()
        {
            if(!ready||!window.activeSelf)return;RefreshLayout();
            if(dirty){dirty=false;progressDirty=false;Render();}else if(progressDirty){progressDirty=false;RenderProgress();}
        }
        void RefreshLayout()
        {
            var root=(RectTransform)window.transform;var canvas=(RectTransform)panel.canvas.transform;
            float height=StationId=="smith"?664:StationId=="processing"?508:404;
            if(shownPanelHeight!=height)
            {
                shownPanelHeight=height;frame.sizeDelta=new Vector2(860,height);
                probabilityLabel.rectTransform.anchoredPosition=new Vector2(24,-(height-136));
                var collect=(RectTransform)collectButton.transform;collect.anchoredPosition=new Vector2(24,-(height-98));
                var close=frame.Find("닫기") as RectTransform;if(close!=null)close.anchoredPosition=new Vector2(572,-(height-98));
                foreach(string name in new[]{"가공소","대장간","연금술공방"}){var tab=frame.Find(name) as RectTransform;if(tab!=null)tab.anchoredPosition=new Vector2(tab.anchoredPosition.x,-(height-46));}
            }
            float scale=Mathf.Min(1,Mathf.Min(Mathf.Max(1,canvas.rect.width-220)/860,Mathf.Max(1,canvas.rect.height-80)/height));
            var position=new Vector2(Mathf.Max(104,root.rect.width-860*scale-24),-Mathf.Max(40,root.rect.height-height*scale-65));
            if(frame.localScale.x!=scale)frame.localScale=new Vector3(scale,scale,1);if(frame.anchoredPosition!=position)frame.anchoredPosition=position;
        }
        public void OpenTemporaryStorage(){if(!ready||Busy)return;var storage=panel.GetComponentInChildren<DeliveryUI>(true);if(storage!=null)storage.OpenStorage();}
        public void Open(){window.SetActive(true);panel.shop.drawer.Open();MarkDirty();}
        public void Close(){if(!Busy)window.SetActive(false);}
        public void Processing(){Switch("processing");}public void Smith(){Switch("smith");}public void Alchemy(){Switch("alchemy");}
        void Switch(string id){if(!ready||Busy)return;StationId=id;selected=null;bound.Clear();repeatMode=Model.GetWorkshop(id).jobs[0].repeatMode;MarkDirty();}
        public void Hire()
        {if(!ready||Busy)return;var w=Model.GetWorkshop(StationId);string error;bool ok=Model.TryHireWorkshop(StationId,!w.hired,panel.inventory.Clock.Day,out error);Message(ok,w.hired?"고용을 중지했습니다. 발생한 일급은 유지됩니다.":"고용 시작. 일급은 새벽에 정산합니다.",error);}
        public void PayNightWork()
        {if(!ready||Busy)return;string error;bool ok=Model.TryPayNightWork(StationId,panel.inventory.Clock.Day,panel.inventory.Clock.Phase,out error);Message(ok,"오늘 밤 작업 신청. 추가 수당은 다음 새벽 정산입니다.",error);}
        public void Put(string job,int slot,ItemProposal proposal)
        {if(!ready||Busy)return;string error;bool ok=Model.TryPutWorkshopMaterial(StationId,job,slot,proposal.containerId,proposal.instanceId,proposal.revision,out error);selected=job;Message(ok,"재료를 넣었습니다.",error);}
        public void Take(string job,int slot,bool result)
        {if(!ready||Busy)return;if(result){CycleCandidate(job);return;}string error;bool ok=Model.TryTakeWorkshopItem(StationId,job,slot,false,"warehouse",out error);Message(ok,"재료를 상자/임시 저장고로 회수했습니다.",error);}
        public void CycleCandidate(string job)
        {
            if(!ready||Busy)return;var choices=Model.MatchWorkshopRecipes(StationId,job);if(choices.Count<2)return;var current=Model.GetWorkshop(StationId).jobs.Find(v=>v.id==job);if(current.started)return;
            int index=choices.FindIndex(recipe=>recipe.id==current.recipeId);string error;bool ok=Model.TryChooseWorkshopRecipe(StationId,job,choices[(index+1)%choices.Count].id,out error);Message(ok,"제작 결과 후보를 바꿨습니다.",error);
        }
        void RestoreRepeatCount(){var job=Model.GetWorkshop("processing").jobs[0];if(job.repeatMode==1)repeatCountInput.text=Math.Min(9999,job.cycles).ToString();}
        public void CycleRepeatMode(){if(!ready||Busy||Model.GetWorkshop("processing").jobs[0].started)return;repeatMode=(repeatMode+1)%3;ApplyRepeat();MarkDirty();}
        public void RepeatEdited(string ignored){if(ready&&!Busy)ApplyRepeat();}
        bool ApplyRepeat()
        {
            int count;if(!int.TryParse(repeatCountInput.text,out count)||count<1||count>9999){panel.ShowMessage("반복 횟수는1~9999입니다.");return false;}
            var job=Model.GetWorkshop("processing").jobs[0];string error;bool ok=Model.TrySetProcessingRepeat(job.id,repeatMode,count,out error);if(!ok)panel.ShowMessage(error);MarkDirty();return ok;
        }
        public void StartJob(string job)
        {if(!ready||Busy)return;if(StationId=="processing"&&!ApplyRepeat())return;string error;bool ok=Model.TryStartWorkshopJob(StationId,job,out error);selected=job;Message(ok,"제작 시작. 휴무에는 진행도가 보존됩니다.",error);}
        public void CancelJob(string job)
        {if(!ready||Busy)return;string error;bool ok=Model.TryCancelWorkshopJob(StationId,job,"warehouse",out error);Message(ok,"남은 재료와 완성품을 상자/임시 저장고로 회수했습니다.",error);}
        public void MoveJob(string job,int direction)
        {if(!ready||Busy)return;string error;bool ok=Model.TryMoveWorkshopJob(StationId,job,direction,out error);Message(ok,"진행도를 유지하고 우선순위를 바꿨습니다.",error);}
        public void Instant(string job)
        {if(!ready||Busy)return;string error;bool ok=Model.TryInstantAlchemy(job,out error);Message(ok,"긴급 제작 적용. 남은 작업 시간이 "+(Model.Workshops.Data.emergencyRemainingFraction*100).ToString("0")+"%가 됩니다. 추가금은 새벽 정산입니다.",error);}
        public void CollectAll()
        {if(!ready||Busy)return;int stored,overflow;string error;bool ok=Model.TryCollectWorkshopResults(StationId,out stored,out overflow,out error);Message(ok,"완성품 수령: 상자 "+stored+"개 / 임시 저장고 "+overflow+"개",error);}
        public void SelectJob(string id){selected=id;MarkDirty();}
        public void UnlockAllForDemo(){if(!ready||Busy)return;string error;bool ok=Model.TryUnlockAllWorkshopsForDemo(out error);Message(ok,"공방3곳 해금. 고용 후 제작하세요.",error);}
        public string ItemName(ItemRecord item){var quality=panel.Catalog.FindQuality(item.itemId,item.quality);return quality==null?item.itemId:quality.name;}
        void Message(bool ok,string success,string error){panel.ShowMessage(ok?success:error);MarkDirty();}
        void Render()
        {
            var w=Model.GetWorkshop(StationId);var definition=Definition;titleLabel.text=definition.name;hireLabel.text=w.hired?"고용 중지":"고용 시작";
            account.text=(w.unlocked?(w.hired?"고용 중":"미고용"):"잠김 · 거래 "+Model.SuccessfulCustomerSales+"/"+definition.unlockSales)+" · 일급 "+definition.dailyWage+" G · 정산 "+w.wageDue+" + 추가 "+w.extraDue+" G";
            repeatControls.SetActive(StationId=="processing");repeatModeButton.interactable=!w.jobs[0].started;repeatCountInput.interactable=repeatMode==1&&!w.jobs[0].started;repeatLabel.text=repeatMode==0?"1회":repeatMode==1?"횟수 지정":"재료 소진까지";
            int count=StationId=="smith"?3:1;var used=new HashSet<WorkshopJobView>();
            for(int index=0;index<count;index++)
            {
                var job=w.jobs[index];WorkshopJobView view;
                if(!bound.TryGetValue(job.id,out view)){view=null;foreach(var candidate in rows)if(!used.Contains(candidate)&&!bound.ContainsValue(candidate)){view=candidate;break;}if(view==null){bound.Clear();foreach(var candidate in rows)if(!used.Contains(candidate)){view=candidate;break;}}bound[job.id]=view;}
                used.Add(view);bool first=!view.gameObject.activeSelf;view.gameObject.SetActive(true);view.SetOrder(index,first);view.Bind(this,job,index,count);
            }
            foreach(var view in rows)if(!used.Contains(view))view.gameObject.SetActive(false);
            var chosen=w.jobs.Find(job=>job.id==selected)??w.jobs[0];var recipe=Model.Workshops.Recipe(chosen.recipeId);
            probabilityLabel.text="5개 슬롯 · 한 칸에 물건1개 · 한 번에 완성품1개";
            if(recipe!=null&&chosen.inputs.Count>0)
            {
                var materials=Model.WorkshopBatchMaterials(chosen);var output=panel.Catalog.Find(recipe.outputItemId);var probabilities=Model.Workshops.Probabilities(output,materials);var text=new StringBuilder("품질 확률: ");
                for(int n=0;n<probabilities.Length;n++)text.Append(QualityLabels.Title(output.qualities[n].quality)).Append(' ').Append((probabilities[n]*100).ToString("0")).Append("%  ");probabilityLabel.text=text.ToString();
            }
            RenderProgress();
        }
        void RenderProgress()
        {
            var w=Model.GetWorkshop(StationId);int count=Model.WorkshopResultCount(StationId);collectLabel.text="완성품 받기 "+count;collectButton.interactable=count>0;
            bool paid=w.nightWorkDay==panel.inventory.Clock.Day;nightLabel.text=paid?"야간 신청 완료":"야간 +"+Model.NightWorkFee(StationId)+" G";
            nightButton.interactable=w.hired&&w.unlocked&&!paid&&panel.inventory.Clock.Phase==ShopPhase.Night;
            foreach(var row in rows)if(row.gameObject.activeSelf){var job=w.jobs.Find(v=>v.id==row.JobId);if(job!=null)row.RefreshProgress(this,job);}
        }
        void OnDestroy(){if(ready){subscribedModel.Changed-=MarkDirty;subscribedModel.WorkshopsChanged-=MarkProgress;subscribedRuntime.Loaded-=Reload;}}
    }
}
