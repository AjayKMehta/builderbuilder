﻿//HintName: PointBuilder.g.cs
// Generated by BuilderGenerator
// Assembly: Tests
// Language version: CSharp13
// Source type: Point
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;

namespace <global namespace>;

[System.CodeDom.Compiler.GeneratedCode("BuilderGenerator", "v1.0.0.0")]
public class PointBuilder{
    public int X { get; set; }
    public int Y { get; set; }

    public Point Build() =>
        new Point(X, Y);
}

