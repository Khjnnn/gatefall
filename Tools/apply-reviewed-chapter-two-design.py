"""Apply the explicitly reviewed presentation conflicts; preserve chapter-two gameplay and assets."""
from pathlib import Path
import hashlib,json,re,shutil

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT.parent/'gatefall-unity'
OUT=ROOT/'TestResults/chapter2-design-20261107/merge'
if not re.search(r'^  bundleVersion: 0\.30\.2\s*$',(ROOT/'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8-sig'),re.M):
    raise SystemExit('One-time integration already applied or target changed. Do not replay over adapted chapter2 UI.')
pattern=re.compile(r'^<<<<<<<.*?\n(.*?)^\|\|\|\|\|\|\|.*?\n(.*?)^=======\n(.*?)^>>>>>>>.*?\n',re.M|re.S)
rows=json.loads((OUT/'report.json').read_text(encoding='utf-8'))
resolutions={
    'MobileHud.cs':['target','target','style','style','target','target','target','style'],
    'MobileHud.Theme.cs':['style']*7,
    'GatefallArt.cs':['target','union','style','art-resource-union','target','target'],
    'CampaignController.cs':['chapter-controller'],
    'CampaignHud.cs':['chapter-header'],
    'PrototypeTools.cs':['style'],
}
resolved=[]
for row in rows:
    path=ROOT/row['path'];name=path.name
    if path.read_text(encoding='utf-8-sig')!=(OUT/(name+'.target')).read_text(encoding='utf-8-sig'):
        raise RuntimeError('Target changed since staging: '+name)
    text=(ROOT/row['staged']).read_text(encoding='utf-8-sig')
    blocks=list(pattern.finditer(text));choices=resolutions.get(name,[])
    if len(blocks)!=len(choices):raise RuntimeError('Unreviewed conflict count: '+name)
    count=[0]
    def replace(match):
        index=count[0];count[0]+=1
        choice=choices[index];target,base,style=match.groups()
        if choice=='target':return target
        if choice=='style':return style
        if choice=='union':return target+style
        if choice=='chapter-controller':return target.replace('= CampaignScreen.City;','= CampaignScreen.Home;')
        if choice=='chapter-header':return target.replace(', false);',', true);')
        if choice=='art-resource-union':
            return style.replace('Resources.Load<Texture2D>("Art/V5/" + name);','Resources.Load<Texture2D>("Art/V5/" + name) ?? Resources.Load<Texture2D>("Art/Chapter2/"+name);')+'                if(texture==null && (name.StartsWith("tower-scatter-") || name.StartsWith("tower-barrage-")))texture=ChapterTowerTexture(name);\n'
        raise RuntimeError(choice)
    text=pattern.sub(replace,text)
    if any(marker in text for marker in ['<<<<<<<','>>>>>>>','|||||||']):raise RuntimeError('Unresolved: '+name)
    if '\ufffd' in text:raise RuntimeError('Replacement character: '+name)
    resolved.append((path,text,row,choices))
# All files resolved before writing any target source.
for path,text,row,choices in resolved:
    path.write_text(text,encoding='utf-8',newline='\n')
    row['reviewedChoices']=choices
    row['resolvedSha256']=hashlib.sha256(path.read_bytes()).hexdigest()

for relative in ['Assets/Resources/Art/HomeWard','Assets/Resources/Art/UrbanWard']:
    shutil.copytree(SOURCE/relative,ROOT/relative,dirs_exist_ok=True)
    shutil.copy2(SOURCE/(relative+'.meta'),ROOT/(relative+'.meta'))
for relative in ['Assets/Scripts/Presentation/MobileHud.Home.cs','Assets/Editor/HomeUiAudit.cs','Assets/Editor/UrbanWardAssetAudit.cs','Assets/Editor/CityBarrierUiAudit.cs']:
    shutil.copy2(SOURCE/relative,ROOT/relative)
    if not (ROOT/(relative+'.meta')).exists():shutil.copy2(SOURCE/(relative+'.meta'),ROOT/(relative+'.meta'))
for name in ['sync-city-barrier-style.py','validate-city-barrier-style.py','sync-urban-ward-assets.py']:
    shutil.copy2(SOURCE/'Tools'/name,ROOT/'Tools'/name)

manifest=ROOT/'Docs/Design/city-barrier-ui-20261106/tokens.json'
chapter=json.loads(manifest.read_text(encoding='utf-8-sig'))
style=json.loads((SOURCE/'Docs/Design/city-barrier-ui-20261106/tokens.json').read_text(encoding='utf-8-sig'))
for key,token in chapter['tokens'].items():
    if key not in style['tokens']:style['tokens'][key]=token
known={r['runtimeKind'] for r in style['roles']}
style['roles'] += [r for r in chapter['roles'] if r['runtimeKind'] not in known]
style['version']='1.3.1'
style['provenance']['newDesignAdjustments'].append('0.30.2 챕터2에 전체 디자인 통합; 산탄/탄막 역할 토큰과 현재 챕터별 홈 진행도 보존')
manifest.write_text(json.dumps(style,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

for relative in ['Assets/Editor/HomeUiAudit.cs','Assets/Editor/CityBarrierUiAudit.cs','Assets/Editor/UrbanWardAssetAudit.cs']:
    path=ROOT/relative
    text=path.read_text(encoding='utf-8-sig')
    for old in ['TestResults/home-ward-20261106','TestResults/urban-ward-20261106']:
        text=text.replace(old,'TestResults/chapter2-design-20261107')
    path.write_text(text,encoding='utf-8')
settings=ROOT/'ProjectSettings/ProjectSettings.asset'
text=settings.read_text(encoding='utf-8-sig').replace('bundleVersion: 0.30.2','bundleVersion: 0.30.3').replace('AndroidBundleVersionCode: 142','AndroidBundleVersionCode: 143')
settings.write_text(text,encoding='utf-8')
(OUT/'resolved.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('Applied9 reviewed presentation merges; copied original assets/audits; style1.3.1 with chapter2 roles; version0.30.3/143. Core untouched.')
