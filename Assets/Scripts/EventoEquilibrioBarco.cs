using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Quando a bruxa ataca, o jogador precisa apertar E e Q
// de forma alternada para equilibrar o barco.
// Se o perigo chegar a 1, o barco vira.
public class EventoEquilibrioBarco : MonoBehaviour
{
    [Header("Teclas (alternar entre elas)")]
    public KeyCode teclaA = KeyCode.E;
    public KeyCode teclaB = KeyCode.Q;

    [Header("Equilibrio (0 = seguro, 1 = barco virou)")]
    [Range(0f, 1f)] public float perigoInicial = 0.4f;
    public float empurraoPorSegundo = 0.15f;   // quanto o perigo sobe sozinho
    public float forcaDoAperto = 0.05f;        // quanto cada aperto certo diminui
    public float penalidadeErro = 0.03f;       // quanto o aperto errado aumenta

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

    [Header("UI (opcional)")]
    public GameObject uiRaiz;
    public Slider barraPerigo;
    public TMP_Text textoAviso;

    [Header("Eventos")]
    public UnityEvent aoIniciarEvento;
    public UnityEvent aoSucesso;
    public UnityEvent aoVirar;     // ligue sons ou efeitos aqui

    // Estado do evento (aparece no Inspector, bom pra testar)
    public bool emAndamento;
    public bool virou;

    BruxaVoadora bruxa;
    KeyCode teclaEsperada;
    float perigo;
    float anguloBase;
    float anguloAtual;
    float velocidadeAngular;
    float afundado;

    void Start()
    {
        if (visualBarco == null) visualBarco = transform;

        anguloBase = visualBarco.eulerAngles.z;
        anguloAtual = anguloBase;

        if (uiRaiz != null) uiRaiz.SetActive(false);
    }

    // A bruxa chama isso quando chega perto.
    // Devolve true se o evento comecou.
    public bool TentarIniciar(BruxaVoadora atacante)
    {
        if (emAndamento || virou) return false;

        // se o jogo ja acabou (ex: vitoria), nao comeca evento
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando)
        {
            return false;
        }

        bruxa = atacante;
        perigo = perigoInicial;
        teclaEsperada = teclaA;
        emAndamento = true;

        if (uiRaiz != null) uiRaiz.SetActive(true);
        AtualizarUI();
        aoIniciarEvento.Invoke();
        return true;
    }

    // O GerenciadorJogo chama isso quando o jogador vence o jogo
    public void Cancelar()
    {
        emAndamento = false;
        if (uiRaiz != null) uiRaiz.SetActive(false);
    }

    // O ProjetilFerro chama isso quando o ferro acerta a bruxa que esta atacando.
    // Conta como vitoria no evento: o barco se endireita e a bruxa recua.
    public void EncerrarComFerro(BruxaVoadora quem)
    {
        if (!emAndamento || quem != bruxa) return;

        Vencer();
    }

    void Update()
    {
        if (emAndamento)
        {
            // o perigo sobe sozinho com o tempo
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

        AtualizarInclinacao();
    }

    void LerTeclas()
    {
        bool apertouA = Input.GetKeyDown(teclaA);
        bool apertouB = Input.GetKeyDown(teclaB);

        // nao apertou nada
        if (!apertouA && !apertouB) return;

        KeyCode apertada = teclaB;
        if (apertouA)
        {
            apertada = teclaA;
        }

        if (apertada == teclaEsperada)
        {
            // acertou: diminui o perigo e troca a tecla esperada
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
            // errou: aumenta o perigo
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
            // barco virado: gira e afunda
            anguloAlvo = anguloBase - lado * anguloVirada;

            if (afundado < profundidadeAfundar)
            {
                float passo = velocidadeAfundar * Time.deltaTime;
                afundado += passo;
                visualBarco.position += Vector3.down * passo;
            }
        }
        else if (emAndamento)
        {
            // quanto maior o perigo, mais o barco inclina
            anguloAlvo = anguloBase - lado * Mathf.Lerp(0f, anguloMaximoInclinacao, perigo);
            anguloAlvo += Mathf.Sin(Time.time * velocidadeBalanco) * anguloBalanco * perigo;
        }

        anguloAtual = Mathf.SmoothDampAngle(anguloAtual, anguloAlvo, ref velocidadeAngular, tempoSuavizacaoInclinacao);
        visualBarco.rotation = Quaternion.Euler(0f, 0f, anguloAtual);
    }

    void AtualizarUI()
    {
        if (barraPerigo != null)
        {
            barraPerigo.value = perigo;
        }

        if (textoAviso != null)
        {
            textoAviso.text = "Aperte " + teclaEsperada + "!";
        }
    }

    void Vencer()
    {
        emAndamento = false;
        if (uiRaiz != null) uiRaiz.SetActive(false);
        if (bruxa != null) bruxa.Repelir();
        aoSucesso.Invoke();
    }

    void Virar()
    {
        emAndamento = false;
        virou = true;
        if (uiRaiz != null) uiRaiz.SetActive(false);
        aoVirar.Invoke();

        // avisa o gerenciador: fim de jogo
        if (GerenciadorJogo.instancia != null)
        {
            GerenciadorJogo.instancia.Derrota();
        }
    }
}