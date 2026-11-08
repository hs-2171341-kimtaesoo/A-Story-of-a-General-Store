using System;
using System.Collections.Generic;
using UnityEngine;

namespace CounterShop
{
    // 화면을 모르는 게임 규칙 계층입니다. 나중에 UI를 교체해도 이 코드는 유지할 수 있습니다.
    public class ShopEngine
    {
        public ShopDatabase Database { get; private set; }
        public WorldState State { get; private set; }
        public Visit Current { get; private set; }
        public VisitPhase Phase { get; private set; }
        public int DailyLimit = 5;
        readonly System.Random random;
        public ShopEngine(ShopDatabase database, WorldState state, int seed = 1234)
        {
            database.ValidateData();
            Database = database; State = state; random = new System.Random(seed);
        }

        bool Alive(CustomerData c) { return c != null && !State.dead.Contains(c.id); }
        bool Eligible(CustomerData c)
        {
            return Alive(c) && State.day >= c.firstDay && State.day <= c.lastDay
                && !State.visitedToday.Contains(c.id) && c.spawnWeight > 0;
        }
        StoryEvent FindStory(string customerId, bool forcedOnly)
        {
            StoryEvent best = null;
            foreach (var s in Database.stories)
            {
                if (customerId != null && s.customerId != customerId) continue;
                if (forcedOnly && !s.forceVisit) continue;
                if (!Alive(Database.Customer(s.customerId)) || State.completedEvents.Contains(s.id)
                    || !s.condition.Matches(State)) continue;
                if (best == null || s.priority > best.priority) best = s;
            }
            return best; // 동률은 데이터 목록에서 앞선 이벤트가 우선합니다.
        }
        public string NextVisitor()
        {
            if (Phase != VisitPhase.Idle) return "먼저 현재 손님의 계산을 마쳐 주세요.";
            CustomerData selected = null;
            StoryEvent story = null;
            // 명시적인 강제 방문은 일반 등장 기간/일일 한도/오늘 방문 여부를 무시합니다.
            // 단, 사망은 항상 최우선입니다. 사망자의 예약은 제거됩니다.
            while (State.forcedQueue.Count > 0 && selected == null)
            {
                string id = State.forcedQueue[0]; State.forcedQueue.RemoveAt(0);
                var c = Database.Customer(id);
                if (Alive(c)) selected = c;
            }
            if (selected == null)
            {
                story = FindStory(null, true);
                if (story != null) selected = Database.Customer(story.customerId);
            }
            if (selected == null)
            {
                if (State.visitsToday >= DailyLimit) return "오늘의 일반 영업이 끝났습니다. 다음 날로 넘어가세요.";
                var candidates = Database.customers.FindAll(Eligible);
                double total = 0;
                foreach (var c in candidates) total += c.spawnWeight;
                if (total <= 0) return "오늘 등장할 수 있는 손님이 없습니다.";
                double roll = random.NextDouble() * total;
                selected = candidates[candidates.Count - 1];
                foreach (var c in candidates) { roll -= c.spawnWeight; if (roll < 0) { selected = c; break; } }
            }
            if (story == null) story = FindStory(selected.id, false);
            Current = new Visit { customer = selected, remainingBudget = selected.budget };
            State.visitsToday++;
            if (!State.visitedToday.Contains(selected.id)) State.visitedToday.Add(selected.id);
            if (story != null)
            {
                // 이 초안에서 이벤트는 대화를 시작한 순간 소비됩니다. 재등장시키려면 새 이벤트 ID를 만드세요.
                State.completedEvents.Add(story.id);
                Current.nodeId = story.firstNodeId;
                Phase = VisitPhase.Dialogue;
            }
            else { Phase = VisitPhase.Dialogue; Current.nodeId = ""; }
            return selected.displayName + " 등장";
        }
        public string DialogueText()
        {
            if (Current == null) return "";
            var node = Database.Node(Current.nodeId);
            return node == null ? Current.customer.greeting : node.text;
        }
        public void Choose(int index)
        {
            if (Phase != VisitPhase.Dialogue) return;
            var node = Database.Node(Current.nodeId);
            if (node == null || node.choices.Count == 0) { Browse(); return; }
            if (index < 0 || index >= node.choices.Count) return;
            var choice = node.choices[index];
            choice.effect.Apply(State);
            // 현재 손님 자신이 사망한 경우에도 계산을 계속하는 모순을 막습니다.
            if (!Alive(Current.customer)) { Current = null; Phase = VisitPhase.Idle; return; }
            if (string.IsNullOrEmpty(choice.nextNodeId)) Browse();
            else Current.nodeId = choice.nextNodeId;
        }

        // 추가 구매 확률은 0~1로 제한합니다. 선호 태그가 여러 개 맞아도 보너스는 한 번만 적용합니다.
        public float PurchaseChance(CustomerData customer, ItemData item, int price)
        {
            bool liked = Array.Exists(item.tags, tag => Array.IndexOf(customer.preferredTags, tag) >= 0);
            float priceAppeal = Mathf.Clamp(1f - price / (float)item.referencePrice, -1f, 1f);
            return Mathf.Clamp01(.10f + (liked ? .30f : 0f) + .25f * priceAppeal
                + .20f * item.quality + .15f * State.reputation);
        }
        void AddCart(Stock stock, int count, bool required)
        {
            if (count <= 0) return;
            Current.cart.Add(new CartLine { itemId = stock.itemId, quantity = count,
                unitPrice = stock.price, required = required });
            // 필수 구매는 반드시 산다는 기획을 우선합니다. 예산은 추가 구매에만 제한으로 쓰입니다.
            Current.remainingBudget = Math.Max(0, Current.remainingBudget - stock.price * count);
        }
        void Browse()
        {
            var c = Current.customer;
            if (!string.IsNullOrEmpty(c.requiredItemId))
            {
                var needed = State.StockOf(c.requiredItemId);
                int available = needed == null ? 0 : Math.Min(c.requiredQuantity, needed.shelf);
                if (available > 0) AddCart(needed, available, true);
                Current.missingRequired = c.requiredQuantity - available;
            }
            foreach (var stock in State.stock)
            {
                // 필수 상품을 추가 구매 대상으로 다시 굴리지 않아 중복 구매를 막습니다.
                if (stock.itemId == c.requiredItemId || stock.shelf <= 0 || stock.price > Current.remainingBudget) continue;
                if (random.NextDouble() < PurchaseChance(c, Database.Item(stock.itemId), stock.price))
                    AddCart(stock, 1, false);
            }
            Phase = VisitPhase.Counter;
            // 아직 재고를 빼지 않습니다. 방문 중 진열 편집을 막아 장바구니 수량을 예약합니다.
        }
        public bool SupplyRequiredFromWarehouse()
        {
            if (Phase != VisitPhase.Counter || Current.missingRequired <= 0) return false;
            var stock = State.StockOf(Current.customer.requiredItemId);
            if (stock == null || stock.warehouse <= 0) return false;
            int count = Math.Min(Current.missingRequired, stock.warehouse);
            // 창고분은 지금 차감합니다. 이후 결제에서는 진열대분과 구분합니다.
            stock.warehouse -= count;
            Current.cart.Add(new CartLine { itemId = stock.itemId, quantity = count, unitPrice = stock.price, required = true, fromWarehouse = true });
            Current.missingRequired -= count;
            return true;
        }
        public int CartTotal()
        {
            int total = 0;
            if (Current != null) foreach (var line in Current.cart) total += line.quantity * line.unitPrice;
            return total;
        }
        public string Checkout()
        {
            if (Phase != VisitPhase.Counter) return "계산할 손님이 없습니다.";
            int total = CartTotal();
            foreach (var line in Current.cart)
            {
                // 창고에서 준 필수분만큼은 이미 차감했으므로 진열대에서는 빼지 않습니다.
                if (!line.fromWarehouse) State.StockOf(line.itemId).shelf -= line.quantity;
            }
            string customerId = Current.customer.id;
            State.gold += total;
            // 거래 결과 플래그는 다음 방문부터 스토리 조건에 반영됩니다.
            if (!string.IsNullOrEmpty(Current.customer.requiredItemId) && Current.missingRequired == 0)
            {
                string flag = "helped_" + customerId;
                if (!State.flags.Contains(flag)) State.flags.Add(flag);
            }
            Current = null; Phase = VisitPhase.Idle;
            return "계산 완료: " + total + " G";
        }
        public bool SetDisplay(string itemId, int change, int price)
        {
            if (Phase != VisitPhase.Idle || price < 1) return false;
            var stock = State.StockOf(itemId);
            if (stock == null || stock.warehouse - change < 0 || stock.shelf + change < 0) return false;
            stock.warehouse -= change; stock.shelf += change; stock.price = price;
            return true;
        }
        public bool NextDay()
        {
            if (Phase != VisitPhase.Idle) return false;
            State.day++; State.visitsToday = 0; State.visitedToday.Clear(); return true;
        }
    }
}
