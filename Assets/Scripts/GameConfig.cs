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
    public float stageDuration = 40f;

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

    // ---------- ADIM 10: ATIK SU BORUSU ----------
    [Header("Atık Su Borusu")]
    [Tooltip("Boru kaç saniyede bir ağzının önüne müsilaj bırakır")]
    public float pipeInterval = 10f;

    [Tooltip("Borunun sol duvarda bulunduğu satır (0 = en dip)")]
    public int pipeRow = 2;

    [Tooltip("Boru her seferinde kaç kare müsilaj bırakır")]
    public int pipeCells = 4;

    [Tooltip("Boru hangi sıcaklık aşamasında açılır (0 = oyun başı)")]
    public int pipeStartStage = 1;

    [Tooltip("Boruyu tıkamak için E tuşunu kaç saniye basılı tutmak gerekir")]
    public float pipePlugHold = 2f;

    [Tooltip("Tıkanan boru kaç saniye kapalı kalır")]
    public float pipePlugDuration = 40f;

    [Tooltip("Boruyu tıkamak için ağzına en fazla ne kadar yakın olmalı (birim)")]
    public float pipePlugRange = 2.5f;

    // ---------- ZORLUK BASKISI ----------
    [Header("Zorluk Baskısı")]
    [Tooltip("Denizde her zaman en az bu kadar çekirdek olur (her aşamada +1). Hepsini temizlersen yenileri doğar")]
    public int minCores = 2;

    [Tooltip("Çekirdek sayısı alt sınırın altına düşünce yenisinin doğması için geçen süre (sn)")]
    public float coreRespawnDelay = 6f;

    [Tooltip("Rütbe yükselince Marmara'nın cevabı: kaç ek çekirdek doğar")]
    public int coresOnRankUp = 2;

    // ---------- KRİZ HABERLERİ ----------
    [Header("Kriz Haberleri")]
    [Tooltip("İlk haber oyunun kaçıncı saniyesinde gelir")]
    public float firstCrisisTime = 25f;

    [Tooltip("Bir haber bittikten sonra yenisine kadar en az kaç saniye geçer")]
    public float crisisGapMin = 15f;

    [Tooltip("Bir haber bittikten sonra yenisine kadar en fazla kaç saniye geçer")]
    public float crisisGapMax = 30f;

    [Tooltip("Oyun başında iyi haber gelme ihtimali")]
    [Range(0f, 1f)] public float goodNewsChance = 0.4f;

    [Tooltip("Her aşamada iyi haber ihtimali bu kadar azalır (en az %10)")]
    public float goodNewsDropPerStage = 0.05f;

    // ---------- BALIKLAR ----------
    [Header("Balıklar")]
    [Tooltip("Müsilaja sıkışan balığı kurtarma puanı")]
    public int fishRescueScore = 150;

    [Tooltip("Balığı emme cezası")]
    public int fishSuckPenalty = 50;

    [Tooltip("Balık bu kadar saniyeden uzun emilirse ceza gelir")]
    public float fishSuckPenaltyTime = 0.3f;

    [Tooltip("Sıkışan balık kaç saniye sonra boğulur")]
    public float fishDrownTime = 20f;

    [Header("Rütbeler")]
    [Tooltip("Rütbe adları, küçükten büyüğe")]
    public string[] rankNames = { "Çaylak Dalgıç", "Kıyı Gönüllüsü", "Serbest Dalgıç", "Marmara Bekçisi", "Denizin Nefesi" };

    [Tooltip("Her rütbe için gereken puan (rankNames ile aynı sırada)")]
    public int[] rankThresholds = { 0, 1500, 4000, 8000, 15000 };

    [Tooltip("Her rütbenin verdiği avantajın açıklaması (rankNames ile aynı sırada)")]
    public string[] rankPerks =
    {
        "Başlangıç donanımı",
        "Ciğer kapasitesi: +3 sn nefes",
        "Geniş başlık: emme menzili +1",
        "Güçlü motor: emme gücü +%40",
        "Usta dalgıç: nefes 2 kat hızlı dolar, kombo tavanı x4",
    };

    [Header("Rütbe Avantajları (birikerek gelir)")]
    [Tooltip("Kıyı Gönüllüsü: ek nefes süresi (sn)")]
    public float perkBreathBonus = 3f;

    [Tooltip("Serbest Dalgıç: ek emme menzili")]
    public float perkRangeBonus = 1f;

    [Tooltip("Marmara Bekçisi: emme gücü artışı (0.4 = %40)")]
    public float perkPowerBonus = 0.4f;

    [Tooltip("Denizin Nefesi: nefes dolum hızı çarpanı")]
    public float perkRefillMultiplier = 2f;

    [Tooltip("Denizin Nefesi: yeni kombo tavanı")]
    public float perkComboMax = 4f;
}