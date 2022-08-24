using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FFPP.Data.Logging;

namespace FFPP.Common
{
    public static class Utilities
    {
        public static readonly List<string> WordDictionary = new() { "bruce","bruceson","john","johnson","big","small","stinky",
            "outrageous","valuable","pineapples","jack","jackson","peter","file","juices","spruce","cactus","blunt","sharp","affluent",
            "camel","toe","elbow","knee","old","sponzengeiger","wallace","william","jane","doe","moe","didactic","barnacle","sponge",
            "bob","qwerty","nelson","full","extreme","upstart","fbi","nsa","noah","ark","lancelot","potty","mouth","underpants","spiky",
            "prickly","plaintiff","outlaw","bicycle","dopamine","pepper","trees","space","bananas","monkey","potatoes","crispy","pork",
            "chop","bitcoin","boat","bone","floating","masked","behold","judith","ian","marcus","phoenix","wind","gasses","fluffy","pony",
            "ewe","eyes","star","pole","dance","umpire","rebel","jones","noise","monero","cypher","aes256","aes128","sha256","sha512","xor",
            "hmacsha256","hmacsha512","parrot","cat","dog","squirrel","skunk","farts","width","mountains","lumps","bumps","goose","high",
            "tall","ball","cool","hot","tomato","chips","minimum","maximum","maximus","cease","strange","ugly","derelict","driven","mongoose",
            "hold","grab","foolish","melons","pickles","lemons","sour","grapes","goat","milk","got","plums","spicy","rotating","ending","world",
            "covid","vaccine","pass","fail","stop","fall","in","out","on","beginner","lessons","lapras","snorlax","round","square","cheeses",
            "cheesy","golden","brown","lamb","killer","staged","frightening","laughter","desired","follicles","ultra","uber","plants","paris",
            "bonjour","not","participating","precipitation","carrying","hooves","horse","rhubarb","apples","chilli","burn","heat","cold","pasta",
            "jolting","crabs","rabbit","kangaroo","deal","screaming","zinger","alpha","bravo","charlie","delta","echo","foxtrot","golf","hotel",
            "india","juliet","kilo","lima","mike","november","oscar","papa","quebec","romeo","sierra","tango","uniform","victor","whiskey","xray",
            "zulu","aircraft","bouncing","bobcat","bonkers","nanny","popping","weasel","rolling","atomic","pure","fine","diesel","fishing","puppet",
            "unicorn","epsilon","gamma","beta","thor","tennis","rally","cry","happy","suspicious","panda","bear","smile","frown","skirt","jellyfish",
            "law","tax","criminal","escapade","popcorn","dogma","scared","lifeless","limitless","potential","voltage","amperage","amped","zapped",
            "devil","salamander","frog","carrots","onions","dude","aubergine","appendage","cloudy","scaled","measured","response","excited","flustered",
            "peacock","bin","garbage","trash","taco","beans","burger","alien","illegal","fragrant","floral","food","popsicle","ajar","test","sensual",
            "schooled","varnish","lazy","starfish","belly","ring","of","fire","ice","yacht","russian","spider","web","fierce","furious","fast","factual",
            "fred","nerf","fern","leaf","good","bad","noodles","boy","girl","sleep","thin","major","minor","private","public","nuisance","coffee","fetish"
        };

        /// <summary>
        /// Returns a base64 encoded string consisting of 4098 crypto random bytes (4kb), this is cryptosafe random
        /// </summary>
        /// <param name="length">Length of characters you want returned, defaults to 4098 (4kb)</param>
        /// <returns>4kb string of random bytes encoded as base64 string</returns>
        public static async Task<string> RandomByteString(int length = 4098)
        {
             return await Base64Encode(RandomNumberGenerator.GetBytes(length));
        }

        /// <summary>
        /// Takes raw JSON and a designated type and it converts the JSON into a list of objects of the given type
        /// </summary>
        /// <typeparam name="type">Will parse into a list of objects of this type</typeparam>
        /// <param name="rawJson"></param>
        /// <returns>List of objects defined by given type</returns>
        public static List<type> ParseJson<type>(List<JsonElement> rawJson)
        {
            List<type> objectArrayList = new();

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                MaxDepth = 64
            };

            foreach (JsonElement je in rawJson)
            {   
                objectArrayList.Add(JsonSerializer.Deserialize<type>(je, options));
            }

            return objectArrayList;
        }

        /// <summary>
        /// Submit any JSON object/s to write to file
        /// </summary>
        /// <typeparam name="type">type of JSON object/s e.g. Tenant or List<Tenant></typeparam>
        /// <param name="json">JSON object/s to serialize to file</param>
        /// <param name="filePath">File path</param>
        public static async void WriteJsonToFile<type>(object json, string filePath, bool encrypt=false, byte[]? key = null)
        {
            string jsonString = JsonSerializer.Serialize((type)json);

            if(encrypt)
            {
                jsonString = await Utilities.Crypto.AesEncrypt(jsonString, key);
            }

            File.WriteAllText(filePath, jsonString);
        }

        /// <summary>
        /// Return file contents as JSON object of specified type
        /// </summary>
        /// <typeparam name="type">Type of our JSON object to make</typeparam>
        /// <param name="filePath">Path to our file containing JSON</param>
        /// <returns>JSON object of specified type</returns>
        public static async Task<type> ReadJsonFromFile<type>(string filePath, bool decrypt=false, byte[]? key=null)
        {
            string jsonString = File.ReadAllText(filePath);

            if(decrypt)
            {
                jsonString = await Utilities.Crypto.AesDecrypt(jsonString, key);
            }

            return JsonSerializer.Deserialize<type>(jsonString);
        }

        /// <summary>
        /// Converts a supplied CSV file into a List of specified objects
        /// </summary>
        /// <typeparam name="type">type of the object we want returned in the list</typeparam>
        /// <param name="csvFilePath">File path to the CSV file</param>
        /// <param name="skipHeader">First line is a header line (not data) so use true to skip it</param>
        /// <returns>List of objects, each object is a line from the CSV</returns>
        public static List<type> CsvToObjectList<type>(string csvFilePath, bool skipHeader = false)
        {
            List<type> returnData = new();

            foreach (string line in File.ReadAllLines(csvFilePath))
            {
                // Skip first row (header row)
                if (skipHeader)
                {
                    skipHeader = false;
                    continue;
                }

                returnData.Add((type)Activator.CreateInstance(typeof(type), line.Split(',')));
            }

            return returnData;
        }

        /// <summary>
        /// Evaluates JSON boolean and treats null as false
        /// </summary>
        /// <param name="property">JSON boolean to check</param>
        /// <returns>true/false</returns>
        public static bool NullIsFalse(JsonElement property)
        {
            try
            {
                return property.GetBoolean();
            }
            catch
            {

            }

            return false;
        }

        /// <summary>
        /// Deletes cache and pre-fetch files used by FFPP API
        /// </summary>
        /// <returns>Boolean indicating sucess or failure</returns>
        public static async Task<bool> RemoveFfppCache()
        {
            try
            {
                // Delete CacheDir and any subdirs/files then re-create cache dir
                Directory.Delete(ApiEnvironment.CacheDir, true);
                ApiEnvironment.DataAndCacheDirectoriesBuild();

            }
            catch (Exception ex)
            {
                FfppLogsDbThreadSafeCoordinator.ThreadSafeAdd(new FfppLogsDbContext.LogEntry()
                {
                    Message = $"Exception purging FFPP API Cache: {ex.Message}",
                    Username = "FFPP",
                    Severity = "Error",
                    API = "RemoveFfppCache"
                });

                return false;
            }

            return true;
        }

        /// <summary>
        /// Decodes a base64url string into a byte array
        /// </summary>
        /// <param name="arg">string to convert to bytes</param>
        /// <returns>byte[] containing decoded bytes</returns>
        /// <exception cref="Exception">Illegal base64url string</exception>
        public static byte[] Base64UrlDecode(string arg)
        {
            string s = arg;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding
            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: s += "=="; break; // Two pad chars
                case 3: s += "="; break; // One pad char
                default:
                    FfppLogsDbContext.DebugConsoleWrite(string.Format("Illegal base64url string: {0}", arg));
                    throw new Exception(string.Format("Illegal base64url string: {0}", arg));
            }
            return Convert.FromBase64String(s); // Standard base64 decoder
        }

        /// <summary>
        /// Encodes a byte array into a Base64 encoded string
        /// </summary>
        /// <param name="bytes">bytes to encode</param>
        /// <returns>Base64 encoded string</returns>
        public static async Task<string> Base64Encode(byte[] bytes)
        {
            Task<string> task = new(() =>
            {
                return Convert.ToBase64String(bytes);
            });

            task.Start();

            return await task;
        }

        /// <summary>
        /// Decodes a Base64 encoded string into a byte array
        /// </summary>
        /// <param name="text"></param>
        /// <returns>byte array containing bytes of string</returns>
        public static async Task<byte[]> Base64Decode(string text)
        {
            Task<byte[]> task = new(() =>
            {
                return Convert.FromBase64String(text);

            });

            task.Start();

            return await task;
        }

        public static async Task<string> UsernameParse(HttpContext context)
        {
            try
            {
                return context.User.Claims.First(x => x.Type.ToLower().Equals("preferred_username")).Value;
            }
            catch
            {
                return "Illegal Alien";
            }
        }

        public static class Crypto
        {
            /// <summary>
            /// Used to describe an ApiRandom object used to coinstruct cryptorandom things
            /// used within the API
            /// </summary>
            public class ApiRandom
            {
                private readonly string _phrase;
                private readonly string _hashedPhrase;
                private readonly string _salt;
                private readonly long _iterations;
                private readonly bool _ignoreCryptoSafe;

                /// <summary>
                /// Creates an ApiRandom object that can be used to create a mnemonic phrase with accompanying hashed bytes
                /// which are cryptographically safe as entropy (assuming at least 24 words in the phrase
                /// </summary>
                /// <param name="phrase">The phrase we will be </param>
                /// <param name="salt"></param>
                /// <param name="iterations"></param>
                public ApiRandom(string phrase, string salt = "mmmsalty888", long iterations = 231010, bool ignoreCryptoSafe = false)
                {
                    _ignoreCryptoSafe = ignoreCryptoSafe;
                    _iterations = iterations;
                    _phrase = phrase;
                    _salt = salt;
                    HMACSHA512 hasher = new(Encoding.Unicode.GetBytes(_phrase + _salt));
                    byte[] hashedPhraseBytes = hasher.ComputeHash(Encoding.Unicode.GetBytes(_phrase));

                    for (long i = 0; i < _iterations; i++)
                    {
                        hashedPhraseBytes = hasher.ComputeHash(hashedPhraseBytes);
                    }

                    _hashedPhrase = Convert.ToHexString(hashedPhraseBytes);
                }

                private bool CheckCryptoSafe()
                {
                    if ((WordDictionary.Count > 299 && ((_phrase.Split('-').Length > 15) || (_phrase.Split('-').Length < 12 && _phrase.Length > 191)) && _iterations > 100000) || _ignoreCryptoSafe)
                    {
                        return true;
                    }

                    return false;
                }

                public string Phrase { get { if (!CheckCryptoSafe()) { throw new("This ApiRandom is not Cryptosafe and we are not instructed to ignore Cryptosafety"); } return _phrase; } }
                public string HashedPhrase { get { if (!CheckCryptoSafe()) { throw new("This ApiRandom is not Cryptosafe and we are not instructed to ignore Cryptosafety"); } return _hashedPhrase; } }
                public byte[] HashedPhraseBytes { get { if (!CheckCryptoSafe()) { throw new("This ApiRandom is not Cryptosafe and we are not instructed to ignore Cryptosafety"); } return Convert.FromHexString(_hashedPhrase); } }
                public string Salt { get { if (!CheckCryptoSafe()) { throw new("This ApiRandom is not Cryptosafe and we are not instructed to ignore Cryptosafety"); } return _salt; } }
                public Guid HashedPhraseBytesAsGuid { get { if (!CheckCryptoSafe()) { throw new("This ApiRandom is not Cryptosafe and we are not instructed to ignore Cryptosafety"); } return new Guid(MD5.HashData(Convert.FromHexString(_hashedPhrase))); } }
                public long Iterations { get => _iterations; }
            }

            /// <summary>
            /// Generates x number of 2 random word phrases from WordDictionary in format [-{0}-{1}], is cryptorandom
            /// so can be used for entropy but need a lot of 2 word phrases to create sufficiently strong entropy.
            /// </summary>
            /// <param name="numberOfPhrases">Number of 2 word phrases to generate in the single line delimited by '-'</param>
            /// <param name="salt">Optional salt to be used during sha512 hashing operation</param>
            /// <returns>List where [0] contains word phrase, [1] contains hex encoded 100,000 pass sha512</returns>
            public static ApiRandom Random2WordPhrase(int numberOfPhrases = 1, string salt = "mmmsalty888", long iterations = 231010, bool ignoreCryptoSafe = false)
            {
                int i = 0;
                string phrase = string.Empty;

                do
                {   // We can't return nothing
                    if (numberOfPhrases < 1)
                    {
                        numberOfPhrases = 1;
                    }

                    static string RandomWord()
                    {
                        return WordDictionary[RandomNumberGenerator.GetInt32(0, WordDictionary.Count)];
                    }

                    phrase += string.Format("-{0}-{1}", RandomWord(), RandomWord());

                    i++;

                } while (i < numberOfPhrases);

                return new(phrase.Remove(0, 1), salt, iterations, ignoreCryptoSafe);
            }

            /// <summary>
            /// Encrypts the provided plaintext using a random IV that is prefixed on the output encrypted string
            /// </summary>
            /// <param name="plaintext">plaintext to encrypt</param>
            /// <param name="key">encryption key</param>
            /// <returns>string of encrypted text (cipherText)</returns>
            /// <exception cref="ArgumentException">Will throw if AES key not a correct size</exception>
            public static async Task<string> AesEncrypt(string plainText, byte[]? key = null)
            {
                if(key==null)
                {
                    key = await ApiEnvironment.GetDeviceId();
                }

                if(key.Length != 16 && key.Length != 24 && key.Length != 32)
                {
                    throw new ArgumentException("AES key must be 16, 24 or 32 bytes in length");
                }

                var plainTextBuffer = Encoding.UTF8.GetBytes(plainText);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;

                    // It is acceptable to use MD5 here as it outputs 16 bytes, it's fast, and IV is not secret
                    aes.IV = MD5.HashData(await Utilities.Base64Decode(await RandomByteString(512)));

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var resultStream = new MemoryStream())
                    {
                        using (var aesStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Write))
                        using (var plainStream = new MemoryStream(plainTextBuffer))
                        {
                            plainStream.CopyTo(aesStream);
                        }

                        var result = resultStream.ToArray();
                        var combined = new byte[aes.IV.Length + result.Length];
                        Array.ConstrainedCopy(aes.IV, 0, combined, 0, aes.IV.Length);
                        Array.ConstrainedCopy(result, 0, combined, aes.IV.Length, result.Length);

                        return await Base64Encode(combined);
                    }
                }
            }

            /// <summary>
            /// Decrypts the provided cipherText and it's assumed the IV is prefixed on the cipherText
            /// </summary>
            /// <param name="cipherText">cipherText string to decrypt</param>
            /// <param name="key">decryption key</param>
            /// <returns>decrypted text (plainText)</returns>
            /// <exception cref="ArgumentException">Will throw if AES key not a correct size</exception>
            public static async Task<string> AesDecrypt(string cipherText, byte[]? key=null)
            {
                if (key == null)
                {
                    key = await ApiEnvironment.GetDeviceId();
                }

                if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                {
                    throw new ArgumentException("AES key must be 16, 24 or 32 bytes in length");
                }

                var combined = await Base64Decode(cipherText);
                var cipherTextBuffer = new byte[combined.Length];

                using (var aes = Aes.Create())
                {
                    aes.Key = key;

                    var iv = new byte[16];
                    var ciphertext = new byte[cipherTextBuffer.Length - iv.Length];

                    Array.ConstrainedCopy(combined, 0, iv, 0, iv.Length);
                    Array.ConstrainedCopy(combined, iv.Length, ciphertext, 0, ciphertext.Length);

                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var resultStream = new MemoryStream())
                    {
                        using (var aesStream = new CryptoStream(resultStream, decryptor, CryptoStreamMode.Write))
                        using (var plainStream = new MemoryStream(ciphertext))
                        {
                            plainStream.CopyTo(aesStream);
                        }

                        return Encoding.UTF8.GetString(resultStream.ToArray());
                    }
                }
            }
        }
    }
}

