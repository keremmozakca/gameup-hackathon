using System.Collections.Generic;
using UnityEngine;

// Kodla çizilen piksel art. Görsel dosyası yok: her sprite ya satır satır yazılmış bir "harita"dan
// ya da küçük bir formülden üretilir. Point filtre sayesinde pikseller keskin kalır.
// Haritalarda her karakter bir piksel, '.' boş; ilk satır en üst satırdır.
public static class PixelArt
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // Sahne yeniden yüklenince Unity kullanılmayan dokuları silebilir; silinmişse yeniden çiz
    static bool TryCached(string key, out Sprite s) => cache.TryGetValue(key, out s) && s != null;

    static Color C(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    static Texture2D NewTex(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.SetPixels(new Color[w * h]); // hepsi şeffaf
        return t;
    }

    static Sprite Make(string key, Texture2D t, float ppu, Vector2 pivot)
    {
        t.Apply();
        Sprite s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, ppu, 0, SpriteMeshType.FullRect);
        // Sahne değişiminde bellekten silinmesin ("Yeniden dal" sonrası görseller kaybolmasın)
        t.hideFlags = HideFlags.DontUnloadUnusedAsset;
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        cache[key] = s;
        return s;
    }

    static Sprite FromRows(string key, string[] rows, Dictionary<char, Color> pal, float ppu, Vector2 pivot)
    {
        if (TryCached(key, out Sprite s)) return s;
        int h = rows.Length, w = 0;
        foreach (string r in rows) w = Mathf.Max(w, r.Length);
        Texture2D t = NewTex(w, h);
        for (int row = 0; row < h; row++)
            for (int x = 0; x < rows[row].Length; x++)
                if (pal.TryGetValue(rows[row][x], out Color c)) t.SetPixel(x, h - 1 - row, c);
        return Make(key, t, ppu, pivot);
    }

    // ---------- Piksel yazı (başlık için) ----------

    // 9 satırlık harfler: üstteki 2 satır noktalar (İ) için boş bırakıldı
    static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        { 'A', new[] { ".....", ".....", ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        { 'E', new[] { ".....", ".....", "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
        { 'F', new[] { ".....", ".....", "#####", "#....", "#....", "####.", "#....", "#....", "#...." } },
        { 'I', new[] { "...", "...", "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" } },
        { 'İ', new[] { ".#.", "...", "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" } },
        { 'M', new[] { ".....", ".....", "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" } },
        { 'N', new[] { ".....", ".....", "#...#", "##..#", "##..#", "#.#.#", "#..##", "#..##", "#...#" } },
        { 'R', new[] { ".....", ".....", "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" } },
        { 'S', new[] { ".....", ".....", ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
        { '\'', new[] { "..", "..", "##", "##", ".#", "#.", "..", "..", ".." } },
        { ' ', new[] { "...", "...", "...", "...", "...", "...", "...", "...", "..." } },
    };

    static int LineWidth(string line)
    {
        int w = 0;
        foreach (char ch in line) if (Glyphs.TryGetValue(ch, out string[] g)) w += g[0].Length + 1;
        return Mathf.Max(0, w - 1);
    }

    // Satırları ortalayarak piksel yazı çizer; her pikselin sağ altına gölge düşer
    public static Sprite Title(string key, string[] lines, Color fill, Color shadow)
    {
        if (TryCached(key, out Sprite s)) return s;
        const int gh = 9, gap = 2;
        int w = 0;
        foreach (string l in lines) w = Mathf.Max(w, LineWidth(l));
        w += 1;
        int h = lines.Length * gh + (lines.Length - 1) * gap + 1;
        Texture2D t = NewTex(w, h);

        for (int pass = 0; pass < 2; pass++) // önce gölge, sonra harf
        {
            for (int li = 0; li < lines.Length; li++)
            {
                int top = h - 1 - li * (gh + gap);
                int x = (w - 1 - LineWidth(lines[li])) / 2;
                foreach (char ch in lines[li])
                {
                    if (!Glyphs.TryGetValue(ch, out string[] g)) continue;
                    for (int r = 0; r < gh; r++)
                        for (int c = 0; c < g[r].Length; c++)
                        {
                            if (g[r][c] != '#') continue;
                            if (pass == 0) t.SetPixel(x + c + 1, top - r - 1, shadow);
                            else t.SetPixel(x + c, top - r, fill);
                        }
                    x += g[0].Length + 1;
                }
            }
        }
        return Make(key, t, 100f, new Vector2(0.5f, 0.5f));
    }

    // ---------- Karakterler ----------

    static readonly Dictionary<char, Color> DiverPal = new Dictionary<char, Color>
    {
        { 'K', C("#1A1C2C") }, { 'S', C("#2B3A55") }, { 'A', C("#FF7A59") }, { 'F', C("#F2B48C") },
        { 'M', C("#8FE3F0") }, { 'H', C("#4A2C1A") }, { 'N', C("#E8603F") },
    };

    // Deniz: yatay yüzen serbest dalgıç (sağa bakar). İki kare: palet aşağıda / yukarıda.
    public static Sprite Diver(int frame) => frame == 0
        ? FromRows("diver0", new[]
        {
            "..........KKKK..",
            ".........KHHHHK.",
            ".........KHFMMK.",
            "NNK.KKKKKKFFMMK.",
            "NNNKSSSSAASSFK..",
            ".NNKSSSSAASSSKFK",
            "NNNKSSSSSSSSKFFK",
            "NNK.KKKKKKKKK.K.",
        }, DiverPal, 12f, new Vector2(0.5f, 0.5f))
        : FromRows("diver1", new[]
        {
            "NN........KKKK..",
            "NNN......KHHHHK.",
            ".NNK.....KHFMMK.",
            "..NKKKKKKKFFMMK.",
            "...KSSSSAASSFK..",
            "...KSSSSAASSSKFK",
            "...KSSSSSSSSKFFK",
            "....KKKKKKKKK.K.",
        }, DiverPal, 12f, new Vector2(0.5f, 0.5f));

    // Emme cihazı: tutma yeri solda, ağzı sağda (pivot tutma yerinde)
    public static Sprite Device() => FromRows("device", new[]
    {
        ".KKKK....",
        "KGGGGKKKK",
        "KAAAAGGGK",
        "KGGGGKKKK",
        ".KKKK....",
    }, new Dictionary<char, Color> { { 'K', C("#1A1C2C") }, { 'G', C("#8A9BA8") }, { 'A', C("#FF7A59") } },
        12f, new Vector2(0.15f, 0.5f));

    // Balık (sağa bakar). Beyaz/gri çizilir, FishManager rengiyle boyanır.
    public static Sprite Fish() => FromRows("fish", new[]
    {
        "....WWWW..",
        "D.WWWWWWW.",
        "DDWWWWWKWW",
        "DDWWLLLWWW",
        "D.WWLLLLW.",
        "....WWW...",
    }, new Dictionary<char, Color>
    {
        { 'W', Color.white }, { 'L', new Color(0.8f, 0.8f, 0.8f) }, { 'D', new Color(0.65f, 0.65f, 0.65f) }, { 'K', C("#1A1C2C") },
    }, 12f, new Vector2(0.5f, 0.5f));

    // Deniz'in ailesinin balıkçı teknesi
    public static Sprite Boat() => FromRows("boat", new[]
    {
        ".............K..........",
        ".............KAA........",
        ".............KA.........",
        ".............K..........",
        "..........KKKKKKK.......",
        "..........KWWGWWK.......",
        "..........KWWWWWK.......",
        "KKKKKKKKKKKKKKKKKKKKKKKK",
        "KRRRRRRRRRRRRRRRRRRRRRRK",
        ".KWWWWWWWWWWWWWWWWWWWWK.",
        "..KRRRRRRRRRRRRRRRRRRK..",
        "...KKKKKKKKKKKKKKKKKK...",
    }, new Dictionary<char, Color>
    {
        { 'K', C("#1A1C2C") }, { 'R', C("#C8423B") }, { 'W', C("#F4F1E8") }, { 'G', C("#8FE3F0") }, { 'A', C("#FF7A59") },
    }, 10f, new Vector2(0.5f, 0.25f));

    // Dolu piksel daire (termometre haznesi gibi arayüz parçaları için)
    public static Sprite Disc()
    {
        if (TryCached("disc", out Sprite s)) return s;
        Texture2D t = NewTex(16, 16);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                if ((x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f) <= 60f) t.SetPixel(x, y, Color.white);
        return Make("disc", t, 16f, new Vector2(0.5f, 0.5f));
    }

    public static Sprite Bubble() => FromRows("bubble", new[] { ".W.", "W.W", ".W." },
        new Dictionary<char, Color> { { 'W', new Color(1f, 1f, 1f, 0.8f) } }, 16f, new Vector2(0.5f, 0.5f));

    // ---------- Müsilaj kareleri (gri tonlu çizilir, tile rengiyle boyanır) ----------

    public static Sprite MucilageTile()
    {
        if (TryCached("mucilage", out Sprite s)) return s;
        System.Random r = new System.Random(7);
        Texture2D t = NewTex(16, 16);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                float v = 0.84f + (float)r.NextDouble() * 0.1f;
                if (r.NextDouble() < 0.07) v = 0.66f;                 // koyu lekeler
                if (r.NextDouble() < 0.04) v = 1f;                    // parlak kabarcık
                if (y == 15) v = 1f;                                  // üst kenar parlak
                if (y == 0) v *= 0.85f;                               // alt kenar gölge
                t.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        return Make("mucilage", t, 16f, new Vector2(0.5f, 0.5f));
    }

    public static Sprite CoreTile()
    {
        if (TryCached("core", out Sprite s)) return s;
        System.Random r = new System.Random(11);
        Texture2D t = NewTex(16, 16);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                float v = 0.55f + (float)r.NextDouble() * 0.12f;
                if (d < 3.5f) v = 1f;                                  // parlayan merkez
                else if ((x + y) % 5 == 0 && d < 7f) v = 0.85f;        // damarlar
                if (d > 7.6f) v *= 0.8f;                               // köşeler koyu
                t.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        return Make("core", t, 16f, new Vector2(0.5f, 0.5f));
    }

    // ---------- Dünya ----------

    // Dalga şeridi (2 birim genişlik): alt kısım su, tepede köpük
    public static Sprite Wave(string key, Color water, Color foam, float phase)
    {
        if (TryCached(key, out Sprite s)) return s;
        Texture2D t = NewTex(32, 8);
        for (int x = 0; x < 32; x++)
        {
            int crest = 4 + Mathf.RoundToInt(1.5f * Mathf.Sin((x / 32f + phase) * Mathf.PI * 2f));
            for (int y = 0; y <= crest; y++)
                t.SetPixel(x, y, y == crest ? foam : y == crest - 1 ? Color.Lerp(water, foam, 0.4f) : water);
        }
        return Make(key, t, 16f, new Vector2(0f, 0f));
    }

    // Dikey degrade (alttan üste), kaba bantlı: piksel art hissi verir
    public static Sprite Gradient(string key, Color bottom, Color top, int steps)
    {
        if (TryCached(key, out Sprite s)) return s;
        Texture2D t = NewTex(1, steps);
        for (int y = 0; y < steps; y++) t.SetPixel(0, y, Color.Lerp(bottom, top, y / (steps - 1f)));
        return Make(key, t, 1f, Vector2.zero);
    }

    // Uzakta İstanbul silueti: binalar, iki cami (kubbe + minareler)
    public static Sprite Skyline()
    {
        if (TryCached("skyline", out Sprite s)) return s;
        const int w = 256, h = 16;
        Color c = C("#8EA3B5");
        Texture2D t = NewTex(w, h);
        System.Random r = new System.Random(3);
        for (int x = 0; x < w;)
        {
            int bw = 3 + r.Next(6), bh = 2 + r.Next(5);
            for (int i = x; i < Mathf.Min(w, x + bw); i++)
                for (int y = 0; y < bh; y++) t.SetPixel(i, y, c);
            x += bw;
        }
        foreach (int cx in new[] { 60, 190 })
        {
            for (int x = cx - 7; x <= cx + 7; x++)
                for (int y = 0; y < 7; y++) t.SetPixel(x, y, c);                       // gövde
            for (int x = cx - 6; x <= cx + 6; x++)
                for (int y = 7; y < h; y++)
                    if ((x - cx) * (x - cx) + (y - 7) * (y - 7) * 2 <= 36) t.SetPixel(x, y, c); // kubbe
            foreach (int mx in new[] { cx - 10, cx + 10 })
                for (int y = 0; y < h; y++) t.SetPixel(mx, y, c);                       // minareler
        }
        return Make("skyline", t, 8f, Vector2.zero);
    }

    public static Sprite Cloud()
    {
        if (TryCached("cloud", out Sprite s)) return s;
        Texture2D t = NewTex(24, 8);
        for (int x = 0; x < 24; x++)
            for (int y = 0; y < 8; y++)
            {
                bool a = (x - 8) * (x - 8) / 49f + (y - 3) * (y - 3) / 9f <= 1f;
                bool b = (x - 15) * (x - 15) / 64f + (y - 2) * (y - 2) / 6f <= 1f;
                if (a || b) t.SetPixel(x, y, new Color(1f, 1f, 1f, y < 2 ? 0.75f : 0.9f));
            }
        return Make("cloud", t, 8f, new Vector2(0.5f, 0.5f));
    }

    // Deniz tabanı: dalgalı kum ve taşlar (32 birim genişlik, 1 birim yükseklik)
    public static Sprite Seabed()
    {
        if (TryCached("seabed", out Sprite s)) return s;
        const int w = 256, h = 8;
        Texture2D t = NewTex(w, h);
        System.Random r = new System.Random(5);
        Color sand = C("#B89A6A"), dark = C("#8C7550"), rock = C("#5E5648");
        for (int x = 0; x < w; x++)
        {
            int top = 4 + Mathf.RoundToInt(Mathf.Sin(x * 0.15f) * 1.2f + Mathf.Sin(x * 0.05f) * 1.5f);
            for (int y = 0; y < Mathf.Min(h, top); y++)
                t.SetPixel(x, y, r.NextDouble() < 0.15 ? dark : sand);
        }
        for (int i = 0; i < 14; i++) // taşlar
        {
            int cx = r.Next(w), rw = 2 + r.Next(3);
            for (int x = cx - rw; x <= cx + rw; x++)
                for (int y = 0; y < 3 + r.Next(2); y++)
                    if (x >= 0 && x < w) t.SetPixel(x, y, rock);
        }
        return Make("seabed", t, 8f, Vector2.zero);
    }

    public static Sprite Kelp()
    {
        if (TryCached("kelp", out Sprite s)) return s;
        Texture2D t = NewTex(4, 22);
        Color g = C("#2E8B57"), d = C("#1F5E3B");
        for (int y = 0; y < 22; y++)
        {
            int x = 1 + Mathf.RoundToInt(Mathf.Sin(y * 0.6f));
            t.SetPixel(x, y, g);
            t.SetPixel(x + 1, y, y % 4 == 0 ? d : g);
        }
        return Make("kelp", t, 10f, new Vector2(0.5f, 0f));
    }
}
