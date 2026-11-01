using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menü: Marmara > Emme Aracını Kur
// Player'ı eksiksiz kurar: Rigidbody2D + PlayerController (eksikse), SuctionTool,
// koni görseli için alt obje + LineRenderer. Tüm bağlantıları doldurur.
public static class SuctionSetup
{
    const string ConfigPath = "Assets/Config/GameConfig.asset";

    [MenuItem("Marmara/Emme Aracını Kur")]
    static void Setup()
    {
        GameConfig cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
        MucilageGrid grid = Object.FindFirstObjectByType<MucilageGrid>();

        // Önce script'e göre ara, yoksa isme göre bul
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        GameObject playerGo = player ? player.gameObject : GameObject.Find("Player");

        if (!cfg || !grid || !playerGo)
        {
            EditorUtility.DisplayDialog("Marmara",
                $"Eksik: {(cfg ? "" : "GameConfig asset'i ")}{(grid ? "" : "MucilageGrid ")}{(playerGo ? "" : "Player objesi")}", "Tamam");
            return;
        }

        if (!grid.cfg) { Undo.RecordObject(grid, "Grid config"); grid.cfg = cfg; }

        // Rigidbody2D: Gravity 0, Linear Damping 3, Freeze Rot Z, Interpolate
        Rigidbody2D rb = playerGo.GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = Undo.AddComponent<Rigidbody2D>(playerGo);
            rb.gravityScale = 0f;
            rb.linearDamping = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (!player) player = Undo.AddComponent<PlayerController>(playerGo);
        if (!player.cfg) player.cfg = cfg;
        if (!player.grid) player.grid = grid;

        // Koni görseli: SpriteRenderer ile aynı objede LineRenderer olamaz, o yüzden alt obje
        Transform coneT = playerGo.transform.Find("SuctionCone");
        if (!coneT)
        {
            GameObject go = new GameObject("SuctionCone");
            Undo.RegisterCreatedObjectUndo(go, "Emme konisi");
            go.transform.SetParent(playerGo.transform, false);
            coneT = go.transform;
        }

        LineRenderer lr = coneT.GetComponent<LineRenderer>();
        if (!lr) lr = Undo.AddComponent<LineRenderer>(coneT.gameObject);
        lr.useWorldSpace = true;
        lr.widthMultiplier = 0.06f;
        // Dalgıcın sprite materyalini kullan: sahnede göründüğünden eminiz
        SpriteRenderer psr = playerGo.GetComponent<SpriteRenderer>();
        lr.sharedMaterial = psr ? psr.sharedMaterial : AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.6f);
        lr.sortingOrder = 10;
        lr.enabled = false;

        SuctionTool tool = playerGo.GetComponent<SuctionTool>();
        if (!tool) tool = Undo.AddComponent<SuctionTool>(playerGo);
        tool.cfg = cfg;
        tool.grid = grid;
        tool.player = player;
        tool.coneLine = lr;

        EditorUtility.SetDirty(grid);
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(tool);
        EditorUtility.SetDirty(lr);
        EditorSceneManager.MarkSceneDirty(playerGo.scene);
        Debug.Log("Dalgıç ve emme aracı kuruldu. Sahneyi kaydetmeyi unutma (Ctrl+S).");
    }
}
