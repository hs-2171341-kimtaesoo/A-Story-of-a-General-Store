#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;

// The main item table is authored data, connected once in Editor, never scanned per frame.
public static class ItemTableTools
{
    [Serializable] sealed class ImportTotals{public int items,qualityItems,recipes;public string[] tradeDisabled;}
    public const string DataFolder="Assets/StoreInventoryData/ItemTable208";
    public static void Configure(InventoryRuntime runtime)
    {
        runtime.itemJson=Data("items");runtime.customerJson=Data("customers");runtime.supplierJson=Data("suppliers");runtime.workshopJson=Data("workshops");
        runtime.clockSettings=JsonUtility.FromJson<ShopClockSettings>(Data("clock_settings").text);
        runtime.displayJson=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/StoreInventory/display_fixtures.json");
        runtime.saveFileName="store-main-items208-v1.json";
        runtime.initialItems=new[]{
            Seed("warehouse","W012","common",1),Seed("warehouse","P001","common",1),Seed("warehouse","P001","superior",1),Seed("warehouse","W002","common",1),
            Seed("warehouse_2","R001","common",2),Seed("warehouse_2","R004","common",2),Seed("warehouse_2","M013","common",1),Seed("warehouse_2","M014","common",1)};
    }
    static InventoryInitialItem Seed(string box,string id,string quality,int amount)
    {return new InventoryInitialItem{containerId=box,itemId=id,quality=quality,amount=amount};}
    static TextAsset Data(string name)
    {
        var file=AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder+"/"+name+"_table.json");
        if(file==null)throw new InvalidOperationException("메인 아이템 표 파일 누락: "+name);return file;
    }
    public static void ApplyScheduleToSelection()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Play를 중지하세요.");return;}
        var selected=Selection.activeGameObject;var runtimes=selected==null?new InventoryRuntime[0]:selected.GetComponentsInChildren<InventoryRuntime>(true);
        if(runtimes.Length!=1){Debug.LogWarning("Store Scene 또는 Store Inventory Data 하나를 선택하세요.");return;}
        var runtime=runtimes[0];var root=runtime.transform.root.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(root,"Apply Workshop Schedule");
        runtime.clockSettings=JsonUtility.FromJson<ShopClockSettings>(Data("clock_settings").text);runtime.workshopJson=Data("workshops");
        var simulation=runtime.GetComponent<ShopSimulation>();if(simulation!=null){simulation.gameMinutesPerSecond=1;simulation.testDeliveryItemId="P001";simulation.testDeliveryQuality="common";EditorUtility.SetDirty(simulation);}
        var panel=root.GetComponentInChildren<InventoryPanel>(true);if(panel!=null){WorkshopSceneBuilder.Rebuild(panel);CompactDemoSceneBuilder.RebuildDelivery(panel);}
        foreach(var toggle in root.GetComponentsInChildren<DebugPanelToggle>(true))
            if(toggle.transform.parent is RectTransform layout)DrawerSceneBuilder.EnsureItemIdGrant(layout,simulation);
        EditorUtility.SetDirty(runtime);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        Debug.Log("낮9분/밤6분/새벽3분, 제작121개, 야간 수당, 16칸 임시 저장고, 재료 조합형 공방 및 ID 지급 적용. 씬을 저장하세요. 가구와 상자 배치는 유지했습니다.",runtime);
    }
    [MenuItem("Store Inventory/Prepare And Connect Item Icons By ID")]
    public static void PrepareIcons()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Play를 중지하세요.");return;}
        var selected=Selection.activeGameObject;var runtimes=selected==null?new InventoryRuntime[0]:selected.GetComponentsInChildren<InventoryRuntime>(true);
        if(runtimes.Length==0)runtimes=UnityEngine.Object.FindObjectsByType<InventoryRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        if(runtimes.Length!=1){Debug.LogWarning("메인 씬의 Store Inventory Data 하나를 선택하세요.");return;}
        ConnectIcons(runtimes[0],true);
    }
    public static void ConnectIcons(InventoryRuntime runtime,bool prepareTextures)
    {
        if(runtime==null||runtime.itemJson==null)throw new InvalidOperationException("Item Json을 먼저 연결하세요.");
        if(runtime.spriteCatalog==null)InventoryAssetTools.Connect(runtime);
        var catalog=new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(runtime.itemJson.text));var keys=new HashSet<string>(StringComparer.Ordinal);
        foreach(var item in catalog.AllDefinitions())keys.Add(catalog.IconKey(item.id));
        var folders=new List<string>();foreach(string folder in new[]{"Assets/ItemIcons","Assets/Resources/ItemIcons","Assets/4.Sprite/Inventory"})if(AssetDatabase.IsValidFolder(folder))folders.Add(folder);
        var sprites=new Dictionary<string,Sprite>(StringComparer.Ordinal);var duplicates=new HashSet<string>(StringComparer.Ordinal);
        if(folders.Count>0)foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",folders.ToArray()))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            if(prepareTextures && !path.StartsWith("Assets/4.Sprite/Inventory/",StringComparison.Ordinal))
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer!=null)
                {
                    bool changed=importer.textureType!=TextureImporterType.Sprite||importer.isReadable||importer.mipmapEnabled||importer.maxTextureSize>512||!importer.alphaIsTransparency||importer.textureCompression!=TextureImporterCompression.Compressed;
                    if(changed)
                    {
                        if(importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;}
                        importer.isReadable=false;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=Math.Min(512,importer.maxTextureSize);importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
                    }
                }
            }
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var sprite=asset as Sprite;if(sprite==null||!keys.Contains(sprite.name))continue;
                Sprite previous;if(sprites.TryGetValue(sprite.name,out previous)&&previous!=sprite){duplicates.Add(sprite.name);continue;}
                sprites[sprite.name]=sprite;
            }
        }
        int linked=0,missing=0;var missingKeys=new List<string>();Undo.RecordObject(runtime.spriteCatalog,"Connect Item Icons By ID");
        foreach(var row in runtime.spriteCatalog.itemIcons)
        {
            Sprite sprite;if(row==null)continue;
            if(row.sprite!=null)continue;
            if(!string.IsNullOrEmpty(row.resourcePath)&&Resources.Load<Sprite>(row.resourcePath)!=null)continue;
            if(!duplicates.Contains(row.key)&&sprites.TryGetValue(row.key,out sprite)&&row.sprite==null)
            {
                string path=AssetDatabase.GetAssetPath(sprite);int resource=path.LastIndexOf("/Resources/",StringComparison.Ordinal);var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(runtime.spriteCatalog.useLazyResourceIcons&&resource>=0&&importer!=null&&importer.spriteImportMode==SpriteImportMode.Single)
                    row.resourcePath=path.Substring(resource+11,path.Length-resource-11-System.IO.Path.GetExtension(path).Length);
                else{row.sprite=sprite;row.resourcePath="";}linked++;
                continue;
            }
            missing++;missingKeys.Add(row.key);
        }
        EditorUtility.SetDirty(runtime.spriteCatalog);EditorUtility.SetDirty(runtime);AssetDatabase.SaveAssets();
        Debug.Log("아이콘 연결 "+linked+"개 / 미연결 "+missing+"개. 기존 수동 연결은 유지. 검색 폴더: Assets/ItemIcons, Assets/Resources/ItemIcons, Assets/4.Sprite/Inventory. ID 대소문자를 맞추세요.",runtime);
        if(missing>0)Debug.LogWarning("원본 Sprite 또는 유효한 경로가 없는 아이템 키: "+string.Join(", ",missingKeys));
        if(duplicates.Count>0)Debug.LogWarning("중복 Sprite 이름으로 자동 연결하지 않은 ID: "+string.Join(", ",duplicates));
    }
    [MenuItem("Store Inventory/Audit/Check Main Item Table")]
    public static void RunChecks()
    {
        CheckOptionalRecipes();
        CheckDebugGrants();
        CheckWorkingSchedule();
        var catalog=new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(Data("items").text));
        var suppliers=new SupplierCatalog(JsonUtility.FromJson<SupplierDatabase>(Data("suppliers").text),catalog);
        var workshops=new WorkshopCatalog(JsonUtility.FromJson<WorkshopDatabase>(Data("workshops").text),catalog);
        var customers=new CustomerCatalog(JsonUtility.FromJson<CustomerDatabase>(Data("customers").text));
        var fixtures=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/StoreInventory/display_fixtures.json").text));
        int quality=0,recipes=0,blocked=0;
        foreach(var item in catalog.AllDefinitions()){if(item.qualities.Length==5)quality++;if(item.sourceRecipe!=null)recipes++;if(!catalog.CanTrade(item.id))blocked++;if(catalog.IconKey(item.id)!=item.id)throw new Exception("공통 아이콘 ID 불일치");}
        var save=new InventorySave{gold=10000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});
        fixtures.AddInitialContainers(save,false,new[]{"mannequin","table","weapon_rack"});
        var model=new InventoryModel(catalog,save,suppliers,fixtures,workshops);string error;string armor=model.OwnedDisplayId("mannequin",0);
        if(model.TryAdd(armor,"W003","common",1,0,out error))throw new Exception("갑옷 거치대가 무기를 받았습니다.");
        if(!model.TryAdd(armor,"W012","common",1,0,out error))throw new Exception("갑옷 수용 실패: "+error);
        if(!model.TryAdd("warehouse","E017","none",1,0,out error))throw new Exception(error);
        var eventItem=model.GetContainer("warehouse").items[0];int price;
        if(model.TryGetOfferPrice("warehouse",eventItem.instanceId,out price)||model.TrySell(new[]{new SaleLine{containerId="warehouse",instanceId=eventItem.instanceId,price=0}},model.Revision,out long revenue,out error))throw new Exception("판매 불가 이벤트 물품 판매 우회");
        if(model.TryOrderNextDawn(1,"E017","none",1,0,out error))throw new Exception("판매 불가 물품 주문 우회");
        // Existing non-armor goods migrate on a copy, preserving their UID and acquisition price.
        var previous=model.Snapshot();var previousArmor=previous.containers.Find(box=>box.containerId==armor).items[0];
        previousArmor.itemId="P001";previousArmor.purchasePrice=77;
        previous.listings.Add(new DisplayListing{instanceId=previousArmor.instanceId,price=100});
        if(!model.TryRestore(previous,out error)||model.GetItem(armor,previousArmor.instanceId)!=null)throw new Exception("기존 마네킹 물품 이동 실패: "+error);
        var relocated=model.GetItem("warehouse",previousArmor.instanceId);
        if(relocated==null||relocated.purchasePrice!=77||model.DisplayListingCount!=0||previous.containers.Find(box=>box.containerId==armor).items.Count!=1)throw new Exception("기존 물품 보존/복사/판매 등록 해제 실패");
        // Full warehouse: overflow moves to delivery, never disappears.
        var full=previous.Copy();var storage=full.containers.Find(box=>box.containerId=="warehouse");storage.items.Clear();
        for(int y=0;y<InventoryModel.WarehouseRows;y++)for(int x=0;x<8;x++)storage.items.Add(new ItemRecord{instanceId="full_"+x+"_"+y,itemId="E001",quality="none",x=x,y=y,purchasePrice=1});
        if(!model.TryRestore(full,out error)||model.GetItem(InventoryModel.TemporaryStorageId,previousArmor.instanceId)==null)throw new Exception("상자 가득 찬 경우 택배 보존 실패: "+error);
        var totals=JsonUtility.FromJson<ImportTotals>(AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder+"/import_report.json").text);
        if(catalog.Count!=totals.items||quality!=totals.qualityItems||recipes!=totals.recipes||blocked!=totals.tradeDisabled.Length)throw new Exception("메인 표 행수/품질/조합/거래 제외 수가 변환 보고서와 다릅니다.");
        Debug.Log("메인 표 검사: "+catalog.Count+"품목 / 품질 "+quality+" / 조합 "+recipes+" / 판매 제외 "+blocked+" / 활성 제작 "+workshops.Data.recipes.Length+" / 손님 "+customers.All().Count);
    }
    static void CheckOptionalRecipes()
    {
        const string plain="\"id\":\"E001\",\"type\":\"Quest\",\"width\":1,\"height\":1,\"qualities\":[{\"quality\":\"none\",\"name\":\"Seed\",\"priceMultiplier\":1}]";
        const string crafted="{\"id\":\"M001\",\"type\":\"Material\",\"width\":1,\"height\":1,\"qualities\":[{\"quality\":\"common\",\"name\":\"Plank\",\"priceMultiplier\":1}],\"sourceRecipe\":{\"outputCount\":1,\"ingredients\":[{\"itemId\":\"E001\",\"count\":1}]}}";
        foreach(string optional in new[]{"",",\"sourceRecipe\":null",",\"sourceRecipe\":{}",",\"sourceRecipe\":{\"outputCount\":0,\"ingredients\":[]}"})
        {
            var catalog=new ItemCatalog(JsonUtility.FromJson<ItemDatabase>("{\"items\":[{"+plain+optional+"},"+crafted+"]}"));
            if(catalog.Find("E001").sourceRecipe!=null||catalog.Find("M001").sourceRecipe==null||catalog.Find("M001").sourceRecipe.ingredients[0].count!=1)
                throw new Exception("생략/빈 원본 조합 정규화 또는 정상 조합 보존 실패");
        }
        // Exercise both forms of Unity's empty inline object regardless of Editor version.
        foreach(var ingredients in new WorkshopIngredient[][]{null,new WorkshopIngredient[0]})
        {
            var data=JsonUtility.FromJson<ItemDatabase>("{\"items\":[{"+plain+"}]} ");data.items[0].sourceRecipe=new ItemRecipeDefinition{ingredients=ingredients};
            if(new ItemCatalog(data).Find("E001").sourceRecipe!=null)throw new Exception("빈 인라인 조합 정규화 실패");
        }
        foreach(string invalid in new[]{"{\"outputCount\":1}","{\"outputCount\":0,\"ingredients\":[{\"itemId\":\"E001\",\"count\":1}]}","{\"outputCount\":1,\"ingredients\":[{\"itemId\":\"E001\",\"count\":0}]}","{\"outputCount\":1,\"ingredients\":[{\"itemId\":\"missing\",\"count\":1}]}"})
        {
            bool rejected=false;try{new ItemCatalog(JsonUtility.FromJson<ItemDatabase>("{\"items\":[{"+plain+",\"sourceRecipe\":"+invalid+"}]}"));}catch(ArgumentException){rejected=true;}
            if(!rejected)throw new Exception("불완전한 원본 조합 검사가 우회되었습니다.");
        }
    }
    static void ScheduleCheck(bool condition,string message)
    {if(!condition)throw new Exception("공방 일정 검사: "+message);}
    static void CheckDebugGrants()
    {
        Action<bool,string> check=(pass,note)=>{if(!pass)throw new Exception("ID 지급 검사: "+note);};
        var model=ScheduleModel();int stored,delivered;string error;long gold=model.Gold;
        check(model.TryGrantDebugItems(" r001 ",null,21,out stored,out delivered,out error),error);
        check(stored==12&&delivered==9&&model.GetContainer("warehouse").items.Count==12&&model.TemporaryItemCount==9,"상자 채우기/초과 택배 즉시 지급");
        check(model.Gold==gold&&model.Snapshot().deliveries.Count==0,"무료 지급/배송 예약 없음");
        var ids=new HashSet<string>();foreach(var box in model.Snapshot().containers)foreach(var item in box.items)check(ids.Add(item.instanceId),"중복 UID");
        check(ids.Count==21,"낱개21개 보존");
        long revision=model.Revision;string before=JsonUtility.ToJson(model.Snapshot());
        check(!model.TryGrantDebugItems("missing",null,1,out stored,out delivered,out error),"없는 ID 거절");
        check(!model.TryGrantDebugItems("R001",null,0,out stored,out delivered,out error),"0개 거절");
        check(!model.TryGrantDebugItems("R001",null,101,out stored,out delivered,out error),"101개 거절");
        check(!model.TryGrantDebugItems("E017","legendary",1,out stored,out delivered,out error),"품질 없는 물품에 전설 품질 거절");
        check(model.Revision==revision&&JsonUtility.ToJson(model.Snapshot())==before,"잘못된 요청은 재고/돈/배송 변경 없음");
        check(model.TryGrantDebugItems("E017",null,1,out stored,out delivered,out error),error);
        var eventItem=model.GetContainer(InventoryModel.TemporaryStorageId).items.Find(item=>item.itemId=="E017");int price;
        check(eventItem!=null&&eventItem.quality=="none"&&!model.TryGetOfferPrice(InventoryModel.TemporaryStorageId,eventItem.instanceId,out price),"테스트 이벤트 지급/판매 금지 유지");
        check(model.TryGrantDebugItems("W012","legendary",1,out stored,out delivered,out error),error);
        check(model.GetContainer(InventoryModel.TemporaryStorageId).items.Exists(item=>item.itemId=="W012"&&item.quality=="legendary"),"전설 품질 선택 지급");
        var snapshot=model.Snapshot();var restored=ScheduleModel();check(restored.TryRestore(snapshot,out error),error);
        check(JsonUtility.ToJson(restored.Snapshot())==JsonUtility.ToJson(snapshot),"지급 물품 저장 복원");
        Debug.Log("ID 지급/수량/품질/상자 초과 택배/실패 원자성/저장 모델 검사 완료");
    }
    static InventoryModel ScheduleModel()
    {
        var items=new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(Data("items").text));
        var workshops=new WorkshopCatalog(JsonUtility.FromJson<WorkshopDatabase>(Data("workshops").text),items);
        var save=new InventorySave{gold=10000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});
        return new InventoryModel(items,save,null,null,workshops);
    }
    static string ScheduleJob(InventoryModel model,string station,string itemId)
    {
        string error,id;ScheduleCheck(model.TryUnlockWorkshop(station,out error),error);
        var recipe=model.Workshops.Recipe("recipe_"+itemId);
        ScheduleCheck(model.TryCreateWorkshopJob(station,recipe.id,1,out id,out error),error);int slot=0;
        foreach(var ingredient in recipe.ingredients)
        {
            ScheduleCheck(model.TryAdd("warehouse",ingredient.itemId,"common",ingredient.count,7,out error),error);
            for(int n=0;n<ingredient.count;n++)
            {
                var record=model.GetContainer("warehouse").items.Find(item=>item.itemId==ingredient.itemId);
                ScheduleCheck(model.TryPutWorkshopMaterial(station,id,slot++,"warehouse",record.instanceId,model.Revision,out error),error);
            }
        }
        ScheduleCheck(model.TryStartWorkshopJob(station,id,out error),error);return id;
    }
    static void CheckWorkingSchedule()
    {
        string error;var model=ScheduleModel();string id=ScheduleJob(model,"processing","M002");
        model.AdvanceWorkshopsDuring(300,10,ShopPhase.Day,1);
        ScheduleCheck(model.GetWorkshop("processing").jobs[0].elapsed==10,"게임 분과 무관하게 실제10초 진행");
        model.AdvanceWorkshopsDuring(360,360,ShopPhase.Night,1);model.AdvanceWorkshopsDuring(180,180,ShopPhase.Dawn,1);
        ScheduleCheck(model.GetWorkshop("processing").jobs[0].elapsed==10,"밤/새벽 진행 보존");
        ScheduleCheck(!model.TryPayNightWork("processing",1,ShopPhase.Dawn,out error),"새벽 결제 차단");
        ScheduleCheck(model.TryPayNightWork("processing",1,ShopPhase.Night,out error)&&model.GetWorkshop("processing").extraDue==20,"일급40의50% 추가");
        ScheduleCheck(!model.TryPayNightWork("processing",1,ShopPhase.Night,out error)&&model.GetWorkshop("processing").extraDue==20,"중복 수당 차단");
        model.AdvanceWorkshopsDuring(1000,10,ShopPhase.Night,1);
        var snapshot=JsonUtility.FromJson<InventorySave>(JsonUtility.ToJson(model.Snapshot()));var restored=ScheduleModel();
        ScheduleCheck(restored.TryRestore(snapshot,out error),error);
        ScheduleCheck(restored.GetWorkshop("processing").nightWorkDay==1&&restored.GetWorkshop("processing").jobs[0].elapsed==20,"야간 권리/진행 저장 보존");
        restored.AdvanceWorkshopsDuring(10,10,ShopPhase.Night,1);
        var finished=restored.GetWorkshop("processing").jobs[0];ScheduleCheck(finished.completed==1&&finished.results.Count==1&&finished.inputs.Count==0,"정확히30실제초 완성/재료1회 소비");
        DawnSettlement settlement;ScheduleCheck(restored.TryBeginDawn(2,0,0,out settlement,out error)&&settlement.workshopCost==60,"일급40+야간20 새벽 정산");
        restored.SetWorkshopTime(ShopPhase.Dawn,2);ScheduleCheck(!restored.WorkshopCanOperate("processing"),"새벽은 유료여도 휴무");
        restored.SetWorkshopTime(ShopPhase.Night,2);ScheduleCheck(!restored.WorkshopCanOperate("processing"),"다음 밤에 권리 재사용 차단");
        // A 24-minute armor needs three daytime windows, or two windows with one paid night.
        model=ScheduleModel();id=ScheduleJob(model,"smith","W022");
        model.AdvanceWorkshopsDuring(540,540,ShopPhase.Day,1);model.AdvanceWorkshopsDuring(360,360,ShopPhase.Night,1);model.AdvanceWorkshopsDuring(180,180,ShopPhase.Dawn,1);
        ScheduleCheck(model.GetWorkshop("smith").jobs[0].elapsed==540,"장기 제작 첫 낮 보존");
        ScheduleCheck(model.TryBeginDawn(2,0,0,out settlement,out error),error);model.AdvanceWorkshopsDuring(540,540,ShopPhase.Day,2);
        ScheduleCheck(model.GetWorkshop("smith").jobs[0].elapsed==1080,"장기 제작 둘째 낮 이어가기");
        ScheduleCheck(model.TryBeginDawn(3,0,0,out settlement,out error),error);model.AdvanceWorkshopsDuring(360,360,ShopPhase.Day,3);
        ScheduleCheck(model.GetWorkshop("smith").jobs[0].completed==1,"세 번째 낮에24작업분 완성");
        model=ScheduleModel();id=ScheduleJob(model,"smith","W022");model.AdvanceWorkshopsDuring(540,540,ShopPhase.Day,1);
        ScheduleCheck(model.TryPayNightWork("smith",1,ShopPhase.Night,out error)&&model.GetWorkshop("smith").extraDue==40,"대장간 야간40");
        model.AdvanceWorkshopsDuring(360,360,ShopPhase.Night,1);model.AdvanceWorkshopsDuring(180,180,ShopPhase.Dawn,1);
        ScheduleCheck(model.GetWorkshop("smith").jobs[0].elapsed==900,"야간360추가/새벽0");
        ScheduleCheck(model.TryBeginDawn(2,0,0,out settlement,out error)&&settlement.workshopCost==120,"대장간 일급80+수당40");
        model.AdvanceWorkshopsDuring(540,540,ShopPhase.Day,2);ScheduleCheck(model.GetWorkshop("smith").jobs[0].completed==1,"유료 야간 이용 시 실제27분 완성");
        var clock=new ShopClock(new ShopClockSettings(),new ShopClockState());
        clock.Advance(540);ScheduleCheck(clock.Phase==ShopPhase.Night,"9분 후 밤");clock.Advance(360);ScheduleCheck(clock.Phase==ShopPhase.Dawn&&clock.Day==2,"6분 후 다음 새벽");clock.Advance(180);ScheduleCheck(clock.Phase==ShopPhase.Day,"3분 후 낮");
        var remapped=clock.RemapSavedTime(new ShopClockState{day=2,phase=ShopPhase.Night,elapsedMinutes=600},new ShopClockSettings{nightMinutes=720});
        ScheduleCheck(remapped.elapsedMinutes==300&&remapped.day==2&&remapped.phase==ShopPhase.Night,"예전 밤 저장 비율 유지");
        Debug.Log("공방 실제초/휴무/야간 수당/저장/장기 제작/18분 주기 모델 검사 완료");
    }

}
#endif
