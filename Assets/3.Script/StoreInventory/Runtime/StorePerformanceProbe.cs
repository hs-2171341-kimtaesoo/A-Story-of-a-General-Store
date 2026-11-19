using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
namespace GeneralStore.Inventory
{
    // Explicit capture only. No Update, forced GC or automatic unload.
    public sealed class StorePerformanceProbe:MonoBehaviour
    {
        public InventoryPanel panel;public CounterTradeUI counter;
        bool previous;long previousAllocated,previousTextures,previousRenderTextures;
        public void Report()
        {
            if(panel==null||panel.inventory==null||panel.inventory.Model==null)return;
            long allocated=Profiler.GetTotalAllocatedMemoryLong(),reserved=Profiler.GetTotalReservedMemoryLong();
            var textures=Resources.FindObjectsOfTypeAll<Texture2D>();var textureSizes=new long[textures.Length];long textureBytes=0;
            for(int n=0;n<textures.Length;n++){textureSizes[n]=Profiler.GetRuntimeMemorySizeLong(textures[n]);textureBytes+=textureSizes[n];}
            Array.Sort(textureSizes,textures);
            var renderTextures=Resources.FindObjectsOfTypeAll<RenderTexture>();var renderSizes=new long[renderTextures.Length];long renderBytes=0;
            for(int n=0;n<renderTextures.Length;n++){renderSizes[n]=Profiler.GetRuntimeMemorySizeLong(renderTextures[n]);renderBytes+=renderSizes[n];}
            Array.Sort(renderSizes,renderTextures);
            var text=new StringBuilder(6000);
            text.AppendLine("STORE MEMORY REPORT "+DateTime.Now.ToString("s"));
            text.AppendLine(Application.isEditor?"Scope: UNITY EDITOR process, includes editor/preview/profiler assets":"Scope: PLAYER process");
            text.AppendLine("Resolution: "+Screen.width+"x"+Screen.height+" | FPS target: "+Application.targetFrameRate);
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                using(var process=System.Diagnostics.Process.GetCurrentProcess())
                {process.Refresh();text.AppendLine("OS working set: "+Mb(process.WorkingSet64)+" | private committed: "+Mb(process.PrivateMemorySize64));}
            }
            catch(Exception e){text.AppendLine("OS process counters unavailable: "+e.GetType().Name);}
#endif
            text.AppendLine("Unity allocated: "+Mb(allocated)+" | Unity reserved: "+Mb(reserved)+" | unused reserved: "+Mb(Profiler.GetTotalUnusedReservedMemoryLong()));
            text.AppendLine("Managed used: "+Mb(Profiler.GetMonoUsedSizeLong())+" | managed heap reserved: "+Mb(Profiler.GetMonoHeapSizeLong()));
            text.AppendLine("Graphics driver estimate: "+Mb(Profiler.GetAllocatedMemoryForGraphicsDriver())+" (may be unavailable/0; not exact VRAM)");
            text.AppendLine("Loaded Texture2D: "+textures.Length+" | estimated native size: "+Mb(textureBytes));
            text.AppendLine("Loaded RenderTexture: "+renderTextures.Length+" | estimated native size: "+Mb(renderBytes));
            if(previous)text.AppendLine("Since previous capture: Unity allocated "+Mb(allocated-previousAllocated)+" | Texture2D "+Mb(textureBytes-previousTextures)+" | RenderTexture "+Mb(renderBytes-previousRenderTextures));
            previous=true;previousAllocated=allocated;previousTextures=textureBytes;previousRenderTextures=renderBytes;
            int active=0;var runtimes=global::UnityEngine.Object.FindObjectsByType<InventoryRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var runtime in runtimes)if(runtime.isActiveAndEnabled)active++;
            var delivery=panel.GetComponentInChildren<DeliveryUI>(true);
            text.AppendLine("InventoryRuntime loaded/active: "+runtimes.Length+"/"+active+" | Canvas loaded: "+Resources.FindObjectsOfTypeAll<Canvas>().Length);
            text.AppendLine("Delivery items/cells: "+panel.Model.DeliveryItemCount+"/"+panel.Model.DeliveryOccupiedCells+" | visible delivery cards: "+(delivery==null?0:delivery.RenderedItemCount));
            text.AppendLine("Display cache: "+panel.shop.displays.CachedWindowCount+" | counter live/cached: "+(counter==null?0:counter.LiveViewCount)+"/"+(counter==null?0:counter.CachedViewCount)+" | registered grids: "+panel.grids.Length);
            text.AppendLine("Largest Texture2D (up to 12):");
            for(int n=textures.Length-1;n>=Math.Max(0,textures.Length-12);n--)
            {
                var texture=textures[n];text.AppendLine("  "+Mb(textureSizes[n])+" | "+texture.name+" | "+texture.width+"x"+texture.height+" | "+texture.format+" | readable="+texture.isReadable);
#if UNITY_EDITOR
                text.AppendLine("    "+UnityEditor.AssetDatabase.GetAssetPath(texture));
#endif
            }
            text.AppendLine("Largest RenderTexture (up to 8):");
            for(int n=renderTextures.Length-1;n>=Math.Max(0,renderTextures.Length-8);n--)
            {var texture=renderTextures[n];text.AppendLine("  "+Mb(renderSizes[n])+" | "+texture.name+" | "+texture.width+"x"+texture.height+" | "+texture.format+" | MSAA="+texture.antiAliasing);}
            text.AppendLine("Do not add these categories: counters overlap. A single sample/delta does not prove a leak. Object size APIs may report 0 without development profiling support.");
            string path=Path.Combine(Application.persistentDataPath,"store-memory-report.txt");
            try{File.WriteAllText(path,text.ToString());Debug.Log(text.ToString()+"\nReport: "+path,this);}
            catch(Exception e){Debug.Log(text.ToString()+"\nCould not write report: "+e.Message,this);}
        }
        static string Mb(long bytes){return (bytes/1048576.0).ToString("F2")+" MiB";}
    }
}
