using System.Security.Cryptography;

namespace PandaAPI.Services
{
    public static class CnpjValidator
    {
        private static readonly int[] FirstCheckDigitWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        private static readonly int[] SecondCheckDigitWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        private const string AlphaNumericCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string GenerateNumeric()
        {
            return Generate(alphanumeric: false);
        }

        public static string GenerateAlphanumeric()
        {
            return Generate(alphanumeric: true);
        }

        public static string Normalize(string cnpj)
        {
            var decodedCnpj = Uri.UnescapeDataString(cnpj);
            return decodedCnpj.Replace(".", "").Replace("/", "").Replace("-", "").Trim().ToUpperInvariant();
        }

        public static string Format(string cnpj)
        {
            var normalizedCnpj = Normalize(cnpj);
            return normalizedCnpj.Length == 14
                ? $"{normalizedCnpj[..2]}.{normalizedCnpj[2..5]}.{normalizedCnpj[5..8]}/{normalizedCnpj[8..12]}-{normalizedCnpj[12..]}"
                : cnpj;
        }

        public static bool IsValid(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
            {
                return false;
            }

            var normalized = Normalize(cnpj);
            if (normalized.Length != 14 || normalized.Distinct().Count() == 1)
            {
                return false;
            }

            for (var i = 0; i < normalized.Length; i++)
            {
                var isAsciiDigit = normalized[i] is >= '0' and <= '9';
                var isAsciiLetter = normalized[i] is >= 'A' and <= 'Z';
                if (!isAsciiDigit && (!isAsciiLetter || i >= 12))
                {
                    return false;
                }
            }

            var firstCheckDigit = CalculateCheckDigit(normalized, FirstCheckDigitWeights);
            var secondCheckDigit = CalculateCheckDigit(normalized, SecondCheckDigitWeights);

            return normalized[12] - '0' == firstCheckDigit &&
                   normalized[13] - '0' == secondCheckDigit;
        }

        private static int CalculateCheckDigit(string cnpj, int[] weights)
        {
            var sum = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                var value = cnpj[i] is >= '0' and <= '9'
                    ? cnpj[i] - '0'
                    : cnpj[i] - 48;
                sum += value * weights[i];
            }

            var remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }

        private static string Generate(bool alphanumeric)
        {
            string cnpj;
            do
            {
                var baseCnpj = string.Concat(Enumerable.Range(0, 12)
                    .Select(_ => alphanumeric
                        ? AlphaNumericCharacters[RandomNumberGenerator.GetInt32(AlphaNumericCharacters.Length)]
                        : (char)('0' + RandomNumberGenerator.GetInt32(0, 10))));
                var firstCheckDigit = CalculateCheckDigit(baseCnpj, FirstCheckDigitWeights);
                var secondCheckDigit = CalculateCheckDigit(baseCnpj + firstCheckDigit, SecondCheckDigitWeights);
                cnpj = baseCnpj + firstCheckDigit + secondCheckDigit;
            }
            while (!IsValid(cnpj) || (alphanumeric && !cnpj[..12].Any(char.IsLetter)));

            return cnpj;
        }
    }
}
