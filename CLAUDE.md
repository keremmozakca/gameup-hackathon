# Marmara'nın Nefesi — Proje Bağlamı

GameUP Türkiye Oyun Hackathonu (COP31 teması: Okyanuslar ve Denizler) için tek kişilik Unity projesi.
**Teslim: bugün 23.30 (son sınır 23.59).** Hız her şeyden önemli; kapsam büyütme.

## Oyun özeti
Müsilajla kaplanan Marmara'da nefesini tutarak dalan bir dalgıç, emme cihazıyla müsilajı temizler.
Müsilaj ızgara üzerinde yayılır; yoğunluk %85'e ulaşınca oyun biter. Sonsuz mod, skor avı.
Ana fikir: "Deniz nefes alamıyor, sen de alamıyorsun." Hedef oynanış: tur başına 3–5 dk, toplamda ~10 dk tekrar oynanabilirlik.

## Teknik kurallar
- Unity 6, 2D (URP). Active Input Handling: **Both**. Basit `Input.GetAxisRaw` / `Input.GetMouseButton` kullan.
- Kamera: Orthographic, Size 9, Position (16, 9, -10). Ekran 32 x 18 birim, sol alt köşe (0,0).
- Deniz: 32 x 14 kare (y 0–14), gökyüzü y 14–18. Grid (0,0,0)'da, hücre 1x1.
  Dünya → kare: `new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y))`.
- Müsilaj verisi `MucilageGrid` içinde dizide (`hp`, `isCore`); Tilemap sadece çizer. Tile'lar kodda oluşturulur (Tile Palette yok).
- Tüm denge sayıları `GameConfig` (ScriptableObject) içinde. **Yeni değişkeni sadece ilgili mekanik eklenirken ekle**, Tooltip ile Türkçe açıkla.
- Kod yorumları Türkçe. Kullanıcıya her değişikliği Türkçe, kısa ve sade açıkla.
- Sahne/prefab (.unity, .prefab) YAML dosyalarını elle düzenleme. Sahne kurulumu gerekiyorsa `Assets/Editor/` altında menüden çalışan bir Editor scripti yaz (ör. "Marmara > Sahneyi Kur") veya kullanıcıya Inspector adımlarını ver.
- Kullanıcı Unity'de Play'e basıp test eder ve sonucu bildirir. Her adım sonunda commit önermeyi unutma.

## Mevcut durum (tamamlandı)
1. Sahne iskeleti: Main Camera, Sky (Square sprite, #C9D6DF), arka plan #0B4F6C.
2. `MucilageTilemap` + `MucilageGrid.cs`: başlangıç müsilajı (üst 4 satır), çekirdekler (alt yarı).
3. Yayılma: `spreadInterval` turları, `spreadChance` / `coreSpreadChance`, yukarı yönlü eğilim (%40).
4. `Player` (Circle, #FF7A59, Rigidbody2D: Gravity 0, Linear Damping 3, Freeze Rot Z) + `PlayerController.cs`:
   WASD yüzme, müsilajda yavaşlama, sınırlar (SurfaceY = H - 0.5, AtSurface, CurrentCell, CanControl).
5. `SuctionTool.cs` (Player üzerinde) + koni görseli `SuctionCone` alt objesi (LineRenderer). `MucilageGrid.Damage` ve
   `OnCellCleared(cell, wasCore)` eventi var. Kurulum menüsü: "Marmara > Emme Aracını Kur" (`Assets/Editor/SuctionSetup.cs`).
6. `BreathSystem.cs` (Player üzerinde): Breath, Breath01, IsFainted, IsRefilling, SurfaceBlocked; eventler `OnBreathTaken`, `OnFainted`.
   Kurulum menüsü: "Marmara > Nefes Sistemini Kur".
7. `GameManager.cs` (ayrı obje, `GameManager.I`): State Menu/Playing/GameOver (Menu ve GameOver'da Time.timeScale = 0),
   Stage, Temperature, ElapsedTime, StageProgress01; eventler `OnStateChanged`, `OnStageChanged`. Yayılma aralığı
   `grid.SpreadInterval` üzerinden değişir (config'e yazılmaz). Restart sahneyi yeniden yükler, menüyü atlar.
   Kurulum menüsü: "Marmara > GameManager'ı Kur".
8. `ScoreManager.cs` (GameManager objesinde, `ScoreManager.I`): Score, Combo, ComboTime01, RankIndex, RankName, IsMaxRank,
   PointsToNextRank, TopScores (PlayerPrefs "top0".."top4"), IsNewBest; eventler `OnRankUp(name)`, `OnPointsGained(points)`.
   Kombo (dalış serisi): sadece su altında büyür (kare +comboStep, çekirdek +comboCoreStep, max comboMax);
   yüzeye değmek (kapalı yüzey dahil), bayılmak veya comboTimeout sn temizlememek x1'e düşürür.
   Menüler: "Marmara > Skor Sistemini Kur", "Marmara > En İyi Skorları Sıfırla".
9. `UIManager.cs` ("UI" objesi): Canvas ve tüm öğeler runtime'da kodla kurulur, yazılar TextMeshPro (Assets/TextMesh Pro içe aktarıldı).
   HUD (nefes barı, termometre, skor + kombo + kombo süre barı, yoğunluk), rütbe bildirimi, ana menü, oyun sonu + bilgi kartı.
   Geçici OnGUI yazıları kaldırıldı. Kurulum menüsü: "Marmara > Arayüzü Kur".

Mevcut GameConfig alanları: width, height, startMucilageCells, startCores, spreadInterval, spreadChance, coreSpreadChance, swimForce, mucilageSpeedMultiplier,
mucilageHp, coreHp, suctionPower, suctionRange, suctionAngle,
breathMax, mucilageBreathMultiplier, breathRefillTime, faintDuration, boatPosition,
stageDuration, startTemperature, temperaturePerStage, stageSpreadMultiplier, coresPerStage, loseDensity,
scoreMucilage, scoreCore, deepScoreMultiplier, scorePerSecond, comboStep, comboCoreStep, comboTimeout, comboMax, rankNames, rankThresholds.

## Kalan adımlar (bu sırayla)
5. **Emme (SuctionTool):** Sol tık basılı → fare yönünde koni (menzil 3, açı 50°). Konideki karelerin hp'si `suctionPower * dt` azalır.
   Config: mucilageHp 0.3, coreHp 2, suctionPower 1, suctionRange 3, suctionAngle 50.
   MucilageGrid'e `Damage(cell, amount)` ve `OnCellCleared(cell, wasCore)` eventi ekle. Geçici koni görseli: LineRenderer.
6. **Nefes (BreathSystem):** Su altında azalır (breathMax 12 sn), müsilajda 1.5x hızlı. Yüzeyde dolar (breathRefillTime 2.5 sn)
   ama **sadece üstündeki yüzey karesi (y = H-1) temizse**. Biterse bayılma: tekneye (16, 13.5) ışınlan, faintDuration 4 sn kontrol yok.
7. **GameManager:** Sıcaklık aşamaları (stageDuration 50 sn, başlangıç 22 °C, her aşama +1 °C, yayılma aralığı x0.85, +1 çekirdek).
   Yoğunluk >= loseDensity (0.85) → oyun sonu. Durumlar: Menü, Oyun, Bitti.
8. **ScoreManager:** Normal kare 10, çekirdek 100, alt yarı x1.5, hayatta kalınan saniye 2. Kombo: yüzeye çıkmadan her kare +0.1 (max x3);
   nefes almak/bayılmak sıfırlar. Rütbeler: Çaylak Dalgıç 0, Kıyı Gönüllüsü 1500, Serbest Dalgıç 4000, Marmara Bekçisi 8000, Denizin Nefesi 15000.
   PlayerPrefs ile en iyi 5 skor. Oyun sonunda "Sonraki rütbeye X puan kaldı".
9. **UI:** HUD (nefes barı karakter üstünde, sol üst termometre, orta üst skor+kombo, sağ üst yoğunluk %), rütbe bildirimi,
   ana menü, oyun sonu ekranı + bilgi kartı: "Müsilaj; ısınan deniz suyu, arıtılmamış atık sular ve durgun su birleştiğinde çoğalır.
   Oyunda yeniden başlayabilirsin. Marmara'da bunun bir tuşu yok."
10. **Cila (vakit kalırsa, sırayla):** partikül/pop, çekirdek nabzı, ses, %70 üstü vignette, balıklar, atık su borusu, giriş ekranı.
11. **WebGL build:** Compression Format: Disabled. itch.io'ya yükle.

Takvim: ~19.45 özellik dondurma → build ve test → 21.00 video → 21.30 sunum → 23.30 teslim.
Renk paleti: derin su #0B4F6C, sığ su #20A4C4, müsilaj #B8AE94, çekirdek #4A4532, gökyüzü #C9D6DF, vurgu #FF7A59.
Görseller: CC0 kaynaklar (kenney.nl, freesound.org CC0 filtresi); kaynakları not et.
