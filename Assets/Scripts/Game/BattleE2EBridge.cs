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
        public bool enabled; public bool selected; public bool visible; public bool reachable; public Box rect;
    }
    [Serializable] public sealed class Unit { public int id; public int originalId; public bool transformed; public int slot; public string name; public bool ally; public int x; public int y; public int hp; public int maxHP; public int level; public int attackBuff; public bool icon; public bool motif; public bool motifVisible; public float motifAlpha; }
    [Serializable] public sealed class Cell { public int x; public int y; public Box rect; }
    [Serializable] public sealed class BattleState
    {
        public bool active; public bool deploying; public bool fusionOpen; public bool result; public bool won;
        public int battleId; public int[] originalIds; public Unit[] units; public Cell[] cells; public BattleFusionRules.Plan plan;
        public int conflicts; public int previewLinks; public string fusionSummary;
        public string[] logs;
        public string resultText; public bool resultTextOverflow;
        public string[] supportTextures; public int supportVfxCount;
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
    }

    private StageData fixtureStage;
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
                case "resume": PrepareFormation(request.value == "alternate"); break;
                case "nextDefeat": fixtureStage.enemyLevel = 2000; break;
                case "gate20": GameManager.Instance.RegisterClearedStage(20); GameManager.Instance.SpendStagePoints(GameManager.Instance.StagePoints); break;
                case "fundFusion": GameManager.Instance.AddStagePoints(10); break;
                case "corruptPlan": PlayerProgressStore.SetString("KanjiBattle.Fusion.AdjacentChoices.v2", "{broken"); PlayerProgressStore.Save(); break;
                case "snapshot": break;
                case "recordsProbe": FindFirstObjectByType<BattleManager>().ProbeBattleRecords(); break;
                case "supportVfxProbe": FindFirstObjectByType<BattleManager>().ProbeSupportVfx(request.value); break;
                case "supportProbe": FindFirstObjectByType<BattleManager>().ProbeSupportSkills(); break;
                case "defenseProbe": FindFirstObjectByType<BattleManager>().ProbeDefenseSkills(); break;
                case "recruitSupport": ProbeRecruitment(new[] { 27, 28, 29 }, 35); break;
                case "recruitCow": ProbeRecruitment(new[] { 30 }, 80); break;
                case "defeatCow": FindFirstObjectByType<BattleManager>().DefeatExperienceUnit(); break;
                case "elementProbe": FindFirstObjectByType<BattleManager>().ProbeElementSkills(); break;
                case "recruitElements": ProbeRecruitment(new[] { 31, 32, 33 }, 100); break;
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
        UIManager.Instance.ShowTop();
        GameManager.Instance.ResetProgress();
        FacilityManager.Instance.ResetProgress();
        PlayerInventory.Instance.ResetProgress();
        ResearchBondService.Instance.ResetProgress();
        CharacterRecords.ResetProgress();
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
            fixtureStage.stageName = "干支と育成の確認";
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

    private Snapshot Capture()
    {
        BattleManager battle = FindObjectsByType<BattleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        FacilityData fusion = FacilityManager.Instance.GetFacilities().First(data => data.effectType == FacilityEffectType.Fusion);
        return new Snapshot
        {
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

    private static void ProbeRecruitment(int[] ids, int threshold)
    {
        CharacterDatabase database = Resources.Load<CharacterDatabase>("CharacterDatabase");
        ResearchBondService bonds = ResearchBondService.Instance;
        foreach (int id in ids)
        {
            CharacterData data = database.GetById(id);
            if (PlayerInventory.Instance.IsOwned(data) || bonds.GetBondThreshold(data) != threshold)
                throw new InvalidOperationException("Invalid new-character recruitment mapping: " + id);
            bonds.RegisterDefeated(data);
            if (!bonds.GetDefeatedCandidates().Contains(data))
                throw new InvalidOperationException("New character missing from defeated candidates: " + id);
            for (int win = 0; win < threshold && !bonds.CanRecruit(data); win++)
                bonds.ResolveVictory(new[] { id });
            if (PlayerInventory.Instance.IsOwned(data) || !bonds.CanRecruit(data) || !bonds.Recruit(data))
                throw new InvalidOperationException("New character could not be recruited: " + id);
        }
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
            reachable = hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Selectable>() == selectable
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
