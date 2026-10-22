using System.Collections;
using UnityEngine;

public class PainelPop : MonoBehaviour
{
    public float duracao = 0.3f;
    [Range(0.1f, 1f)] public float escalaInicial = 0.7f;

    CanvasGroup grupo;
    Vector3 escalaBase;

    void Awake()
    {
        escalaBase = transform.localScale;

        grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(Animar());
    }

    IEnumerator Animar()
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duracao);
            float k = Mathf.Clamp01(t);

            transform.localScale = escalaBase * Mathf.LerpUnclamped(escalaInicial, 1f, EntradaUI.OutBack(k));
            grupo.alpha = Mathf.Clamp01(k * 2f);
            yield return null;
        }

        transform.localScale = escalaBase;
        grupo.alpha = 1f;
    }
}