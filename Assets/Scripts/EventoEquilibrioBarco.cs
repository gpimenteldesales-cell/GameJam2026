using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class EventoEquilibrioBarco : MonoBehaviour
{
    [Header("Teclas (alternar entre elas)")]
    public KeyCode teclaA = KeyCode.E;
    public KeyCode teclaB = KeyCode.Q;

    [Header("Equilíbrio (0 = seguro, 1 = barco virou)")]
    [Range(0f, 1f)] public float perigoInicial = 0.4f;
    public float empurraoPorSegundo = 0.15f;
    public float forcaDoAperto = 0.05f;
    public float penalidadeErro = 0.03f;

    [Header("Inclinação do barco")]
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
    public TMP_Text textoPrompt;

    [Header("Eventos")]
    public UnityEvent aoIniciarEvento;
    public UnityEvent aoSucesso;
    public UnityEvent aoVirar;

    public bool EmAndamento { get; private set; }
    public bool Virou { get; private set; }

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
    public bool TentarIniciar(BruxaVoadora atacante)
    {
        if (EmAndamento || Virou) return false;

        bruxa = atacante;
        perigo = perigoInicial;
        teclaEsperada = teclaA;
        EmAndamento = true;

        if (uiRaiz != null) uiRaiz.SetActive(true);
        AtualizarUI();
        aoIniciarEvento.Invoke();
        return true;
    }

    void Update()
    {
        if (EmAndamento)
        {
            perigo += empurraoPorSegundo * Time.deltaTime;
            ProcessarInput();
            perigo = Mathf.Clamp01(perigo);
            AtualizarUI();

            if (perigo <= 0f) Vencer();
            else if (perigo >= 1f) Virar();
        }

        AtualizarInclinacao();
    }

    void ProcessarInput()
    {
        bool apertouA = Input.GetKeyDown(teclaA);
        bool apertouB = Input.GetKeyDown(teclaB);
        if (!apertouA && !apertouB) return;

        KeyCode apertada = apertouA ? teclaA : teclaB;

        if (apertada == teclaEsperada)
        {
            perigo -= forcaDoAperto;
            teclaEsperada = (teclaEsperada == teclaA) ? teclaB : teclaA; 
        }
        else
        {
            perigo += penalidadeErro; 
        }
    }

    void AtualizarInclinacao()
    {
        float alvo = anguloBase;
        float lado = bruxa != null ? bruxa.Lado : 1f;

        if (Virou)
        {
            alvo = anguloBase - lado * anguloVirada;

            if (afundado < profundidadeAfundar)
            {
                float passo = velocidadeAfundar * Time.deltaTime;
                afundado += passo;
                visualBarco.position += Vector3.down * passo;
            }
        }
        else if (EmAndamento)
        {
            alvo = anguloBase - lado * Mathf.Lerp(0f, anguloMaximoInclinacao, perigo);
            alvo += Mathf.Sin(Time.time * velocidadeBalanco) * anguloBalanco * perigo;
        }

        anguloAtual = Mathf.SmoothDampAngle(anguloAtual, alvo, ref velocidadeAngular, tempoSuavizacaoInclinacao);
        visualBarco.rotation = Quaternion.Euler(0f, 0f, anguloAtual);
    }

    void AtualizarUI()
    {
        if (barraPerigo != null) barraPerigo.value = perigo;
        if (textoPrompt != null) textoPrompt.text = "Aperte " + teclaEsperada + "!";
    }

    void Vencer()
    {
        EmAndamento = false;
        if (uiRaiz != null) uiRaiz.SetActive(false);
        if (bruxa != null) bruxa.Repelir();
        aoSucesso.Invoke();
    }

    void Virar()
    {
        EmAndamento = false;
        Virou = true;
        if (uiRaiz != null) uiRaiz.SetActive(false);
        aoVirar.Invoke(); // ligue sua tela de Game Over aqui
    }
}