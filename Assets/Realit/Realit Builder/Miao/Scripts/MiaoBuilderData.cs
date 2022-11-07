using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Realit.Builder.Miao
{
    [JsonObject(MemberSerialization.OptIn), System.Serializable]
    public class MiaoBuilderData
    {
        [JsonProperty]
        public string ModelPath;

        [JsonProperty]
        public string[] Appertures;

        [JsonProperty]
        public Vector3 PlayerPosition;

        [JsonProperty]
        public Vector3 PlayerRotation;

        [JsonProperty]
        public string ProjectName;
    }
}
