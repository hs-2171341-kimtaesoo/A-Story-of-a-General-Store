using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class CounterTradeUI:MonoBehaviour
    {
        public InventoryPanel panel;public ShopfrontUI shop;
        public RectTransform tableItems;public GameObject controls,priceEditor;
        public global::UnityEngine.UI.Image dropSurface;
        public CounterItemView itemTemplate;public Text totalLabel,infoLabel;public InputField proposedTotalInput;
        public Button sellButton,talkButton;
        [Min(1)]public float counterCellSize=100;
        public float surfaceFromTop=1028.5f,leftInset=100,rightInset=100,ceilingFromTop=40;
        [Range(0,100)]public float surfaceLowerPercent=0;
        [Min(0)]public float surfaceBottomMargin=8;
        public bool keepControlsAboveGoods=true;
        [Min(0)]public float controlsGap=16;
        [Range(100,20000)]public float gravity=2200;
        [Range(0,.5f)]public float bounce=.1f;
        [Min(0)]public float horizontalDamping=6;
        [Min(1)]public float sleepSpeed=25;
        public global::UnityEngine.UI.Image surfaceGuide;public bool showSurfaceGuide;
        [Min(0)]public int maximumCachedViews=16;
        readonly Dictionary<string,CounterItemView> views=new Dictionary<string,CounterItemView>();
        readonly Stack<CounterItemView> pool=new Stack<CounterItemView>();
        readonly HashSet<string> seen=new HashSet<string>();readonly List<string> removed=new List<string>();
        public int LiveViewCount{get{return views.Count;}}public int CachedViewCount{get{return pool.Count;}}
        readonly List<CounterBodyState> bodies=new List<CounterBodyState>();
        readonly Dictionary<string,Vector2> dropPoints=new Dictionary<string,Vector2>();
        readonly CounterTableSettings motion=new CounterTableSettings();
        CounterItemView dragging;bool ready;long quotedTotal;string selectedInfoUid;
        Vector2 authoredControlsPosition,authoredInfoPosition;
        public bool IsItemDragging{get{return dragging!=null;}}
        public bool CanInteractWithItems{get{return ready&&!Busy&&!priceEditor.activeSelf;}}
        public int LastPhysicsSteps{get;private set;}
        public bool TryTablePoint(Vector2 screen,Camera camera,out Vector2 point)
        {return RectTransformUtility.ScreenPointToLocalPointInRectangle(tableItems,screen,camera,out point);}
        public Vector2 CellSizeIn(RectTransform layer)
        {Vector3 x=layer.InverseTransformVector(tableItems.TransformVector(new Vector3(counterCellSize,0,0))),y=layer.InverseTransformVector(tableItems.TransformVector(new Vector3(0,counterCellSize,0)));return new Vector2(x.magnitude,y.magnitude);}
        public void ReceiveStockDrop(UnityEngine.EventSystems.PointerEventData e)
        {if(dropSurface!=null&&!priceEditor.activeSelf){var area=dropSurface.GetComponent<RecommendationDropArea>();if(area!=null)area.OnDrop(e);}}
        public void StageDropLocation(string uid,Vector2 screen)
        {
            Vector2 point;var canvas=tableItems.GetComponentInParent<Canvas>();var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            if(!TryTablePoint(screen,camera,out point))return;
            CounterItemView view;if(views.TryGetValue(uid,out view)){view.Body.x=point.x;view.Body.y=point.y;Clamp(view.Body);ApplyPositions();WakeBodies();}
            else dropPoints[uid]=point;
        }
        void ConfigureMotion()
        {
            motion.left=tableItems.rect.xMin+leftInset;motion.right=tableItems.rect.xMax-rightInset;
            float depth=CounterTableMotion.SurfaceDepth(surfaceFromTop,tableItems.rect.height,surfaceLowerPercent,surfaceBottomMargin);
            motion.surface=tableItems.rect.yMax-depth;motion.ceiling=tableItems.rect.yMax-ceilingFromTop;
            motion.gravity=gravity;motion.bounce=bounce;motion.drag=horizontalDamping;motion.sleepSpeed=sleepSpeed;motion.Validate();
            if(surfaceGuide!=null){surfaceGuide.gameObject.SetActive(showSurfaceGuide);surfaceGuide.rectTransform.anchoredPosition=new Vector2(leftInset,-depth);surfaceGuide.rectTransform.sizeDelta=new Vector2(motion.right-motion.left,2);}
        }
        void PlaceControlsAboveGoods()
        {
            if(!keepControlsAboveGoods)return;float height=0;foreach(var body in bodies)if(body.active)height=Mathf.Max(height,body.halfHeight*2);
            var rect=controls.transform as RectTransform;if(rect==null)return;
            float depth=tableItems.rect.yMax-motion.surface,top=Mathf.Max(0,Mathf.Min(-authoredControlsPosition.y,depth-height-rect.rect.height-controlsGap));
            var position=new Vector2(authoredControlsPosition.x,-top);if(rect.anchoredPosition!=position)rect.anchoredPosition=position;
            position=new Vector2(authoredInfoPosition.x,-Mathf.Max(0,Mathf.Min(-authoredInfoPosition.y,top-infoLabel.rectTransform.rect.height-4)));
            if(infoLabel.rectTransform.anchoredPosition!=position)infoLabel.rectTransform.anchoredPosition=position;
        }
        void ApplyPositions(){foreach(var view in views.Values)view.ApplyPosition();}
        void HideLegacyNavigation()
        {
            foreach(string name in new[]{"Previous Counter Page","Next Counter Page"})
            {var button=tableItems.parent.Find(name);if(button!=null)button.gameObject.SetActive(false);}
        }
        public void Clamp(CounterBodyState body){CounterTableMotion.ClampBody(body,motion);}
        public void WakeBodies(){foreach(var body in bodies)if(body.active)body.asleep=false;}
        public bool TryBeginCounterDrag(CounterItemView view)
        {if(Busy||priceEditor.activeSelf)return false;dragging=view;return true;}
        public void EndCounterDrag(CounterItemView view)
        {if(dragging!=view)return;view.Body.dragged=false;view.Body.asleep=false;dragging=null;Clamp(view.Body);ApplyPositions();WakeBodies();}
        void LateUpdate()
        {
            if(!ready)return;LastPhysicsSteps=CounterTableMotion.Advance(bodies,motion,Mathf.Min(.1f,Time.unscaledDeltaTime));
            if(LastPhysicsSteps>0)ApplyPositions();
        }
        void OnDisable(){if(dragging!=null)EndCounterDrag(dragging);}

        RecommendationSession Session{get{return panel.inventory.Sales;}}
        bool Busy{get{return panel.IsWindowDragging||panel.drag.IsDragging||shop.HasPendingPlacement||IsItemDragging;}}
        void Start()
        {
            string error;if(panel==null||shop==null||tableItems==null||controls==null||priceEditor==null||itemTemplate==null||totalLabel==null||infoLabel==null||proposedTotalInput==null||sellButton==null||talkButton==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("카운터 거래 참조를 확인하세요. 새 데모 씬을 생성하세요.",this);enabled=false;return;}
            authoredControlsPosition=((RectTransform)controls.transform).anchoredPosition;authoredInfoPosition=infoLabel.rectTransform.anchoredPosition;
            HideLegacyNavigation();ConfigureMotion();itemTemplate.gameObject.SetActive(false);priceEditor.SetActive(false);ready=true;panel.drag.DraggingChanged+=OnHeld;Render();
        }
        void ReleaseUnused()
        {
            removed.Clear();foreach(var v in views)if(!seen.Contains(v.Key))removed.Add(v.Key);
            foreach(string id in removed){var v=views[id];v.Release();views.Remove(id);if(pool.Count<maximumCachedViews)pool.Push(v);else Destroy(v.gameObject);}
        }
        public void Render()
        {
            if(!ready)return;
            bool present=Session.CustomerPresent&&Session.CounterPresented;
            bool engaged=present&&shop.customerInteraction!=null&&shop.customerInteraction.Engaged;
            if(controls.activeSelf!=engaged)controls.SetActive(engaged);
            if(tableItems.gameObject.activeSelf!=present)tableItems.gameObject.SetActive(present);
            if(dropSurface!=null)dropSurface.raycastTarget=present&&panel.drag.IsDragging&&!priceEditor.activeSelf;
            seen.Clear();quotedTotal=0;
            if(!present){ReleaseUnused();bodies.Clear();dropPoints.Clear();priceEditor.SetActive(false);infoLabel.text="";return;}
            var cart=Session.Purchases();long pickedTotal=0;foreach(var line in cart)pickedTotal+=line.price;var extra=Session.Offers();cart.AddRange(extra);int count=cart.Count;
            foreach(var line in cart){seen.Add(line.instanceId);quotedTotal+=line.price;}ReleaseUnused();if(selectedInfoUid!=null&&!seen.Contains(selectedInfoUid)){selectedInfoUid=null;infoLabel.text="";}
            ConfigureMotion();bodies.Clear();
            for(int n=0;n<count;n++)
            {
                var offer=cart[n];CounterItemView v;bool arriving=!views.TryGetValue(offer.instanceId,out v);
                if(arriving){v=pool.Count>0?pool.Pop():Instantiate(itemTemplate,tableItems);views.Add(offer.instanceId,v);v.transform.SetParent(tableItems,false);v.gameObject.SetActive(true);}
                v.Bind(this,offer,arriving);bodies.Add(v.Body);
                if(arriving)
                {
                    Vector2 point;
                    if(dropPoints.TryGetValue(offer.instanceId,out point)){v.Body.x=point.x;v.Body.y=point.y;dropPoints.Remove(offer.instanceId);}
                    else{v.Body.x=(motion.left+motion.right)*.5f+((n%5)==0?0:(n%5)%2==1?-1:1)*Mathf.Ceil((n%5)/2f)*230;v.Body.y=motion.surface+v.Body.halfHeight+140;}
                }
            }
            foreach(var body in bodies)Clamp(body);ApplyPositions();WakeBodies();PlaceControlsAboveGoods();
            totalLabel.text="진열 등록 합계 "+pickedTotal.ToString("N0")+" G"+(extra.Count>0?" · 추천 포함 "+quotedTotal.ToString("N0")+" G":"");
            sellButton.interactable=count>0;talkButton.interactable=!Session.ConversationStarted;
        }
        public void Sell(){if(Busy)return;shop.SubmitOffer();}
        public void Talk(){if(Busy)return;shop.BeginTalk();panel.ShowMessage("추가로 권할 물건을 카운터 위에 드래그하세요. 추천 물건은 우클릭으로 제외할 수 있습니다.");}
        public void Reject(){if(!Busy)shop.LeaveCustomer();}
        public void TogglePrice()
        {if(Busy||!Session.CustomerPresent)return;bool open=!priceEditor.activeSelf;priceEditor.SetActive(open);OnHeld(panel.drag.IsDragging);if(open){proposedTotalInput.text=quotedTotal.ToString();proposedTotalInput.ActivateInputField();}}
        public void CancelPrice(){priceEditor.SetActive(false);OnHeld(panel.drag.IsDragging);}
        public void ConfirmPrice()
        {
            if(Busy)return;long total;
            if(!long.TryParse(proposedTotalInput.text,out total)||total<0){panel.ShowMessage("0 이상의 정수 금액을 입력하세요.");return;}
            long revenue;string error;
            if(Session.TrySubmitAtTotal(total,out revenue,out error)){priceEditor.SetActive(false);panel.ShowMessage("거래 완료 +"+revenue+" G");}else panel.ShowMessage(error);
        }
        void OnHeld(bool held){if(dropSurface!=null)dropSurface.raycastTarget=held&&Session.CounterPresented&&!priceEditor.activeSelf;}
        void OnDestroy(){if(ready&&panel!=null&&panel.drag!=null)panel.drag.DraggingChanged-=OnHeld;}
        public void ShowItem(string container,string uid)
        {
            var item=panel.Model.GetItem(container,uid);if(item==null)return;selectedInfoUid=uid;
            var quality=panel.Catalog.FindQuality(item.itemId,item.quality);int price;panel.Model.TryGetOfferPrice(container,uid,out price);
            infoLabel.text=quality.name+" · "+QualityLabels.Title(item.quality)+" · "+(InventoryModel.IsDisplay(container)?"등록가 ":"기준가 ")+price+" G";
        }
    }
}
