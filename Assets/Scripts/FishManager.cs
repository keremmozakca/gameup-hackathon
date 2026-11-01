using UnityEngine;

// Süs balıkları. "FX" objesine ekle.
// Deniz ne kadar temizse o kadar balık yüzer; müsilaja çarpan balık geri döner.
// Oyuncuya "temizledikçe deniz canlanıyor" hissini verir.
public class FishManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;
    [Tooltip("Gövde sprite'ı (daire)")]
    public Sprite bodySprite;
    [Tooltip("Kuyruk sprite'ı (kare)")]
    public Sprite tailSprite;
    public Material material;

    const int MaxFish = 10;
    const float SpawnInterval = 1.2f;
    static readonly Color FishColor = new Color32(0x7F, 0xD6, 0xE8, 0xFF); // sığ sudan biraz açık

    class Fish
    {
        public Transform t;
        public float speed;
        public float dir;   // +1 sağa, -1 sola
        public float baseY;
        public float phase; // yukarı-aşağı salınım için
    }

    Fish[] fish;
    float spawnTimer;

    void Awake()
    {
        fish = new Fish[MaxFish];
        for (int i = 0; i < MaxFish; i++) fish[i] = new Fish { t = BuildFish() };
    }

    // Balık = basık daire gövde + 45° döndürülmüş kare kuyruk
    Transform BuildFish()
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

        foreach (SpriteRenderer sr in new[] { body, tail })
        {
            if (material) sr.sharedMaterial = material;
            sr.color = FishColor;
            sr.sortingOrder = -1; // müsilajın (0) arkasında kalsın
        }

        root.SetActive(false);
        return root.transform;
    }

    void Update()
    {
        // Hedef balık sayısı: temiz denizde çok, kayıp sınırına yaklaştıkça sıfıra iner
        float clean = 1f - Mathf.Clamp01(grid.Density / cfg.loseDensity);
        int target = Mathf.RoundToInt(MaxFish * clean * clean);

        int alive = 0;
        foreach (Fish f in fish) if (f.t.gameObject.activeSelf) alive++;

        spawnTimer -= Time.deltaTime;
        if (alive < target && spawnTimer <= 0f)
        {
            spawnTimer = SpawnInterval;
            TrySpawn();
        }

        foreach (Fish f in fish)
            if (f.t.gameObject.activeSelf) Swim(f);
    }

    void TrySpawn()
    {
        Fish f = System.Array.Find(fish, x => !x.t.gameObject.activeSelf);
        if (f == null) return;

        // Rastgele bir kenardan, temiz bir satırda doğar
        f.dir = Random.value < 0.5f ? 1f : -1f;
        int row = Random.Range(1, grid.H - 2);
        int edgeX = f.dir > 0 ? 0 : grid.W - 1;
        if (grid.IsInfected(new Vector2Int(edgeX, row))) return;

        f.speed = Random.Range(1.2f, 2.5f);
        f.baseY = row + 0.5f;
        f.phase = Random.value * 10f;
        f.t.position = new Vector3(f.dir > 0 ? -0.5f : grid.W + 0.5f, f.baseY, 0f);
        f.t.localScale = new Vector3(f.dir, 1f, 1f);
        f.t.gameObject.SetActive(true);
    }

    void Swim(Fish f)
    {
        Vector3 p = f.t.position;
        p.x += f.dir * f.speed * Time.deltaTime;
        f.phase += Time.deltaTime * 3f;
        p.y = f.baseY + Mathf.Sin(f.phase) * 0.2f;

        // Önündeki kare müsilajlıysa geri dön
        Vector2Int ahead = new Vector2Int(Mathf.FloorToInt(p.x + f.dir * 0.5f), Mathf.FloorToInt(p.y));
        if (grid.IsInfected(ahead))
        {
            f.dir = -f.dir;
            f.t.localScale = new Vector3(f.dir, 1f, 1f);
        }

        f.t.position = p;

        // Ekrandan çıktıysa veya müsilajın içinde kaldıysa kaybolur
        bool outside = p.x < -1f || p.x > grid.W + 1f;
        bool stuck = grid.IsInfected(new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y)));
        if (outside || stuck) f.t.gameObject.SetActive(false);
    }
}
