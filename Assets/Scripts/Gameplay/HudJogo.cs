using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudJogo : MonoBehaviour
{
    public enum Canto
    {
        InferiorDireito,
        InferiorEsquerdo,
        SuperiorDireito,
        SuperiorEsquerdo,
        InferiorCentro,
        SuperiorCentro,
        Centro,
        CentroEsquerdo,
        CentroDireito
    }

    [Header("Ligacoes (vazio = procura sozinho)")]
    public InventarioFerro inventario;

    [Header("Posicao")]
    [Tooltip("Onde o painel fica na tela. As opcoes Centro deixam o painel centralizado naquele eixo.")]
    public Canto canto = Canto.InferiorDireito;
    [Tooltip("Distancia da borda da tela. Nos eixos centralizados vira um deslocamento (x: direita, y: cima).")]
    public Vector2 margem = new Vector2(40f, 40f);
    public float escalaDoPainel = 1f;

    [Header("Sprites (opcionais: vazio = desenha por codigo)")]
    public Sprite spritePainel;
    public Sprite spriteFerro;
    public Sprite spritePerola;
    public Sprite spriteTecla;
    public Color corPainel = Color.white;

    [Header("Painel: tamanho e espacamento")]
    [Tooltip("Largura minima da area util. O painel cresce sozinho se o conteudo precisar de mais.")]
    public float larguraConteudo = 336f;
    [Tooltip("Folga entre a borda do painel e o conteudo: x = esquerda/direita, y = cima/baixo.")]
    public Vector2 margemInterna = new Vector2(22f, 22f);
    [Tooltip("Se o painel tem moldura, o conteudo fica sempre para dentro dela.")]
    public bool respeitarBordasDoSprite = true;
    public float alturaLinha = 64f;
    public float espacoEntreLinhas = 14f;
    public float tamanhoIcone = 60f;
    [Tooltip("Distancia entre o icone e o texto.")]
    public float espacoIcone = 16f;
    [Tooltip("Largura minima reservada para o numero, para o painel nao ficar mexendo quando o valor muda.")]
    public float larguraMinimaValor = 120f;

    [Header("Centralizacao")]
    [Tooltip("Ligado: icone, nome e valor de cada linha ficam centralizados no painel. Desligado: nome na esquerda e valor na direita.")]
    public bool centralizarConteudo = true;

    [Header("Moldura do painel (para sprite sem 9-slice)")]
    [Tooltip("Espessura da moldura em pixels do sprite original. Usado so quando o sprite nao tem bordas definidas no Sprite Editor. 0 = desliga e estica o sprite inteiro.")]
    public float bordaAutomaticaDoSprite = 130f;
    [Tooltip("Espessura que a moldura ocupa na tela (pixels do canvas).")]
    public float espessuraBordaNaTela = 34f;

    [Header("Cores")]
    [Tooltip("Ligado: usa as cores escuras abaixo (boas para painel claro, tipo pergaminho) e sem contorno. Desligado: usa as cores claras.")]
    public bool usarCoresDoPergaminho = true;
    public Color corNomePergaminho = new Color(0.36f, 0.19f, 0.07f);
    public Color corValorPergaminho = new Color(0.2f, 0.09f, 0.02f);
    public Color corValorSemFerroPergaminho = new Color(0.72f, 0.12f, 0.08f);
    public Color corValorCheioPergaminho = new Color(0.62f, 0.33f, 0f);
    public Color corLetraTeclaPergaminho = new Color(0.3f, 0.16f, 0.06f);

    public Color corNome = new Color(0.95f, 0.8f, 0.55f);
    public Color corValor = Color.white;
    public Color corValorSemFerro = new Color(0.95f, 0.4f, 0.35f);
    public Color corValorCheio = new Color(1f, 0.85f, 0.35f);
    public Color corAtacar = Color.white;
    public Color corDica = new Color(0.95f, 0.8f, 0.55f);
    public Color corTecla = Color.white;
    public Color corLetraTecla = new Color(0.3f, 0.16f, 0.06f);

    [Header("Textos")]
    public TMP_FontAsset fonte;
    public string nomeFerro = "Ferro";
    public string nomePerolas = "Perolas";
    public string textoAtacar = "ATACAR";
    public string dicaAtacar = "segure e solte";
    public string dicaSemFerro = "sem ferro";
    public float tamanhoFonteNome = 28f;
    public float tamanhoFonteValor = 44f;
    public float tamanhoFonteAtacar = 34f;
    public float tamanhoFonteDica = 22f;

    [Header("Guia do Pescador")]
    public bool mostrarGuia = true;
    public string textoGuia = "GUIA DO PESCADOR";
    public string dicaGuia = "abrir upgrades";
    public string dicaGuiaNovo = "upgrade disponivel!";

    [Header("Contorno do texto (so quando as cores do pergaminho estao desligadas)")]
    [Range(0f, 1f)] public float contornoTexto = 0.2f;
    public Color corContorno = new Color(0.12f, 0.06f, 0.02f, 1f);

    [Header("Comportamento")]
    public bool esconderTextoFerroAntigo = true;
    public bool esconderQuandoLivroAberto = true;
    [Range(0f, 1f)] public float opacidadeSemFerro = 0.45f;

    Canvas canvas;
    CanvasGroup grupo;
    CanvasGroup grupoAtacar;
    TextMeshProUGUI txtFerro, txtPerolas, txtTecla, txtDica;
    Image imgTecla;
    CanvasGroup grupoGuia;
    TextMeshProUGUI txtTeclaGuia, txtDicaGuia;
    Image imgTeclaGuia;

    Color usoValor, usoValorSemFerro, usoValorCheio, usoNome, usoAtacar, usoDica, usoLetraTecla;
    float usoContorno;

    int ferroMostrado = -1;
    int capacidadeMostrada = -1;
    int perolasMostradas = -1;
    KeyCode teclaMostrada = KeyCode.None;
    int estadoFerroMostrado = -1;
    KeyCode teclaGuiaMostrada = KeyCode.None;
    int estadoGuiaMostrado = -1;

    float popFerro;
    float popPerolas;
    float alfaAtual = 1f;
    float escalaTecla = 1f;
    bool recriar;

    readonly List<Object> criados = new List<Object>();

    const BindingFlags Tudo = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    readonly Dictionary<string, MemberInfo> membros = new Dictionary<string, MemberInfo>();

    object Pegar(string nome)
    {
        if (inventario == null) return null;

        MemberInfo membro;
        if (!membros.TryGetValue(nome, out membro))
        {
            System.Type tipo = inventario.GetType();
            membro = tipo.GetField(nome, Tudo);
            if (membro == null) membro = tipo.GetProperty(nome, Tudo);
            membros[nome] = membro;
        }

        FieldInfo campo = membro as FieldInfo;
        if (campo != null) return campo.GetValue(inventario);

        PropertyInfo propriedade = membro as PropertyInfo;
        if (propriedade != null && propriedade.CanRead) return propriedade.GetValue(inventario, null);

        return null;
    }

    int PegarInt(int padrao, params string[] nomes)
    {
        for (int i = 0; i < nomes.Length; i++)
        {
            object valor = Pegar(nomes[i]);
            if (valor is int) return (int)valor;
        }
        return padrao;
    }

    void Start()
    {
        if (inventario == null) inventario = FindFirstObjectByType<InventarioFerro>();

        if (esconderTextoFerroAntigo && inventario != null)
        {
            TMP_Text antigo = Pegar("textoFerro") as TMP_Text;
            if (antigo != null) antigo.gameObject.SetActive(false);
        }

        Montar();
    }

    void OnValidate()
    {
        if (Application.isPlaying && canvas != null) recriar = true;
    }

    [ContextMenu("Reconstruir HUD")]
    void Reconstruir()
    {
        if (Application.isPlaying) recriar = true;
    }

    void OnDestroy()
    {
        Desmontar();
    }

    void Desmontar()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        canvas = null;
        grupo = null;
        grupoAtacar = null;
        grupoGuia = null;
        GerenciadorUpgrades.hudJogoAtivo = false;

        for (int i = 0; i < criados.Count; i++)
        {
            if (criados[i] != null) Destroy(criados[i]);
        }
        criados.Clear();
    }

    Vector2 AncoraDoCanto()
    {
        switch (canto)
        {
            case Canto.InferiorDireito: return new Vector2(1f, 0f);
            case Canto.InferiorEsquerdo: return new Vector2(0f, 0f);
            case Canto.SuperiorDireito: return new Vector2(1f, 1f);
            case Canto.SuperiorEsquerdo: return new Vector2(0f, 1f);
            case Canto.InferiorCentro: return new Vector2(0.5f, 0f);
            case Canto.SuperiorCentro: return new Vector2(0.5f, 1f);
            case Canto.CentroEsquerdo: return new Vector2(0f, 0.5f);
            case Canto.CentroDireito: return new Vector2(1f, 0.5f);
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    void Montar()
    {
        ferroMostrado = -1;
        capacidadeMostrada = -1;
        perolasMostradas = -1;
        teclaMostrada = KeyCode.None;
        estadoFerroMostrado = -1;
        teclaGuiaMostrada = KeyCode.None;
        estadoGuiaMostrado = -1;
        popFerro = 0f;
        popPerolas = 0f;
        escalaTecla = 1f;

        if (usarCoresDoPergaminho)
        {
            usoNome = corNomePergaminho;
            usoValor = corValorPergaminho;
            usoValorSemFerro = corValorSemFerroPergaminho;
            usoValorCheio = corValorCheioPergaminho;
            usoAtacar = corValorPergaminho;
            usoDica = corNomePergaminho;
            usoLetraTecla = corLetraTeclaPergaminho;
            usoContorno = 0f;
        }
        else
        {
            usoNome = corNome;
            usoValor = corValor;
            usoValorSemFerro = corValorSemFerro;
            usoValorCheio = corValorCheio;
            usoAtacar = corAtacar;
            usoDica = corDica;
            usoLetraTecla = corLetraTecla;
            usoContorno = contornoTexto;
        }

        GameObject raiz = new GameObject("HudJogo_Canvas");
        canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler escalador = raiz.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        Sprite spriteDoPainel;
        float multiplicador = 1f;
        float espessura = 0f;

        if (spritePainel != null)
        {
            spriteDoPainel = spritePainel;
            Vector4 b = spritePainel.border;
            float bordaSprite = Mathf.Max(Mathf.Max(b.x, b.y), Mathf.Max(b.z, b.w));

            if (bordaSprite <= 0f && bordaAutomaticaDoSprite > 0f)
            {
                float menorLado = Mathf.Min(spritePainel.rect.width, spritePainel.rect.height);
                float lado = Mathf.Min(bordaAutomaticaDoSprite, menorLado * 0.45f);
                spriteDoPainel = Sprite.Create(spritePainel.texture, spritePainel.rect, new Vector2(0.5f, 0.5f),
                    spritePainel.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(lado, lado, lado, lado));
                criados.Add(spriteDoPainel);
                bordaSprite = lado;
            }

            if (bordaSprite > 0f)
            {
                espessura = Mathf.Max(1f, espessuraBordaNaTela);
                multiplicador = bordaSprite * canvas.referencePixelsPerUnit / (spriteDoPainel.pixelsPerUnit * espessura);
            }
        }
        else
        {
            spriteDoPainel = Registrar(CriarCaixa(new Color(0.16f, 0.09f, 0.05f, 0.88f), new Color(0.85f, 0.58f, 0.25f, 1f)));
            espessura = 20f;
        }

        float folgaMoldura = (respeitarBordasDoSprite && espessura > 0f) ? espessura + 8f : 0f;
        float mX = Mathf.Max(margemInterna.x, folgaMoldura);
        float mY = Mathf.Max(margemInterna.y, folgaMoldura);

        GameObject objPainel = new GameObject("Painel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        objPainel.transform.SetParent(raiz.transform, false);
        RectTransform painel = (RectTransform)objPainel.transform;

        grupo = objPainel.GetComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        Vector2 ancora = AncoraDoCanto();
        painel.anchorMin = ancora;
        painel.anchorMax = ancora;
        painel.pivot = ancora;
        painel.anchoredPosition = new Vector2(ancora.x >= 1f ? -margem.x : margem.x, ancora.y >= 1f ? -margem.y : margem.y);
        painel.localScale = Vector3.one * escalaDoPainel;

        Image fundo = objPainel.GetComponent<Image>();
        fundo.raycastTarget = false;
        fundo.sprite = spriteDoPainel;
        fundo.type = Image.Type.Sliced;
        fundo.pixelsPerUnitMultiplier = multiplicador;
        fundo.color = spritePainel != null ? corPainel : Color.white;

        VerticalLayoutGroup vertical = objPainel.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(Mathf.RoundToInt(mX), Mathf.RoundToInt(mX), Mathf.RoundToInt(mY), Mathf.RoundToInt(mY));
        vertical.spacing = espacoEntreLinhas;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        ContentSizeFitter ajuste = objPainel.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextAlignmentOptions alinhamentoNome = centralizarConteudo ? TextAlignmentOptions.Midline : TextAlignmentOptions.MidlineLeft;
        TextAlignmentOptions alinhamentoValor = centralizarConteudo ? TextAlignmentOptions.Midline : TextAlignmentOptions.MidlineRight;

        RectTransform linhaFerro = NovaLinha("LinhaFerro", painel);
        NovaImagem("IconeFerro", linhaFerro, spriteFerro != null ? spriteFerro : Registrar(CriarIconeFerro()), tamanhoIcone);
        TextMeshProUGUI nomeDoFerro = NovoTexto("NomeFerro", linhaFerro, nomeFerro.ToUpper(), tamanhoFonteNome, usoNome, alinhamentoNome, true);
        txtFerro = NovoTexto("ValorFerro", linhaFerro, "0/0", tamanhoFonteValor, usoValor, alinhamentoValor, true);
        ConfigurarLinhaDeValor(nomeDoFerro, txtFerro);

        RectTransform linhaPerolas = NovaLinha("LinhaPerolas", painel);
        NovaImagem("IconePerola", linhaPerolas, spritePerola != null ? spritePerola : Registrar(CriarIconePerola()), tamanhoIcone);
        TextMeshProUGUI nomeDasPerolas = NovoTexto("NomePerolas", linhaPerolas, nomePerolas.ToUpper(), tamanhoFonteNome, usoNome, alinhamentoNome, true);
        txtPerolas = NovoTexto("ValorPerolas", linhaPerolas, "0", tamanhoFonteValor, usoValor, alinhamentoValor, true);
        ConfigurarLinhaDeValor(nomeDasPerolas, txtPerolas);

        MontarLinhaDeTecla("LinhaAtacar", painel, textoAtacar, tamanhoFonteAtacar, usoAtacar, dicaAtacar,
            out grupoAtacar, out imgTecla, out txtTecla, out txtDica);

        if (mostrarGuia)
        {
            MontarLinhaDeTecla("LinhaGuia", painel, textoGuia, tamanhoFonteNome, usoAtacar, dicaGuia,
                out grupoGuia, out imgTeclaGuia, out txtTeclaGuia, out txtDicaGuia);
        }

        GerenciadorUpgrades.hudJogoAtivo = mostrarGuia;

        LayoutRebuilder.ForceRebuildLayoutImmediate(painel);
    }

    void MontarLinhaDeTecla(string nomeLinha, Transform painel, string titulo, float tamanhoTitulo, Color corTitulo, string dica,
                            out CanvasGroup grupoDaLinha, out Image imagemTecla, out TextMeshProUGUI letraTecla, out TextMeshProUGUI textoDica)
    {
        RectTransform linha = NovaLinha(nomeLinha, painel);
        grupoDaLinha = linha.gameObject.AddComponent<CanvasGroup>();

        imagemTecla = NovaImagem("Tecla", linha,
            spriteTecla != null ? spriteTecla : Registrar(CriarCaixa(new Color(0.96f, 0.88f, 0.68f, 1f), new Color(0.45f, 0.26f, 0.1f, 1f))),
            tamanhoIcone);
        imagemTecla.type = Image.Type.Sliced;
        imagemTecla.color = corTecla;

        letraTecla = NovoTexto("LetraTecla", imagemTecla.transform, "F", tamanhoFonteValor, usoLetraTecla, TextAlignmentOptions.Midline, false);
        RectTransform letra = letraTecla.rectTransform;
        letra.anchorMin = Vector2.zero;
        letra.anchorMax = Vector2.one;
        letra.offsetMin = Vector2.zero;
        letra.offsetMax = Vector2.zero;
        letraTecla.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        TextAlignmentOptions alinhamento = centralizarConteudo ? TextAlignmentOptions.Midline : TextAlignmentOptions.MidlineLeft;

        RectTransform blocoTextos = NovoRetangulo("Textos", linha);
        VerticalLayoutGroup colunaTextos = blocoTextos.gameObject.AddComponent<VerticalLayoutGroup>();
        colunaTextos.childAlignment = centralizarConteudo ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        colunaTextos.spacing = 0f;
        colunaTextos.childControlWidth = true;
        colunaTextos.childControlHeight = true;
        colunaTextos.childForceExpandWidth = false;
        colunaTextos.childForceExpandHeight = false;

        NovoTexto("Titulo", blocoTextos, titulo, tamanhoTitulo, corTitulo, alinhamento, true);
        textoDica = NovoTexto("Dica", blocoTextos, dica, tamanhoFonteDica, usoDica, alinhamento, true);

        if (!centralizarConteudo)
        {
            blocoTextos.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }
    }

    void ConfigurarLinhaDeValor(TextMeshProUGUI nome, TextMeshProUGUI valor)
    {
        LayoutElement elementoValor = valor.gameObject.AddComponent<LayoutElement>();
        elementoValor.minWidth = larguraMinimaValor;

        if (!centralizarConteudo)
        {
            nome.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }
    }

    void Update()
    {
        if (recriar)
        {
            recriar = false;
            Desmontar();
            Montar();
        }

        if (grupo == null) return;

        float dt = Time.unscaledDeltaTime;

        int ferro = 0;
        int capacidade = 0;
        KeyCode tecla = KeyCode.F;
        if (inventario != null)
        {
            ferro = PegarInt(0, "quantidade");
            capacidade = PegarInt(0, "CapacidadeAtual", "CapacidadeTotal", "capacidade");

            object valorTecla = Pegar("teclaArremessar");
            if (valorTecla is KeyCode) tecla = (KeyCode)valorTecla;
        }

        int perolas = 0;
        if (GerenciadorUpgrades.instancia != null) perolas = GerenciadorUpgrades.instancia.saldo;

        if (ferro != ferroMostrado || capacidade != capacidadeMostrada)
        {
            if (ferroMostrado >= 0) popFerro = 1f;
            ferroMostrado = ferro;
            capacidadeMostrada = capacidade;

            txtFerro.text = ferro + "/" + capacidade;

            if (ferro <= 0) txtFerro.color = usoValorSemFerro;
            else if (ferro >= capacidade) txtFerro.color = usoValorCheio;
            else txtFerro.color = usoValor;
        }

        if (perolas != perolasMostradas)
        {
            if (perolasMostradas >= 0) popPerolas = 1f;
            perolasMostradas = perolas;
            txtPerolas.text = perolas.ToString();
        }

        popFerro = Mathf.MoveTowards(popFerro, 0f, dt * 4f);
        popPerolas = Mathf.MoveTowards(popPerolas, 0f, dt * 4f);
        txtFerro.rectTransform.localScale = Vector3.one * (1f + 0.3f * popFerro);
        txtPerolas.rectTransform.localScale = Vector3.one * (1f + 0.3f * popPerolas);

        if (tecla != teclaMostrada)
        {
            teclaMostrada = tecla;
            string nome = tecla.ToString();
            txtTecla.text = nome;
            txtTecla.fontSize = nome.Length > 2 ? tamanhoFonteValor * 0.55f : tamanhoFonteValor;
        }

        bool temFerro = ferro > 0;
        int estado = temFerro ? 1 : 0;
        if (estado != estadoFerroMostrado)
        {
            estadoFerroMostrado = estado;
            txtDica.text = temFerro ? dicaAtacar : dicaSemFerro;
        }

        grupoAtacar.alpha = Mathf.MoveTowards(grupoAtacar.alpha, temFerro ? 1f : opacidadeSemFerro, dt * 4f);

        bool apertada = temFerro && Input.GetKey(tecla);
        Color corAlvo = corTecla;
        if (apertada) corAlvo = corTecla * new Color(1f, 0.85f, 0.35f);
        imgTecla.color = Color.Lerp(imgTecla.color, corAlvo, Mathf.Clamp01(dt * 20f));
        escalaTecla = Mathf.Lerp(escalaTecla, apertada ? 0.88f : 1f, Mathf.Clamp01(dt * 20f));
        imgTecla.rectTransform.localScale = Vector3.one * escalaTecla;

        if (grupoGuia != null)
        {
            KeyCode teclaGuia = ConfiguracoesJogo.Tecla(AcaoTecla.AbrirLivro);
            if (teclaGuia != teclaGuiaMostrada)
            {
                teclaGuiaMostrada = teclaGuia;
                string nomeGuia = ConfiguracoesJogo.NomeTecla(teclaGuia);
                txtTeclaGuia.text = nomeGuia;
                txtTeclaGuia.fontSize = nomeGuia.Length > 2 ? tamanhoFonteValor * 0.55f : tamanhoFonteValor;
            }

            GerenciadorUpgrades upgrades = GerenciadorUpgrades.instancia;
            bool podeAbrir = upgrades == null || upgrades.PodeAbrir();
            bool temNovo = upgrades != null && upgrades.PodeComprarAlgo();

            int estadoGuia = temNovo ? 1 : 0;
            if (estadoGuia != estadoGuiaMostrado)
            {
                estadoGuiaMostrado = estadoGuia;
                txtDicaGuia.text = temNovo ? dicaGuiaNovo : dicaGuia;
                txtDicaGuia.color = temNovo ? usoValorCheio : usoDica;
            }

            grupoGuia.alpha = Mathf.MoveTowards(grupoGuia.alpha, podeAbrir ? 1f : opacidadeSemFerro, dt * 4f);

            float pulso = temNovo ? 1f + 0.07f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            imgTeclaGuia.rectTransform.localScale = Vector3.one * pulso;
        }

        bool visivel = true;
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando) visivel = false;
        if (esconderQuandoLivroAberto && GerenciadorUpgrades.livroAberto) visivel = false;

        alfaAtual = Mathf.MoveTowards(alfaAtual, visivel ? 1f : 0f, dt * 6f);
        grupo.alpha = alfaAtual;
    }

    RectTransform NovoRetangulo(string nome, Transform pai)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        return (RectTransform)obj.transform;
    }

    RectTransform NovaLinha(string nome, Transform pai)
    {
        RectTransform linha = NovoRetangulo(nome, pai);

        HorizontalLayoutGroup horizontal = linha.gameObject.AddComponent<HorizontalLayoutGroup>();
        horizontal.childAlignment = centralizarConteudo ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        horizontal.spacing = espacoIcone;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;

        LayoutElement elemento = linha.gameObject.AddComponent<LayoutElement>();
        elemento.minWidth = larguraConteudo;
        elemento.minHeight = alturaLinha;
        elemento.preferredHeight = alturaLinha;

        return linha;
    }

    Image NovaImagem(string nome, Transform pai, Sprite sprite, float tamanho)
    {
        RectTransform rt = NovoRetangulo(nome, pai);

        Image imagem = rt.gameObject.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;

        LayoutElement elemento = rt.gameObject.AddComponent<LayoutElement>();
        elemento.minWidth = tamanho;
        elemento.minHeight = tamanho;
        elemento.preferredWidth = tamanho;
        elemento.preferredHeight = tamanho;

        return imagem;
    }

    TextMeshProUGUI NovoTexto(string nome, Transform pai, string texto, float tamanhoFonte, Color cor,
                              TextAlignmentOptions alinhamento, bool comContorno)
    {
        RectTransform rt = NovoRetangulo(nome, pai);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fonte != null) t.font = fonte;
        t.text = texto;
        t.fontSize = tamanhoFonte;
        t.color = cor;
        t.alignment = alinhamento;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;

        if (comContorno && usoContorno > 0f)
        {
            t.outlineWidth = usoContorno;
            t.outlineColor = corContorno;
        }

        return t;
    }

    Sprite Registrar(Sprite sprite)
    {
        criados.Add(sprite);
        criados.Add(sprite.texture);
        return sprite;
    }

    static Sprite CriarCaixa(Color fundo, Color borda)
    {
        const int n = 64;
        const float raio = 14f;
        const float espessura = 3f;

        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[n * n];
        float meio = n / 2f;

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - meio) - (meio - raio);
                float dy = Mathf.Abs(y + 0.5f - meio) - (meio - raio);
                float d = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude
                          + Mathf.Min(Mathf.Max(dx, dy), 0f) - raio;

                Color cor = (d < -espessura) ? fundo : borda;
                cor.a *= Mathf.Clamp01(0.5f - d);
                pixels[y * n + x] = cor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(20f, 20f, 20f, 20f));
    }

    static Sprite CriarIconePerola()
    {
        const int n = 64;
        const float raio = 28f;

        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[n * n];
        float centro = n / 2f;

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - centro;
                float dy = y + 0.5f - centro;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alfa = Mathf.Clamp01(raio - dist + 0.5f);
                float luz = Mathf.Clamp01(0.5f + (-dx + dy) / (2f * raio) * 0.6f);

                Color cor = Color.Lerp(new Color(0.62f, 0.58f, 0.8f), new Color(0.98f, 0.95f, 1f), luz);

                float bx = dx + 10f;
                float by = dy - 10f;
                float brilho = Mathf.Clamp01(1f - Mathf.Sqrt(bx * bx + by * by) / 9f);
                cor = Color.Lerp(cor, Color.white, brilho * 0.9f);

                float bordaSuave = Mathf.Clamp01((dist - (raio - 3f)) / 3f);
                cor = Color.Lerp(cor, new Color(0.35f, 0.3f, 0.5f), bordaSuave * 0.7f);

                cor.a = alfa;
                pixels[y * n + x] = cor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite CriarIconeFerro()
    {
        const int n = 64;

        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[n * n];
        Color escuro = new Color(0.2f, 0.22f, 0.27f, 1f);
        Color claro = new Color(0.84f, 0.88f, 0.94f, 1f);
        Color medioBaixo = new Color(0.42f, 0.46f, 0.52f, 1f);
        Color medioCima = new Color(0.66f, 0.7f, 0.77f, 1f);

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                Color cor = Color.clear;

                if (y >= 14 && y <= 48)
                {
                    float t = (y - 14f) / 34f;
                    float semiLargura = Mathf.Lerp(29f, 21f, t);
                    float dx = Mathf.Abs(x + 0.5f - n / 2f);

                    if (dx <= semiLargura)
                    {
                        bool contorno = dx > semiLargura - 3f || y < 17 || y > 45;
                        if (contorno) cor = escuro;
                        else if (y > 38) cor = claro;
                        else cor = Color.Lerp(medioBaixo, medioCima, t);
                    }
                }

                pixels[y * n + x] = cor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}