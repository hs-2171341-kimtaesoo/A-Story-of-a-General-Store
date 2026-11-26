#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Unity integration checks. Assets live in a unique temporary folder and are removed in finally.
public static class PrefabLayoutChecks
{
    static int checks;
    static void Check(bool ok,string error){if(!ok)throw new InvalidOperationException(error);checks++;}
    static bool Close(Vector2 a,Vector2 b){return (a-b).sqrMagnitude<.001f;}
    public static int Run()
    {
        checks=0;CheckCenteredIcons();CheckPrefabRoundTrip();CheckCurrentLayout();return checks;
    }
    static void CheckCenteredIcons()
    {
        var root=new GameObject("Icon Slot Check",typeof(RectTransform));
        Texture2D texture=null;Sprite tall=null,wide=null;
        try
        {
            var image=InventorySceneBuilder.IconSlot("Icon",root.transform,13,17,160,100);
            var slot=(RectTransform)image.transform.parent;var slotPosition=slot.anchoredPosition;
            texture=new Texture2D(64,64);tall=Sprite.Create(texture,new Rect(0,0,12,48),new Vector2(.2f,.8f),100,0,SpriteMeshType.FullRect);
            wide=Sprite.Create(texture,new Rect(0,0,48,12),new Vector2(.8f,.2f),100,0,SpriteMeshType.FullRect);
            foreach(var sprite in new[]{tall,wide})
            {
                image.sprite=sprite;
                var rect=image.rectTransform;
                Check(rect.anchorMin==Vector2.zero&&rect.anchorMax==Vector2.one&&rect.pivot==new Vector2(.5f,.5f),"아이콘 중앙 기준/Slot 확장 설정 오류");
                using(var mesh=new VertexHelper())
                {
                    typeof(global::UnityEngine.UI.Image).GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(image,new object[]{mesh});
                    Check(mesh.currentVertCount==4,"아이콘 메시 생성 실패");
                    Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
                    for(int n=0;n<mesh.currentVertCount;n++){var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,n);min=Vector2.Min(min,vertex.position);max=Vector2.Max(max,vertex.position);}
                    Check(Close((min+max)*.5f,rect.rect.center),"세로/가로 Sprite가 Slot 중앙에서 벗어났습니다.");
                    Check(Mathf.Abs((max.x-min.x)/(max.y-min.y)-sprite.rect.width/sprite.rect.height)<.001f,"아이콘 원본 비율 손상");
                    Check(max.x<=rect.rect.xMax+.01f&&min.x>=rect.rect.xMin-.01f&&max.y<=rect.rect.yMax+.01f&&min.y>=rect.rect.yMin-.01f,"아이콘 Slot 범위 초과");
                }
                Check(Close(slot.anchoredPosition,slotPosition)&&Close(slot.sizeDelta,new Vector2(160,100)),"Sprite 교체가 배치용 Slot을 변경했습니다.");
            }
            var nested=InventorySceneBuilder.IconSlot("Responsive Icon",slot);slot.sizeDelta=new Vector2(210,130);
            Check(Close(nested.rectTransform.rect.size,new Vector2(210,130)),"부모 크기 변경이 중첩 Slot에 반영되지 않습니다.");
            var area=new Rect(.14f,.07f,.58f,.86f);AuthoredRowLayout.FitViewport(slot,area,300,600);
            Check(Close(slot.anchoredPosition,new Vector2(42,-42))&&Close(slot.sizeDelta,new Vector2(174,516)),"저장된 Viewport 비율 적용 오류");
            var frame=(RectTransform)root.transform;frame.sizeDelta=new Vector2(300,600);
            var read=AuthoredRowLayout.ReadViewport(slot,frame);
            Check(Mathf.Abs(read.x-area.x)<.001f&&Mathf.Abs(read.y-area.y)<.001f&&Mathf.Abs(read.width-area.width)<.001f&&Mathf.Abs(read.height-area.height)<.001f,"Hierarchy Viewport 배치 읽기 오류");
        }
        finally{Object.DestroyImmediate(root);if(tall!=null)Object.DestroyImmediate(tall);if(wide!=null)Object.DestroyImmediate(wide);if(texture!=null)Object.DestroyImmediate(texture);}
    }
    static void CheckPrefabRoundTrip()
    {
        string folder="Assets/StoreInventorySettings/PrefabChecks-"+Guid.NewGuid().ToString("N");
        string rowPath=folder+"/Row.prefab",layoutPath=folder+"/Layout.prefab";
        var scene=EditorSceneManager.NewPreviewScene();GameObject root=null,first=null,second=null,external=null,loadedRow=null;
        try
        {
            root=new GameObject("Authored Layout Check",typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var panel=root.AddComponent<InventoryPanel>();var ui=root.AddComponent<UpgradeUI>();ui.panel=panel;
            PurchaseSceneBuilder.Build(root.transform,ui);
            var authored=(RectTransform)ui.furniturePreview.transform.parent;
            authored.anchoredPosition+=new Vector2(11,-9);Vector2 savedPosition=authored.anchoredPosition;
            ui.storageActionButton.GetComponentInChildren<Text>(true).fontSize=27;
            external=new GameObject("External Reference Check");panel.shop=external.AddComponent<ShopfrontUI>();
            bool rejected=false;try{StoreLayoutPrefabTools.ValidateReferenceScope(root);}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"Prefab 밖의 씬 참조를 저장 전에 차단하지 못했습니다.");panel.shop=null;

            var seed=ui.cardTemplate;ui.cardTemplate=StoreLayoutPrefabTools.SaveTemplateAsset(seed,rowPath);Object.DestroyImmediate(seed.gameObject);
            StoreLayoutPrefabTools.SaveNewLayout(root,layoutPath);
            Check(PrefabUtility.IsPartOfPrefabInstance(root)&&EditorUtility.IsPersistent(ui.cardTemplate),"공통 배치/독립 행 Prefab 연결 실패");
            string original=File.ReadAllText(layoutPath),guid=AssetDatabase.AssetPathToGUID(layoutPath);
            rejected=false;try{StoreLayoutPrefabTools.SaveNewLayout(root,layoutPath);}catch(InvalidOperationException){rejected=true;}
            Check(rejected&&original==File.ReadAllText(layoutPath)&&guid==AssetDatabase.AssetPathToGUID(layoutPath),"기존 Prefab 덮어쓰기 차단 실패");
            Object.DestroyImmediate(root);root=null;

            loadedRow=PrefabUtility.LoadPrefabContents(rowPath);
            var edited=loadedRow.GetComponent<FurnitureCardView>();
            var slot=(RectTransform)edited.thumbnail.transform.parent;slot.anchoredPosition+=new Vector2(5,-3);Vector2 rowPosition=slot.anchoredPosition;
            ((RectTransform)edited.transform).sizeDelta=new Vector2(((RectTransform)edited.transform).sizeDelta.x,140);
            PrefabUtility.SaveAsPrefabAsset(loadedRow,rowPath);PrefabUtility.UnloadPrefabContents(loadedRow);loadedRow=null;
            AssetDatabase.ImportAsset(layoutPath,ImportAssetOptions.ForceUpdate);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(layoutPath);
            first=StoreLayoutPrefabTools.InstantiateLayout(prefab,scene);
            second=StoreLayoutPrefabTools.InstantiateLayout(prefab,scene);
            foreach(var instance in new[]{first,second})
            {
                var bound=instance.GetComponent<UpgradeUI>();
                Check(bound.panel==instance.GetComponent<InventoryPanel>(),"Prefab 재생성 후 데이터 참조가 이전 씬을 가리킵니다.");
                Check(bound.storageActionButton.onClick.GetPersistentEventCount()==1&&bound.storageActionButton.onClick.GetPersistentTarget(0)==bound,"Prefab 버튼 이벤트 참조 재연결 실패/중복");
                Check(Close(((RectTransform)bound.furniturePreview.transform.parent).anchoredPosition,savedPosition),"새 인스턴스에서 수정한 미리보기 배치가 사라졌습니다.");
                Check(bound.storageActionButton.GetComponentInChildren<Text>(true).fontSize==27,"저장한 글자 크기가 유지되지 않습니다.");
                Check(Close(((RectTransform)bound.cardTemplate.thumbnail.transform.parent).anchoredPosition,rowPosition),"중첩 행 Prefab 수정이 새 데모에 전파되지 않습니다.");
                Check(Mathf.Abs(AuthoredRowLayout.Pitch(bound.cardTemplate,112,8,true)-148)<.001f,"수정한 행 Prefab 높이가 스크롤 간격에 반영되지 않습니다.");
                var liveRow=Object.Instantiate(bound.cardTemplate,bound.furnitureList);
                var image=liveRow.thumbnail;var rect=image.rectTransform;Vector2 imagePosition=rect.anchoredPosition,imageSize=rect.sizeDelta;
                liveRow.Bind("test",id=>{},"테스트","진열 공간","",null,true);
                Check(Close(rect.anchoredPosition,imagePosition)&&Close(rect.sizeDelta,imageSize),"데이터 바인딩이 아이콘 배치를 덮어씁니다.");
                Object.DestroyImmediate(liveRow.gameObject);
                StoreLayoutPrefabTools.ValidateReferenceScope(instance);checks++;
            }
            Object.DestroyImmediate(first);first=null;
            Check(second!=null&&second.GetComponent<UpgradeUI>().panel!=null,"다른 테스트 인스턴스 제거가 현재 인스턴스 참조를 끊었습니다.");
            Check(original==File.ReadAllText(layoutPath),"새 인스턴스 생성이 저장된 공통 배치를 변경했습니다.");
        }
        finally
        {
            if(loadedRow!=null)PrefabUtility.UnloadPrefabContents(loadedRow);
            if(root!=null)Object.DestroyImmediate(root);if(first!=null)Object.DestroyImmediate(first);if(second!=null)Object.DestroyImmediate(second);if(external!=null)Object.DestroyImmediate(external);
            EditorSceneManager.ClosePreviewScene(scene);
            if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);
        }
    }
    static void CheckCurrentLayout()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(StoreLayoutPrefabTools.LayoutPath);
        if(prefab==null){Debug.Log("공통 배치 전체 참조 검사는 Create Latest Demo로 Prefab을 생성한 뒤 실행됩니다. 독립 저장/중앙 정렬 검사는 완료했습니다.");return;}
        StoreLayoutPrefabTools.ValidateLayout(prefab);checks++;
        string original=File.ReadAllText(StoreLayoutPrefabTools.LayoutPath);
        var scene=EditorSceneManager.NewPreviewScene();GameObject instance=null;
        try
        {
            instance=StoreLayoutPrefabTools.InstantiateLayout(prefab,scene);StoreLayoutPrefabTools.ValidateLayout(instance);checks++;
            var layout=instance.GetComponent<StoreLayoutRoot>();
            Check(layout.panel.inventory==layout.inventory&&layout.management.panel==layout.panel&&layout.purchases.panel==layout.panel,"공통 배치 인스턴스의 재고/UI 참조 복원 실패");
            var counter=instance.GetComponentInChildren<SceneCounterOcclusion>(true);
            var previousBackground=layout.backgroundImage.sprite;
            var furniture=instance.GetComponentInChildren<SceneFurnitureHotspot>(true);
            var catalogEntry=layout.management.art.Find(furniture.typeId);
            var displayFrame=layout.management.display.typeTemplates[0].template.appearance.frame;
            var tooltipFrame=layout.panel.tooltip.window.GetComponent<global::UnityEngine.UI.Image>();
            furniture.furnitureImage.sprite=null;
            catalogEntry.thumbnail=null;
            displayFrame.sprite=null;
            tooltipFrame.sprite=null;
            layout.panel.tooltip.iconFit=null;
            layout.panel.tooltip.priceLabel=null;
            counter.counterImage.sprite=null;
            Check(DemoSceneArtwork.Repair(instance)&&counter.counterImage.sprite!=null,"카운터 Sprite 복구 실패");
            Check(furniture.furnitureImage.sprite!=null&&catalogEntry.thumbnail!=null&&displayFrame.sprite!=null&&tooltipFrame.sprite!=null&&layout.panel.tooltip.iconFit!=null&&layout.panel.tooltip.priceLabel!=null,"가구/목록 아이콘/진열창/툴팁 아트 복원 실패");
            Check(layout.backgroundImage.sprite==previousBackground&&!DemoSceneArtwork.Repair(instance),"정상 아트 참조 변경 또는 재복구");
            var open=layout.management.openButton.onClick;var buy=layout.purchases.addDisplayButton.onClick;var storage=layout.purchases.storageActionButton.onClick;
            Check(open.GetPersistentEventCount()==1&&open.GetPersistentTarget(0)==layout.management,"진열장 열기 버튼이 다른 Prefab/씬을 가리킵니다.");
            Check(buy.GetPersistentEventCount()==1&&buy.GetPersistentTarget(0)==layout.purchases&&storage.GetPersistentEventCount()==1&&storage.GetPersistentTarget(0)==layout.purchases,"구매 버튼이 다른 Prefab/씬을 가리킵니다.");
            foreach(var entry in layout.management.display.typeTemplates)
                Check(entry.template.transform.IsChildOf(instance.transform),"진열창 템플릿이 다른 데모의 오브젝트를 가리킵니다.");
            Check(original==File.ReadAllText(StoreLayoutPrefabTools.LayoutPath),"통합 데모 재생성이 원본 Prefab을 변경했습니다.");
        }
        finally{if(instance!=null)Object.DestroyImmediate(instance);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
