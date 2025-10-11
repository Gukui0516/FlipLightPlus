using UnityEngine;

/// <summary>
/// 손전등에 감지될 수 있는 객체가 구현해야 하는 인터페이스
/// Enemy 오브젝트에 붙여서 사용
/// </summary>
public interface IDetectable
{
    /// <summary>
    /// 빛 안에 들어왔을 때 호출됨
    /// </summary>
    void OnDetected(GameObject detector);
    
    /// <summary>
    /// 빛에서 벗어났을 때 호출됨
    /// </summary>
    void OnDetectionLost(GameObject detector);
}