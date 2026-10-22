using UnityEngine;

// Segure o botao esquerdo do mouse e mire no peixe.
// Mirando tempo suficiente, o peixe e pescado e voce ganha pontos.
public class RaioDePesca : MonoBehaviour
{
    [Header("Raio")]
    public Transform origem;          // ponta da vara
    public float alcance = 8f;        // tamanho maximo do raio
    public LayerMask camadaPeixe;     // layer onde estao os peixes

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
        linha.positionCount = 2;
        linha.enabled = false;
    }

    void Update()
    {
        if (!PodePescar())
        {
            PararDeMirar();
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

        // 4. lanca um raio invisivel nessa direcao
        RaycastHit2D acerto = Physics2D.Raycast(origem.position, direcao, alcance, camadaPeixe);

        // 5. desenha a linha da vara
        linha.SetPosition(0, origem.position);

        if (acerto.collider != null)
        {
            // o raio bateu em algo
            linha.SetPosition(1, acerto.point);
            Peixe peixe = acerto.collider.GetComponentInParent<Peixe>();

            if (peixe != null && peixe == peixeAtual)
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
        if (eraBruxa && bruxa != null)
        {
            bruxa.ForcarAtaque();
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
