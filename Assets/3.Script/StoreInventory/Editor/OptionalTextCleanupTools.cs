#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GeneralStore.Inventory;
public static class OptionalTextCleanupTools
{
    static int Remove(Text text,GameObject owner)
    {
        if(text==null)return 0;
        // 혹시 라벨이 루트에 직접 붙어 있어도 버튼/창 루트를 지우지 않습니다.
        if(text.gameObject==owner)Undo.DestroyObjectImmediate(text);else Undo.DestroyObjectImmediate(text.gameObject);
        return 1;
    }
    static void Allow(GameObject window)
    {if(window==null)return;var image=window.GetComponent<global::UnityEngine.UI.Image>();if(image!=null)Undo.RecordObject(image,"Allow Outside Side Panel");SidePanelInput.AllowOutside(window);}
    [MenuItem("Store Inventory/Existing Scene Tools/Remove Optional Texts From Selection")]
    public static void Apply()
    {
        if(Application.isPlaying){Debug.LogError("Play를 중지하고 Store Scene을 선택하세요.");return;}
        var selected=Selection.activeGameObject;if(selected==null){Debug.LogError("Store Scene 또는 Store UI Canvas를 선택하세요.");return;}int count=0;
        foreach(var view in selected.GetComponentsInChildren<WarehouseUI>(true)){Undo.RecordObject(view,"Remove Warehouse Status");count+=Remove(view.pageLabel,view.gameObject);view.pageLabel=null;}
        foreach(var view in selected.GetComponentsInChildren<DisplayWindowView>(true))
        {Undo.RecordObject(view,"Remove Display Status");count+=Remove(view.status,view.gameObject);view.status=null;if(view.appearance!=null){Undo.RecordObject(view.appearance,"Clear Status Reference");view.appearance.status=null;}}
        foreach(var view in selected.GetComponentsInChildren<ContainerTabView>(true)){Undo.RecordObject(view,"Remove Tab Text");count+=Remove(view.label,view.gameObject);view.label=null;}
        foreach(var view in selected.GetComponentsInChildren<WarehouseToggleButton>(true)){Undo.RecordObject(view,"Remove Toggle Text");count+=Remove(view.label,view.gameObject);view.label=null;}
        foreach(var view in selected.GetComponentsInChildren<DisplayManagementUI>(true))Allow(view.window);
        foreach(var view in selected.GetComponentsInChildren<UpgradeUI>(true)){Allow(view.window);Allow(view.storageWindow);}
        foreach(var view in selected.GetComponentsInChildren<SupplierUI>(true))Allow(view.window);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
        Debug.Log("선택 범위의 불필요한 Text "+count+"개 제거 / 오른쪽 패널 외부 입력 통과 적용. 창 위치·이미지·관리창 통계는 유지합니다.");
    }
}
#endif
