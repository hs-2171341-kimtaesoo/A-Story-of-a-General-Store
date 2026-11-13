using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 격자당 메시 하나로 선택 모양의 바깥 경계를 표시합니다. 빈 구멍과 ㄱ/ㄷ 모양도 유지합니다.
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
            foreach(var c in shape.InternalCells(item.rotation))
            {
                float x=rect.xMin+(item.x+c.x)*cell.x,y=rect.yMax-(item.y+c.y)*cell.y;
                if(!shape.Contains(item.rotation,c.x,c.y-1))Quad(h,x,y,cell.x,t);
                if(!shape.Contains(item.rotation,c.x,c.y+1))Quad(h,x,y-cell.y+t,cell.x,t);
                if(!shape.Contains(item.rotation,c.x-1,c.y))Quad(h,x,y,t,cell.y);
                if(!shape.Contains(item.rotation,c.x+1,c.y))Quad(h,x+cell.x-t,y,t,cell.y);
            }
        }
        void Quad(VertexHelper h,float x,float y,float w,float height)
        {int n=h.currentVertCount;Add(h,x,y);Add(h,x+w,y);Add(h,x+w,y-height);Add(h,x,y-height);h.AddTriangle(n,n+1,n+2);h.AddTriangle(n+2,n+3,n);}
        void Add(VertexHelper h,float x,float y){var v=UIVertex.simpleVert;v.position=new Vector3(x,y,0);v.color=color;v.uv0=Vector2.zero;h.AddVert(v);}
    }
}
