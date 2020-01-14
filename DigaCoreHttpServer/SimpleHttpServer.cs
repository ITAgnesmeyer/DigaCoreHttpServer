using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DigaCoreHttpServer
{
  class SimpleHttpServer : IDisposable
    {
        private readonly string[] _IndexFiles = {"index.html", "index.htm", "default.html", "default.htm"};

        private static readonly IDictionary<string, string> MimeTypeMappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                #region extension to MIME type list

                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"},
                {".wasm", "application/wasm" },

                #endregion
            };

        private Thread _ServerThread;
        private string _RootDirectory;
        private HttpListener _Listener;
        private int _Port;
        private static bool _Abort;

        public int Port
        {
            get { return this._Port; }
        }

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public SimpleHttpServer(string path, int port)
        {
            Initialize(path, port);
        }

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public SimpleHttpServer(string path)
        {
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint) l.LocalEndpoint).Port;
            l.Stop();
            Initialize(path, port);
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            
           this._ServerThread.Abort();
            this._Listener.Stop();
            
        }


        public ThreadState GetThreadStatus()
        {
            if (this._ServerThread == null)
                return ThreadState.WaitSleepJoin;
            return this._ServerThread.ThreadState;
        }

        private void Listen()
        {
            this._Listener = new HttpListener();

            // Get host name
            String strHostName = Dns.GetHostName();

            List<string> portList = new List<string> {this._Port.ToString()};


            bool isStarted = false;
            foreach (var pItem in portList)
            {
                try
                {
                    this._Port = int.Parse(pItem);
                    this._Listener.Prefixes.Add($"http://localhost:{this._Port}/");
                    this._Listener.Start();

                    isStarted = true;
                }
                catch (Exception e)
                {
                    var prefix = "";
                    foreach (string listenerPrefix in this._Listener.Prefixes)
                    {
                        prefix = listenerPrefix;
                    }

                    Console.WriteLine($"Error:{e.Message} for prefix:{prefix}");
                    this._Listener.Prefixes.Clear();
                }
            }

            if (isStarted == false)
            {
                throw new Exception("Could not start Server");
            }

            foreach (var prefixItem in this._Listener.Prefixes)
            {
                if (prefixItem.Contains($":{this._Port}/"))
                {
                    Console.WriteLine($"Listen to {prefixItem}");
                }
            }


            while (_Abort == false)
            {
                try
                {
                    
                    HttpListenerContext context = this._Listener.GetContext();
                    Process(context);
                }
                catch (Exception)
                {
                    // ignored
                }

                if(_Abort == true)
                {
                    break;
                }
            }

        }

        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            Console.WriteLine(filename);
            filename = filename.Substring(1);
            if (string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in this._IndexFiles)
                {
                    if (File.Exists(Path.Combine(this._RootDirectory, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }

            filename = Path.Combine(this._RootDirectory, filename);
            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Adding permanent http response headers
                    string mime;
                    context.Response.ContentType = MimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime)
                        ? mime
                        : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));
                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();
                    context.Response.StatusCode = (int) HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                }
                catch (Exception)
                {
                    context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                }
            }
            else
            {
                context.Response.StatusCode = (int) HttpStatusCode.NotFound;
            }

            context.Response.OutputStream.Close();
        }

        private void Initialize(string path, int port)
        {
            this._RootDirectory = path;
            this._Port = port;
            this._ServerThread = new Thread(Listen);
            this._ServerThread.Start();
        }

        public void Dispose()
        {
            ((IDisposable) this._Listener)?.Dispose();
        }
    }
}
