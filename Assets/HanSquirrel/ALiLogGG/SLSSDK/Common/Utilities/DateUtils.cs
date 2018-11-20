/*
 * Copyright (C) Alibaba Cloud Computing
 * All rights reserved.
 * 
 * 版权所有 （C）阿里云计算有限公司
 */

using GLib;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Aliyun.Api.LOG.Common.Utilities
{
    /// <summary>
    /// Description of DateUtils.
    /// GG 20181020修改，原始版本如果没有运行在北京时区，则发送的时间戳会混乱。
    /// </summary>
    public static class DateUtils
    {
        private static DateTime _1970StartDateTimeUTC = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private const string _rfc822DateFormat = "ddd, dd MMM yyyy HH:mm:ss \\G\\M\\T";
        private const string _iso8601DateFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        /// <summary>
        /// Formats an instance of <see cref="DateTime" /> to a GMT string.
        /// </summary>
        /// <param name="dt">The date time to format.</param>
        /// <returns></returns>
        public static string FormatRfc822Date(DateTime dt)
        {
            return dt.ToUniversalTime().ToString(_rfc822DateFormat,
                               CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats a GMT date string to an object of <see cref="DateTime" />.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime ParseRfc822Date(String dt)
        {
            Debug.Assert(!string.IsNullOrEmpty(dt));
            return DateTime.SpecifyKind(
                DateTime.ParseExact(dt,
                                    _rfc822DateFormat,
                                    CultureInfo.InvariantCulture),
                DateTimeKind.Utc);
        }

        /// <summary>
        /// Formats a date to a string in the format of ISO 8601 spec.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string FormatIso8601Date(DateTime dt)
        {
            return dt.ToUniversalTime().ToString(_iso8601DateFormat,
                               CultureInfo.CreateSpecificCulture("en-US"));
        }

        /// <summary>
        /// convert time stamp to DateTime.
        /// </summary>
        /// <param name="timeStamp">seconds</param>
        /// <returns></returns>
        public static DateTime GetDateTime(uint timeStamp)
        {
            return _1970StartDateTimeUTC.AddSeconds(timeStamp).ToLocalTime();
        }

        public static uint TimeSpan()
        {
            return (uint)((DateTime.UtcNow - _1970StartDateTimeUTC).TotalSeconds);
        }

        private static TimeSpan _BJTimeDiff = new TimeSpan(08, 00, 00);

        public static DateTime ToBJTime(DateTime dt)
        {
            /// TimeZoneInfo.Local 在Untiy中没有实现。因此用此手工方法
            return dt.ToUniversalTime() + _BJTimeDiff;
        }

        public static uint TimeSpan(DateTime dt)
        {
            return (uint)(dt.ToUniversalTime() - _1970StartDateTimeUTC).TotalSeconds;
        }
    }
}
