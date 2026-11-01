using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Tüm arayüz. Sahnede boş bir "UI" objesine ekle; Canvas ve öğeler oyun başında kodla oluşturulur
// (tile'larda olduğu gibi: elle kurulum yok, sahne dosyası sade kalır).
// Yazılar TextMeshPro: her çözünürlükte keskin kalır (TMP Essential Resources içe aktarılmış olmalı).
// HUD: dalgıcın üstünde nefes barı, sol üst termometre, orta üst skor + kombo, sağ üst yoğunluk.
// Ekranlar: ana menü, rütbe bildirimi, oyun sonu + bilgi kartı.
public class UIManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;
    public BreathSystem breath;

    // Renk paleti (CLAUDE.md)
    static readonly Color Deep = new Color32(0x0B, 0x4F, 0x6C, 0xFF);
    static readonly Color Shallow = new Color32(0x20, 0xA4, 0xC4, 0xFF);
    static readonly Color Mucilage = new Color32(0xB8, 0xAE, 0x94, 0xFF);
    static readonly Color Accent = new Color32(0xFF, 0x7A, 0x59, 0xFF);
    static readonly Color PanelBg = new Color(0.02f, 0.12f, 0.17f, 0.92f);
    static readonly Color BarBg = new Color(0f, 0f, 0f, 0.5f);

    const float ThermoStages = 8f;    // Termometre kaç aşamada tamamen dolar (sadece görsel)
    const float RankToastTime = 4f;   // Rütbe bildiriminin ekranda kalma süresi
    const int PopupCount = 12;        // Aynı anda ekranda olabilecek uçan puan yazısı
    const float PopupTime = 1.4f;
    static readonly Color Good = new Color32(0x7C, 0xF0, 0x9A, 0xFF);   // iyi haber / bonus
    static readonly Color Bad = new Color32(0xE8, 0x4A, 0x3A, 0xFF);    // kötü haber / ceza
    const float VignetteStart = 0.7f;   // Yoğunluk bu orana gelince ekran kenarları kararmaya başlar

    // Giriş ekranı: oyun ilk açıldığında menüden önce sırayla beliren satırlar
    static readonly string[] IntroLines =
    {
        "2021 baharında Marmara Denizi'nin yüzeyi müsilajla kaplandı.",
        "Deniz salyası olarak da bilinen bu tabaka ışığı ve oksijeni keser; altındaki canlılar boğulur.",
        "Isınan su, arıtılmamış atık sular ve durgun deniz onu besler.",
        "Sen bir gönüllü dalgıçsın. Nefesin yettiğince dal ve temizle.",
    };
    const float IntroLineDelay = 1.6f; // Satırların arka arkaya belirme aralığı (sn)

    // Giriş ekranı açıkken GameManager menü tuşlarını dinlemez
    public static bool IntroOpen { get; private set; }
    static bool ranksOpen;
    public static bool MenuBlocked => IntroOpen || ranksOpen; // GameManager menüde SPACE'i dinlemesin
    static bool introDone; // Sahne yeniden yüklense de giriş bir kez gösterilir

    const string InfoText =
        "Müsilaj; ısınan deniz suyu, arıtılmamış atık sular ve durgun su birleştiğinde çoğalır.\n" +
        "Oyunda yeniden başlayabilirsin. Marmara'da bunun bir tuşu yok.";

    TMP_FontAsset font;
    Camera cam;
    RectTransform canvasRt;

    // HUD
    GameObject hud;
    RectTransform breathBar;
    Image breathFill;
    TMP_Text breathLabel;
    Image thermoFill;
    TMP_Text thermoText;
    TMP_Text scoreText;
    TMP_Text comboText;
    Image comboFill;
    Image densityFill;
    TMP_Text densityText;
    TMP_Text rankToast;
    Image vignette;
    TMP_Text rankLine;
    TMP_Text pipePrompt;
    GameObject crisisBox;
    Image crisisBg;
    Image crisisFill;
    TMP_Text crisisText;
    float crisisFlash;
    TMP_Text[] popups;
    float[] popupTimers;
    Vector3[] popupWorld;
    int popupNext;
    GameObject ranksPanel;
    SewagePipe pipe;
    float rankToastTimer;

    // Ekranlar
    GameObject menuPanel;
    GameObject gameOverPanel;
    TMP_Text gameOverStats;
    TMP_Text menuBest;
    bool gameOverPending;
    GameObject introPanel;
    TMP_Text[] introTexts;
    TMP_Text introHint;
    float introTime;

    void Awake()
    {
        font = TMP_Settings.defaultFontAsset;
        cam = Camera.main;
        BuildCanvas();
        BuildHud();
        BuildMenu();
        BuildGameOver();
        BuildRanks();
        BuildIntro();
    }

    void Start()
    {
        GameManager.I.OnStateChanged += HandleState;
        ScoreManager.I.OnRankUp += ShowRankToast;
        ScoreManager.I.OnPopup += ShowPopup;
        CrisisManager.I.OnCrisisStarted += FlashCrisis;
        pipe = FindFirstObjectByType<SewagePipe>();
        HandleState(GameManager.I.CurrentState);
    }

    void OnDestroy()
    {
        if (GameManager.I) GameManager.I.OnStateChanged -= HandleState;
        if (ScoreManager.I)
        {
            ScoreManager.I.OnRankUp -= ShowRankToast;
            ScoreManager.I.OnPopup -= ShowPopup;
        }
        if (CrisisManager.I) CrisisManager.I.OnCrisisStarted -= FlashCrisis;
    }

    void HandleState(GameManager.State s)
    {
        hud.SetActive(s == GameManager.State.Playing);
        menuPanel.SetActive(s == GameManager.State.Menu);
        gameOverPanel.SetActive(s == GameManager.State.GameOver);

        if (s == GameManager.State.Menu) FillMenuBest();
        ranksOpen = false;
        ranksPanel.SetActive(false);
        IntroOpen = s == GameManager.State.Menu && !introDone;
        introPanel.SetActive(IntroOpen);
        // ScoreManager da aynı olayda skoru kaydediyor; sıra belirsiz olduğu için doldurmayı bu karenin sonuna bırakıyoruz
        if (s == GameManager.State.GameOver) gameOverPending = true;
    }

    // LateUpdate, tüm Update'lerden sonra çalışır (zaman durmuşken de)
    void LateUpdate()
    {
        // Giriş ekranı LateUpdate'te kapanır: aynı tuşa basış GameManager'da oyunu da başlatmasın
        if (IntroOpen) UpdateIntro();

        if (!gameOverPending) return;
        gameOverPending = false;
        FillGameOver();
    }

    void Update()
    {
        if (!hud.activeSelf) return;

        // Nefes barı: dalgıcın üstünü takip eder. Overlay canvas'ta konum = ekran pikseli.
        breathBar.position = cam.WorldToScreenPoint(breath.transform.position + Vector3.up * 0.9f);
        float b = breath.Breath01;
        breathFill.fillAmount = b;
        breathFill.color = b < 0.3f ? Accent : Shallow;
        breathBar.gameObject.SetActive(b < 0.999f || breath.IsFainted);
        breathLabel.text = breath.IsFainted ? "Bayıldın!" : breath.SurfaceBlocked ? "Yüzey kapalı!" : "";

        // Termometre
        GameManager gm = GameManager.I;
        thermoFill.fillAmount = Mathf.Clamp01((gm.Stage + gm.StageProgress01) / ThermoStages);
        thermoFill.color = Color.Lerp(Shallow, Accent, thermoFill.fillAmount);
        thermoText.text = $"{gm.Temperature:0} °C";

        // Skor + kombo
        ScoreManager sm = ScoreManager.I;
        scoreText.text = sm.Score.ToString("N0");
        bool combo = sm.Combo > 1.01f;
        comboText.text = combo ? $"Kombo x{sm.Combo:0.0}" : "";
        comboFill.transform.parent.gameObject.SetActive(combo);
        comboFill.fillAmount = sm.ComboTime01; // kombo süresi azalırken bar kısalır

        // Yoğunluk: kayıp sınırına göre dolar
        float danger = grid.Density / cfg.loseDensity;
        densityFill.fillAmount = Mathf.Clamp01(danger);
        densityFill.color = Color.Lerp(Mucilage, Accent, Mathf.InverseLerp(0.6f, 1f, danger));
        densityText.text = $"Müsilaj %{grid.Density * 100f:0}";

        // Vignette: yoğunluk %70'ten kayıp sınırına giderken kenarlar müsilaj rengiyle kararır ve nabız atar
        float v = Mathf.InverseLerp(VignetteStart, cfg.loseDensity, grid.Density);
        Color vc = vignette.color;
        vc.a = v * (0.75f + 0.25f * Mathf.Sin(Time.time * 4f));
        vignette.color = vc;

        // Rütbe satırı: rütbeyi ve sıradakine kalan puanı hep gösterir
        rankLine.text = sm.IsMaxRank ? $"{sm.RankName}  ·  en yüksek rütbe" : $"{sm.RankName}  ·  sonraki rütbeye {sm.PointsToNextRank:N0}";

        UpdateCrisis();
        UpdatePopups();
        UpdatePipePrompt();

        // Rütbe bildirimi: yavaşça kaybolur
        if (rankToastTimer > 0f)
        {
            rankToastTimer -= Time.deltaTime;
            Color c = rankToast.color;
            c.a = Mathf.Clamp01(rankToastTimer / 0.5f);
            rankToast.color = c;
        }
    }

    void UpdateCrisis()
    {
        CrisisManager cm = CrisisManager.I;
        crisisBox.SetActive(cm.Active);
        if (!cm.Active) return;

        crisisText.text = $"<b>{(cm.IsGood ? "İYİ HABER" : "SON DAKİKA")}</b>  {cm.Headline}\n<size=75%>{cm.Effect}</size>";
        crisisFill.fillAmount = cm.Remaining01;
        Color c = cm.IsGood ? Good : Bad;
        crisisFill.color = c;
        crisisFlash = Mathf.Max(0f, crisisFlash - Time.deltaTime);
        crisisBg.color = Color.Lerp(new Color(0.02f, 0.1f, 0.14f, 0.85f), c, crisisFlash); // yeni haberde parlar
    }

    void FlashCrisis() => crisisFlash = 1f;

    void ShowPopup(Vector2 worldPos, string text, bool good)
    {
        int i = popupNext;
        popupNext = (popupNext + 1) % PopupCount;
        popups[i].text = text;
        popups[i].color = good ? Good : Bad;
        popupWorld[i] = worldPos;
        popupTimers[i] = PopupTime;
        popups[i].gameObject.SetActive(true);
    }

    // Uçan yazılar: dünyadaki noktadan yukarı süzülüp kaybolur
    void UpdatePopups()
    {
        for (int i = 0; i < PopupCount; i++)
        {
            if (popupTimers[i] <= 0f) continue;
            popupTimers[i] -= Time.deltaTime;
            if (popupTimers[i] <= 0f)
            {
                popups[i].gameObject.SetActive(false);
                continue;
            }
            popupWorld[i] += Vector3.up * Time.deltaTime * 0.8f;
            popups[i].rectTransform.position = cam.WorldToScreenPoint(popupWorld[i]);
            Color c = popups[i].color;
            c.a = Mathf.Clamp01(popupTimers[i] / 0.4f);
            popups[i].color = c;
        }
    }

    // Borunun üstünde: tıkama ipucu ve ilerlemesi
    void UpdatePipePrompt()
    {
        bool show = pipe && pipe.IsOpen;
        pipePrompt.gameObject.SetActive(show);
        if (!show) return;

        pipePrompt.rectTransform.position = cam.WorldToScreenPoint(pipe.Mouth + new Vector2(1.2f, 1.1f));
        if (pipe.Plugged) pipePrompt.text = $"<color=#7CF09A>Tıkalı: {pipe.PlugRemaining:0} sn</color>";
        else if (CrisisManager.PipeStopped) pipePrompt.text = "<color=#7CF09A>Arıtma tesisi devrede</color>";
        else if (pipe.PlayerNear) pipePrompt.text = pipe.PlugHold01 > 0f ? $"Tıkanıyor… %{pipe.PlugHold01 * 100f:0}" : "<b>E</b> basılı tut: boruyu tıka";
        else pipePrompt.text = "<size=80%>Atık su borusu</size>";
    }

    void ShowRankToast(string rank)
    {
        int idx = ScoreManager.I.RankIndex;
        rankToast.text = $"Yeni rütbe: {rank}\n<size=60%><color=#FFFFFF>Avantaj: {cfg.rankPerks[idx]}</color></size>\n" +
                         $"<size=45%><color=#E84A3A>Marmara karşılık verdi: +{cfg.coresOnRankUp} çekirdek</color></size>";
        rankToastTimer = RankToastTime;
    }

    void FillMenuBest()
    {
        int best = ScoreManager.I.TopScores[0];
        menuBest.text = best > 0 ? $"En iyi skor: {best:N0}" : "";
    }

    void FillGameOver()
    {
        ScoreManager sm = ScoreManager.I;
        string next = sm.IsMaxRank ? "En yüksek rütbedesin!" : $"Sonraki rütbeye {sm.PointsToNextRank:N0} puan kaldı";
        string list = "";
        for (int i = 0; i < sm.TopScores.Length; i++)
            if (sm.TopScores[i] > 0) list += $"{i + 1}.  {sm.TopScores[i]:N0}\n";

        gameOverStats.text =
            $"Skor: {sm.Score:N0}{(sm.IsNewBest ? "   — YENİ REKOR!" : "")}\n" +
            $"Rütbe: {sm.RankName}\n{next}\n" +
            $"Dayandığın süre: {GameManager.I.ElapsedTime:0} sn   En yüksek sıcaklık: {GameManager.I.MaxTemperature:0} °C   Kurtarılan balık: {sm.FishRescued}\n\n" +
            $"En iyi skorlar\n{list}";
    }

    // ---------- Kurulum ----------

    void BuildCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRt = go.GetComponent<RectTransform>();

        // Butonlar için EventSystem (yoksa)
        if (!FindFirstObjectByType<EventSystem>())
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    void BuildHud()
    {
        hud = MakeRect("HUD", canvasRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        RectTransform h = hud.GetComponent<RectTransform>();

        // Vignette en önce eklenir ki diğer HUD öğelerinin arkasında kalsın
        vignette = MakeImage("Vignette", h, new Color(0.18f, 0.16f, 0.1f, 0f));
        vignette.sprite = MakeVignetteSprite();

        // Nefes barı (dalgıcı takip eder; anchor önemli değil, konumu her karede veriyoruz)
        breathBar = MakeRect("BreathBar", h, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 12));
        MakeImage("Bg", breathBar, BarBg);
        breathFill = MakeImage("Fill", breathBar, Shallow);
        MakeFill(breathFill, Image.FillMethod.Horizontal);
        breathLabel = MakeText("Label", breathBar, "", 22, TextAnchor.LowerCenter, Color.white);
        breathLabel.rectTransform.anchoredPosition = new Vector2(0, 26);
        breathLabel.rectTransform.sizeDelta = new Vector2(300, 30);

        // Sol üst: termometre (dikey bar + derece)
        RectTransform thermo = MakeRect("Thermo", h, new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -40), new Vector2(26, 150));
        thermo.pivot = new Vector2(0.5f, 1);
        MakeImage("Bg", thermo, BarBg);
        thermoFill = MakeImage("Fill", thermo, Shallow);
        MakeFill(thermoFill, Image.FillMethod.Vertical);
        thermoText = MakeText("Temp", h, "22 °C", 34, TextAnchor.UpperLeft, Color.white);
        SetRect(thermoText.rectTransform, new Vector2(0, 1), new Vector2(80, -40), new Vector2(200, 50));
        TMP_Text thermoLabel = MakeText("TempLabel", h, "Deniz suyu", 20, TextAnchor.UpperLeft, new Color(1, 1, 1, 0.7f));
        SetRect(thermoLabel.rectTransform, new Vector2(0, 1), new Vector2(80, -80), new Vector2(200, 30));

        // Orta üst: skor + kombo
        scoreText = MakeText("Score", h, "0", 56, TextAnchor.UpperCenter, Color.white);
        SetRect(scoreText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -25), new Vector2(500, 70));
        comboText = MakeText("Combo", h, "", 30, TextAnchor.UpperCenter, Accent);
        SetRect(comboText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(500, 40));
        RectTransform comboBar = MakeRect("ComboBar", h, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -142), new Vector2(160, 6));
        MakeImage("Bg", comboBar, BarBg);
        comboFill = MakeImage("Fill", comboBar, Accent);
        MakeFill(comboFill, Image.FillMethod.Horizontal);

        // Sağ üst: yoğunluk barı + yüzde
        RectTransform dens = MakeRect("Density", h, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -55), new Vector2(260, 22));
        MakeImage("Bg", dens, BarBg);
        densityFill = MakeImage("Fill", dens, Mucilage);
        MakeFill(densityFill, Image.FillMethod.Horizontal);
        densityText = MakeText("DensityText", h, "Müsilaj %0", 28, TextAnchor.UpperRight, Color.white);
        SetRect(densityText.rectTransform, new Vector2(1, 1), new Vector2(-40, -75), new Vector2(400, 40));
        densityText.rectTransform.pivot = new Vector2(1, 1);

        // Rütbe bildirimi (ekran ortası, üstte)
        rankToast = MakeText("RankToast", h, "", 48, TextAnchor.MiddleCenter, new Color(Accent.r, Accent.g, Accent.b, 0f));
        SetRect(rankToast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1400, 220));

        // Skorun altında rütbe satırı
        rankLine = MakeText("RankLine", h, "", 24, TextAnchor.UpperCenter, new Color(1, 1, 1, 0.75f));
        SetRect(rankLine.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -155), new Vector2(800, 34));

        // Kriz haberi bandı (üst orta, rütbe satırının altında)
        RectTransform cb = MakeRect("Crisis", h, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -195), new Vector2(1100, 86));
        cb.pivot = new Vector2(0.5f, 1);
        crisisBox = cb.gameObject;
        crisisBg = MakeImage("Bg", cb, new Color(0.02f, 0.1f, 0.14f, 0.85f));
        crisisText = MakeText("Text", cb, "", 28, TextAnchor.MiddleCenter, Color.white);
        crisisText.rectTransform.anchorMin = Vector2.zero;
        crisisText.rectTransform.anchorMax = Vector2.one;
        crisisText.rectTransform.sizeDelta = new Vector2(-30, -12);
        RectTransform cbar = MakeRect("Timer", cb, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 6));
        cbar.pivot = new Vector2(0.5f, 0);
        crisisFill = MakeImage("Fill", cbar, Bad);
        MakeFill(crisisFill, Image.FillMethod.Horizontal);
        crisisBox.SetActive(false);

        // Boru ipucu (borunun üstünü takip eder)
        pipePrompt = MakeText("PipePrompt", h, "", 24, TextAnchor.MiddleCenter, Color.white);
        pipePrompt.rectTransform.sizeDelta = new Vector2(420, 40);

        // Uçan puan yazıları havuzu
        popups = new TMP_Text[PopupCount];
        popupTimers = new float[PopupCount];
        popupWorld = new Vector3[PopupCount];
        for (int i = 0; i < PopupCount; i++)
        {
            popups[i] = MakeText("Popup", h, "", 26, TextAnchor.MiddleCenter, Good);
            popups[i].rectTransform.sizeDelta = new Vector2(500, 40);
            popups[i].fontStyle = FontStyles.Bold;
            popups[i].gameObject.SetActive(false);
        }
        rankToast.fontStyle = FontStyles.Bold;
    }

    void BuildMenu()
    {
        RectTransform p = MakePanel("Menu");
        menuPanel = p.gameObject;

        TMP_Text title = MakeText("Title", p, "MARMARA'NIN NEFESİ", 96, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1600, 130));

        TMP_Text tag = MakeText("Tagline", p, "Deniz nefes alamıyor, sen de alamıyorsun.", 38, TextAnchor.MiddleCenter, Mucilage);
        SetRect(tag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1600, 60));

        TMP_Text how = MakeText("HowTo", p,
            "WASD: Yüz      Sol tık (basılı): Müsilajı em      E (basılı): Atık su borusunu tıka\n" +
            "Yüzey müsilajla kaplıysa nefes alamazsın. Su altında kaldıkça kombo büyür.\n" +
            "Koyu çekirdekler müsilajı besler. Sıkışan balıkları kurtar, ama onları emme!\n" +
            "Dünyadan gelen haberlere kulak ver. Müsilaj %85'i kaplarsa oyun biter.",
            30, TextAnchor.MiddleCenter, Color.white);
        SetRect(how.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1500, 220));

        MakeButton(p, "Dal  (SPACE)", new Vector2(-230, -170), () => GameManager.I.StartGame());
        MakeButton(p, "Rütbeler", new Vector2(230, -170), () => { ranksOpen = true; ranksPanel.SetActive(true); });

        menuBest = MakeText("Best", p, "", 28, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
        SetRect(menuBest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -270), new Vector2(800, 40));
    }

    // Rütbeler ekranı: her rütbenin puanı ve verdiği avantaj
    void BuildRanks()
    {
        RectTransform p = MakePanel("Ranks");
        p.GetComponent<Image>().color = new Color(0.01f, 0.08f, 0.11f, 0.98f);
        ranksPanel = p.gameObject;

        TMP_Text title = MakeText("Title", p, "RÜTBELER", 72, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 380), new Vector2(1200, 100));

        TMP_Text sub = MakeText("Sub", p,
            "Puan topladıkça rütben yükselir ve her rütbe kalıcı bir avantaj verir.\n" +
            "Ama Marmara da her yükselişte karşılık verir: yeni çekirdekler doğar.",
            28, TextAnchor.MiddleCenter, Mucilage);
        SetRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 290), new Vector2(1500, 90));

        for (int i = 0; i < cfg.rankNames.Length; i++)
        {
            RectTransform row = MakeRect("Rank" + i, p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 180 - i * 105), new Vector2(1300, 90));
            MakeImage("Bg", row, new Color(1, 1, 1, i % 2 == 0 ? 0.06f : 0.03f));

            TMP_Text name = MakeText("Name", row, $"<b>{cfg.rankNames[i]}</b>", 36, TextAnchor.MiddleLeft, i == 0 ? Color.white : Accent);
            SetRect(name.rectTransform, new Vector2(0, 0.5f), new Vector2(30, 14), new Vector2(480, 50));
            name.alignment = TextAlignmentOptions.Left;
            TMP_Text pts = MakeText("Points", row, $"{cfg.rankThresholds[i]:N0} puan", 24, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.6f));
            SetRect(pts.rectTransform, new Vector2(0, 0.5f), new Vector2(30, -24), new Vector2(480, 36));
            pts.alignment = TextAlignmentOptions.Left;
            TMP_Text perk = MakeText("Perk", row, cfg.rankPerks[i], 30, TextAnchor.MiddleLeft, Color.white);
            SetRect(perk.rectTransform, new Vector2(0, 0.5f), new Vector2(520, 0), new Vector2(760, 80));
            perk.alignment = TextAlignmentOptions.Left;
        }

        MakeButton(p, "Geri", new Vector2(0, -400), () => { ranksOpen = false; ranksPanel.SetActive(false); });
        ranksPanel.SetActive(false);
    }

    void UpdateIntro()
    {
        introTime += Time.unscaledDeltaTime; // menüde zaman durduğu için ölçeksiz zaman

        for (int i = 0; i < introTexts.Length; i++)
        {
            Color c = introTexts[i].color;
            c.a = Mathf.Clamp01(introTime - i * IntroLineDelay);
            introTexts[i].color = c;
        }

        float allShown = introTexts.Length * IntroLineDelay;
        Color hc = introHint.color;
        hc.a = Mathf.Clamp01(introTime - allShown) * (0.6f + 0.4f * Mathf.Sin(introTime * 3f));
        introHint.color = hc;

        // Bir tuş/tık: önce tüm satırları göster, ikincisinde menüye geç
        if (!Input.anyKeyDown) return;
        if (introTime < allShown)
        {
            introTime = allShown;
            return;
        }
        introDone = true;
        IntroOpen = false;
        introPanel.SetActive(false);
    }

    void BuildIntro()
    {
        RectTransform p = MakePanel("Intro");
        p.GetComponent<Image>().color = new Color(0.01f, 0.06f, 0.09f, 1f);
        introPanel = p.gameObject;

        introTexts = new TMP_Text[IntroLines.Length];
        for (int i = 0; i < IntroLines.Length; i++)
        {
            Color c = i == IntroLines.Length - 1 ? Accent : Color.white;
            c.a = 0f;
            introTexts[i] = MakeText("Line" + i, p, IntroLines[i], 38, TextAnchor.MiddleCenter, c);
            SetRect(introTexts[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 210 - i * 110), new Vector2(1500, 100));
        }

        introHint = MakeText("Hint", p, "Devam etmek için bir tuşa bas", 28, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0f));
        SetRect(introHint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -360), new Vector2(800, 50));
    }

    void BuildGameOver()
    {
        RectTransform p = MakePanel("GameOver");
        gameOverPanel = p.gameObject;

        TMP_Text title = MakeText("Title", p, "MARMARA NEFESSİZ KALDI", 80, TextAnchor.MiddleCenter, Accent);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 380), new Vector2(1600, 110));

        gameOverStats = MakeText("Stats", p, "", 30, TextAnchor.UpperCenter, Color.white);
        SetRect(gameOverStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1400, 420));
        gameOverStats.rectTransform.pivot = new Vector2(0.5f, 1);

        // Bilgi kartı
        RectTransform card = MakeRect("InfoCard", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -210), new Vector2(1300, 150));
        MakeImage("Bg", card, new Color(Mucilage.r, Mucilage.g, Mucilage.b, 0.18f));
        TMP_Text info = MakeText("Info", card, InfoText, 30, TextAnchor.MiddleCenter, Color.white);
        info.rectTransform.sizeDelta = new Vector2(-60, -20);

        MakeButton(p, "Yeniden dal  (R)", new Vector2(0, -360), () => GameManager.I.Restart());
    }

    // ---------- Yardımcılar ----------

    RectTransform MakePanel(string name)
    {
        RectTransform p = MakeRect(name, canvasRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image bg = p.gameObject.AddComponent<Image>();
        bg.color = PanelBg;
        return p;
    }

    RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x, anchor.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // Ebeveynin tamamını kaplayan düz renkli görsel
    Image MakeImage(string name, Transform parent, Color color)
    {
        RectTransform rt = MakeRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Ortası şeffaf, kenarlara doğru koyulaşan daire biçimli doku
    static Sprite MakeVignetteSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x, y) / (size - 1) * 2f - Vector2.one; // -1..1
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d.magnitude));
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // fillAmount'un çalışması için Image'ın "Filled" tipte ve bir sprite'ı olması gerekir
    static void MakeFill(Image img, Image.FillMethod method)
    {
        img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        img.type = Image.Type.Filled;
        img.fillMethod = method;
        img.fillOrigin = 0; // yatayda soldan, dikeyde alttan
    }

    TMP_Text MakeText(string name, Transform parent, string text, int size, TextAnchor align, Color color)
    {
        RectTransform rt = MakeRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60));
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = ToTmp(align);
        t.color = color;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    // Eski TextAnchor hizalamasını TMP karşılığına çevirir
    static TextAlignmentOptions ToTmp(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            default: return TextAlignmentOptions.Center;
        }
    }

    void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rt = MakeRect("Button", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(420, 90));
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = Accent;
        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        TMP_Text t = MakeText("Label", rt, label, 38, TextAnchor.MiddleCenter, Deep);
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.sizeDelta = Vector2.zero;
        t.fontStyle = FontStyles.Bold;
    }
}
