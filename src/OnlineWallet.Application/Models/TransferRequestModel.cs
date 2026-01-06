namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Data transfer object for transfer requests.
    /// Used when transferring funds between accounts.
    /// </summary>
    public class TransferRequestModel
    {
        /// <summary>
        /// Destination account identifier.
        /// </summary>
        public Guid ToAccount { get; set; }

        /// <summary>
        /// Amount to transfer.
        /// </summary>
        public decimal Amount { get; set; }
    }
}