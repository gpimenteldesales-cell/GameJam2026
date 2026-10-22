using System.Collections.Generic;
using UnityEngine;

// Peixe que nada de um lado pro outro.
// Se "ehBruxa" estiver marcado, ao ser pescado a bruxa aparece!
public class Peixe : MonoBehaviour
{
    public int valor = 10;                                   // pontos que o peixe vale
    [Range(0f, 1f)] public float profundidade = 0.5f;        // 0 = perto, 1 = longe
    public float velocidadeBase = 3f;
    public float limiteX = 8f;                               // ao passar disso, volta pelo outro lado
    public float tempoParaPescar = 1f;                       // peixe grande pode demorar mais

    [Header("Peixe-bruxa")]
    public bool ehBruxa = false;
    [Range(0f, 1f)] public float forcaDaPista = 0.6f;        // o quanto ele pisca roxo (0 = nada)

    // quantos peixes existem agora (o gerador usa isso)
    public static int quantidade = 0;

    float direcao = 1f;   // 1 = indo pra direita, -1 = indo pra esquerda
    SpriteRenderer sr;

    // todos os peixes que existem agora (o raio de pesca usa isso, sem precisar de collider)
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

    // "tamanho" do peixe para o raio de pesca (metade do maior lado do sprite, ja com a escala)
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

        // o peixe comeca nadando em direcao ao centro da tela
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
            // esta perto do centro: sorteia o lado
            if (Random.value > 0.5f)
            {
                direcao = 1f;
            }
            else
            {
                direcao = -1f;
            }
        }

        // peixe longe fica menor e mais atras
        float escala = Mathf.Lerp(1f, 0.4f, profundidade);
        transform.localScale = new Vector3(escala * direcao, escala, 1f);
        sr.sortingOrder = Mathf.RoundToInt(-profundidade * 10);
    }

    void Update()
    {
        // peixe longe tambem e mais lento
        float velocidade = velocidadeBase * Mathf.Lerp(1f, 0.4f, profundidade);
        transform.Translate(Vector3.right * direcao * velocidade * Time.deltaTime, Space.World);

        // chegou no limite? vira e volta
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

    // espelha o sprite para o peixe olhar para onde esta nadando
    void OlharParaDirecao()
    {
        Vector3 escala = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(escala.x) * direcao, escala.y, 1f);
    }

    // a "pista": o peixe-bruxa pisca roxo, de leve
    void PiscarRoxo()
    {
        float t = (Mathf.Sin(Time.time * 6f) + 1f) / 2f;   // vai de 0 a 1 e volta
        Color roxo = new Color(0.75f, 0.4f, 1f);
        sr.color = Color.Lerp(Color.white, roxo, t * forcaDaPista);
    }
}