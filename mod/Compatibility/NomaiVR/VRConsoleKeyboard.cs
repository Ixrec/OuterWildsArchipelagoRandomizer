using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine.UI;

namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// Makes the in-game AP console's text input work under NomaiVR.
///
/// NomaiVR opens the SteamVR overlay keyboard whenever any UnityEngine.UI.InputField is activated (a global
/// Harmony patch in NomaiVR's VirtualKeyboard module), but the half that reads the typed text back out of the
/// keyboard only exists in the title scene (its MonoBehaviour is gated to OWScene.TitleScreen). So in gameplay
/// the keyboard opens but the text is never written back, and NomaiVR never submits it either (the title
/// connection menu submits via a separate Connect button).
///
/// We add the missing half ourselves, scoped to our console field: listen for SteamVR's VREvent_KeyboardClosed,
/// read the typed text, write it into our InputField, and submit it through the normal onEndEdit path.
/// </summary>
public class VRConsoleKeyboard
{
    // The console InputField the keyboard should write back into.
    private InputField consoleField;

    // The last InputField that was activated (any, even if not ours), to know if we need to process the result
    private static InputField lastActivatedField;

    private bool subscribed;
    private bool reflectionFailed;

    // Cached reflection handles (need to cache them for calls in OnKeyboardClosed)
    private static PropertyInfo steamVrInstanceProp; // static SteamVR SteamVR.instance
    private static PropertyInfo steamVrOverlayProp; // CVROverlay SteamVR.overlay
    private static MethodInfo getKeyboardTextMethod; // uint CVROverlay.GetKeyboardText(StringBuilder, uint)

    /// <summary>
    /// Point the keyboard write-back at the current console InputField and make sure we're subscribed to the
    /// SteamVR keyboard-closed event. Safe to call every loop; the subscription is only created once.
    /// </summary>
    public void Configure(InputField newConsoleField)
    {
        consoleField = newConsoleField;
        // we do this on Configure and not inside the constructor because that may run too early.
        EnsureSubscribed();
    }

    private void EnsureSubscribed()
    {
        if (subscribed || reflectionFailed) return;

        try
        {
            var steamVrType = ResolveSteamVrType("Valve.VR.SteamVR");
            var steamVrEventsType = ResolveSteamVrType("Valve.VR.SteamVR_Events");
            var eventTypeEnum = ResolveSteamVrType("Valve.VR.EVREventType");
            var vrEventStructType = ResolveSteamVrType("Valve.VR.VREvent_t");
            if (steamVrType == null || steamVrEventsType == null || eventTypeEnum == null || vrEventStructType == null)
                throw new Exception(
                    "Could not resolve one or more SteamVR types (SteamVR, SteamVR_Events, EVREventType, VREvent_t).");

            steamVrInstanceProp = steamVrType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            steamVrOverlayProp = steamVrType.GetProperty("overlay", BindingFlags.Public | BindingFlags.Instance);
            // CVROverlay.GetKeyboardText(StringBuilder pchText, uint cchText)
            var overlayType = steamVrOverlayProp?.PropertyType;
            getKeyboardTextMethod =
                overlayType?.GetMethod("GetKeyboardText", new[] { typeof(StringBuilder), typeof(uint) });
            if (steamVrInstanceProp == null || steamVrOverlayProp == null || getKeyboardTextMethod == null)
                throw new Exception("Could not resolve SteamVR.instance / .overlay / CVROverlay.GetKeyboardText.");

            // SteamVR_Events.System(EVREventType) -> action object whose Listen takes a UnityAction<VREvent_t>.
            var systemMethod = steamVrEventsType.GetMethod(
                "System", BindingFlags.Public | BindingFlags.Static, null,
                new[] { eventTypeEnum }, null
            );
            if (systemMethod == null)
                throw new Exception("Could not resolve SteamVR_Events.System(EVREventType).");

            var keyboardClosed = Enum.Parse(eventTypeEnum, "VREvent_KeyboardClosed");
            var action = systemMethod.Invoke(null, new[] { keyboardClosed });
            if (action == null)
                throw new Exception("SteamVR_Events.System(VREvent_KeyboardClosed) returned null.");

            var listenMethod = action.GetType().GetMethod("Listen");
            if (listenMethod == null)
                throw new Exception("Could not resolve Listen(...) on the SteamVR event action.");

            // Build a UnityAction<VREvent_t> that ignores its arg and calls our parameterless handler. We can't
            // name VREvent_t at compile time, so construct the closed delegate type and lambda via reflection.
            var unityActionClosed = listenMethod.GetParameters()[0].ParameterType;
            var evtParam = Expression.Parameter(vrEventStructType, "evt");
            // Must be public: a compiled LambdaExpression runs from an anonymously-hosted dynamic method that
            // enforces member visibility, so calling a private method here throws MethodAccessException.
            var handler = typeof(VRConsoleKeyboard).GetMethod(nameof(OnKeyboardClosed),
                BindingFlags.Public | BindingFlags.Instance);
            var lambda = Expression.Lambda(
                unityActionClosed, Expression.Call(Expression.Constant(this), handler), evtParam);
            var del = lambda.Compile();

            listenMethod.Invoke(action, new object[] { del });
            subscribed = true;
            APRandomizer.OWMLModConsole.WriteLine(
                "VRConsoleKeyboard: subscribed to SteamVR VREvent_KeyboardClosed for console text entry.");
        }
        catch (Exception e)
        {
            reflectionFailed = true; // don't spam retries every loop
            APRandomizer.OWMLModConsole.WriteLine(
                $"VRConsoleKeyboard: failed to set up SteamVR keyboard write-back; console text entry will not work in VR.\n{e}",
                OWML.Common.MessageType.Warning);
        }
    }

    private static Type ResolveSteamVrType(string fullName)
    {
        // SteamVR.dll is deployed/loaded by NomaiVR, so it should already be in the AppDomain.
        var type = Type.GetType($"{fullName}, SteamVR");
        if (type != null) return type;

        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(fullName))
            .FirstOrDefault(t => t != null);
    }

    /// <summary>
    /// Fires (on the Unity main thread) when the SteamVR overlay keyboard is closed. If it was opened for our
    /// console field, read the typed text and submit it through the normal console path.
    /// </summary>
    public void OnKeyboardClosed()
    {
        try
        {
            // Guard: only act on our own console field.
            if (consoleField == null || lastActivatedField == null || lastActivatedField != consoleField)
                return;

            var instance = steamVrInstanceProp.GetValue(null);
            if (instance == null) return;
            var overlay = steamVrOverlayProp.GetValue(instance);
            if (overlay == null) return;

            var sb = new StringBuilder(256);
            getKeyboardTextMethod.Invoke(overlay, new object[] { sb, (uint)256 });

            // Submit through the existing onEndEdit listener (ArchConsoleManager.OnConsoleEntry), which handles
            // text submit.
            var entered = sb.ToString();
            consoleField.text = entered;
            consoleField.onEndEdit.Invoke(entered);
            // Clear before deactivating to prevent possible double submits if text submit doesn't clear
            consoleField.text = string.Empty;
            consoleField.DeactivateInputField();
        }
        catch (Exception e)
        {
            APRandomizer.OWMLModConsole.WriteLine($"VRConsoleKeyboard.OnKeyboardClosed failed:\n{e}",
                OWML.Common.MessageType.Error);
        }
    }

    /// <summary>
    /// Records which InputField the SteamVR keyboard was most recently opened for, mirroring NomaiVR's own
    /// ActivateInputField patch. Only active when NomaiVR is loaded.
    /// </summary>
    [HarmonyPatch(typeof(InputField), nameof(InputField.ActivateInputField))]
    private static class TrackActivatedFieldPatch
    {
        [HarmonyPostfix]
        private static void Postfix(InputField __instance)
        {
            if (!VRCompatibility.IsNomaiVRLoaded) return;
            lastActivatedField = __instance;
        }
    }
}