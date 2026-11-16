using System.Collections.Generic;
using UnityEngine;

// Vaga-lumes que vagam devagar acima da agua e acendem e apagam.
// Coloque em um GameObject vazio (posicao 0,0,0 e escala 1).
public class VagaLumes : MonoBehaviour
{
    [Header("Quantidade e area")]
    public int quantidade = 18;
    public Vector2 alturaAcimaDaAgua = new Vector2(0.3f, 4.5f);
    public float margemX = 1f;

    [Header("Movimento")]
    public Vector2 raioDeVoo = new Vector2(1.2f, 2.5f);
    public Vector2 velocidadeDoVagar = new Vector2(0.15f, 0.35f);
    [Tooltip("Deriva lenta para o lado (unidades por segundo). Negativo = esquerda.")]
    public float deriva = -0.15f;

    [Header("Brilho")]
    public Vector2 velocidadeDoPisca = new Vector2(1.2f, 2.6f);
    [Range(0f, 1f)] public float brilhoMinimo = 0.06f;
    [Range(0f, 1f)] public float opacidade = 0.9f;
    public Vector2 tamanho = new Vector2(0.18f, 0.34f);
    public Color[] cores = { new Color(0.78f, 1f, 0.35f), new Color(1f, 0.88f, 0.42f) };

    [Header("Desenho")]
    public int ordemDesenho = 4;
    [Tooltip("Opcional: material Sprite-Unlit-Default para o brilho nao ser escurecido pela Luz Global.")]
    public Material material;

    class VagaLume
    {
        public SpriteRenderer sr;
        public Vector2 centro;
        public Color cor;
        public float seed, raio, vel, freq, fase, tam;
    }

    readonly List<VagaLume> lista = new List<VagaLume>();
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        Sprite sprite = SpritesProcedurais.Brilho();
        float meia = MeiaLargura();
        float cx = CentroX();

        for (int i = 0; i < quantidade; i++)
        {
            VagaLume v = new VagaLume();
            v.centro = new Vector2(
                Random.Range(cx - meia, cx + meia),
                ProfundidadeDoMar.NivelDoMar + Random.Range(alturaAcimaDaAgua.x, alturaAcimaDaAgua.y));
            v.cor = cores != null && cores.Length > 0 ? cores[Random.Range(0, cores.Length)] : Color.white;
            v.seed = Random.value * 100f;
            v.raio = Random.Range(raioDeVoo.x, raioDeVoo.y);
            v.vel = Random.Range(velocidadeDoVagar.x, velocidadeDoVagar.y);
            v.freq = Random.Range(velocidadeDoPisca.x, velocidadeDoPisca.y);
            v.fase = Random.value * 10f;
            v.tam = Random.Range(tamanho.x, tamanho.y);

            GameObject obj = new GameObject("VagaLume");
            obj.transform.SetParent(transform, false);
            v.sr = obj.AddComponent<SpriteRenderer>();
            v.sr.sprite = sprite;
            v.sr.sortingOrder = ordemDesenho;
            if (material != null) v.sr.sharedMaterial = material;

            lista.Add(v);
        }
    }

    void Update()
    {
        float t = Time.time;
        float meia = MeiaLargura();
        float cx = CentroX();

        for (int i = 0; i < lista.Count; i++)
        {
            VagaLume v = lista[i];

            v.centro.x += deriva * Time.deltaTime;
            if (v.centro.x < cx - meia - margemX) v.centro.x = cx + meia + margemX;
            else if (v.centro.x > cx + meia + margemX) v.centro.x = cx - meia - margemX;

            // Perlin da um vagar suave, sem trancos
            float nx = Mathf.PerlinNoise(v.seed, t * v.vel) - 0.5f;
            float ny = Mathf.PerlinNoise(v.seed + 31.7f, t * v.vel) - 0.5f;
            v.sr.transform.position = new Vector3(
                v.centro.x + nx * 3f * v.raio,
                v.centro.y + ny * 3f * v.raio * 0.7f,
                0f);

            // acende por um trecho do ciclo e fica quase apagado no resto
            float s = 0.5f + 0.5f * Mathf.Sin(t * v.freq + v.fase);
            float brilho = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.95f, s));
            brilho = Mathf.Max(brilho, brilhoMinimo);

            Color c = v.cor;
            c.a = brilho * opacidade;
            v.sr.color = c;

            float esc = v.tam * (0.75f + 0.35f * brilho);
            v.sr.transform.localScale = new Vector3(esc, esc, 1f);
        }
    }

    float MeiaLargura()
    {
        return cam != null && cam.orthographic ? cam.orthographicSize * cam.aspect : 14f;
    }

    float CentroX()
    {
        return cam != null ? cam.transform.position.x : 0f;
    }
}
