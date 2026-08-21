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
            PlayerSettings.WebGL.dataCaching = false;

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

            PatchGameIndexHtml();

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

        private static void PatchGameIndexHtml()
        {
            string indexPath = Path.Combine(GameBuildPath, "index.html");
            if (!File.Exists(indexPath))
            {
                Debug.LogWarning($"WebGL index.html was not found at {indexPath}");
                return;
            }

            string html = File.ReadAllText(indexPath);
            html = html.Replace(
                "<canvas id=\"unity-canvas\" width=960 height=600 tabindex=\"-1\"></canvas>",
                "<canvas id=\"unity-canvas\" tabindex=\"-1\"></canvas>");
            html = html.Replace(
                "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {",
                "if (window.matchMedia('(max-width: 720px), (max-height: 520px)').matches || /iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {");
            html = html.Replace(
                "canvas.style.width = \"960px\";\n        canvas.style.height = \"600px\";",
                "canvas.style.width = \"min(960px, 100vw)\";\n        canvas.style.height = \"min(600px, 100vh)\";");
            File.WriteAllText(indexPath, html);

            string stylePath = Path.Combine(GameBuildPath, "TemplateData", "style.css");
            if (!File.Exists(stylePath))
            {
                Debug.LogWarning($"WebGL style.css was not found at {stylePath}");
                return;
            }

            File.WriteAllText(stylePath, GetGameStyleCss());
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
      body { padding: 10px; gap: 10px; }
      header { display: block; }
      p { font-size: 14px; line-height: 1.45; }
      .open-link { display: inline-block; margin-top: 6px; font-size: 20px; }
      .game-frame { height: calc(100dvh - 150px); min-height: 520px; aspect-ratio: auto; }
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

        private static string GetGameStyleCss()
        {
            return @"html, body {
  width: 100%;
  height: 100%;
  min-height: 100dvh;
  padding: 0;
  margin: 0;
  overflow: hidden;
  background: #17120d;
}
#unity-container {
  position: fixed;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #17120d;
}
#unity-container.unity-desktop {
  width: 100%;
  height: 100%;
}
#unity-container.unity-mobile {
  width: 100%;
  height: 100%;
}
#unity-canvas {
  display: block;
  width: min(960px, 100vw);
  height: min(600px, 100vh);
  background: #231F20;
}
.unity-mobile #unity-canvas {
  width: 100vw;
  height: 100dvh;
  max-height: calc(100vh - env(safe-area-inset-bottom, 0px));
}
#unity-loading-bar {
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  display: none;
}
#unity-logo {
  width: 154px;
  height: 130px;
  background: url('unity-logo-dark.png') no-repeat center;
}
#unity-progress-bar-empty {
  width: 141px;
  height: 18px;
  margin-top: 10px;
  margin-left: 6.5px;
  background: url('progress-bar-empty-dark.png') no-repeat center;
}
#unity-progress-bar-full {
  width: 0%;
  height: 18px;
  margin-top: 10px;
  background: url('progress-bar-full-dark.png') no-repeat center;
}
#unity-footer {
  position: absolute;
  left: 50%;
  bottom: 8px;
  width: min(960px, 100vw);
  transform: translateX(-50%);
  color: #fff2d7;
}
.unity-mobile #unity-footer {
  display: none;
}
#unity-logo-title-footer {
  float: left;
  width: 102px;
  height: 38px;
  background: url('unity-logo-title-footer.png') no-repeat center;
}
#unity-build-title {
  float: right;
  margin-right: 10px;
  line-height: 38px;
  font-family: Arial, sans-serif;
  font-size: 18px;
}
#unity-fullscreen-button {
  cursor: pointer;
  float: right;
  width: 38px;
  height: 38px;
  background: url('fullscreen-button.png') no-repeat center;
}
#unity-warning {
  position: absolute;
  left: 50%;
  top: 5%;
  transform: translateX(-50%);
  background: white;
  padding: 10px;
  display: none;
  z-index: 10;
}
";
        }
    }
}
