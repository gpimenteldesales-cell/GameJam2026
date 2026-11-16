using System;
using UnityEngine;

public enum AcaoTecla
{
    EquilibrioA = 0,
    EquilibrioB = 1,
    AbrirLivro = 2,
    Arremessar = 3
}

public static class ConfiguracoesJogo
{
    const string CHAVE_VOLUME = "cfg_volume";
    const string CHAVE_TECLA = "cfg_tecla_";

    static readonly KeyCode[] padroes = { KeyCode.E, KeyCode.Q, KeyCode.B, KeyCode.F };

    static readonly string[] nomesAcoes =
    {
        "Equilibrar barco (1)",
        "Equilibrar barco (2)",
        "Abrir Guia do Pescador",
        "Arremessar ferro"
    };

    static readonly KeyCode[] reservadas =
    {
        KeyCode.None,
        KeyCode.Escape,
        KeyCode.R,
        KeyCode.T,
        KeyCode.Space,
        KeyCode.Return,
        KeyCode.KeypadEnter
    };

    static readonly KeyCode[] teclas = new KeyCode[4];

    static float volume = 1f;
    static bool carregado;

    public static event Action aoMudar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        aoMudar = null;
        carregado = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        Carregar();
    }

    static void Carregar()
    {
        if (carregado) return;
        carregado = true;

        volume = Mathf.Clamp01(PlayerPrefs.GetFloat(CHAVE_VOLUME, 1f));

        for (int i = 0; i < teclas.Length; i++)
        {
            int valor = PlayerPrefs.GetInt(CHAVE_TECLA + i, (int)padroes[i]);
            KeyCode tecla = (KeyCode)valor;

            if (Enum.IsDefined(typeof(KeyCode), valor) && TeclaPermitida(tecla))
            {
                teclas[i] = tecla;
            }
            else
            {
                teclas[i] = padroes[i];
            }
        }

        AudioListener.volume = volume;
    }

    public static float Volume
    {
        get
        {
            Carregar();
            return volume;
        }
    }

    public static void DefinirVolume(float novoVolume)
    {
        Carregar();
        volume = Mathf.Clamp01(novoVolume);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(CHAVE_VOLUME, volume);
        Avisar();
    }

    public static KeyCode Tecla(AcaoTecla acao)
    {
        Carregar();
        return teclas[(int)acao];
    }

    public static string NomeAcao(AcaoTecla acao)
    {
        return nomesAcoes[(int)acao];
    }

    public static bool TeclaPermitida(KeyCode tecla)
    {
        if ((int)tecla >= (int)KeyCode.Mouse0) return false;
        return Array.IndexOf(reservadas, tecla) < 0;
    }

    public static bool DefinirTecla(AcaoTecla acao, KeyCode tecla)
    {
        Carregar();

        if (!TeclaPermitida(tecla)) return false;

        int indice = (int)acao;

        for (int i = 0; i < teclas.Length; i++)
        {
            if (i != indice && teclas[i] == tecla)
            {
                teclas[i] = teclas[indice];
                PlayerPrefs.SetInt(CHAVE_TECLA + i, (int)teclas[i]);
            }
        }

        teclas[indice] = tecla;
        PlayerPrefs.SetInt(CHAVE_TECLA + indice, (int)tecla);
        Avisar();
        return true;
    }

    public static void RestaurarAtalhos()
    {
        Carregar();

        for (int i = 0; i < teclas.Length; i++)
        {
            teclas[i] = padroes[i];
            PlayerPrefs.SetInt(CHAVE_TECLA + i, (int)padroes[i]);
        }

        Avisar();
    }

    public static void Salvar()
    {
        PlayerPrefs.Save();
    }

    public static string NomeTecla(KeyCode tecla)
    {
        switch (tecla)
        {
            case KeyCode.LeftShift: return "Shift esq.";
            case KeyCode.RightShift: return "Shift dir.";
            case KeyCode.LeftControl: return "Ctrl esq.";
            case KeyCode.RightControl: return "Ctrl dir.";
            case KeyCode.LeftAlt: return "Alt esq.";
            case KeyCode.RightAlt: return "Alt dir.";
            case KeyCode.UpArrow: return "Seta cima";
            case KeyCode.DownArrow: return "Seta baixo";
            case KeyCode.LeftArrow: return "Seta esq.";
            case KeyCode.RightArrow: return "Seta dir.";
            case KeyCode.Backspace: return "Apagar";
            case KeyCode.Tab: return "Tab";
        }

        string nome = tecla.ToString();

        if (nome.StartsWith("Alpha")) return nome.Substring(5);
        if (nome.StartsWith("Keypad")) return "Num " + nome.Substring(6);

        return nome;
    }

    public static string SubstituirTeclas(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;

        texto = texto.Replace("{equilibrar1}", NomeTecla(Tecla(AcaoTecla.EquilibrioA)));
        texto = texto.Replace("{equilibrar2}", NomeTecla(Tecla(AcaoTecla.EquilibrioB)));
        texto = texto.Replace("{livro}", NomeTecla(Tecla(AcaoTecla.AbrirLivro)));
        texto = texto.Replace("{arremessar}", NomeTecla(Tecla(AcaoTecla.Arremessar)));

        return texto;
    }

    static void Avisar()
    {
        if (aoMudar != null) aoMudar();
    }
}
