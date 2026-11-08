using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CounterShop.Editor
{
    public static class ShopSetup
    {
        [MenuItem("Counter Shop/Create Demo In Current Scene")]
        public static void CreateDemo()
        {
            // 현재 씬을 지우지 않습니다. Undo로 생성한 오브젝트를 되돌릴 수 있습니다.
            var go = new GameObject("Counter Shop Demo");
            Undo.RegisterCreatedObjectUndo(go, "Create Shop Demo");
            go.AddComponent<ShopDemo>();
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
        [MenuItem("Counter Shop/Create Sample Database Asset")]
        public static void CreateDatabase()
        {
            var db = SampleContent.Create();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/ShopDatabase.asset");
            AssetDatabase.CreateAsset(db, path);
            AssetDatabase.SaveAssets(); Selection.activeObject = db;
        }
    }
}
