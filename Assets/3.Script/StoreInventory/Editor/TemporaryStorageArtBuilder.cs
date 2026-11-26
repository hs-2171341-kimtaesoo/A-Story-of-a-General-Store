#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UI=InventorySceneBuilder;

public static class TemporaryStorageArtBuilder
{
    const float Scale=1284f/1498;
    [Serializable]sealed class Part{public string id,spriteKey;public float sourceWidth,sourceHeight;public int maximumTextureSize;}
    [Serializable]sealed class ArtData{public Part[] entries;}
    static Dictionary<string,Part> Art()
    {
        var json=Resources.Load<TextAsset>("StoreInventory/temporary_storage_art");
        if(json==null)throw new InvalidOperationException("temporary_storage_art.json 누락");
        var data=JsonUtility.FromJson<ArtData>(json.text);var parts=new Dictionary<string,Part>();
        if(data==null||data.entries==null)throw new ArgumentException("임시 저장고 아트 정의 오류");
        foreach(var part in data.entries)
        {
            if(part==null||string.IsNullOrEmpty(part.id)||string.IsNullOrEmpty(part.spriteKey)||parts.ContainsKey(part.id)||part.sourceWidth<=0||part.sourceHeight<=0||part.maximumTextureSize<32||part.maximumTextureSize>2048)
                throw new ArgumentException("임시 저장고 Sprite 정의 오류");
            parts.Add(part.id,part);
        }
        foreach(string key in new[]{"frame","mark","fee","used","recover","recover_disabled","close"})
            if(!parts.ContainsKey(key))throw new ArgumentException("임시 저장고 Sprite 누락: "+key);
        return parts;
    }
    public static void ValidateArt()
    {foreach(var part in Art().Values)if(AssetImporter.GetAtPath("Assets/Resources/"+part.spriteKey+".png")==null)throw new InvalidOperationException("임시 저장고 PNG 누락: "+part.spriteKey);}
    public static void PrepareArt()
    {foreach(var part in Art().Values)SideMenuSceneBuilder.PreparePanelTexture(part.spriteKey,part.maximumTextureSize);}
    static RawImage Artwork(string name,Transform parent,Part part,Rect slot)
    {
        float w=Mathf.Min(slot.width,slot.height*part.sourceWidth/part.sourceHeight),h=w*part.sourceHeight/part.sourceWidth;
        var r=UI.Rect(name,parent,(slot.x+(slot.width-w)*.5f)*Scale,(slot.y+(slot.height-h)*.5f)*Scale,w*Scale,h*Scale);
        var image=r.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>(part.spriteKey);image.uvRect=new Rect(0,0,1,1);image.color=Color.white;image.raycastTarget=false;
        if(image.texture==null)throw new InvalidOperationException("임시 저장고 Texture 누락: "+part.spriteKey);return image;
    }
    static Text Label(string name,Transform parent,Rect r,string content,int size,Color color,TextAnchor alignment)
    {
        var label=UI.Label(name,parent,r.x*Scale,r.y*Scale,r.width*Scale,r.height*Scale,content,Mathf.RoundToInt(size*Scale));
        label.alignment=alignment;label.color=color;label.fontStyle=FontStyle.Bold;label.resizeTextForBestFit=true;label.resizeTextMaxSize=label.fontSize;label.resizeTextMinSize=Mathf.Max(10,Mathf.RoundToInt(label.fontSize*.62f));return label;
    }
    public static void BuildWindow(Transform parent,DeliveryUI ui,InventoryItemView itemTemplate)
    {
        PrepareArt();var parts=Art();ui.useStorageArtwork=true;ui.matchWarehouseCells=false;
        var root=UI.Box("Temporary Storage Window",parent,310,110,1498*Scale,999*Scale,Color.white);root.sprite=Resources.Load<Sprite>(parts["frame"].spriteKey);root.preserveAspect=true;
        if(root.sprite==null)throw new InvalidOperationException("임시 저장고 Frame Sprite 누락");
        var mark=UI.IconSlot("Official Mark",root.transform,58*Scale,43*Scale,172*Scale,172*Scale);mark.sprite=Resources.Load<Sprite>(parts["mark"].spriteKey);
        ui.window=root.rectTransform;
        // The supplied frame already contains both stat boards. Draw their text directly over it.
        // Replacing them with differently proportioned duplicate plates would create double borders.
        var ink=new Color(.07f,.09f,.1f);var cream=new Color(.98f,.91f,.65f);
        Label("Title",root.transform,new Rect(270,73,380,73),"임시저장고",60,ink,TextAnchor.MiddleLeft);
        Label("Fee Rule",root.transform,new Rect(270,152,380,45),"보관료는 매일 새벽 정산됩니다",26,ink,TextAnchor.MiddleLeft);
        ui.feeLabel=Label("Expected Storage Fee",root.transform,new Rect(704,107,276,55),"예정 보관료 0 G",32,cream,TextAnchor.MiddleCenter);
        ui.usedCellsLabel=Label("Used Cells",root.transform,new Rect(1040,107,201,55),"사용 중 0칸",34,ink,TextAnchor.MiddleCenter);
        var recover=UI.Box("Recover",root.transform,1255*Scale,98*Scale,190*Scale,76*Scale,Color.clear);recover.canvasRenderer.cullTransparentMesh=false;
        ui.recoverButton=recover.gameObject.AddComponent<Button>();ui.recoverButton.transition=Selectable.Transition.None;
        ui.recoverEnabledArtwork=Artwork("Enabled Artwork",recover.transform,parts["recover"],new Rect(0,0,190,76));
        ui.recoverDisabledArtwork=Artwork("Disabled Artwork",recover.transform,parts["recover_disabled"],new Rect(0,0,190,76));
        ui.recoverEnabledArtwork.gameObject.SetActive(false);ui.recoverButton.targetGraphic=ui.recoverDisabledArtwork;ui.recoverButton.interactable=false;
        ui.recoverLabel=Label("Text",recover.transform,new Rect(22,3,145,64),"회수",43,new Color(.75f,.77f,.8f),TextAnchor.MiddleCenter);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(ui.recoverButton.onClick,ui.RecoverAll);
        var close=UI.Box("Close",root.transform,1397*Scale,0,99*Scale,91*Scale,Color.clear);close.canvasRenderer.cullTransparentMesh=false;
        var closeArt=Artwork("Artwork",close.transform,parts["close"],new Rect(0,0,99,91));
        var closeButton=close.gameObject.AddComponent<Button>();closeButton.targetGraphic=closeArt;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeButton.onClick,ui.Close);
        var viewport=UI.Box("Temporary Storage Viewport",root.transform,60*Scale,250*Scale,1376*Scale,688*Scale,new Color(1,1,1,.001f));viewport.canvasRenderer.cullTransparentMesh=false;viewport.gameObject.AddComponent<RectMask2D>();
        ui.scroll=viewport.gameObject.AddComponent<ScrollRect>();ui.scroll.viewport=viewport.rectTransform;ui.scroll.horizontal=false;ui.scroll.vertical=true;ui.scroll.movementType=ScrollRect.MovementType.Clamped;ui.scroll.inertia=false;ui.scroll.scrollSensitivity=86*Scale;
        var board=UI.Rect("Scrolling Grid Artwork",viewport.transform,0,0,1376*Scale,688*Scale);board.anchorMin=Vector2.zero;board.anchorMax=Vector2.one;board.pivot=new Vector2(.5f,.5f);board.offsetMin=board.offsetMax=Vector2.zero;
        ui.gridArtwork=board.gameObject.AddComponent<TemporaryStorageGridGraphic>();ui.gridArtwork.artwork=Resources.Load<Texture2D>(parts["frame"].spriteKey);ui.gridArtwork.cellHeight=86*Scale;ui.gridArtwork.raycastTarget=false;
        ui.content=UI.Rect("Virtual Storage Content",viewport.transform,0,0,1376*Scale,688*Scale);ui.scroll.content=ui.content;
        var gridImage=UI.Box("Temporary Visible Rows",ui.content,0,0,1376*Scale,688*Scale,Color.clear);gridImage.raycastTarget=false;gridImage.canvasRenderer.cullTransparentMesh=true;
        var grid=gridImage.gameObject.AddComponent<InventoryGridView>();ui.grid=grid;grid.containerId=InventoryModel.TemporaryStorageId;grid.cellWidth=grid.cellHeight=86*Scale;grid.cellGap=0;grid.viewRows=8;grid.externalRefresh=true;grid.acceptsDrop=true;grid.recycleItems=true;grid.renderCells=false;grid.renderPartitions=false;grid.itemTemplate=itemTemplate;
        grid.itemLayer=UI.Rect("Items",grid.transform,0,0,1376*Scale,688*Scale);
        var input=gridImage.gameObject.AddComponent<DragOnlyRaycastSurface>();input.panel=ui.panel;input.image=gridImage;
        var drag=root.gameObject.AddComponent<TemporaryStorageWindowDrag>();drag.panel=ui.panel;drag.delivery=ui;drag.window=ui.window;drag.bounds=parent as RectTransform;ui.windowDrag=drag;
        ui.details=null;UiPerformanceLayers.Ensure(root.gameObject,true);root.gameObject.SetActive(false);
    }
    public static void UpgradeWindow(DeliveryUI ui)
    {
        var old=ui.window;var oldGrid=ui.grid;var template=oldGrid.itemTemplate;
        var position=old.anchoredPosition;var scale=old.localScale;int sibling=old.GetSiblingIndex();bool active=old.gameObject.activeSelf;
        BuildWindow(old.parent,ui,template);ui.window.anchoredPosition=position;ui.window.localScale=scale;ui.window.SetSiblingIndex(sibling);
        var sceneRoot=ui.transform.root;
        var menu=sceneRoot.GetComponentInChildren<RightSideMenu>(true);
        foreach(var entry in menu.entries)if(entry.window==old.gameObject)
        {entry.window=ui.window.gameObject;entry.icon=Resources.Load<Sprite>("StoreInventory/TemporaryStorage/mark");}
        if(ui.panel.grids!=null)for(int n=0;n<ui.panel.grids.Length;n++)if(ui.panel.grids[n]==oldGrid)ui.panel.grids[n]=ui.grid;
        var viewport=sceneRoot.GetComponentInChildren<ShopViewportLayout>(true);
        if(viewport!=null&&viewport.managementWindows!=null)
        {
            var roots=new List<RectTransform>();var frames=new List<RectTransform>();
            for(int n=0;n<viewport.managementWindows.Length;n++)
                if(viewport.managementWindows[n]!=old){roots.Add(viewport.managementWindows[n]);frames.Add(viewport.managementFrames[n]);}
            viewport.managementWindows=roots.ToArray();viewport.managementFrames=frames.ToArray();
        }
        UnityEngine.Object.DestroyImmediate(old.gameObject);ui.window.gameObject.SetActive(active);
    }
}
#endif
