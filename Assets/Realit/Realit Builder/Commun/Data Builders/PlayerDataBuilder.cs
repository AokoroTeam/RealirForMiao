using Aokoro;
using Newtonsoft.Json.Linq;
using Realit.Models;
using Realit.Scene;
using UnityEngine;

namespace Realit.Builder
{


    [AddComponentMenu("Realit/Builder/Build/Player data builder")]
    public class PlayerDataBuilder : Singleton<PlayerDataBuilder>, IDataBuilder
    {
        public DataSection Section => DataSection.Player;

        public bool IsValid => (positionValid && rotationValid) || Spawn != null;

        private bool positionValid, rotationValid;

        public static Vector3 Position 
        {
            get => Instance.Spawn.position;
            private set => Instance.Spawn.position = value; 
        }
        public static Vector3 Rotation 
        { 
            get => Instance.Spawn.eulerAngles; 
            private set => Instance.Spawn.eulerAngles = value; 
        }

        public Transform Spawn
        {
            get
            {
                if (_spawn == null)
                {
                    GameObject spawnObject = GameObject.FindGameObjectWithTag("Respawn");
                    if (spawnObject == null)
                    {
                        _spawn = new GameObject("Spawn").transform;
                        _spawn.tag = "Respawn";
                    }
                    else
                        _spawn = spawnObject.transform;
                }

                return _spawn;
            }
        }

        private Transform _spawn;

#if !UNITY_SERVER
#endif
        public static void SetPlayerPosition(Vector3 position)
        {
            Instance.positionValid = true;
            Position = position;
        }
        public static void SetPlayerRotation(Vector3 rotation)
        {
            Instance.rotationValid = true;
            Rotation = rotation;
        }

        public static void ClearPlayerPosition() => Instance.positionValid = false;
        public static void ClearPlayerRotation() => Instance.rotationValid = false;

        protected override void OnExistingInstanceFound(PlayerDataBuilder existingInstance)
        {
            Destroy(gameObject);
        }

        public JObject Serialize() => new JObject(
                new JProperty("pos", Position.Serialize()),
                new JProperty("rot", Rotation.Serialize())
            );
    }
}