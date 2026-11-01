using UnityEngine;
using UnityEngine.SceneManagement;

// Oyun akışı: Menü → Oyun → Bitti. Sahnede boş bir "GameManager" objesine ekle.
// Oyun sırasında her aşamada deniz ısınır: müsilaj daha sık yayılır ve yeni çekirdek doğar.
// Menüde ve oyun sonunda zaman durdurulur (Time.timeScale = 0); böylece yayılma, nefes ve fizik kendiliğinden durur.
public class GameManager : MonoBehaviour
{
    public enum State { Menu, Playing, GameOver }

    public static GameManager I { get; private set; } // Diğer scriptler buradan ulaşır

    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;

    public State CurrentState { get; private set; }
    public bool Paused { get; private set; }           // Esc ile duraklatma (sadece oyun sırasında)
    public int Stage { get; private set; }             // 0'dan başlar, her aşamada +1
    public float Temperature => cfg.startTemperature + Stage * cfg.temperaturePerStage + CrisisManager.TempBonus;
    public float MaxTemperature { get; private set; }  // Oyun sonu ekranı için
    public int MinCores => cfg.minCores + Stage;       // Denizde hep en az bu kadar çekirdek olur
    public float ElapsedTime { get; private set; }     // Oyunda geçen süre (hayatta kalma skoru için)
    public float StageProgress01 => stageTimer / cfg.stageDuration;

    // Arayüz ve skor bunları dinleyecek
    public event System.Action<State> OnStateChanged;
    public event System.Action<int> OnStageChanged;    // yeni aşama numarası

    // "Yeniden başla" sahneyi baştan yükler. Bu bayrak sayesinde menüyü atlayıp direkt oyuna gireriz.
    static bool skipMenu;

    float stageTimer;
    float coreRespawnTimer;

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        // Rütbe yükselince Marmara karşılık verir: yeni çekirdekler doğar
        ScoreManager.I.OnRankUp += _ => { for (int i = 0; i < cfg.coresOnRankUp; i++) grid.SpawnCore(); };

        if (skipMenu)
        {
            skipMenu = false;
            StartGame();
        }
        else
        {
            SetState(State.Menu);
        }
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Menu:
                if (UIManager.MenuBlocked) break; // giriş veya rütbeler ekranı açıkken başlama
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) StartGame();
                break;

            case State.Playing:
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Paused = !Paused;
                    Time.timeScale = Paused ? 0f : 1f;
                }
                if (Paused) break;
                ElapsedTime += Time.deltaTime;
                stageTimer += Time.deltaTime;
                if (stageTimer >= cfg.stageDuration)
                {
                    stageTimer -= cfg.stageDuration;
                    NextStage();
                }
                MaxTemperature = Mathf.Max(MaxTemperature, Temperature);
                KeepCoresAlive();
                if (grid.Density >= cfg.loseDensity) SetState(State.GameOver);
                break;

            case State.GameOver:
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space)) Restart();
                if (Input.GetKeyDown(KeyCode.M)) GoToMenu();
                break;
        }
    }

    public void StartGame()
    {
        ElapsedTime = 0f;
        stageTimer = 0f;
        Stage = 0;
        MaxTemperature = Temperature;
        SetState(State.Playing);
    }

    // Bitmeyen baskı: oyuncu çekirdekleri temizledikçe, alt sınırın altına düşünce kısa süre sonra yenisi doğar.
    // Deniz hiçbir zaman "bitmez"; sadece ne kadar dayandığın önemlidir.
    void KeepCoresAlive()
    {
        if (grid.CoreCount >= MinCores)
        {
            coreRespawnTimer = 0f;
            return;
        }
        coreRespawnTimer += Time.deltaTime;
        if (coreRespawnTimer >= cfg.coreRespawnDelay)
        {
            coreRespawnTimer = 0f;
            grid.SpawnCore();
        }
    }

    // Deniz ısınır: yayılma sıklaşır, yeni çekirdek(ler) doğar
    void NextStage()
    {
        Stage++;
        grid.SpreadInterval *= cfg.stageSpreadMultiplier;
        for (int i = 0; i < cfg.coresPerStage; i++) grid.SpawnCore();
        OnStageChanged?.Invoke(Stage);
    }

    // Ana menüye dön: sahne tamamen baştan yüklenir
    public void GoToMenu()
    {
        skipMenu = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Restart()
    {
        skipMenu = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void SetState(State s)
    {
        CurrentState = s;
        Paused = false;
        Time.timeScale = s == State.Playing ? 1f : 0f;
        OnStateChanged?.Invoke(s);
    }
}
