using UnityEngine;

// Cria peixes novos de tempos em tempos, nas bordas da tela.
// Coloque em um objeto vazio da cena (ex: "GeradorDePeixes").
public class GeradorDePeixes : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] prefabsPeixes;      // peixes normais
    public GameObject prefabPeixeBruxa;     // peixe disfarcado (ehBruxa marcado)
    [Range(0f, 1f)] public float chanceDeBruxa = 0.12f;

    [Header("Nascimento")]
    public float intervalo = 1.5f;          // segundos entre um peixe e outro
    public int maximoDePeixes = 8;          // limite de peixes na cena
    public float limiteX = 8f;              // peixes viram quando passam disso
    public float yMinimo = -3f;             // faixa de altura (dentro da agua)
    public float yMaximo = -0.5f;

    float tempo;

    void Start()
    {
        Peixe.quantidade = 0;
    }

    void Update()
    {
        // nao cria peixe se o jogo acabou
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando) return;

        tempo += Time.deltaTime;

        if (tempo >= intervalo)
        {
            tempo = 0f;
            Gerar();
        }
    }

    void Gerar()
    {
        if (Peixe.quantidade >= maximoDePeixes) return;
        if (prefabsPeixes == null || prefabsPeixes.Length == 0) return;

        // escolhe o prefab: as vezes e o peixe-bruxa
        GameObject prefab;
        if (prefabPeixeBruxa != null && Random.value < chanceDeBruxa)
        {
            prefab = prefabPeixeBruxa;
        }
        else
        {
            prefab = prefabsPeixes[Random.Range(0, prefabsPeixes.Length)];
        }

        // escolhe o lado (um pouco fora da area onde o peixe vira)
        float lado = 1f;
        if (Random.value > 0.5f)
        {
            lado = -1f;
        }

        Vector3 posicao = new Vector3(lado * (limiteX + 1f), Random.Range(yMinimo, yMaximo), 0f);
        GameObject novo = Instantiate(prefab, posicao, Quaternion.identity);

        Peixe peixe = novo.GetComponent<Peixe>();
        if (peixe != null)
        {
            peixe.limiteX = limiteX;
            peixe.profundidade = Random.value;

            // peixe longe e menor e mais dificil de acertar, entao vale mais (ate 2x)
            peixe.valor = Mathf.RoundToInt(peixe.valor * Mathf.Lerp(1f, 2f, peixe.profundidade));
        }
    }
}
