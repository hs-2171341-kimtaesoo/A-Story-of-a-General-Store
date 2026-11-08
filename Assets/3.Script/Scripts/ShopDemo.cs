using System;
using System.IO;
using UnityEngine;

namespace CounterShop
{
    // IMGUI는 규칙을 빠르게 확인하기 위한 임시 화면입니다. 최종 화면은 Canvas/TMP로 교체하세요.
    // 빈 GameObject에 이 컴포넌트 하나만 붙이면 플레이할 수 있습니다.
    public class ShopDemo : MonoBehaviour
    {
        public ShopDatabase database;
        [Tooltip("한글이 안 보이면 한글 글리프가 있는 Font 에셋을 넣으세요.")]
        public Font koreanFont;
        ShopEngine engine;
        string notice = "진열을 준비하고 손님을 맞이하세요. 포션은 창고에만 있습니다.";
        Vector2 scroll;
        string SavePath { get { return Path.Combine(Application.persistentDataPath, "counter-shop-v1.json"); } }
        void Awake()
        {
            if (database == null) database = SampleContent.Create();
            engine = new ShopEngine(database, SampleContent.NewState(database));
        }
        void OnGUI()
        {
            if (engine == null) return;
            if (koreanFont != null) GUI.skin.font = koreanFont;
            GUI.skin.label.wordWrap = true;
            GUILayout.BeginArea(new Rect(16, 16, Mathf.Min(Screen.width - 32, 1100), Screen.height - 32), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            var state = engine.State;
            GUILayout.Label("카운터 잡화점 — " + state.day + "일차 / " + state.gold + " G / 평판 " + Mathf.RoundToInt(state.reputation * 100));
            GUILayout.Label(notice);
            GUILayout.Space(12);
            GUILayout.Label("[진열대 / 창고] 방문 중에는 수량·가격을 변경할 수 없습니다.");
            GUI.enabled = engine.Phase == VisitPhase.Idle;
            foreach (var stock in state.stock)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(database.Item(stock.itemId).displayName + " | 진열 " + stock.shelf + " / 창고 " + stock.warehouse + " | " + stock.price + " G", GUILayout.Width(430));
                if (GUILayout.Button("진열 +1")) engine.SetDisplay(stock.itemId, 1, stock.price);
                if (GUILayout.Button("창고로 -1")) engine.SetDisplay(stock.itemId, -1, stock.price);
                if (GUILayout.Button("가격 -10")) engine.SetDisplay(stock.itemId, 0, Math.Max(1, stock.price - 10));
                if (GUILayout.Button("가격 +10")) engine.SetDisplay(stock.itemId, 0, stock.price + 10);
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("다음 손님")) notice = engine.NextVisitor();
            if (GUILayout.Button("다음 날")) { engine.NextDay(); notice = "새로운 하루입니다."; }
            if (GUILayout.Button("저장")) Save();
            if (GUILayout.Button("불러오기")) Load();
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.Space(18);
            if (engine.Current != null)
            {
                var visit = engine.Current;
                GUILayout.Label("[카운터 앞] " + visit.customer.displayName);
                if (visit.customer.portrait != null)
                    GUILayout.Label(visit.customer.portrait.texture, GUILayout.Width(180), GUILayout.Height(180));
                if (engine.Phase == VisitPhase.Dialogue)
                {
                    GUILayout.Label(engine.DialogueText());
                    var node = database.Node(visit.nodeId);
                    if (node == null || node.choices.Count == 0)
                    { if (GUILayout.Button("물건을 둘러보게 한다")) engine.Choose(0); }
                    else for (int i = 0; i < node.choices.Count; i++)
                        if (GUILayout.Button(node.choices[i].text)) { engine.Choose(i); break; }
                }
                else
                {
                    GUILayout.Label("구매 예정 물건");
                    foreach (var line in visit.cart)
                        GUILayout.Label(database.Item(line.itemId).displayName + " × " + line.quantity + " / 개당 " + line.unitPrice + " G" + (line.fromWarehouse ? " (창고)" : " (진열대)"));
                    if (visit.missingRequired > 0)
                    {
                        var stock = state.StockOf(visit.customer.requiredItemId);
                        GUILayout.Label(database.Item(visit.customer.requiredItemId).displayName + " " + visit.missingRequired + "개가 더 필요합니다. 창고에 있나요?");
                        // 재고는 플레이어가 직접 확인하고 버튼으로 꺼냅니다. 자동 판매하지 않습니다.
                        GUILayout.Label("창고 확인: " + (stock == null ? 0 : stock.warehouse) + "개");
                        if (GUILayout.Button("창고에서 꺼내 판매 목록에 추가"))
                            notice = engine.SupplyRequiredFromWarehouse() ? "창고 물건을 준비했습니다." : "창고에도 재고가 없습니다.";
                    }
                    GUILayout.Label("합계: " + engine.CartTotal() + " G");
                    if (GUILayout.Button("현재 목록으로 계산하고 손님 보내기")) notice = engine.Checkout();
                }
            }
            else GUILayout.Label("[카운터 앞] 손님을 기다리고 있습니다.");
            GUILayout.Space(12);
            GUILayout.Label("진행 플래그: " + string.Join(", ", state.flags));
            GUILayout.Label("사망한 인물: " + string.Join(", ", state.dead));
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void Save()
        {
            if (engine.Phase != VisitPhase.Idle) return;
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(engine.State, true)); notice = "저장 완료"; }
            catch (Exception e) { notice = "저장 실패: " + e.Message; }
        }
        void Load()
        {
            if (engine.Phase != VisitPhase.Idle) return;
            try
            {
                if (!File.Exists(SavePath)) { notice = "저장 파일이 없습니다."; return; }
                var state = JsonUtility.FromJson<WorldState>(File.ReadAllText(SavePath));
                if (state == null || state.version != 1 || state.stock == null || state.flags == null || state.dead == null
                    || state.completedEvents == null || state.forcedQueue == null || state.visitedToday == null)
                    throw new InvalidDataException("지원하지 않는 저장 파일입니다.");
                // 초안은 기존 ID 변경/삭제를 지원하지 않습니다. 신규 아이템만 기본 재고 0으로 추가합니다.
                foreach (var stock in state.stock)
                    if (database.Item(stock.itemId) == null || stock.shelf < 0 || stock.warehouse < 0 || stock.price < 1)
                        throw new InvalidDataException("아이템 또는 재고 데이터가 맞지 않습니다.");
                foreach (var item in database.items)
                    if (state.StockOf(item.id) == null) state.stock.Add(new Stock { itemId = item.id, price = item.referencePrice });
                engine = new ShopEngine(database, state); notice = "불러오기 완료";
            }
            catch (Exception e) { notice = "불러오기 실패: " + e.Message; }
        }
    }
}
