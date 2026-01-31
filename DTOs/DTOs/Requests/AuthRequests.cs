using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Requests
{
    /// <summary>
    /// Request model for user registration.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// The full name of the user.
        /// </summary>
        /// <example>John Doe</example>
        [Required]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// The email address of the user.
        /// </summary>
        /// <example>john.doe@example.com</example>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The password for the user account (minimum 6 characters).
        /// </summary>
        /// <example>SecurePass123!</example>
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// The role of the user (e.g., Annotator, Reviewer, Admin). Defaults to "Annotator".
        /// </summary>
        /// <example>Annotator</example>
        public string Role { get; set; } = "Annotator";
    }

    /// <summary>
    /// Request model for user login.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// The email address of the user.
        /// </summary>
        /// <example>john.doe@example.com</example>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The password of the user.
        /// </summary>
        /// <example>SecurePass123!</example>
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for updating payment information.
    /// </summary>
    public class UpdatePaymentRequest
    {
        /// <summary>
        /// The name of the bank.
        /// </summary>
        /// <example>Chase Bank</example>
        [Required]
        public string BankName { get; set; } = string.Empty;

        /// <summary>
        /// The bank account number.
        /// </summary>
        /// <example>1234567890</example>
        [Required]
        public string BankAccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// The tax identification code.
        /// </summary>
        /// <example>TAX-987654321</example>
        [Required]
        public string TaxCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for updating user profile information.
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// The new full name of the user.
        /// </summary>
        /// <example>Jane Doe</example>
        public string? FullName { get; set; }

        /// <summary>
        /// The new email address of the user.
        /// </summary>
        /// <example>jane.doe@example.com</example>
        public string? Email { get; set; }

        /// <summary>
        /// The new role of the user.
        /// </summary>
        /// <example>Reviewer</example>
        public string? Role { get; set; }

        /// <summary>
        /// The new password for the user.
        /// </summary>
        /// <example>NewSecurePass456!</example>
        public string? Password { get; set; }
    }
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
