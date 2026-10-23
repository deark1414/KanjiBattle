using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleFusionPreviewView : IDisposable
{
    private readonly RectTransform root;
    private readonly Transform[,] grid;
    public int LinkCount => root.Cast<Transform>().Count(child => child.gameObject.activeSelf && child.name == "FusionLink");

    public BattleFusionPreviewView(Transform parent, Transform[,] grid)
    {
        root = new GameObject("FusionPreview", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        this.grid = grid;
    }

    public void Draw(BattleFusionRules.Plan plan, IReadOnlyList<Vector2Int> positions, IReadOnlyList<BattleFusionRules.Group> conflicts)
    {
        foreach (Transform child in root)
        {
            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
        if (plan == null || positions == null) return;
        foreach (BattleFusionRules.Group group in plan.groups) DrawGroup(group, positions, new Color(1f, 0.84f, 0.26f), true);
        if (conflicts != null)
            foreach (BattleFusionRules.Group group in conflicts) DrawGroup(group, positions, new Color(1f, 0.43f, 0.22f), false);
        root.SetAsLastSibling();
    }

    private Vector2 Center(Vector2Int cell) => root.InverseTransformPoint(grid[cell.x, cell.y].position);

    private void DrawGroup(BattleFusionRules.Group group, IReadOnlyList<Vector2Int> positions, Color color, bool resolved)
    {
        foreach (int member in group.members)
        {
            Vector2Int cell = positions[member];
            var cellRect = grid[cell.x, cell.y].parent as RectTransform;
            Vector3[] corners = new Vector3[4]; cellRect.GetWorldCorners(corners);
            Vector2 size = (Vector2)(root.InverseTransformPoint(corners[2]) - root.InverseTransformPoint(corners[0]));
            Image ring = Image("FusionMaterial", Center(cell), size * 0.92f, new Color(color.r, color.g, color.b, 0.08f));
            var outline = ring.gameObject.AddComponent<Outline>(); outline.effectColor = color;
            float width = Mathf.Max(2f, size.x * (resolved && member == group.origin ? 0.045f : 0.02f));
            outline.effectDistance = new Vector2(width, -width);
        }
        for (int first = 0; first < group.members.Length; first++)
        for (int second = first + 1; second < group.members.Length; second++)
        {
            Vector2Int a = positions[group.members[first]], b = positions[group.members[second]];
            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) continue;
            Vector2 from = Center(a), to = Center(b), delta = to - from;
            Image line = Image("FusionLink", (from + to) / 2, new Vector2(delta.magnitude * 0.4f, Mathf.Max(3f, delta.magnitude * 0.06f)), color);
            line.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }

    private Image Image(string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(root, false); var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }

    public void Dispose()
    {
        if (root == null) return;
        root.gameObject.SetActive(false); UnityEngine.Object.Destroy(root.gameObject);
    }
}
