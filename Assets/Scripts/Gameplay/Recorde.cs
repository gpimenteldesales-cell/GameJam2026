using System;
using System.Collections.Generic;
using UnityEngine;

// Ranking (leaderboard) local, salvo no computador do jogador (PlayerPrefs).
// Nao precisa ficar em nenhum objeto da cena.
//
//   Recorde.Registrar(p, t)        chame no fim da partida; coloca a partida na lista
//   Recorde.DefinirNomeUltima(n)   troca o nome da partida que acabou de ser registrada (e lembra o nome)
//   Recorde.Total                  quantas partidas existem no ranking
//   Recorde.Pegar(i)               partida na posicao i (0 = primeiro lugar)
//   Recorde.PaginasTotais          quantas paginas de 10 existem (cresce sozinho)
//   Recorde.PosicaoUltima          posicao (1, 2, 3...) da ultima partida, 0 se nao entrou
//   Recorde.Pontos / Recorde.Tempo melhor pontuacao / maior tempo (igual antes)
//   Recorde.NovoDePontos / NovoDeTempo  (igual antes)
public static class Recorde
{
    [Serializable]
    public class Entrada
    {
        public int id;
        public string nome;
        public int pontos;
        public float tempo;
    }

    [Serializable]
    public class ListaSalva
    {
        public List<Entrada> entradas = new List<Entrada>();
    }

    public const int POR_PAGINA = 10;
    public const int TAMANHO_NOME = 12;
    public const string NOME_PADRAO = "Pescador";

    // quantas partidas o ranking guarda no maximo (as piores saem). Aumente se quiser.
    const int MAXIMO_ENTRADAS = 100;

    const string CHAVE_LISTA = "leaderboard_lista";
    const string CHAVE_NOME = "leaderboard_nome";
    const string CHAVE_PROXIMO_ID = "leaderboard_proximo_id";

    static List<Entrada> cache;

    public static bool NovoDePontos { get; private set; }
    public static bool NovoDeTempo { get; private set; }

    // id da ultima partida registrada (0 = nenhuma)
    public static int UltimaId { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        cache = null;
        NovoDePontos = false;
        NovoDeTempo = false;
        UltimaId = 0;
    }

    // ---------- Leitura ----------

    static List<Entrada> Entradas
    {
        get
        {
            if (cache == null) Carregar();
            return cache;
        }
    }

    public static int Total
    {
        get { return Entradas.Count; }
    }

    public static int PaginasTotais
    {
        get { return Mathf.Max(1, Mathf.CeilToInt(Total / (float)POR_PAGINA)); }
    }

    public static int Pontos
    {
        get { return Entradas.Count > 0 ? Entradas[0].pontos : 0; }
    }

    public static float Tempo
    {
        get
        {
            float maior = 0f;

            for (int i = 0; i < Entradas.Count; i++)
            {
                if (Entradas[i].tempo > maior) maior = Entradas[i].tempo;
            }

            return maior;
        }
    }

    public static string NomeSalvo
    {
        get { return PlayerPrefs.GetString(CHAVE_NOME, NOME_PADRAO); }
    }

    public static Entrada Pegar(int posicao)
    {
        if (posicao < 0 || posicao >= Entradas.Count) return null;
        return Entradas[posicao];
    }

    public static int PosicaoUltima
    {
        get
        {
            if (UltimaId == 0) return 0;

            for (int i = 0; i < Entradas.Count; i++)
            {
                if (Entradas[i].id == UltimaId) return i + 1;
            }

            return 0;
        }
    }

    // ---------- Escrita ----------

    public static void Registrar(int pontos, float tempo)
    {
        // so conta como recorde se for maior que o salvo e maior que zero
        NovoDePontos = pontos > 0 && pontos > Pontos;
        NovoDeTempo = tempo > 0f && tempo > Tempo;
        UltimaId = 0;

        // partida com zero pontos nao entra no ranking
        if (pontos <= 0) return;

        Entrada nova = new Entrada();
        nova.id = PlayerPrefs.GetInt(CHAVE_PROXIMO_ID, 1);
        nova.nome = NomeSalvo;
        nova.pontos = pontos;
        nova.tempo = tempo;

        PlayerPrefs.SetInt(CHAVE_PROXIMO_ID, nova.id + 1);

        Entradas.Add(nova);
        Ordenar();

        if (Entradas.Count > MAXIMO_ENTRADAS)
        {
            Entradas.RemoveRange(MAXIMO_ENTRADAS, Entradas.Count - MAXIMO_ENTRADAS);
        }

        UltimaId = nova.id;
        Salvar();
    }

    public static void DefinirNomeUltima(string nome)
    {
        nome = LimparNome(nome);
        PlayerPrefs.SetString(CHAVE_NOME, nome);

        for (int i = 0; i < Entradas.Count; i++)
        {
            if (Entradas[i].id == UltimaId && UltimaId != 0)
            {
                Entradas[i].nome = nome;
                break;
            }
        }

        Salvar();
    }

    public static void Apagar()
    {
        PlayerPrefs.DeleteKey(CHAVE_LISTA);
        PlayerPrefs.DeleteKey(CHAVE_NOME);
        PlayerPrefs.DeleteKey(CHAVE_PROXIMO_ID);
        PlayerPrefs.Save();

        cache = new List<Entrada>();
        NovoDePontos = false;
        NovoDeTempo = false;
        UltimaId = 0;
    }

    // ---------- Internos ----------

    static string LimparNome(string nome)
    {
        if (nome == null) return NOME_PADRAO;

        nome = nome.Trim();

        if (nome.Length > TAMANHO_NOME) nome = nome.Substring(0, TAMANHO_NOME);
        if (nome == "") return NOME_PADRAO;

        return nome;
    }

    static void Carregar()
    {
        cache = new List<Entrada>();

        string json = PlayerPrefs.GetString(CHAVE_LISTA, "");

        if (json != "")
        {
            ListaSalva lista = JsonUtility.FromJson<ListaSalva>(json);

            if (lista != null && lista.entradas != null) cache = lista.entradas;
        }

        Ordenar();
    }

    // mais pontos primeiro; se empatar, quem durou mais tempo; se ainda empatar, quem jogou primeiro
    static void Ordenar()
    {
        cache.Sort((a, b) =>
        {
            if (a.pontos != b.pontos) return b.pontos.CompareTo(a.pontos);
            if (a.tempo != b.tempo) return b.tempo.CompareTo(a.tempo);
            return a.id.CompareTo(b.id);
        });
    }

    static void Salvar()
    {
        ListaSalva lista = new ListaSalva();
        lista.entradas = Entradas;

        PlayerPrefs.SetString(CHAVE_LISTA, JsonUtility.ToJson(lista));
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Ferramentas/Apagar ranking local")]
    static void ApagarNoEditor()
    {
        Apagar();
        Debug.Log("[Recorde] ranking local apagado.");
    }
#endif
}