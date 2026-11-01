using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menü: Marmara > WebGL Build Al ve Çalıştır
// itch.io için gereken ayarları yapar (sıkıştırma kapalı, 1280x720), Builds/WebGL klasörüne build alır
// ve Unity'nin yerel sunucusuyla tarayıcıda açar. Giriş seslendirmesi sadece bu tarayıcı sürümünde duyulur.
// Az RAM'li bilgisayarlar için üretilen kodu küçültür (yüksek stripping + boyut odaklı IL2CPP): derleyici daha az bellek ister.
//
// Menü: Marmara > Windows Build Al (yedek)
// WebGL derlemesi bellek yüzünden düşerse GDD'deki yedek plan: Mono ile hızlı, az bellekli Windows build.
public static class WebGLBuild
{
    const string Scene = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Marmara/WebGL Build Al ve Çalıştır")]
    static void BuildWebGL()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // itch.io'da en sorunsuz yol (GDD)
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        CommonSettings();

        // Daha az C++ kodu = derleyici başına daha az bellek
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);

        Build(BuildTarget.WebGL, "Builds/WebGL", BuildOptions.AutoRunPlayer,
            "itch.io için bu klasörün içeriğini zip'le.");
    }

    [MenuItem("Marmara/Windows Build Al (yedek)")]
    static void BuildWindows()
    {
        CommonSettings();
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x); // C++ derlemesi yok, hızlı
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;

        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/MarmaraninNefesi.exe", BuildOptions.ShowBuiltPlayer,
            "itch.io'ya 'Windows' olarak yüklemek için Builds/Windows klasörünü zip'le.");
    }

    static void CommonSettings()
    {
        PlayerSettings.runInBackground = true;
        PlayerSettings.productName = "Marmara'nın Nefesi";
    }

    static void Build(BuildTarget target, string path, BuildOptions options, string hint)
    {
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = path,
            target = target,
            options = options,
        });

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"Build hazır: {path} ({report.summary.totalSize / (1024 * 1024)} MB). {hint}");
        else
            Debug.LogError($"Build başarısız: {report.summary.result}. Konsoldaki kırmızı hatalara bak.");
    }
}
