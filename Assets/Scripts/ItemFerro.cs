using System.Collections.Generic;
using UnityEngine;

// Pedaco de ferro que boia no mar, levado pela correnteza.
// O jogador pega com o InventarioFerro (clique direito em cima dele).
public class ItemFerro : MonoBehaviour
{
    // Todos os ferros que existem agora (o inventario usa isso)
    public static readonly List<ItemFerro> todos = new List<ItemFerro>();

    [Header("Movimento")]
    public float velocidadeCorrente = 2f;     // use a mesma velocidade do Paralaxe do fundo
    public bool paraEsquerda = true;          // marcado = a correnteza vai pra esquerda
    public float limiteX = 8f;                // depois de passar disso (+1) o ferro some

    [Header("Boiar")]
    public float amplitudeBoiar = 0.15f;
    public float frequenciaBoiar = 2f;

    float yBase;
    float fase;

    void OnEnable()
    {
        todos.Add(this);
    }

    void OnDisable()
    {
        todos.Remove(this);
    }

    void Start()
    {
        yBase = transform.position.y;
        fase = Random.value * 10f;
    }

    void Update()
    {
        float direcao = 1f;
        if (paraEsquerda)
        {
            direcao = -1f;
        }

        // anda junto com o cenario (para quando o barco vira, igual ao Paralaxe)
        Vector3 p = transform.position;
        p.x += direcao * velocidadeCorrente * Paralaxe.multiplicadorGlobal * Time.deltaTime;
        p.y = yBase + Mathf.Sin(Time.time * frequenciaBoiar + fase) * amplitudeBoiar;
        transform.position = p;

        // saiu da tela pelo outro lado: some
        bool saiu = paraEsquerda ? p.x < -limiteX - 1f : p.x > limiteX + 1f;
        if (saiu)
        {
            Destroy(gameObject);
        }
    }

    // chamado pelo InventarioFerro quando o jogador pega
    public void Coletado()
    {
        Destroy(gameObject);
    }

    // Cria um quadradinho colorido, para usar enquanto voce nao tem sprite/prefab.
    // (o GeradorDeFerro e o InventarioFerro usam isso quando o prefab fica vazio)
    public static GameObject CriarPlaceholder(string nome, float tamanho, Color cor)
    {
        GameObject obj = new GameObject(nome);

        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            Texture2D.whiteTexture.width);   // 1 unidade de mundo = a imagem inteira
        render.color = cor;
        render.sortingOrder = 20;

        obj.transform.localScale = new Vector3(tamanho, tamanho, 1f);
        return obj;
    }
}