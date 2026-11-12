using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColiseumGame.EditorTools
{
    [InitializeOnLoad]
    public static class CatapultInstallOnce
    {
        static CatapultInstallOnce() { EditorApplication.delayCall += Run; }
        private static void Run()
        {
            if (!File.Exists("Temp/InstallCatapults.request")) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Run; return; }
            File.Delete("Temp/InstallCatapults.request");
            try { CatapultIntegration.Install(); File.WriteAllText("Temp/CatapultInstall.result", "OK"); }
            catch (Exception ex) { File.WriteAllText("Temp/CatapultInstall.result", ex.ToString()); Debug.LogException(ex); }
        }
    }
}
