using System;
using UnityEngine;

// Coloque este script em qualquer objeto da cena (ex: um objeto vazio "Ambiente").
//  - Loops: sons que ficam tocando o tempo todo (ondas, vento...).
//  - Ocasionais: sons que tocam de vez em quando, sorteados (gaivota, sino de boia, trovao...).
// Quando o jogo acaba (derrota), o ambiente abaixa de volume.
public class SonsAmbiente : MonoBehaviour
{
    [Serializable]
    public class Camada
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.5f;
    }

    [Serializable]
    public class Ocasional
    {
        public string nome = "Som ocasional";
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.6f;
        public float intervaloMin = 8f;
        public float intervaloMax = 20f;
        [HideInInspector] public float proximo;
    }

    [Header("Sons continuos (loop)")]
    public Camada[] loops;

    [Header("Sons de vez em quando")]
    public Ocasional[] ocasionais;

    [Header("Geral")]
    public float duracaoFadeIn = 2f;
    [Range(0f, 1f)] public float volumeAposDerrota = 0.3f;
    public float duracaoFadeDerrota = 1.5f;

    AudioSource[] fontes;
    float mestre;

    void Start()
    {
        if (loops == null) loops = new Camada[0];
        if (ocasionais == null) ocasionais = new Ocasional[0];

        fontes = new AudioSource[loops.Length];

        for (int i = 0; i < loops.Length; i++)
        {
            if (loops[i] == null || loops[i].clip == null) continue;

            AudioSource fonte = gameObject.AddComponent<AudioSource>();
            fonte.clip = loops[i].clip;
            fonte.loop = true;
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
            fonte.volume = 0f;
            fonte.time = UnityEngine.Random.value * loops[i].clip.length;
            fonte.Play();
            fontes[i] = fonte;
        }

        for (int i = 0; i < ocasionais.Length; i++)
        {
            SortearProximo(ocasionais[i]);
        }

        bool temAlgumLoop = false;
        for (int i = 0; i < fontes.Length; i++)
        {
            if (fontes[i] != null) temAlgumLoop = true;
        }

        if (!temAlgumLoop && ocasionais.Length == 0)
        {
            Debug.LogWarning("[Som] SonsAmbiente esta sem nenhum som: arraste AudioClips em 'Loops' ou 'Ocasionais'.", this);
        }
    }

    void Update()
    {
        bool fimDeJogo = GerenciadorJogo.instancia != null && !GerenciadorJogo.instancia.jogando;

        float alvo = fimDeJogo ? volumeAposDerrota : 1f;
        float duracao = fimDeJogo ? duracaoFadeDerrota : duracaoFadeIn;
        mestre = Mathf.MoveTowards(mestre, alvo, Time.unscaledDeltaTime / Mathf.Max(0.01f, duracao));

        for (int i = 0; i < fontes.Length; i++)
        {
            if (fontes[i] != null)
            {
                fontes[i].volume = loops[i].volume * mestre;
            }
        }

        if (fimDeJogo) return;

        for (int i = 0; i < ocasionais.Length; i++)
        {
            Ocasional o = ocasionais[i];
            if (o == null || Time.time < o.proximo) continue;

            SortearProximo(o);

            if (o.clips == null || o.clips.Length == 0) continue;

            AudioClip clip = o.clips[UnityEngine.Random.Range(0, o.clips.Length)];
            GerenciadorSons.Tocar(clip, o.volume * mestre);
        }
    }

    void SortearProximo(Ocasional o)
    {
        if (o == null) return;
        o.proximo = Time.time + UnityEngine.Random.Range(o.intervaloMin, Mathf.Max(o.intervaloMin, o.intervaloMax));
    }
}
