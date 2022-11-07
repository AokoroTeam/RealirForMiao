using Aokoro;
using NaughtyAttributes;
using Newtonsoft.Json.Linq;
using Realit.Models;
using Realit.Scene;
using System;
using System.Collections.Generic;
using TriLibCore;
using UnityEngine;

namespace Realit.Builder
{

    [AddComponentMenu("Realit/Builder/Build/Model data builder")]
    //Script responsible of loading the building mesh data
    public class ModelDataBuilder : Singleton<ModelDataBuilder>, IDataBuilder
    {
        public DataSection Section => DataSection.Model;

        public bool IsValid => FullyLoaded;

        public static GameObject Structure
        {
            get => Instance._structures;
            set => Instance._structures = value;
        }
        public static GameObject Apertures
        {
            get => Instance._apertures;
            set => Instance._apertures = value;
        }

        [SerializeField]
        private GameObject _structures;
        [SerializeField]
        private GameObject _apertures;

        private bool FullyLoaded;

        protected override void OnExistingInstanceFound(ModelDataBuilder existingInstance)
        {
            Destroy(gameObject);
        }

        private void OnEnable()
        {
            ModelImporter.OnErrorLoading += OnErrorLoading;
            ModelImporter.OnStartLoading += OnStartLoading;
            ModelImporter.OnModelLoaded += OnModelLoaded;
            ModelImporter.OnMaterialLoaded += OnMaterialLoaded;
        }


        private void OnDisable()
        {
            ModelImporter.OnErrorLoading -= OnErrorLoading;
            ModelImporter.OnStartLoading -= OnStartLoading;
            ModelImporter.OnModelLoaded -= OnModelLoaded;
            ModelImporter.OnMaterialLoaded -= OnMaterialLoaded;
        }



        public static void AddAperture(string path, params int[] submeshIndices)
        {
            string[] objects = path.Split('/');
            Transform head = Structure.transform;
            for (int i = 0; i < objects.Length; i++)
            {
                string childName = objects[i];
                bool failed = true;
                for (int j = 0; j < head.childCount; j++)
                {
                    Transform child = head.GetChild(j);
                    if (child.name == childName)
                    {
                        head = child;
                        failed = false;
                        break;
                    }
                }

                if (failed)
                {
                    Debug.Log($"Could not find object with path {path}");
                    return;
                }
            }

            AddAperture(head.gameObject, submeshIndices);
        }



        public static void AddAperture(GameObject aperture, params int[] submeshIndices)
        {
            if (!aperture.TryGetComponent(out MeshRenderer mr))
                return;

            //Extracting Submesh
            int submeshCount = submeshIndices.Length;
            if (submeshCount == 0 || mr.materials.Length == submeshCount)
            {
                aperture.transform.SetParent(Apertures.transform);
            }
            else
            {
                GameObject splittedMesh = mr.Split(submeshIndices);
                if (splittedMesh != null)
                    splittedMesh.transform.SetParent(Apertures.transform);
            }
        }

        public static void RemoveAperture(GameObject aperture)
        {
            aperture.transform.SetParent(Structure.transform);
        }
        private static void Optimize(GameObject gameObject)
        {
            Dictionary<Hash128, Material> existingMaterials = new Dictionary<Hash128, Material>();
            var meshfilters = gameObject.GetComponentsInChildren<MeshFilter>();

            for (int i = 0; i < meshfilters.Length; i++)
                meshfilters[i].Optimize(existingMaterials);
        }


        private void OnStartLoading()
        {
            FullyLoaded = false;
        }


        protected virtual void OnModelLoaded(GameObject model)
        {
            Unload();
            Structure = model;
            Apertures = new GameObject("Apertures");

            Structure.transform.SetParent(transform);
            Apertures.transform.SetParent(transform);

        }

        protected virtual void OnMaterialLoaded(AssetLoaderContext assetLoaderContext)
        {
            FullyLoaded = true;
        }

        protected virtual void OnErrorLoading(IContextualizedError obj)
        {

        }
        private static void Unload()
        {
            if (Structure)
                Destroy(Structure);

            if (Apertures)
                Destroy(Apertures);
        }


        public JObject Serialize()
        {

            Optimize(Structure);
            Optimize(Apertures);

            RealitModel structureRM = new RealitModel(Structure);
            RealitModel aperturesRM = new RealitModel(Apertures);

            return new JObject(
                new JProperty("Structure", structureRM.Serialize()),
                new JProperty("Apertures", aperturesRM.Serialize())
                );
        }
    }
}
