using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 화면의 단계만 담당합니다. 선호/예산/물건 선택은 JSON+정책, 재고/돈은 모델이 담당합니다.
    public sealed class ShopfrontUI : MonoBehaviour
    {
        public InventoryPanel panel;
        public ShopSimulation simulation;
        public WarehouseDrawer drawer;public DisplayUI displays;
        public GameObject warehouseWindow,displayWindow,priceDialog;
        public GameObject customerWindow,checkoutPage,recommendationPage,dialogueWindow;
        public InputField displayPriceInput;
        public Text goldLabel,customerNameLabel,dialogueLabel,totalLabel,purchaseTotalLabel,summaryLabel;
        public global::UnityEngine.UI.Image customerPortrait;
        public Sprite portraitSprite; // JSON portraitKey Sprite가 없을 때 사용할 기본 이미지
        public RectTransform offerList,purchaseList;
        public OfferRowView offerTemplate;
        public Button submitButton;
        MoveRequest pendingPlacement;
        CustomerDefinition activeCustomer;
        public ICustomerDialoguePolicy DialoguePolicy {get;set;} = new DefaultCustomerDialoguePolicy();
        public void RefreshDialogue(){MarkDirty();}
        readonly Dictionary<string,OfferRowView> rows=new Dictionary<string,OfferRowView>();
        readonly HashSet<string> seenRows=new HashSet<string>();readonly List<string> removedRows=new List<string>();
        Sprite activePortrait;long shownGold=long.MinValue,shownDebt=long.MinValue;
        bool ready,dirty;
        RecommendationSession Session { get { return panel.inventory.Sales; } }
        public bool HasPendingPlacement { get { return pendingPlacement!=null; } }
        void Start()
        {
            if(panel==null||drawer==null||warehouseWindow==null||displayWindow==null||displays==null
                ||priceDialog==null||displayPriceInput==null||goldLabel==null||customerNameLabel==null||dialogueLabel==null
                ||totalLabel==null||purchaseTotalLabel==null||summaryLabel==null||customerPortrait==null||offerList==null
                ||purchaseList==null||offerTemplate==null||submitButton==null||customerWindow==null||checkoutPage==null
                ||recommendationPage==null||dialogueWindow==null||offerTemplate.icon==null||offerTemplate.removeButton==null
                ||offerTemplate.nameLabel==null||offerTemplate.priceLabel==null)
            {Debug.LogError("ShopfrontUI 참조 오류입니다. 새 Create Canvas Demo로 생성하세요.",this);enabled=false;return;}
            string error;if(!panel.inventory.TryInitialize(out error)){Debug.LogError(error,this);enabled=false;return;}
            panel.Model.Changed+=MarkDirty;Session.Changed+=SessionChanged;ready=true;
            priceDialog.SetActive(false);offerTemplate.gameObject.SetActive(false);Refresh();
        }
        void MarkDirty(){dirty=true;}
        void SessionChanged(){MarkDirty();panel.drag.Cancel(false);CancelDisplayPrice();}
        void LateUpdate(){if(ready&&dirty){dirty=false;Refresh();}}
        void PrepareWindowChange(){panel.drag.Cancel(false);CancelDisplayPrice();}
        public void ToggleWarehouse(){PrepareWindowChange();drawer.Toggle();}
        public void CloseWarehouse(){PrepareWindowChange();drawer.Close();}
        public void ToggleDisplay(){PrepareWindowChange();displays.OpenSelected();}
        public void CloseDisplay(){PrepareWindowChange();displays.CloseSelected();}
        public void ShowShelf1(){displays.SelectIndex(0);OpenDisplay();}
        public void ShowShelf2(){displays.SelectIndex(1);OpenDisplay();}
        public void OpenDisplay(){PrepareWindowChange();displays.OpenSelected();}
        public void OpenTogether(){PrepareWindowChange();drawer.Open();displays.OpenSelected();}
        public void RequestDisplayPlacement(MoveRequest request)
        {
            if(panel.Model.IsReserved(request.instanceId)){panel.ShowMessage("손님 선택 상품은 변경할 수 없습니다.");return;}
            pendingPlacement=request;ItemRecord item=panel.Model.GetItem(request.sourceContainer,request.instanceId);
            if(item==null){CancelDisplayPrice();return;}
            int initial;if(!panel.Model.TryGetOfferPrice(request.sourceContainer,request.instanceId,out initial))
                initial=panel.Catalog.ReferencePrice(item.itemId,item.quality);
            displayPriceInput.text=initial.ToString();priceDialog.SetActive(true);displayPriceInput.ActivateInputField();
            panel.ShowMessage("진열 판매가를 정하세요. 취소하면 원래 위치에 남습니다.");
        }
        public void ConfirmDisplayPrice()
        {
            if(pendingPlacement==null)return;int price;
            if(!int.TryParse(displayPriceInput.text,out price)||price<0){panel.ShowMessage("0 이상의 정수 가격을 입력하세요.");return;}
            MoveRequest request=pendingPlacement;string error;
            bool success=panel.Model.TryListOnDisplay(request,price,out error);CancelDisplayPrice();
            if(success){panel.Select(request.targetContainer,request.instanceId);panel.ShowMessage("진열 완료");}
            else panel.ShowMessage("진열 실패: "+error);
        }
        public void CancelDisplayPrice(){pendingPlacement=null;if(priceDialog!=null)priceDialog.SetActive(false);}
        public void EnterCustomer(){if(ready&&simulation!=null)simulation.TryVisit(true);}
        public bool EnterProfile(CustomerDefinition customer,bool showFailure)
        {
            if(!ready)return false;string error;
            if(!Session.TryEnterShopping(customer,out error)){if(showFailure)panel.ShowMessage(error);return false;}
            activeCustomer=customer.Copy();activePortrait=string.IsNullOrEmpty(customer.portraitKey)?null:Resources.Load<Sprite>("CustomerPortraits/"+customer.portraitKey);panel.ShowMessage(customer.displayName+" 방문: 가져온 물건을 확인하세요.");return true;
        }
        public void BeginTalk()
        {
            if(!ready)return;string error;if(!Session.BeginConversation(out error)){panel.ShowMessage(error);return;}
            drawer.Open();panel.ShowMessage("물건을 오른쪽 추천 영역으로 드래그하세요.");
        }
        public void LeaveCustomer(){if(!ready)return;Session.Leave();activeCustomer=null;panel.ShowMessage("거래 취소. 손님 선택 물건의 잠금을 해제했습니다.");}
        // 추천은 드롭으로만 호출합니다. 선택한 물건 추천 버튼/메서드는 제거했습니다.
        public bool Recommend(string containerId,string instanceId)
        {
            if(!ready)return false;string error;bool success=Session.TryRecommend(containerId,instanceId,out error);
            panel.ShowMessage(success?"추천 목록에 추가했습니다.":error);return success;
        }
        public void RemoveOffer(string uid){if(ready)Session.Remove(uid);}
        public void SubmitOffer(){CompleteSale(false);}
        public void SellPurchases(){CompleteSale(true);}
        void CompleteSale(bool purchasesOnly)
        {
            if(!ready)return;long revenue;string error;
            bool success=purchasesOnly?Session.TrySubmitPurchasesOnly(out revenue,out error):Session.TrySubmit(out revenue,out error);
            panel.ShowMessage(success?"판매 완료 +"+revenue+" G":error);
        }
        static void Visible(GameObject go,bool value){if(go.activeSelf!=value)go.SetActive(value);}
        void AddRows(List<PendingOffer> offers,RectTransform parent)
        {
            foreach(var offer in offers)
            {
                seenRows.Add(offer.instanceId);OfferRowView row;
                if(!rows.TryGetValue(offer.instanceId,out row))
                {row=Instantiate(offerTemplate,parent);row.gameObject.SetActive(true);rows.Add(offer.instanceId,row);}
                if(row.transform.parent!=parent)row.transform.SetParent(parent,false);
                row.Bind(this,offer);
            }
        }
        void RemoveUnusedRows()
        {
            removedRows.Clear();foreach(var pair in rows)if(!seenRows.Contains(pair.Key))removedRows.Add(pair.Key);
            foreach(var id in removedRows){rows[id].gameObject.SetActive(false);Destroy(rows[id].gameObject);rows.Remove(id);}
        }
        void Refresh()
        {
            if(shownGold!=panel.Model.Gold||shownDebt!=panel.Model.UnpaidCosts)
            {shownGold=panel.Model.Gold;shownDebt=panel.Model.UnpaidCosts;goldLabel.text=shownGold.ToString("N0")+" G"+(shownDebt>0?" / 미납 "+shownDebt:"");}
            bool present=Session.CustomerPresent;bool talking=present&&Session.ConversationStarted;
            Visible(customerWindow,present);Visible(dialogueWindow,present);
            Visible(checkoutPage,present&&!talking);Visible(recommendationPage,talking);seenRows.Clear();
            customerPortrait.sprite=activePortrait!=null?activePortrait:portraitSprite;customerPortrait.enabled=present&&customerPortrait.sprite!=null;
            if(!present){RemoveUnusedRows();return;}
            customerNameLabel.text=Session.CustomerName;
            dialogueLabel.text=activeCustomer==null?"물건을 확인해 주세요.":DialoguePolicy.Text(activeCustomer,talking,panel.inventory.Clock.Snapshot());
            var purchases=Session.Purchases();var offers=Session.Offers();long baseTotal=0,recommendTotal=0;
            foreach(var p in purchases)baseTotal+=p.price;foreach(var o in offers)recommendTotal+=o.price;
            if(!talking)AddRows(purchases,purchaseList);else AddRows(offers,offerList);RemoveUnusedRows();
            purchaseTotalLabel.text="합계 "+baseTotal+" G";
            summaryLabel.text="가져온 물건 "+purchases.Count+"개 / "+baseTotal+" G";
            totalLabel.text="추천 "+recommendTotal+" G / 총 결제 "+(baseTotal+recommendTotal)+" G";
            submitButton.interactable=present&&talking&&(purchases.Count+offers.Count)>0;
        }
        void OnDestroy()
        {if(ready&&panel!=null&&panel.inventory!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;Session.Changed-=SessionChanged;}}
    }
}
