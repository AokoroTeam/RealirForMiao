using Realit.Scene;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TriLibCore;
using UnityEngine;
using System.Xml;
using System.Threading.Tasks;
using Aokoro.Sequencing;
using Realit.Settings;
using System.Linq;

namespace Realit.Builder.Miao
{
    public class MiaoBuildJob
    {
        public bool CanBuild => IsPlayerSetup && IsModelSetup && IsProjectSetup;
        public bool IsProjectSetup => Scene.IsValid(DataSection.Project);
        public bool IsPlayerSetup => Scene.IsValid(DataSection.Player);
        public bool IsModelSetup => Scene.IsValid(DataSection.Model);
        public bool AreAssetSetup => Scene.IsValid(DataSection.Assets);

        public RealitScene Scene { get; private set; }
        public MiaoBuilderData Data { get; private set; }
        public string Output { get; private set; }

        public bool IsMiao;

        Sequencer sequencer;

        public MiaoBuildJob(MiaoBuilderData data, string output)
        {
            Scene = new RealitScene();
            this.Data = data;
            this.Output = output;
        }
        private void StopSequence()
        {
            sequencer.Stop();
        }
        public void Process()
        {
            sequencer = SequencerBuilder.Begin()
                .Do(() =>
                {
                    try
                    {
                        MiaoBuilder.Log(".rltb loaded. Creating Scene...");

                        //Setting up data...
                        ModelImporter.OnMaterialLoaded += OnModelLoaded;
                        ModelImporter.Instance.LoadMesh(Data.ModelPath);

                        PlayerDataBuilder.SetPlayerRotation(Data.PlayerRotation);
                        PlayerDataBuilder.SetPlayerPosition(Data.PlayerPosition);
                    }
                    catch
                    {
                        MiaoBuilder.ExitWithError();
                        StopSequence();
                        throw;
                    }
                })
                .WaitUntil(() => CanBuild)
                .Do(() =>
                {
                    try
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        //Building
                        MiaoBuilder.Log($"Scene ready. Building at {Output}...");

                        watch.Start();
                        MiaoBuilder.Log("Gathering Data...");
                        var buildData = Scene.BuildData(false, RealitSettings.GlobalSettings.formatting);
                        watch.Stop();

                        sequencer.SetParameter("buildData", buildData);
                        MiaoBuilder.Log($"Data gathered : {watch.ElapsedMilliseconds / 1000f} secondes. Compressing...");
                    }
                    catch
                    {
                        MiaoBuilder.ExitWithError();
                        StopSequence();
                        throw;
                    }
                })
                .Yield()
                .Do(() =>
                {
                    try
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        byte[] buildData = sequencer.GetParameterValue<byte[]>("buildData");
                        
                        watch.Start();
                        byte[] compressedData = RealitSettings.GlobalSettings.rszCompression switch
                        {
                            Settings.CompressionType.Gzip => RealitCompressor.CompressGzip(buildData, System.IO.Compression.CompressionLevel.Optimal),
                            Settings.CompressionType.Brotli => RealitCompressor.CompressBrotli(buildData, System.IO.Compression.CompressionLevel.Optimal),
                            Settings.CompressionType.None => buildData,
                        };
                        watch.Stop();


                        MiaoBuilder.Log($"Data compressed : {watch.ElapsedMilliseconds / 1000f} secondes");

                        if (MiaoBuilder.Instance.TryGetArg("-embed"))
                        {
                            string templateDir = Path.Combine(Application.streamingAssetsPath, "ReaderTemplate/Web");
                            MiaoBuilder.Log($"Copying template to {Output}...");
                            CopyDirectory(templateDir, Output, true);

                            string path = Path.Combine(Output, "StreamingAssets", $"{Data.ProjectName}.rsz");
                            WriteFile(path, compressedData);
                        }
                        else
                        {
                            string path = Path.Combine(Output, $"{Data.ProjectName}.rsz");
                            WriteFile(path, compressedData);
                        }
                    }
                    catch
                    {
                        MiaoBuilder.ExitWithError();
                        StopSequence();
                        throw;
                    }
                   
                })
                .Build();


            sequencer.Play();
        }

        static void CopyDirectory(string source, string destination, bool recursive)
        {
            // Get information about the source directory
            var sourceDirectory = new DirectoryInfo(source);

            // Check if the source directory exists
            if (!sourceDirectory.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory.FullName}");

            //Cleaning existing directory
            DirectoryInfo destinationDirectory = new DirectoryInfo(destination);
            if (destinationDirectory.Exists)
            {
                foreach (FileInfo file in destinationDirectory.GetFiles())
                    file.Delete();
                foreach (DirectoryInfo d in destinationDirectory.GetDirectories())
                    d.Delete(true);
            }
            else
            {
                // Create the destination directory
                Directory.CreateDirectory(destination);
            }

            // Cache directories before we start copying
            DirectoryInfo[] dirs = sourceDirectory.GetDirectories();

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in sourceDirectory.GetFiles())
            {
                if (file.Extension != ".meta")
                {
                    string targetFilePath = Path.Combine(destination, file.Name);
                    file.CopyTo(targetFilePath);
                }
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destination, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        private static void WriteFile(string path, byte[] buildData)
        {
            try
            {
                MiaoBuilder.Log($"Writing data...");
                File.WriteAllBytes(path, buildData);

                MiaoBuilder.Log($"Successfully writted");
                MiaoBuilder.ExitWithSuccess();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                MiaoBuilder.ExitWithError();
                throw;
            }
        }

        private void OnModelLoaded(AssetLoaderContext ctx)
        {
            var rs = ctx.RootGameObject.GetComponentsInChildren<MeshRenderer>();
            var mats = rs.SelectMany(ctx => ctx.sharedMaterials);

            ModelImporter.OnMaterialLoaded -= OnModelLoaded;

            if (MiaoBuilder.Instance.TryGetArg("-miao", out string xmlPath))
            {
                try
                {
                    if (File.Exists(xmlPath))
                    {
                        XmlDocument xmlDoc = new XmlDocument();

                        using (FileStream fileStream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read))
                            xmlDoc.Load(fileStream);

                        MiaoMapper miao = ScriptableObject.CreateInstance<MiaoMapper>();
                        //miao.PostProcess(xmlDoc, ctx);
                    }
                    else
                    {
                        MiaoBuilder.Log($"Couldn't find a miao collada at path {xmlPath}.");
                    }

                }
                catch
                {
                    MiaoBuilder.Log("Error while reading the miao collada...");
                }
            }
            else
            {

                for (int i = 0; i < Data.Appertures.Length; i++)
                {
                    string[] s = Data.Appertures[i].Split('.');
                    string path = s[0];

                    int[] submeshes = s.Length > 1 ? GetSubmeshesFromString(s[1]) : new int[0];

                    ModelDataBuilder.AddAperture(path, submeshes);
                }
            }
            
        }

        private int[] GetSubmeshesFromString(string s)
        {
            string[] stringIndices = s.Split('&');
            int[] indices = new int[stringIndices.Length];

            for (int i = 0; i < stringIndices.Length; i++)
                indices[i] = int.Parse(stringIndices[i]);

            return indices;
        }

    }
}
