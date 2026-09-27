using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戦闘と軍勢画面で共通に使う、代表VFXの素材選択と見た目の動き。
/// ゲーム固有の判定・ダメージ・描画レイヤーの決定は呼び出し側に残す。
/// </summary>
public static class SkillVfxMotion
{
    private static readonly Dictionary<string, Sprite> representativeSprites = new();
    private static readonly Dictionary<string, Texture2D> weaponTextures = new();

    public static string GetVfxFolder(CharacterData character)
    {
        if (character == null)
        {
            return null;
        }

        if (character.skillType == SkillType.NumberPassive)
        {
            return character.category switch
            {
                CharacterCategory.Number2 => "NumberAura_Mid_Medium",
                CharacterCategory.Number3 => "NumberAura_High_Medium",
                _ => "NumberAura_Low_Medium"
            };
        }

        return character.skillType switch
        {
            SkillType.Counter => "Shield",
            SkillType.AreaCounter => "Wall",
            SkillType.Arrow => "Arrow",
            SkillType.Gun => "Gun",
            SkillType.Spear => "Spear",
            SkillType.Stone => "Stone",
            SkillType.Shield => "Shield",
            SkillType.Armor => "Armor",
            SkillType.Wall => "Wall",
            SkillType.Soil => "Soil",
            SkillType.Fireball => "Fireball",
            SkillType.WoodPush => "WoodPush",
            SkillType.WaterHeal => "WaterHeal",
            SkillType.HorseCharge => "HorseCharge",
            SkillType.BirdRetreat => "BirdRetreat",
            SkillType.TigerTwinClaw => "TigerTwinClaw",
            SkillType.Dragon => "DragonBreath",
            // Sword and hammer use their dedicated RawImage motion paths. Keeping
            // them out of this representative-sprite lookup prevents a caller from
            // silently reviving the retired frame-sequence assets.
            SkillType.Slash or SkillType.StunBlow => null,
            _ => null
        };
    }

    public static Sprite GetRepresentativeSprite(string folder)
    {
        if (string.IsNullOrEmpty(folder))
        {
            return null;
        }

        if (representativeSprites.TryGetValue(folder, out Sprite cached))
        {
            return cached;
        }

        int keyFrame = folder switch
        {
            "NumberAura_High_Strong" => 7,
            "Arrow" or "Gun" => 2,
            "Stone" => 0,
            "Fireball" => 2,
            "WoodPush" or "WaterHeal" or "Soil" => 3,
            "HorseCharge" or "BirdRetreat" => 2,
            "TigerTwinClaw" or "DragonBreath" or "DragonRoar" => 3,
            "Shield" or "Armor" or "Wall" => 2,
            _ => 1
        };
        // Generated VFX PNGs can have an importer-side sprite slice with a non-center
        // pivot. Build from the full source texture so all effects, especially the
        // number aura, rotate around the owning piece consistently in every screen.
        Texture2D texture = Resources.Load<Texture2D>($"VFX/Animated/{folder}/frame_{keyFrame:00}");
        if (texture != null)
        {
            cached = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
        }
        else
        {
            cached = Resources.Load<Sprite>($"VFX/Animated/{folder}/frame_{keyFrame:00}");
        }
        if (cached == null)
        {
            return null;
        }

        if (folder == "NumberAura_High_Strong")
        {
            cached = CreateSafeHighStrongAuraSprite(cached);
        }

        representativeSprites[folder] = cached;
        return cached;
    }

    public static Texture2D GetWeaponTexture(string weapon)
    {
        if (string.IsNullOrEmpty(weapon))
        {
            return null;
        }

        if (weaponTextures.TryGetValue(weapon, out Texture2D cached))
        {
            return cached;
        }

        cached = Resources.Load<Texture2D>($"VFX/Weapons/{weapon}");
        if (cached != null)
        {
            weaponTextures[weapon] = cached;
        }
        return cached;
    }

    public static Vector2 GridDirectionToVisual(Vector2Int gridDirection)
    {
        // Grid Y grows downward, while UI rotation grows upward.
        return new Vector2(gridDirection.x, -gridDirection.y).normalized;
    }

    public static float GetTravelAngle(CharacterData character, Vector3 travel)
    {
        if (character == null || !UsesDirectionalSource(character.skillType) || travel.sqrMagnitude < 0.001f)
        {
            return 0f;
        }

        return Mathf.Atan2(travel.y, travel.x) * Mathf.Rad2Deg;
    }

    public static IEnumerator PlaySwordMotion(
        Transform overlay,
        Vector3 sourcePosition,
        Vector2 visualDirection,
        float size,
        float duration,
        float preWindEnd,
        float sweepEnd,
        Action<GameObject> onCreated = null,
        Action<GameObject> onFinished = null)
    {
        Texture2D texture = GetWeaponTexture("SwordMotion");
        if (overlay == null || texture == null || visualDirection.sqrMagnitude < 0.001f)
        {
            yield break;
        }

        GameObject effectObject = CreateWeaponObject("SwordMotion", overlay, texture, sourcePosition, new Vector2(0.13f, 0.16f), size, onCreated, out RectTransform rect, out RawImage[] layers);
        float attackAngle = Mathf.Atan2(visualDirection.y, visualDirection.x) * Mathf.Rad2Deg;
        const float sourceTipAngle = 45f;
        const float visibleSweepHalfAngle = 45f;
        const float preWindAngle = 30f;
        float sweepSign = visualDirection.x < 0f ? -1f : 1f;
        float fullStartAngle = attackAngle + sweepSign * visibleSweepHalfAngle - sourceTipAngle;
        float fullEndAngle = attackAngle - sweepSign * visibleSweepHalfAngle - sourceTipAngle;
        float preWindStartAngle = fullStartAngle + sweepSign * preWindAngle;
        float elapsed = 0f;

        while (elapsed < duration && effectObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float preWindProgress = Mathf.InverseLerp(0f, preWindEnd, progress);
            float sweepProgress = Mathf.InverseLerp(preWindEnd, sweepEnd, progress);
            if (progress < preWindEnd)
            {
                rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(
                    preWindStartAngle,
                    fullStartAngle,
                    Mathf.SmoothStep(0f, 1f, preWindProgress)));
            }
            else
            {
                float acceleratedSweep = sweepProgress * sweepProgress * sweepProgress;
                rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(fullStartAngle, fullEndAngle, acceleratedSweep));
            }

            float reach = Mathf.Lerp(0.28f, 1f, Mathf.SmoothStep(0f, 1f, preWindProgress));
            float scale = Mathf.Lerp(0.96f, 1.04f, Mathf.Sin(progress * Mathf.PI)) * reach;
            rect.localScale = Vector3.one * scale;
            SetWeaponAlpha(layers, FadeOutAlpha(progress, 0.94f));
            yield return null;
        }

        Finish(effectObject, onFinished);
    }

    public static IEnumerator PlayHammerMotion(
        Transform overlay,
        Vector3 sourcePosition,
        Vector2 visualDirection,
        float size,
        float duration,
        Action<GameObject> onCreated = null,
        Action<GameObject> onFinished = null)
    {
        Texture2D texture = GetWeaponTexture("HammerMotion");
        if (overlay == null || texture == null || visualDirection.sqrMagnitude < 0.001f)
        {
            yield break;
        }

        GameObject effectObject = CreateWeaponObject("HammerMotion", overlay, texture, sourcePosition, new Vector2(0.18f, 0.19f), size, onCreated, out RectTransform rect, out RawImage[] layers);
        float attackAngle = Mathf.Atan2(visualDirection.y, visualDirection.x) * Mathf.Rad2Deg;
        const float sourceHeadAngle = 40f;
        const float swingDegrees = 70f;
        float swingSign = visualDirection.x < 0f ? -1f : 1f;
        float startAngle = attackAngle + swingSign * swingDegrees - sourceHeadAngle;
        float impactAngle = attackAngle - sourceHeadAngle;
        float elapsed = 0f;
        while (elapsed < duration && effectObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float fallProgress = Mathf.Clamp01(progress / 0.62f);
            float gravityFall = fallProgress * fallProgress;
            rect.position = sourcePosition;
            rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, impactAngle, gravityFall));
            rect.localScale = Vector3.one * Mathf.Lerp(0.88f, 0.98f, gravityFall);
            SetWeaponAlpha(layers, FadeOutAlpha(progress, 0.94f));
            yield return null;
        }

        Finish(effectObject, onFinished);
    }

    public static IEnumerator PlayNumberAura(
        Transform parent,
        Sprite sprite,
        Vector3 position,
        float size,
        float duration,
        float rotationDegrees,
        bool useAnchoredPosition,
        Action<GameObject> onCreated = null,
        Action<GameObject> onFinished = null)
    {
        if (parent == null || sprite == null)
        {
            yield break;
        }

        var effectObject = new GameObject("SharedNumberAura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = effectObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * size;
        if (useAnchoredPosition)
        {
            rect.anchoredPosition = position;
        }
        else
        {
            rect.position = position;
        }
        rect.SetAsLastSibling();

        Image image = effectObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        onCreated?.Invoke(effectObject);

        float elapsed = 0f;
        while (elapsed < duration && effectObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            rect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.1f, Mathf.Sin(progress * Mathf.PI * 0.5f));
            rect.localRotation = Quaternion.Euler(0f, 0f, progress * rotationDegrees);
            Color color = image.color;
            color.a = FadeOutAlpha(progress, 0.78f);
            image.color = color;
            yield return null;
        }

        Finish(effectObject, onFinished);
    }

    public static IEnumerator PlayRepresentativeSprite(
        Transform parent,
        Sprite sprite,
        Vector3 sourcePosition,
        Vector3 targetPosition,
        float size,
        float duration,
        float rotationDegrees = 0f)
    {
        if (parent == null || sprite == null)
        {
            yield break;
        }

        var effectObject = new GameObject("SharedRepresentativeVfx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = effectObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * size;
        rect.position = sourcePosition;
        rect.SetAsLastSibling();

        Image image = effectObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        float elapsed = 0f;
        while (elapsed < duration && effectObject != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            rect.position = Vector3.LerpUnclamped(sourcePosition, targetPosition, eased);
            rect.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            rect.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, Mathf.Sin(progress * Mathf.PI));
            Color color = image.color;
            color.a = FadeOutAlpha(progress, 0.72f);
            image.color = color;
            yield return null;
        }

        Finish(effectObject, null);
    }

    private static GameObject CreateWeaponObject(
        string name,
        Transform overlay,
        Texture2D texture,
        Vector3 sourcePosition,
        Vector2 pivot,
        float size,
        Action<GameObject> onCreated,
        out RectTransform rect,
        out RawImage[] layers)
    {
        var effectObject = new GameObject($"Shared{name}", typeof(RectTransform));
        rect = effectObject.GetComponent<RectTransform>();
        rect.SetParent(overlay, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.position = sourcePosition;
        rect.pivot = pivot;
        rect.sizeDelta = Vector2.one * size;
        rect.SetAsLastSibling();
        layers = CreateWeaponLayers(rect, texture);
        onCreated?.Invoke(effectObject);
        return effectObject;
    }

    private static RawImage[] CreateWeaponLayers(RectTransform parent, Texture2D texture)
    {
        const int layerCount = 3;
        var layers = new RawImage[layerCount];
        for (int index = 0; index < layerCount; index++)
        {
            var layerObject = new GameObject($"WeaponVisualLayer_{index + 1:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.SetParent(parent, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            var image = layerObject.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.color = Color.white;
            layers[index] = image;
        }
        return layers;
    }

    private static void SetWeaponAlpha(IEnumerable<RawImage> layers, float alpha)
    {
        if (layers == null)
        {
            return;
        }

        foreach (RawImage layer in layers)
        {
            if (layer == null)
            {
                continue;
            }
            Color color = layer.color;
            color.a = alpha;
            layer.color = color;
        }
    }

    private static float FadeOutAlpha(float progress, float fadeStart)
    {
        float fadeProgress = Mathf.InverseLerp(fadeStart, 1f, progress);
        return Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, fadeProgress));
    }

    private static void Finish(GameObject effectObject, Action<GameObject> onFinished)
    {
        if (effectObject == null)
        {
            return;
        }

        if (onFinished != null)
        {
            onFinished(effectObject);
        }
        else
        {
            UnityEngine.Object.Destroy(effectObject);
        }
    }

    private static bool UsesDirectionalSource(SkillType skillType)
    {
        return skillType is SkillType.Slash
            or SkillType.StunBlow
            or SkillType.Arrow
            or SkillType.Gun
            or SkillType.Spear
            or SkillType.Stone
            or SkillType.Fireball
            or SkillType.WoodPush
            or SkillType.HorseCharge
            or SkillType.BirdRetreat
            or SkillType.TigerTwinClaw
            or SkillType.Dragon;
    }

    private static Sprite CreateSafeHighStrongAuraSprite(Sprite source)
    {
        if (source == null)
        {
            return null;
        }

        const float inset = 16f;
        Rect sourceRect = source.rect;
        float width = Mathf.Max(1f, sourceRect.width - inset * 2f);
        float height = Mathf.Max(1f, sourceRect.height - inset * 2f);
        Rect safeRect = new Rect(sourceRect.x + inset, sourceRect.y + inset, width, height);
        return Sprite.Create(source.texture, safeRect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }
}
