namespace PandaAPI.Services
{
    public class CpfService
    {
        public string Normalize(string cpf)
        {
            return cpf.Replace(".", "").Replace("-", "");
        }

        public bool IsNumeric(string cpf)
        {
            return long.TryParse(cpf, out _);
        }
    }
}
