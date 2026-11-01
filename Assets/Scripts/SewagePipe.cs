using UnityEngine;

// Arıtılmamış atık su borusu. "FX" objesine ekle.
// pipeStartStage aşamasından itibaren açılır: pis su akıtır ve pipeInterval saniyede bir ağzının önüne
// pipeCells kare müsilaj bırakır. Dalgıç ağzın yanında E'yi basılı tutarsa boru bir süre tıkanır:
// "sorunun kaynağına git" mesajı.
public class SewagePipe : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;
    public FxManager fx;
    public PlayerController player;
    public Sprite sprite;
    public Material material;

    static readonly Color PipeColor = new Color32(0x3A, 0x3F, 0x44, 0xFF);
    static readonly Color SewageColor = new Color32(0x6B, 0x5B, 0x3A, 0xFF);
    static readonly Color PlugColor = new Color32(0xFF, 0x7A, 0x59, 0xFF);

    // Arayüz için
    public Vector2 Mouth { get; private set; }                           // Borunun ağzı (dünya konumu)
    public bool IsOpen => GameManager.I.Stage >= cfg.pipeStartStage;      // Aşaması geldi mi?
    public bool Plugged => plugTimer > 0f;
    public bool Flowing => IsOpen && !Plugged && !CrisisManager.PipeStopped;
    public bool PlayerNear => Vector2.Distance(player.transform.position, Mouth) <= cfg.pipePlugRange;
    public float PlugHold01 => plugHold / cfg.pipePlugHold;               // E basılı tutma ilerlemesi
    public float PlugRemaining => plugTimer;

    float pipeTimer;
    float dripTimer;
    float plugHold;
    float plugTimer;
    SpriteRenderer plug;

    void Start()
    {
        Mouth = new Vector2(1.1f, cfg.pipeRow + 0.5f);

        // Boru gövdesi + ağız bileziği: ekranın solundan içeri uzanan iki dikdörtgen
        MakeRect("Pipe", new Vector2(0.45f, Mouth.y), new Vector2(1.3f, 0.55f), PipeColor);
        MakeRect("PipeRim", new Vector2(1.05f, Mouth.y), new Vector2(0.2f, 0.75f), PipeColor * 0.8f);
        plug = MakeRect("Plug", new Vector2(1.2f, Mouth.y), new Vector2(0.15f, 0.6f), PlugColor);
        plug.enabled = false;
    }

    SpriteRenderer MakeRect(string name, Vector2 pos, Vector2 size, Color color)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform, false);
        sr.transform.position = pos;
        sr.transform.localScale = new Vector3(size.x, size.y, 1f);
        sr.sprite = sprite;
        if (material) sr.sharedMaterial = material;
        color.a = 1f;
        sr.color = color;
        sr.sortingOrder = 2; // müsilajın önünde
        return sr;
    }

    void Update()
    {
        UpdatePlug();
        plug.enabled = Plugged;
        if (!Flowing) return;

        // Sürekli akan pis su (sadece görsel); kriz haberinde daha yoğun
        dripTimer -= Time.deltaTime * CrisisManager.PipeRate;
        if (dripTimer <= 0f)
        {
            dripTimer = 0.08f;
            Vector2 vel = new Vector2(Random.Range(1.5f, 3f), Random.Range(-0.6f, 0.6f));
            fx.Spawn(Mouth, vel, SewageColor, Random.Range(0.12f, 0.22f));
        }

        // Belirli aralıklarla ağzın önüne müsilaj bırakır
        pipeTimer += Time.deltaTime * CrisisManager.PipeRate;
        if (pipeTimer >= cfg.pipeInterval)
        {
            pipeTimer = 0f;
            for (int i = 0; i < cfg.pipeCells; i++) Pollute();
        }
    }

    // E basılı tutulursa tıkanır; tıkalıyken süre geri sayar
    void UpdatePlug()
    {
        if (Plugged)
        {
            plugTimer -= Time.deltaTime;
            return;
        }

        bool holding = IsOpen && PlayerNear && player.CanControl && Input.GetKey(KeyCode.E);
        plugHold = holding ? plugHold + Time.deltaTime : 0f;
        if (plugHold >= cfg.pipePlugHold)
        {
            plugHold = 0f;
            plugTimer = cfg.pipePlugDuration;
            ScoreManager.I.Popup(Mouth + Vector2.up, $"Boru tıkandı! ({cfg.pipePlugDuration:0} sn)", true);
        }
    }

    // Ağza en yakın temiz kareyi müsilajlı yapar (önce tam önü, sonra çevresi)
    void Pollute()
    {
        int row = cfg.pipeRow;
        for (int dx = 1; dx <= 4; dx++)
        {
            foreach (int dy in new[] { 0, 1, -1 })
            {
                Vector2Int c = new Vector2Int(dx, row + dy);
                if (grid.InBounds(c) && !grid.IsInfected(c))
                {
                    grid.Infect(c);
                    return;
                }
            }
        }
    }
}
