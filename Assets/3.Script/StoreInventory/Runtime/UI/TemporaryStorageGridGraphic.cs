using UnityEngine;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    // Repeat only the supplied grid's UV rectangle. No generated textures or per-cell objects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TemporaryStorageGridGraphic:MaskableGraphic
    {
        public Texture artwork;
        public Rect sourceUv=new Rect(60f/1498,61f/999,1376f/1498,688f/999);
        [Min(1)]public int tileRows=8;
        [Min(1)]public float cellHeight=86;
        float scrollOffset;
        public override Texture mainTexture{get{return artwork!=null?artwork:Texture2D.whiteTexture;}}
        public void SetScroll(float offset,float rowHeight)
        {
            float tile=Mathf.Max(1,tileRows*rowHeight),next=Mathf.Repeat(Mathf.Max(0,offset),tile);
            if(Mathf.Approximately(scrollOffset,next)&&Mathf.Approximately(cellHeight,rowHeight))return;
            scrollOffset=next;cellHeight=rowHeight;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(artwork==null)return;
            var r=rectTransform.rect;float height=Mathf.Max(1,tileRows*cellHeight);
            for(float top=r.yMax+scrollOffset;top>r.yMin;top-=height)
            {
                int first=mesh.currentVertCount;
                mesh.AddVert(new Vector3(r.xMin,top-height),color,new Vector2(sourceUv.xMin,sourceUv.yMin));
                mesh.AddVert(new Vector3(r.xMin,top),color,new Vector2(sourceUv.xMin,sourceUv.yMax));
                mesh.AddVert(new Vector3(r.xMax,top),color,new Vector2(sourceUv.xMax,sourceUv.yMax));
                mesh.AddVert(new Vector3(r.xMax,top-height),color,new Vector2(sourceUv.xMax,sourceUv.yMin));
                mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first+2,first+3,first);
            }
        }
    }
}
