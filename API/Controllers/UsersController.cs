using System.Collections.Generic;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using API.Interfaces;
using API.DTOs;
using AutoMapper;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace API.Controllers
{
  [Authorize]
  public class UsersController : BaseApiController
  {
    private IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;

    public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
    {
      _photoService = photoService;
      _mapper = mapper;
      _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
    {
      var users = await _userRepository.GetMembersAsync();
      return Ok(users);
    }

    [HttpGet("{username}", Name="GetUSer")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
      return await _userRepository.GetMemberAsync(username);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
    {
      var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var user = await _userRepository.GetUserByUsernameAsync(username);

      _mapper.Map(memberUpdateDto, user);
      _userRepository.Update(user);

      if (await _userRepository.SaveAllSync()) return NoContent();

      return BadRequest("Failed to update the user.");

    }


    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDto>> AddPhoto([FromForm]IFormFile file)
    {
      var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var user = await _userRepository.GetUserByUsernameAsync(username);

      var result = await _photoService.AddPhotoAsync(file);

      if(result.Error != null) return BadRequest(result.Error.Message);

      var photo = new Photo
      {
        Url = result.SecureUrl.AbsoluteUri,
        PublicId = result.PublicId
      };

      if(user.Photos.Count == 0)
      {
        photo.IsMain = true;
      }

      user.Photos.Add(photo);

      if(await _userRepository.SaveAllSync())
      {
        // return _mapper.Map<PhotoDto>(photo);
        return CreatedAtRoute("GetUser", new { username = user.UserName },_mapper.Map<PhotoDto>(photo));
      }

      return BadRequest("Problem adding photo.");

    }
  

    [HttpPut("set-main-photo/{photoId}")]
    public async Task<ActionResult> SetMainPhoto(int photoId) 
    {

      var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      System.Console.WriteLine(username);
      var user = await _userRepository.GetUserByUsernameAsync(username);
      System.Console.WriteLine(user);

      var photo = user.Photos.FirstOrDefault(x => x.Id == photoId); 
      if(photo.IsMain) return BadRequest("This is already your main photo");
     
      var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
      if (currentMain != null) currentMain.IsMain = false;
      photo.IsMain = true;

      if(await _userRepository.SaveAllSync()) return NoContent();

      return BadRequest("Failed to set main photo!");
    }


    [HttpDelete("delete-photo/{photoId}")]
    public async Task<ActionResult> DeletePhoto(int photoId) 
    {
      var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var user = await _userRepository.GetUserByUsernameAsync(username);

      var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

      if(photo == null) return NotFound();

      if(photo.IsMain) return BadRequest("You cannot delete your main photo");

      if(photo.PublicId != null)
      {
        var result = await _photoService.DeletePhotoAsync(photo.PublicId);
        if(result.Error != null) return BadRequest(result.Error.Message);
      }


      user.Photos.Remove(photo);

      if(await _userRepository.SaveAllSync()) return Ok();

      return BadRequest("Failed to delete the photo.");
    }

  }

}