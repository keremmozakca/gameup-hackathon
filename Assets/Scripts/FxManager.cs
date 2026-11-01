using UnityEngine;

// Görsel efektler. Sahnede "FX" objesine ekle.
// Kare temizlenince küçük müsilaj parçacıkları saçılır; çekirdek patlayınca ekran hafifçe sarsılır.
// Parçacıklar ParticleSystem yerine basit sprite havuzuyla yapıldı: materyal/shader derdi yok, WebGL'de güvenli.
public class FxManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public MucilageGrid grid;
    [Tooltip("Parçacık sprite'ı (kare)")]
    public Sprite sprite;
    [Tooltip("Parçacık materyali (dalgıcın sprite materyali)")]
    public Material material;

    const int PoolSize = 200;
    const float Life = 0.55f;        // Bir parçacığın ömrü (sn)
    const float Drag = 4f;           // Parçacıkların sudaki yavaşlaması
    const float Rise = 1.5f;         // Hafif yukarı süzülme (kabarcık gibi)
    const float ShakeTime = 0.18f;
    const float ShakeAmount = 0.15f;

    class Bit
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float life;
        public float size;
    }

    Bit[] bits;
    int next; // Havuzda sıradaki parçacık (dolunca en eskisinin üstüne yazar)
    Transform camT;
    Vector3 camHome;
    float shakeTimer;

    void Awake()
    {
        bits = new Bit[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            GameObject go = new GameObject("Bit");
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (material) sr.sharedMaterial = material;
            sr.sortingOrder = 5;
            go.SetActive(false);
            bits[i] = new Bit { t = go.transform, sr = sr };
        }

        camT = Camera.main.transform;
        camHome = camT.position;
    }

    void Start() => grid.OnCellCleared += HandleCleared;

    void OnDestroy()
    {
        if (grid) grid.OnCellCleared -= HandleCleared;
    }

    void HandleCleared(Vector2Int cell, bool wasCore)
    {
        Vector2 center = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        Color color = wasCore ? grid.coreColor : grid.mucilageColor;
        int count = wasCore ? 16 : 6;
        float speed = wasCore ? 6f : 3.5f;
        float size = wasCore ? 0.3f : 0.2f;

        for (int i = 0; i < count; i++)
            Spawn(center, Random.insideUnitCircle * speed, color, size * Random.Range(0.6f, 1.2f));

        if (wasCore) shakeTimer = ShakeTime;
    }

    // Tek parçacık saçar. Atık su borusu gibi başka scriptler de kullanır.
    public void Spawn(Vector2 pos, Vector2 vel, Color color, float size)
    {
        Bit b = bits[next];
        next = (next + 1) % PoolSize;

        b.t.gameObject.SetActive(true);
        b.t.position = pos;
        b.vel = vel;
        b.life = Life;
        b.size = size;
        b.sr.color = color;
        b.t.localScale = Vector3.one * size;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        foreach (Bit b in bits)
        {
            if (b.life <= 0f) continue;

            b.life -= dt;
            if (b.life <= 0f)
            {
                b.t.gameObject.SetActive(false);
                continue;
            }

            b.vel *= 1f - Drag * dt;
            b.vel.y += Rise * dt;
            b.t.position += (Vector3)(b.vel * dt);

            float k = b.life / Life; // 1 → 0
            b.t.localScale = Vector3.one * b.size * k;
            Color c = b.sr.color;
            c.a = k;
            b.sr.color = c;
        }

        // Ekran sarsıntısı (zaman durunca da sıfırlanır)
        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            float a = ShakeAmount * Mathf.Clamp01(shakeTimer / ShakeTime);
            camT.position = camHome + (Vector3)(Random.insideUnitCircle * a);
        }
        else if (camT.position != camHome)
        {
            camT.position = camHome;
        }
    }
}
