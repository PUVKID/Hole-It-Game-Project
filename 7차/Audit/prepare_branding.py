from pathlib import Path

source = Path(__file__).resolve().parent.parent / 'Source'
replacements = {
    'ProjectSettings/ProjectSettings.asset': [("productName: Gaeun's Hole", 'productName: Hole It!'), ('bundleVersion: 6.0.0', 'bundleVersion: 7.0.0'), ('AndroidBundleVersionCode: 8', 'AndroidBundleVersionCode: 9')],
    'Assets/Scenes/Main.unity': [('m_Name: GaeunGame', 'm_Name: HoleGame')],
    'Assets/Editor/ReferenceBuilder.cs': [('new GameObject("GaeunGame")', 'new GameObject("HoleGame")'), ('PlayerSettings.productName="Gaeun\'s Hole"', 'PlayerSettings.productName="Hole It!"'), ('bundleVersion="6.0.0"', 'bundleVersion="7.0.0"'), ('bundleVersionCode=8', 'bundleVersionCode=9')],
    'Assets/Editor/ReferenceSixthBuilder.cs': [('PlayerSettings.productName="Gaeun\'s Hole"', 'PlayerSettings.productName="Hole It!"'), ('bundleVersion="6.0.0"', 'bundleVersion="7.0.0"'), ('bundleVersionCode=8', 'bundleVersionCode=9')],
    'Assets/Scripts/UI/Korean.cs': [('가은이의 작은 블랙홀 모험', '작은 홀 모험')],
    'Assets/Editor/HoleItBuilder.SmoothEdition.cs': [('가은이의 쏙쏙 수집 놀이', '쏙쏙 수집 놀이')],
    'Build-Android.ps1': [('GaeunHoleBuild-', 'HoleItBuild-')],
    'README.txt': [("Gaeun's Hole - Edition 6", 'Hole It! - Edition 7')],
    'AGENTS.md': [('Edition 6', 'Edition 7')]
}
for relative, changes in replacements.items():
    path = source / relative
    text = path.read_text(encoding='utf-8-sig')
    for old, new in changes:
        text = text.replace(old, new)
    path.write_text(text, encoding='utf-8')
print('Edition 7 display branding and version updated; persistent save keys unchanged.')
