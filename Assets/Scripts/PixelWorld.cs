using UnityEngine;

// Dünyanın piksel art süslemesi. "FX" objesine ekle; her şey oyun başında kodla kurulur.
// Su degradesi, gökyüzü + İstanbul silueti + bulutlar, hareketli dalgalar, sallanan tekne,
// deniz tabanı, sallanan yosunlar ve su içi ışık huzmeleri. Oynanışı etkilemez.
public class PixelWorld : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    [Tooltip("Düz kare sprite (ışık huzmeleri için)")]
    public Sprite squareSprite;
    [Tooltip("Sprite materyali (dalgıcınki)")]
    public Material material;

    const float SurfaceY = 14f; // su yüzeyi (deniz 14 kare yüksekliğinde)

    Transform boat, waveBack, waveFront;
    Transform[] clouds, kelps, rays;

    void Start()
    {
        float w = cfg.width;

        // Eski düz gökyüzü karesini gizle
        GameObject oldSky = GameObject.Find("Sky");
        if (oldSky && oldSky.TryGetComponent(out SpriteRenderer oldSr)) oldSr.enabled = false;

        // Su ve gökyüzü degradeleri (bantlı)
        Make("Water", PixelArt.Gradient("water", new Color32(0x06, 0x2A, 0x3C, 0xFF), new Color32(0x1C, 0x8F, 0xB0, 0xFF), 14),
            new Vector3(0, 0, 0), new Vector3(w, 1f, 1f), -30);
        Make("Sky", PixelArt.Gradient("sky", new Color32(0xC9, 0xD6, 0xDF, 0xFF), new Color32(0x8F, 0xB3, 0xCF, 0xFF), 8),
            new Vector3(0, SurfaceY, 0), new Vector3(w, 0.5f, 1f), -30);
        Make("Skyline", PixelArt.Skyline(), new Vector3(0, SurfaceY + 0.1f, 0), Vector3.one, -29);

        clouds = new Transform[3];
        for (int i = 0; i < clouds.Length; i++)
            clouds[i] = Make("Cloud", PixelArt.Cloud(), new Vector3(5 + i * 11, 16.6f + (i % 2) * 0.7f, 0), Vector3.one, -28).transform;

        // Su içi ışık huzmeleri
        rays = new Transform[4];
        for (int i = 0; i < rays.Length; i++)
        {
            SpriteRenderer r = Make("Ray", squareSprite, new Vector3(4 + i * 8, 9f, 0), new Vector3(1.4f, 14f, 1f), -27);
            r.color = new Color(1f, 1f, 1f, 0.05f);
            rays[i] = r.transform;
        }

        // Deniz tabanı ve yosunlar (müsilajın arkasında)
        Make("Seabed", PixelArt.Seabed(), new Vector3(0, 0, 0), Vector3.one, -20);
        kelps = new Transform[9];
        for (int i = 0; i < kelps.Length; i++)
            kelps[i] = Make("Kelp", PixelArt.Kelp(), new Vector3(2 + i * 3.4f + Random.Range(-0.8f, 0.8f), 0.3f, 0), Vector3.one * Random.Range(0.8f, 1.3f), -19).transform;

        // Dalgalar: arka (açık) ve ön (koyu), aralarında tekne
        waveBack = MakeWaves("WaveBack", PixelArt.Wave("waveB", new Color32(0x3C, 0xB4, 0xD0, 0xFF), new Color32(0xE8, 0xF8, 0xFF, 0xFF), 0f), 3, SurfaceY - 0.35f);
        boat = Make("Boat", PixelArt.Boat(), new Vector3(cfg.boatPosition.x, SurfaceY, 0), Vector3.one, 4).transform;
        waveFront = MakeWaves("WaveFront", PixelArt.Wave("waveF", new Color32(0x1C, 0x8F, 0xB0, 0xE6), new Color32(0xC8, 0xEE, 0xFA, 0xFF), 0.5f), 5, SurfaceY - 0.45f);
    }

    SpriteRenderer Make(string name, Sprite sprite, Vector3 pos, Vector3 scale, int order)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform, false);
        sr.transform.position = pos;
        sr.transform.localScale = scale;
        sr.sprite = sprite;
        if (material) sr.sharedMaterial = material;
        sr.sortingOrder = order;
        return sr;
    }

    // Ekranı yatayda kaplayan, kaydırılabilen dalga şeridi (her parça 2 birim)
    Transform MakeWaves(string name, Sprite sprite, int order, float y)
    {
        Transform root = new GameObject(name).transform;
        root.SetParent(transform, false);
        root.position = new Vector3(0, y, 0);
        for (int i = 0; i < cfg.width / 2 + 2; i++)
        {
            SpriteRenderer sr = Make(name + i, sprite, Vector3.zero, Vector3.one, order);
            sr.transform.SetParent(root, false);
            sr.transform.localPosition = new Vector3(i * 2f, 0, 0);
        }
        return root;
    }

    void Update()
    {
        if (!boat) return;
        float t = Time.time; // menüde de dalgalar oynasın diye ölçekli zaman yerine Time.time (menüde durur, sorun değil)

        waveBack.position = new Vector3(-Mathf.Repeat(t * 0.4f, 2f), waveBack.position.y, 0);
        waveFront.position = new Vector3(-2f + Mathf.Repeat(t * 0.7f, 2f), SurfaceY - 0.45f + Mathf.Sin(t * 1.3f) * 0.04f, 0);

        boat.position = new Vector3(cfg.boatPosition.x, SurfaceY - 0.15f + Mathf.Sin(t * 1.5f) * 0.07f, 0);
        boat.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f) * 3f);

        for (int i = 0; i < clouds.Length; i++)
        {
            Vector3 p = clouds[i].position;
            p.x = Mathf.Repeat(5 + i * 11 + t * (0.15f + i * 0.05f), cfg.width + 4f) - 2f;
            clouds[i].position = p;
        }
        for (int i = 0; i < kelps.Length; i++)
            kelps[i].rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.2f + i) * 8f);
        for (int i = 0; i < rays.Length; i++)
            rays[i].rotation = Quaternion.Euler(0, 0, 18f + Mathf.Sin(t * 0.3f + i * 1.7f) * 4f);
    }
}
