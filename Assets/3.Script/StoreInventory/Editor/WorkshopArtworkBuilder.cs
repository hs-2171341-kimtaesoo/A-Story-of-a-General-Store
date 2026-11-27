#if UNITY_EDITOR
using GeneralStore.Inventory;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UI=InventorySceneBuilder;
using UIImage=UnityEngine.UI.Image;

internal static class WorkshopArtworkBuilder
{
    const string Folder="StoreInventory/WorkshopHud/";
    static readonly Color Ink=new Color(.20f,.12f,.06f);
    static readonly Color Cream=new Color(1f,.86f,.54f);
    static readonly string[] Assets={
        "shop_sign_blank","money_date_bar_blank","crafting_scroll_blank","night_shift_off",
        "panel_down","craft_disabled","craft_primary","tab_active","tab_inactive","title_leaf_left",
        "title_leaf_right","repeat_quantity_field_blank","repeat_mode_bar_blank","radio_off","radio_on",
        "quantity_minus","quantity_plus"
    };

    static Sprite SpriteFor(string key)
    {
        var sprite=Resources.Load<Sprite>(Folder+key);
        if(sprite==null)throw new System.InvalidOperationException("공방/HUD Sprite 누락: "+key);
        return sprite;
    }
    internal static void Prepare()
    {
        foreach(string key in Assets)
        {
            int limit=key=="crafting_scroll_blank"||key=="money_date_bar_blank"?1024:
                key.StartsWith("radio_")||key.StartsWith("title_leaf_")||key.StartsWith("quantity_")?128:512;
            SideMenuSceneBuilder.PreparePanelTexture(Folder+key,limit);
        }
    }

    static UIImage Art(string name,string key,Transform parent,float x,float y,float w,float h)
    {
        var image=UI.Box(name,parent,x,y,w,h,Color.white);
        image.sprite=SpriteFor(key);image.raycastTarget=false;
        return image;
    }
    static void Place(RectTransform rect,Transform parent,float x,float y,float w,float h)
    {
        rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
    }
    static void Style(Text label,int fontSize,Color color,TextAnchor align=TextAnchor.MiddleCenter)
    {
        label.fontSize=fontSize;label.fontStyle=FontStyle.Normal;
        label.color=color;label.alignment=align;label.resizeTextForBestFit=true;
        label.resizeTextMinSize=12;label.resizeTextMaxSize=fontSize;
    }
    static Text Label(string name,Transform parent,float x,float y,float w,float h,string text,int size,Color color)
    {
        var label=UI.Label(name,parent,x,y,w,h,text,size);
        Style(label,size,color);
        return label;
    }
    static Button ButtonArt(string name,string key,Transform parent,float x,float y,float w,float h,UnityAction click,string disabled=null)
    {
        var image=Art(name,key,parent,x,y,w,h);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();
        button.targetGraphic=image;
        if(disabled!=null)
        {
            var idle=image.sprite;
            button.transition=Selectable.Transition.SpriteSwap;
            button.spriteState=new SpriteState{highlightedSprite=idle,selectedSprite=idle,pressedSprite=idle,disabledSprite=SpriteFor(disabled)};
        }
        if(click!=null)UnityEventTools.AddPersistentListener(button.onClick,click);
        return button;
    }

    internal static void ApplyHud(InventoryPanel panel)
    {
        var canvas=(RectTransform)panel.canvas.transform;
        if(canvas.Find("Shop HUD Artwork")!=null)return;
        var hud=UI.Rect("Shop HUD Artwork",canvas,0,0,0,0);
        hud.anchorMin=Vector2.zero;hud.anchorMax=Vector2.one;
        hud.offsetMin=hud.offsetMax=Vector2.zero;
        UiPerformanceLayers.Ensure(hud.gameObject,false);

        var sign=Art("Shop Sign","shop_sign_blank",hud,20,0,332,332*371f/960);
        Label("Shop Name",sign.transform,130,32,187,64,"잡화점",43,Cream);
        var money=Art("Gold and Clock","money_date_bar_blank",hud,0,0,564,564*251f/2048);
        money.rectTransform.anchorMin=money.rectTransform.anchorMax=money.rectTransform.pivot=new Vector2(1,1);
        money.rectTransform.anchoredPosition=new Vector2(-24,-22);
        var gold=panel.shop.goldLabel;
        var clock=panel.shop.simulation.clockLabel;
        Place(gold.rectTransform,money.transform,92,0,155,69);
        Place(clock.rectTransform,money.transform,261,0,284,69);
        Style(gold,27,Cream);Style(clock,26,Cream);
        gold.text=panel.inventory.initialGold.ToString("N0")+" G";
        if(string.IsNullOrEmpty(clock.text))clock.text="1일 / 낮 09:00";
        var divider=UI.Box("Divider",money.transform,251,19,1,31,new Color(1f,.82f,.47f,.75f));
        divider.raycastTarget=false;
    }

    internal static void ApplyProcessing(WorkshopUI ui)
    {
        if(ui.processingView!=null)return;
        var frame=ui.frame;
        ui.legacyBackground=frame.GetComponent<UIImage>();
        ui.legacyPanelControls=new[]{ui.titleLabel.gameObject,ui.account.gameObject,ui.content.gameObject,
            ui.probabilityLabel.gameObject,ui.collectButton.gameObject,ui.nightButton.gameObject,frame.Find("닫기").gameObject};
        var temporaryButton=frame.Find("임시 저장고");
        if(temporaryButton!=null)Object.DestroyImmediate(temporaryButton.gameObject);

        var layout=UI.Rect("Processing Panel",frame,0,0,860,498);
        var view=layout.gameObject.AddComponent<ProcessingPanelView>();ui.processingView=view;
        var parchment=Art("Processing Parchment","crafting_scroll_blank",layout,0,17,860,860*880f/1680);
        parchment.raycastTarget=true;
        ButtonArt("Lower Workshop","panel_down",layout,368,-3,124,43,ui.Close);
        var night=ButtonArt("Night Work","night_shift_off",layout,65,53,180,39,ui.PayNightWork);
        view.night=night;
        var nightColors=night.colors;
        nightColors.disabledColor=new Color(.86f,.80f,.72f,1);
        night.colors=nightColors;
        view.nightCheck=Label("Paid Check",night.transform,146,6,18,26,"✓",18,Ink).gameObject;
        view.nightCheck.SetActive(false);
        view.nightFee=Label("Night Fee",layout,249,53,70,39,"",16,Ink);
        Label("Processing Title",layout,342,48,176,44,"가공소",34,Ink);
        Art("Left Leaves","title_leaf_left",layout,315,55,37,31);
        Art("Right Leaves","title_leaf_right",layout,508,55,37,31);

        view.job=BuildRecipe(layout);

        var countField=ui.repeatCountInput;
        var oldControls=ui.repeatControls;
        var controls=UI.Rect("Repeat Controls",layout,67,260,726,100);
        ui.repeatControls=controls.gameObject;
        Art("Repeat Mode Strip","repeat_mode_bar_blank",controls,0,0,726,44);
        view.radioOff=SpriteFor("radio_off");view.radioOn=SpriteFor("radio_on");
        view.repeatButtons=new Button[3];view.repeatIndicators=new UIImage[3];
        string[] modeNames={"1회","횟수 지정","소진까지"};
        for(int n=0;n<modeNames.Length;n++)
        {
            var hit=UI.Box(modeNames[n],controls,n*242,0,242,44,Color.clear);
            var toggle=hit.gameObject.AddComponent<Button>();toggle.targetGraphic=hit;toggle.transition=Selectable.Transition.None;
            UnityEventTools.AddIntPersistentListener(toggle.onClick,ui.SetRepeatMode,n);
            view.repeatButtons[n]=toggle;
            view.repeatIndicators[n]=Art("Radio",n==0?"radio_on":"radio_off",hit.transform,52,9,27,27);
            Label("Mode",hit.transform,82,0,109,44,modeNames[n],22,Ink).alignment=TextAnchor.MiddleLeft;
        }
        Label("Repeat Quantity Caption",controls,18,50,154,45,"반복 수량",22,Ink);
        Place((RectTransform)countField.transform,controls,381,51,144,43);
        var countArtwork=countField.GetComponent<UIImage>();
        countArtwork.sprite=SpriteFor("repeat_quantity_field_blank");countArtwork.color=Color.white;
        Place(countField.textComponent.rectTransform,countField.transform,11,0,122,43);
        Style(countField.textComponent,24,Ink);
        var countColors=countField.colors;countColors.disabledColor=Color.white;countField.colors=countColors;
        view.minus=ButtonArt("Fewer Repeats","quantity_minus",controls,337,51,43,43,null);
        view.plus=ButtonArt("More Repeats","quantity_plus",controls,526,51,43,43,null);
        UnityEventTools.AddIntPersistentListener(view.minus.onClick,ui.StepRepeatCount,-1);
        UnityEventTools.AddIntPersistentListener(view.plus.onClick,ui.StepRepeatCount,1);
        Label("Repeat Unit",controls,574,51,53,43,"회",21,Ink);
        Object.DestroyImmediate(oldControls);
        ui.repeatModeButton=null;ui.repeatLabel=null;

        view.collect=ButtonArt("Collect Products","craft_primary",layout,478,379,315,58,ui.CollectAll,"craft_disabled");
        view.collectLabel=Label("Collect Count",view.collect.transform,0,0,315,58,"완성품 받기 0",25,Cream);
        view.collect.interactable=false;
        BuildTabs(ui,view,layout);
        frame.sizeDelta=new Vector2(860,498);
        foreach(var control in ui.legacyPanelControls)control.SetActive(false);
        foreach(string name in new[]{"가공소","대장간","연금술공방"})frame.Find(name).gameObject.SetActive(false);
        ui.legacyBackground.enabled=false;
    }

    static void BuildTabs(WorkshopUI ui,ProcessingPanelView view,Transform parent)
    {
        var bar=UI.Rect("Workshop Tabs",parent,79,447,702,48);
        var group=bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.spacing=12;group.childAlignment=TextAnchor.MiddleCenter;
        group.childControlWidth=group.childControlHeight=false;
        group.childForceExpandWidth=group.childForceExpandHeight=false;
        string[] names={"가공소","대장간","연금술공방"};
        UnityAction[] handlers={ui.Processing,ui.Smith,ui.Alchemy};
        view.tabs=new Button[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            var button=ButtonArt(names[i],i==0?"tab_active":"tab_inactive",bar,0,0,208,46,handlers[i]);
            view.tabs[i]=button;
            Label("Text",button.transform,0,0,208,46,names[i],24,i==0?Ink:Cream);
        }
    }

    static WorkshopJobView BuildRecipe(Transform parent)
    {
        var recipe=UI.Rect("Processing Recipe",parent,67,104,726,147);
        var job=recipe.gameObject.AddComponent<WorkshopJobView>();job.progressArea=recipe;
        job.inputs=new WorkshopSlotView[5];
        for(int i=0;i<job.inputs.Length;i++)job.inputs[i]=Slot(recipe,"Ingredient "+(i+1),i*99,i,false);
        Label("Result Arrow",recipe,496,9,79,69,"→",39,new Color(.28f,.40f,.13f));
        job.output=Slot(recipe,"Result",626,0,true);
        job.candidate=ButtonArt("More Results","tab_inactive",recipe,554,86,172,30,null);
        job.candidateLabel=Label("Text",job.candidate.transform,0,0,172,30,"다른 결과 ›",17,Cream);
        job.candidate.gameObject.SetActive(false);
        job.time=Label("Remaining Time",recipe,407,115,309,28,"",18,Ink);
        job.time.alignment=TextAnchor.MiddleRight;
        job.status=Label("Work Status",recipe,10,115,386,28,"",18,Ink);
        job.status.alignment=TextAnchor.MiddleLeft;

        var line=UI.Box("Recipe Divider",recipe,0,145,726,1,new Color(.51f,.30f,.12f,.30f));line.raycastTarget=false;
        var fill=UI.Box("Recipe Progress",recipe,0,0,0,147,new Color(.76f,.42f,.13f,.23f));
        fill.raycastTarget=false;job.fill=fill.rectTransform;

        job.start=ButtonArt("Start Production","craft_primary",parent,67,379,393,58,null,"craft_disabled");
        job.actionLabel=Label("Text",job.start.transform,0,0,393,58,"제작",27,Cream);
        job.cancel=ButtonArt("Cancel and Recover","tab_inactive",parent,67,379,393,58,null);
        Label("Text",job.cancel.transform,0,0,393,58,"취소·회수",27,Cream);
        job.start.interactable=false;
        job.cancel.gameObject.SetActive(false);
        return job;
    }
    static WorkshopSlotView Slot(Transform recipe,string name,float x,int index,bool result)
    {
        var image=Art(name,"repeat_quantity_field_blank",recipe,x,0,result?100:88,84);
        image.raycastTarget=true;
        var slot=image.gameObject.AddComponent<WorkshopSlotView>();
        slot.background=image;slot.slot=index;slot.result=result;slot.artworkSlot=true;
        slot.icon=UI.IconSlot("Item",image.transform,8,7,result?84:72,70);
        if(!result)slot.label=Label("Empty Slot",image.transform,0,0,88,84,"+",30,new Color(.64f,.34f,.15f,.72f));
        return slot;
    }
}
#endif
