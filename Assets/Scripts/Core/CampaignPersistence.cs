using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Gatefall.Core
{
    public sealed class StageProgress
    {
        public int Stars, CrisisStars;
        public float Erosion=1;
        public bool FirstCleared;
        public StageProgress Copy() => (StageProgress)MemberwiseClone();
        public bool Valid() => Stars>=0 && Stars<=3 && CrisisStars>=0 && CrisisStars<=3 && !float.IsNaN(Erosion) && Erosion>=0 && Erosion<=1 && FirstCleared==(Stars>0);
        public static bool ValidId(string id)
        {if(string.IsNullOrEmpty(id) || id.Length>96)return false;foreach(char c in id)if(!char.IsLetterOrDigit(c) && c!='-' && c!='_' && c!='.')return false;return true;}
    }
    // v3: stable stage IDs and unbounded-by-machine-word seen IDs. Indexed arrays exist only in memory.
    // Unknown future stage records survive a read/write. Checksums detect corruption, not save editing.
    public sealed class CampaignStore
    {
        sealed class NewerProfileException : IOException {public NewerProfileException(string message):base(message){}}
        readonly string path;
        public string Warning { get; private set; }="";
        public CampaignStore(string path){this.path=path;}
        static byte[] Checksum(byte[] payload)
        {using(var sha=SHA256.Create()){var hash=sha.ComputeHash(payload);var all=new byte[32+payload.Length];Buffer.BlockCopy(hash,0,all,0,32);Buffer.BlockCopy(payload,0,all,32,payload.Length);return all;}}
        static void WriteProgress(BinaryWriter w,int stars,int crisis,float erosion,bool first)
        {w.Write(stars);w.Write(crisis);w.Write(erosion);w.Write(first);}
        static void ReadProgress(BinaryReader r,CampaignProfile p,int index)
        {p.BestStars[index]=r.ReadInt32();p.CrisisStars[index]=r.ReadInt32();p.BestErosion[index]=r.ReadSingle();p.FirstCleared[index]=r.ReadBoolean();}
        static void WriteIds(BinaryWriter w,HashSet<string> ids)
        {var ordered=new List<string>(ids);ordered.Sort(StringComparer.Ordinal);w.Write(ordered.Count);foreach(var id in ordered)w.Write(id);}
        static HashSet<string> ReadIds(BinaryReader r)
        {int n=r.ReadInt32();if(n<0 || n>4096)throw new InvalidDataException("Invalid seen ID count");var set=new HashSet<string>();for(int i=0;i<n;i++){string id=r.ReadString();if(!StageProgress.ValidId(id) || !set.Add(id))throw new InvalidDataException("Invalid/duplicate seen ID");}return set;}
        public static byte[] Encode(CampaignProfile p)
        {
            if(p==null || !p.Valid())throw new InvalidDataException("Invalid campaign profile");
            using(var mem=new MemoryStream()){
                using(var w=new BinaryWriter(mem,Encoding.UTF8,true)){
                    w.Write("GATEFALL-RAON");w.Write(3);
                    w.Write(p.MagicCores);w.Write(p.SelectedHunter);w.Write(p.StoryMask);w.Write(p.TutorialMask);w.Write(p.Muted);
                    foreach(int v in p.Research)w.Write(v);foreach(int v in p.Growth)w.Write(v);w.Write(p.Loadout);
                    w.Write(p.PendingRun);w.Write(p.PendingStage<0 ? "" : CampaignCatalog.Stages[p.PendingStage].Id);w.Write(p.PendingCrisis);w.Write(p.LastRewardRun);w.Write(p.LastReward);
                    var ids=new List<string>();foreach(var s in CampaignCatalog.Stages)ids.Add(s.Id);
                    foreach(string id in p.UnmappedProgress.Keys)if(CampaignCatalog.FindStage(id)<0)ids.Add(id);
                    ids.Sort(StringComparer.Ordinal);w.Write(ids.Count);
                    foreach(string id in ids){w.Write(id);int i=CampaignCatalog.FindStage(id);
                        if(i>=0)WriteProgress(w,p.BestStars[i],p.CrisisStars[i],p.BestErosion[i],p.FirstCleared[i]);
                        else {var v=p.UnmappedProgress[id];WriteProgress(w,v.Stars,v.CrisisStars,v.Erosion,v.FirstCleared);}}
                    WriteIds(w,p.StorySeenIds);WriteIds(w,p.TutorialSeenIds);
                }return Checksum(mem.ToArray());
            }
        }
        public static CampaignProfile Decode(byte[] data){int version;return Decode(data,out version);}
        static CampaignProfile Decode(byte[] data,out int version)
        {
            if(data==null || data.Length<64 || data.Length>1048576)throw new InvalidDataException("Invalid profile length");
            using(var sha=SHA256.Create()){var hash=sha.ComputeHash(data,32,data.Length-32);for(int i=0;i<32;i++)if(hash[i]!=data[i])throw new InvalidDataException("Profile checksum mismatch");}
            using(var mem=new MemoryStream(data,32,data.Length-32,false))using(var r=new BinaryReader(mem,Encoding.UTF8)){
                if(r.ReadString()!="GATEFALL-RAON")throw new InvalidDataException("Unknown profile format");
                version=r.ReadInt32();if(version>3)throw new NewerProfileException("A newer save version was found; original and backup preserved");if(version<1)throw new InvalidDataException("Unknown profile version");
                var p=new CampaignProfile{MagicCores=r.ReadInt32(),SelectedHunter=r.ReadInt32(),StoryMask=r.ReadInt32(),TutorialMask=r.ReadInt32(),Muted=r.ReadBoolean()};
                if(version<3){
                    for(int i=0;i<6;i++)ReadProgress(r,p,i);
                    for(int i=0;i<4;i++)p.Research[i]=r.ReadInt32();for(int i=0;i<3;i++)p.Growth[i]=r.ReadInt32();
                    p.PendingRun=r.ReadString();p.PendingStage=r.ReadInt32();p.PendingCrisis=r.ReadBoolean();p.LastRewardRun=r.ReadString();p.LastReward=r.ReadInt32();
                    if(version==2){for(int i=6;i<14;i++)ReadProgress(r,p,i);p.Loadout=r.ReadInt32();}
                    for(int i=0;i<14;i++){if((p.StoryMask&(1<<i))!=0)p.StorySeenIds.Add(CampaignCatalog.Stages[i].Id);if((p.TutorialMask&(1<<i))!=0)p.TutorialSeenIds.Add(CampaignCatalog.Stages[i].Id);}
                }else{
                    for(int i=0;i<4;i++)p.Research[i]=r.ReadInt32();for(int i=0;i<3;i++)p.Growth[i]=r.ReadInt32();p.Loadout=r.ReadInt32();
                    p.PendingRun=r.ReadString();string pending=r.ReadString();p.PendingStage=pending==""?-1:CampaignCatalog.FindStage(pending);
                    if(pending!="" && p.PendingStage<0)throw new NewerProfileException("Pending operation belongs to a newer campaign; original save preserved");
                    p.PendingCrisis=r.ReadBoolean();p.LastRewardRun=r.ReadString();p.LastReward=r.ReadInt32();
                    int n=r.ReadInt32();if(n<0 || n>4096)throw new InvalidDataException("Invalid stage count");var ids=new HashSet<string>();
                    for(int x=0;x<n;x++){
                        string id=r.ReadString();if(!StageProgress.ValidId(id) || !ids.Add(id))throw new InvalidDataException("Invalid/duplicate stage ID");int i=CampaignCatalog.FindStage(id);
                        if(i>=0)ReadProgress(r,p,i);else p.UnmappedProgress.Add(id,new StageProgress{Stars=r.ReadInt32(),CrisisStars=r.ReadInt32(),Erosion=r.ReadSingle(),FirstCleared=r.ReadBoolean()});
                    }
                    p.StorySeenIds=ReadIds(r);p.TutorialSeenIds=ReadIds(r);
                }
                if(mem.Position!=mem.Length || !p.Valid())throw new InvalidDataException("Invalid profile values");return p;
            }
        }
        public CampaignProfile Load()
        {
            Warning="";if(!File.Exists(path) && !File.Exists(path+".bak"))return new CampaignProfile();
            byte[] bytes;CampaignProfile p;int version;
            try{bytes=File.ReadAllBytes(path);p=Decode(bytes,out version);}
            catch(NewerProfileException){throw;}
            catch(Exception){try{bytes=File.ReadAllBytes(path+".bak");p=Decode(bytes,out version);Warning="저장 파일을 백업에서 복구했습니다.";}
                catch(Exception e){throw new InvalidDataException("프로필과 백업을 읽을 수 없습니다. 원본 파일을 보존했습니다.",e);}}
            if(version<3){
                try{string legacy=path+".legacy-v"+version+".bak";
                    if(!File.Exists(legacy))File.WriteAllBytes(legacy,bytes);
                    // Verify a byte-for-byte legacy backup before replacing anything.
                    byte[] saved=File.ReadAllBytes(legacy);if(Convert.ToBase64String(saved)!=Convert.ToBase64String(bytes))throw new IOException("Legacy backup differs");
                    Save(p);Warning="기존 진행을 보존하고 2챕터 저장 형식으로 이행했습니다.";
                }catch(Exception e){Warning="저장 형식 이행 보류 · 기존 진행 유지: "+e.Message;}
            }return p;
        }
        public void Save(CampaignProfile p)
        {
            byte[] data=Encode(p);Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string tmp=path+".tmp";File.WriteAllBytes(tmp,data);Decode(File.ReadAllBytes(tmp));
            if(File.Exists(path)){bool good=true;try{Decode(File.ReadAllBytes(path));}catch{good=false;}File.Replace(tmp,path,good?path+".bak":null);}
            else File.Move(tmp,path);
        }
    }
}
