using Bongo.Data;
using Bongo.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bongo.Controllers
{
    [Authorize]
    public class UserReviewController : Controller
    {
        private readonly IRepositoryWrapper _repo;

        public UserReviewController(IRepositoryWrapper repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public IActionResult AddReview()
        {
            UserReview model = _repo.UserReview.FindAll().FirstOrDefault(r => r.Username == User.Identity.Name);
            return View(model ?? new UserReview());
        }
        [HttpPost]
        public IActionResult AddReview(UserReview model)
        {
            try
            {

                model.Username = User.Identity.Name;
                if (_repo.UserReview.FindAll().FirstOrDefault(r => r.Username == model.Username) != null)
                {
                    model.ReviewDate = DateTime.Now; 
                    _repo.UserReview.Update(model);
                    TempData["Message"] = "Review updated successfully. Thank you";
                }
                else
                {
                    model.ReviewDate = DateTime.Now;
                    _repo.UserReview.Create(model);
                    TempData["Message"] = "Review submited successfully. Thank you";
                }
                _repo.SaveChanges();
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Someting went wrong while saving. Please try again later.😔");
            }
            return View(model);
        }
    }
}
