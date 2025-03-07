using ECommerce.Models.ViewModels;
using ECommerce.Models;
using ECommerce.Repository.IRepository;
using ECommerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ECommerce.Areas.Customer.Controllers
{
    [Area("customer")]
    [Authorize]
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly EsewaPaymentService _esewaPaymentService;
        private const string SecretKey = "8gBm/:&EnhH.1/q";

        //private readonly IEmailSender _emailSender;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork, EsewaPaymentService esewaPaymentService)
        {
            _unitOfWork = unitOfWork;
            _esewaPaymentService = esewaPaymentService;
            //_emailSender = emailSender;
        }


        public IActionResult Index()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;


            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Product.ProductImages = productImages.Where(u => u.ProductId == cart.Product.Id).ToList();
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;



            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
        }
       

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
            ShoppingCartVM.OrderHeader.SessionId = Guid.NewGuid().ToString();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;

            // Generate Unique Transaction ID
            string transactionUuid = Guid.NewGuid().ToString();
            ShoppingCartVM.OrderHeader.PaymentIntentId = transactionUuid;

            // Save OrderHeader with UUID
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            // Construct the eSewa Payment URL
            string successUrl = $"{Request.Scheme}://{Request.Host}/customer/cart/PaymentSuccess";
            string failureUrl = $"{Request.Scheme}://{Request.Host}/customer/cart/PaymentFailure?orderId={ShoppingCartVM.OrderHeader.Id}";

            // Build the data to be sent to eSewa
            var formData = new Dictionary<string, string>
    {
        { "amount", ShoppingCartVM.OrderHeader.OrderTotal.ToString("F2") },
        { "tax_amount", "0" },
        { "total_amount", ShoppingCartVM.OrderHeader.OrderTotal.ToString("F2") },
        { "transaction_uuid", transactionUuid },
        { "product_code", "EPAYTEST" },
        { "product_service_charge", "0" },
        { "product_delivery_charge", "0" },
        { "success_url", successUrl },
        { "failure_url", failureUrl },
        { "signed_field_names", "total_amount,transaction_uuid,product_code" },
        { "signature", GenerateSignature(transactionUuid, ShoppingCartVM.OrderHeader.OrderTotal.ToString("F2")) },
        { "url", "https://rc-epay.esewa.com.np/api/epay/main/v2/form" } // Add the URL to the form data
    };


            // Return the view that will submit the form
            return View("EsewaForm", formData);
        }


        public IActionResult PaymentSuccess(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return BadRequest("Invalid eSewa response.");
            }

            var decodedBytes = Convert.FromBase64String(data);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            var responseObj = JsonSerializer.Deserialize<EsewaResponse>(decodedString);

            if (responseObj != null && responseObj.status == "COMPLETE")
            {              

                var orderHeader = _unitOfWork.OrderHeader.Get(u => u.PaymentIntentId == responseObj.transaction_uuid);
                if (orderHeader != null && orderHeader.PaymentIntentId == responseObj.transaction_uuid)
                {
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }

                List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

                _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
                _unitOfWork.Save();

                return View("OrderConfirmation", orderHeader.Id);

            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult PaymentFailure(int orderId, string transaction_uuid, string total_amount, string status)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId);
            if (orderHeader != null && orderHeader.PaymentIntentId == transaction_uuid)
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.PaymentStatusRejected);
                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index));
        }



        public static string GenerateSignature(string transactionUuid, string totalAmount)
        {
            string productCode = "EPAYTEST"; // Ensure this is consistent with your request

            string signedData = $"total_amount={totalAmount},transaction_uuid={transactionUuid},product_code={productCode}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedData));
                return Convert.ToBase64String(hash);
            }
        }


        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if (cartFromDb.Count <= 1)
            {

                //remove that from cart

                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);

            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);

            _unitOfWork.ShoppingCart.Remove(cartFromDb);

            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
              .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }



        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.ListPrice;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.ListPrice;
                }
                else
                {
                    return shoppingCart.Product.ListPrice;
                }
            }
        }
    }
}
