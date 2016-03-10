using UnityEngine;

namespace Assets.Scripts
{
    public class SpecialEffectsScript : MonoBehaviour
    {
        public GameObject TrailPrefab;

        private static SpecialEffectsScript _instance;

        void Awake()
        {
            _instance = this;
        }

        void Start()
        {
            if (TrailPrefab == null)
                Debug.LogError("Missing Trail Prefab!");
        }

        public static GameObject MakeTrail(Vector3 position)
        {
            if (_instance == null)
            {
                Debug.LogError("There is no SpecialEffectsScript in the scene!");
                return null;
            }

            var trail = Instantiate(_instance.TrailPrefab);
            trail.transform.position = position;

            return trail;
        }
    }


}
