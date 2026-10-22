using UnityEngine;
using TMPro;

// Roda depois do balanco do barco (LateUpdate) para a ponta da vara nao ficar "atrasada" na linha.
[DefaultExecutionOrder(100)]
public class RaioDePesca : MonoBehaviour
{
    [Header("Raio")]
    public Transform origem;
    public float alcance = 8f;
    public float larguraDoRaio = 0.4f;
    public LayerMask camadaPeixe;

    [Header("UI (opcional)")]
    public TMP_Text textoPesca;

    [Header("Ligacoes")]
    public EventoEquilibrioBarco eventoBarco;
    public BruxaVoadora bruxa;
    public bool bloquearPescaNoEvento = true;

    [Header("Vara de pesca (a linha sai da ponta dela)")]
    [Tooltip("Objeto vazio filho da Vara, posicionado na PONTA do sprite. Se vazio, a linha sai da Origem.")]
    public Transform pontaDaVara;
    [Tooltip("Sprite da vara (opcional). Se informado, vira junto com o pescador quando ele mira pro outro lado.")]
    public SpriteRenderer spriteVara;
    [Tooltip("Marque se o desenho da vara, sem virar, aponta para a direita.")]
    public bool varaOlhaParaDireita = true;

    [Header("Fisica da linha")]
    public int segmentos = 20;
    [Tooltip("1.00 = linha esticada. Quanto maior, mais frouxa (mais barriga).")]
    public float folgaDaLinha = 1.02f;
    public float gravidade = 9f;
    [Tooltip("Perto de 1 balanca mais tempo; menor para a linha parar mais rapido.")]
    [Range(0.8f, 1f)] public float amortecimento = 0.985f;
    public int iteracoes = 12;

    [Header("Visual e som")]
    public SpriteRenderer spritePescador;
    public AudioSource audioFonte;
    public AudioClip somPesca;

    LineRenderer linha;
    Camera camera2D;
    Peixe peixeAtual;
    float tempoMirando;
    GUIStyle estiloPorcentagem;

    Vector2[] corda;
    Vector2[] cordaAnterior;
    Vector3[] pontosLinha;
    bool cordaIniciada;
    Vector2 fimDaLinha;
    bool viradoParaEsquerda;
    Vector3 localVaraOriginal;
    Vector3 localPontaOriginal;

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

        segmentos = Mathf.Max(2, segmentos);
        corda = new Vector2[segmentos + 1];
        cordaAnterior = new Vector2[segmentos + 1];
        pontosLinha = new Vector3[segmentos + 1];

        linha.useWorldSpace = true;
        linha.positionCount = segmentos + 1;
        linha.enabled = false;

        if (spriteVara != null) localVaraOriginal = spriteVara.transform.localPosition;
        if (pontaDaVara != null) localPontaOriginal = pontaDaVara.localPosition;
    }

    Vector3 PosicaoDaPonta()
    {
        return pontaDaVara != null ? pontaDaVara.position : origem.position;
    }

    void AplicarLadoDaVara(bool esquerda)
    {
        if (spriteVara == null && pontaDaVara == null) return;
        if (esquerda == viradoParaEsquerda) return;

        viradoParaEsquerda = esquerda;
        float espelho = esquerda ? -1f : 1f;

        if (spriteVara != null)
        {
            spriteVara.flipX = varaOlhaParaDireita ? esquerda : !esquerda;

            Vector3 p = localVaraOriginal;
            p.x *= espelho;
            spriteVara.transform.localPosition = p;
        }

        if (pontaDaVara != null)
        {
            Vector3 p = localPontaOriginal;
            p.x *= espelho;
            pontaDaVara.localPosition = p;
        }
    }

    void LateUpdate()
    {
        if (linha == null || !linha.enabled)
        {
            cordaIniciada = false;
            return;
        }

        SimularCorda();
    }

    void SimularCorda()
    {
        int n = corda.Length;
        Vector2 inicio = PosicaoDaPonta();
        Vector2 fim = fimDaLinha;

        if (!cordaIniciada)
        {
            for (int i = 0; i < n; i++)
            {
                Vector2 p = Vector2.Lerp(inicio, fim, i / (float)(n - 1));
                corda[i] = p;
                cordaAnterior[i] = p;
            }

            cordaIniciada = true;
        }

        float dt = Mathf.Clamp(Time.deltaTime, 0.0001f, 1f / 30f);
        float amort = Mathf.Pow(amortecimento, dt * 60f);
        Vector2 aceleracao = Vector2.down * gravidade * dt * dt;

        for (int i = 1; i < n - 1; i++)
        {
            Vector2 velocidade = (corda[i] - cordaAnterior[i]) * amort;
            cordaAnterior[i] = corda[i];
            corda[i] += velocidade + aceleracao;
        }

        float distancia = Vector2.Distance(inicio, fim);
        float tamanhoSegmento = Mathf.Max(0.01f, distancia * Mathf.Max(1f, folgaDaLinha) / (n - 1));

        for (int it = 0; it < Mathf.Max(1, iteracoes); it++)
        {
            corda[0] = inicio;
            corda[n - 1] = fim;

            for (int i = 0; i < n - 1; i++)
            {
                Vector2 delta = corda[i + 1] - corda[i];
                float d = delta.magnitude;
                if (d <= tamanhoSegmento || d < 0.00001f) continue;

                Vector2 correcao = delta / d * (d - tamanhoSegmento);

                if (i == 0)
                {
                    corda[i + 1] -= correcao;
                }
                else if (i + 1 == n - 1)
                {
                    corda[i] += correcao;
                }
                else
                {
                    corda[i] += correcao * 0.5f;
                    corda[i + 1] -= correcao * 0.5f;
                }
            }
        }

        corda[0] = inicio;
        corda[n - 1] = fim;

        for (int i = 0; i < n; i++)
        {
            pontosLinha[i] = new Vector3(corda[i].x, corda[i].y, 0f);
        }

        linha.SetPositions(pontosLinha);
    }

    void Update()
    {
        if (!PodePescar())
        {
            PararDeMirar();
            AtualizarTexto();
            return;
        }

        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            Mirar();
        }
        else
        {
            PararDeMirar();
        }

        AtualizarTexto();
    }

    bool PodePescar()
    {
        if (TutorialInicial.ativo) return false;

        if (GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando)
        {
            return false;
        }

        if (GerenciadorUpgrades.livroAberto) return false;

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

        Vector3 mouse = camera2D.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0f;

        bool esquerda;
        if (spritePescador != null)
        {
            esquerda = mouse.x < spritePescador.transform.position.x;
            spritePescador.flipX = esquerda;
        }
        else
        {
            esquerda = mouse.x < origem.position.x;
        }

        AplicarLadoDaVara(esquerda);

        Vector3 inicioDaLinha = PosicaoDaPonta();
        Vector2 direcao = (mouse - inicioDaLinha).normalized;

        Peixe peixe = AcharPeixeNoRaio(direcao);

        if (peixe != null)
        {
            fimDaLinha = peixe.transform.position;

            if (peixe == peixeAtual)
            {
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
                peixeAtual = peixe;
                tempoMirando = 0f;
                PintarLinha(0f);
            }
        }
        else
        {
            fimDaLinha = inicioDaLinha + (Vector3)(direcao * alcance);
            peixeAtual = null;
            tempoMirando = 0f;
            PintarLinha(0f);
        }
    }

    Peixe AcharPeixeNoRaio(Vector2 direcao)
    {
        Vector2 inicio = PosicaoDaPonta();
        float aoLongo;

        if (peixeAtual != null && PeixeEstaNoRaio(peixeAtual, inicio, direcao, out aoLongo))
        {
            return peixeAtual;
        }

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

    bool PeixeEstaNoRaio(Peixe p, Vector2 inicio, Vector2 direcao, out float aoLongo)
    {
        Vector2 posicao = p.transform.position;

        aoLongo = Mathf.Clamp(Vector2.Dot(posicao - inicio, direcao), 0f, alcance);
        Vector2 pontoNoRaio = inicio + direcao * aoLongo;

        float folga = larguraDoRaio + p.RaioDeAcerto();
        return Vector2.Distance(posicao, pontoNoRaio) <= folga;
    }

    void AtualizarTexto()
    {
        if (textoPesca != null && textoPesca.text != "") textoPesca.text = "";
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (peixeAtual == null || camera2D == null) return;

        float progresso = Mathf.Clamp01(tempoMirando / Mathf.Max(0.01f, peixeAtual.tempoParaPescar));

        SpriteRenderer sr = peixeAtual.GetComponent<SpriteRenderer>();
        float topo = (sr != null) ? sr.bounds.max.y : peixeAtual.transform.position.y + 0.5f;
        Vector3 tela = camera2D.WorldToScreenPoint(new Vector3(peixeAtual.transform.position.x, topo, 0f));

        if (estiloPorcentagem == null)
        {
            estiloPorcentagem = new GUIStyle(GUI.skin.label);
            estiloPorcentagem.fontStyle = FontStyle.Bold;
            estiloPorcentagem.alignment = TextAnchor.MiddleCenter;
            estiloPorcentagem.normal.textColor = Color.white;
        }

        float esc = Screen.height / 720f;
        estiloPorcentagem.fontSize = Mathf.RoundToInt(26f * esc);

        Rect r = new Rect(tela.x - 60f * esc, Screen.height - tela.y - 40f * esc, 120f * esc, 36f * esc);
        string texto = Mathf.RoundToInt(progresso * 100f) + "%";

        Color antes = GUI.color;
        GUI.color = Color.black;
        GUI.Label(new Rect(r.x + 2f * esc, r.y + 2f * esc, r.width, r.height), texto, estiloPorcentagem);
        GUI.color = Color.Lerp(Color.white, Color.yellow, progresso);
        GUI.Label(r, texto, estiloPorcentagem);
        GUI.color = antes;
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

        PeixePescadoVoando.Criar(peixe, origem);
        Destroy(peixe.gameObject);
        peixeAtual = null;
        tempoMirando = 0f;

        if (eraBruxa)
        {
            if (GerenciadorBruxas.instancia != null)
            {
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
        cordaIniciada = false;
        peixeAtual = null;
        tempoMirando = 0f;
    }

    void PintarLinha(float progresso)
    {
        Color cor = Color.Lerp(Color.white, Color.yellow, progresso);
        linha.startColor = cor;
        linha.endColor = cor;
    }
}