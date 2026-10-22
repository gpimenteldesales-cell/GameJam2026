using System.Collections;
using UnityEngine;
using TMPro;

public class PopupPontos : MonoBehaviour
{
    const float duracao = 1f;
    const float queda = 70f;
    const float deslocamentoInicial = 25f;
    static readonly Color cor = new Color(1f, 0.9f, 0.3f, 1f);

    public static void Mostrar(TMP_Text modelo, int valor)
    {
        if (modelo == null) return;

        TMP_Text copia = Instantiate(modelo, modelo.transform.parent);
        copia.gameObject.name = "PopupPontos";
        copia.text = "+" + valor + " pontos";
        copia.color = cor;
        copia.raycastTarget = false;
        copia.transform.SetAsLastSibling();

        PopupPontos popup = copia.gameObject.AddComponent<PopupPontos>();
        popup.StartCoroutine(popup.Animar(copia));
    }

    IEnumerator Animar(TMP_Text texto)
    {
        RectTransform rt = texto.rectTransform;
        Vector2 inicio = rt.anchoredPosition + Vector2.down * deslocamentoInicial;
        Vector3 escalaOriginal = rt.localScale;
        float t = 0f;

        while (t < duracao)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float k = Mathf.Clamp01(t / duracao);
            float suave = 1f - (1f - k) * (1f - k);

            rt.anchoredPosition = inicio + Vector2.down * queda * suave;
            rt.localScale = escalaOriginal * Mathf.Lerp(1.4f, 1f, Mathf.Clamp01(k * 5f));

            Color c = cor;
            c.a = 1f - Mathf.Clamp01((k - 0.4f) / 0.6f);
            texto.color = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}
