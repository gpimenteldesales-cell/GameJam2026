using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Painel de Leaderboard do menu inicial, com o mesmo visual do painel de Opcoes:
// usa o sprite do livro como fundo e uma copia do botao "Voltar" das Opcoes.
// Ele monta a propria tela por codigo (nao precisa montar nada no Inspector):
// o MenuPrincipal chama LeaderboardPainel.Criar(...) no Awake.
//
// O livro tem duas paginas: a esquerda mostra do 1o ao 5o e a direita do 6o ao 10o.
// Quando passa de 10 jogadores, aparecem os botoes para virar a folha.
public class LeaderboardPainel : MonoBehaviour
{
    class Linha
    {
        public GameObject raiz;
        public Image fundo;
        public TMP_Text posicao, nome, pontos, tempo;
    }

    // cores escuras, porque o fundo e papel claro
    static readonly Color TINTA = new Color(0.25f, 0.14f, 0.06f);
    static readonly Color TINTA_SUAVE = new Color(0.25f, 0.14f, 0.06f, 0.6f);
    static readonly Color TITULO = new Color(0.45f, 0.2f, 0.05f);
    static readonly Color OURO = new Color(0.62f, 0.42f, 0.02f);
    static readonly Color PRATA = new Color(0.33f, 0.36f, 0.44f);
    static readonly Color BRONZE = new Color(0.62f, 0.32f, 0.1f);

    const int LINHAS_POR_PAGINA_DO_LIVRO = 5;

    // paginas do livro (de 0 a 1 dentro do painel)
    const float ESQ_MIN = 0.075f, ESQ_MAX = 0.465f;
    const float DIR_MIN = 0.535f, DIR_MAX = 0.925f;

    // colunas (de 0 a 1 dentro de cada linha)
    const float POS_MIN = 0.00f, POS_MAX = 0.13f;
    const float NOME_MIN = 0.15f, NOME_MAX = 0.50f;
    const float PONTOS_MIN = 0.51f, PONTOS_MAX = 0.73f;
    const float TEMPO_MIN = 0.74f, TEMPO_MAX = 1.00f;

    Linha[] linhas;
    TMP_Text textoPagina, textoVazio;
    Button botaoAnterior, botaoProximo;
    int pagina;

    // visual copiado do Voltar das Opcoes (se nao achar, usa cores simples)
    Sprite spriteBotao;
    Color corTextoBotao = new Color(0.2f, 0.2f, 0.2f);

    public static LeaderboardPainel Criar(Transform canvas, MenuPrincipal menu)
    {
        GameObject raiz = new GameObject("PainelLeaderboard (automatico)", typeof(RectTransform));
        raiz.SetActive(false); // desligado enquanto monta, senao o OnEnable roda antes da hora
        raiz.transform.SetParent(canvas, false);

        // mesmo tamanho e posicao do painel de Opcoes
        RectTransform rt = raiz.GetComponent<RectTransform>();
        RectTransform modeloRect = null;
        if (menu != null && menu.painelOpcoes != null) modeloRect = menu.painelOpcoes.GetComponent<RectTransform>();

        if (modeloRect != null)
        {
            rt.anchorMin = modeloRect.anchorMin;
            rt.anchorMax = modeloRect.anchorMax;
            rt.pivot = modeloRect.pivot;
            rt.sizeDelta = modeloRect.sizeDelta;
            rt.anchoredPosition = modeloRect.anchoredPosition;
        }
        else
        {
            Esticar(rt);
        }

        LeaderboardPainel painel = raiz.AddComponent<LeaderboardPainel>();
        painel.Montar(menu);
        raiz.AddComponent<PainelPop>();

        return painel;
    }

    void OnEnable()
    {
        // abre na folha onde esta a ultima partida jogada (se ela entrou no ranking)
        pagina = 0;

        int posicao = Recorde.PosicaoUltima;
        if (posicao > 0) pagina = (posicao - 1) / Recorde.POR_PAGINA;

        Atualizar();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) Proxima();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Anterior();
    }

    public void Proxima()
    {
        if (pagina < Recorde.PaginasTotais - 1)
        {
            pagina++;
            Atualizar();
        }
    }

    public void Anterior()
    {
        if (pagina > 0)
        {
            pagina--;
            Atualizar();
        }
    }

    void Atualizar()
    {
        int total = Recorde.Total;
        int paginas = Recorde.PaginasTotais;

        pagina = Mathf.Clamp(pagina, 0, paginas - 1);

        for (int i = 0; i < linhas.Length; i++)
        {
            Linha l = linhas[i];
            int indice = pagina * Recorde.POR_PAGINA + i;

            if (indice >= total)
            {
                l.raiz.SetActive(false);
                continue;
            }

            l.raiz.SetActive(true);

            Recorde.Entrada e = Recorde.Pegar(indice);
            bool eVoce = e.id == Recorde.UltimaId;

            l.posicao.text = "#" + (indice + 1);
            l.nome.text = e.nome;
            l.pontos.text = e.pontos.ToString();
            l.tempo.text = GerenciadorJogo.FormatarTempo(e.tempo);

            Color cor = TINTA;
            if (indice == 0) cor = OURO;
            else if (indice == 1) cor = PRATA;
            else if (indice == 2) cor = BRONZE;

            l.posicao.color = l.nome.color = l.pontos.color = l.tempo.color = cor;

            // a partida que acabou de ser jogada fica destacada
            if (eVoce) l.fundo.color = new Color(1f, 0.55f, 0.1f, 0.35f);
            else if (i % 2 == 0) l.fundo.color = new Color(0.4f, 0.25f, 0.1f, 0.08f);
            else l.fundo.color = new Color(0f, 0f, 0f, 0f);
        }

        textoVazio.gameObject.SetActive(total == 0);

        // so mostra os botoes de pagina quando existe mais de uma
        bool temPaginas = paginas > 1;
        botaoAnterior.gameObject.SetActive(temPaginas);
        botaoProximo.gameObject.SetActive(temPaginas);
        textoPagina.gameObject.SetActive(temPaginas);

        textoPagina.text = "Pagina " + (pagina + 1) + " / " + paginas;
        botaoAnterior.interactable = pagina > 0;
        botaoProximo.interactable = pagina < paginas - 1;
    }

    // ---------- Montagem da tela ----------

    void Montar(MenuPrincipal menu)
    {
        // fundo: o mesmo livro do painel de Opcoes
        Image fundoLivro = gameObject.AddComponent<Image>();
        Image modeloLivro = null;
        if (menu != null && menu.painelOpcoes != null) modeloLivro = menu.painelOpcoes.GetComponent<Image>();

        if (modeloLivro != null && modeloLivro.sprite != null)
        {
            fundoLivro.sprite = modeloLivro.sprite;
            fundoLivro.preserveAspect = modeloLivro.preserveAspect;
            fundoLivro.color = Color.white;
        }
        else
        {
            fundoLivro.color = new Color(0.96f, 0.84f, 0.55f); // papel liso, se nao achar o livro
        }

        // botao Voltar: uma copia do Voltar das Opcoes (ja vem com o clique ligado)
        Transform voltarModelo = null;
        if (menu != null && menu.painelOpcoes != null) voltarModelo = menu.painelOpcoes.transform.Find("Voltar");

        if (voltarModelo != null)
        {
            GameObject copia = Instantiate(voltarModelo.gameObject, transform, false);
            copia.name = "Voltar";

            Image imgVoltar = copia.GetComponent<Image>();
            if (imgVoltar != null) spriteBotao = imgVoltar.sprite;

            TMP_Text textoVoltar = copia.GetComponentInChildren<TMP_Text>();
            if (textoVoltar != null) corTextoBotao = textoVoltar.color;
        }
        else
        {
            Button voltar = CriarBotao(transform, "Voltar", "Voltar", 0.065f, 0.11f, 0.313f, 0.21f);
            if (menu != null) voltar.onClick.AddListener(menu.FecharPainel);
        }

        CriarTexto(transform, "Titulo", "RANKING", ESQ_MIN, 0.83f, ESQ_MAX, 0.94f, TextAlignmentOptions.Center, 80f, TITULO);

        // cabecalho das colunas, nas duas paginas do livro
        for (int p = 0; p < 2; p++)
        {
            float xMin = p == 0 ? ESQ_MIN : DIR_MIN;
            float xMax = p == 0 ? ESQ_MAX : DIR_MAX;

            RectTransform cab = Retangulo("Cabecalho " + (p + 1), transform, xMin, 0.76f, xMax, 0.82f);
            CriarTexto(cab, "Pos", "#", POS_MIN, 0f, POS_MAX, 1f, TextAlignmentOptions.Center, 30f, TINTA_SUAVE);
            CriarTexto(cab, "Nome", "Nome", NOME_MIN, 0f, NOME_MAX, 1f, TextAlignmentOptions.Left, 30f, TINTA_SUAVE);
            CriarTexto(cab, "Pontos", "Pontos", PONTOS_MIN, 0f, PONTOS_MAX, 1f, TextAlignmentOptions.Right, 30f, TINTA_SUAVE);
            CriarTexto(cab, "Tempo", "Tempo", TEMPO_MIN, 0f, TEMPO_MAX, 1f, TextAlignmentOptions.Right, 30f, TINTA_SUAVE);
        }

        // 10 linhas: 5 na pagina esquerda do livro e 5 na direita
        linhas = new Linha[Recorde.POR_PAGINA];
        float alturaLinha = 0.52f / LINHAS_POR_PAGINA_DO_LIVRO;

        for (int i = 0; i < linhas.Length; i++)
        {
            int paginaDoLivro = i / LINHAS_POR_PAGINA_DO_LIVRO;
            int posicaoNaPagina = i % LINHAS_POR_PAGINA_DO_LIVRO;

            float xMin = paginaDoLivro == 0 ? ESQ_MIN : DIR_MIN;
            float xMax = paginaDoLivro == 0 ? ESQ_MAX : DIR_MAX;
            float yMax = 0.76f - posicaoNaPagina * alturaLinha;
            float yMin = yMax - alturaLinha;

            Linha l = new Linha();
            RectTransform r = Retangulo("Linha " + (i + 1), transform, xMin, yMin, xMax, yMax);
            l.raiz = r.gameObject;
            l.fundo = l.raiz.AddComponent<Image>();
            l.fundo.raycastTarget = false;

            l.posicao = CriarTexto(r, "Pos", "", POS_MIN, 0f, POS_MAX, 1f, TextAlignmentOptions.Center, 36f, TINTA);
            l.nome = CriarTexto(r, "Nome", "", NOME_MIN, 0f, NOME_MAX, 1f, TextAlignmentOptions.Left, 36f, TINTA);
            l.nome.richText = false; // ninguem usa <b> no nome pra bagunçar o ranking
            l.pontos = CriarTexto(r, "Pontos", "", PONTOS_MIN, 0f, PONTOS_MAX, 1f, TextAlignmentOptions.Right, 36f, TINTA);
            l.tempo = CriarTexto(r, "Tempo", "", TEMPO_MIN, 0f, TEMPO_MAX, 1f, TextAlignmentOptions.Right, 36f, TINTA);

            linhas[i] = l;
        }

        textoVazio = CriarTexto(transform, "Vazio", "Ninguem jogou ainda.\nVa pescar!", 0.15f, 0.4f, 0.85f, 0.6f,
            TextAlignmentOptions.Center, 56f, TINTA_SUAVE);

        // navegacao: "Pagina 1 / 3" no topo da pagina direita, botoes embaixo
        textoPagina = CriarTexto(transform, "Pagina", "", DIR_MIN, 0.83f, DIR_MAX, 0.94f, TextAlignmentOptions.Center, 44f, TINTA);
        botaoAnterior = CriarBotao(transform, "Anterior", "< Anterior", 0.52f, 0.11f, 0.72f, 0.21f);
        botaoProximo = CriarBotao(transform, "Proximo", "Proxima >", 0.725f, 0.11f, 0.925f, 0.21f);

        botaoAnterior.onClick.AddListener(Anterior);
        botaoProximo.onClick.AddListener(Proxima);
    }

    static RectTransform Retangulo(string nome, Transform pai, float xMin, float yMin, float xMax, float yMax)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TMP_Text CriarTexto(Transform pai, string nome, string conteudo, float xMin, float yMin, float xMax, float yMax,
        TextAlignmentOptions alinhamento, float tamanhoMax, Color cor)
    {
        RectTransform rt = Retangulo(nome, pai, xMin, yMin, xMax, yMax);

        TextMeshProUGUI texto = rt.gameObject.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.alignment = alinhamento;
        texto.color = cor;
        texto.enableAutoSizing = true; // o texto se ajusta ao tamanho da caixa
        texto.fontSizeMin = 10f;
        texto.fontSizeMax = tamanhoMax;
        texto.raycastTarget = false;
        return texto;
    }

    // botao com o mesmo sprite do Voltar das Opcoes (ou um retangulo marrom, se nao achou o sprite)
    Button CriarBotao(Transform pai, string nome, string rotulo, float xMin, float yMin, float xMax, float yMax)
    {
        RectTransform rt = Retangulo(nome, pai, xMin, yMin, xMax, yMax);

        Image img = rt.gameObject.AddComponent<Image>();
        Color corRotulo = corTextoBotao;

        if (spriteBotao != null)
        {
            img.sprite = spriteBotao;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.6f, 0.38f, 0.18f);
            corRotulo = Color.white;
        }

        Button botao = rt.gameObject.AddComponent<Button>();
        botao.targetGraphic = img;

        CriarTexto(rt, "Texto", rotulo, 0.075f, 0.075f, 0.925f, 0.925f, TextAlignmentOptions.Center, 60f, corRotulo);
        return botao;
    }
}