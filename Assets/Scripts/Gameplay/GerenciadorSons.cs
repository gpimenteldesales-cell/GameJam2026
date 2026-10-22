using System.Collections.Generic;
using UnityEngine;

// Toca sons 2D (sem atenuacao por distancia) de qualquer lugar do jogo.
// Nao precisa colocar na cena: ele se cria sozinho na primeira vez que for usado.
// Como o som sai daqui, ele NAO e cortado quando o objeto que pediu (ex: bruxa que morreu) some.
// Se algum som nao tocar, olhe o Console: ele avisa o motivo.
public class GerenciadorSons : MonoBehaviour
{
    static GerenciadorSons _instancia;
    static readonly HashSet<string> avisados = new HashSet<string>();

    AudioSource fonte;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        _instancia = null;
        avisados.Clear();
    }

    static GerenciadorSons Instancia
    {
        get
        {
            if (_instancia == null)
            {
                GameObject obj = new GameObject("GerenciadorSons");
                _instancia = obj.AddComponent<GerenciadorSons>();
            }

            return _instancia;
        }
    }

    void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        _instancia = this;

        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;

        VerificarAmbiente();
    }

    static void VerificarAmbiente()
    {
#if UNITY_2023_1_OR_NEWER
        bool temOuvinte = FindAnyObjectByType<AudioListener>() != null;
#else
        bool temOuvinte = FindObjectOfType<AudioListener>() != null;
#endif

        if (!temOuvinte)
        {
            Debug.LogWarning("[Som] Nao existe AudioListener na cena. Coloque um na Main Camera (Add Component > Audio Listener), senao nada toca.");
        }

        if (AudioListener.volume <= 0f || AudioListener.pause)
        {
            Debug.LogWarning("[Som] O audio esta mudo/pausado (AudioListener.volume = " + AudioListener.volume + ").");
        }
    }

    // 'nome' so serve para o aviso no Console quando o AudioClip nao foi arrastado.
    public static void Tocar(AudioClip clip, float volume = 1f, string nome = null)
    {
        if (!Application.isPlaying) return;

        if (clip == null)
        {
            if (nome != null && avisados.Add(nome))
            {
                Debug.LogWarning("[Som] \"" + nome + "\" nao tocou: nenhum AudioClip arrastado nesse campo. "
                    + "Se a bruxa nasce pelo GerenciadorBruxas, arraste no PREFAB dela (nao so na cena).");
            }

            return;
        }

        GerenciadorSons g = Instancia;
        if (g.fonte == null) return;

        if (volume <= 0f && avisados.Add("volume0:" + clip.name))
        {
            Debug.LogWarning("[Som] \"" + clip.name + "\" esta com volume 0 no Inspector.");
        }

        g.fonte.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
