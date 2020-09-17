using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RainyReignGames.VariantAssets
{
    public abstract class ProcessVariantAssetsScript : ScriptableObject
    {
        //Check if this asset needs to be processed at all.
        public abstract bool PreprocessCheck(GameObject originalAsset);

        public abstract bool ProcessVariantAsset(GameObject variantAsset);
    }
}
