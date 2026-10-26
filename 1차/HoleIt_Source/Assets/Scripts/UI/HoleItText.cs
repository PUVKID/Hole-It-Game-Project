using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
/// <summary>Built-in font backed uGUI text. No external font files or TMP Essential Resources are required.</summary>
[AddComponentMenu("HoleIt/Text")]
public class HoleItText : Text
{
    private static Font sharedFont;
    protected override void Awake(){base.Awake();EnsureFont();}
    protected override void OnEnable(){EnsureFont();base.OnEnable();}
    private void EnsureFont(){if(font!=null)return;if(sharedFont==null)sharedFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");font=sharedFont;}
    public void SetText(string value){text=value??string.Empty;}
    public void SetText(string format,int a){text=string.Format(CultureInfo.InvariantCulture,format,a);}
    public void SetText(string format,int a,int b){text=string.Format(CultureInfo.InvariantCulture,format,a,b);}
    public void SetText(string format,int a,int b,int c,int d){text=string.Format(CultureInfo.InvariantCulture,format,a,b,c,d);}
    public void SetText(string format,float a){text=string.Format(CultureInfo.InvariantCulture,format,a);}
    public void SetText(string format,float a,float b){text=string.Format(CultureInfo.InvariantCulture,format,a,b);}
    public void SetText(string format,float a,float b,float c){text=string.Format(CultureInfo.InvariantCulture,format,a,b,c);}
    public void SetText(string format,float a,float b,float c,float d){text=string.Format(CultureInfo.InvariantCulture,format,a,b,c,d);}
}
