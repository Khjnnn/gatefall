param([ValidateSet('representatives','bosses','all','merge-followup')][string]$Scope='all')

$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root 'TestResults/chapter3-5-prototype-20261110'
New-Item $out -ItemType Directory -Force | Out-Null
$scratch=Join-Path $out ([Guid]::NewGuid().ToString('N'))
New-Item $scratch -ItemType Directory | Out-Null

function Update-Source([string]$source,[string]$old,[string]$replacement) {
    $index=$source.IndexOf($old,[StringComparison]::Ordinal)
    if($index -lt 0 -or $source.IndexOf($old,$index+$old.Length,[StringComparison]::Ordinal) -ge 0){
        throw "Expected a unique prototype insertion: $old"
    }
    $source.Remove($index,$old.Length).Insert($index,$replacement)
}

# Compile the prototype mechanics and read-only measurements against a temporary copy of the live core.
$paths=[Collections.Generic.List[string]]::new()
try {
foreach($file in Get-ChildItem "$root/Assets/Scripts/Core/*.cs"){
    $source=[IO.File]::ReadAllText($file.FullName)
    switch($file.Name) {
        'BattlefieldLayouts.cs' {
            $source=Update-Source $source 'Sockets = PlaceSockets(d, stage)' 'Sockets = d.Sockets?.Length == stage.SocketCount ? d.Sockets : PlaceSockets(d, stage)'
        }
        'Campaign.cs' {
            $source=Update-Source $source 'list.AddRange(ChapterTwoCampaign.Create());' 'list.AddRange(ChapterTwoCampaign.Opening());'
            $source=Update-Source $source 'list.Add(ChapterTwoCampaign.RailEntry(list.Count));' 'list.AddRange(Gatefall.Tests.ChapterPrototypeReview.ProgressionStages(list.Count));'
            $source=Update-Source $source 'list.AddRange(ChapterTwoCampaign.RailContinuation(list.Count));' ''
            $source=Update-Source $source 'if(kind==TowerKind.Scatter || kind==TowerKind.Barrage || kind==TowerKind.Piercer || kind==TowerKind.Surge)' 'if(kind==TowerKind.Accelerator)return Chapter==5; if(kind==TowerKind.Scatter || kind==TowerKind.Barrage || kind==TowerKind.Piercer || kind==TowerKind.Surge)'
        }
        'ChapterTwoCampaign.cs' {
            $source=Update-Source $source 'static CampaignStage[] Opening()' 'internal static CampaignStage[] Opening()'
        }
        'TowerCatalog.cs' {
            $source=Update-Source $source 'Piercer, Surge }' 'Piercer, Surge, Accelerator }'
            $source=Update-Source $source 'line:.65f))' @'
line:.65f)),
            D(TowerKind.Accelerator,"가속등","공격 포대 둘 지원",1,45,40,TowerTag.None,S(0,1,4,2),S(0,1,4.2f,2),S(0,1,4.4f,2))
'@
        }
        'RaonCombat.cs' {
            $source=Update-Source $source 'else if(kind!=TowerKind.Barrier && s.LineHalfWidth==0)s.Interval*=.85f;' 'else if(kind!=TowerKind.Barrier && s.LineHalfWidth==0 && kind!=TowerKind.Accelerator)s.Interval*=.85f;'
            $source=Update-Source $source 't.Kind!=TowerKind.Marker && t.Kind!=TowerKind.Piercer && t.Commission<=0' 't.Kind!=TowerKind.Marker && t.Kind!=TowerKind.Piercer && t.Kind!=TowerKind.Accelerator && t.Commission<=0'
        }
        'DefenseSimulation.cs' {
            $source=Update-Source $source 'tower.Cooldown-=dt; if(tower.Cooldown>0)continue;' 'if(tower.Kind==TowerKind.Accelerator)continue; tower.Cooldown-=dt*PrototypeRecharge(tower); if(tower.Cooldown>0)continue;'
            $source=Update-Source $source 'for(int i=0;i<Math.Min(hits.Count,stats.MaxTargets);i++)Damage(hits[i],stats.Damage,attack,origin);' @'
for(int i=0;i<Math.Min(hits.Count,stats.MaxTargets);i++)Damage(hits[i],stats.Damage,attack,origin);
            if(tower.Kind==TowerKind.Piercer)PrototypeLineVolley?.Invoke(Math.Min(hits.Count,stats.MaxTargets));
'@
            $source=Update-Source $source 'Enemies.Add(enemy);' 'Enemies.Add(enemy); PrototypeSpawned?.Invoke(enemy,g);'
            $source=Update-Source $source 'e.Health-=damage; e.HitFlash=' 'PrototypeDamage?.Invoke(e,p,Math.Min(e.Health,damage)); e.Health-=damage; e.HitFlash='
        }
    }
    $path=Join-Path $scratch $file.Name
    [IO.File]::WriteAllText($path,$source)
    $paths.Add($path)
}

    Add-Type -Path @($paths.ToArray()+"$root/Tests/ChapterPrototypeReview.cs")
    $report=[Gatefall.Tests.ChapterPrototypeReview]::Run($out,$Scope)
    foreach($group in $report.Representatives | Group-Object Map,Seed){
        if($group.Count -ne 2){throw "Missing A/B pair: $($group.Name)"}
        $a,$b=$group.Group
        if(($a.Cards -join '/') -ne ($b.Cards -join '/') -or $a.Budget -ne $b.Budget -or
            $a.CreditsSpent -ne $b.CreditsSpent -or $a.Scheduled -ne $b.Scheduled -or $a.WaveCalls -ne 0 -or $b.WaveCalls -ne 0){
            throw "Unmatched cards, spending or schedule: $($group.Name)"
        }
    }
    foreach($group in $report.Representatives | Where-Object Id -eq 'C05-S006' | Group-Object Seed){
        $reference=$group.Group[0]
        foreach($other in $group.Group){
            if((ConvertTo-Json -InputObject $reference.ChoicePrefix -Depth 6 -Compress) -ne (ConvertTo-Json -InputObject $other.ChoicePrefix -Depth 6 -Compress) -or
                (ConvertTo-Json -InputObject $reference.Sites -Compress) -ne (ConvertTo-Json -InputObject $other.Sites -Compress) -or
                ($reference.Paths -join '/') -ne ($other.Paths -join '/')){
                throw "5-6 shared and split controls drifted before the purchase choice: $($group.Name)"
            }
        }
    }
    $file=Join-Path $out "$Scope.json"
    $report | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $file -Encoding utf8
    $report
}
finally {
    # Only remove the source copies created by this invocation.
    foreach($path in $paths){Remove-Item -LiteralPath $path -ErrorAction SilentlyContinue}
    Remove-Item -LiteralPath $scratch -ErrorAction SilentlyContinue
}
