using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpcoesMenu : MonoBehaviour
{
    [Serializable]
    public class LinhaAtalho
    {
        [Tooltip("Qual ação do jogo esta linha controla.")]
        public AcaoTecla acao;

        [Tooltip("Botão que o jogador clica para trocar a tecla.")]
        public Button botao;

        [Tooltip("Texto dentro do botão que mostra a tecla atual.")]
        public TMP_Text textoTecla;

        [Tooltip("Texto com o nome da ação (opcional, é preenchido sozinho).")]
        public TMP_Text textoNome;
    }

    [Header("Volume")]
    [Tooltip("Slider do volume geral (vai de 0 a 1 sozinho).")]
    public Slider sliderVolume;

    [Tooltip("Texto que mostra a porcentagem do volume (opcional).")]
    public TMP_Text textoVolume;

    [Header("Atalhos")]
    [Tooltip("Uma linha para cada ação que pode ter a tecla trocada.")]
    public List<LinhaAtalho> linhasAtalho = new List<LinhaAtalho>();

    [Tooltip("Texto de aviso mostrado enquanto espera a nova tecla (opcional).")]
    public TMP_Text textoAviso;

    [Tooltip("Botão que volta os atalhos para E, Q, B e F.")]
    public Button botaoRestaurar;

    static bool escutando;
    static int quadroCancelamento = -1;

    LinhaAtalho linhaEsperando;
    KeyCode[] todasTeclas;
    bool atualizando;

    public static bool BloqueiaEscape
    {
        get { return escutando || quadroCancelamento == Time.frameCount; }
    }

    void Awake()
    {
        todasTeclas = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        if (sliderVolume != null)
        {
            sliderVolume.minValue = 0f;
            sliderVolume.maxValue = 1f;
            sliderVolume.wholeNumbers = false;
            sliderVolume.onValueChanged.AddListener(AoMudarVolume);
        }

        for (int i = 0; i < linhasAtalho.Count; i++)
        {
            LinhaAtalho linha = linhasAtalho[i];

            if (linha == null || linha.botao == null) continue;

            linha.botao.onClick.AddListener(() => ComecarEscuta(linha));
        }

        if (botaoRestaurar != null) botaoRestaurar.onClick.AddListener(RestaurarAtalhos);
    }

    void OnEnable()
    {
        linhaEsperando = null;
        escutando = false;
        AtualizarTudo();
        MostrarAviso("");
    }

    void OnDisable()
    {
        linhaEsperando = null;
        escutando = false;
        ConfiguracoesJogo.Salvar();
    }

    void Update()
    {
        if (linhaEsperando == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelarEscuta();
            MostrarAviso("");
            return;
        }

        for (int i = 0; i < todasTeclas.Length; i++)
        {
            KeyCode tecla = todasTeclas[i];

            if ((int)tecla >= (int)KeyCode.Mouse0) continue;
            if (!Input.GetKeyDown(tecla)) continue;

            if (!ConfiguracoesJogo.TeclaPermitida(tecla))
            {
                MostrarAviso("Essa tecla e reservada. Escolha outra.");
                return;
            }

            ConfiguracoesJogo.DefinirTecla(linhaEsperando.acao, tecla);
            ConfiguracoesJogo.Salvar();
            CancelarEscuta();
            MostrarAviso("");
            return;
        }
    }

    void ComecarEscuta(LinhaAtalho linha)
    {
        linhaEsperando = linha;
        escutando = true;
        AtualizarTextosAtalhos();
        MostrarAviso("Aperte a nova tecla (Esc cancela)");
    }

    void CancelarEscuta()
    {
        linhaEsperando = null;
        escutando = false;
        quadroCancelamento = Time.frameCount;
        AtualizarTextosAtalhos();
    }

    void RestaurarAtalhos()
    {
        CancelarEscuta();
        ConfiguracoesJogo.RestaurarAtalhos();
        ConfiguracoesJogo.Salvar();
        AtualizarTextosAtalhos();
        MostrarAviso("");
    }

    void AoMudarVolume(float valor)
    {
        if (atualizando) return;

        ConfiguracoesJogo.DefinirVolume(valor);
        AtualizarTextoVolume(valor);
    }

    void AtualizarTudo()
    {
        atualizando = true;

        if (sliderVolume != null) sliderVolume.SetValueWithoutNotify(ConfiguracoesJogo.Volume);

        atualizando = false;

        AtualizarTextoVolume(ConfiguracoesJogo.Volume);
        AtualizarTextosAtalhos();
    }

    void AtualizarTextoVolume(float valor)
    {
        if (textoVolume != null) textoVolume.text = Mathf.RoundToInt(valor * 100f) + "%";
    }

    void AtualizarTextosAtalhos()
    {
        for (int i = 0; i < linhasAtalho.Count; i++)
        {
            LinhaAtalho linha = linhasAtalho[i];

            if (linha == null) continue;

            if (linha.textoNome != null)
            {
                linha.textoNome.text = ConfiguracoesJogo.NomeAcao(linha.acao);
            }

            if (linha.textoTecla != null)
            {
                if (linha == linhaEsperando)
                {
                    linha.textoTecla.text = "...";
                }
                else
                {
                    linha.textoTecla.text = ConfiguracoesJogo.NomeTecla(ConfiguracoesJogo.Tecla(linha.acao));
                }
            }
        }
    }

    void MostrarAviso(string mensagem)
    {
        if (textoAviso != null) textoAviso.text = mensagem;
    }
}
