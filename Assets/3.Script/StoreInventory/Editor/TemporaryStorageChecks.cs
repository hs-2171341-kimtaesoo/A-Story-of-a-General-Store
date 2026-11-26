#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using GeneralStore.Inventory;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class TemporaryStorageChecks
{
    static int checks;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
    static void Call(object target,string method){target.GetType().GetMethod(method,Private).Invoke(target,null);}
    static ItemDefinition Definition(string id,int width,int height,bool rotate)
    {return new ItemDefinition{id=id,type="material",width=width,height=height,canRotate=rotate,basePrice=10,qualities=new[]{new QualityDefinition{quality="common",name=id,priceMultiplier=1},new QualityDefinition{quality="superior",name=id,priceMultiplier=1.5f}}};}
    static ItemCatalog Catalog()
    {return new ItemCatalog(new ItemDatabase{items=new[]{Definition("single",1,1,true),Definition("wide",9,1,false),Definition("full",8,7,false),Definition("rotor",2,1,true)}});}
    static ItemRecord Item(string uid,string id,int x,int y,string quality="common")
    {return new ItemRecord{instanceId=uid,itemId=id,x=x,y=y,quality=quality,purchasePrice=17};}
    static void Fill(ContainerRecord box,int count)
    {for(int n=0;n<count;n++)box.items.Add(Item(box.containerId+"-"+n,"single",n%8,n/8));}
    static InventorySave Save(int fill=0)
    {
        var save=new InventorySave{gold=12345,unpaidCosts=87};var box=new ContainerRecord{containerId="warehouse",columns=8,rows=7};Fill(box,fill);save.containers.Add(box);
        save.containers.Add(new ContainerRecord{containerId=InventoryModel.TemporaryStorageId,columns=16,rows=5});return save;
    }
    public static int Run()
    {checks=0;Recovery();ArtworkAndEmptyGate();return checks;}
    static void Recovery()
    {
        string error;TemporaryRecoveryResult result;var save=Save(54);var temp=save.containers[1];
        // Deliberately shuffled source list; the visible y/x order must win.
        temp.items.Add(Item("tail","single",10,0));temp.items.Add(Item("blocked","wide",1,0));temp.items.Add(Item("head","single",0,0,"superior"));
        var model=new InventoryModel(Catalog(),save);long revision=model.Revision;int changed=0;model.Changed+=()=>changed++;
        Check(model.GetContainer(InventoryModel.TemporaryStorageId).rows==8&&save.containers[1].rows==5,"128칸 표시용 최소8행 복원/원본 저장 불변 실패");
        Check(model.TryRecoverTemporary(revision,out result,out error)&&result.recovered==1&&result.remaining==2&&result.stoppedAtInstanceId=="blocked","표시 순서에 따른 접두 구간 회수/첫 실패 중지 오류");
        Check(model.GetItem("warehouse","head")!=null&&model.GetItem("warehouse","tail")==null&&model.GetContainer("warehouse").items.Count==55,"들어갈 수 없는 아이템 뒤의 작은 물건을 건너뛰어 회수했습니다.");
        Check(model.GetItem("warehouse","head").quality=="superior"&&model.GetItem("warehouse","head").purchasePrice==17,"회수 시 UID/품질/매입가 보존 실패");
        Check(model.GetItem(InventoryModel.TemporaryStorageId,"tail").x==10&&model.Gold==12345&&model.UnpaidCosts==87&&model.Revision==revision+1&&changed==1,"부분 회수의 나머지 배치/돈/미납/단일 갱신 보존 실패");
        revision=model.Revision;
        Check(!model.TryRecoverTemporary(revision,out result,out error)&&result.recovered==0&&result.remaining==2&&model.Revision==revision&&changed==1,"맨 앞 물건이 들어가지 않는데 재고를 변경했습니다.");
        Check(!model.TryRecoverTemporary(revision-1,out result,out error)&&model.Revision==revision,"오래된 회수 요청 허용");
        DawnSettlement bill;Check(model.TryBeginDawn(2,0,10,out bill,out error)&&bill.storageCost==100&&bill.occupiedCells==10,"회수 후 남은 임시 물건만 기존 새벽 보관료에 반영되지 않습니다.");

        save=Save();save.containers[0].items.Add(Item("full-box","full",0,0));
        var second=new ContainerRecord{containerId="warehouse_2",columns=8,rows=7};Fill(second,54);save.containers.Insert(1,second);
        temp=save.containers[2];temp.items.Add(Item("second","single",1,0));temp.items.Add(Item("first","single",0,0));
        model=new InventoryModel(Catalog(),save);revision=model.Revision;
        Check(model.TryRecoverTemporary(revision,out result,out error)&&result.recovered==2&&result.remaining==0,"첫 상자가 가득 찼을 때 다음 상자 이용 실패");
        Check(model.GetItem("warehouse_2","first")!=null&&model.GetItem("warehouse_2","second")!=null&&model.GetContainer("warehouse").items.Count==1,"상자 순서/여러 상자 자동 배치 실패");
        Check(model.TemporaryItemCount==0&&model.TemporaryOccupiedCells==0&&model.GetContainer(InventoryModel.TemporaryStorageId).rows==8,"전부 회수한 뒤 0칸/8행 유지 실패");
        revision=model.Revision;Check(!model.TryRecoverTemporary(revision,out result,out error)&&model.Revision==revision,"빈 회수 버튼 중복 처리 실패");
        var restored=new InventoryModel(Catalog(),model.Snapshot());Check(restored.TemporaryItemCount==0&&restored.GetItem("warehouse_2","first")!=null,"회수 후 저장/불러오기 실패");

        save=Save();temp=save.containers[1];temp.items.Add(Item("locked","single",0,0));temp.items.Add(Item("after","single",1,0));model=new InventoryModel(Catalog(),save);
        var reserved=(HashSet<string>)typeof(InventoryModel).GetField("reserved",Private).GetValue(model);reserved.Add("locked");revision=model.Revision;
        Check(!model.TryRecoverTemporary(revision,out result,out error)&&result.stoppedAtInstanceId=="locked"&&model.TemporaryItemCount==2&&model.Revision==revision,"예약된 첫 물건을 건너뛰거나 이동했습니다.");
    }
    static void ArtworkAndEmptyGate()
    {
        var root=new GameObject("Temporary Storage Art Check",typeof(RectTransform),typeof(Canvas));
        try
        {
            var runtime=root.AddComponent<InventoryRuntime>();runtime.initialWarehouseCount=2;runtime.initialGold=10000;ItemTableTools.Configure(runtime);
            string error;if(!runtime.TryInitialize(out error))throw new InvalidOperationException(error);
            var panel=root.AddComponent<InventoryPanel>();panel.inventory=runtime;panel.canvas=root.GetComponent<Canvas>();panel.grids=new InventoryGridView[0];panel.detailViews=new InventoryDetailsView[0];panel.shop=root.AddComponent<ShopfrontUI>();panel.drag=root.AddComponent<InventoryDragHandler>();panel.messageLabel=InventorySceneBuilder.Label("Message",root.transform,0,0,500,40,"",18);
            var ui=root.AddComponent<DeliveryUI>();ui.panel=panel;
            var template=InventorySceneBuilder.Box("Item Template",root.transform,0,0,78,78,Color.white).gameObject.AddComponent<InventoryItemView>();
            template.icon=InventorySceneBuilder.Rect("Icon",template.transform,0,0,78,78).gameObject.AddComponent<InventoryIconGraphic>();template.nameLabel=InventorySceneBuilder.Label("Name",template.transform,0,0,78,40,"",14);template.gameObject.SetActive(false);
            TemporaryStorageArtBuilder.BuildWindow(root.transform,ui,template);
            ui.doorButton=InventorySceneBuilder.ButtonBox("Door",root.transform,0,0,40,40,null);ui.badge=InventorySceneBuilder.Label("Badge",root.transform,0,0,100,30,"",14);
            var menu=root.AddComponent<RightSideMenu>();menu.panel=panel;ui.managementMenu=menu;
            var entry=new ShopMenuEntry{id="temporary_storage",window=ui.window.gameObject,visible=false};entry.onOpen.AddListener(ui.OpenStorage);menu.entries.Add(entry);
            var keys=root.AddComponent<StoreHotkeys>();keys.panel=panel;keys.menu=menu;keys.drawer=root.AddComponent<WarehouseDrawer>();
            Call(ui,"Start");
            Check(ui.useStorageArtwork&&!ui.matchWarehouseCells&&Mathf.Abs(ui.scroll.viewport.rect.width/ui.grid.cellWidth-16)<.001f&&Mathf.Abs(ui.scroll.viewport.rect.height/ui.grid.cellHeight-8)<.001f,"원본 틀과 16×8 칸 정렬 오류");
            Check(!keys.HandleKey(KeyCode.S)&&!ui.window.gameObject.activeSelf,"빈 임시 저장고를 S로 열었습니다.");ui.OpenStorage();Check(!ui.window.gameObject.activeSelf&&!entry.visible,"빈 저장고 직접 열기/목록 표시 허용");
            var source=runtime.Model.Snapshot();var warehouse=source.containers.Find(box=>box.containerId=="warehouse");var item=warehouse.items[0];warehouse.items.RemoveAt(0);item.x=item.y=0;
            source.containers.Find(box=>box.containerId==InventoryModel.TemporaryStorageId).items.Add(item);
            if(!runtime.Model.TryRestore(source,out error))throw new InvalidOperationException(error);Call(ui,"LateUpdate");
            Check(entry.visible,"비활성 창에서도 내용물에 따라 메뉴를 표시하지 못했습니다.");
            ui.OpenStorage();long cells=runtime.Model.TemporaryOccupiedCells;
            Check(ui.window.gameObject.activeSelf&&ui.recoverButton.interactable&&ui.recoverEnabledArtwork.gameObject.activeSelf&&!ui.recoverDisabledArtwork.gameObject.activeSelf,"회수 버튼 활성 상태 표시 오류");
            Check(ui.usedCellsLabel.text=="사용 중 "+cells+"칸"&&ui.feeLabel.text=="예정 보관료 "+(cells*10).ToString("N0")+" G","실제 사용 칸/새벽 예정 보관료 표시 오류");
            Check(ui.recoverButton.onClick.GetPersistentEventCount()==1&&ui.recoverButton.onClick.GetPersistentTarget(0)==ui,"회수 버튼 연결 누락/중복");
            Check(ui.gridArtwork.artwork==ui.window.GetComponent<global::UnityEngine.UI.Image>().sprite.texture,"격자에 다른 색의 생성 텍스처를 사용했습니다.");
            foreach(float offset in new[]{0f,ui.grid.cellHeight*.5f,ui.grid.cellHeight*8,ui.grid.cellHeight*9.5f})
            {
                ui.gridArtwork.SetScroll(offset,ui.grid.cellHeight);
                using(var mesh=new VertexHelper())
                {
                    typeof(TemporaryStorageGridGraphic).GetMethod("OnPopulateMesh",Private).Invoke(ui.gridArtwork,new object[]{mesh});
                    Check(mesh.currentVertCount>=4&&mesh.currentVertCount<=8,"스크롤 격자는 보이는 한두 장만 그려야 합니다.");
                    for(int n=0;n<mesh.currentVertCount;n++)
                    {var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,n);Check(vertex.uv0.x>=0&&vertex.uv0.x<=1&&vertex.uv0.y>=0&&vertex.uv0.y<=1,"반복 격자 UV 범위 오류");}
                }
            }
            long gold=runtime.Model.Gold,debt=runtime.Model.UnpaidCosts;ui.recoverButton.onClick.Invoke();
            Check(runtime.Model.TemporaryItemCount==0&&ui.window.gameObject.activeSelf&&!entry.visible,"전부 회수 후 빈 완료 화면/목록 숨김 실패");
            Check(ui.usedCellsLabel.text=="사용 중 0칸"&&ui.feeLabel.text=="예정 보관료 0 G"&&!ui.recoverButton.interactable&&!ui.recoverEnabledArtwork.gameObject.activeSelf&&ui.recoverDisabledArtwork.gameObject.activeSelf,"마지막 빈 상태 예시의 0칸/0G/비활성 회수 버튼 실패");
            ui.RecoverAll();Check(runtime.Model.Gold==gold&&runtime.Model.UnpaidCosts==debt,"회수 버튼에서 보관료를 즉시 부과하거나 두 번 이동했습니다.");
            ui.Close();var motion=ui.window.GetComponent<ManagementWindowMotion>();motion.Advance(1);Call(menu,"ContinueWindowSwitch");
            Check(!ui.window.gameObject.activeSelf&&!keys.HandleKey(KeyCode.S),"빈 완료 화면을 닫은 뒤 다시 열었습니다.");
            var oldWindow=ui.window;var oldGrid=ui.grid;var position=oldWindow.anchoredPosition;var otherPosition=panel.messageLabel.rectTransform.anchoredPosition;
            TemporaryStorageArtBuilder.UpgradeWindow(ui);
            Check(oldWindow==null&&ui.grid!=oldGrid&&entry.window==ui.window.gameObject&&Array.IndexOf(panel.grids,ui.grid)>=0,"기존 임시 저장고 교체 후 메뉴/격자 참조 복원 실패");
            Check((ui.window.anchoredPosition-position).sqrMagnitude<.01f&&(panel.messageLabel.rectTransform.anchoredPosition-otherPosition).sqrMagnitude<.01f,"임시 저장고 아트 교체가 기존 배치를 변경했습니다.");
        }
        finally{Object.DestroyImmediate(root);}
    }
}
#endif
