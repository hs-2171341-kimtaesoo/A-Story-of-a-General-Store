#if UNITY_EDITOR
using System;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;

internal static class PrefabArtworkBindings
{
    const string Base = "StoreInventory/";

    internal static Sprite LoadSprite(string key, int limit)
    {
        string path = "Assets/Resources/" + key + ".png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("원본 PNG 누락: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = limit;
        importer.SaveAndReimport();
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Sprite 가져오기 실패: " + path);
        return sprite;
    }

    static bool SpriteSlot(Sprite value, string key, int limit, bool repair, Action<Sprite> assign)
    {
        if (value != null) return false;
        if (repair) assign(LoadSprite(key, limit));
        return true;
    }

    static bool TextureSlot(Texture value, string key, bool repair, Action<Texture> assign)
    {
        if (value != null) return false;
        if (repair)
        {
            string path = "Assets/Resources/" + key + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new InvalidOperationException("UI Texture 누락: " + path);
            assign(texture);
        }
        return true;
    }

    static T Definitions<T>(string key) where T : class
    {
        var json = Resources.Load<TextAsset>(Base + key);
        if (json == null) throw new InvalidOperationException("아트 정의 누락: " + key);
        return JsonUtility.FromJson<T>(json.text);
    }

    internal static bool Check(GameObject root, bool repair)
    {
        bool changed = false;
        var layout = root.GetComponent<StoreLayoutRoot>();
        var left=root.GetComponentInChildren<LeftDisplayMenu>(true);
        var right=root.GetComponentInChildren<RightSideMenu>(true);
        if(left!=null && left.menuFrame!=null)
            changed |= SpriteSlot(left.menuFrame.sprite,Base+"DisplayMenu/frame",1024,repair,s => left.menuFrame.sprite=s);
        if(right!=null && right.menuFrame!=null)
            changed |= SpriteSlot(right.menuFrame.sprite,Base+"ManagementMenu/frame",1024,repair,s => right.menuFrame.sprite=s);
        var visitor=root.GetComponentInChildren<StagedCustomerInteraction>(true);
        if(visitor!=null)
        {
            if(visitor.counterPose!=null)changed |= SpriteSlot(visitor.counterPose.sprite,"temporary_customer",StoreTextureBudget.Customer,repair,s => visitor.counterPose.sprite=s);
            if(visitor.browsingStops!=null)foreach(var pose in visitor.browsingStops)
                if(pose!=null)changed |= SpriteSlot(pose.sprite,"temporary_customer",StoreTextureBudget.Customer,repair,s => pose.sprite=s);
        }
        var furniture = Definitions<FurnitureSceneLayout>("furniture_layout");
        var spots = root.GetComponentsInChildren<SceneFurnitureHotspot>(true);
        var catalog = layout.management.art;
        foreach (var entry in furniture.entries)
        {
            var art = catalog.Find(entry.typeId);
            if (art == null) throw new InvalidOperationException("가구 아이콘 항목 누락: " + entry.typeId);
            changed |= SpriteSlot(art.thumbnail, entry.spriteKey, StoreTextureBudget.Furniture, repair, s => art.thumbnail=s);
            foreach (var spot in spots)
            {
                if (spot.typeId != entry.typeId) continue;
                var image = spot.furnitureImage;
                if (image == null) throw new InvalidOperationException("가구 Image 슬롯 누락: " + entry.typeId);
                changed |= SpriteSlot(image.sprite, entry.spriteKey, StoreTextureBudget.Furniture, repair, s => image.sprite=s);
                changed |= SpriteSlot(spot.hoverSprite, entry.hoverSpriteKey, StoreTextureBudget.Outline, repair, s => spot.hoverSprite=s);
            }
        }

        var definitions = Definitions<DisplayWindowArtDatabase>("display_window_art");
        foreach (var template in layout.management.display.typeTemplates)
        {
            var art = Array.Find(definitions.entries, entry => entry.typeId == template.typeId);
            if (art == null) throw new InvalidOperationException("진열창 정의 누락: " + template.typeId);
            var appearance = template.template.appearance;
            if (!appearance.useAuthoredLayout) continue;
            changed |= SpriteSlot(appearance.frame.sprite, art.spriteKey, StoreTextureBudget.DisplayArt, repair, s => appearance.frame.sprite=s);
            foreach (var raw in template.template.GetComponentsInChildren<RawImage>(true))
            {
                string key = null;
                if (raw.name == "Original Frame Slice") key = art.spriteKey;
                else if (art.floors != null)
                    foreach (var floor in art.floors)
                        if (raw.name == "Floor " + floor.sections[0].row) { key = floor.spriteKey; break; }
                if (key != null) changed |= TextureSlot(raw.texture,key,repair,t => raw.texture=t);
            }
        }

        foreach (var paper in root.GetComponentsInChildren<ParchmentPanelGraphic>(true))
            changed |= TextureSlot(paper.artwork,Base+"ManagementPanel/parchment",repair,t => paper.artwork=t);
        var purchase = layout.purchases;
        var frame = purchase.window.transform.Find("Panel Frame");
        if (frame != null)
        {
            var image = frame.GetComponent<UIImage>();
            if (image != null) changed |= SpriteSlot(image.sprite,Base+"PurchasePanel/furniture_frame",2048,repair,s => image.sprite=s);
        }
        if (purchase.useStorageArtwork && purchase.storageImage != null)
            changed |= SpriteSlot(purchase.storageImage.sprite,Base+"PurchasePanel/storage_frame",2048,repair,s => purchase.storageImage.sprite=s);
        var storage = root.GetComponentInChildren<DeliveryUI>(true);
        if (storage != null && storage.useStorageArtwork)
        {
            var image=storage.window.GetComponent<UIImage>();
            changed |= SpriteSlot(image.sprite,Base+"TemporaryStorage/frame",2048,repair,s => image.sprite=s);
            var mark=storage.window.Find("Official Mark Slot/Official Mark");
            if(mark!=null){
                var icon=mark.GetComponent<UIImage>();
                changed |= SpriteSlot(icon.sprite,Base+"TemporaryStorage/mark",256,repair,s => icon.sprite=s);
            }
            var coin=storage.window.Find("Fee Coin");
            if(coin!=null){
                var coinImage=coin.GetComponent<UIImage>();
                changed |= SpriteSlot(coinImage.sprite,Base+"TemporaryStorage/coin",256,repair,s=>coinImage.sprite=s);
            }
            var close=storage.window.Find("Close/Artwork");
            if(close!=null){
                var icon=close.GetComponent<UIImage>();
                if(icon!=null){
                    var button=close.parent.GetComponent<Button>();
                    changed |= SpriteSlot(icon.sprite,Base+"TemporaryStorage/close",128,repair,s=>icon.sprite=s);
                    var state=button.spriteState;
                    if(state.pressedSprite==null || state.highlightedSprite==null || state.selectedSprite==null){
                        changed=true;
                        if(repair){
                            if(state.pressedSprite==null)state.pressedSprite=LoadSprite(Base+"TemporaryStorage/close_pressed",256);
                            if(state.highlightedSprite==null)state.highlightedSprite=icon.sprite;
                            if(state.selectedSprite==null)state.selectedSprite=icon.sprite;
                            button.spriteState=state;
                        }
                    }
                }
            }
            changed |= TextureSlot(storage.gridArtwork.artwork,Base+"TemporaryStorage/frame",repair,t => storage.gridArtwork.artwork=t);
        }
        changed |= PanelTextures(layout.management.window,"ManagementPanel",repair);
        changed |= PanelTextures(purchase.window,"PurchasePanel",repair);
        if(storage!=null && storage.useStorageArtwork) changed |= PanelTextures(storage.window.gameObject,"TemporaryStorage",repair);
        return changed;
    }

    static bool PanelTextures(GameObject root,string folder,bool repair)
    {
        bool changed=false;
        foreach(var raw in root.GetComponentsInChildren<RawImage>(true))
        {
            string name;
            switch(raw.name)
            {
                case "Original Row": case "Normal Row": name="row_normal"; break;
                case "Selected Row": name="row_selected"; break;
                case "Thumbnail Well": name="thumbnail_well"; break;
                case "Price Well": name="price_well"; break;
                case "Preview Well": name="preview_well"; break;
                case "Enabled Artwork": name="recover"; break;
                case "Disabled Artwork": name="recover_disabled"; break;
                case "Button Artwork":
                    switch(raw.transform.parent.name){
                        case "닫기": name="button_close"; break;
                        case "구매": name="button_buy"; break;
                        case "진열장 열기": name="button_open"; break;
                        default: continue;
                    }
                    break;
                default: continue;
            }
            changed |= TextureSlot(raw.texture,Base+folder+"/"+name,repair,t => raw.texture=t);
        }
        return changed;
    }

    internal static void RepairRow(Component row,string folder)
    {
        if(row==null || !EditorUtility.IsPersistent(row) || !PanelTextures(row.gameObject,folder,false)) return;
        string path=AssetDatabase.GetAssetPath(row);
        var contents=PrefabUtility.LoadPrefabContents(path);
        try{
            PanelTextures(contents,folder,true);
            bool saved;
            var result=PrefabUtility.SaveAsPrefabAsset(contents,path,out saved);
            if(!saved || result==null || PanelTextures(result,folder,false))throw new InvalidOperationException("목록 아트 복구 실패: "+path);
        }finally{PrefabUtility.UnloadPrefabContents(contents);}
    }
}
#endif
