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
    { public bool ally; public Vector2Int center; public int remaining = 2; public int recordToken; public float effect=1; public int damage; }
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
        ClearDiceBonuses(ally);
        foreach (RainZone zone in rainZones.Where(zone => zone.ally == ally).ToArray())
        {
            ResolveRainZone(zone); zone.remaining--;
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
                    ResolveMagnet(unit, opponents);
                    break;
                case SkillType.Fortress:
                    if (turn % 2 == 1)
                        foreach (BattleCharacter enemy in opponents.OrderBy(_ => Random.value).Take(2).ToArray())
                        {
                            StartCoroutine(SpriteProjectileVfxRoutine(unit.transform as RectTransform,
                                enemy.transform as RectTransform, "Gun", useArc: true));
                            PlaySupportBurst(enemy, "MortarImpact", .65f);
                            enemy.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(unit.GetEffectiveAttack(this) * .5f*unit.SkillEffectMultiplier)),
                                this, unit, actionLabel: "要塞の迫撃砲");
                        }
                    break;
                case SkillType.Dice: ResolveDice(unit, Random.Range(1, 7)); break;
                case SkillType.Sand: SpawnSandCopy(unit); break;
            }
        }
    }

    private void ResolveMagnet(BattleCharacter source, List<BattleCharacter> opponents)
    {
        var ordered = opponents.OrderBy(unit => BattleRangeService.Distance(source.gridPos, unit.gridPos))
            .ThenBy(unit => unit.InstanceId).ToArray();
        var selected = new List<BattleCharacter>();
        foreach (bool rear in new[] { false, true })
        {
            var target = ordered.FirstOrDefault(unit => AdvancedFusionExecutor.IsMagnetRear(unit.data.skillType) == rear);
            if (target != null) selected.Add(target);
        }
        selected.AddRange(ordered.Where(unit => !selected.Contains(unit)).Take(2 - selected.Count));
        foreach (var target in selected)
        {
            if (source.isDead || target.isDead || target.currentHP <= 0) continue;
            Vector2Int next = AdvancedFusionExecutor.MagnetDestination(source.gridPos, target.gridPos, target.data.skillType);
            PlaySupportMagnet(source, target);
            if (TryFusionDisplace(target, next))
                AddLog($"{source.DisplayName} の磁力で {target.DisplayName} を1マス移動！");
            else
            {
                PlaySupportBurst(target, "MortarImpact", .6f);
                target.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(source.GetEffectiveAttack(this)
                    * source.GetEffectiveSkillPower() * 2)), this, source, actionLabel: "磁力の衝突");
            }
        }
    }

    private void ClearDiceBonuses(bool ally)
    {
        foreach (var unit in gridMap.Values)
            if (unit != null && unit.isAlly == ally)
            { unit.DiceDefensePercent = 0; unit.RemoveAttackBuff(BattleAttackBuff.Dice); }
    }

    internal void ResolveDice(BattleCharacter dice, int face)
    {
        if (face < 1 || face > 6) throw new System.ArgumentOutOfRangeException(nameof(face));
        int bonus = AdvancedFusionExecutor.DiceBonusPercent(fusionTurn, dice.SkillEffectMultiplier);
        foreach (var friend in gridMap.Values.Where(friend => friend != null && !friend.isDead
            && friend.currentHP > 0 && friend.isAlly == dice.isAlly).ToArray())
        {
            if (face <= 2) HealFusion(friend, 1, dice.RecordToken);
            else if (face <= 4) friend.DiceDefensePercent = Mathf.Max(friend.DiceDefensePercent, bonus);
            else friend.ApplyAttackBuff(BattleAttackBuff.Dice, bonus, 2);
        }
        PlaySupportDice(dice, face);
        AddLog($"{dice.DisplayName} の賽子【{face}】！ {(face <= 2 ? "全体全回復" : face <= 4 ? $"防御+{bonus}%" : $"攻撃+{bonus}%")}");
    }

    private void HealFusion(BattleCharacter unit, float rate, int recordToken)
    {
        int amount = Mathf.Min(unit.maxHP - unit.currentHP, Mathf.Max(1, Mathf.RoundToInt(unit.maxHP * rate)));
        if (amount <= 0) return;
        unit.currentHP += amount; unit.UpdateHPBar(); PlayDamageVfx(unit, amount, isHealing: true);
        if (unit.isAlly) battleRecords.Credit(recordToken, healed: amount);
        PlaySupportBurst(unit, "RainDrops", .6f, .65f);
    }

    public void CastRain(BattleCharacter caster, Vector2Int center)
    {
        var zone = new RainZone { ally = caster.isAlly, center = center, recordToken = caster.RecordToken,
            effect=caster.SkillEffectMultiplier, damage=RainDamage(caster) };
        ResolveRainZone(zone); rainZones.Add(zone); RefreshRainMarkers();
        AddLog($"{caster.DisplayName} が恵みと攻撃の雨を設置（3×3・3回・重複可）");
    }

    private int RainDamage(BattleCharacter caster) => Mathf.Max(1, Mathf.RoundToInt(
        caster.GetEffectiveAttack(this) * .5f * caster.SkillEffectMultiplier));

    public Vector2Int FindRainCenter(BattleCharacter caster)
    {
        var units = gridMap.Values.Where(unit => unit != null && !unit.isDead && unit.currentHP > 0).ToArray();
        Vector2Int best = caster.gridPos;
        long bestScore = -1;
        int bestDistance = int.MaxValue;
        int damage = RainDamage(caster);
        for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++)
        {
            var center = new Vector2Int(x,y);
            long score = 0;
            foreach (var unit in units.Where(unit => BattleRangeService.Distance(center, unit.gridPos) <= 1))
                score += unit.isAlly == caster.isAlly
                    ? Mathf.Min(unit.maxHP-unit.currentHP, Mathf.RoundToInt(unit.maxHP*.1f*caster.SkillEffectMultiplier))
                    : Mathf.Min(unit.currentHP, damage);
            int distance = BattleRangeService.Distance(center, caster.gridPos);
            if (score > bestScore || (score == bestScore && distance < bestDistance))
            { best = center; bestScore = score; bestDistance = distance; }
        }
        return best;
    }

    private void ResolveRainZone(RainZone zone)
    {
        foreach (var unit in gridMap.Values.Where(unit => unit != null && !unit.isDead && unit.currentHP > 0
            && BattleRangeService.Distance(zone.center, unit.gridPos) <= 1).ToArray())
        {
            if (unit.isAlly == zone.ally) HealFusion(unit, .1f*zone.effect, zone.recordToken);
            else
            {
                int before = unit.currentHP;
                PlaySupportBurst(unit, "RainDrops", .6f, .65f);
                unit.TakeDamage(zone.damage, this, origin: BattleDamageOrigin.Periodic, actionLabel: "雨の持続ダメージ");
                if (zone.ally) battleRecords.Credit(zone.recordToken, damage: Mathf.Min(before, Mathf.Max(0, before-unit.currentHP)));
            }
        }
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
        int hp = sand ? Mathf.Max(1, Mathf.FloorToInt(source.maxHP*.5f*source.SkillEffectMultiplier)) : source.maxHP;
        int attack = Mathf.Max(1, Mathf.RoundToInt(source.attack * (sand ? .5f : .7f)*source.SkillEffectMultiplier));
        int defense = sand ? Mathf.FloorToInt(source.defense*.5f*source.SkillEffectMultiplier) : source.defense;
        var data = CharacterData.CreateBattleCopy(sand ? "砂" : "草", sand ? SkillType.None : SkillType.Grass, hp, attack, defense);
        fusionRuntimeData.Add(data);
        SpawnCharacter(data, empty.Value, source.isAlly, source.level);
        BattleCharacter copy = gridMap[empty.Value];
        RegisterCopyRecord(copy, source);
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
