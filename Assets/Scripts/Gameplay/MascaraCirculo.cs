using UnityEngine;

public static class MascaraCirculo
{
    static Texture2D texturaFuro;
    static Texture2D texturaAnel;

    public static void Desenhar(Vector2 centro, float raio, float alpha)
    {
        if (alpha <= 0f) return;

        Color antes = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);

        float w = Screen.width;
        float h = Screen.height;

        if (raio < 1f)
        {
            Preencher(0f, 0f, w, h);
            GUI.color = antes;
            return;
        }

        float esquerda = Mathf.Clamp(centro.x - raio, 0f, w);
        float direita = Mathf.Clamp(centro.x + raio, 0f, w);
        float topo = Mathf.Clamp(centro.y - raio, 0f, h);
        float baixo = Mathf.Clamp(centro.y + raio, 0f, h);

        Preencher(0f, 0f, w, topo);
        Preencher(0f, baixo, w, h - baixo);
        Preencher(0f, topo, esquerda, baixo - topo);
        Preencher(direita, topo, w - direita, baixo - topo);

        if (texturaFuro == null) texturaFuro = CriarTextura(false);
        GUI.DrawTexture(new Rect(centro.x - raio, centro.y - raio, raio * 2f, raio * 2f), texturaFuro);

        GUI.color = antes;
    }

    public static void Anel(Vector2 centro, float raio, Color cor)
    {
        if (cor.a <= 0f || raio < 1f) return;

        if (texturaAnel == null) texturaAnel = CriarTextura(true);

        float metade = raio / 0.97f;
        Color antes = GUI.color;
        GUI.color = cor;
        GUI.DrawTexture(new Rect(centro.x - metade, centro.y - metade, metade * 2f, metade * 2f), texturaAnel);
        GUI.color = antes;
    }

    public static void Retangulo(Rect r, Color cor)
    {
        Color antes = GUI.color;
        GUI.color = cor;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = antes;
    }

    static void Preencher(float x, float y, float largura, float altura)
    {
        if (largura <= 0f || altura <= 0f) return;
        GUI.DrawTexture(new Rect(x, y, largura, altura), Texture2D.whiteTexture);
    }

    static Texture2D CriarTextura(bool anel)
    {
        const int tamanho = 256;

        Texture2D tex = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[tamanho * tamanho];
        float meio = (tamanho - 1) / 2f;
        float raioPx = tamanho / 2f;
        Vector2 centro = new Vector2(meio, meio);

        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), centro);

                if (anel)
                {
                    float n = d / raioPx;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(n - 0.97f) / 0.025f);
                    pixels[y * tamanho + x] = new Color(1f, 1f, 1f, a);
                }
                else
                {
                    float a = Mathf.Clamp01((d - (raioPx - 1f)) / 1.5f + 0.5f);
                    pixels[y * tamanho + x] = new Color(0f, 0f, 0f, a);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
