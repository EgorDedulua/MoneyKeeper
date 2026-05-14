namespace MoneyKeeper.Infrastructure.Auth
{
    public class JwtOptions
    {
        public TimeSpan Expires { get; set; }

        public string SecretKey { get; set; } = string.Empty;
    }
}
