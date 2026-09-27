using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BattleRuleRegression
{
    [MenuItem("Tools/Validation/Run Battle Rule Regression")]
    public static void RunFromEditor()
    {
        Run();
        Debug.Log("[BattleRuleRegression] Passed.");
    }

    public static void RunFromCommandLine()
    {
        Run();
        Debug.Log("[BattleRuleRegression] Passed.");
    }

    private static void Run()
    {
        VerifySwordWedge();
        VerifyLineRanges();
        VerifyMovement();
        VerifyStageTerrain();
        VerifyTrapDamage();
        VerifyNumberPassives();
    }

    private static void VerifySwordWedge()
    {
        AssertCells(
            BattleRangeService.GetSwordWedgeCells(new Vector2Int(3, 3), Vector2Int.right),
            new Vector2Int(4, 3), new Vector2Int(4, 4), new Vector2Int(4, 2));
        AssertCells(
            BattleRangeService.GetSwordWedgeCells(new Vector2Int(3, 3), new Vector2Int(1, 1)),
            new Vector2Int(4, 4), new Vector2Int(4, 3), new Vector2Int(3, 4));
    }

    private static void VerifyLineRanges()
    {
        AssertCells(
            BattleRangeService.GetLineToTarget(new Vector2Int(1, 1), new Vector2Int(4, 4), 2),
            new Vector2Int(2, 2), new Vector2Int(3, 3));
        AssertCells(
            BattleRangeService.GetEffectCells(SkillType.Spear, new Vector2Int(2, 2), new Vector2Int(4, 2), 8, 5),
            new Vector2Int(3, 2), new Vector2Int(4, 2));
        AssertCells(
            BattleRangeService.GetLineWithinBoard(new Vector2Int(6, 3), Vector2Int.right, 12, 8, 5),
            new Vector2Int(7, 3));
        AssertCells(
            BattleRangeService.GetSelectionCells(SkillType.WoodPush, new Vector2Int(2, 2), null, 8, 5),
            new Vector2Int(2, 3), new Vector2Int(2, 1), new Vector2Int(1, 2), new Vector2Int(3, 2),
            new Vector2Int(3, 3), new Vector2Int(1, 3), new Vector2Int(3, 1), new Vector2Int(1, 1));
    }

    private static void VerifyMovement()
    {
        var path = new List<Vector2Int>
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0)
        };
        AssertEqual(1, BattleMovementRules.GetDestinationIndex(CharacterCategory.Weapon, path, targetCellOccupied: true), "weapon movement");
        AssertEqual(2, BattleMovementRules.GetDestinationIndex(CharacterCategory.Animal, path, targetCellOccupied: true), "animal movement");
        AssertEqual(1, BattleMovementRules.GetDestinationIndex(CharacterCategory.Animal, path.GetRange(0, 3), targetCellOccupied: true), "animal cannot enter occupied target");
    }

    private static void VerifyStageTerrain()
    {
        var reserved = new HashSet<Vector2Int>
        {
            new Vector2Int(0, 0), new Vector2Int(7, 4), new Vector2Int(3, 2)
        };
        HashSet<Vector2Int> generated = BattleMovementRules.GenerateImpassableCells(8, 5, 3, reserved, 2917);
        AssertEqual(3, generated.Count, "generated obstacle count");
        foreach (Vector2Int cell in reserved)
        {
            if (generated.Contains(cell)) throw new InvalidOperationException("Obstacle overlaps an initial position.");
        }
        if (!BattleMovementRules.AreAllWalkableCellsConnected(8, 5, generated))
        {
            throw new InvalidOperationException("Obstacle generation created unreachable cells.");
        }
        AssertEdgeConnected(generated, "generated water terrain");
    }

    private static void VerifyTrapDamage()
    {
        AssertEqual(23, BattleTrapRules.GetSoilTrapDamage(18), "soil trap level 1 damage");
        AssertEqual(60, BattleTrapRules.GetSoilTrapDamage(48), "soil trap scaled damage");
    }

    private static void AssertEdgeConnected(HashSet<Vector2Int> cells, string label)
    {
        if (cells == null || cells.Count <= 1) return;

        var visited = new HashSet<Vector2Int>();
        var open = new Queue<Vector2Int>();
        foreach (Vector2Int first in cells)
        {
            visited.Add(first);
            open.Enqueue(first);
            break;
        }

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (open.Count > 0)
        {
            Vector2Int current = open.Dequeue();
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;
                if (cells.Contains(next) && visited.Add(next)) open.Enqueue(next);
            }
        }

        if (visited.Count != cells.Count)
        {
            throw new InvalidOperationException($"{label}: expected one edge-connected group.");
        }
    }

    private static void VerifyNumberPassives()
    {
        AssertEqual(2, NumberPassiveRules.GetStandardStrength(new[] { "一", "四", "七" }), "number type strength");
        AssertEqual(0, NumberPassiveRules.GetStandardStrength(new[] { "一", "一" }), "duplicate number type strength");
        AssertEqual(150, NumberPassiveRules.GetOneOnlyAttackBonus(4, 15, 30, 150, 300), "four one party bonus");
        AssertEqual(300, NumberPassiveRules.GetOneOnlyAttackBonus(5, 15, 30, 150, 300), "five one party bonus");
        AssertEqual(21, NumberPassiveRules.GetCompoundedAttackBonus(10, 2, 300), "compound attack bonus");
    }

    private static void AssertCells(List<Vector2Int> actual, params Vector2Int[] expected)
    {
        if (actual.Count != expected.Length)
        {
            throw new InvalidOperationException($"Expected {expected.Length} cells but got {actual.Count}.");
        }

        for (int index = 0; index < expected.Length; index++)
        {
            if (actual[index] != expected[index])
            {
                throw new InvalidOperationException($"Cell {index}: expected {expected[index]}, got {actual[index]}.");
            }
        }
    }

    private static void AssertEqual(int expected, int actual, string label)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
        }
    }
}
