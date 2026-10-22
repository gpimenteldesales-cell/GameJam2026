using UnityEngine;

public class EntradaEscalonada : MonoBehaviour
{
    public float atrasoInicial = 0.6f;
    public float atrasoAoVoltar = 0f;
    public float intervalo = 0.12f;
    public float duracao = 0.5f;
    public float giroInicial = 14f;

    Transform[] filhos;
    Vector3[] escalasFinais;
    BotaoJuice[] botoes;
    float tempo;
    float atrasoAtual;
    bool primeiraVez = true;
    bool terminou;

    void OnEnable()
    {
        int n = transform.childCount;
        filhos = new Transform[n];
        escalasFinais = new Vector3[n];
        botoes = new BotaoJuice[n];

        for (int i = 0; i < n; i++)
        {
            filhos[i] = transform.GetChild(i);
            botoes[i] = filhos[i].GetComponent<BotaoJuice>();
            escalasFinais[i] = filhos[i].localScale;
        }

        atrasoAtual = primeiraVez ? atrasoInicial : atrasoAoVoltar;
        primeiraVez = false;

        tempo = 0f;
        terminou = false;
        Aplicar();
    }

    void OnDisable()
    {
        if (filhos == null) return;

        for (int i = 0; i < filhos.Length; i++)
        {
            if (filhos[i] == null) continue;

            if (botoes[i] != null)
            {
                botoes[i].escalaExterna = 1f;
                botoes[i].anguloExterno = 0f;
            }
            else
            {
                filhos[i].localScale = escalasFinais[i];
            }
        }
    }

    void Update()
    {
        if (terminou) return;

        tempo += Time.unscaledDeltaTime;
        Aplicar();
    }

    void Aplicar()
    {
        bool todosProntos = true;

        for (int i = 0; i < filhos.Length; i++)
        {
            if (filhos[i] == null) continue;

            float t = Mathf.Clamp01((tempo - atrasoAtual - i * intervalo) / Mathf.Max(0.01f, duracao));
            if (t < 1f) todosProntos = false;

            float k = EntradaUI.OutBack(t);
            float giro = (1f - t) * giroInicial * ((i % 2 == 0) ? 1f : -1f);

            if (botoes[i] != null)
            {
                botoes[i].escalaExterna = k;
                botoes[i].anguloExterno = giro;
            }
            else
            {
                filhos[i].localScale = escalasFinais[i] * k;
                filhos[i].localRotation = Quaternion.Euler(0f, 0f, giro);
            }
        }

        if (todosProntos) terminou = true;
    }
}