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
    const float RankToastTime = 2.5f; // Rütbe bildiriminin ekranda kalma süresi

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
    float rankToastTimer;

    // Ekranlar
    GameObject menuPanel;
    GameObject gameOverPanel;
    TMP_Text gameOverStats;
    TMP_Text menuBest;
    bool gameOverPending;

    void Awake()
    {
        font = TMP_Settings.defaultFontAsset;
        cam = Camera.main;
        BuildCanvas();
        BuildHud();
        BuildMenu();
        BuildGameOver();
    }

    void Start()
    {
        GameManager.I.OnStateChanged += HandleState;
        ScoreManager.I.OnRankUp += ShowRankToast;
        HandleState(GameManager.I.CurrentState);
    }

    void OnDestroy()
    {
        if (GameManager.I) GameManager.I.OnStateChanged -= HandleState;
        if (ScoreManager.I) ScoreManager.I.OnRankUp -= ShowRankToast;
    }

    void HandleState(GameManager.State s)
    {
        hud.SetActive(s == GameManager.State.Playing);
        menuPanel.SetActive(s == GameManager.State.Menu);
        gameOverPanel.SetActive(s == GameManager.State.GameOver);

        if (s == GameManager.State.Menu) FillMenuBest();
        // ScoreManager da aynı olayda skoru kaydediyor; sıra belirsiz olduğu için doldurmayı bu karenin sonuna bırakıyoruz
        if (s == GameManager.State.GameOver) gameOverPending = true;
    }

    // LateUpdate, tüm Update'lerden sonra çalışır (zaman durmuşken de)
    void LateUpdate()
    {
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

        // Rütbe bildirimi: yavaşça kaybolur
        if (rankToastTimer > 0f)
        {
            rankToastTimer -= Time.deltaTime;
            Color c = rankToast.color;
            c.a = Mathf.Clamp01(rankToastTimer / 0.5f);
            rankToast.color = c;
        }
    }

    void ShowRankToast(string rank)
    {
        rankToast.text = $"Yeni rütbe: {rank}";
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
            $"Dayandığın süre: {GameManager.I.ElapsedTime:0} sn   Ulaşılan sıcaklık: {GameManager.I.Temperature:0} °C\n\n" +
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
        SetRect(rankToast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(1200, 80));
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
            "WASD: Yüz      Sol tık (basılı): Müsilajı em\n" +
            "Nefesin azalınca yüzeye çık — ama yüzey müsilajla kaplıysa nefes alamazsın.\n" +
            "Derindeki koyu çekirdekler müsilajı besler: önce onları temizle.\n" +
            "Müsilaj denizin %85'ini kaplarsa oyun biter.",
            30, TextAnchor.MiddleCenter, Color.white);
        SetRect(how.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1500, 220));

        MakeButton(p, "Dal  (SPACE)", new Vector2(0, -170), () => GameManager.I.StartGame());

        menuBest = MakeText("Best", p, "", 28, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
        SetRect(menuBest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(800, 40));
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
