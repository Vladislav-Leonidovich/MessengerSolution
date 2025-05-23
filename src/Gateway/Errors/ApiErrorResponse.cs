namespace Gateway.Errors
{
    public class ApiErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public object Details { get; set; }

        public static ApiErrorResponse FromException(Exception ex, string errorCode = "INTERNAL_ERROR")
        {
            return new ApiErrorResponse
            {
                Message = "Сталася помилка при обробці запиту",
                ErrorCode = errorCode,
                Details = ex.Message
            };
        }

        public static ApiErrorResponse ServiceUnavailable(string serviceName)
        {
            return new ApiErrorResponse
            {
                Message = $"Сервіс {serviceName} тимчасово недоступний",
                ErrorCode = "SERVICE_UNAVAILABLE"
            };
        }

        public static ApiErrorResponse Timeout(string serviceName)
        {
            return new ApiErrorResponse
            {
                Message = $"Сервіс {serviceName} не відповів вчасно",
                ErrorCode = "REQUEST_TIMEOUT"
            };
        }
    }
}
