using System.Net.Mime;
using System.Text.RegularExpressions;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Org.BouncyCastle.Crypto.Parameters;

namespace GZCTF.Controllers;

/// <summary>
/// Team related APIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public partial class TeamController(
    UserManager<UserInfo> userManager,
    IBlobRepository blobService,
    ILogger<TeamController> logger,
    ITeamRepository teamRepository,
    IParticipationRepository participationRepository,
    IStringLocalizer<Program> localizer) : ControllerBase
{
    const int MaxTeamsAllowed = 3;

    /// <summary>
    /// Get team information
    /// </summary>
    /// <remarks>
    /// Get basic information of a team by ID
    /// </remarks>
    /// <param name="id">Team ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team information</response>
    /// <response code="400">Team does not exist</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBasicInfo(int id, CancellationToken token)
    {
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// Get current team information
    /// </summary>
    /// <remarks>
    /// Get basic information of a team based on user
    /// </remarks>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team information</response>
    /// <response code="400">Team does not exist</response>
    [HttpGet]
    [RequireUser]
    [ProducesResponseType(typeof(TeamInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTeamsInfo(CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        return Ok((await teamRepository.GetUserTeams(user!, token)).Select(t => TeamInfoModel.FromTeam(t)));
    }

    /// <summary>
    /// Create team
    /// </summary>
    /// <remarks>
    /// User API for creating teams, each user can only create one team
    /// </remarks>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team information</response>
    /// <response code="400">Team does not exist</response>
    [HttpPost]
    [RequireUser]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Concurrency))]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTeam([FromBody] TeamUpdateModel model, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        var teams = await teamRepository.GetUserTeams(user!, token);

        if (teams.Count(t => t.CaptainId == user!.Id) >= MaxTeamsAllowed)
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Team_ExceededCreationLimit)]));

        if (string.IsNullOrEmpty(model.Name))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NameEmpty)]));

        if (model.Name is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_CreationFailed)]));

        var team = await teamRepository.CreateTeam(model, user!, token);

        await userManager.UpdateAsync(user!);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Team_Created), team.Name], user,
            TaskStatus.Success);

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// Update team information
    /// </summary>
    /// <remarks>
    /// Team information update API, must be team creator
    /// </remarks>
    /// <param name="id">Team ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team information</response>
    /// <response code="400">Team does not exist</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Access forbidden</response>
    [RequireUser]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTeam([FromRoute] int id, [FromBody] TeamUpdateModel model,
        CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)]));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                StatusCodes.Status403Forbidden))
            { StatusCode = StatusCodes.Status403Forbidden };

        team.UpdateInfo(model);

        await teamRepository.SaveAsync(token);

        return Ok(TeamInfoModel.FromTeam(team));
    }

    /// <summary>
    /// Transfer team ownership
    /// </summary>
    /// <remarks>
    /// Team ownership transfer API, must be team creator
    /// </remarks>
    /// <param name="id">Team ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team information</response>
    /// <response code="400">Team does not exist</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Access forbidden</response>
    [RequireUser]
    [HttpPut("{id:int}/Transfer")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Transfer([FromRoute] int id, [FromBody] TeamTransferModel model,
        CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        var trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            var team = await teamRepository.GetTeamById(id, token);

            if (team is null)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)]));

            if (team.CaptainId != user!.Id)
                return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                    StatusCodes.Status403Forbidden))
                { StatusCode = StatusCodes.Status403Forbidden };

            var newCaptain = await userManager.Users.SingleOrDefaultAsync(u => u.Id == model.NewCaptainId, token);

            if (newCaptain is null)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NewCaptainNotFound)]));

            var newCaptainTeams = await teamRepository.GetUserTeams(newCaptain, token);

            if (newCaptainTeams.Count(t => t.CaptainId == newCaptain.Id) >= MaxTeamsAllowed)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NewCaptainTeamTooMany)]));

            await teamRepository.Transfer(team, newCaptain, token);
            await trans.CommitAsync(token);

            return Ok(TeamInfoModel.FromTeam(team));
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }
    }

    /// <summary>
    /// Get invitation information
    /// </summary>
    /// <remarks>
    /// Get team invitation information, must be team creator
    /// </remarks>
    /// <param name="id">Team ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team token</response>
    /// <response code="400">Team does not exist</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Access forbidden</response>
    [RequireUser]
    [HttpGet("{id:int}/Invite")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteCode([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)]));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                StatusCodes.Status403Forbidden))
            { StatusCode = StatusCodes.Status403Forbidden };

        return Ok(team.InviteCode);
    }

    /// <summary>
    /// Update invitation token
    /// </summary>
    /// <remarks>
    /// Interface to update invitation token, must be team creator
    /// </remarks>
    /// <param name="id">Team ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team token</response>
    /// <response code="400">Team does not exist</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Access forbidden</response>
    [RequireUser]
    [HttpPut("{id:int}/Invite")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateInviteToken([FromRoute] int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)]));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                StatusCodes.Status403Forbidden))
            { StatusCode = StatusCodes.Status403Forbidden };

        team.UpdateInviteToken();

        await teamRepository.SaveAsync(token);

        return Ok(team.InviteCode);
    }

    /// <summary>
    /// Kick user
    /// </summary>
    /// <remarks>
    /// User kick API, kick user with corresponding ID, requires team creator permission
    /// </remarks>
    /// <param name="id">Team ID</param>
    /// <param name="userId">ID of user to be kicked</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team token</response>
    /// <response code="400">Team does not exist</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Access forbidden</response>
    [RequireUser]
    [HttpPost("{id:int}/Kick/{userId:guid}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> KickUser([FromRoute] int id, [FromRoute] Guid userId, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);

        var trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            var team = await teamRepository.GetTeamById(id, token);

            if (team is null)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)]));

            if (team.CaptainId != user!.Id)
                return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                    StatusCodes.Status403Forbidden))
                { StatusCode = StatusCodes.Status403Forbidden };

            if (team.Locked && await teamRepository.AnyActiveGame(team, token))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_Locked)]));

            var kickUser = team.Members.SingleOrDefault(m => m.Id == userId);
            if (kickUser is null)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.User_NotInTeam)]));

            team.Members.Remove(kickUser);
            await participationRepository.RemoveUserParticipations(user, team, token);

            await teamRepository.SaveAsync(token);
            await trans.CommitAsync(token);

            logger.Log(
                StaticLocalizer[nameof(Resources.Program.Team_MemberRemoved), team.Name,
                    kickUser.UserName ?? "null"], user,
                TaskStatus.Success);
            return Ok(TeamInfoModel.FromTeam(team));
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }
    }

    [GeneratedRegex(":\\d+:[0-9a-f]{32}")]
    private static partial Regex InviteCodeRegex();

    /// <summary>
    /// Accept invitation
    /// </summary>
    /// <remarks>
    /// Interface to accept invitation, requires User permission and not being in team
    /// </remarks>
    /// <param name="code">Team invitation token</param>
    /// <param name="cancelToken"></param>
    /// <response code="200">Accepted team invitation</response>
    /// <response code="400">Team does not exist</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Access forbidden</response>
    [RequireUser]
    [HttpPost("Accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Accept([FromBody] string code, CancellationToken cancelToken)
    {
        if (!InviteCodeRegex().IsMatch(code))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_InvalidCode)]));

        var inviteToken = code[^32..];
        var preCode = code[..^33];

        var lastColon = preCode.LastIndexOf(':');

        if (!int.TryParse(preCode[(lastColon + 1)..], out var teamId))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_IdTransformFailed),
                preCode[(lastColon + 1)..]]));

        var teamName = preCode[..lastColon];
        var trans = await teamRepository.BeginTransactionAsync(cancelToken);

        try
        {
            var team = await teamRepository.GetTeamById(teamId, cancelToken);

            if (team is null)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NameNotFound),
                    teamName]));

            if (team.InviteToken != inviteToken)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_NameInvalidInvitation),
                    teamName]));

            var user = await userManager.GetUserAsync(User);

            if (team.Members.Any(m => m.Id == user!.Id))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.User_AlreadyInTeam)]));

            team.Members.Add(user!);

            await teamRepository.SaveAsync(cancelToken);
            await trans.CommitAsync(cancelToken);

            logger.Log(StaticLocalizer[nameof(Resources.Program.Team_UserJoined), team.Name], user,
                TaskStatus.Success);
            return Ok();
        }
        catch
        {
            await trans.RollbackAsync(cancelToken);
            throw;
        }
    }

    /// <summary>
    /// Leave team
    /// </summary>
    /// <remarks>
    /// Interface to leave team, requires User permission and being in team
    /// </remarks>
    /// <param name="id">Team ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully left team</response>
    /// <response code="400">Team does not exist</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Access forbidden</response>
    [RequireUser]
    [HttpPost("{id:int}/Leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Leave([FromRoute] int id, CancellationToken token)
    {
        var trans = await teamRepository.BeginTransactionAsync(token);

        try
        {
            var team = await teamRepository.GetTeamById(id, token);

            if (team is null)
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                    StatusCodes.Status404NotFound));

            var user = await userManager.GetUserAsync(User);

            if (team.Members.All(m => m.Id != user!.Id))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.User_LeaveNotInTeam)]));

            if (team.Locked && await teamRepository.AnyActiveGame(team, token))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_Locked)]));

            team.Members.Remove(user!);
            await participationRepository.RemoveUserParticipations(user!, team, token);

            await teamRepository.SaveAsync(token);
            await trans.CommitAsync(token);

            logger.Log(StaticLocalizer[nameof(Resources.Program.Team_UserLeft), team.Name], user,
                TaskStatus.Success);
            return Ok();
        }
        catch
        {
            await trans.RollbackAsync(token);
            throw;
        }
    }

    /// <summary>
    /// Update team avatar
    /// </summary>
    /// <remarks>
    /// Use this API to update team avatar, requires User permission and team membership
    /// </remarks>
    /// <response code="200">User avatar URL</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized user</response>
    [RequireUser]
    [HttpPut("{id:int}/Avatar")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Avatar([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                StatusCodes.Status404NotFound));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                StatusCodes.Status403Forbidden))
            { StatusCode = StatusCodes.Status403Forbidden };

        switch (file.Length)
        {
            case 0:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));
            case > 3 * 1024 * 1024:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));
        }

        if (team.AvatarHash is not null)
            _ = await blobService.DeleteBlobByHash(team.AvatarHash, token);

        var avatar = await blobService.CreateOrUpdateImage(file, "avatar", 300, token);

        if (avatar is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_AvatarUpdateFailed)]));

        team.AvatarHash = avatar.Hash;
        await teamRepository.SaveAsync(token);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Team_AvatarUpdated), team.Name, avatar.Hash[..8]],
            user, TaskStatus.Success);

        return Ok(avatar.Url());
    }

    /// <summary>
    /// Delete team
    /// </summary>
    /// <remarks>
    /// User API for deleting team, requires User permission and team captain status
    /// </remarks>
    /// <param name="id">Team ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved team information</response>
    /// <response code="400">Team does not exist</response>
    [RequireUser]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(TeamInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteTeam(int id, CancellationToken token)
    {
        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(id, token);

        if (team is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                StatusCodes.Status404NotFound));

        if (team.CaptainId != user!.Id)
            return new JsonResult(new RequestResponse(localizer[nameof(Resources.Program.Auth_AccessForbidden)],
                StatusCodes.Status403Forbidden))
            { StatusCode = StatusCodes.Status403Forbidden };

        if (team.Locked && await teamRepository.AnyActiveGame(team, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Team_Locked)]));

        await teamRepository.DeleteTeam(team, token);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Team_Deleted), team.Name], user,
            TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// Verify signature
    /// </summary>
    /// <remarks>
    /// Perform signature verification
    /// </remarks>
    /// <response code="200">Signature valid</response>
    /// <response code="400">Input format error</response>
    /// <response code="401">Signature invalid</response>
    [HttpPost("Verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult VerifySignature(SignatureVerifyModel model)
    {
        // Token: <id>:<signature>
        // Data: $"GZCTF_TEAM_{team.Id}"

        var pk = Codec.Base64.DecodeToBytes(model.PublicKey);

        if (pk.Length != 32)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Signature_Invalid)]));

        var pos = model.TeamToken.IndexOf(':');

        if (pos == -1)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Signature_Invalid)]));

        var id = model.TeamToken[..pos];
        var sign = model.TeamToken[(pos + 1)..];

        if (!int.TryParse(id, out _) || string.IsNullOrEmpty(sign))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Signature_Invalid)]));

        Ed25519PublicKeyParameters publicKey = new(pk, 0);

        if (DigitalSignature.VerifySignature($"GZCTF_TEAM_{id}", sign, publicKey, SignAlgorithm.Ed25519))
            return Ok();

        return Unauthorized(new RequestResponse(localizer[nameof(Resources.Program.Signature_Invalid)]));
    }
}
