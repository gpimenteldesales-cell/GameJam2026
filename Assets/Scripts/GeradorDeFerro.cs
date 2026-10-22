using UnityEngine;

// Faz pedacos de ferro aparecerem boiando no mar, de tempos em tempos.
// Coloque em um objeto vazio da cena (ex: "GeradorDeFerro").
public class GeradorDeFerro : MonoBehaviour
{
    [Header("Prefab (opcional)")]
    public GameObject prefabFerro;          // sem prefab, cria um quadradinho cinza

    [Header("Nascimento")]
    public float intervalo = 6f;            // segundos entre um ferro e outro
    public int maximoNaCena = 3;            // limite de ferros boiando ao mesmo tempo
    public float limiteX = 8f;              // ferros somem depois dessa distancia (+1)
    public float yMinimo = -2.5f;           // faixa de altura (dentro da agua)
    public float yMaximo = -0.8f;

    float tempo;

    void Update()
    {
        // nao cria ferro se o jogo acabou
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
        if (ItemFerro.todos.Count >= maximoNaCena) return;

        GameObject novo;
        if (prefabFerro != null)
        {
            novo = Instantiate(prefabFerro);
        }
        else
        {
            novo = ItemFerro.CriarPlaceholder("Ferro", 0.4f, new Color(0.6f, 0.65f, 0.7f));
        }

        // se o prefab nao tem o script do ferro, coloca agora
        ItemFerro item = novo.GetComponent<ItemFerro>();
        if (item == null)
        {
            item = novo.AddComponent<ItemFerro>();
        }

        item.limiteX = limiteX;

        // nasce no lado de onde a correnteza vem
        float lado = 1f;
        if (!item.paraEsquerda)
        {
            lado = -1f;
        }

        novo.transform.position = new Vector3(lado * (limiteX + 1f), Random.Range(yMinimo, yMaximo), 0f);
    }
}