#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CelestBody))]
public class CelestBodyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CelestBody body = (CelestBody)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

        if (GUILayout.Button("Randomize Test Body"))
        {
            body.RandomizeTestBody();
            EditorUtility.SetDirty(body);
            if (body.gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            }
        }

        if (GUILayout.Button("Log Description"))
        {
            Debug.Log(body.GetDescription());
        }
    }
}
#endif
