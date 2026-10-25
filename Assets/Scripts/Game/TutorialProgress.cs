using System;
using System.Collections.Generic;
using System.Linq;

public static class TutorialProgress
{
    public const string SeenKey = "KanjiBattle.Tutorial.Seen.v1";
    public const string OpeningKey = "KanjiBattle.Opening.Enabled";
    public static bool OpeningEnabled
    {
        get => PlayerProgressStore.GetInt(OpeningKey,1)!=0;
        set { PlayerProgressStore.SetInt(OpeningKey,value?1:0);PlayerProgressStore.Save(); }
    }
    public static bool HasSeen(string id) => Read().Contains(id);
    public static string[] Seen => Read().OrderBy(id=>id).ToArray();
    private static HashSet<string> Read() => PlayerProgressStore.GetString(SeenKey).Split(',')
        .Where(id=>!string.IsNullOrWhiteSpace(id)).ToHashSet(StringComparer.Ordinal);
    public static void MarkSeen(string id)
    {
        var seen=Read();if(!seen.Add(id))return;
        PlayerProgressStore.SetString(SeenKey,string.Join(",",seen.OrderBy(item=>item)));PlayerProgressStore.Save();
    }
    public static void ResetProgress(){PlayerProgressStore.Delete(SeenKey);PlayerProgressStore.Save();}
}

public static class TutorialContent
{
    public sealed class Page
    {
        public readonly string id,title,body;
        public readonly int[] actors;
        public Page(string id,string title,string body,params int[] actors)
        {this.id=id;this.title=title;this.body=body;this.actors=actors;}
    }
    public static readonly Page Opening = new("opening","漢字の力を、見つけよう",
        "漢字を集め、組み合わせて勝つ。\nあなただけの軍勢をつくろう。",19,20,21);
    public static readonly IReadOnlyList<Page> Pages = new[]
    {
        new Page("formation","漢字を組み合わせよう","3体の得意技を組み合わせる。\n仲間は全員、同じレベルで育つ。",1,10,20),
        new Page("deployment","前で守る、後ろから支える","剣を前に、水を後ろに。\n配置を決めたら自動戦闘。",10,20),
        new Page("victory","勝利！ 次の仲間を迎えよう","経験値で全員が育つ。\n戦果は城下で、新しい仲間に。",1,10,20),
        new Page("castle","枝を選んで、軍勢を育てよう","コマを選ぶ → 戦果で解放。\n別の枝にも、後から進める。",11,13,19),
        new Page("player-skill","何を、何ターン目に？","種類と発動ターンを選ぶ。\nそのターンに1回、自動発動。",1,10,20),
        new Page("fusion","隣り合わせで、新しい力","上下左右に並べると融合。\n離せば、それぞれで戦う。",10,13),
        new Page("records","誰が活躍した？","攻撃・回復・守りの成績を見る。\n次の作戦のヒントにしよう。",10,13,20),
        new Page("codex","出会った漢字を、図鑑へ","出会うと名前がわかる。\n仲間になると、能力もわかる。",1,10,20),
        new Page("defeat","配置を変えて、再挑戦","前衛で、後衛を守れていた？\n負けても仲間は失われない。",13,17,20)
    };
    public static Page Get(string id) => id=="opening"?Opening:Pages.First(page=>page.id==id);
}
