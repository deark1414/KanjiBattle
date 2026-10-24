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
        if (skillType == SkillType.ExperienceBonus)
            builder.Append($"\n経験値倍率: ×{data.skillPower:0.#}");
        if (data.skillPower > 0f && skillType is not (SkillType.NumberPassive or SkillType.BellEcho or SkillType.ShadowCopy or SkillType.GuardAlly or SkillType.Shield or SkillType.ExperienceBonus or SkillType.WindShift or SkillType.Forge))
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
            && skillType != SkillType.ExperienceBonus
            && skillType != SkillType.WindShift
            && skillType != SkillType.Forge
            && skillType is not (SkillType.Magnet or SkillType.Mountain or SkillType.Coal or SkillType.Fortress
                or SkillType.Dice or SkillType.Sand or SkillType.Grass or SkillType.Lantern)
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
            SkillType.ExperienceBonus => "育成支援",
            SkillType.ChainLightning => "連鎖電撃",
            SkillType.WindShift => "風向操作",
            SkillType.IronAdapt => "武装変形",
            SkillType.Warrior => "斬撃・堅守・鼓舞",
            SkillType.Sentinel => "貫通・堅守・守護回復",
            SkillType.Forge => "鍛造",
            SkillType.Wave => "押し流し",
            SkillType.Magnet => "磁力",
            SkillType.Mountain => "山びこ",
            SkillType.Coal => "燃え上がり",
            SkillType.Fortress => "堅守・反撃・迫撃砲",
            SkillType.Dice => "賽振り",
            SkillType.Rain => "回復の雨",
            SkillType.Sand => "砂の分身",
            SkillType.Grass => "分裂",
            SkillType.Cocoon => "繭の保護",
            SkillType.WindChime => "全体再響",
            SkillType.Lantern => "灯守り",
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
            SkillType.ExperienceBonus => "出撃した戦闘の勝利経験値を増やす。撃破されても有効。複数体いても最大倍率だけを適用し、施設倍率と掛け合わせる。所持だけ・敵側・敗北・訓練・縁・戦果には効果なし。",
            SkillType.ChainLightning => "3マス以内の敵へ電撃。隣接8マスの未命中の敵へ最大4体まで連鎖し、威力は毎回75%になる。味方には命中しない。",
            SkillType.WindShift => "ターン開始時、移動を増やせば攻撃圏へ届く味方がいれば自分と味方1体の移動+1。それ以外は敵1体の移動-1。最低移動1、そのターンのみ、同効果は重複しない。",
            SkillType.IronAdapt => "行動開始時、HP半分以下なら被害25%軽減の盾。それ以外は扇状に敵2体以上で剣、槍が届けば槍、届かなければ剣で接近。形態は次の行動まで維持する。",
            SkillType.Warrior => "剣と同じ扇状攻撃と被害20%軽減。第1・4・7…ターンに自分と最寄り味方1体の攻撃+20%、2ターン。同じ鼓舞は重複しない。",
            SkillType.Sentinel => "槍と同じ2マス貫通攻撃と被害20%軽減。毎ターン開始時、自分と隣接8マスの味方をそれぞれ最大HPの4%回復。",
            SkillType.Forge => "第1・4・7…ターンに味方の剣・槍・槌・盾と対応する武器系融合の攻撃+25%、3ターン。同じ鍛造は重複せず更新。自分は対象外、武器は生成しない。",
            SkillType.Wave => "前方扇状3マスへ威力80%の攻撃と1マス押し出し。移動先が塞がれば移動だけ不発。衝突ダメージ・スタンなし。",
            SkillType.Magnet => "ターン開始時にランダムな敵1体を自身へ1マス引き寄せる。移動先が空かなければ不発、選び直さない。",
            SkillType.Mountain => "直接攻撃で失った実HPを30%の確率で攻撃者へ返す。スキルと致死被弾も対象。罠・肩代わり・反射では発動せず、返還分は再軽減・再反射しない。",
            SkillType.Coal => "被弾前は被害半減。敵の直接攻撃で生き残ると燃え上がり、以後は軽減を失う代わりに攻撃+40%。罠・肩代わり・反射では変化しない。",
            SkillType.Fortress => "被害20%軽減。直接被弾時25%で隣接敵へ実HP損失25%の反撃。第1・3・5…ターンに敵最大2体へ威力50%の迫撃砲。",
            SkillType.Dice => "毎ターン1～6を等確率で振る。1・2は全味方HP6%回復、3・4はそのターン防御+20%、5・6は攻撃+20%。同種の強化は重複しない。",
            SkillType.Rain => "行動時に味方の足元と隣接8マスへ回復域。直後と次の2ターンに味方を各層最大HP3%回復。場所固定・重複可。",
            SkillType.Sand => "ターン開始時、最大HP・素攻撃・防御半分の砂の分身を1体生成。同時最大3体、次ターンから行動。本体退場で消滅。分身から分身は生成できない。",
            SkillType.Grass => "直接被弾後、生存してHP75%以下なら現在HP・最大HPを維持した子へ分裂。系列全体で3回まで。子の攻撃は親の70%、次ターンから行動。致死被弾では分裂しない。",
            SkillType.Cocoon => "保護されていない味方1体へ繭を付与。次の敵の直接攻撃1段を無効化し消える。重ね置き不可。",
            SkillType.WindChime => "盤面全体の味方のスキルを確率で1回再発動。自分は対象外。鈴と重なっても追加は最大1回、再発動から再誘発しない。",
            SkillType.Lantern => "隣接8マスの味方は投射攻撃を70%で回避。自分は守らない。射撃可能な敵から優先して狙われる。槍・電の連鎖・迫撃砲・竜の息は対象外。",
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
