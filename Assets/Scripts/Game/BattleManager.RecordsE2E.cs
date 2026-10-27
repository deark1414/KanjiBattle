#if KANJI_BATTLE_E2E
using System;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    internal void ProbeBattleRecords()
    {
        if (!IsDeploying) throw new InvalidOperationException("Records probe requires deployment.");
        void Check(bool value, string label) { if (!value) throw new Exception("Records probe: " + label); }
        var attacker = allies[0]; var target = enemies[0];
        target.currentHP = 10; target.defense = 0;
        target.TakeDamage(10000, this, attacker, ignoreDefense: true);
        Check(battleRecords.Entries.Single(row => row.token == attacker.RecordToken).result.damage == 60,
            "actual loss excludes overkill");
        if (allies.Any(unit => unit.data.skillType == SkillType.WaterHeal))
        {
            var healer = allies.First(unit => unit.data.skillType == SkillType.WaterHeal);
            healer.currentHP = healer.maxHP - 3;
            Check(SkillExecutor.TryExecute(healer, healer, this), "real healing dispatcher");
            Check(battleRecords.Entries.Single(row => row.token == healer.RecordToken).result.healed == 18,
                "actual heal excludes overheal");
        }
        // Finish via the actual settlement path; a second call must be ignored.
        ShowResult("勝利！", Color.green, true);
        int wins = CharacterRecords.Get(attacker.OriginalData.characterId).wins;
        ShowResult("勝利！", Color.green, true);
        Check(wins == 1 && CharacterRecords.Get(attacker.OriginalData.characterId).wins == wins, "settlement is idempotent");
    }
}
#endif
