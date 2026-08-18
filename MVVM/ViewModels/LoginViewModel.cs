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
    public partial class LoginViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

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

        public string TitleText => IsRecoveryMode
            ? "Recover Password"
            : IsRegisterMode ? "Create Owner Account" : "Login";

        public string SubmitButtonText => IsRegisterMode ? "Register" : "Login";

        public LoginViewModel(WashTrackContext context)
        {
            _context = context;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            IsRegisterMode = !await _context.Users.AnyAsync();
        }

        partial void OnIsRegisterModeChanged(bool value)
        {
            OnPropertyChanged(nameof(TitleText));
            OnPropertyChanged(nameof(SubmitButtonText));
        }

        partial void OnIsRecoveryModeChanged(bool value) => OnPropertyChanged(nameof(TitleText));

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
            var hashed = HashText(Password);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Username.Trim());

            if (user is null || user.Password != hashed)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            CompleteLogin();
        }

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

                RecoveryQuestion = user.SecurityQuestion;
                RecoveryQuestionLoaded = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

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

        private static void CompleteLogin()
        {
            Application.Current!.Windows[0].Page = new AppShell();
        }

        private static string NormalizeAnswer(string answer) => answer.Trim().ToLowerInvariant();

        private static string HashText(string text)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes);
        }
    }
}
