using UnityEngine;
using TMPro;

// Inventario de ferro do pescador.
//  - Clique DIREITO em cima de um ferro que boia no mar para pegar.
//  - Aperte F para arremessar um ferro na bruxa (vai na direcao do mouse).
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

    [Header("Pegar")]
    public int botaoMouseParaPegar = 1;         // 0 = esquerdo, 1 = direito
    public float alcancePegar = 8f;             // distancia maxima da origem
    public float raioClique = 0.7f;             // folga para clicar perto do ferro

    [Header("Arremessar")]
    public KeyCode teclaArremessar = KeyCode.F;
    public float velocidadeArremesso = 14f;
    public float intervaloEntreArremessos = 0.4f;
    public float raioMiraAssistida = 1.5f;      // mouse perto de uma bruxa? o ferro vai nela (0 = desliga)

    [Header("UI (opcional)")]
    public TMP_Text textoFerro;

    [Header("Som (opcional)")]
    public AudioSource audioFonte;
    public AudioClip somPegar;
    public AudioClip somArremessar;

    Camera camera2D;
    float proximoArremesso;

    void Start()
    {
        camera2D = Camera.main;

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
        if (!PodeAgir()) return;

        if (Input.GetMouseButtonDown(botaoMouseParaPegar))
        {
            TentarPegar();
        }

        if (Input.GetKeyDown(teclaArremessar))
        {
            TentarArremessar();
        }
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
        if (quantidade >= capacidade) return;   // inventario cheio

        Vector3 mouse = MouseNoMundo();

        // acha o ferro mais perto do mouse, dentro do alcance da vara
        ItemFerro escolhido = null;
        float menorDistancia = raioClique;

        for (int i = 0; i < ItemFerro.todos.Count; i++)
        {
            ItemFerro item = ItemFerro.todos[i];

            float distanciaDoMouse = Vector2.Distance(item.transform.position, mouse);
            if (distanciaDoMouse > menorDistancia) continue;

            float distanciaDaOrigem = Vector2.Distance(item.transform.position, origem.position);
            if (distanciaDaOrigem > alcancePegar) continue;

            menorDistancia = distanciaDoMouse;
            escolhido = item;
        }

        if (escolhido == null) return;

        quantidade++;
        escolhido.Coletado();
        TocarSom(somPegar);
        AtualizarUI();
    }

    void TentarArremessar()
    {
        if (quantidade <= 0) return;
        if (Time.time < proximoArremesso) return;

        Vector3 destino = MouseNoMundo();

        // mira assistida: se o mouse esta perto de uma bruxa, mira nela
        BruxaVoadora alvo = BruxaPertoDoMouse(destino);
        if (alvo != null)
        {
            destino = alvo.transform.position;
        }

        Vector2 direcao = (Vector2)(destino - origem.position);
        if (direcao.sqrMagnitude < 0.0001f)
        {
            direcao = Vector2.up;
        }
        direcao.Normalize();

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

    // bruxa (que nao esteja fugindo) mais perto do mouse, dentro do raio de mira assistida
    BruxaVoadora BruxaPertoDoMouse(Vector3 mouse)
    {
        if (raioMiraAssistida <= 0f) return null;

        BruxaVoadora melhor = null;
        float menorDistancia = raioMiraAssistida;

        for (int i = 0; i < BruxaVoadora.todas.Count; i++)
        {
            BruxaVoadora bruxa = BruxaVoadora.todas[i];
            if (bruxa.estadoAtual == BruxaVoadora.Estado.Recuando) continue;

            float d = Vector2.Distance(bruxa.transform.position, mouse);
            if (d < menorDistancia)
            {
                menorDistancia = d;
                melhor = bruxa;
            }
        }

        return melhor;
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