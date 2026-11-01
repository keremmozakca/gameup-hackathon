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

    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (coneLine)
        {
            coneLine.positionCount = ArcSegments + 3;
            coneLine.loop = false;
            coneLine.enabled = false;
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
    }

    // Menzil içindeki karelere bakar; merkezi koninin içindeyse hasar verir
    void SuckCells(Vector2 origin)
    {
        float range = cfg.suctionRange;
        float halfAngle = cfg.suctionAngle * 0.5f;
        float amount = cfg.suctionPower * Time.deltaTime;

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

    // Geçici koni görseli: merkez → sol kenar → yay → sağ kenar → merkez
    void DrawCone(Vector2 origin)
    {
        if (!coneLine) return;
        coneLine.enabled = IsSucking;
        if (!IsSucking) return;

        float range = cfg.suctionRange;
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
