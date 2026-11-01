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
    public int Stage { get; private set; }             // 0'dan başlar, her aşamada +1
    public float Temperature => cfg.startTemperature + Stage * cfg.temperaturePerStage;
    public float ElapsedTime { get; private set; }     // Oyunda geçen süre (hayatta kalma skoru için)
    public float StageProgress01 => stageTimer / cfg.stageDuration;

    // Arayüz ve skor bunları dinleyecek
    public event System.Action<State> OnStateChanged;
    public event System.Action<int> OnStageChanged;    // yeni aşama numarası

    // "Yeniden başla" sahneyi baştan yükler. Bu bayrak sayesinde menüyü atlayıp direkt oyuna gireriz.
    static bool skipMenu;

    float stageTimer;

    void Awake()
    {
        I = this;
    }

    void Start()
    {
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
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) StartGame();
                break;

            case State.Playing:
                ElapsedTime += Time.deltaTime;
                stageTimer += Time.deltaTime;
                if (stageTimer >= cfg.stageDuration)
                {
                    stageTimer -= cfg.stageDuration;
                    NextStage();
                }
                if (grid.Density >= cfg.loseDensity) SetState(State.GameOver);
                break;

            case State.GameOver:
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space)) Restart();
                break;
        }
    }

    public void StartGame()
    {
        ElapsedTime = 0f;
        stageTimer = 0f;
        Stage = 0;
        SetState(State.Playing);
    }

    // Deniz ısınır: yayılma sıklaşır, yeni çekirdek(ler) doğar
    void NextStage()
    {
        Stage++;
        grid.SpreadInterval *= cfg.stageSpreadMultiplier;
        for (int i = 0; i < cfg.coresPerStage; i++) grid.SpawnCore();
        OnStageChanged?.Invoke(Stage);
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
        Time.timeScale = s == State.Playing ? 1f : 0f;
        OnStateChanged?.Invoke(s);
    }

    // Geçici test yazıları. Adım 9'da gerçek arayüzle değiştireceğiz.
    void OnGUI()
    {
        GUI.Label(new Rect(10, 50, 400, 25), $"Sıcaklık: {Temperature:0} °C  (aşama {Stage})  Süre: {ElapsedTime:0} sn");

        Rect center = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 20, 300, 40);
        if (CurrentState == State.Menu)
            GUI.Box(center, "MARMARA'NIN NEFESİ\nBaşlamak için SPACE");
        else if (CurrentState == State.GameOver)
            GUI.Box(center, $"MARMARA NEFESSİZ KALDI\nYeniden başlamak için R");
    }
}
