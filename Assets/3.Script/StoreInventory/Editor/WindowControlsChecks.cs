#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class WindowControlsChecks
{
    static int checks;
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
    static void Call(object target,string name){target.GetType().GetMethod(name,Private).Invoke(target,null);}
    static void Set(object target,string name,object value){target.GetType().GetField(name,Private).SetValue(target,value);}
    static bool Close(Vector2 a,Vector2 b){return (a-b).sqrMagnitude<.01f;}
    public static int Run()
    {
        checks=0;HotkeysAndClosing();StorageDragging();MatchingTiming();Migration();return checks;
    }
    static void Finish(RightSideMenu menu,ManagementWindowMotion[] motions)
    {for(int step=0;step<8;step++){foreach(var motion in motions)motion.Advance(1);if(menu.IsSwitching)Call(menu,"ContinueWindowSwitch");}}
    static void HotkeysAndClosing()
    {
        var root=new GameObject("Shortcut Check",typeof(RectTransform));GameObject events=null;
        var previous=EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
        try
        {
            if(EventSystem.current==null)events=new GameObject("Shortcut Check Events",typeof(EventSystem));
            EventSystem.current.SetSelectedGameObject(null);
            ((RectTransform)root.transform).sizeDelta=new Vector2(1920,1080);
            var panel=root.AddComponent<InventoryPanel>();panel.drag=root.AddComponent<InventoryDragHandler>();panel.shop=root.AddComponent<ShopfrontUI>();
            panel.messageLabel=InventorySceneBuilder.Label("Message",root.transform,0,0,100,30,"",14);
            var drawerImage=InventorySceneBuilder.Box("Warehouse",root.transform,20,20,200,200,Color.white);
            var drawer=drawerImage.gameObject.AddComponent<WarehouseDrawer>();drawer.panel=panel;drawer.window=drawerImage.rectTransform;drawer.bounds=(RectTransform)root.transform;drawer.body=InventorySceneBuilder.Rect("Body",drawer.transform,0,0,200,200);
            var menuRoot=InventorySceneBuilder.Rect("Menu",root.transform,0,0,84,700);var menu=menuRoot.gameObject.AddComponent<RightSideMenu>();menu.panel=panel;menu.useArtwork=false;
            menu.content=SideMenuSceneBuilder.List(menuRoot,"Viewport",0,0,84,700);
            var templateImage=InventorySceneBuilder.Box("Button Template",root.transform,0,0,82,86,Color.white);var template=templateImage.gameObject.AddComponent<RightMenuButtonView>();
            template.background=templateImage;template.button=templateImage.gameObject.AddComponent<Button>();template.button.targetGraphic=templateImage;
            template.icon=InventorySceneBuilder.IconSlot("Icon",template.transform);template.label=InventorySceneBuilder.Label("Name",template.transform,0,0,80,32,"",14);template.gameObject.SetActive(false);menu.template=template;
            var shortcuts=root.AddComponent<StoreHotkeys>();shortcuts.panel=panel;shortcuts.menu=menu;shortcuts.drawer=drawer;
            var keys=new[]{KeyCode.Q,KeyCode.W,KeyCode.E,KeyCode.R,KeyCode.A,KeyCode.S};var ids=new[]{"displays","furniture","storage","suppliers","workshops","temporary_storage"};
            var windows=new GameObject[6];var motions=new ManagementWindowMotion[6];var opens=new int[6];
            for(int n=0;n<keys.Length;n++)
            {
                int index=n;windows[n]=InventorySceneBuilder.Rect(ids[n],root.transform,0,0,1920,1080).gameObject;
                var frame=InventorySceneBuilder.Rect("Panel Frame",windows[n].transform,160,90,560,860);
                motions[n]=ManagementWindowMotion.Ensure(windows[n],frame,n==4);windows[n].SetActive(false);
                var entry=new ShopMenuEntry{id=ids[n],window=windows[n],visible=n!=5};entry.onOpen.AddListener(()=>{opens[index]++;windows[index].SetActive(true);motions[index].SendMessage("OnEnable");});menu.entries.Add(entry);
                Check(StoreHotkeys.MenuId(keys[n])==ids[n],"단축키 매핑 오류: "+keys[n]);
            }
            Call(menu,"Start");
            Check(menu.content.GetComponentsInChildren<RightMenuButtonView>().Length==5,"빈 임시 저장고의 메뉴가 표시됩니다.");
            menu.SetVisible("temporary_storage",true);Call(menu,"Render");
            Check(menu.entries.Count==6&&menu.content.GetComponentsInChildren<RightMenuButtonView>().Length==6,"임시 저장고 메뉴 추가/중복 방지 실패");
            menu.SetVisible("temporary_storage",false);Call(menu,"Render");
            Check(menu.content.GetComponentsInChildren<RightMenuButtonView>().Length==5,"임시 저장고가 비었을 때 메뉴 숨김 실패");
            var runtime=root.AddComponent<InventoryRuntime>();runtime.initialWarehouseCount=2;ItemTableTools.Configure(runtime);panel.inventory=runtime;
            string error;if(!runtime.TryInitialize(out error))throw new InvalidOperationException(error);
            var delivery=root.AddComponent<DeliveryUI>();delivery.panel=panel;delivery.managementMenu=menu;delivery.window=(RectTransform)windows[5].transform;
            delivery.doorButton=InventorySceneBuilder.ButtonBox("Door",root.transform,0,0,40,40,null);delivery.badge=InventorySceneBuilder.Label("Badge",root.transform,0,0,80,30,"",14);
            Call(delivery,"Synchronize");Check(!menu.entries[5].visible,"빈 실제 임시 저장고가 메뉴에 표시됩니다.");
            var original=runtime.Model.Snapshot();var withStock=original.Copy();var source=withStock.containers.Find(c=>c.containerId=="warehouse");var stored=source.items[0];source.items.RemoveAt(0);
            stored.x=stored.y=0;var temporary=withStock.containers.Find(c=>c.containerId==InventoryModel.TemporaryStorageId);temporary.items.Add(stored);temporary.rows=Mathf.Max(temporary.rows,runtime.Model.Height(stored));
            if(!runtime.Model.TryRestore(withStock,out error))throw new InvalidOperationException(error);Call(delivery,"Synchronize");
            Check(menu.entries[5].visible&&runtime.Model.TemporaryItemCount==1,"임시 저장고에 물건이 들어온 뒤 메뉴 표시 실패");
            if(!runtime.Model.TryRestore(original,out error))throw new InvalidOperationException(error);Call(delivery,"Synchronize");
            Check(!menu.entries[5].visible&&menu.entries.Count==6,"임시 저장고 비우기/불러오기 후 목록 숨김·중복 방지 실패");
            Check(shortcuts.HandleKey(KeyCode.Space)&&drawer.IsOpen,"Space 상자 열기 실패");
            Check(shortcuts.HandleKey(KeyCode.Space)&&!drawer.IsOpen,"Space 상자 닫기 실패");
            Check(!shortcuts.HandleKey(KeyCode.S),"빈 임시 저장고가 S로 열렸습니다.");
            for(int n=0;n<keys.Length;n++)
            {
                if(keys[n]==KeyCode.S){if(!runtime.Model.TryRestore(withStock,out error))throw new InvalidOperationException(error);Call(delivery,"Synchronize");}
                Check(shortcuts.HandleKey(keys[n]),"단축키 열기 요청 실패: "+keys[n]);Finish(menu,motions);
                for(int other=0;other<windows.Length;other++)Check(windows[other].activeSelf==(other==n),"단축키 전환 후 창이 겹칩니다.");
                Check(motions[n].fromBelow==(n==4),"제작 의뢰 퇴장 방향이 아래가 아닙니다.");
                ManagementWindowMotion.CloseWindow(windows[n]);
                Check(windows[n].activeSelf&&!motions[n].IsClosed,"닫기 버튼에서 즉시 사라졌습니다.");
                motions[n].Advance(.1f);Call(menu,"ContinueWindowSwitch");
                Check(windows[n].activeSelf&&!motions[n].frame.GetComponent<CanvasGroup>().blocksRaycasts,"퇴장 대기/입력 차단 실패");
                Finish(menu,motions);Check(!windows[n].activeSelf,"퇴장 완료 후 창 닫기 실패");
            }
            shortcuts.HandleKey(KeyCode.Q);Finish(menu,motions);int before=opens[1];
            shortcuts.HandleKey(KeyCode.W);motions[0].Advance(.1f);Call(menu,"ContinueWindowSwitch");
            Check(windows[0].activeSelf&&!windows[1].activeSelf,"Q→W에서 기존 창 퇴장 전에 구매창이 열렸습니다.");
            shortcuts.HandleKey(KeyCode.E);Finish(menu,motions);
            Check(windows[2].activeSelf&&!windows[0].activeSelf&&!windows[1].activeSelf&&opens[1]==before,"빠른 Q→W→E에서 마지막 요청만 열기 실패");
            shortcuts.HandleKey(KeyCode.E);Finish(menu,motions);Check(!windows[2].activeSelf,"같은 단축키로 닫기 실패");
            Set(panel.drag,"active",true);
            Check(!shortcuts.HandleKey(KeyCode.R)&&!shortcuts.HandleKey(KeyCode.Space),"아이템 드래그 중 회전 R/드래그를 메뉴 단축키가 가로챕니다.");
            Set(panel.drag,"active",false);
            menu.SetUnlocked("workshops",false);Check(!shortcuts.HandleKey(KeyCode.A),"잠긴 제작 의뢰를 단축키로 우회했습니다.");
            Check(!shortcuts.HandleKey(KeyCode.Z),"등록하지 않은 키가 동작합니다.");
            var input=InventorySceneBuilder.Box("Input",root.transform,0,0,200,40,Color.white).gameObject.AddComponent<InputField>();
            input.textComponent=InventorySceneBuilder.Label("Text",input.transform,0,0,190,36,"",18);
            typeof(InputField).GetField("m_AllowInput",Private).SetValue(input,true);
            Check(StoreHotkeys.IsTyping(input.gameObject),"가격/문자 입력 중 단축키 차단 판별 실패");
            EventSystem.current.SetSelectedGameObject(input.gameObject);
            Check(!shortcuts.HandleKey(KeyCode.Q)&&!shortcuts.HandleKey(KeyCode.Space),"입력 필드에서 문자/공백이 창 단축키로 처리됩니다.");
            typeof(InputField).GetField("m_AllowInput",Private).SetValue(input,false);
            EventSystem.current.SetSelectedGameObject(null);
        }
        finally{if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(previous);Object.DestroyImmediate(root);if(events!=null)Object.DestroyImmediate(events);}
    }
    static void StorageDragging()
    {
        var root=new GameObject("Storage Drag Check",typeof(RectTransform),typeof(Canvas));GameObject events=null;
        try
        {
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var bounds=(RectTransform)root.transform;
            var panel=root.AddComponent<InventoryPanel>();panel.canvas=root.GetComponent<Canvas>();panel.drag=root.AddComponent<InventoryDragHandler>();panel.shop=root.AddComponent<ShopfrontUI>();
            var layer=InventorySceneBuilder.Rect("Edge Layer",bounds,0,0,1920,1080);
            var floating=InventorySceneBuilder.Rect("Floating Layer",bounds,0,0,1920,1080);
            var behind=InventorySceneBuilder.Box("Other Display",floating,300,200,300,300,Color.white);behind.gameObject.AddComponent<DisplayWindowView>();var behindInput=behind.gameObject.AddComponent<CanvasGroup>();
            var image=InventorySceneBuilder.Box("Temporary Storage",layer,100,120,300,200,Color.white);
            var delivery=root.AddComponent<DeliveryUI>();delivery.panel=panel;delivery.window=image.rectTransform;
            var drag=image.gameObject.AddComponent<TemporaryStorageWindowDrag>();drag.panel=panel;drag.delivery=delivery;drag.window=image.rectTransform;drag.bounds=layer;delivery.windowDrag=drag;
            var motion=ManagementWindowMotion.Ensure(image.gameObject,image.rectTransform);motion.Advance(1);
            if(EventSystem.current==null)events=new GameObject("Drag Check Events",typeof(EventSystem));
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerPressRaycast=new RaycastResult{gameObject=image.gameObject}};
            pointer.position=RectTransformUtility.WorldToScreenPoint(null,layer.TransformPoint(new Vector3(110,-130,0)));
            Check(drag.CanDrag(pointer),"임시 저장고 제목/배경 드래그 시작 실패");
            var viewport=InventorySceneBuilder.Box("Viewport",image.transform,10,50,280,130,Color.white);viewport.gameObject.AddComponent<ScrollRect>();
            pointer.pointerPressRaycast=new RaycastResult{gameObject=viewport.gameObject};Check(!drag.CanDrag(pointer),"내부 스크롤을 창 이동이 가로챕니다.");
            var button=InventorySceneBuilder.ButtonBox("닫기",image.transform,200,4,80,36,null);
            pointer.pointerPressRaycast=new RaycastResult{gameObject=button.gameObject};Check(!drag.CanDrag(pointer),"닫기 버튼을 창 이동이 가로챕니다.");
            pointer.pointerPressRaycast=new RaycastResult{gameObject=image.gameObject};var start=image.rectTransform.anchoredPosition;
            drag.OnPointerDown(pointer);Check(panel.IsWindowDragging&&!behindInput.blocksRaycasts,"임시 저장고 이동 중 다른 레이어의 창 입력 차단 실패");
            drag.OnBeginDrag(pointer);pointer.position+=new Vector2(40,-30);drag.OnDrag(pointer);
            Check(drag.IsDragging&&Close(image.rectTransform.anchoredPosition,start+new Vector2(40,-30)),"임시 저장고가 포인터 이동량을 따르지 않습니다.");
            drag.OnPointerUp(pointer);drag.OnEndDrag(pointer);Check(!panel.IsWindowDragging&&behindInput.blocksRaycasts,"창 이동 종료 후 입력 복원 실패");
            delivery.SetDraggedPosition(new Vector2(99999,-99999));
            var position=image.rectTransform.anchoredPosition;Check(position.x<layer.rect.xMax&&position.x+300>layer.rect.xMax&&position.y>layer.rect.yMin&&position.y-200<layer.rect.yMin,"임시 저장고를 화면 밖으로 드래그하거나 다시 잡을 수 없습니다.");
            delivery.SetDraggedPosition(start+new Vector2(40,-30));position=image.rectTransform.anchoredPosition;
            ManagementWindowMotion.CloseWindow(image.gameObject);motion.Advance(.1f);Check(image.gameObject.activeSelf,"독립 저장고 닫기에서 즉시 사라졌습니다.");
            motion.Advance(1);Check(!image.gameObject.activeSelf,"독립 저장고 퇴장 완료 후 비활성화 실패");
            image.gameObject.SetActive(true);motion.SendMessage("OnEnable");motion.Advance(1);Check(Close(position,image.rectTransform.anchoredPosition),"다시 열 때 임시 저장고 드래그 위치 손실");
            drag.OnPointerDown(pointer);drag.SendMessage("OnApplicationFocus",false);Check(!panel.IsWindowDragging,"포커스 상실 후 창 이동 입력 잠금이 남았습니다.");
        }
        finally{Object.DestroyImmediate(root);if(events!=null)Object.DestroyImmediate(events);}
    }
    static void MatchingTiming()
    {
        var root=new GameObject("Shared Slide Timing Check",typeof(RectTransform));DisplayUI display=null;
        try
        {
            ((RectTransform)root.transform).sizeDelta=new Vector2(1920,1080);
            var panel=root.AddComponent<InventoryPanel>();var menu=root.AddComponent<RightSideMenu>();menu.windowSlideSeconds=.45f;
            var image=InventorySceneBuilder.Box("Right Display",root.transform,1950,100,300,500,Color.white);var view=image.gameObject.AddComponent<DisplayWindowView>();var group=image.gameObject.AddComponent<CanvasGroup>();
            display=root.AddComponent<DisplayUI>();display.panel=panel;display.managementMenu=menu;display.windowLayer=(RectTransform)root.transform;
            var windows=(Dictionary<string,DisplayWindowView>)typeof(DisplayUI).GetField("windows",Private).GetValue(display);windows.Add("test",view);
            Set(display,"ready",true);Set(display,"moving",true);Set(display,"currentId","test");Set(display,"requestedId","test");Set(display,"movingRect",image.rectTransform);Set(display,"movingGroup",group);
            var left=InventorySceneBuilder.Rect("Left Window",root.transform,0,0,1920,1080);var frame=InventorySceneBuilder.Rect("Panel Frame",left,160,90,560,860);
            var motion=ManagementWindowMotion.Ensure(left.gameObject,frame);motion.duration=menu.windowSlideSeconds;
            float start=image.rectTransform.anchoredPosition.x;
            display.AdvanceSlide(.15f);motion.Advance(.15f);
            float target=(float)typeof(DisplayUI).GetField("slideTargetX",Private).GetValue(display);
            float expected=Mathf.LerpUnclamped(start,target,WindowSlideTiming.Ease(.15f,menu.windowSlideSeconds));
            Check(Mathf.Abs(image.rectTransform.anchoredPosition.x-expected)<.001f&&Mathf.Abs((float)typeof(ManagementWindowMotion).GetField("progress",Private).GetValue(motion)-WindowSlideTiming.Ease(.15f,menu.windowSlideSeconds))<.0001f,"양쪽 창의 감속 곡선/시간이 다릅니다.");
            Check(!group.blocksRaycasts&&display.IsSwitching,"오른쪽 등장 중 입력 차단 실패");
            display.AdvanceSlide(.30f);Check(!display.IsSwitching&&group.blocksRaycasts,"공통 등장 시간에 오른쪽 창이 끝나지 않습니다.");
            Set(display,"requestedId",null);Set(display,"moving",true);display.AdvanceSlide(.15f);
            Check(image.gameObject.activeSelf&&!group.blocksRaycasts,"오른쪽 퇴장 중 창 유지/입력 차단 실패");
            display.AdvanceSlide(.30f);Check(!image.gameObject.activeSelf,"공통 퇴장 시간에 오른쪽 창이 끝나지 않습니다.");
        }
        finally{if(display!=null)Set(display,"ready",false);Object.DestroyImmediate(root);}
    }
    static void Migration()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(StoreLayoutPrefabTools.LayoutPath);
        if(prefab==null)return;
        string folder="Assets/StoreInventorySettings/ControlChecks-"+Guid.NewGuid().ToString("N"),path=folder+"/Layout.prefab";GameObject contents=null;
        try
        {
            StoreLayoutPrefabTools.EnsureFolder(folder);contents=PrefabUtility.LoadPrefabContents(StoreLayoutPrefabTools.LayoutPath);
            var layout=contents.GetComponent<StoreLayoutRoot>();var frame=layout.management.window.transform.Find("Panel Frame") as RectTransform;
            frame.anchoredPosition+=new Vector2(9,-7);var authored=frame.anchoredPosition;
            layout.schemaVersion=1;if(layout.hotkeys!=null)Object.DestroyImmediate(layout.hotkeys);layout.hotkeys=null;
            var delivery=contents.GetComponentInChildren<DeliveryUI>(true);Object.DestroyImmediate(delivery.window.GetComponent<TemporaryStorageWindowDrag>());
            delivery.windowDrag=null;delivery.managementMenu=null;
            PrefabUtility.SaveAsPrefabAsset(contents,path);PrefabUtility.UnloadPrefabContents(contents);contents=null;
            var upgraded=StoreLayoutPrefabTools.UpgradeControls(path);StoreLayoutPrefabTools.ValidateLayout(upgraded);var bound=upgraded.GetComponent<StoreLayoutRoot>();
            Check(bound.schemaVersion==StoreLayoutPrefabTools.CurrentSchema&&bound.hotkeys!=null&&upgraded.GetComponentsInChildren<StoreHotkeys>(true).Length==1,"Prefab 자동 갱신의 단축키 연결/중복 방지 실패");
            var newFrame=bound.management.window.transform.Find("Panel Frame") as RectTransform;Check(Close(newFrame.anchoredPosition,authored),"Prefab 갱신이 사용자가 수정한 UI 위치를 덮어씁니다.");
            Check(upgraded.GetComponentsInChildren<TemporaryStorageWindowDrag>(true).Length==1,"Prefab 갱신의 임시 저장고 이동 연결 실패");
        }
        finally{if(contents!=null)PrefabUtility.UnloadPrefabContents(contents);if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);}
    }
}
#endif
