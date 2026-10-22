using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OpcoesMenu : MonoBehaviour
{
    [Header("UI")]
    public Slider sliderVolume;
    public Toggle toggleTelaCheia;
    public TMP_Text textoVolume;

    const string CHAVE_VOLUME = "opcoes_volume";
    const string CHAVE_TELA_CHEIA = "opcoes_tela_cheia";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AplicarSalvas()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(CHAVE_VOLUME, 1f);

        if (PlayerPrefs.HasKey(CHAVE_TELA_CHEIA))
        {
            Screen.fullScreen = PlayerPrefs.GetInt(CHAVE_TELA_CHEIA) == 1;
        }
    }

    void Awake()
    {
        if (sliderVolume != null)
        {
            sliderVolume.minValue = 0f;
            sliderVolume.maxValue = 1f;
            sliderVolume.onValueChanged.AddListener(MudarVolume);
        }

        if (toggleTelaCheia != null)
        {
            toggleTelaCheia.onValueChanged.AddListener(MudarTelaCheia);
        }
    }

    void OnEnable()
    {
        float volume = PlayerPrefs.GetFloat(CHAVE_VOLUME, 1f);

        if (sliderVolume != null) sliderVolume.SetValueWithoutNotify(volume);
        if (toggleTelaCheia != null) toggleTelaCheia.SetIsOnWithoutNotify(Screen.fullScreen);

        AtualizarTexto(volume);
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
    }

    void MudarVolume(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(CHAVE_VOLUME, valor);
        AtualizarTexto(valor);
    }

    void MudarTelaCheia(bool ligado)
    {
        Screen.fullScreen = ligado;
        PlayerPrefs.SetInt(CHAVE_TELA_CHEIA, ligado ? 1 : 0);
    }

    void AtualizarTexto(float volume)
    {
        if (textoVolume != null)
        {
            textoVolume.text = Mathf.RoundToInt(volume * 100f) + "%";
        }
    }
}