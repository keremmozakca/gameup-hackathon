using UnityEngine;

// Skor, kombo, rütbe ve en iyi 5 skor. GameManager objesine ekle.
// Temizlenen kareler ve hayatta kalınan süre puan getirir.
// Kombo: yüzeye çıkmadan temizlenen her kare çarpanı artırır; nefes almak veya bayılmak sıfırlar.
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
    public int RankIndex { get; private set; }
    public string RankName => cfg.rankNames[RankIndex];
    public bool IsMaxRank => RankIndex >= cfg.rankThresholds.Length - 1;
    public int PointsToNextRank => IsMaxRank ? 0 : cfg.rankThresholds[RankIndex + 1] - Score;
    public int[] TopScores { get; private set; } = new int[TopCount];
    public bool IsNewBest { get; private set; }        // Oyun sonunda: bu skor 1. sıraya mı girdi?

    // Arayüz bunları dinleyecek
    public event System.Action<string> OnRankUp;       // yeni rütbe adı
    public event System.Action<int> OnPointsGained;    // tek seferde kazanılan puan (kare temizleme)

    float score; // Saniye puanı küsuratlı biriktiği için float tutuyoruz

    void Awake()
    {
        I = this;
        LoadTopScores();
    }

    void Start()
    {
        grid.OnCellCleared += HandleCellCleared;
        breath.OnBreathTaken += ResetCombo;
        breath.OnFainted += ResetCombo;
        GameManager.I.OnStateChanged += HandleStateChanged;
    }

    void OnDestroy()
    {
        if (grid) grid.OnCellCleared -= HandleCellCleared;
        if (breath)
        {
            breath.OnBreathTaken -= ResetCombo;
            breath.OnFainted -= ResetCombo;
        }
        if (GameManager.I) GameManager.I.OnStateChanged -= HandleStateChanged;
    }

    void Update()
    {
        if (GameManager.I.CurrentState != GameManager.State.Playing) return;
        AddScore(cfg.scorePerSecond * Time.deltaTime);
    }

    // Kare temizlendi: taban puan × derinlik çarpanı × kombo, sonra kombo artar
    void HandleCellCleared(Vector2Int cell, bool wasCore)
    {
        float points = wasCore ? cfg.scoreCore : cfg.scoreMucilage;
        if (cell.y < grid.H / 2) points *= cfg.deepScoreMultiplier;
        points *= Combo;

        AddScore(points);
        OnPointsGained?.Invoke(Mathf.RoundToInt(points));

        Combo = Mathf.Min(Combo + cfg.comboStep, cfg.comboMax);
    }

    void ResetCombo() => Combo = 1f;

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

    // Geçici test yazıları. Adım 9'da gerçek arayüzle değiştireceğiz.
    void OnGUI()
    {
        GUI.Label(new Rect(10, 70, 400, 25), $"Skor: {Score}  Kombo: x{Combo:0.0}  Rütbe: {RankName}");

        if (GameManager.I.CurrentState != GameManager.State.GameOver) return;

        string next = IsMaxRank ? "En yüksek rütbedesin!" : $"Sonraki rütbeye {PointsToNextRank} puan kaldı";
        string best = IsNewBest ? "  YENİ REKOR!" : "";
        string list = "";
        for (int i = 0; i < TopCount; i++) list += $"\n{i + 1}. {TopScores[i]}";

        GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 30, 300, 150),
            $"Skor: {Score}{best}\n{RankName} — {next}\n\nEn iyi skorlar:{list}");
    }
}
