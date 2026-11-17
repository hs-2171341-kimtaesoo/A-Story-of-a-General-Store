#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using GeneralStore.Inventory;
public static class LatestDemoSetup
{
    public static bool CanCreate()
    {
        if(Application.isPlaying){Debug.LogError("Play를 중지한 뒤 빈 새 씬에서 실행하세요.");return false;}
        // 선택 오브젝트와 관계없이 로드된 씬의 활성/비활성 데모를 모두 검사합니다. 자동 삭제하지 않습니다.
        var existing=UnityEngine.Object.FindObjectsByType<InventoryRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        if(existing.Length==0)return true;
        Selection.activeGameObject=existing[0].gameObject;
        Debug.LogWarning("이미 Store Inventory Data가 있는 씬이 열려 있습니다. 기존 데모를 보존했습니다. 빈 새 씬을 단독으로 연 뒤 Create Latest Demo를 실행하세요.");return false;
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
