#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
// Import limits only; source PNGs and authored sprite coordinates are unchanged.
public static class StoreTextureBudget
{
    static string Key{get{return "GeneralStore.LowMemoryTextures."+Application.dataPath;}}
    public static bool LowMemory{get{return EditorPrefs.GetBool(Key,true);}}
    public static int Furniture{get{return LowMemory?512:1024;}}
    public static int Outline{get{return LowMemory?256:512;}}
    public static int DisplayArt{get{return LowMemory?1024:2048;}}
    public static int Background{get{return LowMemory?1024:2048;}}
    public static int Customer{get{return LowMemory?512:1024;}}
    [MenuItem("Store Inventory/Memory/Use Low Memory Textures")]
    public static void UseLow(){Apply(true);}
    [MenuItem("Store Inventory/Memory/Restore Previous Texture Quality")]
    public static void UsePrevious(){Apply(false);}
    static void Apply(bool low)
    {
        if(Application.isPlaying){Debug.LogWarning("Stop Play before reimporting textures.");return;}
        EditorPrefs.SetBool(Key,low);
        Furniture13SceneBuilder.PrepareSprites();CustomDisplayArtBuilder.PrepareSprites();
        CounterDemoSceneBuilder.PrepareSprites();SlidingDemoSceneBuilder.PrepareCustomerSprite();
        Debug.Log("Store texture profile: "+(low?"Low Memory":"Previous Quality")+". Original PNGs and scene transforms preserved. Compare in a fresh Player run; this does not clear Editor memory.");
    }
}
#endif
