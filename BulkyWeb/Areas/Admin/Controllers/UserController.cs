using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagment(string userId)
        {
            string roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

            RoleManagmentVM roleManagmentVM = new()
            {
                ApplicationUser = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId),
                RoleList = _db.Roles.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Name
                }),
                CompanyList = _db.Companies.Select(u => new SelectListItem
                {
                    Text= u.Name,
                    Value = u.Id.ToString()
                }),
            };

            roleManagmentVM.ApplicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == roleId).Name;

            return View(roleManagmentVM);
        }

        [HttpPost]
        public IActionResult RoleManagment(RoleManagmentVM roleManagmentVM)
        {
            string roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == roleManagmentVM.ApplicationUser.Id).RoleId;
            string oldRole = _db.Roles.FirstOrDefault(u => u.Id == roleId).Name;

            if(roleManagmentVM.ApplicationUser.Role !=  oldRole)
            {
                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagmentVM.ApplicationUser.Id);
                if (roleManagmentVM.ApplicationUser.Role == SD.Role_Company)
                {
                    user.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
                }
                if(oldRole==SD.Role_Company)
                {
                    user.CompanyId = null;
                }
                _db.SaveChanges();

                _userManager.RemoveFromRoleAsync(user, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(user, roleManagmentVM.ApplicationUser.Role).GetAwaiter().GetResult();

            }

            return RedirectToAction(nameof(Index));
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUserList = _db.ApplicationUsers.Include(u => u.Company).ToList();

            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach (var user in objUserList)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;

                if (user.Company == null)
                {
                    user.Company = new() { Name = "" };
                }
            }

            return Json(new { data = objUserList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var objFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }
            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _db.SaveChanges();

            return Json(new { success = true, message = "Operation Successful" });
        }
        #endregion
    }
}
