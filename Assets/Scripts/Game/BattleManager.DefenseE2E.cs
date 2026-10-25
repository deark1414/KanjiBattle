#if KANJI_BATTLE_E2E
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    internal void ProbeDefenseSkills()
    {
        if (!IsDeploying || allies.Count != 6 || allies[0].data.skillType != SkillType.GuardAlly)
            throw new System.InvalidOperationException("Defense deployment fixture required.");
        void Check(bool value, string label)
        {
            if (!value) throw new System.InvalidOperationException("Defense probe: " + label);
        }
        void Place(BattleCharacter unit, Vector2Int position)
        {
            unit.gridPos = position;
            gridMap[position] = unit;
            unit.transform.position = gridCells[position.x, position.y].position;
        }
        var guard = allies[0]; var shield = allies[1]; var wall = allies[2]; var second = allies[3];
        var enemyGuard = enemies[0]; var enemyShield = enemies[1]; var attacker = enemies[2];
        gridMap.Clear();
        Place(guard, new(1, 3)); Place(shield, new(2, 3)); Place(wall, new(0, 6)); Place(second, new(1, 4));
        Place(allies[4], new(5, 5)); Place(allies[5], new(5, 6));
        Place(enemyGuard, new(1, 1)); Place(enemyShield, new(2, 1)); Place(attacker, new(4, 0));
        guard.defense = 1000;
        int guardHP = guard.currentHP, shieldHP = shield.currentHP, otherHP = second.currentHP;
        shield.TakeDamage(100, this, attacker, ignoreDefense: true, isBasicAttack: true);
        Check(shieldHP - shield.currentHP == 60 && guardHP - guard.currentHP == 20, "mitigation then one guard share");
        Check(second.currentHP == otherHP, "no duplicate or chained sharing");
        var shieldRecord = battleRecords.Entries.Single(row => row.token == shield.RecordToken).result;
        var guardRecord = battleRecords.Entries.Single(row => row.token == guard.RecordToken).result;
        Check(shieldRecord.received == 360 && shieldRecord.prevented == 120,
            "records separate actual HP loss from mitigation");
        Check(guardRecord.received == 120 && guardRecord.guarded == 120 && guardRecord.prevented == 0,
            "guard sharing is not counted as prevention");

        guardHP = guard.currentHP; shieldHP = shield.currentHP;
        shield.TakeDamage(100, this, attacker, ignoreDefense: true, origin: BattleDamageOrigin.Reflected);
        Check(shieldHP - shield.currentHP == 80 && guardHP == guard.currentHP, "reflection is not redirected");

        bool force = SkillExecutor.ForceSkillActivationForVfxReview;
        try
        {
            SkillExecutor.ForceSkillActivationForVfxReview = true;
            int wallHP = wall.currentHP, attackerHP = attacker.currentHP;
            wall.defense = 1000;
            wall.TakeDamage(40, this, attacker, isBasicAttack: true, origin: BattleDamageOrigin.Transferred);
            Check(wallHP - wall.currentHP == 40 && attacker.currentHP == attackerHP, "transfer bypasses defense and reactive skills");
        }
        finally { SkillExecutor.ForceSkillActivationForVfxReview = force; }

        shieldHP = shield.currentHP; otherHP = second.currentHP;
        guard.currentHP = 3;
        shield.TakeDamage(100, this, attacker, ignoreDefense: true);
        Check(guard.isDead && !gridMap.ContainsValue(guard) && shieldHP - shield.currentHP == 77,
            "guard can die and cannot absorb more than remaining HP");
        Check(second.currentHP == otherHP, "guard death does not transfer the same hit again");

        int enemyHP = enemyShield.currentHP, enemyGuardHP = enemyGuard.currentHP;
        enemyShield.TakeDamage(100, this, shield, ignoreDefense: true);
        Check(enemyHP - enemyShield.currentHP == 60 && enemyGuardHP - enemyGuard.currentHP == 20,
            "enemy side uses the same protection rules");
        Check(!SkillExecutor.TryExecute(shield, attacker, this), "shield does not use an active skill action");
        AddLog("防御能力チェック完了");
    }
}
#endif
