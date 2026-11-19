using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 벽마다 GameObject를 만들지 않고 가구 창 하나당 메시 하나만 사용합니다.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DisplayPartitionGraphic:MaskableGraphic
    {
        DisplayTypeDefinition definition;Vector2 cell;float thickness;
        protected override void OnEnable(){if(GetComponent<CanvasRenderer>()==null)gameObject.AddComponent<CanvasRenderer>();base.OnEnable();}
        public void Configure(DisplayTypeDefinition value,Vector2 pitch,float width,Color tint)
        {
            if(definition==value&&cell==pitch&&thickness==width&&color==tint)return;
            definition=value;cell=pitch;thickness=width;color=tint;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();if(definition==null)return;var rect=GetPixelAdjustedRect();float half=thickness*.5f;
            if(definition.horizontalDividers!=null)foreach(int boundary in definition.horizontalDividers)
            {float y=rect.yMax-boundary*cell.y;Quad(helper,rect.xMin,y+half,rect.xMax,y-half,color);}
            if(definition.verticalDividers!=null)foreach(int boundary in definition.verticalDividers)
            {float x=rect.xMin+boundary*cell.x;Quad(helper,x-half,rect.yMax,x+half,rect.yMin,color);}
        }
        static void Quad(VertexHelper h,float left,float top,float right,float bottom,Color tint)
        {
            int start=h.currentVertCount;Add(h,left,top,tint);Add(h,right,top,tint);Add(h,right,bottom,tint);Add(h,left,bottom,tint);
            h.AddTriangle(start,start+1,start+2);h.AddTriangle(start+2,start+3,start);
        }
        static void Add(VertexHelper h,float x,float y,Color tint)
        {var v=UIVertex.simpleVert;v.position=new Vector3(x,y,0);v.color=tint;v.uv0=Vector2.zero;h.AddVert(v);}
    }
}
