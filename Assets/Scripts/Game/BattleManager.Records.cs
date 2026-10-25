using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private readonly BattleRecordBook battleRecords = new();
    private UnityEngine.UI.Button resultRecordsButton;
    public BattleRecordBook BattleRecords => battleRecords;

    private void ConfigureRecordButton()
    {
        if (resultRecordsButton != null) return;
        resultRecordsButton = CharacterCodexUI.Command(resultPanel.transform, "OpenBattleRecords", "成績",
            new(.78f, .9f), new(.97f, .99f), () =>
            {
                bool previouslyPaused = isPaused;
                isPaused = true;
                CharacterCodexUI.OpenResults(transform, battleRecords, () => { if (this != null) isPaused = previouslyPaused; });
            });
    }

    private void RegisterBattleRecord(BattleCharacter unit)
    {
        CharacterRecords.Encounter(unit.data.characterId);
        if (!unit.isAlly) return;
        int id = unit.OriginalData.characterId;
        int[] owners = PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(unit.OriginalData)
            ? new[] { id } : System.Array.Empty<int>();
        battleRecords.Register(unit.GetInstanceID(), unit.data.characterName, unit.data.icon, owners);
        unit.RecordToken = unit.GetInstanceID();
    }

    public void RecordDamage(BattleCharacter target, BattleCharacter attacker, int loss, int prevention)
    {
        if (target.isAlly) battleRecords.Credit(target.RecordToken, received: loss, prevented: prevention);
        if (attacker != null && attacker.isAlly && !target.isAlly)
            battleRecords.Credit(attacker.RecordToken, damage: loss);
    }

    public void RecordGuard(BattleCharacter guard, int amount)
    { if (guard != null && guard.isAlly) battleRecords.Credit(guard.RecordToken, guarded: amount); }

    public void RecordHeal(BattleCharacter source, int amount)
    { if (source != null && source.isAlly) battleRecords.Credit(source.RecordToken, healed: amount); }

    public void RecordProtection(BattleCharacter target, int amount, bool lantern)
    {
        if (!target.isAlly) return;
        int token = target.CocoonRecordToken;
        if (lantern)
        {
            var source = gridMap.Values.Where(unit => unit != null && unit != target && !unit.isDead
                && unit.currentHP > 0 && unit.isAlly == target.isAlly && unit.data.skillType == SkillType.Lantern
                && BattleRangeService.Distance(unit.gridPos, target.gridPos) == 1)
                .OrderBy(unit => unit.InstanceId).FirstOrDefault();
            token = source != null ? source.RecordToken : 0;
        }
        battleRecords.Credit(token, prevented: amount);
    }

    private void RegisterFusionRecord(BattleCharacter unit, BattleFusionRules.Group group, BattleCharacter[] originals)
    {
        foreach (int member in group.members) battleRecords.Remove(originals[member].RecordToken);
        int[] owners = group.members.Select(index => deployedAllies[index])
            .Select(data => PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(data) ? data.characterId : 0).ToArray();
        battleRecords.Register(unit.GetInstanceID(), unit.data.characterName, unit.data.icon, owners);
        unit.RecordToken = unit.GetInstanceID();
    }

    private void RegisterCopyRecord(BattleCharacter copy, BattleCharacter source)
    {
        battleRecords.Remove(copy.GetInstanceID());
        copy.RecordToken = source.RecordToken;
    }
}
