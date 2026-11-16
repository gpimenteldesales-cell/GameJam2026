using UnityEngine;

public class GerenciadorUpgrades : MonoBehaviour
{
    public static GerenciadorUpgrades instancia;
    public static bool livroAberto;

    [Header("Livro")]
    public KeyCode teclaLivro = KeyCode.B;
    public string nomeMoeda = "Perolas";

    [Header("Visual do livro: sprite opcional (so o fundo; o texto continua pelo codigo)")]
    [Tooltip("Seu desenho do livro. Vazio = livro marrom desenhado pelo codigo (visual antigo).")]
    public Sprite spriteLivro;
    [Tooltip("Cor que multiplica o sprite. Branco = cores originais.")]
    public Color corLivro = Color.white;
    [Tooltip("O conteudo (textos, upgrades, botoes) fica numa area fixa de 900 x 520 (720p). Aqui o sprite pode passar dessa area: pixels extras para cada lado (x = esquerda/direita, y = cima/baixo).")]
    public Vector2 livroExpansao = Vector2.zero;
    [Tooltip("Desligue se a sua arte do livro ja traz o fundo dos 3 cartoes de upgrade.")]
    public bool desenharFundoDosCartoes = true;

    [Header("Visual dos botoes: sprites opcionais (so o fundo; o texto continua pelo codigo)")]
    [Tooltip("Sprite do botao (Fechar e Comprar). Vazio = botao padrao da Unity.")]
    public Sprite spriteBotao;
    [Tooltip("Opcional: botao com o mouse em cima. Sem ele, o botao escurece um pouco quando apertado.")]
    public Sprite spriteBotaoHover;
    [Tooltip("Opcional: botao quando nao da pra comprar. Sem ele, usa o sprite normal com 'Cor Botao Desabilitado'.")]
    public Sprite spriteBotaoDesabilitado;
    public Color corBotao = Color.white;
    public Color corBotaoDesabilitado = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color corTextoBotao = new Color(0.25f, 0.14f, 0.07f);
    [Tooltip("Ligue se a arte do livro JA traz os botoes desenhados. Sem Sprite Botao, o codigo desenha so o texto por cima da arte, sem o botao padrao da Unity.")]
    public bool botoesSemFundo = false;

    [Header("1) Anzol Dourado: peixes valem mais")]
    public int[] custoAnzol = { 60, 150, 300 };
    public float[] bonusPontos = { 0.25f, 0.5f, 1f };

    [Header("2) Coletor de Ferro: mochila maior e ferro mais frequente")]
    public int[] custoColetor = { 50, 120, 250 };
    public int[] bonusCapacidade = { 2, 4, 7 };
    public float[] multiplicadorIntervalo = { 0.85f, 0.7f, 0.55f };
    public int[] bonusFerroNaCena = { 0, 1, 2 };

    [Header("3) Arremesso Multiplo: varios ferros de uma vez (gasta so 1)")]
    public int[] custoMultiplo = { 150, 300 };
    public int[] projeteis = { 2, 3 };
    public float anguloEntreProjeteis = 12f;

    [Header("4) Escudo do Barco: absorve um ataque que viraria o barco")]
    public int[] custoEscudo = { 300 };
    [Tooltip("Segundos para o escudo recarregar depois de usado. 0 = nao recarrega (uso unico).")]
    public float[] recargaEscudo = { 60f };

    [Header("5) Ferro Pesado: o ferro causa mais dano nas bruxas")]
    public int[] custoFerroPesado = { 200, 400 };
    [Tooltip("Dano EXTRA somado ao dano do ferro. Bruxa tem 3 de vida: +1 = 2 acertos, +2 = 1 acerto.")]
    public int[] bonusDano = { 1, 2 };

    [Header("6) Cacador de Bruxas: chance de ganhar pontos ao matar uma bruxa")]
    public int[] custoRecompensa = { 150, 300 };
    [Tooltip("Chance de 0 a 1 (0.35 = 35%).")]
    public float[] chanceRecompensa = { 0.35f, 0.65f };
    [Tooltip("Pontos ganhos quando a chance acerta (peixe vale 10).")]
    public int[] pontosRecompensa = { 30, 50 };

    [Header("Depuracao")]
    public bool depurar = true;

    [Header("Estado (so pra ver no Inspector)")]
    public int saldo;
    public int[] niveis = new int[6];
    public bool escudoCarregado;

    const int ANZOL = 0;
    const int COLETOR = 1;
    const int MULTIPLO = 2;
    const int ESCUDO = 3;
    const int PESADO = 4;
    const int RECOMPENSA = 5;
    const int TOTAL = 6;
    const int POR_PAGINA = 3;

    int pagina;
    float escudoProntoEm;
    float avisoEscudoAte;
    GUIStyle estAviso;

    class Entrada
    {
        public string nome;
        public int[] custos;
        public string[] efeitos;
    }

    Entrada[] entradas;

    GUIStyle estTitulo, estMoeda, estNome, estTexto, estBotao, estHud, estBotaoTexto;

    public static int AplicarBonusPontos(int valor)
    {
        if (instancia == null) return valor;
        float mult = 1f + Valor(instancia.bonusPontos, Nivel(ANZOL), 0f);
        int final = Mathf.RoundToInt(valor * mult);
        if (instancia.depurar) Debug.Log("[Upgrades] peixe valia " + valor + ", agora vale " + final);
        return final;
    }

    public static void GanharPerolas(int quantidade)
    {
        if (instancia == null) return;
        instancia.saldo += quantidade;
        if (instancia.depurar) Debug.Log("[Upgrades] +" + quantidade + " perolas (saldo " + instancia.saldo + ")");
    }

    public static int BonusCapacidade
    {
        get { return instancia == null ? 0 : Valor(instancia.bonusCapacidade, Nivel(COLETOR), 0); }
    }

    public static float MultiplicadorIntervaloFerro
    {
        get { return instancia == null ? 1f : Valor(instancia.multiplicadorIntervalo, Nivel(COLETOR), 1f); }
    }

    public static int BonusFerroNaCena
    {
        get { return instancia == null ? 0 : Valor(instancia.bonusFerroNaCena, Nivel(COLETOR), 0); }
    }

    public static int ProjeteisPorArremesso
    {
        get { return instancia == null ? 1 : Mathf.Max(1, Valor(instancia.projeteis, Nivel(MULTIPLO), 1)); }
    }

    public static float AnguloEntreProjeteis
    {
        get { return instancia == null ? 0f : instancia.anguloEntreProjeteis; }
    }

    // Compatibilidade: versoes antigas de RaioDePesca, InventarioFerro e EventoEquilibrioBarco chamavam estes.
    // Nao ha mais upgrade ligado a eles (sempre 1). Pode apagar as chamadas la quando quiser.
    public static float MultiplicadorAlcance { get { return 1f; } }
    public static float MultiplicadorEmpurrao { get { return 1f; } }
    public static float MultiplicadorForcaAperto { get { return 1f; } }
    public static float MultiplicadorPenalidade { get { return 1f; } }

    public static int BonusDano
    {
        get { return instancia == null ? 0 : Valor(instancia.bonusDano, Nivel(PESADO), 0); }
    }

    // Chamado quando o barco ia virar. Devolve true se o escudo absorveu o ataque.
    public static bool TentarUsarEscudo()
    {
        GerenciadorUpgrades g = instancia;
        if (g == null || Nivel(ESCUDO) <= 0 || !g.escudoCarregado) return false;

        g.escudoCarregado = false;

        float recarga = Valor(g.recargaEscudo, Nivel(ESCUDO), 0f);
        g.escudoProntoEm = recarga > 0f ? Time.time + recarga : float.PositiveInfinity;
        g.avisoEscudoAte = Time.unscaledTime + 2.5f;

        if (g.depurar) Debug.Log("[Upgrades] escudo absorveu o ataque (recarga: " + recarga + "s)");
        return true;
    }

    // Chamado quando uma bruxa morre. Sorteia a chance e, se acertar, da pontos.
    public static void BonusPorMatarBruxa()
    {
        GerenciadorUpgrades g = instancia;
        if (g == null) return;

        int nivel = Nivel(RECOMPENSA);
        if (nivel <= 0) return;

        float chance = Valor(g.chanceRecompensa, nivel, 0f);
        int pontos = Valor(g.pontosRecompensa, nivel, 0);

        if (pontos <= 0 || Random.value >= chance) return;

        if (g.depurar) Debug.Log("[Upgrades] recompensa por matar bruxa: +" + pontos + " pontos");

        if (GerenciadorJogo.instancia != null)
        {
            GerenciadorJogo.instancia.AdicionarPontos(pontos);
        }
    }

    void Awake()
    {
        instancia = this;
        livroAberto = false;
        Time.timeScale = 1f;
        CorrigirArraysVazios();
        Montar();
    }

    void CorrigirArraysVazios()
    {
        bool mexeu = false;

        if (custoAnzol == null || custoAnzol.Length == 0) { custoAnzol = new int[] { 60, 150, 300 }; mexeu = true; }
        if (bonusPontos == null || bonusPontos.Length == 0) { bonusPontos = new float[] { 0.25f, 0.5f, 1f }; mexeu = true; }
        if (custoColetor == null || custoColetor.Length == 0) { custoColetor = new int[] { 50, 120, 250 }; mexeu = true; }
        if (bonusCapacidade == null || bonusCapacidade.Length == 0) { bonusCapacidade = new int[] { 2, 4, 7 }; mexeu = true; }
        if (multiplicadorIntervalo == null || multiplicadorIntervalo.Length == 0) { multiplicadorIntervalo = new float[] { 0.85f, 0.7f, 0.55f }; mexeu = true; }
        if (bonusFerroNaCena == null || bonusFerroNaCena.Length == 0) { bonusFerroNaCena = new int[] { 0, 1, 2 }; mexeu = true; }
        if (custoMultiplo == null || custoMultiplo.Length == 0) { custoMultiplo = new int[] { 150, 300 }; mexeu = true; }
        if (projeteis == null || projeteis.Length == 0) { projeteis = new int[] { 2, 3 }; mexeu = true; }
        if (custoEscudo == null || custoEscudo.Length == 0) { custoEscudo = new int[] { 300 }; mexeu = true; }
        if (recargaEscudo == null || recargaEscudo.Length == 0) { recargaEscudo = new float[] { 60f }; mexeu = true; }
        if (custoFerroPesado == null || custoFerroPesado.Length == 0) { custoFerroPesado = new int[] { 200, 400 }; mexeu = true; }
        if (bonusDano == null || bonusDano.Length == 0) { bonusDano = new int[] { 1, 2 }; mexeu = true; }
        if (custoRecompensa == null || custoRecompensa.Length == 0) { custoRecompensa = new int[] { 150, 300 }; mexeu = true; }
        if (chanceRecompensa == null || chanceRecompensa.Length == 0) { chanceRecompensa = new float[] { 0.35f, 0.65f }; mexeu = true; }
        if (pontosRecompensa == null || pontosRecompensa.Length == 0) { pontosRecompensa = new int[] { 30, 50 }; mexeu = true; }

        // a cena salva tinha 3 niveis: aumenta para 6 sem perder o que ja existia
        if (niveis == null) { niveis = new int[TOTAL]; mexeu = true; }
        else if (niveis.Length < TOTAL) { System.Array.Resize(ref niveis, TOTAL); mexeu = true; }

        if (mexeu) Debug.LogWarning("[Upgrades] havia campos vazios no Inspector; usei os valores padrao.");
    }

    void OnDestroy()
    {
        if (instancia == this)
        {
            livroAberto = false;
            Time.timeScale = 1f;
        }
    }

    void Update()
    {
        if (Nivel(ESCUDO) > 0 && !escudoCarregado && Time.time >= escudoProntoEm)
        {
            escudoCarregado = true;
        }

        if (TutorialInicial.ativo) return;

        if (livroAberto)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) MudarPagina(1);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) MudarPagina(-1);
        }

        bool apertouLivro = Input.GetKeyDown(ConfiguracoesJogo.Tecla(AcaoTecla.AbrirLivro));
        bool apertouEsc = livroAberto && Input.GetKeyDown(KeyCode.Escape);
        if (!apertouLivro && !apertouEsc) return;

        if (livroAberto)
        {
            Fechar();
        }
        else if (PodeAbrir())
        {
            Abrir();
        }
    }

    bool PodeAbrir()
    {
        if (PainelPause.aberto) return false;
        GerenciadorJogo jogo = GerenciadorJogo.instancia;
        if (jogo == null) return true;
        if (!jogo.jogando) return false;

        if (jogo.eventoBarco != null && (jogo.eventoBarco.emAndamento || jogo.eventoBarco.virou))
        {
            return false;
        }

        return true;
    }

    void Abrir()
    {
        livroAberto = true;
        Time.timeScale = 0f;
        if (depurar) Debug.Log("[Upgrades] livro ABERTO (jogo pausado). saldo = " + saldo);
    }

    void Fechar()
    {
        livroAberto = false;
        Time.timeScale = 1f;
        if (depurar) Debug.Log("[Upgrades] livro FECHADO (jogo voltou a andar)");
    }

    void Montar()
    {
        entradas = new Entrada[TOTAL];

        entradas[ANZOL] = new Entrada();
        entradas[ANZOL].nome = "Anzol Dourado";
        entradas[ANZOL].custos = custoAnzol;
        entradas[ANZOL].efeitos = new string[custoAnzol.Length];
        for (int i = 0; i < custoAnzol.Length; i++)
        {
            int pct = Mathf.RoundToInt(Valor(bonusPontos, i + 1, 0f) * 100f);
            entradas[ANZOL].efeitos[i] = "Cada peixe vale +" + pct + "% de pontos";
        }

        entradas[COLETOR] = new Entrada();
        entradas[COLETOR].nome = "Coletor de Ferro";
        entradas[COLETOR].custos = custoColetor;
        entradas[COLETOR].efeitos = new string[custoColetor.Length];
        for (int i = 0; i < custoColetor.Length; i++)
        {
            int cap = Valor(bonusCapacidade, i + 1, 0);
            int rapido = Mathf.RoundToInt((1f - Valor(multiplicadorIntervalo, i + 1, 1f)) * 100f);
            int extra = Valor(bonusFerroNaCena, i + 1, 0);

            string texto = "Mochila +" + cap + " ferros  |  ferro nasce " + rapido + "% mais rapido";
            if (extra > 0) texto += "  |  +" + extra + " no mar";
            entradas[COLETOR].efeitos[i] = texto;
        }

        entradas[MULTIPLO] = new Entrada();
        entradas[MULTIPLO].nome = "Arremesso Multiplo";
        entradas[MULTIPLO].custos = custoMultiplo;
        entradas[MULTIPLO].efeitos = new string[custoMultiplo.Length];
        for (int i = 0; i < custoMultiplo.Length; i++)
        {
            entradas[MULTIPLO].efeitos[i] = "Joga " + Valor(projeteis, i + 1, 1) + " ferros de uma vez (gasta so 1)";
        }

        entradas[ESCUDO] = new Entrada();
        entradas[ESCUDO].nome = "Escudo do Barco";
        entradas[ESCUDO].custos = custoEscudo;
        entradas[ESCUDO].efeitos = new string[custoEscudo.Length];
        for (int i = 0; i < custoEscudo.Length; i++)
        {
            float recarga = Valor(recargaEscudo, i + 1, 0f);
            string texto = "Absorve 1 ataque que viraria o barco";
            texto += recarga > 0f ? "  |  recarrega em " + Mathf.RoundToInt(recarga) + "s" : "  |  uso unico";
            entradas[ESCUDO].efeitos[i] = texto;
        }

        entradas[PESADO] = new Entrada();
        entradas[PESADO].nome = "Ferro Pesado";
        entradas[PESADO].custos = custoFerroPesado;
        entradas[PESADO].efeitos = new string[custoFerroPesado.Length];
        for (int i = 0; i < custoFerroPesado.Length; i++)
        {
            entradas[PESADO].efeitos[i] = "Ferro causa +" + Valor(bonusDano, i + 1, 0) + " de dano nas bruxas";
        }

        entradas[RECOMPENSA] = new Entrada();
        entradas[RECOMPENSA].nome = "Cacador de Bruxas";
        entradas[RECOMPENSA].custos = custoRecompensa;
        entradas[RECOMPENSA].efeitos = new string[custoRecompensa.Length];
        for (int i = 0; i < custoRecompensa.Length; i++)
        {
            int pct = Mathf.RoundToInt(Valor(chanceRecompensa, i + 1, 0f) * 100f);
            entradas[RECOMPENSA].efeitos[i] = "Ao matar uma bruxa: " + pct + "% de chance de +" + Valor(pontosRecompensa, i + 1, 0) + " pontos";
        }
    }

    int TotalPaginas()
    {
        return Mathf.Max(1, Mathf.CeilToInt(entradas.Length / (float)POR_PAGINA));
    }

    void MudarPagina(int delta)
    {
        pagina = Mathf.Clamp(pagina + delta, 0, TotalPaginas() - 1);
    }

    void Comprar(int id)
    {
        Entrada e = entradas[id];
        if (niveis[id] >= e.custos.Length) return;

        int custo = e.custos[niveis[id]];
        if (saldo < custo) return;

        saldo -= custo;
        niveis[id]++;

        if (id == ESCUDO) escudoCarregado = true;
    }

    bool PodeComprarAlgo()
    {
        for (int i = 0; i < entradas.Length; i++)
        {
            if (niveis[i] < entradas[i].custos.Length && saldo >= entradas[i].custos[niveis[i]])
            {
                return true;
            }
        }
        return false;
    }

    static int Nivel(int id)
    {
        if (instancia == null || id >= instancia.niveis.Length) return 0;
        return instancia.niveis[id];
    }

    static float Valor(float[] lista, int nivel, float padrao)
    {
        if (lista == null || lista.Length == 0 || nivel <= 0) return padrao;
        return lista[Mathf.Min(nivel, lista.Length) - 1];
    }

    static int Valor(int[] lista, int nivel, int padrao)
    {
        if (lista == null || lista.Length == 0 || nivel <= 0) return padrao;
        return lista[Mathf.Min(nivel, lista.Length) - 1];
    }

    void OnGUI()
    {
        GerenciadorJogo jogo = GerenciadorJogo.instancia;
        if (jogo != null && !jogo.jogando) return;

        IniciarEstilos();

        if (livroAberto)
        {
            DesenharLivro();
        }
        else
        {
            DesenharHud();

            if (Time.unscaledTime < avisoEscudoAte) DesenharAvisoEscudo();
        }
    }

    void DesenharAvisoEscudo()
    {
        if (estAviso == null)
        {
            estAviso = new GUIStyle(estHud);
            estAviso.fontSize = 30;
            estAviso.alignment = TextAnchor.UpperCenter;
        }

        string texto = "O ESCUDO ABSORVEU O ATAQUE!";
        Rect r = new Rect(0f, Screen.height * 0.18f, Screen.width, 50f);

        GUI.color = Color.black;
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), texto, estAviso);
        GUI.color = Color.white;
        GUI.Label(r, texto, estAviso);
    }

    void DesenharHud()
    {
        string saldoTexto = nomeMoeda + ": " + saldo;
        Rect rTexto = new Rect(16f, Screen.height - 94f, 400f, 32f);

        GUI.color = Color.black;
        GUI.Label(new Rect(rTexto.x + 2f, rTexto.y + 2f, rTexto.width, rTexto.height), saldoTexto, estHud);
        GUI.color = Color.white;
        GUI.Label(rTexto, saldoTexto, estHud);

        if (Nivel(ESCUDO) > 0)
        {
            string txtEscudo;
            if (escudoCarregado) txtEscudo = "Escudo: PRONTO";
            else if (float.IsInfinity(escudoProntoEm)) txtEscudo = "Escudo: usado";
            else txtEscudo = "Escudo: recarregando (" + Mathf.CeilToInt(Mathf.Max(0f, escudoProntoEm - Time.time)) + "s)";

            Rect rEscudo = new Rect(16f, Screen.height - 132f, 400f, 32f);

            GUI.color = Color.black;
            GUI.Label(new Rect(rEscudo.x + 2f, rEscudo.y + 2f, rEscudo.width, rEscudo.height), txtEscudo, estHud);
            GUI.color = Color.white;
            GUI.Label(rEscudo, txtEscudo, estHud);
        }

        if (PodeAbrir())
        {
            string dica = "Guia do Pescador [" + teclaLivro + "]";
            if (PodeComprarAlgo()) dica += "  (!)";

            Rect rDica = new Rect(16f, Screen.height - 56f, 400f, 32f);

            GUI.color = Color.black;
            GUI.Label(new Rect(rDica.x + 2f, rDica.y + 2f, rDica.width, rDica.height), dica, estHud);
            GUI.color = Color.white;
            GUI.Label(rDica, dica, estHud);
        }
    }

    void DesenharLivro()
    {
        float escala = Screen.height / 720f;
        Matrix4x4 antes = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));

        float larguraTela = Screen.width / escala;
        const float alturaTela = 720f;

        Retangulo(new Rect(0f, 0f, larguraTela, alturaTela), new Color(0f, 0f, 0f, 0.55f));

        Rect livro = new Rect((larguraTela - 900f) / 2f, (alturaTela - 520f) / 2f, 900f, 520f);

        if (spriteLivro != null)
        {
            Rect destino = new Rect(livro.x - livroExpansao.x, livro.y - livroExpansao.y,
                livro.width + livroExpansao.x * 2f, livro.height + livroExpansao.y * 2f);
            DesenharSprite(destino, spriteLivro, corLivro);
        }
        else
        {
            Retangulo(livro, new Color(0.33f, 0.18f, 0.09f));
            Retangulo(new Rect(livro.x + 12f, livro.y + 12f, 876f, 496f), new Color(0.96f, 0.87f, 0.67f));
            Retangulo(new Rect(livro.center.x - 2f, livro.y + 12f, 4f, 496f), new Color(0.62f, 0.45f, 0.25f));
        }

        float ex = livro.x + 40f;
        float ey = livro.y + 40f;

        GUI.Label(new Rect(ex, ey, 390f, 50f), "Guia do Pescador", estTitulo);
        GUI.Label(new Rect(ex, ey + 60f, 390f, 40f), nomeMoeda + ": " + saldo, estMoeda);
        GUI.Label(new Rect(ex, ey + 120f, 390f, 260f),
            "Cada peixe pescado rende pontos (seu placar) e a mesma quantia em " + nomeMoeda.ToLower() + ".\n\n"
            + "Gaste as " + nomeMoeda.ToLower() + " nos upgrades ao lado. Gastar nao diminui seus pontos.\n\n"
            + "O jogo fica pausado enquanto o livro esta aberto."
            + (TotalPaginas() > 1 ? "\n\nUse as setas do teclado ou os botoes < > para trocar de pagina." : ""), estTexto);

        if (BotaoVisual(new Rect(ex, livro.yMax - 80f, 200f, 40f), "Fechar [" + teclaLivro + "]"))
        {
            Fechar();
        }

        float px = livro.center.x + 24f;

        int totalPaginas = TotalPaginas();
        pagina = Mathf.Clamp(pagina, 0, totalPaginas - 1);
        int primeiro = pagina * POR_PAGINA;
        int ultimo = Mathf.Min(primeiro + POR_PAGINA, entradas.Length);

        if (totalPaginas > 1)
        {
            float ny = livro.yMax - 80f;

            GUI.enabled = pagina > 0;
            if (BotaoVisual(new Rect(ex + 208f, ny, 44f, 40f), "<")) MudarPagina(-1);

            GUI.enabled = true;
            GUI.Label(new Rect(ex + 252f, ny, 94f, 40f), "Pag. " + (pagina + 1) + "/" + totalPaginas, estBotaoTexto);

            GUI.enabled = pagina < totalPaginas - 1;
            if (BotaoVisual(new Rect(ex + 346f, ny, 44f, 40f), ">")) MudarPagina(1);

            GUI.enabled = true;
        }

        for (int i = primeiro; i < ultimo; i++)
        {
            Entrada e = entradas[i];
            int nivel = niveis[i];
            int maximo = e.custos.Length;

            Rect r = new Rect(px, livro.y + 28f + (i - primeiro) * 160f, 396f, 148f);
            if (spriteLivro == null || desenharFundoDosCartoes) Retangulo(r, new Color(0.89f, 0.78f, 0.55f));

            GUI.Label(new Rect(r.x + 12f, r.y + 8f, 260f, 28f), e.nome, estNome);

            for (int n = 0; n < maximo; n++)
            {
                Color cor = (n < nivel) ? new Color(0.9f, 0.65f, 0.1f) : new Color(0.55f, 0.42f, 0.25f);
                Retangulo(new Rect(r.xMax - 14f - (maximo - n) * 22f, r.y + 14f, 16f, 16f), cor);
            }

            string descricao = "";
            if (nivel > 0) descricao += "Agora: " + e.efeitos[nivel - 1] + "\n";
            descricao += (nivel < maximo) ? "Proximo: " + e.efeitos[nivel] : "NIVEL MAXIMO";
            GUI.Label(new Rect(r.x + 12f, r.y + 40f, r.width - 24f, 60f), descricao, estTexto);

            if (nivel < maximo)
            {
                int custo = e.custos[nivel];
                bool pode = saldo >= custo;

                GUI.enabled = pode;
                GUI.backgroundColor = pode ? new Color(0.5f, 0.85f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
                if (BotaoVisual(new Rect(r.x + 12f, r.yMax - 42f, r.width - 24f, 32f),
                    "Comprar  -  " + custo + " " + nomeMoeda.ToLower()))
                {
                    Comprar(i);
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
            }
        }

        GUI.matrix = antes;
    }

    void Retangulo(Rect r, Color cor)
    {
        Color antes = GUI.color;
        GUI.color = cor;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = antes;
    }

    // Desenha um Sprite inteiro (esticado) dentro do retangulo. Funciona com sprite recortado de uma folha.
    void DesenharSprite(Rect destino, Sprite sprite, Color cor)
    {
        if (sprite == null || sprite.texture == null) return;

        Texture tex = sprite.texture;
        Rect t = (sprite.packed && sprite.packingMode == SpritePackingMode.Tight) ? sprite.rect : sprite.textureRect;
        Rect uv = new Rect(t.x / tex.width, t.y / tex.height, t.width / tex.width, t.height / tex.height);

        Color antes = GUI.color;
        GUI.color = cor;
        GUI.DrawTextureWithTexCoords(destino, tex, uv, true);
        GUI.color = antes;
    }

    // Botao com fundo em sprite e texto desenhado pelo codigo. Sem sprite, e o botao padrao de antes.
    bool BotaoVisual(Rect r, string texto)
    {
        if (spriteBotao == null)
        {
            // sem sprite: ou botao padrao (antigo), ou so o texto sobre a arte que ja existe no livro
            return GUI.Button(r, texto, botoesSemFundo ? estBotaoTexto : estBotao);
        }

        bool habilitado = GUI.enabled;
        bool sobre = habilitado && r.Contains(Event.current.mousePosition);
        bool apertado = sobre && Input.GetMouseButton(0);

        Sprite sprite = spriteBotao;
        Color cor = corBotao;

        if (!habilitado)
        {
            if (spriteBotaoDesabilitado != null) sprite = spriteBotaoDesabilitado;
            else cor = corBotaoDesabilitado;
        }
        else if (sobre && spriteBotaoHover != null)
        {
            sprite = spriteBotaoHover;
        }
        else if (apertado)
        {
            cor = new Color(cor.r * 0.85f, cor.g * 0.85f, cor.b * 0.85f, cor.a);
        }

        GUI.enabled = true;
        DesenharSprite(r, sprite, cor);
        GUI.enabled = habilitado;

        return GUI.Button(r, texto, estBotaoTexto);
    }

    void IniciarEstilos()
    {
        if (estTitulo != null) return;

        Color marrom = new Color(0.25f, 0.14f, 0.07f);

        estTitulo = new GUIStyle(GUI.skin.label);
        estTitulo.fontSize = 34;
        estTitulo.fontStyle = FontStyle.Bold;
        estTitulo.normal.textColor = marrom;

        estMoeda = new GUIStyle(GUI.skin.label);
        estMoeda.fontSize = 26;
        estMoeda.fontStyle = FontStyle.Bold;
        estMoeda.normal.textColor = new Color(0.7f, 0.4f, 0.05f);

        estNome = new GUIStyle(GUI.skin.label);
        estNome.fontSize = 22;
        estNome.fontStyle = FontStyle.Bold;
        estNome.normal.textColor = marrom;

        estTexto = new GUIStyle(GUI.skin.label);
        estTexto.fontSize = 17;
        estTexto.wordWrap = true;
        estTexto.normal.textColor = marrom;

        estBotao = new GUIStyle(GUI.skin.button);
        estBotao.fontSize = 18;
        estBotao.fontStyle = FontStyle.Bold;

        estHud = new GUIStyle(GUI.skin.label);
        estHud.fontSize = 22;
        estHud.fontStyle = FontStyle.Bold;
        estHud.normal.textColor = Color.white;

        // botao so com texto (o fundo e o sprite, ou a propria arte do livro).
        // Parte do estilo de LABEL, que nao tem imagem de fundo nenhuma (o estilo de botao padrao
        // guarda imagens extras em scaledBackgrounds, que reaparecem em telas com escala do Windows).
        estBotaoTexto = new GUIStyle(GUI.skin.label);
        estBotaoTexto.fontSize = 18;
        estBotaoTexto.fontStyle = FontStyle.Bold;
        estBotaoTexto.alignment = TextAnchor.MiddleCenter;
        estBotaoTexto.wordWrap = false;
        estBotaoTexto.normal.textColor = corTextoBotao;
        estBotaoTexto.hover.textColor = corTextoBotao;
        estBotaoTexto.active.textColor = corTextoBotao;
        estBotaoTexto.focused.textColor = corTextoBotao;
        estBotaoTexto.onNormal.textColor = corTextoBotao;
        estBotaoTexto.onHover.textColor = corTextoBotao;
        estBotaoTexto.onActive.textColor = corTextoBotao;
        estBotaoTexto.onFocused.textColor = corTextoBotao;

        // fonte padrao (Irish Grover) via FontePadrao.cs
        FontePadrao.Aplicar(estTitulo);
        FontePadrao.Aplicar(estMoeda);
        FontePadrao.Aplicar(estNome);
        FontePadrao.Aplicar(estTexto);
        FontePadrao.Aplicar(estBotao);
        FontePadrao.Aplicar(estHud);
        FontePadrao.Aplicar(estBotaoTexto);
    }
}