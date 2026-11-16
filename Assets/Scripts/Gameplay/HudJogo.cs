using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD do jogo: ferro (quantidade / capacidade), perolas e a dica da tecla de ataque.
// Cria o proprio Canvas por codigo: basta colocar este script em qualquer objeto da cena do jogo.
// Tudo (tamanho, margens, cores, fontes, contorno) pode ser ajustado pelo Inspector.
// Coloque em Assets/Scripts/Gameplay/.
public class HudJogo : MonoBehaviour
{
    public enum Canto { InferiorDireito, InferiorEsquerdo, SuperiorDireito, SuperiorEsquerdo }

    [Header("Ligacoes (vazio = procura sozinho)")]
    public InventarioFerro inventario;

    [Header("Posicao")]
    public Canto canto = Canto.InferiorDireito;
    public Vector2 margem = new Vector2(40f, 40f);
    public float escalaDoPainel = 1f;

    [Header("Sprites (opcionais: vazio = desenha por codigo)")]
    public Sprite spritePainel;
    public Sprite spriteFerro;
    public Sprite spritePerola;                 // dica: use o seu moeda.png
    public Sprite spriteTecla;
    public Color corPainel = Color.white;       // multiplica o sprite do painel

    [Header("Painel: tamanho e espacamento")]
    [Tooltip("Largura da area util, onde ficam os icones e textos. A largura total do painel = isto + as margens.")]
    public float larguraConteudo = 336f;
    [Tooltip("Folga entre a borda do painel e o conteudo: x = esquerda/direita, y = cima/baixo.")]
    public Vector2 margemInterna = new Vector2(22f, 22f);
    [Tooltip("Se o sprite do painel tem bordas (9-slice), o conteudo respeita essas bordas automaticamente.")]
    public bool respeitarBordasDoSprite = true;
    public float alturaLinha = 64f;
    public float espacoEntreLinhas = 14f;
    public float tamanhoIcone = 60f;

    [Header("Cores")]
    public Color corNome = new Color(0.95f, 0.8f, 0.55f);
    public Color corValor = Color.white;
    public Color corValorSemFerro = new Color(0.95f, 0.4f, 0.35f);
    public Color corValorCheio = new Color(1f, 0.85f, 0.35f);
    public Color corAtacar = Color.white;
    public Color corDica = new Color(0.95f, 0.8f, 0.55f);
    public Color corTecla = Color.white;                       // multiplica o desenho da tecla
    public Color corLetraTecla = new Color(0.3f, 0.16f, 0.06f);

    [Header("Textos")]
    public TMP_FontAsset fonte;                 // vazio = fonte padrao do TextMeshPro
    public string nomeFerro = "Ferro";
    public string nomePerolas = "Perolas";
    public string textoAtacar = "ATACAR";
    public string dicaAtacar = "segure e solte";
    public string dicaSemFerro = "sem ferro";
    public float tamanhoFonteNome = 28f;
    public float tamanhoFonteValor = 44f;
    public float tamanhoFonteAtacar = 34f;
    public float tamanhoFonteDica = 22f;

    [Header("Contorno do texto (ajuda a ler em qualquer fundo; 0 = sem contorno)")]
    [Range(0f, 1f)] public float contornoTexto = 0.2f;
    public Color corContorno = new Color(0.12f, 0.06f, 0.02f, 1f);

    [Header("Comportamento")]
    public bool esconderTextoFerroAntigo = true;    // desliga o TextoFerro antigo, porque o HUD ja mostra o ferro
    public bool esconderQuandoLivroAberto = true;
    [Range(0f, 1f)] public float opacidadeSemFerro = 0.45f;

    // ---- interno ----
    Canvas canvas;
    CanvasGroup grupo;
    CanvasGroup grupoAtacar;
    TextMeshProUGUI txtFerro, txtPerolas, txtTecla, txtDica;
    Image imgTecla;

    int ferroMostrado = -1;
    int capacidadeMostrada = -1;
    int perolasMostradas = -1;
    KeyCode teclaMostrada = KeyCode.None;
    int estadoFerroMostrado = -1;   // 0 = sem ferro, 1 = com ferro

    float popFerro;
    float popPerolas;
    float alfaAtual = 1f;
    float escalaTecla = 1f;

    readonly List<Object> criados = new List<Object>();

    // O HUD le o InventarioFerro "por nome", entao funciona mesmo se algum campo dele for private ou protected.
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

    // tenta cada nome em ordem e devolve o primeiro numero inteiro encontrado
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

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);

        for (int i = 0; i < criados.Count; i++)
        {
            if (criados[i] != null) Destroy(criados[i]);
        }
    }

    // ---------------------------------------------------------------- montagem

    // folga usada de um lado: a margem escolhida ou a borda do sprite (a maior), com um respiro extra
    static float Folga(float minimo, float bordaEmPixels, float fatorPixels)
    {
        if (bordaEmPixels <= 0f) return minimo;
        return Mathf.Max(minimo, bordaEmPixels * fatorPixels + 8f);
    }

    void Montar()
    {
        GameObject raiz = new GameObject("HudJogo_Canvas");
        canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler escalador = raiz.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        // folgas do conteudo (respeitam as bordas do sprite do painel, se ele tiver)
        Vector4 borda = Vector4.zero;
        float fator = 1f;
        if (spritePainel != null && respeitarBordasDoSprite)
        {
            borda = spritePainel.border;   // esquerda, baixo, direita, cima (em pixels do sprite)
            fator = canvas.referencePixelsPerUnit / Mathf.Max(0.01f, spritePainel.pixelsPerUnit);
        }
        float mEsquerda = Folga(margemInterna.x, borda.x, fator);
        float mBaixo = Folga(margemInterna.y, borda.y, fator);
        float mDireita = Folga(margemInterna.x, borda.z, fator);
        float mCima = Folga(margemInterna.y, borda.w, fator);

        float largura = mEsquerda + larguraConteudo + mDireita;
        float altura = mCima + alturaLinha * 3f + espacoEntreLinhas * 2f + mBaixo;

        // painel
        GameObject objPainel = new GameObject("Painel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        objPainel.transform.SetParent(raiz.transform, false);
        RectTransform painel = (RectTransform)objPainel.transform;

        grupo = objPainel.GetComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        bool direita = canto == Canto.InferiorDireito || canto == Canto.SuperiorDireito;
        bool superior = canto == Canto.SuperiorDireito || canto == Canto.SuperiorEsquerdo;
        Vector2 ancora = new Vector2(direita ? 1f : 0f, superior ? 1f : 0f);
        painel.anchorMin = ancora;
        painel.anchorMax = ancora;
        painel.pivot = ancora;
        painel.sizeDelta = new Vector2(largura, altura);
        painel.anchoredPosition = new Vector2(direita ? -margem.x : margem.x, superior ? -margem.y : margem.y);
        painel.localScale = Vector3.one * escalaDoPainel;

        Image fundo = objPainel.GetComponent<Image>();
        fundo.raycastTarget = false;
        fundo.type = Image.Type.Sliced;
        if (spritePainel != null)
        {
            fundo.sprite = spritePainel;
            fundo.color = corPainel;
        }
        else
        {
            fundo.sprite = Registrar(CriarCaixa(new Color(0.16f, 0.09f, 0.05f, 0.88f), new Color(0.85f, 0.58f, 0.25f, 1f)));
        }

        float espacoIcone = 16f;                               // distancia entre o icone e o texto
        float xTexto = mEsquerda + tamanhoIcone + espacoIcone;
        float larguraTexto = Mathf.Max(60f, larguraConteudo - tamanhoIcone - espacoIcone);
        float larguraValor = 150f;
        float xValor = mEsquerda + larguraConteudo - larguraValor;
        float margemIcone = (alturaLinha - tamanhoIcone) / 2f;

        // linha 1: ferro
        float y = mCima;
        NovaImagem("IconeFerro", painel, spriteFerro != null ? spriteFerro : Registrar(CriarIconeFerro()),
            new Vector2(mEsquerda, y + margemIcone), new Vector2(tamanhoIcone, tamanhoIcone));
        NovoTexto("NomeFerro", painel, nomeFerro.ToUpper(), tamanhoFonteNome, corNome, TextAlignmentOptions.MidlineLeft,
            new Vector2(xTexto, y), new Vector2(larguraTexto, alturaLinha));
        txtFerro = NovoTexto("ValorFerro", painel, "0/0", tamanhoFonteValor, corValor, TextAlignmentOptions.MidlineRight,
            new Vector2(xValor, y), new Vector2(larguraValor, alturaLinha));

        // linha 2: perolas
        y += alturaLinha + espacoEntreLinhas;
        NovaImagem("IconePerola", painel, spritePerola != null ? spritePerola : Registrar(CriarIconePerola()),
            new Vector2(mEsquerda, y + margemIcone), new Vector2(tamanhoIcone, tamanhoIcone));
        NovoTexto("NomePerolas", painel, nomePerolas.ToUpper(), tamanhoFonteNome, corNome, TextAlignmentOptions.MidlineLeft,
            new Vector2(xTexto, y), new Vector2(larguraTexto, alturaLinha));
        txtPerolas = NovoTexto("ValorPerolas", painel, "0", tamanhoFonteValor, corValor, TextAlignmentOptions.MidlineRight,
            new Vector2(xValor, y), new Vector2(larguraValor, alturaLinha));

        // linha 3: tecla de ataque
        y += alturaLinha + espacoEntreLinhas;
        RectTransform linhaAtacar = NovoRetangulo("LinhaAtacar", painel,
            new Vector2(mEsquerda, y), new Vector2(larguraConteudo, alturaLinha));
        grupoAtacar = linhaAtacar.gameObject.AddComponent<CanvasGroup>();

        imgTecla = NovaImagem("Tecla", linhaAtacar,
            spriteTecla != null ? spriteTecla : Registrar(CriarCaixa(new Color(0.96f, 0.88f, 0.68f, 1f), new Color(0.45f, 0.26f, 0.1f, 1f))),
            new Vector2(0f, margemIcone), new Vector2(tamanhoIcone, tamanhoIcone));
        imgTecla.type = Image.Type.Sliced;
        imgTecla.color = corTecla;
        txtTecla = NovoTexto("LetraTecla", imgTecla.transform, "F", tamanhoFonteValor, corLetraTecla,
            TextAlignmentOptions.Midline, Vector2.zero, new Vector2(tamanhoIcone, tamanhoIcone), false);

        float blocoTextos = tamanhoFonteAtacar + tamanhoFonteDica + 8f;
        float topoTextos = (alturaLinha - blocoTextos) / 2f;
        NovoTexto("Atacar", linhaAtacar, textoAtacar, tamanhoFonteAtacar, corAtacar, TextAlignmentOptions.MidlineLeft,
            new Vector2(tamanhoIcone + espacoIcone, topoTextos), new Vector2(larguraTexto, tamanhoFonteAtacar + 4f));
        txtDica = NovoTexto("DicaAtacar", linhaAtacar, dicaAtacar, tamanhoFonteDica, corDica, TextAlignmentOptions.MidlineLeft,
            new Vector2(tamanhoIcone + espacoIcone, topoTextos + tamanhoFonteAtacar + 4f), new Vector2(larguraTexto, tamanhoFonteDica + 4f));
    }

    // ---------------------------------------------------------------- atualizacao

    void Update()
    {
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

        // ferro
        if (ferro != ferroMostrado || capacidade != capacidadeMostrada)
        {
            if (ferroMostrado >= 0) popFerro = 1f;
            ferroMostrado = ferro;
            capacidadeMostrada = capacidade;

            txtFerro.text = ferro + "/" + capacidade;

            if (ferro <= 0) txtFerro.color = corValorSemFerro;
            else if (ferro >= capacidade) txtFerro.color = corValorCheio;
            else txtFerro.color = corValor;
        }

        // perolas
        if (perolas != perolasMostradas)
        {
            if (perolasMostradas >= 0) popPerolas = 1f;
            perolasMostradas = perolas;
            txtPerolas.text = perolas.ToString();
        }

        // pequeno "pulo" no numero quando ele muda
        popFerro = Mathf.MoveTowards(popFerro, 0f, dt * 4f);
        popPerolas = Mathf.MoveTowards(popPerolas, 0f, dt * 4f);
        txtFerro.rectTransform.localScale = Vector3.one * (1f + 0.3f * popFerro);
        txtPerolas.rectTransform.localScale = Vector3.one * (1f + 0.3f * popPerolas);

        // tecla de ataque
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

        // a tecla "afunda" e fica amarela enquanto voce segura para mirar
        bool apertada = temFerro && Input.GetKey(tecla);
        Color corAlvo = corTecla;
        if (apertada) corAlvo = corTecla * new Color(1f, 0.85f, 0.35f);
        imgTecla.color = Color.Lerp(imgTecla.color, corAlvo, Mathf.Clamp01(dt * 20f));
        escalaTecla = Mathf.Lerp(escalaTecla, apertada ? 0.88f : 1f, Mathf.Clamp01(dt * 20f));
        imgTecla.rectTransform.localScale = Vector3.one * escalaTecla;

        // some no fim de jogo e quando o livro de upgrades esta aberto
        bool visivel = true;
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando) visivel = false;
        if (esconderQuandoLivroAberto && GerenciadorUpgrades.livroAberto) visivel = false;

        alfaAtual = Mathf.MoveTowards(alfaAtual, visivel ? 1f : 0f, dt * 6f);
        grupo.alpha = alfaAtual;
    }

    // ---------------------------------------------------------------- criacao de elementos

    // retangulo posicionado pelo canto superior esquerdo do pai
    RectTransform NovoRetangulo(string nome, Transform pai, Vector2 topoEsquerdo, Vector2 tamanho)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);

        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = new Vector2(topoEsquerdo.x + tamanho.x / 2f, -(topoEsquerdo.y + tamanho.y / 2f));
        return rt;
    }

    Image NovaImagem(string nome, Transform pai, Sprite sprite, Vector2 topoEsquerdo, Vector2 tamanho)
    {
        RectTransform rt = NovoRetangulo(nome, pai, topoEsquerdo, tamanho);
        Image imagem = rt.gameObject.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;
        return imagem;
    }

    TextMeshProUGUI NovoTexto(string nome, Transform pai, string texto, float tamanhoFonte, Color cor,
                              TextAlignmentOptions alinhamento, Vector2 topoEsquerdo, Vector2 tamanho, bool comContorno = true)
    {
        RectTransform rt = NovoRetangulo(nome, pai, topoEsquerdo, tamanho);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fonte != null) t.font = fonte;
        t.text = texto;
        t.fontSize = tamanhoFonte;
        t.color = cor;
        t.alignment = alinhamento;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;

        if (comContorno && contornoTexto > 0f)
        {
            t.outlineWidth = contornoTexto;
            t.outlineColor = corContorno;
        }

        return t;
    }

    // ---------------------------------------------------------------- imagens desenhadas por codigo

    Sprite Registrar(Sprite sprite)
    {
        criados.Add(sprite);
        criados.Add(sprite.texture);
        return sprite;
    }

    // caixa de cantos arredondados com borda (fatiada, estica sem deformar os cantos)
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

    // perola: esfera com luz vindo do alto, brilho e borda suave
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

    // ferro: um lingote cinza (trapezio com a face de cima mais clara)
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
                    float t = (y - 14f) / 34f;                          // 0 embaixo, 1 em cima
                    float semiLargura = Mathf.Lerp(29f, 21f, t);        // base larga, topo estreito
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