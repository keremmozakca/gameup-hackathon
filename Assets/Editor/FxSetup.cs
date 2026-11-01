using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menü: Marmara > Efekt ve Sesleri Kur
// Sahnede "FX" objesi oluşturur (yoksa), FxManager + SoundManager ekler ve bağlantıları doldurur.
public static class FxSetup
{
    [MenuItem("Marmara/Efekt ve Sesleri Kur")]
    static void Setup()
    {
        MucilageGrid grid = Object.FindFirstObjectByType<MucilageGrid>();
        BreathSystem breath = Object.FindFirstObjectByType<BreathSystem>();
        SuctionTool suction = Object.FindFirstObjectByType<SuctionTool>();
        if (!grid || !breath || !suction)
        {
            EditorUtility.DisplayDialog("Marmara", "Önce önceki kurulum menülerini çalıştır.", "Tamam");
            return;
        }

        GameObject go = GameObject.Find("FX");
        if (!go)
        {
            go = new GameObject("FX");
            Undo.RegisterCreatedObjectUndo(go, "FX");
        }

        FxManager fx = go.GetComponent<FxManager>();
        if (!fx) fx = Undo.AddComponent<FxManager>(go);
        fx.grid = grid;
        fx.sprite = grid.squareSprite;
        SpriteRenderer psr = breath.GetComponent<SpriteRenderer>();
        if (psr) fx.material = psr.sharedMaterial;

        SoundManager sm = go.GetComponent<SoundManager>();
        if (!sm) sm = Undo.AddComponent<SoundManager>(go);
        sm.grid = grid;
        sm.breath = breath;
        sm.suction = suction;

        EditorUtility.SetDirty(fx);
        EditorUtility.SetDirty(sm);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("Efekt ve sesler kuruldu. Sahneyi kaydetmeyi unutma (Ctrl+S).");
    }
}
