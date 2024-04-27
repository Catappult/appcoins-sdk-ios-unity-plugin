using System;
using AOT;

// Define the MonoPInvokeCallback attribute if it's not already defined in your project.
public class MonoPInvokeCallbackAttribute : Attribute
{
    public MonoPInvokeCallbackAttribute(Type type) { }
}
