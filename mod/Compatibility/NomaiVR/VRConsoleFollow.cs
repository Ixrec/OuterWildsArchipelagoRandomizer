using UnityEngine;

namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// Keeps the in-game AP console (converted to a world-space canvas) positioned in front of the player in VR.
///
/// This class does not cache the camera unlike NomaiVR's `FollowTarget`. See VRArchConsoleCompatibility on
/// why that's important.
/// </summary>
public class VRConsoleFollow : MonoBehaviour
{
    // Offset relative to the player transform. Mirrors NomaiVR's Menus.AddFollowTarget in-game placement;
    // could be adjusted to tweak the positioning
    public Vector3 localPosition = new Vector3(0f, 0.75f, 1.5f);

    private Transform target;

    private void OnEnable()
    {
        Camera.onPreCull += HandlePreCull;
    }

    private void OnDisable()
    {
        Camera.onPreCull -= HandlePreCull;
    }

    private void HandlePreCull(Camera camera)
    {
        if (target == null) target = Locator.GetPlayerTransform();
        if (target == null) return;

        transform.position = target.TransformPoint(localPosition);
        transform.rotation = target.rotation;
    }
}