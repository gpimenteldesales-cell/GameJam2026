using System.Collections.Generic;
using UnityEngine;

// Estrelas que piscam no ceu, com algumas "cruzadas" e uma estrela cadente de vez em quando.
// Coloque em um GameObject vazio (posicao 0,0,0 e escala 1).
// Dica: a imagem da ponte (hercilioLuz) e opaca e inclui o ceu, entao as estrelas ficam
// por cima dela (ordem 1). Mantenha yMinimo perto do topo das torres para nao cruzar a ponte.
public class EstrelasPiscando : MonoBehaviour
{
    [Header("Area do ceu (unidades do mundo)")]
    public float yMinimo = 4f;
    public float yMaximo = 8f;
    public float margemX = 0.5f;

    [Header("Estrelas")]
    public int quantidade = 80;
    public Vector2 tamanho = new Vector2(0.06f, 0.16f);
    [Range(0f, 1f)] public float chanceDeCruz = 0.12f;
    public float tamanhoDaCruz = 0.45f;
    public Vector2 velocidadeDoPisca = new Vector2(0.8f, 3f);
    [Range(0f, 1f)] public float brilhoMinimo = 0.2f;
    [Range(0f, 1f)] public float brilhoMaximo = 1f;
    public Color[] cores = { Color.white, new Color(0.85f, 0.78f, 1f), new Color(1f, 0.95f, 0.8f) };

    [Header("Evitar a lua")]
    public Transform lua;
    public float raioDaLua = 2.6f;

    [Header("Estrela cadente")]
    public bool estrelasCadentes = true;
    public Vector2 intervaloCadente = new Vector2(8f, 20f);

    [Header("Desenho")]
    public int ordemDesenho = 1;
    [Tooltip("Opcional: material Sprite-Unlit-Default para as estrelas nao serem escurecidas pela Luz Global.")]
    public Material material;

    class Estrela
    {
        public SpriteRenderer sr;
        public Color cor;
        public float escala, freq, fase, altura;
    }

    readonly List<Estrela> estrelas = new List<Estrela>();
    Sprite sprPonto, sprCruz;
    Camera cam;

    GameObject cadObj;
    SpriteRenderer cadSr;
    bool cadAtiva;
    float cadInicio, cadDuracao, cadProxima, cadDistancia;
    Vector2 cadOrigem, cadDir;

    void Start()
    {
        cam = Camera.main;
        if (lua == null)
        {
            GameObject g = GameObject.Find("Lua");
            if (g != null) lua = g.transform;
        }

        sprPonto = SpritesProcedurais.Brilho();
        sprCruz = SpritesProcedurais.EstrelaCruz();

        float meia = MeiaLargura();
        float cx = CentroX();
        int tentativas = 0;

        while (estrelas.Count < quantidade && tentativas < quantidade * 10)
        {
            tentativas++;
            float x = Random.Range(cx - meia - margemX, cx + meia + margemX);
            // sqrt empurra a maioria das estrelas para o alto do ceu
            float y = Mathf.Lerp(yMinimo, yMaximo, Mathf.Sqrt(Random.value));

            if (lua != null && Vector2.Distance(new Vector2(x, y), lua.position) < raioDaLua) continue;

            CriarEstrela(x, y);
        }

        if (estrelasCadentes)
        {
            cadObj = new GameObject("EstrelaCadente");
            cadObj.transform.SetParent(transform, false);
            cadSr = cadObj.AddComponent<SpriteRenderer>();
            cadSr.sprite = sprPonto;
            cadSr.sortingOrder = ordemDesenho;
            if (material != null) cadSr.sharedMaterial = material;
            cadSr.enabled = false;
            cadProxima = Time.time + Random.Range(intervaloCadente.x, intervaloCadente.y);
        }
    }

    void CriarEstrela(float x, float y)
    {
        bool cruz = Random.value < chanceDeCruz;

        GameObject obj = new GameObject("Estrela");
        obj.transform.SetParent(transform, false);
        obj.transform.position = new Vector3(x, y, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = cruz ? sprCruz : sprPonto;
        sr.sortingOrder = ordemDesenho;
        if (material != null) sr.sharedMaterial = material;

        Estrela e = new Estrela();
        e.sr = sr;
        e.cor = cores != null && cores.Length > 0 ? cores[Random.Range(0, cores.Length)] : Color.white;
        e.escala = cruz ? tamanhoDaCruz * Random.Range(0.7f, 1.2f) : Random.Range(tamanho.x, tamanho.y);
        e.freq = Random.Range(velocidadeDoPisca.x, velocidadeDoPisca.y);
        e.fase = Random.value * 10f;
        // estrelas perto do horizonte ficam mais fracas
        e.altura = Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(yMinimo, yMaximo, y));

        estrelas.Add(e);
    }

    void Update()
    {
        float t = Time.time;

        for (int i = 0; i < estrelas.Count; i++)
        {
            Estrela e = estrelas[i];
            float s = 0.5f + 0.5f * Mathf.Sin(t * e.freq + e.fase);
            s *= s; // piscadas mais marcadas

            Color c = e.cor;
            c.a = Mathf.Lerp(brilhoMinimo, brilhoMaximo, s) * e.altura;
            e.sr.color = c;

            float esc = e.escala * Mathf.Lerp(0.8f, 1.1f, s);
            e.sr.transform.localScale = new Vector3(esc, esc, 1f);
        }

        AtualizarCadente();
    }

    void AtualizarCadente()
    {
        if (!estrelasCadentes || cadSr == null) return;

        if (!cadAtiva)
        {
            if (Time.time < cadProxima) return;

            float meia = MeiaLargura();
            float cx = CentroX();
            cadOrigem = new Vector2(cx + Random.Range(-meia, meia * 0.6f), Random.Range(yMaximo - 1.6f, yMaximo - 0.3f));
            cadDir = new Vector2(Random.value < 0.5f ? -1f : 1f, -0.45f).normalized;
            cadDistancia = Random.Range(4f, 7f);
            cadDuracao = Random.Range(0.55f, 0.9f);
            cadInicio = Time.time;
            cadAtiva = true;
            cadSr.enabled = true;
            cadObj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(cadDir.y, cadDir.x) * Mathf.Rad2Deg);
        }

        float p = (Time.time - cadInicio) / cadDuracao;
        if (p >= 1f)
        {
            cadAtiva = false;
            cadSr.enabled = false;
            cadProxima = Time.time + Random.Range(intervaloCadente.x, intervaloCadente.y);
            return;
        }

        float fade = Mathf.Sin(p * Mathf.PI);
        Vector2 pos = cadOrigem + cadDir * cadDistancia * p;
        cadObj.transform.position = new Vector3(pos.x, pos.y, 0f);
        cadObj.transform.localScale = new Vector3(Mathf.Lerp(0.6f, 2.2f, fade), 0.07f, 1f);
        cadSr.color = new Color(1f, 1f, 1f, fade * 0.9f);
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
