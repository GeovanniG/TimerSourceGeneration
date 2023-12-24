using System;

namespace TimerSourceGeneration;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class TimerAttribute : Attribute
{
    public TimerAttribute()
    {
    }
}
