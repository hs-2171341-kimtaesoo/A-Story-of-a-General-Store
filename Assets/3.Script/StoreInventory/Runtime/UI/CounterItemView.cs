using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // Visual body only: inventory ownership and reservation remain in RecommendationSession.
    public sealed class CounterItemView:MonoBehaviour,IPointerClickHandler,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IDropHandler,ICanvasRaycastFilter
    {
        public global::UnityEngine.UI.Image image;public global::UnityEngine.UI.Text fallbackLabel;
        public CounterTradeUI Trade{get{return owner;}}
        public readonly CounterBodyState Body=new CounterBodyState();
        CounterTradeUI owner;string uid,containerId;bool picked,suppressClick;Vector2 grabOffset,lastPointer;float lastTime;
        RectTransform orientation;SpriteIconFit fit;global::UnityEngine.UI.Image fallback;
        void EnsureVisuals()
        {
            if(orientation!=null)return;
            var rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);
            fallback=GetComponent<global::UnityEngine.UI.Image>();
            var go=new GameObject("Visual Orientation",typeof(RectTransform));orientation=(RectTransform)go.transform;orientation.SetParent(transform,false);orientation.anchorMin=orientation.anchorMax=orientation.pivot=new Vector2(.5f,.5f);
            go=new GameObject("Item Sprite",typeof(RectTransform),typeof(CanvasRenderer),typeof(global::UnityEngine.UI.Image));go.transform.SetParent(orientation,false);image=go.GetComponent<global::UnityEngine.UI.Image>();
            fit=orientation.gameObject.AddComponent<SpriteIconFit>();fit.frame=orientation;fit.image=image;fit.padding=0;
            if(fallbackLabel!=null){fallbackLabel.transform.SetAsLastSibling();var label=fallbackLabel.rectTransform;label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(8,8);label.offsetMax=new Vector2(-8,-8);fallbackLabel.raycastTarget=false;}
        }
        public void Bind(CounterTradeUI ui,PendingOffer offer,bool arriving)
        {
            EnsureVisuals();owner=ui;uid=offer.instanceId;containerId=offer.containerId;picked=offer.customerPicked;
            var item=ui.panel.Model.GetItem(containerId,uid);var quality=item==null?null:ui.panel.Catalog.FindQuality(item.itemId,item.quality);
            var sprite=quality==null?null:ui.panel.FindItemIcon(item.itemId);int sourceRotation=item==null?0:item.rotation;
            int columns=item==null?1:ui.panel.Model.Width(item),rows=item==null?1:ui.panel.Model.Height(item);
            int rotation=CounterTableMotion.PresentationRotation(rows,picked,sourceRotation);
            if(rotation!=sourceRotation){int width=columns;columns=rows;rows=width;}
            float aspect=SpriteIconFit.VisibleAspect(sprite);if((rotation&1)!=0&&aspect>0)aspect=1/aspect;
            var size=CounterTableMotion.VisualSize(columns,rows,ui.counterCellSize,aspect);
            Body.halfWidth=size.x*.5f;Body.halfHeight=size.y*.5f;
            var rect=(RectTransform)transform;rect.sizeDelta=new Vector2(size.x,size.y);
            orientation.sizeDelta=(rotation&1)==0?rect.sizeDelta:new Vector2(size.y,size.x);orientation.localRotation=Quaternion.Euler(0,0,-rotation*90);
            fit.SetSprite(sprite);image.raycastTarget=sprite!=null;ui.panel.ApplyQuality(image,item==null?"none":item.quality);
            fallback.enabled=sprite==null;fallback.raycastTarget=sprite==null;fallback.color=new Color(.35f,.23f,.12f,1);
            if(fallbackLabel!=null){fallbackLabel.gameObject.SetActive(sprite==null);fallbackLabel.text=quality==null?"물품":quality.name;}
            Body.active=true;if(arriving){Body.dragged=false;Body.asleep=false;Body.vx=Body.vy=0;}
        }
        public void ApplyPosition()
        {var rect=(RectTransform)transform;var position=new Vector2(Body.x,Body.y);if(rect.anchoredPosition!=position)rect.anchoredPosition=position;}
        public bool IsRaycastLocationValid(Vector2 point,Camera camera)
        {return owner!=null&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform,point,camera);}
        public void OnDrop(PointerEventData e){if(owner!=null)owner.ReceiveStockDrop(e);}
        public void OnPointerDown(PointerEventData e){suppressClick=false;if(e.button==PointerEventData.InputButton.Left&&owner!=null&&owner.CanInteractWithItems)transform.SetAsLastSibling();}
        public void OnBeginDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||owner==null||!owner.TryBeginCounterDrag(this))return;
            Vector2 pointer;if(!owner.TryTablePoint(e.position,e.pressEventCamera,out pointer)){owner.EndCounterDrag(this);return;}
            suppressClick=true;grabOffset=new Vector2(Body.x,Body.y)-pointer;lastPointer=pointer;lastTime=Time.unscaledTime;Body.dragged=true;Body.asleep=false;Body.vx=Body.vy=0;transform.SetAsLastSibling();owner.WakeBodies();
        }
        public void OnDrag(PointerEventData e)
        {
            if(!Body.dragged||owner==null)return;Vector2 pointer;if(!owner.TryTablePoint(e.position,e.pressEventCamera,out pointer))return;
            float dt=Time.unscaledTime-lastTime;var position=pointer+grabOffset;
            if(dt>.001f){Vector2 velocity=Vector2.ClampMagnitude((pointer-lastPointer)/dt,600);Body.vx=velocity.x*.35f;Body.vy=velocity.y*.35f;}
            Body.x=position.x;Body.y=position.y;owner.Clamp(Body);ApplyPosition();lastPointer=pointer;lastTime=Time.unscaledTime;
        }
        public void OnEndDrag(PointerEventData e){if(owner!=null&&Body.dragged){if(Time.unscaledTime-lastTime>.12f)Body.vx=Body.vy=0;owner.EndCounterDrag(this);}}
        void OnApplicationFocus(bool focus){if(!focus&&owner!=null&&Body.dragged)owner.EndCounterDrag(this);}
        void OnDisable(){if(owner!=null&&Body.dragged)owner.EndCounterDrag(this);}
        public void OnPointerClick(PointerEventData e)
        {
            if(suppressClick){suppressClick=false;return;}if(owner==null||!owner.CanInteractWithItems)return;
            if(e.button==PointerEventData.InputButton.Right&&!picked)owner.shop.RemoveOffer(uid);
            else if(e.button==PointerEventData.InputButton.Left){transform.SetAsLastSibling();owner.ShowItem(containerId,uid);}
        }
        public void Release(){if(owner!=null&&Body.dragged)owner.EndCounterDrag(this);Body.active=false;Body.asleep=true;Body.dragged=false;suppressClick=false;owner=null;uid=containerId=null;if(image!=null)image.sprite=null;gameObject.SetActive(false);}
    }
}
