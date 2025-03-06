using ECommerce.Models.ViewModels;
using ECommerce.Models;
using ECommerce.Repository.IRepository;
using ECommerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace ECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> objCatagoryList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();

            return View(objCatagoryList);
        }
        //Update and Insert  
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {

                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                Product = new Product()

            };
            if (id == null || id == 0)
            {
                //Create
                return View(productVM);
            }
            else
            {
                //Update
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "ProductImages");
                return View(productVM);
            }

        }
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Upsert(ProductVM productVM, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                    TempData["success"] = "Product Created Successfully";
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                    TempData["success"] = "Product Updated Successfully";
                }
                _unitOfWork.Save();

                if (files != null)
                {
                    foreach (IFormFile file in files)
                    {
                        // Upload image to ImgBB API
                        string imgBBApiKey = "0243c3e33d80257e55665b4642334aad"; // Your ImgBB API Key
                        string imgBBApiUrl = "https://api.imgbb.com/1/upload";
                        string boundary = "---------------------------boundary";
                        string lineEnd = "\r\n";

                        var request = new HttpRequestMessage(HttpMethod.Post, imgBBApiUrl);
                        var multipartContent = new MultipartFormDataContent(boundary);

                        multipartContent.Add(new StringContent(imgBBApiKey), "key");
                        multipartContent.Add(new StreamContent(file.OpenReadStream()), "image", file.FileName);

                        request.Content = multipartContent;

                        var client = new HttpClient();
                        var response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var imgBBResponse = JsonConvert.DeserializeObject<ImgBBResponse>(responseContent);

                            // Save the ImgBB image URL to your database
                            ProductImage productImage = new()
                            {
                                ImageUrl = imgBBResponse.data.url,
                                ProductId = productVM.Product.Id,
                            };

                            if (productVM.Product.ProductImages == null)
                                productVM.Product.ProductImages = new List<ProductImage>();

                            productVM.Product.ProductImages.Add(productImage);

                            _unitOfWork.Product.Update(productVM.Product);
                            _unitOfWork.Save();
                        }
                        else
                        {
                            // Handle failed upload
                            TempData["error"] = "Failed to upload image to ImgBB";
                        }
                    }
                }

                TempData["success"] = "Product created/updated successfully";

                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(productVM);
            }
        }

        // Helper class to deserialize ImgBB response
        public class ImgBBResponse
        {
            public ImgBBData data { get; set; }
            public bool success { get; set; }
            public int status { get; set; }
        }

        public class ImgBBData
        {
            public string url { get; set; }
            // Add other properties as needed
        }


        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            if (imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath =
                                   Path.Combine(_webHostEnvironment.WebRootPath,
                                   imageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted successfully";
            }

            return RedirectToAction(nameof(Upsert), new { id = productId });
        }


        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(
                includeProperties: "Category").ToList();
            return Json(new { data = objProductList });

        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, Message = "Error While Deleting" });
            }

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }

                Directory.Delete(finalPath);
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}