using UnityEngine;
using UnityEditor.EditorTools;
using Tools = UnityEditor.EditorTools.EditorTools;

namespace QuickEye.EditorTools
{
    public abstract class EditorToolPlus : EditorTool
    {
        [SerializeField, HideInInspector]
        private Texture2D _toolIcon;

        protected GUIContent iconContent;

        public override GUIContent toolbarIcon => iconContent;

        protected virtual void OnEnable()
        {
            iconContent = new GUIContent()
            {
                image = _toolIcon
            };
            Tools.activeToolChanging += ActiveToolChanging;
            Tools.activeToolChanged += ActiveToolChanged;
        }

        protected virtual void OnDisable()
        {
            Tools.activeToolChanging -= ActiveToolChanging;
            Tools.activeToolChanged -= ActiveToolChanged;
        }

        private void ActiveToolChanging()
        {
            if (Tools.IsActiveTool(this))
                OnDeactivate();
        }

        private void ActiveToolChanged()
        {
            if (Tools.IsActiveTool(this))
                OnActivate();
        }

        protected virtual void OnActivate()
        {
        }

        protected virtual void OnDeactivate()
        {
        }
    }
}
