using System;
using System.Collections.Generic;
using MoreLinq;
using MoreLinq.Extensions;
using UnityEngine;

namespace ClientCore
{
     /**
     * 异步请求接口
     */
     public interface IAsyncRequest
     {
          /**
              * 异步请求容器触发: 排队结束开始执行请求
              */
          void DoRequest();

          /**
              * 异步请求容器触发: 检测到请求完成回调
              */
          void AfterDone();

          /**
              * 异步请求容器触发: 正在执行中的执行Tick
              */
          void Tick(float delta);

          /**
              * 完成状态，供外部查询和异步请求容器查询完成状态
              */
          bool IsDone { get; }

          /**
              * 此次请求是否成功，此值IsDone后有效
              */
          bool IsSuccess { get; }

          /**
              * Dispose
              */
          void Dispose();

          /**
              * 必要的错误信息
              */
          string ErrorMessage { get; }

          /**
              * 优先级，异步请求容器基于该值排队: 值越小，优先级越高
              */
          int Priority { get; set; }
     }
}