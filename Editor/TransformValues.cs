using UnityEngine;
using System;

namespace QuickEye.EditorTools
{
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
