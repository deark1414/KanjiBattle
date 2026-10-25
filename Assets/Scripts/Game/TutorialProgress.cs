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
        "一文字ひと文字に、力が宿る世界。\nあなたは軍師。漢字の軍勢を率いる。\n\n漢字を集め、編成と配置で難敵に挑もう。\n戦いは自動。勝ち筋は、あなたの作戦で。",19,20,21);
    public static readonly IReadOnlyList<Page> Pages = new[]
    {
        new Page("formation","漢字を組み合わせよう","漢字ごとに、得意分野がある。\n「一・剣・水」の3体で出陣しよう。\n仲間は全員、同じ軍師レベルで育つ。",1,10,20),
        new Page("deployment","配置を決めて、自動戦闘","前衛の剣は前に。支援役の水は後ろに。\n下2段でコマと移動先を選ぶと、配置を変えられる。\n「開戦」の後は、自動で戦う。",10,20),
        new Page("victory","戦果で、次の一文字を","勝利すると、経験値と戦果を獲得する。\n経験値は仲間全員の成長に。戦果は城下で使う。\n新しい仲間か、施設の強化か。次の作戦を考えよう。",1,10,20),
        new Page("castle","どの枝から、軍勢を育てる？","ノードを選び、戦果で仲間加入・施設強化・増枠を行う。\n章を突破すると、ツリーの奥が解禁される。\n別の枝にも後から進める。最終的にはすべて解放できる。",11,13,19),
        new Page("player-skill","勝負をかけるターンを選ぼう","プレイヤースキルは、種類と発動ターンを戦闘前に選ぶ。\n指定ターンの開始時に、1戦で1回だけ自動発動する。\n発動前に決着した場合は未使用で終了。次の戦闘では再び使える。",1,10,20),
        new Page("fusion","隣り合う漢字に、新しい力","対応する素材を上下左右に隣接させると、開戦時に融合する。\n離すか、斜めだけに接触させると個別に戦う。\n人数は減るが強力な1体に。未発見の完成形は「？」のお楽しみ。",10,13),
        new Page("records","作戦の働きを、成績で確かめよう","与ダメージ・被ダメージ・回復・軽減・肩代わりを確認できる。\n誰が働き、誰が守られたかを、次の編成に活かそう。\n融合したコマには、素材の漢字も表示される。",10,13,20),
        new Page("codex","出会った漢字を、図鑑へ","未遭遇の漢字は「？」、出会うと漢字がわかる。\n仲間に迎えると能力が公開される。\n融合も自軍で初めて成立させると、完成形と能力が記録される。",1,10,20),
        new Page("defeat","作戦を変えて、もう一度","前衛と後衛、射程、回復役の守り方を見直してみよう。\n城下で仲間や施設を増やすことも、突破への一手。\n敗北しても仲間は失われず、融合の素材も元に戻る。",13,17,20)
    };
    public static Page Get(string id) => id=="opening"?Opening:Pages.First(page=>page.id==id);
}
