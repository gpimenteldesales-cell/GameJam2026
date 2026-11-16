using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GerenciadorJogo : MonoBehaviour
{
    public static GerenciadorJogo instancia;

    [Header("Pontos")]
    public int metaDePontos = 300;
    public bool vencerNaMeta = false;

    [Header("Ligacoes")]
    public EventoEquilibrioBarco eventoBarco;
    public BruxaVoadora bruxa;

    [Header("UI")]
    public TMP_Text textoPontos;
    public TMP_Text textoTempo;
    public TMP_Text textoResultado;
    [Tooltip("Opcional: texto do recorde durante o jogo. Vazio = o codigo desenha um 'Recorde: N' no canto superior direito.")]
    public TMP_Text textoRecorde;
    public Slider barraOferenda;
    public GameObject painelVitoria;
    public GameObject painelDerrota;
    public float atrasoVitoria = 1f;
    public float atrasoDerrota = 2f;
    [Tooltip("Se 'Painel Derrota' estiver vazio, o jogo cria uma tela de derrota sozinho com este titulo.")]
    public string tituloDerrota = "O BARCO VIROU!";

    [Header("Fim de jogo")]
    public UnityEvent aoFimDeJogo;

    [Header("Estado (so pra ver no Inspector)")]
    public int pontos;
    public float tempo;
    public bool jogando = true;

    public int PontuacaoFinal { get; private set; }
    public float TempoFinal { get; private set; }

    GUIStyle estiloTempo, estiloRecorde;

    void Awake()
    {
        instancia = this;
        Paralaxe.multiplicadorGlobal = 1f;
        PainelPause.Obter(); // garante que o ESC funciona (usa o painel da cena, ou cria um simples)
    }

    void Start()
    {
        if (painelVitoria != null) painelVitoria.SetActive(false);
        if (painelDerrota != null) painelDerrota.SetActive(false);
        AtualizarUI();
    }

    void Update()
    {
        if (jogando)
        {
            tempo += Time.deltaTime;
        }

        if (textoTempo != null)
        {
            textoTempo.text = FormatarTempo(tempo);
        }

        if (!jogando && !PainelPause.Digitando && Input.GetKeyDown(KeyCode.R))
        {
            Reiniciar();
        }
    }

    public static string FormatarTempo(float segundos)
    {
        int centesimos = Mathf.FloorToInt(Mathf.Max(0f, segundos) * 100f);
        int minutos = centesimos / 6000;
        int seg = (centesimos / 100) % 60;
        int cent = centesimos % 100;
        return minutos.ToString("00") + ":" + seg.ToString("00") + "." + cent.ToString("00");
    }

    void OnGUI()
    {
        if (textoTempo == null)
        {
            if (estiloTempo == null)
            {
                estiloTempo = new GUIStyle(GUI.skin.label);
                estiloTempo.fontSize = 34;
                estiloTempo.fontStyle = FontStyle.Bold;
                estiloTempo.alignment = TextAnchor.UpperRight;
                estiloTempo.normal.textColor = Color.white;
                FontePadrao.Aplicar(estiloTempo);
            }

            GUI.Label(new Rect(Screen.width - 340f, 10f, 320f, 50f), FormatarTempo(tempo), estiloTempo);
        }

        // recorde local durante o jogo (so se nao houver um TMP de recorde na cena)
        if (textoRecorde == null && jogando && Recorde.Pontos > 0)
        {
            if (estiloRecorde == null)
            {
                estiloRecorde = new GUIStyle(GUI.skin.label);
                estiloRecorde.fontSize = 22;
                estiloRecorde.fontStyle = FontStyle.Bold;
                estiloRecorde.alignment = TextAnchor.UpperRight;
                FontePadrao.Aplicar(estiloRecorde);
            }

            bool batendo = pontos > Recorde.Pontos;
            estiloRecorde.normal.textColor = batendo ? new Color(1f, 0.85f, 0.3f) : Color.white;

            GUI.Label(new Rect(Screen.width - 340f, 56f, 320f, 32f),
                "Recorde: " + Mathf.Max(Recorde.Pontos, pontos), estiloRecorde);
        }
    }

    public void AdicionarPontos(int valor)
    {
        if (!jogando) return;

        valor = GerenciadorUpgrades.AplicarBonusPontos(valor);

        pontos += valor;
        PopupPontos.Mostrar(textoPontos, valor);
        GerenciadorUpgrades.GanharPerolas(valor);
        AtualizarUI();

        if (vencerNaMeta && pontos >= metaDePontos)
        {
            Vitoria();
        }
    }

    public float ProgressoMeta()
    {
        return Mathf.Clamp01((float)pontos / metaDePontos);
    }

    void Vitoria()
    {
        jogando = false;
        RegistrarResultado();

        if (eventoBarco != null) eventoBarco.Cancelar();
        if (bruxa != null) bruxa.Repelir();

        for (int i = 0; i < BruxaVoadora.todas.Count; i++)
        {
            BruxaVoadora.todas[i].Repelir();
        }

        Invoke(nameof(MostrarVitoria), atrasoVitoria);
    }

    public void Derrota()
    {
        if (!jogando) return;

        jogando = false;
        Paralaxe.multiplicadorGlobal = 0f;
        RegistrarResultado();

        Invoke(nameof(MostrarDerrota), atrasoDerrota);
    }

    void RegistrarResultado()
    {
        PontuacaoFinal = pontos;
        TempoFinal = tempo;

        Recorde.Registrar(PontuacaoFinal, TempoFinal);

        if (textoResultado != null)
        {
            textoResultado.text = TextoDoResultado();
        }

        aoFimDeJogo.Invoke();
    }

    string TextoDoResultado()
    {
        string texto = "Pontos: " + PontuacaoFinal + "\nTempo: " + FormatarTempo(TempoFinal);

        if (Recorde.NovoDePontos) texto += "\nNOVO RECORDE!";
        else if (Recorde.Pontos > 0) texto += "\nRecorde: " + Recorde.Pontos;

        if (Recorde.PosicaoUltima > 0) texto += "\nRanking: #" + Recorde.PosicaoUltima;

        return texto;
    }

    void MostrarVitoria()
    {
        if (painelVitoria != null) painelVitoria.SetActive(true);
    }

    void MostrarDerrota()
    {
        if (painelDerrota != null)
        {
            painelDerrota.SetActive(true);
            return;
        }

        // sem painel proprio: usa a pagina de pause (com o seu sprite)
        Color cor = Recorde.NovoDePontos ? new Color(1f, 0.85f, 0.3f) : Color.white;
        PainelPause.Obter().MostrarDerrota(tituloDerrota, TextoDoResultado(), cor, Recorde.PosicaoUltima > 0);
    }

    public void Reiniciar()
    {
        // garante que o nome digitado fica salvo no ranking e que o jogo nao fica pausado
        PainelPause.SalvarNome();
        PainelPause.Liberar();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void AtualizarUI()
    {
        if (textoPontos != null)
        {
            if (vencerNaMeta)
            {
                textoPontos.text = "Oferenda: " + pontos + " / " + metaDePontos;
            }
            else
            {
                textoPontos.text = "Pontos: " + pontos;
            }
        }

        if (barraOferenda != null)
        {
            barraOferenda.value = ProgressoMeta();
        }

        AtualizarTextoRecorde();
    }

    void AtualizarTextoRecorde()
    {
        if (textoRecorde == null) return;

        int melhor = Mathf.Max(Recorde.Pontos, pontos);
        textoRecorde.text = melhor > 0 ? "Recorde: " + melhor : "";
    }
}