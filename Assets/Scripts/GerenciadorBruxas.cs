using UnityEngine;
using TMPro;

// Envia as bruxas em niveis.
//  - uma bruxa nova a cada "intervaloSpawn" segundos (20 s)
//  - a cada "bruxasParaSubirNivel" bruxas MORTAS o nivel sobe
//    e TODAS as bruxas (as que ja existem e as novas) ficam mais rapidas
//    (opcional: o nivel tambem pode subir com o tempo, marcando "subirNivelPorTempo")
// Coloque em UM objeto vazio da cena (ex: "GerenciadorBruxas").
public class GerenciadorBruxas : MonoBehaviour
{
    // Qualquer script pode usar: GerenciadorBruxas.instancia
    public static GerenciadorBruxas instancia;

    [Header("Ligacoes")]
    public GameObject prefabBruxa;
    public Transform barco;

    [Header("Nascimento")]
    public float intervaloSpawn = 20f;            // segundos entre uma bruxa e outra
    public float reducaoIntervaloPorNivel = 1.5f; // a cada nivel, nasce mais rapido (0 = desliga)
    public float intervaloMinimo = 8f;
    public float distanciaSpawn = 12f;           // distancia horizontal do barco
    public float alturaSpawn = 4f;               // altura em relacao ao barco
    public int maximoDeBruxas = 4;               // limite de bruxas vivas ao mesmo tempo
    public int nivelParaBruxaExtra = 3;          // a cada N niveis, +1 bruxa por vez

    [Header("Niveis")]
    public int bruxasParaSubirNivel = 3;         // a cada N bruxas mortas o nivel sobe
    public bool subirNivelPorTempo = false;      // marque para o nivel TAMBEM subir com o tempo
    public float duracaoNivel = 60f;             // (so vale se "Subir Nivel Por Tempo" estiver marcado)
    public int nivelMaximo = 10;
    public float aumentoVelocidadePorNivel = 0.2f; // +20% de velocidade por nivel

    [Header("UI (opcional)")]
    public TMP_Text textoNivel;                  // ex: "Nivel 2/10 | bruxas 1.2x | proximo nivel em 40s"
    public bool mostrarTempos = true;            // desmarque para mostrar so "Nivel 2/10 | bruxas 1.2x"

    [Header("Estado (so pra ver no Inspector)")]
    public int nivel = 1;
    public float tempoDeJogo;
    public int mortes;                           // bruxas mortas no total

    float tempoNivel;
    int mortesNoNivel;
    float tempoSpawn;

    // 1 no nivel 1, 1.2 no nivel 2, 1.4 no nivel 3...
    public float MultiplicadorAtual
    {
        get { return 1f + (nivel - 1) * aumentoVelocidadePorNivel; }
    }

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        if (barco == null)
        {
            Debug.LogError("Arraste o barco no campo 'Barco' do GerenciadorBruxas!");
        }

        AtualizarUI();
    }

    void Update()
    {
        if (barco == null || prefabBruxa == null) return;

        // o relogio so anda enquanto o jogo esta rolando
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando) return;

        tempoDeJogo += Time.deltaTime;
        tempoNivel += Time.deltaTime;
        tempoSpawn += Time.deltaTime;

        // opcional: passou o tempo do nivel, sobe o nivel
        if (subirNivelPorTempo && tempoNivel >= duracaoNivel && nivel < nivelMaximo)
        {
            SubirNivel();
        }

        // hora de mandar mais bruxas
        if (tempoSpawn >= IntervaloAtual())
        {
            tempoSpawn = 0f;
            EnviarBruxas();
        }

        AtualizarUI();
    }

    float IntervaloAtual()
    {
        float intervalo = intervaloSpawn - (nivel - 1) * reducaoIntervaloPorNivel;
        return Mathf.Max(intervaloMinimo, intervalo);
    }

    void EnviarBruxas()
    {
        int quantidade = 1 + (nivel - 1) / Mathf.Max(1, nivelParaBruxaExtra);

        for (int i = 0; i < quantidade; i++)
        {
            if (BruxaVoadora.todas.Count >= maximoDeBruxas) return;
            CriarBruxa(false);
        }
    }

    // Cria uma bruxa fora da tela, de um lado aleatorio do barco
    BruxaVoadora CriarBruxa(bool atacarAoNascer)
    {
        float lado = 1f;
        if (Random.value < 0.5f)
        {
            lado = -1f;
        }

        // um pouco de variacao para varias bruxas nao nascerem no mesmo ponto
        Vector3 posicao = new Vector3(
            barco.position.x + lado * (distanciaSpawn + Random.Range(0f, 2f)),
            barco.position.y + alturaSpawn + Random.Range(-1f, 1.5f),
            0f);

        GameObject obj = Instantiate(prefabBruxa, posicao, Quaternion.identity);

        // se o prefab nao tem o script da bruxa, coloca agora
        BruxaVoadora bruxa = obj.GetComponent<BruxaVoadora>();
        if (bruxa == null)
        {
            bruxa = obj.AddComponent<BruxaVoadora>();
        }

        bruxa.barco = barco;
        bruxa.multiplicadorNivel = MultiplicadorAtual;
        bruxa.atacarAoNascer = atacarAoNascer;
        return bruxa;
    }

    // Chamado pelo RaioDePesca quando o jogador pesca um peixe-bruxa:
    // a bruxa mais perto ataca na hora (ou nasce uma ja atacando).
    public void ForcarAtaque()
    {
        if (barco == null) return;

        BruxaVoadora escolhida = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < BruxaVoadora.todas.Count; i++)
        {
            BruxaVoadora b = BruxaVoadora.todas[i];
            if (b.estadoAtual == BruxaVoadora.Estado.Recuando) continue;

            float d = Mathf.Abs(b.transform.position.x - barco.position.x);
            if (d < menorDistancia)
            {
                menorDistancia = d;
                escolhida = b;
            }
        }

        if (escolhida != null)
        {
            escolhida.ForcarAtaque();
        }
        else
        {
            CriarBruxa(true);
        }
    }

    // Chamado pela BruxaVoadora quando ela morre
    public void RegistrarMorte()
    {
        mortes++;
        mortesNoNivel++;

        if (mortesNoNivel >= bruxasParaSubirNivel && nivel < nivelMaximo)
        {
            SubirNivel();
        }

        AtualizarUI();
    }

    // Sobe o nivel e acelera TODAS as bruxas (as que ja existem e as novas)
    void SubirNivel()
    {
        tempoNivel = 0f;
        mortesNoNivel = 0;
        nivel++;
        AplicarNivelEmTodas();
        AtualizarUI();
        Debug.Log("Nivel " + nivel + "! Bruxas " + MultiplicadorAtual.ToString("0.0") + "x mais rapidas");
    }

    // Sobe o multiplicador de quem ja esta voando
    void AplicarNivelEmTodas()
    {
        for (int i = 0; i < BruxaVoadora.todas.Count; i++)
        {
            BruxaVoadora.todas[i].multiplicadorNivel = MultiplicadorAtual;
        }
    }

    // Escreve nivel, velocidade e tempos no texto da tela (se voce ligou o campo "Texto Nivel")
    void AtualizarUI()
    {
        if (textoNivel == null) return;

        string linha = "Nivel " + nivel + "/" + nivelMaximo
            + "  |  bruxas " + MultiplicadorAtual.ToString("0.0") + "x";

        if (nivel < nivelMaximo)
        {
            linha += "  |  mortas " + mortesNoNivel + "/" + bruxasParaSubirNivel;
        }

        if (mostrarTempos)
        {
            float faltaBruxa = Mathf.Max(0f, IntervaloAtual() - tempoSpawn);
            linha += "  |  proxima bruxa em " + Mathf.CeilToInt(faltaBruxa) + "s";

            if (subirNivelPorTempo && nivel < nivelMaximo)
            {
                float faltaNivel = Mathf.Max(0f, duracaoNivel - tempoNivel);
                linha += "  |  nivel por tempo em " + Mathf.CeilToInt(faltaNivel) + "s";
            }
        }

        textoNivel.text = linha;
    }
}