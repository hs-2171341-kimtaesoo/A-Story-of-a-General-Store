#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using GeneralStore.Inventory;
public static class EditableOutlineTools
{
    [MenuItem("Store Inventory/Existing Scene Tools/Add Editable Outline Frames To Selection")]
    public static void AddFrames()
    {
        if(Application.isPlaying){Debug.LogError("Play를 중지한 뒤 가구들이 들어 있는 루트를 선택하세요.");return;}
        var selected=Selection.activeGameObject;if(selected==null){Debug.LogError("Store Scene 또는 Scene Furniture Hotspots를 선택하세요.");return;}
        int created=0,skipped=0;
        foreach(var spot in selected.GetComponentsInChildren<SceneFurnitureHotspot>(true))
        {
            if(spot.outlineFrame!=null){skipped++;continue;}
            if(spot.furnitureImage==null||spot.furnitureImage.sprite==null||spot.furnitureImage.rectTransform.rect.width<=0||spot.furnitureImage.rectTransform.rect.height<=0){skipped++;continue;}
            var image=spot.furnitureImage;var p=FurnitureOutlineGeometry.Align(image.sprite.rect.width,image.sprite.rect.height,image.rectTransform.rect.width,image.rectTransform.rect.height,spot.furnitureAlphaBounds,spot.outlineAlphaBounds,spot.hoverPresenter==null?1:spot.hoverPresenter.padding);
            Undo.RecordObject(spot,"Add Editable Outline Frame");
            var go=new GameObject("Outline Frame",typeof(RectTransform));go.transform.SetParent(image.transform,false);Undo.RegisterCreatedObjectUndo(go,"Create Outline Frame");
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(p.x,-p.y);rect.sizeDelta=new Vector2(p.width,p.height);
            spot.outlineFrame=rect;created++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
        Debug.Log("Outline Frame 추가 "+created+"개 / 기존 또는 미연결 "+skipped+"개. 가구 위치와 기존 Frame은 변경하지 않았습니다.");
    }
}
#endif
