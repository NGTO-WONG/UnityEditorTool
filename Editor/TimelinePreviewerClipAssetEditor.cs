using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;

[CustomEditor(typeof(TimelinePreviewerClip))]
public class TimelinePreviewerClipAssetEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("复制"))
        {
            CopyVariablesToClipboard();
        }
        if (GUILayout.Button("粘贴"))
        {
            PasteVariablesFromClipboard();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void CopyVariablesToClipboard()
    {
        TimelinePreviewerClip myAsset = (TimelinePreviewerClip)target;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Prefab: " + AssetDatabase.GetAssetPath(myAsset.prefab));
        sb.AppendLine("Rotation: " + myAsset.rotation.ToString());
        sb.AppendLine("Position: " + myAsset.position.ToString());
        sb.AppendLine("Scale: " + myAsset.scale.ToString());
        sb.AppendLine("ParticleTimeOffset: " + myAsset.particleTimeOffset.ToString());

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Variables copied to clipboard.");
    }

    
    private void PasteVariablesFromClipboard()
    {
        string clipboard = GUIUtility.systemCopyBuffer;
        TimelinePreviewerClip myAsset = (TimelinePreviewerClip)target;
        Undo.RecordObject(myAsset, "Paste Variables");

        string[] lines = clipboard.Split('\n');
        foreach (string line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length >= 2)
            {
                string fieldName = parts[0].Trim();
                string fieldValue = parts[1].Trim();

                if (fieldName == "Parent")
                {
                    //myAsset.parent = tt;
                }
                else if (fieldName == "Prefab")
                {
                    myAsset.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fieldValue);
                }
                else if (fieldName == "Rotation" || fieldName == "Position" || fieldName == "Scale")
                {
                    Vector3 vectorValue = StringToVector3(fieldValue);
                    typeof(TimelinePreviewerClip).GetField(fieldName.ToLower()).SetValue(myAsset, vectorValue);
                }
                else if (fieldName == "ParticleTimeOffset")
                {
                    float floatValue = float.Parse(fieldValue);
                    myAsset.particleTimeOffset = floatValue;
                }
            }
        }

        EditorUtility.SetDirty(myAsset); // 标记资产已被修改
    }

    private Vector3 StringToVector3(string sVector)
    {
        // 去掉括号
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // 分割
        string[] sArray = sVector.Split(',');

        // 存储为 Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

    
    private string CopyExposedReferenceTransform(ExposedReference<Transform> exposedTransform, PlayableDirector director)
    {
        Transform transform = exposedTransform.Resolve(director.playableGraph.GetResolver());
        return transform != null ? AnimationUtility.CalculateTransformPath(transform, null) : "";
    }

    private void PasteExposedReferenceTransform(string path, ref ExposedReference<Transform> exposedTransform, PlayableDirector director)
    {
        if (!string.IsNullOrEmpty(path))
        {
            Transform transform = FindTransformByPath(path);
            if (transform != null)
            {
                exposedTransform.exposedName = UnityEditor.GUID.Generate().ToString();
                director.SetReferenceValue(exposedTransform.exposedName, transform);
            }
        }
    }

    private Transform FindTransformByPath(string path)
    {
        return GameObject.Find(path)?.transform;
    }

    
    
}