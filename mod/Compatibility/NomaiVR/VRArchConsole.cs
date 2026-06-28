using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// Console compatibility for the NomaiVR mod
/// </summary>
// TODO: One known issue that currently remains: it's not possible to enter text into the console in VR.
public class VRArchConsole
{
    public readonly bool vrLoaded = VRCompatibility.IsNomaiVRLoaded;

    // The geometry of the first console we create under VR.
    // When reloading (for example after resetting the loop) NomaiVR mutates our prefab for the ArchRandoCanvas,
    // so we need to restore the geometry on every subsequent creation of the console.
    private CachedGeometry cachedConsoleGeometry;

    private static readonly Type NomaiVrFollowTargetType =
        Type.GetType("NomaiVR.ReusableBehaviours.FollowTarget, NomaiVR");

    private class CachedGeometry
    {
        internal Vector2 pivot;
        internal Vector2 sizeDelta;
        internal Vector2 anchorMin;
        internal Vector2 anchorMax;
        internal Vector2 anchoredPosition;
    }

    public void Update(GameObject console)
    {
        if (NomaiVrFollowTargetType != null)
        {
            // Remove NomaiVR's FollowTarget, so just our own VRConsoleFollow remains. NomaiVR scans on scene load and
            // adds this component, but we can't reliably time this, so we remove it every frame.
            // XXX: would of course be better to remove it just when needed.
            var nomaiFollow = console.GetComponent(NomaiVrFollowTargetType);
            if (nomaiFollow != null) Object.Destroy(nomaiFollow);
        }

        // NomaiVR doesn't really expect objects to be used both in gameplay and in menus and moved between them
        // (this also causes a bunch of other issues, see SetUpConsoleForVR), so we need to make sure we re-activate
        // the object if NomaiVR has deactivated it due to a pause->gameplay transition.
        if (!console.activeSelf) console.SetActive(true);
    }

    // Setup console for the VR world space & tracking
    // 
    // NomaiVR makes VR work by scanning for ScreenSpaceOverlay canvases once per scene load and
    // converting them to world space, but this breaks when pausing or scene reloading, since that logic essentially
    // only runs once. To avoid these issues, we convert the console to world space ourselves but this also means we
    // must take care of positioning the canvas, which this component does.
    //
    // We also add our own tracking component (VRConsoleFollow). We need to add our own, since when we init the console
    // the camera doesn't exist yet, but NomaiVR's FollowTarget tries to cache it along other issues, leading to it
    // not actually showing.
    // 
    // It's also probably best not to rely on NomaiVR implementation details when we end up positioning the console
    // ourselves anyway (especially with the pause menu interactions & the whole re-create on scene load).
    public void SetUpConsoleForVR(GameObject console)
    {
        var rootCanvas = console.GetComponent<Canvas>();
        if (rootCanvas == null) rootCanvas = console.GetComponentInChildren<Canvas>(true);
        if (rootCanvas == null)
        {
            APRandomizer.OWMLModConsole.WriteLine("Could not find a Canvas on the AP console to set up for VR.",
                OWML.Common.MessageType.Warning);
            return;
        }

        foreach (var c in console.GetComponentsInChildren<Canvas>(true))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                c.renderMode = RenderMode.WorldSpace;

        // Mirror NomaiVR's AdjustScaler for in-game canvases (constant pixel size, then shrink the whole canvas).
        var scaler = rootCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1;
            scaler.referencePixelsPerUnit = 100;
        }

        rootCanvas.transform.localScale = Vector3.one * 0.001f; // ~1px → 1mm in world space

        // Restore geometry to the initial prefab values, see comment at cachedConsoleGeometry
        var rt = rootCanvas.GetComponent<RectTransform>();
        if (cachedConsoleGeometry == null)
        {
            cachedConsoleGeometry = new CachedGeometry
            {
                pivot = rt.pivot,
                sizeDelta = rt.sizeDelta,
                anchorMin = rt.anchorMin,
                anchorMax = rt.anchorMax,
                anchoredPosition = rt.anchoredPosition,
            };
        }
        else
        {
            rt.anchorMin = cachedConsoleGeometry.anchorMin;
            rt.anchorMax = cachedConsoleGeometry.anchorMax;
            rt.pivot = cachedConsoleGeometry.pivot;
            rt.sizeDelta = cachedConsoleGeometry.sizeDelta;
            rt.anchoredPosition = cachedConsoleGeometry.anchoredPosition;
        }

        if (rootCanvas.GetComponent<VRConsoleFollow>() == null)
            rootCanvas.gameObject.AddComponent<VRConsoleFollow>();
    }
}