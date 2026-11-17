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
}
#endif
