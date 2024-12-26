using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cortex
{
    /// <summary>
    /// Helper functions for GameObject related functionality
    /// </summary>
    public static class GameObjectUtils
    {

        /// <summary>
        /// Finds the first child game object of the given transform by name.
        /// </summary>
        /// <param name="transform">The parent transform</param>
        /// <param name="name">The name to search for</param>
        /// <param name="recursive">If true, the transform hierarchy is traversed, otherwise only one layer is checked</param>
        /// <returns>The first game object with the given name. Null, if it does not exist</returns>
        public static GameObject FindGameObjectByName(Transform transform, string name, bool recursive = false)
        {
            var o = transform.Find(name);
            if (o != null)
            {
                return o.gameObject;
            }

            if (recursive)
            {
                foreach (Transform c in transform)
                {
                    var oc = FindGameObjectByName(c, name, recursive);
                    if (oc != null)
                    {
                        return oc;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This method will instantiate a prefab of a given type in an inactive state such that it was not activated before, as some scripts require certain fields to be set before initialization.
        /// Note: This will only affect GameObject and MonoBehaviour types. Other types may be instantiated this way, but are not affected by activity status.
        /// MonoBehaviours will have their associated GameObject inactive.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate</param>
        /// <returns>The instantiated object.</returns>
        public static T InstantiateInactive<T>(T prefab) where T : UnityEngine.Object
        {
            // if we instantiate with a deactivated parent, the instantiated object will not start immediately
            GameObject dummy = new();
            dummy.SetActive(false);

            T result = UnityEngine.Object.Instantiate(prefab, dummy.transform);

            // while you can instantiate unity objects, you can't activate them all or in the same way

            // after the deactivated instantiation we set the created object itself inactive and then remove the unneeded parent 
            if (result is GameObject o)
            {
                o.SetActive(false);
                o.transform.SetParent(null);
                UnityEngine.Object.Destroy(dummy);
            }
            else if (result is MonoBehaviour m)
            {
                GameObject obj = m.gameObject;
                obj.SetActive(false);
                obj.transform.SetParent(null);
                UnityEngine.Object.Destroy(dummy);
            }

            return result;
        }

        /// <summary>
        /// Finds the first object of the given type, which may be an interface or abstract class.
        /// This methods needs to check all current object, so it is pretty slow. Don't call it in performance critical paths.
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <returns>The first object found or the default type value, if not</returns>
        public static T FindFirstGenericGameObject<T>()
        {
            return FindGenericGameObject<T>().FirstOrDefault();
        }
        /// <summary>
        /// Finds all objects of the given type, which may be an interface or abstract class.
        /// This methods needs to check all current object, so it is pretty slow. Don't call it in performance critical paths.
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <returns>The objects found that match the given type</returns>
        public static List<T> FindGenericGameObject<T>()
        {
            return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().Where(o => o is T).Cast<T>().ToList();
        }

        /// <summary>
        /// Finds the first GameObject satisfying a predicate. 
        /// In contrast to the GameObject.Find method, this will also find inactive game objects.
        /// For performance reasons, this should not be called every frame
        /// </summary>
        /// <param name="predicate">A predicate to check whether a given object satisfies a condtion</param>
        /// <returns>The first GameObject satisfying the predicate. Null, if it does not exist</returns>
        public static GameObject FindAnyGameObject(Predicate<GameObject> predicate)
        {
            return Array.Find(Resources.FindObjectsOfTypeAll<GameObject>(), predicate);
        }

        /// <summary>
        /// Finds the first GameObject with the given name. 
        /// In contrast to the GameObject.Find method, this will also find inactive game objects.
        /// For performance reasons, this should not be called every frame
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>The first GameObject satisfying the predicate. Null, if it does not exist</returns>
        public static GameObject FindAnyGameObjectByName(string name)
        {
            return FindAnyGameObject(o => o.name == name);
        }

        /// <summary>
        /// Traverse a GameObject hierarchy depth-first and call the given function for each encountered object.
        /// This included the object itself.
        /// </summary>
        /// <param name="o">The object to be traversed. If null, nothing happens</param>
        /// <param name="action">The action to be called for each object in the hierarchy. If null, nothing happens</param>
        public static void Traverse(GameObject o, Action<GameObject> action)
        {
            if (action == null || o == null)
            {
                return;
            }
            action(o);
            foreach (Transform t in o.transform)
            {
                Traverse(t.gameObject, action);
            }
        }
    }
} // end namespace Cortex