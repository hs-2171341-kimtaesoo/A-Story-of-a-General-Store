using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // Render the supplied empty frame once, without paper patches or repeated texture regions.
    public sealed class ParchmentPanelGraphic:MaskableGraphic
    {
        public Texture artwork;
        public override Texture mainTexture{get{return artwork!=null?artwork:Texture2D.whiteTexture;}}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(artwork==null)return;
            var bounds=rectTransform.rect;
            mesh.AddVert(new Vector3(bounds.xMin,bounds.yMin,0),color,new Vector2(0,0));mesh.AddVert(new Vector3(bounds.xMin,bounds.yMax,0),color,new Vector2(0,1));
            mesh.AddVert(new Vector3(bounds.xMax,bounds.yMax,0),color,new Vector2(1,1));mesh.AddVert(new Vector3(bounds.xMax,bounds.yMin,0),color,new Vector2(1,0));
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(2,3,0);
        }
    }
}
