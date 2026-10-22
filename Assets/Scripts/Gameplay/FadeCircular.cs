using UnityEngine;

public class FadeCircular : MonoBehaviour
{
    public static FadeCircular instancia;
    public static bool concluido;

    [Header("Fade")]
    public Transform alvo;
    public float atraso = 0.2f;
    public float duracao = 1.3f;

    float tempo;
    float raio;
    Camera camera2D;

    void Awake()
    {
        instancia = this;
        concluido = false;
    }

    void Start()
    {
        camera2D = Camera.main;
    }

    void Update()
    {
        if (concluido) return;

        tempo += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

        float k = Mathf.Clamp01((tempo - atraso) / Mathf.Max(0.01f, duracao));
        float suave = 1f - Mathf.Pow(1f - k, 3f);
        raio = suave * RaioMaximo(Centro());

        if (k >= 1f)
        {
            concluido = true;
        }
    }

    void OnGUI()
    {
        if (concluido) return;
        if (Event.current.type != EventType.Repaint) return;

        GUI.depth = -1000;
        MascaraCirculo.Desenhar(Centro(), raio, 1f);
    }

    Vector2 Centro()
    {
        if (alvo != null && camera2D != null)
        {
            Vector3 s = camera2D.WorldToScreenPoint(alvo.position);
            return new Vector2(s.x, Screen.height - s.y);
        }

        return new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    static float RaioMaximo(Vector2 centro)
    {
        float w = Screen.width;
        float h = Screen.height;

        float a = Vector2.Distance(centro, new Vector2(0f, 0f));
        float b = Vector2.Distance(centro, new Vector2(w, 0f));
        float c = Vector2.Distance(centro, new Vector2(0f, h));
        float d = Vector2.Distance(centro, new Vector2(w, h));

        return Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d)) + 4f;
    }
}
