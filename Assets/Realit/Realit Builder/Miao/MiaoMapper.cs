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
        private const string FIND_APPERTURE_MATS =
            @"/cld:COLLADA/cld:library_materials/cld:material[cld:instance_effect/@url=""#{0}""]/@id";

        public void PostProcess(XmlDocument document, AssetLoaderContext context)
        {
            
            // Add the namespace.  
            var root = document.DocumentElement;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("cld", "http://www.collada.org/2005/11/COLLADASchema");

            Dictionary<GameObject, List<int>> futurAppertures = new();

            Dictionary<string, IMaterial> trilibtMats = new();
            foreach (var kvp in context.LoadedMaterials)
            {
                string name = kvp.Key.Name;
                trilibtMats.Add(name.ToLower(), kvp.Key);
            }

            var effects = root.SelectNodes(FIND_APPERTURE_EFFECTS, nsmgr);
            Debug.Log($"[Miao Mapper] {effects.Count} apperture effects found...");
            foreach (XmlNode effect in effects)
            {
                var matNodeList = root.SelectNodes(string.Format(FIND_APPERTURE_MATS, effect.InnerText), nsmgr);

                foreach (XmlNode matNode in matNodeList)
                {
                    if (trilibtMats.TryGetValue(matNode.InnerText, out IMaterial trilibMaterial))
                    {
                        List<MaterialRendererContext> renderersContext = context.MaterialRenderers[trilibMaterial];
                        foreach (var rendererContext in renderersContext)
                        {
                            GameObject go = rendererContext.Renderer.gameObject;
                            if (!futurAppertures.ContainsKey(go))
                                futurAppertures.Add(go, new List<int>());

                            futurAppertures[go].Add(rendererContext.GeometryIndex);
                        }
                    }
                }
            }

            //Setting up appertures
            foreach (var kvp in futurAppertures)
                ModelDataBuilder.AddAperture(kvp.Key, kvp.Value.ToArray());

            Debug.Log($"[Miao Mapper] {futurAppertures.Count} appertures have been added.");

        }
    }
}
