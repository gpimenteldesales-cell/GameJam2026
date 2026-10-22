using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BruxaVoadora : MonoBehaviour
{
    public enum Estado { Aproximando, Atacando, Recuando }

    [Header("Alvo")]
    public Transform barco;

    [Header("Aproximacao")]
    public float distanciaMinima = 3f;
    public float distanciaAtaque = 3.5f;
    public float velocidadeAproximacao = 0.3f;
    public float alturaSobreBarco = 2.5f;

    [Header("Ataque")]
    public float velocidadeAtaque = 2f;
    public float ganancia = 2f;

    [Header("Recuo")]
    public float distanciaRecuo = 10f;
    public float velocidadeRecuo = 3f;

    [Header("Separacao entre bruxas")]
    public float distanciaSeparacao = 1.8f;
    public float espacamentoHorizontal = 2.2f;
    public float espacamentoVertical = 1f;
    public float velocidadeAfastar = 12f;

    [Header("Vida")]
    public int vidaMaxima = 3;
    public float recuoAoLevarDano = 1.5f;
    public float tamanhoBarra = 1.4f;
    public Color corAoLevarDano = new Color(1f, 0.4f, 0.4f);
    public float duracaoPiscada = 0.15f;
    public float duracaoMorte = 0.5f;

    [Header("Nivel (o GerenciadorBruxas muda isso sozinho)")]
    public float multiplicadorNivel = 1f;

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
    public float cacarejoMinimo = 4f;
    public float cacarejoMaximo = 8f;

    [Header("Sons da bruxa")]
    public AudioClip somRisoAparecer;
    public AudioClip somRisoAtaque;
    public AudioClip somAcertada;
    [Range(0f, 1f)] public float volumeSons = 1f;

    public Estado estadoAtual = Estado.Aproximando;

    [HideInInspector] public float lado;

    public static readonly List<BruxaVoadora> todas = new List<BruxaVoadora>();

    [HideInInspector] public bool atacarAoNascer;

    float distanciaAtual;
    Vector2 velocidadeAtual;
    float faseAleatoria;
    float proximoCacarejo;
    SpriteRenderer sr;
    EventoEquilibrioBarco eventoEquilibrio;

    static int contadorOrdem;
    int ordem;
    float slotAtual;

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

        if (!atacarAoNascer)
        {
            GerenciadorSons.Tocar(somRisoAparecer, volumeSons, "Bruxa: riso ao aparecer");
        }

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
            float velocidade = velocidadeAproximacao * (1f + ganancia * ProgressoDosPontos()) * MultiplicadorSeguro();
            distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaMinima, velocidade * Time.deltaTime);

            if (distanciaAtual <= distanciaAtaque && eventoEquilibrio != null)
            {
                if (eventoEquilibrio.TentarIniciar(this))
                {
                    estadoAtual = Estado.Atacando;
                    TocarRisoAtaque();
                }
            }
        }
        else if (estadoAtual == Estado.Atacando)
        {
            distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaMinima, velocidadeAtaque * MultiplicadorSeguro() * Time.deltaTime);
        }
        else if (estadoAtual == Estado.Recuando)
        {
            distanciaAtual = Mathf.MoveTowards(distanciaAtual, distanciaRecuo, velocidadeRecuo * Time.deltaTime);

            if (distanciaAtual >= distanciaRecuo - 0.05f)
            {
                estadoAtual = Estado.Aproximando;
            }
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

        slotAtual = Mathf.MoveTowards(slotAtual, MeuSlot(), 2f * Time.deltaTime);
        float passoHorizontal = Mathf.Max(espacamentoHorizontal, DistanciaSeparacao() * 1.1f);
        alvo.x += lado * slotAtual * passoHorizontal;
        alvo.y += Mathf.PingPong(slotAtual, 1f) * espacamentoVertical;

        float m = MultiplicadorSeguro();
        Vector2 novaPosicao = Vector2.SmoothDamp(
            transform.position, alvo, ref velocidadeAtual, tempoSuavizacao / m, velocidadeMaxima * m);

        transform.position = new Vector3(novaPosicao.x, novaPosicao.y, transform.position.z);

        AfastarDasOutras();
    }

    float DistanciaSeparacao()
    {
        if (sr == null) return distanciaSeparacao;
        return Mathf.Max(distanciaSeparacao, sr.bounds.size.x);
    }

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
                if (outraAtaca) slot++;
            }
            else if (outra.ordem < ordem)
            {
                slot++;
            }
        }

        return slot;
    }

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

            Vector2 direcao;
            if (distancia > 0.01f)
            {
                direcao = diferenca / distancia;
            }
            else
            {
                direcao = new Vector2(Mathf.Cos(faseAleatoria), Mathf.Sin(faseAleatoria));
            }

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

    float MultiplicadorSeguro()
    {
        return Mathf.Max(0.1f, multiplicadorNivel);
    }

    float ProgressoDosPontos()
    {
        if (GerenciadorJogo.instancia == null) return 0f;
        return GerenciadorJogo.instancia.ProgressoMeta();
    }

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

    void TocarRisoAtaque()
    {
        GerenciadorSons.Tocar(somRisoAtaque, volumeSons, "Bruxa: riso ao atacar");
    }

    public void Repelir()
    {
        estadoAtual = Estado.Recuando;
    }

    public void AcertadaPorFerro()
    {
        AcertadaPorFerro(1);
    }

    public void AcertadaPorFerro(int dano)
    {
        if (morta) return;

        GerenciadorSons.Tocar(somAcertada, volumeSons, "Bruxa: som ao ser acertada");
        VozPescador.FalarAcerto();

        vidaAtual = Mathf.Max(0, vidaAtual - dano);
        if (barra != null) barra.DefinirProporcao((float)vidaAtual / Mathf.Max(1, vidaMaxima));

        fimPiscada = Time.time + duracaoPiscada;
        if (sr != null) sr.color = corAoLevarDano;

        if (vidaAtual <= 0)
        {
            Morrer();
            return;
        }

        distanciaAtual = Mathf.Min(distanciaAtual + recuoAoLevarDano, distanciaRecuo);
    }

    void Morrer()
    {
        if (morta) return;
        morta = true;

        if (estadoAtual == Estado.Atacando && eventoEquilibrio != null)
        {
            eventoEquilibrio.EncerrarComFerro(this);
        }

        todas.Remove(this);

        if (barra != null) Destroy(barra.gameObject);

        if (GerenciadorBruxas.instancia != null)
        {
            GerenciadorBruxas.instancia.RegistrarMorte();
        }

        StartCoroutine(AnimacaoDeMorte());
    }

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

    public void ForcarAtaque()
    {
        if (eventoEquilibrio == null) return;

        if (eventoEquilibrio.TentarIniciar(this))
        {
            estadoAtual = Estado.Atacando;

            distanciaAtual = Mathf.Min(distanciaAtual, distanciaAtaque + 1f);

            if (somRisoAtaque != null)
            {
                TocarRisoAtaque();
            }
            else
            {
                TocarCacarejo();
            }
        }
    }

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