#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Makes an extracted HoleIt project ready to play without requiring the user to find and run
/// a custom menu manually. On the first editor load, if there is no generated Main scene, it
/// creates all assets and the scene through HoleItBuilder and opens the resulting scene.
/// Existing scenes/projects are left untouched.
/// </summary>
[InitializeOnLoad]
public static class HoleItFirstOpenSetup
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private static int waitTicks;
    private static bool buildStarted;

    static HoleItFirstOpenSetup()
    {
        // Wait on editor updates until package resolution and C# compilation are complete.
        EditorApplication.update -= WaitForEditorReady;
        EditorApplication.update += WaitForEditorReady;
    }

    private static void WaitForEditorReady()
    {
        if (buildStarted) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // If a scene has already been generated, do not overwrite the user's project.
        if (File.Exists(ScenePath))
        {
            EditorApplication.update -= WaitForEditorReady;
            if(!Application.isBatchMode && !SessionState.GetBool("HoleIt.OpenedMain",false)){
                SessionState.SetBool("HoleIt.OpenedMain",true);var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if(string.IsNullOrEmpty(current.path)&&!current.isDirty)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            }
            return;
        }

        waitTicks++;
        if (waitTicks < 3) return; // let first-import package callbacks settle

        buildStarted = true;
        EditorApplication.update -= WaitForEditorReady;
        EditorApplication.delayCall += CreateProjectOnFirstOpen;
    }

    private static void CreateProjectOnFirstOpen()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            buildStarted = false;
            EditorApplication.update += WaitForEditorReady;
            return;
        }

        try
        {
            Debug.Log("[HoleIt Setup] 처음 실행한 프로젝트에서 Main 씬이 없어, 에셋과 플레이 씬을 자동 생성합니다.");
            bool success = HoleItBuilder.BuildAll(false);
            if (!success || !File.Exists(ScenePath))
            {
                Debug.LogError("[HoleIt Setup] 자동 구성을 완료하지 못했습니다. Console의 [HoleItBuilder] 오류를 확인하세요.");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[HoleIt Setup] 준비 완료: Assets/Scenes/Main.unity 를 열었습니다. Game 탭에서 Play 하세요.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[HoleIt Setup] 자동 구성이 실패했습니다. Console의 첫 번째 오류를 확인하세요.");
        }
    }
}
#endif
