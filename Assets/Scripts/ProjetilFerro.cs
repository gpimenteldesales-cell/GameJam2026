using UnityEngine;

// O ferro arremessado. Voa em linha reta e, se passar perto de uma bruxa,
// acerta e ela foge. Nao precisa de collider: a checagem e feita por distancia.
public class ProjetilFerro : MonoBehaviour
{
    public float raioAcerto = 0.8f;     // tamanho da "area" da bruxa (aumente se estiver dificil acertar)
    public float tempoDeVida = 2.5f;    // some se nao acertar nada
    public float giro = 360f;           // graus por segundo (so visual)

    Vector2 velocidade;

    // chamado pelo InventarioFerro logo depois de criar o projetil
    public void Lancar(Vector2 novaVelocidade)
    {
        velocidade = novaVelocidade;
    }

    void Update()
    {
        Vector2 antes = transform.position;
        Vector2 depois = antes + velocidade * Time.deltaTime;

        // confere o caminho inteiro deste frame (assim nao atravessa a bruxa se for rapido)
        for (int i = 0; i < BruxaVoadora.todas.Count; i++)
        {
            BruxaVoadora bruxa = BruxaVoadora.todas[i];

            // bruxa que ja esta fugindo nao conta
            if (bruxa.estadoAtual == BruxaVoadora.Estado.Recuando) continue;

            float distancia = DistanciaAteOSegmento(bruxa.transform.position, antes, depois);
            if (distancia <= raioAcerto)
            {
                bruxa.AcertadaPorFerro();
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

    // menor distancia entre um ponto e a linha que vai de a ate b
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