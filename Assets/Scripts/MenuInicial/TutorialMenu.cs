using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialMenu : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Texto com o conteudo da pagina")]
    public TMP_Text texto;
    [Tooltip("Opcional: mostra o numero da pagina, ex: 1/4")]
    public TMP_Text textoPagina;
    public Button botaoAnterior;
    public Button botaoProximo;

    [Header("Paginas (edite a vontade)")]
    [TextArea(4, 10)]
    public string[] paginas = new string[]
    {
        "PESCAR\n\nSegure o botao ESQUERDO do mouse e mire no peixe. Continue mirando ate fisgar: a linha vai ficando amarela. Peixes mais longe valem mais pontos!",
        "CUIDADO COM AS BRUXAS\n\nBruxas voam ate o barco para vira-lo. Quando uma atacar, aperte E e Q de forma ALTERNADA para equilibrar o barco. Se o barco virar, acabou!\n\nAlguns peixes sao bruxas disfarcadas...",
        "FERRO\n\nBotoes ESQUERDO + DIREITO juntos, com o mouse em cima de um ferro boiando, para pegar.\n\nSegure F para mirar e SOLTE para arremessar. Acertar a bruxa com ferro faz ela fugir!",
        "GUIA DO PESCADOR\n\nCada peixe rende pontos e perolas. Aperte B para abrir o livro e comprar upgrades.\n\nFaca o MAXIMO de pontos antes do barco virar. Boa pesca!"
    };

    int pagina;

    void OnEnable()
    {
        pagina = 0;
        Mostrar();
    }

    public void Proxima()
    {
        if (pagina < paginas.Length - 1)
        {
            pagina++;
            Mostrar();
        }
    }

    public void Anterior()
    {
        if (pagina > 0)
        {
            pagina--;
            Mostrar();
        }
    }

    void Mostrar()
    {
        if (paginas == null || paginas.Length == 0) return;

        if (texto != null) texto.text = paginas[pagina];
        if (textoPagina != null) textoPagina.text = (pagina + 1) + "/" + paginas.Length;

        if (botaoAnterior != null) botaoAnterior.interactable = pagina > 0;
        if (botaoProximo != null) botaoProximo.interactable = pagina < paginas.Length - 1;
    }
}