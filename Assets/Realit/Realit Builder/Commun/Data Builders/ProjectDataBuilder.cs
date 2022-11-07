using Aokoro;
using UnityEngine;

using Newtonsoft.Json.Linq;
using Realit.Scene;
using Realit.Settings;

namespace Realit.Builder
{

    [AddComponentMenu("Realit/Builder/Build/Project data builder")]
    public class ProjectDataBuilder : Singleton<ProjectDataBuilder>, IDataBuilder
    {
        public static string ProjectName;

        public DataSection Section => DataSection.Project;

        public virtual bool IsValid => !string.IsNullOrWhiteSpace(ProjectName);

        protected override void OnExistingInstanceFound(ProjectDataBuilder existingInstance)
        {

        }

        public JObject Serialize() => new(
            new JProperty("Project", ProjectName??="Project"),
            new JProperty("Settings", JObject.FromObject(RealitSettings.GlobalSettings)));
    }
}
