﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Leaf.xNet;

namespace UsefulExtensions
{
    /// <summary>
    /// Статический класс, предоставляющий методы, часто используемые в создании софта (чекеры, регеры и пр. софт для автоматизации действий на сайтах)
    /// </summary>
    public static class Helper
    {
        #region time stamp

        /// <summary>
        /// Возвращает Unix TimeStamp Seconds
        /// </summary>
        /// <returns>Количество секунд, прошедших с 1 января 1970 г. Часовой пояс UTC</returns>
        public static long GetUnixSeconds() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Возвращает Unix TimeStamp Seconds
        /// </summary>
        /// <param name="dateTime">Время, timestamp которого необходимо получить</param>
        /// <returns>Количество секунд, прошедших с 1 января 1970 г. Часовой пояс UTC</returns>
        public static long GetUnixSeconds(DateTime dateTime) => new DateTimeOffset(dateTime).ToUnixTimeSeconds();

        /// <summary>
        /// Возвращает Unix TimeStamp Milliseconds
        /// </summary>
        /// <returns>Количество миллисекунд, прошедших с 1 января 1970 г. Часовой пояс UTC</returns>
        public static long GetUnixMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Возвращает Unix TimeStamp Milliseconds
        /// </summary>
        /// <param name="dateTime">Время, timestamp которого необходимо получить</param>
        /// <returns>Количество миллисекунд, прошедших с 1 января 1970 г. Часовой пояс UTC</returns>
        public static long GetUnixMilliseconds(DateTime dateTime) => new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();

        #endregion time stamp

        #region MD5 hash

        /// <summary>
        /// Возвращает MD5 хэш строки
        /// </summary>
        /// <param name="value">Исходная строка, хэш которой необходимо получить</param>
        /// <returns>MD5 хэш исходной строки</returns>
        public static string GetMD5Hash(string value) => string.Concat(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x => x.ToString("x2")));

        /// <summary>
        /// Возвращает MD5 хэш массива байтов
        /// </summary>
        /// <param name="value">Массив байтов, хэш которого необходимо получить</param>
        /// <returns>MD5 хэш исходного массива байтов</returns>
        public static string GetMD5Hash(byte[] value) => string.Concat(MD5.Create().ComputeHash(value).Select(x => x.ToString("x2")));

        #endregion MD5 hash

        public static List<List<T>> SplitToSublists<T>(this List<T> source, int count)
        {
            return source
                     .Select((x, i) => new { Index = i, Value = x })
                     .GroupBy(x => x.Index / count)
                     .Select(x => x.Select(v => v.Value).ToList())
                     .ToList();
        }

        #region accounts

        public static List<Account> ParseAccountsFromString(string value)
        {
            value = value.Replace("\r", string.Empty);
            string[] lines = value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<Account> output = new List<Account>();
            foreach(var line in lines)
            {
                if(!string.IsNullOrWhiteSpace(line))
                {
                    string[] data;
                    if(line.Contains(":"))
                        data = line.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    else if(line.Contains(";"))
                        data = line.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    else
                        continue;

                    try
                    {
                        output.Add(new Account(data[0], data[1]));
                    } catch
                    {
                        // ignored
                    }
                }
            }
            return output;
        }

        public static Task<List<Account>> ParseAccountsFromStringAsync(string value) => Task.Run(() => ParseAccountsFromString(value));

        public static List<Account> ParseAccountsFromFile(string fileName) => ParseAccountsFromString(File.ReadAllText(fileName));

        public static async Task<List<Account>> ParseAccountsFromFileAsync(string fileName) => await Task.Run(() => ParseAccountsFromFile(fileName));

        #endregion accounts

        #region proxies

        public static List<ProxyClient> ParseProxiesFromString(string value, ProxyType proxyType)
        {
            value = value.Replace("\r", string.Empty);
            string[] lines = value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return (from line in lines where !string.IsNullOrWhiteSpace(line) select ProxyClient.Parse(proxyType, line)).ToList();
        }

        public static async Task<List<ProxyClient>> ParseProxiesFromStringAsync(string value, ProxyType proxyType) => await Task.Run(() => ParseProxiesFromString(value, proxyType));

        public static List<ProxyClient> ParseProxiesFromFile(string fileName, ProxyType proxyType) => ParseProxiesFromString(File.ReadAllText(fileName), proxyType);

        public static async Task<List<ProxyClient>> ParseProxiesFromFileAsync(string fileName, ProxyType proxyType) => await Task.Run(() => ParseProxiesFromFile(fileName, proxyType));

        public static List<ProxyClient> ParseProxiesFromUrl(string url, ProxyType proxyType)
        {
            HttpRequest request = new HttpRequest();
            request.UserAgentRandomize();

            string rawProxies = request.Get(url).ToString();
            request.Dispose();

            return ParseProxiesFromString(rawProxies, proxyType);
        }

        public static async Task<List<ProxyClient>> ParseProxiesFromUrlAsync(string url, ProxyType proxyType) => await Task.Run(() => ParseProxiesFromUrl(url, proxyType));

        #endregion proxies

        #region cookies

        public static CookieStorage ParseCookiesFromString(string value, bool considerExpires = false, bool considerSecure = false)
        {
            CookieStorage storage = new CookieStorage();

            value = value.Replace("\r", string.Empty);

            string[] lines = value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach(var line in lines)
            {
                try
                {
                    if(string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] data = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    var cookie = new Cookie(data[5], data[6], data[2], data[0]);

                    if(considerExpires)
                        cookie.Expires = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[4])).DateTime;

                    if(considerSecure)
                        cookie.Secure = bool.Parse(data[3]);

                    storage.Add(cookie);
                } catch
                {
                    // ignored
                }
            }

            return storage;
        }

        public static async Task<CookieStorage> ParseCookiesFromStringAsync(string value, bool considerExpires = false, bool considerSecure = false) => await Task.Run(() => ParseCookiesFromString(value, considerExpires, considerSecure));

        public static CookieStorage ParseCookiesFromFile(string fileName, bool considerExpires = false, bool considerSecure = false) => ParseCookiesFromString(File.ReadAllText(fileName), considerExpires, considerSecure);

        public static async Task<CookieStorage> ParseCookiesFromFileAsync(string fileName, bool considerExpires = false, bool considerSecure = false) => await Task.Run(() => ParseCookiesFromFile(fileName, considerExpires, considerSecure));

        public static List<CookieStorage> ParseCookiesFromFolder(string folderPath, bool considerExpires = false, bool considerSecure = false)
        {
            List<CookieStorage> result = new List<CookieStorage>();

            string[] files = Directory.GetFiles(folderPath);

            foreach(var line in files)
            {
                result.Add(ParseCookiesFromFile(line, considerExpires, considerSecure));
            }

            return result;
        }

        public static async Task<List<CookieStorage>> ParseCookiesFromFolderAsync(string folderPath, bool considerExpires = false, bool considerSecure = false) => await Task.Run(() => ParseCookiesFromFolder(folderPath, considerExpires, considerSecure));

        #endregion cookies

        /// <summary>
        /// Преобразует строку в массив байт, используя <see cref="Encoding.UTF8"/>
        /// </summary>
        /// <param name="value">Строка, которую необходимо преобразовать в массив байтов</param>
        public static byte[] GetBytes(this string value) => Encoding.UTF8.GetBytes(value);

        /// <summary>
        /// Преобразует массив байт в строку, используя <see cref="Encoding.UTF8"/>
        /// </summary>
        /// <param name="value">Массив байт, который необходимо преобразовать в строку</param>
        public static string GetString(this byte[] value) => Encoding.UTF8.GetString(value);

        /// <summary>
        /// Копирует существующую папку в новую папку включая подкаталоги, если это указано
        /// </summary>
        /// <param name="sourceDirName">Копируемая папка</param>
        /// <param name="destDirName">Имя целевой папки. Если такой папки не существует, она будет создана</param>
        /// <param name="copySubDirs"><see langword="true"/>, чтобы скопировать папку вместе с подкаталогами и файлами в них</param>
        /// <param name="overwrite"><see langword="true"/>, чтобы разрешить перезапись файлов</param>
        public static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs, bool overwrite = true)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if(!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destDirName);

            FileInfo[] files = dir.GetFiles();
            foreach(FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, overwrite);
            }

            if(copySubDirs)
            {
                foreach(DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    CopyDirectory(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}