using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    [Serializable] public sealed class CustomerDatabase { public CustomerDefinition[] customers; }
    // 캐릭터마다 파일/스크립트를 늘리지 않습니다. JSON의 데이터 행만 추가하세요.
    [Serializable] public sealed class CustomerDefinition
    {
        public string id,displayName,portraitKey,greeting,situationText;
        public bool enabled=true;
        public string[] preferredItemIds;
        public double priceSensitivity=1, preferencePremium=.5, negotiationRoom=.1;
        public double inquiryChance=.6, inquiryPerVisit=.05;
        public int patience=3, sellPrice;
        public string sellItemId,eventText;
        public double[] preferenceStrength;
        public string[] visitStories;
        public double ValueRatio(string itemId)
        {
            int index=Array.IndexOf(preferredItemIds,itemId);
            double liking=index<0?0:preferenceStrength!=null&&index<preferenceStrength.Length?preferenceStrength[index]:1;
            return 1+(Math.Max(0,maxPriceRatio-1)+liking*preferencePremium)/Math.Max(.05,priceSensitivity);
        }
        public int maxPickCount=5;
        public long wallet=1000;
        public double spendingFraction=.5, maxPriceRatio=1.5;
        public CustomerDefinition Copy()
        {
            var copy=(CustomerDefinition)MemberwiseClone();
            copy.preferredItemIds=(string[])preferredItemIds?.Clone();
            copy.preferenceStrength=(double[])preferenceStrength?.Clone();
            copy.visitStories=(string[])visitStories?.Clone();
            return copy;
        }
        public void Validate()
        {
            foreach(double value in new[]{priceSensitivity,preferencePremium,negotiationRoom,inquiryChance,inquiryPerVisit})
                if(double.IsNaN(value)||double.IsInfinity(value)||value<0||value>100)throw new ArgumentException("손님 성향 범위 오류: "+id);
            if(patience<1||sellPrice<0||negotiationRoom>1||inquiryChance>1)throw new ArgumentException("손님 거래 설정 오류: "+id);
            if(preferenceStrength!=null)foreach(double strength in preferenceStrength)
                if(!(strength>=0&&strength<=1))throw new ArgumentException("선호도는 0~1입니다: "+id);
            if(string.IsNullOrWhiteSpace(id)||wallet<0||maxPickCount<1||maxPickCount>5||preferredItemIds==null
                ||double.IsNaN(spendingFraction)||spendingFraction<0||spendingFraction>1
                ||double.IsNaN(maxPriceRatio)||double.IsInfinity(maxPriceRatio)||maxPriceRatio<=0)
                throw new ArgumentException("손님 데이터 오류: "+id);
        }
    }
    public sealed class CustomerCatalog
    {
        readonly List<CustomerDefinition> records=new List<CustomerDefinition>();
        public CustomerCatalog(CustomerDatabase database)
        {
            if(database==null||database.customers==null)throw new ArgumentException("customers 배열이 필요합니다.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(CustomerDefinition c in database.customers)
            { if(c==null)throw new ArgumentException("빈 손님 데이터입니다.");c.Validate();if(!ids.Add(c.id))throw new ArgumentException("중복 손님 id");records.Add(c.Copy()); }
        }
        public List<CustomerDefinition> All()
        { var result=new List<CustomerDefinition>();foreach(var c in records)result.Add(c.Copy());return result; }
    }
    // 이후 상황/평판/품질/이벤트를 반영할 때 이 인터페이스 구현만 교체합니다.
    public interface ICustomerPurchasePolicy
    {
        long SpendingBudget(CustomerDefinition customer);
        bool AcceptItem(CustomerDefinition customer,ItemRecord item,int price,int referencePrice);
    }
    public sealed class DefaultCustomerPurchasePolicy : ICustomerPurchasePolicy
    {
        public long SpendingBudget(CustomerDefinition c)
        { c.Validate();return (long)decimal.Floor(c.wallet*(decimal)c.spendingFraction); }
        public bool AcceptItem(CustomerDefinition c,ItemRecord item,int price,int referencePrice)
        { return price>=0 && price<=referencePrice*c.maxPriceRatio; }
    }
    // 생존 여부/특정 날짜/이벤트 강제 방문은 여기에서 처리하도록 UI와 분리해 둡니다.
    public interface ICustomerVisitPolicy
    { bool CanVisit(CustomerDefinition customer,ShopClockState clock); }
    public sealed class DefaultCustomerVisitPolicy : ICustomerVisitPolicy
    { public bool CanVisit(CustomerDefinition customer,ShopClockState clock){return customer.enabled;} }
    // 상황별 텍스트도 UI에 if문을 계속 추가하지 않고 별도 정책으로 교체할 수 있습니다.
    public interface ICustomerDialoguePolicy
    { string Text(CustomerDefinition customer,bool conversation,ShopClockState clock); }
    public sealed class DefaultCustomerDialoguePolicy : ICustomerDialoguePolicy
    {
        public string Text(CustomerDefinition c,bool conversation,ShopClockState clock)
        {return conversation?c.situationText:c.greeting;}
    }

}
