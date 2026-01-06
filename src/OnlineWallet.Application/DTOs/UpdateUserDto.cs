namespace OnlineWallet.Application.DTOs
{
    /// <summary>
    /// Data transfer object for updating user information.
    /// Used when modifying user details.
    /// </summary>
    public class UpdateUserDto
    {
        /// <summary>
        /// User's first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// User's last name.
        /// </summary>
        public string LastName { get; set; }
    }
}