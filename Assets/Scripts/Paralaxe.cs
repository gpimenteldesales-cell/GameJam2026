using UnityEngine;

// Faz o fundo andar em loop infinito.
// Como o barco fica parado, e o cenario que se mexe.
public class Paralaxe : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 2f;
    public bool paraEsquerda = true;   // marcado = o fundo anda pra esquerda

    // Multiplica a velocidade de todas as camadas ao mesmo tempo
    // (1 = normal, 0 = tudo parado)
    public static float multiplicadorGlobal = 1f;

    Vector3 posicaoInicial;
    float larguraDoTile;
    float deslocamento;

    void Start()
    {
        posicaoInicial = transform.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // guarda a largura de uma imagem do fundo
        larguraDoTile = sr.sprite.bounds.size.x * transform.localScale.x;

        // repete a imagem 3 vezes para nao aparecer buraco
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(sr.sprite.bounds.size.x * 3f, sr.sprite.bounds.size.y);
    }

    void Update()
    {
        // quanto o fundo andou
        deslocamento += velocidade * multiplicadorGlobal * Time.deltaTime;

        // quando passa de uma largura, volta pro zero (e o loop)
        deslocamento = Mathf.Repeat(deslocamento, larguraDoTile);

        float direcao = 1f;
        if (paraEsquerda)
        {
            direcao = -1f;
        }

        transform.position = new Vector3(
            posicaoInicial.x + direcao * deslocamento,
            posicaoInicial.y,
            posicaoInicial.z);
    }
}
