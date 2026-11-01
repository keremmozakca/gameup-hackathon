using UnityEngine;

// Balıklar. "FX" objesine ekle.
// Deniz ne kadar temizse o kadar balık yüzer. Müsilaja giren balık yapışıp kalır:
// etrafını temizleyip kurtarırsan bonus puan, geç kalırsan boğulur.
// Emme konisi bir balığa fishSuckPenaltyTime'dan uzun değerse ceza gelir ve balık kaçar.
public class FishManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;
    public SuctionTool suction;
    [Tooltip("Gövde sprite'ı (daire)")]
    public Sprite bodySprite;
    [Tooltip("Kuyruk sprite'ı (kare)")]
    public Sprite tailSprite;
    public Material material;

    const int MaxFish = 10;
    const float SpawnInterval = 1.2f;
    static readonly Color FishColor = new Color32(0x7F, 0xD6, 0xE8, 0xFF); // sığ sudan biraz açık
    static readonly Color StuckColor = new Color32(0x8A, 0x86, 0x78, 0xFF); // müsilaja bulanmış

    enum FishState { Swim, Stuck, Flee }

    class Fish
    {
        public Transform t;
        public SpriteRenderer[] parts;
        public FishState state;
        public float speed;
        public float dir;        // +1 sağa, -1 sola
        public float baseY;
        public float phase;      // yukarı-aşağı salınım için
        public float suckTimer;  // emme konisinde geçen süre
        public float stuckTimer; // sıkışık geçen süre
        public bool Alive => t.gameObject.activeSelf;
    }

    Fish[] fish;
    float spawnTimer;

    void Awake()
    {
        fish = new Fish[MaxFish];
        for (int i = 0; i < MaxFish; i++) fish[i] = BuildFish();
    }

    // Balık = basık daire gövde + 45° döndürülmüş kare kuyruk
    Fish BuildFish()
    {
        GameObject root = new GameObject("Fish");
        root.transform.SetParent(transform, false);

        SpriteRenderer body = new GameObject("Body").AddComponent<SpriteRenderer>();
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.6f, 0.3f, 1f);
        body.sprite = bodySprite;

        SpriteRenderer tail = new GameObject("Tail").AddComponent<SpriteRenderer>();
        tail.transform.SetParent(root.transform, false);
        tail.transform.localPosition = new Vector3(-0.32f, 0f, 0f);
        tail.transform.localRotation = Quaternion.Euler(0, 0, 45f);
        tail.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
        tail.sprite = tailSprite;

        SpriteRenderer[] parts = { body, tail };
        foreach (SpriteRenderer sr in parts)
        {
            if (material) sr.sharedMaterial = material;
            sr.sortingOrder = 1; // sıkışınca müsilajın (0) önünde görünsün
        }

        root.SetActive(false);
        return new Fish { t = root.transform, parts = parts };
    }

    void Update()
    {
        // Hedef balık sayısı: temiz denizde çok, kayıp sınırına yaklaştıkça sıfıra iner
        float clean = 1f - Mathf.Clamp01(grid.Density / cfg.loseDensity);
        int target = Mathf.RoundToInt(MaxFish * clean * clean);

        int alive = 0;
        foreach (Fish f in fish) if (f.Alive) alive++;

        spawnTimer -= Time.deltaTime;
        if (alive < target && spawnTimer <= 0f)
        {
            spawnTimer = SpawnInterval;
            TrySpawn();
        }

        foreach (Fish f in fish)
        {
            if (!f.Alive) continue;
            if (f.state == FishState.Stuck) UpdateStuck(f);
            else Swim(f);
        }
    }

    void TrySpawn()
    {
        Fish f = System.Array.Find(fish, x => !x.Alive);
        if (f == null) return;

        // Rastgele bir kenardan, temiz bir satırda doğar
        f.dir = Random.value < 0.5f ? 1f : -1f;
        int row = Random.Range(1, grid.H - 2);
        int edgeX = f.dir > 0 ? 0 : grid.W - 1;
        if (grid.IsInfected(new Vector2Int(edgeX, row))) return;

        f.state = FishState.Swim;
        f.speed = Random.Range(1.2f, 2.5f);
        f.baseY = row + 0.5f;
        f.phase = Random.value * 10f;
        f.suckTimer = 0f;
        f.t.position = new Vector3(f.dir > 0 ? -0.5f : grid.W + 0.5f, f.baseY, 0f);
        f.t.localScale = new Vector3(f.dir, 1f, 1f);
        SetColor(f, FishColor);
        f.t.gameObject.SetActive(true);
    }

    void Swim(Fish f)
    {
        Vector3 p = f.t.position;
        float speed = f.state == FishState.Flee ? 6f : f.speed;
        p.x += f.dir * speed * Time.deltaTime;
        f.phase += Time.deltaTime * (f.state == FishState.Flee ? 10f : 3f);
        p.y = f.baseY + Mathf.Sin(f.phase) * 0.2f;
        f.t.position = p;

        if (p.x < -1f || p.x > grid.W + 1f)
        {
            f.t.gameObject.SetActive(false);
            return;
        }

        // Kaçan balık müsilaja takılmaz ve tekrar ceza vermez
        if (f.state == FishState.Flee) return;

        // Müsilaja girdi: yapışıp kalır
        if (grid.IsInfected(Cell(p)))
        {
            f.state = FishState.Stuck;
            f.stuckTimer = 0f;
            SetColor(f, StuckColor);
            return;
        }

        // Emme konisinde fazla kaldı: ceza ve kaçış
        if (suction.IsSucking && suction.InCone(p))
        {
            f.suckTimer += Time.deltaTime;
            if (f.suckTimer >= cfg.fishSuckPenaltyTime)
            {
                ScoreManager.I.AddBonus(-cfg.fishSuckPenalty, p, "Balığa dikkat!");
                Flee(f, p.x >= suction.transform.position.x ? 1f : -1f);
            }
        }
        else
        {
            f.suckTimer = 0f;
        }
    }

    void UpdateStuck(Fish f)
    {
        Vector3 p = f.t.position;

        // Bulunduğu kare temizlendi: kurtarıldı!
        if (!grid.IsInfected(Cell(p)))
        {
            ScoreManager.I.AddBonus(cfg.fishRescueScore, p, "Balık kurtarıldı!");
            ScoreManager.I.CountFishRescued();
            Flee(f, f.dir);
            return;
        }

        // Çırpınma: yerinde titrer ve giderek solar
        f.stuckTimer += Time.deltaTime;
        float k = f.stuckTimer / cfg.fishDrownTime;
        f.t.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 20f) * 15f * (1f - k));
        Color c = StuckColor;
        c.a = 1f - k * 0.7f;
        SetColor(f, c);

        if (f.stuckTimer >= cfg.fishDrownTime)
        {
            ScoreManager.I.Popup(p, "Bir balık boğuldu", false);
            f.t.rotation = Quaternion.identity;
            f.t.gameObject.SetActive(false);
        }
    }

    void Flee(Fish f, float dir)
    {
        f.state = FishState.Flee;
        f.dir = dir;
        f.baseY = f.t.position.y;
        f.t.rotation = Quaternion.identity;
        f.t.localScale = new Vector3(dir, 1f, 1f);
        SetColor(f, FishColor);
    }

    static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

    static void SetColor(Fish f, Color c)
    {
        foreach (SpriteRenderer sr in f.parts) sr.color = c;
    }
}
