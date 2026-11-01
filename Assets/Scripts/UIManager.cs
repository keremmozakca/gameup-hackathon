using System.Collections.Generic;
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
    static readonly Color BoxBg = new Color32(0x05, 0x1E, 0x2B, 0xD9);  // HUD kutularının içi
    static readonly Color Warm = new Color32(0xF2, 0xC1, 0x4E, 0xFF);   // sarı (ara renk)
    static readonly Color Green = new Color32(0x5F, 0xD0, 0x7A, 0xFF);

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
        "Dedem bu denizden ekmeğini çıkardı. Babam da. Şimdi ağlarımız boş, denizin üstü gri bir örtüyle kaplı.",
        "Su ısındıkça, kıyıdan arıtılmamış atıklar aktıkça örtü kalınlaşıyor. Altındaki her şey boğuluyor.",
        "Kimse beklemiyor artık. Ben bekleyemem. Bir nefes, bir dalış. Marmara nefes alana kadar.",
        "— Deniz, serbest dalgıç",
    };
    static float LineTime(string line) => Mathf.Max(3f, line.Length * 0.075f); // seslendirmeye yetecek süre

    // Giriş ekranı açıkken GameManager menü tuşlarını dinlemez
    public static bool IntroOpen { get; private set; }
    static bool ranksOpen;
    static bool scoresOpen;
    public static bool MenuBlocked => IntroOpen || ranksOpen || scoresOpen; // GameManager menüde SPACE'i dinlemesin
    static bool introDone; // Sahne yeniden yüklense de giriş bir kez gösterilir

    const string InfoText =
        "Müsilaj; ısınan deniz suyu, arıtılmamış atık sular ve durgun su birleştiğinde çoğalır.\n" +
        "Oyunda yeniden başlayabilirsin. Marmara'da bunun bir tuşu yok.";

    TMP_FontAsset font;
    Material shadowMat; // gölgeli yazı: piksel denizin üstünde okunur olsun
    Image thermoBulb;
    TMP_Text stageText;
    TMP_Text densityPct;
    bool introStarted;
    int introSpoken;
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
    GameObject pausePanel;
    GameObject scoresPanel;
    TMP_Text scoresList;
    TMP_Text diaryText;
    float diaryTimer;
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
        shadowMat = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Drop Shadow");
        cam = Camera.main;
        BuildCanvas();
        BuildHud();
        BuildMenu();
        BuildGameOver();
        BuildRanks();
        BuildPause();
        BuildScores();
        BuildIntro();
    }

    void Start()
    {
        GameManager.I.OnStateChanged += HandleState;
        ScoreManager.I.OnRankUp += ShowRankToast;
        ScoreManager.I.OnPopup += ShowPopup;
        CrisisManager.I.OnCrisisStarted += FlashCrisis;
        GameManager.I.OnStageChanged += ShowDiary;
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
        if (GameManager.I) GameManager.I.OnStageChanged -= ShowDiary;
    }

    void HandleState(GameManager.State s)
    {
        hud.SetActive(s == GameManager.State.Playing);
        menuPanel.SetActive(s == GameManager.State.Menu);
        gameOverPanel.SetActive(s == GameManager.State.GameOver);

        if (s == GameManager.State.Menu) FillMenuBest();
        ranksOpen = false;
        ranksPanel.SetActive(false);
        scoresOpen = false;
        scoresPanel.SetActive(false);
        if (s == GameManager.State.Playing) ShowDiary(0);
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
        pausePanel.SetActive(GameManager.I.Paused);
        if (!hud.activeSelf) return;

        // Nefes barı: dalgıcın üstünü takip eder. Overlay canvas'ta konum = ekran pikseli.
        breathBar.position = cam.WorldToScreenPoint(breath.transform.position + Vector3.up * 0.9f);
        float b = breath.Breath01;
        breathFill.fillAmount = b;
        // GDD: yeşil → sarı → kırmızı, son 3 saniyede yanıp söner
        Color bc = b > 0.5f ? Color.Lerp(Warm, Green, (b - 0.5f) * 2f) : Color.Lerp(Bad, Warm, b * 2f);
        if (breath.Breath < 3f && !breath.IsFainted && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f) bc = Color.white;
        breathFill.color = bc;
        breathBar.gameObject.SetActive(b < 0.999f || breath.IsFainted);
        breathLabel.text = breath.IsFainted ? "Bayıldın!" : breath.SurfaceBlocked ? "Yüzey kapalı!" : "";

        // Termometre
        GameManager gm = GameManager.I;
        float heat = Mathf.Clamp01((gm.Stage + gm.StageProgress01 + CrisisManager.TempBonus) / ThermoStages);
        thermoFill.fillAmount = heat;
        Color hc = heat < 0.5f ? Color.Lerp(Shallow, Warm, heat * 2f) : Color.Lerp(Warm, Bad, (heat - 0.5f) * 2f);
        thermoFill.color = hc;
        thermoBulb.color = hc;
        thermoText.text = $"{gm.Temperature:0}<size=60%> °C</size>";
        stageText.text = CrisisManager.TempBonus > 0f ? $"<color=#E84A3A>Aşama {gm.Stage + 1} · +{CrisisManager.TempBonus:0} °C</color>" : $"Aşama {gm.Stage + 1}";

        // Skor + kombo
        ScoreManager sm = ScoreManager.I;
        scoreText.text = sm.Score.ToString("N0");
        bool combo = sm.Combo > 1.01f;
        string mult = CrisisManager.ScoreMult > 1f ? $"  <color=#7CF09A>PUAN x{CrisisManager.ScoreMult:0}</color>" : "";
        comboText.text = (combo ? $"KOMBO x{sm.Combo:0.0}" : "") + mult;
        comboFill.transform.parent.gameObject.SetActive(combo);
        comboFill.fillAmount = sm.ComboTime01; // kombo süresi azalırken bar kısalır

        // Yoğunluk: kayıp sınırına göre dolar
        float danger = grid.Density / cfg.loseDensity;
        densityFill.fillAmount = Mathf.Clamp01(danger);
        densityFill.color = danger < 0.6f ? Mucilage : Color.Lerp(Warm, Bad, Mathf.InverseLerp(0.6f, 1f, danger));
        densityPct.text = $"%{grid.Density * 100f:0}";
        densityPct.color = grid.Density >= VignetteStart ? Bad : Color.white; // GDD: %70 üstünde kırmızı

        // Vignette: yoğunluk %70'ten kayıp sınırına giderken kenarlar müsilaj rengiyle kararır ve nabız atar
        float v = Mathf.InverseLerp(VignetteStart, cfg.loseDensity, grid.Density);
        Color vc = vignette.color;
        vc.a = v * (0.75f + 0.25f * Mathf.Sin(Time.time * 4f));
        vignette.color = vc;

        // Rütbe satırı: rütbeyi ve sıradakine kalan puanı hep gösterir
        rankLine.text = sm.IsMaxRank ? $"{sm.RankName}  ·  en yüksek rütbe" : $"{sm.RankName}  ·  sonraki rütbeye {sm.PointsToNextRank:N0}";

        UpdateCrisis();
        UpdatePopups();
        UpdateDiary();
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

    // Deniz'in günlüğü: her sıcaklık aşamasında hikâyeden bir satır
    static readonly string[] Diary =
    {
        "22 °C. Dedemin teknesiyle ilk dalışım. Ciğerlerim hazır, deniz değil.",
        "Dedem bu sıcaklığa 'yaz başı' derdi. Daha bahar bitmedi.",
        "Ağlar yine boş döndü. Çekirdekler derine kök salmış.",
        "Kıyıdaki borudan akan su kokuyor. Kimse görmüyor mu?",
        "Bir balık sürüsü gördüm. Temiz suyu hâlâ hatırlıyorlar.",
        "Nefesim kısalıyor. Deniz de benim gibi.",
        "Haberler hep kötü. Ama her temiz kare bir umut.",
        "Marmara hâlâ nefes alıyor. Ben de.",
    };

    void ShowDiary(int stage)
    {
        diaryText.text = $"<size=70%><color=#FF7A59>Deniz'in günlüğü · {GameManager.I.Temperature:0} °C</color></size>\n<i>{Diary[Mathf.Min(stage, Diary.Length - 1)]}</i>";
        diaryTimer = 7f;
    }

    void UpdateDiary()
    {
        if (diaryTimer <= 0f)
        {
            diaryText.gameObject.SetActive(false);
            return;
        }
        diaryTimer -= Time.deltaTime;
        diaryText.gameObject.SetActive(true);
        Color c = diaryText.color;
        c.a = Mathf.Clamp01(diaryTimer) * Mathf.Clamp01((7f - diaryTimer) * 2f);
        diaryText.color = c;
    }

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
        breathBar = MakeRect("BreathBar", h, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 16));
        MakeImage("Border", breathBar, new Color(0.02f, 0.05f, 0.08f, 0.9f));
        Image bin = MakeImage("Bg", breathBar, BarBg);
        Inset(bin.rectTransform, 3);
        breathFill = MakeImage("Fill", breathBar, Shallow);
        Inset(breathFill.rectTransform, 3);
        MakeFill(breathFill, Image.FillMethod.Horizontal);
        breathLabel = MakeText("Label", breathBar, "", 22, TextAnchor.LowerCenter, Color.white);
        breathLabel.rectTransform.anchoredPosition = new Vector2(0, 26);
        breathLabel.rectTransform.sizeDelta = new Vector2(300, 30);

        // Sol üst: termometre kutusu (cam tüp + hazne + çentikler + derece)
        RectTransform tb = MakeBox("ThermoBox", h, new Vector2(0, 1), new Vector2(24, -24), new Vector2(290, 200));
        RectTransform tube = MakeRect("Tube", tb, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -18), new Vector2(30, 128));
        tube.pivot = new Vector2(0, 1);
        MakeImage("Glass", tube, Color.white);
        Image tin = MakeImage("Inner", tube, new Color(0.02f, 0.08f, 0.12f, 1f));
        Inset(tin.rectTransform, 4);
        thermoFill = MakeImage("Fill", tube, Shallow);
        Inset(thermoFill.rectTransform, 6);
        MakeFill(thermoFill, Image.FillMethod.Vertical);
        RectTransform bulb = MakeRect("Bulb", tb, new Vector2(0, 1), new Vector2(0, 1), new Vector2(49, -160), new Vector2(54, 54));
        Image bulbGlass = bulb.gameObject.AddComponent<Image>();
        bulbGlass.sprite = PixelArt.Disc();
        bulbGlass.raycastTarget = false;
        RectTransform bulbIn = MakeRect("BulbFill", bulb, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
        thermoBulb = bulbIn.gameObject.AddComponent<Image>();
        thermoBulb.sprite = PixelArt.Disc();
        thermoBulb.raycastTarget = false;
        for (int i = 1; i < ThermoStages; i++) // her aşama için bir çentik
        {
            RectTransform tick = MakeRect("Tick", tube, new Vector2(1, 0), new Vector2(1, 0), new Vector2(4, 6 + i * (116f / ThermoStages)), new Vector2(10, 3));
            tick.pivot = new Vector2(0, 0.5f);
            MakeImage("Line", tick, new Color(1, 1, 1, 0.7f));
        }
        thermoText = MakeText("Temp", tb, "22 °C", 64, TextAnchor.UpperLeft, Color.white);
        SetRect(thermoText.rectTransform, new Vector2(0, 1), new Vector2(100, -14), new Vector2(190, 80));
        thermoText.fontStyle = FontStyles.Bold;
        TMP_Text thermoLabel = MakeText("TempLabel", tb, "DENİZ SUYU", 22, TextAnchor.UpperLeft, Mucilage);
        SetRect(thermoLabel.rectTransform, new Vector2(0, 1), new Vector2(102, -92), new Vector2(190, 30));
        stageText = MakeText("Stage", tb, "Aşama 1", 24, TextAnchor.UpperLeft, Accent);
        SetRect(stageText.rectTransform, new Vector2(0, 1), new Vector2(102, -124), new Vector2(190, 60));

        // Orta üst: skor kutusu
        MakeBox("ScoreBox", h, new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(460, 140));
        scoreText = MakeText("Score", h, "0", 64, TextAnchor.UpperCenter, Color.white);
        SetRect(scoreText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(500, 76));
        scoreText.fontStyle = FontStyles.Bold;
        comboText = MakeText("Combo", h, "", 30, TextAnchor.UpperCenter, Accent);
        SetRect(comboText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(500, 40));
        RectTransform comboBar = MakeRect("ComboBar", h, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -142), new Vector2(160, 6));
        MakeImage("Bg", comboBar, BarBg);
        comboFill = MakeImage("Fill", comboBar, Accent);
        MakeFill(comboFill, Image.FillMethod.Horizontal);

        // Sağ üst: yoğunluk kutusu (etiket, büyük yüzde, sınıra göre dolan bar ve %70 çizgisi)
        RectTransform db = MakeBox("DensityBox", h, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(330, 140));
        densityText = MakeText("DensityLabel", db, "MÜSİLAJ YOĞUNLUĞU", 22, TextAnchor.UpperLeft, Mucilage);
        SetRect(densityText.rectTransform, new Vector2(0, 1), new Vector2(20, -16), new Vector2(300, 30));
        densityPct = MakeText("DensityPct", db, "%0", 56, TextAnchor.UpperRight, Color.white);
        SetRect(densityPct.rectTransform, new Vector2(1, 1), new Vector2(-20, -8), new Vector2(200, 70));
        densityPct.fontStyle = FontStyles.Bold;
        RectTransform dens = MakeRect("Bar", db, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(-40, 22));
        dens.pivot = new Vector2(0.5f, 0);
        MakeImage("Bg", dens, BarBg);
        densityFill = MakeImage("Fill", dens, Mucilage);
        MakeFill(densityFill, Image.FillMethod.Horizontal);
        RectTransform warn = MakeRect("Warn", dens, new Vector2(0.7f / 0.85f, 0), new Vector2(0.7f / 0.85f, 1), Vector2.zero, new Vector2(3, 8));
        MakeImage("Line", warn, Bad);
        TMP_Text limit = MakeText("Limit", db, "sınır %85", 18, TextAnchor.LowerRight, new Color(1, 1, 1, 0.6f));
        SetRect(limit.rectTransform, new Vector2(1, 0), new Vector2(-20, 46), new Vector2(200, 24));

        // Rütbe bildirimi (ekran ortası, üstte)
        rankToast = MakeText("RankToast", h, "", 48, TextAnchor.MiddleCenter, new Color(Accent.r, Accent.g, Accent.b, 0f));
        SetRect(rankToast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1400, 220));

        // Skorun altında rütbe satırı
        rankLine = MakeText("RankLine", h, "", 24, TextAnchor.UpperCenter, new Color(1, 1, 1, 0.75f));
        SetRect(rankLine.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -162), new Vector2(800, 34));

        // Kriz haberi bandı (üst orta, rütbe satırının altında)
        RectTransform cb = MakeRect("Crisis", h, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(1100, 86));
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

        // Deniz'in günlüğü (alt orta)
        diaryText = MakeText("Diary", h, "", 30, TextAnchor.LowerCenter, Color.white);
        SetRect(diaryText.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1400, 90));
        diaryText.gameObject.SetActive(false);

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
        p.GetComponent<Image>().color = new Color(0.02f, 0.12f, 0.17f, 0.6f); // arkada piksel deniz görünsün
        menuPanel = p.gameObject;

        // Oyun logosu: piksel yazı başlık + dalgıç
        Sprite titleSprite = PixelArt.Title("title", new[] { "MARMARA'NIN", "NEFESİ" }, Color.white, Accent);
        RectTransform tr = MakeRect("Logo", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300), titleSprite.rect.size * 9f);
        Image titleImg = tr.gameObject.AddComponent<Image>();
        titleImg.sprite = titleSprite;
        titleImg.raycastTarget = false;
        RectTransform dr = MakeRect("Diver", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420, 230), new Vector2(16, 8) * 10f);
        Image diverImg = dr.gameObject.AddComponent<Image>();
        diverImg.sprite = PixelArt.Diver(0);
        diverImg.raycastTarget = false;

        TMP_Text tag = MakeText("Tagline", p, "Deniz nefes alamıyor, sen de alamıyorsun.", 38, TextAnchor.MiddleCenter, Mucilage);
        SetRect(tag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1600, 60));

        // Tema künyesi: COP31 (sol üst) ve SDG 14 (sağ üst)
        Texture2D logoTex = Resources.Load<Texture2D>("cop31_logo"); // Assets/Resources/cop31_logo.png varsa gösterilir
        float copX = 40f;
        if (logoTex)
        {
            RectTransform lr = MakeRect("Cop31Logo", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(150, 150));
            lr.pivot = new Vector2(0, 1);
            Image logo = lr.gameObject.AddComponent<Image>();
            logo.sprite = Sprite.Create(logoTex, new Rect(0, 0, logoTex.width, logoTex.height), new Vector2(0.5f, 0.5f));
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            copX = 210f;
        }
        TMP_Text cop = MakeText("Cop31", p, "<b>COP31</b>\n<size=75%>Antalya, Türkiye · 2026</size>", 32, TextAnchor.UpperLeft, Color.white);
        SetRect(cop.rectTransform, new Vector2(0, 1), new Vector2(copX, -40), new Vector2(500, 100));
        TMP_Text sdg = MakeText("Sdg", p, "<b>SDG 14 · Sudaki Yaşam</b>\n<size=75%>Okyanuslar ve Denizler</size>", 32, TextAnchor.UpperRight, Color.white);
        SetRect(sdg.rectTransform, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(600, 100));

        TMP_Text how = MakeText("HowTo", p,
            "WASD: Yüz      Sol tık (basılı): Müsilajı em      E (basılı): Atık su borusunu tıka\n" +
            "Yüzey müsilajla kaplıysa nefes alamazsın. Su altında kaldıkça kombo büyür.\n" +
            "Koyu çekirdekler müsilajı besler. Sıkışan balıkları kurtar, ama onları emme!\n" +
            "Dünyadan gelen haberlere kulak ver. Müsilaj %85'i kaplarsa oyun biter.",
            30, TextAnchor.MiddleCenter, Color.white);
        SetRect(how.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1500, 220));

        MakeButton(p, "Dal  (SPACE)", new Vector2(-540, -200), () => GameManager.I.StartGame(), 330f);
        MakeButton(p, "Rütbeler", new Vector2(-180, -200), () => { ranksOpen = true; ranksPanel.SetActive(true); }, 330f);
        MakeButton(p, "Skorlar", new Vector2(180, -200), OpenScores, 330f);
        MakeButton(p, "Hikâye", new Vector2(540, -200), ShowIntro, 330f);

        menuBest = MakeText("Best", p, "", 28, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f));
        SetRect(menuBest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -300), new Vector2(800, 40));
    }

    // Hikâyeyi menüden yeniden izle
    void ShowIntro()
    {
        introDone = false;
        IntroOpen = true;
        introTime = 0f;
        introStarted = true; // menüden açılınca zaten tıklandı, ses hemen başlayabilir
        introSpoken = 0;
        if (Narrator.HasRecording) Narrator.PlayRecording(gameObject);
        introPanel.SetActive(true);
    }

    // Skorlar ekranı: kayıtlı tüm oyunlar arasından en iyi 10'u (JSON geçmişinden)
    void OpenScores()
    {
        List<ScoreRecord> list = new List<ScoreRecord>(ScoreManager.I.History);
        list.Sort((a, b) => b.score.CompareTo(a.score));
        string text = "<color=#B8AE94><b>#<pos=7%>Skor<pos=24%>Rütbe<pos=52%>Süre<pos=64%>Balık<pos=76%>Tarih</b></color>\n";
        if (list.Count == 0) text += "\nHenüz kayıtlı oyun yok. İlk dalışını yap!";
        for (int i = 0; i < Mathf.Min(10, list.Count); i++)
        {
            ScoreRecord r = list[i];
            text += $"{i + 1}.<pos=7%>{r.score:N0}<pos=24%>{r.rank}<pos=52%>{r.seconds / 60}:{r.seconds % 60:00}<pos=64%>{r.fish}<pos=76%>{r.date}\n";
        }
        scoresList.text = text;
        scoresOpen = true;
        scoresPanel.SetActive(true);
    }

    void BuildScores()
    {
        RectTransform p = MakePanel("Scores");
        p.GetComponent<Image>().color = new Color(0.01f, 0.08f, 0.11f, 0.98f);
        scoresPanel = p.gameObject;

        TMP_Text title = MakeText("Title", p, "SKORLAR", 72, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 380), new Vector2(1200, 100));

        scoresList = MakeText("List", p, "", 32, TextAnchor.UpperLeft, Color.white);
        SetRect(scoresList.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1300, 620));
        scoresList.rectTransform.pivot = new Vector2(0.5f, 1);
        scoresList.alignment = TextAlignmentOptions.TopLeft;

        MakeButton(p, "Geri", new Vector2(0, -400), () => { scoresOpen = false; scoresPanel.SetActive(false); });
        scoresPanel.SetActive(false);
    }

    void BuildPause()
    {
        RectTransform p = MakePanel("Pause");
        p.GetComponent<Image>().color = new Color(0.01f, 0.06f, 0.09f, 0.75f);
        pausePanel = p.gameObject;
        TMP_Text t = MakeText("Text", p, "DURAKLATILDI\n<size=40%>Devam etmek için Esc</size>", 80, TextAnchor.MiddleCenter, Color.white);
        SetRect(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 300));
        pausePanel.SetActive(false);
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

    // Satırın başlama zamanı: önceki satırların seslendirme süreleri toplamı
    static float LineStart(int i)
    {
        float t = 0f;
        for (int k = 0; k < i; k++) t += LineTime(IntroLines[k]);
        return t;
    }

    void UpdateIntro()
    {
        float allShown = LineStart(IntroLines.Length - 1) + 1.5f;

        // Tarayıcı ilk tıklamadan önce ses çalmaya izin vermez: hikâye bir tıkla/tuşla başlar
        if (!introStarted)
        {
            introHint.text = "Hikâyeyi dinlemek için tıkla";
            Color c0 = introHint.color;
            c0.a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f);
            introHint.color = c0;
            if (!Input.anyKeyDown) return;
            introStarted = true;
            introTime = 0f;
            introSpoken = 0;
            if (Narrator.HasRecording) Narrator.PlayRecording(gameObject);
            return;
        }

        introTime += Time.unscaledDeltaTime; // menüde zaman durduğu için ölçeksiz zaman

        for (int i = 0; i < introTexts.Length; i++)
        {
            Color c = introTexts[i].color;
            c.a = Mathf.Clamp01(introTime - LineStart(i));
            introTexts[i].color = c;

            // Kayıt yoksa her satır belirirken boğuk sesle okunur (son satır imza, okunmaz)
            if (i == introSpoken && introTime >= LineStart(i))
            {
                if (!Narrator.HasRecording && i < IntroLines.Length - 1) Narrator.Speak(IntroLines[i]);
                introSpoken++;
            }
        }

        introHint.text = introTime < allShown ? "Atlamak için bir tuşa bas" : "Devam etmek için bir tuşa bas";
        Color hc = introHint.color;
        hc.a = (introTime < allShown ? 0.35f : 1f) * (0.6f + 0.4f * Mathf.Sin(introTime * 3f));
        introHint.color = hc;

        // Bir tuş/tık: hikâye bitmediyse önce tüm satırları göster, sonra menüye geç
        if (!Input.anyKeyDown) return;
        if (introTime < allShown)
        {
            introTime = allShown;
            introSpoken = IntroLines.Length;
            Narrator.Stop();
            return;
        }
        Narrator.Stop();
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

        MakeButton(p, "Yeniden dal  (R)", new Vector2(-230, -360), () => GameManager.I.Restart());
        MakeButton(p, "Ana menü  (M)", new Vector2(230, -360), () => GameManager.I.GoToMenu());
    }

    // ---------- Yardımcılar ----------

    // HUD kutusu: mercan çerçeve + koyu yarı saydam iç
    RectTransform MakeBox(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform box = MakeRect(name, parent, anchor, anchor, pos, size);
        box.pivot = anchor;
        MakeImage("Border", box, new Color(Accent.r, Accent.g, Accent.b, 0.85f));
        Image inner = MakeImage("Inner", box, BoxBg);
        Inset(inner.rectTransform, 3);
        return box;
    }

    // Ebeveyni kaplayan bir öğenin kenarlarından px kadar içeri çeker
    static void Inset(RectTransform rt, float px)
    {
        rt.offsetMin = new Vector2(px, px);
        rt.offsetMax = new Vector2(-px, -px);
    }

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
        if (shadowMat) t.fontSharedMaterial = shadowMat;
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

    void MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick, float width = 420f)
    {
        RectTransform rt = MakeRect("Button", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(width, 90));
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
