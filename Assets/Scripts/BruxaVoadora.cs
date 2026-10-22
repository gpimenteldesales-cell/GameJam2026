using UnityEngine;

// A bruxa voa ate o barco e tenta virar ele.
// Se o jogador vencer o evento de equilibrio, ela recua.
public class BruxaVoadora : MonoBehaviour
{
    // Os 3 estados da bruxa
    public enum Estado { Aproximando, Atacando, Recuando }

    [Header("Alvo")]
    public Transform barco;

    [Header("Aproximacao")]
    public float distanciaMinima = 3f;
    public float distanciaAtaque = 3.5f;
    public float velocidadeAproximacao = 0.3f;
    public float alturaSobreBarco = 2.5f;

    [Header("Ataque")]
    public float velocidadeAtaque = 2f;      // velocidade ao chegar para atacar
    public float ganancia = 2f;              // quanto mais pontos, mais rapida (0 = desliga)

    [Header("Recuo")]
    public float distanciaRecuo = 10f;
    public float velocidadeRecuo = 3f;

    [Header("Suavidade")]
    public float tempoSuavizacao = 0.6f;
    public float velocidadeMaxima = 6f;

    [Header("Flutuacao (voo)")]
    public float amplitudeVertical = 0.35f;
    public float frequenciaVertical = 1.6f;
    public float amplitudeHorizontal = 0.2f;
    public float frequenciaHorizontal = 0.9f;

    [Header("Visual")]
    public bool spriteOlhaParaDireita = false;

    [Header("Som (opcional)")]
    public AudioSource audioFonte;
    public AudioClip somCacarejo;
    public float cacarejoMinimo = 4f;        // intervalo entre cacarejos
    public float cacarejoMaximo = 8f;

    // Estado atual da bruxa (aparece no Inspector, bom pra testar)
    public Estado estadoAtual = Estado.Aproximando;

    // Lado em que a bruxa esta: 1 = direita do barco, -1 = esquerda
    [HideInInspector] public float lado;

    float distanciaAtual;                // distancia horizontal ate o barco
    Vector2 velocidadeAtual;             // usada pelo SmoothDamp
    float faseAleatoria;                 // para cada bruxa flutuar diferente
    float proximoCacarejo;
    SpriteRenderer sr;
    EventoEquilibrioBarco eventoEquilibrio;

    void Start()
    {
        if (barco == null)
        {
            Debug.LogError("Arraste o barco no campo 'Barco' da bruxa!");
            return;
        }

        sr = GetComponentInChildren<SpriteRenderer>();
        eventoEquilibrio = barco.GetComponent<EventoEquilibrioBarco>();
        faseAleatoria = Random.value * 10f;
        proximoCacarejo = Time.time + Random.Range(cacarejoMinimo, cacarejoMaximo);

        // descobre de que lado do barco a bruxa comeca
        float dx = transform.position.x - barco.position.x;
        if (dx >= 0f)
        {
            lado = 1f;
        }
        else
        {
            lado = -1f;
        }
        distanciaAtual = Mathf.Abs(dx);
    }

    void Update()
    {
        if (barco == null) return;

        AtualizarEstado();
        MoverEFlutuar();
        VirarSprite();
        CacarejarDeVezEmQuando();
    }

    void AtualizarEstado()
    {
        if (estadoAtual == Estado.Aproximando)
        {
            // quanto mais pontos o jogador tem, mais rapido ela chega
            float velocidade = velocidadeAproximacao * (1f + ganancia * ProgressoDosPontos());
            distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaMinima, velocidade * Time.deltaTime);

            // perto o bastante? tenta comecar o evento de equilibrio
            if (distanciaAtual <= distanciaAtaque && eventoEquilibrio != null)
            {
                if (eventoEquilibrio.TentarIniciar(this))
                {
                    estadoAtual = Estado.Atacando;
                }
            }
        }
        else if (estadoAtual == Estado.Atacando)
        {
            // durante o ataque chega mais rapido
            distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaMinima, velocidadeAtaque * Time.deltaTime);
        }
        else if (estadoAtual == Estado.Recuando)
        {
            distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaRecuo, velocidadeRecuo * Time.deltaTime);

            // ja recuou o bastante? volta a se aproximar
            if (distanciaAtual >= distanciaRecuo - 0.05f)
            {
                estadoAtual = Estado.Aproximando;
            }
        }
    }

    void MoverEFlutuar()
    {
        // posicao onde a bruxa quer ficar
        Vector2 alvo = new Vector2(
            barco.position.x + lado * distanciaAtual,
            barco.position.y + alturaSobreBarco);

        // balanco de voo (sobe e desce, vai e volta)
        float t = Time.time + faseAleatoria;
        alvo.y += Mathf.Sin(t * frequenciaVertical) * amplitudeVertical;
        alvo.x += Mathf.Sin(t * frequenciaHorizontal) * amplitudeHorizontal;

        // move suavemente ate o alvo
        Vector2 novaPosicao = Vector2.SmoothDamp(
            transform.position, alvo, ref velocidadeAtual, tempoSuavizacao, velocidadeMaxima);

        transform.position = new Vector3(novaPosicao.x, novaPosicao.y, transform.position.z);
    }

    // faz a bruxa olhar para o barco
    void VirarSprite()
    {
        if (sr == null) return;

        bool barcoEstaADireita = barco.position.x > transform.position.x;

        if (spriteOlhaParaDireita)
        {
            sr.flipX = !barcoEstaADireita;
        }
        else
        {
            sr.flipX = barcoEstaADireita;
        }
    }

    // 0 a 1: quanto da meta de pontos o jogador ja tem
    float ProgressoDosPontos()
    {
        if (GerenciadorJogo.instancia == null) return 0f;
        return GerenciadorJogo.instancia.ProgressoMeta();
    }

    // de vez em quando ela cacareja enquanto se aproxima (aviso pro jogador)
    void CacarejarDeVezEmQuando()
    {
        if (estadoAtual != Estado.Aproximando) return;
        if (Time.time < proximoCacarejo) return;

        proximoCacarejo = Time.time + Random.Range(cacarejoMinimo, cacarejoMaximo);
        TocarCacarejo();
    }

    void TocarCacarejo()
    {
        if (audioFonte != null && somCacarejo != null)
        {
            audioFonte.PlayOneShot(somCacarejo);
        }
    }

    // chamado pelo evento de equilibrio quando o jogador vence
    public void Repelir()
    {
        estadoAtual = Estado.Recuando;
    }

    // chamado quando o jogador pesca um peixe-bruxa: ela ataca agora!
    public void ForcarAtaque()
    {
        if (eventoEquilibrio == null) return;

        if (eventoEquilibrio.TentarIniciar(this))
        {
            estadoAtual = Estado.Atacando;

            // ela chega de uma vez, para o ataque nao comecar de longe
            distanciaAtual = Mathf.Min(distanciaAtual, distanciaAtaque + 1f);
            TocarCacarejo();
        }
    }

    // desenha uma bolinha no editor mostrando onde a bruxa vai ficar
    void OnDrawGizmosSelected()
    {
        if (barco == null) return;

        Gizmos.color = Color.magenta;
        Vector3 ponto = new Vector3(
            barco.position.x + Mathf.Sign(transform.position.x - barco.position.x) * distanciaMinima,
            barco.position.y + alturaSobreBarco,
            0f);
        Gizmos.DrawWireSphere(ponto, 0.3f);
    }
}
