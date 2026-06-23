#nullable enable
namespace ProjectAscension.Shared
{
    public class Result<T>
    {
        public T? Value { get; }
        public Error Error { get; }
        public bool IsSuccess => Error == Error.None;

        private Result(T value) { Value = value; Error = Error.None; }
        private Result(Error error) { Error = error; }

        public static Result<T> Ok(T value) => new(value);
        public static Result<T> Fail(Error error) => new(error);
    }
}
