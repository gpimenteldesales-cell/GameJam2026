using UnityEngine;
using TMPro;

// Segure o botao esquerdo do mouse e mire no peixe.
// Mirando tempo suficiente, o peixe e pescado e voce ganha pontos.
public class RaioDePesca : MonoBehaviour
{
    [Header("Raio")]
    public Transform origem;          // ponta da vara
    public float alcance = 8f;        // tamanho maximo do raio
    public float larguraDoRaio = 0.4f; // folga ao mirar: o peixe conta se o raio passar perto dele
    public LayerMask camadaPeixe;     // (nao e mais usado; fica so pra nao perder a ligacao da cena)

    [Header("UI (opcional)")]
    public TMP_Text textoPesca;       // mostra o estado da pesca (ex: "PODE PESCAR | mirando 40%")

    [Header("Ligacoes")]
    public EventoEquilibrioBarco eventoBarco;
    public BruxaVoadora bruxa;
    public bool bloquearPescaNoEvento = true;   // nao pesca enquanto a bruxa ataca

    [Header("Visual e som")]
    public SpriteRenderer spritePescador;
    public AudioSource audioFonte;    // opcional
    public AudioClip somPesca;        // opcional

    LineRenderer linha;
    Camera camera2D;
    Peixe peixeAtual;      // peixe que esta sendo mirado agora
    float tempoMirando;    // ha quanto tempo esta mirando nele

    void Start()
    {
        camera2D = Camera.main;
        linha = GetComponent<LineRenderer>();

        if (linha == null || origem == null || camera2D == null)
        {
            Debug.LogError("RaioDePesca: falta o LineRenderer no mesmo objeto, o campo 'Origem', ou a camera com a tag MainCamera!");
            enabled = false;
            return;
        }

        linha.positionCount = 2;
        linha.enabled = false;
    }

    void Update()
    {
        if (!PodePescar())
        {
            PararDeMirar();
            AtualizarTexto();
            return;
        }

        if (Input.GetMouseButton(0))
        {
            Mirar();
        }
        else
        {
            PararDeMirar();
        }

        AtualizarTexto();
    }

    // so pesca enquanto o jogo esta rolando e o barco nao virou
    bool PodePescar()
    {
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando)
        {
            return false;
        }

        if (eventoBarco != null)
        {
            if (eventoBarco.virou) return false;
            if (bloquearPescaNoEvento && eventoBarco.emAndamento) return false;
        }

        return true;
    }

    void Mirar()
    {
        linha.enabled = true;

        // 1. posicao do mouse no mundo do jogo
        Vector3 mouse = camera2D.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0f;

        // 2. vira o pescador para o lado do mouse
        if (spritePescador != null)
        {
            spritePescador.flipX = mouse.x < spritePescador.transform.position.x;
        }

        // 3. direcao da vara ate o mouse
        Vector2 direcao = (mouse - origem.position).normalized;

        // 4. procura o peixe que o raio atravessa (por distancia, nao precisa de collider)
        Peixe peixe = AcharPeixeNoRaio(direcao);

        // 5. desenha a linha da vara
        linha.SetPosition(0, origem.position);

        if (peixe != null)
        {
            // o raio passou por um peixe: a linha termina nele
            linha.SetPosition(1, peixe.transform.position);

            if (peixe == peixeAtual)
            {
                // continua mirando no mesmo peixe
                tempoMirando += Time.deltaTime;
                float progresso = tempoMirando / peixeAtual.tempoParaPescar;
                PintarLinha(progresso);

                if (progresso >= 1f)
                {
                    Pescar(peixeAtual);
                }
            }
            else
            {
                // trocou de peixe: comeca a contar de novo
                peixeAtual = peixe;
                tempoMirando = 0f;
                PintarLinha(0f);
            }
        }
        else
        {
            // o raio nao pegou nada
            linha.SetPosition(1, origem.position + (Vector3)(direcao * alcance));
            peixeAtual = null;
            tempoMirando = 0f;
            PintarLinha(0f);
        }
    }

    // Devolve o peixe que o raio atravessa (ou null).
    // Se voce ja esta mirando num peixe e o raio continua nele, ele FICA TRAVADO:
    // outro peixe que cruze na frente nao tira a sua mira.
    // So quando o raio sai do peixe atual e que procura outro (o mais perto da vara).
    Peixe AcharPeixeNoRaio(Vector2 direcao)
    {
        Vector2 inicio = origem.position;
        float aoLongo;

        // 1. continua no peixe que ja estava sendo mirado?
        if (peixeAtual != null && PeixeEstaNoRaio(peixeAtual, inicio, direcao, out aoLongo))
        {
            return peixeAtual;
        }

        // 2. senao, escolhe o peixe mais perto da vara entre os que o raio atravessa
        Peixe melhor = null;
        float menorAoLongo = float.MaxValue;

        for (int i = 0; i < Peixe.todos.Count; i++)
        {
            Peixe p = Peixe.todos[i];

            if (PeixeEstaNoRaio(p, inicio, direcao, out aoLongo) && aoLongo < menorAoLongo)
            {
                menorAoLongo = aoLongo;
                melhor = p;
            }
        }

        return melhor;
    }

    // O peixe conta se o raio passar a menos de "larguraDoRaio" + o tamanho dele.
    // "aoLongo" e a distancia da vara ate o ponto do raio mais perto do peixe.
    bool PeixeEstaNoRaio(Peixe p, Vector2 inicio, Vector2 direcao, out float aoLongo)
    {
        Vector2 posicao = p.transform.position;

        aoLongo = Mathf.Clamp(Vector2.Dot(posicao - inicio, direcao), 0f, alcance);
        Vector2 pontoNoRaio = inicio + direcao * aoLongo;

        float folga = larguraDoRaio + p.RaioDeAcerto();
        return Vector2.Distance(posicao, pontoNoRaio) <= folga;
    }

    // Escreve o estado da pesca no texto da tela (se voce ligou o campo "Texto Pesca")
    void AtualizarTexto()
    {
        if (textoPesca == null) return;

        string estado = "PODE PESCAR";
        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando) estado = "BLOQUEADO: jogo acabou";
        else if (eventoBarco != null && eventoBarco.virou) estado = "BLOQUEADO: barco virou";
        else if (eventoBarco != null && bloquearPescaNoEvento && eventoBarco.emAndamento) estado = "BLOQUEADO: bruxa atacando (aperte E e Q)";

        string mira = "";
        if (peixeAtual != null)
        {
            mira = "  |  mirando (" + Mathf.RoundToInt(100f * tempoMirando / peixeAtual.tempoParaPescar) + "%)";
        }

        textoPesca.text = "Pesca: " + estado + "  |  peixes na cena: " + Peixe.todos.Count + mira;
    }

    void Pescar(Peixe peixe)
    {
        bool eraBruxa = peixe.ehBruxa;

        if (GerenciadorJogo.instancia != null)
        {
            GerenciadorJogo.instancia.AdicionarPontos(peixe.valor);
        }

        if (audioFonte != null && somPesca != null)
        {
            audioFonte.PlayOneShot(somPesca);
        }

        Destroy(peixe.gameObject);
        peixeAtual = null;
        tempoMirando = 0f;

        // era uma bruxa disfarcada! ela ataca na hora
        if (eraBruxa)
        {
            if (GerenciadorBruxas.instancia != null)
            {
                // escolhe a bruxa mais perto (ou cria uma que ja nasce atacando)
                GerenciadorBruxas.instancia.ForcarAtaque();
            }
            else if (bruxa != null)
            {
                bruxa.ForcarAtaque();
            }
        }
    }

    void PararDeMirar()
    {
        linha.enabled = false;
        peixeAtual = null;
        tempoMirando = 0f;
    }

    // a linha vai de branco para amarelo conforme o peixe e fisgado
    void PintarLinha(float progresso)
    {
        Color cor = Color.Lerp(Color.white, Color.yellow, progresso);
        linha.startColor = cor;
        linha.endColor = cor;
    }
}