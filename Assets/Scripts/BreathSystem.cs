using UnityEngine;

// Nefes sistemi. Player objesine ekle.
// Su altında nefes azalır, müsilajda daha hızlı. Yüzeyde dolar ama sadece üstündeki yüzey karesi temizse.
// Nefes biterse dalgıç bayılır: tekneye ışınlanır, bir süre kontrol edilemez.
[RequireComponent(typeof(PlayerController))]
public class BreathSystem : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;

    public float Breath { get; private set; }                 // Kalan nefes (saniye)
    public float Breath01 => Breath / BreathMax;              // 0–1 arası, nefes barı için

    // Rütbe avantajları: Kıyı Gönüllüsü ek nefes, Denizin Nefesi hızlı dolum
    public float BreathMax => cfg.breathMax + (Rank >= 1 ? cfg.perkBreathBonus : 0f);
    float RefillTime => cfg.breathRefillTime / (Rank >= 4 ? cfg.perkRefillMultiplier : 1f);
    static int Rank => ScoreManager.I ? ScoreManager.I.RankIndex : 0;
    public bool IsFainted { get; private set; }
    public bool IsRefilling { get; private set; }              // Şu an yüzeyde nefes alıyor mu?
    public bool SurfaceBlocked { get; private set; }           // Yüzeyde ama üstü müsilajla kapalı mı?

    // Skor sistemi komboyu sıfırlamak için bunları dinleyecek
    public event System.Action OnBreathTaken;  // Yüzeyde nefes almaya başladı
    public event System.Action OnFainted;      // Nefes bitti, bayıldı

    PlayerController player;
    Rigidbody2D rb;
    float faintTimer;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        Breath = cfg.breathMax;
    }

    void Update()
    {
        if (IsFainted)
        {
            faintTimer -= Time.deltaTime;
            if (faintTimer <= 0f) WakeUp();
            return;
        }

        // Yüzeydeki kare: dalgıcın sütununda en üst satır (y = H-1)
        Vector2Int surfaceCell = new Vector2Int(player.CurrentCell.x, grid.H - 1);
        SurfaceBlocked = player.AtSurface && grid.IsInfected(surfaceCell);
        bool refilling = player.AtSurface && !SurfaceBlocked;

        if (refilling && !IsRefilling) OnBreathTaken?.Invoke();
        IsRefilling = refilling;

        if (refilling)
        {
            Breath += BreathMax / RefillTime * Time.deltaTime;
        }
        else
        {
            float mult = player.InMucilage ? cfg.mucilageBreathMultiplier : 1f;
            Breath -= mult * CrisisManager.BreathDrain * Time.deltaTime; // petrol haberi nefesi hızlı bitirir
        }
        Breath = Mathf.Clamp(Breath, 0f, BreathMax);

        if (Breath <= 0f) Faint();
    }

    void Faint()
    {
        IsFainted = true;
        IsRefilling = false;
        faintTimer = cfg.faintDuration;
        player.CanControl = false;

        // Tekneye ışınla ve dur
        rb.position = cfg.boatPosition;
        transform.position = cfg.boatPosition;
        rb.linearVelocity = Vector2.zero;

        OnFainted?.Invoke();
    }

    void WakeUp()
    {
        IsFainted = false;
        Breath = BreathMax;
        player.CanControl = true;
    }
}
