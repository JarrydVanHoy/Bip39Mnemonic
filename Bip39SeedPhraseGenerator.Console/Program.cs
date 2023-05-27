﻿// See https://bitcoinmagazine.com/culture/diy-bitcoin-private-key-project for more info on generating a mnemonic seed phrase (BIP39)
// NOT SUGGESTED: you can validate the result a 3rd party like: https://3rditeration.github.io/mnemonic-recovery/src/index.html
// ALWAYS protect your seed phrase with a pass phrase (BIP39)

using SeedGenerator;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

var coinRegex = new Regex("^[HhTt]+$");
var diceRegex = new Regex("^[1-6]+$");
bool regenerate;
const int wordCount = 24;
var data = new byte[11 * wordCount / 8];

do
{
    Console.WriteLine("Hello, let's generate a 24 word seed phrase!");
    var autogenerate = DoesUserWantAutogenerate();

    if (autogenerate)
    {
        GenerateRawData();
    }
    else
    {
        GetUserGeneratedData();
    }

    Console.WriteLine($"Mnemonic: {string.Join(" ", DeriveSeedPhrase())}");

    regenerate = DoesUserWantToGenerateAnotherSeedPhrase();
} while (regenerate);

bool DoesUserWantAutogenerate()
{
    string? input;
    bool properInput;
    do
    {
        Console.WriteLine("Do you want this autogenerated or would you like to create it yourself? (A)uto/(S)elf");

        input = Console.ReadLine()?.Trim();

        properInput = input is "A" or "a" or "S" or "s";

        if (!properInput)
        {
            Console.WriteLine("Invalid input, lets try again...");
        }
    } while (!properInput);

    return input is "A" or "a";
}

void GenerateRawData()
{
    RandomNumberGenerator.Fill(data);
    AddChecksum();
}

void AddChecksum()
{
    var binary = data.Take(data.Length - 1).ToBinary();
    Console.WriteLine($"Binary: {binary} (pre-checksum)");

    var hashData = SHA256.HashData(data.AsSpan(0, data.Length - 1));
    var checksum = BitConverter.ToString(hashData).Replace("-", string.Empty).ToLowerInvariant();
    var checksumMsb = checksum[..2];
    data[^1] = Convert.ToByte(checksumMsb, 16);
    var checksumBinary = data.Skip(data.Length - 1).Take(1).ToBinary();
    Console.WriteLine($"Checksum: {checksum} --> {checksumMsb[0]}{checksumMsb[1]} (MSB) --> {checksumBinary}");
}

void GetUserGeneratedData()
{
    string? input;
    bool properInput;
    do
    {
        Console.WriteLine("Do you want use a coin or 6 sided dice? (C)oin/(D)ice");

        input = Console.ReadLine()?.Trim();

        properInput = input is "C" or "c" or "D" or "d";

        if (!properInput)
        {
            Console.WriteLine("Invalid input, lets try again...");
        }
    } while (!properInput);

    if (input is "C" or "c")
    {
        GetCoinFlipData();
    }
    else
    {
        GetDiceRollData();
    }

    AddChecksum();
}

void GetCoinFlipData()
{
    var flips = (data.Length - 1) * 8;
    string input;
    bool properInput;
    do
    {
        input = string.Empty;

        Console.WriteLine($"Flip a coin {flips} times and record them without any spacing? (H)eads/(T)ails");

        while (true)
        {
            input += Console.ReadLine()?.Trim() ?? string.Empty;

            properInput = coinRegex.IsMatch(input);

            if (!properInput)
            {
                Console.WriteLine("Invalid input, lets try again...");
                break;
            }

            if (input.Length > flips)
            {
                Console.WriteLine($"You entered too many flips. We'll just the the 1st {flips} flips.");
                input = input[..flips];
                break;
            }

            if (input.Length == flips)
            {
                break;
            }

            Console.WriteLine($"You were short by {flips - input.Length}. Please add more flips.");
        }
    } while (!properInput);

    var sb = new StringBuilder(input);
    for (var i = 0; i < sb.Length; i++)
    {
        sb[i] = sb[i] is 'H' or 'h' ? '1' : '0';
    }

    input = sb.ToString();
    for (var i = 0; i < data.Length - 1; i++)
    {
        data[i] = Convert.ToByte(input.Substring(i * 8, 8), 2);
    }
}

void GetDiceRollData()
{
    const int rollsPerWord = 4;
    const int rollsForFirst3BitsOfLastWord = 3;
    const int rolls = (wordCount - 1) * rollsPerWord + rollsForFirst3BitsOfLastWord;
    string input;
    bool properInput;
    do
    {
        input = string.Empty;

        Console.WriteLine($"Roll a dice {rolls} times and record them without any spacing using 1-6? [1-6]");

        while (true)
        {
            input += Console.ReadLine()?.Trim() ?? string.Empty;

            properInput = diceRegex.IsMatch(input);

            if (!properInput)
            {
                Console.WriteLine("Invalid input, lets try again...");
                break;
            }

            if (input.Length > rolls)
            {
                Console.WriteLine($"You entered too many rolls. We'll just the the 1st {rolls} rolls.");
                input = input[..rolls];
                break;
            }

            if (input.Length == rolls)
            {
                break;
            }

            Console.WriteLine($"You were short by {rolls - input.Length}. Please add more rolls.");
        }
    } while (!properInput);

    var binary = string.Empty;
    for (var i = 0; i < (rolls - rollsForFirst3BitsOfLastWord) / 4; i++)
    {
        var sub = input.Substring(i * 4, 4);
        var binaryWord = string.Empty;
        for (var j = 0; j < 4; j++)
        {
            binaryWord += sub[j] switch
            {
                '1' => "000",
                '2' => "001",
                '3' => "010",
                '4' => "011",
                '5' => "100",
                _ => "101"
            };
        }

        // reverse bits for extra entropy since we only need 11 bits
        if (binaryWord[11] == '1')
        {
            var reverse = string.Empty;
            for (var j = 0; j < 11; j++)
            {
                reverse += binaryWord[j] is '1' ? '0' : '1';
            }

            binary += reverse;
        }
        else
        {
            binary += binaryWord[..11];
        }
    }

    binary += input[^3] is '1' or '2' or '3' ? '0' : '1';
    binary += input[^2] is '1' or '2' or '3' ? '0' : '1';
    binary += input[^1] is '1' or '2' or '3' ? '0' : '1';

    for (var i = 0; i < data.Length - 1; i++)
    {
        data[i] = Convert.ToByte(binary.Substring(i * 8, 8), 2);
    }
}

IEnumerable<string> DeriveSeedPhrase()
{
    var binary = data.ToBinary();
    Console.WriteLine($"Binary: {binary}");

    var words = new int[wordCount];
    for (var i = 0; i < wordCount; i++)
    {
        words[i] = Convert.ToInt32(binary.Substring(i * 11, 11), 2);
    }

    return words.Select(_ => Helpers.Words[_]);
}

bool DoesUserWantToGenerateAnotherSeedPhrase()
{
    string? input;
    bool properInput;
    do
    {
        Console.WriteLine("Do you want to generate another seed phrase? (Y/N)");

        input = Console.ReadLine();

        properInput = input?.Trim() is "Y" or "y" or "N" or "n";

        if (!properInput)
        {
            Console.WriteLine("Invalid input, lets try again...");
        }
    } while (!properInput);

    return input!.Trim() is "Y" or "y";
}

internal partial class Program
{
    [GeneratedRegex("[HhTt]*")]
    private static partial Regex MyRegex();
}