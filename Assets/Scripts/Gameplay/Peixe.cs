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

    public static int quantidade = 0;

    float direcao = 1f;
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