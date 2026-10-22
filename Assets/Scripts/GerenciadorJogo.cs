using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Controla o jogo: pontos, cronometro (estilo speedrun), derrota e reiniciar.
// O objetivo agora e fazer o MAXIMO de pontos possivel antes do barco virar.
// Coloque este script em UM objeto vazio da cena (ex: "Gerenciador").
public class GerenciadorJogo : MonoBehaviour
{
    // Qualquer script pode usar: GerenciadorJogo.instancia
    public static GerenciadorJogo instancia;

    [Header("Pontos")]
    public int metaDePontos = 300;       // ao chegar nisso as bruxas ficam na velocidade maxima (curva de dificuldade)
    public bool vencerNaMeta = false;    // desmarcado = nao existe vitoria: o objetivo e fazer o maximo de pontos

    [Header("Ligacoes")]
    public EventoEquilibrioBarco eventoBarco;
    public BruxaVoadora bruxa;

    [Header("UI")]
    public TMP_Text textoPontos;
    public TMP_Text textoTempo;          // opcional: se ficar vazio, o tempo aparece no canto da tela
    public TMP_Text textoResultado;      // opcional: pontos e tempo finais (coloque dentro do painel de derrota)
    public Slider barraOferenda;         // opcional (mostra o progresso ate a "meta de pontos")
    public GameObject painelVitoria;
    public GameObject painelDerrota;
    public float atrasoVitoria = 1f;
    public float atrasoDerrota = 2f;     // espera o barco afundar

    [Header("Fim de jogo")]
    public UnityEvent aoFimDeJogo;       // ligue aqui o ranking depois

    [Header("Estado (so pra ver no Inspector)")]
    public int pontos;
    public float tempo;                  // segundos desde o comeco da partida (o cronometro)
    public bool jogando = true;

    // resultado da partida (o ranking vai ler isso)
    public int PontuacaoFinal { get; private set; }
    public float TempoFinal { get; private set; }

    GUIStyle estiloTempo;

    void Awake()
    {
        instancia = this;
        Paralaxe.multiplicadorGlobal = 1f;   // o cenario volta a andar
    }

    void Start()
    {
        if (painelVitoria != null) painelVitoria.SetActive(false);
        if (painelDerrota != null) painelDerrota.SetActive(false);
        AtualizarUI();
    }

    void Update()
    {
        // o cronometro so anda enquanto o jogo esta rolando
        if (jogando)
        {
            tempo += Time.deltaTime;
        }

        if (textoTempo != null)
        {
            textoTempo.text = FormatarTempo(tempo);
        }

        // quando o jogo acabou, apertar R reinicia
        if (!jogando && Input.GetKeyDown(KeyCode.R))
        {
            Reiniciar();
        }
    }

    // "01:23.45" (minutos:segundos.centesimos), igual cronometro de speedrun
    public static string FormatarTempo(float segundos)
    {
        int centesimos = Mathf.FloorToInt(Mathf.Max(0f, segundos) * 100f);
        int minutos = centesimos / 6000;
        int seg = (centesimos / 100) % 60;
        int cent = centesimos % 100;
        return minutos.ToString("00") + ":" + seg.ToString("00") + "." + cent.ToString("00");
    }

    // Se voce nao ligou o campo "Texto Tempo", desenha o cronometro no canto da tela
    void OnGUI()
    {
        if (textoTempo != null) return;

        if (estiloTempo == null)
        {
            estiloTempo = new GUIStyle(GUI.skin.label);
            estiloTempo.fontSize = 34;
            estiloTempo.fontStyle = FontStyle.Bold;
            estiloTempo.alignment = TextAnchor.UpperRight;
            estiloTempo.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(Screen.width - 340f, 10f, 320f, 50f), FormatarTempo(tempo), estiloTempo);
    }

    // Chamado pelo RaioDePesca quando um peixe e pescado
    public void AdicionarPontos(int valor)
    {
        if (!jogando) return;

        pontos += valor;
        AtualizarUI();

        if (vencerNaMeta && pontos >= metaDePontos)
        {
            Vitoria();
        }
    }

    // Vai de 0 a 1: quanto da meta de pontos ja foi alcancado (as bruxas usam isso pra acelerar)
    public float ProgressoMeta()
    {
        return Mathf.Clamp01((float)pontos / metaDePontos);
    }

    void Vitoria()
    {
        jogando = false;
        RegistrarResultado();

        // para o evento do barco e a bruxa foge
        if (eventoBarco != null) eventoBarco.Cancelar();
        if (bruxa != null) bruxa.Repelir();

        // repele tambem todas as bruxas criadas pelo GerenciadorBruxas
        for (int i = 0; i < BruxaVoadora.todas.Count; i++)
        {
            BruxaVoadora.todas[i].Repelir();
        }

        Invoke(nameof(MostrarVitoria), atrasoVitoria);
    }

    // Chamado pelo EventoEquilibrioBarco quando o barco vira: fim da partida
    public void Derrota()
    {
        if (!jogando) return;

        jogando = false;
        Paralaxe.multiplicadorGlobal = 0f;   // o cenario para
        RegistrarResultado();

        Invoke(nameof(MostrarDerrota), atrasoDerrota);
    }

    // Guarda pontos e tempo finais, escreve no texto de resultado e avisa quem quiser saber (ranking)
    void RegistrarResultado()
    {
        PontuacaoFinal = pontos;
        TempoFinal = tempo;

        if (textoResultado != null)
        {
            textoResultado.text = "Pontos: " + PontuacaoFinal + "\nTempo: " + FormatarTempo(TempoFinal);
        }

        aoFimDeJogo.Invoke();
    }

    void MostrarVitoria()
    {
        if (painelVitoria != null) painelVitoria.SetActive(true);
    }

    void MostrarDerrota()
    {
        if (painelDerrota != null) painelDerrota.SetActive(true);
    }

    // Pode ligar num botao da UI (OnClick)
    // Obs: a cena precisa estar em File > Build Settings
    public void Reiniciar()
    {
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
    }
}