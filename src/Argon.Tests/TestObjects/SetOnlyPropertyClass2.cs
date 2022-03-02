// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class SetOnlyPropertyClass2
{
    object _value;

    public object SetOnlyProperty
    {
        set => _value = value;
    }

    public object GetValue()
    {
        return _value;
    }
}