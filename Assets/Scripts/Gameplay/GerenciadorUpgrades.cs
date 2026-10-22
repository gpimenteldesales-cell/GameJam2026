using UnityEngine;

public class GerenciadorUpgrades : MonoBehaviour
{
    public static GerenciadorUpgrades instancia;
    public static bool livroAberto;

    [Header("Livro")]
    public KeyCode teclaLivro = KeyCode.B;
    public string nomeMoeda = "Perolas";

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

    [Header("Depuracao")]
    public bool depurar = true;

    [Header("Estado (so pra ver no Inspector)")]
    public int saldo;
    public int[] niveis = new int[3];

    const int ANZOL = 0;
    const int COLETOR = 1;
    const int MULTIPLO = 2;

    class Entrada
    {
        public string nome;
        public int[] custos;
        public string[] efeitos;
    }

    Entrada[] entradas;

    GUIStyle estTitulo, estMoeda, estNome, estTexto, estBotao, estHud;

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
        if (niveis == null || niveis.Length < 3) { niveis = new int[3]; mexeu = true; }

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
        if (TutorialInicial.ativo) return;

        bool apertouLivro = Input.GetKeyDown(teclaLivro);
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
        entradas = new Entrada[3];

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
    }

    void Comprar(int id)
    {
        Entrada e = entradas[id];
        if (niveis[id] >= e.custos.Length) return;

        int custo = e.custos[niveis[id]];
        if (saldo < custo) return;

        saldo -= custo;
        niveis[id]++;
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
        }
    }

    void DesenharHud()
    {
        string saldoTexto = nomeMoeda + ": " + saldo;
        Rect rTexto = new Rect(16f, Screen.height - 94f, 400f, 32f);

        GUI.color = Color.black;
        GUI.Label(new Rect(rTexto.x + 2f, rTexto.y + 2f, rTexto.width, rTexto.height), saldoTexto, estHud);
        GUI.color = Color.white;
        GUI.Label(rTexto, saldoTexto, estHud);

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
        Retangulo(livro, new Color(0.33f, 0.18f, 0.09f));
        Retangulo(new Rect(livro.x + 12f, livro.y + 12f, 876f, 496f), new Color(0.96f, 0.87f, 0.67f));
        Retangulo(new Rect(livro.center.x - 2f, livro.y + 12f, 4f, 496f), new Color(0.62f, 0.45f, 0.25f));

        float ex = livro.x + 40f;
        float ey = livro.y + 40f;

        GUI.Label(new Rect(ex, ey, 390f, 50f), "Guia do Pescador", estTitulo);
        GUI.Label(new Rect(ex, ey + 60f, 390f, 40f), nomeMoeda + ": " + saldo, estMoeda);
        GUI.Label(new Rect(ex, ey + 120f, 390f, 260f),
            "Cada peixe pescado rende pontos (seu placar) e a mesma quantia em " + nomeMoeda.ToLower() + ".\n\n"
            + "Gaste as " + nomeMoeda.ToLower() + " nos upgrades ao lado. Gastar nao diminui seus pontos.\n\n"
            + "O jogo fica pausado enquanto o livro esta aberto.", estTexto);

        if (GUI.Button(new Rect(ex, livro.yMax - 80f, 200f, 40f), "Fechar [" + teclaLivro + "]", estBotao))
        {
            Fechar();
        }

        float px = livro.center.x + 24f;

        for (int i = 0; i < entradas.Length; i++)
        {
            Entrada e = entradas[i];
            int nivel = niveis[i];
            int maximo = e.custos.Length;

            Rect r = new Rect(px, livro.y + 28f + i * 160f, 396f, 148f);
            Retangulo(r, new Color(0.89f, 0.78f, 0.55f));

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
                if (GUI.Button(new Rect(r.x + 12f, r.yMax - 42f, r.width - 24f, 32f),
                    "Comprar  -  " + custo + " " + nomeMoeda.ToLower(), estBotao))
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
    }
}