using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FairTorrent
{
    public class FileInfo
    {
        public int Length { get; set; }
        public string Path { get; set; }
        public string MD5sum { get; set; }
    }

    public abstract class TorrentInfo
    {
        public int PieceLength { get; set; }
        public byte[] Pieces { get; set; }
        public int Private { get; set; }
    }

    public class SingleFileTorrentInfo : TorrentInfo
    {
        /// <summary>
        /// Enkapsulacija podataka o jednom fajlu u istu strukturu kao kod multi-file torrenta; Name = Path
        /// </summary>
        public FileInfo File { get; set; }
    }

    public class MultiFileTorrentInfo : TorrentInfo
    {
        public string Name { get; set; }
        public List<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// Memorijska reprezentacija .torrent fajla
    /// </summary>
    public class Torrent
    {
        public string Announce { get; set; }

        /// <summary>
        /// Opcionalno - ako je null nema u torrentu!
        /// </summary>
        public List<string> AnnounceList { get; set; }
        
        /// <summary>
        /// Opcionalno - ako je null nema u torrentu!
        /// </summary>
        public string Comment { get; set; }
        
        /// <summary>
        /// Opcionalno - ako je null nema u torrentu!
        /// </summary>
        public string CreatedBy { get; set; }
        
        /// <summary>
        /// Opcionalno - ako je 0 nema u torrentu!
        /// </summary>
        public int CreationDate { get; set; }
        
        /// <summary>
        /// Opcionalno - ako je null nema u torrentu!
        /// </summary>
        public string Encoding { get; set; }
        
        /// <summary>
        /// Može biti ili single-file ili multi-file; uvijek se koriste izvedene klase!
        /// </summary>
        public TorrentInfo Info { get; set; }

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

            // Try-catch blokovi se koriste kako bi opcionalne vrijednosti bile nabavljene ako se mogu
            // ili ako se ne mogu postavlja se null ili default vrijednost (za int to je 0)
            // Ukoliko neko opcionalno svojstvo nije u torrentu poziv tog keya će baciti exception koji će biti catchan te umjesto vrijednost null/default spremljen

            try
            {
                this.AnnounceList = new List<string>();
                foreach (object item in (List<object>)dict["announce-list"])
                {
                    foreach (object subitem in (List<object>)item)
                    {
                        this.AnnounceList.Add((string)subitem);
                    }
                }
            }
            catch
            {
                this.AnnounceList = null;
            }

            try
            {
                this.Comment = (string)dict["comment"];
            }
            catch
            {
                this.Comment = null;
            }

            try
            {
                this.CreatedBy = (string)dict["created by"];
            }
            catch
            {
                this.CreatedBy = null;
            }

            try
            {
                this.CreationDate = (int)dict["creation date"];
            }
            catch
            {
                this.CreationDate = default(int);
            }

            try
            {
                this.Encoding = (string)dict["encoding"];
            }
            catch
            {
                this.Encoding = null;
            }

            Dictionary<string, object> infoDict = (Dictionary<string, object>)dict["info"];
            bool isTorrentMultiFile = false;

            try
            {
                var testCast = (List<object>)infoDict["files"];
                isTorrentMultiFile = true;
            }
            catch
            {
                isTorrentMultiFile = false;
            }

            if (!isTorrentMultiFile)
            {
                this.Info = new SingleFileTorrentInfo();
            }
            else
            {
                this.Info = new MultiFileTorrentInfo();
            }
            this.Info.PieceLength = (int)infoDict["piece length"];
            this.Info.Pieces = (byte[])infoDict["pieces"];
            try
            {
                this.Info.Private = (int)infoDict["private"];
            }
            catch
            {
                this.Info.Private = 0;
            }
            if (!isTorrentMultiFile)
            {
                FileInfo file = new FileInfo();
                file.Length = (int)infoDict["length"];
                file.Path = (string)infoDict["name"];
                try
                {
                    file.MD5sum = (string)infoDict["md5sum"];
                }
                catch
                {
                    file.MD5sum = null;
                }

                ((SingleFileTorrentInfo)this.Info).File = file;
            }
            else
            {
                ((MultiFileTorrentInfo)this.Info).Name = (string)infoDict["name"];
                
                List<FileInfo> files = new List<FileInfo>();
                foreach (object f in (List<object>)infoDict["files"])
                {
                    FileInfo file = new FileInfo();
                    Dictionary<string, object> fileDict = (Dictionary<string, object>)f;
                    file.Length = (int)fileDict["length"];
                    try
                    {
                        file.MD5sum = (string)fileDict["md5sum"];
                    }
                    catch
                    {
                        file.MD5sum = null;
                    }
                    file.Path="";
                    foreach (object dirOrFile in (List<object>)fileDict["path"])
                    {
                        file.Path+=(string)dirOrFile+"\\";
                    }
                    file.Path = file.Path.TrimEnd(new char[] { '\\' });

                    files.Add(file);
                }

                ((MultiFileTorrentInfo)this.Info).Files = files;
            }      
        }

    }
}
