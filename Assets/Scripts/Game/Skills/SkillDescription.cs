using System.Text;

public static class SkillDescription
{
    public static string GetShort(SkillType skillType)
    {
        return skillType == SkillType.None ? "スキルなし" : GetName(skillType);
    }

    public static string GetDetail(CharacterData data)
    {
        if (data == null) return "";
        SkillType skillType = data.skillType;
        if (skillType == SkillType.None)
        {
            return $"{data.characterName}\nスキルなし";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"{data.characterName} / {GetName(skillType)}");
        builder.Append(GetBaseDescription(skillType));

        string characterSpecificDescription = GetCharacterSpecificDescription(data);
        if (!string.IsNullOrEmpty(characterSpecificDescription))
        {
            builder.Append("\n");
            builder.Append(characterSpecificDescription);
        }

        int chance = SkillExecutor.GetSkillChance(data, skillType);
        if (UsesChance(skillType))
        {
            builder.Append($"\n発動率: {chance}%");
        }

        if (skillType == SkillType.GuardAlly)
            builder.Append($"\n肩代わり率: {data.skillPower * 100:0.#}%");
        if (data.skillPower > 0f && skillType is not (SkillType.NumberPassive or SkillType.BellEcho or SkillType.ShadowCopy or SkillType.GuardAlly or SkillType.Shield))
        {
            builder.Append($"\n威力補正: x{data.skillPower:0.##}");
        }

        string rangeDiagram = GetRangeDiagram(skillType);
        if (!string.IsNullOrEmpty(rangeDiagram))
        {
            builder.Append("\n対象選択 / 効果範囲:\n");
            builder.Append(rangeDiagram);
        }

        return builder.ToString();
    }

    private static bool UsesChance(SkillType skillType)
    {
        return skillType != SkillType.None
            && !BattleActionRules.IsReliableRoleSkill(skillType)
            && skillType != SkillType.NumberPassive
            && skillType != SkillType.Counter
            && skillType != SkillType.AreaCounter
            && skillType != SkillType.GuardAlly
            && skillType != SkillType.Shield
            && skillType != SkillType.Armor;
    }

    private static string GetName(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.BellEcho => "再響",
            SkillType.ThreadPull => "引き糸",
            SkillType.ShadowCopy => "変身",
            SkillType.DoubleSlash => "双剣",
            SkillType.ScatterShot => "散弾",
            SkillType.Slash => "斬撃",
            SkillType.StunBlow => "スタン打撃",
            SkillType.Counter => "反撃",
            SkillType.AreaCounter => "範囲反撃",
            SkillType.Armor => "鎧",
            SkillType.Heal => "回復",
            SkillType.NumberPassive => "数字の結束",
            SkillType.Arrow => "矢",
            SkillType.Gun => "銃撃",
            SkillType.Spear => "槍",
            SkillType.Stone => "投石",
            SkillType.Shield => "堅守",
            SkillType.GuardAlly => "肩代わり",
            SkillType.Wall => "壁",
            SkillType.Soil => "土罠",
            SkillType.Fireball => "火球",
            SkillType.WoodPush => "木押し",
            SkillType.WaterHeal => "水癒し",
            SkillType.HorseCharge => "突進",
            SkillType.BirdRetreat => "後退",
            SkillType.TigerTwinClaw => "双爪",
            SkillType.Dragon => "竜の息吹",
            _ => skillType.ToString()
        };
    }

    private static string GetBaseDescription(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.BellEcho => "隣接8マスの味方がスキルを発動した時、一定確率でもう一度発動させる。1行動につき追加は1回まで。変身・反撃・常時効果は対象外。",
            SkillType.ThreadPull => "周囲2マスの敵1体にダメージを与え、自分の方へ1マス引き寄せる。移動先が水や他のコマで埋まっている場合はダメージのみ。",
            SkillType.ShadowCopy => "隣接する敵1体に変身し、攻撃・防御・行動・スキルを写す。現在HP・最大HPは変わらない。1戦に1回。ボス・進化・融合には変身できない。",
            SkillType.DoubleSlash => "正面と左右前の3マスに連続攻撃する。",
            SkillType.ScatterShot => "周囲3マスの敵を狙い、その周辺へ散弾を放つ。",
            SkillType.Slash => "隣接した敵を正面に取り、正面・左前・右前の3マスを同時に薙ぐ。正面は全威力、左右前は70%の威力。",
            SkillType.StunBlow => "隣接した敵を攻撃し、短時間スタンさせる。",
            SkillType.Counter => "通常攻撃を受けた時、一定確率で攻撃者へ反撃する。",
            SkillType.AreaCounter => "通常攻撃を受けた時、一定確率で周囲の敵へ反撃する。",
            SkillType.Armor => "受けるダメージをレベルに応じて軽減する。",
            SkillType.Heal => "味方を回復する。",
            SkillType.NumberPassive => "自分以外の生存数字との組み合わせにより、ターン開始時に効果が決まる。",
            SkillType.Arrow => "8方向の直線上の敵へ矢を放つ。味方と水を越え、射撃可能なら接近しない。",
            SkillType.Gun => "8方向の直線上の敵を貫通して撃つ。味方と水を越え、味方には当たらない。",
            SkillType.Spear => "8方向の敵を最大2マスまで貫通して攻撃する。水は越えるが、味方の先へは届かない。",
            SkillType.Stone => "周囲2マスの敵へ石を投げる。味方と水を越える。",
            SkillType.Shield => "自分が受けるダメージを、防御計算後さらに20%軽減する。反射はしない。",
            SkillType.GuardAlly => "隣接8マスの味方の軽減後ダメージの一部を肩代わりする。複数体いても1体だけが担当。移した分は再軽減・再転送・反射しない。残りHPを超える肩代わりはできない。",
            SkillType.Wall => "高い耐久で前線を支える。",
            SkillType.Soil => "周囲5x5の候補から最大2マスに土の罠を設置する。土以外が踏むと設置者の攻撃力に応じたダメージを受け、立ち続けるとターンごとに再びダメージを受ける。",
            SkillType.Fireball => "周囲2マスの敵に防御無視の火球を放つ。味方と水を越える。",
            SkillType.WoodPush => "敵を攻撃し、後方へ押し出す。",
            SkillType.WaterHeal => "自分と隣接味方のうち、HP割合が最も低い負傷者を回復する。回復不要なら攻撃する。",
            SkillType.HorseCharge => "敵へ突進し、距離を詰めながら攻撃する。",
            SkillType.BirdRetreat => "攻撃後に後退し、距離を取る。",
            SkillType.TigerTwinClaw => "近くの敵に連続攻撃を行う。",
            SkillType.Dragon => "広範囲攻撃や咆哮で戦場を制圧する。",
            _ => "特殊な効果を持つ。"
        };
    }

    private static string GetCharacterSpecificDescription(CharacterData data)
    {
        if (data == null) return "";

        if (data.skillType == SkillType.NumberPassive)
        {
            return data.category switch
            {
                CharacterCategory.Number1 => "低位数字: 自分以外の生存数字の種類数により攻撃力+15% / +30% / +50%。一だけの編成は2体以上で発動し、4体・5体では大幅に上がる。",
                CharacterCategory.Number2 => "中位数字: 自分以外の生存数字の種類数により、味方ターン開始時に最大HPの5% / 10% / 15%を自動回復。",
                CharacterCategory.Number3 => "上位数字: 自分以外の生存数字の種類数により、経過ラウンドごとに攻撃力が1.03倍 / 1.05倍 / 1.10倍で増加。生存構成が変わると次の味方ターンに再計算。",
                _ => ""
            };
        }

        if (data.category == CharacterCategory.Animal)
        {
            return "動物: 通常移動時、敵へ近づく移動は最大2マス。";
        }

        return "";
    }

    private static string GetRangeDiagram(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Slash => "選択: 隣接した敵1体を正面に取る\n効果: ・ 70 ・\n　　 70 自 100\n注 正面は全威力、左右前は70%",
            SkillType.AreaCounter => "選択: 被弾時に自動\n効果: 攻 攻 攻\n　　 攻 自 攻\n　　 攻 攻 攻",
            SkillType.Gun => "選択: 8方向の直線\n効果: 選択方向の直線上を貫通",
            SkillType.Spear => "選択: 8方向 / 2マス\n効果: 選択方向の最大2マスを貫通",
            SkillType.Stone => "選択: 自分中心5×5\n効果: 選択した対象1体",
            SkillType.Fireball => "選択: 自分中心5×5\n効果: 選択した対象1体",
            SkillType.Dragon => "選択: 4方向から1方向\n効果: 前方3×3ブレス",
            _ => ""
        };
    }
}
