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

    public void Init(CharacterData data, Vector2Int pos, bool ally, int level = 1)
    {
        this.data = data;
        this.gridPos = pos;
        this.isAlly = ally;
        this.level = level;

        currentHP = data.GetMaxHP(level);
        attack    = data.GetAttack(level);
        defense   = data.GetDefense(level);

        if (background != null)
            background.color = ally ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.7f, 0.7f);

        if (nameText != null)
            nameText.text = data.characterName;

        // 通し番号を割り振り（味方・敵ごとに管理）
        if (ally)
        {
            instanceId = ++allyCounter;
        }
        else
        {
            instanceId = ++enemyCounter;
        }

        // Lvと通し番号を一緒に表示
        if (levelText != null)
            levelText.text = $"Lv.{level} #{instanceId}";

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

    public void TakeDamage(int dmg, BattleManager bm, BattleCharacter attacker = null, bool ignoreDefense = false)
    {
        if (isDead) return;

        int originalDmg = dmg;
        // Defense-based reduction (unless Armor skill or ignoreDefense is true)
        if (!ignoreDefense && (data == null || data.skillType != SkillType.Armor)) {
            float effectiveDefense = Mathf.Max(0, defense / 4f);
            int reducedByDefense = Mathf.FloorToInt(dmg * 100f / (100f + effectiveDefense));
            dmg = Mathf.Max(1, reducedByDefense);
        }
        // Handle Armor skill: after defense/ignoreDefense check, reduce incoming damage by (level * 2), minimum 1
        if (data != null && data.skillType == SkillType.Armor)
        {
            int reduction = level * 2;
            int reduced = Mathf.Max(1, dmg - reduction);
            bm.AddLog($"{DisplayName} のアーマーでダメージが {dmg} → {reduced} に軽減！（Lv{level}×2={reduction} 減少）");
            dmg = reduced;
        }

        bm.AddLog($"{DisplayName} が {originalDmg} → {dmg} ダメージ (DEF {defense})");

        currentHP -= dmg;
        UpdateHPBar();

        if (currentHP <= 0)
        {
            bm.HandleDeath(this);
        }

        // Counter logic after taking damage
        if (data != null)
        {
            // Counter: 30% chance to reflect 30% of taken damage to attacker
            if (data.skillType == SkillType.Counter && attacker != null && !attacker.isDead)
            {
                float roll = Random.value;
                if (roll < 0.3f && dmg > 0)
                {
                    int reflect = Mathf.RoundToInt(dmg * 0.3f);
                    if (reflect > 0)
                    {
                        bm.AddLog($"{DisplayName} のカウンター発動！{attacker.DisplayName} に {reflect} ダメージを反射！");
                        attacker.TakeDamage(reflect, bm, this);
                        attacker.UpdateHPBar();
                    }
                }
            }
            // AreaCounter: 20% chance to reflect 20% of taken damage to all adjacent enemies
            else if (data.skillType == SkillType.AreaCounter && dmg > 0)
            {
                float roll = Random.value;
                if (roll < 0.2f)
                {
                    int reflect = Mathf.RoundToInt(dmg * 0.2f);
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
                                target.TakeDamage(reflect, bm, this);
                                target.UpdateHPBar();
                            }
                        }
                    }
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
        // ★ 正規化（距離は無視して方向だけ残す）
        if (dir.x != 0) dir.x = dir.x > 0 ? 1 : -1;
        if (dir.y != 0) dir.y = dir.y > 0 ? 1 : -1;

        string arrow = "";

        if (dir == Vector2Int.up) arrow = "↓";       // (0,1) は下
        else if (dir == Vector2Int.down) arrow = "↑"; // (0,-1) は上
        else if (dir == Vector2Int.left) arrow = "←";
        else if (dir == Vector2Int.right) arrow = "→";
        else if (dir.x == 1 && dir.y == 1) arrow = "↘";   // 右下
        else if (dir.x == -1 && dir.y == 1) arrow = "↙";  // 左下
        else if (dir.x == 1 && dir.y == -1) arrow = "↗";  // 右上
        else if (dir.x == -1 && dir.y == -1) arrow = "↖"; // 左上

        directionText.text = arrow;
    }

    public IEnumerator AttackEffect()
    {
        if (background == null) yield break;

        Color orig = background.color;
        background.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);

        if (background != null)
            background.color = orig;
    }

    public bool TryUseSkill(BattleCharacter target, BattleManager bm)
    {
        if (data == null || data.skillType == SkillType.None)
            return false;

        // Counter and AreaCounter are handled in TakeDamage, not here
        if (data.skillType == SkillType.Counter || data.skillType == SkillType.AreaCounter)
            return false;

        float roll = Random.value;
        // Dragonは個別に確率分岐するのでスキップ
        if (data.skillType != SkillType.Dragon)
        {
            if (roll > data.skillChance / 100f)
                return false;
        }

        switch (data.skillType)
        {
            case SkillType.Slash:
                if (target != null)
                {
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} が斬撃を放った！ {{0}} ダメージ");
                    return true;
                }
                break;
            case SkillType.StunBlow:
                if (target != null)
                {
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} がスタンブローを放った！ {{0}} ダメージ");
                    target.ApplyStun(1);
                    bm.AddLog($"{target.DisplayName} はスタンした！");
                    return true;
                }
                break;
            case SkillType.Spear:
                if (target != null)
                {
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} が槍を突き出した！ {{0}} ダメージ");
                    return true;
                }
                break;
            case SkillType.Stone:
                if (target != null)
                {
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} が石を投げた！ {{0}} ダメージ");
                    return true;
                }
                break;
            case SkillType.Gun:
                if (target != null)
                {
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} が銃を放った！ {{0}} ダメージ");
                    return true;
                }
                break;
            case SkillType.Arrow:
                if (target != null)
                {
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} が矢を放った！ {{0}} ダメージ");
                    return true;
                }
                break;
            case SkillType.Soil:
                bm.GenerateSoilTraps(this.gridPos);
                bm.AddLog($"{DisplayName} が土のスキルを発動！周囲に罠を設置した！");
                return true;
            case SkillType.Fireball:
                if (target != null)
                {
                    int dmg = Mathf.RoundToInt(GetEffectiveAttack(bm) * data.skillPower);
                    bm.AddLog($"{DisplayName} がファイアボールを放った！ {dmg} ダメージ (防御無視)");
                    target.TakeDamage(dmg, bm, this, ignoreDefense: true);
                    return true;
                }
                break;
            case SkillType.WoodPush:
                if (target != null)
                {
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} が木の力で押し出した！ {{0}} ダメージ");
                    bm.PushBackCharacter(this, target); // Manager側で補助メソッドを実装
                    return true;
                }
                break;
            case SkillType.WaterHeal:
                if (target != null && !target.isDead)
                {
                    int heal = Mathf.RoundToInt(target.data.GetMaxHP(target.level) * data.skillPower);
                    int beforeHP = target.currentHP;
                    target.currentHP = Mathf.Min(target.currentHP + heal, target.data.GetMaxHP(target.level));
                    target.UpdateHPBar();
                    bm.AddLog($"{DisplayName} が {target.DisplayName} を {target.currentHP - beforeHP} 回復！");
                    return true;
                }
                break;
            case SkillType.NumberPassive:
                // NumberPassive is a passive skill, no active use here
                // Note: When fully triggered, passive buff includes defense ignore (handled elsewhere if needed)
                return false;
            case SkillType.HorseCharge:
                if (target != null)
                {
                    bm.PerformHorseCharge(this, target);
                    return true;
                }
                break;
            case SkillType.BirdRetreat:
                if (target != null)
                {
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} がバードリトリートを放った！ {{0}} ダメージ");
                    bm.PerformBirdRetreat(this);
                    return true;
                }
                break;
            case SkillType.TigerTwinClaw:
                if (target != null)
                {
                    // 1撃目：指定ターゲットに攻撃
                    PerformAttack(target, bm, data.skillPower, $"{DisplayName} のツインクロー1撃目！ {{0}} ダメージ");

                    // 2撃目：周囲1マスの敵を Manager の関数で取得
                    BattleCharacter secondTarget = BattleTargetFinder.FindAdjacentEnemy(bm, this);
                    if (secondTarget != null && !secondTarget.isDead)
                    {
                        PerformAttack(secondTarget, bm, data.skillPower, $"{DisplayName} のツインクロー2撃目！ {{0}} ダメージ");
                    }
                    return true;
                }
                break;
            case SkillType.Dragon:
                {
                    // 発動判定
                    if (roll < data.skillChance) // ブレス優先
                    {
                        // Dragon breath should ignore defense (handled in PerformDragonBreath)
                        bool success = bm.PerformDragonBreath(this, 3);
                        if (success) return true; // 敵に当たった時だけ true
                    }
                    else if (!isAlly) // 敵ドラゴンのみ咆哮
                    {
                        bool success = bm.PerformDragonRoar(this);
                        if (success) return true; // 対象がいた時だけ true
                    }
                    return false; // 範囲に誰もいないなら失敗→通常攻撃/移動へ
                }
            default:
                return false;
        }
        return false;
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

        int dmg = Mathf.RoundToInt(GetEffectiveAttack(bm) * powerMultiplier);
        if (string.IsNullOrEmpty(logMessage))
        {
            bm.AddLog($"{DisplayName} は通常攻撃を行った！ {dmg} ダメージ");
        }
        else
        {
            bm.AddLog(string.Format(logMessage, dmg));
        }
        target.TakeDamage(dmg, bm, this, false);
        UpdateDirection(target.gridPos - this.gridPos);
    }

    // Helper method to calculate NumberPassive buff and unique count
    private (int uniqueCount, int countOtherOnes, float buff) CalculateNumberPassiveBuff(BattleManager bm)
    {
        HashSet<string> uniqueNumbers = new HashSet<string>();
        int countOtherOnes = 0;

        foreach (var kvp in bm.gridMap)
        {
            var character = kvp.Value;
            if (character == null || character.isDead) continue;
            if (!character.isAlly) continue;
            if (character == this) continue;
            if (character.data.category != CharacterCategory.Number1 &&
                character.data.category != CharacterCategory.Number2 &&
                character.data.category != CharacterCategory.Number3)
                continue;

            if (character.data.characterName == "一")
            {
                countOtherOnes++;
            }
            else
            {
                uniqueNumbers.Add(character.data.characterName);
            }
        }

        int types = uniqueNumbers.Count;
        float buff = 1f;

        if (data.characterName == "一")
        {
            if (countOtherOnes > 0)
            {
                types += 1;
            }
            float oneMatsuriBonus = Mathf.Min(0.05f * countOtherOnes, 0.25f);
            buff += oneMatsuriBonus;
        }

        if (types == 1) buff += 0.05f;
        else if (types == 2) buff += 0.10f;
        else if (types >= 3) buff += 0.15f;

        int uniqueCount = uniqueNumbers.Count + ((data.characterName == "一" && countOtherOnes > 0) ? 1 : 0);
        return (uniqueCount, countOtherOnes, buff);
    }
}