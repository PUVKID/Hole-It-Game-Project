using UnityEngine;

/// <summary>먹을 수 있는 오브젝트의 종류 (미션 대상 구분용). 크기(SizeTier)와 별개로 관리한다.</summary>
public enum ObjectType
{
    Ball = 0,
    Box = 1,
    Food = 2,
    Tree = 3,
    Car = 4,
    Decoration = 5,
    Obstacle = 6,
    TimeBoost = 7,
    Toy = 8,
    Kitchen = 9
}

/// <summary>미션 대상이 아닌 오브젝트를 먹었을 때의 처리 방식.</summary>
public enum WrongTargetBehavior
{
    /// <summary>먹을 수 있음. 미션 카운트는 오르지 않고 점수만 일반 처리.</summary>
    None = 0,
    /// <summary>Hole 크기와 관계없이 먹을 수 없음.</summary>
    Blocked = 1,
    /// <summary>먹으면 시간/점수 패널티.</summary>
    Penalty = 2
}

/// <summary>오브젝트 흡수 단계.</summary>
public enum SwallowState
{
    Normal = 0,
    Attracted = 1,
    Falling = 2,
    Consumed = 3
}

/// <summary>게임 전체 상태.</summary>
public enum GameState
{
    Title = 0,
    Ready = 1,
    Playing = 2,
    Paused = 3,
    Clearing = 4,
    LevelClear = 5,
    Failed = 6
}
