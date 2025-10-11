using UnityEngine;

public class HoldRotation : MonoBehaviour
{
    [SerializeField] float holdAngle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Euler(transform.localRotation.x, transform.localRotation.y, holdAngle);
    }
}
