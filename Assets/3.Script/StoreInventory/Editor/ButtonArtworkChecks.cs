#if UNITY_EDITOR
using System;
using GeneralStore.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ButtonArtworkChecks
{
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks++;}
    public static int Run()
    {
        checks=0;ButtonArtworkEditor.ValidateSources();ButtonArtworkEditor.PrepareTextures();
        var catalog=ButtonArtworkEditor.EnsureCatalog();
        Check(catalog.management.Length==6&&catalog.furniture.Length==12,"버튼 상태 카탈로그 수 오류");
        foreach(var set in catalog.management)Check(set.normal!=null&&set.pressed!=null&&set.active!=null,"관리 버튼 3상태 누락: "+set.id);
        foreach(var set in catalog.furniture)Check(set.normal!=null&&set.pressed!=null&&set.active!=null,"가구 버튼 3상태 누락: "+set.id);
        Check(catalog.close.normal!=null&&catalog.close.pressed!=null,"X 버튼 2상태 누락");
        var root=new GameObject("Button Sprite State Check",typeof(RectTransform));
        var previous=EventSystem.current;var events=new GameObject("Button Sprite Event Check",typeof(EventSystem));
        try
        {
            var system=events.GetComponent<EventSystem>();EventSystem.current=system;
            var hit=InventorySceneBuilder.Box("Button",root.transform,17,23,84,84,Color.clear);
            var button=hit.gameObject.AddComponent<Button>();var icon=InventorySceneBuilder.IconSlot("Icon",hit.transform,6,7,72,70);
            var slot=(RectTransform)icon.transform.parent;var position=slot.anchoredPosition;var size=slot.sizeDelta;
            var set=catalog.Management("displays");Check(set!=null,"진열장 관리 버튼 매핑 누락");
            ButtonArtworkCatalog.Apply(button,icon,set,false);
            Check(button.transition==Selectable.Transition.SpriteSwap&&button.targetGraphic==icon,"SpriteSwap 대상 연결 실패");
            Check(icon.sprite==set.normal&&icon.color==Color.white,"기본 상태가 틴트/다른 Sprite를 사용합니다.");
            var pointer=new PointerEventData(system){button=PointerEventData.InputButton.Left};
            button.OnPointerEnter(pointer);button.OnSelect(new BaseEventData(system));
            Check(icon.overrideSprite==set.normal,"호버/키보드 포커스가 닫힌 메뉴를 선택 상태로 만듭니다.");
            button.OnPointerDown(pointer);Check(icon.overrideSprite==set.pressed,"마우스 누름 Sprite 실패");
            button.OnPointerExit(pointer);button.OnPointerUp(pointer);
            Check(icon.overrideSprite==set.normal,"바깥에서 놓은 버튼이 눌린 이미지로 남습니다.");
            ButtonArtworkCatalog.Apply(button,icon,set,true);button.OnDeselect(new BaseEventData(system));
            Check(icon.sprite==set.active&&icon.overrideSprite==set.active,"창 선택 상태가 포커스 해제로 지워집니다.");
            button.OnPointerEnter(pointer);button.OnPointerDown(pointer);
            Check(icon.overrideSprite==set.pressed,"선택된 메뉴의 누름 Sprite 실패");
            ButtonArtworkCatalog.Apply(button,icon,set,true);
            Check(icon.overrideSprite==set.pressed,"메뉴 갱신이 눌린 상태를 덮어씁니다.");
            button.OnPointerUp(pointer);Check(icon.overrideSprite==set.active,"놓은 뒤 기존 선택 상태 복원 실패");
            ButtonArtworkCatalog.Apply(button,icon,set,false);Check(icon.overrideSprite==set.normal,"단축키/닫기 후 기본 상태 복원 실패");
            button.interactable=false;button.OnPointerDown(pointer);Check(icon.overrideSprite!=set.pressed,"비활성 버튼에 누름 상태 적용");
            Check(slot.anchoredPosition==position&&slot.sizeDelta==size,"상태 전환이 Hierarchy 아이콘 Slot 배치를 변경했습니다.");
            button.interactable=true;ButtonArtworkCatalog.Apply(button,icon,catalog.close,false);
            Check(button.spriteState.pressedSprite==catalog.close.pressed&&button.spriteState.selectedSprite==catalog.close.normal,"X 버튼은 기본/누름 두 상태만 사용해야 합니다.");
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(events);EventSystem.current=previous;}
        return checks;
    }
}
#endif
