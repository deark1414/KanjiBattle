using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleManager
{
    private SupportVfxLayout supportLayout;
    private readonly Dictionary<string, Texture2D> supportTextures = new();
    private readonly Dictionary<BattleCharacter, GameObject> supportCoal = new();
    private readonly Dictionary<BattleCharacter, GameObject> supportCocoons = new();
    private readonly Dictionary<BattleCharacter, IronForm> supportIron = new();
    private readonly HashSet<GameObject> supportRoots = new();
    private readonly System.Random supportVisualRandom = new System.Random();

    private bool SupportBattleActive() => this != null && isActiveAndEnabled
        && (resultPanel == null || !resultPanel.activeInHierarchy);
    private bool SupportAlive(BattleCharacter unit) => SupportBattleActive()
        && unit != null && !unit.isDead && unit.gameObject.activeInHierarchy;

    public void ClearSupportSkillVfx()
    {
        var roots = new List<GameObject>(supportRoots);
        supportRoots.Clear();
        supportCoal.Clear();
        supportCocoons.Clear();
        supportIron.Clear();
        foreach (var root in roots)
        {
            if (root == null) continue;
            root.SetActive(false);
            DestroyBattleVfx(root);
        }
    }

    // Battle speed is not Time.timeScale in this project; sample it every frame.
    private float SupportDelta() => isPaused ? 0f : Time.deltaTime * CurrentBattleSpeed;

    private SupportVfxEntry SupportEntry(string key)
    {
        if (supportLayout == null)
        {
            var json = Resources.Load<TextAsset>("VFX/SupportSkills/layout");
            if (json == null) return null;
            supportLayout = JsonUtility.FromJson<SupportVfxLayout>(json.text);
        }
        return Array.Find(supportLayout.entries ?? Array.Empty<SupportVfxEntry>(), e => e.key == key);
    }

    private Texture2D SupportTexture(string key)
    {
        if (!supportTextures.TryGetValue(key, out var texture) || texture == null)
        {
            texture = Resources.Load<Texture2D>("VFX/SupportSkills/" + key);
            if (texture != null) supportTextures[key] = texture;
        }
        return texture;
    }

    private GameObject SupportRoot(BattleCharacter target, string name, bool defensive,
        float duration, Action<float> draw, Func<bool> extraValid = null, Action removed = null,
        bool holdOnDeath = false)
    {
        var root = new GameObject("SupportVfx_" + name, typeof(RectTransform));
        var rect = (RectTransform)root.transform;
        rect.SetParent(defensive ? target.transform : EnsureWeaponVfxOverlay(), false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = Vector2.zero;
        rect.position = target.transform.position;
        rect.SetAsLastSibling();
        if (!defensive)
        {
            if (holdOnDeath) root.AddComponent<SupportSkillVfxSnapshotTarget>().Target = target;
            else root.AddComponent<BattleVfxFollowTarget>().Initialize(target.transform as RectTransform);
        }
        RegisterBattleVfx(root);
        supportRoots.Add(root);
        var lifetime = root.AddComponent<SupportSkillVfxLifetime>();
        lifetime.Initialize(
            () => (holdOnDeath ? SupportBattleActive() : SupportAlive(target)) && (extraValid == null || extraValid()),
            SupportDelta, duration, time =>
            {
                if (!defensive && SupportAlive(target)) rect.position = target.transform.position;
                draw?.Invoke(time);
            }, removed);
        lifetime.Release = () =>
        {
            supportRoots.Remove(root);
            if (root != null) { root.SetActive(false); DestroyBattleVfx(root); }
        };
        lifetime.Released = () => supportRoots.Remove(root);
        return root;
    }

    private RawImage SupportImage(Transform parent, string key, float visibleWidth)
    {
        var entry = SupportEntry(key);
        var texture = SupportTexture(key);
        if (entry == null || texture == null || entry.bounds == null || entry.bounds.w <= 0 || entry.bounds.h <= 0)
            return null;
        var go = new GameObject(key, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        // Layout uses top-left PNG coordinates. Crop transparent padding before sizing the visible art.
        image.uvRect = new Rect(entry.bounds.x / entry.width,
            1f - (entry.bounds.y + entry.bounds.h) / entry.height,
            entry.bounds.w / entry.width, entry.bounds.h / entry.height);
        image.rectTransform.sizeDelta = new Vector2(visibleWidth, visibleWidth * entry.bounds.h / entry.bounds.w);
        image.color = new Color(1f, 1f, 1f, 0f);
        return image;
    }

    private static void SupportPose(RawImage image, Vector2 offset, float scale, float alpha, float angle = 0f)
    {
        if (image == null) return;
        image.rectTransform.anchoredPosition = offset;
        image.rectTransform.localScale = Vector3.one * scale;
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private static float SupportFade(float progress) => 1f - Mathf.InverseLerp(.7f, 1f, progress);
    private static bool SupportDefensive(string key) => key == "GuardShell" || key == "GuardShield"
        || key == "CocoonGuard" || key == "ThreadWrap" || key == "CoalCold" || key == "CoalHot";

    public void PlaySupportBurst(BattleCharacter target, string key, float duration = .6f, float scale = 1f)
    {
        if (!SupportBattleActive() || target == null || !target.gameObject.activeInHierarchy
            || string.IsNullOrEmpty(key) || duration <= 0f) return;
        bool defensive = SupportDefensive(key);
        if (defensive && target.isDead) return;
        if (key == "MagneticPull") return; // Magnetic ripples must originate at the two poles.
        RawImage image = null;
        bool shell = key == "GuardShell";
        Vector2 offset = shell ? Vector2.down * currentBattleCellSize * .25f : Vector2.zero;
        var root = SupportRoot(target, key, defensive, duration, time =>
        {
            float p = Mathf.Clamp01(time / duration);
            SupportPose(image, offset, shell ? 1f : Mathf.Lerp(.8f, 1.15f, p), SupportFade(p));
        }, holdOnDeath: !defensive);
        image = SupportImage(root.transform, key, currentBattleCellSize * (shell ? 1.1f : 1.3f) * Mathf.Max(.05f, scale));
        if (image == null) DestroyBattleVfx(root);
        else SupportPose(image, offset, shell ? 1f : .8f, 1f);
    }

    public void PlaySupportLink(BattleCharacter from, BattleCharacter to, string key, float delay = 0f)
    {
        if (!SupportAlive(from) || !SupportAlive(to) || string.IsNullOrEmpty(key)
            || key == "MagneticPull" || key == "MagnetBody") return;
        var entry = SupportEntry(key);
        if (entry == null || entry.start == null || entry.end == null) return;
        delay = Mathf.Max(0f, delay);
        RawImage image = null;
        const float span = .4f;
        var root = SupportRoot(to, key, false, delay + span, time =>
        {
            if (image == null) return;
            float p = Mathf.Clamp01((time - delay) / span);
            Vector3 a = image.transform.parent.InverseTransformPoint(from.transform.position);
            Vector3 b = image.transform.parent.InverseTransformPoint(to.transform.position);
            Vector2 delta = b - a;
            image.rectTransform.sizeDelta = new Vector2(delta.magnitude,
                Mathf.Max(currentBattleCellSize * .035f, delta.magnitude * entry.bounds.h / entry.bounds.w));
            SupportPose(image, (a + b) * .5f, 1f, time < delay ? 0f : SupportFade(p),
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }, () => SupportAlive(from));
        image = SupportImage(root.transform, key, currentBattleCellSize);
        if (image == null) DestroyBattleVfx(root);
    }

    public void PlaySupportEcho(BattleCharacter bell, BattleCharacter recipient)
    {
        if (!SupportAlive(bell) || !SupportAlive(recipient)) return;
        PlaySupportBurst(recipient, "BellEcho", .8f, 1.15f);
        int count = supportVisualRandom.Next(2, 4);
        for (int i = 0; i < count; i++)
        {
            float theta = (i * 360f / count + (float)supportVisualRandom.NextDouble() * 35f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            float tilt = (float)supportVisualRandom.NextDouble() * 35f - 17.5f;
            RawImage note = null;
            var root = SupportRoot(recipient, "BellNote", false, .8f, time =>
                SupportPose(note, direction * currentBattleCellSize * (.72f + time * .2f), 1f,
                    Mathf.Sin(Mathf.Clamp01(time / .8f) * Mathf.PI), tilt));
            note = SupportImage(root.transform, "BellNote", currentBattleCellSize * .22f);
            if (note == null) DestroyBattleVfx(root);
        }
    }

    public void PlaySupportDice(BattleCharacter dice, int face)
    {
        if (!SupportAlive(dice) || face < 1 || face > 6) return;
        RawImage image = null;
        float[] times = { 0f, .05f, .11f, .19f, .3f, .44f, .65f };
        int[] faces = new int[times.Length];
        // Fix the decorative roll up front: pause, frame rate and redraw never reroll it.
        for (int i = 0; i < faces.Length - 1; i++)
        {
            int next;
            do { next = supportVisualRandom.Next(1, 7); }
            while ((i > 0 && next == faces[i - 1]) || (i == faces.Length - 2 && next == face));
            faces[i] = next;
        }
        faces[faces.Length - 1] = face;
        var root = SupportRoot(dice, "Dice", false, 1.8f, time =>
        {
            int index = 0;
            while (index + 1 < times.Length && time >= times[index + 1]) index++;
            int visibleFace = faces[index];
            if (image == null) return;
            var entry = SupportEntry("DiceFace" + visibleFace);
            var texture = SupportTexture("DiceFace" + visibleFace);
            if (entry == null || texture == null) return;
            image.texture = texture;
            image.uvRect = new Rect(entry.bounds.x / entry.width,
                1f - (entry.bounds.y + entry.bounds.h) / entry.height,
                entry.bounds.w / entry.width, entry.bounds.h / entry.height);
            SupportPose(image, Vector2.up * currentBattleCellSize * .65f, 1f,
                SupportFade(Mathf.Clamp01((time - 1.4f) / .4f)));
        });
        image = SupportImage(root.transform, "DiceFace" + face, currentBattleCellSize * .85f);
        if (image != null)
        {
            var first = SupportEntry("DiceFace" + faces[0]);
            if (first != null)
            {
                image.texture = SupportTexture("DiceFace" + faces[0]);
                image.uvRect = new Rect(first.bounds.x / first.width,
                    1f - (first.bounds.y + first.bounds.h) / first.height,
                    first.bounds.w / first.width, first.bounds.h / first.height);
            }
            SupportPose(image, Vector2.up * currentBattleCellSize * .65f, 1f, 1f);
        }
        RawImage light = SupportImage(root.transform, "DiceSparkles", currentBattleCellSize * 1.5f);
        root.AddComponent<SupportSkillVfxLifetime>().Initialize(() => SupportAlive(dice), SupportDelta, 1.8f,
            time => SupportPose(light, Vector2.up * currentBattleCellSize * .65f, 1f,
                time >= .65f && time < 1.15f ? Mathf.Sin((time - .65f) / .5f * Mathf.PI) : 0f));
    }

    public void RefreshSupportState(BattleCharacter unit)
    {
        if (unit == null) return;
        if (supportCoal.TryGetValue(unit, out var oldCoal))
        {
            if (oldCoal != null) { oldCoal.SetActive(false); DestroyBattleVfx(oldCoal); }
            supportCoal.Remove(unit);
        }
        if (!SupportAlive(unit) || unit.data == null) { supportIron.Remove(unit); return; }
        if (unit.data.skillType == SkillType.Coal)
        {
            RawImage image = null;
            var root = SupportRoot(unit, "CoalState", true, float.PositiveInfinity, time =>
            {
                string key = unit.CoalAwake ? "CoalHot" : "CoalCold";
                var entry = SupportEntry(key);
                if (image == null || entry == null) return;
                image.texture = SupportTexture(key);
                image.uvRect = new Rect(entry.bounds.x / entry.width,
                    1f - (entry.bounds.y + entry.bounds.h) / entry.height,
                    entry.bounds.w / entry.width, entry.bounds.h / entry.height);
                SupportPose(image, Vector2.zero, 1f, 1f);
            }, () => unit.data != null && unit.data.skillType == SkillType.Coal,
            () => supportCoal.Remove(unit));
            // Removing an old state must not remove a newer instance from the map.
            root.GetComponent<SupportSkillVfxLifetime>().Removed = () =>
            { if (supportCoal.TryGetValue(unit, out var value) && value == root) supportCoal.Remove(unit); };
            image = SupportImage(root.transform, unit.CoalAwake ? "CoalHot" : "CoalCold", currentBattleCellSize * .9f);
            if (image == null) DestroyBattleVfx(root);
            else { supportCoal[unit] = root; SupportPose(image, Vector2.zero, 1f, 1f); }
        }
        if (unit.data.skillType == SkillType.IronAdapt)
        {
            if (!supportIron.TryGetValue(unit, out var previous) || previous != unit.IronForm)
            {
                supportIron[unit] = unit.IronForm;
                PlaySupportBurst(unit, unit.IronForm == IronForm.Sword ? "ReviewSword"
                    : unit.IronForm == IronForm.Spear ? "ReviewSpear" : "GuardShield", .45f);
            }
        }
        else supportIron.Remove(unit);
        if (!unit.CocoonShield) SetSupportCocoon(unit, false);
    }

    public void SetSupportCocoon(BattleCharacter target, bool active)
    {
        if (target == null) return;
        if (supportCocoons.TryGetValue(target, out var previous))
        {
            if (previous != null) { previous.SetActive(false); DestroyBattleVfx(previous); }
            supportCocoons.Remove(target);
        }
        if (!active || !SupportAlive(target) || !target.CocoonShield) return;
        RawImage wrap = null, guard = null;
        var root = SupportRoot(target, "Cocoon", true, float.PositiveInfinity, time =>
        {
            SupportPose(wrap, Vector2.zero, 1f, time < .55f ? SupportFade(time / .55f) : 0f);
            // An actual blank interval separates the wrap from the persistent shield.
            SupportPose(guard, Vector2.zero, 1f, time >= .63f ? Mathf.Clamp01((time - .63f) / .12f) : 0f);
        }, () => target.CocoonShield);
        root.GetComponent<SupportSkillVfxLifetime>().Removed = () =>
        { if (supportCocoons.TryGetValue(target, out var value) && value == root) supportCocoons.Remove(target); };
        wrap = SupportImage(root.transform, "ThreadWrap", currentBattleCellSize * 1.1f);
        guard = SupportImage(root.transform, "CocoonGuard", currentBattleCellSize * .98f);
        supportCocoons[target] = root;
    }

    public void PlaySupportForge(BattleCharacter target)
    {
        if (!SupportAlive(target)) return;
        RawImage hammer = null, sparks = null;
        Image heat = null;
        var root = SupportRoot(target, "Forge", false, 1.4f, time =>
        {
            float swing = Mathf.Clamp01(time / .4f);
            SupportPose(hammer, new Vector2(-.45f, .45f) * currentBattleCellSize, 1f,
                time < .53f ? 1f : 0f, Mathf.Lerp(45f, -75f, swing * swing));
            SupportPose(sparks, Vector2.zero, 1f,
                time >= .4f && time < .75f ? SupportFade((time - .4f) / .35f) : 0f);
            if (heat != null)
            {
                float alpha = time < .4f ? 0f : Mathf.Min((time - .4f) / .15f, (1.4f - time) / .55f);
                heat.color = new Color(1f, .2f, .06f, Mathf.Clamp01(alpha) * .7f);
            }
        });
        // Copy only the resolved piece sprite. Never tint or replace the real character image.
        Sprite icon = target.data == null ? null : (!target.isAlly && target.data.enemyIcon != null
            ? target.data.enemyIcon : target.data.icon);
        if (icon != null)
        {
            var go = new GameObject("ForgeHeat", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(root.transform, false);
            heat = go.GetComponent<Image>();
            heat.sprite = icon;
            heat.preserveAspect = true;
            heat.raycastTarget = false;
            heat.color = Color.clear;
            var ownerRect = target.transform as RectTransform;
            heat.rectTransform.sizeDelta = ownerRect != null ? ownerRect.rect.size : Vector2.one * currentBattleCellSize;
        }
        hammer = SupportImage(root.transform, "ForgeHammer", currentBattleCellSize * .95f);
        if (hammer != null) hammer.rectTransform.pivot = new Vector2(.1f, .1f);
        sparks = SupportImage(root.transform, "ForgeSparks", currentBattleCellSize);
    }

    public void PlaySupportMagnet(BattleCharacter caster, BattleCharacter target)
    {
        if (!SupportAlive(caster) || !SupportAlive(target)) return;
        Vector2Int gridDirection = target.gridPos - caster.gridPos;
        if (gridDirection == Vector2Int.zero) return;
        Vector2 direction = new Vector2(gridDirection.x, -gridDirection.y).normalized;
        float theta = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // Source art poles point toward (0.25, +1) in Unity's upward Y coordinates.
        float rotation = theta - Mathf.Atan2(1f, .25f) * Mathf.Rad2Deg;
        Quaternion orient = Quaternion.Euler(0f, 0f, rotation);
        RawImage body = null;
        var waves = new List<RawImage>();
        var root = SupportRoot(caster, "Magnet", false, 1.25f, time =>
        {
            SupportPose(body, Vector2.zero, 1f, SupportFade(time / 1.25f), rotation);
            for (int i = 0; i < waves.Count; i++)
            {
                int pole = i / 3, pulse = i % 3;
                float q = (time - .12f - pulse * .12f) / .65f;
                Vector2 tip = new Vector2(pole == 0 ? -12.8f : 16f, pole == 0 ? 29.24f : 15.48f);
                Vector2 offset = (Vector2)(orient * tip) * currentBattleCellSize / 80f
                    + direction * currentBattleCellSize * (.1f + Mathf.Clamp01(q) * .52f);
                SupportPose(waves[i], offset, 1f + Mathf.Clamp01(q) * .6f,
                    q >= 0f && q <= 1f ? Mathf.Sin(q * Mathf.PI) : 0f, theta);
            }
        }, () => SupportAlive(target));
        body = SupportImage(root.transform, "MagnetBody", currentBattleCellSize);
        for (int i = 0; i < 6; i++) waves.Add(SupportImage(root.transform, "MagneticPull", currentBattleCellSize * .08f));
    }
}

[Serializable]
public sealed class SupportVfxLayout { public SupportVfxEntry[] entries; }
[Serializable]
public sealed class SupportVfxEntry
{
    public string key, sprite;
    public float width, height;
    public SupportVfxBounds bounds;
    public float[] pivot, start, end;
}
[Serializable]
public sealed class SupportVfxBounds { public float x, y, w, h; }

// Lives on each effect rather than on BattleManager's gameplay coroutine scheduler.
public sealed class SupportSkillVfxLifetime : MonoBehaviour
{
    private Func<bool> valid;
    private Func<float> delta;
    private Action<float> draw;
    private float duration, elapsed;
    public Action Removed;
    public Action Release;
    public Action Released;
    private bool finishing;

    public void Initialize(Func<bool> isValid, Func<float> getDelta, float seconds,
        Action<float> render, Action removed = null)
    { valid = isValid; delta = getDelta; duration = seconds; draw = render; Removed = removed; }

#if KANJI_BATTLE_E2E
    internal void FreezeForReview(float seconds)
    {
        elapsed = Mathf.Clamp(seconds, 0f, Mathf.Max(0f, duration - .001f));
        delta = () => 0f;
        draw?.Invoke(elapsed);
    }
#endif

    private void Update()
    {
        if (valid == null) return;
        if (!valid()) { Finish(); return; }
        elapsed += Mathf.Max(0f, delta());
        if (elapsed >= duration) { Finish(); return; }
        draw?.Invoke(elapsed);
    }

    private void Finish()
    {
        if (finishing) return;
        finishing = true;
        if (Release != null) Release();
        else { gameObject.SetActive(false); Destroy(gameObject); }
    }

    private void OnDisable() { if (valid != null) Finish(); }
    private void OnDestroy() { Removed?.Invoke(); Released?.Invoke(); Removed = Released = Release = null; }
}

public sealed class SupportSkillVfxSnapshotTarget : MonoBehaviour
{
    public BattleCharacter Target;
    private void LateUpdate()
    {
        if (Target != null && !Target.isDead && Target.gameObject.activeInHierarchy)
            transform.position = Target.transform.position;
    }
}
