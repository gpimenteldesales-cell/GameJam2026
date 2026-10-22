using UnityEngine;

public class FishingRay : MonoBehaviour
{
    public Transform origem;          // ponto de onde sai o raio (ponta da vara)
    public float alcance = 8f;        // tamanho máximo do raio
    public float tempoParaPescar = 1f; // segundos mirando no peixe pra fisgar
    public LayerMask camadaPeixe;     // layer dos peixes

    LineRenderer linha;
    Camera cam;
    float tempoMirando = 0f;
    Fish peixeAtual;
    public int pontos = 0;
    public SpriteRenderer spritePescador;


    void Start()
    {
        cam = Camera.main;
        linha = GetComponent<LineRenderer>();
        linha.positionCount = 2;
        linha.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            linha.enabled = true;

            // 1. posição do mouse no mundo
            Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
            mouse.z = 0f;

            // 2. flip do sprite do pescador
            if (mouse.x < 0f)
            {
                spritePescador.flipX = true;
            }
            else
            {
                spritePescador.flipX = false;
            }

                // 3. direção da origem até o mouse
                Vector2 direcao = (mouse - origem.position).normalized;

            // 4. dispara o raio invisível
            RaycastHit2D hit = Physics2D.Raycast(origem.position, direcao, alcance, camadaPeixe);

            // 5. desenha a linha visível
            linha.SetPosition(0, origem.position);
            if (hit.collider != null)
            {
                linha.SetPosition(1, hit.point);
                Fish peixe = hit.collider.GetComponent<Fish>();

                if (peixe != null && peixe == peixeAtual)
                    tempoMirando += Time.deltaTime;
                else
                {
                    peixeAtual = peixe;
                    tempoMirando = 0f;
                }

                if (peixeAtual != null && tempoMirando >= tempoParaPescar)
                {
                    pontos += peixeAtual.valor;
                    Debug.Log("Pescou! Pontos: " + pontos);
                    Destroy(peixeAtual.gameObject);
                    peixeAtual = null;
                    tempoMirando = 0f;
                }
            }
            else
            {
                linha.SetPosition(1, origem.position + (Vector3)(direcao * alcance));
                peixeAtual = null;
                tempoMirando = 0f;
            }
        }
        else
        {
            linha.enabled = false;
            peixeAtual = null;
            tempoMirando = 0f;
        }
    }
}