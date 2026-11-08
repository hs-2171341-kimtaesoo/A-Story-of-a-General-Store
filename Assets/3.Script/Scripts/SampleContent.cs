using System.Collections.Generic;
using UnityEngine;

namespace CounterShop
{
    // 별도 에셋 없이 즉시 실험할 수 있는 샘플입니다. 실제 제작에서는 Database 에셋으로 이동하세요.
    public static class SampleContent
    {
        public static ShopDatabase Create()
        {
            var db = ScriptableObject.CreateInstance<ShopDatabase>();
            db.items.Add(new ItemData { id = "potion", displayName = "회복 포션", tags = new[] { "medicine" }, referencePrice = 100, quality = .8f });
            db.items.Add(new ItemData { id = "bread", displayName = "여행자 빵", tags = new[] { "food" }, referencePrice = 30, quality = .6f });
            db.items.Add(new ItemData { id = "charm", displayName = "은빛 부적", tags = new[] { "magic" }, referencePrice = 150, quality = .9f });
            db.customers.Add(new CustomerData { id = "traveler", displayName = "여행자", requiredItemId = "potion", preferredTags = new[] { "food" }, greeting = "먼 길을 떠나려네. 물건 좀 보겠네.", budget = 250 });
            db.customers.Add(new CustomerData { id = "healer", displayName = "치유사", requiredItemId = "bread", preferredTags = new[] { "medicine" }, greeting = "오늘도 문을 열었군요.", budget = 300 });
            db.customers.Add(new CustomerData { id = "messenger", displayName = "전령", preferredTags = new[] { "magic" }, greeting = "잠깐 둘러보겠소.", budget = 200 });
            db.stories.Add(new StoryEvent { id = "departure", customerId = "traveler", firstNodeId = "departure_start", forceVisit = true, priority = 100 });
            db.dialogue.Add(new DialogueNode { id = "departure_start", text = "북쪽 숲으로 가려네. 숲의 위험에 대해 들은 게 있나?", choices = new List<DialogueChoice> {
                new DialogueChoice { text = "위험하니 치유사와 함께 가세요.", effect = new StoryEffect { addFlags = new[] { "warned" }, forceVisitors = new[] { "healer" }, reputationDelta = 5 } },
                new DialogueChoice { text = "혼자 가도 괜찮을 겁니다.", effect = new StoryEffect { addFlags = new[] { "unwarned" } } }
            } });
            db.stories.Add(new StoryEvent { id = "return_safe", customerId = "traveler", firstNodeId = "safe", forceVisit = true, priority = 80,
                condition = new StoryCondition { minDay = 2, requiredFlags = new[] { "warned", "helped_traveler" } } });
            db.dialogue.Add(new DialogueNode { id = "safe", text = "포션 덕에 무사했네. 치유사도 자네에게 고맙다고 하더군." });
            db.stories.Add(new StoryEvent { id = "bad_news", customerId = "messenger", firstNodeId = "death", forceVisit = true, priority = 90,
                condition = new StoryCondition { minDay = 2, requiredFlags = new[] { "unwarned" }, mustBeAlive = new[] { "traveler" } } });
            db.dialogue.Add(new DialogueNode { id = "death", text = "북쪽 숲에서 여행자가 숨졌소. 이제 그를 볼 수 없을 거요.", choices = new List<DialogueChoice> {
                new DialogueChoice { text = "소식을 전해 줘서 고맙습니다.", effect = new StoryEffect { killCustomers = new[] { "traveler" }, addFlags = new[] { "death_reported" } } }
            } });
            return db;
        }
        public static WorldState NewState(ShopDatabase db)
        {
            var state = new WorldState();
            foreach (var item in db.items)
                state.stock.Add(new Stock { itemId = item.id, warehouse = 5, shelf = item.id == "potion" ? 0 : 2, price = item.referencePrice });
            return state;
        }
    }
}
