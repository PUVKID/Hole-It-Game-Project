using UnityEngine;
[CreateAssetMenu(menuName="HoleIt/Theme Data")]
public class ThemeData : ScriptableObject
{
    public string themeId;
    public string displayName;
    public Color background = new Color(0.55f,0.73f,0.96f);
    public Color floorA = new Color(0.58f,0.35f,0.78f);
    public Color floorB = new Color(0.51f,0.28f,0.7f);
    public Color rimColor = new Color(0.3f,1f,0.82f);
    public Color lightColor = Color.white;
    public Color accent = new Color(0.3f,1f,0.82f);
    public AudioClip music;
}
