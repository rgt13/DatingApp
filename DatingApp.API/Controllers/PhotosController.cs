using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.DTOs;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("users/{userID}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(
            IDatingRepository repo,
            IMapper mapper,
            IOptions<CloudinarySettings> cloudinaryConfig
            ) {

            _repo = repo;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            Account account = new Account (
              _cloudinaryConfig.Value.CloudName,
              _cloudinaryConfig.Value.ApiKey,
              _cloudinaryConfig.Value.ApiSecret  
            );

            _cloudinary = new Cloudinary(account);
        }


        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id) {
            var photoFromRepo = await _repo.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }


        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(
            int userID, 
            [FromForm]PhotoForCreationDto photoForCreationDto
            ) {
            
            if (userID != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized();
            }

            var userFromRepo = await _repo.GetUser(userID);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();

            if(file.Length > 0) {
                using (var stream = file.OpenReadStream()) {
                    var uploadParams = new ImageUploadParams() {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation()
                            .Width(500)
                            .Height(500)
                            .Crop("fill")
                            .Gravity("face")
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicID = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if(!userFromRepo.photos.Any(u => u.IsMain)) {
                photo.IsMain = true;
            }

            userFromRepo.photos.Add(photo);

            if(await _repo.SaveAll()) {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new { userID = userID, id = photo.PhotoID }, photoToReturn);
            }

            return BadRequest("Could not add the photo.");
        }


        [HttpPost("{photoID}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userID, int photoID) 
        {
            if (userID != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) {
                return BadRequest("AHAHAHAHAH FUCK YOU");
            }

            var userFromRepo = await _repo.GetUser(userID);

            if(!userFromRepo.photos.Any(p => p.PhotoID == photoID))
            {
                return BadRequest("AHAHAHAHAH FUCK YOU TWICE");
            }

            var photoFromRepo = await _repo.GetPhoto(photoID);

            if(photoFromRepo.IsMain) return BadRequest("This is already the main photo.");

            var currentMainPhoto = await _repo.GetMainPhotoForUser(userID);
            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if(await _repo.SaveAll())
            {
                return NoContent();
            }

            return BadRequest("Could not set photo to main.");
        }

        [HttpDelete("{photoID}/delete")]
        public async Task<IActionResult> DeletePhoto(int userID, int photoID)
        {
            if (userID != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized();
            }

            var userFromRepo = await _repo.GetUser(userID);

            if(!userFromRepo.photos.Any(p => p.PhotoID == photoID))
            {
                return BadRequest("Attempt to delete photo failed.");
            }

            var photoToDelete = await _repo.GetPhoto(photoID);

            if(photoToDelete.IsMain)
            {
                return BadRequest("You cannot delete a main photo");
            }

            if(photoToDelete.PublicID != null)
            {
                var deleteParams = new DeletionParams(photoToDelete.PublicID);

                var result = _cloudinary.Destroy(deleteParams);

                if(result.Result == "ok")
                {
                   _repo.Delete(photoToDelete);
                    //userFromRepo.photos.Remove(photoToDelete);
                }
            } else {
                _repo.Delete(photoToDelete);
                //userFromRepo.photos.Remove(photoToDelete);
            }

            

            userFromRepo.photos.Remove(photoToDelete);

            if(await _repo.SaveAll())
            {
                return NoContent();
            }

            return BadRequest("Unable to delete photo.");
        }
    }
}