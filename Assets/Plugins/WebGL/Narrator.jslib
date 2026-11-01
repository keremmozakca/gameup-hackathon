// Tarayıcının yerleşik konuşma motoru (Web Speech API) ile Türkçe seslendirme.
// Sadece WebGL build'de çalışır; Unity editöründe sessizdir.
mergeInto(LibraryManager.library, {
  NarratorSpeak: function (textPtr, rate, pitch) {
    if (!('speechSynthesis' in window)) return;
    var text = UTF8ToString(textPtr);
    var u = new SpeechSynthesisUtterance(text);
    u.lang = 'tr-TR';
    u.rate = rate;
    u.pitch = pitch;
    u.volume = 0.9;
    var voices = window.speechSynthesis.getVoices();
    for (var i = 0; i < voices.length; i++) {
      if (voices[i].lang && voices[i].lang.toLowerCase().indexOf('tr') === 0) { u.voice = voices[i]; break; }
    }
    window.speechSynthesis.speak(u);
  },
  NarratorStop: function () {
    if ('speechSynthesis' in window) window.speechSynthesis.cancel();
  }
});
