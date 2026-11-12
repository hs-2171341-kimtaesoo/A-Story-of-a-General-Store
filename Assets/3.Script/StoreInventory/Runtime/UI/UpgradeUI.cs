using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class UpgradeUI : MonoBehaviour
    {
        public InventoryPanel panel;public WarehouseUI warehouse;public DisplayUI display;
        public GameObject window;public Text storageInfo,storageActionLabel,typeLabel,displayInfo;
        public Button storageActionButton,addDisplayButton;
        [Range(1,10)] public int newWarehouseRows=8;
        List<string> types;int typeIndex;bool ready,dirty;
        void Start()
        {
            string error;if(panel==null||warehouse==null||display==null||window==null||storageInfo==null||storageActionLabel==null
                ||typeLabel==null||displayInfo==null||storageActionButton==null||addDisplayButton==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("UpgradeUI 참조를 확인하세요.",this);enabled=false;return;}
            types=panel.Model.Displays.TypeIds();
            panel.Model.Changed+=MarkDirty;warehouse.SelectionChanged+=MarkDirty;ready=true;window.SetActive(false);
        }
        void MarkDirty(){dirty=true;}
        public void Toggle()
        {if(!ready)return;panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();window.SetActive(!window.activeSelf);dirty=true;}
        public void Close(){window.SetActive(false);}
        public void SelectType(string id){int index=types.IndexOf(id);if(index>=0){typeIndex=index;dirty=true;}}
        public void PreviousType(){typeIndex=(typeIndex+types.Count-1)%types.Count;dirty=true;}
        public void NextType(){typeIndex=(typeIndex+1)%types.Count;dirty=true;}
        // 현재 선택한 창이 아니라 모델이 정한 마지막 상자를 확장합니다.
        public void UpgradeStorage()
        {
            if(!ready)return;string id,error;
            if(panel.Model.TryUpgradeStorage(panel.Model.StorageUpgradeTarget(),newWarehouseRows,out id,out error))
            {warehouse.Select(id);panel.ShowMessage("상자 업그레이드를 적용했습니다.");}
            else panel.ShowMessage(error);
        }
        // UI는 금액을 직접 차감하지 않습니다. 모델에서 검증과 추가를 원자적으로 처리합니다.
        public void AddDisplay()
        {
            if(!ready)return;string id,error;
            if(panel.Model.TryBuyDisplay(types[typeIndex],out id,out error))
            {display.Select(id);panel.ShowMessage("가구를 구매했습니다. 진열장 탭에서 바로 열 수 있습니다.");}
            else panel.ShowMessage(error);
        }
        // 창이 보이며 변경된 경우에만 목록의 상세 정보와 버튼 상태를 갱신합니다.
        void LateUpdate()
        {
            if(!ready||!dirty||!window.activeSelf)return;dirty=false;
            string target=panel.Model.StorageUpgradeTarget();var c=panel.Model.GetContainer(target);var action=panel.Model.StorageAction(target);
            storageInfo.text="확장 대상: "+target+" / "+(c.rows*8)+" / 80칸";
            storageActionLabel.text=action==StorageUpgradeAction.Expand?"공간 확장 (+8칸)":"상자 추가";
            storageActionButton.interactable=action==StorageUpgradeAction.Expand||panel.Model.WarehouseIds().Count<30;
            string type=types[typeIndex];var t=panel.Model.Displays.Type(type);typeLabel.text=t.title;string error;
            bool can=panel.Model.CanBuyDisplay(type,out error);addDisplayButton.interactable=can;
            displayInfo.text=t.columns+"×"+t.rows+" · "+(t.columns*t.rows)+"칸 / 구매가 "+t.purchasePrice+" G\n소지금 "+panel.Model.Gold+" G · "+(can?"구매 즉시 추가":error)+"\n"
                +(t.allowedItemTypes==null||t.allowedItemTypes.Length==0?"모든 물품 종류 허용":"허용: "+string.Join(", ",t.allowedItemTypes));
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;warehouse.SelectionChanged-=MarkDirty;}}
    }
}
