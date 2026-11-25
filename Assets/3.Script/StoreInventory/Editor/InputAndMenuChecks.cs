#if UNITY_EDITOR
using GeneralStore.Inventory;
using Object=UnityEngine.Object;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

// InputAndMenuChecks.cs
namespace GeneralStore.Inventory.Verification
{
    public static class InputAndMenuChecks
    {
        static int count;static void Check(bool value,string rule){count++;if(!value)throw new Exception("입력/중앙 메뉴 검사: "+rule);}
        public static int Run()
        {
            count=0;var four=CenteredMenuLayout.Calculate(4,86,8,1080,100,700);
            Check(four.contentHeight==368&&four.top==356&&!four.scrolls,"4개는 마지막 간격을 빼고 중앙");
            float second=four.top+94+43,third=four.top+188+43;Check((second+third)*.5f==540,"4개는2번/3번 사이가 중앙");
            var five=CenteredMenuLayout.Calculate(5,86,8,1080,100,700);Check(five.top+2*94+43==540,"5개는3번이 중앙");
            var seven=CenteredMenuLayout.Calculate(7,86,8,1080,100,700);Check(seven.top+3*94+43==540&&!seven.scrolls,"7개도 가운데4번이 중앙");
            for(int n=0;n<=60;n++)
            {
                var p=CenteredMenuLayout.Calculate(n,86,8,1080,100,700);
                Check(Math.Abs(p.top+p.viewportHeight*.5f-540)<.001f,"개수와 관계없이 표시 영역 중앙");
                Check(p.top>=100&&p.top+p.viewportHeight<=980,"많아져도 위/아래 여백 유지");
                Check(p.scrolls==(p.contentHeight>p.viewportHeight)&&p.viewportHeight<=700,"넘치면 자동 스크롤");
                if(!p.scrolls&&n>0)Check(Math.Abs((p.top+43+p.top+(n-1)*94+43)*.5f-540)<.001f,"전체 버튼 묶음의 중앙");
            }
            var many=CenteredMenuLayout.Calculate(30,86,8,1080,100,700);Check(many.scrolls&&many.viewportHeight==700&&many.contentHeight>700,"30개 이상도 잘리지 않는 스크롤 데이터");
            var small=CenteredMenuLayout.Calculate(8,86,8,600,100,700);Check(small.viewportHeight==400&&small.top==100,"작은 화면은 사용 가능한 높이 우선");
            var lease=new DragInputLease();var a=new object();var b=new object();
            Check(!lease.Acquire(null)&&!lease.Active,"없는 입력 주체 차단");
            Check(lease.Acquire(a)&&lease.IsOwner(a)&&lease.Active,"창 이동 입력 소유");
            Check(!lease.Acquire(b)&&!lease.Release(b)&&lease.IsOwner(a),"다른 창이 입력을 빼앗거나 해제하지 못함");
            Check(lease.Acquire(a)&&lease.Release(a)&&!lease.Active,"같은 창 재진입/해제");
            Check(!lease.Release(a)&&lease.Acquire(b),"반복 해제는 무시하고 다음 드래그 가능");
            lease.Reset();Check(!lease.Active&&!lease.IsOwner(b),"포커스 상실/비활성화 해제");
            var viewport=new Rect(0,0,1920,1080);var popupSize=new Vector2(340,226);
            var popup=ItemHoverTooltip.PlaceNearPointer(viewport,new Vector2(500,600),popupSize,12);
            Check(popup==new Vector2(512,588),"정보창은 마우스 오른쪽에 표시");
            popup=ItemHoverTooltip.PlaceNearPointer(viewport,new Vector2(1900,600),popupSize,12);
            Check(popup==new Vector2(1548,588),"오른쪽 화면 끝에서는 마우스 왼쪽에 표시");
            popup=ItemHoverTooltip.PlaceNearPointer(viewport,new Vector2(1900,0),popupSize,12);
            Check(popup.y-popupSize.y>=8&&popup.x>=8&&popup.x+popupSize.x<=1912,"하단/우측 경계 안쪽 유지");
            var shape=new ItemFootprint(2,3);
            for(int rotation=0;rotation<4;rotation++)
            {
                var quad=IconMeshGeometry.BuildQuad(shape,rotation,true,2f/3,.8f);
                float visibleAspect=(2f/3)*.7f/.6f;
                IconMeshGeometry.ApplyPadding(ref quad,rotation,.1f,.15f,.2f,.25f);
                float actual=(quad.x1-quad.x0)*.8f/(quad.y1-quad.y0);
                Check(Math.Abs(actual-(rotation%2==0?visibleAspect:1/visibleAspect))<.0001f,"비대칭 투명 여백/회전/직사각형 칸에서도 원본 그림 비율 유지");
            }
            return count;
        }
    }
}

// SceneLayerFeatureChecks.cs
namespace GeneralStore.Inventory.Verification
{
    // 렌더링/포인터 입력이 아니라 순수 C# 데이터와 배치 수학을 검증합니다.
    public static class SceneLayerFeatureChecks
    {
        static int count;static void Check(bool ok,string message){count++;if(!ok)throw new Exception("레이어 검사: "+message);}
        public static int Run()
        {
            count=0;Check(SceneLayerOrder.Background<SceneLayerOrder.Furniture&&SceneLayerOrder.Furniture<SceneLayerOrder.Counter&&SceneLayerOrder.Counter<SceneLayerOrder.UI,"그리기 순서");
            var mask=new PackedHitMask(new PackedHitMaskData{width=2,height=2,bits=Convert.ToBase64String(new byte[]{9})});
            Check(mask.Contains(.1f,.1f)&&mask.Contains(.9f,.9f),"마스크 왼쪽위/오른쪽아래");
            Check(!mask.Contains(.9f,.1f)&&!mask.Contains(.1f,.9f),"투명 구역");
            Check(!mask.Contains(-.1f,0)&&!mask.Contains(1,1)&&!mask.Contains(float.NaN,0),"범위 밖 입력");
            bool invalid=false;try{new PackedHitMask(new PackedHitMaskData{width=16,height=16,bits=Convert.ToBase64String(new byte[]{0})});}catch(ArgumentException){invalid=true;}Check(invalid,"잘못된 데이터 길이 거절");
            var b=new[]{.1f,.1f,.9f,.9f};var outline=new[]{.05f,.05f,.95f,.95f};
            var p=FurnitureOutlineGeometry.Align(100,200,200,400,b,outline,1);
            Check(Math.Abs(p.x+outline[0]*p.width-19)<.001f&&Math.Abs(p.y+outline[1]*p.height-39)<.001f,"투명 여백과 확대 배율을 반영한 왼쪽위");
            Check(Math.Abs((outline[2]-outline[0])*p.width-162)<.001f&&Math.Abs((outline[3]-outline[1])*p.height-322)<.001f,"실제 외곽 경계 크기");
            var q=FurnitureOutlineGeometry.Align(100,200,400,400,b,outline,0);
            Check(Math.Abs(q.x+outline[0]*q.width-120)<.001f,"비율 유지의 가로 레터박스 반영");
            return count;
        }
    }
}

// InactiveWindowReferenceChecks.cs
// Unity Editor의 Run Model Checks에서 실제 비활성 계층을 만들어 검사합니다.
// 로컬 대체 API 검사는 이 메서드를 실행하지 않고 컴파일만 합니다.
public static class InactiveWindowReferenceChecks
{
    public static void Run()
    {
        var root=new GameObject("Inactive Scroll Reference Check",typeof(RectTransform));root.SetActive(false);
        try
        {
            var scroll=root.AddComponent<ScrollRect>();
            var child=new GameObject("Inactive Grid",typeof(RectTransform));child.transform.SetParent(root.transform,false);
            if(DisplayWindowAppearance.FindScrollInParents(child.transform)!=scroll)
                throw new System.InvalidOperationException("비활성 진열창의 ScrollRect 검색 실패");
            if(DisplayWindowAppearance.FindScrollInParents(null)!=null)
                throw new System.InvalidOperationException("없는 부모 검색 결과 오류");
            Debug.Log("비활성 진열창 ScrollRect 참조 검사 통과");
        }
        finally{Object.DestroyImmediate(root);}
    }
}

// WindowShieldUnityChecks.cs
// 실제 CanvasGroup 차단/복원은 Unity Editor의 Run Model Checks에서 검사합니다.
public static class WindowShieldUnityChecks
{
    public static void Run()
    {
        var root=new GameObject("Window Shield Check",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        try
        {
            var bounds=(RectTransform)root.transform;bounds.sizeDelta=new Vector2(1920,1080);
            var moving=new GameObject("Moving",typeof(RectTransform));moving.transform.SetParent(bounds,false);
            var other=new GameObject("Behind",typeof(RectTransform));other.transform.SetParent(bounds,false);other.AddComponent<DisplayWindowView>();
            var group=other.AddComponent<CanvasGroup>();group.blocksRaycasts=true;
            var shield=root.AddComponent<WindowDragInputShield>();
            if(!shield.Begin(shield,(RectTransform)moving.transform,bounds)||group.blocksRaycasts)throw new System.Exception("뒤쪽 창 입력 차단 실패");
            shield.End(shield);if(!group.blocksRaycasts)throw new System.Exception("입력 복원 실패");
            group.blocksRaycasts=false;shield.Begin(shield,(RectTransform)moving.transform,bounds);shield.End(shield);
            if(group.blocksRaycasts)throw new System.Exception("기존 입력 비활성 상태 보존 실패");
            Debug.Log("창 드래그 입력 차단/원래 상태 복원 검사 통과");
        }
        finally{Object.DestroyImmediate(root);}
    }
}

// ManagementWindowTransitionChecks.cs
public static class ManagementWindowTransitionChecks
{
    public static int Run()
    {
        var root=new GameObject("Menu Transition Check",typeof(RectTransform));int checks=0;
        try
        {
            var panel=root.AddComponent<InventoryPanel>();panel.drag=root.AddComponent<InventoryDragHandler>();panel.shop=root.AddComponent<ShopfrontUI>();
            var menu=root.AddComponent<RightSideMenu>();menu.panel=panel;
            var windows=new GameObject[3];var motions=new ManagementWindowMotion[3];int[] opens=new int[3];
            for(int n=0;n<3;n++)
            {
                int index=n;windows[n]=new GameObject("Window "+n,typeof(RectTransform));windows[n].transform.SetParent(root.transform,false);
                var frame=new GameObject("Panel Frame",typeof(RectTransform));frame.transform.SetParent(windows[n].transform,false);
                motions[n]=ManagementWindowMotion.Ensure(windows[n],(RectTransform)frame.transform);windows[n].SetActive(false);
                var entry=new ShopMenuEntry{id=n.ToString(),window=windows[n]};
                entry.onOpen.AddListener(()=>{opens[index]++;windows[index].SetActive(true);});menu.AddEntry(entry);
            }
            var tick=typeof(RightSideMenu).GetMethod("ContinueWindowSwitch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            menu.Toggle("0");motions[0].Advance(1);
            if(!windows[0].activeSelf||opens[0]!=1)throw new Exception("첫 관리 창 열기 실패");checks++;
            menu.Toggle("1");motions[0].Advance(.1f);tick.Invoke(menu,null);
            if(!windows[0].activeSelf||windows[1].activeSelf||motions[0].IsClosed)throw new Exception("퇴장 완료 전 새 창이 열렸습니다.");checks++;
            var input=motions[0].frame.GetComponent<CanvasGroup>();
            if(input==null||input.blocksRaycasts||input.interactable)throw new Exception("퇴장 중 창 입력 차단 실패");checks++;
            menu.Toggle("2");motions[0].Advance(1);tick.Invoke(menu,null);
            if(windows[0].activeSelf||windows[1].activeSelf||!windows[2].activeSelf||opens[1]!=0||opens[2]!=1)throw new Exception("전환 도중 마지막 선택만 여는 처리 실패");checks++;
            motions[2].Advance(1);menu.Toggle("2");motions[2].Advance(1);tick.Invoke(menu,null);
            if(windows[2].activeSelf||opens[2]!=1)throw new Exception("선택 메뉴 재클릭으로 닫기 실패");checks++;
            // Reset the lifecycle explicitly for deterministic Edit-mode stepping.
            menu.Toggle("0");motions[0].SendMessage("OnEnable");motions[0].Advance(1);menu.Toggle("1");menu.Toggle("1");motions[0].Advance(1);tick.Invoke(menu,null);
            if(windows[0].activeSelf||windows[1].activeSelf||opens[1]!=0)throw new Exception("대기 중 메뉴 재클릭 취소 실패");checks++;
            menu.entries[2].unlocked=false;menu.Toggle("2");
            if(windows[2].activeSelf||opens[2]!=1)throw new Exception("잠긴 메뉴 열림");checks++;
            menu.Toggle("0");motions[0].SendMessage("OnEnable");motions[0].Advance(.1f);menu.Toggle("1");
            motions[0].Advance(.1f);tick.Invoke(menu,null);
            if(windows[1].activeSelf)throw new Exception("등장 도중 전환 시 퇴장 대기 실패");checks++;
            windows[0].SetActive(false);tick.Invoke(menu,null);
            if(!windows[1].activeSelf||opens[1]!=1)throw new Exception("이전 창이 외부에서 닫힌 뒤 전환 실패");checks++;
            return checks;
        }
        finally{Object.DestroyImmediate(root);}
    }
}

// ParchmentManagementChecks.cs
public static class ParchmentManagementChecks
{
    public static int Run()
    {
        var root=new GameObject("Parchment Management Check",typeof(RectTransform));int count=0;
        try
        {
            var ui=root.AddComponent<DisplayManagementUI>();SideMenuSceneBuilder.BuildManagement(root.transform,ui);
            var frame=ui.window.transform.Find("Panel Frame") as RectTransform;var paper=frame.GetComponent<ParchmentPanelGraphic>();
            if(!ui.parchmentStyle||paper==null||paper.artwork==null||Math.Abs(frame.rect.width/frame.rect.height-1006f/1564)>.0001f)throw new Exception("관리창 원본 비율/공통 Texture 연결 실패");count++;
            var displayScroll=DisplayWindowAppearance.FindScrollInParents(ui.displayList);var stockScroll=DisplayWindowAppearance.FindScrollInParents(ui.stockList);
            if(displayScroll==null||stockScroll==null||displayScroll.horizontal||stockScroll.horizontal||!displayScroll.vertical||!stockScroll.vertical)throw new Exception("관리창 독립 세로 스크롤 연결 실패");count++;
            if(Math.Abs(displayScroll.viewport.rect.height-ui.displayRowHeight*3)>.01f||Math.Abs(stockScroll.viewport.rect.height-ui.stockRowHeight*3)>.01f)throw new Exception("목록별 3개 행 표시 영역 실패");count++;
            var row=Object.Instantiate(ui.cardTemplate,ui.displayList);row.gameObject.SetActive(true);
            row.Bind("first",id=>{},"첫 진열장","남은 25칸","",null,false);
            if(row.selectionArtwork.gameObject.activeSelf)throw new Exception("선택하지 않은 행에 강조가 남았습니다.");count++;
            row.Bind("first",id=>{},"첫 진열장","남은 25칸","",null,true);
            if(!row.selectionArtwork.gameObject.activeSelf)throw new Exception("선택 강조가 행에 연결되지 않았습니다.");count++;
            var next=Object.Instantiate(ui.cardTemplate,ui.displayList);next.gameObject.SetActive(true);
            row.Bind("first",id=>{},"첫 진열장","남은 25칸","",null,false);next.Bind("second",id=>{},"다음 진열장","남은 36칸","",null,true);
            if(row.selectionArtwork.gameObject.activeSelf||!next.selectionArtwork.gameObject.activeSelf)throw new Exception("선택 행 변경 후 강조 이동 실패");count++;
            using(var mesh=new VertexHelper())
            {
                typeof(ParchmentPanelGraphic).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(paper,new object[]{mesh});
                if(mesh.currentVertCount==0||mesh.currentVertCount>100)throw new Exception("관리창 종이 메시가 비었거나 과도합니다.");
                for(int n=0;n<mesh.currentVertCount;n++)
                {
                    var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,n);
                    if(vertex.uv0.x<0||vertex.uv0.x>1||vertex.uv0.y<0||vertex.uv0.y>1||vertex.position.x<frame.rect.xMin-.001f||vertex.position.x>frame.rect.xMax+.001f||vertex.position.y<frame.rect.yMin-.001f||vertex.position.y>frame.rect.yMax+.001f)throw new Exception("관리창 메시/UV 범위 오류");
                }
                count++;
            }
            if(ui.openButton.onClick.GetPersistentEventCount()!=1||ui.emptyDisplayLabel==null||ui.emptyStockLabel==null)throw new Exception("열기 버튼/빈 목록 연결 실패");count++;
            return count;
        }
        finally{Object.DestroyImmediate(root);}
    }
}

// CompactDemoUnityChecks.cs
public static class CompactDemoUnityChecks
{
    // 실제 Unity 초기화/이벤트 계층 검사입니다. 이 제작 환경에서는 실행하지 못했습니다.
    public static int Run()
    {
        var go=new GameObject("Compact Demo Initialization Check");go.SetActive(false);GameObject ui=null,tempEvents=null;
        try
        {
            var runtime=go.AddComponent<InventoryRuntime>();runtime.initialWarehouseCount=2;runtime.initialGold=10000;ItemTableTools.Configure(runtime);
            runtime.initialFurnitureForDemo=new[]{"mannequin","table","weapon_rack"};string error;
            if(!runtime.TryInitialize(out error))throw new Exception(error);
            var ids=runtime.Model.WarehouseIds();
            if(ids.Count!=2||ids[0]!="warehouse"||ids[1]!="warehouse_2")throw new Exception("초기 상자2개 검사 실패");
            foreach(string id in ids){var box=runtime.Model.GetContainer(id);if(box.columns!=8||box.rows!=7)throw new Exception("상자8×7 검사 실패");}
            if(runtime.Model.Gold!=10000)throw new Exception("기본 상자 추가로 소지금이 차감되었습니다.");
            if(runtime.Model.DisplayIds().Count!=3)throw new Exception("초기 진열장3개 검사 실패");
            ui=new GameObject("Compact Icon Input Check",typeof(RectTransform),typeof(Canvas));
            var panel=ui.AddComponent<InventoryPanel>();panel.inventory=runtime;panel.canvas=ui.GetComponent<Canvas>();
            var gridGo=new GameObject("Grid",typeof(RectTransform));gridGo.transform.SetParent(ui.transform,false);var grid=gridGo.AddComponent<InventoryGridView>();
            var itemGo=new GameObject("Item",typeof(RectTransform),typeof(global::UnityEngine.UI.Image),typeof(CanvasGroup));itemGo.transform.SetParent(gridGo.transform,false);
            var view=itemGo.AddComponent<InventoryItemView>();var iconGo=new GameObject("Icon",typeof(RectTransform));iconGo.transform.SetParent(itemGo.transform,false);view.icon=iconGo.AddComponent<InventoryIconGraphic>();
            var labelGo=new GameObject("Name",typeof(RectTransform));labelGo.transform.SetParent(itemGo.transform,false);view.nameLabel=labelGo.AddComponent<global::UnityEngine.UI.Text>();view.nameLabel.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");view.nameLabel.raycastTarget=false;
            var record=runtime.Model.GetContainer("warehouse").items[0];view.Bind(panel,grid,record);
            if(itemGo.GetComponent<global::UnityEngine.UI.Image>().raycastTarget||!view.icon.raycastTarget)throw new Exception("아이콘 직접 입력 설정 실패");
            if(ExecuteEvents.GetEventHandler<IPointerClickHandler>(iconGo)!=itemGo||ExecuteEvents.GetEventHandler<IBeginDragHandler>(iconGo)!=itemGo)throw new Exception("아이콘에서 물건 컴포넌트로 이벤트 전달 실패");
            var events=EventSystem.current;if(events==null){tempEvents=new GameObject("Temporary EventSystem",typeof(EventSystem));events=tempEvents.GetComponent<EventSystem>();}
            long revision=runtime.Model.Revision;var pointer=new PointerEventData(events){button=PointerEventData.InputButton.Left};ExecuteEvents.ExecuteHierarchy(iconGo,pointer,ExecuteEvents.pointerClickHandler);
            if(runtime.Model.Revision!=revision)throw new Exception("아이콘 클릭은 재고 상태를 변경하지 않아야 합니다.");
            return 8;
        }
        finally
        {
            if(ui!=null)global::UnityEngine.Object.DestroyImmediate(ui);
            if(tempEvents!=null)global::UnityEngine.Object.DestroyImmediate(tempEvents);
            global::UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
#endif
