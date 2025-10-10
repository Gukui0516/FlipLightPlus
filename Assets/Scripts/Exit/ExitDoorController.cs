using UnityEngine;

public class ExitDoorController : MonoBehaviour
{
    [SerializeField] private Transform _doorRotatePoint; // 문 회전 중심점
    [SerializeField] private float _openAngle = 180f; // 문이 열리는 각도
    [SerializeField] private float _openOpenTime = 1.5f; // 문이 열리는 시간
    [SerializeField] private float _originRotateY; // 문이 닫혀있을 때의 회전 각도 Y

    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    


}
