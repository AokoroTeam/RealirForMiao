using Aokoro;
using NaughtyAttributes;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TriLibCore;
using TriLibCore.Extensions;
using UnityEngine;

namespace Realit.Builder
{

    //Script responsible of loading the building mesh data
    
    [AddComponentMenu("Realit/Builder/Import/Model Importer")]
    public class ModelImporter : Singleton<ModelImporter>
    {
        public static event Action OnStartLoading;
        public static event Action<GameObject> OnModelLoaded;
        public static event Action<AssetLoaderContext> OnMaterialLoaded;
        public static event Action<IContextualizedError> OnErrorLoading;

        [SerializeField, BoxGroup("Model loading")]
        //Settings for the building import
        private AssetLoaderOptions assetLoaderOptions;

        [Button]
        public void LoadMesh()
        {
            var assetLoaderFilePicker = AssetLoaderFilePicker.Create();
            assetLoaderFilePicker.LoadModelFromFilePickerAsync(
                "Selectionner le model 3D de votre batiment",
                OnLoad,
                OnMaterialsLoad,
                OnProgress,
                OnBeginLoad,
                OnError,
                gameObject,
                assetLoaderOptions);
        }

        public void LoadMesh(string path, Action<AssetLoaderContext> onLoad = null)
        {
            Debug.Log($"[Realit] Loading model at path {path}");
            
            var ctx = AssetLoader.LoadModelFromFile(path, OnLoad, OnMaterialsLoad, OnProgress, OnError, gameObject, assetLoaderOptions);
            if(onLoad != null)
                ctx.OnLoad += onLoad;
        }
        public void LoadMesh(Stream stream, string extention, Action<AssetLoaderContext> onLoad = null)
        {

            var ctx = AssetLoader.LoadModelFromStreamNoThread(stream, 
                fileExtension: extention,
                onError: OnError, 
                wrapperGameObject: gameObject,
                assetLoaderOptions: assetLoaderOptions);

            if (onLoad != null)
                ctx.OnLoad += onLoad;
        }
        #region Loading callbacks
        /// Called when the the Model begins to load.
        private void OnBeginLoad(bool filesSelected)
        {
            if(filesSelected)
                OnStartLoading?.Invoke();
        }

        /// Called when any error occurs.
        private void OnError(IContextualizedError error)
        {
            Debug.LogError($"An error occurred while loading your Model: {error.GetInnerException()}");
            OnErrorLoading?.Invoke(error);
        }

        /// Called when the Model loading progress changes.
        private void OnProgress(AssetLoaderContext assetLoaderContext, float progress)
        {
            //Debug.Log(progress);
        }

        /// Called when the Model (including Textures and Materials) has been fully loaded.
        private void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
        {
            Debug.Log("Material loaded");
            OnMaterialLoaded?.Invoke(assetLoaderContext);
        }

        /// Called when the Model Meshes and hierarchy are loaded.
        private void OnLoad(AssetLoaderContext assetLoaderContext)
        {
            Debug.Log("Model loaded");
            string extention = Path.GetExtension(assetLoaderContext.Filename);
            if(extention == ".obj")
            {
                assetLoaderContext.RootGameObject.transform.Rotate(Vector3.left * 90);
            }
            OnModelLoaded?.Invoke(assetLoaderContext.RootGameObject);
        }
        #endregion


        protected override void OnExistingInstanceFound(ModelImporter existingInstance)
        {
            Destroy(gameObject);
        }
    }
}
