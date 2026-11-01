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
}