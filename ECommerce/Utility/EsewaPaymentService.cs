namespace ECommerce.Utility
{
    public class EsewaPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public EsewaPaymentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<EsewaResponse> MakePaymentAsync(EsewaRequest request)
        {
            var apiUrl = "https://rc-epay.esewa.com.np/api/epay/main/v2/form";
            var response = await _httpClient.PostAsJsonAsync(apiUrl, request);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EsewaResponse>();
        }
    }

    public class EsewaRequest
    {
        public string amount { get; set; }
        public string tax_amount { get; set; }
        public string total_amount { get; set; }
        public string transaction_uuid { get; set; }
        public string product_code { get; set; }
        public string product_service_charge { get; set; }
        public string product_delivery_charge { get; set; }
        public string success_url { get; set; }
        public string failure_url { get; set; }
        public string signed_field_names { get; set; }
        public string signature { get; set; }
    }

    public class EsewaResponse
    {
        public string transaction_code { get; set; }
        public string status { get; set; }
        public string transaction_uuid { get; set; }
    }
}
