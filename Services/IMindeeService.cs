namespace CarInsuranceBot.Services
{
    /// <summary>
    /// Defines the contract for document processing services using Mindee OCR and Computer Vision.
    /// Provides methods for extracting metadata from identity and vehicle documents.
    /// </summary>
    public interface IMindeeService
    {
        /// <summary>
        /// Asynchronously parses a passport image to extract personal identification data.
        /// </summary>
        /// <param name="fileBytes">The raw image data of the passport.</param>
        /// <param name="fileName">The name of the uploaded file.</param>
        /// <returns>A formatted string containing the extracted passport details.</returns>
        Task<string> ParsePassportAsync(byte[] fileBytes, string fileName);

        /// <summary>
        /// Asynchronously parses a vehicle registration document (e.g., Carte Grise) 
        /// to extract technical vehicle data.
        /// </summary>
        /// <param name="fileBytes">The raw image data of the vehicle document.</param>
        /// <param name="fileName">The name of the uploaded file.</param>
        /// <returns>A formatted string containing the extracted vehicle details.</returns>
        Task<string> ParseVehicleDocumentAsync(byte[] fileBytes, string fileName);
    }
}