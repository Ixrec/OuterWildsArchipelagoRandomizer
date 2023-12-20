using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using OWML.Common;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Fixes the layout of the console when they're enabled
    /// </summary>
    public class FixConsoleLayout : MonoBehaviour
    {
        private RectTransform rect;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            StartCoroutine(FixLayout());
        }

        IEnumerator FixLayout()
        {
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
}