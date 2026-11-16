using System.Collections.Generic;
using UnityEngine;

// Morcegos que cruzam o ceu de vez em quando. Alguns passam na frente da lua.
// Profundidade: morcego longe = menor, mais lento e mais claro (some na cor do ceu).
// Coloque em um GameObject vazio (posicao 0,0,0 e escala 1).
public class MorcegosVoando : MonoBehaviour
{
    [Header("Aparicao")]
    public Vector2 intervalo = new Vector2(4f, 10f);
    public int maximo = 3;
    public float primeiroEm = 2f;

    [Header("Voo")]
    public Vector2 alturaDoVoo = new Vector2(2.8f, 7.2f);
    [Range(0f, 1f)] public float chanceDePassarPelaLua = 0.3f;
    public Transform lua;
    public Vector2 velocidade = new Vector2(2f, 4.5f);
    public Vector2 amplitudeDaOnda = new Vector2(0.3f, 0.8f);
    public Vector2 frequenciaDaOnda = new Vector2(0.8f, 1.6f);
    public Vector2 batidasPorSegundo = new Vector2(7f, 10f);

    [Header("Aparencia (longe = menor e mais claro)")]
    public Vector2 tamanho = new Vector2(0.55f, 1.1f);
    public Color corPerto = new Color(0.08f, 0.04f, 0.14f, 1f);
    public Color corLonge = new Color(0.29f, 0.18f, 0.47f, 1f);

    [Header("Desenho")]
    public int ordemDesenho = 2;
    public Material material;

    class Morcego
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public float dir, vel, x, yBase, amp, freq, fase, batida, fasBatida, escala;
    }

    readonly List<Morcego> morcegos = new List<Morcego>();
    Sprite[] quadros;
    Camera cam;
    float proximo;

    void Start()
    {
        cam = Camera.main;
        quadros = SpritesProcedurais.Morcego();
        proximo = primeiroEm;

        if (lua == null)
        {
            GameObject g = GameObject.Find("Lua");
            if (g != null) lua = g.transform;
        }
    }

    void Update()
    {
        proximo -= Time.deltaTime;
        if (proximo <= 0f)
        {
            if (morcegos.Count < maximo) Criar();
            proximo = Random.Range(intervalo.x, intervalo.y);
        }

        float meia = MeiaLargura();
        float cx = CentroX();
        // acompanha o fundo: se o parallax desacelera (ex.: ao perder), eles tambem
        float mult = Mathf.Max(0f, Paralaxe.multiplicadorGlobal);

        for (int i = morcegos.Count - 1; i >= 0; i--)
        {
            Morcego b = morcegos[i];

            b.x += b.dir * b.vel * mult * Time.deltaTime;

            float fase = Time.time * b.freq + b.fase;
            float y = b.yBase + Mathf.Sin(fase) * b.amp;
            float vy = Mathf.Cos(fase) * b.freq * b.amp;

            b.obj.transform.position = new Vector3(b.x, y, 0f);
            b.obj.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vy, b.vel) * Mathf.Rad2Deg * 0.5f * b.dir);

            int quadro = (int)(Time.time * b.batida + b.fasBatida) % 2;
            b.sr.sprite = quadros[quadro];

            bool saiu = (b.dir > 0f && b.x > cx + meia + 2f) || (b.dir < 0f && b.x < cx - meia - 2f);
            if (saiu)
            {
                Destroy(b.obj);
                morcegos.RemoveAt(i);
            }
        }
    }

    void Criar()
    {
        float meia = MeiaLargura();
        float cx = CentroX();
        float profundidade = Random.value; // 0 = perto, 1 = longe

        Morcego b = new Morcego();
        b.dir = Random.value < 0.5f ? -1f : 1f;
        b.x = b.dir > 0f ? cx - meia - 1.5f : cx + meia + 1.5f;
        b.yBase = (lua != null && Random.value < chanceDePassarPelaLua)
            ? lua.position.y + Random.Range(-0.9f, 0.9f)
            : Random.Range(alturaDoVoo.x, alturaDoVoo.y);
        b.vel = Mathf.Lerp(velocidade.y, velocidade.x, profundidade);
        b.amp = Random.Range(amplitudeDaOnda.x, amplitudeDaOnda.y);
        b.freq = Random.Range(frequenciaDaOnda.x, frequenciaDaOnda.y);
        b.fase = Random.value * 10f;
        b.batida = Random.Range(batidasPorSegundo.x, batidasPorSegundo.y);
        b.fasBatida = Random.value * 2f;
        b.escala = Mathf.Lerp(tamanho.y, tamanho.x, profundidade);

        b.obj = new GameObject("Morcego");
        b.obj.transform.SetParent(transform, false);
        b.obj.transform.localScale = new Vector3(b.escala, b.escala, 1f);

        b.sr = b.obj.AddComponent<SpriteRenderer>();
        b.sr.sprite = quadros[0];
        b.sr.color = Color.Lerp(corPerto, corLonge, profundidade * 0.8f);
        b.sr.sortingOrder = ordemDesenho;
        if (material != null) b.sr.sharedMaterial = material;

        b.obj.transform.position = new Vector3(b.x, b.yBase, 0f);
        morcegos.Add(b);
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
