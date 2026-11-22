using UnityEditor;
using UnityEditor.UI;

// the button inspector plus the theme fields
[CustomEditor(typeof(UIButton), true)]
[CanEditMultipleObjects]
public class UIButtonEditor : ButtonEditor
{
    public override void OnInspectorGUI() {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("style"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sound"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("background"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("label"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("accent"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lit"));
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
