using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Plays the real scene and action coroutines. No alternate damage/AI simulator.
[InitializeOnLoad]
public static class CampaignBalanceRunner
{
    private const string ActiveKey = "KanjiBattle.Balance.Active";
    [Serializable] public sealed class Job
    {
        public int stage, level, seed, economyWins, enemyLevel, farmWins;
        public string profile, layout;
        public int[] party;
        public bool upgrades;
        public int[] enemyIds;
        public bool playthrough;
    }
    private static Job[] jobs;
    private static readonly List<object> results = new();
    private static readonly List<CharacterData> generated = new();
    private static BattleManager battle;
    private static int index;
    private static double started, ready;
    private static bool running;
    private static string output;
    private static object[] initialUnits;
    private static JArray economyRows;
    private static JObject economyEntry;
    private static StageData fixtureStage;
    private static int attempts, wins, battleSeed;
    private static readonly List<object> campaign = new();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(string name) => (T)typeof(BattleManager).GetField(name, Private).GetValue(battle);
    static CampaignBalanceRunner()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        PlayerProgressStore.BeginIsolatedEditorSession();
        AudioListener.volume = 0;
        AudioListener.pause = true;
        PlayerProgressStore.SetInt("KanjiBattle.Settings.BgmEnabled", 0);
        PlayerProgressStore.SetInt("KanjiBattle.Settings.SfxEnabled", 0);
        EditorApplication.update += Tick;
    }
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run from edit mode.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Unsaved scene changes.");
        SessionState.SetBool(ActiveKey, true);
        PlayerProgressStore.BeginIsolatedEditorSession();
        AudioListener.volume = 0;
        AudioListener.pause = true;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.EnterPlaymode();
    }
    private static string Argument(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs(); int position = Array.IndexOf(args, name);
        return position >= 0 && position + 1 < args.Length ? args[position + 1] : fallback;
    }
    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (GameManager.Instance == null || PlayerInventory.Instance == null || UIManager.Instance == null) return;
            if (jobs == null)
            {
                jobs = JsonConvert.DeserializeObject<Job[]>(File.ReadAllText(Argument("-balancePlan", "tmp/balance/plan.json")));
                output = Argument("-balanceOutput", "tmp/balance/results.json");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                TutorialDirector.Instance?.SuppressForDebug(true);
                SkillExecutor.ForceSkillActivationForVfxReview = false;
                QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Time.timeScale = 50; Time.fixedDeltaTime = 1; Time.maximumDeltaTime = 5;
                PlayerInventory.Instance.enabled = false; // No wall-clock mock-training income.
                var stageDatabase = Resources.FindObjectsOfTypeAll<StageDatabase>().First();
                economyRows = JArray.FromObject(CampaignEconomyProbe.Measure(stageDatabase));
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "economy.json"), economyRows.ToString());
                if (!jobs.Any(job => job.playthrough))
                {
                    GameManager.Instance.RegisterClearedStage(40);
                    GameManager.Instance.AddStagePoints(100000);
                    var rank = FacilityManager.Instance.GetFacilities().Single(f => f.effectType == FacilityEffectType.PlayerLevelCap);
                    for (int i = 0; i < 10 && FacilityManager.Instance.Upgrade(rank); i++) { }
                }
                battle = UnityEngine.Object.FindObjectsByType<BattleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
                ready = EditorApplication.timeSinceStartup + 1;
                Debug.Log($"[CampaignBalance] Starting {jobs.Length} real-engine battles; save isolated.");
            }
            if (EditorApplication.timeSinceStartup < ready) return;
            if (!running)
            {
                if (index == jobs.Length) { Finish(0); return; }
                Start(jobs[index]); return;
            }
            if (EditorApplication.timeSinceStartup - started > 90) throw new TimeoutException("Battle timed out: " + index);
            if (!Field<bool>("battleFinished")) return;
            Job job = jobs[index];
            var allies = Field<List<BattleCharacter>>("allies");
            var enemies = Field<List<BattleCharacter>>("enemies");
            bool won = Field<bool>("lastBattleWon");
            results.Add(new { job.stage, job.level, seed = battleSeed, job.profile, job.party, job.upgrades, job.economyWins,
                job.layout, job.playthrough, attempt = attempts + 1,
                initialUnits, economyEntry, enemyIds = Field<StageData>("currentStage").enemyPool.Select(data => data.characterId).ToArray(),
                won, turns = Field<int>("fusionTurn"),
                allyHP = allies.Where(u => u != null && !u.isDead).Sum(u => u.currentHP),
                enemyHP = enemies.Where(u => u != null && !u.isDead).Sum(u => u.currentHP),
                seconds = EditorApplication.timeSinceStartup - started });
            File.WriteAllText(output, JsonConvert.SerializeObject(results, Formatting.Indented));
            Debug.Log($"[CampaignBalance] {index + 1}/{jobs.Length}: stage {job.stage} {job.profile} win={Field<bool>("lastBattleWon")} turns={Field<int>("fusionTurn")}");
            if (job.playthrough)
            {
                attempts++;
                if (won) { wins++; CampaignEconomyProbe.Spend(job.stage, Route(job.profile)); }
                if (wins >= job.farmWins || attempts >= 7)
                {
                    campaign.Add(new { job.stage, attempts, wins, level = PlayerInventory.Instance.PlayerLevel,
                        slots = CastleTree.Slots, points = GameManager.Instance.StagePoints });
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "campaign.json"), JsonConvert.SerializeObject(campaign, Formatting.Indented));
                    if (wins == 0) { Debug.Log("[CampaignBalance] Campaign blocked at stage " + job.stage); Finish(0); return; }
                    index++; attempts = wins = 0;
                }
            }
            else index++;
            running = false; ready = EditorApplication.timeSinceStartup + .05;
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(1); }
    }
    private static void Start(Job job)
    {
        UIManager.Instance.ShowTop();
        if (fixtureStage != null) UnityEngine.Object.Destroy(fixtureStage);
        fixtureStage = null;
        foreach (var data in generated) UnityEngine.Object.Destroy(data);
        generated.Clear();
        if (!job.playthrough) CharacterInvestment.Clear();
        PlayerSkillRules.ResetProgress();
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        economyEntry = null;
        if (job.playthrough)
        {
            economyEntry = JObject.FromObject(new { level = PlayerInventory.Instance.PlayerLevel, slots = CastleTree.Slots,
                owned = PlayerInventory.Instance.GetUnlockedCharacters().Select(c => c.characterId).ToArray(),
                investments = CharacterInvestment.Purchased, fusionUnlocked = FacilityManager.Instance.IsFusionUnlocked() });
        }
        else if (job.economyWins > 0)
        {
            string route = Route(job.profile);
            economyEntry = (JObject)economyRows.Single(row => (int)row["stage"] == job.stage
                && (int)row["winsPerStage"] == job.economyWins && (string)row["route"] == route);
        }
        if (economyEntry != null)
        {
            job.level = (int)economyEntry["level"];
            foreach (string id in economyEntry["investments"].Values<string>()) CharacterInvestment.Record(id, 0);
            var owned = economyEntry["owned"].Values<int>().ToHashSet();
            int capacity = (int)economyEntry["slots"];
            int[] requestedFusions = job.party.Where(id => id < 0).ToArray();
            var materialIds = job.party.SelectMany(id => id > 0 ? new[] { id }
                : BattleFusionRules.GetRecipe(id).Materials.Select(name => db.characters.Single(c => c.characterName == name).characterId)).ToList();
            var selected = materialIds.Where(id => id != 20 && owned.Contains(id))
                .Take(capacity - (owned.Contains(20) ? 1 : 0)).ToList();
            if (owned.Contains(20)) selected.Add(20);
            while (selected.Count < capacity) selected.Insert(0, owned.Contains(10) ? 10 : 1);
            if (selected.Any(id => !owned.Contains(id))) throw new Exception("Unpaid recruitment in progression benchmark.");
            foreach (int id in requestedFusions)
            {
                if (!(bool)economyEntry["fusionUnlocked"]) continue;
                var materials = BattleFusionRules.GetRecipe(id).Materials.Select(name => db.characters.Single(c => c.characterName == name).characterId).ToArray();
                var remainder = new List<int>(selected);
                if (!materials.All(remainder.Remove)) continue;
                selected = remainder; selected.Insert(0, id);
            }
            job.party = selected.ToArray();
        }
        else if (job.upgrades)
        {
            foreach (var node in CastleTree.Nodes.Where(n => n.kind == CastleTree.Kind.Character))
                foreach (var stat in CharacterInvestment.Upgrades(CastleTree.Character(node)))
                {
                    int gate = Mathf.Max(node.stage, CharacterInvestment.IsSecond(stat) ? 20 : stat == CharacterInvestment.Stat.Chance ? 10 : 5);
                    if (gate < job.stage) CharacterInvestment.Record(CharacterInvestment.NodeId(node.target, stat), 0);
                }
            foreach (var node in CastleTree.Nodes.Where(n => n.kind == CastleTree.Kind.CategoryUpgrade && n.stage < job.stage))
                CharacterInvestment.Record(node.id, 0);
        }
        CharacterData Resolve(int id)
        {
            if (id > 0) return db.GetById(id) ?? throw new Exception("Unknown character " + id);
            var recipe = BattleFusionRules.GetRecipe(id) ?? throw new Exception("Unknown fusion " + id);
            var data = CharacterData.CreateBattleFusion(recipe, recipe.Materials.Select(name => db.characters.Single(c => c.characterName == name)).ToArray(), job.level);
            generated.Add(data); return data;
        }
        var party = job.party.Select(Resolve).ToList();
        // SpawnCharacter intentionally uses Lv1 for unowned preview characters.
        // A benchmark party must first be recruited in the isolated inventory.
        foreach (var data in party.Where(data => data.characterId > 0))
            PlayerInventory.Instance.AddCharacter(data);
        if (!job.playthrough) PlayerInventory.Instance.SetPlayerLevelForDebug(job.level);
        if (PlayerInventory.Instance.PlayerLevel != job.level) throw new Exception("Requested level capped.");
        var stage = Resources.FindObjectsOfTypeAll<StageDatabase>().First().GetStageById(job.stage);
        if (job.enemyIds != null)
        {
            fixtureStage = UnityEngine.Object.Instantiate(stage);
            fixtureStage.enemyPool = job.enemyIds.Select(db.GetById).ToList();
            fixtureStage.enemyPositions = CampaignTreeAuthoring.Positions(job.enemyIds);
            fixtureStage.enemyLevel = job.enemyLevel;
            stage = fixtureStage;
        }
        GameManager.Instance.SetSelectedStage(stage);
        bool Rear(CharacterData data) => data.category == CharacterCategory.Ranged || data.skillType is SkillType.WaterHeal or SkillType.BellEcho or SkillType.WindChime or SkillType.Forge;
        int front = 0, rear = 0; int[] columns = { 2, 3, 1, 4, 0, 5 };
        bool staggered = job.layout == "staggered" || (job.layout == "adaptive" && FacilityManager.Instance.IsFusionUnlocked());
        int[] frontColumns = staggered ? party.Count(data => !Rear(data)) > 3
            ? new[] { 0, 1, 4, 2, 3, 5 } : new[] { 0, 2, 4, 1, 3, 5 } : columns;
        int[] rearColumns = staggered ? new[] { 1, 3, 5, 0, 2, 4 } : columns;
        var positions = party.Select(data => Rear(data) ? new Vector2Int(rearColumns[rear++], 6) : new Vector2Int(frontColumns[front++], 5)).ToArray();
        if (job.playthrough && FacilityManager.Instance.IsFusionUnlocked()) positions = SeparateFusionMaterials(party, positions);
        BattleDeploymentRules.Save(party.Select(data => data.characterId).ToArray(), positions);
        battleSeed = job.seed + (job.playthrough ? attempts * 997 : 0);
        UnityEngine.Random.InitState(battleSeed);
        UIManager.Instance.ShowBattle();
        battle.StartBattle(party, stage);
        var units = Field<List<BattleCharacter>>("allies");
        // The normal fusion path passes an explicit level override. These factory
        // fixtures bypass material spawning, so reproduce that override here.
        foreach (var unit in units.Where(unit => unit.data.characterId < 0)) unit.SetLevelForDebug(job.level);
        foreach (var unit in units)
            if (unit.level != job.level || unit.maxHP != unit.data.GetMaxHP(job.level)
                || unit.attack != unit.data.GetAttack(job.level) || unit.defense != unit.data.GetDefense(job.level))
                throw new Exception("Benchmark level/stat mismatch: " + unit.data.characterName);
        initialUnits = units.Select(unit => (object)new { id = unit.data.characterId, unit.level,
            hp = unit.maxHP, unit.attack, unit.defense,
            x = unit.gridPos.x, y = unit.gridPos.y,
            materials = unit.data.InvestmentMaterials?.Select(data => data.characterId).ToArray() ?? new[] { unit.data.characterId } }).ToArray();
        typeof(BattleManager).GetMethod("ConfirmDeployment", Private).Invoke(battle, null);
        if (battle.IsDeploying) throw new Exception("Deployment was blocked.");
        if (!Field<List<BattleCharacter>>("allies").Select(unit => unit.data.characterId).OrderBy(id => id)
            .SequenceEqual(party.Select(data => data.characterId).OrderBy(id => id)))
            throw new Exception("Unexpected automatic fusion changed the benchmark party.");
        started = EditorApplication.timeSinceStartup; running = true;
    }
    private static void Finish(int code)
    {
        SessionState.SetBool(ActiveKey, false); EditorApplication.update -= Tick;
        Debug.Log($"[CampaignBalance] Finished: {results.Count} battles, exit {code}.");
        EditorApplication.Exit(code);
    }
    private static string Route(string profile) => profile == "ranged" ? "ranged" : profile is "spear" or "sentinel" ? "spear" : "balanced";

    private static Vector2Int[] SeparateFusionMaterials(List<CharacterData> party, Vector2Int[] preferred)
    {
        var choices = BattleFusionRules.Empty(party);
        if (BattleFusionRules.ResolveAdjacent(party, preferred, choices).candidates.Count == 0) return preferred;
        var groups = BattleFusionRules.Candidates(party);
        var cells = Enumerable.Range(0, BattleDeploymentRules.Capacity).Select(i => BattleDeploymentRules.DefaultPosition(i, true)).ToArray();
        var assigned = new Vector2Int[party.Count];
        var occupied = new HashSet<Vector2Int>();
        int searched = 0;
        bool Place(int index)
        {
            if (++searched > 100000) throw new Exception("Could not find a non-fusing benchmark placement.");
            if (index == party.Count) return BattleFusionRules.ResolveAdjacent(party, assigned, choices).candidates.Count == 0;
            foreach (var cell in cells.OrderBy(cell => cell.y == preferred[index].y ? 0 : 1)
                .ThenBy(cell => Mathf.Abs(cell.x - preferred[index].x)))
            {
                if (occupied.Contains(cell)) continue;
                assigned[index] = cell;
                bool pair = groups.Where(group => group.members.Length == 2 && group.members.Contains(index))
                    .Any(group => group.members.Any(other => other < index &&
                        Mathf.Abs(assigned[other].x - cell.x) + Mathf.Abs(assigned[other].y - cell.y) == 1));
                if (pair) continue;
                occupied.Add(cell);
                if (Place(index + 1)) return true;
                occupied.Remove(cell);
            }
            return false;
        }
        if (!Place(0)) throw new Exception("No non-fusing benchmark placement exists.");
        return assigned;
    }
}
