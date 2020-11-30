using System.Linq;

namespace InernetVotingApplication.ExtensionMethods
{
    public static class IDNumberValidation
    {
        const string availableChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static bool ValidateIdNumber(string IDnumber)
        {
            int checkSum, i;

            //Sprawdzenie czy pierwsze trzy znaki to litery
            for (i = 0; i < 3; i++)
            {
                if (GetLetterValue(IDnumber[i]) < 10)
                {
                    return false;
                }
            }

            //Sprawdzenie czy kolejne 6 znaków to cyfry
            for (i = 3; i < 9; i++)
            {
                if (GetLetterValue(IDnumber[i]) < 0 || GetLetterValue(IDnumber[i]) > 9)
                {
                    return false;
                }
            }

            //Sprawdzenie cyfry kontrolnej
            checkSum = CheckSum(IDnumber);
            return checkSum == GetLetterValue(IDnumber[3]);
        }

        private static int GetLetterValue(char character)
        {
            for (int i = 0; i < availableChars.Count(); i++)
            {
                if (character == availableChars[i])
                {
                    return i;
                }
            }
            return -1;
        }

        private static int CheckSum(string IDnumber)
        {
            int checkSum = 7 * GetLetterValue(IDnumber[0]);
            checkSum += 3 * GetLetterValue(IDnumber[1]);
            checkSum += 1 * GetLetterValue(IDnumber[2]);
            checkSum += 7 * GetLetterValue(IDnumber[4]);
            checkSum += 3 * GetLetterValue(IDnumber[5]);
            checkSum += 1 * GetLetterValue(IDnumber[6]);
            checkSum += 7 * GetLetterValue(IDnumber[7]);
            checkSum += 3 * GetLetterValue(IDnumber[8]);
            checkSum %= 10;
            return checkSum;
        }
    }
}