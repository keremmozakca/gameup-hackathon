using System.Runtime.InteropServices;
using UnityEngine;

// Giriş hikâyesinin seslendirmesi.
// 1) Assets/Resources/intro_voice (wav/mp3/ogg) varsa o kayıt çalınır (kendi sesinle kaydedebilirsin).
// 2) Yoksa WebGL'de tarayıcının Türkçe konuşma motoru kalın ve yavaş bir sesle okur (Narrator.jslib).
// Editörde konuşma motoru yoktur; ses sadece build'de duyulur.
public static class Narrator
{
    const float Rate = 0.85f;  // yavaş
    const float Pitch = 0.6f;  // kalın, boğuk

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void NarratorSpeak(string text, float rate, float pitch);
    [DllImport("__Internal")] static extern void NarratorStop();
#endif

    static AudioSource source;

    // Kayıtlı ses dosyası var mı? Varsa satır satır değil, bir kerede çalınır.
    public static bool HasRecording => Resources.Load<AudioClip>("intro_voice") != null;

    public static void PlayRecording(GameObject host)
    {
        AudioClip clip = Resources.Load<AudioClip>("intro_voice");
        if (!clip) return;
        if (!source) source = host.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = 1f;
        source.Play();
    }

    public static void Speak(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        NarratorSpeak(text, Rate, Pitch);
#endif
    }

    public static void Stop()
    {
        if (source) source.Stop();
#if UNITY_WEBGL && !UNITY_EDITOR
        NarratorStop();
#endif
    }
}
