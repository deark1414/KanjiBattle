#if KANJI_BATTLE_E2E
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Compiled only into the isolated local E2E build, never a normal player.
public sealed class BattleE2EBridge : MonoBehaviour
{
    [Serializable] public sealed class Request { public string id; public string action; public string value; }
    [Serializable] public sealed class Box { public float x; public float y; public float w; public float h; }
    [Serializable] public sealed class Control
    {
        public string path; public string text; public string kind; public int id;
        public bool enabled; public bool selected; public bool visible; public bool reachable; public string hit; public Box rect;
    }
    [Serializable] public sealed class Unit { public int id; public int originalId; public bool transformed; public int slot; public string name; public bool ally; public int x; public int y; public int hp; public int maxHP; public int level; public int attackBuff; public bool icon; public bool motif; public bool motifVisible; public float motifAlpha; }
    [Serializable] public sealed class Cell { public int x; public int y; public Box rect; }
    [Serializable] public sealed class BattleState
    {
        public bool active; public bool deploying; public bool fusionOpen; public bool result; public bool won;
        public bool paused; public Box header;
        public int battleId; public int[] originalIds; public Unit[] units; public Cell[] cells; public BattleFusionRules.Plan plan;
        public int conflicts; public int previewLinks; public string fusionSummary;
        public int fusionFrameCount; public bool fusionFramesValid;
        public int fusionGlowCount;
        public string[] logs;
        public string resultText; public bool resultTextOverflow;
        public string[] supportTextures; public int supportVfxCount;
        public PlayerSkillRules.Plan playerSkillPlan;
        public bool playerSkillOpen, playerSkillFired, playerSkillActive;
        public int playerSkillRemaining;
    }
    [Serializable] private sealed class Snapshot
    {
        public int protocol = 1; public string requestId; public string error;
        public Control[] controls; public BattleState battle; public int[] formation; public int[] owned;
        public int[] discovered; public bool fusionUnlocked; public bool canUnlockFusion; public int stagePoints;
        public int level; public int experience; public int baseBattleExperience; public int[] roster;
        public int visibleMotifs;
        public int[] visibleMotifIds;
        public CharacterRecord[] records;
        public string[] codexTitles;
        public bool codexOverflow;
        public string[] codexOverflowNames;
        public int treeSlots;
        public string[] treeOwned;
        public bool treeOverflow;
        public bool treeLabelsForeground;
        public string[] treeVisible,treeFacilityIcons;
        public float treeZoom;
        public int investmentRefund;
        public string[] investmentOwned;
        public int retiredRefundExpected;
        public bool lastTreePurchase;
        public Box treeViewport;
        public string guideId;
        public string guideTarget;
        public int guideStep;
        public float guideInstructionSize;
        public string[] guideSeen;
        public bool openingEnabled,guideOverflow;
        public string[] guideOverflowNames,treeOverflowNames;
    }

    private StageData fixtureStage;
    private bool lastTreePurchase;
    private const string FixtureKey = "KanjiBattle.E2E.Fixture";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() => new GameObject("BattleE2E").AddComponent<BattleE2EBridge>();

    [Preserve] public void Command(string json) => StartCoroutine(Respond(json));

    private IEnumerator Respond(string json)
    {
        Request request = JsonUtility.FromJson<Request>(json);
        string error = null;
        try
        {
            switch (request.action)
            {
                case "seed": Seed(request.value); break;
                case "resume":
                    if(PlayerProgressStore.GetString(FixtureKey)=="tutorial")
                    {
                        if(request.value!="tutorial")UIManager.Instance.ShowFormation();
                    }
                    else if(PlayerProgressStore.GetString(FixtureKey)=="tree")
                    {
                        if(request.value=="tree")UIManager.Instance.ShowFacility();
                        else PrepareTreeCampaign(Mathf.Clamp(GameManager.Instance.GetHighestClearedStageId()+1,1,40));
                    }
                    else PrepareFormation(request.value == "alternate");break;
                case "nextDefeat": fixtureStage.enemyLevel = 2000; break;
                case "gate20": GameManager.Instance.RegisterClearedStage(20); GameManager.Instance.SpendStagePoints(GameManager.Instance.StagePoints); break;
                case "fundFusion": GameManager.Instance.AddStagePoints(10); break;
                case "corruptPlan": PlayerProgressStore.SetString("KanjiBattle.Fusion.AdjacentChoices.v2", "{broken"); PlayerProgressStore.Save(); break;
                case "snapshot": break;
                case "defeatSide": FindFirstObjectByType<BattleManager>().DefeatSideForE2E(request.value=="ally");break;
                case "waterBalanceProbe": FindFirstObjectByType<BattleManager>().ProbeWaterBalanceForE2E(request.value=="max");break;
                case "stalemate": FindFirstObjectByType<BattleManager>().SeedStalemateForE2E();break;
                case "tutorialRoute":
                    switch(request.value)
                    {
                        case "formation": UIManager.Instance.ShowFormation();break;
                        case "top": UIManager.Instance.ShowTop();break;
                        case "castle": UIManager.Instance.ShowFacility();break;
                        case "codex": UIManager.Instance.ShowCodex();break;
                        default: throw new Exception("Unknown tutorial route.");
                    }
                    break;
                case "tutorialEnable":
                    TutorialProgress.ResetProgress();PlayerProgressStore.SetString(FixtureKey,"tutorial");PlayerProgressStore.Save();
                    TutorialDirector.Instance.SuppressForDebug(false);break;
                case "treeFresh":
                    ResetTreeFixture();break;
                case "treeFunds": GameManager.Instance.AddStagePoints(int.Parse(request.value));break;
                case "investmentProbe": ProbeInvestment();break;
                case "skillInvestmentProbe":ProbeSkillInvestment();break;
                case "legacyStatSave":
                    CharacterInvestment.Record("u10_0",4);CharacterInvestment.Record("u10_1",6);CharacterInvestment.Record("u10_2",8);break;
                case "treeTryPurchase":lastTreePurchase=CastleTree.Purchase(request.value);break;
                case "legacyCombatSave":
                    var retired=FacilityManager.Instance.GetFacilities().Where(FacilityManager.IsRetiredCombatFacility).ToArray();
                    PlayerProgressStore.SetString("KanjiBattle.Facilities.Unlocked",PlayerProgressStore.GetString("KanjiBattle.Facilities.Unlocked")+","+string.Join(",",retired.Select(f=>f.facilityId)));
                    PlayerProgressStore.SetString("KanjiBattle.Facilities.Levels",PlayerProgressStore.GetString("KanjiBattle.Facilities.Levels")+","+string.Join(",",retired.Select(f=>$"{f.facilityId}:2")));
                    PlayerProgressStore.Save();break;
                case "treeAdvance": GameManager.Instance.RegisterClearedStage(int.Parse(request.value));break;
                case "treePurchase": if(!CastleTree.Purchase(request.value))throw new Exception("Tree purchase blocked: "+request.value);break;
                case "treeCampaign": PrepareTreeCampaign(int.Parse(request.value));break;
                case "treeCampaignDefault": PrepareTreeCampaign(int.Parse(request.value),false);break;
                case "treeSpecialProbe":
                    var limited=ScriptableObject.CreateInstance<StageData>();limited.slotCount=2;limited.restrictFormationSlots=true;
                    if(GameManager.Instance.GetFormationCapacity(limited)!=2)throw new Exception("Special stage limit ignored.");
                    limited.restrictFormationSlots=false;
                    if(GameManager.Instance.GetFormationCapacity(limited)!=GameManager.Instance.GetFacilityFormationSlots())throw new Exception("Normal stage was capped.");
                    Destroy(limited);break;
                case "recordsProbe": FindFirstObjectByType<BattleManager>().ProbeBattleRecords(); break;
                case "playerSkillProbe": FindFirstObjectByType<BattleManager>().ProbePlayerSkill(request.value); break;
                case "playerSkillTraps": FindFirstObjectByType<BattleManager>().SeedPlayerSkillLethalTraps(); break;
                case "playerSkillGate":
                    PlayerProgressStore.SetInt("KanjiBattle.HighestClearedStageId",0); GameManager.Instance.LoadProgress(); break;
                case "corruptPlayerSkill": PlayerProgressStore.SetString(PlayerSkillRules.SaveKey,"{broken"); PlayerProgressStore.Save(); break;
                case "supportVfxProbe": FindFirstObjectByType<BattleManager>().ProbeSupportVfx(request.value); break;
                case "supportProbe": FindFirstObjectByType<BattleManager>().ProbeSupportSkills(); break;
                case "shootingDeadZoneProbe": FindFirstObjectByType<BattleManager>().ProbeShootingDeadZoneForE2E(); break;
                case "scatterModeProbe": FindFirstObjectByType<BattleManager>().ProbeScatterModesForE2E(); break;
                case "defenseProbe": FindFirstObjectByType<BattleManager>().ProbeDefenseSkills(); break;
                case "projectileGuardProbe": FindFirstObjectByType<BattleManager>().ProbeProjectileGuard(); break;
                case "recruitSupport": ProbeTreeRecruitment(new[] { 27, 28, 29 }); break;
                case "recruitCow": ProbeTreeRecruitment(new[] { 30 }); break;
                case "defeatCow": FindFirstObjectByType<BattleManager>().DefeatOxForE2E(); break;
                case "elementProbe": FindFirstObjectByType<BattleManager>().ProbeElementSkills(); break;
                case "recruitElements": ProbeTreeRecruitment(new[] { 31, 32, 33 }); break;
                case "fusionSupportProbe": FindFirstObjectByType<BattleManager>().ProbeFusionSupport(); break;
                case "advancedFusionProbe": FindFirstObjectByType<BattleManager>().ProbeAdvancedFusion(); break;
                default: throw new ArgumentException("Unknown E2E action: " + request.action);
            }
        }
        catch (Exception exception) { error = exception.ToString(); }
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        Snapshot state;
        try { state = Capture(); state.error = error; }
        catch (Exception exception) { state = new Snapshot { error = exception.ToString() }; }
        state.requestId = request.id;
        // Unity truncates long WebGL console lines. ASCII chunks also preserve
        // multibyte Japanese text when a response crosses the log-size limit.
        string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(state)));
        const int chunkSize = 6000;
        int count = Mathf.CeilToInt(payload.Length / (float)chunkSize);
        for (int index = 0; index < count; index++)
            Debug.Log($"KANJI_E2E_PART:{request.id}:{index}:{count}:" + payload.Substring(index * chunkSize, Mathf.Min(chunkSize, payload.Length - index * chunkSize)));
    }

    private void Seed(string kind)
    {
        TutorialDirector.Instance?.SuppressForDebug(true);
        if(kind=="tutorial")
        {
            Seed("standard");ResetTreeFixture();TutorialProgress.ResetProgress();TutorialProgress.OpeningEnabled=true;
            FormationUI.Instance.ClearAllSlots();FormationUI.Instance.RememberCurrentFormation();
            PlayerProgressStore.SetString(FixtureKey,"tutorial");PlayerProgressStore.Save();
            TutorialDirector.Instance.SuppressForDebug(false);TutorialDirector.Instance.BeginStartup();return;
        }
        if(kind=="tree")
        {
            Seed("standard");ResetTreeFixture();GameManager.Instance.AddStagePoints(30);return;
        }
        UIManager.Instance.ShowTop();
        GameManager.Instance.ResetProgress();
        FacilityManager.Instance.ResetProgress();
        PlayerInventory.Instance.ResetProgress();
        ResearchBondService.Instance.ResetProgress();
        CastleTree.ResetProgress();
        CharacterRecords.ResetProgress();
        PlayerSkillRules.ResetProgress();
        PlayerProgressStore.Delete("KanjiBattle.Deployment.Positions.v1");
        PlayerProgressStore.SetString(FixtureKey, kind);
        GameManager.Instance.RegisterClearedStage(kind == "locked" ? 19 : 40);
        GameManager.Instance.AddStagePoints(10000);
        foreach (FacilityData facility in FacilityManager.Instance.GetFacilities())
        {
            if (facility.effectType is not (FacilityEffectType.FormationSlot or FacilityEffectType.PlayerLevelCap
                or FacilityEffectType.BattleSpeed or FacilityEffectType.StageRetry or FacilityEffectType.Fusion)) continue;
            if (facility.effectType == FacilityEffectType.Fusion && kind == "locked") continue;
            if (!FacilityManager.Instance.IsUnlocked(facility)) FacilityManager.Instance.Unlock(facility);
            for (int attempt = 0; attempt < 10 && FacilityManager.Instance.Upgrade(facility); attempt++) { }
        }
        PlayerInventory.Instance.SetPlayerLevelForDebug(20);
        GameManager.Instance.SetBattleSpeedIndex(GameManager.Instance.BattleSpeedTier);
        PrepareFormation(false);
        if (kind is "soldiers" or "forge")
            BattleDeploymentRules.Save(FormationUI.Instance.GetFormation().Select(data => data.characterId).ToArray(),
                new[] { new Vector2Int(0,5), new(0,6), new(3,5), new(3,6), new(5,6), new(5,5) });
        if (kind == "elements")
            BattleDeploymentRules.Save(FormationUI.Instance.GetFormation().Select(data => data.characterId).ToArray(),
                new[] { new Vector2Int(0,5), new(2,5), new(4,5), new(1,6), new(3,6), new(5,6) });
        if (kind.StartsWith("fusion-", StringComparison.Ordinal))
            BattleDeploymentRules.Save(FormationUI.Instance.GetFormation().Select(data => data.characterId).ToArray(),
                new[] { new Vector2Int(0,5), new(0,6), new(1,6), new(3,5), new(5,5), new(3,6) });
        UnityEngine.Random.InitState(1729);
        PlayerProgressStore.Save();
    }

    private void PrepareFormation(bool alternate)
    {
        CharacterDatabase database = Resources.Load<CharacterDatabase>("CharacterDatabase");
        var names = alternate ? new[] { "剣", "二", "二", "剣", "三", "銃" } : new[] { "二", "剣", "二", "剣", "三", "銃" };
        bool support = PlayerProgressStore.GetString(FixtureKey) == "support";
        bool defense = PlayerProgressStore.GetString(FixtureKey) == "defense";
        string kind = PlayerProgressStore.GetString(FixtureKey);
        bool animals = kind.StartsWith("animals", StringComparison.Ordinal);
        if (support) names = new[] { "鈴", "糸", "影", "剣", "槍", "水" };
        if (defense) names = new[] { "甲", "盾", "壁", "甲", "弓", "水" };
        if (animals) names = new[] { "丑", "午", "酉", "寅", "剣", "水" };
        if (kind == "animals-double") names = new[] { "丑", "丑", "午", "酉", "剣", "水" };
        if (kind == "animals-enemy") names = new[] { "午", "酉", "寅", "剣", "槍", "水" };
        if (kind == "elements") names = new[] { "電", "風", "鉄", "午", "剣", "水" };
        if (kind == "soldiers") names = new[] { "剣", "盾", "槍", "盾", "一", "水" };
        if (kind == "forge") names = new[] { "火", "槌", "二", "剣", "水", "甲" };
        var reviewRecipe = kind.StartsWith("fusion-", StringComparison.Ordinal)
            ? BattleFusionRules.Recipes.FirstOrDefault(recipe => string.Equals(recipe.Skill.ToString(), kind.Substring(7), StringComparison.OrdinalIgnoreCase)) : null;
        if (reviewRecipe != null) names = reviewRecipe.Materials.Concat(new[] { "午", "酉", "寅", "丑" }).Take(6).ToArray();
        if (PlayerProgressStore.GetString(FixtureKey) == "locked") names = names.Take(4).ToArray();
        CharacterData[] party = names.Select(name => database.characters.First(data => data.characterName == name)).ToArray();
        foreach (CharacterData data in party) PlayerInventory.Instance.AddCharacter(data);
        StageData source = Resources.FindObjectsOfTypeAll<StageData>().First(stage => stage.stageId == 21 && stage != fixtureStage);
        fixtureStage = Instantiate(source);
        fixtureStage.enemyPositions.Clear();
        fixtureStage.name = "E2E_FixtureStage";
        fixtureStage.hideFlags = HideFlags.DontSave;
        fixtureStage.enemyPool = new List<CharacterData> { database.characters.First(data => data.characterName == "一") };
        fixtureStage.enemyLevel = kind is "defeat" or "animals-defeat" ? 2000 : 1;
        if (support)
        {
            fixtureStage.stageName = "新能力の確認";
            fixtureStage.chapterId = 2;
            fixtureStage.enemyPool = new List<CharacterData>
            {
                database.characters.First(data => data.characterName == "剣"),
                database.characters.First(data => data.characterName == "糸"),
                database.characters.First(data => data.characterName == "鈴")
            };
            fixtureStage.enemyLevel = 20;
        }
        if (defense)
        {
            fixtureStage.stageName = "守りの確認";
            fixtureStage.chapterId = 2;
            fixtureStage.enemyPool = new List<CharacterData>
            {
                database.GetById(14), database.GetById(13), database.GetById(10)
            };
            fixtureStage.enemyLevel = 20;
        }
        if (animals)
        {
            fixtureStage.stageName = "干支と遠距離防御の確認";
            fixtureStage.chapterId = 2;
            if (kind == "animals-enemy")
            {
                fixtureStage.enemyPool = new List<CharacterData> { database.GetById(30) };
                PlayerInventory.Instance.AddCharacter(database.GetById(30));
            }
        }
        if (kind == "elements")
        {
            fixtureStage.stageName = "電・風・鉄の確認";
            fixtureStage.chapterId = 2;
            fixtureStage.enemyPool = new List<CharacterData>
            {
                database.GetById(10), database.GetById(10), database.GetById(10),
                database.GetById(10), database.GetById(10), database.GetById(32)
            };
            fixtureStage.enemyLevel = 20;
        }
        fixtureStage.slotCount = party.Length;
        if (kind is "soldiers" or "forge")
        {
            fixtureStage.stageName = kind == "soldiers" ? "戦士・衛兵の確認" : "鍛冶と双剣の確認";
            fixtureStage.chapterId = 2;
            fixtureStage.enemyPool = new List<CharacterData>
            { database.GetById(10), database.GetById(10), database.GetById(10), database.GetById(11) };
            fixtureStage.enemyLevel = 20;
        }
        fixtureStage.trapCount = 0;
        if (reviewRecipe != null)
        {
            fixtureStage.stageName = reviewRecipe.Name + "の確認"; fixtureStage.chapterId = 2;
            fixtureStage.enemyPool = new List<CharacterData>
                { database.GetById(10), database.GetById(10), database.GetById(11), database.GetById(20) };
            fixtureStage.enemyLevel = 12;
        }
        fixtureStage.reinforcementEnemy = new List<CharacterData>();
        fixtureStage.reinforcementInterval = 0;
        GameManager.Instance.SetSelectedStage(fixtureStage);
        UIManager.Instance.ShowFormation();
        FormationUI.Instance.SetupSlots(party.Length);
        FormationUI.Instance.ClearAllSlots();
        for (int index = 0; index < party.Length; index++) FormationUI.Instance.SetCharacterToSlot(index, party[index]);
        FormationUI.Instance.RememberCurrentFormation();
    }

    private void ResetTreeFixture()
    {
        UIManager.Instance.ShowTop();GameManager.Instance.ResetProgress();FacilityManager.Instance.ResetProgress();
        CastleTree.ResetProgress();PlayerInventory.Instance.ResetProgress();CharacterRecords.ResetProgress();
        PlayerSkillRules.ResetProgress();BattleFusionRules.ResetProgress();
        PlayerProgressStore.Delete("KanjiBattle.Deployment.Positions.v1");PlayerProgressStore.SetString(FixtureKey,"tree");
        PlayerProgressStore.Save();GameManager.Instance.SetBattleSpeedIndex(0);UIManager.Instance.ShowFacility();
    }

    private void PrepareTreeCampaign(int stageId,bool selectParty=true)
    {
        var stages=Resources.FindObjectsOfTypeAll<StageDatabase>().First();
        var stage=stages.GetStageById(stageId);
        if(stageId>1)GameManager.Instance.RegisterClearedStage(stageId-1);
        fixtureStage=stage;
        if(!selectParty){FormationUI.Instance.ClearAllSlots();FormationUI.Instance.RememberCurrentFormation();}
        GameManager.Instance.SetSelectedStage(stage);UIManager.Instance.ShowFormation();
        if(!selectParty)return;
        FormationUI.Instance.SetupSlots(GameManager.Instance.GetFormationCapacity(stage));FormationUI.Instance.ClearAllSlots();
        var party=PlayerInventory.Instance.GetUnlockedCharacters().OrderBy(data=>data.characterId).Take(GameManager.Instance.GetFormationCapacity(stage)).ToArray();
        for(int i=0;i<party.Length;i++)FormationUI.Instance.SetCharacterToSlot(i,party[i]);
        FormationUI.Instance.RememberCurrentFormation();
    }

    private Snapshot Capture()
    {
        Canvas.ForceUpdateCanvases();
        BattleManager battle = FindObjectsByType<BattleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        FacilityData fusion = FacilityManager.Instance.GetFacilities().First(data => data.effectType == FacilityEffectType.Fusion);
        return new Snapshot
        {
            guideId=TutorialDirector.Instance?.CurrentId??"",guideSeen=TutorialProgress.Seen,openingEnabled=TutorialProgress.OpeningEnabled,
            guideTarget=TutorialDirector.Instance?.GuideTarget??"",guideStep=TutorialDirector.Instance?.GuideStep??-1,
            guideInstructionSize=FindAnyObjectByType<GuidedTutorialView>()?.InstructionPixels??0,
            guideOverflowNames=FindObjectsByType<GuidedTutorialView>(FindObjectsSortMode.None).SelectMany(view=>view.GetComponentsInChildren<TMP_Text>()).Where(text=>text.isTextOverflowing).Select(text=>text.name).ToArray(),
            treeOverflowNames=FindObjectsByType<CastleTreeView>(FindObjectsSortMode.None).SelectMany(view=>view.GetComponentsInChildren<TMP_Text>()).Where(text=>text.isTextOverflowing).Select(text=>text.transform.parent.name+"/"+text.name).ToArray(),
            guideOverflow=FindObjectsByType<FullScreenGuideView>(FindObjectsSortMode.None).Any(view=>view.GetComponentsInChildren<TMP_Text>().Any(text=>text.isTextOverflowing))
                ||FindObjectsByType<GuidedTutorialView>(FindObjectsSortMode.None).Any(view=>view.GetComponentsInChildren<TMP_Text>().Any(text=>text.isTextOverflowing)),
            treeSlots=GameManager.Instance.GetFacilityFormationSlots(),
            treeVisible=CastleTree.Nodes.Where(CastleTree.Visible).Select(n=>n.id).ToArray(),
            treeZoom=FindAnyObjectByType<CastleTreeView>()?.Zoom??0,
            treeLabelsForeground=TreeLabelsForeground(),
            treeFacilityIcons=FindObjectsByType<CastleTreeView>(FindObjectsSortMode.None).SelectMany(view=>view.GetComponentsInChildren<Image>())
                .Where(image=>image.name=="NodeIcon"&&image.sprite!=null&&image.transform.parent.name.StartsWith("TreeNode_f")).Select(image=>image.transform.parent.name.Substring(9)).ToArray(),
            treeOwned=CastleTree.Nodes.Where(CastleTree.Owned).Select(n=>n.id).ToArray(),
            investmentRefund=CharacterInvestment.Refund,
            investmentOwned=CharacterInvestment.Purchased,
            retiredRefundExpected=FacilityManager.Instance.GetFacilities().Where(FacilityManager.IsRetiredCombatFacility)
                .Sum(f=>(f.unlockType==FacilityUnlockType.Free?0:f.unlockStagePointCost)+f.GetUpgradeCost(0)+f.GetUpgradeCost(1)),
            lastTreePurchase=lastTreePurchase,
            treeViewport=FindAnyObjectByType<CastleTreeView>() is { } tree ? Bounds(tree.transform.Find("TreeViewport") as RectTransform) : null,
            treeOverflow=FindObjectsByType<CastleTreeView>(FindObjectsSortMode.None).Any(view=>view.GetComponentsInChildren<TMP_Text>().Any(text=>text.isTextOverflowing)),
            controls = FindObjectsByType<Selectable>(FindObjectsSortMode.None).Where(control => control.gameObject.activeInHierarchy)
                .Select(CaptureControl).ToArray(),
            visibleMotifs = FindObjectsByType<PieceMotifPresentation>(FindObjectsSortMode.None).Count(motif => motif.IsVisible),
            visibleMotifIds = FindObjectsByType<PieceMotifPresentation>(FindObjectsSortMode.None).Where(motif => motif.IsVisible).Select(motif => motif.CharacterId).ToArray(),
            battle = battle.CaptureE2EState(),
            records = PlayerInventory.Instance.GetUnlockedCharacters().Select(data => CharacterRecords.Get(data.characterId)).ToArray(),
            codexTitles = CharacterCodexUI.Catalog().Select(item => item.title + ":" + item.status).ToArray(),
            codexOverflow = FindObjectsByType<CharacterCodexUI>(FindObjectsSortMode.None).Any(window =>
                window.GetComponentsInChildren<TMP_Text>().Any(text => text.name != "Description" && text.isTextOverflowing)),
            codexOverflowNames = FindObjectsByType<CharacterCodexUI>(FindObjectsSortMode.None)
                .SelectMany(window => window.GetComponentsInChildren<TMP_Text>())
                .Where(text => text.name != "Description" && text.isTextOverflowing)
                .Select(text => text.transform.parent.name + "/" + text.name + ":" + text.text).ToArray(),
            formation = FormationUI.Instance == null || FormationUI.Instance.GetFormation() == null ? new int[0]
                : FormationUI.Instance.GetFormation().Select(data => data == null ? 0 : data.characterId).ToArray(),
            owned = PlayerInventory.Instance.GetUnlockedCharacters().Select(data => data.characterId).OrderBy(id => id).ToArray(),
            discovered = BattleFusionRules.Recipes.Where(recipe => BattleFusionRules.IsDiscovered(recipe.Id)).Select(recipe => recipe.Id).ToArray(),
            fusionUnlocked = FacilityManager.Instance.IsFusionUnlocked(), canUnlockFusion = FacilityManager.Instance.CanUnlock(fusion),
            stagePoints = GameManager.Instance.StagePoints,
            level = PlayerInventory.Instance.PlayerLevel, experience = PlayerInventory.Instance.PlayerExperience,
            roster = Resources.Load<CharacterDatabase>("CharacterDatabase").characters.Where(data => !data.isBoss)
                .Select(data => data.characterId).OrderBy(id => id).ToArray(),
            baseBattleExperience = fixtureStage == null ? 0 : PlayerInventory.Instance.GetEffectiveBattleExperienceReward(fixtureStage)
        };
    }

    private static void ProbeInvestment()
    {
        var db=Resources.Load<CharacterDatabase>("CharacterDatabase");
        var sword=db.GetById(10);var two=db.GetById(2);
        var ally=new BattleCharacterState();var enemy=new BattleCharacterState();
        ally.Initialize(sword,10,true);enemy.Initialize(sword,10,false);
        if(ally.MaxHP!=enemy.MaxHP||ally.Attack!=enemy.Attack||ally.Defense!=enemy.Defense)throw new Exception("Skill tree must not modify base stats");
        var fusion=CharacterData.CreateBattleFusion(BattleFusionRules.GetRecipe(-1001),new[]{two,sword},10);
        try
        {
            float expected=(CharacterInvestment.Effect(two)+CharacterInvestment.Effect(sword))/2;
            if(Mathf.Abs(CharacterInvestment.Effect(fusion)-expected)>.0001f)throw new Exception("Fusion skill inheritance");
            ally.Initialize(fusion,10,true);enemy.Initialize(fusion,10,false);
            if(ally.MaxHP!=enemy.MaxHP||ally.Attack!=enemy.Attack)throw new Exception("Fusion base stats unchanged");
        }
        finally{Destroy(fusion);}
    }

    private static void ProbeSkillInvestment()
    {
        var db=Resources.Load<CharacterDatabase>("CharacterDatabase");var hosts=new List<GameObject>();
        BattleCharacter Unit(int id,bool ally,Vector2Int cell)
        {var host=new GameObject("SkillInvestmentProbe",typeof(RectTransform));host.SetActive(false);hosts.Add(host);
            var unit=host.AddComponent<BattleCharacter>();unit.Init(db.GetById(id),cell,ally,10);return unit;}
        void Check(bool condition,string message){if(!condition)throw new Exception("Skill investment: "+message);}
        try
        {
            var sword=Unit(10,true,new(0,0));var enemy=Unit(10,false,new(1,0));
            Check(sword.attack==enemy.attack&&sword.maxHP==enemy.maxHP,"base stats unchanged");
            Check(Mathf.Abs(sword.GetEffectiveSkillPower()-sword.data.skillPower*CharacterInvestment.Effect(sword.data))<.0001f,"ally skill power");
            Check(enemy.GetEffectiveSkillPower()==enemy.data.skillPower,"enemy not boosted");
            var shield=Unit(13,true,new(0,0));int hp=shield.currentHP;
            int defended=Mathf.Max(1,Mathf.FloorToInt(100*100f/(100+shield.defense/4f)));
            int expected=BattleDamageRules.ReduceByShield(defended,shield.SkillEffectMultiplier);
            shield.TakeDamage(100,null);Check(hp-shield.currentHP==expected,"shield skill reduction applies");
            var shadow=Unit(29,true,new(0,0));hp=shadow.currentHP;
            float strength=shadow.SkillEffectMultiplier;
            Check(shadow.CopyBattleForm(enemy),"shadow transforms");
            Check(shadow.attack==Mathf.RoundToInt(enemy.attack*strength)&&shadow.currentHP==hp,"shadow copies enhanced attack, keeps HP");
            Check(shadow.GetEffectiveSkillPower()==shadow.data.skillPower,"shadow copied skill not enhanced a second time");
            var wind=Unit(32,true,new(0,0));int turns=CharacterInvestment.WindDuration(wind.data);
            wind.ApplyWind(true,turns);for(int i=1;i<turns;i++){wind.TickWind();Check(wind.WindBoost,"wind persists");}
            wind.TickWind();Check(!wind.WindBoost,"wind expires");
            var ox=db.GetById(30);
            var alliedOx=Unit(30,true,new(0,0));var enemyOx=Unit(30,false,new(1,0));
            Check(ox.skillType==SkillType.ProjectileGuard && CharacterInvestment.CurrentSummary(ox).Contains("遠距離肩代わり"),"ox investment shows projectile protection");
            Check(Mathf.Abs(alliedOx.GetEffectiveSkillPower()-.5f*CharacterInvestment.Effect(ox))<.0001f
                && enemyOx.GetEffectiveSkillPower()==.5f,"only allied ox protection is upgraded");
            Check(BattleRewardRules.ArmyExperienceMultiplier(new[]{ox,ox})==1f,"ox no longer grants experience bonuses");
        }
        finally{foreach(var host in hosts)Destroy(host);}
    }

    private static void ProbeTreeRecruitment(int[] ids)
    {
        CharacterDatabase database = Resources.Load<CharacterDatabase>("CharacterDatabase");
        void Acquire(CastleTree.Node node)
        {
            if(CastleTree.Owned(node))return;
            if(node.parent!=null)Acquire(CastleTree.Nodes.Single(n=>n.id==node.parent));
            if(!CastleTree.Purchase(node.id))throw new Exception("Tree recruitment failed: "+node.id);
        }
        int experience=PlayerInventory.Instance.PlayerExperience;
        foreach (int id in ids)
        {
            CharacterData data = database.GetById(id);
            ResearchBondService.Instance.RegisterDefeated(data);
            if(ResearchBondService.Instance.ResolveVictory(new[]{id}).Count!=0||ResearchBondService.Instance.CanRecruit(data))
                throw new Exception("Legacy bond path is still active.");
            Acquire(CastleTree.Nodes.Single(n=>n.kind==CastleTree.Kind.Character&&n.target==id));
        }
        if(experience!=PlayerInventory.Instance.PlayerExperience)throw new Exception("Recruitment must not award experience.");
    }

    private static bool TreeLabelsForeground()
    {
        var tree=FindAnyObjectByType<CastleTreeView>();
        var content=tree?.transform.Find("TreeViewport/TreeContent");
        if(content==null)return false;
        var children=content.Cast<Transform>().Where(t=>t.gameObject.activeSelf).ToArray();
        int lastRoad=children.Where(t=>t.name.StartsWith("Link_")||t.name.StartsWith("BranchLink_"))
            .Select(t=>t.GetSiblingIndex()).DefaultIfEmpty(-1).Max();
        return children.Where(t=>t.name.StartsWith("TreeBranch_")||t.name.StartsWith("TreeNode_")||t.name=="TreeOrigin")
            .All(t=>t.GetSiblingIndex()>lastRoad);
    }

    private static Control CaptureControl(Selectable selectable)
    {
        Box box = Bounds(selectable.transform as RectTransform);
        float cx = box.x + box.w / 2, cy = box.y + box.h / 2;
        bool visible = cx >= 0 && cx <= 1 && cy >= 0 && cy <= 1 && box.w > 0 && box.h > 0;
        foreach (RectMask2D mask in selectable.GetComponentsInParent<RectMask2D>())
        {
            Box clip = Bounds(mask.transform as RectTransform);
            visible &= cx >= clip.x && cx <= clip.x + clip.w && cy >= clip.y && cy <= clip.y + clip.h;
        }
        var toggle = selectable as Toggle;
        var hits = new List<RaycastResult>();
        if (EventSystem.current != null)
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = new Vector2(cx * Screen.width, (1 - cy) * Screen.height) }, hits);
        return new Control
        {
            path = Path(selectable.transform), id = selectable.GetInstanceID(), kind = selectable.GetType().Name,
            text = string.Join(" ", selectable.GetComponentsInChildren<TMP_Text>().Select(text => text.text)),
            enabled = selectable.IsInteractable(), selected = toggle != null && toggle.isOn,
            rect = box, visible = visible,
            reachable = hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Selectable>() == selectable,
            hit=hits.Count>0?Path(hits[0].gameObject.transform):""
        };
    }

    private static string Path(Transform transform) => transform.parent == null ? transform.name : Path(transform.parent) + "/" + transform.name;
    internal static Box Bounds(RectTransform transform)
    {
        if (transform == null) return new Box();
        var corners = new Vector3[4]; transform.GetWorldCorners(corners);
        Canvas canvas = transform.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return new Box { x = min.x / Screen.width, y = 1 - max.y / Screen.height, w = (max.x - min.x) / Screen.width, h = (max.y - min.y) / Screen.height };
    }
}
#endif
