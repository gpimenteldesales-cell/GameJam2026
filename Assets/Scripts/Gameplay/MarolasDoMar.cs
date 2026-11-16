using System.Collections.Generic;
using UnityEngine;

// Marolas (aneis achatados que se abrem na superficie):
//  - ao acaso pelo mar
//  - ao redor do barco (cola o barco na agua)
//  - sob demanda: MarolasDoMar.Instancia.Criar(posicao, 1.5f) quando o anzol cair na agua
// Coloque em um GameObject vazio (posicao 0,0,0 e escala 1).
public class MarolasDoMar : MonoBehaviour
{
    public static MarolasDoMar Instancia;

    [Header("Ao acaso")]
    public bool marolasAoAcaso = true;
    public Vector2 intervalo = new Vector2(0.5f, 1.4f);
    [Tooltip("Quanto abaixo da superficie a marola aparece (mais fundo = mais perto da camera).")]
    public Vector2 distanciaAbaixoDaSuperficie = new Vector2(0.05f, 1.2f);
    public Vector2 larguraFinal = new Vector2(0.6f, 1.6f);

    [Header("Ao redor do barco")]
    public bool marolasDoBarco = true;
    public Transform barco;
    public float intervaloDoBarco = 1.2f;
    public float espalhamentoX = 1.1f;
    public Vector2 larguraDoBarco = new Vector2(2.2f, 3.4f);

    [Header("Aparencia")]
    public Color cor = new Color(0.78f, 0.7f, 1f, 1f);
    [Range(0f, 1f)] public float opacidade = 0.4f;
    public float duracao = 1.8f;
    [Range(0.05f, 0.6f)] public float achatamento = 0.2f;
    public int maximo = 30;

    [Header("Desenho")]
    public int ordemDesenho = 4;
    public Material material;

    class Marola
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public float inicio, largura, forca;
    }

    readonly List<Marola> ativas = new List<Marola>();
    Sprite sprite;
    Camera cam;
    float tempoAcaso, tempoBarco;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        cam = Camera.main;
        sprite = SpritesProcedurais.Anel();

        if (barco == null)
        {
            GameObject g = GameObject.Find("Bote");
            if (g != null) barco = g.transform;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (marolasAoAcaso)
        {
            tempoAcaso -= dt;
            if (tempoAcaso <= 0f)
            {
                tempoAcaso = Random.Range(intervalo.x, intervalo.y);
                float meia = cam != null && cam.orthographic ? cam.orthographicSize * cam.aspect : 14f;
                float cx = cam != null ? cam.transform.position.x : 0f;
                float x = Random.Range(cx - meia, cx + meia);
                float y = ProfundidadeDoMar.NivelDoMar - Random.Range(distanciaAbaixoDaSuperficie.x, distanciaAbaixoDaSuperficie.y);
                Criar(new Vector2(x, y), Random.Range(larguraFinal.x, larguraFinal.y), 0.8f);
            }
        }

        if (marolasDoBarco && barco != null)
        {
            tempoBarco -= dt;
            if (tempoBarco <= 0f)
            {
                tempoBarco = intervaloDoBarco * Random.Range(0.8f, 1.2f);
                float x = barco.position.x + Random.Range(-espalhamentoX, espalhamentoX);
                Criar(new Vector2(x, ProfundidadeDoMar.NivelDoMar - 0.05f), Random.Range(larguraDoBarco.x, larguraDoBarco.y), 1f);
            }
        }

        for (int i = ativas.Count - 1; i >= 0; i--)
        {
            Marola m = ativas[i];
            float p = (Time.time - m.inicio) / duracao;

            if (p >= 1f)
            {
                Destroy(m.obj);
                ativas.RemoveAt(i);
                continue;
            }

            float aberta = 1f - (1f - p) * (1f - p); // abre rapido e desacelera
            float w = Mathf.Lerp(m.largura * 0.15f, m.largura, aberta);
            m.obj.transform.localScale = new Vector3(w, w * achatamento, 1f);

            Color c = cor;
            c.a = opacidade * m.forca * (1f - p) * Mathf.Clamp01(p / 0.1f);
            m.sr.color = c;
        }
    }

    // posicao em coordenadas do mundo; largura = tamanho final do anel
    public void Criar(Vector2 posicao, float largura = 1.5f, float forca = 1f)
    {
        if (sprite == null || ativas.Count >= maximo) return;

        Marola m = new Marola();
        m.inicio = Time.time;
        m.largura = largura;
        m.forca = forca;

        m.obj = new GameObject("Marola");
        m.obj.transform.SetParent(transform, false);
        m.obj.transform.position = new Vector3(posicao.x, posicao.y, 0f);
        m.obj.transform.localScale = new Vector3(largura * 0.15f, largura * 0.15f * achatamento, 1f);

        m.sr = m.obj.AddComponent<SpriteRenderer>();
        m.sr.sprite = sprite;
        m.sr.color = new Color(cor.r, cor.g, cor.b, 0f);
        m.sr.sortingOrder = ordemDesenho;
        if (material != null) m.sr.sharedMaterial = material;

        ativas.Add(m);
    }
}
