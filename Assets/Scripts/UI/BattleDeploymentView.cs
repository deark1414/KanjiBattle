using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleDeploymentView : IDisposable
{
    private readonly GameObject root;
    private readonly TextMeshProUGUI selection;
    private readonly Dictionary<Vector2Int, Image> cells = new();

    public BattleDeploymentView(Transform parent, Transform[,] grid, Action<Vector2Int> selectCell, Action confirm, Action cancel, Action fusion = null)
    {
        root = new GameObject("DeploymentControls", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(parent, false);
        Place(root.GetComponent<RectTransform>(), new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.31f));
        root.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.09f, 1f);
        selection = Text(root.transform, "SelectedPiece", "布陣", new Vector2(0.04f, 0.63f), new Vector2(0.96f, 0.96f));
        if (fusion != null)
        {
            Place(selection.rectTransform, new Vector2(0.04f, 0.63f), new Vector2(0.69f, 0.96f));
            Command(root.transform, "FusionSelection", "融合", new Vector2(0.72f, 0.65f), new Vector2(0.96f, 0.95f), fusion);
        }
        Command(root.transform, "ConfirmDeployment", "開戦", new Vector2(0.52f, 0.10f), new Vector2(0.96f, 0.55f), confirm);
        Command(root.transform, "CancelDeployment", "編成に戻る", new Vector2(0.04f, 0.10f), new Vector2(0.48f, 0.55f), cancel);

        for (int y = BattleDeploymentRules.Rows - BattleDeploymentRules.DeploymentRows; y < BattleDeploymentRules.Rows; y++)
        for (int x = 0; x < BattleDeploymentRules.Columns; x++)
        {
            var cell = new Vector2Int(x, y);
            var go = new GameObject("DeploymentTarget", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(grid[x, y].parent, false);
            Place(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.15f, 0.75f, 0.45f, 0.12f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => selectCell(cell));
            cells.Add(cell, image);
        }
    }

    public void Refresh(BattleCharacter selected)
    {
        selection.text = selected != null ? $"布陣　{selected.data.characterName}" : "布陣";
        foreach (var pair in cells)
            pair.Value.color = selected != null && selected.gridPos == pair.Key
                ? new Color(1f, 0.82f, 0.25f, 0.45f)
                : new Color(0.15f, 0.75f, 0.45f, 0.12f);
        BringToFront();
    }

    public void BringToFront() => root.transform.SetAsLastSibling();

    public void Dispose()
    {
        foreach (Image cell in cells.Values)
        {
            if (cell == null) continue;
            cell.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(cell.gameObject);
        }
        cells.Clear();
        if (root == null) return;
        root.SetActive(false);
        UnityEngine.Object.Destroy(root);
    }

    private static void Command(Transform parent, string name, string label, Vector2 min, Vector2 max, Action action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), min, max);
        var button = go.GetComponent<Button>();
        ModernWafuuPresentation.ApplyButtonTreatment(button);
        button.onClick.AddListener(() => action());
        Text(go.transform, "Label", label, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f));
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string value, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.text = value;
        text.color = new Color(1f, 0.96f, 0.82f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = 40f;
        text.raycastTarget = false;
        Place(text.rectTransform, min, max);
        return text;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
