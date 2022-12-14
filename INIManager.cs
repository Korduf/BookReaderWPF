using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace DeepReadApp
{
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class INIManager
    {
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
        private static extern int GetPrivateString(string section, string key, string def, StringBuilder buffer, int size, string path);
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
        private static extern int WritePrivateString(string section, string key, string str, string path);
        
        private readonly string _path;
        private const int Size = 1024;

        public INIManager(string outerpath)
        {
            _path = outerpath;
        }

        public string GetPrivateString(string oSection, string oKey)
        {
            var buffer = new StringBuilder(Size);
            GetPrivateString(oSection, oKey, null, buffer, Size, _path);
            return buffer.ToString();
        }

        public void WritePrivateString(string oSection, string oKey, string oValue)
        {
            WritePrivateString(oSection, oKey, oValue, _path);
        }
    }
}