using System;
using System.Collections.Generic;

namespace CounterShop
{
    // Unity JsonUtility는 Dictionary를 직접 저장하지 못하므로 리스트 형태로 저장합니다.
    [Serializable] public class Stock
    {
        public string itemId;
        public int warehouse, shelf;
        public int price;
    }

    [Serializable] public class WorldState
    {
        public int version = 1;
        public int day = 1, gold = 0, visitsToday;
        public float reputation = .5f;
        public List<Stock> stock = new List<Stock>();
        public List<string> flags = new List<string>();
        public List<string> dead = new List<string>();
        public List<string> completedEvents = new List<string>();
        public List<string> forcedQueue = new List<string>();
        public List<string> visitedToday = new List<string>();
        public Stock StockOf(string id) { return stock.Find(x => x.itemId == id); }
    }

    public enum VisitPhase { Idle, Dialogue, Counter }

    public class CartLine
    {
        public string itemId;
        public int quantity, unitPrice;
        public bool required;
        public bool fromWarehouse; // 이미 창고에서 꺼낸 품목인지 기록합니다.
    }

    // 방문 중 정보는 임시 상태입니다. 초안에서는 대기 중에만 저장/불러오기를 허용합니다.
    public class Visit
    {
        public CustomerData customer;
        public string nodeId;
        public List<CartLine> cart = new List<CartLine>();
        public int missingRequired;
        public int remainingBudget;
    }
}
