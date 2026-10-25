using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleManager
{
    private int fusionTurn;
    public int CurrentBattleTurn => fusionTurn;
    private readonly Dictionary<int, int> grassSplits = new();
    private sealed class RainZone
    { public bool ally; public Vector2Int center; public int remaining = 2; }
    private readonly List<RainZone> rainZones = new();
    private readonly Dictionary<Vector2Int, GameObject> rainMarkers = new();

    public void SetCocoonMarker(BattleCharacter target, bool active)
    {
        if (target != null) SetSupportCocoon(target, active);
    }

    private void ClearAdvancedFusionState()
    {
        ClearSupportSkillVfx();
        fusionTurn = 0; grassSplits.Clear(); rainZones.Clear();
        foreach (GameObject marker in rainMarkers.Values) if (marker != null) Destroy(marker);
        rainMarkers.Clear();
    }

    public bool TryFusionDisplace(BattleCharacter target, Vector2Int destination)
    {
        if (target == null || target.isDead || !IsCellFree(destination)) return false;
        MoveCharacter(target, destination, target.isAlly ? "味方" : "敵");
        return true;
    }

    private void BeginAdvancedFusionTurn(bool ally, int turn)
    {
        ClearDiceDefense(ally);
        foreach (RainZone zone in rainZones.Where(zone => zone.ally == ally).ToArray())
        {
            HealRainZone(zone); zone.remaining--;
            if (zone.remaining <= 0) rainZones.Remove(zone);
        }
        RefreshRainMarkers();
        foreach (BattleCharacter unit in gridMap.Values.Where(unit => unit != null && !unit.isDead
            && unit.currentHP > 0 && unit.isAlly == ally).ToArray())
        {
            if (unit.IsStunned() || unit.EligibleTurn > turn) continue;
            var opponents = gridMap.Values.Where(enemy => enemy != null && !enemy.isDead
                && enemy.currentHP > 0 && enemy.isAlly != ally).ToList();
            switch (unit.data.skillType)
            {
                case SkillType.Magnet:
                    if (opponents.Count > 0)
                    {
                        var enemy = opponents[Random.Range(0, opponents.Count)];
                        Vector2Int next = enemy.gridPos + BattleRangeService.NormalizeDirection(unit.gridPos - enemy.gridPos);
                        if (TryFusionDisplace(enemy, next))
                        {
                            PlaySupportMagnet(unit, enemy);
                            AddLog($"{unit.DisplayName} の磁力で1マス引き寄せた！");
                        }
                    }
                    break;
                case SkillType.Fortress:
                    if (turn % 2 == 1)
                        foreach (BattleCharacter enemy in opponents.OrderBy(_ => Random.value).Take(2).ToArray())
                        {
                            StartCoroutine(SpriteProjectileVfxRoutine(unit.transform as RectTransform,
                                enemy.transform as RectTransform, "Gun", useArc: true));
                            PlaySupportBurst(enemy, "MortarImpact", .65f);
                            enemy.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(unit.GetEffectiveAttack(this) * .5f)),
                                this, unit, actionLabel: "要塞の迫撃砲");
                        }
                    break;
                case SkillType.Dice: ResolveDice(unit, Random.Range(1, 7)); break;
                case SkillType.Sand: SpawnSandCopy(unit); break;
            }
        }
    }

    private void ClearDiceDefense(bool ally)
    {
        foreach (var unit in gridMap.Values)
            if (unit != null && unit.isAlly == ally) unit.DiceDefensePercent = 0;
    }

    internal void ResolveDice(BattleCharacter dice, int face)
    {
        foreach (var friend in gridMap.Values.Where(friend => friend != null && !friend.isDead
            && friend.currentHP > 0 && friend.isAlly == dice.isAlly).ToArray())
        {
            if (face <= 2) HealFusion(friend, .06f);
            else if (face <= 4) friend.DiceDefensePercent = 20;
            else friend.ApplyAttackBuff(BattleAttackBuff.Dice, 20, 1);
        }
        PlaySupportDice(dice, face);
        AddLog($"{dice.DisplayName} の賽子【{face}】！ {(face <= 2 ? "全体回復" : face <= 4 ? "防御+20%" : "攻撃+20%")}");
    }

    private void HealFusion(BattleCharacter unit, float rate)
    {
        int amount = Mathf.Min(unit.maxHP - unit.currentHP, Mathf.Max(1, Mathf.RoundToInt(unit.maxHP * rate)));
        if (amount <= 0) return;
        unit.currentHP += amount; unit.UpdateHPBar(); PlayDamageVfx(unit, amount, isHealing: true);
        PlaySupportBurst(unit, "RainDrops", .6f, .65f);
    }

    public void CastRain(BattleCharacter caster, Vector2Int center)
    {
        var zone = new RainZone { ally = caster.isAlly, center = center };
        HealRainZone(zone); rainZones.Add(zone); RefreshRainMarkers();
        AddLog($"{caster.DisplayName} が回復の雨を設置（3回・重複可）");
    }

    private void HealRainZone(RainZone zone)
    {
        foreach (var unit in gridMap.Values.Where(unit => unit != null && !unit.isDead && unit.currentHP > 0
            && unit.isAlly == zone.ally && BattleRangeService.Distance(zone.center, unit.gridPos) <= 1).ToArray())
            HealFusion(unit, .03f);
    }

    private void RefreshRainMarkers()
    {
        if (gridCells == null) return;
        var cells = new HashSet<Vector2Int>();
        foreach (RainZone zone in rainZones)
            for (int x = 0; x < cols; x++) for (int y = 0; y < rows; y++)
                if (BattleRangeService.Distance(zone.center, new(x, y)) <= 1) cells.Add(new(x, y));
        foreach (var cell in rainMarkers.Keys.Where(cell => !cells.Contains(cell)).ToArray())
        { if (rainMarkers[cell] != null) Destroy(rainMarkers[cell]); rainMarkers.Remove(cell); }
        foreach (var cell in cells.Where(cell => !rainMarkers.ContainsKey(cell)))
        {
            var go = new GameObject("RainArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(gridCells[cell.x, cell.y], false); go.transform.SetAsFirstSibling();
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>("VFX/SupportSkills/RainRipple");
            image.color = new Color(1f, 1f, 1f, .7f); image.raycastTarget = false;
            rainMarkers[cell] = go;
        }
    }

    internal BattleCharacter SpawnSandCopy(BattleCharacter source)
    {
        if (source.IsGenerated || gridMap.Values.Count(unit => unit != null && !unit.isDead && unit.Summoner == source) >= 3) return null;
        return SpawnFusionCopy(source, true);
    }

    public void SplitGrass(BattleCharacter source)
    {
        grassSplits.TryGetValue(source.LineageId, out int count);
        if (count >= 3 || source.currentHP <= 0) return;
        if (SpawnFusionCopy(source, false) != null) grassSplits[source.LineageId] = count + 1;
    }

    private BattleCharacter SpawnFusionCopy(BattleCharacter source, bool sand)
    {
        Vector2Int? empty = null;
        for (int y = -1; y <= 1 && !empty.HasValue; y++) for (int x = -1; x <= 1; x++)
        {
            var cell = source.gridPos + new Vector2Int(x, y);
            if (IsCellFree(cell)) { empty = cell; break; }
        }
        if (!empty.HasValue) return null;
        int hp = sand ? Mathf.Max(1, source.maxHP / 2) : source.maxHP;
        int attack = Mathf.Max(1, Mathf.RoundToInt(source.attack * (sand ? .5f : .7f)));
        int defense = sand ? source.defense / 2 : source.defense;
        var data = CharacterData.CreateBattleCopy(sand ? "砂" : "草", sand ? SkillType.None : SkillType.Grass, hp, attack, defense);
        fusionRuntimeData.Add(data);
        SpawnCharacter(data, empty.Value, source.isAlly, source.level);
        BattleCharacter copy = gridMap[empty.Value];
        copy.SetBattleVitals(hp, sand ? hp : source.currentHP, attack, defense);
        copy.IsGenerated = true; copy.Summoner = sand ? source : null;
        copy.LineageId = source.LineageId; copy.EligibleTurn = fusionTurn + 1;
        PlaySupportBurst(copy, sand ? "SandBloom" : "GrassSprout", .7f);
        AddLog($"{source.DisplayName} が{(sand ? "砂の分身を生成" : "分裂")}！");
        return copy;
    }

    private void RemoveFusionSummons(BattleCharacter source)
    {
        foreach (BattleCharacter copy in gridMap.Values.Where(unit => unit != null && unit.Summoner == source).ToArray())
            HandleDeath(copy);
    }
}
