using System;
using System.Security.Cryptography;
using System.Text;

namespace GamePackBuilder.GUI
{
    /// <summary>
    /// Шифрует/расшифровывает секретные строки через Windows DPAPI.
    /// Привязано к текущей учётной записи Windows — на другом ПК
    /// расшифровать не получится (и это правильно).
    /// </summary>
    internal static class SecretProtector
    {
        public static string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";

            byte[] data = Encoding.UTF8.GetBytes(plain);
            byte[] encrypted = ProtectedData.Protect(
                data, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(encrypted);
        }

        public static string Unprotect(string? encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64)) return "";

            try
            {
                byte[] encrypted = Convert.FromBase64String(encryptedBase64);
                byte[] data = ProtectedData.Unprotect(
                    encrypted, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                // Другой компьютер, другая учётка или повреждённый файл.
                // Молча возвращаем пустую строку — пользователь введёт пароль заново.
                return "";
            }
        }
    }
}