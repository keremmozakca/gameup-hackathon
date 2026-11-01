using UnityEngine;

// Emme cihazı. Player objesine ekle.
// Sol tık basılıyken fare yönünde bir koni açılır; konideki müsilaj karelerinin canı azalır.
public class SuctionTool : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;
    public PlayerController player;
    [Tooltip("Koninin geçici görseli (Player'ın alt objesindeki LineRenderer)")]
    public LineRenderer coneLine;

    public bool IsSucking { get; private set; }
    public Vector2 AimDir { get; private set; } = Vector2.right;

    const int ArcSegments = 12; // Koni yayının kaç parçayla çizileceği

    // Rütbe avantajları (Serbest Dalgıç menzil, Marmara Bekçisi güç) ve kriz haberleri (gönüllüler)
    public float Range => cfg.suctionRange + (Rank >= 2 ? cfg.perkRangeBonus : 0f);
    public float Power => cfg.suctionPower * (Rank >= 3 ? 1f + cfg.perkPowerBonus : 1f) * CrisisManager.SuctionPower;
    static int Rank => ScoreManager.I ? ScoreManager.I.RankIndex : 0;

    Camera cam;

    // Piksel art cihaz ve emilen kabarcıklar
    const int BubbleCount = 24;
    SpriteRenderer device;
    SpriteRenderer[] bubbles;
    float[] bubbleLife;
    float bubbleSpawn;

    void Awake()
    {
        cam = Camera.main;
        if (coneLine)
        {
            coneLine.positionCount = ArcSegments + 3;
            coneLine.loop = false;
            coneLine.enabled = false;
            coneLine.startColor = coneLine.endColor = new Color(1f, 1f, 1f, 0.15f); // koni sadece ince bir iz
        }

        Material mat = GetComponent<SpriteRenderer>() ? GetComponent<SpriteRenderer>().sharedMaterial : null;

        device = new GameObject("Device").AddComponent<SpriteRenderer>();
        device.transform.SetParent(transform, false);
        device.sprite = PixelArt.Device();
        device.sortingOrder = 3;
        if (mat) device.sharedMaterial = mat;

        // Kabarcıklar dünyada serbest dursun diye ayrı bir köke bağlı
        Transform root = new GameObject("SuctionBubbles").transform;
        bubbles = new SpriteRenderer[BubbleCount];
        bubbleLife = new float[BubbleCount];
        for (int i = 0; i < BubbleCount; i++)
        {
            bubbles[i] = new GameObject("Bubble").AddComponent<SpriteRenderer>();
            bubbles[i].transform.SetParent(root, false);
            bubbles[i].sprite = PixelArt.Bubble();
            bubbles[i].sortingOrder = 3;
            if (mat) bubbles[i].sharedMaterial = mat;
            bubbles[i].enabled = false;
        }
    }

    Vector2 Nozzle => (Vector2)transform.position + AimDir * 0.75f;

    // Cihaz elde, nişan yönüne döner; sola bakarken ters çevrilir
    void UpdateDevice()
    {
        float angle = Mathf.Atan2(AimDir.y, AimDir.x) * Mathf.Rad2Deg;
        device.transform.position = (Vector2)transform.position + AimDir * 0.25f;
        device.transform.rotation = Quaternion.Euler(0, 0, angle);
        device.flipY = AimDir.x < 0f;
    }

    // Emerken koninin içinden ağza doğru akan kabarcıklar
    void UpdateBubbles()
    {
        if (IsSucking)
        {
            bubbleSpawn -= Time.deltaTime;
            while (bubbleSpawn <= 0f)
            {
                bubbleSpawn += 0.03f;
                int i = System.Array.FindIndex(bubbleLife, l => l <= 0f);
                if (i < 0) break;
                float a = (Mathf.Atan2(AimDir.y, AimDir.x) * Mathf.Rad2Deg + Random.Range(-0.5f, 0.5f) * cfg.suctionAngle) * Mathf.Deg2Rad;
                bubbles[i].transform.position = (Vector2)transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range * Random.Range(0.5f, 1f);
                bubbleLife[i] = 1f;
                bubbles[i].enabled = true;
            }
        }

        Vector2 nozzle = Nozzle;
        for (int i = 0; i < BubbleCount; i++)
        {
            if (bubbleLife[i] <= 0f) continue;
            Transform t = bubbles[i].transform;
            t.position = Vector2.MoveTowards(t.position, nozzle, Time.deltaTime * 7f);
            bubbleLife[i] -= Time.deltaTime * 2.5f;
            if (bubbleLife[i] <= 0f || Vector2.Distance(t.position, nozzle) < 0.1f)
            {
                bubbleLife[i] = 0f;
                bubbles[i].enabled = false;
            }
        }
    }

    void Update()
    {
        bool canUse = player == null || player.CanControl;
        IsSucking = canUse && Input.GetMouseButton(0);

        // Fare yönünü bul
        Vector2 origin = transform.position;
        Vector2 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouse - origin;
        if (dir.sqrMagnitude > 0.0001f) AimDir = dir.normalized;

        if (IsSucking) SuckCells(origin);
        DrawCone(origin);
        UpdateDevice();
        UpdateBubbles();
    }

    // Menzil içindeki karelere bakar; merkezi koninin içindeyse hasar verir
    void SuckCells(Vector2 origin)
    {
        float range = Range;
        float halfAngle = cfg.suctionAngle * 0.5f;
        float amount = Power * Time.deltaTime;

        int minX = Mathf.FloorToInt(origin.x - range);
        int maxX = Mathf.FloorToInt(origin.x + range);
        int minY = Mathf.FloorToInt(origin.y - range);
        int maxY = Mathf.FloorToInt(origin.y + range);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int c = new Vector2Int(x, y);
                if (!grid.IsInfected(c)) continue;

                Vector2 toCell = new Vector2(x + 0.5f, y + 0.5f) - origin; // karenin merkezi
                float dist = toCell.magnitude;
                if (dist > range) continue;

                // Dalgıcın kendi karesi her zaman emilir (açı hesabı çok yakında saçmalar)
                if (dist > 0.75f && Vector2.Angle(AimDir, toCell) > halfAngle) continue;

                grid.Damage(c, amount);
            }
        }
    }

    // Bir nokta şu an emme konisinin içinde mi? (Balık cezası için)
    public bool InCone(Vector2 p)
    {
        Vector2 to = p - (Vector2)transform.position;
        return to.magnitude <= Range && Vector2.Angle(AimDir, to) <= cfg.suctionAngle * 0.5f;
    }

    // Geçici koni görseli: merkez → sol kenar → yay → sağ kenar → merkez
    void DrawCone(Vector2 origin)
    {
        if (!coneLine) return;
        coneLine.enabled = IsSucking;
        if (!IsSucking) return;

        float range = Range;
        float half = cfg.suctionAngle * 0.5f;
        float baseAngle = Mathf.Atan2(AimDir.y, AimDir.x) * Mathf.Rad2Deg;

        coneLine.SetPosition(0, origin);
        for (int i = 0; i <= ArcSegments; i++)
        {
            float a = (baseAngle - half + cfg.suctionAngle * i / ArcSegments) * Mathf.Deg2Rad;
            coneLine.SetPosition(i + 1, origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * range);
        }
        coneLine.SetPosition(ArcSegments + 2, origin);
    }
}
