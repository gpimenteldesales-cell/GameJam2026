using System.Collections.Generic;
using UnityEngine;

public class Peixe : MonoBehaviour
{
    public int valor = 10;
    [Range(0f, 1f)] public float profundidade = 0.5f;
    public float velocidadeBase = 3f;
    public float limiteX = 8f;
    public float tempoParaPescar = 1f;

    [Header("Peixe-bruxa")]
    public bool ehBruxa = false;
    [Range(0f, 1f)] public float forcaDaPista = 0.6f;

    [Header("Peixe dourado (o GeradorDePeixes liga isso sozinho)")]
    public bool ehDourado = false;
    public Color corOuro = new Color(1f, 0.85f, 0.2f);
    public float velocidadeDoBrilho = 5f;

    public static int quantidade = 0;
    public static int douradosNaCena = 0;

    float direcao = 1f;
    float vidaRestante;
    SpriteRenderer sr;

    public static readonly List<Peixe> todos = new List<Peixe>();

    void Awake()
    {
        quantidade++;
    }

    void OnEnable()
    {
        todos.Add(this);
    }

    void OnDisable()
    {
        todos.Remove(this);
    }

    void OnDestroy()
    {
        quantidade--;
        if (ehDourado) douradosNaCena = Mathf.Max(0, douradosNaCena - 1);
    }

    // Transforma este peixe em peixe dourado: vale mais, nada mais rapido e foge depois de um tempo.
    public void TornarDourado(int multiplicadorValor, float multiplicadorVelocidade, float segundosDeVida)
    {
        if (ehDourado) return;

        ehDourado = true;
        douradosNaCena++;

        valor *= Mathf.Max(1, multiplicadorValor);
        velocidadeBase *= Mathf.Max(0.1f, multiplicadorVelocidade);
        vidaRestante = Mathf.Max(0f, segundosDeVida);
    }

    public float RaioDeAcerto()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return 0.5f;

        Vector3 meio = sr.bounds.extents;
        return Mathf.Max(meio.x, meio.y);
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        float x = transform.position.x;
        if (x > 1f)
        {
            direcao = -1f;
        }
        else if (x < -1f)
        {
            direcao = 1f;
        }
        else
        {
            if (Random.value > 0.5f)
            {
                direcao = 1f;
            }
            else
            {
                direcao = -1f;
            }
        }

        float escala = Mathf.Lerp(1f, 0.4f, profundidade);
        transform.localScale = new Vector3(escala * direcao, escala, 1f);
        sr.sortingOrder = Mathf.RoundToInt(-profundidade * 10);
    }

    void Update()
    {
        float velocidade = velocidadeBase * Mathf.Lerp(1f, 0.4f, profundidade);
        transform.Translate(Vector3.right * direcao * velocidade * Time.deltaTime, Space.World);

        if (transform.position.x > limiteX)
        {
            direcao = -1f;
            OlharParaDirecao();
        }
        else if (transform.position.x < -limiteX)
        {
            direcao = 1f;
            OlharParaDirecao();
        }

        if (ehBruxa)
        {
            PiscarRoxo();
        }

        if (ehDourado)
        {
            BrilharOuro();
        }
    }

    void BrilharOuro()
    {
        if (sr == null) return;

        float t = (Mathf.Sin(Time.time * velocidadeDoBrilho) + 1f) / 2f;
        Color cor = Color.Lerp(Color.white, corOuro, 0.4f + 0.6f * t);

        if (vidaRestante > 0f)
        {
            vidaRestante -= Time.deltaTime;

            if (vidaRestante <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // nos ultimos 3 segundos pisca, avisando que vai fugir
            if (vidaRestante < 3f && Mathf.FloorToInt(Time.time * 8f) % 2 == 0)
            {
                cor.a = 0.35f;
            }
        }

        sr.color = cor;
    }

    void OlharParaDirecao()
    {
        Vector3 escala = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(escala.x) * direcao, escala.y, 1f);
    }

    void PiscarRoxo()
    {
        float t = (Mathf.Sin(Time.time * 6f) + 1f) / 2f;
        Color roxo = new Color(0.75f, 0.4f, 1f);
        sr.color = Color.Lerp(Color.white, roxo, t * forcaDaPista);
    }
}