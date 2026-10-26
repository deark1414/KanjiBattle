using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Assumes victories; this measures their income, not whether a route can win.
public static class CampaignEconomyProbe
{
    public static List<object> Measure(StageDatabase stages)
    {
        var rows = new List<object>();
        foreach (int repetitions in new[] { 1, 3, 5 })
        {
            GameManager.Instance.ResetProgress(); FacilityManager.Instance.ResetProgress();
            CastleTree.ResetProgress(); PlayerInventory.Instance.ResetProgress();
            foreach (var stage in stages.stages.OrderBy(s => s.stageId))
            {
                rows.Add(new { winsPerStage = repetitions, stage = stage.stageId,
                    level = PlayerInventory.Instance.PlayerLevel, enemyLevel = stage.enemyLevel,
                    slots = CastleTree.Slots, points = GameManager.Instance.StagePoints,
                    owned = PlayerInventory.Instance.GetUnlockedCharacters().Select(c => c.characterId).OrderBy(id => id).ToArray(),
                    investments = CharacterInvestment.Purchased,
                    training = FacilityManager.Instance.GetBattleExperienceMultiplier() });
                for (int win = 0; win < repetitions; win++)
                {
                    GameManager.Instance.RegisterClearedStage(stage.stageId);
                    PlayerInventory.Instance.GrantBattleExperience(stage);
                    GameManager.Instance.AddStagePoints(GameManager.Instance.GetEffectiveStagePointReward(stage.rewardStagePoints));
                    Spend(stage.stageId);
                }
            }
        }
        GameManager.Instance.ResetProgress(); FacilityManager.Instance.ResetProgress();
        CastleTree.ResetProgress(); PlayerInventory.Instance.ResetProgress();
        return rows;
    }
    private static void Spend(int cleared)
    {
        var nodes = CastleTree.Nodes;
        CastleTree.Node Node(string id) => nodes.Single(n => n.id == id);
        bool Buy(CastleTree.Node node)
        {
            if (CastleTree.Owned(node)) return true;
            if (CastleTree.RequiredStage(node) > cleared) return false;
            if (node.parent != null && !Buy(Node(node.parent))) return false;
            return CastleTree.Purchase(node.id);
        }
        // Reserve the next capacity milestone before optional skill spending.
        Buy(Node("f12"));
        while (CastleTree.Purchase("f12")) { }
        foreach (var slot in nodes.Where(n => n.kind == CastleTree.Kind.Slot && n.stage <= cleared)) Buy(slot);
        int[] desired = cleared < 5 ? new[] { 13, 17 } : cleared < 10 ? new[] { 13, 17, 11 }
            : cleared < 20 ? new[] { 13, 17, 11, 27 } : new[] { 13, 17, 11, 27, 14, 31 };
        foreach (int id in desired) Buy(Node("c" + id));
        Buy(Node("f3")); while (CastleTree.Purchase("f3")) { }
        if (cleared >= 20) Buy(Node("f13"));
        foreach (int id in new[] { 10, 20 }.Concat(desired))
            if (CastleTree.Owned(Node("c" + id)))
                while (CastleTree.Purchase("c" + id)) { }
        foreach (var category in nodes.Where(n => n.kind == CastleTree.Kind.CategoryUpgrade)) Buy(category);
    }
}
