using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

namespace AmalgaDrive.Utilities
{
    public static class SecurityUtilities
    {
        public static byte[] EncryptSecureString(this SecureString text, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (text == null)
                return null;

            // note: .NET's ProtectedData uses byte[] which defeats the whole SecureString purpose... we want IntPtr!
            var cipherTextBlob = new DATA_BLOB();
            var bstr = Marshal.SecureStringToBSTR(text);
            bool ok = false;
            try
            {
                var plainTextBlob = new DATA_BLOB();
                plainTextBlob.cbData = text.Length * sizeof(char); // BSTR is unicode, and we don't add terminating zero
                plainTextBlob.pbData = bstr;

                ok = CryptProtectData(ref plainTextBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    scope == DataProtectionScope.LocalMachine ? CRYPTPROTECT_LOCAL_MACHINE | CRYPTPROTECT_UI_FORBIDDEN : CRYPTPROTECT_UI_FORBIDDEN,
                    ref cipherTextBlob);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(bstr);
            }

            if (!ok)
                return null;

            try
            {
                var cipherText = new byte[cipherTextBlob.cbData];
                Marshal.Copy(cipherTextBlob.pbData, cipherText, 0, cipherTextBlob.cbData);
                return cipherText;
            }
            finally
            {
                RtlZeroMemory(cipherTextBlob.pbData, new IntPtr(cipherTextBlob.cbData));
                Marshal.FreeHGlobal(cipherTextBlob.pbData);
            }
        }

        public static SecureString DecryptSecureString(this byte[] data, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            if (data == null)
                return null;

            var plainTextBlob = new DATA_BLOB();
            var cipherBlob = new DATA_BLOB();
            cipherBlob.cbData = data.Length;
            cipherBlob.pbData = Marshal.AllocHGlobal(data.Length);

            try
            {
                Marshal.Copy(data, 0, cipherBlob.pbData, cipherBlob.cbData);
                if (!CryptUnprotectData(ref cipherBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    scope == DataProtectionScope.LocalMachine ? CRYPTPROTECT_LOCAL_MACHINE | CRYPTPROTECT_UI_FORBIDDEN : CRYPTPROTECT_UI_FORBIDDEN,
                    ref plainTextBlob))
                    return null;
            }
            finally
            {
                Marshal.FreeHGlobal(cipherBlob.pbData);
            }

            var text = new SecureString();
            try
            {
                var ptr = plainTextBlob.pbData;
                for (int i = 0; i < plainTextBlob.cbData / sizeof(char); i++)
                {
                    text.AppendChar((char)Marshal.ReadInt16(ptr));
                    ptr = new IntPtr(ptr.ToInt64() + sizeof(char));
                }
                return text;
            }
            finally
            {
                RtlZeroMemory(plainTextBlob.pbData, new IntPtr(plainTextBlob.cbData));
                Marshal.FreeHGlobal(plainTextBlob.pbData);
            }
        }

        public static string ToInsecureString(SecureString text)
        {
            if (text == null)
                return null;

            var ptr = Marshal.SecureStringToBSTR(text);
            try
            {
                return Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
        }

        public static SecureString ToSecureString(string text)
        {
            if (text == null)
                return null;

            var s = new SecureString();
            foreach (char c in text)
            {
                s.AppendChar(c);
            }
            return s;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;
        private const int CRYPTPROTECT_LOCAL_MACHINE = 0x4;

        [DllImport("crypt32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, string szDataDescr, IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("crypt32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, string szDataDescr, IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("kernel32")]
        private static extern void RtlZeroMemory(IntPtr address, IntPtr length);
    }
}
