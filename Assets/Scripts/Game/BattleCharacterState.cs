using UnityEngine;

// Runtime-only combat state. BattleCharacter remains responsible for Unity UI and presentation.
public sealed class BattleCharacterState
{
    public int Level { get; set; }
    public int CurrentHP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public bool IsDead { get; set; }
    public int NumberPassiveAttackBonusPercent { get; private set; }
    public int StunTurns { get; private set; }

    public void Initialize(CharacterData data, int level)
    {
        Level = level;
        CurrentHP = data.GetMaxHP(level);
        Attack = data.GetAttack(level);
        Defense = data.GetDefense(level);
        IsDead = false;
        NumberPassiveAttackBonusPercent = 0;
        StunTurns = 0;
    }

    public int GetEffectiveAttack()
    {
        return Mathf.RoundToInt(Attack * (1f + NumberPassiveAttackBonusPercent / 100f));
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
