using System.Collections.Generic;
using UnityEngine;

public class BolhasDoMar : MonoBehaviour
{
    [Header("Area do mar")]
    public float xMinimo = -9f;
    public float xMaximo = 9f;
    public float yFundo = -3.5f;
    public float ySuperficie = -0.3f;

    [Header("Bolhas")]
    public float intervalo = 0.25f;
    public int maximoDeBolhas = 40;
    public Vector2 tamanho = new Vector2(0.08f, 0.22f);
    public Vector2 velocidadeSubida = new Vector2(0.4f, 1.1f);
    public float balancoLateral = 0.15f;
    public float frequenciaBalanco = 2f;
    public float deriva = -0.5f;
    public Color cor = new Color(1f, 1f, 1f, 0.55f);
    public int ordemDesenho = 3;

    class Bolha
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public float velocidade;
        public float x;
        public float y;
        public float fase;
        public float tamanho;
    }

    readonly List<Bolha> bolhas = new List<Bolha>();
    Sprite sprite;
    float tempo;

    void Start()
    {
        sprite = CriarSprite();
    }

    void Update()
    {
        tempo += Time.deltaTime;

        if (tempo >= intervalo)
        {
            tempo = 0f;
            Criar();
        }

        float altura = Mathf.Max(0.01f, ySuperficie - yFundo);

        for (int i = bolhas.Count - 1; i >= 0; i--)
        {
            Bolha b = bolhas[i];

            b.y += b.velocidade * Time.deltaTime;
            b.x += deriva * Paralaxe.multiplicadorGlobal * Time.deltaTime;

            float progresso = (b.y - yFundo) / altura;

            if (progresso >= 1f)
            {
                Destroy(b.obj);
                bolhas.RemoveAt(i);
                continue;
            }

            float balanco = Mathf.Sin(Time.time * frequenciaBalanco + b.fase) * balancoLateral;
            b.obj.transform.position = new Vector3(b.x + balanco, b.y, 0f);

            float escala = b.tamanho * Mathf.Lerp(0.7f, 1.3f, progresso);
            b.obj.transform.localScale = new Vector3(escala, escala, 1f);

            Color c = cor;
            c.a = cor.a * Mathf.Clamp01(progresso / 0.1f) * (1f - Mathf.Clamp01((progresso - 0.8f) / 0.2f));
            b.sr.color = c;
        }
    }

    void Criar()
    {
        if (sprite == null || bolhas.Count >= maximoDeBolhas) return;

        Bolha b = new Bolha();
        b.obj = new GameObject("Bolha");
        b.obj.transform.SetParent(transform, false);

        b.sr = b.obj.AddComponent<SpriteRenderer>();
        b.sr.sprite = sprite;
        b.sr.color = new Color(cor.r, cor.g, cor.b, 0f);
        b.sr.sortingOrder = ordemDesenho;

        b.x = Random.Range(xMinimo, xMaximo);
        b.y = yFundo;
        b.velocidade = Random.Range(velocidadeSubida.x, velocidadeSubida.y);
        b.fase = Random.value * 10f;
        b.tamanho = Random.Range(tamanho.x, tamanho.y);

        b.obj.transform.position = new Vector3(b.x, b.y, 0f);
        bolhas.Add(b);
    }

    static Sprite CriarSprite()
    {
        const int tam = 64;

        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[tam * tam];
        float meio = (tam - 1) / 2f;
        float raio = tam / 2f - 1f;
        Vector2 centro = new Vector2(meio, meio);
        Vector2 brilho = new Vector2(meio - raio * 0.35f, meio + raio * 0.4f);

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                Vector2 p = new Vector2(x, y);
                float n = Vector2.Distance(p, centro) / raio;

                float fora = Mathf.Clamp01((1f - n) / 0.06f);
                float borda = Mathf.Clamp01(1f - Mathf.Abs(n - 0.92f) / 0.1f);
                float miolo = 0.18f;
                float ponto = Mathf.Clamp01(1f - Vector2.Distance(p, brilho) / (raio * 0.22f));

                float a = Mathf.Max(Mathf.Max(borda, miolo), ponto) * fora;
                pixels[y * tam + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), tam);
    }
}
