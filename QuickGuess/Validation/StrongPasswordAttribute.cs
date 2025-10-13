using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace QuickGuess.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class StrongPasswordAttribute : ValidationAttribute
    {
        private static readonly Regex Rx = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            RegexOptions.Compiled);

        public StrongPasswordAttribute()
            : base("Hasło musi mieć min. 8 znaków oraz zawierać małą i wielką literę, cyfrę i znak specjalny.") { }

        public override bool IsValid(object? value)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return Rx.IsMatch(s);
        }
    }
}