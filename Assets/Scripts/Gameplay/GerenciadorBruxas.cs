using UnityEngine;
using TMPro;

public class GerenciadorBruxas : MonoBehaviour
{
    public static GerenciadorBruxas instancia;

    [Header("Ligacoes")]
    public GameObject prefabBruxa;
    public Transform barco;

    [Header("Nascimento")]
    public float intervaloSpawn = 20f;
    public float reducaoIntervaloPorNivel = 1.5f;
    public float intervaloMinimo = 8f;
    public float distanciaSpawn = 12f;
    public float alturaSpawn = 4f;
    public int maximoDeBruxas = 4;
    public int nivelParaBruxaExtra = 3;

    [Header("Niveis")]
    public int bruxasParaSubirNivel = 3;
    public bool subirNivelPorTempo = false;
    public float duracaoNivel = 60f;
    public int nivelMaximo = 10;
    public float aumentoVelocidadePorNivel = 0.2f;

    [Header("UI (opcional)")]
    public TMP_Text textoNivel;
    public bool mostrarTempos = true;

    [Header("Estado (so pra ver no Inspector)")]
    public int nivel = 1;
    public float tempoDeJogo;
    public int mortes;

    float tempoNivel;
    int mortesNoNivel;
    float tempoSpawn;

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

        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando) return;

        tempoDeJogo += Time.deltaTime;
        tempoNivel += Time.deltaTime;
        tempoSpawn += Time.deltaTime;

        if (subirNivelPorTempo && tempoNivel >= duracaoNivel && nivel < nivelMaximo)
        {
            SubirNivel();
        }

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

    BruxaVoadora CriarBruxa(bool atacarAoNascer)
    {
        float lado = 1f;
        if (Random.value < 0.5f)
        {
            lado = -1f;
        }

        Vector3 posicao = new Vector3(
            barco.position.x + lado * (distanciaSpawn + Random.Range(0f, 2f)),
            barco.position.y + alturaSpawn + Random.Range(-1f, 1.5f),
            0f);

        GameObject obj = Instantiate(prefabBruxa, posicao, Quaternion.identity);

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

    void SubirNivel()
    {
        tempoNivel = 0f;
        mortesNoNivel = 0;
        nivel++;
        AplicarNivelEmTodas();
        AtualizarUI();
        Debug.Log("Nivel " + nivel + "! Bruxas " + MultiplicadorAtual.ToString("0.0") + "x mais rapidas");
    }

    void AplicarNivelEmTodas()
    {
        for (int i = 0; i < BruxaVoadora.todas.Count; i++)
        {
            BruxaVoadora.todas[i].multiplicadorNivel = MultiplicadorAtual;
        }
    }

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