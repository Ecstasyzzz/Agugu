using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class UguiTreeMigrator
{
    private static readonly PropertyInfo InspectorModeInfo =
        typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void MigrateAppliedPrefabModification
    (
        GameObject sourcePrefabRoot,
        GameObject targetGameObjectRoot
    )
    {
        var sourceGameObjectRoot = GameObject.Instantiate(sourcePrefabRoot);

        Dictionary<int, GameObject> sourceMapping =
            _BuildLayerIdToGameObjectMapping(sourceGameObjectRoot);
        Dictionary<int, GameObject> targetMapping =
            _BuildLayerIdToGameObjectMapping(targetGameObjectRoot);
        var joinResult =
            from sourceEntry in sourceMapping
            join targetEntry in targetMapping on sourceEntry.Key equals targetEntry.Key
            select new KeyValuePair<GameObject, GameObject>(sourceEntry.Value, targetEntry.Value);

        var importedGameObjectMapping = new Dictionary<GameObject, GameObject>();
        foreach(var joinPair in joinResult)
        {
            importedGameObjectMapping.Add(joinPair.Key, joinPair.Value);
        }

        foreach (KeyValuePair<GameObject, GameObject> gameObjectPair in importedGameObjectMapping)
        {
            GameObject source = gameObjectPair.Key;
            GameObject target = gameObjectPair.Value;

            _MoveNonImportedGameObjects(source, target);
        }

        var componentMapping = new Dictionary<Component, Component>();
        foreach (KeyValuePair<GameObject, GameObject> gameObjectPair in importedGameObjectMapping)
        {
            GameObject source = gameObjectPair.Key;
            GameObject target = gameObjectPair.Value;

            _CopyNonImportedComponents(source, target, componentMapping);
        }

        foreach (KeyValuePair<GameObject, GameObject> gameObjectPair in importedGameObjectMapping)
        {
            GameObject source = gameObjectPair.Key;
            GameObject target = gameObjectPair.Value;

            _AddPairedComponent<RectTransform>(source, target, componentMapping);
            _AddPairedComponent<CanvasRenderer>(source, target, componentMapping);
            _AddPairedComponent<Image>(source, target, componentMapping);
            _AddPairedComponent<Text>(source, target, componentMapping);
        }

        _RetargetObjectReference(targetGameObjectRoot, importedGameObjectMapping, componentMapping);

        GameObject.DestroyImmediate(sourceGameObjectRoot);
    }

    private static Dictionary<int, GameObject> _BuildLayerIdToGameObjectMapping(GameObject root)
    {
        var psdLayerIdTagList = root.GetComponentsInChildren<PsdLayerIdTag>();

        var mapping = new Dictionary<int, GameObject>();
        foreach (PsdLayerIdTag layerIdTag in psdLayerIdTagList)
        {
            mapping.Add(layerIdTag.LayerId, layerIdTag.gameObject);
        }

        return mapping;
    }

    private static void _MoveNonImportedGameObjects(GameObject source, GameObject target)
    {
        var gameObjectsShouldMove = new List<GameObject>();
        foreach (Transform child in source.transform)
        {
            if (child.GetComponent<PsdLayerIdTag>() == null)
            {
                gameObjectsShouldMove.Add(child.gameObject);
            }
        }

        gameObjectsShouldMove.ForEach(go => go.transform.SetParent(target.transform));
    }

    private static void _CopyNonImportedComponents
    (
        GameObject                       source,
        GameObject                       target,
        Dictionary<Component, Component> componentMapping
    )
    {
        _CopyLocalId(source, target);

        var sourceComponents = source.GetComponents<Component>();
        foreach (var sourceComponent in sourceComponents)
        {
            if (!(sourceComponent is Graphic) &&
                !(sourceComponent is CanvasRenderer) &&
                !(sourceComponent is Canvas) &&
                !(sourceComponent is CanvasScaler) &&
                !(sourceComponent is RectTransform) &&
                !(sourceComponent is GraphicRaycaster) &&
                !(sourceComponent is PsdLayerIdTag))
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(sourceComponent);
                var targetComponent = target.AddComponent(sourceComponent.GetType());
                if (targetComponent != null)
                {
                    
                    UnityEditorInternal.ComponentUtility.PasteComponentValues(targetComponent);
                    componentMapping.Add(sourceComponent, targetComponent);
                }
                else
                {
                    Debug.LogErrorFormat("UguiTreeMigrator copy component failed at {0} type: {1}",
                                         sourceComponent.gameObject.name, sourceComponent.GetType());
                }
            }
        }
    }

    private static void _AddPairedComponent<T>
    (
        GameObject                       source,
        GameObject                       target,
        Dictionary<Component, Component> pairDictionary
    )
        where T : Component
    {
        var sourceComponent = source.GetComponent<T>();
        var targetComponent = target.GetComponent<T>();
        if (sourceComponent != null && targetComponent != null)
        {
            pairDictionary.Add(sourceComponent, targetComponent);
        }
    }

    private static void _CopyLocalId(UnityEngine.Object source, UnityEngine.Object target)
    {
        var sourceSerializedObject = new SerializedObject(source);
        InspectorModeInfo.SetValue(sourceSerializedObject, InspectorMode.Debug, null);
        SerializedProperty sourceLocalIdSerializedProperty = 
            sourceSerializedObject.FindProperty("m_LocalIdentfierInFile");
        long sourceLocalIdValue = sourceLocalIdSerializedProperty.longValue;

        var targetSerializedObject = new SerializedObject(target);
        InspectorModeInfo.SetValue(targetSerializedObject, InspectorMode.Debug, null);
        SerializedProperty targetLocalIdSerializedProperty =
            targetSerializedObject.FindProperty("m_LocalIdentfierInFile");
        targetLocalIdSerializedProperty.longValue = sourceLocalIdValue;
        targetSerializedObject.ApplyModifiedProperties();
    }

    private static void _RetargetObjectReference
    (
        GameObject targetGameObjectRoot,
        Dictionary<GameObject, GameObject> gameObjectMapping,
        Dictionary<Component, Component> componentMapping
    )
    {
        Component[] allTargetComponents = targetGameObjectRoot.GetComponentsInChildren<Component>();
        foreach (Component targetComponent in allTargetComponents)
        {
            _RetargetObjectReferenceOnComponent(targetComponent, gameObjectMapping, componentMapping);
        }
    }

    private static void _RetargetObjectReferenceOnComponent
    (
        Component                          targetComponent,
        Dictionary<GameObject, GameObject> gameObjectMapping,
        Dictionary<Component, Component>   componentMapping
    )
    {
        var targetSerializedObject = new SerializedObject(targetComponent);
        SerializedProperty targetSerializedPropertyIterator = targetSerializedObject.GetIterator();
        while (targetSerializedPropertyIterator.Next(true))
        {
            if (targetSerializedPropertyIterator.propertyType == SerializedPropertyType.ObjectReference &&
                !string.Equals(targetSerializedPropertyIterator.name, "m_PrefabParentObject") &&
                !string.Equals(targetSerializedPropertyIterator.name, "m_PrefabInternal") &&
                !string.Equals(targetSerializedPropertyIterator.name, "m_GameObject") &&
                !string.Equals(targetSerializedPropertyIterator.name, "m_Script"))
            {
                UnityEngine.Object objectReference = targetSerializedPropertyIterator.objectReferenceValue;
                if (objectReference is GameObject)
                {
                    var gameObjectReference = objectReference as GameObject;
                    if (gameObjectMapping.ContainsKey(gameObjectReference))
                    {
                        targetSerializedPropertyIterator.objectReferenceValue = 
                            gameObjectMapping[gameObjectReference];
                    }
                }
                else if (objectReference is Component)
                {
                    var componentReference = objectReference as Component;
                    if (componentMapping.ContainsKey(componentReference))
                    {
                        targetSerializedPropertyIterator.objectReferenceValue =
                            componentMapping[componentReference];
                    }
                }
            }
        }

        targetSerializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}