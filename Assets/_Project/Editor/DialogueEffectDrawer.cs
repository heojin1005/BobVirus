#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueEffect))]
public class DialogueEffectDrawer : PropertyDrawer
{
    private const float V = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var typeProp = property.FindPropertyRelative("type");

        float y = position.y;
        float w = position.width;

        // Foldout
        Rect foldRect = new Rect(position.x, y, w, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);
        y += EditorGUIUtility.singleLineHeight + V;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            DrawField(ref y, position.x, w, typeProp);

            var type = (DialogueEffectType)typeProp.enumValueIndex;

            switch (type)
            {
                case DialogueEffectType.StoryProgressAdd:
                case DialogueEffectType.StoryProgressSet:
                case DialogueEffectType.TestAdd:
                case DialogueEffectType.TestSet:
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("intValue"));
                    break;

                case DialogueEffectType.TalkChange:
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("objectName"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("NodeName"));
                    break;

                case DialogueEffectType.QuestChange:
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("objectName"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("QuestNode"));
                    break;

                case DialogueEffectType.StoreAdd:
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("objectName"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("storeTakeItemId"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("storeTakeCount"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("storeGiveItemId"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("storeGiveCount"));
                    break;

                case DialogueEffectType.ShowDebug:
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("debugObject"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("stringValue"), true);
                    break;

                case DialogueEffectType.Give:
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("inventoryUI"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("takeItemId"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("takeCount"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("giveItemId"));
                    DrawField(ref y, position.x, w, property.FindPropertyRelative("giveCount"));
                    break;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUIUtility.singleLineHeight + V;

        if (!property.isExpanded)
            return h;

        var typeProp = property.FindPropertyRelative("type");
        var type = (DialogueEffectType)typeProp.enumValueIndex;

        h += GetFieldHeight(typeProp);

        switch (type)
        {
            case DialogueEffectType.StoryProgressAdd:
            case DialogueEffectType.StoryProgressSet:
            case DialogueEffectType.TestAdd:
            case DialogueEffectType.TestSet:
                h += GetFieldHeight(property.FindPropertyRelative("intValue"));
                break;

            case DialogueEffectType.TalkChange:
                h += GetFieldHeight(property.FindPropertyRelative("objectName"));
                h += GetFieldHeight(property.FindPropertyRelative("NodeName"));
                break;

            case DialogueEffectType.QuestChange:
                h += GetFieldHeight(property.FindPropertyRelative("objectName"));
                h += GetFieldHeight(property.FindPropertyRelative("QuestNode"));
                break;

            case DialogueEffectType.StoreAdd:
                h += GetFieldHeight(property.FindPropertyRelative("objectName"));
                h += GetFieldHeight(property.FindPropertyRelative("storeTakeItemId"));
                h += GetFieldHeight(property.FindPropertyRelative("storeTakeCount"));
                h += GetFieldHeight(property.FindPropertyRelative("storeGiveItemId"));
                h += GetFieldHeight(property.FindPropertyRelative("storeGiveCount"));
                break;

            case DialogueEffectType.ShowDebug:
                h += GetFieldHeight(property.FindPropertyRelative("debugObject"));
                h += GetFieldHeight(property.FindPropertyRelative("stringValue"), true);
                break;

            case DialogueEffectType.Give:
                h += GetFieldHeight(property.FindPropertyRelative("inventoryUI"));
                h += GetFieldHeight(property.FindPropertyRelative("takeItemId"));
                h += GetFieldHeight(property.FindPropertyRelative("takeCount"));
                h += GetFieldHeight(property.FindPropertyRelative("giveItemId"));
                h += GetFieldHeight(property.FindPropertyRelative("giveCount"));
                break;
        }

        return h;
    }

    private void DrawField(ref float y, float x, float width, SerializedProperty prop, bool includeChildren = false)
    {
        float h = EditorGUI.GetPropertyHeight(prop, includeChildren);
        Rect r = new Rect(x, y, width, h);
        EditorGUI.PropertyField(r, prop, includeChildren);
        y += h + V;
    }

    private float GetFieldHeight(SerializedProperty prop, bool includeChildren = false)
    {
        return EditorGUI.GetPropertyHeight(prop, includeChildren) + V;
    }
}
#endif