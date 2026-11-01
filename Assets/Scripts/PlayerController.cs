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
    SuctionTool suction;
    float animTimer;
    int animFrame;
    bool facingLeft;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        suction = GetComponent<SuctionTool>();

        // Piksel art dalgıç (Deniz): düz renkli daire yerine kodla çizilmiş karakter
        normalColor = Color.white;
        if (sr)
        {
            sr.sprite = PixelArt.Diver(0);
            sr.color = normalColor;
        }
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

        if (!sr) return;

        // Müsilajın içindeyken dalgıç koyulaşır
        sr.color = InMucilage ? Color.Lerp(normalColor, Color.black, 0.4f) : normalColor;

        // Yüzme animasyonu: hareket ederken paletler çırpar
        bool moving = rb.linearVelocity.sqrMagnitude > 0.2f;
        animTimer += Time.deltaTime * (moving ? 1f : 0.3f);
        if (animTimer >= 0.18f)
        {
            animTimer = 0f;
            animFrame = 1 - animFrame;
            sr.sprite = PixelArt.Diver(animFrame);
        }

        // Yön: emerken nişan yönüne, yoksa yüzdüğü yöne bakar
        if (suction && suction.IsSucking) facingLeft = suction.AimDir.x < 0f;
        else if (Mathf.Abs(input.x) > 0.1f) facingLeft = input.x < 0f;
        sr.flipX = facingLeft;
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