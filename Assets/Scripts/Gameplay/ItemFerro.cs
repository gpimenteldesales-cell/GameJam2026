using System.Collections.Generic;
using UnityEngine;

public class ItemFerro : MonoBehaviour
{
    public static readonly List<ItemFerro> todos = new List<ItemFerro>();

    [Header("Movimento")]
    public float velocidadeCorrente = 2f;
    public bool paraEsquerda = true;
    public float limiteX = 8f;

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

        Vector3 p = transform.position;
        p.x += direcao * velocidadeCorrente * Paralaxe.multiplicadorGlobal * Time.deltaTime;
        p.y = yBase + Mathf.Sin(Time.time * frequenciaBoiar + fase) * amplitudeBoiar;
        transform.position = p;

        bool saiu = paraEsquerda ? p.x < -limiteX - 1f : p.x > limiteX + 1f;
        if (saiu)
        {
            Destroy(gameObject);
        }
    }

    public void Coletado()
    {
        Destroy(gameObject);
    }

    public static GameObject CriarPlaceholder(string nome, float tamanho, Color cor)
    {
        GameObject obj = new GameObject(nome);

        SpriteRenderer render = obj.AddComponent<SpriteRenderer>();
        render.sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            Texture2D.whiteTexture.width);
        render.color = cor;
        render.sortingOrder = 20;

        obj.transform.localScale = new Vector3(tamanho, tamanho, 1f);
        return obj;
    }
}