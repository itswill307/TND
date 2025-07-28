using UnityEditor;
using UnityEngine;

public class MeshReadWriteEnabler
{
    [MenuItem("Tools/Enable ReadWrite for All Models")]
    static void EnableReadWrite()
    {
        // Adjust the path to your model folder
        string[] assetGUIDs = AssetDatabase.FindAssets("t:Model", new[] {"Assets/Models"});
        foreach (string guid in assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log($"Enabled Read/Write on: {path}");
            }
        }
        Debug.Log("Read/Write enabling complete.");
    }
}
