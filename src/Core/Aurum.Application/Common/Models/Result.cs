using System;
using System.Collections.Generic;
using System.Text;

namespace Aurum.Application.Common.Models
{
    public class Result
    {
        public bool Succeeded { get; protected set; }
        public IEnumerable<string> Errors { get; protected set; } = [];

        public static Result Success() => new() { Succeeded = true };
        public static Result Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors };
    }

    public class Result<T> : Result
    {
        public T? Data { get; private set; }

        public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
        new public static Result<T> Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors };
    }
}
