using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.DTOs;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route( "[controller]" )] // <--- https://localhost:5001/auth
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthController( IAuthRepository repo, IConfiguration config, IMapper mapper )
        {
            _repo = repo;
            _config = config;
            _mapper = mapper;
        }

        [HttpPost( "register" )]
        public async Task<IActionResult> Register( UserForRegisterDto userForRegisterDto )
        {
            //validate request
            if( !ModelState.IsValid ) return BadRequest( ModelState );

            userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

            if( await _repo.UserExists( userForRegisterDto.Username ) ) return BadRequest( "Username already exists" );

            var userToCreate = new User {
                Username = userForRegisterDto.Username
            };
            
            var createdUser = await _repo.Register( userToCreate, userForRegisterDto.Password );

            return StatusCode( 201 ); // TODO: Replace this with routing to page with created user
        }

        [HttpPost( "login" )]
        public async Task<IActionResult> Login( UserForLoginDto userForLoginDto )
        {
            var userFromRepo = await _repo.Login( userForLoginDto.Username, userForLoginDto.Password );

            if( userFromRepo == null ) return Unauthorized();

            var claims = new[] {
                new Claim( ClaimTypes.NameIdentifier, userFromRepo.UserID.ToString() ),
                new Claim( ClaimTypes.Name, userFromRepo.Username )                
            };

            var key = new SymmetricSecurityKey( Encoding.UTF8.GetBytes( _config.GetSection( "AppSettings:Token" ).Value ) );

            var creds = new SigningCredentials( key, SecurityAlgorithms.HmacSha512Signature );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken( tokenDescriptor );

            var user = _mapper.Map<UserForListDto>(userFromRepo);

            return Ok( new { token = tokenHandler.WriteToken( token ), user } ); // TODO: Replace this with routing to page with login user
        }
    }
}