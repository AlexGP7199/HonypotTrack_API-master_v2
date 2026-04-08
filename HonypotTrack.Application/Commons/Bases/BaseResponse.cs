namespace HonypotTrack.Application.Commons.Bases;

public class BaseResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static BaseResponse<T> Success(T data, string message = "Operación exitosa")
    {
        return new BaseResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }

    public static BaseResponse<T> Fail(string message, List<string>? errors = null)
    {
        return new BaseResponse<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors
        };
    }
}
