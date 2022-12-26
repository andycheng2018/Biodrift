using System.Collections;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEngine;
#endif

#if UNITY_EDITOR
public class CaptureIcon : MonoBehaviour
{
    public string path;

    public void captureIcon()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var sprite = Sprite.Create(AssetPreview.GetAssetPreview(go), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 50);
            SaveTexture(AssetPreview.GetAssetPreview(sprite), go.name);
        }
    }
    private void SaveTexture(Texture2D texture, string name)
    {
        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/RenderOutput";
        if (!System.IO.Directory.Exists(dirPath))
        {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        System.IO.File.WriteAllBytes(dirPath + "/" + name + ".png", bytes);
        AssetDatabase.Refresh();
    }
}
#endif