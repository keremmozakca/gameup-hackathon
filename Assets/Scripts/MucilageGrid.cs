using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Müsilaj ızgarası. Bu scripti Tilemap objesinin üzerine ekle.
// Mantık: müsilaj bilgisi "hp" dizisinde tutulur, Tilemap sadece bu bilgiyi ekrana çizer.
[RequireComponent(typeof(Tilemap))]
public class MucilageGrid : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public Sprite squareSprite;

    [Header("Renkler")]
    public Color mucilageColor = new Color32(0xB8, 0xAE, 0x94, 0xFF); // gri-bej
    public Color coreColor = new Color32(0x4A, 0x45, 0x32, 0xFF);     // koyu zeytin-kahve

    public int W => cfg.width;
    public int H => cfg.height;
    public int InfectedCount { get; private set; }
    public float Density => (float)InfectedCount / (W * H);

    // Bir kare temizlenince haber verir (kare, çekirdek miydi?). Skor ve efektler bunu dinleyecek.
    public event System.Action<Vector2Int, bool> OnCellCleared;

    // O anki yayılma aralığı. Config'teki değerle başlar, GameManager her aşamada kısaltır.
    // (Config'i doğrudan değiştirmiyoruz: editörde asset kalıcı olarak bozulurdu.)
    public float SpreadInterval { get; set; }

    Tilemap tilemap;
    float[,] hp;       // Her karenin "canı". 0 = temiz, 0'dan büyük = müsilajlı
    bool[,] isCore;    // Bu kare çekirdek mi?
    Tile mucilageTile;
    Tile coreTile;
    float spreadTimer; // Bir sonraki yayılmaya kadar geçen süreyi sayar

    // Yayılma yönleri. "Yukarı" iki kez yazılı: 5 yönden 2'si yukarı, yani %40 ihtimalle yukarı yayılır.
    // Gerçekte de müsilaj yüzeyde birikir; bu, yüzeyin zamanla kapanmasını sağlayacak.
    static readonly Vector2Int[] dirs =
    {
        Vector2Int.up, Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        hp = new float[W, H];
        isCore = new bool[W, H];
        mucilageTile = MakeTile(mucilageColor);
        coreTile = MakeTile(coreColor);
        SpreadInterval = cfg.spreadInterval;
    }

    void Start()
    {
        Seed();
    }

    // Her karede çalışır: süre dolunca bir yayılma turu yapar
    void Update()
    {
        spreadTimer += Time.deltaTime;
        if (spreadTimer >= SpreadInterval)
        {
            spreadTimer = 0f;
            SpreadTick();
        }

        PulseCores();
    }

    // Çekirdekler nabız gibi atar: hafifçe büyüyüp küçülür ve renkleri açılıp koyulaşır
    void PulseCores()
    {
        float p = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
        float s = 1f + 0.15f * p;
        Matrix4x4 m = Matrix4x4.Scale(new Vector3(s, s, 1f));

        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                if (!isCore[x, y]) continue;
                Vector3Int c = new Vector3Int(x, y, 0);
                tilemap.SetTransformMatrix(c, m);
                tilemap.SetColor(c, CellColor(x, y, p));
            }
        }
    }

    // Karenin rengi: canı azaldıkça solar (emme geri bildirimi). pulse sadece çekirdekte kullanılır.
    Color CellColor(int x, int y, float pulse = 0f)
    {
        bool core = isCore[x, y];
        Color c = core ? Color.Lerp(coreColor, mucilageColor, 0.3f * pulse) : mucilageColor;
        float ratio = hp[x, y] / (core ? cfg.coreHp : cfg.mucilageHp);
        c.a = Mathf.Lerp(0.25f, 1f, ratio);
        return c;
    }

    Tile MakeTile(Color color)
    {
        Tile t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = squareSprite;
        t.color = color;
        t.flags = TileFlags.None; // Kare kare renk ve boyut değiştirebilmek için kilitleri kaldır
        return t;
    }

    // Başlangıç: üst 4 satıra rastgele müsilaj, derine çekirdekler
    void Seed()
    {
        int placed = 0;
        int tries = 0;
        while (placed < cfg.startMucilageCells && tries < 1000)
        {
            tries++;
            Vector2Int c = new Vector2Int(Random.Range(0, W), Random.Range(H - 4, H));
            if (!IsInfected(c))
            {
                Infect(c);
                placed++;
            }
        }

        for (int i = 0; i < cfg.startCores; i++)
        {
            SpawnCore();
        }
    }

    // Alt yarıda rastgele bir yere çekirdek koyar.
    // Sonraki adımlarda sıcaklık her arttığında bunu çağıracağız.
    public void SpawnCore()
    {
        for (int tries = 0; tries < 200; tries++)
        {
            Vector2Int c = new Vector2Int(Random.Range(1, W - 1), Random.Range(0, H / 2));
            if (!isCore[c.x, c.y])
            {
                Infect(c, true);
                return;
            }
        }
    }

    // Bir yayılma turu: her müsilajlı kare, şansı tutarsa rastgele bir komşusuna bulaşır
    void SpreadTick()
    {
        // Önce yeni bulaşacak kareleri listeye topluyoruz, döngü bitince uyguluyoruz.
        // Döngü içinde direkt bulaştırsaydık, yeni bulaşan kare aynı turda tekrar yayılır
        // ve müsilaj bir anda tüm ekranı kaplardı.
        List<Vector2Int> toInfect = new List<Vector2Int>();

        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                if (hp[x, y] <= 0f) continue; // temiz kare yayılmaz

                float chance = isCore[x, y] ? cfg.coreSpreadChance : cfg.spreadChance;
                if (Random.value > chance) continue; // şans tutmadı

                Vector2Int neighbor = new Vector2Int(x, y) + dirs[Random.Range(0, dirs.Length)];
                if (InBounds(neighbor) && !IsInfected(neighbor))
                {
                    toInfect.Add(neighbor);
                }
            }
        }

        foreach (Vector2Int c in toInfect)
        {
            Infect(c);
        }
    }

    // Bir kareyi müsilajlı (veya çekirdek) yapar: hem veriye yazar hem ekrana çizer
    public void Infect(Vector2Int c, bool core = false)
    {
        if (!InBounds(c)) return;
        if (IsInfected(c) && !core) return; // zaten müsilajlı, çekirdeğe de dönmüyorsa bir şey yapma

        if (!IsInfected(c)) InfectedCount++;
        hp[c.x, c.y] = core ? cfg.coreHp : cfg.mucilageHp;
        isCore[c.x, c.y] = core;
        tilemap.SetTile((Vector3Int)c, core ? coreTile : mucilageTile);
        tilemap.SetColor((Vector3Int)c, CellColor(c.x, c.y));
    }

    // Bir karenin canını azaltır. Can 0'a inerse kare temizlenir ve OnCellCleared haber verir.
    public void Damage(Vector2Int c, float amount)
    {
        if (!IsInfected(c)) return;

        hp[c.x, c.y] -= amount;
        if (hp[c.x, c.y] > 0f)
        {
            tilemap.SetColor((Vector3Int)c, CellColor(c.x, c.y));
            return;
        }

        bool wasCore = isCore[c.x, c.y];
        hp[c.x, c.y] = 0f;
        isCore[c.x, c.y] = false;
        InfectedCount--;
        tilemap.SetTile((Vector3Int)c, null);
        OnCellCleared?.Invoke(c, wasCore);
    }

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.x < W && c.y >= 0 && c.y < H;
    public bool IsInfected(Vector2Int c) => InBounds(c) && hp[c.x, c.y] > 0f;
    public bool IsCore(Vector2Int c) => InBounds(c) && isCore[c.x, c.y];
}