using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class WorkshopDatabase
    {public WorkshopDefinition[] workshops;public WorkshopRecipe[] recipes;public QualityWeight[] qualityWeights;public double qualityInfluence=.8;
        public bool usePhaseSchedule,dayWork,nightWork,dawnWork,paidNightWorkEnabled;public double nightWageMultiplier;public double emergencyRemainingFraction=.25;public bool repeatUsesTemporary=true;}
    [Serializable]public sealed class WorkshopDefinition
    {public string id,name;public int dailyWage,instantFee,unlockSales,queueCapacity=1;}
    [Serializable]public sealed class WorkshopIngredient{public string itemId;public int count=1;}
    [Serializable]public sealed class WorkshopRecipe
    {public string id,name,workshopId,outputItemId;public int outputCount=1;public double minutes=30,workSeconds;public WorkshopIngredient[] ingredients;
        public double Duration{get{return workSeconds>0?workSeconds:minutes;}}}
    [Serializable]public sealed class QualityWeight{public string quality;public double weight;public int rank;}
    [Serializable]public sealed class WorkshopMaterial
    {public int slot;public ItemRecord item;public WorkshopMaterial Copy(){return new WorkshopMaterial{slot=slot,item=item==null?null:item.Copy()};}}
    [Serializable]public sealed class WorkshopJob
    {
        public string id,recipeId;public bool started,rushed,exhausted;public int repeatMode,cycles=1,completed;public double elapsed;
        public List<WorkshopMaterial> inputs=new List<WorkshopMaterial>();public List<int> usedSlots=new List<int>();public List<ItemRecord> results=new List<ItemRecord>();
        public WorkshopJob Copy(){var j=new WorkshopJob{id=id,recipeId=recipeId,started=started,rushed=rushed,exhausted=exhausted,repeatMode=repeatMode,cycles=cycles,completed=completed,elapsed=elapsed};j.inputs.Capacity=inputs.Count;j.results.Capacity=results.Count;j.usedSlots.Capacity=usedSlots.Count;foreach(var i in inputs)j.inputs.Add(i==null?null:i.Copy());j.usedSlots.AddRange(usedSlots);foreach(var i in results)j.results.Add(i==null?null:i.Copy());return j;}
    }
    [Serializable]public sealed class WorkshopState
    {
        public string id;public bool unlocked,hired;public long wageDue,extraDue;public int wageDay,nightWorkDay;
        public List<WorkshopJob> jobs=new List<WorkshopJob>();
        public WorkshopState Copy(){var s=new WorkshopState{id=id,unlocked=unlocked,hired=hired,wageDue=wageDue,extraDue=extraDue,wageDay=wageDay,nightWorkDay=nightWorkDay};s.jobs.Capacity=jobs.Count;foreach(var j in jobs)s.jobs.Add(j==null?null:j.Copy());return s;}
    }
    public struct WorkshopProgress
    {public bool exists,hired,unlocked,nightPaid,started,hasRecipe,showTime,canRush;public int resultCount,completed,repeatMode;public float ratio;public double remaining;public string status;}
    public sealed class WorkshopCatalog
    {
        public readonly WorkshopDatabase Data;
        readonly Dictionary<string,WorkshopDefinition> workshops=new Dictionary<string,WorkshopDefinition>(StringComparer.Ordinal);
        readonly Dictionary<Combination,List<WorkshopRecipe>> combinations=new Dictionary<Combination,List<WorkshopRecipe>>();
        // At most five physical inputs: sort references inside a value type, without strings/arrays/dictionaries per query.
        struct Combination:IEquatable<Combination>
        {
            public string station;string a,b,c,d,e;int count;
            string Get(int n){switch(n){case 0:return a;case 1:return b;case 2:return c;case 3:return d;default:return e;}}
            void Set(int n,string value){switch(n){case 0:a=value;break;case 1:b=value;break;case 2:c=value;break;case 3:d=value;break;default:e=value;break;}}
            public void Add(string id){int at=count++;while(at>0&&string.CompareOrdinal(Get(at-1),id)>0){Set(at,Get(at-1));at--;}Set(at,id);}
            public bool Equals(Combination x){return station==x.station&&count==x.count&&a==x.a&&b==x.b&&c==x.c&&d==x.d&&e==x.e;}
            public override bool Equals(object x){return x is Combination&&Equals((Combination)x);}
            public override int GetHashCode(){unchecked{int h=station==null?0:station.GetHashCode();for(int n=0;n<count;n++)h=h*31+(Get(n)==null?0:Get(n).GetHashCode());return h;}}
        }
        readonly Dictionary<string,WorkshopRecipe> recipes=new Dictionary<string,WorkshopRecipe>(StringComparer.Ordinal);
        public WorkshopCatalog(WorkshopDatabase data,ItemCatalog items)
        {
            Data=data;if(data==null||data.workshops==null||data.recipes==null||data.qualityWeights==null||data.qualityWeights.Length==0||!Finite(data.qualityInfluence)||data.qualityInfluence<0||data.qualityInfluence>10)throw new ArgumentException("공방 데이터 오류");
            if(!Finite(data.nightWageMultiplier)||data.nightWageMultiplier<0||data.nightWageMultiplier>10)throw new ArgumentException("야간 수당 배율 오류");
            if(!Finite(data.emergencyRemainingFraction)||data.emergencyRemainingFraction<=0||data.emergencyRemainingFraction>1)throw new ArgumentException("긴급 제작 배율은0초과1이하입니다.");
            foreach(var w in data.workshops)
            {
                if(w==null||(w.id!="processing"&&w.id!="alchemy"&&w.id!="smith")||workshops.ContainsKey(w.id)||string.IsNullOrWhiteSpace(w.name)||w.dailyWage<0||w.instantFee<0||w.unlockSales<0||w.queueCapacity<1||w.queueCapacity>(w.id=="smith"?5:1)||(w.id=="smith"&&w.queueCapacity<3)||(w.id!="alchemy"&&w.instantFee!=0))throw new ArgumentException("공방 설정 오류");workshops.Add(w.id,w);
            }
            if(workshops.Count!=3)throw new ArgumentException("공방3곳 설정이 필요합니다.");
            var qualities=new HashSet<string>();foreach(var q in data.qualityWeights)if(q==null||string.IsNullOrWhiteSpace(q.quality)||!qualities.Add(q.quality)||!Finite(q.weight)||q.weight<=0||q.weight>1000000||q.rank<0||q.rank>5)throw new ArgumentException("품질 확률 설정 오류");
            foreach(var item in items.AllDefinitions())foreach(var quality in item.qualities)if(!qualities.Contains(quality.quality))throw new ArgumentException("품질 확률 누락: "+quality.quality);
            foreach(var r in data.recipes)
            {
                if(r==null||string.IsNullOrWhiteSpace(r.id)||recipes.ContainsKey(r.id)||!workshops.ContainsKey(r.workshopId)||items.Find(r.outputItemId)==null||r.outputCount!=1||!Finite(r.workSeconds)||r.workSeconds<0||r.workSeconds>604800||!Finite(r.Duration)||r.Duration<=0||(r.workSeconds==0&&r.minutes>10080)||r.ingredients==null||r.ingredients.Length<1||r.ingredients.Length>5)throw new ArgumentException("제작 조합 오류");
                int count=0;var ids=new HashSet<string>();foreach(var i in r.ingredients){if(i==null||items.Find(i.itemId)==null||!ids.Add(i.itemId)||i.count<1||i.count>5)throw new ArgumentException("제작 재료 오류");count+=i.count;}if(count>5)throw new ArgumentException("한 회차 재료는 총5개 이하입니다.");recipes.Add(r.id,r);var key=CombinationKey(r.workshopId,r.ingredients);List<WorkshopRecipe> values;if(!combinations.TryGetValue(key,out values)){values=new List<WorkshopRecipe>();combinations.Add(key,values);}values.Add(r);
            }
        }
        static Combination CombinationKey(string station,WorkshopIngredient[] ingredients)
        {var key=new Combination{station=station};foreach(var ingredient in ingredients)for(int n=0;n<ingredient.count;n++)key.Add(ingredient.itemId);return key;}
        bool TryMatch(string station,IList<WorkshopMaterial> materials,out List<WorkshopRecipe> matches)
        {
            matches=null;if(materials==null||materials.Count>5)return false;var key=new Combination{station=station};
            for(int n=0;n<materials.Count;n++){var m=materials[n];if(m==null||m.item==null)return false;key.Add(m.item.itemId);}
            return combinations.TryGetValue(key,out matches);
        }
        public List<WorkshopRecipe> Match(string station,IList<WorkshopMaterial> materials)
        {List<WorkshopRecipe> matches;return TryMatch(station,materials,out matches)?new List<WorkshopRecipe>(matches):new List<WorkshopRecipe>();}
        public bool IsMatch(string station,IList<WorkshopMaterial> materials,string recipeId)
        {List<WorkshopRecipe> matches;if(!TryMatch(station,materials,out matches))return false;for(int n=0;n<matches.Count;n++)if(matches[n].id==recipeId)return true;return false;}
        public bool HasMatch(string station,IList<WorkshopMaterial> materials)
        {List<WorkshopRecipe> matches;return TryMatch(station,materials,out matches)&&matches.Count>0;}
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
