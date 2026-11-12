using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace GeneralStore.Inventory
{
    // 아이콘 하나/CanvasRenderer 하나로 모양 전체를 표시합니다.
    // Occupied Cell, Tile, RectMask2D, Clipped Icon 자식들을 만들지 않습니다.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class InventoryIconGraphic : global::UnityEngine.UI.MaskableGraphic
    {
        // 이미 만들어 둔 씬/프리팹에는 RequireComponent 추가만으로 누락이 복구되지 않을 수 있습니다.
        // 마스크 등록(base.OnEnable) 전에 렌더러를 보장하여 RectMask2D 오류를 막습니다.
        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        public bool preserveSpriteAspect = true;
        public Color missingIconColor = new Color(.62f,.68f,.75f,.85f);
        Sprite iconSprite;
        ItemFootprint shape;
        int rotation;
        float cellAspect=1;
        public override Texture mainTexture { get { return iconSprite != null ? iconSprite.texture : Texture2D.whiteTexture; } }
        public void Configure(ItemFootprint footprint, Sprite sprite, int direction,float widthOverHeight)
        {
            if(shape==footprint && iconSprite==sprite && rotation==direction && cellAspect==widthOverHeight) return;
            shape=footprint; iconSprite=sprite; rotation=direction;cellAspect=widthOverHeight;
            SetVerticesDirty(); SetMaterialDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); if(shape==null) return;
            Rect rect=GetPixelAdjustedRect();
            bool hasSprite=iconSprite!=null;
            float aspect=hasSprite?iconSprite.rect.width/iconSprite.rect.height:1;
            Vector4 uv=hasSprite?DataUtility.GetOuterUV(iconSprite):new Vector4(0,0,1,1);
            float sx=rect.width/shape.Width(rotation),sy=rect.height/shape.Height(rotation);
            Color tint=hasSprite?color:missingIconColor;
            foreach(IconQuad quad in IconMeshGeometry.Build(shape,rotation,hasSprite && preserveSpriteAspect,aspect,cellAspect))
            {
                int start=helper.currentVertCount;
                Add(helper,rect.xMin+quad.x0*sx,rect.yMax-quad.y0*sy,quad.topLeft,uv,tint);
                Add(helper,rect.xMin+quad.x1*sx,rect.yMax-quad.y0*sy,quad.topRight,uv,tint);
                Add(helper,rect.xMin+quad.x1*sx,rect.yMax-quad.y1*sy,quad.bottomRight,uv,tint);
                Add(helper,rect.xMin+quad.x0*sx,rect.yMax-quad.y1*sy,quad.bottomLeft,uv,tint);
                helper.AddTriangle(start,start+1,start+2); helper.AddTriangle(start+2,start+3,start);
            }
        }
        static void Add(VertexHelper helper,float x,float y,IconPoint point,Vector4 uv,Color tint)
        {
            UIVertex vertex=UIVertex.simpleVert; vertex.position=new Vector3(x,y,0); vertex.color=tint;
            vertex.uv0=new Vector2(uv.x+(uv.z-uv.x)*point.x,uv.y+(uv.w-uv.y)*point.y); helper.AddVert(vertex);
        }
    }
}
