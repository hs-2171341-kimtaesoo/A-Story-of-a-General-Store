using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShopItems
{
    public class ItemManager : MonoBehaviour
    {
        [SerializeField] private string resourcePath = "items";

        // key: 아이템 코드 / value: 아이템 정의
        private Dictionary<string, ItemDefinition> itemLookup = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);

        // 아이템별 품질 검색표입니다. 코드와 품질을 각각 키로 사용합니다.
        private Dictionary<string, Dictionary<string, ItemQualityData>> qualityLookup = new Dictionary<string, Dictionary<string, ItemQualityData>>(StringComparer.Ordinal);

        public bool IsLoaded { get; private set; }
        public int ItemCount => itemLookup.Count;

        private void Awake()
        {
            LoadItems();
        }

        public bool LoadItems()
        {
            // 초기화
            IsLoaded = false;
            itemLookup.Clear();
            qualityLookup.Clear();

            TextAsset file = Resources.Load<TextAsset>(resourcePath);
            if (file == null)
            {
                Debug.LogError("[ItemManager] JSON 파일을 찾지 못했습니다: Resources/" + resourcePath + ".json", this);
                return false;
            }

            try
            {
                // 파일의 글자를 ItemDatabase 구조의 C# 객체로 변환
                ItemDatabase database = JsonUtility.FromJson<ItemDatabase>(file.text);
                if (database == null || database.items == null)
                {
                    throw new InvalidOperationException("JSON의 items 배열이 없습니다.");
                }

                // 임시 검색표에 검사하며 넣고, 모두 정상일 때만 실제 검색표로 교체합니다.
                var newItems = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
                var newQualities = new Dictionary<string, Dictionary<string, ItemQualityData>>(StringComparer.Ordinal);

                foreach (ItemDefinition item in database.items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.id))
                    {
                        throw new InvalidOperationException("아이템 코드가 비어 있습니다.");
                    }
                    if (string.IsNullOrWhiteSpace(item.type))
                    {
                        throw new InvalidOperationException("아이템 종류가 비어 있습니다: " + item.id);
                    }
                    if (newItems.ContainsKey(item.id))
                    {
                        throw new InvalidOperationException("중복 아이템 코드: " + item.id);
                    }
                    if (item.basePrice < 0)
                    {
                        throw new InvalidOperationException("기본 가격은 음수일 수 없습니다: " + item.id);
                    }
                    if (item.qualities == null || item.qualities.Length == 0)
                    {
                        throw new InvalidOperationException("품질 목록이 없습니다: " + item.id);
                    }

                    var qualities = new Dictionary<string, ItemQualityData>(StringComparer.Ordinal);
                    foreach (ItemQualityData quality in item.qualities)
                    {
                        if (quality == null || string.IsNullOrWhiteSpace(quality.quality) || string.IsNullOrWhiteSpace(quality.name) || string.IsNullOrWhiteSpace(quality.iconKey))
                        {
                            throw new InvalidOperationException("품질 코드/이름/이미지 키가 비어 있습니다: " + item.id);
                        }
                        if (qualities.ContainsKey(quality.quality))
                        {
                            throw new InvalidOperationException("중복 품질: " + item.id + "/" + quality.quality);
                        }
                        if (float.IsNaN(quality.priceMultiplier) || float.IsInfinity(quality.priceMultiplier) || quality.priceMultiplier <= 0)
                        {
                            throw new InvalidOperationException("가격 배율은 양수여야 합니다: " + item.id);
                        }

                        // 최종 가격을 int로 표현할 수 있는지도 검사합니다.
                        double price = Math.Round((double)item.basePrice * quality.priceMultiplier, MidpointRounding.AwayFromZero);
                        if (price > int.MaxValue)
                        {
                            throw new InvalidOperationException("가격이 너무 큽니다: " + item.id);
                        }
                        // 이전 JSON에 설명이 없더라도 로드할 수 있게 빈 문자열로 통일합니다.
                        // 설명은 선택 항목이므로 비어 있어도 오류로 처리하지 않습니다.
                        quality.explanation = quality.explanation ?? string.Empty;
                        qualities.Add(quality.quality, quality);
                    }
                    newItems.Add(item.id, item);
                    newQualities.Add(item.id, qualities);
                }

                itemLookup = newItems;
                qualityLookup = newQualities;
                IsLoaded = true;
                Debug.Log("[ItemManager] 아이템 " + ItemCount + "종 로드 완료", this);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[ItemManager] 로드 실패: " + e.Message, this);
                return false;
            }
        }

        // 종류 정보 찾기: manager.GetItem("C001")
        // 못 찾으면 null입니다. 호출하는 쪽에서 null을 확인하세요.
        public ItemDefinition GetItem(string itemId)
        {
            if (!IsLoaded || string.IsNullOrEmpty(itemId)) return null;
            ItemDefinition item;
            return itemLookup.TryGetValue(itemId, out item) ? item : null;
        }

        // 품질 정보 찾기: manager.GetQuality("C001", "superior")
        // 문자열은 대소문자를 구분합니다. "None"과 "none"은 서로 다릅니다.
        public ItemQualityData GetQuality(string itemId, string qualityCode)
        {
            if (!IsLoaded || string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(qualityCode)) return null;
            Dictionary<string, ItemQualityData> qualities;
            if (!qualityLookup.TryGetValue(itemId, out qualities)) return null;
            ItemQualityData quality;
            return qualities.TryGetValue(qualityCode, out quality) ? quality : null;
        }

        // 아이템 설명을 간편하게 조회합니다.
        // 예: GetExplanation("C001", "superior")
        // 아직 로드하지 않았거나 코드/품질이 없거나 설명이 비어 있으면 ""를 반환합니다.
        // 유효한 아이템인지까지 구분하려면 GetQuality의 null 여부를 먼저 확인하세요.
        public string GetExplanation(string itemId, string qualityCode)
        {
            ItemQualityData quality = GetQuality(itemId, qualityCode);
            return quality == null ? string.Empty : (quality.explanation ?? string.Empty);
        }

        // 품질별 기준 가격 = basePrice × priceMultiplier
        // 소수점은 반올림하고, 정확히 .5인 값은 올립니다(양수 금액).
        // 성공 여부를 bool로 반환하므로 '없는 아이템'과 '가격이 0인 아이템'을 구분할 수 있습니다.
        public bool TryGetQualityPrice(string itemId, string qualityCode, out int price)
        {
            price = 0;
            ItemDefinition item = GetItem(itemId);
            ItemQualityData quality = GetQuality(itemId, qualityCode);
            if (item == null || quality == null) return false;

            price = (int)Math.Round((double)item.basePrice * quality.priceMultiplier,
                MidpointRounding.AwayFromZero);
            return true;
        }

        // 반환된 객체는 원본 정의를 참조합니다. 조회용으로 사용하고 값을 변경하지 마세요.
        // 플레이어 판매가, 창고 위치, 보유 수량은 별도의 런타임/저장 데이터에 두어야 합니다.
    }
}
