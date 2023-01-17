using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using ClientCore;
using UnityEngine;
using HttpContent = ClientCore.HttpContent;

/**
 * 网络通讯实现抽象类
 *  派生类只需要实现以下接口,成功或者失败调用MarkRequestDone()即可
 *  public abstract void DoRequestImplement(Hashtable headers, byte[] requestBytes);
 *  public abstract void Tick(float delta);
 *  public abstract void Dispose();
 */
public abstract class AbstractHttpRequest : IAsyncRequest
{
    // Fields
    protected HttpContent _httpContent;
    public HttpContent HttpContent => _httpContent;

    protected HttpEncoderProvider _encoderProvider;
    protected HttpDecoderProvider _decoderProvider;
    protected HttpProcessorProvider _processorProvider;
    protected HttpUrlProvider _urlProvider;
    protected bool _isDone = false;
    protected bool _isSuccess = false;
    public static readonly DateTime time19700101 = new DateTime(0x7b2, 1, 1);

    #region abstract functions
    public abstract void DoRequestImplement(Hashtable headers, byte[] requestBytes);
    public abstract void Tick(float delta);
    public abstract void Dispose();
    #endregion
    
    //
    public AbstractHttpRequest(HttpContent httpContent, HttpUrlProvider urlProvider, HttpEncoderProvider encoderProvider, HttpDecoderProvider decoderProvider, HttpProcessorProvider processorProvider)
    {
        this._httpContent = httpContent;
        this._urlProvider = urlProvider;
        this._encoderProvider = encoderProvider;
        this._decoderProvider = decoderProvider;
        this._processorProvider = processorProvider;
    }
    
    public void DoRequest()
    {
        List<IHttpProcessor> list = this._processorProvider();
        foreach (IHttpProcessor processor in list)
        {
            if (!processor.ProcessBeforeEncoding(this._httpContent))
            {
                break;
            }
        }
        foreach (IHttpEncoder encoder in this._encoderProvider())
        {
            if (!encoder.Encode(this._httpContent))
            {
                break;
            }
        }
        foreach (IHttpProcessor processor in list)
        {
            if (!processor.ProcessAfterEncoding(this._httpContent))
            {
                break;
            }
        }
        
        DoRequestImplement(this._httpContent.RequestHeader, this._httpContent.RequestBytes);
    }

    public void AfterDone()
    {
        List<IHttpProcessor> list = this._processorProvider();
       
        foreach (IHttpProcessor processor in list)
        {
            if (!processor.ProcessAfterDecoding(this._httpContent))
            {
                break;
            }
        }
        
        foreach (IHttpProcessor processor in list)
        {
            if (!processor.ProcessBeforeDispose(this._httpContent))
            {
                break;
            }
        }
    }

    protected void SetResponseData(bool isSuccess, byte[] responseBytes, int httpStatus, long sendUtcTime, long receiveUtcTime)
    {
        _isSuccess = isSuccess;
        _httpContent.ResponseBytes = responseBytes;
        _httpContent.HttpStatus = httpStatus;
        _httpContent.SendUtcTime = sendUtcTime;
        _httpContent.ReceiveUtcTime = receiveUtcTime;
        _httpContent.Latency = (receiveUtcTime > 0 ? receiveUtcTime - sendUtcTime : 0);
    }

    protected void SetDecodeResult(bool decodeResult)
    {
        _httpContent.DecodeSuccess = decodeResult;
    }

    protected void MarkRequestDone()
    {
        _isDone = true;
    }
    
    // Properties
    public bool IsDone =>
        this._isDone;

    public bool IsSuccess =>
        this._isSuccess;

    public string ErrorMessage { get; }

    public int Priority { get; set; }

    protected long CurrentUtcTime =>
        (long) DateTime.UtcNow.Subtract(time19700101).TotalMilliseconds;
}

