using System;
using System.Linq;
using UnityEngine;

public static class TutorialRegression
{
    public static void Run()
    {
        void Check(bool value,string text){if(!value)throw new Exception("Tutorial: "+text);}
        string seen=PlayerProgressStore.GetString(TutorialProgress.SeenKey);
        int opening=PlayerProgressStore.GetInt(TutorialProgress.OpeningKey,-1);
        try
        {
            TutorialProgress.ResetProgress();
            Check(!TutorialProgress.HasSeen("formation"),"unread by default");
            TutorialProgress.MarkSeen("formation");TutorialProgress.MarkSeen("formation");
            Check(TutorialProgress.Seen.SequenceEqual(new[]{"formation"}),"acknowledgement is idempotent");
            TutorialProgress.OpeningEnabled=false;Check(!TutorialProgress.OpeningEnabled,"opening preference persists");
            TutorialProgress.ResetProgress();Check(!TutorialProgress.HasSeen("formation")&&!TutorialProgress.OpeningEnabled,"progress reset retains preferences");
            Check(TutorialContent.Pages.Select(page=>page.id).Distinct().Count()==TutorialContent.Pages.Count,"unique stable feature ids");
            Check(TutorialContent.Pages.All(page=>page.actors.Length>0&&!string.IsNullOrWhiteSpace(page.body)),"visual and copy on every page");
            Debug.Log("[TutorialRegression] Passed.");
        }
        finally
        {
            if(string.IsNullOrEmpty(seen))PlayerProgressStore.Delete(TutorialProgress.SeenKey);else PlayerProgressStore.SetString(TutorialProgress.SeenKey,seen);
            if(opening<0)PlayerProgressStore.Delete(TutorialProgress.OpeningKey);else PlayerProgressStore.SetInt(TutorialProgress.OpeningKey,opening);
            PlayerProgressStore.Save();
        }
    }
    public static void BuildReview()
    {
        BattleRuleRegression.RunFromCommandLine();GameDataValidator.ValidateFromCommandLine();
        KanjiBattle.Editor.WebGLBuild.BuildLocalE2E();
    }
}
