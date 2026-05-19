namespace NexusAs.Api.Responses
{
    public class ApiResponse<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();

        public static ApiResponse<T> Success(T data, string message = "Operación exitosa.")
        {
            return new ApiResponse<T>
            {
                Succeeded = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Succeeded = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}