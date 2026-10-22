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

    [Header("Ajuda ao longo do jogo (sobe junto com o nivel das bruxas)")]
    public float reducaoIntervaloPorNivel = 0.5f;  // a cada nivel, nasce ferro mais rapido
    public float intervaloMinimo = 2.5f;           // nunca nasce mais rapido que isso
    public int nivelParaFerroExtra = 2;            // a cada N niveis, cabe +1 ferro boiando ao mesmo tempo
    public int maximoAbsoluto = 8;                 // limite de ferros boiando, aconteca o que acontecer

    float tempo;

    void Update()
    {
        // nao cria ferro se o jogo acabou
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando) return;

        tempo += Time.deltaTime;

        if (tempo >= IntervaloAtual())
        {
            tempo = 0f;
            Gerar();
        }
    }

    // nivel atual das bruxas (1 se nao houver GerenciadorBruxas na cena)
    int NivelAtual()
    {
        if (GerenciadorBruxas.instancia != null) return GerenciadorBruxas.instancia.nivel;
        return 1;
    }

    // quanto maior o nivel, mais rapido nasce ferro
    float IntervaloAtual()
    {
        float valor = intervalo - (NivelAtual() - 1) * reducaoIntervaloPorNivel;
        return Mathf.Max(intervaloMinimo, valor);
    }

    // quanto maior o nivel, mais ferros podem boiar ao mesmo tempo
    int MaximoAtual()
    {
        int extra = (NivelAtual() - 1) / Mathf.Max(1, nivelParaFerroExtra);
        return Mathf.Min(maximoAbsoluto, maximoNaCena + extra);
    }

    void Gerar()
    {
        if (ItemFerro.todos.Count >= MaximoAtual()) return;

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