#if UNITY_EDITOR
using System;
using System.Reflection;
using GeneralStore.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class PanelPresentationChecks
{
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks++;}
    public static int Run()
    {
        checks=0;var root=new GameObject("Panel Presentation Check",typeof(RectTransform));
        var previous=EventSystem.current;var events=new GameObject("Text Feedback Event Check",typeof(EventSystem));
        Texture2D texture=null;Sprite tall=null,wide=null;
        try
        {
            var legacy=InventorySceneBuilder.Box("Item Icon",root.transform,27,39,64,72,Color.white);
            var oldRect=legacy.rectTransform;var bounds=new Vector3[4];oldRect.GetWorldCorners(bounds);
            PanelPresentationEditor.CenterIcon(legacy);var slot=legacy.transform.parent as RectTransform;
            var after=new Vector3[4];slot.GetWorldCorners(after);
            for(int n=0;n<4;n++)Check((bounds[n]-after[n]).sqrMagnitude<.0001f,"중앙 정렬 갱신이 기존 아이콘 Slot 영역을 변경했습니다.");
            Check(legacy.preserveAspect&&legacy.rectTransform.pivot==new Vector2(.5f,.5f),"아이콘 중심 피벗/원본 비율 설정 실패");
            Check(legacy.rectTransform.anchorMin==Vector2.zero&&legacy.rectTransform.anchorMax==Vector2.one,"아이콘이 Slot 크기를 따르지 않습니다.");
            texture=new Texture2D(64,64);tall=Sprite.Create(texture,new Rect(0,0,12,48),new Vector2(0,1));wide=Sprite.Create(texture,new Rect(0,0,48,12),new Vector2(0,1));
            foreach(var sprite in new[]{tall,wide})
            {
                legacy.sprite=sprite;
                using(var mesh=new VertexHelper())
                {
                    typeof(global::UnityEngine.UI.Image).GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(legacy,new object[]{mesh});
                    Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
                    for(int n=0;n<mesh.currentVertCount;n++){var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,n);min=Vector2.Min(min,vertex.position);max=Vector2.Max(max,vertex.position);}
                    Check(((min+max)*.5f-legacy.rectTransform.rect.center).sqrMagnitude<.0001f,"가로/세로 Sprite가 슬롯 중앙에서 벗어났습니다.");
                }
            }
            int children=root.transform.childCount;PanelPresentationEditor.CenterIcon(legacy);
            Check(root.transform.childCount==children&&legacy.transform.parent==slot,"재적용 시 아이콘 Slot을 중복 생성합니다.");
            var button=InventorySceneBuilder.ButtonBox("구매",root.transform,110,15,170,50,null);
            var background=button.targetGraphic;var label=button.transform.Find("Text").GetComponent<Text>();var labelColor=label.color;
            InventorySceneBuilder.UseTextButtonFeedback(button);
            Check(button.targetGraphic==label&&button.transition==Selectable.Transition.ColorTint,"버튼 색 전환 대상이 글자가 아닙니다.");
            Check(label.color==labelColor&&button.colors.disabledColor==new Color(.5f,.5f,.5f,1),"글자 원본 색/상자 추가 비활성 색 손상");
            var system=events.GetComponent<EventSystem>();EventSystem.current=system;
            var colors=button.colors;colors.fadeDuration=0;button.colors=colors;
            var pointer=new PointerEventData(system){button=PointerEventData.InputButton.Left};
            button.OnPointerEnter(pointer);button.OnPointerDown(pointer);
            Check(label.canvasRenderer.GetColor()==colors.pressedColor,"글자 누름 색 전환 실패");
            button.OnPointerUp(pointer);Check(label.canvasRenderer.GetColor()==Color.white,"누름 해제 시 글자 밝기 복원 실패");
            var group=root.AddComponent<CanvasGroup>();group.interactable=false;
            button.SendMessage("OnCanvasGroupChanged",SendMessageOptions.DontRequireReceiver);
            Check(label.canvasRenderer.GetColor()==colors.disabledColor,"창 등장 입력 잠금 중 글자가 어두워지지 않습니다.");
            group.interactable=true;button.SendMessage("OnCanvasGroupChanged",SendMessageOptions.DontRequireReceiver);
            Check(label.canvasRenderer.GetColor()==Color.white,"입력 잠금 해제 후 글자가 밝아지지 않습니다.");
            Check(background.canvasRenderer.GetColor()==Color.white,"배경만 깜빡이는 이전 연결이 남았습니다.");
            InventorySceneBuilder.UseTextButtonFeedback(button);Check(button.targetGraphic==label&&label.color==labelColor,"글자 연출 재적용이 중첩/변색됩니다.");
        }
        finally
        {
            Object.DestroyImmediate(root);Object.DestroyImmediate(events);EventSystem.current=previous;
            if(tall!=null)Object.DestroyImmediate(tall);if(wide!=null)Object.DestroyImmediate(wide);if(texture!=null)Object.DestroyImmediate(texture);
        }
        return checks;
    }
}
#endif
