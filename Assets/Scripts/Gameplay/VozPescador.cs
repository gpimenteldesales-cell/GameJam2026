using UnityEngine;

// Coloque este script no objeto do pescador.
// Arraste as falas no campo "Falas Acerto" (uma e sorteada a cada acerto, sem repetir a anterior).
public class VozPescador : MonoBehaviour
{
    public static VozPescador instancia;

    [Header("Falas ao acertar a bruxa")]
    public AudioClip[] falasAcerto;
    [Range(0f, 1f)] public float volume = 1f;
    public float intervaloMinimo = 0.4f;

    float proximaFala;
    int ultima = -1;

    void Awake()
    {
        instancia = this;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    static bool avisouSemPescador;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        instancia = null;
        avisouSemPescador = false;
    }

    public static void FalarAcerto()
    {
        if (instancia != null)
        {
            instancia.Falar();
        }
        else if (!avisouSemPescador)
        {
            avisouSemPescador = true;
            Debug.LogWarning("[Som] A fala do pescador nao tocou: o script VozPescador nao esta em nenhum objeto ativo da cena.");
        }
    }

    void Falar()
    {
        if (falasAcerto == null || falasAcerto.Length == 0)
        {
            GerenciadorSons.Tocar(null, 1f, "Pescador: falas ao acertar (lista Falas Acerto vazia)");
            return;
        }

        if (Time.time < proximaFala) return;

        int indice = Random.Range(0, falasAcerto.Length);
        if (falasAcerto.Length > 1 && indice == ultima)
        {
            indice = (indice + 1 + Random.Range(0, falasAcerto.Length - 1)) % falasAcerto.Length;
        }

        ultima = indice;
        proximaFala = Time.time + intervaloMinimo;
        GerenciadorSons.Tocar(falasAcerto[indice], volume);
    }
}
