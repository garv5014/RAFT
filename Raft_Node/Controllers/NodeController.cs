using Microsoft.AspNetCore.Mvc;
using Raft_Library;
using Raft_Node.Options;

namespace Raft_Node.controllers;

[ApiController]
[Route("api/[controller]")]
public class NodeController : ControllerBase
{
    public RaftNodeService node { get; }
    private readonly ApiOptions _apiOptions;
    private readonly ILogger<NodeController> _logger;

    public NodeController(ApiOptions apiOptions, RaftNodeService raftNodeService, ILogger<NodeController> logger)
    {
        _apiOptions = apiOptions;
        node = raftNodeService;
        _logger = logger;
    }


    [HttpGet]
    public string Get()
    {
        return "You talked to node " + _apiOptions.NodeIdentifier;
    }

    [HttpGet("identify")]
    public int Identify()
    {
        return _apiOptions.NodeIdentifier;
    }

    [HttpPost("append-entries")]
    public IActionResult AppendEntriesHeartbeat(AppendEntriesRequest request)
    {
        node.AppendEntries(request);
        return Ok();
    }

    [HttpPost("append-entry")]
    public IActionResult AppendEntry(AppendEntryRequest request)
    {
        if (node.AppendEntry(request))
            return Ok();
        return BadRequest("Failed to append entry.");
    }

    [HttpPost("request-vote")]
    public ActionResult<VoteResponse> RequestVote(VoteRequest request)
    {
        var votedYes = node.VoteForCandidate(request);
        return new VoteResponse
        {
            VotedId = node.Id,
            VoteGranted = votedYes
        };
    }

    [HttpGet("whoisleader")]
    public ActionResult<int> WhoIsLeader()
    {
        if (node.LeaderId == 0)
        {
            return NotFound();
        }
        if (node.IsLeader)
        {
            return node.Id;
        }
        return node.LeaderId;
    }

    [HttpGet("StrongGet")]
    public async Task<ActionResult<VersionedValue<string>>> StrongGet([FromQuery] string key)
    {
        _logger.LogInformation("StrongGet called with key: {key}", key);
        if (node.IsLeader)
        {
            return await node.StrongGet(key);
        }
        else
        {
            return StatusCode(404, "This node is not the leader");
        }
    }

    [HttpGet("EventualGet")]
    public VersionedValue<string> EventualGet([FromQuery] string key)
    {
        _logger.LogInformation("EventualGet called with key: {key}", key);
        return node.EventualGet(key);
    }

    [HttpPost("CompareAndSwap")]
    public async Task<ActionResult> CompareAndSwap(CompareAndSwapRequest request)
    {
        _logger.LogInformation("CompareAndSwap called with key: {key}, oldValue: {value}, newValue: {newValue}", request.Key, request.OldValue, request.NewValue);
        await node.CompareAndSwap(request.Key, request.OldValue, request.NewValue);
        return Ok();
    }
}
