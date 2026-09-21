using System.Collections.Generic;
using UnityEngine;

public static class SkillExecutor
{
    private const float SwordSideDamageMultiplier = 0.70f;

    // Local VFX review mode. Enable only for an explicitly requested local review build.
    public static bool ForceSkillActivationForVfxReview = false;

    public static bool TryExecute(BattleCharacter caster, BattleCharacter target, BattleManager bm)
    {
        if (caster == null || caster.data == null)
        {
            return false;
        }

        SkillType skillType = caster.data.skillType;
        if (skillType == SkillType.None)
        {
            return false;
        }

        if (skillType == SkillType.Counter || skillType == SkillType.AreaCounter)
        {
            return false;
        }

        if (skillType == SkillType.NumberPassive)
        {
            return false;
        }

        if (skillType != SkillType.Dragon)
        {
            int chance = GetSkillChance(caster.data, skillType);
            if (!RollSkillActivation(chance))
            {
                return false;
            }
        }

        bool executed;
        if (skillType == SkillType.Slash)
        {
            var context = new SkillContext(bm, caster, target);
            if (!CanSwordSlashHit(context))
            {
                return false;
            }

            // 範囲内の複数体が倒れても演出の向きがぶれないよう、ダメージ前に開始する。
            bm?.PlaySkillVfx(caster, target, skillType);
            return ExecuteSwordSlash(context);
        }
        if (skillType == SkillType.Spear)
        {
            var context = new SkillContext(bm, caster, target);
            if (!CanSpearPierceHit(context))
            {
                return false;
            }

            // 槍は命中で対象が即座に破棄されることがあるため、座標を参照できる
            // ダメージ処理前に演出を開始する。
            bm?.PlaySkillVfx(caster, target, skillType);
            return ExecuteSpearPierce(context);
        }
        if (skillType == SkillType.Gun)
        {
            executed = ExecuteGunLine(new SkillContext(bm, caster, target));
            return executed;
        }
        if (skillType == SkillType.TigerTwinClaw)
        {
            var context = new SkillContext(bm, caster, target);
            if (target == null || target.isDead)
            {
                return false;
            }

            // 一撃目で倒した場合も爪痕が消えないよう、対象の座標を保てるうちに開始する。
            bm?.PlaySkillVfx(caster, target, skillType);
            return ExecuteTigerTwinClaw(context);
        }
        if (skillType == SkillType.Dragon)
        {
            executed = ExecuteDragon(new SkillContext(bm, caster, target));
            return executed;
        }

        SkillData dataDriven = SkillCatalog.Get(skillType);
        if (dataDriven != null && dataDriven.effects != null && dataDriven.effects.Count > 0)
        {
            bool playBeforeResolution = HasCombatImpact(dataDriven);
            if (playBeforeResolution) bm?.PlaySkillVfx(caster, target, skillType);
            executed = ExecuteDataDriven(new SkillContext(bm, caster, target), dataDriven);
            if (executed && !playBeforeResolution) bm?.PlaySkillVfx(caster, target, skillType);
            return executed;
        }

        bool legacyCombat = target != null && skillType is SkillType.Stone
            or SkillType.Arrow
            or SkillType.Fireball
            or SkillType.WoodPush
            or SkillType.HorseCharge
            or SkillType.BirdRetreat;
        if (legacyCombat) bm?.PlaySkillVfx(caster, target, skillType);
        executed = ExecuteLegacy(new SkillContext(bm, caster, target));
        if (executed && !legacyCombat) bm?.PlaySkillVfx(caster, target, skillType);
        return executed;
    }

    public static int GetSkillChance(CharacterData data, SkillType skillType)
    {
        if (ForceSkillActivationForVfxReview)
        {
            return 100;
        }

        SkillData skillData = SkillCatalog.Get(skillType);
        if (skillData != null && skillData.chanceOverride >= 0)
        {
            return skillData.chanceOverride;
        }
        return data.skillChance;
    }

    public static bool RollSkillActivation(int chance)
    {
        return ForceSkillActivationForVfxReview || Random.value < Mathf.Clamp(chance, 0, 100) / 100f;
    }

    private static bool IsCounterEligibleForVfxReview(SkillType skillType)
    {
        return ForceSkillActivationForVfxReview
            && (skillType == SkillType.Slash || skillType == SkillType.StunBlow);
    }

    private static bool ExecuteDataDriven(SkillContext ctx, SkillData skillData)
    {
        if (ctx.Target == null && RequiresTarget(skillData))
        {
            return false;
        }

        foreach (var effect in skillData.effects)
        {
            switch (effect.effectType)
            {
                case SkillEffectType.Damage:
                    if (ctx.Target == null)
                    {
                        return false;
                    }
                    int dmg = Mathf.RoundToInt(ctx.Caster.GetEffectiveAttack(ctx.BattleManager)
                        * ctx.Caster.data.skillPower
                        * skillData.powerMultiplier
                        * effect.powerMultiplier);
                    if (!string.IsNullOrEmpty(skillData.logMessage))
                    {
                        ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} の {ctx.Caster.data.skillType}: " + string.Format(skillData.logMessage, dmg));
                    }
                    else
                    {
                        ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} の {ctx.Caster.data.skillType}！ {dmg} ダメージ");
                    }
                    ctx.Target.TakeDamage(
                        dmg,
                        ctx.BattleManager,
                        ctx.Caster,
                        effect.ignoreDefense,
                        isBasicAttack: IsCounterEligibleForVfxReview(ctx.Caster.data.skillType));
                    ctx.Caster.UpdateDirection(ctx.Target.gridPos - ctx.Caster.gridPos);
                    break;
                case SkillEffectType.Heal:
                    if (ctx.Target == null)
                    {
                        return false;
                    }
                    int maxHP = ctx.Target.data.GetMaxHP(ctx.Target.level);
                    int missing = maxHP - ctx.Target.currentHP;
                    if (missing <= 0)
                    {
                        return false;
                    }

                    int heal = Mathf.RoundToInt(maxHP
                        * ctx.Caster.data.skillPower
                        * skillData.powerMultiplier
                        * effect.powerMultiplier);
                    int beforeHP = ctx.Target.currentHP;
                    ctx.Target.currentHP = Mathf.Min(ctx.Target.currentHP + heal, maxHP);
                    ctx.Target.UpdateHPBar();
                    int actualHeal = ctx.Target.currentHP - beforeHP;
                    ctx.BattleManager.PlayDamageVfx(ctx.Target, actualHeal, isHealing: true);
                    ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} が {ctx.Target.DisplayName} を {actualHeal} 回復！");
                    break;
                case SkillEffectType.Stun:
                    if (ctx.Target == null)
                    {
                        return false;
                    }
                    ctx.Target.ApplyStun(effect.value);
                    ctx.BattleManager.AddLog($"{ctx.Target.DisplayName} はスタンした！");
                    break;
                case SkillEffectType.PushBack:
                    if (ctx.Target == null)
                    {
                        return false;
                    }
                    ctx.BattleManager.PushBackCharacter(ctx.Caster, ctx.Target);
                    break;
                case SkillEffectType.SoilTrap:
                    ctx.BattleManager.GenerateSoilTraps(ctx.Caster.gridPos);
                    ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} が土のスキルを発動！周囲に罠を設置した！");
                    break;
                case SkillEffectType.Retreat:
                    ctx.BattleManager.PerformBirdRetreat(ctx.Caster);
                    break;
                case SkillEffectType.MultiHit:
                    if (ctx.Target == null)
                    {
                        return false;
                    }
                    int hits = Mathf.Max(1, effect.value);
                    for (int i = 0; i < hits; i++)
                    {
                        if (ctx.Target.isDead)
                        {
                            break;
                        }
                        int hitDamage = Mathf.RoundToInt(ctx.Caster.GetEffectiveAttack(ctx.BattleManager)
                            * ctx.Caster.data.skillPower
                            * skillData.powerMultiplier
                            * effect.powerMultiplier);
                        ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} の連撃！ {ctx.Target.DisplayName} に {hitDamage} ダメージ");
                        ctx.Target.TakeDamage(
                            hitDamage,
                            ctx.BattleManager,
                            ctx.Caster,
                            effect.ignoreDefense,
                            isBasicAttack: IsCounterEligibleForVfxReview(ctx.Caster.data.skillType));
                    }
                    break;
                case SkillEffectType.Charge:
                    if (ctx.Target == null)
                    {
                        return false;
                    }
                    ctx.BattleManager.PerformHorseCharge(ctx.Caster, ctx.Target);
                    break;
                default:
                    Debug.LogWarning($"[SkillExecutor] Unsupported effect: {effect.effectType}");
                    break;
            }
        }

        return true;
    }

    private static bool RequiresTarget(SkillData data)
    {
        foreach (var effect in data.effects)
        {
            if (effect.effectType == SkillEffectType.Damage ||
                effect.effectType == SkillEffectType.Heal ||
                effect.effectType == SkillEffectType.Stun ||
                effect.effectType == SkillEffectType.PushBack ||
                effect.effectType == SkillEffectType.Charge)
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasCombatImpact(SkillData data)
    {
        foreach (SkillEffectData effect in data.effects)
        {
            if (effect.effectType is SkillEffectType.Damage
                or SkillEffectType.MultiHit
                or SkillEffectType.PushBack
                or SkillEffectType.Charge
                or SkillEffectType.Retreat)
            {
                return true;
            }
        }
        return false;
    }

    private static bool ExecuteLegacy(SkillContext ctx)
    {
        SkillType skillType = ctx.Caster.data.skillType;
        float roll = Random.value;

        switch (skillType)
        {
            case SkillType.Stone:
                if (ctx.Target != null)
                {
                    ctx.Caster.PerformAttack(ctx.Target, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} が石を投げた！ {{0}} ダメージ");
                    return true;
                }
                break;
            case SkillType.Gun:
                if (ctx.Target != null)
                {
                    ctx.Caster.PerformAttack(ctx.Target, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} が銃を放った！ {{0}} ダメージ");
                    return true;
                }
                break;
            case SkillType.Arrow:
                if (ctx.Target != null)
                {
                    ctx.Caster.PerformAttack(ctx.Target, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} が矢を放った！ {{0}} ダメージ");
                    return true;
                }
                break;
            case SkillType.Soil:
                ctx.BattleManager.GenerateSoilTraps(ctx.Caster.gridPos);
                ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} が土のスキルを発動！周囲に罠を設置した！");
                return true;
            case SkillType.Fireball:
                if (ctx.Target != null)
                {
                    int dmg = Mathf.RoundToInt(ctx.Caster.GetEffectiveAttack(ctx.BattleManager) * ctx.Caster.data.skillPower);
                    ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} がファイアボールを放った！ {dmg} ダメージ (防御無視)");
                    ctx.Target.TakeDamage(dmg, ctx.BattleManager, ctx.Caster, ignoreDefense: true, isBasicAttack: false);
                    return true;
                }
                break;
            case SkillType.WoodPush:
                if (ctx.Target != null)
                {
                    ctx.Caster.PerformAttack(ctx.Target, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} が木の力で押し出した！ {{0}} ダメージ");
                    ctx.BattleManager.PushBackCharacter(ctx.Caster, ctx.Target);
                    return true;
                }
                break;
            case SkillType.HorseCharge:
                if (ctx.Target != null)
                {
                    ctx.BattleManager.PerformHorseCharge(ctx.Caster, ctx.Target);
                    return true;
                }
                break;
            case SkillType.BirdRetreat:
                if (ctx.Target != null)
                {
                    ctx.Caster.PerformAttack(ctx.Target, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} がバードリトリートを放った！ {{0}} ダメージ");
                    ctx.BattleManager.PerformBirdRetreat(ctx.Caster);
                    return true;
                }
                break;
            case SkillType.TigerTwinClaw:
                if (ctx.Target != null)
                {
                    ctx.Caster.PerformAttack(ctx.Target, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} のツインクロー1撃目！ {{0}} ダメージ");
                    BattleCharacter secondTarget = TargetingService.FindAdjacentEnemy(ctx.BattleManager, ctx.Caster);
                    if (secondTarget != null && !secondTarget.isDead)
                    {
                        ctx.Caster.PerformAttack(secondTarget, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} のツインクロー2撃目！ {{0}} ダメージ");
                    }
                    return true;
                }
                break;
            default:
                return false;
        }
        return false;
    }

    private static bool ExecuteSpearPierce(SkillContext ctx)
    {
        if (ctx.Target == null)
        {
            return false;
        }

        Vector2Int delta = ctx.Target.gridPos - ctx.Caster.gridPos;
        int dx = Mathf.Clamp(delta.x, -1, 1);
        int dy = Mathf.Clamp(delta.y, -1, 1);
        if (dx == 0 && dy == 0)
        {
            return false;
        }

        Vector2Int dir = new Vector2Int(dx, dy);
        Vector2Int pos1 = ctx.Caster.gridPos + dir;
        Vector2Int pos2 = ctx.Caster.gridPos + dir * 2;

        ctx.BattleManager.gridMap.TryGetValue(pos1, out BattleCharacter firstHit);
        ctx.BattleManager.gridMap.TryGetValue(pos2, out BattleCharacter secondHit);

        bool didHit = false;
        if (firstHit != null && !firstHit.isDead && firstHit.isAlly != ctx.Caster.isAlly)
        {
            ctx.Caster.PerformAttack(firstHit, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} が槍を突き出した！ {{0}} ダメージ");
            didHit = true;
        }

        if (secondHit != null && !secondHit.isDead && secondHit.isAlly != ctx.Caster.isAlly)
        {
            ctx.Caster.PerformAttack(secondHit, ctx.BattleManager, ctx.Caster.data.skillPower, $"{ctx.Caster.DisplayName} の貫通が命中！ {{0}} ダメージ");
            didHit = true;
        }

        return didHit;
    }

    private static bool ExecuteSwordSlash(SkillContext ctx)
    {
        if (ctx.Caster == null || ctx.Target == null || ctx.BattleManager == null)
        {
            return false;
        }

        Vector2Int direction = TargetingService.GetSwordAttackDirection(ctx.Caster, ctx.Target);
        List<BattleCharacter> targets = TargetingService.GetSwordWedgeTargets(ctx.BattleManager, ctx.Caster, direction);
        if (targets.Count == 0)
        {
            return false;
        }

        SkillData skillData = SkillCatalog.Get(SkillType.Slash);
        SkillEffectData damageEffect = skillData?.effects?.Find(effect => effect.effectType == SkillEffectType.Damage);
        float skillMultiplier = skillData != null ? skillData.powerMultiplier : 1f;
        float effectMultiplier = damageEffect != null ? damageEffect.powerMultiplier : 1f;
        bool ignoreDefense = damageEffect != null && damageEffect.ignoreDefense;
        int damage = Mathf.RoundToInt(ctx.Caster.GetEffectiveAttack(ctx.BattleManager)
            * ctx.Caster.data.skillPower
            * skillMultiplier
            * effectMultiplier);

        ctx.Caster.UpdateDirection(direction);
        Vector2Int centerCell = ctx.Caster.gridPos + direction;
        foreach (BattleCharacter victim in targets)
        {
            if (victim == null || victim.isDead)
            {
                continue;
            }

            bool isCenterHit = victim.gridPos == centerCell;
            int targetDamage = isCenterHit
                ? damage
                : Mathf.Max(1, Mathf.RoundToInt(damage * SwordSideDamageMultiplier));
            string message = !string.IsNullOrEmpty(skillData?.logMessage)
                ? string.Format(skillData.logMessage, targetDamage)
                : $"{targetDamage} ダメージを与えた！";
            ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} の斬撃が {victim.DisplayName} に {message}");
            victim.TakeDamage(
                targetDamage,
                ctx.BattleManager,
                ctx.Caster,
                ignoreDefense,
                isBasicAttack: IsCounterEligibleForVfxReview(SkillType.Slash));
        }

        return true;
    }

    private static bool CanSwordSlashHit(SkillContext ctx)
    {
        if (ctx.Caster == null || ctx.Target == null || ctx.BattleManager == null)
        {
            return false;
        }

        Vector2Int direction = TargetingService.GetSwordAttackDirection(ctx.Caster, ctx.Target);
        return TargetingService.GetSwordWedgeTargets(ctx.BattleManager, ctx.Caster, direction).Count > 0;
    }

    private static bool CanSpearPierceHit(SkillContext ctx)
    {
        if (ctx.Target == null || ctx.BattleManager == null)
        {
            return false;
        }

        Vector2Int delta = ctx.Target.gridPos - ctx.Caster.gridPos;
        Vector2Int dir = new Vector2Int(Mathf.Clamp(delta.x, -1, 1), Mathf.Clamp(delta.y, -1, 1));
        if (dir == Vector2Int.zero)
        {
            return false;
        }

        Vector2Int pos1 = ctx.Caster.gridPos + dir;
        Vector2Int pos2 = ctx.Caster.gridPos + dir * 2;
        return IsEnemyAt(ctx, pos1) || IsEnemyAt(ctx, pos2);
    }

    private static bool IsEnemyAt(SkillContext ctx, Vector2Int position)
    {
        return ctx.BattleManager.gridMap.TryGetValue(position, out BattleCharacter character)
            && character != null
            && !character.isDead
            && character.isAlly != ctx.Caster.isAlly;
    }

    private static bool ExecuteGunLine(SkillContext ctx)
    {
        SkillData skillData = SkillCatalog.Get(SkillType.Gun);
        float powerMultiplier = skillData != null ? skillData.powerMultiplier : 1f;
        SkillEffectData damageEffect = null;
        if (skillData != null && skillData.effects != null)
        {
            foreach (var effect in skillData.effects)
            {
                if (effect.effectType == SkillEffectType.Damage)
                {
                    damageEffect = effect;
                    break;
                }
            }
        }

        Vector2Int[] dirs =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        var validDirs = new List<Vector2Int>();
        foreach (var dir in dirs)
        {
            for (int d = 1; d <= ctx.BattleManager.Rows + ctx.BattleManager.Cols; d++)
            {
                Vector2Int pos = ctx.Caster.gridPos + dir * d;
                if (pos.x < 0 || pos.x >= ctx.BattleManager.Cols || pos.y < 0 || pos.y >= ctx.BattleManager.Rows)
                {
                    break;
                }
                if (ctx.BattleManager.gridMap.TryGetValue(pos, out var bc))
                {
                    if (bc != null && !bc.isDead && bc.isAlly != ctx.Caster.isAlly)
                    {
                        validDirs.Add(dir);
                    }
                    break;
                }
            }
        }

        if (validDirs.Count == 0)
        {
            return false;
        }

        Vector2Int chosenDir = validDirs[Random.Range(0, validDirs.Count)];
        bool didHit = false;
        ctx.BattleManager.PlayGunLineVfx(ctx.Caster, chosenDir);

        for (int d = 1; d <= ctx.BattleManager.Rows + ctx.BattleManager.Cols; d++)
        {
            Vector2Int pos = ctx.Caster.gridPos + chosenDir * d;
            if (pos.x < 0 || pos.x >= ctx.BattleManager.Cols || pos.y < 0 || pos.y >= ctx.BattleManager.Rows)
            {
                break;
            }
            if (!ctx.BattleManager.gridMap.TryGetValue(pos, out var target) || target == null)
            {
                continue;
            }
            if (target.isDead || target.isAlly == ctx.Caster.isAlly)
            {
                continue;
            }

            float effectMultiplier = damageEffect != null ? damageEffect.powerMultiplier : 1f;
            bool ignoreDefense = damageEffect != null && damageEffect.ignoreDefense;
            int dmg = Mathf.RoundToInt(ctx.Caster.GetEffectiveAttack(ctx.BattleManager)
                * ctx.Caster.data.skillPower
                * powerMultiplier
                * effectMultiplier);
            ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} の銃撃！ {target.DisplayName} に {dmg} ダメージ");
            target.TakeDamage(dmg, ctx.BattleManager, ctx.Caster, ignoreDefense, isBasicAttack: false);
            ctx.Caster.UpdateDirection(target.gridPos - ctx.Caster.gridPos);
            didHit = true;
        }

        return didHit;
    }

    private static bool ExecuteTigerTwinClaw(SkillContext ctx)
    {
        if (ctx.Target == null)
        {
            return false;
        }

        SkillData skillData = SkillCatalog.Get(SkillType.TigerTwinClaw);
        float powerMultiplier = skillData != null ? skillData.powerMultiplier : 1f;
        SkillEffectData multiHit = null;
        if (skillData != null && skillData.effects != null)
        {
            foreach (var effect in skillData.effects)
            {
                if (effect.effectType == SkillEffectType.MultiHit)
                {
                    multiHit = effect;
                    break;
                }
            }
        }

        int hits = Mathf.Max(1, multiHit != null ? multiHit.value : 2);
        bool didHit = false;

        for (int i = 0; i < hits; i++)
        {
            if (ctx.Target.isDead)
            {
                break;
            }
            int hitDamage = Mathf.RoundToInt(ctx.Caster.GetEffectiveAttack(ctx.BattleManager)
                * ctx.Caster.data.skillPower
                * powerMultiplier
                * (multiHit != null ? multiHit.powerMultiplier : 1f));
            ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} のツインクロー！ {ctx.Target.DisplayName} に {hitDamage} ダメージ");
            ctx.Target.TakeDamage(hitDamage, ctx.BattleManager, ctx.Caster, multiHit != null && multiHit.ignoreDefense, isBasicAttack: false);
            didHit = true;
        }

        BattleCharacter secondTarget = TargetingService.FindAdjacentEnemy(ctx.BattleManager, ctx.Caster);
        if (secondTarget != null && !secondTarget.isDead && secondTarget != ctx.Target)
        {
            int secondHitDamage = Mathf.RoundToInt(ctx.Caster.GetEffectiveAttack(ctx.BattleManager)
                * ctx.Caster.data.skillPower
                * powerMultiplier);
            ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} のツインクロー2撃目！ {secondTarget.DisplayName} に {secondHitDamage} ダメージ");
            secondTarget.TakeDamage(secondHitDamage, ctx.BattleManager, ctx.Caster, false, isBasicAttack: false);
            didHit = true;
        }

        return didHit;
    }

    private static bool ExecuteDragon(SkillContext ctx)
    {
        SkillData skillData = SkillCatalog.Get(SkillType.Dragon);
        int chance = GetSkillChance(ctx.Caster.data, SkillType.Dragon);

        int range = 3;
        bool hasRoar = false;
        if (skillData != null && skillData.effects != null)
        {
            foreach (var effect in skillData.effects)
            {
                if (effect.effectType == SkillEffectType.DragonBreath && effect.value > 0)
                {
                    range = effect.value;
                }
                if (effect.effectType == SkillEffectType.DragonRoar)
                {
                    hasRoar = true;
                }
            }
        }

        // 演出確認ではブレスと咆哮を同じ頻度で選ぶ。咆哮対象がいない場合だけ、
        // 行動が空振りにならないようブレスへフォールバックする。
        if (ForceSkillActivationForVfxReview)
        {
            bool selectRoar = hasRoar && !ctx.Caster.isAlly && ctx.Caster.data.isBoss && Random.value < 0.5f;
            if (selectRoar)
            {
                if (ctx.BattleManager.PerformDragonRoar(ctx.Caster)) return true;
                return ctx.BattleManager.PerformDragonBreath(ctx.Caster, range);
            }

            if (ctx.BattleManager.PerformDragonBreath(ctx.Caster, range)) return true;
            return hasRoar && !ctx.Caster.isAlly && ctx.Caster.data.isBoss
                && ctx.BattleManager.PerformDragonRoar(ctx.Caster);
        }

        if (RollSkillActivation(chance))
        {
            bool success = ctx.BattleManager.PerformDragonBreath(ctx.Caster, range);
            if (success) return true;
        }

        if (!ctx.Caster.isAlly && hasRoar && ctx.Caster.data.isBoss)
        {
            int roarChance = skillData != null ? skillData.dragonRoarChance : -1;
            if (roarChance < 0)
            {
                roarChance = chance;
            }
            if (RollSkillActivation(roarChance))
            {
                bool success = ctx.BattleManager.PerformDragonRoar(ctx.Caster);
                if (success) return true;
            }
        }

        return false;
    }
}
