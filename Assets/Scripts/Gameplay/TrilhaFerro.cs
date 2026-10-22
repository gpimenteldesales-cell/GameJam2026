using UnityEngine;

// Coloque este script no objeto do ferro (ou no prefab).
// O rastro liga sozinho quando o ferro se move rapido (arremesso)
// e desliga quando ele fica parado ou esta sendo segurado devagar.
public class TrilhaFerro : MonoBehaviour
{
    [Header("Rastro")]
    public float tempoVida = 0.35f;
    public float larguraInicial = 0.25f;
    public Color corInicio = new Color(1f, 1f, 1f, 0.8f);
    public Color corFim = new Color(1f, 0.9f, 0.3f, 0f);
    public int ordemDesenho = 39;

    [Header("Quando aparecer")]
    public float velocidadeMinima = 4f;

    TrailRenderer trilha;
    Vector3 ultimaPosicao;
    bool emitindo;

    void Awake()
    {
        trilha = GetComponent<TrailRenderer>();
        if (trilha == null) trilha = gameObject.AddComponent<TrailRenderer>();

        trilha.material = new Material(Shader.Find("Sprites/Default"));
        trilha.time = tempoVida;
        trilha.minVertexDistance = 0.05f;
        trilha.numCapVertices = 4;
        trilha.sortingOrder = ordemDesenho;
        trilha.alignment = LineAlignment.View;

        AnimationCurve largura = new AnimationCurve();
        largura.AddKey(0f, larguraInicial);
        largura.AddKey(1f, 0f);
        trilha.widthCurve = largura;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(corInicio, 0f), new GradientColorKey(corFim, 1f) },
            new[] { new GradientAlphaKey(corInicio.a, 0f), new GradientAlphaKey(corFim.a, 1f) });
        trilha.colorGradient = grad;

        trilha.emitting = false;
        ultimaPosicao = transform.position;
    }

    void OnEnable()
    {
        ultimaPosicao = transform.position;
        emitindo = false;
        if (trilha != null)
        {
            trilha.Clear();
            trilha.emitting = false;
        }
    }

    void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;

        float velocidade = (transform.position - ultimaPosicao).magnitude / Time.deltaTime;
        ultimaPosicao = transform.position;

        bool deveEmitir = velocidade >= velocidadeMinima;

        if (deveEmitir && !emitindo)
        {
            trilha.Clear();
        }

        emitindo = deveEmitir;
        trilha.emitting = emitindo;
    }
}
