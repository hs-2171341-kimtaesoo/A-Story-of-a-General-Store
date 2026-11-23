using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    // JSON 전용 데이터입니다. 필드 이름과 대소문자는 JSON과 같아야 합니다.
    [Serializable] public sealed class ItemDatabase { public ItemDefinition[] items; public ItemDefinition[] qualityOverrides; }
    [Serializable] public sealed class ItemDefinition
    {
        public string id;
        public string type;
        public string iconKey; // 공통 아이콘. 비어 있으면 common/none/첫 품질 키를 사용합니다.
        public string displayName,description;
        public bool tradeDisabled; // 생략된 구형 JSON은 기존처럼 거래 가능.
        public bool roundQualityPriceUp; // 엑셀의 품질 가격 올림 규칙. 구형 데이터는 반올림 유지.
        public ItemRecipeDefinition sourceRecipe; // 원본 조합 보존. 공방/시간을 확정한 조합만 workshops에 등록합니다.
        public int basePrice;
        // 회전 전 점유 크기입니다. 물건은 언제나 낱개이며 수량/중첩 필드는 없습니다.
        public int width;
        public int height;
        public bool canRotate;
        public string[] tags;
        public QualityDefinition[] qualities;
    }
    [Serializable] public sealed class ItemRecipeDefinition
    {
        public int outputCount;
        public WorkshopIngredient[] ingredients;
    }
    [Serializable] public sealed class QualityDefinition
    {
        public string quality;
        public string name;
        public float priceMultiplier;
        public string iconKey;
        public string explanation;
    }

    // 화면이나 Unity API를 모르는 아이템 도감입니다. 단위 테스트에서도 그대로 사용합니다.
    public sealed class ItemCatalog
    {
        readonly Dictionary<string, ItemDefinition> definitions = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ItemFootprint> shapes = new Dictionary<string, ItemFootprint>(StringComparer.Ordinal);
        static readonly string[] SharedIconPreference={"common","none"};
        public IEnumerable<ItemDefinition> AllDefinitions(){return definitions.Values;}
        public int Count { get { return definitions.Count; } }
        public string IconKey(string itemId)
        {
            ItemDefinition item;if(!definitions.TryGetValue(itemId,out item))return null;
            if(!string.IsNullOrWhiteSpace(item.iconKey))return item.iconKey;
            foreach(string standard in SharedIconPreference)foreach(var quality in item.qualities)
                if(quality.quality==standard&&!string.IsNullOrWhiteSpace(quality.iconKey))return quality.iconKey;
            foreach(var quality in item.qualities)if(!string.IsNullOrWhiteSpace(quality.iconKey))return quality.iconKey;
            return item.id;
        }

        public ItemCatalog(ItemDatabase database)
        {
            if (database == null || database.items == null) throw new ArgumentException("items 배열이 없습니다.");
            foreach (ItemDefinition definition in database.items)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.id) || definitions.ContainsKey(definition.id))
                    throw new ArgumentException("아이템 ID가 비어 있거나 중복되었습니다.");
                if (string.IsNullOrWhiteSpace(definition.type) || definition.basePrice < 0
                    || definition.width < 1 || definition.width > 64 || definition.height < 1 || definition.height > 64
                    || definition.qualities == null || definition.qualities.Length == 0)
                    throw new ArgumentException("아이템 종류/가격/크기/품질 오류: " + definition.id);
                var codes = new HashSet<string>(StringComparer.Ordinal);
                foreach (QualityDefinition quality in definition.qualities)
                {
                    if (quality == null || string.IsNullOrWhiteSpace(quality.quality) || !codes.Add(quality.quality)
                        || string.IsNullOrWhiteSpace(quality.name)
                        || float.IsNaN(quality.priceMultiplier) || float.IsInfinity(quality.priceMultiplier)
                        || quality.priceMultiplier <= 0)
                        throw new ArgumentException("품질 데이터 오류: " + definition.id);
                    double price = CalculatePrice(definition,quality);
                    if (price > int.MaxValue) throw new ArgumentException("기준 가격이 너무 큽니다: " + definition.id);
                    if(string.IsNullOrEmpty(quality.explanation))quality.explanation=definition.description??"";
                }
                // 크기는 직사각형 하나로 정의하며 짝수/홀수 회전 좌표만 필요할 때 캐시합니다.
                shapes.Add(definition.id, new ItemFootprint(definition.width, definition.height));
                definitions.Add(definition.id, definition);
            }
            foreach(var definition in definitions.Values)
            {
                var recipe=definition.sourceRecipe;if(recipe==null)continue;
                // Unity inline deserialization may materialize an omitted optional class.
                // Only a completely empty recipe means "not craftable"; partial recipes stay invalid.
                if(recipe.outputCount==0&&(recipe.ingredients==null||recipe.ingredients.Length==0))
                {definition.sourceRecipe=null;continue;}
                if(recipe.outputCount<1||recipe.outputCount>5||recipe.ingredients==null||recipe.ingredients.Length<1||recipe.ingredients.Length>5)
                    throw new ArgumentException("원본 조합 수량 오류: "+definition.id);
                int count=0;var seen=new HashSet<string>(StringComparer.Ordinal);
                foreach(var ingredient in recipe.ingredients)
                {
                    if(ingredient==null||string.IsNullOrEmpty(ingredient.itemId)||!definitions.ContainsKey(ingredient.itemId)||!seen.Add(ingredient.itemId)||ingredient.count<1||ingredient.count>5)
                        throw new ArgumentException("원본 조합 재료 오류: "+definition.id);
                    count+=ingredient.count;
                }
                if(count>5)throw new ArgumentException("한 회차 재료는 총5개 이하입니다: "+definition.id);
            }
        }
        // 정의는 읽기 전용으로 사용하세요. 실제 물건의 변경은 InventoryModel의 명령으로 처리합니다.
        public ItemDefinition Find(string id)
        { ItemDefinition value; return id != null && definitions.TryGetValue(id, out value) ? value : null; }
        public QualityDefinition FindQuality(string id, string qualityCode)
        {
            ItemDefinition definition = Find(id);
            return definition == null ? null : Array.Find(definition.qualities, q => q.quality == qualityCode);
        }
        public ItemFootprint Shape(string id)
        {
            ItemFootprint shape;
            if (id == null || !shapes.TryGetValue(id, out shape)) throw new ArgumentException("없는 아이템 모양입니다.");
            return shape;
        }
        public int ReferencePrice(string id, string qualityCode)
        {
            ItemDefinition definition = Find(id); QualityDefinition quality = FindQuality(id, qualityCode);
            if (quality == null) throw new ArgumentException("없는 아이템/품질입니다.");
            // 정적 기준 가격일 뿐 현재 시세/플레이어 판매가/실제 매입가가 아닙니다.
            return (int)CalculatePrice(definition,quality);
        }
        public bool CanTrade(string id){var item=Find(id);return item!=null&&!item.tradeDisabled;}
        static double CalculatePrice(ItemDefinition item,QualityDefinition quality)
        {
            // Decimal conversion prevents 1.4f's binary error from adding a spurious extra gold.
            if(item.roundQualityPriceUp)return (double)Math.Ceiling((decimal)item.basePrice*(decimal)quality.priceMultiplier);
            return Math.Round((double)item.basePrice*quality.priceMultiplier,MidpointRounding.AwayFromZero);
        }
    }
}
