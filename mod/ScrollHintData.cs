using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Sets the colors of scroll text and 
    /// </summary>
    public class ScrollHintData : MonoBehaviour
    {
        // since scrolls don't have a good way to see if they're traps, Trap can simply mean empty here
        private CheckImportance importance = CheckImportance.Trap;

        private void Start()
        {
            APRandomizer.OWMLModConsole.WriteLine("bleh", OWML.Common.MessageType.Success);
            StartCoroutine(SetScrollColors());
        }

        IEnumerator SetScrollColors()
        {
            // want to make sure all text has been initialized properly, so we wait a few frames
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            Renderer rend = transform.Find("Props_NOM_Scroll/Props_NOM_Scroll_Geo").GetComponent<Renderer>();
            ArcHintData[] arcs = GetComponentsInChildren<ArcHintData>();
            // We can ignore trying to change scroll colors if there are no hints found
            if (arcs.Length == 0) yield break;
            importance = arcs.Max(x => x.DisplayImportance);
            Color textColor = Color.white;
            Color trimColor = Color.white;
            if (arcs.All(x => x.HasBeenFound))
            {
                textColor = HintColors.FoundColor;
                trimColor = HintColors.FoundColorTrim;
            }
            else switch (importance)
            {
                case CheckImportance.Filler:
                {
                    textColor = HintColors.FillerColor;
                    trimColor = HintColors.FillerColorTrim;
                    break;
                }
                case CheckImportance.Useful:
                {
                    textColor = HintColors.UsefulColor;
                    trimColor = HintColors.UsefulColorTrim;
                    break;
                }
                case CheckImportance.Progression:
                {
                    textColor = HintColors.FillerColor;
                    trimColor = HintColors.FillerColorTrim;
                    break;
                }
                default:
                {
                    APRandomizer.OWMLModConsole.WriteLine($"Uh this code shouldn't have been reached, the scroll at {transform.parent.name} somehow didn't inherit an importance priority.", OWML.Common.MessageType.Error);
                    textColor = Color.red;
                    trimColor = Color.red;
                    break;
                }
            }
            rend.material.SetColor("_Detail1EmissionColor", textColor);
            rend.material.SetColor("_Detail3EmissionColor", trimColor);
            APRandomizer.OWMLModConsole.WriteLine($"Scroll has been given the color relating to importance {importance}");
        }
    }
}
