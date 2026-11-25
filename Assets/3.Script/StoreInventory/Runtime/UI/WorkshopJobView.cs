using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class WorkshopJobView:MonoBehaviour
    {
        public Text title,status,time,candidateLabel;public RectTransform fill;public WorkshopSlotView[] inputs;public WorkshopSlotView output;
        public Button start,cancel,up,down,instant,focus,candidate;
        bool listenersReady;WorkshopUI owner;public string JobId{get;private set;}Vector2 target;bool positionReady;
        public void Initialize(WorkshopUI ui)
        {
            owner=ui;if(listenersReady)return;listenersReady=true;start.onClick.AddListener(()=>owner.StartJob(JobId));cancel.onClick.AddListener(()=>owner.CancelJob(JobId));up.onClick.AddListener(()=>owner.MoveJob(JobId,-1));down.onClick.AddListener(()=>owner.MoveJob(JobId,1));instant.onClick.AddListener(()=>owner.Instant(JobId));focus.onClick.AddListener(()=>owner.SelectJob(JobId));candidate.onClick.AddListener(()=>owner.CycleCandidate(JobId));
        }
        public void SetOrder(int index,bool snap)
        {target=new Vector2(0,-index*144);if(snap||!positionReady){((RectTransform)transform).anchoredPosition=target;positionReady=true;}}
        void Update()
        {
            var rect=(RectTransform)transform;if((rect.anchoredPosition-target).sqrMagnitude<.01f){if(rect.anchoredPosition!=target)rect.anchoredPosition=target;return;}
            rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,target,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
        }
        public void Bind(WorkshopUI ui,WorkshopJob job,int index,int count)
        {
            JobId=job.id;var recipe=ui.Model.Workshops.Recipe(job.recipeId);title.text=(index+1)+". "+(recipe==null?"재료를 넣으세요":recipe.name);
            foreach(var slot in inputs){var material=job.inputs.Find(m=>m.slot==slot.slot);slot.Bind(ui,job.id,material==null?null:material.item,job.started);}
            var matches=ui.Model.MatchWorkshopRecipes(ui.StationId,job.id);bool valid=recipe!=null&&(job.started||matches.Exists(r=>r.id==recipe.id));
            if(!valid)title.text=(index+1)+". 재료를 넣으세요";
            output.Bind(ui,job.id,valid?new ItemRecord{itemId=recipe.outputItemId,quality=ui.panel.Catalog.Find(recipe.outputItemId).qualities[0].quality}:null,false);
            candidate.gameObject.SetActive(matches.Count>1&&!job.started);candidateLabel.text=matches.Count>1?"결과 후보 "+(matches.FindIndex(r=>r.id==job.recipeId)+1)+"/"+matches.Count:"";
            start.interactable=ui.Model.CanStartWorkshopJob(ui.StationId,job.id);cancel.interactable=job.inputs.Count>0||job.results.Count>0||job.started;
            up.gameObject.SetActive(ui.StationId=="smith");down.gameObject.SetActive(ui.StationId=="smith");up.interactable=index>0;down.interactable=index<count-1;
            instant.gameObject.SetActive(ui.StationId=="alchemy");RefreshProgress(ui,job);
        }
        long shownSeconds=long.MinValue;int shownCompleted=-1,shownRepeat=-1;bool shownStarted,shownTime;
        public void RefreshProgress(WorkshopUI ui,WorkshopJob job){RefreshProgress(ui,ui.Model.ReadWorkshopProgress(ui.StationId,job.id));}
        public void RefreshProgress(WorkshopUI ui,WorkshopProgress progress)
        {
            var size=new Vector2(200*progress.ratio,12);if(fill.sizeDelta!=size)fill.sizeDelta=size;status.text=progress.status??"";
            long seconds=(long)System.Math.Ceiling(progress.remaining);
            if(shownSeconds!=seconds||shownCompleted!=progress.completed||shownRepeat!=progress.repeatMode||shownStarted!=progress.started||shownTime!=progress.showTime)
            {
                shownSeconds=seconds;shownCompleted=progress.completed;shownRepeat=progress.repeatMode;shownStarted=progress.started;shownTime=progress.showTime;
                time.text=!progress.showTime?"":FormatSeconds(seconds)+(progress.started?" 남음":" / 1회")+(progress.repeatMode!=0?" · "+progress.completed+"회 완료":"");
            }
            instant.interactable=progress.canRush;
        }
        public static string FormatSeconds(double seconds){long total=(long)System.Math.Ceiling(seconds);return total/60+"분 "+(total%60).ToString("00")+"초";}
    }
}
