using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WashTrack.Data;
using WashTrack.Models;

namespace WashTrack.MVVM.ViewModels
{
    // Drives the login page, which is really three screens in one. The page
    // swaps between them using IsRegisterMode / IsRecoveryMode:
    //   register — shown only on a fresh install, to create the owner account
    //   login    — the normal path
    //   recovery — username -> security question -> new password
    // Passwords and security answers are only ever stored as SHA-256 hashes.
    public partial class LoginViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        // Fixed list offered at registration; the chosen one is saved on the
        // User row and shown back during recovery.
        public ObservableCollection<string> SecurityQuestions { get; } = new()
        {
            "What was the name of your first pet?",
            "What city were you born in?",
            "What is your mother's maiden name?",
            "What was your first laundry shop's nickname?"
        };

        [ObservableProperty] private bool isRegisterMode;
        [ObservableProperty] private bool isRecoveryMode;
        [ObservableProperty] private bool isBusy;

        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string password = string.Empty;
        [ObservableProperty] private string confirmPassword = string.Empty;
        [ObservableProperty] private string selectedSecurityQuestion = string.Empty;
        [ObservableProperty] private string securityAnswer = string.Empty;

        [ObservableProperty] private string recoveryUsername = string.Empty;
        [ObservableProperty] private bool recoveryQuestionLoaded;
        [ObservableProperty] private string recoveryQuestion = string.Empty;
        [ObservableProperty] private string recoveryAnswer = string.Empty;
        [ObservableProperty] private string newPassword = string.Empty;
        [ObservableProperty] private string confirmNewPassword = string.Empty;

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private string infoMessage = string.Empty;

        public string SubmitButtonText => IsRegisterMode ? "Register" : "Login";

        public LoginViewModel(WashTrackContext context)
        {
            _context = context;
            _ = InitializeAsync();
        }

        // No owner account yet means this is a first launch, so open straight
        // into registration instead of asking for credentials that don't exist.
        private async Task InitializeAsync()
        {
            IsRegisterMode = !await _context.Users.AnyAsync();
        }

        // SubmitButtonText is derived, so it has to be re-raised by hand.
        partial void OnIsRegisterModeChanged(bool value)
        {
            OnPropertyChanged(nameof(SubmitButtonText));
        }

        // Single handler behind the one button on the login page — it registers
        // or logs in depending on which mode the page is currently in.
        [RelayCommand]
        private async Task SubmitAsync()
        {
            if (IsBusy) return;

            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Username and password are required.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsRegisterMode)
                    await RegisterAsync();
                else
                    await LoginAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Creates the one and only owner account, then drops the user into the app.
        private async Task RegisterAsync()
        {
            // Someone else may have registered first between page load and submit
            // (e.g. two devices sharing a fresh install) — re-check before writing.
            if (await _context.Users.AnyAsync())
            {
                IsRegisterMode = false;
                ErrorMessage = "An owner account already exists. Please log in.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }
            if (Password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SelectedSecurityQuestion))
            {
                ErrorMessage = "Please choose a security question.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SecurityAnswer))
            {
                ErrorMessage = "Please provide an answer for account recovery.";
                return;
            }

            var owner = new User
            {
                Username = Username.Trim(),
                Password = HashText(Password),
                SecurityQuestion = SelectedSecurityQuestion,
                SecurityAnswer = HashText(NormalizeAnswer(SecurityAnswer)),
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(owner);
            await _context.SaveChangesAsync();

            CompleteLogin();
        }

        private async Task LoginAsync()
        {
            // Compare hashes, never plain text — the stored password is a hash.
            var hashed = HashText(Password);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Username.Trim());

            // Same message for both cases so a wrong guess can't reveal
            // whether the username exists.
            if (user is null || user.Password != hashed)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            CompleteLogin();
        }

        // Enters recovery mode with every field cleared, so a half-finished
        // attempt never carries over into the next one.
        [RelayCommand]
        private void ForgotPassword()
        {
            IsRecoveryMode = true;
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;
            RecoveryUsername = string.Empty;
            RecoveryQuestionLoaded = false;
            RecoveryQuestion = string.Empty;
            RecoveryAnswer = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
        }

        [RelayCommand]
        private void BackToLogin()
        {
            IsRecoveryMode = false;
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;
        }

        // Recovery step 1: look up the account and reveal its security question.
        [RelayCommand]
        private async Task FindAccountAsync()
        {
            if (IsBusy) return;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(RecoveryUsername))
            {
                ErrorMessage = "Enter the owner username.";
                return;
            }

            IsBusy = true;
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == RecoveryUsername.Trim());

                if (user is null)
                {
                    ErrorMessage = "No account found with that username.";
                    return;
                }

                // Unlocks the answer + new password fields on the page.
                RecoveryQuestion = user.SecurityQuestion;
                RecoveryQuestionLoaded = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Recovery step 2: check the security answer, then overwrite the password.
        [RelayCommand]
        private async Task ResetPasswordAsync()
        {
            if (IsBusy) return;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(RecoveryAnswer))
            {
                ErrorMessage = "Please answer the security question.";
                return;
            }
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                ErrorMessage = "New password must be at least 6 characters.";
                return;
            }
            if (NewPassword != ConfirmNewPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            IsBusy = true;
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == RecoveryUsername.Trim());

                if (user is null || user.SecurityAnswer != HashText(NormalizeAnswer(RecoveryAnswer)))
                {
                    ErrorMessage = "That answer doesn't match our records.";
                    return;
                }

                user.Password = HashText(NewPassword);
                await _context.SaveChangesAsync();

                // Back to login with the username pre-filled, password blank —
                // the owner still has to log in with what they just set.
                IsRecoveryMode = false;
                Username = user.Username;
                Password = string.Empty;
                InfoMessage = "Password reset. You can log in now.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Replaces the login page with the main shell. Swapping the root page
        // (rather than navigating) means there's no back route to the login screen.
        private static void CompleteLogin()
        {
            Application.Current!.Windows[0].Page = new AppShell();
        }

        // Security answers are matched case- and whitespace-insensitively, so
        // they must be normalized the same way when saved and when checked.
        private static string NormalizeAnswer(string answer) => answer.Trim().ToLowerInvariant();

        private static string HashText(string text)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes);
        }
    }
}
