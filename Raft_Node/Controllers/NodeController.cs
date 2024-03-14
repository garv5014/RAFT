using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Raft_Node.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NodeController : ControllerBase
    {
        public RaftNodeService RaftNodeService { get; }
        private readonly IConfiguration configuration;
        private readonly ApiOptions apiOptions;

        public NodeController(IConfiguration configuration, ApiOptions apiOptions, RaftNodeService raftNodeService)
        {
            this.configuration = configuration;
            this.apiOptions = apiOptions;
            RaftNodeService = raftNodeService;
        }


        [HttpGet]
        public string Get()
        {
            return "You talked to node " + apiOptions.NodeIdentifier;
        }

        [HttpGet("identify")]
        public int Identify()
        {
            return apiOptions.NodeIdentifier;
        }

        [HttpGet("receiveVoteRequest")]
        public ActionResult<VoteResponse> ReceiveVoteRequest([FromQuery] int term, [FromQuery] Guid voteForName)
        {
            var successs = RaftNodeService.ReceiveVoteRequest(term, voteForName);
            var res = new VoteResponse
            {
                VotedId = apiOptions.NodeIdentifier,
                VoteGranted = successs
            };
            return res;
        }

        [HttpPost("appendEntries")]
        public void AppendEntries([FromBody] AppendEntriesRequest request)
        {
            RaftNodeService.AppendEntries(request);
        }

        [HttpGet("receiveHeartbeat")]
        public void ReceiveHeartbeat([FromQuery] int term, [FromQuery] Guid leaderId)
        {
            RaftNodeService.ReceiveHeartbeat(term, leaderId);
        }
    }
}
