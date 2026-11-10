#if UNITY_EDITOR
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Play 전에 실행하면 실제 Canvas가 씬에 생성됩니다. 이후 Inspector에서 아트를 바꿀 수 있습니다.
public static class InventorySceneBuilder
{
    static Font font;
    static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
    }
    static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = rect.offsetMax = Vector2.zero; }
    static global::UnityEngine.UI.Image ImageBox(string name, Transform parent, float x, float y, float width, float height, Color color)
    { var image = Rect(name, parent, x, y, width, height).gameObject.AddComponent<global::UnityEngine.UI.Image>(); image.color = color; return image; }
    static Text Label(string name, Transform parent, float x, float y, float width, float height, string text, int size = 20)
    {
        Text label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
        label.font = font; label.fontSize = size; label.text = text; label.color = new Color(.97f, .9f, .75f);
        label.raycastTarget = false; label.supportRichText = false; return label;
    }
    static void Button(string text, Transform parent, float x, UnityEngine.Events.UnityAction action)
    {
        global::UnityEngine.UI.Image image = ImageBox(text, parent, x, 70, 150, 40, new Color(.4f, .25f, .1f));
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Label("Text", image.transform, 0, 0, 150, 40, text).alignment = TextAnchor.MiddleCenter;
        // 씬을 저장해도 버튼 연결이 남도록 persistent listener로 연결합니다.
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
    }
    static InventoryGridView Grid(Transform parent, string id, float x, float y, int columns, int rows, InventoryItemView template)
    {
        global::UnityEngine.UI.Image image = ImageBox(id, parent, x, y, columns * 52, rows * 52, new Color(.38f, .29f, .18f));
        InventoryGridView grid = image.gameObject.AddComponent<InventoryGridView>();
        grid.containerId = id; grid.cellSize = 52; grid.itemTemplate = template;
        grid.itemLayer = Rect("Items", grid.transform, 0, 0, columns * 52, rows * 52); Stretch(grid.itemLayer); return grid;
    }
    [MenuItem("Store Inventory/Create Canvas Demo")]
    public static void Create()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var data = new GameObject("Store Inventory Data"); Undo.RegisterCreatedObjectUndo(data, "Create Inventory Data");
        var runtime = data.AddComponent<InventoryRuntime>();
        var go = new GameObject("Store Inventory Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Inventory Canvas");
        Canvas canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
        InventoryPanel panel = go.AddComponent<InventoryPanel>(); panel.inventory = runtime; panel.canvas = canvas;
        global::UnityEngine.UI.Image background = ImageBox("Background", go.transform, 0, 0, 1440, 900, new Color(.09f, .075f, .055f)); Stretch(background.rectTransform);
        RectTransform layout = Rect("Layout", go.transform, 0, 0, 1440, 900);
        layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f); layout.anchoredPosition = Vector2.zero;
        panel.titleLabel = Label("Title", layout, 30, 15, 1300, 45, "보관함", 28);
        Button("저장", layout, 30, panel.SaveInventory); Button("불러오기", layout, 190, panel.LoadInventory);
        Label("Help", layout, 365, 70, 1000, 50, "드래그: 물건 1개 이동 | R: 회전 | Esc: 취소", 18);
        global::UnityEngine.UI.Image card = ImageBox("Item Template", layout, 0, 0, 104, 104, new Color(.35f, .23f, .12f, .95f));
        InventoryItemView template = card.gameObject.AddComponent<InventoryItemView>();
        RectTransform icon = Rect("Icon", card.transform, 0, 0, 80, 80);
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f); icon.anchoredPosition = Vector2.zero;
        template.icon = icon.gameObject.AddComponent<global::UnityEngine.UI.Image>(); template.icon.raycastTarget = false;
        template.nameLabel = Label("Name", card.transform, 4, 4, 96, 40, "", 15);
        RectTransform name = template.nameLabel.rectTransform; name.anchorMin = new Vector2(0, 1); name.anchorMax = Vector2.one;
        name.offsetMin = new Vector2(4, -40); name.offsetMax = new Vector2(-4, -4);
        card.gameObject.SetActive(false);
        Label("Warehouse Label", layout, 30, 120, 530, 30, "보관함");
        Label("Display Label", layout, 620, 120, 350, 30, "진열 연결 테스트 공간", 18);
        panel.grids = new[] { Grid(layout, "warehouse", 30, 160, 10, 8, template), Grid(layout, "display_preview", 620, 160, 6, 4, template) };
        global::UnityEngine.UI.Image recommendation = ImageBox("Recommendation Drop Area", layout, 620, 430, 312, 140, new Color(.23f, .32f, .25f));
        recommendation.gameObject.AddComponent<RecommendationDropArea>().panel = panel;
        Label("Hint", recommendation.transform, 10, 15, 292, 115, "추천 연결 영역\n물건 정보를 전달합니다.\n재고 차감/판매는 하지 않습니다.", 18);
        global::UnityEngine.UI.Image details = ImageBox("Details", layout, 990, 160, 410, 590, new Color(.16f, .12f, .08f));
        panel.detailNameLabel = Label("Name", details.transform, 15, 15, 380, 65, "물건을 선택하세요.", 24);
        panel.detailIcon = ImageBox("Icon", details.transform, 125, 80, 160, 160, Color.white);
        panel.detailIcon.raycastTarget = false; panel.detailIcon.enabled = false;
        panel.detailBodyLabel = Label("Body", details.transform, 15, 255, 380, 325, "", 20);
        panel.messageLabel = Label("Message", layout, 30, 770, 1360, 100, "", 20);
        RectTransform layer = Rect("Drag Layer", go.transform, 0, 0, 1440, 900); Stretch(layer);
        panel.drag = go.AddComponent<InventoryDragHandler>(); panel.drag.dragLayer = layer;
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem)); Undo.RegisterCreatedObjectUndo(es, "Create Inventory EventSystem");
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
        Selection.activeGameObject = go;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("생성 완료. InventoryPanel.Korean Font에 한글 폰트를 연결한 뒤 Play 하세요.");
    }
}

#endif
