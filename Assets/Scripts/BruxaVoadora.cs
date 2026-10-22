using System.Collections;
using System.Collections.Generic;
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

    [Header("Separacao entre bruxas")]
    public float distanciaSeparacao = 1.8f;      // distancia minima entre duas bruxas (sobe sozinha se o sprite for maior)
    public float espacamentoHorizontal = 2.2f;   // cada bruxa extra do mesmo lado fica mais longe do barco por isso
    public float espacamentoVertical = 1f;       // e vai alternando entre mais alta e mais baixa
    public float velocidadeAfastar = 12f;        // quao rapido uma bruxa sai de cima da outra

    [Header("Vida")]
    public int vidaMaxima = 3;                   // quantos acertos de ferro ela aguenta
    public float recuoAoLevarDano = 1.5f;        // cada acerto empurra a bruxa um pouco para longe do barco
    public float tamanhoBarra = 1.4f;            // largura da barrinha de vida
    public Color corAoLevarDano = new Color(1f, 0.4f, 0.4f);
    public float duracaoPiscada = 0.15f;
    public float duracaoMorte = 0.5f;

    [Header("Nivel (o GerenciadorBruxas muda isso sozinho)")]
    public float multiplicadorNivel = 1f;    // 1 = normal, 1.5 = 50% mais rapida

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

    // Todas as bruxas que existem agora (o ferro e o gerenciador usam isso)
    public static readonly List<BruxaVoadora> todas = new List<BruxaVoadora>();

    // O GerenciadorBruxas marca isso quando a bruxa nasce de um peixe-bruxa
    [HideInInspector] public bool atacarAoNascer;

    float distanciaAtual;                // distancia horizontal ate o barco
    Vector2 velocidadeAtual;             // usada pelo SmoothDamp
    float faseAleatoria;                 // para cada bruxa flutuar diferente
    float proximoCacarejo;
    SpriteRenderer sr;
    EventoEquilibrioBarco eventoEquilibrio;

    // numero de chegada: decide quem fica na frente quando ha varias bruxas do mesmo lado
    static int contadorOrdem;
    int ordem;
    float slotAtual;                     // vaga que a bruxa ocupa (0 = a mais perto do barco)

    int vidaAtual;
    bool morta;
    float fimPiscada;
    Color corOriginal = Color.white;
    BarraDeVida barra;

    void Awake()
    {
        vidaAtual = vidaMaxima;
    }

    void OnEnable()
    {
        ordem = contadorOrdem++;
        todas.Add(this);
    }

    void OnDisable()
    {
        todas.Remove(this);
    }

    void Start()
    {
        if (barco == null)
        {
            Debug.LogError("Arraste o barco no campo 'Barco' da bruxa!");
            return;
        }

        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) corOriginal = sr.color;
        barra = BarraDeVida.Criar(transform, sr, tamanhoBarra);
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
        slotAtual = MeuSlot();

        // nasceu de um peixe-bruxa: ataca assim que aparece
        if (atacarAoNascer)
        {
            ForcarAtaque();
        }
    }

    void Update()
    {
        if (barco == null || morta) return;

        RestaurarCorDepoisDaPiscada();

        AtualizarEstado();
        MoverEFlutuar();
        VirarSprite();
        CacarejarDeVezEmQuando();
    }

    void AtualizarEstado()
    {
        if (estadoAtual == Estado.Aproximando)
        {
            // quanto mais pontos o jogador tem (e quanto mais alto o nivel), mais rapido ela chega
            float velocidade = velocidadeAproximacao * (1f + ganancia * ProgressoDosPontos()) * MultiplicadorSeguro();
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
            distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaMinima, velocidadeAtaque * MultiplicadorSeguro() * Time.deltaTime);
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

        // cada bruxa ocupa uma vaga propria do mesmo lado do barco:
        // as extras ficam mais longe e alternam entre mais altas e mais baixas
        slotAtual = Mathf.MoveTowards(slotAtual, MeuSlot(), 2f * Time.deltaTime);
        float passoHorizontal = Mathf.Max(espacamentoHorizontal, DistanciaSeparacao() * 1.1f);
        alvo.x += lado * slotAtual * passoHorizontal;
        alvo.y += Mathf.PingPong(slotAtual, 1f) * espacamentoVertical;

        // move suavemente ate o alvo (em nivel alto ela responde mais rapido)
        float m = MultiplicadorSeguro();
        Vector2 novaPosicao = Vector2.SmoothDamp(
            transform.position, alvo, ref velocidadeAtual, tempoSuavizacao / m, velocidadeMaxima * m);

        transform.position = new Vector3(novaPosicao.x, novaPosicao.y, transform.position.z);

        // rede de seguranca: se mesmo assim ficarem coladas, uma sai de cima da outra
        AfastarDasOutras();
    }

    // Distancia minima entre duas bruxas: a configurada, ou a largura do sprite se for maior
    float DistanciaSeparacao()
    {
        if (sr == null) return distanciaSeparacao;
        return Mathf.Max(distanciaSeparacao, sr.bounds.size.x);
    }

    // Que vaga a bruxa ocupa do lado dela: 0 = a mais perto do barco, 1 = logo atras, e assim por diante.
    // Quem esta atacando tem prioridade; entre as demais, a que chegou primeiro fica na frente.
    int MeuSlot()
    {
        int slot = 0;
        bool euAtaco = estadoAtual == Estado.Atacando;

        for (int i = 0; i < todas.Count; i++)
        {
            BruxaVoadora outra = todas[i];
            if (outra == this || outra.lado != lado) continue;
            if (outra.estadoAtual == Estado.Recuando) continue;

            bool outraAtaca = outra.estadoAtual == Estado.Atacando;

            if (outraAtaca != euAtaco)
            {
                if (outraAtaca) slot++;       // quem ataca fica na frente
            }
            else if (outra.ordem < ordem)
            {
                slot++;                       // quem chegou primeiro fica na frente
            }
        }

        return slot;
    }

    // Se duas bruxas ficarem mais perto que a distancia minima, esta se afasta da outra.
    // A que esta atacando nao sai do lugar: as outras desviam dela.
    void AfastarDasOutras()
    {
        if (estadoAtual == Estado.Atacando) return;

        Vector2 posicao = transform.position;

        for (int i = 0; i < todas.Count; i++)
        {
            BruxaVoadora outra = todas[i];
            if (outra == this) continue;

            float minima = Mathf.Max(DistanciaSeparacao(), outra.DistanciaSeparacao());
            Vector2 diferenca = posicao - (Vector2)outra.transform.position;
            float distancia = diferenca.magnitude;
            if (distancia >= minima) continue;

            // exatamente no mesmo lugar? foge numa direcao propria de cada bruxa
            Vector2 direcao;
            if (distancia > 0.01f)
            {
                direcao = diferenca / distancia;
            }
            else
            {
                direcao = new Vector2(Mathf.Cos(faseAleatoria), Mathf.Sin(faseAleatoria));
            }

            // se a outra esta atacando (parada), eu desvio tudo; senao dividimos o desvio
            float parte = 0.5f;
            if (outra.estadoAtual == Estado.Atacando)
            {
                parte = 1f;
            }

            float passo = Mathf.Clamp01(velocidadeAfastar * Time.deltaTime);
            posicao += direcao * (minima - distancia) * parte * passo;
        }

        transform.position = new Vector3(posicao.x, posicao.y, transform.position.z);
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

    // nunca deixa o multiplicador chegar a zero (evitaria divisao por zero)
    float MultiplicadorSeguro()
    {
        return Mathf.Max(0.1f, multiplicadorNivel);
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

    // chamado pelo ProjetilFerro quando o ferro acerta a bruxa
    public void AcertadaPorFerro()
    {
        AcertadaPorFerro(1);
    }

    public void AcertadaPorFerro(int dano)
    {
        if (morta) return;

        vidaAtual = Mathf.Max(0, vidaAtual - dano);
        if (barra != null) barra.DefinirProporcao((float)vidaAtual / Mathf.Max(1, vidaMaxima));

        // pisca vermelha
        fimPiscada = Time.time + duracaoPiscada;
        if (sr != null) sr.color = corAoLevarDano;

        if (vidaAtual <= 0)
        {
            Morrer();
            return;
        }

        // sobreviveu: o acerto empurra ela um pouco para tras
        distanciaAtual = Mathf.Min(distanciaAtual + recuoAoLevarDano, distanciaRecuo);
    }

    void Morrer()
    {
        if (morta) return;
        morta = true;

        // se ela estava atacando, o evento de equilibrio termina com sucesso
        if (estadoAtual == Estado.Atacando && eventoEquilibrio != null)
        {
            eventoEquilibrio.EncerrarComFerro(this);
        }

        todas.Remove(this);   // ninguem mais mira, desvia ou conta uma bruxa morta

        if (barra != null) Destroy(barra.gameObject);

        // conta para o nivel (o GerenciadorBruxas sobe o nivel a cada N mortes)
        if (GerenciadorBruxas.instancia != null)
        {
            GerenciadorBruxas.instancia.RegistrarMorte();
        }

        StartCoroutine(AnimacaoDeMorte());
    }

    // cai e some aos poucos, depois destroi
    IEnumerator AnimacaoDeMorte()
    {
        Vector3 posicaoInicial = transform.position;
        float tempo = 0f;

        while (tempo < duracaoMorte)
        {
            tempo += Time.deltaTime;
            float k = Mathf.Clamp01(tempo / Mathf.Max(0.01f, duracaoMorte));

            transform.position = posicaoInicial + Vector3.down * (k * 1.5f);

            if (sr != null)
            {
                Color cor = corAoLevarDano;
                cor.a = 1f - k;
                sr.color = cor;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    void RestaurarCorDepoisDaPiscada()
    {
        if (sr == null || fimPiscada <= 0f) return;

        if (Time.time >= fimPiscada)
        {
            sr.color = corOriginal;
            fimPiscada = 0f;
        }
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