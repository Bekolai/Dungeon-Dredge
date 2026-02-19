using UnityEngine;

    public enum SurfaceType
    {
        Stone,
        Metal,
        Dirt,
        Water,
        Wood,
        Grass,
        Gravel
    }

    public class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Stone;
        public SurfaceType SurfaceType => surfaceType;
    }

