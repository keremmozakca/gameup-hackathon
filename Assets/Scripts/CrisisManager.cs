using UnityEngine;

// Dünyadan kriz haberleri. GameManager objesine ekle.
// Belirli aralıklarla "SON DAKİKA" haberi gelir; her haberin süreli bir etkisi vardır.
// Kötü haberler oyunu zorlaştırır, iyi haberler nefes aldırır. Aşama ilerledikçe kötü haber sıklaşır.
// Diğer scriptler etkileri statik çarpanlardan okur (ör. CrisisManager.SpreadSpeed).
public class CrisisManager : MonoBehaviour
{
    public static CrisisManager I { get; private set; }

    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;

    // ---- Etkiler (haber yokken hepsi nötr) ----
    public static float SpreadSpeed { get; private set; } = 1f;  // Müsilaj yayılma hızı çarpanı
    public static float BreathDrain { get; private set; } = 1f;  // Nefes tüketim çarpanı
    public static float SuctionPower { get; private set; } = 1f; // Emme gücü çarpanı
    public static float PipeRate { get; private set; } = 1f;     // Boru akış hızı çarpanı
    public static bool PipeStopped { get; private set; }         // Boru tamamen durdu mu?
    public static float TempBonus { get; private set; }          // Termometreye eklenen geçici °C

    // ---- Arayüz için ----
    public bool Active => remaining > 0f;
    public string Headline { get; private set; }
    public string Effect { get; private set; }
    public bool IsGood { get; private set; }
    public float Remaining01 => duration > 0f ? remaining / duration : 0f;
    public event System.Action OnCrisisStarted;

    enum Kind { Tanker, HeatWave, Industry, Bloom, Storm, Volunteers, Treatment }
    static readonly Kind[] BadNews = { Kind.Tanker, Kind.HeatWave, Kind.Industry, Kind.Bloom };
    static readonly Kind[] GoodNews = { Kind.Storm, Kind.Volunteers, Kind.Treatment };

    float nextTimer;  // Sonraki habere kalan süre
    float remaining;  // Aktif haberin kalan süresi
    float duration;
    Kind last = (Kind)(-1);

    void Awake()
    {
        I = this;
        ResetEffects(); // statik değerler sahne yeniden yüklense de eski kalmasın
        nextTimer = cfg.firstCrisisTime;
    }

    void Update()
    {
        if (GameManager.I.CurrentState != GameManager.State.Playing) return;

        if (Active)
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                ResetEffects();
                nextTimer = Random.Range(cfg.crisisGapMin, cfg.crisisGapMax);
            }
            return;
        }

        nextTimer -= Time.deltaTime;
        if (nextTimer <= 0f) StartRandom();
    }

    void StartRandom()
    {
        float goodChance = Mathf.Max(0.1f, cfg.goodNewsChance - GameManager.I.Stage * cfg.goodNewsDropPerStage);
        Kind[] pool = Random.value < goodChance ? GoodNews : BadNews;

        Kind k = pool[Random.Range(0, pool.Length)];
        if (k == last) k = pool[(System.Array.IndexOf(pool, k) + 1) % pool.Length]; // aynı haber üst üste gelmesin
        last = k;
        Begin(k);
    }

    void Begin(Kind k)
    {
        ResetEffects();
        switch (k)
        {
            case Kind.Tanker:
                Set("Tanker kazası! Marmara'ya petrol sızdı", "Yüzeyin bir kısmı kapandı, nefesin 1.5 kat hızlı bitiyor", false, 25f);
                BreathDrain = 1.5f;
                CoverSurface(9);
                break;
            case Kind.HeatWave:
                Set("Sıcak hava dalgası: deniz suyu 2 °C ısındı", "Müsilaj 2 kat hızlı yayılıyor", false, 25f);
                SpreadSpeed = 2f;
                TempBonus = 2f;
                break;
            case Kind.Industry:
                Set("Sanayi atıkları arıtılmadan denize verildi", "Borudan 3 kat fazla atık akıyor, 2 yeni çekirdek", false, 25f);
                PipeRate = 3f;
                grid.SpawnCore();
                grid.SpawnCore();
                break;
            case Kind.Bloom:
                Set("Kıyıda müsilaj patlaması!", "Derinde 3 yeni çekirdek belirdi", false, 8f);
                for (int i = 0; i < 3; i++) grid.SpawnCore();
                break;
            case Kind.Storm:
                Set("Fırtına çıktı: akıntı denizi karıştırdı", "Müsilajın bir kısmı dağıldı", true, 8f);
                grid.ScatterMucilage(0.25f);
                break;
            case Kind.Volunteers:
                Set("Gönüllü dalgıçlar temizliğe katıldı", "Emme gücün 1.6 kat", true, 20f);
                SuctionPower = 1.6f;
                break;
            case Kind.Treatment:
                Set("Belediye yeni arıtma tesisini açtı", "Boru durdu, müsilaj yavaş yayılıyor", true, 30f);
                PipeStopped = true;
                SpreadSpeed = 0.6f;
                break;
        }
        OnCrisisStarted?.Invoke();
    }

    void Set(string headline, string effect, bool good, float dur)
    {
        Headline = headline;
        Effect = effect;
        IsGood = good;
        duration = remaining = dur;
    }

    // Petrol lekesi: yüzey satırında rastgele bir yerden başlayan bir şerit kapanır
    void CoverSurface(int length)
    {
        int start = Random.Range(0, grid.W - length);
        for (int x = start; x < start + length; x++)
        {
            grid.Infect(new Vector2Int(x, grid.H - 1));
            if (Random.value < 0.5f) grid.Infect(new Vector2Int(x, grid.H - 2));
        }
    }

    static void ResetEffects()
    {
        SpreadSpeed = BreathDrain = SuctionPower = PipeRate = 1f;
        PipeStopped = false;
        TempBonus = 0f;
    }
}
