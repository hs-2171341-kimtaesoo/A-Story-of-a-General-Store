#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using GeneralStore.Inventory;
public static class CompactDemoUnityChecks
{
    // 실제 Unity 초기화/이벤트 계층 검사입니다. 이 제작 환경에서는 실행하지 못했습니다.
    public static int Run()
    {
        var go=new GameObject("Compact Demo Initialization Check");go.SetActive(false);GameObject ui=null,tempEvents=null;
        try
        {
            var runtime=go.AddComponent<InventoryRuntime>();runtime.initialWarehouseCount=2;runtime.initialGold=10000;ItemTableTools.Configure(runtime);
            runtime.initialFurnitureForDemo=new[]{"mannequin","table","weapon_rack"};string error;
            if(!runtime.TryInitialize(out error))throw new Exception(error);
            var ids=runtime.Model.WarehouseIds();
            if(ids.Count!=2||ids[0]!="warehouse"||ids[1]!="warehouse_2")throw new Exception("초기 상자2개 검사 실패");
            foreach(string id in ids){var box=runtime.Model.GetContainer(id);if(box.columns!=8||box.rows!=7)throw new Exception("상자8×7 검사 실패");}
            if(runtime.Model.Gold!=10000)throw new Exception("기본 상자 추가로 소지금이 차감되었습니다.");
            if(runtime.Model.DisplayIds().Count!=3)throw new Exception("초기 진열장3개 검사 실패");
            ui=new GameObject("Compact Icon Input Check",typeof(RectTransform),typeof(Canvas));
            var panel=ui.AddComponent<InventoryPanel>();panel.inventory=runtime;panel.canvas=ui.GetComponent<Canvas>();
            var gridGo=new GameObject("Grid",typeof(RectTransform));gridGo.transform.SetParent(ui.transform,false);var grid=gridGo.AddComponent<InventoryGridView>();
            var itemGo=new GameObject("Item",typeof(RectTransform),typeof(global::UnityEngine.UI.Image),typeof(CanvasGroup));itemGo.transform.SetParent(gridGo.transform,false);
            var view=itemGo.AddComponent<InventoryItemView>();var iconGo=new GameObject("Icon",typeof(RectTransform));iconGo.transform.SetParent(itemGo.transform,false);view.icon=iconGo.AddComponent<InventoryIconGraphic>();
            var labelGo=new GameObject("Name",typeof(RectTransform));labelGo.transform.SetParent(itemGo.transform,false);view.nameLabel=labelGo.AddComponent<global::UnityEngine.UI.Text>();view.nameLabel.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");view.nameLabel.raycastTarget=false;
            var record=runtime.Model.GetContainer("warehouse").items[0];view.Bind(panel,grid,record);
            if(itemGo.GetComponent<global::UnityEngine.UI.Image>().raycastTarget||!view.icon.raycastTarget)throw new Exception("아이콘 직접 입력 설정 실패");
            if(ExecuteEvents.GetEventHandler<IPointerClickHandler>(iconGo)!=itemGo||ExecuteEvents.GetEventHandler<IBeginDragHandler>(iconGo)!=itemGo)throw new Exception("아이콘에서 물건 컴포넌트로 이벤트 전달 실패");
            var events=EventSystem.current;if(events==null){tempEvents=new GameObject("Temporary EventSystem",typeof(EventSystem));events=tempEvents.GetComponent<EventSystem>();}
            var pointer=new PointerEventData(events){button=PointerEventData.InputButton.Left};ExecuteEvents.ExecuteHierarchy(iconGo,pointer,ExecuteEvents.pointerClickHandler);
            if(panel.SelectedInstanceId!=record.instanceId)throw new Exception("아이콘 클릭 선택 실패");
            return 8;
        }
        finally
        {
            if(ui!=null)global::UnityEngine.Object.DestroyImmediate(ui);
            if(tempEvents!=null)global::UnityEngine.Object.DestroyImmediate(tempEvents);
            global::UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
#endif
