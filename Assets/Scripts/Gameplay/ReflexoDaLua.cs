using UnityEngine;

// Coluna de reflexo da lua na agua: tracos horizontais que balancam e cintilam,
// ficando mais largos e mais fracos conforme descem.
// Coloque em um GameObject vazio (posicao 0,0,0 e escala 1).
public class ReflexoDaLua : MonoBehaviour
{
    [Header("Posicao")]
    public Transform lua;
    [Tooltip("Usado se a lua nao for encontrada.")]
    public float xDaLua = 12.5f;

    [Header("Forma")]
    public int tracos = 14;
    public float profundidade = 3.2f;
    public Vector2 largura = new Vector2(0.5f, 2.6f);
    public float espessura = 0.07f;

    [Header("Movimento")]
    public float balanco = 0.18f;
    public float velocidadeDoBalanco = 1.4f;
    public float velocidadeDoBrilho = 1.8f;

    [Header("Aparencia")]
    public Color cor = new Color(1f, 0.55f, 0.32f, 1f);
    [Range(0f, 1f)] public float opacidade = 0.5f;

    [Header("Desenho")]
    public int ordemDesenho = 5;
    [Tooltip("Opcional: material Sprite-Unlit-Default.")]
    public Material material;

    SpriteRenderer[] itens;

    void Start()
    {
        if (lua == null)
        {
            GameObject g = GameObject.Find("Lua");
            if (g != null) lua = g.transform;
        }

        Sprite sprite = SpritesProcedurais.Brilho();
        itens = new SpriteRenderer[tracos];

        for (int i = 0; i < tracos; i++)
        {
            GameObject obj = new GameObject("Reflexo_" + i);
            obj.transform.SetParent(transform, false);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = ordemDesenho;
            if (material != null) sr.sharedMaterial = material;
            itens[i] = sr;
        }
    }

    void Update()
    {
        if (itens == null) return;

        float t = Time.time;
        float x0 = lua != null ? lua.position.x : xDaLua;

        for (int i = 0; i < itens.Length; i++)
        {
            float u = (i + 0.5f) / itens.Length;

            // espacamento cresce com a profundidade (perspectiva)
            float y = ProfundidadeDoMar.NivelDoMar - 0.08f - Mathf.Pow(u, 1.4f) * profundidade;
            float x = x0 + Mathf.Sin(t * velocidadeDoBalanco + i * 1.7f) * balanco * (0.4f + u);

            float ruido = Mathf.PerlinNoise(i * 7.3f, t * velocidadeDoBrilho);
            float cintila = Mathf.InverseLerp(0.25f, 0.75f, ruido);
            float w = Mathf.Lerp(largura.x, largura.y, u) * (0.7f + 0.5f * ruido);

            Color c = cor;
            c.a = opacidade * Mathf.Pow(1f - u, 0.8f) * cintila;

            itens[i].color = c;
            itens[i].transform.position = new Vector3(x, y, 0f);
            itens[i].transform.localScale = new Vector3(w, espessura * (1f + u * 1.5f), 1f);
        }
    }
}
