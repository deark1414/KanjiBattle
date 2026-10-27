#if KANJI_BATTLE_E2E
using System;
using UnityEngine;

public partial class BattleManager
{
    internal void ProbeSupportVfx(string mode)
    {
        if (!IsDeploying || allies.Count < 2 || enemies.Count == 0)
            throw new InvalidOperationException("VFX review needs an isolated deployment fixture.");
        GameManager.Instance.SetBattleSpeedIndex(0);
        SyncBattleSpeedFromProgress();
        var before = JsonUtility.ToJson(UnityEngine.Random.state);
        BattleCharacter source = allies[0], target = allies[1];
        switch (mode)
        {
            case "dice": PlaySupportDice(source, 6); break;
            case "cocoon": target.CocoonShield = true; SetCocoonMarker(target, true); break;
            case "consume": target.CocoonShield = false; SetCocoonMarker(target, false); break;
            case "coal":
                var data = CharacterData.CreateBattleCopy("炭", SkillType.Coal, source.maxHP, source.attack, source.defense);
                data.icon = source.data.icon; data.enemyIcon = source.data.enemyIcon;
                fusionRuntimeData.Add(data); source.data = data; source.CoalAwake = false;
                RefreshSupportState(source); break;
            case "ignite": source.CoalAwake = true; RefreshSupportState(source); break;
            case "shell": PlaySupportBurst(target, "GuardShell", 1.2f); break;
            case "forge": PlaySupportForge(target); break;
            case "magnet": PlaySupportMagnet(source, enemies[0]); break;
            case "chain": PlaySupportLink(source, enemies[0], "ChainLightning"); break;
            case "clear": ClearSupportSkillVfx(); break;
            default: throw new ArgumentException("Unknown VFX review: " + mode);
        }
        if (before != JsonUtility.ToJson(UnityEngine.Random.state))
            throw new InvalidOperationException("Visual effects consumed combat random state.");
    }
}
#endif
