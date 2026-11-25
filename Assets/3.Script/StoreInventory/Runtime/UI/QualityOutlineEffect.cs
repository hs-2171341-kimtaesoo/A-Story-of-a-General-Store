using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // Four silhouette offsets, one shared material. RGB comes from the rarity, alpha from the sprite.
    [DisallowMultipleComponent]
    public sealed class QualityOutlineEffect:BaseMeshEffect
    {
        static Material sharedMaterial;
        static bool warned;
        readonly List<UIVertex> vertices=new List<UIVertex>();
        Color outlineColor=Color.white;float width=2;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetMaterial(){ReleaseSharedMaterial(false);}
        public static void ReleaseSharedMaterial(bool immediate)
        {
            if(sharedMaterial==null){warned=false;return;}
            foreach(var effect in Object.FindObjectsByType<QualityOutlineEffect>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(effect.graphic!=null){effect.graphic.material=null;effect.graphic.SetVerticesDirty();}
            if(sharedMaterial!=null){if(immediate)Object.DestroyImmediate(sharedMaterial);else Object.Destroy(sharedMaterial);}
            sharedMaterial=null;warned=false;
        }
        protected override void OnEnable(){base.OnEnable();Prepare();}
        protected override void OnCanvasHierarchyChanged(){base.OnCanvasHierarchyChanged();Prepare();}
        public void Configure(Color tint,float thickness)
        {
            thickness=Mathf.Clamp(thickness,0,6);
            if(outlineColor!=tint||width!=thickness){outlineColor=tint;width=thickness;if(graphic!=null)graphic.SetVerticesDirty();}
            enabled=width>0;
            if(enabled)Prepare();
        }
        void Prepare()
        {
            if(graphic==null)return;
            if(sharedMaterial==null)
            {
                var shader=Resources.Load<Shader>("StoreInventory/QualityOutline");
                if(shader==null||!shader.isSupported){if(!warned){warned=true;Debug.LogError("QualityOutline.shader is missing or unsupported; using the original graphic.");}return;}
                sharedMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            }
            if(graphic.material!=sharedMaterial)graphic.material=sharedMaterial;
            var canvas=graphic.canvas;if(canvas==null)canvas=GetComponentInParent<Canvas>(true);
            if(canvas!=null)canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
        }
        public override void ModifyMesh(VertexHelper helper)
        {
            if(!IsActive()||width<=0)return;
            if(sharedMaterial==null)Prepare();if(sharedMaterial==null)return;
            vertices.Clear();helper.GetUIVertexStream(vertices);int count=vertices.Count;
            // Keep pathological user-created meshes within the UI vertex budget.
            if(count==0||count>6000)return;
            if(vertices.Capacity<count*6)vertices.Capacity=count*6;
            for(int pass=0;pass<4;pass++)
            {
                float x=(pass<2?-width:width),y=(pass%2==0?-width:width);
                for(int n=0;n<count;n++)
                {
                    var vertex=vertices[n];vertex.position+=new Vector3(x,y,0);
                    var tint=outlineColor;tint.a*=vertex.color.a/255f;vertex.color=tint;
                    vertex.uv1=new Vector4(1,0,0,0);vertices.Add(vertex);
                }
            }
            // Draw silhouette copies first; the original graphic stays on top.
            for(int n=0;n<count;n++){var vertex=vertices[n];vertex.uv1=Vector4.zero;vertices.Add(vertex);}
            vertices.RemoveRange(0,count);helper.Clear();helper.AddUIVertexTriangleStream(vertices);
        }
    }
}
