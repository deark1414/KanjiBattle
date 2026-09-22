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
