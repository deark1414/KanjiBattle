using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleDeploymentView : IDisposable
{
    private readonly GameObject root;
    private readonly TextMeshProUGUI selection;
    private readonly TextMeshProUGUI fusionSummary;
    private readonly Button confirmButton;
    private readonly Button fusionButton;
    private readonly Button playerSkillButton;
    private bool fusionReady = true, playerSkillReady = true;
    private readonly Dictionary<Vector2Int, Image> cells = new();
    public string FusionSummary => fusionSummary.text;

    public BattleDeploymentView(Transform parent, Transform[,] grid, Action<Vector2Int> selectCell, Action confirm, Action cancel, Action fusion = null, Action playerSkill = null)
    {
        root = new GameObject("DeploymentControls", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(parent, false);
        Place(root.GetComponent<RectTransform>(), new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.31f));
        root.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.09f, 1f);
        selection = Text(root.transform, "SelectedPiece", "布陣", new Vector2(0.04f, 0.74f), new Vector2(0.69f, 0.98f));
        fusionSummary = Text(root.transform, "FusionSummary", "融合なし", new Vector2(0.04f, 0.51f), new Vector2(0.96f, 0.72f));
        if (fusion != null)
        {
            fusionButton = Command(root.transform, "FusionSelection", "組合せ", new Vector2(0.72f, 0.75f), new Vector2(0.96f, 0.97f), fusion);
        }
        playerSkillButton = Command(root.transform, "PlayerSkillSelection", "プレイヤースキル：なし", new(.04f,.30f), new(.96f,.49f), () => playerSkill?.Invoke());
        confirmButton = Command(root.transform, "ConfirmDeployment", "開戦", new Vector2(0.52f, 0.03f), new Vector2(0.96f, 0.28f), confirm);
        Command(root.transform, "CancelDeployment", "編成に戻る", new Vector2(0.04f, 0.03f), new Vector2(0.48f, 0.28f), cancel);

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

    public void SetFusionState(string summary, bool ready, bool hasAlternatives)
    {
        fusionSummary.text = summary;
        fusionSummary.color = ready ? new Color(1f, 0.93f, 0.65f) : new Color(1f, 0.65f, 0.42f);
        fusionReady = ready;
        confirmButton.interactable = fusionReady && playerSkillReady;
        if (fusionButton != null) fusionButton.gameObject.SetActive(hasAlternatives);
    }

    public void SetPlayerSkillState(string summary, bool ready)
    {
        playerSkillButton.GetComponentInChildren<TextMeshProUGUI>().text = summary;
        playerSkillReady = ready; confirmButton.interactable = fusionReady && playerSkillReady;
    }

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

    private static Button Command(Transform parent, string name, string label, Vector2 min, Vector2 max, Action action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), min, max);
        var button = go.GetComponent<Button>();
        ModernWafuuPresentation.ApplyButtonTreatment(button);
        button.onClick.AddListener(() => action());
        Text(go.transform, "Label", label, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.94f));
        return button;
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
