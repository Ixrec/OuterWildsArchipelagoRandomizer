namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// Compatibility for the NomaiVR mod
/// </summary>
public static class VRCompatibility
{
    public static bool IsNomaiVRLoaded => APRandomizer.Instance.ModHelper.Interaction.ModExists("Raicuparta.NomaiVR");
}