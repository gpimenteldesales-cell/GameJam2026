using UnityEngine;

public class PeixePescadoVoando : MonoBehaviour
{
    Transform destino;
    Vector3 inicio;
    Vector3 escalaInicial;
    SpriteRenderer sr;
    Color corInicial;
    float duracao = 0.6f;
    float giros = 2f;
    float alturaArco = 0.6f;
    float tempo;

    public static PeixePescadoVoando Criar(Peixe peixe, Transform destino)
    {
        SpriteRenderer origem = peixe.GetComponent<SpriteRenderer>();
        if (origem == null || destino == null) return null;

        GameObject obj = new GameObject("PeixePescado");
        obj.transform.position = peixe.transform.position;
        obj.transform.localScale = peixe.transform.lossyScale;

        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = origem.sprite;
        render.color = origem.color;
        render.flipX = origem.flipX;
        render.flipY = origem.flipY;
        render.sortingLayerID = origem.sortingLayerID;
        render.sortingOrder = 40;

        PeixePescadoVoando voador = obj.AddComponent<PeixePescadoVoando>();
        voador.destino = destino;
        voador.inicio = obj.transform.position;
        voador.escalaInicial = obj.transform.localScale;
        voador.sr = render;
        voador.corInicial = render.color;
        return voador;
    }

    void Update()
    {
        if (destino == null)
        {
            Destroy(gameObject);
            return;
        }

        tempo += Time.deltaTime;
        float k = Mathf.Clamp01(tempo / duracao);
        float entrada = k * k;

        Vector3 posicao = Vector3.Lerp(inicio, destino.position, entrada);
        posicao.y += Mathf.Sin(k * Mathf.PI) * alturaArco;
        transform.position = posicao;

        transform.rotation = Quaternion.Euler(0f, 0f, 360f * giros * k);
        transform.localScale = Vector3.Lerp(escalaInicial, escalaInicial * 0.35f, k);

        Color cor = corInicial;
        cor.a = 1f - Mathf.Clamp01((k - 0.8f) / 0.2f);
        sr.color = cor;

        if (k >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
