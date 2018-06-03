using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

using Agugu.Runtime;

namespace Agugu.Editor
{
    public class UguiTreeMigrator
    {
        public static void MigrateAppliedPrefabModification
        (
            GameObject sourceGameObjectRoot,
            GameObject targetGameObjectRoot
        )
        {
            Dictionary<int, GameObject> layerIdToSourceMapping =
                _BuildLayerIdToGameObjectMapping(sourceGameObjectRoot);
            Dictionary<int, GameObject> layerIdToTargetMapping =
                _BuildLayerIdToGameObjectMapping(targetGameObjectRoot);
            Dictionary<GameObject, GameObject> importedGameObjectMapping =
                _BuildSourceToTargetMapping(layerIdToSourceMapping, layerIdToTargetMapping);

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
        }

        private static Dictionary<int, GameObject> _BuildLayerIdToGameObjectMapping(GameObject root)
        {
            var psdLayerIdTagArray = root.GetComponentsInChildren<PsdLayerIdTag>();

            var mapping = new Dictionary<int, GameObject>();
            foreach (PsdLayerIdTag layerIdTag in psdLayerIdTagArray)
            {
                mapping.Add(layerIdTag.LayerId, layerIdTag.gameObject);
            }

            return mapping;
        }

        private static Dictionary<GameObject, GameObject> _BuildSourceToTargetMapping
        (
            Dictionary<int, GameObject> layerIdToSourceMapping,
            Dictionary<int, GameObject> layerIdToTargetMapping
        )
        {
            var joinResult =
                from sourceEntry in layerIdToSourceMapping
                join targetEntry in layerIdToTargetMapping on sourceEntry.Key equals targetEntry.Key
                select new KeyValuePair<GameObject, GameObject>(sourceEntry.Value, targetEntry.Value);

            var importedGameObjectMapping = new Dictionary<GameObject, GameObject>();
            foreach (var joinPair in joinResult)
            {
                importedGameObjectMapping.Add(joinPair.Key, joinPair.Value);
            }

            return importedGameObjectMapping;
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

        private static void _RetargetObjectReference
        (
            GameObject                         targetGameObjectRoot,
            Dictionary<GameObject, GameObject> gameObjectMapping,
            Dictionary<Component, Component>   componentMapping
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
}