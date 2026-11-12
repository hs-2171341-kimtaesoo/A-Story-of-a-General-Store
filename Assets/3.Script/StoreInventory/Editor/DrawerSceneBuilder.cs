#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class DrawerSceneBuilder
{
    public static void Hotspots(Transform parent,InventoryPanel panel,DisplayUI displays)
    {
        Add(parent,panel,displays,"shelf",330,200,720,640);Add(parent,panel,displays,"wall_shelf",1630,270,250,220);
        Add(parent,panel,displays,"weapon_rack",80,650,280,300);Add(parent,panel,displays,"table",1610,630,280,250);
    }
    static void Add(Transform parent,InventoryPanel panel,DisplayUI displays,string type,float x,float y,float w,float h)
    {
        var root=UI.Box("Hotspot "+type,parent,x,y,w,h,new Color(0,0,0,0));root.canvasRenderer.cullTransparentMesh=false;
        var spot=root.gameObject.AddComponent<SceneFurnitureHotspot>();spot.panel=panel;spot.displays=displays;spot.typeId=type;
        spot.furnitureImage=UI.Box("Furniture Sprite",root.transform,0,0,w,h,new Color(.3f,.23f,.12f,.25f));spot.furnitureImage.raycastTarget=false;
        spot.highlightImage=UI.Box("Highlight Sprite",root.transform,0,0,w,h,new Color(1,.75f,.15f,.18f));spot.highlightImage.raycastTarget=false;spot.highlightImage.enabled=false;
        spot.label=UI.Label("Furniture Label",root.transform,w-190,h*.42f,180,64,type,21);
    }
    public static void MoveDebugButtons(RectTransform layout)
    {
        // 개발용 버튼은 기본 배경을 가리지 않도록 하나의 접이식 메뉴로 모읍니다.
        var root=UI.Box("Debug Controls",layout,24,70,430,660,new Color(.12f,.1f,.07f,.96f));var toggle=root.gameObject.AddComponent<DebugPanelToggle>();toggle.window=root.gameObject;
        var buttons=layout.GetComponentsInChildren<Button>(true);int index=0;
        foreach(var button in buttons)
        {
            string name=button.gameObject.name;
            if(name!="손님 맞이"&&name!="저장"&&name!="불러오기"&&name!="시간 정지/재개"&&name!="다음 단계 테스트"&&name!="다음 새벽 배송 테스트"&&name!="진열장 관리")continue;
            button.transform.SetParent(root.transform,false);var r=(RectTransform)button.transform;r.anchoredPosition=new Vector2(14,-14-index*84);r.sizeDelta=new Vector2(402,68);
            button.GetComponentsInChildren<Text>(true)[0].rectTransform.sizeDelta=new Vector2(402,68);index++;
        }
        foreach(var button in buttons)if(button.gameObject.name=="업그레이드"||button.gameObject.name=="거래처 열기/닫기")
        {
            bool furniture=button.gameObject.name=="업그레이드";var r=(RectTransform)button.transform;r.anchoredPosition=new Vector2(furniture?1480:1690,-1020);r.sizeDelta=new Vector2(200,42);
            var text=button.GetComponentsInChildren<Text>(true)[0];text.text=furniture?"가구 구매":"거래처";text.rectTransform.sizeDelta=new Vector2(200,42);
        }
        foreach(var text in layout.GetComponentsInChildren<Text>(true))if(text.gameObject.name=="Floor Hint")text.gameObject.SetActive(false);
        UI.ButtonBox("테스트",layout,1810,970,86,36,toggle.Toggle);root.gameObject.SetActive(false);
    }
}
#endif
