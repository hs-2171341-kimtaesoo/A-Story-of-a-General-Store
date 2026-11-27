using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class WorkshopJobView:MonoBehaviour
    {
        public Text title,status,time,candidateLabel;public RectTransform fill;public WorkshopSlotView[] inputs;public WorkshopSlotView output;
        public Button start,cancel,up,down,instant,focus,candidate;
        public RectTransform progressArea;
        public Text actionLabel;
        bool UsesProcessingLayout{get{return progressArea!=null;}}
        bool listenersReady;WorkshopUI owner;public string JobId{get;private set;}Vector2 target;bool positionReady;
        public void Initialize(WorkshopUI ui)
        {
            owner=ui;
            if(listenersReady)return;
            listenersReady=true;
            start.onClick.AddListener(()=>owner.StartJob(JobId));
            cancel.onClick.AddListener(()=>owner.CancelJob(JobId));
            candidate.onClick.AddListener(()=>owner.CycleCandidate(JobId));
            if(up!=null)up.onClick.AddListener(()=>owner.MoveJob(JobId,-1));
            if(down!=null)down.onClick.AddListener(()=>owner.MoveJob(JobId,1));
            if(instant!=null)instant.onClick.AddListener(()=>owner.Instant(JobId));
            if(focus!=null)focus.onClick.AddListener(()=>owner.SelectJob(JobId));
        }
        public void SetOrder(int index,bool snap)
        {target=new Vector2(0,-index*144);if(snap||!positionReady){((RectTransform)transform).anchoredPosition=target;positionReady=true;}}
        void Update()
        {
            if(!positionReady)return;
            var rect=(RectTransform)transform;if((rect.anchoredPosition-target).sqrMagnitude<.01f){if(rect.anchoredPosition!=target)rect.anchoredPosition=target;return;}
            rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,target,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
        }
        public void Bind(WorkshopUI ui,WorkshopJob job,int index,int count)
        {
            JobId=job.id;var recipe=ui.Model.Workshops.Recipe(job.recipeId);
            foreach(var slot in inputs){var material=job.inputs.Find(m=>m.slot==slot.slot);slot.Bind(ui,job.id,material==null?null:material.item,job.started);}
            var matches=ui.Model.MatchWorkshopRecipes(ui.StationId,job.id);bool valid=recipe!=null&&(job.started||matches.Exists(r=>r.id==recipe.id));
            if(title!=null)title.text=(index+1)+". "+(valid?recipe.name:"재료를 넣으세요");
            var preview=valid?new ItemRecord{itemId=recipe.outputItemId,quality=ui.panel.Catalog.Find(recipe.outputItemId).qualities[0].quality}:null;
            if(UsesProcessingLayout&&preview==null&&job.results.Count>0)preview=job.results[job.results.Count-1];
            output.Bind(ui,job.id,preview,false);
            bool hasChoices=matches.Count>1&&!job.started;
            candidate.gameObject.SetActive(hasChoices);
            if(hasChoices){
                int current=Mathf.Max(0,matches.FindIndex(r=>r.id==job.recipeId))+1;
                candidateLabel.text=(UsesProcessingLayout?"다른 결과 ":"결과 후보 ")+current+"/"+matches.Count+" ›";
            }
            start.interactable=ui.Model.CanStartWorkshopJob(ui.StationId,job.id);
            cancel.interactable=job.inputs.Count>0||job.results.Count>0||job.started;
            if(UsesProcessingLayout)
            {
                start.gameObject.SetActive(!job.started);
                cancel.gameObject.SetActive(job.started);
                actionLabel.text=job.repeatMode==0?"제작":"반복 제작";
                actionLabel.color=start.interactable?new Color(.20f,.12f,.06f):new Color(1f,.86f,.54f);
            }
            else
            {
                up.gameObject.SetActive(ui.StationId=="smith");down.gameObject.SetActive(ui.StationId=="smith");
                up.interactable=index>0;down.interactable=index<count-1;
                instant.gameObject.SetActive(ui.StationId=="alchemy");
            }
            RefreshProgress(ui,job);
        }
        long shownSeconds=long.MinValue;int shownCompleted=-1,shownRepeat=-1;bool shownStarted,shownTime;
        public void RefreshProgress(WorkshopUI ui,WorkshopJob job){RefreshProgress(ui,ui.Model.ReadWorkshopProgress(ui.StationId,job.id));}
        public void RefreshProgress(WorkshopUI ui,WorkshopProgress progress)
        {
            float ratio=Mathf.Clamp01(progress.ratio);
            var size=UsesProcessingLayout
                ?new Vector2(progress.started?progressArea.rect.width*ratio:0,progressArea.rect.height)
                :new Vector2(200*ratio,12);
            if(fill.sizeDelta!=size)fill.sizeDelta=size;
            status.text=UsesProcessingLayout&&progress.status!="야간 휴무"&&progress.status!="새벽 휴무"&&progress.status!="재료 소진 · 반복 종료"
                ?"":progress.status??"";
            long seconds=(long)System.Math.Ceiling(progress.remaining);
            if(shownSeconds!=seconds||shownCompleted!=progress.completed||shownRepeat!=progress.repeatMode||shownStarted!=progress.started||shownTime!=progress.showTime)
            {
                shownSeconds=seconds;shownCompleted=progress.completed;shownRepeat=progress.repeatMode;shownStarted=progress.started;shownTime=progress.showTime;
                time.text=!progress.showTime?"":FormatSeconds(seconds)+(progress.started?" 남음":" / 1회")+(progress.repeatMode!=0?" · "+progress.completed+"회 완료":"");
            }
            if(instant!=null)instant.interactable=progress.canRush;
        }
        public static string FormatSeconds(double seconds){long total=(long)System.Math.Ceiling(seconds);return total/60+"분 "+(total%60).ToString("00")+"초";}
    }
}
