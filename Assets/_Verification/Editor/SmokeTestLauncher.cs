using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>TEMPORAL (no se versiona): abre Test_Bruja y entra en Play Mode para el verificador.</summary>
public static class SmokeTestLauncher
{
    private const string ScenePath = "Assets/Scenes/Test_Bruja.unity";
    private const string FlagPath = "Temp/run_smoke.flag";

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);
        File.WriteAllText(FlagPath, "1");
        EditorApplication.EnterPlaymode();
    }
}
