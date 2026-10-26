using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
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
        public int stage, level, seed;
        public string profile;
        public int[] party;
        public bool upgrades;
    }
    private static Job[] jobs;
    private static readonly List<object> results = new();
    private static readonly List<CharacterData> generated = new();
    private static BattleManager battle;
    private static int index;
    private static double started, ready;
    private static bool running;
    private static string output;
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
                Time.timeScale = 50; Time.fixedDeltaTime = 1; Time.maximumDeltaTime = 5;
                PlayerInventory.Instance.enabled = false; // No wall-clock mock-training income.
                var stageDatabase = Resources.FindObjectsOfTypeAll<StageDatabase>().First();
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "economy.json"),
                    JsonConvert.SerializeObject(CampaignEconomyProbe.Measure(stageDatabase), Formatting.Indented));
                GameManager.Instance.RegisterClearedStage(40);
                GameManager.Instance.AddStagePoints(100000);
                var rank = FacilityManager.Instance.GetFacilities().Single(f => f.effectType == FacilityEffectType.PlayerLevelCap);
                for (int i = 0; i < 10 && FacilityManager.Instance.Upgrade(rank); i++) { }
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
            results.Add(new { job.stage, job.level, job.seed, job.profile, job.party, job.upgrades,
                won = Field<bool>("lastBattleWon"), turns = Field<int>("fusionTurn"),
                allyHP = allies.Where(u => u != null && !u.isDead).Sum(u => u.currentHP),
                enemyHP = enemies.Where(u => u != null && !u.isDead).Sum(u => u.currentHP),
                seconds = EditorApplication.timeSinceStartup - started });
            File.WriteAllText(output, JsonConvert.SerializeObject(results, Formatting.Indented));
            Debug.Log($"[CampaignBalance] {index + 1}/{jobs.Length}: stage {job.stage} {job.profile} win={Field<bool>("lastBattleWon")} turns={Field<int>("fusionTurn")}");
            running = false; index++; ready = EditorApplication.timeSinceStartup + .05;
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(1); }
    }
    private static void Start(Job job)
    {
        UIManager.Instance.ShowTop();
        foreach (var data in generated) UnityEngine.Object.Destroy(data);
        generated.Clear(); CharacterInvestment.Clear(); PlayerSkillRules.ResetProgress();
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (job.upgrades)
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
        PlayerInventory.Instance.SetPlayerLevelForDebug(job.level);
        if (PlayerInventory.Instance.PlayerLevel != job.level) throw new Exception("Requested level capped.");
        var stage = Resources.FindObjectsOfTypeAll<StageDatabase>().First().GetStageById(job.stage);
        GameManager.Instance.SetSelectedStage(stage);
        int front = 0, rear = 0; int[] columns = { 2, 3, 1, 4, 0, 5 };
        bool Rear(CharacterData data) => data.category == CharacterCategory.Ranged || data.skillType is SkillType.WaterHeal or SkillType.BellEcho or SkillType.WindChime or SkillType.Forge;
        var positions = party.Select(data => Rear(data) ? new Vector2Int(columns[rear++], 6) : new Vector2Int(columns[front++], 5)).ToArray();
        BattleDeploymentRules.Save(party.Select(data => data.characterId).ToArray(), positions);
        UnityEngine.Random.InitState(job.seed);
        UIManager.Instance.ShowBattle();
        battle.StartBattle(party, stage);
        typeof(BattleManager).GetMethod("ConfirmDeployment", Private).Invoke(battle, null);
        if (battle.IsDeploying) throw new Exception("Deployment was blocked.");
        started = EditorApplication.timeSinceStartup; running = true;
    }
    private static void Finish(int code)
    {
        SessionState.SetBool(ActiveKey, false); EditorApplication.update -= Tick;
        Debug.Log($"[CampaignBalance] Finished: {results.Count} battles, exit {code}.");
        EditorApplication.Exit(code);
    }
}
