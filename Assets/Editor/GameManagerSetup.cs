using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menü: Marmara > GameManager'ı Kur
// Sahnede "GameManager" objesi oluşturur (yoksa), GameManager scriptini ekler ve bağlantıları doldurur.
public static class GameManagerSetup
{
    const string ConfigPath = "Assets/Config/GameConfig.asset";

    [MenuItem("Marmara/GameManager'ı Kur")]
    static void Setup()
    {
        GameConfig cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
        MucilageGrid grid = Object.FindFirstObjectByType<MucilageGrid>();
        if (!cfg || !grid)
        {
            EditorUtility.DisplayDialog("Marmara", "GameConfig asset'i veya MucilageGrid bulunamadı.", "Tamam");
            return;
        }

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (!gm)
        {
            GameObject go = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(go, "GameManager");
            gm = go.AddComponent<GameManager>();
        }
        gm.cfg = cfg;
        gm.grid = grid;

        EditorUtility.SetDirty(gm);
        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        Debug.Log("GameManager kuruldu. Sahneyi kaydetmeyi unutma (Ctrl+S).");
    }
}
