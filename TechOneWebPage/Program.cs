using System;
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
                    if (posTwo) return "Forty";
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

        private string Convert(char c, int pos, int length, bool posOneAndTwo, bool removeAnd, bool cents = false)
        {
            string retStr = null;
            //if (c == '.') return retStr;
            switch (pos % 3)
            {
                case -1:
                    if (posOneAndTwo)
                    {
                        if (!removeAnd) retStr = "And ";
                        retStr += GetWord(c, false, true);
                    }
                    else { retStr = GetWord(c); }
                    if (pos == -1) retStr += cents == false ? " Dollars" : " Cents";
                    if (pos == -4 && (length <= 6 || retStr != null)) retStr += " Thousand";
                    if (pos == -7 && (length <= 9 || retStr != null)) retStr += " Million";
                    if (pos == -10 && (length <= 12 || retStr != null)) retStr += " Billion";
                    if (pos == -13 && (length <= 15 || retStr != null)) retStr += " Trillion";
                    if (pos == -16 && (length <= 18 || retStr != null)) retStr += " Quadrillion";
                    if (pos == -19 && (length <= 21 || retStr != null)) retStr += " Sextillion";
                    break;
                case -2:
                    string word = GetWord(c, true);
                    if (word != null)
                    {
                        if (!removeAnd) retStr = "And ";
                        retStr += word;
                    }
                    break;
                case 0:
                    retStr = GetWord(c);
                    if (retStr != null ) retStr += " Hundred";
                    break;
            }
            return retStr;
        }

        private string NumToWords(string num)
        {
            int decimalIndex = num.IndexOf('.');
            if (decimalIndex == -1) { decimalIndex = num.Length; } // if no decimal in num, set to be length of string
            string dollars = num.Substring(0, decimalIndex);
            string cents = num.Substring(decimalIndex); // if number has cents then store it
            List<System.Threading.Tasks.Task> tasks = new List<System.Threading.Tasks.Task>();
            if (dollars[0] == '-')
            {
                dollars = dollars.Substring(1);
                tasks.Add(System.Threading.Tasks.Task.Run(() => { return "Negative"; }));
            }

            for (int i = 0; i < dollars.Length; i++) // DOLLARS FORLOOP
            {
                if (float.Parse(dollars) == 0)
                {
                    tasks.Add(System.Threading.Tasks.Task.Run(() => { return "Zero Dollars"; }));
                    break;
                }
                var index = i;
                if (dollars[index] == '1' && (dollars.Length - index) % 3 == 2)
                { // to deal with numbers ten ... nineteen specifically
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        return Convert(dollars[index + 1], index - dollars.Length + 1, dollars.Length, true, index == 0 ? true: false);
                    }));
                    i++;
                }
                else
                {
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        return Convert(dollars[index], index - dollars.Length, dollars.Length, false, index == 0 ? true : false);
                    }));
                }
            }

            if (cents.Length > 1 && dollars.Length > 0 && float.Parse(dollars) >= 0) tasks.Add(System.Threading.Tasks.Task.Run(() =>{return "<b>AND</b>"; }));
            bool leadingZerosCents = true; int start = 0;
            for (int i = 0; i < cents.Length; i++) // CENTS FORLOOP
            {
                var index = i;
                if (cents[index] == '.') continue;
                if (leadingZerosCents && cents[index] != '0') {
                    leadingZerosCents = false; start = index;
                }
                if (leadingZerosCents) continue;

                if (cents[index] == '1' && (cents.Length - index) % 3 == 2)
                { // to deal with numbers eleven ... nineteen
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        return Convert(cents[index + 1], index - cents.Length + 1, num.Length, true, index == start ? true : false, true);
                    }));
                    i++;
                }
                else
                {
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        return Convert(cents[index], index - cents.Length, num.Length, false, index == start ? true : false, true);
                    }));
                }
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            string result = "";
            foreach (System.Threading.Tasks.Task<string> item in tasks)
            {
                if (item.Result != null) result += item.Result + " ";
            }
            return result;
        }

        public void Run()
        {
            //NumToWords("-.45");
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
                                    foreach (char s in data) //NOTE DOESNT ACCOUNT FOR MULTIPLES
                                    {
                                        if (s == '=' && !isData) { isData = true; }
                                        else if (isData) { number = number + s; }
                                    }
                                    rstr = NumToWords(number);
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
