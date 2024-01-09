﻿using System;
using RabbitMq.Connector.Exceptions;
using RabbitMq.Connector.Model;

namespace RabbitMq.Connector
{
    public class RetryPolicyConfiguration<T> where T : Event
    {
        internal RetryPolicyConfiguration() { }

        internal int MaxRetryTimes { get; private set; }
        internal bool ForeverRetry { get; private set; } = true;
        internal Func<Failure<T>, TimeSpan> RetryTime { get; private set; } = f => TimeSpan.FromSeconds(5);
        internal Func<Failure<T>, bool> Retry { get; private set; } = f => true;
        internal Func<Failure<T>, bool> DiscardEvent { get; private set; } = f => false;

        public RetryPolicyConfiguration<T> RetryForTimes(int times)
        {
            MaxRetryTimes = times;
            ForeverRetry = false;
            return this;
        }

        public RetryPolicyConfiguration<T> RetryForever()
        {
            if (MaxRetryTimes > 0)
                throw new CantRetryForeverOverRetryTimesConfigurationException();

            ForeverRetry = true;
            return this;
        }

        public RetryPolicyConfiguration<T> ForEachRetryWait(TimeSpan waitTime)
        {
            RetryTime = f => waitTime;
            return this;
        }

        public RetryPolicyConfiguration<T> ForEachRetryWait(Func<Failure<T>, TimeSpan> retryFunc)
        {
            RetryTime = retryFunc;
            return this;
        }

        public RetryPolicyConfiguration<T> ShouldRetry(Func<Failure<T>, bool> shoudlRetry)
        {
            Retry = shoudlRetry;
            return this;
        }

        public RetryPolicyConfiguration<T> ShouldRetry(bool shoudlRetry = true)
        {
            Retry = f => shoudlRetry;
            return this;
        }

        public RetryPolicyConfiguration<T> ShouldNotRetry()
        {
            Retry = f => false;
            return this;
        }

        public RetryPolicyConfiguration<T> ShouldDiscardEventAfterFailures(Func<Failure<T>, bool> shouldDiscardEventAfterFail)
        {
            DiscardEvent = shouldDiscardEventAfterFail;
            return this;
        }

        public RetryPolicyConfiguration<T> ShouldNotDiscardEventAfterFailures()
        {
            DiscardEvent = f => false;
            return this;
        }

        public RetryPolicyConfiguration<T> ShouldDiscardEventAfterFailures(bool shouldDiscardEventAfterFail = true)
        {
            DiscardEvent = f => shouldDiscardEventAfterFail;
            return this;
        }
    }
}