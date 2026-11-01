using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menü: Marmara > Arayüzü Kur
// Sahnede "UI" objesi oluşturur (yoksa), UIManager ekler ve bağlantıları doldurur.
// Canvas ve tüm öğeler oyun başında kodla kurulur; burada sadece bağlantılar var.
// TextMeshPro kaynakları (font, ayarlar) projede yoksa onları da içe aktarır.
public static class UISetup
{
    [MenuItem("Marmara/Arayüzü Kur")]
    static void Setup()
    {
        if (!Resources.Load<TMPro.TMP_Settings>("TMP Settings"))
        {
            TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.Log("TextMeshPro kaynakları içe aktarıldı.");
        }

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        BreathSystem breath = Object.FindFirstObjectByType<BreathSystem>();
        ScoreManager score = Object.FindFirstObjectByType<ScoreManager>();
        if (!gm || !breath || !score)
        {
            EditorUtility.DisplayDialog("Marmara",
                "Önce GameManager, nefes ve skor kurulum menülerini çalıştır.", "Tamam");
            return;
        }

        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (!ui)
        {
            GameObject go = new GameObject("UI");
            Undo.RegisterCreatedObjectUndo(go, "UI");
            ui = go.AddComponent<UIManager>();
        }
        ui.cfg = gm.cfg;
        ui.grid = gm.grid;
        ui.breath = breath;

        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Debug.Log("Arayüz kuruldu. Sahneyi kaydetmeyi unutma (Ctrl+S).");
    }
}
