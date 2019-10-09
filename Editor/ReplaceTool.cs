using UnityEditor;
using UnityEngine;
using UnityEditor.EditorTools;
using System;
using static UnityEditor.EditorGUILayout;
using static UnityEngine.GUILayout;
using HorizontalScope = UnityEditor.EditorGUILayout.HorizontalScope;
using VerticalScope = UnityEditor.EditorGUILayout.VerticalScope;
using System.Linq;

namespace QuickEye.EditorTools
{
    [CustomEditor(typeof(ReplaceTool))]
    public class ReplaceToolEditor : Editor
    {
        private ReplaceTool tool;

        private SerializedProperty sample, offset, useOffset, showPreview;
        private SerializedProperty posMod, rotMod, scaleMod;

        private void OnEnable()
        {
            tool = target as ReplaceTool;

            sample = serializedObject.FindProperty(nameof(tool.sample));
            offset = serializedObject.FindProperty(nameof(tool.offset));
            useOffset = serializedObject.FindProperty(nameof(tool.replaceWithOffset));
            showPreview = serializedObject.FindProperty(nameof(tool.showPreview));

            posMod = serializedObject.FindProperty("_positionMod");
            rotMod = serializedObject.FindProperty("_rotationMod");
            scaleMod = serializedObject.FindProperty("_scaleMod");
        }

        public override void OnInspectorGUI()
        {
            using (var s = new EditorGUI.ChangeCheckScope())
            {
                DrawReplaceSection();
                if (s.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawReplaceSection()
        {
            using (new VerticalScope())
            {
                PropertyField(posMod);
                PropertyField(rotMod);
                PropertyField(scaleMod);

                using (new HorizontalScope())
                {
                    PropertyField(sample);

                    using (new EditorGUI.DisabledScope(sample.objectReferenceValue == null))
                    {
                        if (Button("Replace Selected With sample"))
                        {
                            tool.Replace();
                        }
                    }
                }
                PropertyField(showPreview);

                PropertyField(useOffset);
                if (useOffset.boolValue)
                {
                    PropertyField(offset);

                    using (new EditorGUI.DisabledScope(Selection.transforms.Length != 2))
                    using (new HorizontalScope())
                    {
                        if (Button("Set Offset To Selection Difference"))
                        {
                            //offset.managedReferenceValue = TransformValues.GetDifference(Selection.transforms[0], Selection.transforms[1]);
                            tool.offset = TransformValues.GetDifference(Selection.transforms[0], Selection.transforms[1]);
                            EditorUtility.SetDirty(target);
                            serializedObject.Update();
                        }
                    }
                }
            }
        }
    }

    [EditorTool(_toolName)]
    class ReplaceTool : EditorToolPlus
    {
        private const string _toolName = "Replace Tool";

        [SerializeField]
        public GameObject sample, preview;

        [SerializeField]
        public bool replaceWithOffset, showPreview;

        [SerializeField]
        public TransformValues offset;

        [SerializeField]
        private TransformOperation _positionMod = TransformOperation.UseOldValue, _rotationMod = TransformOperation.UseNewValue, _scaleMod = TransformOperation.UseNewValue;

        protected override void OnEnable()
        {
            base.OnEnable();

            iconContent.text =
            iconContent.tooltip = _toolName;
        }

        protected override void OnActivate()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Active Tool");
            
            TogglePreviewObject(true);
        }

        protected override void OnDeactivate()
        {
            TogglePreviewObject(false);
        }

        private void TogglePreviewObject(bool value)
        {
            if(sample == null)
            {
                return;
            }
            Debug.Log($"Toggle {value}");
            if (value)
            {
                preview = InstantiateSample(null);
                //previewObj.hideFlags = HideFlags.HideAndDontSave;
            }
            else if(preview != null)
            {
                DestroyImmediate(preview);
            }
        }

        private GameObject InstantiateSample(Transform parent)
        {
            GameObject newObj;

            if (PrefabUtility.GetPrefabInstanceHandle(sample) != null)
            {
                newObj = PrefabUtility.InstantiatePrefab(sample, parent) as GameObject;
            }
            else
            {
                newObj = Instantiate(sample, parent) as GameObject;
            }

            return newObj;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if(Selection.transforms.Length < 1)
            {
                return;
            }
            if(showPreview && preview == null)
            {
                TogglePreviewObject(true);
            }
            if (preview)
            {
                var p = preview.transform;
                var t = Selection.transforms[0];
                p.parent = t.parent;
                p.localPosition = t.localPosition;
                p.localEulerAngles = t.localEulerAngles;
                p.localScale = t.localScale;
            }

            //EditorGUI.BeginChangeCheck();

            //Vector3 position = Tools.handlePosition;

            //using (new Handles.DrawingScope(Color.green))
            //{
            //    position = Handles.Slider(position, Vector3.right);
            //}

            //if (EditorGUI.EndChangeCheck())
            //{
            //    Vector3 delta = position - Tools.handlePosition;

            //    Undo.RecordObjects(Selection.transforms, "Move Platform");

            //    foreach (var transform in Selection.transforms)
            //        transform.position += delta;
            //}
        }


        public void Replace()
        {
            ReplaceObjectsWithPrefab(Selection.gameObjects, sample, replaceWithOffset ? offset : new TransformValues());
        }

        public void ReplaceObjectsWithPrefab(GameObject[] objects, GameObject sample, TransformValues offset)
        {
            Selection.objects = objects.Select(ReplaceWithPrefab).ToArray();

            GameObject ReplaceWithPrefab(GameObject obj)
            {
                var hierarchyIndex = obj.transform.GetSiblingIndex();

                GameObject newObj = InstantiateSample(obj.transform.parent);

                Undo.RegisterCreatedObjectUndo(newObj, "Instantiated Prefab");

                SetupTransformValues();

                Undo.DestroyObjectImmediate(obj);
                newObj.transform.SetSiblingIndex(hierarchyIndex);
                return newObj;

                void SetupTransformValues()
                {
                    newObj.transform.localPosition = GetNewTransformationValue(_positionMod, obj.transform.localPosition, newObj.transform.localPosition);
                    newObj.transform.localEulerAngles = GetNewTransformationValue(_rotationMod, obj.transform.localEulerAngles, newObj.transform.localEulerAngles);
                    newObj.transform.localScale = GetNewTransformationValue(_scaleMod, obj.transform.localScale, newObj.transform.localScale);

                    offset.AddTo(newObj.transform);
                }

                Vector3 GetNewTransformationValue(TransformOperation transformation, Vector3 oldValue, Vector3 newValue)
                {
                    switch (transformation)
                    {
                        default:
                        case TransformOperation.UseOldValue:
                            {
                                return oldValue;
                            }
                        case TransformOperation.UseNewValue:
                            {
                                return newValue;
                            }
                        case TransformOperation.AddNewValue:
                            {
                                return oldValue + newValue;
                            }
                    }
                }
            }
        }

        private void TransformObjectsBy(Transform[] objects)
        {
            foreach (var transform in objects)
            {
                Undo.RecordObject(transform, "Transformed object with offset");
                offset.AddTo(transform);
            }
        }

        public enum TransformOperation
        {
            UseOldValue,
            UseNewValue,
            AddNewValue
        }
    }
}
