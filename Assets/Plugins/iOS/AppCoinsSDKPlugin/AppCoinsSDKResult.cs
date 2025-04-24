using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AppCoinsSDKPurchaseStatus
{
    Success,
    Pending,
    Cancelled,
    Failed
}

public class AppCoinsSDKPurchaseResult<T>{
    public AppCoinsSDKPurchaseStatus    Status { get; }
    public T                            Value { get; }
    public AppCoinsSDKError             Error { get; }

    private AppCoinsSDKPurchaseResult(
        AppCoinsSDKPurchaseStatus status,
        T value = default,
        AppCoinsSDKError error = null)
    {
        Status   = status;
        Value    = value;
        Error    = error;
    }

    public static AppCoinsSDKPurchaseResult<T> Success(T value)
        => new AppCoinsSDKPurchaseResult<T>(
             status:   AppCoinsSDKPurchaseStatus.Success,
             value:    value,
            );

    public static AppCoinsSDKPurchaseResult<T> Pending()
        => new AppCoinsSDKPurchaseResult<T>(status: AppCoinsSDKPurchaseStatus.Pending);

    public static AppCoinsSDKPurchaseResult<T> Cancelled()
        => new AppCoinsSDKPurchaseResult<T>(status: AppCoinsSDKPurchaseStatus.Cancelled);

    public static AppCoinsSDKPurchaseResult<T> Failure(string type, string message, string description, string request = null)
        => new AppCoinsSDKPurchaseResult<T>(
             status: AppCoinsSDKPurchaseStatus.Failed,
             error:  new AppCoinsSDKError(type, message, description, request));
}

public class AppCoinsSDKResult<T>
{
    public bool             IsSuccess { get; }
    public T                Value     { get; }
    public AppCoinsSDKError Error    { get; }

    private AppCoinsSDKResult(T value)
    {
        IsSuccess = true;
        Value     = value;
    }

    private AppCoinsSDKResult(AppCoinsSDKError error)
    {
        IsSuccess = false;
        Error     = error;
    }

    public static AppCoinsSDKResult<T> Success(T value)
        => new AppCoinsSDKResult<T>(value);

    public static AppCoinsSDKResult<T> Failure(string type, string message, string description, string request = null)
        => new AppCoinsSDKResult<T>(new AppCoinsSDKError(type, message, description, request));
}

public class AppCoinsSDKError
{
    public string Type  { get; }
    public string Message { get; }
    public string Description { get; }
    public string Request { get; }

    public AppCoinsSDKError(string type, string message, string description, string request = null)
    {
        Type        = type;
        Message     = message;
        Description = description;
        Request     = request;
    }

    public override string ToString()
        => $"[{Type}] {Message}: {Description}"
           + (Request != null 
               ? $"\nRequest: {Request}" 
               : "");
}