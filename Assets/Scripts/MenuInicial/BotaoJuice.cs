using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BotaoJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Hover (mouse em cima)")]
    public float escalaHover = 1.1f;
    public float anguloHover = 4f;

    [Header("Clique")]
    public float escalaClique = 0.9f;
    public float impulsoClique = 4f;

    [Header("Parado")]
    public float respiracao = 0.015f;
    public float velocidadeRespiracao = 2.2f;

    [Header("Mola")]
    public float rigidez = 220f;
    public float amortecimento = 12f;

    [Header("Som (opcional)")]

    public AudioSource audioFonte;
    public AudioClip somHover;
    public AudioClip somClique;

    [HideInInspector] public float escalaExterna = 1f;
    [HideInInspector] public float anguloExterno = 0f;

    Selectable selecionavel;
    Vector3 escalaBase;
    float anguloBase;

    float escala = 1f, velEscala;
    float angulo, velAngulo;
    float lado = 1f;
    float fase;
    bool hover, pressionado;

    void Awake()
    {
        selecionavel = GetComponent<Selectable>();
        escalaBase = transform.localScale;
        anguloBase = transform.localEulerAngles.z;
        fase = Random.value * 10f;
    }

    void OnEnable()
    {
        hover = false;
        pressionado = false;
        escala = 1f;
        velEscala = 0f;
        angulo = 0f;
        velAngulo = 0f;
    }

    bool Ativo()
    {
        return selecionavel == null || selecionavel.interactable;
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);

        float alvoEscala = 1f;
        float alvoAngulo = 0f;

        if (Ativo())
        {
            if (pressionado)
            {
                alvoEscala = escalaClique;
            }
            else if (hover)
            {
                alvoEscala = escalaHover;
                alvoAngulo = anguloHover * lado;
            }
            else
            {
                alvoEscala = 1f + Mathf.Sin(Time.unscaledTime * velocidadeRespiracao + fase) * respiracao;
            }
        }

        velEscala += (alvoEscala - escala) * rigidez * dt;
        velEscala *= Mathf.Exp(-amortecimento * dt);
        escala += velEscala * dt;

        velAngulo += (alvoAngulo - angulo) * rigidez * dt;
        velAngulo *= Mathf.Exp(-amortecimento * dt);
        angulo += velAngulo * dt;

        transform.localScale = escalaBase * (escala * escalaExterna);
        transform.localRotation = Quaternion.Euler(0f, 0f, anguloBase + angulo + anguloExterno);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!Ativo()) return;

        hover = true;
        lado = (Random.value < 0.5f) ? -1f : 1f;
        velEscala += 2f;
        Tocar(somHover);
    }

    public void OnPointerExit(PointerEventData e)
    {
        hover = false;
        pressionado = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!Ativo()) return;
        pressionado = true;
    }

    public void OnPointerUp(PointerEventData e)
    {
        pressionado = false;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!Ativo()) return;

        velEscala += impulsoClique;
        velAngulo += impulsoClique * 20f * lado;
        Tocar(somClique);
    }

    void Tocar(AudioClip clip)
    {
        if (clip == null) return;

        if (audioFonte != null)
        {
            audioFonte.PlayOneShot(clip);
        }
        else
        {
            Vector3 pos = (Camera.main != null) ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }
}