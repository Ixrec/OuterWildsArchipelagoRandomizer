using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class DeathLinkManager
{
    enum DeathLinkSetting: long
    {
        Off = 0,
        Default = 1,
        AllDeaths = 2,
    }

    private static DeathLinkSetting setting = DeathLinkSetting.Off;

    private static DeathLinkService service = null;

    private static bool manualDeathInProgress = false;

    public static void Enable(long value)
    {
        if (Enum.IsDefined(typeof(DeathLinkSetting), value))
            setting = (DeathLinkSetting)value;
        else
            Randomizer.OWMLModConsole.WriteLine($"{value} is not a valid death link setting", OWML.Common.MessageType.Error);

        if (setting != DeathLinkSetting.Off && service is null)
        {
            service = Randomizer.APSession.CreateDeathLinkService();
            service.EnableDeathLink();

            service.OnDeathLinkReceived += (deathLinkObject) => {
                Randomizer.OWMLModConsole.WriteLine($"OnDeathLinkReceived() Timestamp={deathLinkObject.Timestamp}, Source={deathLinkObject.Source}, Cause={deathLinkObject.Cause}");
                DeathLinkManager.manualDeathInProgress = true;

                Locator.GetDeathManager().KillPlayer(DeathType.Default);
                Randomizer.InGameAPConsole.AddText(deathLinkObject.Cause);

                DeathLinkManager.manualDeathInProgress = false;
            };
        }
    }

    private static Random prng = new Random();

    private static Dictionary<DeathType, List<string>> deathMessages = new Dictionary<DeathType, List<string>> {
        { DeathType.Default, new List<string>
        {
            "became one with the universe",
            "stubbed their toe on a fascinating rock"
        } },
        { DeathType.Impact, new List<string>
        {
            "should've slowed down"
        } },
        { DeathType.Asphyxiation, new List<string>
        {
            "forgot to hug a tree"
        } },
        { DeathType.Energy, new List<string>
        {
            "experienced nuclear fusion firsthand"
        } },
        { DeathType.Supernova, new List<string>
        {
            "roasted all the marshmallows"
        } },
        { DeathType.Digestion, new List<string>
        {
            "met the fish"
        } },
        { DeathType.BigBang, new List<string>
        {
            "'s garage band got out of control"
        } },
        /*{ DeathType.Crushed, new List<string>
        {
        } },
        { DeathType.Meditation, new List<string>
        {
        } },
        { DeathType.TimeLoop, new List<string>
        {
        } },
        { DeathType.Lava, new List<string>
        {
        } },
        { DeathType.BlackHole, new List<string>
        {
        } },*/
        { DeathType.Dream, new List<string>
        {
            "underestimated the astral plane"
        } },
        { DeathType.DreamExplosion, new List<string>
        {
            "learned why product recalls are important"
        } },
        { DeathType.CrushedByElevator, new List<string>
        {
            "became a Flat Hearther"
        } },
    };

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
    public static void DeathManager_KillPlayer_Prefix(DeathType deathType)
    {
        // if this death was sent to us by another player's death link, do nothing, since that would start an infinite death loop
        if (manualDeathInProgress) return;

        if (setting == DeathLinkSetting.Off || service is null) return;

        if (setting == DeathLinkSetting.Default) {
            if (deathType == DeathType.Meditation || deathType == DeathType.Supernova || deathType == DeathType.TimeLoop || deathType == DeathType.BigBang)
            {
                Randomizer.OWMLModConsole.WriteLine($"DeathManager.KillPlayer ignoring {deathType} death since death_link is only set to Default");
                return;
            }
        }

        Randomizer.OWMLModConsole.WriteLine($"DeathManager.KillPlayer detected a {deathType} death, sending to AP server");
        var messages = deathMessages.ContainsKey(deathType) ? deathMessages[deathType] : deathMessages[DeathType.Default];
        var message = messages[prng.Next(0, messages.Count)];
        service.SendDeathLink(new DeathLink(Randomizer.SaveData.apConnectionData.slotName, message));
    }
}
