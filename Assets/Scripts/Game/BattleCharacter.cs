using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleCharacter : MonoBehaviour
{
    public CharacterData data;
    public CharacterData OriginalData { get; private set; }
    public bool HasTransformed { get; private set; }
    private CharacterData copiedForm;
    public Vector2Int gridPos;
    public bool isAlly;
    private readonly BattleCharacterState state = new();
    public int level { get => state.Level; set => state.Level = value; }
    public int currentHP { get => state.CurrentHP; set => state.CurrentHP = value; }
    public int attack { get => state.Attack; set => state.Attack = value; }
    public int defense { get => state.Defense; set => state.Defense = value; }
    public int maxHP => state.MaxHP;
    public bool isDead { get => state.IsDead; set => state.IsDead = value; }
    public bool WindBoost { get => state.WindBoost; set => state.WindBoost = value; }
    public bool WindSlow { get => state.WindSlow; set => state.WindSlow = value; }
    public void ApplyWind(bool boost,int turns)
    {if(boost)state.WindBoostTurns=Mathf.Max(state.WindBoostTurns,turns);else state.WindSlowTurns=Mathf.Max(state.WindSlowTurns,turns);}
    public void TickWind(){state.WindBoostTurns=Mathf.Max(0,state.WindBoostTurns-1);state.WindSlowTurns=Mathf.Max(0,state.WindSlowTurns-1);}
    public float SkillEffectMultiplier=>isAlly&&!HasTransformed?CharacterInvestment.Effect(OriginalData):1;
    public int MovementModifier => (WindBoost ? 1 : 0) - (WindSlow ? 1 : 0);
    public IronForm IronForm { get => state.IronForm; set => state.IronForm = value; }
    public int AttackBuffPercent => state.AttackBuffPercent;
    public void ApplyAttackBuff(BattleAttackBuff source, int percent, int turns) => state.ApplyAttackBuff(source, percent, turns);
    public void TickAttackBuffs() => state.TickAttackBuffs();
    public bool CoalAwake { get => state.CoalAwake; set => state.CoalAwake = value; }
    public bool CocoonShield { get => state.CocoonShield; set => state.CocoonShield = value; }
    public int DiceDefensePercent { get => state.DiceDefensePercent; set => state.DiceDefensePercent = value; }
    public int OxDefensePercent => state.OxDefensePercent;
    public bool ApplyOxDefense(int turn) => state.ApplyOxDefense(turn);
    public bool IsGenerated { get; set; }
    public BattleCharacter Summoner { get; set; }
    public int LineageId { get; set; }
    public int EligibleTurn { get; set; }
    public int RecordToken { get; set; }
    public int CocoonRecordToken { get; set; }
    public void SetBattleVitals(int hp, int current, int attack, int defense)
    { state.SetVitals(hp, current, attack, defense); UpdateHPBar(); }

    [Header("UI")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image hpBar;
    [SerializeField] private TextMeshProUGUI directionText;

    private static int allyCounter = 0; // 味方インスタンス番号管理
    private static int enemyCounter = 0; // 敵インスタンス番号管理
    private int instanceId;
    private Color baseBackgroundColor = Color.white;
    private static readonly Color battleInfoTextColor = new Color(1f, 0.96f, 0.78f, 1f);
    private static readonly Color battleInfoShadowColor = new Color(0f, 0f, 0f, 0.85f);
    private Vector3 baseScale = Vector3.one;
    private Coroutine visualEffectRoutine;

    public int InstanceId => instanceId;

public void Init(CharacterData data, Vector2Int pos, bool ally, int level = 1)
{
    this.data = data;
    OriginalData = data;
    HasTransformed = false;
    this.gridPos = pos;
    this.isAlly = ally;
    this.level = level;

    state.Initialize(data, level, ally);
    LineageId = GetInstanceID();
    IsGenerated = false; Summoner = null; EligibleTurn = 0;

bool usesEnemyIcon = !ally && data.enemyIcon != null;
Sprite displayIcon = usesEnemyIcon ? data.enemyIcon : data.icon;
bool usesIcon = displayIcon != null;
if (background != null)
{
    if (usesIcon)
    {
        background.sprite = displayIcon;
        background.type = Image.Type.Simple;
        background.preserveAspect = true;
        background.color = Color.white;
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
    {
        levelText.text = usesIcon ? $"Lv{level}" : $"Lv.{level} #{instanceId}";
    }

    UpdateHPBar();
    UpdateDirection(Vector2Int.down);
    if (usesIcon && background != null)
        PieceMotifPresentation.Attach(background.rectTransform, data, false);
}

    public bool CopyBattleForm(BattleCharacter target)
    {
        if (HasTransformed || !SupportSkillExecutor.CanCopy(this, target)) return false;
        float copyEffect=SkillEffectMultiplier;
        HasTransformed = true;
        copiedForm = Instantiate(target.data);
        copiedForm.hideFlags = HideFlags.HideAndDontSave;
        data = copiedForm;
        attack = Mathf.RoundToInt(target.attack*copyEffect);
        defense = target.defense;
        state.SetNumberPassiveAttackBonus(0);
        Sprite sprite = !isAlly && data.enemyIcon != null ? data.enemyIcon : data.icon;
        bool usesIcon = sprite != null;
        if (background != null)
        {
            background.sprite = sprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = usesIcon;
            background.color = usesIcon ? Color.white
                : isAlly ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.7f, 0.7f);
        }
        if (nameText != null)
        {
            nameText.gameObject.SetActive(!usesIcon);
            nameText.text = usesIcon ? "" : data.characterName;
        }
        ConfigureBattleLabels(usesIcon, isAlly);
        CaptureBaseVisualState();
        UpdateHPBar();
        return true;
    }

    private void OnDestroy()
    {
        if (copiedForm == null) return;
        if (Application.isPlaying) Destroy(copiedForm);
        else DestroyImmediate(copiedForm);
    }

    public void PlayMotifArrival()
    {
        if (background != null)
            background.GetComponent<PieceMotifPresentation>()?.PlayArrival();
        if (hpBar != null) hpBar.transform.SetAsLastSibling();
        if (levelText != null) levelText.transform.SetAsLastSibling();
        if (directionText != null) directionText.transform.SetAsLastSibling();
    }

    public string DisplayName
    {
        get
        {
            return (isAlly ? "味方" : "敵") + data.characterName + $"#{instanceId}";
        }
    }

    public void TakeDamage(
        int dmg,
        BattleManager bm,
        BattleCharacter attacker = null,
        bool ignoreDefense = false,
        bool isBasicAttack = false,
        string actionLabel = null,
        BattleDamageOrigin origin = BattleDamageOrigin.Direct)
    {
        if (isDead || dmg <= 0) return;
        int incomingDamage = dmg;
        if (origin == BattleDamageOrigin.Direct && !isBasicAttack && attacker != null
            && attacker.isAlly != isAlly && BattleDamageRules.StopsProjectile(this)
            && FusionReactionRules.IsProjectile(attacker.data.skillType))
        {
            dmg = BattleDamageRules.ReduceByWall(dmg, data.skillPower);
            bm?.PlayDefensiveVfx(this, SkillType.Wall);
            if (dmg == 0)
            {
                bm?.RecordDamage(this, attacker, 0, incomingDamage);
                bm?.AddLog($"{DisplayName} が遠距離攻撃を遮った！");
                return;
            }
        }
        if (origin == BattleDamageOrigin.Direct && attacker != null && attacker.isAlly != isAlly
            && FusionReactionRules.TryProtect(this, attacker, bm, !isBasicAttack, dmg)) return;

        bool canMitigate = origin is not (BattleDamageOrigin.Transferred or BattleDamageOrigin.Returned);

        bool armorReducedDamage = false;
        // Defense-based reduction (unless Armor skill or ignoreDefense is true)
        if (canMitigate && !ignoreDefense && (data == null || data.skillType != SkillType.Armor)) {
            float effectiveDefense = Mathf.Max(0, defense * (1f + (DiceDefensePercent + OxDefensePercent) / 100f) / 4f);
            int reducedByDefense = Mathf.FloorToInt(dmg * 100f / (100f + effectiveDefense));
            dmg = Mathf.Max(1, reducedByDefense);
        }
        // Handle Armor skill: after defense/ignoreDefense check, reduce incoming damage
        if (canMitigate && data != null && data.skillType == SkillType.Armor)
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
            bm?.PlayDefensiveVfx(this, SkillType.Armor);
            armorReducedDamage = reduced < dmg;
            dmg = reduced;
        }

        if (canMitigate && data != null && BattleDamageRules.HasShield(data.skillType))
        {
            dmg = BattleDamageRules.ReduceByShield(dmg,SkillEffectMultiplier);
            bm?.PlayDefensiveVfx(this, SkillType.Shield);
        }

        if (canMitigate && data != null
            && data.skillType == SkillType.IronAdapt && IronForm == IronForm.Shield)
            dmg = Mathf.Max(1, Mathf.FloorToInt(dmg * (1-Mathf.Min(.5f,.25f*SkillEffectMultiplier))));

        if (canMitigate && data != null && data.skillType == SkillType.Coal && !CoalAwake)
            dmg = Mathf.Max(1, Mathf.FloorToInt(dmg * 0.5f));

        int preventedDamage = Mathf.Max(0, incomingDamage - dmg);
        if (canMitigate && bm != null) dmg = bm.ApplyPlayerSkillGuard(this, dmg);
        if (origin == BattleDamageOrigin.Direct && bm != null)
        {
            bool projectileHit = !isBasicAttack && attacker != null && attacker.isAlly != isAlly
                && FusionReactionRules.IsProjectile(attacker.data.skillType);
            BattleCharacter protector = BattleDamageRules.FindProtector(this, bm.gridMap.Values, projectileHit);
            bool projectileGuard = protector != null && protector.data.skillType == SkillType.ProjectileGuard;
            int shared = protector == null ? 0 : BattleDamageRules.GuardShare(dmg, protector.currentHP,
                protector.GetEffectiveSkillPower(), projectileGuard);
            if (shared > 0)
            {
                dmg -= shared;
                int loss = BattleDamageRules.GuardLoss(shared, projectileGuard);
                bm.RecordGuard(protector, shared, shared - loss);
                string effect = projectileGuard ? "GuardShield" : "GuardShell";
                bm.PlaySupportBurst(this, effect, .6f, .8f);
                bm.PlaySupportBurst(protector, effect, .6f, .7f);
                protector.TakeDamage(loss, bm, attacker, ignoreDefense: true,
                    actionLabel: $"{protector.DisplayName} が {DisplayName} をかばった！",
                    origin: BattleDamageOrigin.Transferred);
                if (projectileGuard)
                {
                    // Per-recipient turn stamps also prevent several oxen from stacking a single hit.
                    int turn = bm.CurrentBattleTurn;
                    if (protector.ApplyOxDefense(turn))
                        bm.AddLog($"{protector.DisplayName} の防御が強化（+{protector.OxDefensePercent}%）");
                    if (ApplyOxDefense(turn))
                        bm.AddLog($"{DisplayName} の防御が強化（+{OxDefensePercent}%）");
                }
            }
        }

        if (!string.IsNullOrEmpty(actionLabel))
        {
            string mitigation = armorReducedDamage ? "（鎧で軽減）" : string.Empty;
            bm?.AddLog($"{actionLabel} {dmg} ダメージ{mitigation}");
        }

        int actualLoss = Mathf.Min(Mathf.Max(0, currentHP), dmg);
        bm?.RecordDamage(this, attacker, actualLoss, preventedDamage);
        currentHP -= dmg;
        UpdateHPBar();
        bm?.PlayDamageVfx(this, dmg);

        FusionReactionRules.AfterDamage(this, attacker, actualLoss, origin, bm);

        if (currentHP <= 0)
        {
            if (bm != null) bm.HandleDeath(this);
            else isDead = true;
        }

        // Counter logic after taking damage
        if (data != null && isBasicAttack && origin == BattleDamageOrigin.Direct)
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
                        bm?.PlayDefensiveVfx(this, SkillType.Counter);
                        attacker.TakeDamage(reflect, bm, this, ignoreDefense: false, isBasicAttack: false, origin: BattleDamageOrigin.Reflected);
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
                            bm?.PlayDefensiveVfx(this, SkillType.AreaCounter);
                            foreach (var target in targets)
                            {
                                target.TakeDamage(reflect, bm, this, ignoreDefense: false, isBasicAttack: false, origin: BattleDamageOrigin.Reflected);
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
        if (skillData != null && skillData.chanceOverride >= 0)
        {
            chance = skillData.chanceOverride;
        }

        if (skillData != null && skillData.effects != null)
        {
            foreach (var effect in skillData.effects)
            {
                if (effect.effectType == SkillEffectType.Counter || effect.effectType == SkillEffectType.AreaCounter)
                {
                    if (effect.value > 0)
                    {
                        reflectPercent = effect.value / 100f;
                    }
                    break;
                }
            }
        }

        chance=CharacterInvestment.AdjustChance(OriginalData,skillType,chance,isAlly);
        reflectPercent=Mathf.Min(.8f,reflectPercent*SkillEffectMultiplier);
        if (SkillExecutor.ForceSkillActivationForVfxReview)
        {
            chance = 100;
        }
    }

    public void UpdateHPBar()
    {
        if (hpBar != null)
            hpBar.fillAmount = Mathf.Clamp01((float)currentHP / Mathf.Max(1, maxHP));
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
    {
        directionText.text = arrow;
    }
}

private void ConfigureBattleLabels(bool usesIcon, bool ally)
{

    if (hpBar != null)
    {
        hpBar.gameObject.SetActive(true);
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
        StyleBattleInfoText(levelText, usesIcon ? 14f : 12f, TextAlignmentOptions.TopRight);
        var rect = levelText.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = usesIcon ? new Vector2(-2f, -1f) : new Vector2(-12f, -10f);
            rect.sizeDelta = usesIcon ? new Vector2(48f, 20f) : new Vector2(56f, 22f);
    }

    if (directionText != null)
    {
        directionText.gameObject.SetActive(true);
        StyleBattleInfoText(directionText, usesIcon ? 22f : 14f, TextAlignmentOptions.TopLeft);
        var rect = directionText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = usesIcon ? new Vector2(3f, -1f) : new Vector2(3f, -10f);
            rect.sizeDelta = usesIcon ? new Vector2(30f, 24f) : new Vector2(22f, 18f);
    }
}

private static void StyleBattleInfoText(TextMeshProUGUI text, float fontSize, TextAlignmentOptions alignment)
{
    if (text == null) return;

    UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
    text.color = battleInfoTextColor;
    text.fontSize = fontSize;
    text.fontStyle = FontStyles.Bold;
    text.alignment = alignment;
    text.enableAutoSizing = false;
    text.raycastTarget = false;
    text.transform.SetAsLastSibling();

    var shadow = text.GetComponent<Shadow>();
    if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
    shadow.effectColor = battleInfoShadowColor;
    shadow.effectDistance = new Vector2(1f, -1f);

    text.SetAllDirty();
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
        int attack = state.GetEffectiveAttack();
        if (bm != null) attack = bm.ApplyPlayerSkillAttack(this, attack);
        return attack;
    }

    public float GetEffectiveSkillPower()
    {
        float power = data != null ? data.skillPower : 1f;
        if(data?.skillType==SkillType.WaterHeal)return CharacterInvestment.WaterRecoveryRatio(data,SkillEffectMultiplier);
        return power*SkillEffectMultiplier;
    }

    public void SetNumberPassiveAttackBonus(int percent)
    {
        state.SetNumberPassiveAttackBonus(percent);
    }

    public bool IsStunned() => state.StunTurns > 0;

    public void TickStun()
    {
        state.TickStun();
    }

    public void ApplyStun(int turns)
    {
        state.ApplyStun(turns);
    }

    public void PerformAttack(BattleCharacter target, BattleManager bm, float powerMultiplier = 1f, string logMessage = null)
    {
        if (target == null || isDead) return;

        bm?.PlayAttackVfx(this, target, powerMultiplier > 1f || !string.IsNullOrEmpty(logMessage));

        int dmg = Mathf.RoundToInt(GetEffectiveAttack(bm) * powerMultiplier);
        string actionLabel = string.IsNullOrEmpty(logMessage)
            ? $"{DisplayName} の攻撃 → {target.DisplayName}"
            : $"{logMessage.Replace("{0} ダメージ", string.Empty).Trim()} → {target.DisplayName}";
        target.TakeDamage(dmg, bm, this, false, isBasicAttack: true, actionLabel: actionLabel);
        UpdateDirection(target.gridPos - this.gridPos);
    }

    public void PlayCastEffect(Color color)
    {
        StartVisualEffect(CastEffectRoutine(color));
    }

    public void PlayHitEffect(Color color, bool emphasize = false)
    {
        StartVisualEffect(HitEffectRoutine(color, emphasize));
    }

    // スキルの着弾先を、素材が届く前から短く示す。実際のダメージ時には
    // PlayHitEffect がこの演出を引き継いで、より強いヒット反応へ切り替える。
    public void PlaySkillTargetCue(Color color)
    {
        StartVisualEffect(SkillTargetCueRoutine(color));
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

    private IEnumerator HitEffectRoutine(Color flashColor, bool emphasize)
    {
        if (background == null) yield break;

        var rect = transform as RectTransform;
        if (!emphasize)
        {
            background.color = flashColor;
            yield return new WaitForSeconds(0.08f);
            RestoreBaseVisualState();
            visualEffectRoutine = null;
            yield break;
        }

        // スキル命中は半透明と不透明を繰り返す。火球を含む全ダメージ系スキルの
        // 着弾先を見失わず、短いヒットストップも視覚的に伝える。
        const float duration = 0.24f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float blink = Mathf.PingPong(progress * 3f, 1f);
            Color current = Color.Lerp(baseBackgroundColor, flashColor, 0.9f);
            current.a = baseBackgroundColor.a * Mathf.Lerp(0.42f, 1f, blink);
            background.color = current;
            if (rect != null)
            {
                rect.localScale = baseScale * Mathf.Lerp(1.05f, 1.14f, blink);
            }
            yield return null;
        }

        RestoreBaseVisualState();
        visualEffectRoutine = null;
    }

    private IEnumerator SkillTargetCueRoutine(Color cueColor)
    {
        if (background == null) yield break;

        var rect = transform as RectTransform;
        const float duration = 0.18f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float blink = Mathf.PingPong(progress * 2f, 1f);
            Color current = Color.Lerp(baseBackgroundColor, cueColor, 0.46f);
            current.a = baseBackgroundColor.a * Mathf.Lerp(0.48f, 1f, blink);
            background.color = current;
            if (rect != null)
            {
                rect.localScale = baseScale * Mathf.Lerp(1.01f, 1.06f, blink);
            }
            yield return null;
        }

        RestoreBaseVisualState();
        visualEffectRoutine = null;
    }

    public void SetLevelForDebug(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        state.Initialize(data, level, isAlly);
if (levelText != null)
{
    levelText.text = data != null && data.icon != null ? $"Lv{level}" : $"Lv.{level} #{instanceId}";
}
UpdateHPBar();
    }

}
