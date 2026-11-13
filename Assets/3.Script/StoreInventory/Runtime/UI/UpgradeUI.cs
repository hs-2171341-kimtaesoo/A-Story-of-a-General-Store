using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 가구 구매와 상자 확장은 별도 화면입니다. 실제 변경은 모델에서 한 번에 확정합니다.
    public sealed class UpgradeUI:MonoBehaviour
    {
        public InventoryPanel panel;public WarehouseUI warehouse;public DisplayUI display;public FurnitureArtCatalog art;
        public GameObject window,storageWindow;public RectTransform furnitureList;public FurnitureCardView cardTemplate;
        public global::UnityEngine.UI.Image furniturePreview,storageImage;
        public Text typeLabel,displayInfo,storageInfo,storageNumbers,storageActionLabel;
        public Button addDisplayButton,storageActionButton;public StoragePreviewGraphic storagePreview;
        [Min(0)]public int expansionCost=400,newBoxCost=600;
        [Range(1,10)]public int newWarehouseRows=5;
        readonly Dictionary<string,FurnitureCardView> cards=new Dictionary<string,FurnitureCardView>();
        long storageQuoteRevision=-1;string selectedType;bool ready,dirty;
        void Start(){string error;if(panel==null||warehouse==null||display==null||window==null||storageWindow==null||art==null||furnitureList==null||cardTemplate==null||typeLabel==null||displayInfo==null||storageInfo==null||storageNumbers==null||storageActionLabel==null||addDisplayButton==null||storageActionButton==null||storagePreview==null||furniturePreview==null||!panel.inventory.TryInitialize(out error)){Debug.LogError("새 메뉴 버전 씬을 생성하고 UpgradeUI 참조를 확인하세요.",this);enabled=false;return;}panel.Model.Changed+=MarkDirty;ready=true;window.SetActive(false);storageWindow.SetActive(false);}
        void MarkDirty(){dirty=true;}
        public void Toggle(){OpenFurniture();}
        public void OpenFurniture(){if(!ready)return;panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();storageWindow.SetActive(false);window.SetActive(true);dirty=true;}
        public void OpenStorage(){if(!ready)return;panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();window.SetActive(false);storageWindow.SetActive(true);dirty=true;}
        public void Close(){window.SetActive(false);storageWindow.SetActive(false);}
        public void SelectType(string id){selectedType=id;dirty=true;}
        public void PreviousType(){}public void NextType(){} // 이전 버튼 연결 호환. 새 화면은 목록 선택입니다.
        public void UpgradeStorage()
        {
            if(!ready)return;
            if(storageQuoteRevision!=panel.Model.Revision){panel.ShowMessage("변경된 비용과 공간을 확인한 뒤 다시 눌러주세요.");dirty=true;return;}
            storageActionButton.interactable=false;string id,error;
            if(panel.Model.TryPurchaseStorageUpgrade(expansionCost,newBoxCost,out id,out error,newWarehouseRows)){warehouse.Select(id);panel.ShowMessage("상자 업그레이드 완료");}
            else panel.ShowMessage(error);dirty=true;
        }
        public void AddDisplay()
        {
            if(!ready||selectedType==null)return;string id,error;
            if(panel.Model.TryBuyDisplay(selectedType,out id,out error)){display.Select(id);selectedType=null;panel.ShowMessage("구매 완료. 배경 가구와 관리창에서 열 수 있습니다.");}
            else panel.ShowMessage(error);dirty=true;
        }
        void RefreshFurniture()
        {
            var ids=panel.Model.AvailableFurnitureTypes();if(!ids.Contains(selectedType))selectedType=ids.Count>0?ids[0]:null;
            foreach(var row in cards.Values)row.gameObject.SetActive(false);int n=0;
            foreach(string id in ids)
            {
                FurnitureCardView row;if(!cards.TryGetValue(id,out row)){row=Instantiate(cardTemplate,furnitureList);cards.Add(id,row);}row.gameObject.SetActive(true);
                var t=panel.Model.Displays.Type(id);var a=art.Find(id);row.Bind(id,SelectType,t.title,t.columns+"×"+t.rows+" · "+(t.columns*t.rows)+"칸",t.purchasePrice+" G",a==null?null:a.thumbnail,id==selectedType);
                var r=(RectTransform)row.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(0,-n++*112);r.sizeDelta=new Vector2(furnitureList.rect.width,104);
            }
            furnitureList.sizeDelta=new Vector2(furnitureList.sizeDelta.x,n*112);
            if(selectedType==null){typeLabel.text="구매할 가구가 없습니다.";displayInfo.text="소유한 가구는 목록에서 숨깁니다.";furniturePreview.enabled=false;addDisplayButton.interactable=false;return;}
            var selected=panel.Model.Displays.Type(selectedType);var image=art.Find(selectedType);furniturePreview.sprite=image==null?null:image.thumbnail;furniturePreview.enabled=furniturePreview.sprite!=null;furniturePreview.preserveAspect=true;
            typeLabel.text=selected.title;string error;addDisplayButton.interactable=panel.Model.CanBuyDisplay(selectedType,out error);
            displayInfo.text=(selected.columns*selected.rows)+"칸 · "+selected.purchasePrice+" G\n"+(panel.Model.Gold>=selected.purchasePrice?"구매 후 소지금 "+(panel.Model.Gold-selected.purchasePrice)+" G":"부족 금액 "+(selected.purchasePrice-panel.Model.Gold)+" G")+"\n"+(image==null?"구매 즉시 사용 가능합니다.":image.description)+(addDisplayButton.interactable?"":"\n"+error);
        }
        void RefreshStorage()
        {
            var q=panel.Model.QuoteStorageUpgrade(expansionCost,newBoxCost,newWarehouseRows);storageQuoteRevision=panel.Model.Revision;
            storageNumbers.text=q.addsBox?"새 상자 "+q.afterCells+"칸 추가":q.beforeCells+"칸 → "+q.afterCells+"칸  (+"+q.addedCells+"칸)";
            storageInfo.text="대상: 상자 "+panel.Model.WarehouseIds().Count+"\n현재 남은 공간 "+q.freeBefore+"칸\n"+(q.addsBox?"새 상자 공간 ":"확장 후 남은 공간 ")+q.freeAfter+"칸\n비용 "+q.cost+" G\n"+(q.goldAfter>=0?"구매 후 소지금 "+q.goldAfter+" G":"소지금 부족")+"\n"+(q.allowed?"즉시 적용 · 물건 위치 유지":q.error);
            storageActionLabel.text=q.cost+" G로 "+(q.addsBox?"상자 추가":"확장");storageActionButton.interactable=q.allowed;
            storagePreview.Configure(q.addsBox?0:q.beforeCells,q.afterCells);
        }
        void LateUpdate(){if(!ready||!dirty||(!window.activeSelf&&!storageWindow.activeSelf))return;dirty=false;if(window.activeSelf)RefreshFurniture();if(storageWindow.activeSelf)RefreshStorage();}
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null)panel.Model.Changed-=MarkDirty;}
    }
}
