using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 창 열기/닫기, 진열 가격 대화창, 손님/추천 UI만 담당합니다.
    // 실제 판매의 원자적 처리는 InventoryModel.TrySell이 담당합니다.
    public sealed class ShopfrontUI : MonoBehaviour
    {
        public InventoryPanel panel;
        public GameObject warehouseWindow,displayWindow,shelfPage1,shelfPage2,priceDialog;
        public InputField displayPriceInput;
        public Text goldLabel,customerNameLabel,dialogueLabel,totalLabel;
        public global::UnityEngine.UI.Image customerPortrait;
        public Sprite portraitSprite;
        public RectTransform offerList;
        public OfferRowView offerTemplate;
        public Button submitButton;
        [Tooltip("손님 맞이 버튼으로 등장할 테스트 손님 이름")]
        public string demoCustomerName="여행자";
        [TextArea] public string demoDialogue="북쪽 폐광으로 떠나려는데, 챙겨 갈 물건을 추천해 주실 수 있나요?";
        [Tooltip("제안 합계가 이 예산을 넘으면 손님이 거절합니다.")]
        public long demoBudget=1000;
        [Tooltip("각 제안 가격이 기준가의 이 배율을 넘으면 거절합니다. 실제 취향 AI는 추후 연결합니다.")]
        [Min(.01f)] public float acceptablePriceRatio=1.5f;
        [Tooltip("켜면 판매가가 등록된 진열 상품이 있어야 손님이 머뭅니다.")]
        public bool requireDisplayedStock=true;
        MoveRequest pendingPlacement;
        readonly List<GameObject> rows=new List<GameObject>();
        bool ready,dirty;
        RecommendationSession Session { get { return panel.inventory.Sales; } }
        public bool HasPendingPlacement { get { return pendingPlacement!=null; } }
        void Start()
        {
            if(panel==null || warehouseWindow==null || displayWindow==null || shelfPage1==null || shelfPage2==null
                || priceDialog==null || displayPriceInput==null || goldLabel==null || customerNameLabel==null
                || dialogueLabel==null || totalLabel==null || customerPortrait==null || offerList==null || offerTemplate==null || submitButton==null
                || offerTemplate.icon==null || offerTemplate.nameLabel==null || offerTemplate.priceLabel==null || offerTemplate.removeButton==null)
            {Debug.LogError("ShopfrontUI 참조를 확인하세요.",this);enabled=false;return;}
            string error;if(!panel.inventory.TryInitialize(out error)){Debug.LogError(error,this);enabled=false;return;}
            panel.Model.Changed+=MarkDirty;Session.Changed+=MarkDirty;ready=true;
            priceDialog.SetActive(false);offerTemplate.gameObject.SetActive(false);Refresh();
        }
        void MarkDirty(){dirty=true;}
        void LateUpdate(){if(ready && dirty){dirty=false;Refresh();}}
        void PrepareWindowChange(){panel.drag.Cancel(false);CancelDisplayPrice();}
        public void ToggleWarehouse(){PrepareWindowChange();warehouseWindow.SetActive(!warehouseWindow.activeSelf);}
        public void CloseWarehouse(){PrepareWindowChange();warehouseWindow.SetActive(false);}
        public void ToggleDisplay(){PrepareWindowChange();displayWindow.SetActive(!displayWindow.activeSelf);}
        public void CloseDisplay(){PrepareWindowChange();displayWindow.SetActive(false);}
        public void ShowShelf1(){PrepareWindowChange();displayWindow.SetActive(true);shelfPage1.SetActive(true);shelfPage2.SetActive(false);}
        public void ShowShelf2(){PrepareWindowChange();displayWindow.SetActive(true);shelfPage1.SetActive(false);shelfPage2.SetActive(true);}
        public void RequestDisplayPlacement(MoveRequest request)
        {
            pendingPlacement=request;ItemRecord item=panel.Model.GetItem(request.sourceContainer,request.instanceId);
            if(item==null){CancelDisplayPrice();return;}
            int initial; if(!panel.Model.TryGetOfferPrice(request.sourceContainer,request.instanceId,out initial))
                initial=panel.Catalog.ReferencePrice(item.itemId,item.quality);
            displayPriceInput.text=initial.ToString();priceDialog.SetActive(true);displayPriceInput.ActivateInputField();
            panel.ShowMessage("진열할 물건의 개당 판매가를 정하세요. 취소하면 원래 위치에 남습니다.");
        }
        public void ConfirmDisplayPrice()
        {
            if(pendingPlacement==null)return;int price;
            if(!int.TryParse(displayPriceInput.text,out price) || price<0){panel.ShowMessage("0 이상의 정수 가격을 입력하세요.");return;}
            MoveRequest request=pendingPlacement;string error;
            bool success=panel.Model.TryListOnDisplay(request,price,out error);CancelDisplayPrice();
            if(success){panel.Select(request.targetContainer,request.instanceId);panel.ShowMessage("진열과 판매가 등록 완료");}
            else panel.ShowMessage("진열 실패: "+error+" 다시 드래그하세요.");
        }
        public void CancelDisplayPrice(){pendingPlacement=null;if(priceDialog!=null)priceDialog.SetActive(false);}
        public void EnterCustomer()
        {
            if(!ready)return;string error;
            if(Session.TryEnter(demoCustomerName,demoBudget,acceptablePriceRatio,requireDisplayedStock,out error))
                panel.ShowMessage(demoCustomerName+"이(가) 카운터에 왔습니다.");
            else panel.ShowMessage(error);
            MarkDirty();
        }
        public void LeaveCustomer(){if(!ready)return;Session.Leave();panel.ShowMessage("손님을 보냈습니다.");}
        public bool Recommend(string containerId,string instanceId)
        {
            if(!ready)return false;string error;bool success=Session.TryRecommend(containerId,instanceId,out error);
            panel.ShowMessage(success?"추천 목록에 추가했습니다.":error);return success;
        }
        public void RecommendSelected()
        {
            if(string.IsNullOrEmpty(panel.SelectedInstanceId)){panel.ShowMessage("먼저 물건을 선택하세요.");return;}
            Recommend(panel.SelectedContainerId,panel.SelectedInstanceId);
        }
        public void RemoveOffer(string instanceId){if(ready)Session.Remove(instanceId);}
        public void SubmitOffer()
        {
            if(!ready)return;long revenue;string error;
            if(Session.TrySubmit(out revenue,out error))panel.ShowMessage("손님이 제안을 수락했습니다. 판매 완료 +"+revenue+" G");
            else panel.ShowMessage(error);
        }
        void Refresh()
        {
            goldLabel.text=panel.Model.Gold.ToString("N0")+" G";
            customerNameLabel.text=Session.CustomerPresent?Session.CustomerName:"손님 대기 중";
            dialogueLabel.text=Session.CustomerPresent?demoDialogue:"진열을 준비한 뒤 손님 맞이 버튼을 눌러 주세요.";
            customerPortrait.sprite=portraitSprite;customerPortrait.enabled=Session.CustomerPresent && portraitSprite!=null;
            foreach(GameObject row in rows){row.SetActive(false);Destroy(row);}rows.Clear();
            long total=0;List<PendingOffer> offers=Session.Offers();
            foreach(PendingOffer offer in offers)
            {
                total+=offer.price;OfferRowView row=Instantiate(offerTemplate,offerList);row.gameObject.SetActive(true);
                row.Bind(this,offer);rows.Add(row.gameObject);
            }
            totalLabel.text="제안 합계  "+total+" G";submitButton.interactable=Session.CustomerPresent && offers.Count>0;
        }
        void OnDestroy()
        {
            if(ready && panel!=null && panel.inventory!=null && panel.inventory.Model!=null)
            {panel.Model.Changed-=MarkDirty;Session.Changed-=MarkDirty;}
        }
    }
}
