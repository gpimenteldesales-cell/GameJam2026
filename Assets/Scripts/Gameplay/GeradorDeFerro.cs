using UnityEngine;

public class GeradorDeFerro : MonoBehaviour
{
    [Header("Prefab (opcional)")]
    public GameObject prefabFerro;

    [Header("Nascimento")]
    public float intervalo = 6f;
    public int maximoNaCena = 3;
    public float limiteX = 8f;
    public float yMinimo = -2.5f;
    public float yMaximo = -0.8f;

    [Header("Ajuda ao longo do jogo (sobe junto com o nivel das bruxas)")]
    public float reducaoIntervaloPorNivel = 0.5f;
    public float intervaloMinimo = 2.5f;
    public int nivelParaFerroExtra = 2;
    public int maximoAbsoluto = 8;

    float tempo;

    void Update()
    {
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando) return;

        tempo += Time.deltaTime;

        if (tempo >= IntervaloAtual())
        {
            tempo = 0f;
            Gerar();
        }
    }

    int NivelAtual()
    {
        if (GerenciadorBruxas.instancia != null) return GerenciadorBruxas.instancia.nivel;
        return 1;
    }

    float IntervaloAtual()
    {
        float valor = intervalo - (NivelAtual() - 1) * reducaoIntervaloPorNivel;
        return Mathf.Max(intervaloMinimo, valor) * GerenciadorUpgrades.MultiplicadorIntervaloFerro;
    }

    int MaximoAtual()
    {
        int extra = (NivelAtual() - 1) / Mathf.Max(1, nivelParaFerroExtra);
        return Mathf.Min(maximoAbsoluto, maximoNaCena + extra) + GerenciadorUpgrades.BonusFerroNaCena;
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

        ItemFerro item = novo.GetComponent<ItemFerro>();
        if (item == null)
        {
            item = novo.AddComponent<ItemFerro>();
        }

        item.limiteX = limiteX;

        float lado = 1f;
        if (!item.paraEsquerda)
        {
            lado = -1f;
        }

        novo.transform.position = new Vector3(lado * (limiteX + 1f), Random.Range(yMinimo, yMaximo), 0f);
    }
}