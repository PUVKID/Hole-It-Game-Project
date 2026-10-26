// Existing serialized values are preserved. New values are appended only.
public enum ObjectType { Ball=0, Box=1, Food=2, Tree=3, Car=4, Decoration=5, Obstacle=6, TimeBoost=7, Toy=8, Kitchen=9, Bomb=10 }
public enum WrongTargetBehavior { None=0, Blocked=1, Penalty=2 }
public enum SwallowState { Normal=0, Attracted=1, Falling=2, Consumed=3 }
public enum GameState { Title=0, Ready=1, Playing=2, Paused=3, Clearing=4, LevelClear=5, Failed=6, Loading=7, Boot=8 }
public enum ControlMode { Drag=0, FixedJoystick=1 }
public enum ColorVariant { None=0, Red=1, Blue=2, Yellow=3, Green=4, Pink=5, Purple=6, White=7 }
public enum MissionKind { ExactObject=0, ObjectCategory=1, Color=2, Tier=3, TimeBooster=4 }
