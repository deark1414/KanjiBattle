using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleDamageRegression
{
    public static void Run()
    {
        Check(BattleDamageRules.ReduceByShield(100) == 80, "shield reduces twenty percent");
        Check(BattleDamageRules.ReduceByShield(1) == 1 && BattleDamageRules.ReduceByShield(0) == 0, "shield minimum and zero");
        Check(BattleDamageRules.GuardShare(80, 100, 0.25f) == 20, "share after mitigation");
        Check(BattleDamageRules.GuardShare(80, 3, 0.25f) == 3, "share cannot exceed remaining HP");
        Check(BattleDamageRules.GuardShare(1, 100, 0.25f) == 0, "small hit does not create extra damage");
        Check(BattleDamageRules.GuardShare(80, 100, 2f) == 80
            && BattleDamageRules.GuardShare(80, 100, -1f) == 0, "invalid ratios are clamped");
        Check(BattleDamageRules.GuardShare(100, 100, .5f, true) == 50
            && BattleDamageRules.GuardLoss(50, true) == 35, "ox halves the hit and reduces only its share by thirty percent");
        for (int hp = 0; hp <= 100; hp++)
        {
            int share = BattleDamageRules.GuardShare(100, hp, .5f, true);
            int loss = BattleDamageRules.GuardLoss(share, true);
            Check(loss <= hp && loss <= share && (share == 0 || loss >= 1), "bounded protection with no free absorption");
        }
        Check(BattleDamageRules.GuardShare(100, 7, .5f, true) == 10
            && BattleDamageRules.GuardLoss(10, true) == 7, "low HP capacity accounts for transfer mitigation exactly once");
        Check(BattleDamageRules.GuardShare(int.MaxValue, int.MaxValue, 1, true) == int.MaxValue,
            "large damage protection does not overflow");
        var ox = Resources.Load<CharacterDatabase>("CharacterDatabase").GetById(30);
        Check(ox.skillType == SkillType.ProjectileGuard && ox.skillPower == .5f, "ox asset uses the new passive");
        Check(BattleRewardRules.ArmyExperienceMultiplier(new[] { ox, ox }) == 1, "actual ox data has no experience bonus");
        Check(Resources.Load<CharacterDatabase>("CharacterDatabase").GetById(15).skillType == SkillType.Wall,
            "wall asset replaces reflection with projectile blocking");
        Check(BattleDamageRules.ReduceByWall(100,1)==0 && BattleDamageRules.ReduceByWall(100,.8f)==20,
            "wall supports immunity and later fractional mitigation");
        var host = new GameObject("DamageRegression", typeof(RectTransform));
        host.SetActive(false);
        var definitions = new List<CharacterData>();
        bool force = SkillExecutor.ForceSkillActivationForVfxReview;
        try
        {
            BattleCharacter Create(Vector2Int pos, bool ally, SkillType skill, float ratio = 0.25f)
            {
                var data = ScriptableObject.CreateInstance<CharacterData>();
                definitions.Add(data);
                data.skillType = skill; data.skillPower = ratio;
                var go = new GameObject("Unit", typeof(RectTransform));
                go.transform.SetParent(host.transform, false);
                var unit = go.AddComponent<BattleCharacter>();
                unit.Init(data, pos, ally);
                return unit;
            }
            var target = Create(new(2, 3), true, SkillType.Shield);
            var guard = Create(new(1, 3), true, SkillType.GuardAlly);
            var strong = Create(new(3, 3), true, SkillType.GuardAlly, 0.4f);
            var wrong = Create(new(2, 2), false, SkillType.GuardAlly, 1f);
            var units = new[] { target, guard, strong, wrong };
            Check(BattleDamageRules.FindProtector(target, units) == strong, "highest-ratio friendly guard only");
            strong.isDead = true;
            Check(BattleDamageRules.FindProtector(target, units) == guard, "dead guard ignored");
            strong.isDead = false; strong.currentHP = 0;
            Check(BattleDamageRules.FindProtector(target, units) == guard, "zero HP guard ignored");
            guard.gridPos = new(0, 0);
            Check(BattleDamageRules.FindProtector(target, units) == null, "distance and enemy guards rejected");
            Check(BattleDamageRules.FindProtector(guard, new[] { guard }) == null, "no self protection");
            var projectileGuard = Create(new(1, 3), true, SkillType.ProjectileGuard, .5f);
            var projectileUnits = new[] { guard, projectileGuard };
            guard.gridPos = new(3, 3);
            Check(BattleDamageRules.FindProtector(target, projectileUnits) == guard, "ox does not intercept non-projectiles");
            Check(BattleDamageRules.FindProtector(target, projectileUnits, true) == projectileGuard,
                "one strongest eligible protector is used, not ox plus shell");
            guard.data.skillPower = .5f;
            Check(BattleDamageRules.FindProtector(target, projectileUnits, true) == projectileGuard,
                "equal shares prefer the protector with lower transferred damage");
            projectileGuard.isDead = true;
            Check(BattleDamageRules.FindProtector(target, projectileUnits, true) == guard, "dead ox falls back to shell");
            target.defense = 0;
            target.TakeDamage(100, null, ignoreDefense: true);
            Check(target.currentHP == 20, "shield executes in damage pipeline");
            target.currentHP = 100;
            target.TakeDamage(40, null, origin: BattleDamageOrigin.Transferred);
            Check(target.currentHP == 60, "transfer bypasses shield reduction");
            target.currentHP = 100;
            target.TakeDamage(0, null);
            Check(target.currentHP == 100, "zero damage is not promoted to one");
            target.data.skillType = SkillType.AreaCounter;
            SkillExecutor.ForceSkillActivationForVfxReview = true;
            target.TakeDamage(40, null, wrong, isBasicAttack: true, origin: BattleDamageOrigin.Transferred);
            Check(target.currentHP == 60 && wrong.currentHP == wrong.maxHP,
                "transferred hit cannot trigger an adjacent area counter");
            Check(SkillExecutor.TryExecuteDetailed(guard, target, null).Failure == SkillExecutionFailure.PassiveOrReactiveSkill,
                "guard is passive, not a chance to use its ordinary turn");
            Check(SkillExecutor.TryExecuteDetailed(projectileGuard, target, null).Failure == SkillExecutionFailure.InvalidCaster,
                "dead ox cannot act");
            projectileGuard.isDead = false;
            Check(SkillExecutor.TryExecuteDetailed(projectileGuard, target, null).Failure == SkillExecutionFailure.PassiveOrReactiveSkill,
                "ox is a passive and does not consume its ordinary attack action");
            var wall=Create(new(2,3),true,SkillType.Wall,1);
            var shooter=Create(new(2,0),false,SkillType.Gun);
            foreach(bool side in new[]{true,false})
            {
                wall.isAlly=side;shooter.isAlly=!side;
                foreach(SkillType skill in new[]{SkillType.Arrow,SkillType.Gun,SkillType.Stone,SkillType.Fireball,SkillType.ScatterShot})
                {
                    shooter.data.skillType=skill;wall.currentHP=100;wall.CocoonShield=true;
                    wall.TakeDamage(40,null,shooter,ignoreDefense:true);
                    Check(wall.currentHP==100&&wall.CocoonShield,"wall blocks projectiles before consuming cocoon");
                }
                wall.CocoonShield=false;
                foreach(SkillType skill in new[]{SkillType.Slash,SkillType.Spear,SkillType.ChainLightning,SkillType.Dragon})
                {
                    wall.currentHP=100;shooter.data.skillType=skill;
                    wall.TakeDamage(40,null,shooter,ignoreDefense:true);
                    Check(wall.currentHP==60&&shooter.currentHP==shooter.maxHP,"non-projectiles hurt wall without reflection");
                }
                shooter.data.skillType=SkillType.Gun;
                foreach(BattleDamageOrigin origin in new[]{BattleDamageOrigin.Transferred,BattleDamageOrigin.Reflected,BattleDamageOrigin.Returned})
                {
                    wall.currentHP=100;wall.TakeDamage(40,null,shooter,ignoreDefense:true,origin:origin);
                    Check(wall.currentHP==60,"wall does not block indirect damage");
                }
                wall.currentHP=100;wall.TakeDamage(40,null,shooter,ignoreDefense:true,isBasicAttack:true);
                Check(wall.currentHP==60,"basic attack is not a projectile");
                wall.currentHP=100;wall.TakeDamage(40,null,ignoreDefense:true);
                Check(wall.currentHP==60,"traps still hurt wall");
                var grid=new Dictionary<Vector2Int,BattleCharacter>{{wall.gridPos,wall}};
                Check(BattleDamageRules.GunLine(shooter,Vector2Int.up,grid,6,7).Count==3,"bullet stops on hostile wall");
                wall.isDead=true;
                Check(BattleDamageRules.GunLine(shooter,Vector2Int.up,grid,6,7).Count==6,"dead wall does not block");
                wall.isDead=false;wall.isAlly=shooter.isAlly;
                Check(BattleDamageRules.GunLine(shooter,Vector2Int.up,grid,6,7).Count==6,"friendly wall does not obstruct friendly fire");
            }
            Check(SkillExecutor.TryExecuteDetailed(wall,shooter,null).Failure==SkillExecutionFailure.PassiveOrReactiveSkill,
                "wall immunity does not consume an attack action");
        }
        finally
        {
            SkillExecutor.ForceSkillActivationForVfxReview = force;
            UnityEngine.Object.DestroyImmediate(host);
            foreach (CharacterData data in definitions) UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException("[BattleDamageRegression] " + label);
    }
}
