using System;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 가구 데이터(칸/허용 아이템)와 외형(Sprite/색/여백)을 분리합니다.
    // 독립 창은 첫 오픈에만 생성하고 재사용합니다. 재고는 복제하지 않습니다.
    [Serializable]
    public sealed class DisplayAppearanceProfile
    {
        public string typeId="shelf";public Vector2 initialPosition=new Vector2(960,-146);
        public Sprite frameSprite; // 물건/격자가 없는 투명 프레임 이미지를 연결하세요.
        public Color frameColor=new Color(.23f,.16f,.09f);
        public Color insetColor=new Color(.12f,.1f,.07f);
        public float sidePadding=24,topPadding=164,bottomPadding=220;
        public bool closeBelowFrame;
        public float minimumWidth=440,maximumGridWidth=624,maximumGridHeight=520;
    }
    public sealed class DisplayWindowAppearance : MonoBehaviour
    {
        public bool independentWindow;
        public RectTransform window,bounds,header,closeButton,tabStrip,title,status,finishButton,details;
        public Text detailsName,detailsBody;
        public ScrollRect scroll;
        public InventoryGridView grid;
        public global::UnityEngine.UI.Image frame;
        public DisplayAppearanceProfile[] profiles;
        string currentType;int currentColumns,currentRows;

        // GetComponentInParent의 기본 검색은 비활성 부모를 건너뜁니다.
        // Transform 부모를 직접 따라가면 아직 열지 않은 템플릿에서도 정확히 찾습니다.
        public static ScrollRect FindScrollInParents(Transform child)
        {
            for(Transform node=child;node!=null;node=node.parent)
            {var found=node.GetComponent<ScrollRect>();if(found!=null)return found;}
            return null;
        }
        // DisplayUI의 dirty 갱신에서 호출합니다. 매 프레임 레이아웃을 계산하지 않습니다.
        public void Apply(string typeId,int columns,int rows)
        {
            // 이미 생성/저장된 씬의 누락된 참조도 복구합니다. 첫 연결 이후에는 검색하지 않습니다.
            if(scroll==null&&grid!=null)scroll=FindScrollInParents(grid.transform);
            if(scroll==null||scroll.viewport==null)
            {Debug.LogError("진열창 ScrollRect/Viewport 연결이 없습니다. DisplayWindowAppearance.Scroll에 격자 부모의 ScrollRect를 연결하세요.",this);return;}
            if(currentType==typeId&&currentColumns==columns&&currentRows==rows)return;
            DisplayAppearanceProfile p=null;
            if(profiles!=null)foreach(var candidate in profiles)if(candidate!=null&&candidate.typeId==typeId){p=candidate;break;}
            if(p==null)p=new DisplayAppearanceProfile(); // 새 가구에 프로필이 없으면 기본 외형으로 엽니다.
            var layout=DisplayWindowLayout.Calculate(columns,rows,grid.cellWidth,grid.cellHeight,p.sidePadding,p.topPadding,p.bottomPadding,p.minimumWidth,p.maximumGridWidth,p.maximumGridHeight);
            window.sizeDelta=new Vector2(layout.width,layout.height);
            frame.sprite=p.frameSprite;frame.color=p.frameSprite==null?p.frameColor:Color.white;
            frame.preserveAspect=false; // 프레임만 창 크기에 맞춥니다. 아이템 이미지 비율은 유지합니다.
            float w=layout.width,h=layout.height;
            Place(header,0,0,independentWindow?w:w-62,64);
            var headerText=header.GetComponentsInChildren<Text>(true)[0];Place(headerText.rectTransform,20,12,w-96,42);headerText.text=independentWindow?"":"진열장 · 드래그로 이동";if(closeButton!=null)Place(closeButton,w-58,16,40,40);
            if(tabStrip!=null)Place(tabStrip,24,70,w-48,48);Place(title,24,independentWindow?18:122,w-48,34);
            float x=(w-layout.gridWidth-22)*.5f;
            Place((RectTransform)scroll.transform,x,p.topPadding,layout.gridWidth+22,layout.gridHeight+22);
            Place(scroll.viewport,0,0,layout.gridWidth,layout.gridHeight);
            if(scroll.horizontalScrollbar!=null)Place((RectTransform)scroll.horizontalScrollbar.transform,0,layout.gridHeight+4,layout.gridWidth,18);
            if(scroll.verticalScrollbar!=null)Place((RectTransform)scroll.verticalScrollbar.transform,layout.gridWidth+4,0,18,layout.gridHeight);
            scroll.horizontal=columns*grid.cellWidth>layout.gridWidth;
            scroll.vertical=rows*grid.cellHeight>layout.gridHeight;
            scroll.viewport.GetComponent<global::UnityEngine.UI.Image>().color=p.insetColor;
            Place(details,20,h-214,w-40,136);
            Place(detailsName.rectTransform,112,8,w-164,32);Place(detailsBody.rectTransform,112,44,w-164,86);
            Place(status,24,h-76,w-48,32);Place(finishButton,w-194,h-42,170,34);
            if(independentWindow)
            {
                // closeBelowFrame은 이전 씬 호환 필드만 남기고 실제 배치는 우측 하단으로 통일합니다.
                Place(frame.rectTransform,0,0,w,h);Place(details,20,h-166,w-40,100);
                Place(detailsBody.rectTransform,112,44,w-164,52);
                Place(status,24,h-56,w-224,44);Place(finishButton,w-194,h-54,170,40);
            }
            var detailView=details.GetComponent<InventoryDetailsView>();if(detailView!=null)detailView.AlignIconFrame();
            // 버튼 안의 글자도 함께 배치합니다. 자식별 Update/LayoutGroup은 없습니다.
            var label=finishButton.GetComponentsInChildren<Text>(true)[0];if(label!=null)Place(label.rectTransform,0,0,170,independentWindow?40:34);
            scroll.horizontalNormalizedPosition=0;scroll.verticalNormalizedPosition=1;
            if(currentType==null)window.anchoredPosition=p.initialPosition;
            var pos=WindowDragGeometry.Clamp(window.anchoredPosition.x,window.anchoredPosition.y,w,h,bounds.rect.width,bounds.rect.height);
            window.anchoredPosition=new Vector2(pos.x,pos.y);
            currentType=typeId;currentColumns=columns;currentRows=rows;
        }
        static void Place(RectTransform r,float x,float y,float width,float height)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    }
}
