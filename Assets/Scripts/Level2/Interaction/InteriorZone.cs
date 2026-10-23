using UnityEngine;

namespace Level2
{
    /// <summary>
    /// Define los límites espaciales y de cámara de un interior explorable del siglo XVII
    /// (Botica, Posada con sótano, Casa Abandonada con ático, Mansión del Magistrado).
    /// </summary>
    public class InteriorZone : MonoBehaviour
    {
        [Header("Identificación del Interior")]
        [SerializeField] private string interiorId = "interior_house_1";
        [SerializeField] private string houseDisplayName = "Botica de Herbolaria";
        [SerializeField] private int floorsCount = 2;

        [Header("Encuadre de Cámara (CameraFollow Limits)")]
        [Tooltip("Esquina inferior izquierda permitida para la cámara dentro de esta casa (coordenadas de mundo).")]
        [SerializeField] private Vector2 cameraMinLimit = new Vector2(10f, 26f);
        [Tooltip("Esquina superior derecha permitida para la cámara dentro de esta casa (coordenadas de mundo).")]
        [SerializeField] private Vector2 cameraMaxLimit = new Vector2(28f, 38f);
        [Tooltip("Offset de cámara para la exploración interior.")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 1.1f, -10f);

        public string InteriorId => interiorId;
        public string HouseDisplayName => houseDisplayName;
        public int FloorsCount => floorsCount;
        public Vector2 CameraMinLimit => cameraMinLimit;
        public Vector2 CameraMaxLimit => cameraMaxLimit;
        public Vector3 CameraOffset => cameraOffset;

        public void Configure(string id, string title, int floors, Vector2 minBounds, Vector2 maxBounds)
        {
            interiorId = id;
            houseDisplayName = title;
            floorsCount = floors;
            cameraMinLimit = minBounds;
            cameraMaxLimit = maxBounds;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.95f, 0.78f, 0.32f, 0.45f);
            Vector3 center = (cameraMinLimit + cameraMaxLimit) * 0.5f;
            Vector3 size = cameraMaxLimit - cameraMinLimit;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
