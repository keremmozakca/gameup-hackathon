using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menü: Marmara > Skor Sistemini Kur
// GameManager objesine ScoreManager ekler ve bağlantıları doldurur. Önce GameManager ve nefes sistemi kurulmuş olmalı.
public static class ScoreSetup
{
    [MenuItem("Marmara/Skor Sistemini Kur")]
    static void Setup()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        BreathSystem breath = Object.FindFirstObjectByType<BreathSystem>();
        if (!gm || !breath)
        {
            EditorUtility.DisplayDialog("Marmara",
                "Önce \"GameManager'ı Kur\" ve \"Nefes Sistemini Kur\" menülerini çalıştır.", "Tamam");
            return;
        }

        ScoreManager score = gm.GetComponent<ScoreManager>();
        if (!score) score = Undo.AddComponent<ScoreManager>(gm.gameObject);
        score.cfg = gm.cfg;
        score.grid = gm.grid;
        score.breath = breath;

        EditorUtility.SetDirty(score);
        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        Debug.Log("Skor sistemi kuruldu. Sahneyi kaydetmeyi unutma (Ctrl+S).");
    }

    [MenuItem("Marmara/En İyi Skorları Sıfırla")]
    static void ResetTopScores()
    {
        for (int i = 0; i < 5; i++) PlayerPrefs.DeleteKey("top" + i);
        PlayerPrefs.Save();
        Debug.Log("En iyi skorlar silindi.");
    }
}
