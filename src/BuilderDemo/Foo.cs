using System;

using BuilderGenerator;

namespace BuilderDemo;

[Buildable]
public sealed class Foo<T> : IEquatable<Foo<T>>
{
    public Foo(int x, T y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public T Y { get; }

    public override bool Equals(object? obj) => obj is not null && obj is Foo<T> other && Equals(other);

    public bool Equals(Foo<T>? other) => other is not null && X == other.X && Equals(Y, other.Y);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public override string ToString() => $"Foo{{X = {X}, Y = {Y}}}";
}
