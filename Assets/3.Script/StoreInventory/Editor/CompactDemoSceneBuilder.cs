#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class CompactDemoSceneBuilder
{
    public static void Build(RectTransform layout,InventoryPanel panel)
    {
        var edge=UI.Rect("Screen Edge UI",layout,0,0,1920,1080);
        var settings=layout.gameObject.AddComponent<ShopViewportLayout>();settings.panel=panel;settings.stage=panel.shop.displays.windowLayer;settings.edgeLayer=edge;
        settings.drawer=panel.shop.drawer;settings.drawer.startClosed=true;settings.displays=panel.shop.displays;settings.displays.slideFromRight=true;
        var management=layout.GetComponentInChildren<RightSideMenu>(true);
        management.artworkScale=.64f;management.peekWidth=26;
        settings.managementMenu=(RectTransform)management.transform;settings.managementMenu.name="Left Management Menu";settings.managementMenu.SetParent(edge,false);
        settings.displayMenu=layout.GetComponentInChildren<LeftDisplayMenu>(true);settings.displayMenu.dockRight=true;
        settings.displayMenu.artworkScale=.64f;settings.displayMenu.peekWidth=26;
        settings.displayMenuLayer=(RectTransform)settings.displayMenu.transform;settings.displayMenuLayer.name="Right Display Menu Layer";settings.displayMenuLayer.SetParent(edge,false);
        var roots=new List<RectTransform>();var frames=new List<RectTransform>();
        foreach(var entry in management.entries)if(entry.window!=null)
        {
            var root=(RectTransform)entry.window.transform;root.SetParent(edge,false);root.anchoredPosition=Vector2.zero;
            var frame=root.Find("Panel Frame") as RectTransform;if(frame!=null){roots.Add(root);frames.Add(frame);}
        }
        settings.managementWindows=roots.ToArray();settings.managementFrames=frames.ToArray();
        // 실제 프레임 그림이 입력을 받고 중복된 투명 창 면은 그리지 않습니다.
        var warehouseImage=settings.drawer.window.GetComponent<global::UnityEngine.UI.Image>();warehouseImage.raycastTarget=false;warehouseImage.canvasRenderer.cullTransparentMesh=true;
        var frameImage=settings.drawer.window.Find("Authored Warehouse Frame").GetComponent<global::UnityEngine.UI.Image>();frameImage.raycastTarget=true;
        var warehouse=panel.GetComponent<WarehouseUI>();settings.drawer.dragExcludedGrid=warehouse.grid.Rect;settings.displays.cellReference=warehouse.grid;settings.delivery=layout.GetComponent<DeliveryUI>();
        foreach(var view in layout.GetComponentsInChildren<DisplayWindowView>(true))
        {
            var image=view.GetComponent<global::UnityEngine.UI.Image>();if(image!=null&&image.color.a<=.001f){image.raycastTarget=false;image.canvasRenderer.cullTransparentMesh=true;}
            if(view.appearance!=null&&view.appearance.frame!=null)view.appearance.frame.raycastTarget=true;
        }
        foreach(var grid in layout.GetComponentsInChildren<InventoryGridView>(true))
        {
            var image=grid.GetComponent<global::UnityEngine.UI.Image>();
            if(image==null||grid.renderCells||image.color.a>.001f)continue;
            var surface=grid.GetComponent<DragOnlyRaycastSurface>();if(surface==null)surface=grid.gameObject.AddComponent<DragOnlyRaycastSurface>();surface.panel=panel;surface.image=image;
            image.raycastTarget=false;image.canvasRenderer.cullTransparentMesh=true;
        }
        var hud=UI.Rect("HUD Text Layer",layout,0,0,1920,1080);UiPerformanceLayers.Ensure(hud.gameObject,false);
        panel.shop.goldLabel.transform.SetParent(hud,false);panel.shop.simulation.clockLabel.transform.SetParent(hud,false);panel.messageLabel.transform.SetParent(hud,false);
        var message=panel.messageLabel.rectTransform;message.anchoredPosition=new Vector2(400,-74);message.sizeDelta=new Vector2(1120,42);panel.messageLabel.fontSize=18;panel.messageLabel.alignment=TextAnchor.MiddleCenter;
        settings.Apply();panel.shop.priceDialog.transform.SetAsLastSibling();ConfigureUiOnlyCamera(panel.gameObject.scene.handle);
    }
    public static void BuildDelivery(RectTransform layout,RectTransform furnitureLayout,InventoryPanel panel,InventoryItemView itemTemplate)
    {
        var delivery=layout.gameObject.AddComponent<DeliveryUI>();delivery.panel=panel;delivery.cellReference=panel.GetComponent<WarehouseUI>().grid;
        // The authored door position is only set during generation. Replace the sprite/RectTransform in Inspector.
        var icon=UI.Box("Delivery At Door",furnitureLayout,550,478,104,104,Color.white);
        icon.sprite=Resources.Load<Sprite>("StoreInventory/CounterDemo/warehouse_frame");icon.preserveAspect=true;
        delivery.doorButton=icon.gameObject.AddComponent<Button>();delivery.doorButton.targetGraphic=icon;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(delivery.doorButton.onClick,delivery.Toggle);
        delivery.badge=UI.Label("Delivery Badge",icon.transform,-12,100,128,32,"택배",21);delivery.badge.alignment=TextAnchor.MiddleCenter;
        TemporaryStorageArtBuilder.BuildWindow(layout,delivery,itemTemplate);
        icon.gameObject.SetActive(false);
        var menu=layout.GetComponentInChildren<RightSideMenu>(true);delivery.managementMenu=menu;
        var entry=new ShopMenuEntry{id="temporary_storage",title="임시 저장고",window=delivery.window.gameObject,icon=Resources.Load<Sprite>("StoreInventory/TemporaryStorage/mark"),visible=false};
        UnityEditor.Events.UnityEventTools.AddPersistentListener(entry.onOpen,delivery.OpenStorage);menu.AddEntry(entry);
    }
    public static void RebuildDelivery(InventoryPanel panel)
    {
        var root=panel.transform.root;var old=root.GetComponentInChildren<DeliveryUI>(true);if(old==null)return;
        var layout=(RectTransform)old.transform;var furniture=(RectTransform)old.doorButton.transform.parent;
        var oldDoor=(RectTransform)old.doorButton.transform;var position=oldDoor.anchoredPosition;var size=oldDoor.sizeDelta;var scale=oldDoor.localScale;var rotation=oldDoor.localRotation;var anchorMin=oldDoor.anchorMin;var anchorMax=oldDoor.anchorMax;var pivot=oldDoor.pivot;
        var sprite=old.doorButton.GetComponent<global::UnityEngine.UI.Image>().sprite;var template=old.grid.itemTemplate;
        var menu=root.GetComponentInChildren<RightSideMenu>(true);menu.entries.RemoveAll(entry=>entry.id=="temporary_storage");
        panel.UnregisterWindowGrid(old.grid,old.details);
        Object.DestroyImmediate(old.window.gameObject);Object.DestroyImmediate(old.doorButton.gameObject);Object.DestroyImmediate(old);
        BuildDelivery(layout,furniture,panel,template);var current=layout.GetComponent<DeliveryUI>();
        var door=(RectTransform)current.doorButton.transform;door.anchorMin=anchorMin;door.anchorMax=anchorMax;door.pivot=pivot;door.anchoredPosition=position;door.sizeDelta=size;door.localScale=scale;door.localRotation=rotation;
        if(sprite!=null)current.doorButton.GetComponent<global::UnityEngine.UI.Image>().sprite=sprite;
        var viewport=root.GetComponentInChildren<ShopViewportLayout>(true);if(viewport!=null){viewport.delivery=current;current.window.SetParent(viewport.edgeLayer,false);current.windowDrag.bounds=viewport.edgeLayer;viewport.Apply();}
    }
    internal static void ConfigureUiOnlyCamera(int sceneHandle)
    {
        // 새 UI 전용 데모에서만 적용합니다. Renderer/Terrain이 있으면 기존 카메라를 유지합니다.
        if(Object.FindObjectsByType<global::UnityEngine.Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length>0||Terrain.activeTerrain!=null)return;
        foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))if(camera.name=="Main Camera"&&camera.gameObject.scene.handle==sceneHandle)
        {
            Undo.RecordObject(camera,"Configure UI-only Demo Camera");camera.cullingMask=0;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=false;camera.allowMSAA=false;
        }
    }
}
#endif
