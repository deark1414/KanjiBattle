using System.Collections.Generic;
using UnityEngine;

public static class CastleTreeArt
{
    private static readonly Dictionary<string,Sprite> sprites=new();
    public static Sprite Building(string name)
    {
        if(sprites.TryGetValue(name,out var sprite))return sprite;
        var texture=Resources.Load<Texture2D>("UI/ModernWafuu/Buildings/"+name);
        if(texture==null)return null;
        sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new(.5f,.5f),100);
        sprites[name]=sprite;return sprite;
    }
    public static Sprite Facility(FacilityData facility)=>Building(facility==null?"FacilityBarracks":facility.effectType switch
    {
        FacilityEffectType.StagePointBoost=>"FacilityStorehouse",
        FacilityEffectType.Training=>"FacilityDojo",
        FacilityEffectType.TrainingFrequency=>"FacilityDrillTower",
        FacilityEffectType.BattleSpeed=>"FacilityBattleArchive",
        FacilityEffectType.StageRetry=>"FacilityCouncilPavilion",
        FacilityEffectType.PlayerLevelCap=>"FacilityRankTower",
        FacilityEffectType.AttackBoost=>"FacilityMartialHall",
        FacilityEffectType.SkillPowerBoost=>"FacilityStrategyHall",
        FacilityEffectType.SkillChanceBoost=>"FacilityShrine",
        FacilityEffectType.HealthBoost=>"FacilityInfirmary",
        FacilityEffectType.Fusion=>"FacilityBondShrine",
        _=>"FacilityBarracks"
    });
    public static Sprite Investment(CharacterInvestment.Stat stat)=>Building(stat switch
    {
        CharacterInvestment.Stat.Chance or CharacterInvestment.Stat.ChanceII=>"FacilityShrine",
        CharacterInvestment.Stat.Duration or CharacterInvestment.Stat.DurationII=>"FacilityDrillTower",
        _=>"FacilityStrategyHall"
    });
}
