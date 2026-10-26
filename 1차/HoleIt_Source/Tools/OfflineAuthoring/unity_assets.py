"""Deterministic Unity text assets. No Unity process is emulated by this writer."""
from pathlib import Path
import json,hashlib,math,struct,re,copy
import yaml
import numpy as np
R=Path(__file__).resolve().parents[2]
class Flow(dict):pass
class Dumper(yaml.SafeDumper):
 def ignore_aliases(self,data):return True
Dumper.add_representer(Flow,lambda d,v:d.represent_mapping('tag:yaml.org,2002:map',v,flow_style=True))
def flow(**kw):return Flow(kw)
def guid(path):return hashlib.md5(('HoleIt.Completed.v8/'+str(path)).encode()).hexdigest()
def ref(path=None,id=0,type=2):return flow(fileID=id,**({'guid':guid(path),'type':type} if path else {}))
def vec(*args):return Flow(zip('xyzw',map(lambda v:round(float(v),7),args)))
def color(c):return Flow(zip('rgba',map(float,list(c)+([1] if len(c)==3 else []))))
def dump(obj):return yaml.dump(obj,Dumper=Dumper,sort_keys=False,allow_unicode=True,width=150)
def meta(path,kind=None,fileid=None,user='HoleItGenerated:v8'):
 path=str(path);f=R/(path+'.meta');f.parent.mkdir(parents=True,exist_ok=True)
 if f.exists():return
 if kind is None:kind='MonoImporter' if path.endswith('.cs') else 'ShaderImporter' if path.endswith('.shader') else 'NativeFormatImporter' if path.endswith(('.asset','.mat')) else 'PrefabImporter' if path.endswith('.prefab') else 'AssemblyDefinitionImporter' if path.endswith('.asmdef') else 'DefaultImporter'
 data={'fileFormatVersion':2,'guid':guid(path)}
 imp={'externalObjects':{},'userData':user,'assetBundleName':'','assetBundleVariant':''}
 if kind=='MonoImporter':imp.update({'serializedVersion':2,'defaultReferences':[],'executionOrder':0,'icon':ref()})
 if fileid is not None:imp['mainObjectFileID']=fileid
 data[kind]=imp;f.write_text(dump(data))
def asset(path,cls,fileid,body,name=None):
 f=R/path;f.parent.mkdir(parents=True,exist_ok=True)
 name=name or {114:'MonoBehaviour',43:'Mesh',21:'Material'}.get(cls)
 f.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!'+str(cls)+' &'+str(fileid)+'\n'+dump({name:body}));meta(path,fileid=fileid)
def common():return dict(m_ObjectHideFlags=0,m_CorrespondingSourceObject=ref(),m_PrefabInstance=ref(),m_PrefabAsset=ref())
def so(path,script,data):
 b=common();b.update(m_GameObject=ref(),m_Enabled=1,m_EditorHideFlags=0,m_Script=ref(script,11500000,3),m_Name=Path(path).stem,m_EditorClassIdentifier='');b.update(data);asset(path,114,11400000,b)
def material(path,shader,colors=None,floats=None,queue=-1):
 b=common();b.update(serializedVersion=8,m_Name=Path(path).stem,m_Shader=ref(shader,4800000,3),m_Parent=ref(),m_ModifiedSerializedProperties=0,m_ValidKeywords=[],m_InvalidKeywords=[],m_LightmapFlags=4,m_EnableInstancingVariants=1,m_DoubleSidedGI=0,m_CustomRenderQueue=queue,stringTagMap={},disabledShaderPasses=[],m_LockedProperties='',m_SavedProperties={'serializedVersion':3,'m_TexEnvs':[],'m_Ints':[],'m_Floats':[{k:float(v)} for k,v in (floats or {}).items()],'m_Colors':[{k:color(v)} for k,v in (colors or {}).items()]},m_BuildTextureStacks=[])
 asset(path,21,2100000,b)
def mesh_asset(path,verts,faces,normals=None,colors=None,uv=None):
 v=np.asarray(verts,dtype=np.float32);faces=np.asarray(faces,dtype=np.int64)
 assert np.isfinite(v).all() and len(v)>0 and faces.min()>=0 and faces.max()<len(v)
 if normals is None:
  n=np.zeros(v.shape,dtype=np.float32);fn=np.cross(v[faces[:,1]]-v[faces[:,0]],v[faces[:,2]]-v[faces[:,0]])
  for j in range(3):np.add.at(n,faces[:,j],fn)
  mag=np.linalg.norm(n,axis=1,keepdims=True);n/=np.maximum(mag,1e-8)
 else:n=np.asarray(normals,dtype=np.float32)
 if uv is None:uv=np.zeros((len(v),2),dtype=np.float32)
 if colors is None:colors=np.ones((len(v),4),dtype=np.float32)
 co=np.asarray(colors,dtype=np.float32)
 dat=np.concatenate([v,n,np.asarray(uv,dtype=np.float32),co],axis=1).astype('<f4').tobytes().hex()
 # Keep geometric cross-product normals and winding consistent (Unity uses a left-handed camera space).
 indices=faces.reshape(-1);fmt=0 if len(v)<=65535 else 1
 ib=indices.astype('<u2' if fmt==0 else '<u4').tobytes().hex()
 mins=v.min(axis=0);maxs=v.max(axis=0);bounds={'m_Center':vec(*((mins+maxs)/2)),'m_Extent':vec(*((maxs-mins)/2))}
 channels=[]
 for i in range(14):
  off,dim={0:(0,3),1:(12,3),3:(32,4),4:(24,2)}.get(i,(0,0));channels.append({'stream':0,'offset':off,'format':0,'dimension':dim})
 b=common();b.update(m_Name=Path(path).stem,serializedVersion=12,m_SubMeshes=[{'serializedVersion':2,'firstByte':0,'indexCount':len(indices),'topology':0,'baseVertex':0,'firstVertex':0,'vertexCount':len(v),'localAABB':bounds}],m_Shapes={'vertices':[],'shapes':[],'channels':[],'fullWeights':[]},m_BindPose=[],m_BoneNameHashes='',m_RootBoneNameHash=0,m_BonesAABB=[],m_VariableBoneCountWeights={'m_Data':''},m_MeshCompression=0,m_IsReadable=1,m_KeepVertices=1,m_KeepIndices=1,m_IndexFormat=fmt,m_IndexBuffer=ib,m_VertexData={'serializedVersion':3,'m_VertexCount':len(v),'m_Channels':channels,'m_DataSize':len(v)*48,'_typelessdata':dat},m_LocalAABB=bounds,m_MeshUsageFlags=0,m_CookingOptions=30,m_BakedConvexCollisionMesh='',m_BakedTriangleCollisionMesh='',m_MeshMetrics=[1,1],m_MeshOptimizationFlags=1,m_StreamData={'serializedVersion':2,'offset':0,'size':0,'path':''})
 asset(path,43,4300000,b)
 return {'vertices':len(v),'triangles':len(faces),'bounds_min':mins.tolist(),'bounds_max':maxs.tolist()}
class Scene:
 def __init__(self):self.docs=[];self.next=1000;self.nodes=[]
 def add(self,cls,name,body):
  i=self.next;self.next+=1;self.docs.append((cls,i,name,body));return i
 def go(self,name,parent=None,pos=(0,0,0),scale=(1,1,1),rotation=(0,0,0,1),active=True,layer=0,rect=None):
  gb=common();gb.update(serializedVersion=6,m_Component=[],m_Layer=layer,m_Name=name,m_TagString='Untagged',m_Icon=ref(),m_NavMeshLayer=0,m_StaticEditorFlags=0,m_IsActive=int(active));gi=self.add(1,'GameObject',gb)
  tb=common();tb.update(m_GameObject=ref(id=gi),serializedVersion=2,m_LocalRotation=vec(*rotation),m_LocalPosition=vec(*pos),m_LocalScale=vec(*scale),m_ConstrainProportionsScale=0,m_Children=[],m_Father=ref(id=parent['tid'] if parent else 0),m_LocalEulerAnglesHint=vec(0,0,0))
  if rect is not None:
   tb.update(m_AnchorMin=vec(*rect.get('amin',(.5,.5))),m_AnchorMax=vec(*rect.get('amax',rect.get('amin',(.5,.5)))),m_AnchoredPosition=vec(*rect.get('pos',(0,0))),m_SizeDelta=vec(*rect.get('size',(100,100))),m_Pivot=vec(*rect.get('pivot',(.5,.5))))
  ti=self.add(224 if rect is not None else 4,'RectTransform' if rect is not None else 'Transform',tb);gb['m_Component'].append({'component':ref(id=ti)})
  node={'gid':gi,'tid':ti,'go':gb,'transform':tb,'name':name,'components':{},'parent':parent};self.nodes.append(node)
  if parent:parent['transform']['m_Children'].append(ref(id=ti))
  return node
 def component(self,node,cls,name,fields=None,script=None):
  b=common();b['m_GameObject']=ref(id=node['gid'])
  if cls==114:b.update(m_Enabled=1,m_EditorHideFlags=0,m_Script=ref(script,11500000,3),m_Name='',m_EditorClassIdentifier='')
  if fields:b.update(fields)
  i=self.add(cls,name,b);node['go']['m_Component'].append({'component':ref(id=i)});node['components'][Path(script).stem if script else name]=i;return i
 def mono(self,node,script,fields=None):return self.component(node,114,'MonoBehaviour',fields,script)
 def render(self,node,mesh,mat,shadows=1):
  self.component(node,33,'MeshFilter',{'m_Mesh':ref(mesh,4300000)})
  return self.component(node,23,'MeshRenderer',dict(m_Enabled=1,m_CastShadows=shadows,m_ReceiveShadows=1,m_DynamicOccludee=1,m_StaticShadowCaster=0,m_MotionVectors=1,m_LightProbeUsage=0,m_ReflectionProbeUsage=0,m_RayTracingMode=0,m_RayTraceProcedural=0,m_RenderingLayerMask=1,m_RendererPriority=0,m_Materials=[ref(mat,2100000)],m_StaticBatchInfo={'firstSubMesh':0,'subMeshCount':0},m_StaticBatchRoot=ref(),m_ProbeAnchor=ref(),m_LightProbeVolumeOverride=ref(),m_ScaleInLightmap=1,m_ReceiveGI=1,m_PreserveUVs=0,m_IgnoreNormalsForChartDetection=0,m_ImportantGI=0,m_StitchLightmapSeams=1,m_SelectedEditorRenderState=3,m_MinimumChartSize=4,m_AutoUVMaxDistance=.5,m_AutoUVMaxAngle=89,m_LightmapParameters=ref(),m_SortingLayerID=0,m_SortingLayer=0,m_SortingOrder=0,m_AdditionalVertexStreams=ref()))
 def collider(self,node,shape='Box',size=(1,1,1),center=(0,0,0),trigger=False):
  b=dict(m_Material=ref(),m_IncludeLayers={'serializedVersion':2,'m_Bits':0},m_ExcludeLayers={'serializedVersion':2,'m_Bits':0},m_LayerOverridePriority=0,m_IsTrigger=int(trigger),m_ProvidesContacts=0,m_Enabled=1,serializedVersion=3,m_Center=vec(*center))
  if shape=='Box':b['m_Size']=vec(*size)
  else:b['m_Radius']=float(size[0])
  return self.component(node,65 if shape=='Box' else 135,shape+'Collider',b)
 def body(self,node):return self.component(node,54,'Rigidbody',dict(serializedVersion=5,m_Mass=1,m_LinearDamping=0,m_AngularDamping=.05,m_CenterOfMass=vec(0,0,0),m_InertiaTensor=vec(1,1,1),m_InertiaRotation=vec(0,0,0,1),m_IncludeLayers={'serializedVersion':2,'m_Bits':0},m_ExcludeLayers={'serializedVersion':2,'m_Bits':0},m_ImplicitCom=1,m_ImplicitTensor=1,m_UseGravity=0,m_IsKinematic=1,m_Interpolate=0,m_Constraints=0,m_CollisionDetection=0))
 def save(self,path):
  f=R/path;f.parent.mkdir(parents=True,exist_ok=True)
  t='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
  for cls,i,name,b in self.docs:t+=f'--- !u!{cls} &{i}\n'+dump({name:b})
  f.write_text(t);meta(path)
 def cref(self,node,comp):return ref(id=node['components'][comp])
