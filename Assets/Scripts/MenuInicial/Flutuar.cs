using UnityEngine;

public class Flutuar : MonoBehaviour
{
    [Header("Subir e descer")]
    public float amplitudeY = 12f;
    public float velocidadeY = 1.4f;

    [Header("Ir e voltar")]
    public float amplitudeX = 0f;
    public float velocidadeX = 0.9f;

    [Header("Balancar")]
    public float grausBalanco = 2f;
    public float velocidadeBalanco = 1.1f;

    [Header("Pulsar")]
    [Range(0f, 0.3f)] public float pulso = 0.03f;
    public float velocidadePulso = 1.8f;

    [Header("Inicio")]
    public float atrasoInicio = 0f;
    public bool fasesAleatorias = true;

    float fase;
    float inicio;
    Vector3 offAnterior;
    float rotAnterior;
    float escAnterior = 1f;

    void Awake()
    {
        if (fasesAleatorias) fase = Random.value * 20f;
    }

    void OnEnable()
    {
        inicio = Time.unscaledTime;
    }

    void Update()
    {
        if (Time.unscaledTime - inicio < atrasoInicio) return;

        float t = Time.unscaledTime + fase;

        Vector3 off = new Vector3(
            Mathf.Sin(t * velocidadeX) * amplitudeX,
            Mathf.Sin(t * velocidadeY) * amplitudeY,
            0f);
        float rot = Mathf.Sin(t * velocidadeBalanco) * grausBalanco;
        float esc = 1f + Mathf.Sin(t * velocidadePulso) * pulso;

        transform.localPosition += off - offAnterior;
        transform.localRotation = Quaternion.Euler(0f, 0f, transform.localEulerAngles.z + rot - rotAnterior);
        transform.localScale *= esc / escAnterior;

        offAnterior = off;
        rotAnterior = rot;
        escAnterior = esc;
    }
}