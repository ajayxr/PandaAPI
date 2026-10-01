namespace PandaAPI.Services
{
    public static class CpfValidator
    {
        public static string Normalize(string cpf)
        {
            return cpf.Replace(".", "").Replace("-", "");
        }

        public static bool IsNumeric(string cpf)
        {
            return long.TryParse(cpf, out _);
        }

        public static bool IsNotDistinct(string cpf)
        {
            if (cpf.Distinct().Count() == 1)
                return false;
            return true;
        }

        public static bool IsValid(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;
            cpf = Normalize(cpf);
            if (cpf.Length != 11 || !IsNumeric(cpf))
                return false;
            if (!IsNotDistinct(cpf))
                return false;
            var firstNineDigits = cpf.Substring(0, 9);
            var firstCheckDigit = CalculateCheckDigit(firstNineDigits);
            var secondCheckDigit = CalculateCheckDigit(firstNineDigits + firstCheckDigit);
            return cpf.EndsWith(firstCheckDigit.ToString() + secondCheckDigit.ToString());
        }

        private static int CalculateCheckDigit(string cpf)
        {
            int sum = 0;
            for (int i = 0; i < cpf.Length; i++)
            {
                sum += (cpf[i] - '0') * (cpf.Length + 1 - i);
            }
            int remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }
    }
}
