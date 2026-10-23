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
        public Recipe(int id, string name, SkillType skill, params string[] materials)
        { Id = id; Name = name; Skill = skill; Materials = materials; }
    }

    // Only recipes with implemented battle effects are selectable.
    public static readonly IReadOnlyList<Recipe> Recipes = new[]
    {
        new Recipe(-1001, "双剣", SkillType.DoubleSlash, "二", "剣"),
        new Recipe(-1002, "散弾", SkillType.ScatterShot, "三", "銃")
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

    private const string PlanKey = "KanjiBattle.Fusion.Plan.v1";
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
