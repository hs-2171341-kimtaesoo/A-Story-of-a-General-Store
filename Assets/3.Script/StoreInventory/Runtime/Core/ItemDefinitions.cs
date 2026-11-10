using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    // JSON 전용 데이터입니다. 필드 이름과 대소문자는 JSON과 같아야 합니다.
    [Serializable] public sealed class ItemDatabase { public ItemDefinition[] items; }
    [Serializable] public sealed class ItemDefinition
    {
        public string id;
        public string type;
        public int basePrice;
        // 회전 전 점유 크기입니다. 물건은 언제나 낱개이며 수량/중첩 필드는 없습니다.
        public int width;
        public int height;
        public bool canRotate;
        // 비어 있으면 width×height 직사각형입니다. 있으면 이 좌표만 실제로 점유합니다.
        public GridCell[] occupiedCells;
        public string[] tags;
        public QualityDefinition[] qualities;
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
        public int Count { get { return definitions.Count; } }
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
                        || string.IsNullOrWhiteSpace(quality.name) || string.IsNullOrWhiteSpace(quality.iconKey)
                        || float.IsNaN(quality.priceMultiplier) || float.IsInfinity(quality.priceMultiplier)
                        || quality.priceMultiplier <= 0)
                        throw new ArgumentException("품질 데이터 오류: " + definition.id);
                    double price = Math.Round((double)definition.basePrice * quality.priceMultiplier, MidpointRounding.AwayFromZero);
                    if (price > int.MaxValue) throw new ArgumentException("기준 가격이 너무 큽니다: " + definition.id);
                    quality.explanation = quality.explanation ?? "";
                }
                // 생성 단계에서 중복/범위/외곽 크기를 검사하고 네 방향 좌표를 미리 계산합니다.
                shapes.Add(definition.id, new ItemFootprint(definition.width, definition.height, definition.occupiedCells));
                definitions.Add(definition.id, definition);
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
            return (int)Math.Round((double)definition.basePrice * quality.priceMultiplier, MidpointRounding.AwayFromZero);
        }
    }
}
