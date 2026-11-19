using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 화면의 단계만 담당합니다. 선호/예산/물건 선택은 JSON+정책, 재고/돈은 모델이 담당합니다.
    public sealed class ShopfrontUI : MonoBehaviour
    {
        public CounterTradeUI counterTrade;
        public InventoryPanel panel;public StagedCustomerInteraction customerInteraction;
        public ShopSimulation simulation;
        public WarehouseDrawer drawer;public DisplayUI displays;
        public GameObject warehouseWindow,displayWindow,priceDialog;
        public InputField displayPriceInput;
        public Text goldLabel;
        MoveRequest pendingPlacement;
        public void RefreshDialogue(){MarkDirty();}
        long shownGold=long.MinValue,shownDebt=long.MinValue;
        bool ready,dirty;
        RecommendationSession Session { get { return panel.inventory.Sales; } }
        public bool HasPendingPlacement { get { return pendingPlacement!=null; } }
        void Start()
        {
            bool commonMissing=panel==null||drawer==null||warehouseWindow==null||displayWindow==null||displays==null
                ||priceDialog==null||displayPriceInput==null||goldLabel==null||counterTrade==null;
            if(commonMissing){Debug.LogError("ShopfrontUI 참조 오류입니다. Create Latest Demo로 생성하세요.",this);enabled=false;return;}
            string error;if(!panel.inventory.TryInitialize(out error)){Debug.LogError(error,this);enabled=false;return;}
            panel.Model.Changed+=MarkDirty;Session.Changed+=SessionChanged;ready=true;
            priceDialog.SetActive(false);Refresh();
        }
        void MarkDirty(){dirty=true;}
        void SessionChanged(){MarkDirty();if(!panel.drag.IsDragging)CancelDisplayPrice();}
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
            if(customerInteraction!=null)customerInteraction.BeginVisit();
            panel.ShowMessage(customer.displayName+" 방문: 가져온 물건을 확인하세요.");return true;
        }
        public void BeginTalk()
        {
            if(!ready)return;string error;if(!Session.BeginConversation(out error)){panel.ShowMessage(error);return;}
            drawer.Open();panel.ShowMessage("물건을 카운터 위로 드래그하세요.");
        }
        public void LeaveCustomer(){if(!ready)return;Session.Leave();panel.ShowMessage("거래 취소. 손님 선택 물건의 잠금을 해제했습니다.");}
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
        void Refresh()
        {
            if(shownGold!=panel.Model.Gold||shownDebt!=panel.Model.UnpaidCosts)
            {shownGold=panel.Model.Gold;shownDebt=panel.Model.UnpaidCosts;goldLabel.text=shownGold.ToString("N0")+" G"+(shownDebt>0?" / 미납 "+shownDebt:"");}
            using(StoreProfiler.CounterRefresh.Auto())counterTrade.Render();
        }
        void OnDestroy()
        {if(ready&&panel!=null&&panel.inventory!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;Session.Changed-=SessionChanged;}}
    }
}
