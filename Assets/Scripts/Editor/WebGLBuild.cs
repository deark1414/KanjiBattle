using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KanjiBattle.Editor
{
    public static class WebGLBuild
    {
        private const string BuildRoot = "Docs";
        private const string GameBuildPath = BuildRoot + "/game";

        [MenuItem("KanjiBattle/Build/GitHub Pages WebGL")]
        public static void BuildGitHubPages()
        {
            if (EditorApplication.isPlaying)
            {
                throw new System.Exception("Exit Play Mode before running the WebGL build.");
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new System.Exception("WebGL Build Support is not installed for this Unity editor.");
            }

            AssetDatabase.SaveAssets();

            PrepareOutputDirectory();
            WriteGitHubPagesFiles();

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = GameBuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception($"WebGL build failed: {summary.result}");
            }

            Debug.Log($"WebGL build succeeded: {GameBuildPath} ({summary.totalSize} bytes)");
        }

        private static void PrepareOutputDirectory()
        {
            if (Directory.Exists(GameBuildPath))
            {
                Directory.Delete(GameBuildPath, true);
            }

            Directory.CreateDirectory(GameBuildPath);
            Directory.CreateDirectory(BuildRoot);
        }

        private static void WriteGitHubPagesFiles()
        {
            File.WriteAllText(Path.Combine(BuildRoot, ".nojekyll"), string.Empty);
            File.WriteAllText(Path.Combine(BuildRoot, "index.html"), GetIndexHtml());
        }

        private static string GetIndexHtml()
        {
            return @"<!doctype html>
<html lang=""ja"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Kanji Battle</title>
  <style>
    :root {
      color-scheme: dark;
      --bg: #17120d;
      --panel: #241a12;
      --text: #fff2d7;
      --muted: #cdbb99;
      --accent: #88d27a;
    }
    * { box-sizing: border-box; }
    html, body { margin: 0; min-height: 100%; background: var(--bg); color: var(--text); font-family: system-ui, -apple-system, BlinkMacSystemFont, ""Hiragino Sans"", ""Yu Gothic"", sans-serif; }
    body { display: flex; flex-direction: column; align-items: center; padding: 20px; gap: 14px; }
    header { width: min(1100px, 100%); display: flex; justify-content: space-between; align-items: end; gap: 16px; }
    h1 { margin: 0; font-size: clamp(24px, 4vw, 42px); font-weight: 800; }
    p { margin: 4px 0 0; color: var(--muted); line-height: 1.6; }
    main { width: min(1100px, 100%); }
    .game-frame { width: 100%; aspect-ratio: 16 / 10; min-height: 520px; border: 1px solid #4a3521; background: #000; }
    iframe { display: block; width: 100%; height: 100%; border: 0; }
    .open-link { color: var(--accent); text-decoration: none; font-weight: 700; white-space: nowrap; }
    @media (max-width: 720px) {
      body { padding: 10px; }
      header { display: block; }
      .game-frame { min-height: 72vh; aspect-ratio: auto; }
    }
  </style>
</head>
<body>
  <header>
    <div>
      <h1>Kanji Battle</h1>
      <p>漢字を集め、編成し、ステージを攻略するブラウザ版です。</p>
    </div>
    <a class=""open-link"" href=""game/index.html"">ゲームを別画面で開く</a>
  </header>
  <main>
    <div class=""game-frame"">
      <iframe src=""game/index.html"" title=""Kanji Battle WebGL"" allowfullscreen></iframe>
    </div>
  </main>
</body>
</html>
";
        }
    }
}
