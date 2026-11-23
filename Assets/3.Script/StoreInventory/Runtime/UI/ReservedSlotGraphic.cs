using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // One mesh per grid, never one object per occupied cell.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ReservedSlotGraphic:MaskableGraphic,ICanvasRaycastFilter,IPointerEnterHandler,IPointerExitHandler,IPointerMoveHandler,IPointerClickHandler
    {
        readonly List<Rect> reservations=new List<Rect>();readonly List<string> ids=new List<string>();Vector2 cell;InventoryGridView owner;string hovered;
        public void Show(IList<ItemRecord> items,InventoryGridView grid)
        {
            reservations.Clear();ids.Clear();cell=grid.CellSize;owner=grid;
            foreach(var item in items){reservations.Add(new Rect(item.x-grid.startColumn,item.y-grid.startRow,grid.Panel.Model.Width(item),grid.Panel.Model.Height(item)));ids.Add(item.instanceId);}
            raycastTarget=items.Count>0;if(hovered!=null&&!ids.Contains(hovered))ClearHover();SetVerticesDirty();
        }
        string Hit(Vector2 screen)
        {
            int x,y;if(owner==null||owner.Panel==null||!owner.PointerCell(screen,out x,out y))return null;
            for(int n=0;n<reservations.Count;n++)if(reservations[n].Contains(new Vector2(x-owner.startColumn,y-owner.startRow)))return ids[n];return null;
        }
        public bool IsRaycastLocationValid(Vector2 screen,Camera camera){return Hit(screen)!=null;}
        void Hover(PointerEventData e){string uid=Hit(e.position);if(uid==hovered)return;ClearHover();hovered=uid;if(uid!=null&&owner.Panel.tooltip!=null)owner.Panel.tooltip.EnterReservation(owner,uid);}
        void ClearHover(){if(hovered!=null&&owner!=null&&owner.Panel?.tooltip!=null)owner.Panel.tooltip.ExitReservation(owner,hovered);hovered=null;}
        public void OnPointerEnter(PointerEventData e){Hover(e);}public void OnPointerMove(PointerEventData e){Hover(e);}public void OnPointerExit(PointerEventData e){ClearHover();}
        public void OnPointerClick(PointerEventData e){string uid=Hit(e.position);if(uid!=null&&e.button==PointerEventData.InputButton.Left)owner.Panel.ToggleSelection(owner.containerId,uid);}
        protected override void OnDisable(){ClearHover();base.OnDisable();}
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();var r=GetPixelAdjustedRect();var gold=new Color(1,.72f,.18f,.85f);var shade=new Color(.92f,.49f,.08f,.22f);
            foreach(var item in reservations)
            {
                float x=r.xMin+item.x*cell.x+1,top=r.yMax-item.y*cell.y-1,w=item.width*cell.x-2,height=item.height*cell.y-2;
                Quad(h,x,top,w,height,shade);Quad(h,x,top,w,2,gold);Quad(h,x,top-height+2,w,2,gold);Quad(h,x,top,2,height,gold);Quad(h,x+w-2,top,2,height,gold);
                for(float y=8;y<height-6;y+=12)Quad(h,x+5,top-y,w-10,1,new Color(1,.75f,.3f,.28f));
                float cx=x+w*.5f,cy=top-height*.5f;Quad(h,cx-8,cy+3,16,13,gold);Quad(h,cx-6,cy+11,3,8,gold);Quad(h,cx+3,cy+11,3,8,gold);Quad(h,cx-6,cy+13,12,3,gold);Quad(h,cx-1,cy,2,5,new Color(.24f,.12f,.03f,1));
            }
        }
        static void Quad(VertexHelper h,float x,float y,float w,float height,Color tint)
        {
            if(w<=0||height<=0)return;int k=h.currentVertCount;Add(h,x,y,tint);Add(h,x+w,y,tint);Add(h,x+w,y-height,tint);Add(h,x,y-height,tint);h.AddTriangle(k,k+1,k+2);h.AddTriangle(k+2,k+3,k);
        }
        static void Add(VertexHelper h,float x,float y,Color color){var v=UIVertex.simpleVert;v.position=new Vector3(x,y,0);v.color=color;v.uv0=Vector2.zero;h.AddVert(v);}
    }
}
