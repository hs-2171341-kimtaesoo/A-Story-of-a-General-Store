using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class SupplierUI : MonoBehaviour
    {
        public InventoryPanel panel;
        public GameObject window,listPage,tradePage;
        public RectTransform supplierList,offerList;
        public SupplierListRowView supplierTemplate;
        public SupplierOfferRowView offerTemplate;
        public Text titleLabel,greetingLabel,cycleLabel,totalLabel,testSelectionLabel;
        public global::UnityEngine.UI.Image portrait;
        public Button buyButton;
        readonly Dictionary<string,SupplierListRowView> suppliers=new Dictionary<string,SupplierListRowView>();
        readonly Dictionary<string,SupplierOfferRowView> offers=new Dictionary<string,SupplierOfferRowView>();
        readonly Dictionary<string,int> quantities=new Dictionary<string,int>();
        readonly Dictionary<string,int> prices=new Dictionary<string,int>();
        readonly Dictionary<string,Sprite> portraits=new Dictionary<string,Sprite>();
        readonly HashSet<string> seen=new HashSet<string>();readonly List<string> removed=new List<string>();
        string selected,quoteSupplier;int viewCycle=-1;bool ready,dirty;
        void Start()
        {
            string error;
            if(panel==null||window==null||listPage==null||tradePage==null||supplierList==null||offerList==null||supplierTemplate==null
                ||offerTemplate==null||titleLabel==null||greetingLabel==null||cycleLabel==null||totalLabel==null||testSelectionLabel==null||portrait==null||buyButton==null
                ||supplierTemplate.portrait==null||supplierTemplate.nameLabel==null||supplierTemplate.descriptionLabel==null||supplierTemplate.statusLabel==null
                ||supplierTemplate.selectButton==null||supplierTemplate.tradeButton==null||offerTemplate.icon==null||offerTemplate.nameLabel==null
                ||offerTemplate.priceLabel==null||offerTemplate.stockLabel==null||offerTemplate.quantityLabel==null||offerTemplate.minusButton==null||offerTemplate.plusButton==null
                ||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("SupplierUI 참조를 확인하세요. 새 데모를 생성하세요.",this);enabled=false;return;}
            panel.Model.Changed+=MarkDirty;panel.inventory.Loaded+=OnLoaded;ready=true;window.SetActive(false);ShowList();
        }
        void MarkDirty(){dirty=true;}
        void OnLoaded(){selected=null;quoteSupplier=null;viewCycle=-1;quantities.Clear();prices.Clear();ShowList();}
        void LateUpdate(){if(ready&&dirty&&window.activeSelf){dirty=false;Refresh();}}
        public Sprite FindPortrait(string key)
        {if(string.IsNullOrEmpty(key))return null;Sprite p;if(!portraits.TryGetValue(key,out p)){p=Resources.Load<Sprite>("SupplierPortraits/"+key);portraits[key]=p;}return p;}
        public void Toggle()
        {
            if(!ready)return;panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();
            window.SetActive(!window.activeSelf);if(window.activeSelf){EnsureCycle();ShowList();}
        }
        public void Close(){if(window!=null)window.SetActive(false);}
        void EnsureCycle(){string error;if(!panel.Model.RefreshSuppliersForDay(panel.inventory.Clock.Snapshot().day,out error))panel.ShowMessage(error);}
        public void ShowList(){listPage.SetActive(true);tradePage.SetActive(false);dirty=true;}
        public void SelectSupplierForTest(string id){selected=id;dirty=true;}
        public void OpenTrade(string id)
        {
            if(!ready)return;EnsureCycle();var state=panel.Model.Supplier(id);
            if(state==null||!state.unlocked||state.blocked){panel.ShowMessage("지금은 거래할 수 없습니다.");return;}
            selected=id;quoteSupplier=id;viewCycle=state.cycle;quantities.Clear();prices.Clear();listPage.SetActive(false);tradePage.SetActive(true);dirty=true;
        }
        public void ChangeQuantity(string offer,int delta)
        {
            var s=panel.Model.Supplier(selected);if(s==null)return;var stock=s.stocks.Find(v=>v.offerId==offer);if(stock==null)return;
            int n;quantities.TryGetValue(offer,out n);quantities[offer]=Mathf.Clamp(n+delta,0,stock.remaining);dirty=true;
        }
        public void Buy()
        {
            if(!ready)return;EnsureCycle();var lines=new List<SupplierPurchaseLine>();
            foreach(var pair in quantities)if(pair.Value>0)
            {int price;if(!prices.TryGetValue(pair.Key,out price)){panel.ShowMessage("목록을 다시 확인하세요.");return;}lines.Add(new SupplierPurchaseLine{offerId=pair.Key,quantity=pair.Value,expectedUnitPrice=price});}
            string error;bool success=panel.Model.TryPurchaseFromSupplier(selected,lines,panel.inventory.Clock.Snapshot().day,viewCycle,out error);
            panel.ShowMessage(success?"구매 완료. 다음 날 새벽 합배송 상자로 도착합니다.":error);if(success)quantities.Clear();dirty=true;
        }
        public void UnlockNext()
        {
            if(!ready)return;
            foreach(var s in panel.Model.SupplierStates())if(!s.unlocked)
            {selected=s.supplierId;SetAccess(true,s.blocked,s.blockReason);return;}
            panel.ShowMessage("이미 모든 거래처가 등록되었습니다.");
        }
        void PickIfNeeded()
        {if(panel.Model.Supplier(selected)!=null)return;foreach(var s in panel.Model.SupplierStates())if(s.unlocked){selected=s.supplierId;return;}}
        public void BlockSelected(){if(!ready)return;PickIfNeeded();SetAccess(true,true,"진행 이벤트로 거래 중단 (테스트)");}
        public void ResumeSelected(){if(!ready)return;PickIfNeeded();SetAccess(true,false,"");}
        void SetAccess(bool unlocked,bool blocked,string reason)
        {
            string error;bool ok=panel.Model.SetSupplierAccess(selected,unlocked,blocked,reason,out error);
            panel.ShowMessage(ok?"거래처 상태를 변경했습니다.":error);dirty=true;
        }
        void Refresh()
        {
            var states=panel.Model.SupplierStates();seen.Clear();
            foreach(var s in states)if(s.unlocked)
            {
                var def=panel.Model.Suppliers.Find(s.supplierId);seen.Add(def.id);SupplierListRowView row;
                if(!suppliers.TryGetValue(def.id,out row)){row=Instantiate(supplierTemplate,supplierList);row.gameObject.SetActive(true);suppliers.Add(def.id,row);}
                row.Bind(this,def,s);row.transform.SetAsLastSibling();
            }
            removed.Clear();foreach(var pair in suppliers)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(var id in removed){suppliers[id].gameObject.SetActive(false);Destroy(suppliers[id].gameObject);suppliers.Remove(id);}
            var current=panel.Model.Supplier(selected);var definition=panel.Model.Suppliers.Find(selected);
            testSelectionLabel.text="상태 테스트 대상: "+(definition==null?"목록의 행을 눌러 선택":definition.name);
            if(!tradePage.activeSelf)return;
            if(current==null||!current.unlocked){ShowList();return;}
            if(quoteSupplier!=selected||current.cycle!=viewCycle)
            {quoteSupplier=selected;viewCycle=current.cycle;quantities.Clear();panel.ShowMessage("이틀 주기가 바뀌어 매입 목록을 갱신했습니다.");}
            titleLabel.text=definition.name;greetingLabel.text=current.blocked?current.blockReason:definition.greeting;
            portrait.sprite=FindPortrait(definition.portraitKey);portrait.enabled=portrait.sprite!=null;portrait.preserveAspect=true;
            int first=current.cycle*definition.refreshDays+1;
            cycleLabel.text=first+"~"+(first+definition.refreshDays-1)+"일차 재고 / 다음 날 새벽 배송";
            long total=0;int amount=0;seen.Clear();prices.Clear();
            foreach(var stock in current.stocks)
            {
                seen.Add(stock.offerId);prices[stock.offerId]=stock.unitPrice;int n;quantities.TryGetValue(stock.offerId,out n);
                if(n>stock.remaining){n=0;quantities[stock.offerId]=0;}
                amount+=n;total+=(long)n*stock.unitPrice;SupplierOfferRowView row;
                if(!offers.TryGetValue(stock.offerId,out row)){row=Instantiate(offerTemplate,offerList);row.gameObject.SetActive(true);offers.Add(stock.offerId,row);}
                row.Bind(this,panel.Model.Suppliers.Offer(selected,stock.offerId),stock,n,!current.blocked);row.transform.SetAsLastSibling();
            }
            removed.Clear();foreach(var pair in offers)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(var id in removed){offers[id].gameObject.SetActive(false);Destroy(offers[id].gameObject);offers.Remove(id);}
            totalLabel.text="주문 합계  "+total+" G ("+amount+"개)\n"+(total<=panel.Model.Gold?"주문 후 소지금  "+(panel.Model.Gold-total)+" G":"부족 금액  "+(total-panel.Model.Gold)+" G");
            buyButton.interactable=!current.blocked&&amount>0&&amount<=100&&total<=panel.Model.Gold;
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;panel.inventory.Loaded-=OnLoaded;}}
    }
}
