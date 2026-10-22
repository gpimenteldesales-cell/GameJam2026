using UnityEngine;

public class BarraDeVida : MonoBehaviour
{
    Transform alvo;
    SpriteRenderer spriteDoAlvo;
    float largura = 1.4f;
    float altura = 0.16f;
    float borda = 0.04f;
    float folgaAcima = 0.3f;
    int ordemBase = 50;
    float velocidadeEsvaziar = 1.2f;

    float proporcaoAlvo = 1f;
    float proporcaoMostrada = 1f;
    Transform preenchimento;
    SpriteRenderer preenchimentoSr;

    static Sprite spriteCentro;
    static Sprite spriteEsquerda;

    public static BarraDeVida Criar(Transform alvo, SpriteRenderer spriteDoAlvo, float largura)
    {
        GameObject obj = new GameObject("BarraDeVida");
        BarraDeVida barra = obj.AddComponent<BarraDeVida>();
        barra.alvo = alvo;
        barra.spriteDoAlvo = spriteDoAlvo;
        barra.largura = largura;
        barra.Montar();
        barra.Posicionar();
        return barra;
    }

    public void DefinirProporcao(float proporcao)
    {
        proporcaoAlvo = Mathf.Clamp01(proporcao);
    }

    void Montar()
    {
        if (spriteCentro == null) spriteCentro = CriarSprite(new Vector2(0.5f, 0.5f));
        if (spriteEsquerda == null) spriteEsquerda = CriarSprite(new Vector2(0f, 0.5f));

        SpriteRenderer ignorado;

        CriarQuadrado("Moldura", spriteCentro,
            new Vector2(largura + borda * 2f, altura + borda * 2f), Vector2.zero,
            new Color(0.08f, 0.08f, 0.1f), ordemBase, out ignorado);

        CriarQuadrado("Fundo", spriteCentro,
            new Vector2(largura, altura), Vector2.zero,
            new Color(0.35f, 0.35f, 0.38f), ordemBase + 1, out ignorado);

        preenchimento = CriarQuadrado("Vida", spriteEsquerda,
            new Vector2(largura, altura), new Vector2(-largura / 2f, 0f),
            new Color(0.35f, 0.85f, 0.35f), ordemBase + 2, out preenchimentoSr);
    }

    Transform CriarQuadrado(string nome, Sprite sprite, Vector2 tamanho, Vector2 posicao,
                            Color cor, int ordem, out SpriteRenderer render)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = new Vector3(posicao.x, posicao.y, 0f);
        obj.transform.localScale = new Vector3(tamanho.x, tamanho.y, 1f);

        render = obj.AddComponent<SpriteRenderer>();
        render.sprite = sprite;
        render.color = cor;
        render.sortingOrder = ordem;
        return obj.transform;
    }

    static Sprite CriarSprite(Vector2 pivo)
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivo, tex.width);
    }

    void LateUpdate()
    {
        if (alvo == null)
        {
            Destroy(gameObject);
            return;
        }

        proporcaoMostrada = Mathf.MoveTowards(proporcaoMostrada, proporcaoAlvo, velocidadeEsvaziar * Time.deltaTime);

        Vector3 escala = preenchimento.localScale;
        escala.x = largura * proporcaoMostrada;
        preenchimento.localScale = escala;

        Color cor;
        if (proporcaoMostrada > 0.5f)
        {
            cor = new Color(0.35f, 0.85f, 0.35f);
        }
        else if (proporcaoMostrada > 0.2f)
        {
            cor = new Color(0.97f, 0.8f, 0.2f);
        }
        else
        {
            cor = new Color(0.9f, 0.25f, 0.2f);
        }
        preenchimentoSr.color = cor;

        Posicionar();
    }

    void Posicionar()
    {
        float topo = alvo.position.y + 1f;
        if (spriteDoAlvo != null)
        {
            topo = spriteDoAlvo.bounds.max.y;
        }

        transform.position = new Vector3(alvo.position.x, topo + folgaAcima, alvo.position.z);
    }
}