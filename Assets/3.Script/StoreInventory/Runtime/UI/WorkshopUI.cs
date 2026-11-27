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
        public Text titleLabel,account,probabilityLabel,collectLabel,repeatLabel,nightLabel;
        [HideInInspector]public Text hireLabel; // Only used to hide the button in older scenes.
        public Button collectButton,nightButton,repeatModeButton;public InputField repeatCountInput;public WorkshopJobView[] rows;
        public ProcessingPanelView processingView;
        public GameObject[] legacyPanelControls;
        public global::UnityEngine.UI.Image legacyBackground;
        bool StyledProcessing{get{return processingView!=null&&StationId=="processing";}}
        public string StationId{get;private set;}="processing";
        public InventoryModel Model{get{return panel.Model;}}public WorkshopDefinition Definition{get{return Model.Workshops.Find(StationId);}}
        readonly HashSet<WorkshopJobView> usedRows=new HashSet<WorkshopJobView>();
        [Range(.05f,1)]public float progressRefreshSeconds=.1f;float nextProgress;
        readonly Dictionary<string,WorkshopJobView> bound=new Dictionary<string,WorkshopJobView>();
        bool ready,dirty=true,progressDirty,accessDirty=true;string selected;int repeatMode;float shownPanelHeight;
        static readonly string[] stationIds={"processing","smith","alchemy"};
        static readonly string[] stationNames={"가공소","대장간","연금술공방"};
        readonly RectTransform[] tabs=new RectTransform[3];
        RightSideMenu menu;ManagementWindowMotion motion;
        InventoryModel subscribedModel;InventoryRuntime subscribedRuntime;
        bool Busy{get{return panel.drag.IsDragging||panel.IsWindowDragging||panel.shop.HasPendingPlacement||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging);}}
        void Start()
        {
            string error;if(panel==null||window==null||frame==null||rows==null||rows.Length<3||titleLabel==null||collectButton==null||!panel.inventory.TryInitialize(out error)||Model.Workshops==null){Debug.LogError("공방 UI 참조를 갱신하세요. 기존 씬 업데이트 메뉴 또는 Create Latest Demo를 사용하세요.",this);enabled=false;return;}
            foreach(var row in rows)row.Initialize(this);
            if(processingView!=null)processingView.job.Initialize(this);
            for(int n=0;n<tabs.Length;n++)tabs[n]=frame.Find(stationNames[n]) as RectTransform;
            if(hireLabel!=null){var oldButton=hireLabel.GetComponentInParent<Button>();if(oldButton!=null)oldButton.gameObject.SetActive(false);}
            menu=panel.transform.root.GetComponentInChildren<RightSideMenu>(true);motion=ManagementWindowMotion.Ensure(window,frame,true);
            subscribedModel=Model;subscribedRuntime=panel.inventory;Model.Changed+=MarkDirty;Model.WorkshopsChanged+=MarkProgress;panel.inventory.Loaded+=Reload;ready=true;repeatMode=Model.GetWorkshop("processing").jobs[0].repeatMode;RestoreRepeatCount();MarkDirty();RefreshAccess();
        }
        void MarkDirty(){dirty=true;accessDirty=true;}void MarkProgress(){progressDirty=true;}
        void Reload(){selected=null;bound.Clear();repeatMode=Model.GetWorkshop("processing").jobs[0].repeatMode;RestoreRepeatCount();MarkDirty();}
        void LateUpdate()
        {
            if(!ready)return;if(accessDirty)RefreshAccess();if(!window.activeSelf)return;RefreshLayout();
            if(dirty){dirty=false;progressDirty=false;Render();}else if(progressDirty&&Time.unscaledTime>=nextProgress){progressDirty=false;RenderProgress();}
        }
        void RefreshLayout()
        {
            var root=(RectTransform)window.transform;var canvas=(RectTransform)panel.canvas.transform;
            float height=StyledProcessing?((RectTransform)processingView.transform).rect.height:StationId=="smith"?664:StationId=="processing"?508:404;
            if(shownPanelHeight!=height)
            {
                shownPanelHeight=height;frame.sizeDelta=new Vector2(860,height);
                probabilityLabel.rectTransform.anchoredPosition=new Vector2(24,-(height-136));
                var collect=(RectTransform)collectButton.transform;collect.anchoredPosition=new Vector2(24,-(height-98));
                var close=frame.Find("닫기") as RectTransform;if(close!=null)close.anchoredPosition=new Vector2(572,-(height-98));
            }
            int visible=0;for(int n=0;n<tabs.Length;n++)if(tabs[n]!=null&&tabs[n].gameObject.activeSelf)
            {var tabPosition=new Vector2(24+276*visible++,-(height-46));if(tabs[n].anchoredPosition!=tabPosition)tabs[n].anchoredPosition=tabPosition;}
            float scale=Mathf.Min(1,Mathf.Min(Mathf.Max(1,canvas.rect.width-220)/860,Mathf.Max(1,canvas.rect.height-80)/height));
            var position=new Vector2(Mathf.Max(104,root.rect.width-860*scale-24),-Mathf.Max(40,root.rect.height-height*scale-65));
            if(frame.localScale.x!=scale)frame.localScale=new Vector3(scale,scale,1);motion.SetTarget(position);
        }
        void RefreshAccess()
        {
            accessDirty=false;string first=null;
            for(int n=0;n<stationIds.Length;n++)
            {
                bool available=Model.IsWorkshopUnlocked(stationIds[n]);if(available&&first==null)first=stationIds[n];
                bool showLegacy=available&&!StyledProcessing;
                if(tabs[n]!=null&&tabs[n].gameObject.activeSelf!=showLegacy)tabs[n].gameObject.SetActive(showLegacy);
                if(processingView!=null&&processingView.tabs[n].gameObject.activeSelf!=available)
                    processingView.tabs[n].gameObject.SetActive(available);
            }
            if(menu!=null)menu.SetUnlocked("workshops",first!=null);
            if(first==null){window.SetActive(false);return;}
            if(!Model.IsWorkshopUnlocked(StationId))SelectStation(first);
        }
        public void OpenTemporaryStorage(){if(!ready||Busy)return;var storage=panel.GetComponentInChildren<DeliveryUI>(true);if(storage!=null)storage.OpenStorage();}
        public void Open(){if(!ready||Busy)return;RefreshAccess();if(!Model.IsWorkshopUnlocked(StationId))return;window.SetActive(true);panel.shop.drawer.Open();MarkDirty();}
        public void Close(){if(!Busy){if(motion!=null)motion.fromBelow=true;ManagementWindowMotion.CloseWindow(window);}}
        public void Processing(){Switch("processing");}public void Smith(){Switch("smith");}public void Alchemy(){Switch("alchemy");}
        void Switch(string id){if(!ready||Busy||!Model.IsWorkshopUnlocked(id))return;SelectStation(id);}
        void SelectStation(string id)
        {
            if(StationId==id)return;
            StationId=id;selected=null;bound.Clear();
            if(id=="processing"){repeatMode=Model.GetWorkshop(id).jobs[0].repeatMode;RestoreRepeatCount();}
            MarkDirty();
        }
        public void Hire()
        {if(ready)panel.ShowMessage("해금된 공방은 자동으로 고용됩니다.");}
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
        public void CycleRepeatMode(){SetRepeatMode((repeatMode+1)%3);}
        public void SetRepeatMode(int mode)
        {
            if(!ready||Busy||StationId!="processing")return;
            if(ApplyRepeat(mode)){repeatMode=mode;MarkDirty();}
        }
        public void StepRepeatCount(int step)
        {
            if(!ready||Busy||StationId!="processing"||repeatMode!=1||Model.GetWorkshop("processing").jobs[0].started)return;
            int count;
            if(!int.TryParse(repeatCountInput.text,out count))count=1;
            repeatCountInput.SetTextWithoutNotify(Mathf.Clamp(Mathf.Clamp(count,1,9999)+step,1,9999).ToString());
            ApplyRepeat(repeatMode);
        }
        public void RepeatEdited(string ignored){if(ready&&!Busy&&StationId=="processing")ApplyRepeat(repeatMode);}
        bool ApplyRepeat(int mode)
        {
            int count=1;
            if(mode==1&&(!int.TryParse(repeatCountInput.text,out count)||count<1||count>9999))
            {
                panel.ShowMessage("반복 횟수는 1~9999입니다.");
                return false;
            }
            var job=Model.GetWorkshop("processing").jobs[0];
            if(!job.started&&mode==job.repeatMode&&(mode!=1||count==job.cycles))return true;
            string error;
            bool ok=Model.TrySetProcessingRepeat(job.id,mode,count,out error);
            if(!ok)panel.ShowMessage(error);
            MarkDirty();
            return ok;
        }
        public void StartJob(string job)
        {if(!ready||Busy)return;if(StationId=="processing"&&!ApplyRepeat(repeatMode))return;string error;bool ok=Model.TryStartWorkshopJob(StationId,job,out error);selected=job;Message(ok,"제작 시작. 휴무에는 진행도가 보존됩니다.",error);}
        public void CancelJob(string job)
        {if(!ready||Busy)return;string error;bool ok=Model.TryCancelWorkshopJob(StationId,job,"warehouse",out error);Message(ok,"남은 재료와 완성품을 상자/임시 저장고로 회수했습니다.",error);}
        public void MoveJob(string job,int direction)
        {if(!ready||Busy)return;string error;bool ok=Model.TryMoveWorkshopJob(StationId,job,direction,out error);Message(ok,"진행도를 유지하고 우선순위를 바꿨습니다.",error);}
        public void Instant(string job)
        {if(!ready||Busy)return;string error;bool ok=Model.TryInstantAlchemy(job,out error);Message(ok,"긴급 제작 적용. 남은 작업 시간이 "+(Model.Workshops.Data.emergencyRemainingFraction*100).ToString("0")+"%가 됩니다. 추가금은 새벽 정산입니다.",error);}
        public void CollectAll()
        {if(!ready||Busy)return;int stored,overflow;string error;bool ok=Model.TryCollectWorkshopResults(StationId,out stored,out overflow,out error);Message(ok,"완성품 수령: 상자 "+stored+"개 / 임시 저장고 "+overflow+"개",error);}
        public void SelectJob(string id){selected=id;MarkDirty();}
        public void UnlockAllForDemo(){if(!ready||Busy)return;string error;bool ok=Model.TryUnlockAllWorkshopsForDemo(out error);Message(ok,"공방3곳 해금 및 고용 완료.",error);}
        public string ItemName(ItemRecord item){var quality=panel.Catalog.FindQuality(item.itemId,item.quality);return quality==null?item.itemId:quality.name;}
        void Message(bool ok,string success,string error){panel.ShowMessage(ok?success:error);MarkDirty();}
        void Render()
        {
            var w=Model.GetWorkshop(StationId);var definition=Definition;titleLabel.text=definition.name;
            account.text="고용 중 · 일급 "+definition.dailyWage+" G · 정산 "+w.wageDue+" + 추가 "+w.extraDue+" G";
            if(processingView!=null)
            {
                processingView.gameObject.SetActive(StyledProcessing);
                legacyBackground.enabled=!StyledProcessing;
                foreach(var control in legacyPanelControls)control.SetActive(!StyledProcessing);
            }
            repeatControls.SetActive(StationId=="processing");
            repeatCountInput.interactable=repeatMode==1&&!w.jobs[0].started;
            if(StyledProcessing)
            {
                processingView.ShowRepeat(repeatMode,w.jobs[0].started);
                processingView.job.Bind(this,w.jobs[0],0,1);
                RenderProgress();
                return;
            }
            if(repeatModeButton!=null)repeatModeButton.interactable=!w.jobs[0].started;
            if(repeatLabel!=null)repeatLabel.text=repeatMode==0?"1회":repeatMode==1?"횟수 지정":"재료 소진까지";
            int count=StationId=="smith"?3:1;var used=usedRows;used.Clear();
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
            nextProgress=Time.unscaledTime+Mathf.Max(.05f,progressRefreshSeconds);var status=Model.ReadWorkshopProgress(StationId);int count=status.resultCount;
            bool canPay=status.hired&&status.unlocked&&!status.nightPaid&&panel.inventory.Clock.Phase==ShopPhase.Night;
            if(StyledProcessing)
            {
                processingView.ShowStatus(count,canPay,status.nightPaid,Model.NightWorkFee(StationId));
                var row=processingView.job;
                row.RefreshProgress(this,Model.ReadWorkshopProgress(StationId,row.JobId));
                return;
            }
            if(shownResultCount!=count){shownResultCount=count;collectLabel.text="완성품 받기 "+count;}collectButton.interactable=count>0;
            if(shownNightPaid!=status.nightPaid||shownNightFee!=Model.NightWorkFee(StationId)){shownNightPaid=status.nightPaid;shownNightFee=Model.NightWorkFee(StationId);nightLabel.text=status.nightPaid?"야간 신청 완료":"야간 +"+shownNightFee+" G";}
            nightButton.interactable=canPay;
            foreach(var row in rows)if(row.gameObject.activeSelf)row.RefreshProgress(this,Model.ReadWorkshopProgress(StationId,row.JobId));
        }
        int shownResultCount=-1;bool shownNightPaid;long shownNightFee=-1;

        void OnDestroy(){if(ready){subscribedModel.Changed-=MarkDirty;subscribedModel.WorkshopsChanged-=MarkProgress;subscribedRuntime.Loaded-=Reload;}}
    }
}
