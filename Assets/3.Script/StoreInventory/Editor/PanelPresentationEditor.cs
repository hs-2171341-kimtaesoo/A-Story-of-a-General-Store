#if UNITY_EDITOR
using GeneralStore.Inventory;
using UnityEngine;
using UnityEngine.UI;

// Upgrade presentation in place: keep the authored slots, sprites, listeners and data refs.
public static class PanelPresentationEditor
{
    public static void CenterIcon(global::UnityEngine.UI.Image icon)
    {
        if(icon==null)return;icon.preserveAspect=true;icon.raycastTarget=false;
        var rect=icon.rectTransform;
        if(rect.anchorMin==Vector2.zero&&rect.anchorMax==Vector2.one&&rect.pivot==new Vector2(.5f,.5f))return;
        var slot=rect.parent as RectTransform;if(slot==null)return;
        if(slot.name!=icon.name+" Slot")
        {
            var parent=rect.parent;int index=rect.GetSiblingIndex();
            slot=new GameObject(icon.name+" Slot",typeof(RectTransform)).GetComponent<RectTransform>();
            slot.SetParent(parent,false);slot.SetSiblingIndex(index);
            slot.anchorMin=rect.anchorMin;slot.anchorMax=rect.anchorMax;slot.pivot=rect.pivot;
            slot.sizeDelta=rect.sizeDelta;slot.anchoredPosition3D=rect.anchoredPosition3D;slot.localRotation=rect.localRotation;slot.localScale=rect.localScale;
            rect.SetParent(slot,false);
        }
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);
        rect.offsetMin=rect.offsetMax=Vector2.zero;rect.anchoredPosition3D=Vector3.zero;rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one;
    }
    public static void ApplyTextButtons(GameObject window)
    {
        if(window==null)return;
        foreach(var button in window.GetComponentsInChildren<Button>(true))InventorySceneBuilder.UseTextButtonFeedback(button);
    }
    public static void Apply(GameObject root)
    {
        foreach(var ui in root.GetComponentsInChildren<SupplierUI>(true))
        {
            CenterIcon(ui.portrait);
            if(ui.supplierTemplate!=null)
            {
                CenterIcon(ui.supplierTemplate.portrait);
                if(ui.supplierTemplate.sampleIcons!=null)foreach(var image in ui.supplierTemplate.sampleIcons)CenterIcon(image);
            }
            if(ui.offerTemplate!=null){CenterIcon(ui.offerTemplate.icon);ApplyTextButtons(ui.offerTemplate.gameObject);}
            ApplyTextButtons(ui.window);
        }
        foreach(var slot in root.GetComponentsInChildren<WorkshopSlotView>(true))CenterIcon(slot.icon);
        foreach(var ui in root.GetComponentsInChildren<DisplayManagementUI>(true))ApplyTextButtons(ui.window);
        foreach(var ui in root.GetComponentsInChildren<UpgradeUI>(true)){ApplyTextButtons(ui.window);ApplyTextButtons(ui.storageWindow);}
    }
}
#endif
