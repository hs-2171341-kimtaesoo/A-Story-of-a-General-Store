using System;
using System.Collections.Generic;
using UnityEngine;

namespace CounterShop
{
    // ID는 세이브 파일과 스토리를 연결하는 영구 키입니다. 출시 후 이름이 바뀌어도 ID는 바꾸지 마세요.
    [Serializable]
    public class ItemData
    {
        public string id, displayName;
        public string[] tags = new string[0];
        [Min(1)] public int referencePrice = 100;
        [Range(0, 1)] public float quality = .5f;
        public Sprite icon;
    }

    [Serializable]
    public class CustomerData
    {
        public string id, displayName;
        public Sprite portrait;
        public string[] preferredTags = new string[0];
        public string requiredItemId;
        [Min(1)] public int requiredQuantity = 1;
        [Min(0)] public int firstDay = 1;
        public int lastDay = 9999;
        [Min(0)] public float spawnWeight = 1;
        public int budget = 1000;
        [TextArea] public string greeting;
    }

    [Serializable]
    public class StoryCondition
    {
        public int minDay = 1, maxDay = 9999;
        public string[] requiredFlags = new string[0];
        public string[] forbiddenFlags = new string[0];
        public string[] mustBeAlive = new string[0];
        public string[] mustBeDead = new string[0];

        // 모든 조건은 AND입니다. OR은 별도 이벤트를 두거나 다음 단계에서 조건 트리로 확장합니다.
        public bool Matches(WorldState state)
        {
            if (state.day < minDay || state.day > maxDay) return false;
            foreach (string id in requiredFlags) if (!state.flags.Contains(id)) return false;
            foreach (string id in forbiddenFlags) if (state.flags.Contains(id)) return false;
            foreach (string id in mustBeAlive) if (state.dead.Contains(id)) return false;
            foreach (string id in mustBeDead) if (!state.dead.Contains(id)) return false;
            return true;
        }
    }

    [Serializable]
    public class StoryEffect
    {
        public string[] addFlags = new string[0];
        public string[] killCustomers = new string[0];
        public string[] forceVisitors = new string[0];
        public int reputationDelta;

        public void Apply(WorldState state)
        {
            foreach (string id in addFlags) if (!state.flags.Contains(id)) state.flags.Add(id);
            foreach (string id in killCustomers) if (!state.dead.Contains(id)) state.dead.Add(id);
            foreach (string id in forceVisitors) state.forcedQueue.Add(id);
            state.reputation = Mathf.Clamp01(state.reputation + reputationDelta / 100f);
        }
    }

    [Serializable]
    public class DialogueChoice
    {
        public string text;
        public string nextNodeId; // 빈 문자열이면 대화 종료 → 진열장 둘러보기.
        public StoryEffect effect = new StoryEffect();
    }

    [Serializable]
    public class DialogueNode
    {
        public string id;
        [TextArea(2, 8)] public string text;
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    [Serializable]
    public class StoryEvent
    {
        public string id, customerId, firstNodeId;
        public int priority;
        public bool forceVisit;
        public StoryCondition condition = new StoryCondition();
    }

    // 에셋은 변경하지 않는 '설계도'입니다. 수량/생사/진행도는 WorldState에만 저장합니다.
    [CreateAssetMenu(menuName = "Counter Shop/Database")]
    public class ShopDatabase : ScriptableObject
    {
        public List<ItemData> items = new List<ItemData>();
        public List<CustomerData> customers = new List<CustomerData>();
        public List<StoryEvent> stories = new List<StoryEvent>();
        public List<DialogueNode> dialogue = new List<DialogueNode>();
        public ItemData Item(string id) { return items.Find(x => x.id == id); }
        public CustomerData Customer(string id) { return customers.Find(x => x.id == id); }
        public DialogueNode Node(string id) { return dialogue.Find(x => x.id == id); }

        // 잘못된 ID는 늦게 터지기보다 시작 단계에서 잡는 편이 편합니다.
        public void ValidateData()
        {
            var ids = new HashSet<string>();
            foreach (var x in items)
                if (string.IsNullOrWhiteSpace(x.id) || !ids.Add(x.id) || x.referencePrice <= 0)
                    throw new InvalidOperationException("아이템 ID/기준 가격 오류: " + x.id);
            ids.Clear();
            foreach (var x in customers)
            {
                if (string.IsNullOrWhiteSpace(x.id) || !ids.Add(x.id) || x.requiredQuantity < 1)
                    throw new InvalidOperationException("손님 데이터 오류: " + x.id);
                if (!string.IsNullOrEmpty(x.requiredItemId) && Item(x.requiredItemId) == null)
                    throw new InvalidOperationException("없는 필수 아이템: " + x.requiredItemId);
            }
            ids.Clear();
            foreach (var x in dialogue)
                if (string.IsNullOrWhiteSpace(x.id) || !ids.Add(x.id))
                    throw new InvalidOperationException("대화 ID 오류: " + x.id);
            foreach (var x in dialogue)
                foreach (var c in x.choices)
                {
                    if (!string.IsNullOrEmpty(c.nextNodeId) && Node(c.nextNodeId) == null)
                        throw new InvalidOperationException("없는 다음 대화: " + c.nextNodeId);
                    ValidateEffect(c.effect);
                }
            ids.Clear();
            foreach (var x in stories)
            {
                if (string.IsNullOrWhiteSpace(x.id) || !ids.Add(x.id) || Customer(x.customerId) == null || Node(x.firstNodeId) == null)
                    throw new InvalidOperationException("이벤트 참조 오류: " + x.id);
                foreach (string id in x.condition.mustBeAlive) CheckCustomer(id);
                foreach (string id in x.condition.mustBeDead) CheckCustomer(id);
            }
        }
        void CheckCustomer(string id)
        {
            if (Customer(id) == null) throw new InvalidOperationException("없는 손님: " + id);
        }
        void ValidateEffect(StoryEffect effect)
        {
            foreach (string id in effect.killCustomers) CheckCustomer(id);
            foreach (string id in effect.forceVisitors) CheckCustomer(id);
        }
    }
}
