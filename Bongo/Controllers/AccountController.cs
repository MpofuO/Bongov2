using Bongo.Models.ViewModels;
using Bongo.Services;
using iTextSharp.text.pdf.parser;
using iTextSharp.text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Drawing;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Org.BouncyCastle.Bcpg;
using NuGet.Common;

namespace Bongo.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IMailService _mailSender;
        private readonly IConfiguration _config;

        public AccountController(UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager, IMailService mailSender, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _mailSender = mailSender;
            _config = configuration;
        }
        [TempData]
        public string Message { get; set; }




        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        [AllowAnonymous]
        public IActionResult SignIn(string returnUrl)
        {
            return View(new LoginViewModel
            { ReturnUrl = returnUrl }
                        );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel loginModel)
        {
            if (ModelState.IsValid)
            {
                IdentityUser user =
                await _userManager.FindByNameAsync(loginModel.Username);  //check if the user exists
                if (user != null)
                {

                    var result = await _signInManager.PasswordSignInAsync(user,
                       loginModel.Password, isPersistent: loginModel.RememberMe, false);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("TimeTableFileUpload", "Session");
                    }
                }
                ModelState.AddModelError("", "Invalid username or password");

            }
            return View("SignIn", loginModel);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterModel registerModel)
        {
            if (ModelState.IsValid)
            {
                if (await _userManager.FindByNameAsync(registerModel.UserName) != null)
                {
                    ModelState.AddModelError("", "Username already exists💀");
                    return View();
                }
                var user = new IdentityUser
                {
                    UserName = registerModel.UserName,
                    Email = registerModel.Email
                };

                var result = await _userManager.CreateAsync(user, registerModel.Password);

                if (result.Succeeded)
                {
                    try
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        Dictionary<string, string> emailOptions = new Dictionary<string, string>
                        { { "username", user.UserName},{ "link",_config.GetValue<string>("Application:AppDomain") + $"Account/ConfirmEmail?userId={user.Id}&token{token}"
                        } };

                        await _mailSender.SendMailAsync(registerModel.Email, "Welcome to Bongo", "WelcomeEmail", emailOptions);
                        Message = "Successfully registered";
                    }
                    catch (Exception)
                    {
                        await _userManager.DeleteAsync(user);
                        ModelState.AddModelError("", "Something went wrong while registering your account. It's not you, it's us💀");
                        return View();
                    }

                    return RedirectToAction("SignIn");
                }

                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }

            }
            return View(registerModel);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ConfirmEmail(string userId, string token)
        {
            return View(new ConfirmEmail { UserId = userId, Token = token});
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmail model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user != null)
                {
                    var result = await _userManager.ConfirmEmailAsync(user, model.Token);
                    if (result.Succeeded)
                    {
                        Message = "Successfully registered";
                    }
                    else
                    {
                        
                    }
                    TempData["Message"] = "Email verified successfully";
                }
                TempData["Message"] = "Something went wrong😐.";

                return RedirectToAction("SignIn");
            }
            return View(model);
            
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPassword model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    Dictionary<string, string> emailOptions = new Dictionary<string, string>
                    {
                        { "username", user.UserName },
                        {
                            "link", _config.GetValue<string>("Application:AppDomain") + $"Account/ResetPassword?userId={user.Id}&token{token}"
                        }

                    };
                    await _mailSender.SendMailAsync(user.Email, "Reset Password", "ForgotPassword", emailOptions);
                    TempData["Message"] = $"Reset email was sent to {user.Email}";
                    return View();
                }
                ModelState.AddModelError("", "Invalid. Email not regestered");
            }
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            return View(new ResetPassword() { Token = token, UserId = userId });
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user != null)
                {
                    var result = await _userManager.ResetPasswordAsync(user, model.Token, model.ConfirmPassword);
                    if (result.Succeeded)
                    {
                        TempData["Message"] = "Successfully reset password";
                        return RedirectToAction("SignIn");
                    }
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
