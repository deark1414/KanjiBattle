using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleFusionPreviewView : IDisposable
{
    private readonly RectTransform root;
    private readonly Transform[,] grid;
    private readonly List<GameObject> materialFrames = new();
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
        ClearMaterialFrames();
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
            var frame = new GameObject("FusionMaterial", typeof(RectTransform));
            frame.transform.SetParent(grid[cell.x, cell.y].parent, false);
            materialFrames.Add(frame);
            var rect = (RectTransform)frame.transform;
            rect.anchorMin = new(.04f,.04f); rect.anchorMax = new(.96f,.96f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            float width = (resolved && member == group.origin ? 3f : 2f) / UiReadability.Scale(root.GetComponentInParent<Canvas>());
            Edge(rect,"Top",new(0,1),Vector2.one,new(0,-width),Vector2.zero,color);
            Edge(rect,"Bottom",Vector2.zero,new(1,0),Vector2.zero,new(0,width),color);
            Edge(rect,"Left",Vector2.zero,new(0,1),Vector2.zero,new(width,0),color);
            Edge(rect,"Right",new(1,0),Vector2.one,new(-width,0),Vector2.zero,color);
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

    private static void Edge(RectTransform parent,string name,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax,Color color)
    {
        var go=new GameObject("FusionFrame"+name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=offsetMin;rect.offsetMax=offsetMax;
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;
    }

    private void ClearMaterialFrames()
    {
        foreach(var frame in materialFrames)
            if(frame!=null){frame.SetActive(false);UnityEngine.Object.Destroy(frame);}
        materialFrames.Clear();
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
        ClearMaterialFrames();
        if (root == null) return;
        root.gameObject.SetActive(false); UnityEngine.Object.Destroy(root.gameObject);
    }
}
