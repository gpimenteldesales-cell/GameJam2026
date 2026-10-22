using UnityEngine;

public class EntradaUI : MonoBehaviour
{
    public float atraso = 0f;
    public float duracao = 0.7f;
    public Vector2 deslocamentoInicial = new Vector2(0f, 400f);
    [Range(0f, 1.5f)] public float escalaInicial = 0.6f;
    public float giroInicial = 0f;

    Vector3 posFinal;
    Vector3 escalaFinal;
    float rotFinal;
    float tempo;

    void Awake()
    {
        posFinal = transform.localPosition;
        escalaFinal = transform.localScale;
        rotFinal = transform.localEulerAngles.z;
    }

    void OnEnable()
    {
        tempo = 0f;
        Aplicar(0f);
    }

    void Update()
    {
        if (tempo >= atraso + duracao) return;

        tempo += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01((tempo - atraso) / Mathf.Max(0.01f, duracao));
        Aplicar(t);
    }

    void Aplicar(float t)
    {
        float k = OutBack(t);

        transform.localPosition = posFinal + (Vector3)(deslocamentoInicial * (1f - k));
        transform.localScale = escalaFinal * Mathf.LerpUnclamped(escalaInicial, 1f, k);
        transform.localRotation = Quaternion.Euler(0f, 0f, rotFinal + giroInicial * (1f - k));
    }

    public static float OutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}