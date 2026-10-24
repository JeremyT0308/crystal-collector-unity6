using UnityEditor;

namespace CrystalCollector.EditorTools
{
    /// <summary>
    /// Batch-compatible generation and validation entry point.
    /// </summary>
    public static class CrystalCollectorBatch
    {
        public static void GenerateAndValidate()
        {
            CrystalCollectorGenerator.GenerateCompleteGame();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            var valid = ProjectValidator.ValidateProject(false);
            UnityEngine.Debug.Log(valid ? "CRYSTAL COLLECTOR BATCH VALIDATION PASSED" : "CRYSTAL COLLECTOR BATCH VALIDATION FAILED");
            EditorApplication.Exit(valid ? 0 : 1);
        }
    }
}
