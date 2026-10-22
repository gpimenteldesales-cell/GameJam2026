using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialInicial : MonoBehaviour
{
    public static bool ativo;
    static bool jaMostrado;

    [Header("Alvos")]
    public Transform pescador;
    public RectTransform contadorPontos;
    public RectTransform contadorFerro;
    public Transform barco;
    public bool explicarLivro = true;

    [Header("Textos")]
    [TextArea] public string mensagemPesca = "Segure o botao ESQUERDO do mouse e mire num peixe para pescar.\nPeixes que piscam roxo sao bruxas disfarcadas!";
    [TextArea] public string mensagemPontos = "Cada peixe vale pontos. Faca o maximo antes do barco virar!";
    [TextArea] public string mensagemFerro = "Aperte os DOIS botoes do mouse em cima de um ferro para pegar.\nSegure F, mire e solte para arremessar.";
    [TextArea] public string mensagemBarco = "Quando a bruxa atacar, aperte E e Q alternados para equilibrar o barco.";
    [TextArea] public string mensagemLivro = "Aperte B para abrir o Guia do Pescador e comprar upgrades.";

    [Header("Foco")]
    public float raioMinimo = 70f;
    public float raioMaximo = 320f;
    public float folga = 24f;
    public float duracaoZoom = 0.7f;
    [Range(0f, 1f)] public float escuridaoMaxima = 0.8f;
    public Color corAnel = new Color(1f, 0.9f, 0.3f, 1f);

    [Header("Livro (posicao fixa na tela, em pixels a partir do canto inferior esquerdo)")]
    public Vector2 livroDistanciaDoCanto = new Vector2(166f, 62f);
    public float livroRaio = 150f;

    [Header("Quando mostrar")]
    public bool mostrarSempre = false;

    class Passo
    {
        public string texto;
        public Transform mundo;
        public RectTransform ui;
        public bool fixo;
    }

    readonly Vector3[] cantos = new Vector3[4];

    Camera camera2D;
    bool visivel;
    bool pular;
    Vector2 centroAtual;
    float raioAtual;
    float escuridao;
    float alphaCaixa;
    string mensagem = "";
    int numeroPasso;
    int totalPassos;

    GUIStyle estTexto;
    GUIStyle estDica;

    void Awake()
    {
        ativo = mostrarSempre || !jaMostrado;
        visivel = false;
        pular = false;
    }

    void Start()
    {
        camera2D = Camera.main;

        if (!ativo) return;

        StartCoroutine(Rodar());
    }

    void OnDestroy()
    {
        if (ativo)
        {
            ativo = false;
            Time.timeScale = 1f;
        }
    }

    List<Passo> MontarPassos()
    {
        List<Passo> passos = new List<Passo>();

        if (pescador != null) passos.Add(new Passo { texto = mensagemPesca, mundo = pescador });
        if (contadorPontos != null) passos.Add(new Passo { texto = mensagemPontos, ui = contadorPontos });
        if (contadorFerro != null) passos.Add(new Passo { texto = mensagemFerro, ui = contadorFerro });
        if (barco != null) passos.Add(new Passo { texto = mensagemBarco, mundo = barco });
        if (explicarLivro && GerenciadorUpgrades.instancia != null) passos.Add(new Passo { texto = mensagemLivro, fixo = true });

        return passos;
    }

    IEnumerator Rodar()
    {
        Time.timeScale = 0f;

        while (FadeCircular.instancia != null && !FadeCircular.concluido)
        {
            yield return null;
        }

        List<Passo> passos = MontarPassos();

        if (passos.Count == 0)
        {
            Time.timeScale = 1f;
            jaMostrado = true;
            ativo = false;
            yield break;
        }

        totalPassos = passos.Count;
        centroAtual = new Vector2(Screen.width / 2f, Screen.height / 2f);
        raioAtual = Diagonal();
        escuridao = 0f;
        visivel = true;

        for (int i = 0; i < passos.Count && !pular; i++)
        {
            numeroPasso = i + 1;
            mensagem = passos[i].texto;
            alphaCaixa = 0f;

            Vector2 deCentro = centroAtual;
            float deRaio = (i == 0) ? raioAtual : Mathf.Max(raioAtual, 120f) * 1.8f;
            float t = 0f;

            while (!pular)
            {
                float dt = DeltaReal();
                t += dt;

                float k = Suave(t / Mathf.Max(0.01f, duracaoZoom));

                Vector2 c;
                float r;
                ObterAlvo(passos[i], out c, out r);
                r *= 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.03f * k;

                centroAtual = Vector2.Lerp(deCentro, c, k);
                raioAtual = Mathf.Lerp(deRaio, r, k);
                escuridao = Mathf.MoveTowards(escuridao, escuridaoMaxima, dt * 4f);
                alphaCaixa = Mathf.Clamp01((t - duracaoZoom * 0.4f) / 0.3f);

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    pular = true;
                    break;
                }

                if (t >= duracaoZoom * 0.6f && Avancou()) break;

                yield return null;
            }
        }

        float s = 0f;
        float raioFinal = raioAtual;
        float diagonal = Diagonal();
        float escuridaoFinal = escuridao;
        float caixaFinal = alphaCaixa;

        while (s < 1f)
        {
            s += DeltaReal() / 0.45f;
            float k = Suave(s);
            raioAtual = Mathf.Lerp(raioFinal, diagonal, k);
            escuridao = Mathf.Lerp(escuridaoFinal, 0f, k);
            alphaCaixa = Mathf.Lerp(caixaFinal, 0f, k);
            yield return null;
        }

        visivel = false;
        Time.timeScale = 1f;
        jaMostrado = true;

        while (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            yield return null;
        }

        ativo = false;
    }

    static bool Avancou()
    {
        return Input.GetMouseButtonDown(0)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return);
    }

    static float DeltaReal()
    {
        return Mathf.Min(Time.unscaledDeltaTime, 0.05f);
    }

    static float Suave(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    static float Diagonal()
    {
        return new Vector2(Screen.width, Screen.height).magnitude;
    }

    void ObterAlvo(Passo p, out Vector2 centro, out float raio)
    {
        if (p.fixo)
        {
            centro = new Vector2(livroDistanciaDoCanto.x, Screen.height - livroDistanciaDoCanto.y);
            raio = livroRaio;
            return;
        }

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        if (p.ui != null)
        {
            Canvas canvas = p.ui.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            TMP_Text tmp = p.ui.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.ForceMeshUpdate();
                Bounds b = tmp.textBounds;
                cantos[0] = tmp.transform.TransformPoint(new Vector3(b.min.x, b.min.y, 0f));
                cantos[1] = tmp.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0f));
                cantos[2] = tmp.transform.TransformPoint(new Vector3(b.max.x, b.max.y, 0f));
                cantos[3] = tmp.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0f));
            }
            else
            {
                p.ui.GetWorldCorners(cantos);
            }

            for (int i = 0; i < 4; i++)
            {
                Incluir(RectTransformUtility.WorldToScreenPoint(cam, cantos[i]), ref min, ref max);
            }
        }
        else if (p.mundo != null && camera2D != null)
        {
            SpriteRenderer sr = p.mundo.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Bounds b = sr.bounds;
                Incluir(camera2D.WorldToScreenPoint(b.min), ref min, ref max);
                Incluir(camera2D.WorldToScreenPoint(b.max), ref min, ref max);
            }
            else
            {
                Incluir(camera2D.WorldToScreenPoint(p.mundo.position), ref min, ref max);
            }
        }
        else
        {
            Incluir(new Vector2(Screen.width / 2f, Screen.height / 2f), ref min, ref max);
        }

        Vector2 meio = (min + max) / 2f;
        centro = new Vector2(meio.x, Screen.height - meio.y);
        raio = Mathf.Clamp((max - min).magnitude / 2f + folga, raioMinimo, raioMaximo);
    }

    static void Incluir(Vector2 ponto, ref Vector2 min, ref Vector2 max)
    {
        min = Vector2.Min(min, ponto);
        max = Vector2.Max(max, ponto);
    }

    void OnGUI()
    {
        if (!visivel) return;
        if (Event.current.type != EventType.Repaint) return;

        GUI.depth = -900;

        MascaraCirculo.Desenhar(centroAtual, raioAtual, escuridao);

        if (raioAtual < Diagonal() * 0.6f)
        {
            Color anel = corAnel;
            anel.a *= escuridao / Mathf.Max(0.01f, escuridaoMaxima);
            MascaraCirculo.Anel(centroAtual, raioAtual, anel);
        }

        DesenharCaixa();
    }

    void DesenharCaixa()
    {
        if (alphaCaixa <= 0f) return;

        if (estTexto == null)
        {
            estTexto = new GUIStyle(GUI.skin.label);
            estTexto.fontStyle = FontStyle.Bold;
            estTexto.wordWrap = true;
            estTexto.alignment = TextAnchor.MiddleCenter;
            estTexto.normal.textColor = Color.white;

            estDica = new GUIStyle(GUI.skin.label);
            estDica.fontStyle = FontStyle.Normal;
            estDica.alignment = TextAnchor.MiddleCenter;
            estDica.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
        }

        float esc = Screen.height / 720f;
        estTexto.fontSize = Mathf.RoundToInt(22f * esc);
        estDica.fontSize = Mathf.RoundToInt(15f * esc);

        float largura = 580f * esc;
        float altura = 140f * esc;
        float margem = 24f * esc;

        float y;
        if (centroAtual.y > Screen.height / 2f)
        {
            y = centroAtual.y - raioAtual - margem - altura;
        }
        else
        {
            y = centroAtual.y + raioAtual + margem;
        }

        y = Mathf.Clamp(y, margem, Screen.height - altura - margem);
        float x = Mathf.Clamp(centroAtual.x - largura / 2f, margem, Screen.width - largura - margem);

        Rect caixa = new Rect(x, y, largura, altura);
        float borda = 3f * esc;

        MascaraCirculo.Retangulo(new Rect(caixa.x - borda, caixa.y - borda, caixa.width + borda * 2f, caixa.height + borda * 2f),
            new Color(corAnel.r, corAnel.g, corAnel.b, alphaCaixa));
        MascaraCirculo.Retangulo(caixa, new Color(0f, 0f, 0f, alphaCaixa));

        Color antes = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alphaCaixa);

        GUI.Label(new Rect(caixa.x + 16f * esc, caixa.y + 10f * esc, caixa.width - 32f * esc, caixa.height - 44f * esc), mensagem, estTexto);
        GUI.Label(new Rect(caixa.x, caixa.yMax - 32f * esc, caixa.width, 26f * esc),
            numeroPasso + "/" + totalPassos + "   |   clique para continuar   |   Esc pula", estDica);

        GUI.color = antes;
    }
}
