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
        public bool verticalControls=true;
        [Range(.6f,.95f)]public float controlsCenterFraction=.8f;
        [Range(.7f,.9f)]public float controlsTopFraction=.82f;
        Rect visibleRegion=new Rect(0,-1080,1920,1080);
        public void RefreshViewport(Rect region)
        {
            visibleRegion=region;if(!verticalControls||controls==null||priceEditor==null)return;
            if(infoLabel!=null)infoLabel.gameObject.SetActive(false);
            float scale=Mathf.Min(1,Mathf.Min(region.width*.28f/540,region.height*(1-controlsTopFraction)/172));
            var position=new Vector2(region.xMin+region.width*controlsCenterFraction-270*scale,region.yMax-region.height*controlsTopFraction);
            foreach(var go in new[]{controls,priceEditor}){var rect=(RectTransform)go.transform;rect.localScale=new Vector3(scale,scale,1);rect.anchoredPosition=position;}
            if(ready)ConfigureMotion();
        }
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
        public event System.Action<bool> DraggingChanged;
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
            if(verticalControls||!keepControlsAboveGoods)return;float height=0;foreach(var body in bodies)if(body.active)height=Mathf.Max(height,body.halfHeight*2);
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
        {if(Busy||priceEditor.activeSelf)return false;dragging=view;DraggingChanged?.Invoke(true);return true;}
        public void EndCounterDrag(CounterItemView view)
        {if(dragging!=view)return;dragging=null;view.Body.dragged=false;view.Body.asleep=false;view.RestoreCounterLayer();panel.drag.ClearCounterPreview();Clamp(view.Body);ApplyPositions();WakeBodies();DraggingChanged?.Invoke(false);}
        Vector2 counterPointer;
        public void CounterPointer(Vector2 point){counterPointer=point;}
        void LateUpdate()
        {
            if(!ready)return;if(dragging!=null)panel.drag.UpdateCounterPreview(dragging,counterPointer);
            LastPhysicsSteps=CounterTableMotion.Advance(bodies,motion,Mathf.Min(.1f,Time.unscaledDeltaTime));
            if(LastPhysicsSteps>0)ApplyPositions();
        }
        readonly Dictionary<string,GameObject> returnEffects = new Dictionary<string,GameObject>();
        WarehouseUI returnWarehouse;
        void OnDisable()
        {
            if (dragging != null) EndCounterDrag(dragging);
            StopAllCoroutines();
            priceTween = null;
            previousCounter = -1;
            foreach (var effect in returnEffects)
                {
                    if (returnWarehouse != null) returnWarehouse.grid.SetReturnInFlight(effect.Key, false);
                    if (effect.Value != null) Destroy(effect.Value);
                }
            returnEffects.Clear();
        }

        RecommendationSession Session{get{return panel.inventory.Sales;}}
        bool Busy{get{return panel.IsWindowDragging||panel.drag.IsDragging||shop.HasPendingPlacement||IsItemDragging;}}
        void Start()
        {
            string error;if(panel==null||shop==null||tableItems==null||controls==null||priceEditor==null||itemTemplate==null||totalLabel==null||infoLabel==null||proposedTotalInput==null||sellButton==null||talkButton==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("카운터 거래 참조를 확인하세요. 새 데모 씬을 생성하세요.",this);enabled=false;return;}
            authoredControlsPosition=((RectTransform)controls.transform).anchoredPosition;authoredInfoPosition=infoLabel.rectTransform.anchoredPosition;
            RefreshViewport(visibleRegion);HideLegacyNavigation();ConfigureMotion();itemTemplate.gameObject.SetActive(false);priceEditor.SetActive(false);ready=true;Session.Returned+=FlyReturn;panel.drag.DraggingChanged+=OnHeld;Render();
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
            ShowSpeech(engaged?Session.Speech:null);
            seen.Clear();quotedTotal=0;
            if(!present){previousCounter=-1;ReleaseUnused();bodies.Clear();dropPoints.Clear();priceEditor.SetActive(false);infoLabel.text="";return;}
            int count=Session.CounterLineCount;
            int picks=Session.PickedCount;
            long pickedTotal=0;
            for(int i=0;i<count;i++)
            {
                var line=Session.CounterLineAt(i);
                seen.Add(line.instanceId);
                quotedTotal+=line.price;
                if(i<picks)pickedTotal+=line.price;
            }
            ReleaseUnused();if(selectedInfoUid!=null&&!seen.Contains(selectedInfoUid)){selectedInfoUid=null;infoLabel.text="";}
            ConfigureMotion();bodies.Clear();
            for(int n=0;n<count;n++)
            {
                var offer=Session.CounterLineAt(n);CounterItemView v;bool arriving=!views.TryGetValue(offer.instanceId,out v);
                if(arriving){v=pool.Count>0?pool.Pop():Instantiate(itemTemplate,tableItems);views.Add(offer.instanceId,v);v.transform.SetParent(tableItems,false);v.gameObject.SetActive(true);}
                v.Bind(this,offer,arriving);bodies.Add(v.Body);
                if(arriving)
                {
                    Vector2 point;
                    if(dropPoints.TryGetValue(offer.instanceId,out point)){v.Body.x=point.x;v.Body.y=point.y;dropPoints.Remove(offer.instanceId);}
                    else{v.Body.x=(motion.left+motion.right)*.5f+((n%5)==0?0:(n%5)%2==1?-1:1)*Mathf.Ceil((n%5)/2f)*230;v.Body.y=motion.surface+v.Body.halfHeight+140;}
                }
            }
            foreach(var body in bodies)if(!body.dragged)Clamp(body);ApplyPositions();WakeBodies();PlaceControlsAboveGoods();
            if(Session.ProposedPrice>=0)quotedTotal=Session.ProposedPrice;
            long beforeCounter=quotedTotal;
            if(Session.CounterPrice>=0)quotedTotal=Session.CounterPrice;
            if(Session.Selling)quotedTotal=Session.SellerPrice;
            if(sellLabel==null)sellLabel=sellButton.GetComponentInChildren<Text>(true);
            if(sellLabel!=null)sellLabel.text=Session.Selling?"매입":Session.EventVisit?"확인":"판매";
            totalLabel.text=(verticalControls||Session.StoryVisit)?quotedTotal.ToString("N0")+" G":"진열 등록 합계 "+pickedTotal.ToString("N0")+" G"+(count>picks?" · 추천 포함 "+quotedTotal.ToString("N0")+" G":"");
            if(Session.CounterPrice>=0&&previousCounter!=Session.CounterPrice){
                if(priceTween!=null)StopCoroutine(priceTween);
                priceTween=StartCoroutine(CountDown(beforeCounter,Session.CounterPrice));
            }else if(Session.CounterPrice<0&&priceTween!=null){StopCoroutine(priceTween);priceTween=null;}
            previousCounter=Session.CounterPrice;
            sellButton.interactable=count>0||Session.Selling||Session.EventVisit;talkButton.interactable=true;
        }
        long previousCounter=-1;
        Coroutine priceTween;
        System.Collections.IEnumerator CountDown(long from,long to){
            for(float t=0;t<1;t+=Time.unscaledDeltaTime*3){
                if(!Session.CustomerPresent||Session.CounterPrice!=to)yield break;
                totalLabel.text=((long)(from+(to-from)*(double)t)).ToString("N0")+" G";yield return null;
            }
            totalLabel.text=to.ToString("N0")+" G";priceTween=null;
        }
        Text speechText, sellLabel;
        void ShowSpeech(string line){
            if(speechText==null&&string.IsNullOrEmpty(line))return;
            if(speechText==null){
                var box=new GameObject("Customer Dialogue",typeof(RectTransform),typeof(global::UnityEngine.UI.Image));
                var rect=(RectTransform)box.transform;rect.SetParent(controls.transform,false);
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,0);
                rect.sizeDelta=new Vector2(530,100);rect.anchoredPosition=new Vector2(0,12);
                var bg=box.GetComponent<global::UnityEngine.UI.Image>();bg.color=new Color(.04f,.035f,.03f,.8f);bg.raycastTarget=false;
                speechText=Instantiate(totalLabel,rect);var t=speechText.rectTransform;
                t.anchorMin=Vector2.zero;t.anchorMax=Vector2.one;t.offsetMin=new Vector2(12,6);t.offsetMax=new Vector2(-12,-6);
                speechText.resizeTextForBestFit=true;speechText.raycastTarget=false;
            }
            speechText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(line));speechText.text=line;
        }
        void FlyReturn(string uid,string destination)
        {
            CounterItemView view;
            if(!isActiveAndEnabled||!views.TryGetValue(uid,out view)||view.image==null)return;
            var item = panel.Model.GetItem(destination, uid);
            if (item == null || returnEffects.ContainsKey(uid)) return;
            if (returnWarehouse == null) returnWarehouse = panel.GetComponent<WarehouseUI>();
            var canvas = (RectTransform)panel.canvas.rootCanvas.transform;
            var source = view.image.sprite != null ? view.image.rectTransform : (RectTransform)view.transform;
            var ghost = Instantiate(view.image, canvas, false);
            ghost.enabled = true;
            ghost.raycastTarget = false;
            if (ghost.sprite == null)
            {
                ghost.color = new Color(.35f,.23f,.12f,1);
                if (view.fallbackLabel != null) Instantiate(view.fallbackLabel, ghost.transform, false).raycastTarget = false;
            }
            var rect = ghost.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.localScale = Vector3.one;
            rect.position = source.TransformPoint(source.rect.center);
            rect.rotation = source.rotation;
            rect.sizeDelta = CanvasSize(source, canvas, source.rect.size);
            returnEffects.Add(uid, ghost.gameObject);
            if (returnWarehouse != null && InventoryModel.IsWarehouse(destination))
                returnWarehouse.grid.SetReturnInFlight(uid, true);
            StartCoroutine(Fly(uid, destination, item, ghost, canvas));
        }

        static Vector2 CanvasSize(RectTransform source, RectTransform canvas, Vector2 size)
        {
            return new Vector2(canvas.InverseTransformVector(source.TransformVector(Vector3.right * size.x)).magnitude,
                canvas.InverseTransformVector(source.TransformVector(Vector3.up * size.y)).magnitude);
        }

        System.Collections.IEnumerator Fly(string uid, string destination, ItemRecord item, global::UnityEngine.UI.Image ghost, RectTransform canvas)
        {
            var rect = ghost.rectTransform;
            var start = rect.localPosition;
            var initialSize = rect.sizeDelta;
            var initialRotation = rect.localRotation;
            bool warehouse = InventoryModel.IsWarehouse(destination);
            var drawer = shop.drawer;
            var menu = warehouse ? null : panel.transform.root.GetComponentInChildren<RightSideMenu>(true);
            var shape = panel.Catalog.Shape(item.itemId);
            for (float elapsed = 0; ; elapsed += Time.unscaledDeltaTime)
            {
                var target = drawer.window;
                var end = canvas.InverseTransformPoint(target.TransformPoint(target.rect.center));
                var size = initialSize;
                var rotation = initialRotation;
                if (warehouse && !drawer.IsOpen)
                {
                    // Pass the bottom edge with enough clearance for the entire rotated icon.
                    end.y = canvas.rect.yMin - initialSize.magnitude;
                }
                else if (warehouse && returnWarehouse != null && returnWarehouse.SelectedId == destination)
                {
                    var grid = returnWarehouse.grid;
                    float w = grid.cellWidth, h = grid.cellHeight;
                    int r = item.rotation;
                    var center = new Vector3((item.x-grid.startColumn+shape.Width(r)*.5f)*w,
                        -(item.y-grid.startRow+shape.Height(r)*.5f)*h, 0);
                    end = canvas.InverseTransformPoint(grid.itemLayer.TransformPoint(center));
                    var sprite = ghost.sprite;
                    var quad = IconMeshGeometry.BuildQuad(shape, r, sprite != null,
                        sprite == null ? 1 : sprite.rect.width/sprite.rect.height, w/h);
                    size = CanvasSize(grid.itemLayer, canvas, new Vector2((quad.x1-quad.x0)*w, (quad.y1-quad.y0)*h));
                    if ((r & 1) != 0) size = new Vector2(size.y, size.x);
                    rotation = Quaternion.Inverse(canvas.rotation)*grid.itemLayer.rotation*Quaternion.Euler(0,0,-90*r);
                }
                else
                {
                    if (warehouse && returnWarehouse != null) target = returnWarehouse.tabs.FindTab(destination) ?? target;
                    else if (menu != null) target = (RectTransform)menu.transform;
                    end = canvas.InverseTransformPoint(target.TransformPoint(target.rect.center));
                    size *= .14f;
                }
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed/.38f));
                rect.localPosition = Vector3.Lerp(start, end, t);
                rect.sizeDelta = Vector2.Lerp(initialSize, size, t);
                rect.localRotation = Quaternion.Slerp(initialRotation, rotation, t);
                if (t >= 1) break;
                yield return null;
            }
            if (warehouse && returnWarehouse != null) returnWarehouse.grid.SetReturnInFlight(uid, false);
            returnEffects.Remove(uid);
            Destroy(ghost.gameObject);
        }
        public void Sell(){if(Busy)return;
            if(Session.EventVisit){Session.Leave();return;}
            string message;
            if(Session.Selling){Session.AcceptSeller(out message);panel.ShowMessage(message);return;}
            long revenue;if(Session.TrySubmitAtTotal(quotedTotal,out revenue,out message))message="판매 완료 +"+revenue+" G";
            panel.ShowMessage(message);}
        public void Talk(){if(Busy)return;shop.BeginTalk();}
        public void Reject(){if(!Busy)shop.LeaveCustomer();}
        public void TogglePrice()
        {if(Busy||!Session.CustomerPresent||Session.Selling||Session.EventVisit)return;bool open=!priceEditor.activeSelf;priceEditor.SetActive(open);OnHeld(panel.drag.IsDragging);if(open){proposedTotalInput.text=quotedTotal.ToString();proposedTotalInput.ActivateInputField();}}
        public void CancelPrice(){priceEditor.SetActive(false);OnHeld(panel.drag.IsDragging);}
        public void ConfirmPrice()
        {
            if(Busy)return;long total;
            if(!long.TryParse(proposedTotalInput.text,out total)||total<0){panel.ShowMessage("0 이상의 정수 금액을 입력하세요.");return;}
            if(Session.StoryVisit){CancelPrice();Session.SetOffer(total);return;}
            long revenue;string error;
            if(Session.TrySubmitAtTotal(total,out revenue,out error)){priceEditor.SetActive(false);panel.ShowMessage("거래 완료 +"+revenue+" G");}else {priceEditor.SetActive(false);panel.ShowMessage(error);}
        }
        void OnHeld(bool held){if(dropSurface!=null)dropSurface.raycastTarget=held&&Session.CounterPresented&&!priceEditor.activeSelf;}
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory!=null&&Session!=null)Session.Returned-=FlyReturn;if(ready&&panel!=null&&panel.drag!=null)panel.drag.DraggingChanged-=OnHeld;}
        public void ShowItem(string container,string uid)
        {
            var item=panel.Model.GetItem(container,uid);if(item==null)return;selectedInfoUid=uid;
            var quality=panel.Catalog.FindQuality(item.itemId,item.quality);int price;panel.Model.TryGetOfferPrice(container,uid,out price);
            infoLabel.text=quality.name+" · "+QualityLabels.Title(item.quality)+" · "+(InventoryModel.IsDisplay(container)?"등록가 ":"기준가 ")+price+" G";
            if(verticalControls)panel.ShowMessage(infoLabel.text);
        }
    }
}
