using NUnit.Framework;
using TMPro;
using UnityEngine;

public class ReferenceTypographyTests {
    [Test] public void FontUsesOriginalAtlasAndMeasuredGlyphAdvance(){
        var font=Resources.Load<TMP_FontAsset>("ReferenceTypography/LinikSansBlack");
        Assert.IsNotNull(font);Assert.AreEqual("Linik Sans",font.faceInfo.familyName);Assert.AreEqual("Black",font.faceInfo.styleName);
        Assert.AreEqual(AtlasPopulationMode.Static,font.atlasPopulationMode);
        Assert.AreEqual(512,font.atlasTextures[0].width);Assert.AreEqual(512,font.atlasTextures[0].height);
        Assert.AreEqual(99,font.glyphTable.Count);Assert.AreEqual(99,font.characterTable.Count);
        for(int c=32;c<=126;c++)Assert.IsTrue(font.HasCharacter(c),"Missing original Latin character "+c);
        Assert.AreEqual(48.484375f,font.characterLookupTable[65].glyph.metrics.horizontalAdvance,.0001f);
        Assert.AreEqual(6,font.material.GetFloat("_GradientScale"),.0001f);
    }
    [Test] public void ButtonAndTitleMaterialsKeepOriginalOutlineAndUnderlay(){
        var green=Resources.Load<Material>("ReferenceTypography/Materials/Outline_Overlay_Green_Button");
        var purple=Resources.Load<Material>("ReferenceTypography/Materials/Outline_Overlay_Purple_Title");
        Assert.IsNotNull(green);Assert.IsNotNull(purple);Assert.AreEqual("TextMeshPro/Distance Field",green.shader.name);
        Assert.IsTrue(green.IsKeywordEnabled("UNDERLAY_ON"));Assert.AreEqual(.325f,green.GetFloat("_OutlineWidth"),.0001f);
        Assert.AreEqual(-.62f,green.GetFloat("_UnderlayOffsetY"),.0001f);
        Assert.AreEqual(.5294118f,purple.GetColor("_OutlineColor").b,.0001f);
    }
}
