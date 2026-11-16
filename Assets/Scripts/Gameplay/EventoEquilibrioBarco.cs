using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class EventoEquilibrioBarco : MonoBehaviour
{
    KeyCode teclaA { get { return ConfiguracoesJogo.Tecla(AcaoTecla.EquilibrioA); } }
    KeyCode teclaB { get { return ConfiguracoesJogo.Tecla(AcaoTecla.EquilibrioB); } }

    [Header("Equilibrio (0 = seguro, 1 = barco virou)")]
    [Range(0f, 1f)] public float perigoInicial = 0.4f;
    public float empurraoPorSegundo = 0.15f;
    public float forcaDoAperto = 0.05f;
    public float penalidadeErro = 0.03f;

    [Header("Inclinacao do barco")]
    public Transform visualBarco;
    public float anguloMaximoInclinacao = 35f;
    public float tempoSuavizacaoInclinacao = 0.15f;
    public float anguloBalanco = 3f;
    public float velocidadeBalanco = 14f;

    [Header("Virada")]
    public float anguloVirada = 180f;
    public float velocidadeAfundar = 1.5f;
    public float profundidadeAfundar = 3f;

    [Header("Quando o evento comeca de verdade")]
    public float distanciaParaComecar = 5f;

    [Header("UI (opcional)")]
    public GameObject uiRaiz;
    public Image barraPerigo;
    public TMP_Text textoAviso;

    [Header("Barra com sprite (opcional)")]
    public Sprite spriteFundoBarra;
    public Color corFundoBarra = Color.white;
    public bool encaixarPeloTrilho = true;
    [Range(0f, 0.6f)] public float trilhoEsquerda = 0.046f;
    [Range(0f, 0.6f)] public float trilhoDireita = 0.044f;
    [Range(0f, 0.6f)] public float trilhoCima = 0.328f;
    [Range(0f, 0.6f)] public float trilhoBaixo = 0.459f;
    public Vector2 margemFundoBarra = new Vector2(12f, 12f);

    [Header("Barra de vida (cores e comportamento)")]
    public bool barraEsvaziaComPerigo = true;
    public float suavidadeBarra = 6f;
    public Color corVerde = new Color(0.35f, 0.85f, 0.35f);
    public Color corAmarela = new Color(0.97f, 0.8f, 0.2f);
    public Color corVermelha = new Color(0.9f, 0.25f, 0.2f);
    [Range(0f, 1f)] public float limiteVerde = 0.5f;
    [Range(0f, 1f)] public float limiteAmarelo = 0.2f;
    public float aumentoEmpurraoPorNivel = 0.08f;

    [Header("Eventos")]
    public UnityEvent aoIniciarEvento;
    public UnityEvent aoSucesso;
    public UnityEvent aoVirar;

    public bool emAndamento;
    public bool virou;

    BruxaVoadora bruxa;
    bool bruxaPerto;
    KeyCode teclaEsperada;
    float perigo;
    float anguloBase;
    float anguloAtual;
    float velocidadeAngular;
    float afundado;
    float proporcaoMostrada = 1f;
    GameObject fundoBarra;
    bool mostrandoTutorial;

    void Start()
    {
        if (visualBarco == null) visualBarco = transform;

        anguloBase = visualBarco.eulerAngles.z;
        anguloAtual = anguloBase;

        PrepararBarra();
        CriarFundoDaBarra();
        MostrarUI(false);
    }

    void PrepararBarra()
    {
        if (barraPerigo == null) return;

        if (barraPerigo.sprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            barraPerigo.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }

        barraPerigo.type = Image.Type.Filled;
        barraPerigo.fillMethod = Image.FillMethod.Horizontal;
        barraPerigo.fillOrigin = (int)Image.OriginHorizontal.Left;
        barraPerigo.fillAmount = 1f;
    }

    // Coloca a arte (sprite) ATRAS da barra de perigo.
    // Com "encaixarPeloTrilho", a arte e esticada para o trilho escuro dela ficar exatamente
    // onde esta a barra colorida (as medidas do trilho sao fracoes da imagem, de 0 a 1).
    void CriarFundoDaBarra()
    {
        if (spriteFundoBarra == null || barraPerigo == null) return;

        RectTransform alvo = barraPerigo.rectTransform;
        if (alvo.parent == null) return;

        Canvas.ForceUpdateCanvases();   // garante que o tamanho da barra ja foi calculado

        // quanto a arte passa de cada lado da barra
        float esquerda, direita, cima, baixo;
        if (encaixarPeloTrilho)
        {
            float fracaoLargura = Mathf.Max(0.01f, 1f - trilhoEsquerda - trilhoDireita);
            float fracaoAltura = Mathf.Max(0.01f, 1f - trilhoCima - trilhoBaixo);
            float larguraTotal = alvo.rect.width / fracaoLargura;
            float alturaTotal = alvo.rect.height / fracaoAltura;

            esquerda = trilhoEsquerda * larguraTotal;
            direita = trilhoDireita * larguraTotal;
            cima = trilhoCima * alturaTotal;
            baixo = trilhoBaixo * alturaTotal;
        }
        else
        {
            esquerda = margemFundoBarra.x;
            direita = margemFundoBarra.x;
            cima = margemFundoBarra.y;
            baixo = margemFundoBarra.y;
        }

        fundoBarra = new GameObject("FundoBarraSprite", typeof(RectTransform), typeof(Image));
        fundoBarra.transform.SetParent(alvo.parent, false);
        fundoBarra.transform.SetSiblingIndex(alvo.GetSiblingIndex());   // logo antes da barra = desenhado atras dela

        RectTransform rt = (RectTransform)fundoBarra.transform;
        rt.anchorMin = alvo.anchorMin;
        rt.anchorMax = alvo.anchorMax;
        rt.pivot = alvo.pivot;
        rt.sizeDelta = alvo.sizeDelta + new Vector2(esquerda + direita, baixo + cima);
        rt.anchoredPosition = alvo.anchoredPosition + new Vector2(
            -esquerda + alvo.pivot.x * (esquerda + direita),
            -baixo + alvo.pivot.y * (baixo + cima));
        rt.localScale = alvo.localScale;
        rt.localRotation = alvo.localRotation;

        Image imagem = fundoBarra.GetComponent<Image>();
        imagem.sprite = spriteFundoBarra;
        imagem.color = corFundoBarra;
        imagem.raycastTarget = false;
        imagem.type = Image.Type.Simple;
    }

    public RectTransform RetanguloDaBarra
    {
        get { return barraPerigo != null ? barraPerigo.rectTransform : null; }
    }

    public void MostrarBarraNoTutorial(bool mostrar)
    {
        mostrandoTutorial = mostrar;

        if (mostrar)
        {
            proporcaoMostrada = 1f;

            if (barraPerigo != null)
            {
                barraPerigo.fillAmount = 1f;
                barraPerigo.color = CorDaVida(1f);
            }

            if (textoAviso != null)
            {
                textoAviso.text = "Aperte " + ConfiguracoesJogo.NomeTecla(teclaA) + " e " + ConfiguracoesJogo.NomeTecla(teclaB) + " alternados!";
            }
        }

        if (!emAndamento) MostrarUI(mostrar);
    }
    float EmpurraoAtual()
    {
        int nivel = GerenciadorBruxas.instancia != null ? GerenciadorBruxas.instancia.nivel : 1;
        return empurraoPorSegundo * (1f + (nivel - 1) * aumentoEmpurraoPorNivel);
    }
    public bool TentarIniciar(BruxaVoadora atacante)
    {
        if (emAndamento || virou) return false;

        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando)
        {
            return false;
        }

        bruxa = atacante;
        perigo = perigoInicial;
        teclaEsperada = teclaA;
        emAndamento = true;

        proporcaoMostrada = ProporcaoDaBarra();

        MostrarUI(false);
        AtualizarUI();
        aoIniciarEvento.Invoke();
        return true;
    }

    public void Cancelar()
    {
        emAndamento = false;
        MostrarUI(false);
    }

    public void EncerrarComFerro(BruxaVoadora quem)
    {
        if (!emAndamento || quem != bruxa) return;

        Vencer();
    }

    void Update()
    {
        bruxaPerto = false;

        if (emAndamento)
        {
            bruxaPerto = BruxaChegou();
            MostrarUI(bruxaPerto);

            if (bruxaPerto)
            {
                perigo += EmpurraoAtual() * Time.deltaTime;

                LerTeclas();

                perigo = Mathf.Clamp01(perigo);
                AtualizarUI();

                if (perigo <= 0f)
                {
                    Vencer();
                }
                else if (perigo >= 1f)
                {
                    Virar();
                }
            }
        }

        AtualizarInclinacao();
    }

    bool BruxaChegou()
    {
        if (bruxa == null) return true;
        if (distanciaParaComecar <= 0f) return true;

        return Mathf.Abs(bruxa.transform.position.x - transform.position.x) <= distanciaParaComecar;
    }

    void MostrarUI(bool mostrar)
    {
        mostrar = mostrar || mostrandoTutorial;

        if (fundoBarra != null) fundoBarra.SetActive(mostrar);

        if (uiRaiz != null)
        {
            uiRaiz.SetActive(mostrar);
            return;
        }

        if (barraPerigo != null) barraPerigo.gameObject.SetActive(mostrar);
        if (textoAviso != null) textoAviso.gameObject.SetActive(mostrar);
    }

    void LerTeclas()
    {
        if (PainelPause.aberto) return;

        bool apertouA = Input.GetKeyDown(teclaA);
        bool apertouB = Input.GetKeyDown(teclaB);

        if (!apertouA && !apertouB) return;

        KeyCode apertada = teclaB;
        if (apertouA)
        {
            apertada = teclaA;
        }

        if (apertada == teclaEsperada)
        {
            perigo -= forcaDoAperto;

            if (teclaEsperada == teclaA)
            {
                teclaEsperada = teclaB;
            }
            else
            {
                teclaEsperada = teclaA;
            }
        }
        else
        {
            perigo += penalidadeErro;
        }
    }

    void AtualizarInclinacao()
    {
        float anguloAlvo = anguloBase;

        float lado = 1f;
        if (bruxa != null)
        {
            lado = bruxa.lado;
        }

        if (virou)
        {
            anguloAlvo = anguloBase - lado * anguloVirada;

            if (afundado < profundidadeAfundar)
            {
                float passo = velocidadeAfundar * Time.deltaTime;
                afundado += passo;
                visualBarco.position += Vector3.down * passo;
            }
        }
        else if (emAndamento && bruxaPerto)
        {
            anguloAlvo = anguloBase - lado * Mathf.Lerp(0f, anguloMaximoInclinacao, perigo);
            anguloAlvo += Mathf.Sin(Time.time * velocidadeBalanco) * anguloBalanco * perigo;
        }

        anguloAtual = Mathf.SmoothDampAngle(anguloAtual, anguloAlvo, ref velocidadeAngular, tempoSuavizacaoInclinacao);
        visualBarco.rotation = Quaternion.Euler(0f, 0f, anguloAtual);
    }

    float ProporcaoDaBarra()
    {
        return barraEsvaziaComPerigo ? 1f - perigo : perigo;
    }

    Color CorDaVida(float vida)
    {
        if (vida > limiteVerde) return corVerde;
        if (vida > limiteAmarelo) return corAmarela;
        return corVermelha;
    }

    void AtualizarUI()
    {
        if (barraPerigo != null)
        {
            float alvo = ProporcaoDaBarra();

            if (suavidadeBarra > 0f)
            {
                proporcaoMostrada = Mathf.Lerp(proporcaoMostrada, alvo, 1f - Mathf.Exp(-suavidadeBarra * Time.deltaTime));
            }
            else
            {
                proporcaoMostrada = alvo;
            }

            barraPerigo.fillAmount = proporcaoMostrada;
            barraPerigo.color = CorDaVida(1f - perigo);
        }

        if (textoAviso != null)
        {
            textoAviso.text = "Aperte " + ConfiguracoesJogo.NomeTecla(teclaEsperada) + "!";
        }
    }

    void Vencer()
    {
        emAndamento = false;
        MostrarUI(false);
        if (bruxa != null) bruxa.Repelir();
        aoSucesso.Invoke();
    }

    void Virar()
    {
        // Escudo do Barco: absorve o ataque que viraria o barco
        if (GerenciadorUpgrades.TentarUsarEscudo())
        {
            Vencer(); // a bruxa é repelida, como se o jogador tivesse equilibrado
            return;
        }
        emAndamento = false;
        virou = true;
        MostrarUI(false);
        aoVirar.Invoke();

        if (GerenciadorJogo.instancia != null)
        {
            GerenciadorJogo.instancia.Derrota();
        }
    }
}