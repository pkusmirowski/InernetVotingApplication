namespace InernetVotingApplication.ExtensionMethods
{
    public static class EmailValidation
    {
        // Metoda służąca do sprawdzania poprawności adresu e-mail.
        // Wykorzystuje klasę System.Net.Mail.MailAddress do utworzenia obiektu reprezentującego adres e-mail,
        // a następnie porównuje go z pierwotnym adresem e-mail. 
        // Metoda zwraca true, jeśli adres e-mail jest poprawny, a false, jeśli nie jest.
        public static bool IsValidEmail(string email)
        {
            return System.Net.Mail.MailAddress.TryCreate(email, out var addr) && addr.Address == email;
        }
    }
}