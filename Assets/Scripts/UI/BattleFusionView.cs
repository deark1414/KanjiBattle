using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleFusionView : IDisposable
{
    private readonly GameObject root;
    private readonly List<Action> refreshers = new();

    public BattleFusionView(Transform parent, IReadOnlyList<CharacterData> party, BattleFusionRules.Plan plan, Action close)
    {
        root = Panel(parent, "FusionSelection", new Color(0.06f, 0.08f, 0.07f, 0.99f), new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.94f));
        Label(root.transform, "融合", new Vector2(0.05f, 0.89f), new Vector2(0.95f, 0.98f));
        var viewport = Panel(root.transform, "Recipes", new Color(0.11f, 0.13f, 0.11f), new Vector2(0.03f, 0.20f), new Vector2(0.97f, 0.88f));
        viewport.AddComponent<RectMask2D>();
        var scroll = viewport.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var rect = content.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8; layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlHeight = true; layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = rect;
        List<BattleFusionRules.Group> candidates = BattleFusionRules.Candidates(party);
        if (candidates.Count == 0) Label(viewport.transform, "融合できる組み合わせなし", Vector2.zero, Vector2.one);
        foreach (BattleFusionRules.Group candidate in candidates)
        {
            var row = Panel(content.transform, "Recipe", new Color(0.17f, 0.20f, 0.17f), Vector2.zero, Vector2.one);
            row.AddComponent<LayoutElement>().preferredHeight = 104;
            var toggle = row.AddComponent<Toggle>();
            var box = Panel(row.transform, "Toggle", new Color(0.35f, 0.4f, 0.35f), new Vector2(0.02f, 0.34f), new Vector2(0.08f, 0.66f));
            var check = Panel(box.transform, "Checked", new Color(0.6f, 0.85f, 0.4f), new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
            toggle.targetGraphic = box.GetComponent<Image>(); toggle.graphic = check.GetComponent<Image>();
            string members = string.Join("＋", candidate.members.Select(index => $"{party[index].characterName}[{index + 1}]"));
            var recipe = BattleFusionRules.GetRecipe(candidate.recipeId);
            var label = Label(row.transform, "", new Vector2(0.10f, 0.06f), new Vector2(0.70f, 0.94f));
            var origin = Button(row.transform, "", new Vector2(0.72f, 0.12f), new Vector2(0.98f, 0.88f), () =>
            {
                BattleFusionRules.Group selected = Find(plan, candidate);
                if (selected == null) return;
                int offset = Array.IndexOf(selected.members, selected.origin);
                selected.origin = selected.members[(offset + 1) % selected.members.Length];
                Refresh();
            });
            toggle.onValueChanged.AddListener(enabled =>
            {
                if (enabled && Find(plan, candidate) == null)
                    plan.groups.Add(new BattleFusionRules.Group { recipeId = candidate.recipeId, members = (int[])candidate.members.Clone(), origin = candidate.origin });
                if (!enabled) plan.groups.RemoveAll(group => Same(group, candidate));
                Refresh();
            });
            refreshers.Add(() =>
            {
                BattleFusionRules.Group selected = Find(plan, candidate);
                bool conflict = selected == null && plan.groups.Any(group => group.members.Intersect(candidate.members).Any());
                toggle.SetIsOnWithoutNotify(selected != null);
                toggle.interactable = !conflict;
                label.text = $"{members} → {(BattleFusionRules.IsDiscovered(recipe.Id) ? recipe.Name : "？")}\n{candidate.members.Length}体 → 1体";
                label.alpha = conflict ? 0.4f : 1f;
                origin.interactable = selected != null;
                origin.GetComponentInChildren<TextMeshProUGUI>().text = selected == null ? "―" : $"位置\n{party[selected.origin].characterName}[{selected.origin + 1}]";
            });
        }
        Button(root.transform, "全解除", new Vector2(0.04f, 0.04f), new Vector2(0.46f, 0.16f), () => { plan.groups.Clear(); Refresh(); });
        Button(root.transform, "布陣に戻る", new Vector2(0.52f, 0.04f), new Vector2(0.96f, 0.16f), close);
        Refresh();
    }

    private void Refresh() { foreach (Action action in refreshers) action(); }
    public void BringToFront() => root.transform.SetAsLastSibling();
    private static bool Same(BattleFusionRules.Group a, BattleFusionRules.Group b) => a.recipeId == b.recipeId
        && a.members.OrderBy(index => index).SequenceEqual(b.members.OrderBy(index => index));
    private static BattleFusionRules.Group Find(BattleFusionRules.Plan plan, BattleFusionRules.Group candidate) => plan.groups.FirstOrDefault(group => Same(group, candidate));
    public void Dispose() { if (root == null) return; root.SetActive(false); UnityEngine.Object.Destroy(root); }

    private static GameObject Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color;
        Place(go.GetComponent<RectTransform>(), min, max); return go;
    }
    private static Button Button(Transform parent, string value, Vector2 min, Vector2 max, Action action)
    {
        var go = Panel(parent, "Command", Color.white, min, max);
        var button = go.AddComponent<Button>(); ModernWafuuPresentation.ApplyButtonTreatment(button);
        button.onClick.AddListener(() => action()); Label(go.transform, value, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f)); return button;
    }
    private static TextMeshProUGUI Label(Transform parent, string value, Vector2 min, Vector2 max)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false); var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text); text.text = value; text.color = new Color(0.98f, 0.95f, 0.85f);
        text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 30; text.raycastTarget = false;
        Place(text.rectTransform, min, max); return text;
    }
    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
