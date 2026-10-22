using UnityEngine;

public class BruxaVoadora : MonoBehaviour
{
    public enum Estado { Aproximando, Atacando, Recuando }

    [Header("Alvo")]
    public Transform barco;

    [Header("Aproximação")]
    public float distanciaMinima = 3f;
    public float distanciaAtaque = 3.5f;
    public float velocidadeAproximacao = 0.3f;
    public float alturaSobreBarco = 2.5f;

    [Header("Recuo")]
    public float distanciaRecuo = 10f;
    public float velocidadeRecuo = 3f;

    [Header("Suavidade")]
    public float tempoSuavizacao = 0.6f;
    public float velocidadeMaxima = 6f;

    [Header("Flutuação (voo)")]
    public float amplitudeVertical = 0.35f;
    public float frequenciaVertical = 1.6f;
    public float amplitudeHorizontal = 0.2f;
    public float frequenciaHorizontal = 0.9f;

    [Header("Visual")]
    public bool spriteOlhaParaDireita = false;

    public Estado EstadoAtual { get; private set; } = Estado.Aproximando;
    public float Lado => lado;

    float distanciaAtual;   
    float lado;
    Vector2 velocidade;    
    float faseAleatoria;    
    SpriteRenderer sr;
    EventoEquilibrioBarco eventoEquilibrio;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        eventoEquilibrio = barco.GetComponent<EventoEquilibrioBarco>();
        faseAleatoria = Random.value * 10f;

        float dx = transform.position.x - barco.position.x;
        lado = dx >= 0f ? 1f : -1f;
        distanciaAtual = Mathf.Abs(dx);
    }

    void Update()
    {
        if (barco == null) return;

        AtualizarEstado();
        MoverEFlutuar();
        VirarSprite();
    }

    void AtualizarEstado()
    {
        switch (EstadoAtual)
        {
            case Estado.Aproximando:
                distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaMinima, velocidadeAproximacao * Time.deltaTime);

                if (distanciaAtual <= distanciaAtaque && eventoEquilibrio != null && eventoEquilibrio.TentarIniciar(this))
                    EstadoAtual = Estado.Atacando;
                break;

            case Estado.Atacando:
                distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaMinima, velocidadeAproximacao * 3f * Time.deltaTime);
                break;

            case Estado.Recuando:
                distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaRecuo, velocidadeRecuo * Time.deltaTime);
                if (distanciaAtual >= distanciaRecuo - 0.05f)
                    EstadoAtual = Estado.Aproximando;
                break;
        }
    }

    void MoverEFlutuar()
    {
        Vector2 alvo = new Vector2(
            barco.position.x + lado * distanciaAtual,
            barco.position.y + alturaSobreBarco);

        float t = Time.time + faseAleatoria;
        alvo.y += Mathf.Sin(t * frequenciaVertical) * amplitudeVertical;
        alvo.x += Mathf.Sin(t * frequenciaHorizontal) * amplitudeHorizontal;

        Vector2 novaPos = Vector2.SmoothDamp(
            transform.position, alvo, ref velocidade, tempoSuavizacao, velocidadeMaxima);
        transform.position = new Vector3(novaPos.x, novaPos.y, transform.position.z);
    }

    void VirarSprite()
    {
        if (sr == null) return;
        bool barcoADireita = barco.position.x > transform.position.x;
        sr.flipX = spriteOlhaParaDireita ? !barcoADireita : barcoADireita;
    }
    public void Repelir()
    {
        EstadoAtual = Estado.Recuando;
    }

    void OnDrawGizmosSelected()
    {
        if (barco == null) return;
        Gizmos.color = Color.magenta;
        Vector3 p = new Vector3(
            barco.position.x + Mathf.Sign(transform.position.x - barco.position.x) * distanciaMinima,
            barco.position.y + alturaSobreBarco,
            0f);
        Gizmos.DrawWireSphere(p, 0.3f);
    }
}