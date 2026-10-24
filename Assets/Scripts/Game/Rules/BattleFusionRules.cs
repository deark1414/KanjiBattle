using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BattleFusionRules
{
    public sealed class Recipe
    {
        public int Id { get; }
        public string Name { get; }
        public string[] Materials { get; }
        public SkillType Skill { get; }
        public CharacterCategory Category { get; }
        public Vector3 StatMultipliers { get; }
        public bool ForgeEligible { get; }
        public Recipe(int id, string name, SkillType skill, CharacterCategory category, Vector3 multipliers,
            bool forgeEligible, params string[] materials)
        {
            Id = id; Name = name; Skill = skill; Materials = materials;
            Category = category; StatMultipliers = multipliers; ForgeEligible = forgeEligible;
        }
    }

    // Only recipes with implemented battle effects are selectable.
    public static readonly IReadOnlyList<Recipe> Recipes = new[]
    {
        new Recipe(-1001, "双剣", SkillType.DoubleSlash, CharacterCategory.Weapon, new(3.5f, 1.5f, 1.2f), true, "二", "剣"),
        new Recipe(-1002, "散弾", SkillType.ScatterShot, CharacterCategory.Ranged, new(3.5f, 1.5f, 1.2f), true, "三", "銃"),
        new Recipe(-1003, "戦士", SkillType.Warrior, CharacterCategory.Weapon, new(4f, 1.2f, 1.4f), true, "剣", "盾"),
        new Recipe(-1004, "衛兵", SkillType.Sentinel, CharacterCategory.Defense, new(4f, 1.2f, 1.4f), true, "槍", "盾"),
        new Recipe(-1005, "鍛冶", SkillType.Forge, CharacterCategory.Weapon, new(3f, 1.2f, 1.1f), false, "火", "槌")
    };

    [Serializable] public sealed class Group
    {
        public int recipeId;
        public int[] members;
        public int origin;
    }

    [Serializable] public sealed class Plan
    {
        public int[] partyIds;
        public List<Group> groups = new();
    }

    private const string PlanKey = "KanjiBattle.Fusion.AdjacentChoices.v2";
    private const string DiscoveryPrefix = "KanjiBattle.Fusion.Discovered.";

    public static Recipe GetRecipe(int id) => Recipes.FirstOrDefault(recipe => recipe.Id == id);

    public static List<Group> Candidates(IReadOnlyList<CharacterData> party)
    {
        var result = new List<Group>();
        for (int first = 0; first < party.Count; first++)
        for (int second = first + 1; second < party.Count; second++)
        foreach (Recipe recipe in Recipes)
        {
            var names = new[] { party[first].characterName, party[second].characterName }.OrderBy(value => value);
            if (names.SequenceEqual(recipe.Materials.OrderBy(value => value)))
                result.Add(new Group { recipeId = recipe.Id, members = new[] { first, second }, origin = first });
        }
        return result;
    }

    public sealed class Resolution
    {
        public Plan plan;
        public List<Group> candidates;
        public List<Group> conflicts;
        public List<Group> alternatives;
    }

    public static bool SameGroup(Group left, Group right) => left.recipeId == right.recipeId
        && left.members.OrderBy(index => index).SequenceEqual(right.members.OrderBy(index => index));

    public static bool Overlaps(Group left, Group right) => left.members.Intersect(right.members).Any();

    public static Resolution ResolveAdjacent(IReadOnlyList<CharacterData> party, IReadOnlyList<Vector2Int> positions, Plan choices)
    {
        if (party == null || positions == null || party.Count != positions.Count)
            throw new ArgumentException("Fusion positions must match the original party.");
        var candidates = Candidates(party).Where(group => IsConnected(group.members, positions)).ToList();
        foreach (Group candidate in candidates)
            candidate.origin = candidate.members.OrderBy(index => positions[index].y).ThenBy(index => positions[index].x).First();
        var plan = Empty(party);
        if (IsValid(party, choices))
            foreach (Group choice in choices.groups)
                if (candidates.Any(candidate => SameGroup(candidate, choice)))
                    plan.groups.Add(new Group { recipeId = choice.recipeId, members = (int[])choice.members.Clone(), origin = choice.origin });
        List<Group> remaining = candidates.Where(candidate => !plan.groups.Any(chosen => Overlaps(candidate, chosen))).ToList();
        List<Group> unique = remaining.Where(candidate => !remaining.Any(other => other != candidate && Overlaps(candidate, other))).ToList();
        plan.groups.AddRange(unique);
        return new Resolution
        {
            plan = plan, candidates = candidates,
            conflicts = remaining.Except(unique).ToList(),
            alternatives = candidates.Where(candidate => candidates.Any(other => other != candidate && Overlaps(candidate, other))).ToList()
        };
    }

    private static bool IsConnected(int[] members, IReadOnlyList<Vector2Int> positions)
    {
        var visited = new HashSet<int> { members[0] };
        bool changed;
        do
        {
            changed = false;
            foreach (int member in members)
            {
                if (visited.Contains(member)) continue;
                if (visited.Any(other => Mathf.Abs(positions[member].x - positions[other].x)
                    + Mathf.Abs(positions[member].y - positions[other].y) == 1)) changed |= visited.Add(member);
            }
        } while (changed);
        return visited.Count == members.Length;
    }

    public static bool IsValid(IReadOnlyList<CharacterData> party, Plan plan)
    {
        if (party == null || party.Any(data => data == null) || plan?.partyIds == null || plan.groups == null
            || !party.Select(data => data.characterId).SequenceEqual(plan.partyIds)) return false;
        var used = new HashSet<int>();
        foreach (Group group in plan.groups)
        {
            Recipe recipe = group == null ? null : GetRecipe(group.recipeId);
            if (recipe == null || group.members == null || group.members.Length != recipe.Materials.Length
                || !group.members.Contains(group.origin)) return false;
            foreach (int member in group.members)
                if (member < 0 || member >= party.Count || !used.Add(member)) return false;
            if (!group.members.Select(index => party[index].characterName).OrderBy(value => value)
                .SequenceEqual(recipe.Materials.OrderBy(value => value))) return false;
        }
        return true;
    }

    public static Plan Empty(IReadOnlyList<CharacterData> party) =>
        new() { partyIds = party.Select(data => data.characterId).ToArray() };

    public static Plan Load(IReadOnlyList<CharacterData> party)
    {
        try
        {
            Plan plan = JsonUtility.FromJson<Plan>(PlayerProgressStore.GetString(PlanKey));
            return IsValid(party, plan) ? plan : Empty(party);
        }
        catch (ArgumentException) { return Empty(party); }
    }

    public static void Save(IReadOnlyList<CharacterData> party, Plan plan)
    {
        if (!IsValid(party, plan)) throw new ArgumentException("Invalid fusion plan.");
        PlayerProgressStore.SetString(PlanKey, JsonUtility.ToJson(plan));
        PlayerProgressStore.Save();
    }

    public static bool IsDiscovered(int recipeId) => PlayerProgressStore.GetInt(DiscoveryPrefix + recipeId) == 1;
    public static void ResetProgress()
    {
        PlayerProgressStore.Delete(PlanKey);
        PlayerProgressStore.Delete("KanjiBattle.Fusion.Plan.v1");
        foreach (Recipe recipe in Recipes) PlayerProgressStore.Delete(DiscoveryPrefix + recipe.Id);
        PlayerProgressStore.Save();
    }
    public static void Discover(int recipeId)
    {
        if (GetRecipe(recipeId) == null) throw new ArgumentOutOfRangeException(nameof(recipeId));
        PlayerProgressStore.SetInt(DiscoveryPrefix + recipeId, 1);
        PlayerProgressStore.Save();
    }
}
