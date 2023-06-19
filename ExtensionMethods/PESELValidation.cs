namespace InternetVotingApplication.ExtensionMethods
{
    public static class PeselValidation
    {
        public static bool IsValidPESEL(string input)
        {
            int[] weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };

            if (input.Length != 11)
                return false;

            int controlSum = CalculateControlSum(input, weights);
            int controlNumber = (10 - (controlSum % 10)) % 10;

            int lastDigit = int.Parse(input[^1].ToString());

            return controlNumber == lastDigit;
        }

        private static int CalculateControlSum(string input, int[] weights)
        {
            int controlSum = 0;
            for (int i = 0; i < input.Length - 1; i++)
            {
                controlSum += weights[i] * int.Parse(input[i].ToString());
            }
            return controlSum;
        }
    }
}
