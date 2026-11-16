using UnityEngine;

// Gera por codigo os sprites usados pelos scripts de ambiente (nao precisa importar arte).
// Todos saem em branco com transparencia: a cor final vem do SpriteRenderer.
public static class SpritesProcedurais
{
    static Texture2D NovaTextura(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    static Sprite Finalizar(Texture2D tex, Color32[] px, float pixelsPorUnidade)
    {
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPorUnidade);
    }

    static Color32 Branco(float alfa)
    {
        return new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alfa) * 255f));
    }

    // Bolinha de luz: miolo forte + halo suave. Mede 1 unidade.
    public static Sprite Brilho(int tam = 64)
    {
        Texture2D tex = NovaTextura(tam, tam);
        Color32[] px = new Color32[tam * tam];
        float meio = (tam - 1) * 0.5f;
        float raio = tam * 0.5f;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Mathf.Sqrt((x - meio) * (x - meio) + (y - meio) * (y - meio)) / raio;
                float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f) * 1.25f;
                float miolo = Mathf.Clamp01(1f - d / 0.2f);
                px[y * tam + x] = Branco(Mathf.Max(halo, miolo));
            }
        }

        return Finalizar(tex, px, tam);
    }

    // Estrela com cruz de brilho. Mede 1 unidade.
    public static Sprite EstrelaCruz(int tam = 64)
    {
        Texture2D tex = NovaTextura(tam, tam);
        Color32[] px = new Color32[tam * tam];
        float meio = (tam - 1) * 0.5f;
        float raio = tam * 0.5f;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = (x - meio) / raio;
                float dy = (y - meio) / raio;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 3f) * 1.2f;
                float bracoH = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dx)), 3f) * Mathf.Clamp01(1f - Mathf.Abs(dy) / 0.08f);
                float bracoV = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dy)), 3f) * Mathf.Clamp01(1f - Mathf.Abs(dx) / 0.08f);

                px[y * tam + x] = Branco(Mathf.Max(halo, Mathf.Max(bracoH, bracoV)));
            }
        }

        return Finalizar(tex, px, tam);
    }

    // Anel macio (para marolas). Mede 1 unidade.
    public static Sprite Anel(int tam = 128)
    {
        Texture2D tex = NovaTextura(tam, tam);
        Color32[] px = new Color32[tam * tam];
        float meio = (tam - 1) * 0.5f;
        float raio = tam * 0.5f;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Mathf.Sqrt((x - meio) * (x - meio) + (y - meio) * (y - meio)) / raio;
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.12f);
                a = a * a * (3f - 2f * a);
                px[y * tam + x] = Branco(a);
            }
        }

        return Finalizar(tex, px, tam);
    }

    // Degrade vertical: transparente em cima, opaco embaixo. Altura = 1 unidade.
    public static Sprite Gradiente(int altura = 128, float expoente = 1.5f)
    {
        Texture2D tex = NovaTextura(2, altura);
        Color32[] px = new Color32[2 * altura];

        for (int y = 0; y < altura; y++)
        {
            float t = 1f - y / (float)(altura - 1);
            Color32 c = Branco(Mathf.Pow(t, expoente));
            px[y * 2] = c;
            px[y * 2 + 1] = c;
        }

        return Finalizar(tex, px, altura);
    }

    // ---------- Morcego (2 quadros: asas para cima e para baixo) ----------

    static readonly Vector2[] AsaCima =
    {
        new Vector2(35f, 17f), new Vector2(40f, 22f), new Vector2(46f, 27f), new Vector2(53f, 30f),
        new Vector2(61f, 30f), new Vector2(57f, 24f), new Vector2(55f, 19f), new Vector2(51f, 21f),
        new Vector2(47f, 15f), new Vector2(42f, 16f), new Vector2(38f, 11f)
    };

    static readonly Vector2[] AsaBaixo =
    {
        new Vector2(35f, 17f), new Vector2(41f, 16f), new Vector2(47f, 13f), new Vector2(53f, 8f),
        new Vector2(61f, 3f), new Vector2(56f, 10f), new Vector2(55f, 14f), new Vector2(51f, 12f),
        new Vector2(47f, 8f), new Vector2(42f, 10f), new Vector2(38f, 8f)
    };

    static readonly Vector2[] OrelhaEsq = { new Vector2(29f, 23f), new Vector2(29.6f, 28.5f), new Vector2(31.4f, 24f) };

    public static Sprite[] Morcego(float pixelsPorUnidade = 48f)
    {
        return new Sprite[]
        {
            QuadroMorcego(AsaCima, pixelsPorUnidade),
            QuadroMorcego(AsaBaixo, pixelsPorUnidade)
        };
    }

    static Vector2[] Espelhar(Vector2[] p)
    {
        Vector2[] r = new Vector2[p.Length];
        for (int i = 0; i < p.Length; i++) r[i] = new Vector2(64f - p[i].x, p[i].y);
        return r;
    }

    static bool Dentro(Vector2[] poly, float x, float y)
    {
        bool dentro = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > y) != (poly[j].y > y) &&
                x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
            {
                dentro = !dentro;
            }
        }
        return dentro;
    }

    static bool Corpo(float x, float y)
    {
        float cx = (x - 32f) / 3.4f, cy = (y - 14f) / 6.5f;
        float hx = (x - 32f) / 3.2f, hy = (y - 21.5f) / 3.2f;
        return cx * cx + cy * cy <= 1f || hx * hx + hy * hy <= 1f;
    }

    static Sprite QuadroMorcego(Vector2[] asaDireita, float ppu)
    {
        const int W = 64, H = 32;
        Vector2[] asaEsquerda = Espelhar(asaDireita);
        Vector2[] orelhaDir = Espelhar(OrelhaEsq);

        Texture2D tex = NovaTextura(W, H);
        Color32[] px = new Color32[W * H];

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int acertos = 0;
                for (int sy = 0; sy < 3; sy++)
                {
                    for (int sx = 0; sx < 3; sx++)
                    {
                        float X = x + (sx + 0.5f) / 3f;
                        float Y = y + (sy + 0.5f) / 3f;
                        if (Dentro(asaDireita, X, Y) || Dentro(asaEsquerda, X, Y) ||
                            Dentro(OrelhaEsq, X, Y) || Dentro(orelhaDir, X, Y) || Corpo(X, Y))
                        {
                            acertos++;
                        }
                    }
                }
                px[y * W + x] = Branco(acertos / 9f);
            }
        }

        return Finalizar(tex, px, ppu);
    }
}
