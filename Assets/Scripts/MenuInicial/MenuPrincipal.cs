using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Cena")]
    public string nomeCenaJogo = "Jogo";

    [Header("Telas")]
    public GameObject raizBotoes;
    public GameObject painelTutorial;
    public GameObject painelOpcoes;
    public bool esconderBotoesComPainel = true;
    public GameObject botaoSair;

    [Header("Fade preto")]
    public CanvasGroup fadePreto;
    public float duracaoFadeEntrada = 0.8f;
    public float duracaoFadeSaida = 0.5f;

    [Header("Som (opcional)")]
    public AudioSource audioFonte;
    public AudioClip somJogar;
    public AudioClip somAbrirPainel;
    public AudioClip somFecharPainel;

    GameObject painelAberto;
    bool saindo;

    void Start()
    {
        Time.timeScale = 1f;

        if (painelTutorial != null) painelTutorial.SetActive(false);
        if (painelOpcoes != null) painelOpcoes.SetActive(false);

        if (botaoSair != null && Application.platform == RuntimePlatform.WebGLPlayer)
        {
            botaoSair.SetActive(false);
        }

        if (fadePreto != null)
        {
            fadePreto.alpha = 1f;
            StartCoroutine(FadeDeEntrada());
        }
    }

    void Update()
    {
        if (painelAberto != null && Input.GetKeyDown(KeyCode.Escape))
        {
            FecharPainel();
        }
    }

    public void Jogar()
    {
        if (saindo) return;

        if (!Application.CanStreamedLevelBeLoaded(nomeCenaJogo))
        {
            Debug.LogError("MenuPrincipal: a cena '" + nomeCenaJogo + "' nao esta no Build Settings (ou o nome esta errado)!");
            return;
        }

        saindo = true;
        Tocar(somJogar);
        StartCoroutine(Fade(0f, 1f, duracaoFadeSaida, () => SceneManager.LoadScene(nomeCenaJogo)));
    }

    public void AbrirTutorial()
    {
        Abrir(painelTutorial);
    }

    public void AbrirOpcoes()
    {
        Abrir(painelOpcoes);
    }

    public void FecharPainel()
    {
        if (painelAberto == null) return;

        painelAberto.SetActive(false);
        painelAberto = null;
        Tocar(somFecharPainel);

        if (raizBotoes != null) raizBotoes.SetActive(true);
    }

    public void Sair()
    {
        if (saindo) return;

        saindo = true;
        Tocar(somFecharPainel);
        StartCoroutine(Fade(0f, 1f, duracaoFadeSaida, Encerrar));
    }

    void Abrir(GameObject painel)
    {
        if (saindo || painel == null) return;

        if (painelAberto != null) painelAberto.SetActive(false);

        painelAberto = painel;
        painel.SetActive(true);
        Tocar(somAbrirPainel);

        if (esconderBotoesComPainel && raizBotoes != null) raizBotoes.SetActive(false);
    }

    static void Encerrar()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator FadeDeEntrada()
    {
        yield return null;
        yield return null;
        yield return Fade(1f, 0f, duracaoFadeEntrada, null);
    }

    IEnumerator Fade(float de, float para, float duracao, Action aoTerminar)
    {
        if (fadePreto == null)
        {
            if (aoTerminar != null) aoTerminar();
            yield break;
        }

        fadePreto.blocksRaycasts = true;

        float t = 0f;
        while (t < 1f)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt / Mathf.Max(0.01f, duracao);
            fadePreto.alpha = Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }

        fadePreto.alpha = para;
        fadePreto.blocksRaycasts = para > 0.01f;

        if (aoTerminar != null) aoTerminar();
    }

    void Tocar(AudioClip clip)
    {
        if (audioFonte != null && clip != null)
        {
            audioFonte.PlayOneShot(clip);
        }
    }
}