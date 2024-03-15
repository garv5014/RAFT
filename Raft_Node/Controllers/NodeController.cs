using Microsoft.AspNetCore.Mvc;
using Raft_Library;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Raft_Node.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NodeController : ControllerBase
    {
        public RaftNodeService RaftNodeService { get; }
        private readonly ApiOptions apiOptions;
        private readonly HttpClient client;

        public NodeController(ApiOptions apiOptions, RaftNodeService raftNodeService, HttpClient client)
        {
            this.apiOptions = apiOptions;
            RaftNodeService = raftNodeService;
            this.client = client;
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
        public void ReceiveHeartbeat([FromQuery] int term, [FromQuery] int leaderId)
        {
            RaftNodeService.ReceiveHeartbeat(term, leaderId);
        }

        [HttpGet("whoisleader")]
        public ActionResult<int> WhoLeader()
        {
            if (RaftNodeService.MostRecentLeaderId == 0)
            {
                return NotFound();
            }
            if (RaftNodeService.GetState() == RaftNodeState.Leader)
            {
                return apiOptions.NodeIdentifier;
            }
            return RaftNodeService.MostRecentLeaderId;
        }

        [HttpGet("StrongGet")]
        public async Task<ActionResult<VersionedValue<string>>> StrongGet([FromQuery] string key)
        {
            if (RaftNodeService.GetState() != RaftNodeState.Leader)
            {
                throw new Exception("Not the leader.");
            }
            var confirmLeaderCount = 1;
            foreach (var nodeAddr in RaftNodeService.otherNodeAddresses)
            {
                if (await ConfirmLeader(nodeAddr))
                {
                    confirmLeaderCount++;
                }
                if (confirmLeaderCount > apiOptions.NodeCount / 2)
                {
                    if (RaftNodeService.Log.ContainsKey(key))
                    {
                        return RaftNodeService.Log[key];
                    }
                    else
                    {
                        throw new Exception("Value not found.");
                    }
                }
            }
            throw new Exception("Not the leader.");

        }

        [HttpGet("EventualGet")]
        public async Task<ActionResult<VersionedValue<string>>> EventualGet([FromQuery] string key)
        {
            if (RaftNodeService.Log.ContainsKey(key))
            {
                return RaftNodeService.Log[key];
            }
            else
            {
                return new VersionedValue<string> { Value = string.Empty, Version = 0 };
            }
        }

        [HttpPost("CompareAndSwap")]
        public async Task<ActionResult> CompareAndSwap(string key, string? oldValue, string newValue)
        {
            if (RaftNodeService.GetState() != RaftNodeState.Leader)
            {
                throw new Exception("Not the leader.");
            }

            var newIndex = 1;
            if (Directory.Exists(apiOptions.EntryLogPath))
            {
                newIndex = new DirectoryInfo(apiOptions.EntryLogPath).GetFiles().Length + 1;
            }
            if (RaftNodeService.Log.ContainsKey(key) && oldValue != RaftNodeService.Log[key].Value)
                throw new Exception("Value does not match.");

            if (await RaftNodeService.BroadcastReplication(key, newValue, newIndex))
            {
                // if majority of nodes have replicated the log, update the data
                RaftNodeService.Log[key] = new VersionedValue<string> { Value = newValue, Version = newIndex };
                RaftNodeService.CommittedIndex = newIndex;
                return Ok();
            }
            throw new Exception("Could not replicate to majority of nodes.");
        }

        public async Task<bool> ConfirmLeader(string addr)
        {
            var leaderId = await client.GetFromJsonAsync<int>($"{addr}/api/node/whoisleader");
            if (leaderId == apiOptions.NodeIdentifier)
            {
                return true;
            }
            return false;
        }
    }
}
