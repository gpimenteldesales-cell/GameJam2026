using UnityEngine;

// Coloque este script em CADA camada do fundo (as 3 sprites), cada uma com sua velocidade.
// Ele repete a sprite sozinho (sem buracos, em qualquer tamanho de tela), deixa a camada
// ondular de leve e pode "enevoar" as camadas distantes para dar sensacao de profundidade.
[RequireComponent(typeof(SpriteRenderer))]
public class Paralaxe : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 2f;
    public bool paraEsquerda = true;

    [Header("Ondulacao suave (vida no fundo)")]
    public float amplitudeVertical = 0.06f;
    public float frequenciaVertical = 0.6f;

    [Header("Profundidade (camadas distantes ficam mais claras)")]
    [Range(0f, 1f)] public float neblina = 0f;
    public Color corNeblina = new Color(0.62f, 0.78f, 0.92f);

    [Header("Cobertura da tela")]
    [Tooltip("Tiles extras alem do minimo, so por seguranca.")]
    public int tilesExtras = 1;

    public static float multiplicadorGlobal = 1f;

    Vector3 posicaoInicial;
    float larguraDoTile;
    float deslocamento;
    float fase;
    float fatorAtual = 1f;
    SpriteRenderer sr;
    Camera cam;
    int larguraTela;
    int alturaTela;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        if (sr.sprite == null)
        {
            Debug.LogError("Paralaxe: " + name + " esta sem sprite!", this);
            enabled = false;
            return;
        }

        cam = Camera.main;
        posicaoInicial = transform.position;
        fase = Random.value * 10f;
        fatorAtual = multiplicadorGlobal;

        larguraDoTile = sr.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);

        if (neblina > 0f)
        {
            sr.color = Color.Lerp(sr.color, corNeblina, neblina);
        }

        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        AjustarCobertura();
    }

    // Calcula quantas repeticoes sao necessarias para a camera nunca ver o fim da imagem.
    void AjustarCobertura()
    {
        larguraTela = Screen.width;
        alturaTela = Screen.height;

        float larguraSprite = sr.sprite.bounds.size.x;
        float escalaX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        int tiles = 3;

        if (cam != null && cam.orthographic)
        {
            float larguraCamera = cam.orthographicSize * 2f * cam.aspect;
            float distanciaDoCentro = Mathf.Abs(posicaoInicial.x - cam.transform.position.x);

            // a camada anda ate 1 tile pra um lado, e o desenho e centrado no objeto
            float precisa = 2f * (distanciaDoCentro + larguraCamera * 0.5f + larguraDoTile);
            tiles = Mathf.CeilToInt(precisa / (larguraSprite * escalaX)) + Mathf.Max(0, tilesExtras);
        }

        tiles = Mathf.Max(3, tiles);
        if (tiles % 2 == 0) tiles++;

        sr.size = new Vector2(larguraSprite * tiles, sr.sprite.bounds.size.y);
    }

    void Update()
    {
        if (Screen.width != larguraTela || Screen.height != alturaTela)
        {
            AjustarCobertura();
        }

        // freia e acelera suavemente (ex: ao perder, o fundo desacelera em vez de travar de uma vez)
        fatorAtual = Mathf.MoveTowards(fatorAtual, multiplicadorGlobal, 2f * Time.deltaTime);

        float direcao = paraEsquerda ? -1f : 1f;

        deslocamento += velocidade * fatorAtual * Time.deltaTime;
        deslocamento = Mathf.Repeat(deslocamento, larguraDoTile);

        float ondulacao = Mathf.Sin((Time.time + fase) * frequenciaVertical) * amplitudeVertical;

        transform.position = new Vector3(
            posicaoInicial.x + direcao * deslocamento,
            posicaoInicial.y + ondulacao,
            posicaoInicial.z);
    }
}
