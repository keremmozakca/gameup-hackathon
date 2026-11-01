using UnityEngine;

// Oyunun tüm ayar sayıları burada durur. Her yeni mekanikte yeni değişkenler ekliyoruz.
[CreateAssetMenu(fileName = "GameConfig", menuName = "Marmara/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Izgara")]
    [Tooltip("Deniz kaç kare genişlikte (kameraya göre 32)")]
    public int width = 32;

    [Tooltip("Deniz kaç kare yükseklikte (su yüzeyi y=14'te, yani 14)")]
    public int height = 14;

    [Header("Başlangıç")]
    [Tooltip("Oyun başında yüzeye yakın kaç müsilaj karesi olacak")]
    public int startMucilageCells = 20;

    [Tooltip("Oyun başında derinde kaç çekirdek olacak")]
    public int startCores = 2;

    // ---------- ADIM 3: YAYILMA ----------
    [Header("Yayılma")]
    [Tooltip("Kaç saniyede bir yayılma kontrolü yapılır. Küçüldükçe müsilaj hızlanır")]
    public float spreadInterval = 1.5f;

    [Tooltip("Her kontrolde normal bir müsilaj karesinin komşusuna bulaşma ihtimali (0.08 = %8)")]
    [Range(0f, 1f)] public float spreadChance = 0.08f;

    [Tooltip("Her kontrolde bir çekirdeğin komşusuna bulaşma ihtimali (0.5 = %50)")]
    [Range(0f, 1f)] public float coreSpreadChance = 0.5f;

    // ---------- ADIM 4: HAREKET ----------
    [Header("Hareket")]
    [Tooltip("Yüzme gücü. Büyüdükçe dalgıç hızlanır")]
    public float swimForce = 25f;

    [Tooltip("Müsilajın içindeyken hız çarpanı (0.5 = yarı hız)")]
    [Range(0f, 1f)] public float mucilageSpeedMultiplier = 0.5f;

    // ---------- ADIM 5: EMME ----------
    [Header("Emme")]
    [Tooltip("Normal müsilaj karesinin canı. Emme gücüyle bu kadar saniyede temizlenir (gücü 1 ise)")]
    public float mucilageHp = 0.3f;

    [Tooltip("Çekirdeğin canı. Normal kareden çok daha dayanıklı")]
    public float coreHp = 2f;

    [Tooltip("Emme cihazının saniyede verdiği hasar")]
    public float suctionPower = 1f;

    [Tooltip("Emme konisinin menzili (kare cinsinden)")]
    public float suctionRange = 3f;

    [Tooltip("Emme konisinin toplam açısı (derece)")]
    [Range(1f, 180f)] public float suctionAngle = 50f;

    // ---------- ADIM 6: NEFES ----------
    [Header("Nefes")]
    [Tooltip("Su altında nefes kaç saniye yeter")]
    public float breathMax = 12f;

    [Tooltip("Müsilajın içindeyken nefes kaç kat hızlı biter (1.5 = %50 daha hızlı)")]
    public float mucilageBreathMultiplier = 1.5f;

    [Tooltip("Yüzeyde boştan tama dolması kaç saniye sürer")]
    public float breathRefillTime = 2.5f;

    [Tooltip("Nefes bitince kaç saniye kontrol olmadan baygın kalır")]
    public float faintDuration = 4f;

    [Tooltip("Bayılınca dalgıcın ışınlandığı tekne konumu")]
    public Vector2 boatPosition = new Vector2(16f, 13.5f);

    // ---------- ADIM 7: OYUN AKIŞI ----------
    [Header("Sıcaklık Aşamaları")]
    [Tooltip("Bir sıcaklık aşaması kaç saniye sürer")]
    public float stageDuration = 50f;

    [Tooltip("Oyun başındaki deniz suyu sıcaklığı (°C)")]
    public float startTemperature = 22f;

    [Tooltip("Her aşamada sıcaklık kaç derece artar")]
    public float temperaturePerStage = 1f;

    [Tooltip("Her aşamada yayılma aralığı bununla çarpılır (0.85 = %15 daha sık yayılır)")]
    [Range(0.1f, 1f)] public float stageSpreadMultiplier = 0.85f;

    [Tooltip("Her aşamada kaç yeni çekirdek doğar")]
    public int coresPerStage = 1;

    [Header("Oyun Sonu")]
    [Tooltip("Müsilaj yoğunluğu bu orana ulaşınca oyun biter (0.85 = %85)")]
    [Range(0f, 1f)] public float loseDensity = 0.85f;

    // ---------- ADIM 8: SKOR ----------
    [Header("Skor")]
    [Tooltip("Normal müsilaj karesi temizleme puanı")]
    public int scoreMucilage = 10;

    [Tooltip("Çekirdek temizleme puanı")]
    public int scoreCore = 100;

    [Tooltip("Alt yarıda (derinde) temizlenen karelerin puan çarpanı")]
    public float deepScoreMultiplier = 1.5f;

    [Tooltip("Oyunda hayatta kalınan her saniye için puan")]
    public float scorePerSecond = 2f;

    [Header("Kombo")]
    [Tooltip("Su altında temizlenen her normal kare komboyu bu kadar artırır")]
    public float comboStep = 0.1f;

    [Tooltip("Su altında temizlenen her çekirdek komboyu bu kadar artırır")]
    public float comboCoreStep = 0.5f;

    [Tooltip("Bu kadar saniye hiç kare temizlenmezse kombo düşer (x1'e döner)")]
    public float comboTimeout = 3f;

    [Tooltip("Kombo çarpanının üst sınırı")]
    public float comboMax = 3f;

    [Header("Rütbeler")]
    [Tooltip("Rütbe adları, küçükten büyüğe")]
    public string[] rankNames = { "Çaylak Dalgıç", "Kıyı Gönüllüsü", "Serbest Dalgıç", "Marmara Bekçisi", "Denizin Nefesi" };

    [Tooltip("Her rütbe için gereken puan (rankNames ile aynı sırada)")]
    public int[] rankThresholds = { 0, 1500, 4000, 8000, 15000 };
}