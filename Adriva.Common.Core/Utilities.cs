using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using JsonFormatting = Newtonsoft.Json.Formatting;

namespace Adriva.Common.Core
{
    public static class Utilities
    {
        private static readonly JsonSerializerSettings defaultSerializerSettings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        private static readonly XmlWriterSettings defaultXmlWriterSettings = new XmlWriterSettings() { Async = true, Indent = false, Encoding = Encoding.UTF8 };
        private static readonly List<string> publicSuffixes = new List<string>();

        private static readonly IDictionary<char, char> CharacterMappings = new Dictionary<char, char>{
                {'Á','A'},                {'á','a'},
                {'Č','C'},                {'č','c'},
                {'ď','d'},                {'é','e'},
                {'ě','e'},                {'É','E'},
                {'Ě','E'},                {'í','i'},
                {'Í','i'},                {'Ň','N'},
                {'ň','n'},                {'Ó','O'},
                {'ó','o'},                {'Ř','R'},
                {'ř','r'},                {'Š','S'},
                {'š','s'},                {'ť','t'},
                {'Ú','U'},                {'ú','u'},
                {'Ů','U'},                {'ů','u'},
                {'Ý','Y'},                {'ý','y'},
                {'Ž','Z'},                {'ž','z'},
                {'ç','c'},                {'ğ','g'},
                {'ı','i'},                {'İ','i'},
                {'ö','o'},                {'ş','s'},
                {'ü','u'},                {'Ç','C'},
                {'Ğ','G'},                {'I','I'},
                {'Ö','O'},                {'Ş','S'},
                {'Ü','U'},                {'â','a'}
            };

        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly DateTimeOffset AzureMinDateTimeOffset = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static readonly string Base63Alphabet = "_0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static readonly string Base36Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

        public static async Task InitializeAsync(Stream suffixListStream)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            List<string> suffixes = new List<string>();

            using (var reader = new StreamReader(suffixListStream, Encoding.Default, false, 512, true))
            {
                string line = null;
                do
                {
                    line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                    else suffixes.Add("." + line.Trim().ToLowerInvariant());
                } while (null != line);
            }

            suffixes.Add(".sys.local");

            Utilities.publicSuffixes.AddRange(suffixes.OrderByDescending(x => x.Length));
        }

        public static string GetMainDomainName(string url)
        {
            string hostName = null;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                hostName = uri.Host;
            }
            else if (Uri.TryCreate(string.Concat("http://", url), UriKind.Absolute, out uri))
            {
                hostName = uri.Host;
            }
            else
            {
                if (null == url) return null;

                hostName = url;
            }

            if (!string.IsNullOrWhiteSpace(hostName))
            {
                var matchingSuffix = Utilities.publicSuffixes.First(suf => hostName.EndsWith(suf, StringComparison.OrdinalIgnoreCase));

                hostName = hostName.Substring(0, hostName.Length - matchingSuffix.Length);
                var hostParts = hostName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                return string.Concat(hostParts[hostParts.Length - 1], matchingSuffix).ToLowerInvariant();
            }
            return null;
        }

        #region String Utilities

        public static string ConvertToAscii(string word)
        {
            string output = string.Empty;
            if (word.Any(c => Utilities.CharacterMappings.ContainsKey(c)))
            {
                for (int loop = 0; loop < word.Length; loop++)
                {
                    if (Utilities.CharacterMappings.ContainsKey(word[loop]))
                    {
                        output += Utilities.CharacterMappings[word[loop]];
                    }
                    else
                    {
                        output += word[loop];
                    }
                }
            }
            else
            {
                return word;
            }
            return output;
        }

        public static string TruncateString(string input, int maxLength, bool isFullSentence = false)
        {
            maxLength = Math.Max(0, maxLength);

            if (string.IsNullOrWhiteSpace(input)) return null;

            input = input.Trim();

            if (maxLength >= input.Length) return input;

            string truncatedString = input.Substring(0, maxLength);
            if (!isFullSentence) return truncatedString;
            else
            {
                if (!string.IsNullOrWhiteSpace(truncatedString))
                {
                    int lastPeriodIndex = truncatedString.LastIndexOf('.');
                    if (0 < lastPeriodIndex)
                    {
                        return truncatedString.Substring(0, 1 + lastPeriodIndex);
                    }
                }

                return truncatedString;
            }
        }

        public static string TruncateAzureString(string input, bool isFullSentence = false)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            return Utilities.TruncateString(input, 30000, isFullSentence);
        }

        public static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            StringBuilder buffer = new StringBuilder(input);

            // some sources (e.g youtube) are sending weird chars such as \\n instead of \n
            buffer.Replace(@"\\n", @"\n");
            buffer.Replace(@"\n", @" ");

            return Utilities.CompressWhitespaces(buffer.ToString());
        }

        public static string CompressWhitespaces(string input)
        {
            if (null == input) return null;
            else if (0 == input.Length) return string.Empty;

            input = input.Trim();
            input = input.Replace(Environment.NewLine, " ");

            StringBuilder buffer = new StringBuilder();
            int loop = 0;

            while (loop < input.Length)
            {
                while (loop < input.Length && !char.IsWhiteSpace(input[loop]))
                {
                    buffer.Append(input[loop]);
                    loop++;
                }

                if (loop < input.Length - 1) buffer.Append(" ");

                while (loop < input.Length && char.IsWhiteSpace(input[loop]))
                {
                    loop++;
                }
            }

            return buffer.ToString();
        }

        public static string RemoveWhitespaces(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            StringBuilder buffer = new StringBuilder();

            foreach (char character in input)
            {
                if (!char.IsWhiteSpace(character)) buffer.Append(character);
            }

            return buffer.ToString();
        }

        // preserves order (assuming 2 distinct data are comparable long, string or sth.)
        public static string GetBaseString(byte[] data, string alphabet, int blockSize)
        {
            if (null == data) return string.Empty;

            StringBuilder buffer = new StringBuilder();

            Array.Resize(ref data, 1 + data.Length);
            BigInteger bigint = new BigInteger(data);

            do
            {
                buffer.Insert(0, alphabet[(int)(bigint % alphabet.Length)]);
                bigint = bigint / (ulong)alphabet.Length;
            } while (0 < bigint);

            while (blockSize > buffer.Length)
            {
                buffer.Insert(0, alphabet[0]);
            }

            return buffer.ToString();
        }
        // preserves order

        public static string GetBaseString(ulong number, string alphabet, int blockSize)
        {
            StringBuilder buffer = new StringBuilder();

            do
            {
                buffer.Insert(0, alphabet[(int)(number % (ulong)alphabet.Length)]);
                number = number / (ulong)alphabet.Length;
            } while (0 < number);

            while (blockSize > buffer.Length)
            {
                buffer.Insert(0, alphabet[0]);
            }

            return buffer.ToString();
        }

        public static string RestoreNegatedString(string input)
        {
            int count = input.Length / 2;
            int loop = 0; int index = 0;
            List<byte> buffer = new List<byte>();

            while (index < count)
            {
                var byteValue = (16 * "0123456789ABCDEF".IndexOf(input[loop])) + "0123456789ABCDEF".IndexOf(input[1 + loop]);
                var normalizedValue = (byte)(0xFF - byteValue);
                if (0 < normalizedValue) buffer.Add(normalizedValue);
                loop += 2; index++;
            }

            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        public static string NegateString(string input, int maxLength)
        {
            if (0 > maxLength) return string.Empty;
            if (maxLength < input.Length) maxLength = input.Length;

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] diffBytes = new byte[maxLength];

            int loop = 0; int delta = 0;

            while (loop < maxLength - input.Length)
            {
                diffBytes[loop] = 0xFF;
                loop++;
            }

            delta = loop;

            while (loop < maxLength)
            {
                diffBytes[loop] = (byte)(0xFF - inputBytes[loop - delta]);
                loop++;
            }

            return BitConverter.ToString(diffBytes).Replace("-", null);
        }

        public static byte[] UrlTokenDecode(string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            int len = input.Length;
            if (len < 1)
                return new byte[0];

            ///////////////////////////////////////////////////////////////////
            // Step 1: Calculate the number of padding chars to append to this string.
            //         The number of padding chars to append is stored in the last char of the string.
            int numPadChars = (int)input[len - 1] - (int)'0';
            if (numPadChars < 0 || numPadChars > 10)
                return null;


            ///////////////////////////////////////////////////////////////////
            // Step 2: Create array to store the chars (not including the last char)
            //          and the padding chars
            char[] base64Chars = new char[len - 1 + numPadChars];


            ////////////////////////////////////////////////////////
            // Step 3: Copy in the chars. Transform the "-" to "+", and "*" to "/"
            for (int iter = 0; iter < len - 1; iter++)
            {
                char c = input[iter];

                switch (c)
                {
                    case '-':
                        base64Chars[iter] = '+';
                        break;

                    case '_':
                        base64Chars[iter] = '/';
                        break;

                    default:
                        base64Chars[iter] = c;
                        break;
                }
            }

            ////////////////////////////////////////////////////////
            // Step 4: Add padding chars
            for (int iter = len - 1; iter < base64Chars.Length; iter++)
            {
                base64Chars[iter] = '=';
            }

            // Do the actual conversion
            return Convert.FromBase64CharArray(base64Chars, 0, base64Chars.Length);
        }

        public static string UrlTokenEncode(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            if (input.Length < 1)
                return String.Empty;

            string base64Str = null;
            int endPos = 0;
            char[] base64Chars = null;

            ////////////////////////////////////////////////////////
            // Step 1: Do a Base64 encoding
            base64Str = Convert.ToBase64String(input);
            if (base64Str == null)
                return null;

            ////////////////////////////////////////////////////////
            // Step 2: Find how many padding chars are present in the end
            for (endPos = base64Str.Length; endPos > 0; endPos--)
            {
                if (base64Str[endPos - 1] != '=') // Found a non-padding char!
                {
                    break; // Stop here
                }
            }

            ////////////////////////////////////////////////////////
            // Step 3: Create char array to store all non-padding chars,
            //      plus a char to indicate how many padding chars are needed
            base64Chars = new char[endPos + 1];
            base64Chars[endPos] = (char)((int)'0' + base64Str.Length - endPos); // Store a char at the end, to indicate how many padding chars are needed

            ////////////////////////////////////////////////////////
            // Step 3: Copy in the other chars. Transform the "+" to "-", and "/" to "_"
            for (int iter = 0; iter < endPos; iter++)
            {
                char c = base64Str[iter];

                switch (c)
                {
                    case '+':
                        base64Chars[iter] = '-';
                        break;

                    case '/':
                        base64Chars[iter] = '_';
                        break;

                    case '=':
                        base64Chars[iter] = c;
                        break;

                    default:
                        base64Chars[iter] = c;
                        break;
                }
            }
            return new string(base64Chars);
        }

        public static string Slugify(string input, int wordCount)
        {
            wordCount = Math.Max(1, wordCount);
            var dataParts = Utilities.Slugify(input).Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries).Take(wordCount);
            return string.Join("-", dataParts);
        }

        public static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            input = Utilities.ConvertToAscii(input);

            StringBuilder buffer = new StringBuilder(input.Length);
            bool lastWhitespace = false;
            char lastChar = '\0';

            foreach (char character in input)
            {
                if (' ' == character && lastWhitespace)
                {
                    if ('-' != lastChar)
                    {
                        buffer.Append("-");
                        lastChar = '-';
                    }
                    lastWhitespace = true;
                }
                else
                {
                    if (char.IsLetterOrDigit(character))
                    {
                        buffer.Append(character);
                        lastChar = character;
                    }
                    else if ('-' != lastChar)
                    {
                        buffer.Append("-");
                        lastChar = '-';
                    }
                    lastWhitespace = false;
                }
            }
            if (0 < buffer.Length && '-' == buffer[buffer.Length - 1]) buffer.Remove(buffer.Length - 1, 1);
            return buffer.ToString().ToLowerInvariant();
        }

        #endregion

        // isreversed : can be used to keep rowkey in order (azuretable)
        //				since row keys are by design, ordered asc
        public static string GetAzureString(DateTimeOffset dateTimeOffset, bool isReversed)
        {
            if (!isReversed)
            {
                return Convert.ToString(dateTimeOffset.UtcTicks);
            }

            return Convert.ToString(DateTimeOffset.MaxValue.Subtract(dateTimeOffset).Ticks);
        }

        public static DateTimeOffset ParseAzureDateTime(string ticksString, bool isReversed)
        {
            if (!long.TryParse(ticksString, out long longTicks)) return DateTimeOffset.MinValue;
            if (!isReversed) return new DateTimeOffset(longTicks, TimeSpan.Zero);
            var deltaTime = TimeSpan.FromTicks(longTicks);
            return DateTimeOffset.MaxValue.Subtract(deltaTime);
        }

        public static DateTimeOffset GetAzureDateTime(DateTimeOffset dateTimeOffset)
        {
            if (dateTimeOffset < Utilities.AzureMinDateTimeOffset) dateTimeOffset = Utilities.AzureMinDateTimeOffset;
            return dateTimeOffset;
        }

        #region Currency Utilities

        public static decimal ExtractMoney(string input)
        {
            input = Utilities.RemoveWhitespaces(input);
            var match = Regex.Match(input, @"((\d{1,3}((\,\d{3}|\.\d{3})*))+(\,|\.)*\d{0,2})");
            if (!match.Success) return -1;
            return Utilities.ParseCurrency(match.Value);
        }

        public static decimal ParseCurrency(string input)
        {

            int count = input.Length;

            for (int loop = count - 1; loop >= 0; loop--)
            {
                if (input[loop] == '.' && 4 > count - loop)
                {
                    input = input.Replace(",", string.Empty).Replace(".", ",");
                }
            }

            if (!decimal.TryParse(input, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-tr"), out decimal output))
            {
                if (!decimal.TryParse(input, NumberStyles.Any, CultureInfo.GetCultureInfo("en-us"), out output))
                {
                }
            }

            return output;
        }

        public static string ParseCurrencyCode(string text)
        {
            var currenyCodeMap = new Dictionary<string, string> { { "try", "TRY" }, { "tl", "TRY" }, { "usd", "USD" }, { "eur", "EUR" }, { "$", "USD" }, { "€", "EUR" }, { "₺", "TRY" }, { "cny", "CNY" }, { "rub", "RUB" }, { "azn", "AZN" }, { "gbp", "GBP" } };

            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = Regex.Match(text, "TL|tl|USD|usd|TRY|try|EUR|eur|[$]|€|₺");

            if (!match.Success || !currenyCodeMap.ContainsKey(match.Value.ToLowerInvariant())) return string.Empty;

            return currenyCodeMap[match.Value.ToLowerInvariant()];
        }

        public static bool TryParseDecimal(string input, out decimal value)
        {
            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        public static string ConvertToAzureString(decimal input)
        {
            return Convert.ToString(input, CultureInfo.InvariantCulture);
        }
        #endregion

        public static string GetRandomId()
        {
            using (var generator = RandomNumberGenerator.Create())
            {
                byte[] buffer = new byte[8];
                generator.GetBytes(buffer, 0, 8);
                return Utilities.GetBaseString(buffer, Utilities.Base36Alphabet, 0);
            }
        }

        public static string GetRandomId(int sizeInBytes)
        {
            sizeInBytes = Math.Max(sizeInBytes, 1);
            using (var generator = RandomNumberGenerator.Create())
            {
                byte[] buffer = new byte[sizeInBytes];
                generator.GetBytes(buffer, 0, sizeInBytes);
                return Utilities.GetBaseString(buffer, Utilities.Base63Alphabet, 0);
            }
        }

        public static byte[] GetRandomBytes(int size)
        {
            byte[] buffer = new byte[size];

            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(buffer);
            }

            return buffer;
        }

        public static T[] GetFlags<T>(Enum value, Enum ignoredValue)
        {
            List<T> items = new List<T>();

            var enumValues = Enum.GetValues(typeof(T));

            foreach (Enum enumValue in enumValues)
            {
                if (value.HasFlag(enumValue) && (null == ignoredValue || (null != ignoredValue && 0 != enumValue.CompareTo(ignoredValue))))
                {
                    items.Add((T)(object)enumValue);
                }
            }

            return items.ToArray();
        }

        public static T CastObject<T>(object data)
        {
            if (data is JObject jData)
            {
                return jData.ToObject<T>();
            }
            else if (data is T concreteData)
            {
                return concreteData;
            }
            else if (null == data)
            {
                return default(T);
            }
            else
            {
                Type typeofTData = typeof(T);

                if (typeofTData.IsEnum)
                {
                    if (Enum.IsDefined(typeofTData, data))
                    {
                        return (T)Enum.ToObject(typeofTData, data);
                    }
                }
                else
                {
                    return (T)Convert.ChangeType(data, typeofTData);
                }
            }

            throw new InvalidCastException();
        }

        public static byte[] FixArraySize(byte[] buffer, int desiredSize)
        {
            if (desiredSize > buffer.Length)
            {
                byte[] temp = new byte[desiredSize];
                Array.Copy(buffer, temp, buffer.Length);
                return temp;
            }
            else if (desiredSize < buffer.Length)
            {
                return buffer.Take(desiredSize).ToArray();
            }
            return buffer;
        }

        #region User Utilities

        public static string GenerateUserId()
        {
            return Utilities.GenerateUserId(out Guid ignore);
        }

        public static string GenerateUserId(out Guid id)
        {
            Guid uiid = Guid.NewGuid();
            id = uiid;
            byte[] userIdBytes = uiid.ToByteArray();
            var userIdLongLeft = BitConverter.ToUInt64(userIdBytes, 0);
            var userIdLongRight = BitConverter.ToUInt64(userIdBytes, 8);


            return string.Concat(Utilities.GetBaseString(userIdLongLeft, Utilities.Base63Alphabet, 0), ".", Utilities.GetBaseString(userIdLongRight, Utilities.Base63Alphabet, 0));
        }

        public static Guid ResolveUserId(string id)
        {

            byte[] GetUint64Bytes(string data)
            {
                int digitIndex = 0;
                BigInteger output = 0;
                for (int loop = data.Length - 1; loop >= 0; loop--)
                {
                    char digit = data[loop];
                    ulong digitval = (ulong)Utilities.Base63Alphabet.IndexOf(digit);
                    var globalValue = (BigInteger.Pow(Utilities.Base63Alphabet.Length, digitIndex)) * digitval;
                    output += globalValue;
                    digitIndex++;
                }

                return BitConverter.GetBytes((ulong)output);
            }

            var parts = id.Split('.');
            var left = parts[0];
            var right = parts[1];

            var bytesHigh = GetUint64Bytes(left);
            var bytesLow = GetUint64Bytes(right);

            byte[] guidBytes = new byte[16];
            Array.Copy(bytesHigh, 0, guidBytes, 0, bytesHigh.Length);
            Array.Copy(bytesLow, 0, guidBytes, bytesHigh.Length, bytesLow.Length);
            return new Guid(guidBytes);
        }

        public static string GetUserPartitionKey(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            if (3 > userId.Length) return null;

            int bucket = (((7 * (int)userId[0]) + (7 * (int)userId[1]) + (17 * (int)userId[2])) % 1513);

            while (0 > bucket) bucket += 1513;

            return bucket.ToString("x4");
        }

        #endregion

        #region Serialization Utilities

        public static string SafeSerialize(object item)
        {
            return JsonConvert.SerializeObject(item, JsonFormatting.None, Utilities.defaultSerializerSettings);
        }

        public static string SafeSerialize(object item, JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(item, JsonFormatting.None, settings);
        }

        public static T SafeDeserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Utilities.defaultSerializerSettings);
        }

        public static T SafeDeserialize<T>(string json, JsonSerializerSettings settings)
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static T SafeDeserialize<T>(byte[] buffer)
        {
            var json = Encoding.UTF8.GetString(buffer);
            return Utilities.SafeDeserialize<T>(json);
        }

        #endregion

        #region Crypto Utilities

        public static string CalculateKeySafeHash(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            using (var hashAlgorithm = SHA1.Create())
            {
                hashAlgorithm.Initialize();
                var hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        public static string CalculateHash(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            using (var hashAlgorithm = SHA256.Create())
            {
                hashAlgorithm.Initialize();
                var hashBytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Utilities.GetBaseString(hashBytes, Utilities.Base36Alphabet, 0);
            }
        }

        public static string CalculateBinaryHash(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            using (var hashAlgorithm = SHA256.Create())
            {
                hashAlgorithm.Initialize();
                var hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        public static string Encrpyt(string data)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            byte[] output = Utilities.Encrpyt(buffer);
            return Convert.ToBase64String(output);
        }

        public static byte[] Encrpyt(byte[] data)
        {

            byte[] key = Encoding.UTF8.GetBytes(RuntimeSettings.Default.EncryptionKey);
            byte[] iv = Encoding.UTF8.GetBytes(RuntimeSettings.Default.EncryptionKey);

            key = Utilities.FixArraySize(key, 32);
            iv = Utilities.FixArraySize(iv, 16);

            using (var algorithm = Aes.Create())
            {
                algorithm.Key = key;
                algorithm.IV = iv;

                using (var encryptor = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(data, 0, data.Length);
                        }

                        return memoryStream.ToArray();
                    }
                }
            }
        }

        public static string Decrpyt(string protectedData)
        {
            byte[] secureBuffer = Convert.FromBase64String(protectedData);
            byte[] buffer = Utilities.Decrpyt(secureBuffer);
            return Encoding.UTF8.GetString(buffer);
        }

        public static byte[] Decrpyt(byte[] data)
        {

            byte[] key = Encoding.UTF8.GetBytes(RuntimeSettings.Default.EncryptionKey);
            byte[] iv = Encoding.UTF8.GetBytes(RuntimeSettings.Default.EncryptionKey);

            key = Utilities.FixArraySize(key, 32);
            iv = Utilities.FixArraySize(iv, 16);

            using (var algorithm = Aes.Create())
            {
                algorithm.Key = key;
                algorithm.IV = iv;

                using (var decryptor = algorithm.CreateDecryptor(algorithm.Key, algorithm.IV))
                {
                    using (var encryptedStream = new MemoryStream(data))
                    {
                        using (var decryptedStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read))
                            {
                                cryptoStream.CopyTo(decryptedStream);
                            }

                            return decryptedStream.ToArray();
                        }
                    }
                }
            }
        }
        #endregion

        #region Http Client Utilities

        private static Dictionary<string, string> GetDefaultHttpHeaders()
        {
            return new Dictionary<string, string>() {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36"}
            };
        }

        public static bool TryParseCookie(string cookieString, out Dictionary<string, string> cookieProperties)
        {
            cookieProperties = null;

            if (string.IsNullOrWhiteSpace(cookieString)) return false;

            var cookieParts = cookieString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            cookieProperties = new Dictionary<string, string>();
            bool isFirst = true;

            foreach (var cookiePart in cookieParts)
            {
                var cookiePairs = cookiePart.Split(new[] { '=' }, StringSplitOptions.None);
                if (2 == cookiePairs.Length)
                {
                    if (isFirst)
                    {
                        cookieProperties.Add("name", cookiePairs[0]);
                        cookieProperties.Add("value", cookiePairs[1]);
                        isFirst = false;
                    }
                    else
                    {
                        cookieProperties.Add(cookiePairs[0].Trim().ToLowerInvariant(), cookiePairs[1].Trim());
                    }
                }
            }

            if (0 == cookieProperties.Count)
            {
                cookieProperties = null;
                return false;
            }


            return true;
        }

        public static bool TryGetCookieValue(HttpResponseHeaders headers, string cookieName, out string value)
        {
            value = null;

            if (null == headers || string.IsNullOrWhiteSpace(cookieName)) return false;

            if (!headers.TryGetValues("set-cookie", out IEnumerable<string> cookieStrings)) return false;

            foreach (var cookieString in cookieStrings)
            {
                if (Utilities.TryParseCookie(cookieString, out Dictionary<string, string> cookieProperties))
                {
                    if (cookieProperties.ContainsKey("name") && 0 == string.Compare(cookieProperties["name"], cookieName, StringComparison.Ordinal))
                    {
                        value = cookieProperties.ContainsKey("value") ? cookieProperties["value"] : null;
                        return true;
                    }
                }
            }

            return false;
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpHeadAsync(string url)
        {
            return await Utilities.HttpHeadAsync(url, string.Empty);
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpHeadAsync(string baseUrl, string requestUri)
        {
            return await Utilities.HttpHeadAsync(baseUrl, requestUri, Utilities.GetDefaultHttpHeaders());
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpHeadAsync(string baseUrl, string requestUri, Dictionary<string, string> headers)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseUrl);

                if (null != headers)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                using (var request = new HttpRequestMessage(HttpMethod.Head, requestUri))
                {
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    return response;
                }
            }
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpGetAsync(string url)
        {
            return await Utilities.HttpGetAsync(url, string.Empty);
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpGetAsync(string url, bool shouldCheckStatusCode)
        {
            return await Utilities.HttpGetAsync(url, string.Empty, Utilities.GetDefaultHttpHeaders(), shouldCheckStatusCode);
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpGetAsync(string url, bool shouldCheckStatusCode, HttpCompletionOption httpCompletionOption)
        {
            return await Utilities.HttpGetAsync(url, string.Empty, Utilities.GetDefaultHttpHeaders(), shouldCheckStatusCode, httpCompletionOption);
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpGetAsync(string baseUrl, string requestUri)
        {
            return await Utilities.HttpGetAsync(baseUrl, requestUri, Utilities.GetDefaultHttpHeaders(), true);
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpGetAsync(string baseUrl, string requestUri, Dictionary<string, string> headers, bool shouldCheckStatusCode)
        {
            return await Utilities.HttpGetAsync(baseUrl, requestUri, headers, shouldCheckStatusCode, HttpCompletionOption.ResponseContentRead);
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpGetAsync(string baseUrl, string requestUri, Dictionary<string, string> headers, bool shouldCheckStatusCode, HttpCompletionOption httpCompletionOption)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseUrl);

                if (null != headers)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                var response = await client.GetAsync(requestUri, httpCompletionOption);
                if (shouldCheckStatusCode) response.EnsureSuccessStatusCode();
                return response;
            }
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpGetAsync(string baseUrl, string requestUri, Dictionary<string, string> headers, string cookieString)
        {
            return await Utilities.HttpGetAsync(baseUrl, requestUri, headers, cookieString, HttpCompletionOption.ResponseContentRead);
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpGetAsync(string baseUrl, string requestUri, Dictionary<string, string> headers, string cookieString, HttpCompletionOption httpCompletionOption)
        {
            var jar = new CookieContainer();

            using (var handler = new HttpClientHandler())
            {
                handler.CookieContainer = jar;
                handler.UseCookies = true;

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(baseUrl);

                    using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                    {

                        if (null != headers)
                        {
                            foreach (var header in headers)
                            {
                                request.Headers.Add(header.Key, header.Value);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(cookieString))
                        {
                            var cookies = cookieString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var cookie in cookies)
                            {
                                var cookieData = cookie.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                                if (2 == cookieData.Length)
                                {
                                    var httpCookie = new Cookie(cookieData[0], cookieData[1]);
                                    jar.Add(client.BaseAddress, httpCookie);
                                }
                            }
                        }

                        var response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        return response;
                    }
                }
            }
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpPostAsync<T>(string url, T data) where T : HttpContent
        {
            return await Utilities.HttpPostAsync<T>(url, data, null);
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpPostAsync(string url, HttpContent content, Dictionary<string, string> headers)
        {
            using (var client = new HttpClient())
            {
                if (null != headers)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                return response;
            }
        }

        [Obsolete("Http methods are moved to an extension library. Please use classes from Adriva.Extensions.dll / Adriva.Extensions.Http namespace.")]
        public static async Task<HttpResponseMessage> HttpPostAsync<T>(string url, T data, Dictionary<string, string> headers) where T : HttpContent
        {
            using (var client = new HttpClient())
            {
                if (null != headers)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                var response = await client.PostAsync(url, data);
                response.EnsureSuccessStatusCode();
                return response;
            }
        }

        public static bool IsHttpSuccess(int statusCode)
        {
            return 200 <= statusCode && 299 >= statusCode;
        }

        public static bool IsValidHttpUri(Uri uri)
        {
            if (null == uri) return false;
            if (!uri.IsAbsoluteUri) return false;
            return 0 == string.Compare(Uri.UriSchemeHttp, uri.Scheme, StringComparison.Ordinal)
                || 0 == string.Compare(Uri.UriSchemeHttps, uri.Scheme, StringComparison.Ordinal);
        }

        #endregion

        #region Async Utilities

        public static async Task<bool> WaitAsync(object lockObject)
        {

            if (null == lockObject) return false;

            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.None);
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Monitor.Enter(state);
                completionSource.TrySetResult(true);
            });
            return await completionSource.Task;
        }

        public static async Task<bool> ReleaseAsync(object lockObject)
        {
            if (null == lockObject) return false;

            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.None);
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Monitor.Exit(state);
                completionSource.TrySetResult(true);
            });
            return await completionSource.Task;
        }

        #endregion

        #region Reflection Utilities

        public static bool TryParseQualifiedMethod(string qualifiedMethod, out MethodBase methodBase)
        {
            methodBase = null;
            if (string.IsNullOrWhiteSpace(qualifiedMethod)) return false;

            int typeSeperatorIndex = qualifiedMethod.LastIndexOf(",");

            if (0 >= typeSeperatorIndex || typeSeperatorIndex == qualifiedMethod.Length - 1) return false;

            string typeName = qualifiedMethod.Substring(0, typeSeperatorIndex);
            string methodName = qualifiedMethod.Substring(typeSeperatorIndex + 1);

            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(methodName)) return false;

            typeName = typeName.Trim();
            methodName = methodName.Trim();

            var type = Type.GetType(typeName, false, true);

            if (null == type) return false;

            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

            if (null != methodInfo)
            {
                methodBase = methodInfo;
                return true;
            }

            return false;
        }

        #endregion

        #region File Utilities

        public static IEnumerable<IFileInfo> EnumerateFiles(IFileProvider fileProvider, string rootDirectory)
        {
            fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));

            var directoryContents = fileProvider.GetDirectoryContents(rootDirectory);

            var fileEnumerator = directoryContents.GetEnumerator();

            while (fileEnumerator.MoveNext())
            {
                if (fileEnumerator.Current.IsDirectory)
                {
                    var childEnumerable = Utilities.EnumerateFiles(fileProvider, Path.Combine(rootDirectory, fileEnumerator.Current.Name));
                    var childEnumerator = childEnumerable.GetEnumerator();

                    while (childEnumerator.MoveNext())
                    {
                        yield return childEnumerator.Current;
                    }
                }
                else
                {
                    yield return fileEnumerator.Current;
                }
            }
        }

        public static byte[] CalculateFileHash(IFileInfo fileInfo)
        {
            if (null == fileInfo) return Array.Empty<byte>();

            using (var stream = fileInfo.CreateReadStream())
            using (var algorithm = MD5.Create())
            {
                return algorithm.ComputeHash(stream);
            }

        }

        public static byte[] CalculateMultiFileHash(IEnumerable<IFileInfo> fileInfoList)
        {
            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                foreach (var fileInfo in fileInfoList)
                {
                    using (var stream = fileInfo.CreateReadStream())
                    using (var algorithm = MD5.Create())
                    {
                        byte[] hash = algorithm.ComputeHash(stream);
                        memoryStream.Write(hash, 0, hash.Length);
                    }
                }
                memoryStream.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);

                using (var algorithm = MD5.Create())
                {
                    return algorithm.ComputeHash(memoryStream);
                }
            }
        }

        #endregion
    }
}
