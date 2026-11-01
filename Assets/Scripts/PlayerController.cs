using UnityEngine;

// Dalgıç hareketi. Player objesine ekle.
// Rigidbody2D ayarları: Gravity Scale 0, Linear Damping 3, Interpolate, Freeze Rotation Z
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;

    // Diğer scriptlerin okuyacağı bilgiler
    public bool CanControl { get; set; } = true;              // Bayılınca false yapacağız
    public bool InMucilage { get; private set; }               // Şu an müsilajın içinde mi?
    public float SurfaceY => grid.H - 0.5f;                    // Çıkabileceği en yüksek nokta (13.5)
    public bool AtSurface => transform.position.y >= SurfaceY - 0.2f;

    // Dalgıcın bulunduğu kare. Grid (0,0)'da ve kareler 1 birim olduğu için
    // dünya konumunu aşağı yuvarlamak yeterli: (5.7, 3.2) → kare (5, 3)
    public Vector2Int CurrentCell => new Vector2Int(
        Mathf.FloorToInt(transform.position.x),
        Mathf.FloorToInt(transform.position.y));

    Rigidbody2D rb;
    SpriteRenderer sr;
    Color normalColor;
    Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr) normalColor = sr.color;
    }

    // Update: her ekran karesinde. Tuşları okumak için.
    void Update()
    {
        if (CanControl)
        {
            // WASD veya ok tuşları: -1, 0 veya 1 döner
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        }
        else
        {
            input = Vector2.zero;
        }

        InMucilage = grid.IsInfected(CurrentCell);

        // Geçici geri bildirim: müsilajın içindeyken dalgıç koyulaşır
        if (sr) sr.color = InMucilage ? Color.Lerp(normalColor, Color.black, 0.4f) : normalColor;
    }

    // FixedUpdate: sabit aralıklarla, fizik için. Kuvvet uygulamak burada yapılır.
    void FixedUpdate()
    {
        float mult = InMucilage ? cfg.mucilageSpeedMultiplier : 1f;
        rb.AddForce(input * cfg.swimForce * mult);

        // Ekranın dışına ve su yüzeyinin üstüne çıkmayı engelle
        Vector2 p = rb.position;
        Vector2 clamped = new Vector2(
            Mathf.Clamp(p.x, 0.5f, grid.W - 0.5f),
            Mathf.Clamp(p.y, 0.5f, SurfaceY));
        if (clamped != p) rb.position = clamped;
    }
}