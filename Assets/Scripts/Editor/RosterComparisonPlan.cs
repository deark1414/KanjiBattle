using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

// A performance screen, not a progression simulation: equal level, no investments, eight material slots.
public static class RosterComparisonPlan
{
    private static readonly int[] Stages = { 35, 36, 40 };
    private static readonly int[] Backbone = { 13, 10, 17, 20, 27, 18 };
    private static bool Rear(CharacterData data) => data.category == CharacterCategory.Ranged
        || data.skillType is SkillType.Fireball or SkillType.WaterHeal or SkillType.BellEcho
            or SkillType.WindShift or SkillType.ChainLightning or SkillType.Forge or SkillType.Magnet
            or SkillType.Dice or SkillType.Rain or SkillType.Sand or SkillType.Cocoon
            or SkillType.WindChime or SkillType.Lantern;

    public static CampaignBalanceRunner.Job[] Create(string directory, int seeds)
    {
        if (seeds < 1 || seeds > 10) throw new ArgumentOutOfRangeException(nameof(seeds));
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        var jobs = new List<CampaignBalanceRunner.Job>();
        var definitions = new List<object>();
        var recipes = BattleFusionRules.Recipes;
        File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonConvert.SerializeObject(new
        {
            level = 33, stages = Stages, seeds, materialSlots = 8, upgrades = false,
            characters = CastleTree.Nodes.Where(node => node.kind == CastleTree.Kind.Character)
                .Select(node => new { id = node.target, name = db.GetById(node.target).characterName }),
            recipes = recipes.Select(recipe => new { id = recipe.Id, name = recipe.Name,
                materials = recipe.Materials.Select(name => db.characters.Single(data => data.characterName == name).characterId) })
        }, Formatting.Indented));

        Vector2Int[] Place(List<CharacterData> party)
        {
            int front = 0, rear = 0; int[] columns = { 2, 3, 1, 4, 0, 5 };
            var preferred = party.Select(data => Rear(data) ? new Vector2Int(columns[rear++], 6)
                : new Vector2Int(columns[front++], 5)).ToArray();
            return CampaignBalanceRunner.SeparateFusionMaterials(party, preferred);
        }
        void Add(string profile, List<CharacterData> party, Vector2Int[] positions)
        {
            if (party.Count > 8 || positions.Distinct().Count() != positions.Length
                || positions.Any(cell => !BattleDeploymentRules.IsAllyCell(cell)))
                throw new Exception("Invalid comparison formation: " + profile);
            int budget = party.Sum(data => data.characterId > 0 ? 1 : BattleFusionRules.GetRecipe(data.characterId).Materials.Length);
            if (budget != 8) throw new Exception("Unequal material budget: " + profile);
            definitions.Add(new { profile, party = party.Select(data => data.characterId), positions, budget });
            foreach (int stage in Stages) for (int seed = 0; seed < seeds; seed++)
                jobs.Add(new CampaignBalanceRunner.Job { stage = stage, level = 33, seed = 1729 + seed * 7919,
                    profile = profile, variant = "baseline", comparison = true, layout = "comparison",
                    party = party.Select(data => data.characterId).ToArray(), allyPositions = positions });
        }
        foreach (var node in CastleTree.Nodes.Where(node => node.kind == CastleTree.Kind.Character))
        {
            var party = new[] { node.target }.Concat(Backbone).Append(11).Select(db.GetById).ToList();
            Add("normal/" + node.target, party, Place(party));
        }
        foreach (var recipe in recipes)
        {
            var materials = recipe.Materials.Select(name => db.characters.Single(data => data.characterName == name)).ToList();
            var control = materials.Concat(Backbone.Take(8 - materials.Count).Select(db.GetById)).ToList();
            var positions = Place(control);
            Add("fusion/" + recipe.Id + "/materials", control, positions);
            var fusion = CharacterData.CreateBattleFusion(recipe, materials.ToArray(), 33);
            try
            {
                var retained = positions.Skip(materials.Count).ToArray();
                var candidates = positions.Take(materials.Count).Concat(Enumerable.Range(0, 12)
                    .Select(index => BattleDeploymentRules.DefaultPosition(index, true))).Distinct()
                    .Where(cell => !retained.Contains(cell));
                Vector2Int origin = candidates.OrderBy(cell => cell.y == (Rear(fusion) ? 6 : 5) ? 0 : 1)
                    .ThenBy(cell => Mathf.Abs(cell.x - 2)).First();
                Add("fusion/" + recipe.Id + "/fused", new[] { fusion }.Concat(control.Skip(materials.Count)).ToList(),
                    new[] { origin }.Concat(retained).ToArray());
            }
            finally { UnityEngine.Object.Destroy(fusion); }
        }
        File.WriteAllText(Path.Combine(directory, "formations.json"), JsonConvert.SerializeObject(definitions, Formatting.Indented));
        return jobs.ToArray();
    }
}
