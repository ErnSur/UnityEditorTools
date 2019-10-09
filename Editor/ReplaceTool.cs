using UnityEditor;
using UnityEngine;
using UnityEditor.EditorTools;
using System;
using static UnityEditor.EditorGUILayout;
using static UnityEngine.GUILayout;
using HorizontalScope = UnityEditor.EditorGUILayout.HorizontalScope;
using VerticalScope = UnityEditor.EditorGUILayout.VerticalScope;

namespace QuickEye.EditorTools
{
    [EditorTool(_toolName)]
    class ReplaceTool : EditorTool
    {
        private const string _toolName = "Replace Tool";

        [SerializeField]
        Texture2D _toolIcon;

        GUIContent _iconContent;

        void OnEnable()
        {
            Debug.Log("Enable Replace Tool");
            _iconContent = new GUIContent()
            {
                image = _toolIcon,
                text = _toolName,
                tooltip = _toolName
            };
        }

        public override GUIContent toolbarIcon
        {
            get { return _iconContent; }
        }

        // This is called for each window that your tool is active in. Put the functionality of your tool here.
        public override void OnToolGUI(EditorWindow window)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 position = Tools.handlePosition;

            using (new Handles.DrawingScope(Color.green))
            {
                position = Handles.Slider(position, Vector3.right);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = position - Tools.handlePosition;

                Undo.RecordObjects(Selection.transforms, "Move Platform");

                foreach (var transform in Selection.transforms)
                    transform.position += delta;
            }
        }

        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private TransformValues _offset;

        [SerializeField]
        private ReplaceTransformation _positionMod, _rotationMod, _scaleMod;

        [SerializeField]
        private bool _replaceWithOffset;

        private void OnMenuGUI()
        {
            DrawOffsetSection();
            DrawReplaceSection();
        }

        private void DrawReplaceSection()
        {
            using (new VerticalScope("box"))
            {
                DrawTransformModificationSection();
                _replaceWithOffset = Toggle("Use Offset", _replaceWithOffset);
                using (new HorizontalScope())
                {
                    _prefab = ObjectField(_prefab, typeof(GameObject), false) as GameObject;

                    using (new EditorGUI.DisabledScope(_prefab == null))
                    {
                        if (Button("Replace Selected With Prefab"))
                        {
                            ReplaceObjectsWithPrefab(Selection.gameObjects, _prefab, _replaceWithOffset ? _offset : new TransformValues());
                        }
                    }
                }
            }
        }

        private void DrawOffsetSection()
        {
            using (new VerticalScope("box"))
            {
                Label("Offset");
                _offset.position = Vector3Field("position", _offset.position);
                _offset.rotation = Vector3Field("rotation", _offset.rotation);
                _offset.scale = Vector3Field("scale", _offset.scale);

                using (new HorizontalScope())
                {
                    if (Button("Set Offset To Selection Difference") && Selection.transforms.Length == 2)
                    {
                        _offset = TransformValues.GetDifference(Selection.transforms[0], Selection.transforms[1]);
                    }
                    if (Button("Add Offset To Selection"))
                    {
                        TransformObjectsBy(Selection.gameObjects.Select(o => o.transform).ToArray());
                    }
                }
            }
        }

        private void DrawTransformModificationSection()
        {
            using (new HorizontalScope())
            {
                DrawModificationTemplate("Position", ref _positionMod);
                DrawModificationTemplate("Rotation", ref _rotationMod);
                DrawModificationTemplate("Scale", ref _scaleMod);
            }
            void DrawModificationTemplate(string label, ref ReplaceTransformation mod)
            {
                using (new VerticalScope())
                {
                    PrefixLabel(label);
                    mod = (ReplaceTransformation)EnumPopup(mod);
                }
            }
        }

        private void ReplaceObjectsWithPrefab(GameObject[] objects, GameObject prefab, TransformValues offset)
        {
            Selection.objects = objects.Select(ReplaceWithPrefab).ToArray();

            GameObject ReplaceWithPrefab(GameObject obj)
            {
                var hierarchyIndex = obj.transform.GetSiblingIndex();
                var newObj = PrefabUtility.InstantiatePrefab(prefab, obj.transform.parent) as GameObject;
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

                Vector3 GetNewTransformationValue(ReplaceTransformation transformation, Vector3 oldValue, Vector3 newValue)
                {
                    switch (transformation)
                    {
                        default:
                        case ReplaceTransformation.UseOldValue:
                            {
                                return oldValue;
                            }
                        case ReplaceTransformation.UseNewValue:
                            {
                                return newValue;
                            }
                        case ReplaceTransformation.AddNewValue:
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
                _offset.AddTo(transform);
            }
        }

        [Flags]
        public enum ReplaceTransformation
        {
            UseOldValue,
            UseNewValue,
            AddNewValue
        }
    }

    [Serializable]
    public struct TransformValues
    {
        public Vector3 position, rotation, scale;

        public TransformValues(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public TransformValues(Transform transform) : this(transform.position, transform.eulerAngles, transform.localScale)
        {
        }

        public static TransformValues GetDifference(Transform lhs, Transform rhs)
        {
            return new TransformValues
            {
                position = lhs.localPosition - rhs.localPosition,
                rotation = lhs.localEulerAngles - rhs.localEulerAngles,
                scale = lhs.localScale - rhs.localScale
            };
        }

        public void AddTo(Transform transform, bool local = true)
        {
            if (local)
            {
                transform.localPosition += position;
                transform.localEulerAngles += rotation;
                transform.localScale += scale;
            }
            else
            {
                transform.position += position;
                transform.rotation *= Quaternion.Euler(rotation);
                transform.localScale += scale;
            }
        }

        public void SetTo(Transform transform, bool local = true)
        {
            if (local)
            {
                transform.localPosition = position;
                transform.localEulerAngles = rotation;
                transform.localScale = scale;
            }
            else
            {
                transform.position = position;
                transform.rotation = Quaternion.Euler(rotation);
                transform.localScale = scale;
            }
        }

        public static implicit operator TransformValues(Transform t)
        {
            return new TransformValues(t);
        }
    }
}
