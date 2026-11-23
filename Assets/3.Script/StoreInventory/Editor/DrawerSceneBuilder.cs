#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class DrawerSceneBuilder
{
    public static void MoveDebugButtons(RectTransform layout)
    {
        // 개발용 버튼은 기본 배경을 가리지 않도록 하나의 접이식 메뉴로 모읍니다.
        var root=UI.Box("Debug Controls",layout,24,70,430,470,new Color(.12f,.1f,.07f,.96f));var toggle=root.gameObject.AddComponent<DebugPanelToggle>();toggle.window=root.gameObject;
        var buttons=layout.GetComponentsInChildren<Button>(true);int index=0;
        foreach(var button in buttons)
        {
            string name=button.gameObject.name;
            if(name!="손님 맞이"&&name!="저장"&&name!="불러오기"&&name!="시간 정지/재개"&&name!="다음 단계 테스트"&&name!="다음 새벽 배송 테스트"&&name!="진열장 관리"&&name!="거래처 해금 테스트"&&name!="거래처 중단 테스트"&&name!="거래처 재개 테스트")continue;
            button.transform.SetParent(root.transform,false);var r=(RectTransform)button.transform;r.anchoredPosition=new Vector2(14+(index%2)*208,-14-(index/2)*84);r.sizeDelta=new Vector2(194,68);
            button.GetComponentsInChildren<Text>(true)[0].rectTransform.sizeDelta=new Vector2(194,68);index++;
        }
        foreach(var button in buttons)if(button.gameObject.name=="업그레이드"||button.gameObject.name=="거래처 열기/닫기")
        {
            bool furniture=button.gameObject.name=="업그레이드";var r=(RectTransform)button.transform;r.anchoredPosition=new Vector2(furniture?1480:1690,-1020);r.sizeDelta=new Vector2(200,42);
            var text=button.GetComponentsInChildren<Text>(true)[0];text.text=furniture?"가구 구매":"거래처";text.rectTransform.sizeDelta=new Vector2(200,42);
        }
        UI.ButtonBox("테스트",layout,1810,970,86,36,toggle.Toggle);root.gameObject.SetActive(false);
    }
    public static void EnsureItemIdGrant(RectTransform layout,ShopSimulation simulation)
    {
        if(layout==null||simulation==null)return;
        var debug=layout.Find("Debug Controls") as RectTransform;if(debug==null)return;
        if(debug.Find("Item ID Grant")!=null)return;
        float top=14;
        foreach(Transform child in debug)
        {var rect=child as RectTransform;if(rect!=null)top=Mathf.Max(top,-rect.anchoredPosition.y+rect.sizeDelta.y+14);}
        var frame=UI.Box("Item ID Grant",debug,14,top,402,148,new Color(.2f,.15f,.09f));frame.raycastTarget=false;
        UI.Label("Title",frame.transform,8,4,386,26,"아이템 ID로 받기",18);
        simulation.debugItemIdInput=GrantInput(frame.transform,"Item ID",8,180,simulation.debugItemId,false);
        simulation.debugItemAmountInput=GrantInput(frame.transform,"Amount",196,58,simulation.debugItemAmount.ToString(),true);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(simulation.debugItemIdInput.onEndEdit,simulation.DebugItemIdEdited);
        UI.ButtonBox("받기",frame.transform,262,38,132,38,simulation.GrantDebugItem);
        var quality=UI.ButtonBox("품질: 평범",frame.transform,8,86,386,34,simulation.CycleDebugItemQuality);
        simulation.debugItemQualityLabel=quality.GetComponentInChildren<Text>(true);simulation.debugItemQualityLabel.fontSize=17;
        UI.Label("Destination",frame.transform,8,126,386,18,"상자 우선 · 공간이 부족하면 임시 저장고로",12);
        debug.sizeDelta=new Vector2(debug.sizeDelta.x,Mathf.Max(debug.sizeDelta.y,top+162));
        UnityEditor.EditorUtility.SetDirty(simulation);
    }
    static InputField GrantInput(Transform parent,string name,float x,float width,string value,bool integer)
    {
        var image=UI.Box(name,parent,x,38,width,38,new Color(.07f,.06f,.04f));
        var field=image.gameObject.AddComponent<InputField>();field.targetGraphic=image;
        field.contentType=integer?InputField.ContentType.IntegerNumber:InputField.ContentType.Standard;field.characterLimit=integer?3:64;
        field.textComponent=UI.Label("Text",image.transform,6,4,width-12,30,"",19);field.textComponent.alignment=TextAnchor.MiddleLeft;
        field.text=value;return field;
    }
}
#endif
