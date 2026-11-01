using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menü: Marmara > Nefes Sistemini Kur
// Player'a BreathSystem ekler ve bağlantılarını doldurur. Önce "Emme Aracını Kur" çalışmış olmalı.
public static class BreathSetup
{
    [MenuItem("Marmara/Nefes Sistemini Kur")]
    static void Setup()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        MucilageGrid grid = Object.FindFirstObjectByType<MucilageGrid>();
        if (!player || !grid)
        {
            EditorUtility.DisplayDialog("Marmara", "Önce \"Marmara > Emme Aracını Kur\" menüsünü çalıştır.", "Tamam");
            return;
        }

        BreathSystem breath = player.GetComponent<BreathSystem>();
        if (!breath) breath = Undo.AddComponent<BreathSystem>(player.gameObject);
        breath.cfg = player.cfg;
        breath.grid = grid;

        EditorUtility.SetDirty(breath);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Debug.Log("Nefes sistemi kuruldu. Sahneyi kaydetmeyi unutma (Ctrl+S).");
    }
}
