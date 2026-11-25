#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class LatestDemoSetup
{
    public static bool BeginNewScene()
    {
        if(Application.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Play를 중지한 뒤 실행하세요.");return false;}
        // Let Unity save/cancel unsaved work first. Generate the demo only in the new unsaved scene.
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return false;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
        return true;
    }
    public static Font FindKoreanFont()
    {
        // 프로젝트에 이미 있는 Font만 재사용합니다. 다운로드/런타임 검색은 하지 않습니다.
        var ids=AssetDatabase.FindAssets("t:Font");var paths=new string[ids.Length];
        for(int n=0;n<ids.Length;n++)paths[n]=AssetDatabase.GUIDToAssetPath(ids[n]);Array.Sort(paths,StringComparer.Ordinal);
        foreach(string key in new[]{"notosanskr","notoserifkr","notosanscjkkr","pretendard","nanum","malgun"})
            foreach(string path in paths)
            {string name=System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Replace("-","").Replace("_","").Replace(" ","");if(name.Contains(key)){var font=AssetDatabase.LoadAssetAtPath<Font>(path);if(font!=null)return font;}}
        return null;
    }
}
#endif
