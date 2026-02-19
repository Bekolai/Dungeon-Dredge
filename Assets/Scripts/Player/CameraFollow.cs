using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.3f;
    public Vector3 offset = new Vector3(0, 0, 0);
    private Vector3 velocity = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 targetCamPos = target.position - target.transform.forward*0.2f + offset;
            
            transform.position = Vector3.SmoothDamp(transform.position, targetCamPos, ref velocity, smoothTime);
        }
    }
}
