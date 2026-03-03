using System.Collections.Generic;
using UnityEngine;

public static class SkillExecutor
{
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
            if (Random.value > chance / 100f)
            {
                return false;
            }
        }

        if (skillType == SkillType.Spear)
        {
            return ExecuteSpearPierce(new SkillContext(bm, caster, target));
        }
        if (skillType == SkillType.Gun)
        {
            return ExecuteGunLine(new SkillContext(bm, caster, target));
        }
        if (skillType == SkillType.TigerTwinClaw)
        {
            return ExecuteTigerTwinClaw(new SkillContext(bm, caster, target));
        }
        if (skillType == SkillType.Dragon)
        {
            return ExecuteDragon(new SkillContext(bm, caster, target));
        }

        SkillData dataDriven = SkillCatalog.Get(skillType);
        if (dataDriven != null && dataDriven.effects != null && dataDriven.effects.Count > 0)
        {
            return ExecuteDataDriven(new SkillContext(bm, caster, target), dataDriven);
        }

        return ExecuteLegacy(new SkillContext(bm, caster, target));
    }

    private static int GetSkillChance(CharacterData data, SkillType skillType)
    {
        SkillData skillData = SkillCatalog.Get(skillType);
        if (skillData != null && skillData.chanceOverride >= 0)
        {
            return skillData.chanceOverride;
        }
        return data.skillChance;
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
                    ctx.Target.TakeDamage(dmg, ctx.BattleManager, ctx.Caster, effect.ignoreDefense, isBasicAttack: false);
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
                    ctx.BattleManager.AddLog($"{ctx.Caster.DisplayName} が {ctx.Target.DisplayName} を {ctx.Target.currentHP - beforeHP} 回復！");
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
                        ctx.Target.TakeDamage(hitDamage, ctx.BattleManager, ctx.Caster, effect.ignoreDefense, isBasicAttack: false);
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
        int chance = ctx.Caster.data.skillChance;
        if (skillData != null && skillData.chanceOverride >= 0)
        {
            chance = skillData.chanceOverride;
        }

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

        if (Random.value < chance / 100f)
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
            if (Random.value < roarChance / 100f)
            {
                bool success = ctx.BattleManager.PerformDragonRoar(ctx.Caster);
                if (success) return true;
            }
        }

        return false;
    }
}
