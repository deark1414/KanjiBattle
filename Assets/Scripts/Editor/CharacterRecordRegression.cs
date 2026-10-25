using System;
using System.Linq;
using UnityEngine;

public static class CharacterRecordRegression
{
    public static void RunFromCommandLine()
    {
        const string key = "KanjiBattle.CharacterRecords.v1";
        string previous = PlayerProgressStore.GetString(key);
        void Check(bool condition, string message)
        { if (!condition) throw new Exception("Character records: " + message); }
        try
        {
            CharacterRecords.ResetProgress();
            var book = new BattleRecordBook();
            book.Register(1, "賽子", null, 1, 2, 3);
            book.Register(2, "二", null, 2);
            book.Credit(1, damage: 1, received: 7, healed: 5, prevented: 4, guarded: 2);
            book.Credit(2, damage: 10);
            var totals = book.MaterialTotals();
            Check(totals.Single(row => row.id == 1).damage == 2, "thirds are preserved");
            Check(totals.Single(row => row.id == 2).damage == 62, "same type is combined");
            Check(book.Complete(true) && !book.Complete(true), "result can settle only once");
            Check(CharacterRecords.Get(2).wins == 1 && CharacterRecords.Get(2).damage == 62, "one win per type");
            var loss = new BattleRecordBook(); loss.Register(1, "二", null, 2); loss.Credit(1, damage: 200);
            loss.Complete(false);
            Check(CharacterRecords.Get(2).damage == 62 && loss.Entries.Single().result.damage == 1200, "defeat visible but not saved");
            loss.Clear(); loss.Register(1, "二", null, 2); loss.Credit(1, damage: 200); loss.Complete(true, trial: true);
            Check(CharacterRecords.Get(2).damage == 62, "trial does not add progress");
            var duplicate = new BattleRecordBook(); duplicate.Register(1, "山", null, 19, 19);
            duplicate.Credit(1, damage: 3); duplicate.Complete(true);
            Check(CharacterRecords.Get(19).damage == 18 && CharacterRecords.Get(19).wins == 1, "duplicate materials conserve totals");
            bool discovered = BattleFusionRules.IsDiscovered(-1001);
            CharacterRecords.Encounter(-1001);
            Check(CharacterRecords.HasEncountered(-1001) && BattleFusionRules.IsDiscovered(-1001) == discovered, "encounter is not recipe discovery");
            Check(CharacterRecords.Get(3).healed == 10 && CharacterRecords.Get(3).prevented == 8, "all metrics share equally");
            CharacterRecords.ResetProgress(); Check(CharacterRecords.Get(2).wins == 0, "reset clears records");
            PlayerProgressStore.SetString(key, "{broken"); Check(CharacterRecords.Get(2).wins == 0, "bad save is safe");
            Debug.Log("[CharacterRecordRegression] Passed.");
        }
        finally
        {
            if (string.IsNullOrEmpty(previous)) PlayerProgressStore.Delete(key); else PlayerProgressStore.SetString(key, previous);
            PlayerProgressStore.Save();
        }
    }

    public static void BuildLocalReview()
    {
        BattleRuleRegression.RunFromCommandLine();
        GameDataValidator.ValidateFromCommandLine();
        KanjiBattle.Editor.WebGLBuild.BuildLocalE2E();
    }
}
