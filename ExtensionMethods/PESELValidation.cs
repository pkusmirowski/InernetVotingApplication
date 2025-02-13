namespace InternetVotingApplication.ExtensionMethods
{
    public static class PeselValidation
    {
        private static readonly int[] Weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };

        /// <summary>
        /// Validates if the provided PESEL number is correct.
        /// </summary>
        /// <param name="input">The PESEL number to validate.</param>
        /// <returns>True if the PESEL number is valid; otherwise, false.</returns>
        public static bool IsValidPESEL(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length != 11 || !long.TryParse(input, out _))
            {
                return false;
            }

            int controlSum = CalculateControlSum(input);
            int controlNumber = (10 - (controlSum % 10)) % 10;

            int lastDigit = input[^1] - '0';

            return controlNumber == lastDigit;
        }

        private static int CalculateControlSum(string input)
        {
            int controlSum = 0;
            for (int i = 0; i < Weights.Length; i++)
            {
                controlSum += Weights[i] * (input[i] - '0');
            }
            return controlSum;
        }
    }
}