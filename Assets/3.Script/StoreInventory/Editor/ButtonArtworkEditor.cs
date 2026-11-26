#if UNITY_EDITOR
using System;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ButtonArtworkEditor
{
    public const string CatalogPath="Assets/StoreInventorySettings/ButtonArtwork.asset";
    const string Prefix="StoreInventory/ButtonStates/";
    static readonly string[] ManagementIds={"displays","furniture","storage","suppliers","workshops","temporary_storage"};
    static readonly string[] ManagementFiles={"01_display_management","02_furniture_purchase","03_add_box","04_suppliers","05_crafting","06_temporary_storage"};
    static readonly string[] FurnitureIds={"weapon_rack","mannequin","wall_shelf_lower","wall_shelf","aisle_left","table","aisle_right","cubby_cabinet","shelf","side_shelf","single_stand","drawer_cabinet"};
    static readonly string[] FurnitureFiles={"01_swords","02_armor","03_wall_shelf","04_decorated_wall_shelf","05_counter_left","06_cabinet","07_counter_right","08_cubby_shelf","09_flower_shelf","10_bookcase","11_shield","12_shelf_drawers"};
    static void EachSource(Action<string> action)
    {
        foreach(string name in ManagementFiles)foreach(string state in new[]{"normal","pressed","active"})action(Prefix+"management/"+name+"_"+state);
        foreach(string name in FurnitureFiles)foreach(string state in new[]{"normal","pressed","active"})action(Prefix+"furniture/"+name+"_"+state);
        foreach(string state in new[]{"normal","pressed"})action(Prefix+"other/close_x_"+state);
    }
    public static void ValidateSources()
    {
        EachSource(key=>{if(AssetImporter.GetAtPath("Assets/Resources/"+key+".png")==null)throw new InvalidOperationException("버튼 상태 Sprite 누락: "+key);});
    }
    public static void PrepareTextures(){EachSource(key=>SideMenuSceneBuilder.PreparePanelTexture(key,256));}
    static Sprite Load(string key)
    {
        var sprite=Resources.Load<Sprite>(key);
        if(sprite==null)throw new InvalidOperationException("버튼 상태 Sprite 불러오기 실패: "+key);
        return sprite;
    }
    static ButtonSpriteSet Set(string id,string stem,bool selected=true)
    {
        return new ButtonSpriteSet{id=id,normal=Load(stem+"_normal"),pressed=Load(stem+"_pressed"),active=selected?Load(stem+"_active"):null};
    }
    static bool Fill(ButtonSpriteSet set,string stem,bool selected)
    {
        bool changed=false;
        if(set.normal==null){set.normal=PrefabArtworkBindings.LoadSprite(stem+"_normal",256);changed=true;}
        if(set.pressed==null){set.pressed=PrefabArtworkBindings.LoadSprite(stem+"_pressed",256);changed=true;}
        if(selected&&set.active==null){set.active=PrefabArtworkBindings.LoadSprite(stem+"_active",256);changed=true;}
        return changed;
    }

    static bool FillGroup(ref ButtonSpriteSet[] sets,string[] ids,string[] files,string group)
    {
        var list=new System.Collections.Generic.List<ButtonSpriteSet>(sets??new ButtonSpriteSet[0]);
        bool changed=false;
        for(int i=0;i<ids.Length;i++)
        {
            string id=ids[i];
            var set=list.Find(candidate=>candidate!=null&&candidate.id==id);
            if(set==null){set=new ButtonSpriteSet{id=id};list.Add(set);changed=true;}
            changed |= Fill(set,Prefix+group+"/"+files[i],true);
        }
        if(changed)sets=list.ToArray();
        return changed;
    }

    internal static void RepairCatalog(ButtonArtworkCatalog catalog)
    {
        if(catalog==null)return;
        bool changed=FillGroup(ref catalog.management,ManagementIds,ManagementFiles,"management");
        changed |= FillGroup(ref catalog.furniture,FurnitureIds,FurnitureFiles,"furniture");
        if(catalog.close==null){catalog.close=new ButtonSpriteSet{id="close"};changed=true;}
        changed |= Fill(catalog.close,Prefix+"other/close_x",false);
        if(changed){EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);}
    }

    public static ButtonArtworkCatalog EnsureCatalog()
    {
        var asset=AssetDatabase.LoadAssetAtPath<ButtonArtworkCatalog>(CatalogPath);
        if(asset!=null)return asset; // Preserve the user's sprite overrides on future demos.
        if(AssetDatabase.LoadMainAssetAtPath(CatalogPath)!=null)throw new InvalidOperationException("버튼 아트 Asset 형식을 확인하세요: "+CatalogPath);
        PrepareTextures();StoreLayoutPrefabTools.EnsureFolder("Assets/StoreInventorySettings");
        asset=ScriptableObject.CreateInstance<ButtonArtworkCatalog>();
        asset.management=new ButtonSpriteSet[ManagementIds.Length];asset.furniture=new ButtonSpriteSet[FurnitureIds.Length];
        for(int n=0;n<ManagementIds.Length;n++)asset.management[n]=Set(ManagementIds[n],Prefix+"management/"+ManagementFiles[n]);
        for(int n=0;n<FurnitureIds.Length;n++)asset.furniture[n]=Set(FurnitureIds[n],Prefix+"furniture/"+FurnitureFiles[n]);
        asset.close=Set("close",Prefix+"other/close_x",false);
        AssetDatabase.CreateAsset(asset,CatalogPath);AssetDatabase.SaveAssets();return asset;
    }
    public static void ApplyToLayout(GameObject root)
    {
        var layout=root.GetComponent<StoreLayoutRoot>();
        var asset=layout!=null&&layout.buttonArtwork!=null?layout.buttonArtwork:EnsureCatalog();
        if(layout!=null)layout.buttonArtwork=asset;
        var management=root.GetComponentInChildren<RightSideMenu>(true);var furniture=root.GetComponentInChildren<LeftDisplayMenu>(true);
        if(management!=null&&management.buttonArtwork==null)management.buttonArtwork=asset;
        if(furniture!=null&&furniture.buttonArtwork==null)furniture.buttonArtwork=asset;
    }
}
#endif
