using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 확장 미리보기80칸을 오브젝트80개 대신 하나의 정적 메시로 그립니다.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StoragePreviewGraphic:MaskableGraphic
    {
        int before,after;
        public Color existingColor=new Color(.25f,.18f,.1f),addedColor=new Color(1,.75f,.15f);
        protected override void OnEnable(){if(GetComponent<CanvasRenderer>()==null)gameObject.AddComponent<CanvasRenderer>();base.OnEnable();}
        public void Configure(int oldCells,int newCells){if(before==oldCells&&after==newCells)return;before=oldCells;after=newCells;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();var r=GetPixelAdjustedRect();float cw=r.width/8,ch=r.height/10;
            for(int i=0;i<after;i++)
            {
                float x=r.xMin+(i%8)*cw+1,y=r.yMax-(i/8)*ch-1;int n=h.currentVertCount;var tint=i>=before?addedColor:existingColor;
                Add(h,x,y,tint);Add(h,x+cw-2,y,tint);Add(h,x+cw-2,y-ch+2,tint);Add(h,x,y-ch+2,tint);h.AddTriangle(n,n+1,n+2);h.AddTriangle(n+2,n+3,n);
            }
        }
        static void Add(VertexHelper h,float x,float y,Color c){var v=UIVertex.simpleVert;v.position=new Vector3(x,y,0);v.color=c;v.uv0=Vector2.zero;h.AddVert(v);}
    }
}
