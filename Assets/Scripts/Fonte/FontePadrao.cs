using UnityEngine;

// Fonte padrao para textos desenhados com OnGUI/GUIStyle (que nao usam TextMeshPro).
// Coloque em Assets/Scripts/Gameplay/. A fonte e carregada de Assets/Resources/Fonts/IrishGrover-Regular
// (a ferramenta Ferramentas > Aplicar Irish Grover em tudo move o .ttf para la).
//
// Uso:  FontePadrao.Aplicar(meuEstilo);
public static class FontePadrao
{
    const string CAMINHO = "Fonts/IrishGrover-Regular";

    static Font fonte;
    static bool avisou;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        fonte = null;
        avisou = false;
    }

    public static Font Fonte
    {
        get
        {
            if (fonte == null)
            {
                fonte = Resources.Load<Font>(CAMINHO);

                if (fonte == null && !avisou)
                {
                    avisou = true;
                    Debug.LogWarning("[Fonte] Nao achei Resources/" + CAMINHO + ". "
                        + "Rode Ferramentas > Aplicar Irish Grover em tudo (ou mova o .ttf para Assets/Resources/Fonts).");
                }
            }

            return fonte;
        }
    }

    public static GUIStyle Aplicar(GUIStyle estilo)
    {
        Font f = Fonte;
        if (estilo != null && f != null) estilo.font = f;
        return estilo;
    }
}