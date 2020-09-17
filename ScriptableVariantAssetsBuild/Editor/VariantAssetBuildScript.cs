using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Serialization;

namespace RainyReignGames.VariantAssets
{
    [CreateAssetMenu(fileName = "VariantAssetBuildScript.asset", menuName = "Addressables/Custom Build/Variant Assets")]
    public class VariantAssetBuildScript : BuildScriptPackedMode//, ISerializationCallbackReceiver
    {
        public struct AssetEntry
        {
            public string assetGUID;
            public string address;
            public AddressableAssetGroup group;
            public HashSet<string> labels;
        }
        List<AssetEntry> entriesToRestore = new List<AssetEntry>();
        List<string> variantEntriesToRemove = new List<string>();
        List<string> variantsPrefabsToDelete = new List<string>();

        string defaultBaseDirectory = "Assets/AddressablesGenerated";

        public override string Name
        {
            get { return "Variant Assets Build"; }
        }

        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput context)
        {
            var result = base.BuildDataImplementation<TResult>(context);

            AddressableAssetSettings settings = context.AddressableSettings;
            RestoreAndCleanup(settings);
            return result;
        }

        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            if (assetGroup.HasSchema<ProcessVariantAssetsSchema>())
            {
                ProcessVariantAssetsSchema schema = assetGroup.GetSchema<ProcessVariantAssetsSchema>();
                var errorString = ProcessVariants(schema, assetGroup, aaContext);
                if (!string.IsNullOrEmpty(errorString))
                    return errorString;
            }

            return base.ProcessGroup(assetGroup, aaContext);
        }

        string ProcessVariants(ProcessVariantAssetsSchema schema, AddressableAssetGroup group, AddressableAssetsBuildContext context)
        {
            var settings = context.Settings;

            var entries = new List<AddressableAssetEntry>(group.entries);
            foreach (var mainEntry in entries)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(mainEntry.AssetPath) != typeof(GameObject))
                    continue;

                GameObject mainAsset = AssetDatabase.LoadMainAssetAtPath(mainEntry.AssetPath) as GameObject;
                if (!schema.PreprocessCheck(mainAsset))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(mainEntry.AssetPath);
                string mainAssetPath = AssetDatabase.GUIDToAssetPath(mainEntry.guid);

                string groupDirectory = Path.Combine(defaultBaseDirectory, $"{group.Name}").Replace('\\', '/');
                string newPrefabPath = groupDirectory + '/' + Path.GetFileName(mainEntry.AssetPath).Replace(fileName, $"{fileName}_variant");
                Directory.CreateDirectory(groupDirectory);

                if (!AssetDatabase.CopyAsset(mainAssetPath, newPrefabPath))
                {
                    Debug.LogError("Failed to copy asset " + mainAssetPath);
                    continue;
                }
                if (schema.deleteVariants)
                {
                    variantsPrefabsToDelete.Add(newPrefabPath);
                }
                GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);

                foreach(var script in schema.variantAssetsScripts)
                {
                    script.ProcessVariantAsset(variant);
                }

                //Create the Variant Entry and set it's address and labels.
                var variantEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(newPrefabPath), mainEntry.parentGroup, false, false);
                variantEntry.address = mainEntry.address;
                foreach (string label in mainEntry.labels)
                {
                    variantEntry.SetLabel(label, true, false, false);
                }
                variantEntriesToRemove.Add(AssetDatabase.AssetPathToGUID(newPrefabPath));
                entriesToRestore.Add(new AssetEntry { address = mainEntry.address, assetGUID = mainEntry.guid, group = mainEntry.parentGroup, labels = mainEntry.labels });
                settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(mainEntry.AssetPath), false);
            }

            return string.Empty;
        }

        void RestoreAndCleanup(AddressableAssetSettings settings)
        {
            //Delete our variant entries
            foreach(string guid in variantEntriesToRemove)
            {
                settings.RemoveAssetEntry(guid, false);
            }
            variantEntriesToRemove.Clear();

            //Delete all our variants.
            List<string> failedPaths = new List<string>();
            AssetDatabase.DeleteAssets(variantsPrefabsToDelete.ToArray(), failedPaths);
            foreach(string path in failedPaths)
            {
                Debug.LogError("Failed to delete: " + path);
            }
            variantsPrefabsToDelete.Clear();

            //Restore our original addressable entries
            foreach(AssetEntry entry in entriesToRestore)
            {
                var restoredEntry = settings.CreateOrMoveEntry(entry.assetGUID, entry.group, false, false);
                restoredEntry.address = entry.address;
                foreach(string label in entry.labels)
                {
                    restoredEntry.SetLabel(label, true, false, false);
                }   
            }

            entriesToRestore.Clear();
        }
    }
}
