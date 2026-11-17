using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Gatefall.Core
{
    internal static class SaveLineage
    {
        internal static string Hash(byte[] data)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        internal static bool Valid(CampaignProfile p)
        {
            if(!ValidHash(p.CommitHash) || p.AncestorHashes==null || p.AncestorHashes.Count>CampaignProfile.AncestorLimit)return false;
            var seen=new HashSet<string>(StringComparer.Ordinal){p.CommitHash};
            foreach(string hash in p.AncestorHashes)if(!ValidHash(hash) || !seen.Add(hash))return false;return true;
        }
        static bool ValidHash(string hash)
        {if(hash==null || hash.Length!=64)return false;foreach(char c in hash)if(!(c>='0' && c<='9') && !(c>='a' && c<='f'))return false;return true;}
        internal static void Advance(CampaignProfile p)
        {
            string next=Hash(CampaignStore.Encode(p));p.AncestorHashes.Insert(0,p.CommitHash);
            if(p.AncestorHashes.Count>CampaignProfile.AncestorLimit)p.AncestorHashes.RemoveAt(CampaignProfile.AncestorLimit);
            p.CommitHash=next;
        }
    }
}
