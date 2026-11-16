using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pagina de pause com o SEU sprite. Aparece em dois casos:
//   - quando o jogador aperta ESC durante a partida (pausa o jogo)
//   - quando o jogador perde (mostra o resultado e o campo de nome do ranking)
//
// No pause ha 4 botoes: Continuar, Reiniciar, Opcoes e Menu.
// "Opcoes" abre, dentro da propria pagina, o volume e os atalhos (usa o OpcoesMenu.cs e o
// ConfiguracoesJogo.cs, os mesmos do menu inicial). ESC ou "Voltar" volta para o pause.
//
// COMO USAR: crie um GameObject vazio na cena do jogo (ex: "PainelPause"), coloque este script nele
// e arraste o seu sprite para o campo "Sprite Painel". Se nao fizer isso, o jogo cria um painel
// simples sozinho (sem sprite). Tambem funciona sem arrastar: coloque a imagem em
// Assets/Resources/Sprites com o nome  PainelPause  (Texture Type = Sprite).
//
// O painel se ajusta ao formato (proporcao) do seu sprite, entao qualquer tamanho serve.
public class PainelPause : MonoBehaviour
{
    // true enquanto o jogo esta pausado (os outros scripts usam isso pra ignorar o mouse/teclado)
    public static bool aberto;

    static PainelPause instancia;

    [Header("Seu visual")]
    [Tooltip("O sprite da pagina de pause. Vazio = painel azul simples.")]
    public Sprite spritePainel;
    [Tooltip("Opcional: sprite dos botoes. Vazio = botoes marrons simples.")]
    public Sprite spriteBotao;
    [Tooltip("Desligue se o seu sprite ja tem o titulo desenhado.")]
    public bool mostrarTitulo = true;
    public Color corTexto = Color.white;

    [Header("Textos")]
    public string tituloPause = "PAUSADO";
    public string tituloOpcoes = "OPCOES";
    [Tooltip("Nome da cena do menu inicial (precisa estar no Build Settings).")]
    public string nomeCenaMenu = "TelaInicial";

    GameObject raiz;
    GameObject paginaOpcoes;
    TMP_Text textoTitulo, textoResultado, textoNome, textoDica;
    TMP_InputField campoNome;
    Button botaoContinuar, botaoReiniciar, botaoOpcoes, botaoJogarDeNovo, botaoMenu;
    bool tutorialNoQuadroAnterior;
    bool opcoesAbertas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        aberto = false;
        instancia = null;
    }

    // devolve o painel da cena (ou cria um simples se nao existir nenhum)
    public static PainelPause Obter()
    {
        if (instancia == null)
        {
            GameObject obj = new GameObject("PainelPause (automatico)");
            obj.AddComponent<PainelPause>(); // o Awake guarda em 'instancia'
        }

        return instancia;
    }

    // o jogador esta digitando o nome? (pra o R nao reiniciar no meio do nome)
    public static bool Digitando
    {
        get { return instancia != null && instancia.campoNome != null && instancia.campoNome.isFocused; }
    }

    // grava o nome digitado no ranking
    public static void SalvarNome()
    {
        if (instancia == null || instancia.campoNome == null) return;
        if (!instancia.campoNome.gameObject.activeInHierarchy) return;

        Recorde.DefinirNomeUltima(instancia.campoNome.text);
    }

    // despausa tudo (chamado antes de reiniciar ou trocar de cena)
    public static void Liberar()
    {
        aberto = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Awake()
    {
        // se ja existe um painel automatico, o que voce colocou na cena vence
        if (instancia != null && instancia != this) Destroy(instancia.gameObject);

        instancia = this;
        aberto = false;
    }

    void OnDestroy()
    {
        if (instancia == this)
        {
            instancia = null;

            if (aberto) Liberar();
        }
    }

    void Update()
    {
        // o tutorial usa ESC pra pular: ignora o ESC enquanto ele esta ativo (e no quadro seguinte)
        bool tutorial = TutorialInicial.ativo;
        bool bloqueado = tutorial || tutorialNoQuadroAnterior;
        tutorialNoQuadroAnterior = tutorial;

        if (bloqueado || !Input.GetKeyDown(KeyCode.Escape)) return;

        if (aberto)
        {
            // ESC que cancela a escolha de uma tecla nova nao deve fechar nada
            if (OpcoesMenu.BloqueiaEscape) return;

            if (opcoesAbertas) FecharOpcoes();
            else Continuar();
        }
        else
        {
            Pausar();
        }
    }

    // ---------- Pause (ESC) ----------

    public void Pausar()
    {
        if (aberto) return;

        GerenciadorJogo jogo = GerenciadorJogo.instancia;
        if (jogo == null || !jogo.jogando) return;
        if (GerenciadorUpgrades.livroAberto) return;

        Construir();

        Configurar(tituloPause, ResumoDoJogo(), corTexto, false, false);

        aberto = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        raiz.SetActive(true);
    }

    public void Continuar()
    {
        if (!aberto) return;

        Liberar();
        if (raiz != null) raiz.SetActive(false);
    }

    string ResumoDoJogo()
    {
        GerenciadorJogo jogo = GerenciadorJogo.instancia;
        if (jogo == null) return "";

        return "Pontos: " + jogo.pontos + "\nTempo: " + GerenciadorJogo.FormatarTempo(jogo.tempo);
    }

    // ---------- Opcoes (dentro do pause) ----------

    void AbrirOpcoes()
    {
        if (!aberto || paginaOpcoes == null) return;

        opcoesAbertas = true;

        textoTitulo.text = tituloOpcoes;
        textoResultado.gameObject.SetActive(false);
        textoNome.gameObject.SetActive(false);
        campoNome.gameObject.SetActive(false);
        textoDica.gameObject.SetActive(false);

        botaoContinuar.gameObject.SetActive(false);
        botaoReiniciar.gameObject.SetActive(false);
        botaoOpcoes.gameObject.SetActive(false);
        botaoJogarDeNovo.gameObject.SetActive(false);
        botaoMenu.gameObject.SetActive(false);

        paginaOpcoes.SetActive(true); // o OpcoesMenu atualiza volume e teclas no OnEnable
    }

    void FecharOpcoes()
    {
        if (!opcoesAbertas) return;

        // o Configurar desliga a pagina de opcoes e religa os botoes do pause
        Configurar(tituloPause, ResumoDoJogo(), corTexto, false, false);
    }

    // ---------- Derrota ----------

    public void MostrarDerrota(string titulo, string resultado, Color corResultado, bool pedirNome)
    {
        Construir();
        Configurar(titulo, resultado, corResultado, pedirNome, true);
        raiz.SetActive(true);
    }

    // ---------- Botoes ----------

    void ReiniciarJogo()
    {
        if (GerenciadorJogo.instancia != null)
        {
            GerenciadorJogo.instancia.Reiniciar();
            return;
        }

        SalvarNome();
        Liberar();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void IrParaMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(nomeCenaMenu))
        {
            Debug.LogError("PainelPause: a cena '" + nomeCenaMenu + "' nao esta no Build Settings (ou o nome esta errado)!");
            return;
        }

        SalvarNome();
        Liberar();
        SceneManager.LoadScene(nomeCenaMenu);
    }

    // ---------- Montagem da tela ----------

    // liga/desliga os textos e botoes de cada modo (pause ou derrota)
    void Configurar(string titulo, string resultado, Color corResultado, bool pedirNome, bool derrota)
    {
        opcoesAbertas = false;
        if (paginaOpcoes != null) paginaOpcoes.SetActive(false);

        textoTitulo.gameObject.SetActive(mostrarTitulo);
        textoTitulo.text = titulo;

        textoResultado.gameObject.SetActive(resultado != "");
        textoResultado.text = resultado;
        textoResultado.color = corResultado;

        textoNome.gameObject.SetActive(pedirNome);
        campoNome.gameObject.SetActive(pedirNome);
        if (pedirNome) campoNome.text = Recorde.NomeSalvo;

        textoDica.gameObject.SetActive(derrota);

        botaoContinuar.gameObject.SetActive(false);
        botaoReiniciar.gameObject.SetActive(false);
        botaoOpcoes.gameObject.SetActive(false);
        botaoJogarDeNovo.gameObject.SetActive(false);
        botaoMenu.gameObject.SetActive(false);

        if (derrota) Distribuir(botaoJogarDeNovo, botaoMenu);
        else Distribuir(botaoContinuar, botaoReiniciar, botaoOpcoes, botaoMenu);
    }

    // coloca os botoes lado a lado na parte de baixo da pagina
    void Distribuir(params Button[] botoes)
    {
        float largura = 0.8f / botoes.Length;

        for (int i = 0; i < botoes.Length; i++)
        {
            RectTransform rt = botoes[i].GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f + i * largura + 0.01f, 0.1f);
            rt.anchorMax = new Vector2(0.1f + (i + 1) * largura - 0.01f, 0.26f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            botoes[i].gameObject.SetActive(true);
        }
    }

    void Construir()
    {
        if (raiz != null)
        {
            // painel completo: reaproveita
            if (textoTitulo != null && paginaOpcoes != null) return;

            // painel que ficou pela metade (deu erro na montagem): joga fora e refaz
            raiz.SetActive(false);
            Destroy(raiz);
            raiz = null;
        }

        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        if (spritePainel == null) spritePainel = Resources.Load<Sprite>("Sprites/PainelPause");

        // IMPORTANTE: monta com o objeto LIGADO (o TextMeshPro so carrega a fonte no Awake)
        // e so desliga no fim da funcao.
        raiz = new GameObject("PainelPause", typeof(RectTransform));
        raiz.transform.SetParent(transform, false);

        Canvas canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;

        raiz.AddComponent<GraphicRaycaster>();

        // fundo escurecido (tambem impede de clicar no jogo por tras)
        Image escurecer = raiz.AddComponent<Image>();
        escurecer.color = new Color(0f, 0f, 0f, 0.65f);

        // o cartao se ajusta ao formato do sprite, sem esticar
        RectTransform area = Retangulo("Area", raiz.transform, 0.04f, 0.04f, 0.96f, 0.96f);
        RectTransform cartao = Retangulo("Cartao", area, 0f, 0f, 1f, 1f);

        Image imagem = cartao.gameObject.AddComponent<Image>();
        float aspecto = 1.6f;

        if (spritePainel != null)
        {
            imagem.sprite = spritePainel;
            imagem.color = Color.white;
            aspecto = spritePainel.rect.width / spritePainel.rect.height;
        }
        else
        {
            imagem.color = new Color(0.1f, 0.16f, 0.27f, 0.97f);
        }

        AspectRatioFitter ajuste = cartao.gameObject.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ajuste.aspectRatio = aspecto;

        cartao.gameObject.AddComponent<PainelPop>();

        // textos (tudo em porcentagem do cartao, entao funciona em qualquer tamanho de tela)
        textoTitulo = CriarTexto(cartao, "Titulo", "", 0.1f, 0.76f, 0.9f, 0.94f, 90f);
        textoResultado = CriarTexto(cartao, "Resultado", "", 0.1f, 0.48f, 0.9f, 0.76f, 52f);
        textoNome = CriarTexto(cartao, "TextoNome", "Seu nome no ranking:", 0.1f, 0.40f, 0.9f, 0.47f, 34f);
        campoNome = CriarCampoNome(cartao, 0.3f, 0.29f, 0.7f, 0.40f);
        textoDica = CriarTexto(cartao, "Dica", "ou aperte R", 0.3f, 0.03f, 0.7f, 0.09f, 28f);

        botaoContinuar = CriarBotao(cartao, "Continuar", "Continuar", Continuar);
        botaoReiniciar = CriarBotao(cartao, "Reiniciar", "Reiniciar", ReiniciarJogo);
        botaoOpcoes = CriarBotao(cartao, "Opcoes", "Opcoes", AbrirOpcoes);
        botaoJogarDeNovo = CriarBotao(cartao, "JogarDeNovo", "Jogar de novo", ReiniciarJogo);
        botaoMenu = CriarBotao(cartao, "Menu", "Menu", IrParaMenu);

        ConstruirOpcoes(cartao);

        raiz.SetActive(false); // pronto: fica escondido ate chamarem Pausar / MostrarDerrota
    }

    // Pagina de opcoes: volume + 4 atalhos. Quem faz o trabalho e o OpcoesMenu (o mesmo do menu inicial).
    void ConstruirOpcoes(RectTransform cartao)
    {
        RectTransform pagina = Retangulo("PaginaOpcoes", cartao, 0f, 0f, 1f, 1f);
        paginaOpcoes = pagina.gameObject;

        // volume
        TMP_Text rotuloVolume = CriarTexto(pagina, "RotuloVolume", "Volume", 0.08f, 0.62f, 0.30f, 0.74f, 40f);
        rotuloVolume.alignment = TextAlignmentOptions.MidlineLeft;

        Slider slider = CriarSlider(pagina, "SliderVolume", 0.32f, 0.62f, 0.80f, 0.74f);
        TMP_Text textoVolume = CriarTexto(pagina, "TextoVolume", "100%", 0.82f, 0.62f, 0.94f, 0.74f, 36f);

        // atalhos (um por linha: nome da acao + botao com a tecla atual)
        AcaoTecla[] acoes =
        {
            AcaoTecla.EquilibrioA,
            AcaoTecla.EquilibrioB,
            AcaoTecla.AbrirLivro,
            AcaoTecla.Arremessar
        };

        List<OpcoesMenu.LinhaAtalho> linhas = new List<OpcoesMenu.LinhaAtalho>();

        for (int i = 0; i < acoes.Length; i++)
        {
            float yMax = 0.60f - i * 0.095f;
            float yMin = yMax - 0.085f;

            TMP_Text nome = CriarTexto(pagina, "NomeAtalho" + i, "", 0.08f, yMin, 0.54f, yMax, 34f);
            nome.alignment = TextAlignmentOptions.MidlineLeft;

            Button botaoTecla = CriarBotao(pagina, "Tecla" + i, "", null);
            Ancorar(botaoTecla.GetComponent<RectTransform>(), 0.58f, yMin, 0.92f, yMax);

            OpcoesMenu.LinhaAtalho linha = new OpcoesMenu.LinhaAtalho();
            linha.acao = acoes[i];
            linha.botao = botaoTecla;
            linha.textoTecla = botaoTecla.GetComponentInChildren<TMP_Text>();
            linha.textoNome = nome;
            linhas.Add(linha);
        }

        TMP_Text aviso = CriarTexto(pagina, "Aviso", "", 0.08f, 0.17f, 0.92f, 0.24f, 28f);

        Button restaurar = CriarBotao(pagina, "RestaurarAtalhos", "Restaurar atalhos", null);
        Ancorar(restaurar.GetComponent<RectTransform>(), 0.08f, 0.04f, 0.48f, 0.15f);

        Button voltar = CriarBotao(pagina, "Voltar", "Voltar", FecharOpcoes);
        Ancorar(voltar.GetComponent<RectTransform>(), 0.52f, 0.04f, 0.92f, 0.15f);

        // O OpcoesMenu le os campos no Awake. Por isso ele nasce num objeto DESLIGADO, recebe tudo
        // e so entao e ligado.
        GameObject logica = new GameObject("LogicaOpcoes");
        logica.transform.SetParent(pagina, false);
        logica.SetActive(false);

        OpcoesMenu menu = logica.AddComponent<OpcoesMenu>();
        menu.sliderVolume = slider;
        menu.textoVolume = textoVolume;
        menu.linhasAtalho = linhas;
        menu.textoAviso = aviso;
        menu.botaoRestaurar = restaurar;

        logica.SetActive(true);

        paginaOpcoes.SetActive(false);
    }

    static void Ancorar(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static RectTransform Retangulo(string nome, Transform pai, float xMin, float yMin, float xMax, float yMax)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        Ancorar(rt, xMin, yMin, xMax, yMax);
        return rt;
    }

    TMP_Text CriarTexto(Transform pai, string nome, string conteudo, float xMin, float yMin, float xMax, float yMax, float tamanhoMax)
    {
        RectTransform rt = Retangulo(nome, pai, xMin, yMin, xMax, yMax);

        TextMeshProUGUI texto = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) texto.font = TMP_Settings.defaultFontAsset;

        texto.text = conteudo;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = corTexto;
        texto.enableAutoSizing = true; // o texto se ajusta ao tamanho da caixa
        texto.fontSizeMin = 10f;
        texto.fontSizeMax = tamanhoMax;
        texto.raycastTarget = false;

        // contorno escuro, pra ler bem em cima de qualquer desenho
        if (texto.fontSharedMaterial != null)
        {
            texto.outlineWidth = 0.2f;
            texto.outlineColor = new Color32(0, 0, 0, 255);
        }

        return texto;
    }

    Button CriarBotao(Transform pai, string nome, string rotulo, UnityAction acao)
    {
        RectTransform rt = Retangulo(nome, pai, 0f, 0f, 1f, 1f);

        Image img = rt.gameObject.AddComponent<Image>();

        if (spriteBotao != null)
        {
            img.sprite = spriteBotao;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.6f, 0.38f, 0.18f);
        }

        Button botao = rt.gameObject.AddComponent<Button>();
        botao.targetGraphic = img;
        if (acao != null) botao.onClick.AddListener(acao);

        CriarTexto(rt, "Texto", rotulo, 0.075f, 0.075f, 0.925f, 0.925f, 60f);
        return botao;
    }

    // slider 0..1 montado por codigo (trilho, preenchimento e puxador)
    Slider CriarSlider(Transform pai, string nome, float xMin, float yMin, float xMax, float yMax)
    {
        RectTransform rt = Retangulo(nome, pai, xMin, yMin, xMax, yMax);

        // area invisivel que aumenta o lugar onde da pra clicar
        Image alvo = rt.gameObject.AddComponent<Image>();
        alvo.color = new Color(1f, 1f, 1f, 0f);

        RectTransform trilho = Retangulo("Trilho", rt, 0f, 0.35f, 1f, 0.65f);
        trilho.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);

        RectTransform areaPreenchimento = Retangulo("AreaPreenchimento", rt, 0f, 0.35f, 1f, 0.65f);
        RectTransform preenchimento = Retangulo("Preenchimento", areaPreenchimento, 0f, 0f, 1f, 1f);
        preenchimento.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.7f, 0.25f);

        RectTransform areaPuxador = Retangulo("AreaPuxador", rt, 0f, 0f, 1f, 1f);
        areaPuxador.offsetMin = new Vector2(14f, 0f);
        areaPuxador.offsetMax = new Vector2(-14f, 0f);

        RectTransform puxador = Retangulo("Puxador", areaPuxador, 0f, 0f, 0f, 1f);
        puxador.sizeDelta = new Vector2(28f, 0f);
        Image imgPuxador = puxador.gameObject.AddComponent<Image>();
        imgPuxador.color = Color.white;

        Slider slider = rt.gameObject.AddComponent<Slider>();
        slider.fillRect = preenchimento;
        slider.handleRect = puxador;
        slider.targetGraphic = imgPuxador;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    TMP_InputField CriarCampoNome(Transform pai, float xMin, float yMin, float xMax, float yMax)
    {
        RectTransform rt = Retangulo("CampoNome", pai, xMin, yMin, xMax, yMax);

        Image fundo = rt.gameObject.AddComponent<Image>();
        fundo.color = new Color(1f, 1f, 1f, 0.25f);

        // area onde o texto aparece (corta o que passar)
        RectTransform area = Retangulo("AreaTexto", rt, 0.04f, 0.1f, 0.96f, 0.9f);
        area.gameObject.AddComponent<RectMask2D>();

        TMP_Text placeholder = CriarTexto(area, "Placeholder", "Digite seu nome", 0f, 0f, 1f, 1f, 40f);
        placeholder.color = new Color(1f, 1f, 1f, 0.4f);
        TMP_Text texto = CriarTexto(area, "Texto", "", 0f, 0f, 1f, 1f, 40f);

        // dentro do campo o tamanho da letra fica fixo
        placeholder.enableAutoSizing = false;
        placeholder.fontSize = 40f;
        texto.enableAutoSizing = false;
        texto.fontSize = 40f;

        TMP_InputField campo = rt.gameObject.AddComponent<TMP_InputField>();
        campo.targetGraphic = fundo;
        campo.textViewport = area;
        campo.textComponent = texto;
        campo.placeholder = placeholder;
        campo.characterLimit = Recorde.TAMANHO_NOME;
        campo.onEndEdit.AddListener(Recorde.DefinirNomeUltima);
        return campo;
    }
}