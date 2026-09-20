using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Info;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[RequireAdmin]
[ApiController]
[Route("api/edit")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class EditPostsController(
    UserManager<UserInfo> users,
    IPostRepository posts) : ControllerBase
{
    [HttpPost("Posts")]
    public async Task<IActionResult> Create([FromBody] PostEditModel model, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var post = await posts.CreatePost(new Post().Update(model, user), token);
        return Ok(post.Id);
    }

    [HttpPut("Posts/{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PostEditModel model, CancellationToken token)
    {
        var post = await posts.GetPostById(id, token);
        if (post is null) return NotFound();
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        await posts.UpdatePost(post.Update(model, user), token);
        return Ok(PostDetailModel.FromPost(post));
    }

    [HttpDelete("Posts/{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken token)
    {
        var post = await posts.GetPostById(id, token);
        if (post is null) return NotFound();
        await posts.RemovePost(post, token);
        return Ok();
    }
}
