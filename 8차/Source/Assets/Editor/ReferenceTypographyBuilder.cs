using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>Rebuilds the supplied reference's static TMP font without a substitute TTF.</summary>
public static class ReferenceTypographyBuilder {
    const string Root="Assets/Resources/ReferenceTypography";
    [Serializable] class FloatValue { public string name; public float value; }
    [Serializable] class ColorValue { public string name; public Color value; }
    [Serializable] class MaterialSource { public string key,name; public string[] keywords; public FloatValue[] floats; public ColorValue[] colors; }
    [Serializable] class MaterialSources { public MaterialSource[] materials; }
    [Serializable] class FaceSource {
        public int m_FaceIndex,m_UnitsPerEM;
        public string m_FamilyName,m_StyleName;
        public float m_PointSize,m_Scale,m_LineHeight,m_AscentLine,m_CapLine,m_MeanLine,m_Baseline,m_DescentLine,
            m_SuperscriptOffset,m_SuperscriptSize,m_SubscriptOffset,m_SubscriptSize,m_UnderlineOffset,m_UnderlineThickness,
            m_StrikethroughOffset,m_StrikethroughThickness,m_TabWidth;
    }
    [Serializable] class MetricSource { public float m_Width,m_Height,m_HorizontalBearingX,m_HorizontalBearingY,m_HorizontalAdvance; }
    [Serializable] class RectSource { public int m_X,m_Y,m_Width,m_Height; }
    [Serializable] class GlyphSource { public uint m_Index; public MetricSource m_Metrics; public RectSource m_GlyphRect; public float m_Scale; public int m_AtlasIndex; }
    [Serializable] class CharacterSource { public uint m_Unicode,m_GlyphIndex; public float m_Scale; }
    [Serializable] class FontSource {
        public FaceSource m_FaceInfo;public GlyphSource[] m_GlyphTable;public CharacterSource[] m_CharacterTable;
        public int m_AtlasWidth,m_AtlasHeight,m_AtlasPadding,m_AtlasRenderMode,italicStyle,tabSize;
        public float normalStyle,normalSpacingOffset,boldStyle,boldSpacing;
    }
    sealed class PackageEntry { public string path; public byte[] asset,meta; }

    static void ReconstructFont(TMP_FontAsset font,Texture2D atlas,Material material,FontSource source){
        if(atlas==null)throw new InvalidOperationException("Original SDF atlas did not import");
        // Avoid EditorJsonUtility's native-object wrapper format and version-dependent
        // private nested-field deserialization. Write scalars explicitly and construct
        // every original Glyph / TMP_Character using the current public TextCore API.
        var serialized=new SerializedObject(font);
        serialized.FindProperty("m_Version").stringValue="1.1.0";
        var face=serialized.FindProperty("m_FaceInfo");
        foreach(var field in typeof(FaceSource).GetFields()){
            var property=face.FindPropertyRelative(field.Name);if(property==null)throw new InvalidOperationException("Unsupported FaceInfo field "+field.Name);
            object value=field.GetValue(source.m_FaceInfo);
            if(value is string text)property.stringValue=text;
            else if(property.propertyType==SerializedPropertyType.Integer)property.intValue=Convert.ToInt32(value);
            else property.floatValue=Convert.ToSingle(value);
        }
        serialized.FindProperty("m_AtlasWidth").intValue=source.m_AtlasWidth;
        serialized.FindProperty("m_AtlasHeight").intValue=source.m_AtlasHeight;
        serialized.FindProperty("m_AtlasPadding").intValue=source.m_AtlasPadding;
        serialized.FindProperty("m_AtlasRenderMode").intValue=source.m_AtlasRenderMode;
        serialized.FindProperty("m_AtlasPopulationMode").intValue=(int)AtlasPopulationMode.Static;
        var textures=serialized.FindProperty("m_AtlasTextures");textures.arraySize=1;textures.GetArrayElementAtIndex(0).objectReferenceValue=atlas;
        serialized.FindProperty("m_Material").objectReferenceValue=material;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        font.name="LinikSansBlack";font.normalStyle=source.normalStyle;font.normalSpacingOffset=source.normalSpacingOffset;
        font.boldStyle=source.boldStyle;font.boldSpacing=source.boldSpacing;font.italicStyle=(byte)source.italicStyle;font.tabSize=(byte)source.tabSize;
        var glyphs=new Dictionary<uint,Glyph>();font.glyphTable.Clear();
        foreach(var item in source.m_GlyphTable){
            var m=item.m_Metrics;var r=item.m_GlyphRect;
            var glyph=new Glyph(item.m_Index,new GlyphMetrics(m.m_Width,m.m_Height,m.m_HorizontalBearingX,m.m_HorizontalBearingY,m.m_HorizontalAdvance),new GlyphRect(r.m_X,r.m_Y,r.m_Width,r.m_Height),item.m_Scale,item.m_AtlasIndex);
            font.glyphTable.Add(glyph);glyphs.Add(glyph.index,glyph);
        }
        font.characterTable.Clear();
        foreach(var item in source.m_CharacterTable)font.characterTable.Add(new TMP_Character(item.m_Unicode,font,glyphs[item.m_GlyphIndex]){scale=item.m_Scale});
        font.ReadFontAssetDefinition();EditorUtility.SetDirty(font);
        if(font.characterTable.Count!=99||font.glyphTable.Count!=99||font.faceInfo.familyName!="Linik Sans"||font.atlasTextures[0]!=atlas)
            throw new InvalidOperationException("Original Linik Sans font reconstruction invalid: "+font.faceInfo.familyName+" / "+font.glyphTable.Count+" glyphs / "+font.characterTable.Count+" characters");
    }

    static byte[] ReadExactly(Stream stream,int count){
        var bytes=new byte[count];int offset=0;
        while(offset<count){int read=stream.Read(bytes,offset,count-offset);if(read<=0)throw new EndOfStreamException("Incomplete bundled TMP package");offset+=read;}
        return bytes;
    }
    static string TarString(byte[] bytes,int offset,int length)=>Encoding.UTF8.GetString(bytes,offset,length).TrimEnd('\0',' ');
    static void InstallBundledResourcesSynchronously(){
        string package=Path.Combine(EditorApplication.applicationContentsPath,"Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
        if(!File.Exists(package))throw new FileNotFoundException("Unity TMP essential resources missing",package);
        // ImportPackage queues work until the editor loop returns. A batch executeMethod
        // followed by -quit cannot wait for that queue, so read the local gzip/tar directly.
        var entries=new Dictionary<string,PackageEntry>();
        using(var file=File.OpenRead(package))using(var tar=new GZipStream(file,CompressionMode.Decompress)){
            while(true){
                byte[] header=ReadExactly(tar,512);bool empty=true;foreach(byte b in header)if(b!=0){empty=false;break;}if(empty)break;
                string name=TarString(header,0,100),prefix=TarString(header,345,155);if(prefix.Length>0)name=prefix+"/"+name;while(name.StartsWith("./",StringComparison.Ordinal))name=name.Substring(2);
                string sizeText=TarString(header,124,12).Trim();long size=sizeText.Length==0?0:Convert.ToInt64(sizeText,8);
                if(size<0||size>64*1024*1024)throw new InvalidDataException("Unexpected TMP package entry size");
                byte[] data=ReadExactly(tar,(int)size);int padding=(int)((512-size%512)%512);if(padding>0)ReadExactly(tar,padding);
                int slash=name.IndexOf('/');if(slash<=0)continue;string guid=name.Substring(0,slash),kind=name.Substring(slash+1);
                if(kind!="pathname"&&kind!="asset"&&kind!="asset.meta")continue;
                if(!entries.TryGetValue(guid,out var entry)){entry=new PackageEntry();entries.Add(guid,entry);}
                if(kind=="pathname")entry.path=Encoding.UTF8.GetString(data).TrimEnd('\0','\r','\n');
                else if(kind=="asset")entry.asset=data;else entry.meta=data;
            }
        }
        if(entries.Count==0)throw new InvalidDataException("Bundled TMP package contained no asset records");
        string project=Directory.GetParent(Application.dataPath).FullName;
        string allowed=Path.GetFullPath(Path.Combine(project,"Assets/TextMesh Pro"));
        foreach(var entry in entries.Values){
            if(string.IsNullOrEmpty(entry.path))continue;
            string destination=Path.GetFullPath(Path.Combine(project,entry.path.Replace('/',Path.DirectorySeparatorChar)));
            if(!destination.Equals(allowed,StringComparison.OrdinalIgnoreCase)&&!destination.StartsWith(allowed+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("TMP package path is outside its asset directory: "+entry.path);
            if(entry.asset==null)Directory.CreateDirectory(destination);
            else {Directory.CreateDirectory(Path.GetDirectoryName(destination));if(!File.Exists(destination))File.WriteAllBytes(destination,entry.asset);}
            if(entry.meta!=null&&!File.Exists(destination+".meta"))File.WriteAllBytes(destination+".meta",entry.meta);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/TextMesh Pro/Shaders/TMP_SDF.shader",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
    }

    [MenuItem("Hole It/Prepare Original Typography")]
    public static void Prepare(){
        const string shaderPath="Assets/TextMesh Pro/Shaders/TMP_SDF.shader";
        // Use the concrete asset path rather than a shader-name cache after installation.
        if(!File.Exists(shaderPath)||!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))InstallBundledResourcesSynchronously();
        AssetDatabase.ImportAsset(shaderPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if(shader==null)throw new InvalidOperationException("TMP Distance Field shader was not imported");
        string texturePath=Root+"/LinikSansBlackAtlas.png";
        AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=false;importer.sRGBTexture=false;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=512;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var sources=JsonUtility.FromJson<MaterialSources>(File.ReadAllText(Root+"/MaterialsSource.json"));
        foreach(var source in sources.materials){
            string path=Root+"/Materials/"+source.key+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.shader=shader;material.name=source.key;material.shaderKeywords=source.keywords;
            foreach(var property in source.floats)if(material.HasProperty(property.name))material.SetFloat(property.name,property.value);
            foreach(var property in source.colors)if(material.HasProperty(property.name))material.SetColor(property.name,property.value);
            material.mainTexture=atlas;EditorUtility.SetDirty(material);
        }
        string fontPath=Root+"/LinikSansBlack.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if(font==null){font=ScriptableObject.CreateInstance<TMP_FontAsset>();AssetDatabase.CreateAsset(font,fontPath);}
        ReconstructFont(font,atlas,AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Base.mat"),JsonUtility.FromJson<FontSource>(File.ReadAllText(Root+"/FontSource.json")));
        AssetDatabase.SaveAssets();
        if(font.characterTable.Count!=99||font.glyphTable.Count!=99||font.faceInfo.familyName!="Linik Sans")
            throw new InvalidOperationException("Original Linik Sans font did not reconstruct correctly");
        Debug.Log("Reference typography ready: Linik Sans Black, 99 glyphs, 512x512 original SDF, "+sources.materials.Length+" material variants.");
    }
}
