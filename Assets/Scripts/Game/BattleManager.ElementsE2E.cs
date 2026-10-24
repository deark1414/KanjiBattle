#if KANJI_BATTLE_E2E
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    internal void ProbeElementSkills()
    {
        if (!IsDeploying || allies.Count != 6 || allies[0].data.skillType != SkillType.ChainLightning || enemies.Count != 6)
            throw new System.InvalidOperationException("Element fixture required.");
        void Check(bool value, string label)
        {
            if (!value) throw new System.InvalidOperationException("Element probe: " + label);
        }
        void Place(BattleCharacter unit, Vector2Int pos)
        {
            if (gridMap.TryGetValue(unit.gridPos, out var previous) && previous == unit) gridMap.Remove(unit.gridPos);
            unit.gridPos = pos; gridMap[pos] = unit;
            unit.transform.position = gridCells[pos.x, pos.y].position;
        }
        gridMap.Clear();
        var electric = allies[0]; var wind = allies[1]; var iron = allies[2]; var horse = allies[3];
        Place(electric, new(0, 5)); Place(wind, new(5, 6)); Place(iron, new(4, 6));
        Place(horse, new(3, 6)); Place(allies[4], new(1, 3)); Place(allies[5], new(2, 6));
        for (int i = 0; i < 5; i++) Place(enemies[i], new(i, 2));
        Place(enemies[5], new(5, 0));
        electric.attack = 40;
        foreach (var enemy in enemies) enemy.defense = 0;
        int[] before = enemies.Select(unit => unit.currentHP).ToArray();
        int friendHP = allies[4].currentHP;
        Check(SkillExecutor.TryExecute(electric, enemies[0], this), "reliable chain attack");
        for (int i = 0; i < 4; i++)
            Check(before[i] - enemies[i].currentHP == Mathf.RoundToInt(40 * Mathf.Pow(0.75f, i)), "chain decay and no repeat " + i);
        Check(enemies[4].currentHP == before[4] && allies[4].currentHP == friendHP,
            "four-target cap and no friendly fire");
        Check(!SkillExecutor.TryExecute(electric, allies[4], this), "friendly target rejected");
        Check(!SkillExecutor.TryExecute(electric, enemies[5], this), "initial range enforced");

        foreach (var enemy in enemies) enemy.currentHP = enemy.maxHP;
        Place(allies[4], new(0, 6)); Place(iron, new(2, 3));
        Place(enemies[0], new(2, 2)); Place(enemies[1], new(3, 2));
        Place(enemies[2], new(0, 0)); Place(enemies[3], new(1, 0)); Place(enemies[4], new(4, 0));
        iron.attack = 20;
        ElementSkillExecutor.SelectIronForm(iron, this);
        Check(iron.IronForm == IronForm.Sword, "wedge chooses sword");
        int first = enemies[0].currentHP, second = enemies[1].currentHP;
        Check(SkillExecutor.TryExecute(iron, enemies[0], this)
            && enemies[0].currentHP < first && enemies[1].currentHP < second, "sword hits its wedge");
        Place(enemies[0], new(2, 1)); Place(enemies[1], new(5, 2));
        ElementSkillExecutor.SelectIronForm(iron, this);
        Check(iron.IronForm == IronForm.Spear && SkillExecutor.TryExecute(iron, enemies[0], this), "spear reaches two cells");
        Place(allies[4], new(2, 2));
        Check(!SkillExecutor.TryExecute(iron, enemies[0], this), "spear blocked by ally");
        Place(allies[4], new(0, 6));
        iron.currentHP = iron.maxHP / 2;
        ElementSkillExecutor.SelectIronForm(iron, this);
        Check(iron.IronForm == IronForm.Shield, "low HP overrides attack forms");
        int hp = iron.currentHP;
        iron.TakeDamage(40, this, enemies[0], ignoreDefense: true);
        Check(hp - iron.currentHP == 30 && !SkillExecutor.TryExecute(iron, enemies[0], this), "shield only mitigates");
        iron.currentHP = iron.maxHP;
        ElementSkillExecutor.SelectIronForm(iron, this);
        Check(iron.IronForm == IronForm.Spear, "recovery allows another form");

        gridMap.Clear();
        Place(electric, new(0, 6)); Place(wind, new(1, 5)); Place(iron, new(5, 6));
        Place(horse, new(2, 4)); Place(allies[4], new(4, 6)); Place(allies[5], new(5, 5));
        for (int i = 0; i < 6; i++) Place(enemies[i], new(i, 0));
        ElementSkillExecutor.EndSideTurn(this, true); ElementSkillExecutor.EndSideTurn(this, false);
        ElementSkillExecutor.ApplyWind(this, true, impassableCells);
        Check(wind.WindBoost && horse.WindBoost && horse.MovementModifier == 1, "wind selects reachable animal");
        ElementSkillExecutor.EndSideTurn(this, true);
        Check(!wind.WindBoost && !horse.WindBoost, "boost expires after action side");
        ElementSkillExecutor.ApplyWind(this, false, impassableCells);
        Check(allies.Count(unit => unit.WindSlow) == 1, "enemy wind applies headwind");
        ElementSkillExecutor.EndSideTurn(this, false);
        Check(allies.Count(unit => unit.WindSlow) == 1, "enemy-side end preserves pending allied debuff");
        ElementSkillExecutor.EndSideTurn(this, true);
        Check(allies.All(unit => !unit.WindSlow), "debuff expires after affected side acts");
        AddLog("電風鉄の能力チェック完了");
    }
}
#endif
