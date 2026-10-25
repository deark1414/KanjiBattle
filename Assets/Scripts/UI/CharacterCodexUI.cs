using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterCodexUI : MonoBehaviour
{
    public sealed class Item
    {
        public string title, status, detail;
        public Sprite icon;
        public int id;
        public bool fusion, enemy;
    }
    private static CharacterCodexUI active;
    private readonly List<Item> items = new();
    private RectTransform listContent, detailContent;
    private TextMeshProUGUI heading;
    private int category;
    private bool results;
    private Item selected;
    private Action closed;
    private float previousScale;
    private int previousWidth, previousHeight;

    private void OnDisable() { var callback = closed; closed = null; callback?.Invoke(); }

    private void LateUpdate()
    {
        float scale = GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        if (previousScale <= 0) previousScale = scale;
        if (Mathf.Approximately(previousScale, scale) && previousWidth == Screen.width && previousHeight == Screen.height) return;
        float ratio = previousScale / scale;
        foreach (var text in GetComponentsInChildren<TextMeshProUGUI>())
        { text.fontSize *= ratio; text.fontSizeMin *= ratio; text.fontSizeMax *= ratio; }
        previousScale = scale;
        previousWidth = Screen.width; previousHeight = Screen.height;
        ApplyLayout();
        Draw();
    }

    private void ApplyLayout()
    {
        bool compact = Screen.width > Screen.height && Screen.height < 500;
        Place(heading.rectTransform, new(.05f, compact ? .87f : .91f), new(.80f, .99f));
        var close = transform.Find("CloseCodex") as RectTransform;
        if (close != null) Place(close, new(.85f, compact ? .87f : .91f), new(.97f, .99f));
        string[] tabs = { "NormalTab", "FusionTab", "EnemyTab" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var tab = transform.Find(tabs[i]) as RectTransform;
            if (tab != null) Place(tab, new(.05f + i * .31f, compact ? .75f : .83f), new(.34f + i * .31f, compact ? .86f : .90f));
        }
        Place((RectTransform)detailContent.parent, new(.05f, .43f), new(.95f, compact ? .73f : .81f));
    }

    public static void CloseAny()
    {
        if (active == null) return;
        active.gameObject.SetActive(false); Destroy(active.gameObject); active = null;
    }
    public static List<Item> Catalog()
    {
        var list = new List<Item>();
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (db == null) return list;
        var candidates = new HashSet<int>(ResearchBondService.Instance.GetDefeatedCandidates().Select(data => data.characterId));
        foreach (var data in db.characters.Where(data => data != null).GroupBy(data => data.characterId).Select(group => group.First()))
        {
            bool acquired = PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(data);
            bool seen = acquired || CharacterRecords.HasEncountered(data.characterId) || candidates.Contains(data.characterId);
            var record = CharacterRecords.Get(data.characterId);
            list.Add(new Item { id = data.characterId, enemy = data.isBoss, title = seen ? data.characterName : "？",
                status = acquired ? "加入済" : seen ? "遭遇済" : "未遭遇", icon = acquired ? data.icon : null,
                detail = acquired ? SkillDescription.GetDetail(data) + $"\n\n勝利実績　{record.wins}勝\n" + record.Summary : seen ? data.isBoss ? "遭遇済" : "未加入" : "未遭遇" });
        }
        foreach (var recipe in BattleFusionRules.Recipes)
        {
            bool acquired = BattleFusionRules.IsDiscovered(recipe.Id);
            bool seen = acquired || CharacterRecords.HasEncountered(recipe.Id);
            string detail = seen ? "未発見" : "未遭遇";
            Sprite icon = null;
            if (acquired)
            {
                CharacterData[] materials = recipe.Materials.Select(db.GetByName).ToArray();
                if (materials.All(material => material != null))
                {
                    var data = CharacterData.CreateBattleFusion(recipe, materials, PlayerInventory.Instance?.PlayerLevel ?? 1);
                    detail = string.Join(" ＋ ", recipe.Materials) + "\n\n" + SkillDescription.GetDetail(data);
                    icon = data.icon;
                    if (Application.isPlaying) Destroy(data); else DestroyImmediate(data);
                }
            }
            list.Add(new Item { id = recipe.Id, fusion = true, title = seen ? recipe.Name : "？",
                status = acquired ? "発見済" : seen ? "遭遇済" : "未遭遇", detail = detail, icon = icon });
        }
        return list;
    }
    public static void OpenCodex(Transform source)
    {
        var window = Open(source, "漢字図鑑", true); if (window == null) return;
        window.items.AddRange(Catalog());
        Command(window.transform, "NormalTab", "通常", new(.05f, .83f), new(.34f, .90f), () => { window.category = 0; window.Draw(); });
        Command(window.transform, "FusionTab", "融合", new(.36f, .83f), new(.65f, .90f), () => { window.category = 1; window.Draw(); });
        Command(window.transform, "EnemyTab", "敵将", new(.67f, .83f), new(.95f, .90f), () => { window.category = 2; window.Draw(); });
        window.ApplyLayout();
        window.Draw();
    }
    public static void OpenResults(Transform source, BattleRecordBook book, Action closed = null)
    {
        var window = Open(source, "戦闘成績"); if (window == null) return;
        window.results = true;
        window.closed = closed;
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        foreach (var entry in book.Entries)
        {
            string materials = string.Join("＋", entry.owners.Select(id => db != null ? db.GetById(id)?.characterName ?? "？" : "？"));
            window.items.Add(new Item { title = entry.name, status = entry.result.Summary, icon = entry.icon,
                detail = (entry.owners.Length > 1 ? $"素材　{materials}\n\n" : "") + entry.result.Summary });
        }
        window.Draw();
    }
    private static CharacterCodexUI Open(Transform source, string title, bool screen = false)
    {
        CloseAny();
        Canvas canvas = source.GetComponentInParent<Canvas>()?.rootCanvas; if (canvas == null) return null;
        var root = Surface(screen ? source : canvas.transform, screen ? "CharacterCodexScreen" : "CharacterCodex",
            new(.055f, .075f, .07f, 1f), new(.025f, .11f), new(.975f, .88f));
        if (!screen)
        {
            var layer = root.AddComponent<Canvas>(); layer.overrideSorting = true; layer.sortingOrder = 1100;
            root.AddComponent<GraphicRaycaster>();
        }
        active = root.AddComponent<CharacterCodexUI>();
        active.heading = Text(root.transform, "Heading", title, new(.05f, .91f), new(.80f, .99f), 28);
        if (!screen) Command(root.transform, "CloseCodex", "×", new(.85f, .91f), new(.97f, .99f), CloseAny);
        active.detailContent = Scroll(root.transform, "Detail", new(.05f, .43f), new(.95f, .81f));
        active.listContent = Scroll(root.transform, "Entries", new(.05f, .03f), new(.95f, .41f));
        active.ApplyLayout();
        return active;
    }
    private void Draw()
    {
        Clear(listContent); Clear(detailContent);
        List<Item> visible = items.Where(item => results || (category == 2 ? item.enemy : category == 1 ? item.fusion : !item.fusion && !item.enemy))
            .OrderByDescending(item => item.status is "加入済" or "発見済").ToList();
        if (!results)
        {
            int count = visible.Count(item => category == 2 ? item.status != "未遭遇" : item.status is "加入済" or "発見済");
            heading.text = $"漢字図鑑　{count}/{visible.Count}";
            string[] tabs = { "NormalTab", "FusionTab", "EnemyTab" };
            for (int i = 0; i < tabs.Length; i++) transform.Find(tabs[i]).GetComponent<Image>().color =
                i == category ? new Color(.38f, .24f, .12f) : new Color(.18f, .24f, .21f);
        }
        for (int index = 0; index < visible.Count; index++)
        {
            Item item = visible[index];
            var row = Surface(listContent, "Entry_" + index, new(.12f, .15f, .13f), Vector2.zero, Vector2.one);
            row.AddComponent<LayoutElement>().preferredHeight = (results ? 116 : 76) / GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
            var button = row.AddComponent<Button>(); button.onClick.AddListener(() => Inspect(item));
            if (item.icon != null)
            {
                var icon = Surface(row.transform, "Piece", Color.white, new(.015f, .08f), new(.17f, .92f)).GetComponent<Image>();
                icon.sprite = item.icon; icon.preserveAspect = true; icon.raycastTarget = false;
            }
            Text(row.transform, "Name", item.title, new(.19f, .08f), new(results ? .38f : .47f, .92f), 24);
            Text(row.transform, "Status", item.status, new(results ? .40f : .48f, .05f), new(.98f, .95f), results ? 18 : 20);
        }
        if (!visible.Contains(selected)) { selected = visible.FirstOrDefault(); listContent.anchoredPosition = Vector2.zero; }
        if (selected != null) Inspect(selected);
    }
    private void Inspect(Item item)
    {
        selected = item;
        Clear(detailContent);
        detailContent.anchoredPosition = Vector2.zero;
        var text = Text(detailContent, "Description", item.title + "\n\n" + item.detail, Vector2.zero, Vector2.one, 22);
        text.enableAutoSizing = false; text.fontSize = 18 / GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.gameObject.AddComponent<LayoutElement>().minHeight = 140;
    }
    private static void Clear(Transform parent)
    { foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); } }
    private static RectTransform Scroll(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var view = Surface(parent, name, new(.08f, .10f, .09f), min, max); view.AddComponent<RectMask2D>();
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(view.transform, false);
        var rect = (RectTransform)content.transform; rect.anchorMin = new(0, 1); rect.anchorMax = Vector2.one;
        rect.pivot = new(.5f, 1); rect.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>(); layout.spacing = 3; layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = rect; scroll.viewport = (RectTransform)view.transform;
        var bar = Surface(view.transform, "Scrollbar", new(.16f, .20f, .17f), new(.98f, 0), Vector2.one);
        var handle = Surface(bar.transform, "Handle", new(.72f, .60f, .38f), Vector2.zero, Vector2.one);
        var scrollbar = bar.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = (RectTransform)handle.transform; scrollbar.targetGraphic = handle.GetComponent<Image>();
        scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return rect;
    }
    public static Button Command(Transform parent, string name, string label, Vector2 min, Vector2 max, Action action)
    {
        var go = Surface(parent, name, new(.23f, .29f, .25f), min, max);
        var button = go.AddComponent<Button>(); button.onClick.AddListener(() => action());
        Text(go.transform, "Label", label, new(.03f, .03f), new(.97f, .97f), 24); return button;
    }
    private static GameObject Surface(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color;
        Place((RectTransform)go.transform, min, max); return go;
    }
    private static TextMeshProUGUI Text(Transform parent, string name, string value, Vector2 min, Vector2 max, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false); var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text); text.text = value; text.color = new(.96f, .96f, .90f);
        float scale = text.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        text.alignment = TextAlignmentOptions.MidlineLeft; text.enableAutoSizing = true;
        text.fontSizeMin = 16 / scale; text.fontSizeMax = size / scale; text.raycastTarget = false;
        Place(text.rectTransform, min, max); return text;
    }
    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
