
using System;
using UnityEngine;

namespace Cortex
{

    /// <summary>
    /// Empty message that can be used to easily signify a signal without content
    /// </summary>
    [Serializable]
    public struct MsgEmpty
    {

    }

    /// <summary>
    /// A message containing a single value type
    /// </summary>
    /// <typeparam name="T">The type to be stored. Must be serializable</typeparam>
    [Serializable]
    public struct MsgValue<T>
    {
        public T value;

        public MsgValue(T value)
        {
            this.value = value;
        }
    }

    /// <summary>
    /// A message containing an array for single value type
    /// </summary>
    /// <typeparam name="T">The type to be stored. Must be serializable</typeparam>
    [Serializable]
    public struct MsgArray<T>
    {
        public T[] values;

        public MsgArray(T[] values)
        {
            this.values = values;
        }
    }

    /// <summary>
    /// A message containing a pair of values.
    /// </summary>
    /// <typeparam name="T1">The first type. Must be serializable</typeparam>
    /// <typeparam name="T2">The second type. Must be serializable</typeparam>
    [Serializable]
    public struct MsgPair<T1, T2>
    {
        public T1 first;
        public T2 second;

        public MsgPair(T1 first, T2 second)
        {
            this.first = first;
            this.second = second;
        }
    }

    /// <summary>
    /// Serializable version of a simple transformation consisting of a position, rotation and scale.
    /// </summary>

    [Serializable]
    public struct Transformation
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public Transformation(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
        public Transformation(Transform t)
        {
            position = t.position;
            rotation = t.rotation;
            scale = t.localScale;
        }

        private static readonly Transformation identityTransform = new(Vector3.zero, Quaternion.identity, Vector3.one);

        public static Transformation Identity
        {
            get
            {
                return identityTransform;
            }
        }

    }

} // end namespace Cortex