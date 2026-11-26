#if UNITY_EDITOR
using System;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;
using UIImage = UnityEngine.UI.Image;

internal static class DemoSceneArtwork
{


    // First generation and existing prefabs use the same required slots.
    static bool CheckSlots(GameObject root, bool repair)
    {
        var layout = root.GetComponent<StoreLayoutRoot>();
        var background = layout.backgroundImage;
        if (background == null)
        {
            var node = root.transform.Find("Store Background Canvas/Background Layout/Background");
            if (node != null) background = node.GetComponent<UIImage>();
        }
        var counter = root.GetComponentInChildren<SceneCounterOcclusion>(true);
        bool changed = Bind(background, "CounterDemo/background", StoreTextureBudget.Background, repair);
        changed |= Bind(counter == null ? null : counter.counterImage, "Scene/counter", StoreTextureBudget.Background, repair);
        if (background != null && layout.backgroundImage == null)
        {
            if (repair) layout.backgroundImage = background;
            changed = true;
        }
        var panel = layout.panel;
        if (panel.tooltip == null)
        {
            changed = true;
            if (repair)
            {
                panel.tooltip = root.GetComponentInChildren<ItemHoverTooltip>(true);
                if (panel.tooltip == null) CounterDemoSceneBuilder.BuildTooltip(panel);
            }
        }
        if (panel.tooltip != null && panel.tooltip.window == null)
        {
            changed = true;
            if (repair)
            {
                var window = panel.drag.dragLayer.Find("Shared Item Tooltip") as RectTransform;
                if(window == null) CounterDemoSceneBuilder.BuildTooltip(panel);
                else panel.tooltip.window = window;
            }
        }
        if (panel.tooltip != null && panel.tooltip.window != null)
        {
            var tip=panel.tooltip;
            if(tip.panel==null){changed=true;if(repair)tip.panel=panel;}
            changed |= Bind(tip.window.GetComponent<UIImage>(), "CounterDemo/item_tooltip", 1024, repair);
            changed |= Link(ref tip.icon,tip.window,"Icon Frame/Item Icon",repair);
            changed |= Link(ref tip.iconFit,tip.window,"Icon Frame",repair);
            changed |= Link(ref tip.nameLabel,tip.window,"Name",repair);
            changed |= Link(ref tip.qualityLabel,tip.window,"Quality",repair);
            changed |= Link(ref tip.sizeLabel,tip.window,"Footprint",repair);
            changed |= Link(ref tip.descriptionLabel,tip.window,"Description",repair);
            changed |= Link(ref tip.priceLabel,tip.window,"Base Price",repair);
            if(tip.iconFit!=null){
                changed |= Link(ref tip.iconFit.image,tip.window,"Icon Frame/Item Icon",repair);
                changed |= Link(ref tip.iconFit.frame,tip.window,"Icon Frame",repair);
            }
        }
        if (panel != null && panel.shop != null && panel.shop.drawer != null && panel.shop.drawer.window != null)
        {
            var frame = panel.shop.drawer.window.Find("Authored Warehouse Frame");
            if (frame != null) changed |= Bind(frame.GetComponent<UIImage>(), "CounterDemo/warehouse_frame", 1024, repair);
        }
        changed |= PrefabArtworkBindings.Check(root,repair);
        return changed;
    }

    static bool Link<T>(ref T field, Transform root, string path, bool repair) where T:Component
    {
        if(field!=null)return false;
        if(repair){
            var node=root.Find(path);
            field=node!=null?node.GetComponent<T>():null;
            if(field==null)throw new InvalidOperationException("아이템 정보창 구성 누락: "+path);
        }
        return true;
    }

    static bool Bind(UIImage image, string key, int size, bool repair)
    {
        if (image == null) throw new InvalidOperationException("매장 이미지 슬롯을 찾을 수 없습니다: " + key);
        if (image.sprite != null) return false;
        if (!repair) return true;

        image.sprite = PrefabArtworkBindings.LoadSprite("StoreInventory/" + key,size);
        return true;
    }

    internal static bool Repair(GameObject root) { return CheckSlots(root, true); }

    internal static void Verify(GameObject root)
    {
        if (CheckSlots(root, false)) throw new InvalidOperationException("저장된 매장 아트 연결을 확인하세요: " + root.name);
    }

    internal static GameObject RepairPrefab(GameObject prefab, string path)
    {
        if (!CheckSlots(prefab, false)) return prefab;
        var contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Repair(contents);
            bool saved;
            prefab = PrefabUtility.SaveAsPrefabAsset(contents, path, out saved);
            if (!saved || prefab == null) throw new InvalidOperationException("매장 아트 연결 저장 실패: " + path);
            Verify(prefab);
            return prefab;
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }
}
#endif
