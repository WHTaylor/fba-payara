using System;
using System.Threading.Tasks;

namespace FBAPayaraD;

public abstract class Try<T>
{
    public static Try<T> TryTo(Func<T> f)
    {
        try
        {
            return new Success<T>(f.Invoke());
        }
        catch (Exception ex)
        {
            return new Failure<T>(ex);
        }
    }

    public abstract Try<U> Map<U>(Func<T, U> f);
    public abstract Try<T> Recover(Func<T> f);
    public abstract Try<T> RecoverFrom(Type t, Func<Exception, T> ex);
    public abstract T Or(T other);
    public abstract T Get();
}

internal class Success<T> : Try<T>
{
    private readonly T _value;

    public Success(T value)
    {
        _value = value;
    }

    public override Try<U> Map<U>(Func<T, U> f) =>
        Try<U>.TryTo(() => f.Invoke(_value));
    public override Try<T> Recover(Func<T> f) => this;
    public override Try<T> RecoverFrom(Type t, Func<Exception, T> f) => this;
    public override T Or(T other) => _value;
    public override T Get() => _value;
}

internal class Failure<T> : Try<T>
{
    private readonly Exception _ex;

    internal Failure(Exception ex)
    {
        _ex = ex;
    }

    public override Try<U> Map<U>(Func<T, U> f) => new Failure<U>(_ex);
    public override Try<T> Recover(Func<T> f) => TryTo(f);
    public override Try<T> RecoverFrom(Type t, Func<Exception, T> f) =>
        _ex.GetType() == t ? TryTo(() => f.Invoke(_ex)) : this;
    public override T Or(T other) => other;

    public override T Get() => throw new Exception("Cannot call 'Get' on Failure");
}
