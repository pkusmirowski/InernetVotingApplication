using System;

namespace InernetVotingApplication.ExtensionMethods
{
    public static class PESELValidation
    {
        public static bool IsValidPESEL(string input)
        {
            int[] weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
            bool result = false;
            if (input.Length == 11)
            {
                int controlSum = CalculateControlSum(input, weights);
                int controlNumber = controlSum % 10;
                controlNumber = 10 - controlNumber;

                if (controlNumber == 10)
                {
                    controlNumber = 0;
                }

                int lastDigit = Int32.Parse(input[^1].ToString());
                result = controlNumber == lastDigit;
            }
            return result;
        }

        private static int CalculateControlSum(string input, int[] weights, int offset = 0)
        {
            int controlSum = 0;
            for (int i = 0; i < input.Length - 1; i++)
            {
                controlSum += weights[i + offset] * Int32.Parse(input[i].ToString());
            }
            return controlSum;
        }
    }
}
