#if KANJI_BATTLE_E2E
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    internal void ProbeFusionSupport()
    {
        if (IsDeploying || allies.Count != 4 || enemies.Count != 4)
            throw new System.InvalidOperationException("Started support fusion fixture required.");
        void Check(bool value, string label)
        {
            if (!value) throw new System.InvalidOperationException("Fusion support probe: " + label);
        }
        void Place(BattleCharacter unit, Vector2Int position)
        {
            unit.gridPos = position; gridMap[position] = unit;
            unit.transform.position = gridCells[position.x, position.y].position;
        }
        gridMap.Clear();
        foreach (BattleCharacter unit in allies)
        {
            unit.currentHP = unit.maxHP;
            for (int i = 0; i < 3; i++) unit.TickAttackBuffs();
        }
        var warrior = allies.FirstOrDefault(unit => unit.data.skillType == SkillType.Warrior);
        if (warrior != null)
        {
            var guard = allies.Single(unit => unit.data.skillType == SkillType.Sentinel);
            var water = allies.Single(unit => unit.data.skillType == SkillType.WaterHeal);
            var number = allies.Single(unit => unit.data.skillType == SkillType.NumberPassive);
            Place(warrior, new(1, 4)); Place(guard, new(2, 4)); Place(water, new(2, 5)); Place(number, new(5, 6));
            Place(enemies[0], new(1, 3)); Place(enemies[1], new(2, 3));
            Place(enemies[2], new(4, 0)); Place(enemies[3], new(5, 0));
            warrior.currentHP -= 50; guard.currentHP -= 50; water.currentHP -= 50; number.currentHP -= 50;
            var nearby = new[] { warrior, guard, water };
            int[] hp = nearby.Select(unit => unit.currentHP).ToArray(); int farHP = number.currentHP;
            FusionSupportExecutor.BeginSideTurn(this, true, 1);
            Check(warrior.AttackBuffPercent == 20 && allies.Count(unit => unit.AttackBuffPercent == 20) == 2,
                "warrior buffs exactly self and one friendly unit");
            for (int i = 0; i < nearby.Length; i++)
                Check(nearby[i].currentHP - hp[i] == Mathf.Min(50, Mathf.Max(1, Mathf.RoundToInt(nearby[i].maxHP * .04f))),
                    "guard heals each adjacent friendly " + i);
            Check(number.currentHP == farHP, "distant unit is not healed");
            FusionSupportExecutor.BeginSideTurn(this, true, 1);
            Check(allies.All(unit => unit.AttackBuffPercent <= 20), "same war cry does not stack");
            FusionSupportExecutor.EndSideTurn(this, false);
            Check(warrior.AttackBuffPercent == 20, "opponent end does not shorten buff");
            FusionSupportExecutor.EndSideTurn(this, true);
            Check(warrior.AttackBuffPercent == 20, "buff remains through second action phase");
            FusionSupportExecutor.EndSideTurn(this, true);
            Check(allies.All(unit => unit.AttackBuffPercent == 0), "war cry expires after two phases");
            int before = enemies[0].currentHP;
            Check(SkillExecutor.TryExecute(warrior, enemies[0], this) && enemies[0].currentHP < before,
                "warrior real dispatcher attack");
            int health = warrior.currentHP;
            warrior.TakeDamage(100, this, enemies[2], ignoreDefense: true);
            Check(health - warrior.currentHP == 80, "warrior shield");
            gridMap.Remove(enemies[0].gridPos); Place(enemies[0], new(2, 2));
            gridMap.Remove(enemies[1].gridPos); Place(enemies[1], new(5, 2));
            Check(SkillExecutor.TryExecute(guard, enemies[0], this), "guard spear reaches two");
            gridMap.Remove(water.gridPos); Place(water, new(2, 3));
            Check(!SkillExecutor.TryExecute(guard, enemies[0], this), "guard spear respects friend blocking");
        }
        else
        {
            var forge = allies.Single(unit => unit.data.skillType == SkillType.Forge);
            var blades = allies.Single(unit => unit.data.skillType == SkillType.DoubleSlash);
            var water = allies.Single(unit => unit.data.skillType == SkillType.WaterHeal);
            var guard = allies.Single(unit => unit.data.skillType == SkillType.GuardAlly);
            Place(forge, new(0, 5)); Place(blades, new(3, 5)); Place(water, new(5, 6)); Place(guard, new(5, 5));
            for (int i = 0; i < 4; i++) Place(enemies[i], new(i, 0));
            FusionSupportExecutor.BeginSideTurn(this, true, 1);
            Check(blades.AttackBuffPercent == 25 && forge.AttackBuffPercent == 0
                && water.AttackBuffPercent == 0 && guard.AttackBuffPercent == 0, "forge only strengthens eligible weapons");
            FusionSupportExecutor.BeginSideTurn(this, true, 1);
            Check(blades.AttackBuffPercent == 25, "forge cannot stack itself");
            for (int i = 0; i < 2; i++) FusionSupportExecutor.EndSideTurn(this, true);
            Check(blades.AttackBuffPercent == 25, "forge lasts three phases");
            FusionSupportExecutor.EndSideTurn(this, true);
            Check(blades.AttackBuffPercent == 0, "forge expires");
            FusionSupportExecutor.BeginSideTurn(this, true, 3);
            Check(blades.AttackBuffPercent == 0, "no out-of-cycle application");
            FusionSupportExecutor.BeginSideTurn(this, true, 4);
            Check(blades.AttackBuffPercent == 25, "forge renews on fourth turn");
            Check(!SkillExecutor.TryExecute(forge, enemies[0], this), "forge support does not consume active skill action");
        }
        Check(enemies.All(unit => unit.AttackBuffPercent == 0), "no enemy receives friendly support");
        AddLog("支援融合の能力チェック完了");
    }
}
#endif
