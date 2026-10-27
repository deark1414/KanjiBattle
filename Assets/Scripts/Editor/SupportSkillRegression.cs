using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SupportSkillRegression
{
    public static void Run()
    {
        var host = new GameObject("SupportRegression", typeof(RectTransform));
        host.SetActive(false);
        var definitions = new List<CharacterData>();
        bool force = SkillExecutor.ForceSkillActivationForVfxReview;
        UnityEngine.Random.State random = UnityEngine.Random.state;
        try
        {
            var battle = host.AddComponent<BattleManager>();
            BattleCharacter Create(string name, SkillType skill, Vector2Int pos, bool ally)
            {
                var data = ScriptableObject.CreateInstance<CharacterData>();
                data.characterId = definitions.Count + 1;
                data.characterName = name;
                data.skillType = skill;
                data.skillChance = 0;
                definitions.Add(data);
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(host.transform, false);
                var unit = go.AddComponent<BattleCharacter>();
                unit.Init(data, pos, ally);
                battle.gridMap[pos] = unit;
                return unit;
            }

            var shadow = Create("影", SkillType.ShadowCopy, new(2, 3), true);
            var sword = Create("剣", SkillType.Slash, new(2, 2), false);
            var bell = Create("鈴", SkillType.BellEcho, new(1, 3), true);
            var bell2 = Create("鈴", SkillType.BellEcho, new(3, 3), true);
            Check(SupportSkillExecutor.CanEcho(bell, shadow), "adjacent friendly bell");
            Check(!SupportSkillExecutor.CanEcho(bell, sword), "no enemy support");
            Check(!SupportSkillExecutor.CanEcho(bell, bell), "cannot support itself");
            bell.isDead = true;
            Check(!SupportSkillExecutor.CanEcho(bell, shadow), "dead bell ignored");
            bell.isDead = false;
            bell.gridPos = new(0, 0);
            Check(!SupportSkillExecutor.CanEcho(bell, shadow), "distant bell ignored");
            bell.gridPos = new(1, 3);
            Check(SkillExecutor.TryExecuteDetailed(bell, sword, battle).Failure == SkillExecutionFailure.PassiveOrReactiveSkill,
                "bell does not consume active skill action");
            int repeats = 0;
            SupportSkillExecutor.TryEcho(shadow, sword, battle, _ => { repeats++; return false; });
            Check(repeats == 0, "zero chance never repeats");
            SkillExecutor.ForceSkillActivationForVfxReview = true;
            SupportSkillExecutor.TryEcho(shadow, sword, battle, target =>
            {
                Check(target == sword, "repeat resolves a valid target");
                repeats++;
                return false;
            });
            Check(repeats == 1, "overlapping bells only attempt one repeat");
            SkillExecutor.ForceSkillActivationForVfxReview = false;
            bell.data.skillChance = 25;
            bell2.data.skillChance = 40;
            int seed = 0;
            for (; seed < 1000; seed++)
            {
                UnityEngine.Random.InitState(seed);
                float roll = UnityEngine.Random.value;
                if (roll > 0.25f && roll < 0.4f) break;
            }
            Check(seed < 1000, "chance fixture found");
            UnityEngine.Random.InitState(seed);
            SupportSkillExecutor.TryEcho(shadow, sword, battle, _ => { repeats++; return false; });
            Check(repeats == 2, "highest-chance bell is selected");
            sword.isDead = true;
            SupportSkillExecutor.TryEcho(shadow, sword, battle, _ => { repeats++; return false; });
            Check(repeats == 2, "dead original target is not replaced");
            sword.isDead = false;

            Check(SupportSkillExecutor.CanCopy(shadow, sword), "basic adjacent form allowed");
            sword.data.isBoss = true;
            Check(!SupportSkillExecutor.CanCopy(shadow, sword), "boss rejected");
            sword.data.isBoss = false;
            sword.data.isEvolved = true;
            Check(!SupportSkillExecutor.CanCopy(shadow, sword), "evolution rejected");
            sword.data.isEvolved = false;
            sword.data.characterId = -1001;
            Check(!SupportSkillExecutor.CanCopy(shadow, sword), "fusion rejected");
            sword.data.characterId = 10;
            sword.attack = 77;
            sword.defense = 33;
            shadow.currentHP = 31;
            shadow.ApplyStun(2);
            int hp = shadow.maxHP;
            CharacterData original = shadow.data;
            Check(shadow.CopyBattleForm(sword), "copy succeeds");
            Check(shadow.currentHP == 31 && shadow.maxHP == hp && shadow.IsStunned(), "HP and stun preserved");
            Check(shadow.attack == 77 && shadow.defense == 33 && shadow.data.skillType == SkillType.Slash, "combat stats copied");
            Check(shadow.OriginalData == original && original.skillType == SkillType.ShadowCopy, "identity and shared definition preserved");
            Check(shadow.data != sword.data && shadow.data.characterId == 10, "copy is battle-local");
            Check(!shadow.CopyBattleForm(sword), "only one transformation per battle");
            Check(BattleRangeService.CanTargetCell(SkillType.ThreadPull, new(2, 4), new(4, 2), 6, 7), "thread range two");
            Check(!BattleRangeService.CanTargetCell(SkillType.ThreadPull, new(2, 4), new(2, 1), 6, 7), "thread range limited");
            Check(SkillExecutor.GetSkillChance(original, SkillType.ThreadPull) == 100, "thread is reliable");
            Check(!battle.PullCharacter(shadow, sword), "adjacent pull cannot enter caster cell");
        }
        finally
        {
            SkillExecutor.ForceSkillActivationForVfxReview = force;
            UnityEngine.Random.state = random;
            UnityEngine.Object.DestroyImmediate(host);
            foreach (CharacterData data in definitions) UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("[SupportSkillRegression] " + label);
    }
}
