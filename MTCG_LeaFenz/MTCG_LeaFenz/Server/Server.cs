using MTCG_LeaFenz.BusinessLayer.Controllers;
using MTCG_LeaFenz.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MTCG_LeaFenz.Server
{
    internal class Server
    {
        private readonly HttpListener _listener;
        private readonly int _port = 10001;
        private readonly RequestHandler _handler;

        public Server()
        {
            string connectionString = "Host=localhost;Username=postgres;Password=lea1234;Database=postgres;";
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _handler = new RequestHandler(new UserManager(new Database(), connectionString), connectionString);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Server läuft auf http://localhost:{_port}/");

            while (true)
            {
                var context = _listener.GetContext();
                _handler.HandleRequest(context);
            }
        }
    }
}
