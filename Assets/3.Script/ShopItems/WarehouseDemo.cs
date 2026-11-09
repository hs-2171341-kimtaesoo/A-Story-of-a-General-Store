using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShopItems
{
    // 빈 GameObject에 ItemManager와 이 컴포넌트를 붙이면 실행되는 창고 데모입니다.
    // 사진의 '왼쪽 목록 + 오른쪽 상세' 흐름을 IMGUI로 구현합니다. 최종 아트는 포함하지 않습니다.
    [RequireComponent(typeof(ItemManager))]
    public class WarehouseDemo : MonoBehaviour
    {
        public Font koreanFont;
        [Range(5, 200)] public int capacity = 40;
        public bool addSampleStock = true;
        public WarehouseInventory Inventory { get; private set; }
        ItemManager items;
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        string filter = "", notice = "같은 코드와 품질은 같은 칸에 합쳐집니다.";
        string priceText = "0";
        int selected = -1;
        Vector2 scroll;
        string SavePath { get { return Path.Combine(Application.persistentDataPath, "warehouse-v1.json"); } }

        void Start()
        {
            TryGetComponent(out items);
            if (!items.IsLoaded) { enabled = false; return; }
            Inventory = new WarehouseInventory(items, capacity);
            if (addSampleStock)
            {
                Add("C001", "common", 10, 80);
                Add("C001", "common", 2, 100); // 위 칸에 합쳐져 12개, 평균 매입가 약 83.33
                Add("C001", "superior", 3, 110); // 품질이 다르므로 별도 칸
                Add("E001", "None", 5, 0);
            }
        }
        void Add(string id, string quality, int count, int cost)
        {
            string error;
            if (!Inventory.TryAdd(id, quality, count, cost, out error)) notice = error;
        }
        Sprite LoadIcon(string key)
        {
            Sprite sprite;
            if (!icons.TryGetValue(key, out sprite))
            {
                sprite = Resources.Load<Sprite>("ItemIcons/" + key);
                icons.Add(key, sprite); // 없는 이미지도 기록해 매 프레임 재검색하지 않습니다.
                if (sprite == null) Debug.LogWarning("아이콘 없음: Resources/ItemIcons/" + key, this);
            }
            return sprite;
        }
        void Select(int index)
        {
            selected = index;
            WarehouseStack stack = Inventory.GetSlot(index);
            priceText = stack == null ? "0" : stack.sellingPrice.ToString();
        }
        void ChangeFilter(string type) { filter = type; Select(-1); }

        void OnGUI()
        {
            if (Inventory == null) return;
            if (koreanFont != null) GUI.skin.font = koreanFont;
            GUI.skin.label.wordWrap = true;
            // 작은 화면에서는 스크롤로 접근합니다. 최종 게임은 Canvas 레이아웃으로 교체하세요.
            GUILayout.BeginArea(new Rect(12, 12, Screen.width - 24, Screen.height - 24), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("재고 관리 화면 — 사용 중 " + Inventory.UsedSlots + " / " + Inventory.Capacity + "칸");
            GUILayout.Label(notice);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("전체")) ChangeFilter("");
            if (GUILayout.Button("포션")) ChangeFilter("potion");
            if (GUILayout.Button("음식")) ChangeFilter("food");
            if (GUILayout.Button("저장")) Save();
            if (GUILayout.Button("불러오기")) Load();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(530));
            List<int> slots = Inventory.GetVisibleSlots(filter);
            for (int start = 0; start < slots.Count; start += 5)
            {
                GUILayout.BeginHorizontal();
                for (int offset = 0; offset < 5 && start + offset < slots.Count; offset++)
                {
                    int index = slots[start + offset];
                    WarehouseStack stack = Inventory.GetSlot(index);
                    GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(100));
                    if (stack == null)
                    {
                        if (GUILayout.Button("빈 칸", GUILayout.Width(90), GUILayout.Height(80))) Select(index);
                    }
                    else
                    {
                        ItemQualityData info = items.GetQuality(stack.itemId, stack.quality);
                        Sprite icon = LoadIcon(info.iconKey);
                        GUIContent content = icon == null ? new GUIContent(info.name) : new GUIContent(icon.texture);
                        if (GUILayout.Button(content, GUILayout.Width(90), GUILayout.Height(80))) Select(index);
                        GUILayout.Label(info.name + " × " + stack.quantity);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
            if (slots.Count == 0) GUILayout.Label("해당 종류의 물건이 없습니다.");
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
            WarehouseStack chosen = Inventory.GetSlot(selected);
            if (chosen == null) GUILayout.Label("아이템을 선택하세요.");
            else
            {
                ItemQualityData info = items.GetQuality(chosen.itemId, chosen.quality);
                GUILayout.Label(info.name + " / " + chosen.quality);
                Sprite icon = LoadIcon(info.iconKey);
                if (icon != null) GUILayout.Label(icon.texture, GUILayout.Width(180), GUILayout.Height(180));
                GUILayout.Label("보유 수량: " + chosen.quantity);
                GUILayout.Label("평균 매입가: " + chosen.averagePurchasePrice.ToString("0.##"));
                GUILayout.Label("판매가(개당): " + chosen.sellingPrice);
                GUILayout.Label(info.explanation);
                GUILayout.Label("새 판매가");
                priceText = GUILayout.TextField(priceText);
                if (GUILayout.Button("판매가 적용"))
                {
                    int price;
                    notice = int.TryParse(priceText, out price) && Inventory.TrySetSellingPrice(selected, price)
                        ? "판매가를 변경했습니다." : "0 이상의 정수를 입력하세요.";
                }
                if (GUILayout.Button("테스트: 1개 출고"))
                {
                    string error;
                    notice = Inventory.TryRemove(chosen.itemId, chosen.quality, 1, out error)
                        ? "1개 출고했습니다. 이 버튼은 돈을 증가시키지 않습니다." : error;
                }
            }
            GUILayout.EndVertical(); GUILayout.EndHorizontal();
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void Save()
        {
            try { File.WriteAllText(SavePath, Inventory.ToJson()); notice = "창고 저장 완료"; }
            catch (Exception e) { notice = "저장 실패: " + e.Message; }
        }
        void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) { notice = "창고 저장 파일이 없습니다."; return; }
                string error;
                if (Inventory.TryLoadJson(File.ReadAllText(SavePath), out error))
                { Select(-1); notice = "창고 불러오기 완료"; }
                else notice = "불러오기 실패: " + error;
            }
            catch (Exception e) { notice = "불러오기 실패: " + e.Message; }
        }
    }
}
