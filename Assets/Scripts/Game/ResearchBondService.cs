using System;
using System.Collections.Generic;

// Compatibility surface for existing UI and old save reset paths. Recruitment
// now belongs exclusively to CastleTree; battles no longer advance bonds.
public sealed class ResearchBondService
{
    public static ResearchBondService Instance { get; } = new();
    public event Action OnChanged;
    public IReadOnlyList<CharacterData> GetDefeatedCandidates() => Array.Empty<CharacterData>();
    public void RegisterDefeated(CharacterData data) { }
    public int GetBond(CharacterData data) => 0;
    public int GetBondThreshold(CharacterData data) => 0;
    public bool CanRecruit(CharacterData data) => false;
    public bool Recruit(CharacterData data) => false;
    public IReadOnlyList<RecruitmentResult> ResolveVictory(IReadOnlyCollection<int> ids) => Array.Empty<RecruitmentResult>();
    public void ResetProgress()
    {
        PlayerProgressStore.Delete("KanjiBattle.ResearchBond.Bond");
        PlayerProgressStore.Delete("KanjiBattle.ResearchBond.Defeated");
        PlayerProgressStore.Save();OnChanged?.Invoke();
    }
}
public readonly struct RecruitmentResult
{
    public readonly CharacterData character;
    public readonly int bondGained, bond, threshold;
    public readonly bool readyToRecruit, luckyCompletion;
    public RecruitmentResult(CharacterData character,int bondGained,int bond,int threshold,bool readyToRecruit,bool luckyCompletion)
    {
        this.character=character;this.bondGained=bondGained;this.bond=bond;this.threshold=threshold;
        this.readyToRecruit=readyToRecruit;this.luckyCompletion=luckyCompletion;
    }
}
