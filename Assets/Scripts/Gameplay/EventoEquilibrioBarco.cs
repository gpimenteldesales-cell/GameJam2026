using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class EventoEquilibrioBarco : MonoBehaviour
{
    [Header("Teclas (alternar entre elas)")]
    public KeyCode teclaA = KeyCode.E;
    public KeyCode teclaB = KeyCode.Q;

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

    [Header("Barra de vida (cores e comportamento)")]
    public bool barraEsvaziaComPerigo = true;
    public float suavidadeBarra = 6f;
    public Color corVerde = new Color(0.35f, 0.85f, 0.35f);
    public Color corAmarela = new Color(0.97f, 0.8f, 0.2f);
    public Color corVermelha = new Color(0.9f, 0.25f, 0.2f);
    [Range(0f, 1f)] public float limiteVerde = 0.5f;
    [Range(0f, 1f)] public float limiteAmarelo = 0.2f;

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

    void Start()
    {
        if (visualBarco == null) visualBarco = transform;

        anguloBase = visualBarco.eulerAngles.z;
        anguloAtual = anguloBase;

        PrepararBarra();
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
                perigo += empurraoPorSegundo * Time.deltaTime;

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
            textoAviso.text = "Aperte " + teclaEsperada + "!";
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