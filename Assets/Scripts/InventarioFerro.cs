using UnityEngine;
using TMPro;

// Inventario de ferro do pescador.
//  - PEGAR: aperte os botoes ESQUERDO e DIREITO do mouse AO MESMO TEMPO, com o mouse em cima do ferro.
//  - ARREMESSAR: SEGURE a tecla (F) para mirar (aparece uma setinha) e SOLTE para lancar na direcao do mouse.
// Coloque no mesmo objeto do RaioDePesca (o "Pescador").
public class InventarioFerro : MonoBehaviour
{
    [Header("Ligacoes (opcionais: pega do RaioDePesca se estiver no mesmo objeto)")]
    public Transform origem;                    // de onde o ferro sai e de onde se alcanca
    public EventoEquilibrioBarco eventoBarco;   // so pra nao jogar com o barco virado
    public GameObject prefabProjetil;           // sem prefab, cria um quadradinho escuro

    [Header("Inventario")]
    public int quantidade = 0;                  // ferros que voce tem agora
    public int capacidade = 5;

    [Header("Pegar (botao esquerdo + direito JUNTOS)")]
    public float alcancePegar = 8f;             // distancia maxima da origem
    public float raioClique = 0.7f;             // folga para clicar perto do ferro

    [Header("Arremessar (segure para mirar, solte para lancar)")]
    public KeyCode teclaArremessar = KeyCode.F;
    public float velocidadeArremesso = 14f;
    public float intervaloEntreArremessos = 0.4f;

    [Header("Setinha de mira")]
    public Sprite spriteSeta;                   // opcional: sem sprite, desenha uma seta amarela
    public float comprimentoSeta = 2.5f;
    public Color corSeta = new Color(1f, 0.9f, 0.2f, 0.95f);
    public int ordemSeta = 30;                  // ordem de desenho (maior = na frente)

    [Header("UI (opcional)")]
    public TMP_Text textoFerro;

    [Header("Som (opcional)")]
    public AudioSource audioFonte;
    public AudioClip somPegar;
    public AudioClip somArremessar;

    [Header("Depuracao")]
    public bool depurar = false;                // escreve no Console por que o ferro nao foi pego

    Camera camera2D;
    float proximoArremesso;
    SpriteRenderer seta;

    void Start()
    {
        camera2D = Camera.main;
        if (camera2D == null)
        {
            Debug.LogError("InventarioFerro: falta a camera com a tag MainCamera!");
            enabled = false;
            return;
        }

        // aproveita as ligacoes do RaioDePesca, se ele estiver no mesmo objeto
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
        if (!PodeAgir())
        {
            EsconderSeta();
            return;
        }

        // PEGAR: os dois botoes juntos (age uma vez, no instante em que o segundo botao desce)
        bool doisBotoes = Input.GetMouseButton(0) && Input.GetMouseButton(1);
        bool acabouDeJuntar = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
        if (doisBotoes && acabouDeJuntar)
        {
            TentarPegar();
        }

        // MIRAR: segurando a tecla (e tendo ferro), mostra a setinha
        if (quantidade > 0 && Input.GetKey(teclaArremessar))
        {
            MostrarSeta();
        }
        else
        {
            EsconderSeta();
        }

        // ARREMESSAR: ao soltar a tecla
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

    // so age enquanto o jogo esta rolando e o barco nao virou
    // (de proposito, pode arremessar DURANTE o ataque da bruxa: e a saida rapida)
    bool PodeAgir()
    {
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando)
        {
            return false;
        }

        if (eventoBarco != null && eventoBarco.virou)
        {
            return false;
        }

        return true;
    }

    void TentarPegar()
    {
        if (quantidade >= capacidade)
        {
            if (depurar) Debug.Log("[Ferro] inventario cheio (" + quantidade + "/" + capacidade + ").");
            return;
        }

        Vector3 mouse = MouseNoMundo();

        // acha o ferro mais perto do mouse, dentro do alcance da vara
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

        // o ferro vai exatamente para onde o mouse esta apontando
        Vector2 direcao = DirecaoParaOMouse();

        quantidade--;
        proximoArremesso = Time.time + intervaloEntreArremessos;

        // cria o projetil
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

        // se o prefab nao tem o script do projetil, coloca agora
        ProjetilFerro projetil = obj.GetComponent<ProjetilFerro>();
        if (projetil == null)
        {
            projetil = obj.AddComponent<ProjetilFerro>();
        }

        projetil.Lancar(direcao * velocidadeArremesso);

        TocarSom(somArremessar);
        AtualizarUI();
    }

    // ---------- setinha de mira ----------

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

    // Desenha uma seta branca apontando para a direita (o ponto de giro fica na ponta de tras).
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

    // ---------- utilidades ----------

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
        if (textoFerro != null)
        {
            textoFerro.text = "Ferro: " + quantidade + " / " + capacidade;
        }
    }
}