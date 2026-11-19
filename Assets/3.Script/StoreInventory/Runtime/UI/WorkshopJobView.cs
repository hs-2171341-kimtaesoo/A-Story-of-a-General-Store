using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class WorkshopJobView:MonoBehaviour
    {
        public Text title,status,time;public RectTransform fill;public WorkshopSlotView[] inputs;public WorkshopSlotView output;
        public Button start,cancel,up,down,instant,focus;
        WorkshopUI owner;string jobId;float lastFill=-1;
        public void Initialize(WorkshopUI ui)
        {
            owner=ui;start.onClick.AddListener(()=>owner.StartJob(jobId));cancel.onClick.AddListener(()=>owner.CancelJob(jobId));up.onClick.AddListener(()=>owner.MoveJob(jobId,-1));down.onClick.AddListener(()=>owner.MoveJob(jobId,1));instant.onClick.AddListener(()=>owner.Instant(jobId));focus.onClick.AddListener(()=>owner.SelectJob(jobId));
        }
        public void Bind(WorkshopUI ui,WorkshopJob job,int index,int count)
        {
            jobId=job==null?null:job.id;var d=ui.Definition;
            title.text=job==null?"작업 자리 "+(index+1)+" · 비어 있음":(index+1)+". "+ui.Model.Workshops.Recipe(job.recipeId).name;
            foreach(var slot in inputs){var m=job==null?null:job.inputs.Find(x=>x.slot==slot.slot);slot.Bind(ui,jobId,m==null?null:m.item,job!=null&&job.usedSlots.Contains(slot.slot));}
            output.Bind(ui,jobId,job==null||job.results.Count==0?null:job.results[0],false,job==null?0:job.results.Count);
            double ratio=0;time.text="";status.text=job==null?"조합을 선택하고 작업을 추가하세요.":ui.Model.WorkshopJobStatus(ui.StationId,job);
            if(job!=null)
            {
                var recipe=ui.Model.Workshops.Recipe(job.recipeId);ratio=job.completed==job.cycles?1:job.elapsed/recipe.minutes;
                time.text=(job.completed==job.cycles?"완료":(index==0?"남은 작업 ":"대기 · 남은 작업 ")+System.Math.Ceiling(recipe.minutes-job.elapsed)+"분")+" · "+job.completed+"/"+job.cycles+"회";
            }
            float value=(float)ratio;if(value!=lastFill){fill.sizeDelta=new Vector2(468*value,12);lastFill=value;}
            bool exists=job!=null;start.interactable=exists&&!job.started&&d!=null;cancel.interactable=exists;focus.interactable=exists;
            up.gameObject.SetActive(d.id=="smith");down.gameObject.SetActive(d.id=="smith");up.interactable=exists&&index>0;down.interactable=exists&&index<count-1;
            instant.gameObject.SetActive(d.id=="alchemy");instant.interactable=exists&&job.completed<job.cycles;
        }
    }
}
