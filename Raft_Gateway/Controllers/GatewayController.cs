using Microsoft.AspNetCore.Mvc;
using Raft_Gateway.Options;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Raft_Gateway.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GatewayController : ControllerBase
    {


        private readonly ILogger<GatewayController> _logger;
        private readonly ApiOptions options;
        private readonly HttpClient client;
        private List<string> nodeAddresses = new List<string>();

        public GatewayController(ILogger<GatewayController> logger, ApiOptions options, HttpClient client)
        {
            _logger = logger;
            this.options = options;
            this.client = client;
            InitializeNodeAddresses(options);
        }

        private void InitializeNodeAddresses(ApiOptions options) // Thanks Caleb
        {
            nodeAddresses = [];
            for (var i = 1; i <= options.NodeCount; i++)
            {
                nodeAddresses.Add($"http://{options.NodeServiceName}{i}:{options.NodeServicePort}");
            }
        }

        // GET api/<GatewayController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            client.BaseAddress = new Uri(nodeAddresses[id]);

            var response = client.GetAsync("api/node").Result;
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Request to node {id} successful", id);
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                _logger.LogError("Request to node {id} failed", id);
                return "Error";
            }
        }
    }
}
