using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 선택된 물품의 직사각형 경계 네 면만 표시합니다.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ItemSelectionGraphic:MaskableGraphic
    {
        ItemRecord item;ItemFootprint shape;Vector2 cell;float thickness;
        protected override void OnEnable(){if(GetComponent<CanvasRenderer>()==null)gameObject.AddComponent<CanvasRenderer>();base.OnEnable();}
        public void Show(ItemRecord value,ItemFootprint footprint,Vector2 size,float line)
        {
            if(item!=null&&value!=null&&item.instanceId==value.instanceId&&item.x==value.x&&item.y==value.y&&item.rotation==value.rotation&&cell.x==size.x&&cell.y==size.y&&thickness==line)return;
            item=value==null?null:value.Copy();shape=footprint;cell=size;thickness=line;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();if(item==null||shape==null)return;var rect=GetPixelAdjustedRect();float t=Mathf.Min(thickness,Mathf.Min(cell.x,cell.y)*.25f);
            float x=rect.xMin+item.x*cell.x,y=rect.yMax-item.y*cell.y;
            float width=shape.Width(item.rotation)*cell.x,height=shape.Height(item.rotation)*cell.y;
            Quad(h,x,y,width,t);Quad(h,x,y-height+t,width,t);Quad(h,x,y,t,height);Quad(h,x+width-t,y,t,height);
        }
        void Quad(VertexHelper h,float x,float y,float w,float height)
        {int n=h.currentVertCount;Add(h,x,y);Add(h,x+w,y);Add(h,x+w,y-height);Add(h,x,y-height);h.AddTriangle(n,n+1,n+2);h.AddTriangle(n+2,n+3,n);}
        void Add(VertexHelper h,float x,float y){var v=UIVertex.simpleVert;v.position=new Vector3(x,y,0);v.color=color;v.uv0=Vector2.zero;h.AddVert(v);}
    }
}
