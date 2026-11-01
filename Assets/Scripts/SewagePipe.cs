using UnityEngine;

// Arıtılmamış atık su borusu. "FX" objesine ekle.
// Sol duvardan denize çıkan bir boru: sürekli kahverengi su akıtır ve pipeInterval saniyede bir
// ağzının önüne müsilaj bırakır. Müsilajın kaynağını oyunda görünür kılar.
public class SewagePipe : MonoBehaviour
{
    [Header("Bağlantılar")]
    public GameConfig cfg;
    public MucilageGrid grid;
    public FxManager fx;
    public Sprite sprite;
    public Material material;

    static readonly Color PipeColor = new Color32(0x3A, 0x3F, 0x44, 0xFF);
    static readonly Color SewageColor = new Color32(0x6B, 0x5B, 0x3A, 0xFF);

    float pipeTimer;
    float dripTimer;
    Vector2 mouth; // Borunun ağzı (dünya konumu)

    void Start()
    {
        mouth = new Vector2(1.1f, cfg.pipeRow + 0.5f);

        // Boru gövdesi + ağız bileziği: ekranın solundan içeri uzanan iki dikdörtgen
        MakeRect("Pipe", new Vector2(0.45f, mouth.y), new Vector2(1.3f, 0.55f), PipeColor);
        MakeRect("PipeRim", new Vector2(1.05f, mouth.y), new Vector2(0.2f, 0.75f), PipeColor * 0.8f);
    }

    void MakeRect(string name, Vector2 pos, Vector2 size, Color color)
    {
        SpriteRenderer sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform, false);
        sr.transform.position = pos;
        sr.transform.localScale = new Vector3(size.x, size.y, 1f);
        sr.sprite = sprite;
        if (material) sr.sharedMaterial = material;
        color.a = 1f;
        sr.color = color;
        sr.sortingOrder = 2; // müsilajın önünde
    }

    void Update()
    {
        // Sürekli akan pis su (sadece görsel)
        dripTimer -= Time.deltaTime;
        if (dripTimer <= 0f)
        {
            dripTimer = 0.08f;
            Vector2 vel = new Vector2(Random.Range(1.5f, 3f), Random.Range(-0.6f, 0.6f));
            fx.Spawn(mouth, vel, SewageColor, Random.Range(0.12f, 0.22f));
        }

        // Belirli aralıklarla ağzın önüne müsilaj bırakır
        pipeTimer += Time.deltaTime;
        if (pipeTimer >= cfg.pipeInterval)
        {
            pipeTimer = 0f;
            Pollute();
        }
    }

    // Ağza en yakın temiz kareyi müsilajlı yapar (önce tam önü, sonra çevresi)
    void Pollute()
    {
        int row = cfg.pipeRow;
        for (int dx = 1; dx <= 3; dx++)
        {
            foreach (int dy in new[] { 0, 1, -1 })
            {
                Vector2Int c = new Vector2Int(dx, row + dy);
                if (grid.InBounds(c) && !grid.IsInfected(c))
                {
                    grid.Infect(c);
                    return;
                }
            }
        }
    }
}
