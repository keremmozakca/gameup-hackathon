using UnityEngine;

// Sesler. "FX" objesine ekle. Ses dosyası yok: tüm sesler oyun başında kodla üretilir
// (sinüs + gürültü). Lisans derdi yok, WebGL'de de çalışır.
public class SoundManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    public MucilageGrid grid;
    public BreathSystem breath;
    public SuctionTool suction;

    [Range(0f, 1f)] public float masterVolume = 0.6f;

    const int SampleRate = 44100;
    const float PopCooldown = 0.04f; // Aynı anda çok kare temizlenince ses yığılmasın

    AudioSource oneShot;
    AudioSource suctionLoop;
    AudioClip pop, corePop, gasp, faint, rankUp, gameOver, suctionNoise;
    float popTimer;

    void Awake()
    {
        oneShot = gameObject.AddComponent<AudioSource>();
        oneShot.playOnAwake = false;

        pop = Sweep("pop", 0.07f, 900f, 350f, 0.5f, 0f);
        corePop = Sweep("corePop", 0.35f, 300f, 60f, 0.8f, 0.35f);
        gasp = Noise("gasp", 0.45f, 0.35f, 0.15f);
        faint = Sweep("faint", 0.8f, 180f, 45f, 0.7f, 0.1f);
        rankUp = Notes("rankUp", new[] { 523f, 659f, 784f, 1047f }, 0.11f, 0.35f);
        gameOver = Notes("gameOver", new[] { 392f, 330f, 262f, 196f }, 0.28f, 0.4f);
        suctionNoise = Noise("suction", 1f, 1f, 0.02f, true);

        suctionLoop = gameObject.AddComponent<AudioSource>();
        suctionLoop.clip = suctionNoise;
        suctionLoop.loop = true;
        suctionLoop.volume = 0f;
        suctionLoop.Play();
    }

    void Start()
    {
        grid.OnCellCleared += HandleCleared;
        breath.OnBreathTaken += HandleBreath;
        breath.OnFainted += HandleFaint;
        ScoreManager.I.OnRankUp += HandleRankUp;
        GameManager.I.OnStateChanged += HandleState;
    }

    void OnDestroy()
    {
        if (grid) grid.OnCellCleared -= HandleCleared;
        if (breath)
        {
            breath.OnBreathTaken -= HandleBreath;
            breath.OnFainted -= HandleFaint;
        }
        if (ScoreManager.I) ScoreManager.I.OnRankUp -= HandleRankUp;
        if (GameManager.I) GameManager.I.OnStateChanged -= HandleState;
    }

    void Update()
    {
        popTimer -= Time.unscaledDeltaTime;

        // Emme sesi: basılıyken yumuşakça açılır, bırakınca kapanır
        bool on = suction.IsSucking && GameManager.I.CurrentState == GameManager.State.Playing;
        float target = on ? 0.25f * masterVolume : 0f;
        suctionLoop.volume = Mathf.MoveTowards(suctionLoop.volume, target, Time.unscaledDeltaTime * 2f);
    }

    void HandleCleared(Vector2Int cell, bool wasCore)
    {
        if (wasCore)
        {
            Play(corePop, 1f, 1f);
            return;
        }
        if (popTimer > 0f) return;
        popTimer = PopCooldown;
        Play(pop, Random.Range(0.85f, 1.25f), 0.6f);
    }

    void HandleBreath() => Play(gasp, 1f, 0.8f);
    void HandleFaint() => Play(faint, 1f, 1f);
    void HandleRankUp(string _) => Play(rankUp, 1f, 0.8f);

    void HandleState(GameManager.State s)
    {
        if (s == GameManager.State.GameOver) Play(gameOver, 1f, 0.9f);
    }

    void Play(AudioClip clip, float pitch, float volume)
    {
        oneShot.pitch = pitch;
        oneShot.PlayOneShot(clip, volume * masterVolume);
    }

    // ---------- Ses üretimi ----------

    // Frekansı f0'dan f1'e kayan sinüs + isteğe bağlı gürültü. Sonu yumuşakça söner.
    static AudioClip Sweep(string name, float dur, float f0, float f1, float vol, float noise)
    {
        int n = (int)(SampleRate * dur);
        float[] d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            phase += 2f * Mathf.PI * Mathf.Lerp(f0, f1, k) / SampleRate;
            float s = Mathf.Sin(phase) * (1f - noise) + Random.Range(-1f, 1f) * noise;
            d[i] = s * vol * Envelope(k, 0.02f);
        }
        return Make(name, d);
    }

    // Yumuşatılmış gürültü (nefes / emme sesi). smooth büyüdükçe ses kalınlaşır.
    static AudioClip Noise(string name, float dur, float vol, float smooth, bool loop = false)
    {
        int n = (int)(SampleRate * dur);
        float[] d = new float[n];
        float y = 0f;
        for (int i = 0; i < n; i++)
        {
            y += (Random.Range(-1f, 1f) - y) * smooth; // basit alçak geçiren filtre
            float env = loop ? 1f : Mathf.Sin(Mathf.PI * i / n); // döngüde zarf yok, nefeste aç-kapa
            d[i] = y * vol * env * (loop ? 8f : 4f);
        }
        return Make(name, d);
    }

    // Art arda kısa notalar (rütbe / oyun sonu)
    static AudioClip Notes(string name, float[] freqs, float noteDur, float vol)
    {
        int per = (int)(SampleRate * noteDur);
        float[] d = new float[per * freqs.Length];
        for (int j = 0; j < freqs.Length; j++)
        {
            for (int i = 0; i < per; i++)
            {
                float t = (float)i / SampleRate;
                float k = (float)i / per;
                d[j * per + i] = Mathf.Sin(2f * Mathf.PI * freqs[j] * t) * vol * Envelope(k, 0.05f);
            }
        }
        return Make(name, d);
    }

    // Hızlı açılış, yavaş sönüş: tık sesi olmasın
    static float Envelope(float k, float attack) => Mathf.Min(1f, k / attack) * (1f - k) * (1f - k);

    static AudioClip Make(string name, float[] data)
    {
        AudioClip c = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        c.SetData(data, 0);
        return c;
    }
}
