using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 2f;
    public bool toLeft = true;

    public static float multiplicadorGlobal = 1f;

    Vector3 posInicial;
    float larguraTile;
    float deslocamento;
    bool paraEsquerda;

    void Start()
    {
        posInicial = transform.position;

        var sr = GetComponent<SpriteRenderer>();

        larguraTile = sr.sprite.bounds.size.x * transform.localScale.x;

        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(sr.sprite.bounds.size.x * 3f, sr.sprite.bounds.size.y);
    }

    void Update()
    {
        deslocamento += velocidade * multiplicadorGlobal * Time.deltaTime;
        deslocamento = Mathf.Repeat(deslocamento, larguraTile);

        float direcao = paraEsquerda ? -1f : 1f;
        transform.position = new Vector3(posInicial.x + direcao * deslocamento, posInicial.y, posInicial.z);
    }
}