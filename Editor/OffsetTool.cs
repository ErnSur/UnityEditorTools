using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor;
using static UnityEditor.EditorGUILayout;
using static UnityEngine.GUILayout;
using HorizontalScope = UnityEditor.EditorGUILayout.HorizontalScope;
using VerticalScope = UnityEditor.EditorGUILayout.VerticalScope;
using System.Linq;

namespace QuickEye.EditorTools
{
    [EditorTool(_toolName)]
    class OffsetTool : EditorTool
    {
        private const string _toolName = "Offset Tool";

        [SerializeField]
        private Texture2D _toolIcon;

        private GUIContent _iconContent;

        [SerializeField]
        private TransformValues _offset;

        public override GUIContent toolbarIcon => _iconContent;

        public override void OnToolGUI(EditorWindow window)
        {
            DrawOffsetSection();
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
                        TransformObjectsBy(Selection.transforms);
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


        private void OnEnable()
        {
            _iconContent = new GUIContent()
            {
                image = _toolIcon,
                text = _toolName,
                tooltip = _toolName
            };
        }

        public enum ReplaceTransformation
        {
            UseOldValue,
            UseNewValue,
            AddNewValue
        }
    }
}
