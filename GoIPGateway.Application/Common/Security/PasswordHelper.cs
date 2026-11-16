using System.Security.Cryptography;
using System.Text;

namespace GoIPGateway.Application.Common.Security;

public static class PasswordHelper
{
    private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowercaseChars = "abcdefghijkmnpqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SpecialChars = "!@#$%&*";

    public static string GenerateSecureTempPassword(int length = 10)
    {
        if (length < 8) length = 8;

        var password = new StringBuilder();
        // Ensure at least one of each required type
        password.Append(UppercaseChars[RandomNumberGenerator.GetInt32(UppercaseChars.Length)]);
        password.Append(LowercaseChars[RandomNumberGenerator.GetInt32(LowercaseChars.Length)]);
        password.Append(DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)]);
        password.Append(SpecialChars[RandomNumberGenerator.GetInt32(SpecialChars.Length)]);

        const string allChars = UppercaseChars + LowercaseChars + DigitChars + SpecialChars;
        for (int i = 4; i < length; i++)
        {
            password.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
        }

        // Shuffle
        var array = password.ToString().ToCharArray();
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }

        return new string(array);
    }
}

