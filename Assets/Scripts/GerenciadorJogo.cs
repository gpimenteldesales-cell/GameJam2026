using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Controla o jogo: pontos, meta, vitoria, derrota e reiniciar.
// Coloque este script em UM objeto vazio da cena (ex: "Gerenciador").
public class GerenciadorJogo : MonoBehaviour
{
    // Qualquer script pode usar: GerenciadorJogo.instancia
    public static GerenciadorJogo instancia;

    [Header("Meta")]
    public int metaDePontos = 300;       // oferenda que as bruxas pedem

    [Header("Ligacoes")]
    public EventoEquilibrioBarco eventoBarco;
    public BruxaVoadora bruxa;

    [Header("UI")]
    public TMP_Text textoPontos;
    public Slider barraOferenda;         // opcional
    public GameObject painelVitoria;
    public GameObject painelDerrota;
    public float atrasoVitoria = 1f;
    public float atrasoDerrota = 2f;     // espera o barco afundar

    [Header("Estado (so pra ver no Inspector)")]
    public int pontos;
    public bool jogando = true;

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
        // quando o jogo acabou, apertar R reinicia
        if (!jogando && Input.GetKeyDown(KeyCode.R))
        {
            Reiniciar();
        }
    }

    // Chamado pelo RaioDePesca quando um peixe e pescado
    public void AdicionarPontos(int valor)
    {
        if (!jogando) return;

        pontos += valor;
        AtualizarUI();

        if (pontos >= metaDePontos)
        {
            Vitoria();
        }
    }

    // Vai de 0 a 1: quanto da meta ja foi conquistado
    public float ProgressoMeta()
    {
        return Mathf.Clamp01((float)pontos / metaDePontos);
    }

    void Vitoria()
    {
        jogando = false;

        // para o evento do barco e a bruxa foge
        if (eventoBarco != null) eventoBarco.Cancelar();
        if (bruxa != null) bruxa.Repelir();

        Invoke(nameof(MostrarVitoria), atrasoVitoria);
    }

    // Chamado pelo EventoEquilibrioBarco quando o barco vira
    public void Derrota()
    {
        if (!jogando) return;

        jogando = false;
        Paralaxe.multiplicadorGlobal = 0f;   // o cenario para

        Invoke(nameof(MostrarDerrota), atrasoDerrota);
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
            textoPontos.text = "Oferenda: " + pontos + " / " + metaDePontos;
        }

        if (barraOferenda != null)
        {
            barraOferenda.value = ProgressoMeta();
        }
    }
}
