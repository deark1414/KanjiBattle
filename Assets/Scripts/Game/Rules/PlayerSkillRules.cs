using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PlayerSkillRules
{
    public const int MaxTurns = 50;
    public const string SaveKey = "KanjiBattle.PlayerSkill.Plan.v1";
    public enum Effect { None, Attack, Guard, Heal }
    public sealed class Definition
    {
        public readonly int id, unlockStage, percent, duration;
        public readonly string name;
        public readonly Effect effect;
        public Definition(int id, string name, Effect effect, int unlockStage, int percent, int duration)
        { this.id=id; this.name=name; this.effect=effect; this.unlockStage=unlockStage; this.percent=percent; this.duration=duration; }
        public string Detail => effect switch
        {
            Effect.Attack => $"味方全体の攻撃 +{percent}%・{duration}ターン",
            Effect.Guard => $"味方全体の被ダメージ -{percent}%・{duration}ターン",
            Effect.Heal => $"味方全体のHPを最大HPの{percent}%回復・即時",
            _ => "使用なし"
        };
    }
    // Initial values for playtesting; not final stage-balance targets.
    public static readonly IReadOnlyList<Definition> Definitions = new[]
    {
        new Definition(0, "なし", Effect.None, 0, 0, 0),
        new Definition(1, "鼓舞", Effect.Attack, 0, 30, 2),
        new Definition(2, "堅守", Effect.Guard, 5, 30, 1),
        new Definition(3, "癒し", Effect.Heal, 10, 25, 0)
    };
    [Serializable] public sealed class Plan
    {
        public int skillId;
        public int turn = 1;
        public Plan Copy() => new() { skillId = skillId, turn = turn };
    }
    public static Definition Get(int id) => Definitions.FirstOrDefault(item => item.id == id);
    public static bool IsValid(Plan plan, int cleared) => plan != null && Get(plan.skillId) is Definition item
        && cleared >= item.unlockStage && plan.turn >= 1 && plan.turn <= MaxTurns;
    public static string Summary(Plan plan) => Get(plan?.skillId ?? -1) is Definition item
        ? item.id == 0 ? "プレイヤースキル：なし" : $"{item.name}　第{plan.turn}ターン"
        : "プレイヤースキル：設定を選び直してください";
    public static Plan Load()
    {
        string json = PlayerProgressStore.GetString(SaveKey);
        if (string.IsNullOrEmpty(json)) return new Plan();
        try { return JsonUtility.FromJson<Plan>(json) ?? new Plan { skillId=-1 }; }
        catch (ArgumentException) { return new Plan { skillId=-1 }; }
    }
    public static void Save(Plan plan, int cleared)
    {
        if (!IsValid(plan, cleared)) throw new ArgumentException("Invalid player skill plan.");
        PlayerProgressStore.SetString(SaveKey, JsonUtility.ToJson(plan)); PlayerProgressStore.Save();
    }
    public static void ResetProgress() { PlayerProgressStore.Delete(SaveKey); PlayerProgressStore.Save(); }

    public sealed class BattleState
    {
        public Plan Plan { get; private set; } = new();
        public bool Fired { get; private set; }
        public bool Finished { get; private set; }
        public int Remaining { get; private set; }
        public Definition Definition => Get(Plan.skillId);
        public bool Active => !Finished && Remaining > 0;
        public void Reset(Plan plan) { Plan=(plan ?? new Plan()).Copy(); Fired=false; Finished=false; Remaining=0; }
        public bool Begin(int turn, bool bothSidesAlive)
        {
            if (Finished || Fired || !bothSidesAlive || Definition == null || Definition.id == 0
                || Plan.turn < 1 || Plan.turn > MaxTurns || turn != Plan.turn) return false;
            Fired=true; Remaining=Definition.duration; return true;
        }
        public void EndTurn() { if (Active) Remaining--; }
        public void Finish() { Finished=true; Remaining=0; }
        public int AttackPercent => Active && Definition.effect == Effect.Attack ? Definition.percent : 0;
        public int ReduceDamage(int damage) => Active && Definition.effect == Effect.Guard && damage > 0
            ? Mathf.Max(1, Mathf.FloorToInt(damage * (100 - Definition.percent) / 100f)) : damage;
    }
}
