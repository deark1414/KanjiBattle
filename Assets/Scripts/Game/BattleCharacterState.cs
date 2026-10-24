using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum BattleAttackBuff { Warrior, Forge, Dice }

// Runtime-only combat state. BattleCharacter remains responsible for Unity UI and presentation.
public sealed class BattleCharacterState
{
    public int Level { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; private set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public bool IsDead { get; set; }
    public int NumberPassiveAttackBonusPercent { get; private set; }
    public int StunTurns { get; private set; }
    public bool WindBoost { get; set; }
    public bool WindSlow { get; set; }
    public IronForm IronForm { get; set; }
    public bool CoalAwake { get; set; }
    public bool CocoonShield { get; set; }
    public int DiceDefensePercent { get; set; }
    private readonly Dictionary<BattleAttackBuff, (int percent, int turns)> attackBuffs = new();

    public void Initialize(CharacterData data, int level, bool isAlly)
    {
        Level = level;
        float healthMultiplier = isAlly && FacilityManager.Instance != null
            ? FacilityManager.Instance.GetHealthMultiplier()
            : 1f;
        MaxHP = Mathf.Max(1, Mathf.RoundToInt(data.GetMaxHP(level) * healthMultiplier));
        CurrentHP = MaxHP;
        Attack = data.GetAttack(level);
        Defense = data.GetDefense(level);
        IsDead = false;
        NumberPassiveAttackBonusPercent = 0;
        StunTurns = 0;
        WindBoost = WindSlow = false;
        IronForm = IronForm.Sword;
        attackBuffs.Clear();
        CoalAwake = CocoonShield = false;
        DiceDefensePercent = 0;
    }

    public int GetEffectiveAttack()
    {
        return Mathf.RoundToInt(Attack * (1f + (NumberPassiveAttackBonusPercent + AttackBuffPercent + (CoalAwake ? 40 : 0)) / 100f));
    }

    public void SetVitals(int hp, int currentHP, int attack, int defense)
    {
        MaxHP = Mathf.Max(1, hp); CurrentHP = Mathf.Clamp(currentHP, 1, MaxHP);
        Attack = Mathf.Max(1, attack); Defense = Mathf.Max(0, defense);
    }

    public int AttackBuffPercent => attackBuffs.Values.Sum(buff => buff.percent);

    public void ApplyAttackBuff(BattleAttackBuff source, int percent, int turns)
    {
        if (percent <= 0 || turns <= 0) return;
        attackBuffs.TryGetValue(source, out var existing);
        attackBuffs[source] = (Mathf.Max(existing.percent, percent), Mathf.Max(existing.turns, turns));
    }

    public void TickAttackBuffs()
    {
        foreach (var source in attackBuffs.Keys.ToArray())
        {
            var buff = attackBuffs[source];
            if (buff.turns <= 1) attackBuffs.Remove(source);
            else attackBuffs[source] = (buff.percent, buff.turns - 1);
        }
    }

    public void SetNumberPassiveAttackBonus(int percent)
    {
        NumberPassiveAttackBonusPercent = Mathf.Max(0, percent);
    }

    public void ApplyStun(int turns)
    {
        StunTurns = Mathf.Max(StunTurns, turns);
    }

    public void TickStun()
    {
        if (StunTurns > 0) StunTurns--;
    }
}
