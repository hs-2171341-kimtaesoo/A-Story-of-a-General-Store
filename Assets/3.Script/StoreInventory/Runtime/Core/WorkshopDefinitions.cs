using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class WorkshopDatabase
    {public WorkshopDefinition[] workshops;public WorkshopRecipe[] recipes;public QualityWeight[] qualityWeights;public double qualityInfluence=.8;}
    [Serializable]public sealed class WorkshopDefinition
    {public string id,name;public int dailyWage,instantFee,unlockSales,queueCapacity=1;}
    [Serializable]public sealed class WorkshopIngredient{public string itemId;public int count=1;}
    [Serializable]public sealed class WorkshopRecipe
    {public string id,name,workshopId,outputItemId;public int outputCount=1;public double minutes=30;public WorkshopIngredient[] ingredients;}
    [Serializable]public sealed class QualityWeight{public string quality;public double weight;public int rank;}
    [Serializable]public sealed class WorkshopMaterial
    {public int slot;public ItemRecord item;public WorkshopMaterial Copy(){return new WorkshopMaterial{slot=slot,item=item==null?null:item.Copy()};}}
    [Serializable]public sealed class WorkshopJob
    {
        public string id,recipeId;public bool started;public int cycles=1,completed;public double elapsed;
        public List<WorkshopMaterial> inputs=new List<WorkshopMaterial>();public List<int> usedSlots=new List<int>();public List<ItemRecord> results=new List<ItemRecord>();
        public WorkshopJob Copy(){var j=new WorkshopJob{id=id,recipeId=recipeId,started=started,cycles=cycles,completed=completed,elapsed=elapsed};foreach(var i in inputs)j.inputs.Add(i==null?null:i.Copy());j.usedSlots.AddRange(usedSlots);foreach(var i in results)j.results.Add(i==null?null:i.Copy());return j;}
    }
    [Serializable]public sealed class WorkshopState
    {
        public string id;public bool unlocked,hired;public long wageDue,extraDue;public int wageDay;
        public List<WorkshopJob> jobs=new List<WorkshopJob>();
        public WorkshopState Copy(){var s=new WorkshopState{id=id,unlocked=unlocked,hired=hired,wageDue=wageDue,extraDue=extraDue,wageDay=wageDay};foreach(var j in jobs)s.jobs.Add(j==null?null:j.Copy());return s;}
    }
    public sealed class WorkshopCatalog
    {
        public readonly WorkshopDatabase Data;
        readonly Dictionary<string,WorkshopDefinition> workshops=new Dictionary<string,WorkshopDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string,WorkshopRecipe> recipes=new Dictionary<string,WorkshopRecipe>(StringComparer.Ordinal);
        public WorkshopCatalog(WorkshopDatabase data,ItemCatalog items)
        {
            Data=data;if(data==null||data.workshops==null||data.recipes==null||data.qualityWeights==null||data.qualityWeights.Length==0||!Finite(data.qualityInfluence)||data.qualityInfluence<0||data.qualityInfluence>10)throw new ArgumentException("공방 데이터 오류");
            foreach(var w in data.workshops)
            {
                if(w==null||(w.id!="processing"&&w.id!="alchemy"&&w.id!="smith")||workshops.ContainsKey(w.id)||string.IsNullOrWhiteSpace(w.name)||w.dailyWage<0||w.instantFee<0||w.unlockSales<0||w.queueCapacity<1||w.queueCapacity>(w.id=="smith"?5:1)||(w.id=="smith"&&w.queueCapacity<3)||(w.id!="alchemy"&&w.instantFee!=0))throw new ArgumentException("공방 설정 오류");workshops.Add(w.id,w);
            }
            if(workshops.Count!=3)throw new ArgumentException("공방3곳 설정이 필요합니다.");
            var qualities=new HashSet<string>();foreach(var q in data.qualityWeights)if(q==null||string.IsNullOrWhiteSpace(q.quality)||!qualities.Add(q.quality)||!Finite(q.weight)||q.weight<=0||q.weight>1000000||q.rank<0||q.rank>5)throw new ArgumentException("품질 확률 설정 오류");
            foreach(var item in items.AllDefinitions())foreach(var quality in item.qualities)if(!qualities.Contains(quality.quality))throw new ArgumentException("품질 확률 누락: "+quality.quality);
            foreach(var r in data.recipes)
            {
                if(r==null||string.IsNullOrWhiteSpace(r.id)||recipes.ContainsKey(r.id)||!workshops.ContainsKey(r.workshopId)||items.Find(r.outputItemId)==null||r.outputCount<1||r.outputCount>5||!Finite(r.minutes)||r.minutes<=0||r.minutes>10080||r.ingredients==null||r.ingredients.Length<1||r.ingredients.Length>5)throw new ArgumentException("제작 조합 오류");
                int count=0;var ids=new HashSet<string>();foreach(var i in r.ingredients){if(i==null||items.Find(i.itemId)==null||!ids.Add(i.itemId)||i.count<1||i.count>5)throw new ArgumentException("제작 재료 오류");count+=i.count;}if(count>5)throw new ArgumentException("한 회차 재료는 총5개 이하입니다.");recipes.Add(r.id,r);
            }
        }
        // Optional demo-only extensions. The user's original item JSON is never overwritten.
        public static ItemDatabase MergeDemoItems(ItemDatabase original,ItemDatabase demo)
        {
            if(original==null||original.items==null||demo==null||demo.items==null)throw new ArgumentException("공방 시험 아이템 데이터 오류");
            var merged=new List<ItemDefinition>(original.items);merged.AddRange(demo.items);
            if(demo.qualityOverrides!=null)foreach(var patch in demo.qualityOverrides)
            {var item=merged.Find(x=>x.id==patch.id);if(item==null||patch.qualities==null)throw new ArgumentException("시험 품질 확장 대상이 없습니다.");item.qualities=patch.qualities;}
            return new ItemDatabase{items=merged.ToArray()};
        }
        public static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
        public WorkshopDefinition Find(string id){WorkshopDefinition d;return id!=null&&workshops.TryGetValue(id,out d)?d:null;}
        public WorkshopRecipe Recipe(string id){WorkshopRecipe r;return id!=null&&recipes.TryGetValue(id,out r)?r:null;}
        public QualityWeight Quality(string code){return Array.Find(Data.qualityWeights,q=>q.quality==code);}
        public double[] Probabilities(ItemDefinition output,IList<ItemRecord> inputs)
        {
            double rank=0;foreach(var i in inputs)rank+=Quality(i.quality).rank;rank=inputs.Count==0?1:rank/inputs.Count;
            var weights=new double[output.qualities.Length];double sum=0;
            for(int i=0;i<weights.Length;i++){var q=Quality(output.qualities[i].quality);weights[i]=q.weight*Math.Exp(Data.qualityInfluence*(rank-1)*(q.rank-1)/4);sum+=weights[i];}
            for(int i=0;i<weights.Length;i++)weights[i]/=sum;return weights;
        }
    }
}
