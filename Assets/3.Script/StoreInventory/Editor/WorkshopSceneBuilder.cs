#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class WorkshopSceneBuilder
{
    static Text Dark(string name,Transform parent,float x,float y,float width,float height,string value,int size)
    {var text=UI.Label(name,parent,x,y,width,height,value,size);text.color=new Color(.24f,.12f,.05f);return text;}
    static WorkshopSlotView Slot(Transform parent,string name,float x,bool result,int index)
    {
        var image=UI.Box(name,parent,x,32,66,72,new Color(.24f,.16f,.09f));var slot=image.gameObject.AddComponent<WorkshopSlotView>();slot.background=image;slot.result=result;slot.slot=index;
        slot.icon=UI.Box("Icon",image.transform,5,3,56,42,Color.white);slot.icon.raycastTarget=false;slot.icon.preserveAspect=true;
        slot.label=UI.Label("Name",image.transform,2,45,62,25,"",11);slot.label.alignment=TextAnchor.MiddleCenter;return slot;
    }
    public static void Build(RectTransform layout,InventoryPanel panel)
    {
        panel.shop.simulation.wages=0;var ui=layout.gameObject.AddComponent<WorkshopUI>();ui.panel=panel;
        ui.frame=SideMenuSceneBuilder.Panel(layout,"Workshop Window","",out ui.window);ui.frame.sizeDelta=new Vector2(860,664);ui.frame.GetComponent<global::UnityEngine.UI.Image>().color=new Color(.91f,.78f,.57f,.99f);
        var oldTitle=ui.frame.Find("Title");if(oldTitle!=null)Object.DestroyImmediate(oldTitle.gameObject);
        ui.titleLabel=Dark("Workshop Title",ui.frame,24,14,260,36,"가공소",29);
        UI.ButtonBox("임시 저장고",ui.frame,298,14,182,36,ui.OpenTemporaryStorage);
        var hire=UI.ButtonBox("고용 시작",ui.frame,492,14,152,36,ui.Hire);ui.hireLabel=hire.GetComponentInChildren<Text>(true);
        ui.nightButton=UI.ButtonBox("야간 작업",ui.frame,654,14,182,36,ui.PayNightWork);ui.nightLabel=ui.nightButton.GetComponentInChildren<Text>(true);ui.nightLabel.fontSize=16;
        ui.account=Dark("Daily Wages",ui.frame,24,56,812,28,"",17);
        ui.content=UI.Rect("Workshop Cards",ui.frame,24,92,812,432);ui.rows=new WorkshopJobView[3];
        for(int n=0;n<3;n++)
        {
            var card=UI.Box("Workshop Card "+n,ui.content,0,n*144,812,136,new Color(.98f,.87f,.67f));var row=card.gameObject.AddComponent<WorkshopJobView>();ui.rows[n]=row;
            row.title=Dark("Recipe Name",card.transform,10,2,330,28,"",18);row.status=Dark("Status",card.transform,376,2,350,28,"",17);
            row.inputs=new WorkshopSlotView[5];for(int s=0;s<5;s++)row.inputs[s]=Slot(card.transform,"Input "+s,10+s*72,false,s);
            Dark("Arrow",card.transform,373,50,30,34,"→",25);row.output=Slot(card.transform,"Result Preview",411,true,0);
            var track=UI.Box("Progress Track",card.transform,494,42,200,12,new Color(.32f,.22f,.12f));track.raycastTarget=false;
            row.fill=UI.Box("Progress",track.transform,0,0,0,12,new Color(.87f,.46f,.10f)).rectTransform;row.fill.GetComponent<global::UnityEngine.UI.Image>().raycastTarget=false;
            row.time=Dark("Remaining",card.transform,494,57,220,24,"",15);
            row.start=UI.ButtonBox("제작",card.transform,494,88,96,32,null);row.cancel=UI.ButtonBox("취소·회수",card.transform,598,88,116,32,null);
            row.up=UI.ButtonBox("▲",card.transform,752,8,46,44,null);row.down=UI.ButtonBox("▼",card.transform,752,60,46,44,null);
            row.candidate=UI.ButtonBox("결과 후보",card.transform,10,108,252,24,null);row.candidateLabel=row.candidate.GetComponentInChildren<Text>(true);row.candidateLabel.fontSize=13;
            row.instant=UI.ButtonBox("긴급 제작",card.transform,10,108,252,24,null);row.instant.GetComponentInChildren<Text>(true).fontSize=14;
            // Candidate choice and emergency action occupy different horizontal spaces.
            var urgent=(RectTransform)row.instant.transform;urgent.anchoredPosition=new Vector2(280,-108);urgent.sizeDelta=new Vector2(195,24);
            row.focus=card.gameObject.AddComponent<Button>();row.focus.targetGraphic=card;card.gameObject.SetActive(false);
        }
        var controls=UI.Rect("Processing Repeat",ui.frame,24,260,812,100);ui.repeatControls=controls.gameObject;
        var mode=UI.ButtonBox("1회",controls,0,0,270,40,ui.CycleRepeatMode);ui.repeatModeButton=mode;ui.repeatLabel=mode.GetComponentInChildren<Text>(true);
        Dark("Repeat Count Caption",controls,294,4,108,32,"반복 횟수",19);
        var countImage=UI.Box("Repeat Count",controls,412,0,110,40,new Color(.25f,.17f,.09f));ui.repeatCountInput=countImage.gameObject.AddComponent<InputField>();ui.repeatCountInput.targetGraphic=countImage;ui.repeatCountInput.contentType=InputField.ContentType.IntegerNumber;ui.repeatCountInput.characterLimit=4;
        ui.repeatCountInput.textComponent=UI.Label("Text",countImage.transform,8,5,94,30,"",20);ui.repeatCountInput.text="1";UnityEditor.Events.UnityEventTools.AddPersistentListener(ui.repeatCountInput.onEndEdit,ui.RepeatEdited);
        Dark("Repeat Source",controls,0,52,800,32,"자동 재료: 상자 우선 → 임시 저장고 · 진열장 제외",18);
        ui.probabilityLabel=Dark("Quality Chances",ui.frame,24,528,812,28,"",16);
        ui.collectButton=UI.ButtonBox("완성품 받기 0",ui.frame,24,564,530,42,ui.CollectAll);ui.collectLabel=ui.collectButton.GetComponentInChildren<Text>(true);
        UI.ButtonBox("닫기",ui.frame,572,564,264,42,ui.Close);
        UI.ButtonBox("가공소",ui.frame,24,616,260,38,ui.Processing);UI.ButtonBox("대장간",ui.frame,300,616,260,38,ui.Smith);UI.ButtonBox("연금술공방",ui.frame,576,616,260,38,ui.Alchemy);
        var menu=layout.GetComponentInChildren<RightSideMenu>(true);var entry=new ShopMenuEntry{id="workshops",title="제작 의뢰",window=ui.window};UnityEditor.Events.UnityEventTools.AddPersistentListener(entry.onOpen,ui.Open);menu.AddEntry(entry);ui.window.SetActive(false);
        var debug=layout.Find("Debug Controls") as RectTransform;if(debug!=null)
        {int count=debug.GetComponentsInChildren<Button>(true).Length;UI.ButtonBox("공방 전체 해금",debug,14+(count%2)*208,14+(count/2)*84,194,68,ui.UnlockAllForDemo);debug.sizeDelta=new Vector2(430,Mathf.Max(debug.sizeDelta.y,28+(count/2+1)*84));}
    }
    public static void Rebuild(InventoryPanel panel)
    {
        var root=panel.transform.root;var existing=root.GetComponentInChildren<WorkshopUI>(true);if(existing==null)return;
        var layout=(RectTransform)existing.transform;var menu=root.GetComponentInChildren<RightSideMenu>(true);menu.entries.RemoveAll(e=>e.id=="workshops");
        if(existing.window!=null)Object.DestroyImmediate(existing.window);Object.DestroyImmediate(existing);
        var debug=layout.Find("Debug Controls");if(debug!=null)
        {
            var grant=debug.Find("Item ID Grant");if(grant!=null)Object.DestroyImmediate(grant.gameObject);
            foreach(var button in debug.GetComponentsInChildren<Button>(true))if(button.name=="공방 전체 해금")Object.DestroyImmediate(button.gameObject);
        }
        Build(layout,panel);DrawerSceneBuilder.EnsureItemIdGrant(layout,panel.shop.simulation);
        var viewport=root.GetComponentInChildren<ShopViewportLayout>(true);if(viewport!=null)
        {
            var windows=new System.Collections.Generic.List<RectTransform>();var frames=new System.Collections.Generic.List<RectTransform>();
            foreach(var entry in menu.entries)if(entry.window!=null){var window=(RectTransform)entry.window.transform;window.SetParent(viewport.edgeLayer,false);window.anchoredPosition=Vector2.zero;var frame=window.Find("Panel Frame") as RectTransform;if(frame!=null){windows.Add(window);frames.Add(frame);}}
            viewport.managementWindows=windows.ToArray();viewport.managementFrames=frames.ToArray();viewport.Apply();
        }
    }
}
#endif
