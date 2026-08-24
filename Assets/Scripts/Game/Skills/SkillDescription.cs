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

        SkillData skillData = SkillCatalog.Get(skillType);
        int chance = skillData != null && skillData.chanceOverride >= 0 ? skillData.chanceOverride : data.skillChance;
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
            builder.Append("\n範囲:\n");
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
            SkillType.Slash => "隣接した敵に強い近接攻撃を行う。",
            SkillType.StunBlow => "隣接した敵を攻撃し、短時間スタンさせる。",
            SkillType.Counter => "通常攻撃を受けた時、一定確率で攻撃者へ反撃する。",
            SkillType.AreaCounter => "通常攻撃を受けた時、一定確率で周囲の敵へ反撃する。",
            SkillType.Armor => "受けるダメージをレベルに応じて軽減する。",
            SkillType.Heal => "味方を回復する。",
            SkillType.NumberPassive => "編成内の数字の種類に応じて攻撃力が上がる。",
            SkillType.Arrow => "離れた敵に矢を放つ。",
            SkillType.Gun => "直線上の敵を撃ち抜く。",
            SkillType.Spear => "8方向の敵を最大2マスまで貫通して攻撃する。",
            SkillType.Stone => "離れた敵へ石を投げる。",
            SkillType.Shield => "防御寄りの能力を持つ。",
            SkillType.Wall => "高い耐久で前線を支える。",
            SkillType.Soil => "自分の周囲に土の罠を設置する。",
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
                CharacterCategory.Number1 => "初位数字: 数字の種類1つごとに攻撃力+3%、最大+9%。",
                CharacterCategory.Number2 => "中位数字: 数字の種類1つごとに攻撃力+2%、最大+6%。通常攻撃時、自分周囲8マスの敵に攻撃力25%の追加ダメージ。",
                CharacterCategory.Number3 => "上位数字: 数字の種類1つごとに攻撃力+6%、最大+18%。通常攻撃時35%で、主対象以外の低HP敵へ攻撃力60%の追撃。",
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
            SkillType.AreaCounter => "攻 攻 攻\n攻 自 攻\n攻 攻 攻\n注被弾時に周囲へ反撃",
            SkillType.Gun => "攻 ・ 攻 ・ 攻\n・ 攻 攻 攻 ・\n攻 攻 自 攻 攻\n・ 攻 攻 攻 ・\n攻 ・ 攻 ・ 攻\n注8方向の直線を端まで貫通",
            SkillType.Spear => "攻 ・ 攻 ・ 攻\n・ 攻 攻 攻 ・\n攻 攻 自 攻 攻\n・ 攻 攻 攻 ・\n攻 ・ 攻 ・ 攻\n注8方向2マスまで貫通",
            SkillType.Soil => "攻 攻 攻 攻 攻\n攻 攻 攻 攻 攻\n攻 攻 自 攻 攻\n攻 攻 攻 攻 攻\n攻 攻 攻 攻 攻\n注候補から最大2マスに罠",
            SkillType.Dragon => "・ ・ ・ ・ ・\n・ ・ 攻 攻 攻\n・ 自 攻 攻 攻\n・ ・ 攻 攻 攻\n・ ・ ・ ・ ・\n注方向を選び前方3x3ブレス",
            _ => ""
        };
    }
}
