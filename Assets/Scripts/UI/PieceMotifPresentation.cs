using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class PieceMotifCatalog
{
    public PieceMotifEntry[] entries;
}

[Serializable]
public sealed class PieceMotifEntry
{
    public int id;
    public string name;
    public string source;
    public string resource;
    public string piece;
    public string enemyPiece;
    public float u, v, width, height, aspect, scale, rotation;
}

public sealed class PieceMotifPresentation : MonoBehaviour
{
    private static readonly Dictionary<int, PieceMotifEntry> entries = new();
    private static readonly Dictionary<int, Texture2D> textures = new();
    private static bool catalogLoaded;
    private RawImage image;
    private RectTransform frame;
    private Coroutine arrival;
    private bool persistent;
    private Graphic pieceGraphic;
    private Color tint = Color.white;
    private float visibility;

    public bool HasMotif => image != null && image.texture != null;
    public bool IsVisible => frame != null && frame.gameObject.activeInHierarchy && visibility > 0.01f;
    public float VisibleAmount => frame != null && frame.gameObject.activeInHierarchy ? visibility : 0f;
    public int CharacterId { get; private set; }

    public static PieceMotifPresentation Attach(RectTransform piece, CharacterData character, bool visible = true)
    {
        if (piece == null) return null;
        var presentation = piece.GetComponent<PieceMotifPresentation>() ?? piece.gameObject.AddComponent<PieceMotifPresentation>();
        presentation.Configure(character, visible);
        return presentation;
    }

    private void Configure(CharacterData character, bool visible)
    {
        CharacterId = character == null ? 0 : character.characterId;
        CancelArrival();
        persistent = visible;
        pieceGraphic = GetComponent<Graphic>();
        tint = Color.white;
        visibility = 0f;
        if (pieceGraphic != null) pieceGraphic.canvasRenderer.SetAlpha(1f);
        if (!catalogLoaded)
        {
            catalogLoaded = true;
            var asset = Resources.Load<TextAsset>("UI/PieceMotifs/catalog");
            if (asset != null)
            {
                var catalog = JsonUtility.FromJson<PieceMotifCatalog>(asset.text);
                if (catalog?.entries != null)
                    foreach (var entry in catalog.entries) entries[entry.id] = entry;
            }
        }

        if (character == null || !entries.TryGetValue(CharacterArtCatalog.ArtId(character), out var motif))
        {
            if (frame != null) frame.gameObject.SetActive(false);
            return;
        }
        if (!textures.TryGetValue(motif.id, out var texture))
        {
            texture = Resources.Load<Texture2D>(motif.resource);
            textures[motif.id] = texture;
        }
        if (texture == null)
        {
            if (frame != null) frame.gameObject.SetActive(false);
            return;
        }

        if (frame == null)
        {
            var host = new GameObject("PieceMotif", typeof(RectTransform), typeof(LayoutElement));
            host.transform.SetParent(transform, false);
            host.GetComponent<LayoutElement>().ignoreLayout = true;
            frame = host.GetComponent<RectTransform>();
            var art = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
            art.transform.SetParent(frame, false);
            image = art.GetComponent<RawImage>();
            image.raycastTarget = false;
            var fitter = art.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        }
        image.texture = texture;
        frame.anchorMin = Vector2.one * (0.5f - motif.scale * 0.5f);
        frame.anchorMax = Vector2.one * (0.5f + motif.scale * 0.5f);
        frame.offsetMin = frame.offsetMax = Vector2.zero;
        image.uvRect = new Rect(motif.u, motif.v, motif.width, motif.height);
        image.GetComponent<AspectRatioFitter>().aspectRatio = motif.aspect;
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, motif.rotation);
        var edge = image.GetComponent<Shadow>() ?? image.gameObject.AddComponent<Shadow>();
        edge.enabled = motif.id == 10 || motif.id == 11;
        edge.effectColor = new Color(1f, 0.96f, 0.80f, 0.42f);
        edge.effectDistance = new Vector2(1f, -1f);
        image.color = Color.white;
        frame.localScale = Vector3.one;
        frame.anchoredPosition = Vector2.zero;
        frame.SetAsLastSibling();
        frame.gameObject.SetActive(visible);
        visibility = visible ? 1f : 0f;
    }

    public void SetTint(Color color)
    {
        tint = color;
        if (image != null) image.color = color;
    }

    public void SetVisible(bool visible)
    {
        CancelArrival();
        persistent = visible;
        visibility = visible ? 1f : 0f;
        if (frame != null)
        {
            frame.localScale = Vector3.one;
            frame.anchoredPosition = Vector2.zero;
            image.color = tint;
            frame.gameObject.SetActive(visible);
        }
        if (pieceGraphic != null) pieceGraphic.canvasRenderer.SetAlpha(visible ? 0.24f : 1f);
    }

    public void PlayArrival(float duration = 1.2f)
    {
        if (frame == null || !isActiveAndEnabled) return;
        CancelArrival();
        arrival = StartCoroutine(ArrivalRoutine(Mathf.Max(0.3f, duration)));
    }

    private IEnumerator ArrivalRoutine(float duration)
    {
        frame.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.16f));
            float alpha = enter * (persistent ? 1f : 1f - Mathf.InverseLerp(0.78f, 1f, progress));
            visibility = alpha;
            frame.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, enter);
            frame.anchoredPosition = new Vector2(0f, Mathf.Lerp(-8f, 0f, enter));
            image.color = new Color(tint.r, tint.g, tint.b, tint.a * alpha);
            yield return null;
        }
        frame.localScale = Vector3.one;
        frame.anchoredPosition = Vector2.zero;
        image.color = tint;
        frame.gameObject.SetActive(persistent);
        visibility = persistent ? 1f : 0f;
        arrival = null;
    }

    private void CancelArrival()
    {
        if (arrival != null) StopCoroutine(arrival);
        arrival = null;
    }

    private void OnDisable()
    {
        CancelArrival();
        if (frame == null) return;
        frame.localScale = Vector3.one;
        frame.anchoredPosition = Vector2.zero;
        image.color = tint;
        frame.gameObject.SetActive(persistent);
        visibility = persistent ? 1f : 0f;
        if (pieceGraphic != null) pieceGraphic.canvasRenderer.SetAlpha(1f);
    }

    private void LateUpdate()
    {
        // Fade only the kanji graphic; its child motif and battle labels stay opaque.
        if (pieceGraphic != null)
            pieceGraphic.canvasRenderer.SetAlpha(Mathf.Lerp(1f, 0.24f, frame != null && frame.gameObject.activeSelf ? visibility : 0f));
    }
}
