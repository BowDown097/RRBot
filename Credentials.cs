namespace RRBot;
internal sealed class Credentials
{
    private static readonly string AssemblyDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "";
    private static readonly string CredentialsPath = Path.Combine(AssemblyDir, "credentials.json");

    private static Credentials? _instance;
    public static Credentials Instance => _instance ??= new Credentials(CredentialsPath);

    public enum ValidationResult
    {
        Success,
        MissingCredentialsFile,
        NeedMongoConnectionString,
        NeedToken
    }

    [JsonProperty("mongoConnectionString")] public string MongoConnectionString { get; set; } = "";
    [JsonProperty("token")] public string Token { get; set; } = "";

    [JsonConstructor] private Credentials() {}

    private Credentials(string jsonPath)
    {
        Credentials? c = JsonConvert.DeserializeObject<Credentials>(File.ReadAllText(jsonPath));
        if (c is null)
        {
            Console.WriteLine("credentials.json file not found or is not a valid JSON file.");
            Environment.Exit(1);
        }

        MongoConnectionString = c.MongoConnectionString;
        Token = c.Token;
    }

    [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
    public ValidationResult Validate()
    {
        if (!File.Exists(CredentialsPath))
            return ValidationResult.MissingCredentialsFile;
        if (string.IsNullOrWhiteSpace(MongoConnectionString))
            return ValidationResult.NeedMongoConnectionString;
        if (string.IsNullOrWhiteSpace(Token))
            return ValidationResult.NeedToken;
        return ValidationResult.Success;
    }
}