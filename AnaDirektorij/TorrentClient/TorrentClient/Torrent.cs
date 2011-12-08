using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FairTorrent
{
    public class FileInfo
    {
        public int Length {get;set;}
        public string Path {get;set;}
    }
    
    public class TorrentInfo
    {
        public List<FileInfo> Files {get;set;}
        public string Name {get;set;}
        public int PieceLength {get;set;}
        public byte[] Pieces {get;set;}
    }
    
    public class Torrent
    {
        public string Announce { get; set; }
        public List<string> AnnounceList { get; set; }
        public string Comment { get; set; }
        public string CreatedBy { get; set; }
        public int CreationDate { get; set; }
        public string ErrCallback { get; set; }
        public List<string> Errors { get; set; }
        public TorrentInfo Info { get; set; }
        public string LogCallback { get; set; }

        public Torrent()
        {
        }
        
        public Torrent(string pathToTorrentFile)
        {
            DecodeTorrent(File.ReadAllBytes(pathToTorrentFile));
        }

        public Torrent(byte[] bencodedData)
        {
            DecodeTorrent(bencodedData);
        }

        public void DecodeTorrent(byte[] bencodedData)
        {
            Dictionary<string, object> dict = BEncoder.BEncoder.Decode(bencodedData);

            this.Announce = (string)dict["announce"];
            
            this.AnnounceList = new List<string>();
            foreach (object item in (List<object>)dict["announce-list"])
            {
                this.AnnounceList.Add((string)((List<object>)item)[0]);
            }

            this.Comment = (string)dict["comment"];
            
            this.CreatedBy = (string)dict["created by"];
            
            this.CreationDate = (int)dict["creation date"];
            
            this.ErrCallback = (string)dict["err_callback"];
            
            this.Errors = new List<string>();
            foreach (object item in (List<object>)dict["errors"])
            {
                this.Errors.Add((string)((List<object>)item)[0]);
            }

            Dictionary<string,object> infoDict = (Dictionary<string,object>)dict["info"];
            List<FileInfo> files = new List<FileInfo>();
            foreach (object f in (List<object>)infoDict["files"])
            {
                Dictionary<string, object> fileDict = (Dictionary<string, object>)f;
                files.Add(new FileInfo()
                {
                    Length = (int)fileDict["length"],
                    Path = (string)((List<object>)fileDict["path"])[0]
                });
            }

            this.Info = new TorrentInfo()
            {
                Name = (string)infoDict["name"],
                PieceLength = (int)infoDict["piece length"],
                Pieces = (byte[])infoDict["pieces"],
                Files = files
            };

            this.LogCallback = (string)dict["log_callback"];
        }

    }
}
