using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // Reuse one atlas: keep the original scroll edges and tile clean paper behind live rows.
    public sealed class ParchmentPanelGraphic:MaskableGraphic
    {
        public Texture artwork;
        public override Texture mainTexture{get{return artwork!=null?artwork:Texture2D.whiteTexture;}}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(artwork==null)return;
            Patch(mesh,new Rect(0,0,1006,250),new Rect(0,0,1006,250));
            Patch(mesh,new Rect(0,1278,1006,286),new Rect(0,1278,1006,286));
            Patch(mesh,new Rect(0,250,94,1028),new Rect(0,250,94,1028));
            Patch(mesh,new Rect(910,250,96,1028),new Rect(910,250,96,1028));
            for(float y=250;y<1278;y+=136)for(float x=94;x<910;x+=600)
            {
                float w=Mathf.Min(600,910-x),h=Mathf.Min(136,1278-y);
                Patch(mesh,new Rect(x,y,w,h),new Rect(300,260,w,h));
            }
        }
        void Patch(VertexHelper mesh,Rect destination,Rect source)
        {
            var bounds=rectTransform.rect;float sx=bounds.width/1006,sy=bounds.height/1564;
            float x0=bounds.xMin+destination.x*sx,x1=x0+destination.width*sx;
            float y1=bounds.yMax-destination.y*sy,y0=y1-destination.height*sy;
            float u0=source.x/1006,u1=(source.x+source.width)/1006,v1=1-source.y/1564,v0=1-(source.y+source.height)/1564;
            int n=mesh.currentVertCount;
            mesh.AddVert(new Vector3(x0,y0,0),color,new Vector2(u0,v0));mesh.AddVert(new Vector3(x0,y1,0),color,new Vector2(u0,v1));
            mesh.AddVert(new Vector3(x1,y1,0),color,new Vector2(u1,v1));mesh.AddVert(new Vector3(x1,y0,0),color,new Vector2(u1,v0));
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n+2,n+3,n);
        }
    }
}
