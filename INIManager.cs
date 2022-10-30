using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DeepReadApp
{
    public class INIManager
    {
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
        private static extern int GetPrivateString(string section, string key, string def, StringBuilder buffer, int size, string path);
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
        private static extern int WritePrivateString(string section, string key, string str, string path);
        
        private string path = null;
        private const int Size = 1024;

        public string Path
        {
            get { return path; }
            set { path = value; }
        }
        
        
        public INIManager(string outerpath)
        {
            path = outerpath;
        }

        public string GetPrivateString(string oSection, string oKey)
        {
            StringBuilder buffer = new StringBuilder(Size);
            GetPrivateString(oSection, oKey, null, buffer, Size, path);
            return buffer.ToString();
        }

        public void WritePrivateString(string oSection, string oKey, string oValue)
        {
            WritePrivateString(oSection, oKey, oValue, path);
        }
    }
}