using UnityEngine;
using TMPro;

public class InventarioFerro : MonoBehaviour
{
    [Header("Ligacoes (opcionais: pega do RaioDePesca se estiver no mesmo objeto)")]
    public Transform origem;
    public EventoEquilibrioBarco eventoBarco;
    public GameObject prefabProjetil;

    [Header("Inventario")]
    public int quantidade = 0;
    public int capacidade = 5;

    [Header("Pegar (botao esquerdo + direito JUNTOS)")]
    public float alcancePegar = 8f;
    public float raioClique = 0.7f;

    [Header("Arremessar (segure para mirar, solte para lancar)")]
    public KeyCode teclaArremessar = KeyCode.F;
    public float velocidadeArremesso = 14f;
    public float intervaloEntreArremessos = 0.4f;

    [Header("Setinha de mira")]
    public Sprite spriteSeta;
    public float comprimentoSeta = 2.5f;
    public Color corSeta = new Color(1f, 0.9f, 0.2f, 0.95f);
    public int ordemSeta = 30;

    [Header("UI (opcional)")]
    public TMP_Text textoFerro;

    [Header("Som (opcional)")]
    public AudioSource audioFonte;
    public AudioClip somPegar;
    public AudioClip somArremessar;

    [Header("Depuracao")]
    public bool depurar = false;

    Camera camera2D;
    float proximoArremesso;
    SpriteRenderer seta;
    int capacidadeMostrada = -1;

    public int CapacidadeAtual
    {
        get { return capacidade + GerenciadorUpgrades.BonusCapacidade; }
    }

    void Start()
    {
        camera2D = Camera.main;
        if (camera2D == null)
        {
            Debug.LogError("InventarioFerro: falta a camera com a tag MainCamera!");
            enabled = false;
            return;
        }

        RaioDePesca pesca = GetComponent<RaioDePesca>();
        if (pesca != null)
        {
            if (origem == null) origem = pesca.origem;
            if (eventoBarco == null) eventoBarco = pesca.eventoBarco;
        }

        if (origem == null) origem = transform;

        AtualizarUI();
    }

    void Update()
    {
        if (CapacidadeAtual != capacidadeMostrada)
        {
            AtualizarUI();
        }

        if (!PodeAgir())
        {
            EsconderSeta();
            return;
        }

        bool doisBotoes = Input.GetMouseButton(0) && Input.GetMouseButton(1);
        bool acabouDeJuntar = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
        if (doisBotoes && acabouDeJuntar)
        {
            TentarPegar();
        }

        if (quantidade > 0 && Input.GetKey(teclaArremessar))
        {
            MostrarSeta();
        }
        else
        {
            EsconderSeta();
        }

        if (Input.GetKeyUp(teclaArremessar))
        {
            TentarArremessar();
        }
    }

    void OnDisable()
    {
        EsconderSeta();
    }

    void OnDestroy()
    {
        if (seta != null) Destroy(seta.gameObject);
    }

    bool PodeAgir()
    {
        if (TutorialInicial.ativo) return false;

        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando)
        {
            return false;
        }

        if (eventoBarco != null && eventoBarco.virou)
        {
            return false;
        }

        if (GerenciadorUpgrades.livroAberto) return false;

        return true;
    }

    void TentarPegar()
    {
        if (quantidade >= CapacidadeAtual)
        {
            if (depurar) Debug.Log("[Ferro] inventario cheio (" + quantidade + "/" + CapacidadeAtual + ").");
            return;
        }

        Vector3 mouse = MouseNoMundo();

        ItemFerro escolhido = null;
        float menorDistancia = raioClique;

        for (int i = 0; i < ItemFerro.todos.Count; i++)
        {
            ItemFerro item = ItemFerro.todos[i];

            float distanciaDoMouse = Vector2.Distance(item.transform.position, mouse);
            float distanciaDaOrigem = Vector2.Distance(item.transform.position, origem.position);

            if (depurar)
            {
                Debug.Log("[Ferro] item em " + item.transform.position
                    + " | distancia do mouse: " + distanciaDoMouse.ToString("0.00") + " (max " + raioClique + ")"
                    + " | distancia da origem: " + distanciaDaOrigem.ToString("0.00") + " (max " + alcancePegar + ")");
            }

            if (distanciaDoMouse > menorDistancia) continue;
            if (distanciaDaOrigem > alcancePegar) continue;

            menorDistancia = distanciaDoMouse;
            escolhido = item;
        }

        if (escolhido == null)
        {
            if (depurar) Debug.Log("[Ferro] nenhum ferro perto do mouse (ferros na cena: " + ItemFerro.todos.Count + ").");
            return;
        }

        quantidade++;
        escolhido.Coletado();
        TocarSom(somPegar);
        AtualizarUI();
    }

    void TentarArremessar()
    {
        if (quantidade <= 0) return;
        if (Time.time < proximoArremesso) return;

        Vector2 direcao = DirecaoParaOMouse();

        quantidade--;
        proximoArremesso = Time.time + intervaloEntreArremessos;

        int total = GerenciadorUpgrades.ProjeteisPorArremesso;
        float passo = GerenciadorUpgrades.AnguloEntreProjeteis;

        for (int i = 0; i < total; i++)
        {
            float desvio = (i - (total - 1) / 2f) * passo;
            LancarUm(Girar(direcao, desvio));
        }

        TocarSom(somArremessar);
        AtualizarUI();
    }

    void LancarUm(Vector2 direcao)
    {
        GameObject obj;
        if (prefabProjetil != null)
        {
            obj = Instantiate(prefabProjetil, origem.position, Quaternion.identity);
        }
        else
        {
            obj = ItemFerro.CriarPlaceholder("FerroArremessado", 0.35f, new Color(0.3f, 0.32f, 0.36f));
            obj.transform.position = origem.position;
        }

        ProjetilFerro projetil = obj.GetComponent<ProjetilFerro>();
        if (projetil == null)
        {
            projetil = obj.AddComponent<ProjetilFerro>();
        }

        projetil.Lancar(direcao * velocidadeArremesso);
    }

    static Vector2 Girar(Vector2 v, float graus)
    {
        float rad = graus * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    void MostrarSeta()
    {
        if (seta == null) CriarSeta();

        Vector2 direcao = DirecaoParaOMouse();
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

        seta.transform.position = origem.position;
        seta.transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        float escala = comprimentoSeta / seta.sprite.bounds.size.x;
        seta.transform.localScale = new Vector3(escala, escala, 1f);

        seta.gameObject.SetActive(true);
    }

    void EsconderSeta()
    {
        if (seta != null) seta.gameObject.SetActive(false);
    }

    void CriarSeta()
    {
        GameObject obj = new GameObject("SetaDeMira");
        seta = obj.AddComponent<SpriteRenderer>();
        seta.sprite = (spriteSeta != null) ? spriteSeta : CriarSpriteSeta();
        seta.color = corSeta;
        seta.sortingOrder = ordemSeta;
        obj.SetActive(false);
    }

    static Sprite CriarSpriteSeta()
    {
        const int largura = 64;
        const int altura = 16;
        const int inicioPonta = 44;

        Texture2D tex = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float meio = (altura - 1) / 2f;
        Color[] pixels = new Color[largura * altura];

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                bool corpo = x < inicioPonta && Mathf.Abs(y - meio) <= 2f;

                float progresso = (x - inicioPonta) / (float)(largura - 1 - inicioPonta);
                float metade = Mathf.Lerp(altura / 2f, 0f, progresso);
                bool ponta = x >= inicioPonta && Mathf.Abs(y - meio) <= metade;

                pixels[y * largura + x] = (corpo || ponta) ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, largura, altura), new Vector2(0f, 0.5f), 32f);
    }

    Vector2 DirecaoParaOMouse()
    {
        Vector2 direcao = (Vector2)(MouseNoMundo() - origem.position);
        if (direcao.sqrMagnitude < 0.0001f)
        {
            return Vector2.up;
        }
        return direcao.normalized;
    }

    Vector3 MouseNoMundo()
    {
        Vector3 mouse = camera2D.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0f;
        return mouse;
    }

    void TocarSom(AudioClip clip)
    {
        if (audioFonte != null && clip != null)
        {
            audioFonte.PlayOneShot(clip);
        }
    }

    void AtualizarUI()
    {
        capacidadeMostrada = CapacidadeAtual;

        if (textoFerro != null)
        {
            textoFerro.text = "Ferro: " + quantidade + " / " + CapacidadeAtual;
        }
    }
}