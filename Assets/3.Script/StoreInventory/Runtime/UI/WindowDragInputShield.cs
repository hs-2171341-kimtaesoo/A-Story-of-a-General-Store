using System.Collections.Generic;
using UnityEngine;
namespace GeneralStore.Inventory
{
    // 창 이동 중만 사용하는 입력막입니다. 다른 진열창의 hover/클릭/드롭을 차단합니다.
    public sealed class WindowDragInputShield:MonoBehaviour
    {
        readonly DragInputLease lease=new DragInputLease();
        readonly List<CanvasGroup> groups=new List<CanvasGroup>();readonly List<bool> original=new List<bool>();
        RectTransform shield;
        public bool Active{get{return lease.Active;}}
        public bool Begin(Object actor,RectTransform moving,RectTransform bounds)
        {
            if(lease.IsOwner(actor))return true;if(!lease.Acquire(actor))return false;
            if(shield==null)
            {
                var go=new GameObject("Window Drag Input Shield",typeof(RectTransform),typeof(CanvasRenderer),typeof(global::UnityEngine.UI.Image));
                shield=(RectTransform)go.transform;var image=go.GetComponent<global::UnityEngine.UI.Image>();
                image.color=new Color(1,1,1,0);image.raycastTarget=true;image.canvasRenderer.cullTransparentMesh=false;
            }
            shield.SetParent(bounds,false);shield.anchorMin=Vector2.zero;shield.anchorMax=Vector2.one;shield.pivot=new Vector2(.5f,.5f);shield.offsetMin=shield.offsetMax=Vector2.zero;
            shield.gameObject.SetActive(true);shield.SetAsLastSibling();moving.SetAsLastSibling();
            // 중첩 Canvas가 있는 진열창도 입력막의 렌더 순서에만 의존하지 않게 차단합니다.
            foreach(var view in bounds.GetComponentsInChildren<DisplayWindowView>(true))
                if(view.gameObject.activeInHierarchy&&view.transform!=moving)Block(view.gameObject);
            foreach(var drawer in bounds.GetComponentsInChildren<WarehouseDrawer>(true))
                if(drawer.window!=moving&&drawer.window.gameObject.activeInHierarchy)Block(drawer.window.gameObject);
            return true;
        }
        void Block(GameObject target)
        {
            var group=target.GetComponent<CanvasGroup>();if(group==null)group=target.AddComponent<CanvasGroup>();
            if(groups.Contains(group))return;groups.Add(group);original.Add(group.blocksRaycasts);group.blocksRaycasts=false;
        }
        public void End(Object actor){if(lease.Release(actor))Restore();}
        void Restore()
        {
            for(int n=0;n<groups.Count;n++)if(groups[n]!=null)groups[n].blocksRaycasts=original[n];
            groups.Clear();original.Clear();if(shield!=null)shield.gameObject.SetActive(false);
        }
        void OnApplicationFocus(bool focused){if(!focused){lease.Reset();Restore();}}
        void OnDisable(){lease.Reset();Restore();}
        void OnDestroy(){lease.Reset();Restore();if(shield!=null)Destroy(shield.gameObject);}
    }
}
