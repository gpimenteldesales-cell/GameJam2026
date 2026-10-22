using UnityEngine;

public class Fish : MonoBehaviour
{
    public int valor = 10;
    [Range(0f, 1f)] public float profundidade = 0.5f; // 0 = perto, 1 = longe
    public float velocidadeBase = 3f;
    public float limiteX = 8f;   // quando passa disso, volta pelo outro lado

    float direcao = 1f;

    void Start()
    {
        direcao = Random.value > 0.5f ? 1f : -1f;

        // parallax: longe = menor, mais lento, atrás
        float escala = Mathf.Lerp(1f, 0.4f, profundidade);
        transform.localScale = new Vector3(escala * direcao, escala, 1f);
        GetComponent<SpriteRenderer>().sortingOrder = Mathf.RoundToInt(-profundidade * 10);
    }

    void Update()
    {
        float vel = velocidadeBase * Mathf.Lerp(1f, 0.4f, profundidade);
        transform.Translate(Vector3.right * direcao * vel * Time.deltaTime * Mathf.Sign(transform.localScale.x) * Mathf.Sign(transform.localScale.x));

        if (Mathf.Abs(transform.position.x) > limiteX)
        {
            direcao *= -1f;
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(s.x) * direcao, s.y, 1f);
        }
    }
}