"""Stage three-way presentation merges; never replace target code with unresolved conflicts."""
from pathlib import Path
import json, subprocess

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT.parent / 'gatefall-unity'
OUT = ROOT / 'TestResults/chapter2-design-20261107/merge'
OUT.mkdir(parents=True, exist_ok=True)
presentation = 'Assets/Scripts/Presentation/'
rows = []
names = ['ArenaView.CombatEffects.cs','ArenaView.cs','GatefallArt.cs','MobileHud.Theme.cs','MobileHud.cs','CampaignController.cs','CampaignHud.cs','GatefallGame.cs']
for relative in [presentation + name for name in names] + ['Assets/Editor/PrototypeTools.cs']:
    name = Path(relative).name
    backup = 'home-ward-20261106' if name in ['CampaignController.cs','CampaignHud.cs','GatefallGame.cs'] else 'urban-ward-20261106'
    base = SOURCE / 'Backups' / backup / name
    base_ref=str(base) if base.exists() else 'git:ab2ba77:'+relative
    if base.exists():
        base_text=base.read_text(encoding='utf-8-sig')
    else:
        base_text=subprocess.run(['git','-C',str(SOURCE),'show','ab2ba77:'+relative],check=True,capture_output=True).stdout.decode('utf-8-sig')
    (OUT/(name+'.base')).write_text(base_text,encoding='utf-8',newline='\n')
    for suffix, path in [('target',ROOT/relative),('style',SOURCE/relative)]:
        (OUT/(name+'.'+suffix)).write_text(path.read_text(encoding='utf-8-sig'),encoding='utf-8',newline='\n')
    result = subprocess.run(['git','merge-file','--diff3','--stdout','-L','chapter2-0.30.2','-L','recorded-common-base','-L','approved-style-1.3',str(OUT/(name+'.target')),str(OUT/(name+'.base')),str(OUT/(name+'.style'))],capture_output=True)
    if result.returncode < 0 or result.returncode > 127:
        raise RuntimeError(result.stderr.decode(errors='replace'))
    merged = OUT/(name+'.merged')
    merged.write_bytes(result.stdout)
    rows.append(dict(path=relative,base=base_ref,conflicts=result.returncode,staged=str(merged.relative_to(ROOT))))
    print(name+': '+('clean' if result.returncode==0 else str(result.returncode)+' conflicts'))
(OUT/'report.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('Staged only; target files not yet changed.')
