﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace TechOneWebPage
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(IReadOnlyCollection<string> prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            }

            // URI prefixes are required eg: "http://localhost:8080/numToWords/"
            if (prefixes == null || prefixes.Count == 0)
            {
                throw new ArgumentException("URI prefixes are required");
            }

            foreach (var s in prefixes)
            {
                _listener.Prefixes.Add(s);
            }

            _responderMethod = method ?? throw new ArgumentException("responder method required");
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
           : this(prefixes, method)
        {
        }

        private string GetWord(char c, bool posTwo = false, bool posOneAndTwo = false)
        {
            switch (c)
            {
                //case '.':
                //    return "And";
                case '0':
                    if (posOneAndTwo) return "Ten";
                    return null;
                case '1':
                    if (posOneAndTwo) return "Eleven";
                    return "One";
                case '2':
                    if (posTwo) return "Twenty";
                    if (posOneAndTwo) return "Twelve";
                    return "Two";
                case '3':
                    if (posTwo) return "Thirty";
                    if (posOneAndTwo) return "Thirteen";
                    return "Three";
                case '4':
                    if (posTwo) return "Fourty";
                    if (posOneAndTwo) return "Fourteen";
                    return "Four";
                case '5':
                    if (posTwo) return "Fifty";
                    if (posOneAndTwo) return "Fifteen";
                    return "Five";
                case '6':
                    if (posTwo) return "Sixty";
                    if (posOneAndTwo) return "Sixteen";
                    return "Six";
                case '7':
                    if (posTwo) return "Seventy";
                    if (posOneAndTwo) return "Seventeen";
                    return "Seven";
                case '8':
                    if (posTwo) return "Eighty";
                    if (posOneAndTwo) return "Eighteen";
                    return "Eight";
                case '9':
                    if (posTwo) return "Ninety";
                    if (posOneAndTwo) return "Nineteen";
                    return "Nine";
            }
            return null;
        }

        private string Convert(char c, int pos, bool posOneAndTwo, bool removeAnd)
        {
            string retStr = null;
            switch (pos)
            {
                case -1:
                    if (posOneAndTwo)
                    {
                        if (!removeAnd) retStr = "And ";
                        retStr += GetWord(c, false, true);
                    }
                    else { retStr = GetWord(c); }
                    retStr += " Dollars";
                    break;
                case -2:
                    string word = GetWord(c, true);
                    if (!removeAnd) retStr = "And ";
                    if (word != null)
                    {
                        retStr += word;
                    }
                    break;
                case -3:
                    retStr = GetWord(c);
                    if (retStr != null ) retStr += " Hundred";
                    break;
                case -4:
                    if (posOneAndTwo) { retStr = GetWord(c, false, true); }
                    else { retStr = GetWord(c); }
                    if (c != '0') retStr += " Thousand";
                    break;
                case -5:
                    retStr = GetWord(c, true);
                    break;
                case -6:
                    if (!removeAnd && c != '0') retStr = "And ";
                    word = GetWord(c);
                    if (word != null)
                    {
                        retStr += word;
                        retStr += " Hundred";
                    }
                    break;
                case -7:
                    if (posOneAndTwo)
                    {
                        if (!removeAnd) retStr = "And ";
                        retStr += GetWord(c, false, true);
                    }
                    else {
                        retStr = GetWord(c);
                    }
                    retStr += " Million";
                    break;
                case -8:
                    word = GetWord(c, true);
                    if (word != null)
                    {
                        if (!removeAnd) retStr = "And ";
                        retStr += GetWord(c, true);
                    }
                    break;
                case -9:
                    retStr = GetWord(c);
                    if (retStr != null) retStr += " Hundred";
                    break;
            }

            return retStr;
        }

        private string NumToWords(string num)
        {
            List<System.Threading.Tasks.Task> tasks = new List<System.Threading.Tasks.Task>(num.Length);
            int decimalIndex = num.IndexOf('.');
            if (decimalIndex == -1) { decimalIndex = num.Length; } // if no decimal in num, set to be length of string

            for(int i = 0; i < decimalIndex; i++)
            {
                var index = i;
                if (num[index] == '1' && (decimalIndex - index) % 3 == 2)
                { // to deal with numbers eleven ... nineteen
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        return Convert(num[index + 1], index - decimalIndex + 1, true, index == 0 ? true: false);
                    }));
                    i++;
                }
                else
                {
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        return Convert(num[index], index - decimalIndex, false, index == 0 ? true : false);
                    }));
                }
            }
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            var result = "";
            foreach (System.Threading.Tasks.Task<string> item in tasks)
            {
                Console.Write(item.Result + " ");
            }
            return "";
        }

        public void Run()
        {
            NumToWords("11202.");
            Console.WriteLine("\nretu");
            ThreadPool.QueueUserWorkItem(o =>
            {
                Console.WriteLine("Webserver running...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem(c =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                if (ctx == null)
                                {
                                    return;
                                }
                                var rstr = _responderMethod(ctx.Request); ;
                                if (ctx.Request.HasEntityBody)
                                {
                                    System.IO.Stream body = ctx.Request.InputStream;
                                    System.Text.Encoding encoding = ctx.Request.ContentEncoding;
                                    System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);

                                    string data = reader.ReadToEnd();
                                    string number = ""; bool isData = false;
                                    foreach(char s in data) //NOTE DOESNT ACCOUNT FOR MULTIPLES
                                    {
                                        if (s == '=' && !isData) { isData = true; }
                                        else if (isData) { number = number + s; }
                                    }
                                    NumToWords(number);
                                    rstr = number;
                                }
                                var buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch
                            {
                                // ignored
                            }
                            finally
                            {
                                // always close the stream
                                if (ctx != null)
                                {
                                    ctx.Response.OutputStream.Close();
                                }
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (Exception ex)
                {
                    // ignored
                }
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }

    internal class Program
    {
        public static string SendResponse(HttpListenerRequest request)
        {
            var directory = Environment.CurrentDirectory;
            var site = System.IO.File.ReadAllText(directory + "\\site.html");
            //var ret = string.Format(site);
            return site;
        }

        private static void Main(string[] args)
        {
            var ws = new WebServer(SendResponse, "http://localhost:8080/numToWords/");
            ws.Run();
            Console.WriteLine("Num to Words server running at http://localhost:8080/numToWords/. Press any key to quit.");
            Console.ReadKey();
            ws.Stop();
        }
    }
}