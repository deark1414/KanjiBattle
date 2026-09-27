using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 軍勢画面の加入と模擬稽古で、戦闘で確認済みの代表VFXを一枚絵として再利用する。
/// </summary>
public sealed class RosterPracticeVfx : MonoBehaviour
{
    private sealed class PracticeMember
    {
        public CharacterData character;
        public RectTransform source;
        public RectTransform target;
    }

    private readonly List<PracticeMember> members = new();
    private RectTransform overlay;
    private RectTransform trainingField;
    private TextMeshProUGUI trainingStatus;
    private Coroutine activePractice;
    private float nextPracticeAt = -1f;
    private float nextStatusRefreshAt;
    private bool isSubscribedToTraining;
    private const int MaximumTrainingPairs = 4;

    public void ConfigureTraining(Transform host, IReadOnlyList<CharacterData> roster, int slotCount)
    {
        if (host == null)
        {
            return;
        }

        if (activePractice != null)
        {
            StopCoroutine(activePractice);
            activePractice = null;
        }

        EnsureTrainingField(host);
        ClearTrainingField();
        members.Clear();
        if (roster != null && trainingField != null)
        {
            List<CharacterData> candidates = SelectTrainingMembers(roster, slotCount);
            CreateTrainingPairs(candidates);
        }

        EnsureOverlay();
        SubscribeToTrainingProgress();
        RefreshTrainingStatus();
        nextPracticeAt = Time.unscaledTime + UnityEngine.Random.Range(1.8f, 3.2f);
        nextStatusRefreshAt = Time.unscaledTime;
    }

    private void OnDisable()
    {
        if (activePractice != null)
        {
            StopCoroutine(activePractice);
            activePractice = null;
        }

        if (overlay != null)
        {
            for (int i = overlay.childCount - 1; i >= 0; i--)
            {
                Destroy(overlay.GetChild(i).gameObject);
            }
        }

        if (trainingField != null)
        {
            ClearTrainingField();
        }

        UnsubscribeFromTrainingProgress();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTrainingProgress();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextStatusRefreshAt)
        {
            RefreshTrainingStatus();
            nextStatusRefreshAt = Time.unscaledTime + 1f;
        }

        if (!isActiveAndEnabled || activePractice != null || members.Count == 0)
        {
            return;
        }

        if (Time.unscaledTime < nextPracticeAt)
        {
            return;
        }

        activePractice = StartCoroutine(PracticeRoutine());
    }

    private void EnsureTrainingField(Transform host)
    {
        if (trainingField != null && trainingField.parent != host)
        {
            trainingField.SetParent(host, false);
        }

        Transform existing = host.Find("MockTrainingField");
        if (existing != null)
        {
            trainingField = existing as RectTransform;
        }

        if (trainingField == null)
        {
            var fieldObject = new GameObject("MockTrainingField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            fieldObject.transform.SetParent(host, false);
            trainingField = fieldObject.GetComponent<RectTransform>();
        }

        var surface = trainingField.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(surface, new Color(0.07f, 0.12f, 0.09f, 0.80f));
        surface.raycastTarget = false;
        var outline = trainingField.GetComponent<Outline>();
        outline.effectColor = new Color(0.62f, 0.50f, 0.28f, 0.60f);
        outline.effectDistance = new Vector2(1f, -1f);
        trainingField.anchorMin = new Vector2(0.02f, 0.04f);
        trainingField.anchorMax = new Vector2(0.98f, 0.96f);
        trainingField.offsetMin = Vector2.zero;
        trainingField.offsetMax = Vector2.zero;
        trainingField.SetAsLastSibling();

        if (trainingStatus == null)
        {
            var labelObject = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(trainingField, false);
            trainingStatus = labelObject.GetComponent<TextMeshProUGUI>();
            UnityUIRuntimeTheme.EnsureJapaneseCapableFont(trainingStatus);
            trainingStatus.enableAutoSizing = true;
            trainingStatus.fontSizeMin = 21f;
            trainingStatus.fontSizeMax = 24f;
            trainingStatus.fontStyle = FontStyles.Bold;
            trainingStatus.alignment = TextAlignmentOptions.MidlineLeft;
            trainingStatus.color = new Color(0.94f, 0.89f, 0.72f, 1f);
            trainingStatus.outlineColor = new Color(0.02f, 0.02f, 0.01f, 0.9f);
            trainingStatus.outlineWidth = 0.18f;
            trainingStatus.raycastTarget = false;
            var labelRect = trainingStatus.rectTransform;
            labelRect.anchorMin = new Vector2(0.04f, 0.82f);
            labelRect.anchorMax = new Vector2(0.96f, 0.98f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            trainingStatus.gameObject.SetActive(true);
        }
    }

    private static List<CharacterData> SelectTrainingMembers(IReadOnlyList<CharacterData> roster, int slotCount)
    {
        List<CharacterData> pool = roster
            .Where(character => character != null)
            .OrderBy(_ => UnityEngine.Random.value)
            .ToList();
        int desiredCount = Mathf.Clamp(slotCount, 2, MaximumTrainingPairs);
        if (pool.Count == 0)
        {
            return new List<CharacterData>();
        }

        List<CharacterData> selected = pool.Take(desiredCount).ToList();
        // The preview represents formation slots, which may legitimately contain
        // duplicate pieces. Fill a sparse early roster instead of leaving the field empty.
        for (int index = selected.Count; index < desiredCount; index++)
        {
            selected.Add(pool[index % pool.Count]);
        }
        return selected;
    }

    private void CreateTrainingPairs(IReadOnlyList<CharacterData> characters)
    {
        if (trainingField == null)
        {
            return;
        }

        Sprite targetSprite = Resources.Load<Sprite>("UI/ModernWafuu/Practice/PracticeScarecrow");
        for (int index = 0; index < characters.Count; index++)
        {
            CharacterData character = characters[index];
            GetTrainingPairPosition(index, characters.Count, out float sourceX, out float targetX, out float y);
            RectTransform source = CreateTrainingPiece("Trainee_" + character.characterName, character.icon, sourceX, y, 1.96f, 0.98f);
            RectTransform target = CreateTrainingPiece("Scarecrow_" + index, targetSprite, targetX, 0.62f, 1.90f, 1f);
            if (source != null && target != null)
            {
                members.Add(new PracticeMember { character = character, source = source, target = target });
            }
        }
    }

    private static void GetTrainingPairPosition(int index, int pairCount, out float sourceX, out float targetX, out float y)
    {
        float spacing = pairCount <= 1 ? 0f : 0.68f / (pairCount - 1);
        sourceX = 0.16f + spacing * index;
        targetX = sourceX;
        y = 0.28f;
    }

    private RectTransform CreateTrainingPiece(string name, Sprite sprite, float x, float y, float sizeScale, float alpha)
    {
        if (sprite == null || trainingField == null)
        {
            return null;
        }

        var unitObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        unitObject.transform.SetParent(trainingField, false);
        var unit = unitObject.GetComponent<Image>();
        unit.sprite = sprite;
        unit.preserveAspect = true;
        unit.raycastTarget = false;
        unit.color = new Color(1f, 1f, 1f, alpha);
        var unitRect = unit.rectTransform;
        unitRect.anchorMin = new Vector2(x, y);
        unitRect.anchorMax = new Vector2(x, y);
        unitRect.pivot = new Vector2(0.5f, 0.5f);
        unitRect.anchoredPosition = Vector2.zero;
        unitRect.sizeDelta = Vector2.one * (56f * sizeScale);
        return unitRect;
    }

    private void ClearTrainingField()
    {
        if (trainingField == null)
        {
            return;
        }

        for (int i = trainingField.childCount - 1; i >= 0; i--)
        {
            Transform child = trainingField.GetChild(i);
            if (trainingStatus == null || child != trainingStatus.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public IEnumerator PlayRecruitment(CharacterData character, RectTransform source)
    {
        if (character == null || source == null)
        {
            yield break;
        }

        EnsureOverlay();
        if (overlay == null)
        {
            yield break;
        }
        // Recruitment is an arrival, not a generic flourish: each piece demonstrates
        // its own skill in a clear downward stroke before joining the roster.
        Vector3 target = source.position + Vector3.down * 118f;
        if (character.skillType == SkillType.Slash)
        {
            yield return PlaySwordPracticeMotion(source, target, 0.68f, 142f);
        }
        else if (character.skillType == SkillType.StunBlow)
        {
            yield return PlayHammerPracticeMotion(source, target, 0.68f, 142f);
        }
        else
        {
            yield return PlaySkillSprite(character, source.position, target, 0.68f, 1.25f);
        }
    }

    private IEnumerator PracticeRoutine()
    {
        int activeMemberCount = 0;
        foreach (PracticeMember member in members.OrderBy(_ => UnityEngine.Random.value))
        {
            if (member.source == null || member.target == null || !member.source.gameObject.activeInHierarchy || !member.target.gameObject.activeInHierarchy)
            {
                continue;
            }

            StartCoroutine(PracticeMemberRoutine(member, activeMemberCount * UnityEngine.Random.Range(0.12f, 0.24f)));
            activeMemberCount++;
        }

        // The offset makes this feel like several simultaneous exchanges rather
        // than a queue, while keeping individual effects readable.
        yield return new WaitForSecondsRealtime(activeMemberCount > 0 ? 2.35f : 0f);
        nextPracticeAt = Time.unscaledTime + UnityEngine.Random.Range(7.5f, 11f);
        activePractice = null;
    }

    private IEnumerator PracticeMemberRoutine(PracticeMember member, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (member == null || member.character == null || member.source == null || member.target == null)
        {
            yield break;
        }

        bool isNumberAura = member.character.skillType == SkillType.NumberPassive;
        Vector3 restPosition = member.source.position;
        if (!isNumberAura)
        {
            Vector3 engagedPosition = Vector3.Lerp(restPosition, member.target.position, 0.28f);
            yield return MovePracticePiece(member.source, restPosition, engagedPosition, 0.20f);
        }

        if (isNumberAura)
        {
            // Practice always shows the owner's number aura. It is deliberately
            // independent of the battle party's number-combination calculation.
            yield return PlayNumberPracticeAura(member.character, member.source);
        }
        else if (member.character.skillType == SkillType.Slash)
        {
            yield return PlaySwordPracticeMotion(member.source, member.target.position);
        }
        else if (member.character.skillType == SkillType.StunBlow)
        {
            yield return PlayHammerPracticeMotion(member.source, member.target.position);
        }
        else
        {
            yield return PlaySkillSprite(member.character, member.source.position, member.target.position, 1.0f, 1.02f);
        }
        yield return FlashTarget(member.target);
        if (!isNumberAura && member.source != null)
        {
            yield return MovePracticePiece(member.source, member.source.position, restPosition, 0.20f);
        }
    }

    private static IEnumerator MovePracticePiece(RectTransform piece, Vector3 from, Vector3 to, float duration)
    {
        if (piece == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            piece.position = Vector3.Lerp(from, to, eased);
            piece.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, Mathf.Sin(progress * Mathf.PI));
            yield return null;
        }

        piece.position = to;
        piece.localScale = Vector3.one;
    }

    private IEnumerator PlayNumberPracticeAura(CharacterData character, RectTransform source)
    {
        if (overlay == null || source == null)
        {
            yield break;
        }

        Sprite sprite = SkillVfxMotion.GetRepresentativeSprite(SkillVfxMotion.GetVfxFolder(character));
        if (sprite == null)
        {
            yield break;
        }

        yield return SkillVfxMotion.PlayNumberAura(
            overlay,
            sprite,
            source.position,
            156f,
            1f,
            110f,
            useAnchoredPosition: false);
    }

    private IEnumerator FlashTarget(RectTransform target)
    {
        if (target == null || !target.TryGetComponent(out Image image))
        {
            yield break;
        }

        Color baseColor = image.color;
        float elapsed = 0f;
        const float duration = 0.30f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            image.color = Color.Lerp(baseColor, new Color(1f, 0.58f, 0.42f, baseColor.a), pulse);
            target.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, pulse);
            yield return null;
        }
        image.color = baseColor;
        target.localScale = Vector3.one;
    }

    private void SubscribeToTrainingProgress()
    {
        if (isSubscribedToTraining || PlayerInventory.Instance == null)
        {
            return;
        }

        PlayerInventory.Instance.onMockTrainingCompleted += HandleMockTrainingCompleted;
        isSubscribedToTraining = true;
    }

    private void UnsubscribeFromTrainingProgress()
    {
        if (!isSubscribedToTraining || PlayerInventory.Instance == null)
        {
            return;
        }

        PlayerInventory.Instance.onMockTrainingCompleted -= HandleMockTrainingCompleted;
        isSubscribedToTraining = false;
    }

    private void HandleMockTrainingCompleted(PlayerExperienceResult result)
    {
        if (result.experience > 0)
        {
            RefreshTrainingStatus(result.level > result.previousLevel
                ? $"Lv.{result.previousLevel} → Lv.{result.level}"
                : $"+{result.experience} EXP");
        }
    }

    private void RefreshTrainingStatus(string recentReward = null)
    {
        if (trainingStatus == null || PlayerInventory.Instance == null)
        {
            return;
        }

        int reward = PlayerInventory.Instance.GetMockTrainingExperienceReward();
        trainingStatus.gameObject.SetActive(true);
        trainingStatus.text = reward <= 0
            ? "現在の上限に到達"
            : string.IsNullOrEmpty(recentReward)
            ? $"一回 +{reward} EXP"
            : recentReward;
    }

    private IEnumerator PlaySwordPracticeMotion(RectTransform source, Vector3 targetPosition, float duration = 0.92f, float size = 132f)
    {
        if (source == null || overlay == null)
        {
            yield break;
        }

        Vector3 travel = targetPosition - source.position;
        if (travel.sqrMagnitude < 0.001f)
        {
            yield break;
        }

        // Battle uses the same motion; the practice view only stretches the timing
        // so the full sweep is readable without changing its direction or curve.
        yield return SkillVfxMotion.PlaySwordMotion(
            overlay,
            source.position,
            travel.normalized,
            size,
            duration,
            0.20f,
            0.86f);
    }

    private IEnumerator PlayHammerPracticeMotion(RectTransform source, Vector3 targetPosition, float duration = 0.88f, float size = 132f)
    {
        if (source == null || overlay == null)
        {
            yield break;
        }

        Vector3 travel = targetPosition - source.position;
        if (travel.sqrMagnitude < 0.001f)
        {
            yield break;
        }

        yield return SkillVfxMotion.PlayHammerMotion(
            overlay,
            source.position,
            travel.normalized,
            size,
            duration);
    }

    private IEnumerator PlaySkillSprite(CharacterData character, Vector3 from, Vector3 to, float duration, float scale)
    {
        if (overlay == null)
        {
            yield break;
        }

        Sprite sprite = SkillVfxMotion.GetRepresentativeSprite(SkillVfxMotion.GetVfxFolder(character));
        if (sprite == null)
        {
            yield break;
        }

        var effectObject = new GameObject("RosterSkillVfx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        effectObject.transform.SetParent(overlay, false);
        var effect = effectObject.GetComponent<Image>();
        effect.sprite = sprite;
        effect.preserveAspect = true;
        effect.raycastTarget = false;
        var effectRect = effect.rectTransform;
        effectRect.sizeDelta = Vector2.one * (118f * scale);
        effectRect.position = from;
        effect.transform.SetAsLastSibling();

        Image pieceFlash = CreatePieceFlash(character, from, scale);
        float baseAngle = SkillVfxMotion.GetTravelAngle(character, to - from);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            effectRect.position = Vector3.Lerp(from, to, eased);
            effectRect.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.08f, Mathf.Sin(progress * Mathf.PI));
            effectRect.localRotation = Quaternion.Euler(0f, 0f, baseAngle + Mathf.Lerp(-14f, 18f, eased));
            Color effectColor = Color.white;
            effectColor.a = progress < 0.12f ? progress / 0.12f : 1f - Mathf.InverseLerp(0.70f, 1f, progress);
            effect.color = effectColor;

            if (pieceFlash != null)
            {
                float flashScale = Mathf.Lerp(0.9f, 1.20f, Mathf.Sin(progress * Mathf.PI));
                pieceFlash.rectTransform.localScale = Vector3.one * flashScale;
                Color flashColor = new Color(1f, 0.92f, 0.56f, 1f - progress);
                pieceFlash.color = flashColor;
            }
            yield return null;
        }

        if (effectObject != null)
        {
            Destroy(effectObject);
        }
        if (pieceFlash != null)
        {
            Destroy(pieceFlash.gameObject);
        }
    }

    private Image CreatePieceFlash(CharacterData character, Vector3 position, float scale)
    {
        if (character == null || character.icon == null)
        {
            return null;
        }

        var flashObject = new GameObject("RosterPieceFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flashObject.transform.SetParent(overlay, false);
        var flash = flashObject.GetComponent<Image>();
        flash.sprite = character.icon;
        flash.preserveAspect = true;
        flash.raycastTarget = false;
        flash.color = new Color(1f, 0.92f, 0.56f, 0.9f);
        flash.rectTransform.sizeDelta = Vector2.one * (76f * scale);
        flash.rectTransform.position = position;
        flash.transform.SetAsLastSibling();
        return flash;
    }

    private void EnsureOverlay()
    {
        if (overlay != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var overlayObject = new GameObject("RosterVfxOverlay", typeof(RectTransform));
        overlayObject.transform.SetParent(canvas.transform, false);
        overlay = overlayObject.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        overlay.SetAsLastSibling();
    }

}
