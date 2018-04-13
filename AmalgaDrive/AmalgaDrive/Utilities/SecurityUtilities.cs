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

        public static bool EqualsOrdinal(this SecureString text1, SecureString text2, bool ignoreCase = false)
        {
            if (text1 == text2)
                return true;

            if (text1 == null)
                return text2 == null;

            if (text2 == null)
                return false;

            if (text1.Length != text2.Length)
                return false;

            var b1 = IntPtr.Zero;
            var b2 = IntPtr.Zero;
            try
            {
                b1 = Marshal.SecureStringToBSTR(text1);
                b2 = Marshal.SecureStringToBSTR(text2);
                return CompareStringOrdinal(b1, text1.Length, b2, text2.Length, ignoreCase) == CSTR_EQUAL;
            }
            finally
            {
                if (b1 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(b1);
                }

                if (b2 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(b2);
                }
            }
        }

        public static bool EqualsOrdinal(this SecureString text1, string text2, bool ignoreCase = false)
        {
            if (text1 == null)
                return text2 == null;

            if (text2 == null)
                return false;

            if (text1.Length != text2.Length)
                return false;

            var b = IntPtr.Zero;
            try
            {
                b = Marshal.SecureStringToBSTR(text1);
                return CompareStringOrdinal(b, text1.Length, text2, text2.Length, ignoreCase) == CSTR_EQUAL;
            }
            finally
            {
                if (b != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(b);
                }
            }
        }

        public static string ToInsecureString(this SecureString text)
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

        private const int CSTR_EQUAL = 2;

        [DllImport("kernel32")]
        private static extern int CompareStringOrdinal(IntPtr lpString1, int cchCount1, IntPtr lpString2, int cchCount2, bool bIgnoreCase);

        [DllImport("kernel32")]
        private static extern int CompareStringOrdinal(IntPtr lpString1, int cchCount1, [MarshalAs(UnmanagedType.LPWStr)] string lpString2, int cchCount2, bool bIgnoreCase);
    }
}
