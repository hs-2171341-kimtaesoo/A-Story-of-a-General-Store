using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    public sealed class FloorDropArea : MonoBehaviour,IDropHandler
    {
        public InventoryPanel panel;
        public FloorItemView itemTemplate;
        public ParcelView parcelTemplate;
        public Sprite parcelSprite;
        [Min(16)] public float iconSize=64,parcelSize=96;
        [Range(0,.85f)] public float counterHeightRatio=.2f;
        [Min(1)] public float gravity=2000;
        [Range(0,.8f)] public float bounce=.18f;
        [Min(1)] public float sleepSpeed=35;
        [Min(0)] public float burstHorizontalSpeed=220,burstUpwardSpeed=650;
        [Range(.05f,.95f)] public float parcelPositionX=.84f;
        public RectTransform surfaceLine;
        public bool showCounterLine=true;
        readonly Dictionary<string,FloorItemView> views=new Dictionary<string,FloorItemView>();
        readonly Dictionary<string,ParcelView> boxes=new Dictionary<string,ParcelView>();
        readonly List<FloorMotionState> motion=new List<FloorMotionState>();
        readonly HashSet<string> seen=new HashSet<string>();
        readonly List<string> removed=new List<string>();
        readonly FloorPhysicsSettings physics=new FloorPhysicsSettings();
        bool ready,dirty;
        float lastParcelX=-1,lastSurface=-1,lastParcelSize=-1,lastWidth=-1,lastHeight=-1;
        RectTransform Rect {get{return (RectTransform)transform;}}
        void Start()
        {
            string error;
            if(panel==null||itemTemplate==null||parcelTemplate==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("바닥/택배 UI 참조를 확인하세요. 새 데모 씬을 생성하세요.",this);enabled=false;return;}
            UiPerformanceLayers.Ensure(gameObject,true);
            panel.Model.Changed+=MarkDirty;ready=true;dirty=true;
            if(surfaceLine!=null)
            {surfaceLine.anchorMin=new Vector2(0,0);surfaceLine.anchorMax=new Vector2(1,0);surfaceLine.pivot=new Vector2(.5f,.5f);surfaceLine.sizeDelta=new Vector2(0,2);}
        }
        void MarkDirty(){dirty=true;}
        void PreparePhysics()
        {
            float w=Mathf.Max(1,Rect.rect.width),h=Mathf.Max(1,Rect.rect.height);
            physics.gravity=gravity/h;physics.surfaceY=counterHeightRatio;
            physics.radiusX=iconSize*.5f/w;physics.radiusY=iconSize*.5f/h;
            physics.restitution=bounce;physics.sleepSpeed=sleepSpeed/h;physics.Validate();
        }
        float BoxCenterY {get{return Mathf.Min(1-parcelSize*.5f/Rect.rect.height,counterHeightRatio+parcelSize*.5f/Rect.rect.height);}}
        void Position(RectTransform rect,float x,float y,float size)
        {
            if(rect.anchorMin.x!=.5f||rect.anchorMin.y!=.5f||rect.anchorMax.x!=.5f||rect.anchorMax.y!=.5f||rect.pivot.x!=.5f||rect.pivot.y!=.5f)
                rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            Vector2 p=new Vector2((x-.5f)*Rect.rect.width,(y-.5f)*Rect.rect.height);
            if(rect.anchoredPosition.x!=p.x||rect.anchoredPosition.y!=p.y)rect.anchoredPosition=p;
            if(rect.sizeDelta.x!=size||rect.sizeDelta.y!=size)rect.sizeDelta=Vector2.one*size;
        }
        void SyncViews()
        {
            // 같은 UID는 재사용합니다. 지출/예약/다른 물건 변경 때 기존 카드를 다시 만들지 않습니다.
            seen.Clear();
            foreach(var floor in panel.Model.FloorItems())
            {
                seen.Add(floor.item.instanceId);FloorItemView view;
                if(!views.TryGetValue(floor.item.instanceId,out view))
                {view=Instantiate(itemTemplate,transform);view.gameObject.SetActive(true);views.Add(floor.item.instanceId,view);}
                view.Bind(this,floor);
            }
            removed.Clear();foreach(var pair in views)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(var id in removed){views[id].gameObject.SetActive(false);Destroy(views[id].gameObject);views.Remove(id);}
            seen.Clear();
            foreach(var parcel in panel.Model.ParcelSummaries())
            {
                seen.Add(parcel.id);ParcelView view;
                if(!boxes.TryGetValue(parcel.id,out view))
                {view=Instantiate(parcelTemplate,transform);view.gameObject.SetActive(true);boxes.Add(parcel.id,view);}
                view.Bind(this,parcel);Position((RectTransform)view.transform,parcelPositionX,BoxCenterY,parcelSize);
            }
            removed.Clear();foreach(var pair in boxes)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(var id in removed){boxes[id].gameObject.SetActive(false);Destroy(boxes[id].gameObject);boxes.Remove(id);}
        }
        void LateUpdate()
        {
            if(!ready)return;
            try
            {
                PreparePhysics();
                if(lastParcelX!=parcelPositionX||lastSurface!=counterHeightRatio||lastParcelSize!=parcelSize||lastWidth!=Rect.rect.width||lastHeight!=Rect.rect.height)
                {lastParcelX=parcelPositionX;lastSurface=counterHeightRatio;lastParcelSize=parcelSize;lastWidth=Rect.rect.width;lastHeight=Rect.rect.height;dirty=true;}
                if(dirty){dirty=false;using(StoreProfiler.FloorViews.Auto())SyncViews();}
                // 시간 정지 테스트는 영업 시계만 멈춥니다. 낙하는 현실 프레임 시간으로 진행합니다.
                using(StoreProfiler.FloorMotion.Auto())panel.Model.AdvanceFloorMotion(Mathf.Min(.1f,Time.deltaTime),physics,panel.drag.HeldFloorId,motion);
                foreach(var p in motion)
                {FloorItemView view;if(views.TryGetValue(p.id,out view))Position((RectTransform)view.transform,p.x,p.y,iconSize);}
                if(surfaceLine!=null)
                {
                    if(surfaceLine.gameObject.activeSelf!=showCounterLine)surfaceLine.gameObject.SetActive(showCounterLine);
                    float y=Rect.rect.height*counterHeightRatio;
                    if(surfaceLine.anchoredPosition.y!=y)surfaceLine.anchoredPosition=new Vector2(0,y);
                }
            }
            catch(ArgumentException e){enabled=false;panel.ShowMessage(e.Message+" 바닥 크기/낙하 수치를 확인한 뒤 재시작하세요.");}
        }
        public void OpenParcel(string id)
        {
            string error;
            bool success=panel.Model.TryOpenParcel(id,burstHorizontalSpeed/Rect.rect.width,burstUpwardSpeed/Rect.rect.height,parcelPositionX,BoxCenterY,out error);
            panel.ShowMessage(success?"택배를 열었습니다. 물건을 창고로 정리하세요.":error);
        }
        public void OnDrop(PointerEventData e)
        {
            Vector2 local;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect,e.position,panel.EventCamera,out local))return;
            PreparePhysics();
            float x=Mathf.Clamp((local.x-Rect.rect.xMin)/Rect.rect.width,physics.radiusX,1-physics.radiusX);
            float y=Mathf.Clamp((local.y-Rect.rect.yMin)/Rect.rect.height,physics.radiusY,1-physics.radiusY);
            panel.drag.DropToFloor(x,y);
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null)panel.Model.Changed-=MarkDirty;}
    }
}
