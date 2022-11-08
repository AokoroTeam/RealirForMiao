using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using TriLibCore;
using TriLibCore.Interfaces;
using System.Linq;

namespace Realit.Builder.Miao
{
    public class MiaoMapper : ScriptableObject
    {
        private const string FIND_APPERTURE_EFFECTS =
            "/cld:COLLADA/cld:library_effects/cld:effect[cld:profile_COMMON/cld:extra/cld:technique/cld:Menuiserie=1 or cld:profile_COMMON/cld:extra/cld:technique/cld:MenuiserieAutre=1]/@id";
            //"/cld:COLLADA/cld:library_effects/cld:effect[cld:profile_COMMON/cld:extra/cld:technique/cld:Menuiserie=1]/@id";
        private const string FIND_APPERTURE_MATS =
            @"/cld:COLLADA/cld:library_materials/cld:material[cld:instance_effect/@url=""#{0}""]/@id";
        private const string instanceEndingName = " (Instance)";

        public void PostProcess(XmlDocument document, AssetLoaderContext context)
        {

            // Add the namespace.  
            var root = document.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("cld", "http://www.collada.org/2005/11/COLLADASchema");

            var effects = root.SelectNodes(FIND_APPERTURE_EFFECTS, nsmgr);
            Debug.Log($"[Miao Mapper] {effects.Count} effects found...");

            List<string> concernedMaterials = new();


            foreach (XmlNode effect in effects)
            {
                var matNodeList = root.SelectNodes(string.Format(FIND_APPERTURE_MATS, effect.InnerText), nsmgr);
                foreach (XmlNode matNode in matNodeList)
                {
                    concernedMaterials.Add(matNode.InnerText);
                    //Debug.Log($"{matNode.InnerText} has {effect.InnerText}");
                }
            }
            Dictionary<MeshRenderer, List<string>> generatedMaterials = GetRenderersAndData(context);
            Dictionary<MeshRenderer, List<int>> futurAppertures = new();

            foreach (var mat in concernedMaterials)
            {
                foreach (var kvp in generatedMaterials)
                {
                    if(!futurAppertures.ContainsKey(kvp.Key))
                        futurAppertures.Add(kvp.Key, new List<int>());

                    int opaque = kvp.Value.IndexOf(mat);
                    if(opaque != -1)
                        futurAppertures[kvp.Key].Add(opaque);

                    int transparent = kvp.Value.IndexOf(mat + "_alpha");
                    if(transparent != -1)
                        futurAppertures[kvp.Key].Add(transparent);
                }
            }


            //Setting up appertures
            foreach (var kvp in futurAppertures)
            {
                List<int> list = kvp.Value;
                if (list.Count > 0)
                {
                    ModelDataBuilder.AddAperture(kvp.Key.gameObject, list.ToArray());
                }
            }
        }

        private static Dictionary<MeshRenderer, List<string>> GetRenderersAndData(AssetLoaderContext context)
        {
            Dictionary<MeshRenderer, List<string>> generatedMaterials = new();
            MeshRenderer[] renderers = context.RootGameObject.GetComponentsInChildren<MeshRenderer>();

            List<Material> mats = new List<Material>();

            for (int i = 0; i < renderers.Length; i++)
            {
                List<string> matNames = new List<string>();

                var renderer = renderers[i];
                renderer.GetMaterials(mats);
                foreach(var mat in mats)
                {
                    string matName = mat.name;

                    if (matName.EndsWith(instanceEndingName))
                        matNames.Add(matName.Remove(matName.Length - instanceEndingName.Length, instanceEndingName.Length));
                    else
                        matNames.Add(matName);
                }

                generatedMaterials.Add(renderer, matNames);
            }
            return generatedMaterials;
        }
    }
}
