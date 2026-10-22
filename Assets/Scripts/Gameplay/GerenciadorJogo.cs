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

    GUIStyle estiloTempo;

    void Awake()
    {
        instancia = this;
        Paralaxe.multiplicadorGlobal = 1f;
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

        if (!jogando && Input.GetKeyDown(KeyCode.R))
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
        if (painelDerrota == null)
        {
            CriarPainelDerrotaPadrao();
        }
        else
        {
            painelDerrota.SetActive(true);
        }
    }

    // ---------- Tela de derrota automatica (usada quando nenhum painel foi arrastado) ----------

    void CriarPainelDerrotaPadrao()
    {
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject raiz = new GameObject("TelaDerrota (automatica)");
        Canvas canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1280f, 720f);
        escala.matchWidthOrHeight = 0.5f;

        raiz.AddComponent<GraphicRaycaster>();
        CanvasGroup grupo = raiz.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        GameObject fundoObj = new GameObject("Fundo", typeof(RectTransform));
        fundoObj.transform.SetParent(raiz.transform, false);
        Image fundo = fundoObj.AddComponent<Image>();
        fundo.color = new Color(0.02f, 0.04f, 0.1f, 0.85f);
        RectTransform rf = fundo.rectTransform;
        rf.anchorMin = Vector2.zero;
        rf.anchorMax = Vector2.one;
        rf.offsetMin = Vector2.zero;
        rf.offsetMax = Vector2.zero;

        CriarTexto(raiz.transform, "Titulo", tituloDerrota, 84, new Vector2(0f, 170f), new Vector2(1100f, 120f), new Color(1f, 0.85f, 0.4f));

        string resultado = "Pontos: " + PontuacaoFinal + "\nTempo: " + FormatarTempo(TempoFinal);
        CriarTexto(raiz.transform, "Resultado", resultado, 44, new Vector2(0f, 30f), new Vector2(900f, 140f), Color.white);

        GameObject botaoObj = new GameObject("BotaoReiniciar", typeof(RectTransform));
        botaoObj.transform.SetParent(raiz.transform, false);
        Image imgBotao = botaoObj.AddComponent<Image>();
        imgBotao.color = new Color(0.9f, 0.35f, 0.25f);
        Button botao = botaoObj.AddComponent<Button>();
        botao.targetGraphic = imgBotao;
        botao.onClick.AddListener(Reiniciar);
        RectTransform rb = botaoObj.GetComponent<RectTransform>();
        rb.anchorMin = rb.anchorMax = new Vector2(0.5f, 0.5f);
        rb.sizeDelta = new Vector2(340f, 84f);
        rb.anchoredPosition = new Vector2(0f, -110f);
        CriarTexto(botaoObj.transform, "Texto", "Jogar de novo", 38, Vector2.zero, new Vector2(340f, 84f), Color.white);

        CriarTexto(raiz.transform, "Dica", "ou aperte R", 28, new Vector2(0f, -200f), new Vector2(600f, 50f), new Color(1f, 1f, 1f, 0.7f));

        painelDerrota = raiz;
        StartCoroutine(AparecerGradualmente(grupo));
    }

    TMP_Text CriarTexto(Transform pai, string nome, string conteudo, float tamanho, Vector2 posicao, Vector2 area, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

        TextMeshProUGUI texto = obj.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = cor;
        texto.raycastTarget = false;

        RectTransform rt = texto.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = area;
        rt.anchoredPosition = posicao;
        return texto;
    }

    IEnumerator AparecerGradualmente(CanvasGroup grupo)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.6f;
            grupo.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }

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