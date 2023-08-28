using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using HarmonyLib;

using UnityEngine;

using RTFunctions.Functions.IO;

namespace CreativePlayers.Patchers
{
    [HarmonyPatch(typeof(SoundLibrary))]
    public class SoundLibraryPatch : MonoBehaviour
    {
        public static Dictionary<string, AudioClip[]> originalClips = new Dictionary<string, AudioClip[]>();

        public static SoundLibrary inst;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        static void AwakePostfix(SoundLibrary __instance)
        {
            inst = __instance;

            foreach (var soundGroup in __instance.soundGroups)
            {
                if (!originalClips.ContainsKey(soundGroup.soundID))
                    originalClips.Add(soundGroup.soundID, soundGroup.group);
            }
        }

        public static IEnumerator SetAudioClips()
        {
            if (inst == null)
            {
                inst = (SoundLibrary)AudioManager.inst.GetType().GetField("library", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(AudioManager.inst);
            }

            string path = RTFile.ApplicationDirectory + RTFile.basePath + "SoundGroups";
            if (RTFile.DirectoryExists(path))
            {
                var dictionary = new Dictionary<string, AudioClip[]>();
                if (RTFile.FileExists(path + "/boost.wav"))
                {
                    yield return FileManager.inst.StartCoroutine(FileManager.inst.LoadMusicFile(path.Replace(RTFile.ApplicationDirectory, "") + "/boost.wav", delegate (AudioClip _clip)
                    {
                        Debug.LogFormat("{0}Setting boost sound", PlayerPlugin.className);
                        _clip.name = "boost";
                        var clipper = new AudioClip[1]
                        {
                            _clip
                        };
                        if (!dictionary.ContainsKey("boost"))
                            dictionary.Add("boost", clipper);
                        else
                            dictionary["boost"] = clipper;
                    }));
                }
                if (RTFile.FileExists(path + "/boostback.wav"))
                {
                    yield return FileManager.inst.StartCoroutine(FileManager.inst.LoadMusicFile(path.Replace(RTFile.ApplicationDirectory, "") + "/boostback.wav", delegate (AudioClip _clip)
                    {
                        Debug.LogFormat("{0}Setting boost back sound", PlayerPlugin.className);
                        _clip.name = "boostback";
                        var clipper = new AudioClip[1]
                        {
                            _clip
                        };
                        if (!dictionary.ContainsKey("boost_recover"))
                            dictionary.Add("boost_recover", clipper);
                        else
                            dictionary["boost_recover"] = clipper;
                    }));
                }
                if (RTFile.FileExists(path + "/checkpoint.wav"))
                {
                    yield return FileManager.inst.StartCoroutine(FileManager.inst.LoadMusicFile(path.Replace(RTFile.ApplicationDirectory, "") + "/checkpoint.wav", delegate (AudioClip _clip)
                    {
                        Debug.LogFormat("{0}Setting checkpoint sound", PlayerPlugin.className);
                        _clip.name = "checkpoint";
                        var clipper = new AudioClip[1]
                        {
                            _clip
                        };
                        if (!dictionary.ContainsKey("checkpoint"))
                            dictionary.Add("checkpoint", clipper);
                        else
                            dictionary["checkpoint"] = clipper;
                    }));
                }

                foreach (var soundGroup in originalClips)
                {
                    if (!dictionary.ContainsKey(soundGroup.Key))
                        dictionary.Add(soundGroup.Key, soundGroup.Value);
                }

                inst.GetType().GetField("soundClips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inst, dictionary);
            }
            yield break;
        }

        public static void Reset()
        {
            inst.GetType().GetField("soundClips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(inst, originalClips);
        }
    }
}
