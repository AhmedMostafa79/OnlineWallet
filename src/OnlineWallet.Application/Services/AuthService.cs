using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Helpers;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Models;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Services
{
    /// <summary>
    /// Service implementation for authentication operations.
    /// Handles user registration and login with JWT token generation.
    /// </summary>
    public class AuthService: IAuthService
    {
        private readonly ICustomerService _customerService;
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Initializes a new instance of AuthService.
        /// </summary>
        /// <param name="customerService">Service for customer operations</param>
        /// <param name="tokenService">Service for token generation</param>
        public AuthService(ICustomerService customerService, ITokenService  tokenService)
        {
            _customerService = customerService;
            _tokenService= tokenService;
        }

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="model">User registration details including email, password, and personal information</param>
        /// <returns>Authentication response token and user details</returns>
        /// <exception cref="InvalidOperationException">Thrown when email is already registered</exception>
        /// <exception cref="Exception">Wrapped exception when registration fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Creates a token upon successful registration.
        /// </remarks>
        public async Task<AuthResponseModel> RegisterAsync(RegisterModel model)
        {

            if (await _customerService.IsEmailRegistered(model.Email))
                throw new InvalidOperationException("Email is already registered");
            
            try
            {
                var newUser= await _customerService.CreateCustomerAsync(model);
                var token = _tokenService.GetToken(newUser);
                return new AuthResponseModel
                {
                    Email = newUser.Email,
                    ExpiresIn = token.ExpiresAt,
                    IsAuthenticated = true,
                    Role = newUser.Role,
                    Token = token.Token,
                    UserName = $"{newUser.FirstName} {newUser.LastName}"
                };
            }
            catch(Exception ex) when (ExceptionHelper.IsBusinessException(ex))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to register customer with email {model.Email}",ex);
            }

        }

        /// <summary>
        /// Authenticates an existing user.
        /// </summary>
        /// <param name="model">User login credentials (email and password)</param>
        /// <returns>Authentication response with JWT token and user details</returns>
        /// <exception cref="InvalidOperationException">Thrown when credentials are invalid</exception>
        /// <exception cref="Exception">Wrapped exception when login fails</exception>
        /// <remarks>
        /// Business exceptions bubble up through ExceptionHelper.IsBusinessException filter.
        /// System exceptions are wrapped with context information.
        /// Validates password and generates a token upon successful authentication.
        /// </remarks>
        public async Task<AuthResponseModel> LoginAsync(TokenRequestModel model)
        {
            try
            {
                var user = await _customerService.FindByEmailAsync(model.Email);
                if (!await _customerService.CheckUserPasswordAsync(user.Id, model.Password))
                {
                    throw new InvalidOperationException("Invalid email or password");
                }

                var token = _tokenService.GetToken(user);
                return new AuthResponseModel
                {
                    Email = user.Email,
                    ExpiresIn = token.ExpiresAt,
                    IsAuthenticated = true,
                    Role = user.Role,
                    Token = token.Token,
                    UserName = $"{user.FirstName} {user.LastName}"
                };
            }
            catch (Exception ex) when (ExceptionHelper.IsBusinessException(ex)) {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to Login user with email {model.Email}", ex);
            }
        }
    }
}
