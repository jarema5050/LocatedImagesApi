using blob.Data;
using blob.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace blob.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        static CloudBlobClient _blobClient;
        const string blobContainerName = "imagecontainer";
        private readonly IConfiguration _configuration;
        static CloudBlobContainer _blobContainer;
        public HomeController(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }
       public async Task<ActionResult> Index()
        {
            try
            {
                var storageConnectionString = _configuration.GetValue<string>("StorageConnectionString");
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                _blobClient = storageAccount.CreateCloudBlobClient();
                _blobContainer = _blobClient.GetContainerReference(blobContainerName);
                await _blobContainer.CreateIfNotExistsAsync();

                await _blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                List<Uri> allBlobs = new List<Uri>();
                BlobContinuationToken blobContinuationToken = null;
                do
                {
                    var response = await _blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);

                    foreach (IListBlobItem blob in response.Results)
                    {
                        if (blob.GetType() == typeof(CloudBlockBlob))
                            allBlobs.Add(blob.Uri);
                    }
                    blobContinuationToken = response.ContinuationToken;
                }
                while (blobContinuationToken != null);

                return View(allBlobs);
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> AddLocatedImage([FromBody] LocatedImageDto locatedImageDto)
        {
            var locationDto = locatedImageDto.LocationDto;

            var byteArray = Convert.FromBase64String(locatedImageDto.Base64);
            var memoryStream = new MemoryStream(byteArray);
            IFormFile file = new FormFile(memoryStream, 0, byteArray.Length, "name", "fileName.jpg");
            
                if (file.Length > 0 && locationDto != null)
                {
                    var randomName = GetRandomBlobName(file.FileName);
                    var blob = _blobContainer.GetBlockBlobReference(randomName);
                    
                    using (var stream = file.OpenReadStream())
                    {
                        await blob.UploadFromStreamAsync(stream); 
                    }
                    var user = _context.Users.SingleOrDefault(u => u.Id == Convert.ToInt64(locatedImageDto.UserId));
                    var imageLocation = new ImageLocation()
                    {
                        Uri = blob.Uri.ToString(),
                        Latitude = locationDto.Latitude,
                        Longitude = locationDto.Longitude,
                        User = user,
                        UserId = Convert.ToInt64(locatedImageDto.UserId)
                    };

                    _context.ImageLocations.Add(imageLocation);
                    _context.SaveChanges();
                    return Ok();
                }
                else
                    return NoContent();
            
        }

        [HttpPost]
        public ActionResult AddUser([FromBody] UserDto userDto)
        {
            if (_context.Users.Count(u => u.Email == userDto.Email) == 0)
            {
                _context.Add(new User()
                {
                    Email = userDto.Email,
                    Password = userDto.Password
                });
                _context.SaveChanges();
                return Ok();

            }
            else
                return BadRequest("User with such email egsist in app.");
        }
        [HttpGet]
        public List<User> GetUsers()
        {
            return _context.Users.ToList();
            
        }
        [HttpPost]
        public LoginData Login([FromBody] UserDto userDto)
        {
            var user = _context.Users.Where(u => u.Email == userDto.Email).FirstOrDefault();

            if (user == null)
            {
                return new LoginData()
                {
                    Status = "Rejected",
                    Message = "Specified user not egsist."
                };
            }

            if (user.Password == userDto.Password)
            {
                return new LoginData()
                {
                    Email = user.Email,
                    Id = user.Id,
                    Status = "Ok",
                    Message = "Succesfully logged in."
                };
            }
            else {
                return new LoginData()
                {
                    Status = "Rejected",
                    Message = "Given password is incorrect."
                };
            }
            
        }

        [HttpGet]

        public List<ImageLocation> GetBlobs(long userId)
        {
            var locationList = _context.ImageLocations.Where(loc => loc.UserId == userId).ToList();
            if (locationList.Count == 0)
                return null;
            return locationList;

        }

        [HttpGet]

        public List<ImageLocation> GetAllBlobs()
        {
            var locationList = _context.ImageLocations.ToList();
            if (locationList.Count == 0)
                return null;
            return locationList;
        }

        [HttpPost]
        public async Task<ActionResult> UploadAsync()
        {
            try
            {
                var request = await HttpContext.Request.ReadFormAsync();

                if (request.Files == null)
                    return BadRequest("Couldn't upload files");
                var files = request.Files;
                if (request.Files == null)
                    return BadRequest("Couldn't upload empty files");

                for (int i = 0; i < files.Count; i++)
                {
                    var blob = _blobContainer.GetBlockBlobReference(GetRandomBlobName(files[i].FileName));
                    
                    using (var stream = files[i].OpenReadStream())
                    {
                        await blob.UploadFromStreamAsync(stream);
                    } 
                }
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        /// <summary> 
        /// Task<ActionResult> DeleteImage(string name) 
        /// Documentation References:  
        /// - Delete Blobs: https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#delete-blobs
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> DeleteImage(string name)
        {
            try
            {
                Uri uri = new Uri(name);
                string filename = Path.GetFileName(uri.LocalPath);

                var blob = _blobContainer.GetBlockBlobReference(filename);
                await blob.DeleteIfExistsAsync();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        /// <summary> 
        /// Task<ActionResult> DeleteAll(string name) 
        /// Documentation References:  
        /// - Delete Blobs: https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/#delete-blobs
        /// </summary> 
        [HttpPost]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                BlobContinuationToken blobContinuationToken = null;
                do
                {
                    var response = await _blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);

                    foreach (IListBlobItem blob in response.Results)
                    {
                        if (blob.GetType() == typeof(CloudBlockBlob))
                            await ((CloudBlockBlob)blob).DeleteIfExistsAsync();
                    }
                    blobContinuationToken = response.ContinuationToken;
                }
                while (blobContinuationToken != null);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        /// <summary> 
        /// string GetRandomBlobName(string filename): Generates a unique random file name to be uploaded  
        /// </summary> 
        private string GetRandomBlobName(string filename)
        {
            string ext = Path.GetExtension(filename);
            return string.Format("{0:10}_{1}{2}", DateTime.Now.Ticks, Guid.NewGuid(), ext);
        }
    }
}
