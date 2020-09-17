using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RainyReignGames.VariantAssets
{
    public class ProcessVariantAssetsSchema : AddressableAssetGroupSchema
    {
        [SerializeField, Tooltip("Whether to keep the generated Variant Assets around or automatically remove them after the build is complete.")] 
        public bool deleteVariants = true;
        
        [SerializeField] 
        public List<ProcessVariantAssetsScript> variantAssetsScripts;

        public bool PreprocessCheck(GameObject mainAsset)
        {
            if(variantAssetsScripts != null)
            {
                foreach(var script in variantAssetsScripts)
                {
                    if (script.PreprocessCheck(mainAsset))
                        return true;
                }
            }
            return false;
        }
    }
}
