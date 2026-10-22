using UnityEngine;

public class BalancoDoBarco : MonoBehaviour
{
    public EventoEquilibrioBarco evento;

    [Header("Sobe e desce")]
    public float amplitudeVertical = 0.08f;
    public float frequenciaVertical = 1.3f;

    [Header("Inclinacao")]
    public float amplitudeInclinacao = 1.5f;
    public float frequenciaInclinacao = 0.9f;

    [Header("Geral")]
    public float suavidade = 3f;

    Vector3 deslocamentoAplicado;
    float anguloAplicado;
    float intensidade = 1f;

    void Awake()
    {
        if (evento == null) evento = GetComponent<EventoEquilibrioBarco>();
    }

    void LateUpdate()
    {
        Transform alvo = transform;
        if (evento != null && evento.visualBarco != null) alvo = evento.visualBarco;

        bool parar = evento != null && evento.virou;
        intensidade = Mathf.MoveTowards(intensidade, parar ? 0f : 1f, suavidade * Time.deltaTime);

        Vector3 deslocamento = Vector3.up * Mathf.Sin(Time.time * frequenciaVertical) * amplitudeVertical * intensidade;
        alvo.position += deslocamento - deslocamentoAplicado;
        deslocamentoAplicado = deslocamento;

        float angulo = Mathf.Sin(Time.time * frequenciaInclinacao + 1.3f) * amplitudeInclinacao * intensidade;

        if (evento != null)
        {
            alvo.rotation = alvo.rotation * Quaternion.Euler(0f, 0f, angulo);
        }
        else
        {
            alvo.Rotate(0f, 0f, angulo - anguloAplicado);
            anguloAplicado = angulo;
        }
    }
}
