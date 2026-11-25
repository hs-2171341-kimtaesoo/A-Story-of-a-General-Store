using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // Small static gold ornaments; no extra texture, Update, or per-frame allocation.
    public sealed class LaurelBadgeGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            for(int side=0;side<2;side++)
            {
                for(int n=0;n<12;n++)
                {
                    Vector2 a=Stem(n/12f),b=Stem((n+1)/12f),normal=new Vector2(b.y-a.y,a.x-b.x).normalized*.8f;
                    Quad(mesh,side,a-normal,a+normal,b+normal,b-normal,new Color(.52f,.28f,.06f));
                }
                for(int n=0;n<6;n++)
                {
                    var p=Stem(.1f+n*.15f);
                    Leaf(mesh,side,p,new Vector2(-12,11));Leaf(mesh,side,p,new Vector2(13,12));
                }
            }
        }
        static Vector2 Stem(float t){return new Vector2(13+22*(2*t-1)*(2*t-1),12+76*t);}
        void Leaf(VertexHelper mesh,int side,Vector2 root,Vector2 direction)
        {
            var middle=root+direction*.52f;var normal=new Vector2(-direction.y,direction.x).normalized*3.7f;
            Quad(mesh,side,root,middle+normal,root+direction,middle-normal,new Color(.78f,.49f,.13f));
        }
        void Quad(VertexHelper mesh,int side,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color shade)
        {
            int n=mesh.currentVertCount;Vertex(mesh,side,a,shade);Vertex(mesh,side,b,Color.Lerp(shade,Color.white,.22f));Vertex(mesh,side,c,shade);Vertex(mesh,side,d,shade);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n+2,n+3,n);
        }
        void Vertex(VertexHelper mesh,int side,Vector2 point,Color shade)
        {
            var bounds=rectTransform.rect;if(side==1)point.x=230-point.x;
            mesh.AddVert(new Vector3(bounds.xMin+point.x*bounds.width/230,bounds.yMin+point.y*bounds.height/100,0),shade*color,Vector2.zero);
        }
    }
}
