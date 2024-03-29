﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PIZZA.Infrastructure;
using PIZZA.Models;
using PIZZA.Models.ViewModels;
using System.Security.Claims;

namespace PIZZA.Controllers
{

	public class CartController : Controller
	{
		private readonly ApplicationDbContext _context;

		public CartController(ApplicationDbContext context)
		{
			_context = context;

		}
		public IActionResult Index()
		{
			List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

			CartViewModel cartVM = new CartViewModel
			{
				CartItems = cart,
				GrantTotal = cart.Sum(x => x.Quantity * x.Price)
			};


			return View(cartVM);
		}

		public async Task<IActionResult> Add(int id)
		{
			Product product = await _context.Products.FindAsync(id);

			List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

			CartItem cartItem = cart.Where(c => c.ProductId == id).FirstOrDefault();

			if (cartItem == null)
			{
				cart.Add(new CartItem(product));
			}
			else
			{
				cartItem.Quantity += 1;
			}

			HttpContext.Session.SetJson("Cart", cart);

			TempData["Success"] = "Produkt został dodany do koszyka!";

			return Redirect(Request.Headers["Referer"].ToString());
		}
		public async Task<IActionResult> Decrease(int id)
		{
			List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart");

			CartItem cartItem = cart.Where(c => c.ProductId == id).FirstOrDefault();

			if (cartItem.Quantity > 1)
			{
				--cartItem.Quantity;
			}
			else
			{
				cart.RemoveAll(x => x.ProductId == id);
			}

			if (cart.Count == 0)
			{
				HttpContext.Session.Remove("Cart");
			}
			else
			{
				HttpContext.Session.SetJson("Cart", cart);
			}

			TempData["Success"] = "Produkt został usunięty z koszyka!";

			return RedirectToAction("Index");
		}
		public async Task<IActionResult> Clear(int id)
		{
			HttpContext.Session.Remove("Cart");

			TempData["Success"] = "Wszystkie produkty zostały usunięte!";

			return RedirectToAction("Index");
		}
		[HttpPost]
		[Authorize]
		public IActionResult Checkout(Order order)
		{

			List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

			if (cart.Count == 0)
			{
				TempData["Error"] = "Twój koszyk jest pusty, nie można dokonać zamówienia.";
				return RedirectToAction("Index", "Home");
			}

			order.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			order.OrderDate = DateTime.Now;
			order.Total = cart.Sum(x => x.Quantity * x.Price);
			order.CartItems = cart;

			_context.Orders.Add(order);
			_context.SaveChanges();

			HttpContext.Session.Remove("Cart");

			TempData["Success"] = "Twoje zamówienie zostało złożone! Numer Twojego zamówienia to: " + order.OrderNumber;
			return RedirectToAction("Index", "Home");
		}
	}
}
