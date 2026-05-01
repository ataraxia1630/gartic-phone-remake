using System;

namespace InkEcho.Network.Core
{
    public static class RoomCode
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Generate(int length = 4)
        {
            var rng = new Random();
            var buf = new char[length];
            for (int i = 0; i < length; i++) buf[i] = Alphabet[rng.Next(Alphabet.Length)];
            return new string(buf);
        }

        public static bool IsValidShape(string code, int expectedLength)
        {
            if (string.IsNullOrEmpty(code) || code.Length != expectedLength) return false;
            foreach (var c in code) if (Alphabet.IndexOf(c) < 0) return false;
            return true;
        }

        public static string Normalize(string code) => code?.Trim().ToUpperInvariant();
    }
}
