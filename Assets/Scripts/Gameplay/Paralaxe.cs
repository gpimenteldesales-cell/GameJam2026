using System.Collections.Generic;
using UnityEngine;

// Coloque este script em CADA camada do fundo (mar, ceu...), cada uma com sua velocidade.
//
// Parallax infinito sem costura:
//  - A camada e desenhada por varios tiles (SpriteRenderers filhos) colocados lado a lado em passos exatos
//    da largura do sprite. O movimento repete a cada 1 tile (ou 2, no modo espelho), entao o "pulo" do reinicio
//    cai exatamente em cima de um desenho identico: nao existe ponto onde a imagem acaba.
//  - Funciona com qualquer Mesh Type do sprite (Tight ou Full Rect) e com qualquer tamanho de tela.
//  - Espelhar Tiles: vira tiles alternados, entao a borda de um sempre casa com a do vizinho,
//    mesmo que a arte nao seja "tileavel".
//  - O SpriteRenderer original e desligado; os tiles copiam sprite, cor, material e ordem de desenho dele.
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class Paralaxe : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 2f;
    public bool paraEsquerda = true;

    [Header("Ondulacao suave (vida no fundo)")]
    public float amplitudeVertical = 0.06f;
    public float frequenciaVertical = 0.6f;
    [Tooltip("Ligado: cada camada ondula em fase propria. Desligado: todas ondulam juntas (sem desencontro entre camadas iguais).")]
    public bool ondaAleatoria = true;

    [Header("Profundidade (camadas distantes ficam mais claras)")]
    [Range(0f, 1f)] public float neblina = 0f;
    public Color corNeblina = new Color(0.62f, 0.78f, 0.92f);

    [Header("Emenda infinita (sem costura)")]
    [Tooltip("Vira tiles alternados como espelho. Garante emenda perfeita mesmo se a arte nao for 'tileavel'. Desligue se a sua arte ja emenda sozinha.")]
    public bool espelharTiles = true;
    [Tooltip("Sobreposicao entre tiles, em pixels da imagem, para esconder linhas finas de emenda.")]
    public float sobreposicaoPixels = 1f;
    [Tooltip("Alinha a camada aos pixels da tela durante o movimento (evita tremida e linhas finas).")]
    public bool alinharAosPixels = true;
    [Tooltip("Tiles extras alem do minimo, so por seguranca.")]
    public int tilesExtras = 1;

    public static float multiplicadorGlobal = 1f;

    SpriteRenderer sr;
    Camera cam;
    Vector3 posicaoInicial;
    float larguraTileLocal;
    float larguraTileMundo;
    float deslocamento;
    float fase;
    float fatorAtual = 1f;
    int larguraTela;
    int alturaTela;
    readonly List<GameObject> tiles = new List<GameObject>();

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
        fase = ondaAleatoria ? Random.value * 10f : 0f;
        fatorAtual = multiplicadorGlobal;

        if (neblina > 0f)
        {
            sr.color = Color.Lerp(sr.color, corNeblina, neblina);
        }

        // Largura do retangulo inteiro do sprite (nao da malha), assim Tight e Full Rect dao o mesmo resultado.
        Sprite s = sr.sprite;
        larguraTileLocal = s.rect.width / s.pixelsPerUnit;
        larguraTileMundo = larguraTileLocal * Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));

        Reconstruir();
        sr.enabled = false;
    }

    void Reconstruir()
    {
        larguraTela = Screen.width;
        alturaTela = Screen.height;

        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null) Destroy(tiles[i]);
        }
        tiles.Clear();

        float periodo = larguraTileMundo * (espelharTiles ? 2f : 1f);
        int lados = 2;

        if (cam != null && cam.orthographic)
        {
            float larguraCamera = cam.orthographicSize * 2f * cam.aspect;
            float distancia = Mathf.Abs(posicaoInicial.x - cam.transform.position.x);

            // a camada anda ate 1 periodo, e os tiles sao centrados no objeto
            float precisa = distancia + periodo + larguraCamera * 0.5f;
            lados = Mathf.CeilToInt(precisa / larguraTileMundo - 0.5f) + Mathf.Max(0, tilesExtras);
        }

        lados = Mathf.Max(2, lados);

        for (int k = -lados; k <= lados; k++)
        {
            tiles.Add(CriarTile(k));
        }
    }

    GameObject CriarTile(int k)
    {
        Sprite s = sr.sprite;
        Rect r = s.rect;

        GameObject obj = new GameObject("tile_" + k);
        obj.transform.SetParent(transform, false);

        SpriteRenderer t = obj.AddComponent<SpriteRenderer>();
        t.sprite = s;
        t.color = sr.color;
        t.sharedMaterial = sr.sharedMaterial;
        t.sortingLayerID = sr.sortingLayerID;
        t.sortingOrder = sr.sortingOrder;
        t.maskInteraction = sr.maskInteraction;
        t.drawMode = SpriteDrawMode.Simple;

        bool espelhado = espelharTiles && (Mathf.Abs(k) % 2 == 1);
        t.flipX = sr.flipX != espelhado;
        t.flipY = sr.flipY;

        // escala so no eixo X, em torno do centro do retangulo, para sobrepor um pouco os vizinhos
        float escala = 1f + Mathf.Max(0f, sobreposicaoPixels) / Mathf.Max(1f, r.width);
        float centro = (r.width * 0.5f - s.pivot.x) / s.pixelsPerUnit;
        float centroNoTile = t.flipX ? -centro : centro;

        obj.transform.localScale = new Vector3(escala, 1f, 1f);
        obj.transform.localPosition = new Vector3(k * larguraTileLocal + centro - escala * centroNoTile, 0f, 0f);

        return obj;
    }

    void Update()
    {
        if (Screen.width != larguraTela || Screen.height != alturaTela)
        {
            Reconstruir();
        }

        // freia e acelera suavemente (ex: ao perder, o fundo desacelera em vez de travar de uma vez)
        fatorAtual = Mathf.MoveTowards(fatorAtual, multiplicadorGlobal, 2f * Time.deltaTime);

        float periodo = larguraTileMundo * (espelharTiles ? 2f : 1f);
        float direcao = paraEsquerda ? -1f : 1f;

        deslocamento = Mathf.Repeat(deslocamento + velocidade * fatorAtual * Time.deltaTime, periodo);

        float ondulacao = Mathf.Sin((Time.time + fase) * frequenciaVertical) * amplitudeVertical;

        float x = posicaoInicial.x + direcao * deslocamento;
        float y = posicaoInicial.y + ondulacao;

        if (alinharAosPixels && cam != null && cam.orthographic && Screen.height > 0)
        {
            float porPixel = cam.orthographicSize * 2f / Screen.height;
            x = Mathf.Round(x / porPixel) * porPixel;
            y = Mathf.Round(y / porPixel) * porPixel;
        }

        transform.position = new Vector3(x, y, posicaoInicial.z);
    }
}