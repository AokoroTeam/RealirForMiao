using Aokoro;
using NaughtyAttributes;
using Newtonsoft.Json.Linq;
using Realit.Scene;
using Realit.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Realit.Builder
{
    [DefaultExecutionOrder(-50)]
    public abstract class RealitBuilderBase : Singleton<RealitBuilderBase>
    {
        [SerializeField, Expandable, AllowNesting]
        private RealitSettings settings;

        public bool IsValid => true;


        public static T GetInstance<T>() where T : RealitBuilderBase => Instance as T;
        protected override void Awake()
        {
            base.Awake();
            RealitSettings.GlobalSettings = Instance.settings;
        }

        protected override void OnExistingInstanceFound(RealitBuilderBase existingInstance)
        {
            Destroy(gameObject);
        }

    }
}
