using UnityEngine;

/// <summary>
/// 반전 상태에 따라 색상을 변경할 수 있는 컴포넌트가 구현하는 인터페이스
/// </summary>
public interface IInvertibleColor
{
    /// <summary>
    /// 반전 색상으로 변경
    /// </summary>
    void SetInvertedColor();

    /// <summary>
    /// 원본 색상으로 복구
    /// </summary>
    void SetOriginalColor();
}