using Mindee;
using Mindee.Input;
using Mindee.Parsing.V2.Field;

namespace CarInsuranceBot.Services
{
    /// <summary>
    /// Service for processing documents using Mindee's Computer Vision API.
    /// Handles extraction for both personal identification and vehicle registration.
    /// </summary>
    public class MindeeService : IMindeeService
    {
        private readonly string _apiKey;

        // Custom Model ID for Passport recognition
        private readonly string _passportModelId = "574b3a07-541c-4f92-ac25-21def16e6dfe";

        // Model ID for Vehicle Registration (French Carte Grise standard)
        private readonly string _vehicleModelId = "878f56f4-2e8d-4acf-98f8-d558d8f7d48a";

        public MindeeService(IConfiguration config)
        {
            _apiKey = config["BotConfiguration:MindeeApiKey"]
                      ?? throw new Exception("Mindee API Key is missing in configuration.");
        }

        /// <summary>
        /// Extracts personal data from a passport image using a custom Mindee model.
        /// </summary>
        public async Task<string> ParsePassportAsync(byte[] fileBytes, string fileName)
        {
            try
            {
                var mindeeClient = new MindeeClientV2(_apiKey);
                var inferenceParams = new InferenceParameters(modelId: _passportModelId);

                using var memoryStream = new MemoryStream(fileBytes);
                var inputSource = new LocalInputSource(memoryStream, fileName);

                // Send document to Mindee for asynchronous inference
                var response = await mindeeClient.EnqueueAndGetInferenceAsync(inputSource, inferenceParams);
                var fields = response.Inference.Result.Fields;

                // Extract values using custom model keys defined in the Mindee dashboard
                string givenName = fields.TryGetValue("given_names", out var gn) ? gn.ToString() : "Not found";
                string surname = fields.TryGetValue("surnames", out var sn) ? sn.ToString() : "Not found";
                string passportNumber = fields.TryGetValue("passport_number", out var pn) ? pn.ToString() : "Not found";

                return $"👤 First Name: {givenName}\n👥 Last Name: {surname}\n📄 Passport No: {passportNumber}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mindee SDK Error (Passport): {ex.Message}");
                return "Failed to recognize passport. Please ensure the photo is clear and try again.";
            }
        }

        /// <summary>
        /// Extracts vehicle technical data from a registration document.
        /// Uses standardized keys (a, e, d1, c1) according to EU registration standards.
        /// </summary>
        public async Task<string> ParseVehicleDocumentAsync(byte[] fileBytes, string fileName)
        {
            try
            {
                var mindeeClient = new MindeeClientV2(_apiKey);
                var inferenceParams = new InferenceParameters(modelId: _vehicleModelId);

                using var memoryStream = new MemoryStream(fileBytes);
                var inputSource = new LocalInputSource(memoryStream, fileName);

                var response = await mindeeClient.EnqueueAndGetInferenceAsync(inputSource, inferenceParams);
                var fields = response.Inference.Result.Fields;

                // Extract vehicle data using standardized document field identifiers
                // a: Plate, e: VIN, d1: Make/Brand, c1: Owner
                string plateNumber = fields.TryGetValue("a", out var aProp) ? aProp.ToString() : "Not found";
                string vin = fields.TryGetValue("e", out var eProp) ? eProp.ToString