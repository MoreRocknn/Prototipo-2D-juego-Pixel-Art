using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

public class CameraControlTrigger : MonoBehaviour
{
    public CustomInspectorObjects customInspectorObjects;
    private Collider2D _coll;

    private void Start()
    {
        _coll = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (customInspectorObjects.targetCamera != null)
            {
                CameraManager.instance.ActivateCameraFromTrigger(customInspectorObjects.targetCamera);
            }

            if (customInspectorObjects.panCameraOnContact)
            {
                CameraManager.instance.PanCameraOnContact(
                    customInspectorObjects.panDistance,
                    customInspectorObjects.panTime,
                    customInspectorObjects.panDirection,
                    false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (customInspectorObjects.panCameraOnContact)
            {
                CameraManager.instance.PanCameraOnContact(
                    customInspectorObjects.panDistance,
                    customInspectorObjects.panTime,
                    customInspectorObjects.panDirection,
                    true);
            }
        }
    }

    [System.Serializable]
    public class CustomInspectorObjects
    {
        public CinemachineCamera targetCamera;
        public bool panCameraOnContact = false;

        [HideInInspector] public PanDirection panDirection;
        [HideInInspector] public float panDistance = 3f;
        [HideInInspector] public float panTime = 0.35f;
    }

    public enum PanDirection
    {
        Up, Down, Left, Right
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CameraControlTrigger))]
    public class MyScriptEditor : Editor
    {
        CameraControlTrigger cameraControlTrigger;

        private void OnEnable()
        {
            cameraControlTrigger = (CameraControlTrigger)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (cameraControlTrigger.customInspectorObjects.panCameraOnContact)
            {
                cameraControlTrigger.customInspectorObjects.panDirection = (PanDirection)EditorGUILayout.EnumPopup("Camera Pan Direction",
                    cameraControlTrigger.customInspectorObjects.panDirection);
                cameraControlTrigger.customInspectorObjects.panDistance = EditorGUILayout.FloatField("Pan Distance",
                    cameraControlTrigger.customInspectorObjects.panDistance);
                cameraControlTrigger.customInspectorObjects.panTime = EditorGUILayout.FloatField("Pan Time",
                    cameraControlTrigger.customInspectorObjects.panTime);
            }

            if (GUI.changed)
                EditorUtility.SetDirty(cameraControlTrigger);
        }
    }
#endif
}