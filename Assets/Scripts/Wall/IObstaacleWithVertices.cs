using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 장애물이 꼭지점 정보를 제공하는 인터페이스
/// ObstacleVertexCache 같은 컴포넌트에서 구현
/// </summary>
public interface IObstacleWithVertices
{
    /// <summary>
    /// 월드 좌표계의 꼭지점 리스트 반환
    /// </summary>
    List<Vector2> GetWorldVertices();
}