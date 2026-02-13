namespace projet0.Application.Commun.Ressources
{
    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public string? Message { get; set; }
        public int ResultCode { get; set; }

        public bool IsSuccess => ResultCode == 0;

        private ApiResponse() { }

        //  Succès
        public static ApiResponse<T> Success(
            T? data = default,
            string? message = null,
 int resultCode = 0)
        {
            return new ApiResponse<T>
            {
                Data = data,
                Message = message,
                ResultCode = resultCode,
                Errors = null
            };
        }

        //  Erreur
        public static ApiResponse<T> Failure(
            string message,
            List<string>? errors = null,
        int resultCode = 1
            )
        {
            return new ApiResponse<T>
            {
                Data = default,

                Message = message,
                Errors = errors ?? new List<string>(),
                ResultCode = resultCode
            };
        }
    }
}
