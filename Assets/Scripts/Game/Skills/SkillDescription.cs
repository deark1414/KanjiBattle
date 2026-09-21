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

        if (data.skillPower > 0f && skillType != SkillType.NumberPassive)
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
            && skillType != SkillType.NumberPassive
            && skillType != SkillType.Counter
            && skillType != SkillType.AreaCounter
            && skillType != SkillType.Armor;
    }

    private static string GetName(SkillType skillType)
    {
        return skillType switch
        {
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
            SkillType.Shield => "盾",
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
            SkillType.Slash => "隣接した敵を正面に取り、正面・左前・右前の3マスを同時に薙ぐ。正面は全威力、左右前は70%の威力。",
            SkillType.StunBlow => "隣接した敵を攻撃し、短時間スタンさせる。",
            SkillType.Counter => "通常攻撃を受けた時、一定確率で攻撃者へ反撃する。",
            SkillType.AreaCounter => "通常攻撃を受けた時、一定確率で周囲の敵へ反撃する。",
            SkillType.Armor => "受けるダメージをレベルに応じて軽減する。",
            SkillType.Heal => "味方を回復する。",
            SkillType.NumberPassive => "自分以外の生存数字との組み合わせにより、ターン開始時に効果が決まる。",
            SkillType.Arrow => "離れた敵に矢を放つ。",
            SkillType.Gun => "直線上の敵を撃ち抜く。",
            SkillType.Spear => "8方向の敵を最大2マスまで貫通して攻撃する。",
            SkillType.Stone => "離れた敵へ石を投げる。",
            SkillType.Shield => "防御寄りの能力を持つ。",
            SkillType.Wall => "高い耐久で前線を支える。",
            SkillType.Soil => "周囲5x5の候補から最大2マスに土の罠を設置する。土以外が土罠の上にいると、ターンごとにダメージを受ける。",
            SkillType.Fireball => "敵に防御無視の火球を放つ。",
            SkillType.WoodPush => "敵を攻撃し、後方へ押し出す。",
            SkillType.WaterHeal => "隣接した味方を回復する。",
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
