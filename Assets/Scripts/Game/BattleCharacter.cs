using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleCharacter : MonoBehaviour
{
    public CharacterData data;
    public Vector2Int gridPos;
    public bool isAlly;
    public int level;

    public int currentHP;
    public int attack;
    public int defense;
    public bool isDead = false;

    [Header("UI")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image hpBar;
    [SerializeField] private TextMeshProUGUI directionText;

    private static int allyCounter = 0; // 味方インスタンス番号管理
    private static int enemyCounter = 0; // 敵インスタンス番号管理
    private int instanceId;
    private int stunCounter = 0;
    private Color baseBackgroundColor = Color.white;
    private Vector3 baseScale = Vector3.one;
    private Coroutine visualEffectRoutine;

    public int InstanceId => instanceId;

public void Init(CharacterData data, Vector2Int pos, bool ally, int level = 1)
{
    this.data = data;
    this.gridPos = pos;
    this.isAlly = ally;
    this.level = level;

    currentHP = data.GetMaxHP(level);
    attack    = data.GetAttack(level);
    defense   = data.GetDefense(level);

Sprite displayIcon = ally || data.enemyIcon == null ? data.icon : data.enemyIcon;
bool usesIcon = displayIcon != null;
if (background != null)
{
    if (usesIcon)
    {
        background.sprite = displayIcon;
        background.type = Image.Type.Simple;
        background.preserveAspect = true;
        background.color = ally ? new Color(0.28f, 0.58f, 1f) : new Color(1f, 0.28f, 0.22f);
    }
    else
        {
            background.color = ally ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.7f, 0.7f);
        }
    }

    CaptureBaseVisualState();
    ConfigureBattleLabels(usesIcon, ally);

    if (nameText != null)
    {
        nameText.gameObject.SetActive(!usesIcon);
        nameText.text = usesIcon ? "" : data.characterName;
    }

    if (ally)
    {
        instanceId = ++allyCounter;
    }
    else
    {
        instanceId = ++enemyCounter;
    }

    if (levelText != null)
        levelText.text = usesIcon ? $"Lv{level}" : $"Lv.{level} #{instanceId}";

    UpdateHPBar();
    UpdateDirection(Vector2Int.down);
}

    public string DisplayName
    {
        get
        {
            return (isAlly ? "味方" : "敵") + data.characterName + $"#{instanceId}";
        }
    }

    public void TakeDamage(int dmg, BattleManager bm, BattleCharacter attacker = null, bool ignoreDefense = false, bool isBasicAttack = false)
    {
        if (isDead) return;

        int originalDmg = dmg;
        // Defense-based reduction (unless Armor skill or ignoreDefense is true)
        if (!ignoreDefense && (data == null || data.skillType != SkillType.Armor)) {
            float effectiveDefense = Mathf.Max(0, defense / 4f);
            int reducedByDefense = Mathf.FloorToInt(dmg * 100f / (100f + effectiveDefense));
            dmg = Mathf.Max(1, reducedByDefense);
        }
        // Handle Armor skill: after defense/ignoreDefense check, reduce incoming damage
        if (data != null && data.skillType == SkillType.Armor)
        {
            int reductionPerLevel = 2;
            SkillData skillData = SkillCatalog.Get(SkillType.Armor);
            if (skillData?.effects != null)
            {
                foreach (var effect in skillData.effects)
                {
                    if (effect.effectType == SkillEffectType.DamageReduction)
                    {
                        reductionPerLevel = effect.value;
                        break;
                    }
                }
            }

            int reduction = level * Mathf.Max(0, reductionPerLevel);
            int reduced = Mathf.Max(1, dmg - reduction);
            bm.AddLog($"{DisplayName} のアーマーでダメージが {dmg} → {reduced} に軽減！（Lv{level}×{reductionPerLevel}={reduction} 減少）");
            dmg = reduced;
        }

        bm.AddLog($"{DisplayName} が {originalDmg} → {dmg} ダメージ (DEF {defense})");

        currentHP -= dmg;
        UpdateHPBar();
        bm?.PlayDamageVfx(this, dmg);

        if (currentHP <= 0)
        {
            bm.HandleDeath(this);
        }

        // Counter logic after taking damage
        if (data != null && isBasicAttack)
        {
            // Counter: 30% chance to reflect 30% of taken damage to attacker
            if (data.skillType == SkillType.Counter && attacker != null && !attacker.isDead)
            {
                GetCounterSettings(SkillType.Counter, 30, 0.3f, out int chance, out float reflectPercent);
                float roll = Random.value;
                if (roll < chance / 100f && dmg > 0)
                {
                    int reflect = Mathf.RoundToInt(dmg * reflectPercent);
                    if (reflect > 0)
                    {
                        bm.AddLog($"{DisplayName} のカウンター発動！{attacker.DisplayName} に {reflect} ダメージを反射！");
                        attacker.TakeDamage(reflect, bm, this, ignoreDefense: false, isBasicAttack: false);
                        attacker.UpdateHPBar();
                    }
                }
            }
            // AreaCounter: 20% chance to reflect 20% of taken damage to all adjacent enemies
            else if (data.skillType == SkillType.AreaCounter && dmg > 0)
            {
                GetCounterSettings(SkillType.AreaCounter, 20, 0.2f, out int chance, out float reflectPercent);
                float roll = Random.value;
                if (roll < chance / 100f)
                {
                    int reflect = Mathf.RoundToInt(dmg * reflectPercent);
                    if (reflect > 0)
                    {
                        List<BattleCharacter> targets = new List<BattleCharacter>();
                        Vector2Int[] dirs = new Vector2Int[]
                        {
                            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
                        };
                        foreach (var dir in dirs)
                        {
                            Vector2Int checkPos = gridPos + dir;
                            if (bm.gridMap.TryGetValue(checkPos, out var neighbor))
                            {
                                if (neighbor != null && !neighbor.isDead && neighbor.isAlly != this.isAlly)
                                {
                                    targets.Add(neighbor);
                                }
                            }
                        }
                        if (targets.Count > 0)
                        {
                            bm.AddLog($"{DisplayName} の範囲カウンター発動！周囲の敵に {reflect} ダメージを反射！");
                            foreach (var target in targets)
                            {
                                target.TakeDamage(reflect, bm, this, ignoreDefense: false, isBasicAttack: false);
                                target.UpdateHPBar();
                            }
                        }
                    }
                }
            }
        }
    }

    private void GetCounterSettings(SkillType skillType, int defaultChance, float defaultReflect, out int chance, out float reflectPercent)
    {
        chance = defaultChance;
        reflectPercent = defaultReflect;

        SkillData skillData = SkillCatalog.Get(skillType);
        if (skillData == null)
        {
            return;
        }

        if (skillData.chanceOverride >= 0)
        {
            chance = skillData.chanceOverride;
        }

        if (skillData.effects != null)
        {
            foreach (var effect in skillData.effects)
            {
                if (effect.effectType == SkillEffectType.Counter || effect.effectType == SkillEffectType.AreaCounter)
                {
                    if (effect.value > 0)
                    {
                        reflectPercent = effect.value / 100f;
                    }
                    return;
                }
            }
        }
    }

    public void UpdateHPBar()
    {
        if (hpBar != null)
            hpBar.fillAmount = Mathf.Clamp01((float)currentHP / data.GetMaxHP(level));
    }

public void UpdateDirection(Vector2Int dir)
{
    if (dir.x != 0) dir.x = dir.x > 0 ? 1 : -1;
    if (dir.y != 0) dir.y = dir.y > 0 ? 1 : -1;

    string arrow = "";
    if (dir == Vector2Int.up) arrow = "↓";
    else if (dir == Vector2Int.down) arrow = "↑";
    else if (dir == Vector2Int.left) arrow = "←";
    else if (dir == Vector2Int.right) arrow = "→";
    else if (dir.x == 1 && dir.y == 1) arrow = "↘";
    else if (dir.x == -1 && dir.y == 1) arrow = "↙";
    else if (dir.x == 1 && dir.y == -1) arrow = "↗";
    else if (dir.x == -1 && dir.y == -1) arrow = "↖";

    if (directionText != null)
        directionText.text = arrow;
}

private void ConfigureBattleLabels(bool usesIcon, bool ally)
{
    Color teamTextColor = new Color(0.08f, 0.05f, 0.03f, 1f);

    if (hpBar != null)
    {
        hpBar.color = ally ? new Color(0.1f, 0.9f, 0.28f, 1f) : new Color(1f, 0.22f, 0.16f, 1f);
        var hpRect = hpBar.rectTransform;
        hpRect.anchorMin = new Vector2(0.5f, 0f);
        hpRect.anchorMax = new Vector2(0.5f, 0f);
        hpRect.pivot = new Vector2(0.5f, 0f);
        hpRect.anchoredPosition = new Vector2(0f, 1f);
            hpRect.sizeDelta = usesIcon ? new Vector2(52f, 10f) : new Vector2(50f, 10f);
    }

    if (levelText != null)
    {
        levelText.gameObject.SetActive(true);
        levelText.color = usesIcon ? teamTextColor : new Color(0.13f, 0.08f, 0.04f, 0.95f);
        levelText.fontSize = usesIcon ? 14f : 10f;
        levelText.fontStyle = FontStyles.Bold;
        levelText.alignment = TextAlignmentOptions.TopRight;
        levelText.enableAutoSizing = false;
        var rect = levelText.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = usesIcon ? new Vector2(-2f, -1f) : new Vector2(-12f, -10f);
            rect.sizeDelta = usesIcon ? new Vector2(42f, 18f) : new Vector2(50f, 20f);
    }

    if (directionText != null)
    {
        directionText.gameObject.SetActive(true);
        directionText.color = usesIcon ? teamTextColor : new Color(0.13f, 0.08f, 0.04f, 0.95f);
        directionText.fontSize = usesIcon ? 20f : 10f;
        directionText.fontStyle = FontStyles.Bold;
        directionText.alignment = TextAlignmentOptions.TopLeft;
        directionText.enableAutoSizing = false;
        var rect = directionText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = usesIcon ? new Vector2(3f, -1f) : new Vector2(3f, -10f);
            rect.sizeDelta = usesIcon ? new Vector2(26f, 22f) : new Vector2(10f, 10f);
    }
}

    public IEnumerator AttackEffect()
    {
        if (background == null) yield break;

        background.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        RestoreBaseVisualState();
    }

    public bool TryUseSkill(BattleCharacter target, BattleManager bm)
    {
        bool used = SkillExecutor.TryExecute(this, target, bm);
        if (used)
        {
            PlayCastEffect(new Color(0.45f, 0.85f, 1f));
            bm?.ShowFloatingText(this, "SKILL", new Color(0.62f, 0.92f, 1f));
        }
        return used;
    }

    public int GetEffectiveAttack(BattleManager bm)
    {
        float buff = 1f;
        int uniqueCount = 0;

        if (data.skillType == SkillType.NumberPassive)
        {
            var result = CalculateNumberPassiveBuff(bm);
            uniqueCount = result.uniqueCount;
            buff = result.buff;
        }

        if (buff > 1f && bm != null)
        {
            int percent = Mathf.RoundToInt((buff - 1f) * 100);
            bm.AddLog($"{DisplayName} の攻撃力バフ: ユニークタイプ数 {uniqueCount}, バフ倍率 {percent}%");
        }

        return Mathf.RoundToInt(attack * buff);
    }

    public bool IsStunned() => stunCounter > 0;

    public void TickStun()
    {
        if (stunCounter > 0) stunCounter--;
    }

    public void ApplyStun(int turns)
    {
        stunCounter = Mathf.Max(stunCounter, turns);
    }

    public void PerformAttack(BattleCharacter target, BattleManager bm, float powerMultiplier = 1f, string logMessage = null)
    {
        if (target == null || isDead) return;

        bm?.PlayAttackVfx(this, target, powerMultiplier > 1f || !string.IsNullOrEmpty(logMessage));

        int dmg = Mathf.RoundToInt(GetEffectiveAttack(bm) * powerMultiplier);
        if (string.IsNullOrEmpty(logMessage))
        {
            bm.AddLog($"{DisplayName} は通常攻撃を行った！ {dmg} ダメージ");
        }
        else
        {
            bm.AddLog(string.Format(logMessage, dmg));
        }
        target.TakeDamage(dmg, bm, this, false, isBasicAttack: true);
        ApplyNumberPassiveAttackEffect(target, bm);
        UpdateDirection(target.gridPos - this.gridPos);
    }

    public void PlayCastEffect(Color color)
    {
        StartVisualEffect(CastEffectRoutine(color));
    }

    public void PlayHitEffect(Color color)
    {
        StartVisualEffect(HitEffectRoutine(color));
    }

    private void CaptureBaseVisualState()
    {
        if (background != null) baseBackgroundColor = background.color;
        var rect = transform as RectTransform;
        baseScale = rect != null ? rect.localScale : transform.localScale;
    }

    private void RestoreBaseVisualState()
    {
        if (background != null) background.color = baseBackgroundColor;
        var rect = transform as RectTransform;
        if (rect != null) rect.localScale = baseScale;
        else transform.localScale = baseScale;
    }

    private void StartVisualEffect(IEnumerator routine)
    {
        if (visualEffectRoutine != null)
        {
            StopCoroutine(visualEffectRoutine);
        }

        RestoreBaseVisualState();
        visualEffectRoutine = StartCoroutine(routine);
    }

    private IEnumerator CastEffectRoutine(Color flashColor)
    {
        var rect = transform as RectTransform;
        Vector3 originalScale = baseScale;

        float duration = 0.18f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            if (rect != null) rect.localScale = originalScale * (1f + 0.18f * t);
            if (background != null) background.color = Color.Lerp(baseBackgroundColor, flashColor, t);
            yield return null;
        }

        RestoreBaseVisualState();
        visualEffectRoutine = null;
    }

    private IEnumerator HitEffectRoutine(Color flashColor)
    {
        if (background == null) yield break;

        background.color = flashColor;
        yield return new WaitForSeconds(0.08f);
        RestoreBaseVisualState();
        visualEffectRoutine = null;
    }

    public void SetLevelForDebug(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        currentHP = data.GetMaxHP(level);
        attack = data.GetAttack(level);
        defense = data.GetDefense(level);
if (levelText != null)
    levelText.text = data != null && data.icon != null ? $"Lv{level}" : $"Lv.{level} #{instanceId}";
UpdateHPBar();
    }

    // Helper method to calculate NumberPassive buff and unique count
    private (int uniqueCount, int countOtherOnes, float buff) CalculateNumberPassiveBuff(BattleManager bm)
    {
        HashSet<string> uniqueNumbers = new HashSet<string>();

        if (bm == null || data == null)
        {
            return (0, 0, 1f);
        }

        foreach (var kvp in bm.gridMap)
        {
            var character = kvp.Value;
            if (character == null || character.isDead || character.data == null) continue;
            if (character.isAlly != isAlly) continue;
            if (!IsNumberCategory(character.data.category)) continue;

            uniqueNumbers.Add(character.data.characterName);
        }

        int uniqueCount = uniqueNumbers.Count;
        float perTypeBonus = 0.03f;
        float maxBonus = 0.09f;

        if (data.category == CharacterCategory.Number2)
        {
            perTypeBonus = 0.02f;
            maxBonus = 0.06f;
        }
        else if (data.category == CharacterCategory.Number3)
        {
            perTypeBonus = 0.06f;
            maxBonus = 0.18f;
        }

        float buff = 1f + Mathf.Min(uniqueCount * perTypeBonus, maxBonus);
        return (uniqueCount, 0, buff);
    }

    private static bool IsNumberCategory(CharacterCategory category)
    {
        return category == CharacterCategory.Number1 ||
               category == CharacterCategory.Number2 ||
               category == CharacterCategory.Number3;
    }

    private void ApplyNumberPassiveAttackEffect(BattleCharacter target, BattleManager bm)
    {
        if (bm == null || target == null || data == null || data.skillType != SkillType.NumberPassive)
        {
            return;
        }

        if (data.category == CharacterCategory.Number2)
        {
            ApplyNumber2Splash(target, bm);
        }
        else if (data.category == CharacterCategory.Number3)
        {
            ApplyNumber3Judgement(target, bm);
        }
    }

    private void ApplyNumber2Splash(BattleCharacter target, BattleManager bm)
    {
        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(GetEffectiveAttack(bm) * 0.25f));
        bool hit = false;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Vector2Int pos = target.gridPos + new Vector2Int(dx, dy);
                if (!bm.gridMap.TryGetValue(pos, out BattleCharacter splashTarget)) continue;
                if (splashTarget == null || splashTarget.isDead || splashTarget.isAlly == isAlly) continue;

                splashTarget.TakeDamage(splashDamage, bm, this, false, isBasicAttack: false);
                hit = true;
            }
        }

        if (hit)
        {
            bm.AddLog($"{DisplayName} の中位数字効果！ 周囲に {splashDamage} ダメージ");
        }
    }

    private void ApplyNumber3Judgement(BattleCharacter target, BattleManager bm)
    {
        if (Random.value > 0.35f)
        {
            return;
        }

        BattleCharacter judgementTarget = null;
        foreach (var kvp in bm.gridMap)
        {
            BattleCharacter candidate = kvp.Value;
            if (candidate == null || candidate.isDead || candidate.isAlly == isAlly || candidate == target)
            {
                continue;
            }

            if (judgementTarget == null || candidate.currentHP < judgementTarget.currentHP)
            {
                judgementTarget = candidate;
            }
        }

        if (judgementTarget == null)
        {
            return;
        }

        int judgementDamage = Mathf.Max(1, Mathf.RoundToInt(GetEffectiveAttack(bm) * 0.6f));
        bm.AddLog($"{DisplayName} の上位数字効果！ {judgementTarget.DisplayName} に {judgementDamage} ダメージ");
        judgementTarget.TakeDamage(judgementDamage, bm, this, false, isBasicAttack: false);
    }
}
