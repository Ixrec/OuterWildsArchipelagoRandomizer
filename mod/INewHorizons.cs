using OWML.Common;
using UnityEngine;
using UnityEngine.Events;

namespace ArchipelagoRandomizer;

public interface INewHorizons
{
    string GetCurrentStarSystem();
    bool SetDefaultSystem(string name);
    UnityEvent<string> GetStarSystemLoadedEvent();
    UnityEvent<string> GetChangeStarSystemEvent();
    GameObject SpawnObject(IModBehaviour mod, GameObject planet, Sector sector, string propToCopyPath, Vector3 position, Vector3 eulerAngles, float scale, bool alignWithNormal);
    void CreatePlanet(string config, IModBehaviour mod);
    void DefineStarSystem(string name, string config, IModBehaviour mod);
}
