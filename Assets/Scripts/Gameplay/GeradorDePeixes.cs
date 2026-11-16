using UnityEngine;

public class GeradorDePeixes : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] prefabsPeixes;
    public GameObject prefabPeixeBruxa;
    [Range(0f, 1f)] public float chanceDeBruxa = 0.12f;

    [Header("Peixe dourado (raro e valioso)")]
    [Tooltip("Opcional: prefab proprio do peixe dourado. Vazio = um peixe comum ganha brilho dourado.")]
    public GameObject prefabPeixeDourado;
    [Range(0f, 1f)] public float chanceDeDourado = 0.06f;
    [Tooltip("Quantas vezes o valor normal do peixe.")]
    public int multiplicadorValorDourado = 5;
    public float multiplicadorVelocidadeDourado = 1.6f;
    [Tooltip("Segundos ate o peixe dourado fugir. 0 = nunca foge.")]
    public float segundosDoDourado = 14f;
    public AudioClip somDouradoAparece;
    [Range(0f, 1f)] public float volumeSom = 1f;

    [Header("Nascimento")]
    public float intervalo = 1.5f;
    public int maximoDePeixes = 8;
    public float limiteX = 8f;
    public float yMinimo = -3f;
    public float yMaximo = -0.5f;

    float tempo;

    void Start()
    {
        Peixe.quantidade = 0;
        Peixe.douradosNaCena = 0;
    }

    void Update()
    {
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

        GameObject prefab;
        bool dourado = false;

        if (prefabPeixeBruxa != null && Random.value < chanceDeBruxa)
        {
            prefab = prefabPeixeBruxa;
        }
        else
        {
            // so um peixe dourado por vez na cena
            dourado = Peixe.douradosNaCena < 1 && Random.value < chanceDeDourado;

            if (dourado && prefabPeixeDourado != null)
            {
                prefab = prefabPeixeDourado;
            }
            else
            {
                prefab = prefabsPeixes[Random.Range(0, prefabsPeixes.Length)];
            }
        }

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

            peixe.valor = Mathf.RoundToInt(peixe.valor * Mathf.Lerp(1f, 2f, peixe.profundidade));

            if (dourado)
            {
                peixe.TornarDourado(multiplicadorValorDourado, multiplicadorVelocidadeDourado, segundosDoDourado);
                GerenciadorSons.Tocar(somDouradoAparece, volumeSom, "Peixe dourado: som ao aparecer");
            }
        }
    }
}