using System;
using UnityEditor;
using UnityEngine;

namespace CounterShop.Editor
{
    // 외부 테스트 패키지 없이 실행하는 핵심 규칙 검증입니다.
    // Unity 메뉴 Counter Shop/Run Rule Checks에서 실행하고 Console 결과를 확인하세요.
    public static class ShopRuleChecks
    {
        static void Check(bool condition, string message)
        { if (!condition) throw new Exception("검증 실패: " + message); }
        [MenuItem("Counter Shop/Run Rule Checks")]
        public static void Run()
        {
            var db = SampleContent.Create();
            try
            {
                var state = SampleContent.NewState(db);
                var game = new ShopEngine(db, state, 7);
                game.NextVisitor();
                Check(game.Current.customer.id == "traveler", "강제 이벤트 우선 등장");
                game.Choose(0);
                Check(state.flags.Contains("warned"), "선택지 플래그");
                Check(game.Current.missingRequired == 1, "진열대에 없는 필수품 문의");
                Check(!game.SetDisplay("potion", 1, 100), "방문 중 진열 변경 차단");
                Check(game.SupplyRequiredFromWarehouse(), "창고에서 공급");
                Check(!game.SupplyRequiredFromWarehouse(), "필수 수량 초과 공급 차단");
                int total = game.CartTotal();
                game.Checkout();
                Check(state.StockOf("potion").warehouse == 4 && state.StockOf("potion").shelf == 0, "창고 재고만 1개 차감");
                Check(state.gold == total && state.flags.Contains("helped_traveler"), "결제 및 거래 플래그");
                game.Checkout(); Check(state.gold == total, "중복 결제 차단");
                game.NextVisitor(); Check(game.Current.customer.id == "healer", "선택지가 예약한 강제 방문");

                // 별도 상태로 사망 분기를 검증합니다.
                var bad = SampleContent.NewState(db);
                var badGame = new ShopEngine(db, bad);
                badGame.NextVisitor(); badGame.Choose(1); badGame.Checkout();
                badGame.NextDay(); badGame.NextVisitor();
                Check(badGame.Current.customer.id == "messenger", "2일차 사망 소식 이벤트");
                badGame.Choose(0); badGame.Checkout();
                Check(bad.dead.Contains("traveler"), "사망 처리");
                bad.forcedQueue.Add("traveler"); bad.forcedQueue.Add("healer");
                badGame.NextVisitor(); Check(badGame.Current.customer.id == "healer", "사망자가 강제 예약되어도 등장 금지");

                // 필수품 진열 구매와 재고 부족을 확인합니다.
                var direct = SampleContent.NewState(db);
                direct.StockOf("potion").shelf = 1;
                var directGame = new ShopEngine(db, direct);
                directGame.NextVisitor(); directGame.Choose(0);
                Check(directGame.Current.missingRequired == 0, "진열 필수품은 확정 구매");
                directGame.Checkout();
                Check(direct.StockOf("potion").shelf == 0 && direct.StockOf("potion").warehouse == 5, "진열 재고만 차감");
                var empty = SampleContent.NewState(db); empty.StockOf("potion").warehouse = 0;
                var emptyGame = new ShopEngine(db, empty);
                emptyGame.NextVisitor(); emptyGame.Choose(0);
                Check(!emptyGame.SupplyRequiredFromWarehouse(), "창고 품절 처리");
                emptyGame.Checkout(); Check(!empty.flags.Contains("helped_traveler"), "필수품 못 주면 도움 플래그 없음");
                var c = db.Customer("traveler"); var item = db.Item("bread");
                Check(game.PurchaseChance(c, item, 15) > game.PurchaseChance(c, item, 60), "가격 매력도 반영");
                var saved = JsonUtility.FromJson<WorldState>(JsonUtility.ToJson(bad));
                Check(saved.dead.Contains("traveler") && saved.day == 2, "저장 데이터 왕복");
                Debug.Log("Counter Shop: 모든 규칙 검증 통과");
            }
            finally { UnityEngine.Object.DestroyImmediate(db); }
        }
    }
}
