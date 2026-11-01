using UnityEngine;

// Skor, kombo, rütbe ve en iyi 5 skor. GameManager objesine ekle.
// Temizlenen kareler ve hayatta kalınan süre puan getirir.
// Kombo (dalış serisi): sadece su altında büyür. Yüzeye değmek (kapalı yüzey dahil), bayılmak
// veya comboTimeout saniye hiç kare temizlememek komboyu x1'e düşürür.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager I { get; private set; }

    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;
    public BreathSystem breath;

    const int TopCount = 5;

    public int Score => Mathf.FloorToInt(score);
    public float Combo { get; private set; } = 1f;
    public float ComboTime01 => Combo > 1f ? comboTimer / cfg.comboTimeout : 0f; // Kombonun düşmesine kalan süre (0–1)
    public int RankIndex { get; private set; }
    public string RankName => cfg.rankNames[RankIndex];
    public bool IsMaxRank => RankIndex >= cfg.rankThresholds.Length - 1;
    public int PointsToNextRank => IsMaxRank ? 0 : cfg.rankThresholds[RankIndex + 1] - Score;
    public int[] TopScores { get; private set; } = new int[TopCount];
    public bool IsNewBest { get; private set; }        // Oyun sonunda: bu skor 1. sıraya mı girdi?

    // Arayüz bunları dinleyecek
    public event System.Action<string> OnRankUp;       // yeni rütbe adı
    public event System.Action<int> OnPointsGained;    // tek seferde kazanılan puan (kare temizleme)

    float score;      // Saniye puanı küsuratlı biriktiği için float tutuyoruz
    float comboTimer; // Kombonun düşmesine kalan saniye
    PlayerController player;

    void Awake()
    {
        I = this;
        LoadTopScores();
    }

    void Start()
    {
        player = breath.GetComponent<PlayerController>();
        grid.OnCellCleared += HandleCellCleared;
        GameManager.I.OnStateChanged += HandleStateChanged;
    }

    void OnDestroy()
    {
        if (grid) grid.OnCellCleared -= HandleCellCleared;
        if (GameManager.I) GameManager.I.OnStateChanged -= HandleStateChanged;
    }

    void Update()
    {
        if (GameManager.I.CurrentState != GameManager.State.Playing) return;
        AddScore(cfg.scorePerSecond * Time.deltaTime);

        // Dalış bitti (yüzeye değdi veya bayıldı) ya da süre doldu → kombo düşer
        comboTimer -= Time.deltaTime;
        if (!IsDiving || comboTimer <= 0f) ResetCombo();
    }

    // Kombo sadece su altında ve baygın değilken geçerli
    bool IsDiving => !player.AtSurface && !breath.IsFainted;

    // Kare temizlendi: taban puan × derinlik çarpanı × kombo.
    // Su altındaysa sonra kombo artar ve süresi yenilenir; yüzeyden emerken kombo büyümez.
    void HandleCellCleared(Vector2Int cell, bool wasCore)
    {
        float points = wasCore ? cfg.scoreCore : cfg.scoreMucilage;
        if (cell.y < grid.H / 2) points *= cfg.deepScoreMultiplier;
        points *= Combo;

        AddScore(points);
        OnPointsGained?.Invoke(Mathf.RoundToInt(points));

        if (!IsDiving) return;
        Combo = Mathf.Min(Combo + (wasCore ? cfg.comboCoreStep : cfg.comboStep), cfg.comboMax);
        comboTimer = cfg.comboTimeout;
    }

    void ResetCombo()
    {
        Combo = 1f;
        comboTimer = 0f;
    }

    void AddScore(float amount)
    {
        score += amount;

        // Yeni rütbeye ulaştı mı? (Bir seferde birden fazla atlayabilir)
        while (!IsMaxRank && Score >= cfg.rankThresholds[RankIndex + 1])
        {
            RankIndex++;
            OnRankUp?.Invoke(RankName);
        }
    }

    void HandleStateChanged(GameManager.State s)
    {
        if (s == GameManager.State.GameOver) SaveScore();
    }

    // ---------- En iyi 5 skor (PlayerPrefs) ----------

    void LoadTopScores()
    {
        for (int i = 0; i < TopCount; i++) TopScores[i] = PlayerPrefs.GetInt("top" + i, 0);
    }

    // Skoru doğru sıraya yerleştirir, alttakileri bir aşağı kaydırır
    void SaveScore()
    {
        int s = Score;
        IsNewBest = s > TopScores[0];

        for (int i = 0; i < TopCount; i++)
        {
            if (s <= TopScores[i]) continue;
            for (int j = TopCount - 1; j > i; j--) TopScores[j] = TopScores[j - 1];
            TopScores[i] = s;
            break;
        }

        for (int i = 0; i < TopCount; i++) PlayerPrefs.SetInt("top" + i, TopScores[i]);
        PlayerPrefs.Save();
    }
}
