
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AplicarFontePadrao 
{
    const string NOME_ASSET = "IrishGrover-Regular SDF";
    const string TTF_ORIGEM = "Assets/Fonts/IrishGrover-Regular.ttf";
    const string PASTA_RESOURCES = "Assets/Resources";
    const string PASTA_FONTES = "Assets/Resources/Fonts";
    const string TTF_DESTINO = "Assets/Resources/Fonts/IrishGrover-Regular.ttf";

    [MenuItem("Ferramentas/Aplicar Irish Grover em tudo")]
    static void Aplicar()
    {
        TMP_FontAsset fonte = CarregarFonte();
        if (fonte == null)
        {
            EditorUtility.DisplayDialog("Irish Grover",
                "Nao achei o asset \"" + NOME_ASSET + "\". Confira se ele existe em Assets/Fonts.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string cenaAberta = SceneManager.GetActiveScene().path;

        AjustarTmpSettings(fonte);
        MoverTtf();
        int prefabs = AjustarPrefabs(fonte);
        int cenas = AjustarCenas(fonte);

        if (!string.IsNullOrEmpty(cenaAberta))
        {
            EditorSceneManager.OpenScene(cenaAberta, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Fonte] Irish Grover aplicada. Textos trocados: " + cenas + " em cenas, " + prefabs + " em prefabs.");
    }

    static TMP_FontAsset CarregarFonte()
    {
        string[] guids = AssetDatabase.FindAssets(NOME_ASSET + " t:TMP_FontAsset");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static void AjustarTmpSettings(TMP_FontAsset fonte)
    {
        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
        {
            Debug.LogWarning("[Fonte] TMP Settings nao encontrado. Use Window > TextMeshPro > Import TMP Essential Resources.");
            return;
        }

        SerializedObject so = new SerializedObject(settings);
        SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
        prop.objectReferenceValue = fonte;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(settings);
    }

    static void MoverTtf()
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(TTF_ORIGEM) == null) return;
        if (AssetDatabase.LoadAssetAtPath<Object>(TTF_DESTINO) != null) return;

        if (!AssetDatabase.IsValidFolder(PASTA_RESOURCES)) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PASTA_FONTES)) AssetDatabase.CreateFolder(PASTA_RESOURCES, "Fonts");

        string erro = AssetDatabase.MoveAsset(TTF_ORIGEM, TTF_DESTINO);
        if (!string.IsNullOrEmpty(erro)) Debug.LogWarning("[Fonte] Nao consegui mover o .ttf: " + erro);
    }

    static int AjustarPrefabs(TMP_FontAsset fonte)
    {
        int total = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

        foreach (string guid in guids)
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            GameObject raiz = PrefabUtility.LoadPrefabContents(caminho);

            int trocados = 0;
            foreach (TMP_Text t in raiz.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.font == fonte) continue;
                t.font = fonte;
                trocados++;
            }

            if (trocados > 0) PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
            PrefabUtility.UnloadPrefabContents(raiz);
            total += trocados;
        }

        return total;
    }

    static int AjustarCenas(TMP_FontAsset fonte)
    {
        int total = 0;
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        foreach (string guid in guids)
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            Scene cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

            int trocados = 0;
            TMP_Text[] textos = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (TMP_Text t in textos)
            {
                if (t.font == fonte) continue;
                t.font = fonte;
                EditorUtility.SetDirty(t);
                trocados++;
            }

            if (trocados > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            total += trocados;
        }

        return total;
    }
}