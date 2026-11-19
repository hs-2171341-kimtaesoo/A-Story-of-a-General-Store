#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class WorkshopSceneBuilder
{
    static WorkshopSlotView Slot(Transform parent,string name,float x,bool result,int index)
    {
        float width=result?76:62;var box=UI.Box(name,parent,x,52,width,82,new Color(.13f,.10f,.07f));var slot=box.gameObject.AddComponent<WorkshopSlotView>();slot.background=box;slot.result=result;slot.slot=index;
        slot.icon=UI.Box("Sprite",box.transform,8,2,width-16,32,Color.white);slot.icon.preserveAspect=true;slot.icon.raycastTarget=false;
        slot.label=UI.Label("Item",box.transform,2,35,width-4,45,"",12);slot.label.alignment=TextAnchor.MiddleCenter;return slot;
    }
    public static void Build(RectTransform layout,InventoryPanel panel)
    {
        panel.shop.simulation.wages=0; // This demo bills hired workshop staff individually.
        var ui=layout.gameObject.AddComponent<WorkshopUI>();ui.panel=panel;ui.frame=SideMenuSceneBuilder.Panel(layout,"Workshop Window","제작 의뢰",out ui.window);
        var frame=ui.frame;
        UI.ButtonBox("가공소",frame,24,80,164,42,ui.Processing);UI.ButtonBox("연금술",frame,198,80,164,42,ui.Alchemy);UI.ButtonBox("대장간",frame,372,80,164,42,ui.Smith);
        ui.account=UI.Label("Wages And Unlock",frame,24,130,512,64,"",18);
        var hire=UI.ButtonBox("고용 시작",frame,24,200,146,40,ui.Hire);ui.hireLabel=hire.GetComponentInChildren<Text>();
        var warehouse=UI.ButtonBox("수령 상자",frame,180,200,230,40,ui.CycleWarehouse);ui.warehouseLabel=warehouse.GetComponentInChildren<Text>();
        UI.ButtonBox("열기",frame,420,200,116,40,ui.OpenMaterialBox);
        UI.ButtonBox("<",frame,24,250,42,70,ui.PreviousRecipe);ui.recipeLabel=UI.Label("Recipe And Ingredients",frame,74,250,414,70,"",18);UI.ButtonBox(">",frame,494,250,42,70,ui.NextRecipe);
        var quantity=UI.ButtonBox("제작 수량",frame,24,326,148,40,ui.CycleQuantity);ui.quantityLabel=quantity.GetComponentInChildren<Text>();
        ui.addButton=UI.ButtonBox("작업 추가",frame,182,326,174,40,ui.AddJob);UI.ButtonBox("닫기",frame,366,326,170,40,ui.Close);
        ui.probabilityLabel=UI.Label("Quality Probabilities",frame,24,374,512,68,"",16);
        ui.content=SideMenuSceneBuilder.List(frame,"Workshop Jobs",24,450,512,380);ui.rows=new WorkshopJobView[5];
        for(int i=0;i<ui.rows.Length;i++)
        {
            var card=UI.Box("Job "+(i+1),ui.content,0,i*232,508,224,new Color(.25f,.18f,.11f));var row=card.gameObject.AddComponent<WorkshopJobView>();ui.rows[i]=row;
            row.title=UI.Label("Title",card.transform,12,8,340,34,"",22);row.focus=card.gameObject.AddComponent<Button>();row.focus.targetGraphic=card;
            row.up=UI.ButtonBox("위",card.transform,370,8,56,34,null);row.down=UI.ButtonBox("아래",card.transform,434,8,62,34,null);
            row.inputs=new WorkshopSlotView[5];for(int s=0;s<5;s++)row.inputs[s]=Slot(card.transform,"Material "+(s+1),12+s*68,false,s);
            UI.Label("Arrow",card.transform,352,75,42,30,">",24);row.output=Slot(card.transform,"Result",408,true,0);
            var bar=UI.Box("Progress Track",card.transform,20,141,468,12,new Color(.1f,.09f,.06f));row.fill=UI.Box("Progress",bar.transform,0,0,0,12,new Color(.85f,.62f,.2f)).rectTransform;row.fill.GetComponent<global::UnityEngine.UI.Image>().raycastTarget=false;bar.raycastTarget=false;
            row.status=UI.Label("Status",card.transform,12,158,220,26,"",17);row.time=UI.Label("Remaining Work",card.transform,232,158,266,26,"",16);row.time.alignment=TextAnchor.MiddleRight;
            row.start=UI.ButtonBox("시작",card.transform,12,188,110,30,null);row.cancel=UI.ButtonBox("취소·회수",card.transform,130,188,138,30,null);row.instant=UI.ButtonBox("즉시 조제",card.transform,276,188,210,30,null);
        }
        var menu=layout.GetComponentInChildren<RightSideMenu>(true);var entry=new ShopMenuEntry{id="workshops",title="제작 의뢰",window=ui.window};UnityEditor.Events.UnityEventTools.AddPersistentListener(entry.onOpen,ui.Open);menu.AddEntry(entry);
        ui.window.SetActive(false);
        var debug=layout.Find("Debug Controls") as RectTransform;if(debug!=null)
        {
            int n=debug.GetComponentsInChildren<Button>(true).Length;UI.ButtonBox("공방 전체 해금",debug,14+(n%2)*208,14+(n/2)*84,194,68,ui.UnlockAllForDemo);debug.sizeDelta=new Vector2(430,Mathf.Max(debug.sizeDelta.y,28+(n/2+1)*84));
        }
    }
}
#endif
