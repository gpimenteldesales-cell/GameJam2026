using UnityEngine;

// Da profundidade ao mar sem mexer na arte:
//  - degrade escuro que cresce com a profundidade (peixes e plantas mais fundos ficam mais escuros)
//  - faixa de luz roxa clara logo abaixo da superficie
// Tambem guarda o nivel da agua (NivelDoMar), usado pelos outros scripts de ambiente.
// Coloque em um GameObject vazio (posicao 0,0,0 e escala 1).
// No Scene view aparece uma linha amarela no nivel da agua: ajuste ySuperficie ate ela
// ficar em cima da linha do horizonte do mar.
public class ProfundidadeDoMar : MonoBehaviour
{
    public static float NivelDoMar = -2f;

    [Header("Nivel da agua (y da linha do mar)")]
    public float ySuperficie = -2f;

    [Header("Escurecer com a profundidade")]
    public Color corDoFundo = new Color(0.04f, 0.02f, 0.14f, 1f);
    [Range(0f, 1f)] public float intensidade = 0.6f;
    [Tooltip("Maior = o escuro so aparece mais para o fundo.")]
    public float curva = 1.4f;

    [Header("Luz perto da superficie")]
    public bool faixaDeLuz = true;
    public Color corDaLuz = new Color(0.55f, 0.4f, 0.9f, 1f);
    [Range(0f, 1f)] public float intensidadeDaLuz = 0.28f;
    public float alturaDaLuz = 2.2f;

    [Header("Desenho")]
    [Tooltip("Tem que ficar acima da ordem do mar (3) para escurecer o que esta dentro d'agua.")]
    public int ordemDesenho = 4;
    public Material material;

    void Awake()
    {
        NivelDoMar = ySuperficie;
    }

    void Start()
    {
        Camera cam = Camera.main;
        float meia = cam != null && cam.orthographic ? cam.orthographicSize * cam.aspect : 14f;
        float cx = cam != null ? cam.transform.position.x : 0f;
        float yBase = cam != null ? cam.transform.position.y - cam.orthographicSize - 1f : -9f;
        float largura = meia * 2f + 2f;

        Sprite grad = SpritesProcedurais.Gradiente(128, curva);
        Vector2 tam = grad.bounds.size;

        float altura = Mathf.Max(0.1f, ySuperficie - yBase);
        Criar("EscurecerFundo", grad, corDoFundo, intensidade, false,
              new Vector2(cx, (ySuperficie + yBase) * 0.5f),
              new Vector2(largura / tam.x, altura / tam.y));

        if (faixaDeLuz)
        {
            Sprite suave = SpritesProcedurais.Gradiente(128, 2f);
            Criar("LuzDaSuperficie", suave, corDaLuz, intensidadeDaLuz, true,
                  new Vector2(cx, ySuperficie - alturaDaLuz * 0.5f),
                  new Vector2(largura / tam.x, alturaDaLuz / tam.y));
        }
    }

    void Criar(string nome, Sprite sprite, Color cor, float alfa, bool inverter, Vector2 pos, Vector2 escala)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(transform, false);
        obj.transform.position = new Vector3(pos.x, pos.y, 0f);
        obj.transform.localScale = new Vector3(escala.x, escala.y, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipY = inverter;
        cor.a = alfa;
        sr.color = cor;
        sr.sortingOrder = ordemDesenho;
        if (material != null) sr.sharedMaterial = material;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-40f, ySuperficie, 0f), new Vector3(40f, ySuperficie, 0f));
    }
}
