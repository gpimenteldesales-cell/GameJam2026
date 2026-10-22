using UnityEngine;

// Coloque num objeto vazio da cena, de Play e leia o Console (procure "DIAGNOSTICO").
// Mostra o que esta ligado e o que falta. Tambem testa o audio: arraste qualquer AudioClip em
// "Clip De Teste" e aperte T durante o jogo.
public class DiagnosticoJogo : MonoBehaviour
{
    public AudioClip clipDeTeste;
    public KeyCode teclaTeste = KeyCode.T;

    void Start()
    {
        Invoke(nameof(Verificar), 0.7f);
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaTeste))
        {
            Debug.Log("[DIAGNOSTICO] Tocando o clip de teste...");
            GerenciadorSons.Tocar(clipDeTeste, 1f, "Diagnostico: Clip De Teste");
        }
    }

    static T Achar<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindAnyObjectByType<T>();
#else
        return FindObjectOfType<T>();
#endif
    }

    static int Contar<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<T>(FindObjectsSortMode.None).Length;
#else
        return FindObjectsOfType<T>().Length;
#endif
    }

    static string Linha(string texto, bool ok)
    {
        return (ok ? "  [OK]    " : "  [FALTA] ") + texto + "\n";
    }

    void Verificar()
    {
        string r = "[DIAGNOSTICO] ================\n";

        r += "AUDIO\n";
        r += Linha("AudioListener na cena (Main Camera)", Achar<AudioListener>() != null);
        r += Linha("Audio nao esta mudo/pausado", AudioListener.volume > 0f && !AudioListener.pause);

        VozPescador voz = VozPescador.instancia;
        r += Linha("VozPescador em objeto ativo da cena", voz != null);
        if (voz != null)
        {
            r += Linha("VozPescador com falas (" + (voz.falasAcerto == null ? 0 : voz.falasAcerto.Length) + ")",
                voz.falasAcerto != null && voz.falasAcerto.Length > 0);
        }

        r += Linha("SonsAmbiente na cena", Achar<SonsAmbiente>() != null);

        r += "BRUXA\n";
        GerenciadorBruxas gb = Achar<GerenciadorBruxas>();
        r += Linha("GerenciadorBruxas na cena", gb != null);
        if (gb != null)
        {
            r += Linha("Prefab Bruxa ligado no GerenciadorBruxas", gb.prefabBruxa != null);

            if (gb.prefabBruxa != null)
            {
                BruxaVoadora b = gb.prefabBruxa.GetComponent<BruxaVoadora>();
                r += Linha("Prefab tem o script BruxaVoadora", b != null);

                if (b != null)
                {
                    r += Linha("Prefab: Som Acertada", b.somAcertada != null);
                    r += Linha("Prefab: Som Riso Aparecer", b.somRisoAparecer != null);
                    r += Linha("Prefab: Som Riso Ataque", b.somRisoAtaque != null);
                }
            }
        }

        r += "LINHA DE PESCA\n";
        RaioDePesca raio = Achar<RaioDePesca>();
        r += Linha("RaioDePesca na cena", raio != null);
        if (raio != null)
        {
            r += Linha("Ponta Da Vara arrastada", raio.pontaDaVara != null);
            r += Linha("Sprite Vara arrastado (opcional)", raio.spriteVara != null);
        }

        r += "FUNDO / DERROTA\n";
        r += Linha("Camadas com Paralaxe: " + Contar<Paralaxe>(), Contar<Paralaxe>() > 0);

        GerenciadorJogo gj = Achar<GerenciadorJogo>();
        r += Linha("GerenciadorJogo na cena", gj != null);
        if (gj != null)
        {
            r += "  [info]  Painel Derrota " + (gj.painelDerrota != null ? "arrastado" : "vazio (sera criado sozinho)") + "\n";
        }

        Debug.Log(r);
    }
}
