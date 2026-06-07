using System.Collections.Generic;
using Jiangshi.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CorridorSceneBridge
{
    private const string PrototypeSceneName = "Prototype";

    private static readonly List<Behaviour> SuspendedDefenseBehaviours = new();
    private static bool defenseSuspended;

    public static void SuspendDefenseScenes(Scene corridorScene)
    {
        if (defenseSuspended)
        {
            return;
        }

        SuspendedDefenseBehaviours.Clear();
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded || scene == corridorScene)
            {
                continue;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                SuspendBehaviours(root, corridorScene);
            }
        }

        defenseSuspended = true;
    }

    public static void ReturnToDefenseScene(MissionResult result)
    {
        MissionResultState.Result = result;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var corridorScene = SceneManager.GetActiveScene();
        var prototypeScene = SceneManager.GetSceneByName(PrototypeSceneName);
        if (!prototypeScene.IsValid() || !prototypeScene.isLoaded)
        {
            SceneManager.LoadScene(PrototypeSceneName);
            return;
        }

        SceneManager.SetActiveScene(prototypeScene);
        ResumeDefenseScenes();
        GameManager.Instance?.ApplyPendingCorridorResult();

        if (corridorScene.IsValid() && corridorScene.isLoaded && corridorScene != prototypeScene)
        {
            SceneManager.UnloadSceneAsync(corridorScene);
        }
    }

    public static void ResumeDefenseScenes()
    {
        for (var i = 0; i < SuspendedDefenseBehaviours.Count; i++)
        {
            var behaviour = SuspendedDefenseBehaviours[i];
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        SuspendedDefenseBehaviours.Clear();
        defenseSuspended = false;
    }

    private static void SuspendBehaviours(GameObject root, Scene corridorScene)
    {
        var behaviours = root.GetComponentsInChildren<Behaviour>(true);
        foreach (var behaviour in behaviours)
        {
            if (behaviour == null || !behaviour.enabled || behaviour.gameObject.scene == corridorScene)
            {
                continue;
            }

            SuspendedDefenseBehaviours.Add(behaviour);
            behaviour.enabled = false;
        }
    }
}
