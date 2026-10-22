using UnityEngine;

public class ProjetilFerro : MonoBehaviour
{
    public float raioAcerto = 0.8f;
    public float tempoDeVida = 2.5f;
    public int dano = 1;
    public float giro = 360f;

    Vector2 velocidade;

    public void Lancar(Vector2 novaVelocidade)
    {
        velocidade = novaVelocidade;

        ItemFerro boiando = GetComponent<ItemFerro>();
        if (boiando != null)
        {
            Destroy(boiando);
        }

        if (GetComponent<TrilhaFerro>() == null)
        {
            gameObject.AddComponent<TrilhaFerro>();
        }
    }

    void Update()
    {
        Vector2 antes = transform.position;
        Vector2 depois = antes + velocidade * Time.deltaTime;

        for (int i = 0; i < BruxaVoadora.todas.Count; i++)
        {
            BruxaVoadora bruxa = BruxaVoadora.todas[i];

            float distancia = DistanciaAteOSegmento(bruxa.transform.position, antes, depois);
            if (distancia <= raioAcerto)
            {
                bruxa.AcertadaPorFerro(dano);
                Destroy(gameObject);
                return;
            }
        }

        transform.position = new Vector3(depois.x, depois.y, transform.position.z);
        transform.Rotate(0f, 0f, giro * Time.deltaTime);

        tempoDeVida -= Time.deltaTime;
        if (tempoDeVida <= 0f)
        {
            Destroy(gameObject);
        }
    }

    static float DistanciaAteOSegmento(Vector2 ponto, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = 0f;

        if (ab.sqrMagnitude > 0f)
        {
            t = Mathf.Clamp01(Vector2.Dot(ponto - a, ab) / ab.sqrMagnitude);
        }

        return Vector2.Distance(ponto, a + ab * t);
    }
}