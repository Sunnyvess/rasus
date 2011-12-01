using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FerTorrent
{
    public static class Config
    {
        static string filePath = "\\Download";
        public static string FilePath
        {
            get { return Config.filePath; }
            set { Config.filePath = value; }
        }

        static string metaPath = "\\Torrent";
        public static string MetaPath
        {
            get { return Config.metaPath; }
            set { Config.metaPath = value; }
        }

        static int maxIncoming = 10;
        public static int MaxIncoming
        {
            get { return Config.maxIncoming; }
            set { Config.maxIncoming = value; }
        }

        static int maxOutgoing = 5;
        public static int MaxOutgoing
        {
            get { return Config.maxOutgoing; }
            set { Config.maxOutgoing = value; }
        }

        public static void LoadConfig()
        {
        }

        public static void SaveConfig()
        {
        }
    }
}
